Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

''' <summary>Producto del consolidado: lo que piden todas las operaciones juntas en el periodo.</summary>
Public NotInheritable Class ConsolidadoLineaDto
    Public Property ProductoBaseId As Long
    Public Property ProductoCodigo As String
    Public Property Producto As String
    Public Property Unidad As String
    Public Property Familia As String
    Public Property DemandaU6 As Long
    Public Property StockU6 As Long
    Public Property ReservaU6 As Long
    Public Property PendienteU6 As Long
    ''' <summary>Σ por operación de max(0, demanda + reserva − stock − pendiente de recibir).</summary>
    Public Property AComprarU6 As Long
    ''' <summary>Costo de lo que hay que comprar con el precio del producto activo de cada operación; Nothing si alguna no tiene precio.</summary>
    Public Property CostoEstimadoU6 As Long?
    Public Property Operaciones As Integer
End Class

''' <summary>Detalle de una operación para un producto.</summary>
Public NotInheritable Class ConsolidadoOperacionDto
    Public Property Operacion As String
    Public Property ProductoBaseId As Long
    Public Property Producto As String
    Public Property Unidad As String
    Public Property DemandaU6 As Long
    Public Property StockU6 As Long
    Public Property ReservaU6 As Long
    Public Property PendienteU6 As Long
    Public Property AComprarU6 As Long
    Public Property CostoUnitarioU6 As Long?
    Public Property CostoEstimadoU6 As Long?
End Class

Public NotInheritable Class ConsolidadoComprasDto
    Public Property Desde As Date
    Public Property Hasta As Date
    Public Property IncluyeBorradores As Boolean
    Public ReadOnly Property Operaciones As New List(Of String)
    Public ReadOnly Property Lineas As New List(Of ConsolidadoLineaDto)
    Public ReadOnly Property Detalle As New List(Of ConsolidadoOperacionDto)
    Public ReadOnly Property CostoTotalU6 As Long
        Get
            Return Lineas.Sum(Function(l) If(l.CostoEstimadoU6, 0L))
        End Get
    End Property
    Public ReadOnly Property ProductosSinPrecio As Integer
        Get
            Return Lineas.Where(Function(l) l.AComprarU6 > 0 AndAlso Not l.CostoEstimadoU6.HasValue).Count()
        End Get
    End Property
End Class

''' <summary>
''' Abastecimiento: consolidado de compras de un periodo (por ejemplo, un mes) entre TODAS las operaciones a las que la
''' persona tiene COMPRAS_CONSOLIDAR, según el alcance de su rol (V021; el dueño, todas). La demanda sale de las minutas que hizo
''' Planificación (menú, factores y pax). Por operación se descuenta su stock y lo pendiente de recibir, y se suma su
''' reserva. El costo usa el precio del producto activo de cada operación (D02) y no incluye IGV (D03).
''' </summary>
Public NotInheritable Class ServicioConsolidadoCompras
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    ''' <param name="incluirBorradores">También las minutas en borrador (menú aún no aprobado), para planificar con anticipación.</param>
    Public Function Calcular(desde As Date, hasta As Date, Optional incluirBorradores As Boolean = False, Optional moneda As String = "PEN") As ConsolidadoComprasDto
        If hasta < desde Then Throw New ReglaNegocioException("DATO_INVALIDO", "El periodo termina antes de empezar.")
        Return EnTransaccion(Permisos.ComprasConsolidar,
            Function(u)
                Dim r As New ConsolidadoComprasDto With {.Desde = desde.Date, .Hasta = hasta.Date, .IncluyeBorradores = incluirBorradores}
                Dim operaciones = u.Consultar(
                    "SELECT o.id, o.codigo || ' - ' || o.nombre FROM operacion o WHERE o.activo = 1 " &
                    "AND @p IN (SELECT fn_permisos_usuario(@u, o.id)) ORDER BY o.codigo",
                    Function(rd) (Id:=rd.GetInt64(0), Nombre:=rd.GetString(1)), "u", Sesion.UsuarioId, "p", Permisos.ComprasConsolidar)
                Dim estados = If(incluirBorradores, New String() {"borrador", "aprobada", "cerrada"}, New String() {"aprobada", "cerrada"})
                Dim productos = u.Consultar(
                    "SELECT p.id, p.codigo, p.descripcion, um.codigo, COALESCE(c.nombre, '') FROM producto_base p " &
                    "JOIN unidad_medida um ON um.id = p.unidad_base_id LEFT JOIN categoria_producto c ON c.id = p.categoria_id",
                    Function(rd) (Id:=rd.GetInt64(0), Codigo:=rd.GetString(1), Descripcion:=rd.GetString(2), Unidad:=rd.GetString(3), Familia:=rd.GetString(4))) _
                    .ToDictionary(Function(x) x.Id)

                For Each op In operaciones
                    r.Operaciones.Add(op.Nombre)
                    Dim demanda = u.Consultar(
                        "SELECT i.producto_base_id, i.cantidad_base_bruta_u6, rv.rendimiento_raciones_u6, d.raciones FROM minuta m " &
                        "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id JOIN minuta_detalle d ON d.minuta_id = m.id " &
                        "JOIN receta_version rv ON rv.id = d.receta_version_id JOIN receta_ingrediente i ON i.receta_version_id = rv.id " &
                        "JOIN producto_base pb ON pb.id = i.producto_base_id " &
                        "WHERE os.operacion_id = @o AND m.estado = ANY(@e) AND m.fecha BETWEEN @d AND @h AND NOT pb.sin_costo_compra " &
                        "UNION ALL SELECT f.producto_base_id, f.cantidad_base_u6, NULL, NULL FROM minuta m " &
                        "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id JOIN minuta_estructura_fija f ON f.minuta_id = m.id " &
                        "WHERE os.operacion_id = @o AND m.estado = ANY(@e) AND m.fecha BETWEEN @d AND @h",
                        Function(rd) (Producto:=rd.GetInt64(0), Cantidad:=If(rd.IsDBNull(2), rd.GetInt64(1),
                                      Recetas.NecesidadIngredienteU6(rd.GetInt64(1), rd.GetInt64(2), rd.GetInt64(3) * EscalaU6.Factor))),
                        "o", op.Id, "e", estados, "d", desde.Date, "h", hasta.Date) _
                        .GroupBy(Function(x) x.Producto).ToDictionary(Function(g) g.Key, Function(g) g.Sum(Function(x) x.Cantidad))
                    Dim stock = PorProducto(u, "SELECT v.producto_base_id, sum(s.cantidad_base_u6)::bigint FROM saldo_stock s JOIN almacen a ON a.id = s.almacen_id " &
                                               "JOIN variante_producto v ON v.id = s.variante_id WHERE a.operacion_id = @o GROUP BY 1", op.Id)
                    Dim reserva = PorProducto(u, "SELECT pa.producto_base_id, sum(pa.reserva_base_u6)::bigint FROM politica_abastecimiento pa " &
                                                 "JOIN almacen a ON a.id = pa.almacen_id WHERE a.operacion_id = @o GROUP BY 1", op.Id)
                    Dim pendiente = PorProducto(u,
                        "SELECT v.producto_base_id, sum(GREATEST(d.cantidad_base_u6 - COALESCE((SELECT sum(rd.cantidad_base_u6) FROM recepcion_detalle rd " &
                        "  JOIN recepcion r ON r.id = rd.recepcion_id WHERE rd.pedido_detalle_id = d.id AND r.estado = 'confirmada'), 0), 0))::bigint " &
                        "FROM pedido_detalle d JOIN pedido_compra p ON p.id = d.pedido_id JOIN almacen a ON a.id = p.almacen_id " &
                        "JOIN empaque_compra e ON e.id = d.empaque_id JOIN variante_producto v ON v.id = e.variante_id " &
                        "WHERE a.operacion_id = @o AND p.estado IN ('aprobado','enviado','parcial') GROUP BY 1", op.Id)

                    For Each p In demanda.Keys.Union(reserva.Where(Function(x) x.Value > 0).Select(Function(x) x.Key))
                        Dim dem As Long = 0, st As Long = 0, res As Long = 0, pen As Long = 0
                        demanda.TryGetValue(p, dem) : stock.TryGetValue(p, st) : reserva.TryGetValue(p, res) : pendiente.TryGetValue(p, pen)
                        Dim comprar = Math.Max(0L, dem + res - st - pen)
                        Dim unitario = If(comprar > 0, CType(CosteoBD.CostoProducto(u, p, hasta.Date, moneda, op.Id), Long?), Nothing)
                        Dim info = productos(p)
                        r.Detalle.Add(New ConsolidadoOperacionDto With {
                            .Operacion = op.Nombre, .ProductoBaseId = p, .Producto = info.Descripcion, .Unidad = info.Unidad, .DemandaU6 = dem, .StockU6 = st,
                            .ReservaU6 = res, .PendienteU6 = pen, .AComprarU6 = comprar, .CostoUnitarioU6 = unitario,
                            .CostoEstimadoU6 = If(comprar = 0, 0L, If(unitario.HasValue, EscalaU6.Multiplicar(comprar, unitario.Value), CType(Nothing, Long?)))})
                    Next
                Next

                For Each g In r.Detalle.GroupBy(Function(d) d.ProductoBaseId)
                    Dim info = productos(g.Key)
                    r.Lineas.Add(New ConsolidadoLineaDto With {
                        .ProductoBaseId = g.Key, .ProductoCodigo = info.Codigo, .Producto = info.Descripcion, .Unidad = info.Unidad, .Familia = info.Familia,
                        .DemandaU6 = g.Sum(Function(d) d.DemandaU6), .StockU6 = g.Sum(Function(d) d.StockU6), .ReservaU6 = g.Sum(Function(d) d.ReservaU6),
                        .PendienteU6 = g.Sum(Function(d) d.PendienteU6), .AComprarU6 = g.Sum(Function(d) d.AComprarU6), .Operaciones = g.Count(),
                        .CostoEstimadoU6 = If(g.All(Function(d) d.CostoEstimadoU6.HasValue), g.Sum(Function(d) d.CostoEstimadoU6.Value), CType(Nothing, Long?))})
                Next
                r.Lineas.Sort(Function(a, b) If(a.Familia <> b.Familia, String.CompareOrdinal(a.Familia, b.Familia), String.CompareOrdinal(a.Producto, b.Producto)))
                Return r
            End Function)
    End Function

    Private Shared Function PorProducto(u As UnidadDeTrabajo, sql As String, operacionId As Long) As Dictionary(Of Long, Long)
        Return u.Consultar(sql, Function(rd) (rd.GetInt64(0), rd.GetInt64(1)), "o", operacionId).ToDictionary(Function(x) x.Item1, Function(x) x.Item2)
    End Function

    ''' <summary>El consolidado como reporte para imprimir o exportar a Excel.</summary>
    Public Function Reporte(desde As Date, hasta As Date, Optional incluirBorradores As Boolean = False) As Reporte
        Dim c = Calcular(desde, hasta, incluirBorradores)
        Dim r As New Reporte($"Consolidado de compras {desde:yyyy-MM-dd} a {hasta:yyyy-MM-dd}") With {.GeneradoPor = Sesion.NombreUsuario}
        r.Dato("Empresa", Sesion.EmpresaCodigo)
        r.Dato("Periodo", $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
        r.Dato("Operaciones", String.Join(", ", c.Operaciones))
        r.Dato("Minutas", If(incluirBorradores, "aprobadas, cerradas y en borrador", "aprobadas y cerradas"))
        r.Dato("Costo estimado (S/, sin IGV)", Reporte.Valor(c.CostoTotalU6, FormatoColumna.Dinero, Globalization.CultureInfo.InvariantCulture, False))
        Dim s = r.Seccion("Consolidado por producto", New ColumnaReporte("Familia"), New ColumnaReporte("Codigo"), New ColumnaReporte("Producto"),
                          New ColumnaReporte("Unidad"), New ColumnaReporte("Demanda", FormatoColumna.Cantidad), New ColumnaReporte("Stock", FormatoColumna.Cantidad),
                          New ColumnaReporte("Reserva", FormatoColumna.Cantidad), New ColumnaReporte("Pendiente de recibir", FormatoColumna.Cantidad),
                          New ColumnaReporte("A comprar", FormatoColumna.Cantidad), New ColumnaReporte("Costo estimado", FormatoColumna.Dinero),
                          New ColumnaReporte("Operaciones", FormatoColumna.Entero))
        For Each l In c.Lineas
            s.Agregar(l.Familia, l.ProductoCodigo, l.Producto, l.Unidad, l.DemandaU6, l.StockU6, l.ReservaU6, l.PendienteU6, l.AComprarU6, l.CostoEstimadoU6, l.Operaciones)
        Next
        s.Totales = {"TOTAL", Nothing, $"{c.Lineas.Count} productos", Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, c.CostoTotalU6, Nothing}
        Dim d = r.Seccion("Detalle por operacion", New ColumnaReporte("Operacion"), New ColumnaReporte("Producto"), New ColumnaReporte("Unidad"),
                          New ColumnaReporte("Demanda", FormatoColumna.Cantidad), New ColumnaReporte("Stock", FormatoColumna.Cantidad),
                          New ColumnaReporte("Pendiente de recibir", FormatoColumna.Cantidad), New ColumnaReporte("A comprar", FormatoColumna.Cantidad),
                          New ColumnaReporte("Costo unitario", FormatoColumna.Dinero), New ColumnaReporte("Costo estimado", FormatoColumna.Dinero))
        For Each x In c.Detalle.OrderBy(Function(y) y.Operacion).ThenBy(Function(y) y.Producto)
            d.Agregar(x.Operacion, x.Producto, x.Unidad, x.DemandaU6, x.StockU6, x.PendienteU6, x.AComprarU6, x.CostoUnitarioU6, x.CostoEstimadoU6)
        Next
        If c.ProductosSinPrecio > 0 Then r.Notas.Add($"{c.ProductosSinPrecio} producto(s) a comprar sin precio del producto activo: su costo queda vacio (no se estima).")
        r.Notas.Add("A comprar = demanda de las minutas + reserva - stock - pendiente de recibir, por operacion; el consolidado suma las operaciones.")
        Return r
    End Function

End Class
