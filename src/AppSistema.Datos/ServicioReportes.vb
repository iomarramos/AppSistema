Imports AppSistema.Dominio.Inventario
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad
Imports AppSistema.Dominio.Stock

''' <summary>
''' Reportes imprimibles o exportables: minuta del día, requerimiento a almacén, kárdex valorizado, hoja y resultado de
''' inventario físico, y stock valorizado. Cada uno exige el mismo permiso que la pantalla de donde sale y solo lee datos
''' de la operación de la sesión. Los importes sin precio quedan vacíos (nunca cero).
''' </summary>
Public NotInheritable Class ServicioReportes
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    Private Function Nuevo(titulo As String) As Reporte
        Dim r As New Reporte(titulo) With {.GeneradoPor = Sesion.NombreUsuario}
        r.Dato("Empresa", Sesion.EmpresaCodigo)
        r.Dato("Operacion", Sesion.Operacion?.ToString())
        Return r
    End Function

    Private Shared Function Soles(valorU6 As Long) As String
        Return Reporte.Valor(valorU6, FormatoColumna.Dinero, Globalization.CultureInfo.InvariantCulture, False)
    End Function

    Private Shared Function C(nombre As String, Optional formato As FormatoColumna = FormatoColumna.Texto) As ColumnaReporte
        Return New ColumnaReporte(nombre, formato)
    End Function

    ''' <summary>Cantidad × costo a valor. Sin cantidad (sin contar) = vacío, no cero.</summary>
    Private Shared Function ValorDe(cantidadU6 As Long?, costoU6 As Long?) As Object
        If Not cantidadU6.HasValue OrElse Not costoU6.HasValue Then Return Nothing
        Return EscalaU6.Multiplicar(cantidadU6.Value, costoU6.Value)
    End Function

    ' ---------- Minuta del día ----------

    ''' <summary>Minuta con sus platos por componente, fijos y la necesidad consolidada de insumos para cocina.</summary>
    Public Function MinutaDelDia(minutaId As Long) As Reporte
        Dim op = Sesion.OperacionId
        Dim m = EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT s.nombre, r.nombre, m.fecha, m.comensales, m.estado, m.costo_previsto_u6, m.venta_prevista_u6, m.food_cost_objetivo_bp FROM minuta m " &
                "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id JOIN servicio s ON s.id = os.servicio_id JOIN regimen r ON r.id = os.regimen_id " &
                "WHERE m.id = @m AND os.operacion_id = @o",
                Function(rd) New MinutaDto With {.ServicioNombre = rd.GetString(0), .RegimenNombre = rd.GetString(1), .Fecha = rd.GetDateTime(2),
                                                 .Comensales = rd.GetInt64(3), .Estado = rd.GetString(4), .CostoPrevistoU6 = rd.LongONada("costo_previsto_u6"),
                                                 .VentaPrevistaU6 = rd.LongONada("venta_prevista_u6"), .FoodCostObjetivoBp = rd.LongONada("food_cost_objetivo_bp")},
                "m", minutaId, "o", op).SingleOrDefault())
        If m Is Nothing Then Throw New ReglaNegocioException("OPERACION_AJENA", "La minuta no pertenece a la operacion seleccionada.")
        Dim minutas As New ServicioMinutas(CadenaConexion, Sesion)

        Dim r = Nuevo($"Minuta {m.ServicioNombre} {m.Fecha:yyyy-MM-dd}")
        r.Horizontal = True
        r.Dato("Servicio", $"{m.ServicioNombre} ({m.RegimenNombre})")
        r.Dato("Fecha", m.Fecha.ToString("dd/MM/yyyy"))
        r.Dato("Comensales", m.Comensales.ToString())
        r.Dato("Estado", m.Estado)

        Dim platos = r.Seccion("Platos por componente", C("Componente"), C("Receta"), C("Version", FormatoColumna.Entero), C("Raciones", FormatoColumna.Entero),
                               C("Costo racion", FormatoColumna.Dinero), C("Costo total", FormatoColumna.Dinero), C("Observacion"))
        Dim sinCosto = False
        For Each p In minutas.ListarPlatos(minutaId)
            Dim total = If(p.CostoPrevistoRacionU6.HasValue, EscalaU6.Multiplicar(p.CostoPrevistoRacionU6.Value, p.Raciones * EscalaU6.Factor), CType(Nothing, Long?))
            Dim obs = If(p.IngredientesSinCosto > 0, $"{p.IngredientesSinCosto} ingrediente(s) sin precio", Nothing)
            If p.IngredientesSinCosto > 0 OrElse (m.Estado <> "borrador" AndAlso Not p.CostoPrevistoRacionU6.HasValue) Then sinCosto = True
            platos.Agregar(p.EstructuraNombre, $"{p.RecetaCodigo} {p.RecetaNombre}", p.Version, p.Raciones, p.CostoPrevistoRacionU6, total, obs)
        Next

        Dim fijos = minutas.ListarFijos(minutaId)
        If fijos.Count > 0 Then
            Dim s = r.Seccion("Productos fijos del servicio", C("Producto"), C("Unidad"), C("Cantidad", FormatoColumna.Cantidad),
                              C("Costo unitario", FormatoColumna.Dinero), C("Costo total", FormatoColumna.Dinero))
            For Each f In fijos
                s.Agregar(f.ProductoDescripcion, f.Unidad, f.CantidadBaseU6, f.CostoPrevistoUnitarioU6,
                          If(f.CostoPrevistoUnitarioU6.HasValue, EscalaU6.Multiplicar(f.CantidadBaseU6, f.CostoPrevistoUnitarioU6.Value), CType(Nothing, Long?)))
            Next
        End If

        Dim necesidades = r.Seccion("Necesidad de insumos para cocina", C("Codigo"), C("Producto"), C("Unidad"), C("Cantidad", FormatoColumna.Cantidad))
        For Each n In minutas.Necesidades({minutaId})
            necesidades.Agregar(n.ProductoCodigo, n.ProductoDescripcion, n.Unidad, n.CantidadU6)
        Next

        If m.CostoPrevistoU6.HasValue Then
            r.Dato("Costo previsto (S/)", Soles(m.CostoPrevistoU6.Value))
            r.Dato("Costo por comensal (S/)", If(m.Comensales > 0, Soles(EscalaU6.MultiplicarDividir(m.CostoPrevistoU6.Value, 1, m.Comensales)), ""))
        End If
        If m.VentaPrevistaU6.HasValue Then
            r.Dato("Venta prevista (S/)", Soles(m.VentaPrevistaU6.Value))
            r.Dato("Food Cost objetivo", $"{If(m.FoodCostObjetivoBp, VentaEstructura.ObjetivoPorDefectoBp) / 100D:0.##} %")
        End If
        If m.Estado = "borrador" Then r.Notas.Add("Minuta en borrador: el costo se calcula al aprobarla.")
        If sinCosto Then r.Notas.Add("Hay platos con ingredientes sin precio: su costo no se considera (no se estima).")
        r.Firmas.AddRange({"Elaborado por (nutricion)", "Aprobado por", "Recibido por (cocina)"})
        Return r
    End Function

    ' ---------- Requerimiento ----------

    ''' <summary>Requerimiento a almacén con lo previsto y lo solicitado, para firmar la entrega.</summary>
    Public Function Requerimiento(requerimientoId As Long) As Reporte
        Dim op = Sesion.OperacionId
        Dim cab = EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT q.numero, q.tipo, q.fecha, q.estado, a.codigo || ' - ' || a.nombre, s.nombre, m.fecha FROM requerimiento q " &
                "JOIN almacen a ON a.id = q.almacen_id JOIN operacion_servicio os ON os.id = q.operacion_servicio_id JOIN servicio s ON s.id = os.servicio_id " &
                "LEFT JOIN minuta m ON m.id = q.minuta_id WHERE q.id = @q AND a.operacion_id = @o",
                Function(rd) (Numero:=rd.GetString(0), Tipo:=rd.GetString(1), Fecha:=rd.GetDateTime(2), Estado:=rd.GetString(3), Almacen:=rd.GetString(4),
                              Servicio:=rd.GetString(5), FechaMinuta:=If(rd.IsDBNull(6), CType(Nothing, Date?), rd.GetDateTime(6))),
                "q", requerimientoId, "o", op).ToList())
        If cab.Count = 0 Then Throw New ReglaNegocioException("OPERACION_AJENA", "El requerimiento no pertenece a la operacion seleccionada.")
        Dim q = cab(0)
        Dim r = Nuevo($"Requerimiento {q.Numero}")
        r.Dato("Numero", q.Numero)
        r.Dato("Tipo", q.Tipo)
        r.Dato("Fecha", q.Fecha.ToString("dd/MM/yyyy"))
        r.Dato("Estado", q.Estado)
        r.Dato("Almacen", q.Almacen)
        r.Dato("Servicio", If(q.FechaMinuta.HasValue, $"{q.Servicio} del {q.FechaMinuta.Value:dd/MM/yyyy}", q.Servicio))
        ' Bulto en decimales, como la requisición del SGP (usuario, 2026-10-03): solicitado ÷ contenido de la presentación
        ' activa en la operación (D02). Sin presentación activa queda vacío.
        Dim activas = EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT po.producto_base_id, v.contenido_base_por_envase_u6, v.tipo_envase, v.descripcion_comercial FROM producto_operacion po " &
                "JOIN variante_producto v ON v.id = po.variante_id WHERE po.operacion_id = @o",
                Function(rd) (Producto:=rd.GetInt64(0), ContenidoU6:=rd.GetInt64(1), Envase:=rd.GetString(2), Presentacion:=rd.GetString(3)), "o", op) _
                .ToDictionary(Function(x) x.Producto))
        Dim s = r.Seccion("", C("Producto"), C("Unidad"), C("Previsto", FormatoColumna.Cantidad), C("Solicitado", FormatoColumna.Cantidad),
                          C("Presentacion activa"), C("Bulto", FormatoColumna.Cantidad), C("Envase"), C("Entregado"))
        For Each l In New ServicioProduccion(CadenaConexion, Sesion).ListarLineas(requerimientoId)
            Dim a As (Producto As Long, ContenidoU6 As Long, Envase As String, Presentacion As String) = Nothing
            If activas.TryGetValue(l.ProductoBaseId, a) Then
                s.Agregar(l.ProductoDescripcion, l.Unidad, l.PrevistoU6, l.SolicitadoU6, a.Presentacion,
                          EscalaU6.MultiplicarDividir(l.SolicitadoU6, EscalaU6.Factor, a.ContenidoU6), a.Envase, Nothing)
            Else
                s.Agregar(l.ProductoDescripcion, l.Unidad, l.PrevistoU6, l.SolicitadoU6, Nothing, Nothing, Nothing, Nothing)
            End If
        Next
        r.Notas.Add("Bulto = solicitado / contenido de la presentacion activa, en decimales (como la requisicion de la planificacion).")
        r.Notas.Add("Se entrega la presentacion completa (D12); lo entregado a cocina se da por consumido.")
        r.Firmas.AddRange({"Solicitado por (cocina)", "Entregado por (almacen)", "Recibido por"})
        Return r
    End Function

    ' ---------- Kárdex ----------

    ''' <summary>Kárdex valorizado (promedio móvil) de una presentación en un almacén.</summary>
    Public Function Kardex(almacenId As Long, varianteId As Long, desde As Date, hasta As Date) As Reporte
        Dim nombres = NombresAlmacenVariante(almacenId, varianteId, Permisos.CatalogoVer)
        Dim movimientos = New ServicioAlmacen(CadenaConexion, Sesion).Kardex(almacenId, varianteId, desde, hasta)
        Dim r = Nuevo($"Kardex {nombres.Variante}")
        r.Dato("Almacen", nombres.Almacen)
        r.Dato("Producto", nombres.Variante)
        r.Dato("Unidad", nombres.Unidad)
        r.Dato("Periodo", $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
        Dim s = r.Seccion("", C("Fecha", FormatoColumna.Fecha), C("Documento"), C("Tipo"), C("Entrada", FormatoColumna.Cantidad), C("Salida", FormatoColumna.Cantidad),
                          C("Valor movimiento", FormatoColumna.Dinero), C("Saldo", FormatoColumna.Cantidad), C("Saldo valor", FormatoColumna.Dinero),
                          C("Costo promedio", FormatoColumna.Dinero))
        For Each k In movimientos
            s.Agregar(k.Fecha, k.Documento, k.Tipo, k.EntradaU6, k.SalidaU6, If(k.Documento = "SALDO INICIAL", CType(Nothing, Long?), k.ValorMovimientoU6),
                      k.SaldoCantidadU6, k.SaldoValorU6, k.CostoPromedioU6)
        Next
        Dim ultimo = movimientos.Last()
        s.Totales = {"TOTAL", Nothing, Nothing, movimientos.Sum(Function(k) If(k.EntradaU6, 0L)), movimientos.Sum(Function(k) If(k.SalidaU6, 0L)),
                     Nothing, ultimo.SaldoCantidadU6, ultimo.SaldoValorU6, ultimo.CostoPromedioU6}
        r.Notas.Add("Entradas a su costo; salidas a cantidad x costo promedio vigente (D01).")
        Return r
    End Function

    ' ---------- Inventario físico ----------

    ''' <summary>
    ''' Inventario físico. Con <paramref name="hojaDeConteo"/> sale la hoja para contar a mano (sin el stock del sistema,
    ''' con columnas en blanco); sin ella, el resultado con sistema, físico, diferencia y su valor.
    ''' </summary>
    Public Function Inventario(inventarioId As Long, hojaDeConteo As Boolean) As Reporte
        Dim op = Sesion.OperacionId
        Dim cab = EnTransaccion(Permisos.InventarioVer,
            Function(u) u.Consultar(
                "SELECT i.numero, i.tipo, i.fecha_corte, i.estado, a.codigo || ' - ' || a.nombre FROM inventario i JOIN almacen a ON a.id = i.almacen_id " &
                "WHERE i.id = @i AND a.operacion_id = @o",
                Function(rd) (Numero:=rd.GetString(0), Tipo:=rd.GetString(1), Corte:=rd.GetDateTime(2), Estado:=rd.GetString(3), Almacen:=rd.GetString(4)),
                "i", inventarioId, "o", op).ToList())
        If cab.Count = 0 Then Throw New ReglaNegocioException("OPERACION_AJENA", "El inventario no pertenece a la operacion seleccionada.")
        Dim i = cab(0)
        Dim servicio As New ServicioInventarios(CadenaConexion, Sesion)
        Dim r = Nuevo(If(hojaDeConteo, $"Listado para toma de inventario {i.Numero}", $"Inventario {i.Numero}"))
        r.Dato("Numero", i.Numero)
        r.Dato("Almacen", i.Almacen)
        r.Dato("Tipo", i.Tipo)
        r.Dato("Fecha de corte", i.Corte.ToString("dd/MM/yyyy"))
        r.Dato("Estado", i.Estado)
        If hojaDeConteo Then
            Dim s = r.Seccion("", C("Codigo"), C("Descripcion"), C("Presentacion"), C("Contenido", FormatoColumna.Cantidad), C("Unidad"), C("Envases"), C("Parcial"), C("Observacion"))
            For Each l In servicio.Hoja(inventarioId, ciego:=True)
                s.Agregar(l.VarianteCodigo, l.Descripcion, l.Presentacion, l.ContenidoEnvaseU6, l.Unidad, Nothing, Nothing, Nothing)
            Next
            r.Notas.Add("Contar envases cerrados y, aparte, el parcial en la unidad indicada. Celda vacia = sin contar (no es cero).")
            r.Firmas.AddRange({"Contado por", "Verificado por"})
        Else
            ' Formato "Diferencias fisico vs sistema - valorizado" del SGP: P.M.P., stock y total fisico, stock y total del sistema, diferencia y total de la diferencia.
            Dim s = r.Seccion("Diferencias fisico vs sistema", C("Codigo"), C("Descripcion"), C("Unidad"), C("P.M.P.", FormatoColumna.Dinero),
                              C("Stock fisico", FormatoColumna.Cantidad), C("Total fisico", FormatoColumna.Dinero), C("Stock sist.", FormatoColumna.Cantidad),
                              C("Total sist.", FormatoColumna.Dinero), C("Diferencia", FormatoColumna.Cantidad), C("Total dif.", FormatoColumna.Dinero))
            For Each l In servicio.Hoja(inventarioId)
                s.Agregar(l.VarianteCodigo, l.Descripcion, l.Unidad, l.CostoU6, l.FisicoU6, ValorDe(l.FisicoU6, l.CostoU6),
                          l.SistemaU6, ValorDe(l.SistemaU6, l.CostoU6), l.DiferenciaU6, l.ValorDiferenciaU6)
            Next
            Dim res = servicio.Resumen(inventarioId)
            s.Totales = {"TOTAL", $"{res.Lineas} lineas", Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, res.SobranteValorU6 - res.FaltanteValorU6}
            r.Dato("Lineas sin contar", res.SinContar.ToString())
            r.Dato("Lineas con diferencia", res.ConDiferencia.ToString())
            r.Dato("Faltante (S/)", Soles(res.FaltanteValorU6))
            r.Dato("Sobrante (S/)", Soles(res.SobranteValorU6))
            r.Firmas.AddRange({"Contado por", "Revisado por", "Autorizado por"})
        End If
        Return r
    End Function

    ' ---------- Stock valorizado ----------

    ''' <summary>
    ''' Inventario físico valorizado con el formato del SGP (Informes > Stock): cabecera de bodega y toma, y una tabla por
    ''' familia con código, descripción, unidad, cantidad, precio (costo promedio) y total, con el subtotal de cada familia.
    ''' </summary>
    Public Function StockValorizado(almacenId As Long) As Reporte
        Dim nombres = NombresAlmacenVariante(almacenId, Nothing, Permisos.CatalogoVer)
        Dim saldos = New ServicioStock(CadenaConexion, Sesion).ConsultarSaldos(almacenId, "")
        Dim r = Nuevo($"Inventario fisico valorizado {nombres.Almacen}")
        r.Dato("Bodega", nombres.Almacen)
        r.Dato("Toma de inventario", Date.Today.ToString("dd/MM/yyyy"))
        r.Dato("Familia de producto", "Todas")
        For Each familia In saldos.GroupBy(Function(x) x.Familia).OrderBy(Function(g) g.Key, StringComparer.CurrentCultureIgnoreCase)
            Dim s = r.Seccion(familia.Key, C("Codigo"), C("Descripcion"), C("Unidad"), C("Cantidad", FormatoColumna.Cantidad),
                              C("Precio", FormatoColumna.Dinero), C("Total", FormatoColumna.Dinero))
            For Each x In familia
                s.Agregar(x.VarianteCodigo, x.VarianteDescripcion, x.Unidad, x.CantidadBaseU6, x.CostoPromedioU6, x.ValorU6)
            Next
            s.Totales = {"Total " & familia.Key, Nothing, Nothing, Nothing, Nothing, familia.Sum(Function(x) x.ValorU6)}
        Next
        r.Dato("Total general (S/)", Soles(saldos.Sum(Function(x) x.ValorU6)))
        Return r
    End Function

    ''' <summary>
    ''' Movimiento de stock sintético (formato del SGP, Informes > Stock): por familia, el saldo anterior al periodo, las
    ''' entradas (recepciones y traspasos recibidos), la implantación (apertura), las retiradas (salidas a producción y bajas),
    ''' los ajustes, las salidas por traspaso, las devoluciones de producción y el saldo actual, todo en valor.
    ''' AppSistema valoriza cada salida al promedio móvil del momento (D01): no hay una reexpresión de costo aparte, por eso
    ''' el reporte no tiene la columna "Dif. evol. costo" del SGP.
    ''' </summary>
    Public Function MovimientoStockSintetico(almacenId As Long, desde As Date, hasta As Date) As Reporte
        If hasta < desde Then Throw New ReglaNegocioException("DATO_INVALIDO", "El periodo termina antes de empezar.")
        Dim nombres = NombresAlmacenVariante(almacenId, Nothing, Permisos.CatalogoVer)
        Dim op = Sesion.OperacionId
        Dim filas = EnTransaccion(Permisos.CatalogoVer,
            Function(u)
                Return u.Consultar(
                    "SELECT COALESCE(cp.nombre, 'SIN FAMILIA'), CASE WHEN m.fecha < @d THEN 'ANTERIOR' ELSE d.tipo END, sum(m.signo * m.valor_u6)::bigint " &
                    "FROM movimiento_stock m JOIN documento_stock_detalle l ON l.id = m.documento_detalle_id JOIN documento_stock d ON d.id = l.documento_id " &
                    "JOIN variante_producto v ON v.id = m.variante_id LEFT JOIN categoria_producto cp ON cp.id = v.categoria_id " &
                    "JOIN almacen a ON a.id = m.almacen_id " &
                    "WHERE m.almacen_id = @a AND a.operacion_id = @o AND m.fecha <= @h GROUP BY 1, 2",
                    Function(rd) (Familia:=rd.GetString(0), Tipo:=rd.GetString(1), Valor:=rd.GetInt64(2)),
                    "a", almacenId, "o", op, "d", desde.Date, "h", hasta.Date).ToList()
            End Function)

        Dim r = Nuevo($"Movimiento de stock sintetico {nombres.Almacen}")
        r.Dato("Bodega", nombres.Almacen)
        r.Dato("Periodo", $"{desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}")
        Dim s = r.Seccion("", C("Familia"), C("Saldo anterior", FormatoColumna.Dinero), C("Entradas", FormatoColumna.Dinero),
                          C("Implantacion", FormatoColumna.Dinero), C("Retiradas", FormatoColumna.Dinero), C("Ajuste", FormatoColumna.Dinero),
                          C("Salida", FormatoColumna.Dinero), C("Devolucion", FormatoColumna.Dinero), C("Saldo actual", FormatoColumna.Dinero))
        Dim totales(8) As Object
        For Each familia In filas.GroupBy(Function(x) x.Familia).OrderBy(Function(g) g.Key, StringComparer.CurrentCultureIgnoreCase)
            Dim Suma = Function(tipos As String()) familia.Where(Function(x) tipos.Contains(x.Tipo)).Sum(Function(x) x.Valor)
            Dim anterior = Suma({"ANTERIOR"})
            Dim entradas = Suma({"recepcion", "traspaso_entrada"})
            Dim implantacion = Suma({"apertura"})
            Dim retiradas = -Suma({"salida_produccion", "baja"})
            Dim ajuste = Suma({"ajuste_positivo", "ajuste_negativo", "reversion"})
            Dim salida = -Suma({"traspaso_salida"})
            Dim devolucion = Suma({"devolucion_produccion"})
            ' Retiradas y salidas se muestran en positivo: se restan para el saldo.
            Dim actual = anterior + entradas + implantacion - retiradas + ajuste - salida + devolucion
            s.Agregar(familia.Key, anterior, entradas, implantacion, retiradas, ajuste, salida, devolucion, actual)
            For i = 1 To 8
                totales(i) = CLng(If(totales(i), 0L)) + CLng(s.Filas.Last()(i))
            Next
        Next
        totales(0) = "TOTAL"
        s.Totales = totales
        r.Notas.Add("Retiradas = salidas a produccion y bajas. Ajuste = ajustes y reversiones. Cada salida se valoriza al promedio del momento.")
        Return r
    End Function

    ''' <summary>
    ''' Resumen consolidado de salidas a producción (o de devoluciones a bodega) del periodo, por producto, con la cantidad y
    ''' el total valorizado, como el "Resumen de salidas para producción consolidado" del SGP.
    ''' </summary>
    Public Function SalidasConsolidadas(almacenId As Long, desde As Date, hasta As Date, devoluciones As Boolean) As Reporte
        If hasta < desde Then Throw New ReglaNegocioException("DATO_INVALIDO", "El periodo termina antes de empezar.")
        Dim nombres = NombresAlmacenVariante(almacenId, Nothing, Permisos.CatalogoVer)
        Dim op = Sesion.OperacionId
        Dim tipo = If(devoluciones, "devolucion_produccion", "salida_produccion")
        ' Las salidas van con signo negativo en el movimiento: se invierte para mostrar cantidades positivas.
        Dim signo = If(devoluciones, 1L, -1L)
        Dim filas = EnTransaccion(Permisos.CatalogoVer,
            Function(u)
                Return u.Consultar(
                    "SELECT v.codigo, v.descripcion_comercial, um.codigo, sum(m.signo * m.cantidad_base_u6)::bigint, sum(m.signo * m.valor_u6)::bigint " &
                    "FROM movimiento_stock m JOIN documento_stock_detalle l ON l.id = m.documento_detalle_id JOIN documento_stock d ON d.id = l.documento_id " &
                    "JOIN variante_producto v ON v.id = m.variante_id JOIN producto_base p ON p.id = v.producto_base_id " &
                    "JOIN unidad_medida um ON um.id = p.unidad_base_id JOIN almacen a ON a.id = m.almacen_id " &
                    "WHERE m.almacen_id = @a AND a.operacion_id = @o AND d.tipo = @t AND m.fecha BETWEEN @d AND @h " &
                    "GROUP BY v.codigo, v.descripcion_comercial, um.codigo ORDER BY v.descripcion_comercial",
                    Function(rd) (Codigo:=rd.GetString(0), Descripcion:=rd.GetString(1), Unidad:=rd.GetString(2), Cant:=rd.GetInt64(3), Valor:=rd.GetInt64(4)),
                    "a", almacenId, "o", op, "t", tipo, "d", desde.Date, "h", hasta.Date).ToList()
            End Function)
        Dim r = Nuevo(If(devoluciones, "Resumen de devolucion de produccion a bodega consolidado", "Resumen de salidas para produccion consolidado"))
        r.Dato("Bodega", nombres.Almacen)
        r.Dato("Periodo", $"{desde:dd/MM/yyyy} - {hasta:dd/MM/yyyy}")
        Dim s = r.Seccion("", C("Codigo"), C("Descripcion"), C("Cantidad", FormatoColumna.Cantidad), C("Unidad"), C("Total", FormatoColumna.Dinero))
        For Each x In filas
            s.Agregar(x.Codigo, x.Descripcion, signo * x.Cant, x.Unidad, signo * x.Valor)
        Next
        s.Totales = {"TOTAL", $"{filas.Count} productos", Nothing, Nothing, signo * filas.Sum(Function(x) x.Valor)}
        Return r
    End Function

    ''' <summary>
    ''' Resultado operacional mensual con el formato A13 del SGP. El consumo realizado sale del inventario como en el SGP:
    ''' inventario inicial + recepciones + traspasos recibidos − traspasos enviados − inventario final. La diferencia contra el
    ''' costo diario de los servicios (a − b) debe dar cero. Las ventas y el costo de alimentos vienen del resultado mensual.
    ''' </summary>
    Public Function ResultadoA13(anio As Integer, mes As Integer) As Reporte
        Dim desde = New Date(anio, mes, 1)
        Dim hasta = desde.AddMonths(1).AddDays(-1)
        Dim op = Sesion.OperacionId
        Dim saldos = EnTransaccion(Permisos.CatalogoVer,
            Function(u)
                Return u.Consultar(
                    "SELECT CASE WHEN m.fecha < @d THEN 'INICIAL' ELSE d.tipo END, sum(m.signo * m.valor_u6)::bigint " &
                    "FROM movimiento_stock m JOIN documento_stock_detalle l ON l.id = m.documento_detalle_id JOIN documento_stock d ON d.id = l.documento_id " &
                    "JOIN almacen a ON a.id = m.almacen_id WHERE a.operacion_id = @o AND m.fecha <= @h GROUP BY 1",
                    Function(rd) (Tipo:=rd.GetString(0), Valor:=rd.GetInt64(1)), "o", op, "d", desde.Date, "h", hasta.Date).ToList()
            End Function)
        Dim Del = Function(tipos As String()) saldos.Where(Function(x) tipos.Contains(x.Tipo)).Sum(Function(x) x.Valor)
        Dim inicial = Del({"INICIAL"})
        Dim recepciones = Del({"recepcion"})
        Dim trasladosRecibidos = Del({"traspaso_entrada"})
        Dim implantacion = Del({"apertura"})
        Dim trasladosEnviados = -Del({"traspaso_salida"})
        Dim final = saldos.Sum(Function(x) x.Valor)
        Dim consumo = inicial + recepciones + implantacion + trasladosRecibidos - trasladosEnviados - final

        Dim resultado = New ServicioResultados(CadenaConexion, Sesion).ResultadoMensual(anio, mes)
        Dim ventas = resultado.Total.IngresoU6
        Dim costoServicios = resultado.Total.CostoAlimentosU6
        ' Criterio del contrato: el consumo diario es el consumo del periodo entre los días base de la operación (20, 21, 31…).
        Dim diasBase = EnTransaccion(Permisos.CatalogoVer, Function(u) u.EscalarLong("SELECT dias_stock_base FROM operacion WHERE id = @o", "o", op))
        Dim consumoDiario = consumo \ diasBase

        Dim r = Nuevo($"Resultados operacionales mensual A13 {desde:MMMM yyyy}")
        r.Dato("Periodo", $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
        Dim ventasSec = r.Seccion("Ventas del periodo", C("Concepto"), C("Total", FormatoColumna.Dinero))
        ventasSec.Agregar("Ventas servicios", ventas)
        Dim consumoSec = r.Seccion("Consumo realizado", C("Concepto"), C("Total", FormatoColumna.Dinero))
        consumoSec.Agregar("Inventario inicial", inicial)
        consumoSec.Agregar("Recepcion proveedor", recepciones)
        consumoSec.Agregar("Implantacion (apertura)", implantacion)
        consumoSec.Agregar("Traspasos recibidos", trasladosRecibidos)
        consumoSec.Agregar("Traspasos enviados (salida)", trasladosEnviados)
        consumoSec.Agregar("Inventario final", final)
        consumoSec.Totales = {"Consumo segun A13 (a)", consumo}
        Dim pie = r.Seccion("Resultado", C("Concepto"), C("Valor"))
        pie.Agregar("Consumo segun A13 (a)", Soles(consumo))
        pie.Agregar("Porcentaje de consumo sobre ventas", If(ventas > 0, Math.Round(consumo * 100D / ventas, 2).ToString("0.00", Globalization.CultureInfo.InvariantCulture), Nothing))
        pie.Agregar("Consumo diario", Soles(consumoDiario))
        pie.Agregar("Dias de stock (inventario final / consumo diario)", If(consumoDiario > 0, Math.Round(CDec(final) / CDec(consumoDiario), 0).ToString("0", Globalization.CultureInfo.InvariantCulture), Nothing))
        pie.Agregar("Costo diario de los servicios (b)", Soles(costoServicios))
        pie.Agregar("Diferencia (a - b)", Soles(consumo - costoServicios))
        pie.Agregar("Total de gastos (alimentos)", Soles(consumo))
        pie.Agregar("Utilidad operacional", Soles(ventas - consumo))
        r.Notas.Add("Solo alimentos. Los gastos de personal, operacion y otros estan en el resultado mensual, no en este formato.")
        r.Notas.Add($"Dias de stock = inventario final / (consumo del periodo / {diasBase} dias base del contrato).")
        r.Firmas.AddRange({"Elaborado por", "Revisado por"})
        Return r
    End Function

    ' ---------- Food cost y comparativo de tres niveles por minuta (formato del SGP) ----------

    ''' <summary>Minutas aprobadas o cerradas de la operación en el mes: fecha, régimen, servicio.</summary>
    Private Function MinutasDelMes(anio As Integer, mes As Integer) As List(Of (Id As Long, Fecha As Date, Regimen As String, Servicio As String))
        Dim desde As New Date(anio, mes, 1)
        Dim hasta = desde.AddMonths(1).AddDays(-1)
        Dim op = Sesion.OperacionId
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                Return u.Consultar(
                    "SELECT m.id, m.fecha, reg.nombre, s.nombre FROM minuta m JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
                    "JOIN servicio s ON s.id = os.servicio_id JOIN regimen reg ON reg.id = os.regimen_id " &
                    "WHERE os.operacion_id = @o AND m.estado IN ('aprobada','cerrada') AND m.fecha BETWEEN @d AND @h ORDER BY m.fecha, reg.nombre, s.nombre",
                    Function(rd) (Id:=rd.GetInt64(0), Fecha:=rd.GetDateTime(1).Date, Regimen:=rd.GetString(2), Servicio:=rd.GetString(3)),
                    "o", op, "d", desde.Date, "h", hasta.Date).ToList()
            End Function)
    End Function

    ''' <summary>Costo u6 de una bandeja: total ÷ raciones (raciones sin cifra o en cero = vacío).</summary>
    Private Shared Function PorBandeja(totalU6 As Long?, racionesCount As Long?) As Object
        If Not totalU6.HasValue OrElse Not racionesCount.HasValue OrElse racionesCount.Value <= 0 Then Return Nothing
        Return totalU6.Value \ racionesCount.Value
    End Function

    Private Shared Function Porcentaje(parte As Long?, total As Long?) As Object
        If Not parte.HasValue OrElse Not total.HasValue OrElse total.Value <= 0 Then Return Nothing
        Return (CDbl(parte.Value) * 100D / total.Value).ToString("0.00", Globalization.CultureInfo.InvariantCulture) & " %"
    End Function

    ''' <summary>
    ''' Food cost por minuta (formato "Food Cost (Alimento)" del SGP): por día y servicio, raciones preparadas y vendidas,
    ''' venta del día, valor de la bandeja (venta ÷ raciones vendidas), costo del día, costo de la bandeja (costo ÷ raciones
    ''' preparadas) y food cost (costo ÷ venta). Hoja horizontal.
    ''' </summary>
    Public Function FoodCost(anio As Integer, mes As Integer) As Reporte
        Dim comparativo As New ServicioComparativo(CadenaConexion, Sesion)
        Dim r = Nuevo($"Food cost alimento {mes:00}/{anio}")
        r.Horizontal = True
        r.Dato("Periodo", $"{mes:00}/{anio}")
        Dim s = r.Seccion("", C("Fecha"), C("Regimen"), C("Servicio"), C("Rac. preparadas", FormatoColumna.Entero), C("Rac. vendidas", FormatoColumna.Entero),
                          C("Venta dia", FormatoColumna.Dinero), C("Valor bandeja", FormatoColumna.Dinero), C("Costo dia", FormatoColumna.Dinero),
                          C("Costo bandeja", FormatoColumna.Dinero), C("Food cost"))
        Dim ventaT As Long = 0, costoT As Long = 0, racPrepT As Long = 0, racVendT As Long = 0
        For Each m In MinutasDelMes(anio, mes)
            Dim c = comparativo.Comparativo(m.Id)
            Dim venta = c.VentaRealU6
            Dim costo = CType(c.CostoRealU6, Long?)
            Dim racPrep = c.RacionesPreparadas
            Dim racVend = c.RacionesVendidas
            s.Agregar(m.Fecha.ToString("dd/MM/yyyy"), m.Regimen, m.Servicio, racPrep, racVend, venta, PorBandeja(venta, racVend), costo,
                      PorBandeja(costo, racPrep), Porcentaje(costo, venta))
            ventaT += If(venta, 0L) : costoT += If(costo, 0L) : racPrepT += If(racPrep, 0L) : racVendT += If(racVend, 0L)
        Next
        s.Totales = {"TOTAL", Nothing, Nothing, racPrepT, racVendT, ventaT, PorBandeja(ventaT, racVendT), costoT,
                     PorBandeja(costoT, racPrepT), Porcentaje(costoT, ventaT)}
        r.Notas.Add("Venta y costo reales de cada minuta. Valor bandeja = venta ÷ raciones vendidas; costo bandeja = costo ÷ raciones preparadas.")
        Return r
    End Function

    ''' <summary>
    ''' Comparativo de tres niveles por minuta (formato del SGP): teórico (lo planificado), realizado (lo producido y
    ''' consumido) y la desviación. Hoja horizontal. El plan real por día no se separa aquí: viene del plan del SGP importado (V023).
    ''' </summary>
    Public Function ComparativoTresNiveles(anio As Integer, mes As Integer) As Reporte
        Dim comparativo As New ServicioComparativo(CadenaConexion, Sesion)
        Dim r = Nuevo($"Comparativo de tres niveles {mes:00}/{anio}")
        r.Horizontal = True
        r.Dato("Periodo", $"{mes:00}/{anio}")
        Dim s = r.Seccion("", C("Fecha"), C("Servicio"), C("Teorico raciones", FormatoColumna.Entero), C("Teorico costo total", FormatoColumna.Dinero),
                          C("Teorico costo bandeja", FormatoColumna.Dinero), C("Realizado raciones", FormatoColumna.Entero),
                          C("Realizado costo total", FormatoColumna.Dinero), C("Realizado costo bandeja", FormatoColumna.Dinero),
                          C("Desviacion costo total", FormatoColumna.Dinero), C("Desviacion costo bandeja", FormatoColumna.Dinero))
        Dim teoRac As Long = 0, teoCosto As Long = 0, realRac As Long = 0, realCosto As Long = 0
        For Each m In MinutasDelMes(anio, mes)
            Dim c = comparativo.Comparativo(m.Id)
            Dim teoricoCosto = c.CostoTeoricoU6
            Dim teoricoBandeja = PorBandeja(teoricoCosto, c.ComensalesPlan)
            Dim realCosto_ = CType(c.CostoRealU6, Long?)
            Dim realBandeja = PorBandeja(realCosto_, c.RacionesPreparadas)
            Dim desvBandeja = If(teoricoBandeja Is Nothing OrElse realBandeja Is Nothing, Nothing, CObj(CLng(realBandeja) - CLng(teoricoBandeja)))
            s.Agregar(m.Fecha.ToString("dd/MM/yyyy"), m.Servicio, c.ComensalesPlan, teoricoCosto, teoricoBandeja, c.RacionesPreparadas,
                      realCosto_, realBandeja, c.DiferenciaCostoU6, desvBandeja)
            teoRac += c.ComensalesPlan : teoCosto += If(teoricoCosto, 0L) : realRac += If(c.RacionesPreparadas, 0L) : realCosto += c.CostoRealU6
        Next
        s.Totales = {"TOTAL", Nothing, teoRac, teoCosto, PorBandeja(teoCosto, teoRac), realRac, realCosto, PorBandeja(realCosto, realRac),
                     realCosto - teoCosto, Nothing}
        r.Notas.Add("Teorico = lo planificado de la minuta. Realizado = lo producido y consumido (salidas netas de devoluciones).")
        Return r
    End Function

    ''' <summary>
    ''' Traspasos del periodo (entrada y salida) de las bodegas de la operación, con número de documento, fecha, bodega y
    ''' total valorizado, como el "Resumen de traspasos" del SGP. Solo documentos confirmados.
    ''' </summary>
    Public Function Traspasos(desde As Date, hasta As Date) As Reporte
        If hasta < desde Then Throw New ReglaNegocioException("DATO_INVALIDO", "El periodo termina antes de empezar.")
        Dim op = Sesion.OperacionId
        Dim filas = EnTransaccion(Permisos.CatalogoVer,
            Function(u)
                Return u.Consultar(
                    "SELECT d.tipo, d.numero, d.fecha, a.nombre, sum(l.valor_u6)::bigint FROM documento_stock d " &
                    "JOIN almacen a ON a.id = d.almacen_id JOIN documento_stock_detalle l ON l.documento_id = d.id " &
                    "WHERE a.operacion_id = @o AND d.tipo IN ('traspaso_entrada','traspaso_salida') AND d.estado = 'confirmado' AND d.fecha BETWEEN @d AND @h " &
                    "GROUP BY d.tipo, d.numero, d.fecha, a.nombre ORDER BY d.tipo, d.fecha, d.numero",
                    Function(rd) (Tipo:=rd.GetString(0), Numero:=rd.GetString(1), Fecha:=rd.GetDateTime(2).Date, Bodega:=rd.GetString(3), Total:=rd.GetInt64(4)),
                    "o", op, "d", desde.Date, "h", hasta.Date).ToList()
            End Function)
        Dim r = Nuevo("Resumen de traspasos")
        r.Dato("Periodo", $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
        For Each grupo In {"traspaso_entrada", "traspaso_salida"}
            Dim lineas = filas.Where(Function(x) x.Tipo = grupo).ToList()
            Dim s = r.Seccion(If(grupo = "traspaso_entrada", "Traspasos de entrada", "Traspasos de salida"),
                              C("N. documento"), C("Fecha"), C("Bodega"), C("Total", FormatoColumna.Dinero))
            For Each x In lineas
                s.Agregar(x.Numero, x.Fecha.ToString("dd/MM/yyyy"), x.Bodega, x.Total)
            Next
            s.Totales = {"Total " & If(grupo = "traspaso_entrada", "entradas", "salidas"), $"{lineas.Count} documentos", Nothing, lineas.Sum(Function(x) x.Total)}
        Next
        Return r
    End Function

    ''' <summary>
    ''' Boleta de ajuste de inventario (formato R-AL-15-2 del SGP): los ajustes de entrada y de salida del periodo, con el
    ''' código, la descripción, la unidad, la cantidad, el costo y el valor. La columna de explicación queda en blanco con
    ''' espacio para escribir a mano: el formato se llena en papel y no se guarda en el sistema.
    ''' </summary>
    Public Function BoletaAjuste(desde As Date, hasta As Date) As Reporte
        If hasta < desde Then Throw New ReglaNegocioException("DATO_INVALIDO", "El periodo termina antes de empezar.")
        Dim op = Sesion.OperacionId
        Dim filas = EnTransaccion(Permisos.CatalogoVer,
            Function(u)
                Return u.Consultar(
                    "SELECT d.tipo, v.codigo, v.descripcion_comercial, um.codigo, sum(abs(l.cantidad_base_u6))::bigint, sum(abs(l.valor_u6))::bigint " &
                    "FROM documento_stock d JOIN almacen a ON a.id = d.almacen_id JOIN documento_stock_detalle l ON l.documento_id = d.id " &
                    "JOIN variante_producto v ON v.id = l.variante_id JOIN producto_base p ON p.id = v.producto_base_id " &
                    "JOIN unidad_medida um ON um.id = p.unidad_base_id " &
                    "WHERE a.operacion_id = @o AND d.tipo IN ('ajuste_positivo','ajuste_negativo') AND d.estado = 'confirmado' AND d.fecha BETWEEN @d AND @h " &
                    "GROUP BY d.tipo, v.codigo, v.descripcion_comercial, um.codigo ORDER BY d.tipo, v.descripcion_comercial",
                    Function(rd) (Tipo:=rd.GetString(0), Codigo:=rd.GetString(1), Descripcion:=rd.GetString(2), Unidad:=rd.GetString(3),
                                  Cantidad:=rd.GetInt64(4), Valor:=rd.GetInt64(5)),
                    "o", op, "d", desde.Date, "h", hasta.Date).ToList()
            End Function)
        Dim r = Nuevo("Boleta de ajuste de inventario")
        r.Dato("Contrato", Sesion.Operacion?.ToString())
        r.Dato("Boleta No.", "")
        r.Dato("Periodo", $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
        For Each tipo In {"ajuste_positivo", "ajuste_negativo"}
            Dim lineas = filas.Where(Function(x) x.Tipo = tipo).ToList()
            Dim s = r.Seccion(If(tipo = "ajuste_positivo", "Tipo de ajuste: ENTRADA", "Tipo de ajuste: SALIDA"),
                              C("Codigo"), C("Descripcion"), C("Explicacion del ajuste"), C("Unidad"), C("Cantidad", FormatoColumna.Cantidad),
                              C("Costo", FormatoColumna.Dinero), C("Valor S/", FormatoColumna.Dinero))
            s.AlturaFila = 46
            Dim valorTotal As Long = 0
            For Each x In lineas
                Dim costo = If(x.Cantidad > 0, CLng(Math.Round(CDec(x.Valor) * 1000000D / x.Cantidad, 0, MidpointRounding.AwayFromZero)), CType(Nothing, Long?))
                s.Agregar(x.Codigo, x.Descripcion, Nothing, x.Unidad, x.Cantidad, costo, x.Valor)
                valorTotal += x.Valor
            Next
            s.Totales = {"TOTAL", $"{lineas.Count} productos", Nothing, Nothing, Nothing, Nothing, valorTotal}
        Next
        r.Firmas.AddRange({"Elaborado por", "Autorizado por"})
        Return r
    End Function

    ''' <summary>
    ''' Explicación de ajustes (formato R-AI-15-2 del SGP): las diferencias físico vs sistema de un inventario, ordenadas de
    ''' mayor a menor valor, con el ajuste (+ o −), el porcentaje que representa y el acumulado, y una columna de motivo
    ''' para llenar a mano. Hoja horizontal.
    ''' </summary>
    Public Function ExplicacionAjustes(inventarioId As Long) As Reporte
        Dim servicio As New ServicioInventarios(CadenaConexion, Sesion)
        Dim cab = EnTransaccion(Permisos.InventarioContar,
            Function(u) u.Consultar(
                "SELECT i.numero, i.fecha_corte, a.codigo || ' - ' || a.nombre FROM inventario i JOIN almacen a ON a.id = i.almacen_id " &
                "WHERE i.id = @i AND a.operacion_id = @o",
                Function(rd) (Numero:=rd.GetString(0), Corte:=rd.GetDateTime(1), Almacen:=rd.GetString(2)),
                "i", inventarioId, "o", Sesion.OperacionId).ToList())
        If cab.Count = 0 Then Throw New ReglaNegocioException("OPERACION_AJENA", "El inventario no pertenece a la operacion seleccionada.")
        Dim lineas = servicio.Hoja(inventarioId).Where(Function(l) l.FisicoU6.HasValue AndAlso l.DiferenciaU6.HasValue AndAlso l.DiferenciaU6.Value <> 0).
                     OrderByDescending(Function(l) Math.Abs(l.ValorDiferenciaU6.GetValueOrDefault())).ToList()
        Dim total = lineas.Sum(Function(l) Math.Abs(l.ValorDiferenciaU6.GetValueOrDefault()))

        Dim r = Nuevo($"Explicacion de ajustes {cab(0).Numero}")
        r.Horizontal = True
        r.Dato("Bodega", cab(0).Almacen)
        r.Dato("Fecha de toma de inventario", cab(0).Corte.ToString("dd/MM/yyyy"))
        r.Dato("Familia de producto", "Todas")
        Dim s = r.Seccion("", C("Codigo"), C("Descripcion"), C("Unidad"), C("P.M.P.", FormatoColumna.Dinero), C("Stock fisico", FormatoColumna.Cantidad),
                          C("Total fisico", FormatoColumna.Dinero), C("Stock sist.", FormatoColumna.Cantidad), C("Total sist.", FormatoColumna.Dinero),
                          C("Diferencia", FormatoColumna.Cantidad), C("Total dif.", FormatoColumna.Dinero), C("Ajuste +", FormatoColumna.Dinero),
                          C("Ajuste -", FormatoColumna.Dinero), C("Ajuste total", FormatoColumna.Dinero), C("% acumulado"), C("Motivo del ajuste"))
        s.AlturaFila = 30
        Dim acumulado As Long = 0, mas As Long = 0, menos As Long = 0
        For Each l In lineas
            Dim valor = l.ValorDiferenciaU6.GetValueOrDefault()
            acumulado += Math.Abs(valor)
            mas += Math.Max(valor, 0L) : menos += Math.Min(valor, 0L)
            Dim pct = If(total > 0, (CDbl(acumulado) * 100D / total).ToString("0.00", Globalization.CultureInfo.InvariantCulture) & " %", Nothing)
            s.Agregar(l.VarianteCodigo, l.Descripcion, l.Unidad, l.CostoU6, l.FisicoU6, ValorDe(l.FisicoU6, l.CostoU6), l.SistemaU6,
                      ValorDe(l.SistemaU6, l.CostoU6), l.DiferenciaU6, valor, If(valor > 0, CObj(valor), Nothing), If(valor < 0, CObj(valor), Nothing),
                      Math.Abs(valor), pct, Nothing)
        Next
        s.Totales = {"TOTAL", $"{lineas.Count} productos", Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, mas + menos, mas, menos, total, Nothing, Nothing}
        r.Firmas.AddRange({"Elaborado por", "Revisado por", "Autorizado por"})
        Return r
    End Function

    ''' <summary>
    ''' Consumo alternativo (Informes > Stock del SGP; antes "ajuste de inventario"): los productos de una toma de inventario cuya
    ''' diferencia entre lo físico y el sistema no es cero, en orden alfabético, con su diferencia, precio (P.M.P.) y total, y el
    ''' total general. En el cierre de Orcopampa del 27/09/2026 son exactamente las filas con diferencia del reporte
    ''' "Diferencias físico vs sistema - valorizado", con los mismos valores.
    ''' </summary>
    Public Function ConsumoAlternativo(inventarioId As Long) As Reporte
        Dim servicio As New ServicioInventarios(CadenaConexion, Sesion)
        Dim cab = EnTransaccion(Permisos.InventarioContar,
            Function(u) u.Consultar(
                "SELECT i.numero, i.fecha_corte, a.codigo || ' - ' || a.nombre FROM inventario i JOIN almacen a ON a.id = i.almacen_id " &
                "WHERE i.id = @i AND a.operacion_id = @o",
                Function(rd) (Numero:=rd.GetString(0), Corte:=rd.GetDateTime(1), Almacen:=rd.GetString(2)),
                "i", inventarioId, "o", Sesion.OperacionId).ToList())
        If cab.Count = 0 Then Throw New ReglaNegocioException("OPERACION_AJENA", "El inventario no pertenece a la operacion seleccionada.")
        Dim lineas = servicio.Hoja(inventarioId).Where(Function(l) l.FisicoU6.HasValue AndAlso l.DiferenciaU6.HasValue AndAlso l.DiferenciaU6.Value <> 0).
                     OrderBy(Function(l) l.Descripcion, StringComparer.CurrentCultureIgnoreCase).ToList()

        Dim r = Nuevo("Consumo alternativo")
        r.Dato("Bodega", cab(0).Almacen)
        r.Dato("Toma de inventario", cab(0).Corte.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("", C("Codigo"), C("Descripcion"), C("Unidad"), C("Diferencia", FormatoColumna.Cantidad), C("Precio", FormatoColumna.Dinero), C("Total", FormatoColumna.Dinero))
        Dim total As Long = 0
        For Each l In lineas
            Dim valor = l.ValorDiferenciaU6.GetValueOrDefault()
            total += valor
            s.Agregar(l.VarianteCodigo, l.Descripcion, l.Unidad, l.DiferenciaU6, l.CostoU6, valor)
        Next
        s.Totales = {"TOTAL GENERAL", $"{lineas.Count} productos", Nothing, Nothing, Nothing, total}
        r.Firmas.AddRange({"Elaborado por", "Revisado por", "Autorizado por"})
        Return r
    End Function

    ''' <summary>
    ''' Menú del mes por servicio, en hoja horizontal, como la planificación del SGP: una columna por día, con el costo del
    ''' día y el costo total del servicio. El teórico es lo que planifica el área de planificación (la minuta aprobada, con
    ''' sus raciones y su costo previsto por ración). El real son las raciones preparadas que registra el chef en el cierre
    ''' diario (consumo por plato); si el día no tiene registro, la celda queda vacía.
    ''' </summary>
    ''' <param name="real">False = teórico (planificación); True = real (chef).</param>
    Public Function MenuMes(anio As Integer, mes As Integer, real As Boolean) As Reporte
        Dim desde As New Date(anio, mes, 1)
        Dim hasta = desde.AddMonths(1).AddDays(-1)
        Dim op = Sesion.OperacionId
        Dim platos = EnTransaccion(Permisos.MenusVer,
            Function(u)
                Return u.Consultar(
                    "SELECT s.nombre, reg.nombre, m.fecha, e.orden, e.nombre, r.codigo, md.raciones, md.costo_previsto_racion_u6, cp.raciones_preparadas " &
                    "FROM minuta m JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
                    "JOIN servicio s ON s.id = os.servicio_id JOIN regimen reg ON reg.id = os.regimen_id " &
                    "JOIN minuta_detalle md ON md.minuta_id = m.id JOIN estructura_servicio e ON e.id = md.estructura_id " &
                    "JOIN receta_version rv ON rv.id = md.receta_version_id JOIN receta r ON r.id = rv.receta_id " &
                    "LEFT JOIN consumo_plato cp ON cp.minuta_detalle_id = md.id " &
                    "WHERE os.operacion_id = @o AND m.estado IN ('aprobada','cerrada') AND m.fecha BETWEEN @d AND @h " &
                    "ORDER BY s.nombre, reg.nombre, m.fecha, e.orden",
                    Function(rd) (Servicio:=rd.GetString(0) & " - " & rd.GetString(1), Fecha:=rd.GetDateTime(2).Date, Orden:=rd.GetInt64(3),
                                  Estructura:=rd.GetString(4), Receta:=rd.GetString(5), Raciones:=rd.GetInt64(6),
                                  CostoU6:=If(rd.IsDBNull(7), CType(Nothing, Long?), rd.GetInt64(7)),
                                  RacionesReales:=If(rd.IsDBNull(8), CType(Nothing, Long?), rd.GetInt64(8))),
                    "o", op, "d", desde.Date, "h", hasta.Date).ToList()
            End Function)

        Dim r = Nuevo($"{If(real, "Menu real", "Menu teorico (planificacion)")} {mes:00}/{anio}")
        r.Horizontal = True
        r.Dato("Periodo", $"{mes:00}/{anio}")
        r.Dato("Nivel", If(real, "REAL (chef, cierre diario)", "TEORICO (planificacion)"))
        Dim dias = hasta.Day
        Dim cultura = Globalization.CultureInfo.InvariantCulture

        For Each grupo In platos.GroupBy(Function(x) x.Servicio).OrderBy(Function(g) g.Key, StringComparer.CurrentCultureIgnoreCase)
            Dim columnas As New List(Of ColumnaReporte) From {C("Estructura")}
            For d = 1 To dias
                columnas.Add(C(d.ToString("00"), FormatoColumna.Entero))
            Next
            Dim s = r.Seccion(grupo.Key, columnas.ToArray())

            ' Una estructura ocupa tres filas: receta (código), raciones y costo del plato por ración (S/).
            Dim ordenes = grupo.Select(Function(x) x.Orden).Distinct().OrderBy(Function(x) x).ToList()
            Dim costoTotalU6 As Long = 0
            Dim costoDiaU6(dias) As Long
            Dim hayDia(dias) As Boolean
            For Each orden In ordenes
                Dim estructura = grupo.First(Function(x) x.Orden = orden).Estructura
                Dim fila1 As New List(Of Object) From {estructura}
                Dim fila2 As New List(Of Object) From {"  Rac."}
                Dim fila3 As New List(Of Object) From {"  Cto."}
                For d = 1 To dias
                    Dim dia = d
                    Dim plato = grupo.Where(Function(x) x.Orden = orden AndAlso x.Fecha.Day = dia).ToList()
                    If plato.Count = 0 Then
                        fila1.Add(Nothing) : fila2.Add(Nothing) : fila3.Add(Nothing)
                        Continue For
                    End If
                    Dim p = plato(0)
                    Dim raciones = If(real, p.RacionesReales, CType(p.Raciones, Long?))
                    fila1.Add(p.Receta)
                    fila2.Add(raciones)
                    If p.CostoU6.HasValue Then
                        fila3.Add((p.CostoU6.Value / 1000000D).ToString("0.00", cultura))
                        If raciones.HasValue Then
                            costoDiaU6(d) += p.CostoU6.Value * raciones.Value
                            hayDia(d) = True
                        End If
                    Else
                        fila3.Add(Nothing)
                    End If
                Next
                s.Agregar(fila1.ToArray()) : s.Agregar(fila2.ToArray()) : s.Agregar(fila3.ToArray())
            Next

            ' Costo del día (raciones × costo por ración de cada plato) y costo total del servicio en el mes.
            Dim filaCosto As New List(Of Object) From {"Costo minuta dia"}
            For d = 1 To dias
                If hayDia(d) Then
                    filaCosto.Add((costoDiaU6(d) / 1000000D).ToString("0", cultura))
                    costoTotalU6 += costoDiaU6(d)
                Else
                    filaCosto.Add(Nothing)
                End If
            Next
            filaCosto(0) = "Costo minuta dia (S/)"
            s.Totales = filaCosto.ToArray()
            r.Notas.Add($"{grupo.Key}: costo total del servicio en el mes = S/ {(costoTotalU6 / 1000000D).ToString("N2", cultura)}")
        Next

        r.Notas.Add("Codigo = codigo de la receta en AppSistema. Cto. = costo del plato por racion (S/). Costo minuta dia = raciones x costo por racion de cada plato.")
        r.Firmas.AddRange({"Elaborado por", "Revisado por"})
        Return r
    End Function

    ' ---------- Registro de inventario permanente valorizado (SUNAT, formato 13.1) ----------

    ''' <summary>
    ''' Formato 13.1 de SUNAT del almacén en el periodo: por cada existencia con saldo o movimiento, el saldo inicial y
    ''' cada entrada y salida con su comprobante (tabla 10), tipo de operación (tabla 12), cantidad, costo unitario y costo
    ''' total, y el saldo corrido. Cantidades en la unidad base del producto; valuación por promedio móvil (D01).
    ''' </summary>
    ''' <param name="tipoExistencia">Tabla 5 (01 mercaderías, 03 materias primas…). Por defecto 03.</param>
    Public Function RegistroInventarioPermanente(almacenId As Long, desde As Date, hasta As Date, Optional tipoExistencia As String = "03") As Reporte
        If hasta < desde Then Throw New ReglaNegocioException("DATO_INVALIDO", "El periodo termina antes de empezar.")
        Dim tipo = Sunat.TablasSunat.TipoExistencia(tipoExistencia)
        Dim op = Sesion.OperacionId
        Dim datos = EnTransaccion(Permisos.CatalogoVer,
            Function(u)
                Dim almacen = u.Escalar("SELECT codigo || ' - ' || nombre FROM almacen WHERE id = @a AND operacion_id = @o", "a", almacenId, "o", op)
                If almacen Is Nothing Then Throw New ReglaNegocioException("OPERACION_AJENA", "El almacen no pertenece a la operacion seleccionada.")
                Dim empresa = u.Consultar("SELECT nombre, COALESCE(identificacion_fiscal, '') FROM empresa WHERE id = @e",
                                          Function(rd) (Nombre:=rd.GetString(0), Ruc:=rd.GetString(1)), "e", Sesion.EmpresaId).Single()
                Dim iniciales = u.Consultar(
                    "SELECT variante_id, sum(signo * cantidad_base_u6)::bigint, sum(signo * valor_u6)::bigint FROM movimiento_stock " &
                    "WHERE almacen_id = @a AND fecha < @d GROUP BY variante_id HAVING sum(signo * cantidad_base_u6) <> 0 OR sum(signo * valor_u6) <> 0",
                    Function(rd) (Variante:=rd.GetInt64(0), Cant:=rd.GetInt64(1), Valor:=rd.GetInt64(2)), "a", almacenId, "d", desde.Date) _
                    .ToDictionary(Function(x) x.Variante)
                Dim movimientos = u.Consultar(
                    "SELECT m.variante_id, m.fecha, d.numero, d.tipo, m.signo, m.cantidad_base_u6, m.valor_u6, r.tipo_documento, r.numero_documento " &
                    "FROM movimiento_stock m JOIN documento_stock_detalle l ON l.id = m.documento_detalle_id JOIN documento_stock d ON d.id = l.documento_id " &
                    "LEFT JOIN recepcion r ON r.id = d.recepcion_id " &
                    "WHERE m.almacen_id = @a AND m.fecha BETWEEN @d AND @h ORDER BY m.variante_id, m.fecha, m.secuencia, m.id",
                    Function(rd) (Variante:=rd.GetInt64(0), Fecha:=rd.GetDateTime(1), Numero:=rd.GetString(2), Tipo:=rd.GetString(3), Signo:=rd.GetInt64(4),
                                  Cant:=rd.GetInt64(5), Valor:=rd.GetInt64(6), Comprobante:=rd.TextoONada("tipo_documento"), NumeroComprobante:=rd.TextoONada("numero_documento")),
                    "a", almacenId, "d", desde.Date, "h", hasta.Date)
                Dim ids = iniciales.Keys.Union(movimientos.Select(Function(m) m.Variante)).Distinct().ToArray()
                Dim variantes = u.Consultar(
                    "SELECT v.id, v.codigo, v.descripcion_comercial, um.codigo FROM variante_producto v JOIN producto_base p ON p.id = v.producto_base_id " &
                    "JOIN unidad_medida um ON um.id = p.unidad_base_id WHERE v.id = ANY(@ids) ORDER BY v.codigo",
                    Function(rd) (Id:=rd.GetInt64(0), Codigo:=rd.GetString(1), Descripcion:=rd.GetString(2), Unidad:=rd.GetString(3)), "ids", ids)
                Return (Almacen:=CStr(almacen), Empresa:=empresa, Iniciales:=iniciales, Movimientos:=movimientos, Variantes:=variantes)
            End Function)

        Dim r = Nuevo("Registro de inventario permanente valorizado")
        r.Dato("Formato", "FORMATO 13.1: REGISTRO DE INVENTARIO PERMANENTE VALORIZADO - DETALLE DEL INVENTARIO VALORIZADO")
        r.Dato("Periodo", If(desde.Day = 1 AndAlso hasta = desde.AddMonths(1).AddDays(-1), desde.ToString("MM/yyyy"), $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}"))
        r.Dato("RUC", If(datos.Empresa.Ruc = "", "(sin RUC registrado en la empresa)", datos.Empresa.Ruc))
        r.Dato("Apellidos y nombres, denominacion o razon social", datos.Empresa.Nombre)
        r.Dato("Establecimiento", datos.Almacen)
        r.Dato("Metodo de valuacion", Sunat.TablasSunat.MetodoValuacion)
        Dim totalEntradas As Long = 0, totalSalidas As Long = 0, totalSaldo As Long = 0
        For Each v In datos.Variantes
            Dim s = r.Seccion($"Codigo de la existencia: {v.Codigo} | Tipo (tabla 5): {tipo} | Descripcion: {v.Descripcion} | " &
                              $"Unidad de medida (tabla 6): {Sunat.TablasSunat.UnidadMedida(v.Unidad)}",
                              C("Fecha", FormatoColumna.Fecha), C("Tipo comprobante (tabla 10)"), C("Serie"), C("Numero"), C("Tipo de operacion (tabla 12)"),
                              C("Entradas cantidad", FormatoColumna.Cantidad), C("Entradas costo unitario", FormatoColumna.Dinero), C("Entradas costo total", FormatoColumna.Dinero),
                              C("Salidas cantidad", FormatoColumna.Cantidad), C("Salidas costo unitario", FormatoColumna.Dinero), C("Salidas costo total", FormatoColumna.Dinero),
                              C("Saldo cantidad", FormatoColumna.Cantidad), C("Saldo costo unitario", FormatoColumna.Dinero), C("Saldo costo total", FormatoColumna.Dinero))
            Dim ini As (Variante As Long, Cant As Long, Valor As Long) = Nothing
            Dim cant As Long = 0, valor As Long = 0
            If datos.Iniciales.TryGetValue(v.Id, ini) Then cant = ini.Cant : valor = ini.Valor
            s.Agregar(desde.Date, "00", "", "SALDO INICIAL", Sunat.TablasSunat.TipoOperacion("apertura"), Nothing, Nothing, Nothing, Nothing, Nothing, Nothing,
                      cant, Valoracion.CostoUnitarioU6(valor, cant), valor)
            Dim entCant As Long = 0, entValor As Long = 0, salCant As Long = 0, salValor As Long = 0
            For Each m In datos.Movimientos.Where(Function(x) x.Variante = v.Id)
                cant += m.Signo * m.Cant
                valor += m.Signo * m.Valor
                Dim externo = m.Comprobante IsNot Nothing
                Dim sn = If(externo, Sunat.TablasSunat.SerieYNumero(m.NumeroComprobante), ("", m.Numero))
                Dim unitario = Valoracion.CostoUnitarioU6(m.Valor, m.Cant)
                If m.Signo > 0 Then
                    entCant += m.Cant : entValor += m.Valor
                    s.Agregar(m.Fecha, If(externo, Sunat.TablasSunat.TipoComprobante(m.Comprobante), "00"), sn.Item1, sn.Item2, Sunat.TablasSunat.TipoOperacion(m.Tipo),
                              m.Cant, unitario, m.Valor, Nothing, Nothing, Nothing, cant, Valoracion.CostoUnitarioU6(valor, cant), valor)
                Else
                    salCant += m.Cant : salValor += m.Valor
                    s.Agregar(m.Fecha, "00", "", m.Numero, Sunat.TablasSunat.TipoOperacion(m.Tipo),
                              Nothing, Nothing, Nothing, m.Cant, unitario, m.Valor, cant, Valoracion.CostoUnitarioU6(valor, cant), valor)
                End If
            Next
            s.Totales = {"TOTALES", Nothing, Nothing, Nothing, Nothing, entCant, Nothing, entValor, salCant, Nothing, salValor, cant, Valoracion.CostoUnitarioU6(valor, cant), valor}
            totalEntradas += entValor : totalSalidas += salValor : totalSaldo += valor
        Next
        r.Dato("Existencias", datos.Variantes.Count.ToString())
        r.Dato("Total entradas (S/)", Soles(totalEntradas))
        r.Dato("Total salidas (S/)", Soles(totalSalidas))
        r.Dato("Saldo final valorizado (S/)", Soles(totalSaldo))
        r.Notas.Add("Tablas de SUNAT: 5 tipo de existencia, 6 unidad de medida, 10 tipo de comprobante (00 = documento interno) y 12 tipo de operacion.")
        r.Notas.Add("Cantidades en la unidad base del producto. Valuacion por promedio movil: entradas a su costo y salidas al costo promedio vigente (D01).")
        Return r
    End Function

    ' ---------- Reportes del menú de reportes: requisición por rango, salidas por servicio, frecuencia, piso y techo ----------

    ''' <summary>
    ''' Requisición detallada por rango (formato del SGP): por servicio y día, cada receta con sus productos, las raciones,
    ''' la cantidad bruta por ración, el bulto y el despacho. Viene del plan del SGP importado (V023). Hoja horizontal.
    ''' </summary>
    Public Function RequisicionRango(desde As Date, hasta As Date) As Reporte
        If hasta < desde Then Throw New ReglaNegocioException("DATO_INVALIDO", "El periodo termina antes de empezar.")
        Dim op = Sesion.OperacionId
        Dim filas = EnTransaccion(Permisos.MenusVer,
            Function(u)
                Return u.Consultar(
                    "SELECT q.regimen, q.servicio, q.fecha, cr.nombre_sgp, cp.descripcion_sgp, q.raciones_u6, q.cantidad_bruta_racion_u6, " &
                    "       q.cantidad_bulto_u6, q.unidad_bulto, q.cantidad_despacho_u6, q.unidad_despacho " &
                    "FROM sgp_requisicion q JOIN sgp_codigo_receta cr ON cr.empresa_id = q.empresa_id AND cr.codigo_sgp = q.receta_codigo_sgp " &
                    "JOIN sgp_codigo_producto cp ON cp.empresa_id = q.empresa_id AND cp.codigo_sgp = q.producto_codigo_sgp " &
                    "WHERE q.operacion_id = @o AND q.fecha BETWEEN @d AND @h ORDER BY q.regimen, q.servicio, q.fecha, cr.nombre_sgp, cp.descripcion_sgp",
                    Function(rd) (Regimen:=rd.GetString(0), Servicio:=rd.GetString(1), Fecha:=rd.GetDateTime(2).Date, Receta:=rd.GetString(3),
                                  Producto:=rd.GetString(4), Raciones:=rd.GetInt64(5), Bruta:=rd.GetInt64(6), Bulto:=rd.GetInt64(7),
                                  UnidadBulto:=rd.TextoONada("unidad_bulto"), Despacho:=rd.GetInt64(9), UnidadDespacho:=rd.TextoONada("unidad_despacho")),
                    "o", op, "d", desde.Date, "h", hasta.Date).ToList()
            End Function)
        Dim r = Nuevo($"Requisicion detallada {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
        r.Horizontal = True
        r.Dato("Periodo", $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
        For Each grupo In filas.GroupBy(Function(x) x.Servicio & " - " & x.Regimen & " - " & x.Fecha.ToString("dd/MM/yyyy")).ToList()
            Dim s = r.Seccion(grupo.Key, C("Receta"), C("Producto"), C("Raciones", FormatoColumna.Entero), C("Bruta por racion", FormatoColumna.Cantidad),
                              C("Bulto", FormatoColumna.Cantidad), C("Unidad bulto"), C("Despacho", FormatoColumna.Cantidad), C("Unidad despacho"))
            For Each x In grupo
                s.Agregar(x.Receta, x.Producto, x.Raciones / 1000000L, x.Bruta, x.Bulto, x.UnidadBulto, x.Despacho, x.UnidadDespacho)
            Next
        Next
        r.Notas.Add("Bruta por racion = cantidad del producto por racion. Bulto = presentacion de compra; despacho = cantidad a entregar.")
        Return r
    End Function

    ''' <summary>
    ''' Salidas (o devoluciones) a producción por servicio y producto del periodo: una tabla por servicio y régimen, con la
    ''' cantidad y el total valorizado. Hoja horizontal.
    ''' </summary>
    Public Function SalidasPorServicio(desde As Date, hasta As Date, devoluciones As Boolean) As Reporte
        If hasta < desde Then Throw New ReglaNegocioException("DATO_INVALIDO", "El periodo termina antes de empezar.")
        Dim op = Sesion.OperacionId
        Dim tipo = If(devoluciones, "devolucion_produccion", "salida_produccion")
        Dim signo = If(devoluciones, 1L, -1L)
        Dim filas = EnTransaccion(Permisos.CatalogoVer,
            Function(u)
                Return u.Consultar(
                    "SELECT s.nombre || ' - ' || reg.nombre, v.codigo, v.descripcion_comercial, um.codigo, sum(m.signo * m.cantidad_base_u6)::bigint, sum(m.signo * m.valor_u6)::bigint " &
                    "FROM movimiento_stock m JOIN documento_stock_detalle l ON l.id = m.documento_detalle_id JOIN documento_stock d ON d.id = l.documento_id " &
                    "JOIN variante_producto v ON v.id = m.variante_id JOIN producto_base p ON p.id = v.producto_base_id " &
                    "JOIN unidad_medida um ON um.id = p.unidad_base_id " &
                    "JOIN operacion_servicio os ON os.id = d.operacion_servicio_id JOIN servicio s ON s.id = os.servicio_id " &
                    "JOIN regimen reg ON reg.id = os.regimen_id " &
                    "WHERE os.operacion_id = @o AND d.tipo = @t AND m.fecha BETWEEN @d AND @h " &
                    "GROUP BY 1, 2, 3, 4 ORDER BY 1, 3",
                    Function(rd) (Servicio:=rd.GetString(0), Codigo:=rd.GetString(1), Descripcion:=rd.GetString(2), Unidad:=rd.GetString(3),
                                  Cant:=rd.GetInt64(4), Valor:=rd.GetInt64(5)),
                    "o", op, "t", tipo, "d", desde.Date, "h", hasta.Date).ToList()
            End Function)
        Dim r = Nuevo(If(devoluciones, "Devoluciones de produccion por servicio", "Salidas a produccion por servicio"))
        r.Horizontal = True
        r.Dato("Periodo", $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}")
        For Each grupo In filas.GroupBy(Function(x) x.Servicio).ToList()
            Dim s = r.Seccion(grupo.Key, C("Codigo"), C("Descripcion"), C("Cantidad", FormatoColumna.Cantidad), C("Unidad"), C("Total", FormatoColumna.Dinero))
            For Each x In grupo
                s.Agregar(x.Codigo, x.Descripcion, signo * x.Cant, x.Unidad, signo * x.Valor)
            Next
            s.Totales = {"TOTAL", $"{grupo.Count()} productos", Nothing, Nothing, signo * grupo.Sum(Function(x) x.Valor)}
        Next
        Return r
    End Function

    ''' <summary>
    ''' Frecuencia de la planificación teórica del mes (formato del SGP): cuántas veces sale cada receta, en qué días, las
    ''' raciones y el costo por ración promedio. Hoja horizontal.
    ''' </summary>
    Public Function FrecuenciaTeorica(anio As Integer, mes As Integer) As Reporte
        Dim desde As New Date(anio, mes, 1)
        Dim hasta = desde.AddMonths(1).AddDays(-1)
        Dim op = Sesion.OperacionId
        Dim filas = EnTransaccion(Permisos.MenusVer,
            Function(u)
                Return u.Consultar(
                    "SELECT r.codigo, r.nombre, m.fecha, md.raciones, md.costo_previsto_racion_u6 " &
                    "FROM minuta m JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
                    "JOIN minuta_detalle md ON md.minuta_id = m.id JOIN receta_version rv ON rv.id = md.receta_version_id JOIN receta r ON r.id = rv.receta_id " &
                    "WHERE os.operacion_id = @o AND m.estado IN ('aprobada','cerrada') AND m.fecha BETWEEN @d AND @h ORDER BY r.nombre, m.fecha",
                    Function(rd) (Codigo:=rd.GetString(0), Receta:=rd.GetString(1), Fecha:=rd.GetDateTime(2).Date, Raciones:=rd.GetInt64(3),
                                  Costo:=If(rd.IsDBNull(4), CType(Nothing, Long?), rd.GetInt64(4))),
                    "o", op, "d", desde.Date, "h", hasta.Date).ToList()
            End Function)
        Dim r = Nuevo($"Frecuencia de la planificacion teorica {mes:00}/{anio}")
        r.Horizontal = True
        r.Dato("Periodo", $"{mes:00}/{anio}")
        Dim s = r.Seccion("", C("Codigo"), C("Receta"), C("Veces", FormatoColumna.Entero), C("Dias del mes"), C("Raciones", FormatoColumna.Entero),
                          C("Costo por racion (prom.)", FormatoColumna.Dinero), C("Costo total", FormatoColumna.Dinero))
        Dim totalCosto As Long = 0
        For Each grupo In filas.GroupBy(Function(x) x.Codigo).OrderBy(Function(g) g.First().Receta, StringComparer.CurrentCultureIgnoreCase)
            Dim fechas = grupo.Select(Function(x) x.Fecha).Distinct().OrderBy(Function(x) x).ToList()
            Dim raciones = grupo.Sum(Function(x) x.Raciones)
            Dim costoU6 = grupo.Where(Function(x) x.Costo.HasValue).Sum(Function(x) x.Costo.Value * x.Raciones)
            totalCosto += costoU6
            s.Agregar(grupo.Key, grupo.First().Receta, fechas.Count, String.Join(", ", fechas.Select(Function(f) f.Day.ToString("00"))), raciones,
                      PorBandeja(costoU6, raciones), costoU6 \ 1000000L)
        Next
        s.Totales = {"TOTAL", $"{filas.Select(Function(x) x.Codigo).Distinct().Count()} recetas", Nothing, Nothing, Nothing, Nothing, totalCosto \ 1000000L}
        r.Notas.Add("Costo por racion (prom.) = costo total de la receta / raciones. Costo total en soles.")
        Return r
    End Function

    ''' <summary>
    ''' Costo piso y techo por factores del mes (formato del SGP): por nivel, servicio y mes, el piso, el costo medio y el
    ''' techo de la banda, con el día más barato y el más caro. Viene del plan del SGP importado (V023). Hoja horizontal.
    ''' </summary>
    Public Function CostoPisoTecho(anio As Integer, mes As Integer) As Reporte
        Dim mesTexto = $"{anio}-{mes:00}"
        Dim op = Sesion.OperacionId
        Dim filas = EnTransaccion(Permisos.MenusVer,
            Function(u)
                Return u.Consultar(
                    "SELECT nivel, servicio, componentes, costo_piso_u6, costo_medio_u6, costo_techo_u6, dia_mas_barato_u6, dia_mas_caro_u6, dias_dentro " &
                    "FROM sgp_costo_piso_techo WHERE operacion_id = @o AND mes = @m ORDER BY nivel, servicio",
                    Function(rd) (Nivel:=rd.GetString(0), Servicio:=rd.GetString(1), Componentes:=rd.GetInt64(2), Piso:=rd.GetInt64(3),
                                  Medio:=rd.GetInt64(4), Techo:=rd.GetInt64(5), Barato:=rd.GetInt64(6), Caro:=rd.GetInt64(7), Dentro:=rd.GetString(8)),
                    "o", op, "m", mesTexto).ToList()
            End Function)
        Dim r = Nuevo($"Costo piso y techo {mes:00}/{anio}")
        r.Horizontal = True
        r.Dato("Periodo", $"{mes:00}/{anio}")
        Dim s = r.Seccion("", C("Nivel"), C("Servicio"), C("Componentes", FormatoColumna.Entero), C("Costo piso", FormatoColumna.Dinero),
                          C("Costo medio", FormatoColumna.Dinero), C("Costo techo", FormatoColumna.Dinero), C("Dia mas barato", FormatoColumna.Dinero),
                          C("Dia mas caro", FormatoColumna.Dinero), C("Dias dentro de la banda"))
        For Each x In filas
            s.Agregar(x.Nivel, x.Servicio, x.Componentes, x.Piso, x.Medio, x.Techo, x.Barato, x.Caro, x.Dentro)
        Next
        r.Notas.Add("Piso = suma de factor x la racion mas barata de cada componente; techo = suma de factor x la mas cara.")
        Return r
    End Function

    Private Function NombresAlmacenVariante(almacenId As Long, varianteId As Long?, permiso As String) As (Almacen As String, Variante As String, Unidad As String)
        Dim op = Sesion.OperacionId
        Return EnTransaccion(permiso,
            Function(u)
                Dim almacen = u.Escalar("SELECT codigo || ' - ' || nombre FROM almacen WHERE id = @a AND operacion_id = @o", "a", almacenId, "o", op)
                If almacen Is Nothing Then Throw New ReglaNegocioException("OPERACION_AJENA", "El almacen no pertenece a la operacion seleccionada.")
                If Not varianteId.HasValue Then Return (CStr(almacen), "", "")
                Dim v = u.Consultar("SELECT v.codigo || ' ' || v.descripcion_comercial, um.codigo FROM variante_producto v JOIN producto_base p ON p.id = v.producto_base_id " &
                                    "JOIN unidad_medida um ON um.id = p.unidad_base_id WHERE v.id = @v",
                                    Function(rd) (rd.GetString(0), rd.GetString(1)), "v", varianteId.Value).SingleOrDefault()
                If v.Item1 Is Nothing Then Throw New ReglaNegocioException("NO_ENCONTRADO", "La presentacion no existe.")
                Return (CStr(almacen), v.Item1, v.Item2)
            End Function)
    End Function

    ' ---------- Matriz de planificación ----------

    ''' <summary>Matriz del periodo: una fila por minuta y un resumen por día con costo, techo y desviación. Sin costo = vacío.</summary>
    Public Function MatrizDelPeriodo(desde As Date, hasta As Date) As Reporte
        Dim matriz As New ServicioMatrizMenu(CadenaConexion, Sesion)
        Dim celdas = matriz.Celdas(desde, hasta)
        Dim r = Nuevo("Matriz de planificacion de menus")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim minutas = r.Seccion("Minutas", C("Fecha", FormatoColumna.Fecha), C("Servicio"), C("Regimen"), C("Estado"),
                                C("Comensales", FormatoColumna.Entero), C("Raciones teoricas", FormatoColumna.Entero),
                                C("Costo previsto (S/)", FormatoColumna.Dinero), C("Costo por comensal (S/)", FormatoColumna.Dinero),
                                C("Techo (S/)", FormatoColumna.Dinero), C("Desviacion (S/)", FormatoColumna.Dinero), C("Platos"))
        For Each celda In celdas
            minutas.Agregar(celda.Fecha, celda.ServicioNombre, celda.RegimenNombre, celda.Estado, celda.Comensales, celda.RacionesTeoricas,
                            celda.CostoPrevistoU6, celda.CostoPorComensalU6, celda.TechoU6, celda.DesviacionU6, celda.Platos)
        Next
        Dim dias = r.Seccion("Resumen por dia", C("Dia", FormatoColumna.Fecha), C("Minutas", FormatoColumna.Entero),
                             C("Comensales", FormatoColumna.Entero), C("Sin costo", FormatoColumna.Entero),
                             C("Costo del dia (S/)", FormatoColumna.Dinero), C("Costo por bandeja (S/)", FormatoColumna.Dinero),
                             C("Techo (S/)", FormatoColumna.Dinero), C("Desviacion (S/)", FormatoColumna.Dinero))
        For Each d In matriz.ResumenPorDia(celdas)
            dias.Agregar(d.Fecha, d.Minutas, d.Comensales, d.MinutasSinCosto, d.CostoDiaU6, d.CostoPorComensalU6, d.TechoU6, d.DesviacionU6)
        Next
        r.Notas.Add("Costo y techo salen del snapshot aprobado. Una minuta sin aprobar no tiene costo y queda vacia, nunca en cero.")
        Return r
    End Function

    ' ---------- Salida a producción (R11) ----------

    ''' <summary>Salidas a producción confirmadas del periodo: documento, bodega, servicio, producto, cantidad y precio promedio.</summary>
    Public Function SalidasAProduccion(desde As Date, hasta As Date) As Reporte
        Dim filas = EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar(
                "SELECT d.fecha, d.numero, a.nombre, COALESCE(s.nombre, ''), p.descripcion, um.codigo, l.cantidad_base_u6, l.valor_u6 " &
                "FROM documento_stock d JOIN documento_stock_detalle l ON l.documento_id = d.id " &
                "JOIN almacen a ON a.id = d.almacen_id JOIN variante_producto v ON v.id = l.variante_id " &
                "JOIN producto_base p ON p.id = v.producto_base_id JOIN unidad_medida um ON um.id = p.unidad_base_id " &
                "LEFT JOIN operacion_servicio os ON os.id = d.operacion_servicio_id LEFT JOIN servicio s ON s.id = os.servicio_id " &
                "WHERE a.operacion_id = @o AND d.tipo = 'salida_produccion' AND d.estado = 'confirmado' AND d.fecha BETWEEN @d AND @h " &
                "ORDER BY d.fecha, d.numero, p.descripcion",
                Function(rd) (Fecha:=rd.GetDateTime(0), Numero:=rd.GetString(1), Bodega:=rd.GetString(2), Servicio:=rd.GetString(3),
                              Producto:=rd.GetString(4), Unidad:=rd.GetString(5), CantidadU6:=rd.GetInt64(6), ValorU6:=rd.GetInt64(7)),
                "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date))
        Dim r = Nuevo("Salida a produccion")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Salidas", C("Fecha", FormatoColumna.Fecha), C("Documento"), C("Bodega"), C("Servicio"), C("Producto"), C("Unidad"),
                          C("Cantidad", FormatoColumna.Cantidad), C("Precio promedio (S/)", FormatoColumna.Dinero), C("Total (S/)", FormatoColumna.Dinero))
        Dim total As Long = 0
        For Each f In filas
            Dim precio As Long? = If(f.CantidadU6 > 0, EscalaU6.MultiplicarDividir(f.ValorU6, EscalaU6.Factor, f.CantidadU6), CType(Nothing, Long?))
            s.Agregar(f.Fecha, f.Numero, f.Bodega, f.Servicio, f.Producto, f.Unidad, f.CantidadU6, precio, f.ValorU6)
            total += f.ValorU6
        Next
        s.Totales = New Object() {"TOTAL", Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, total}
        Return r
    End Function

    ' ---------- Comparativo mensual (R20) ----------

    ''' <summary>Teórico frente a realizado de un servicio en el mes: raciones, venta, costo y food cost.</summary>
    Public Function ComparativoMensual(operacionServicioId As Long, anio As Integer, mes As Integer) As Reporte
        Dim comparativo = New ServicioComparativo(CadenaConexion, Sesion).ComparativoMes(operacionServicioId, anio, mes)
        Dim r = Nuevo(If(String.IsNullOrEmpty(comparativo.Titulo), "Comparativo mensual", comparativo.Titulo))
        r.Dato("Periodo", $"{mes:00}/{anio}")
        r.Dato("Minutas", comparativo.Minutas.ToString())
        Dim raciones = r.Seccion("Raciones", C("Concepto"), C("Teorico", FormatoColumna.Entero), C("Realizado", FormatoColumna.Entero),
                                 C("Diferencia", FormatoColumna.Entero))
        raciones.Agregar("Raciones (plan frente a vendidas)", comparativo.ComensalesPlan, comparativo.RacionesVendidas,
                         If(comparativo.RacionesVendidas.HasValue, comparativo.RacionesVendidas.Value - comparativo.ComensalesPlan, CType(Nothing, Long?)))
        Dim dinero = r.Seccion("Venta y costo (S/)", C("Concepto"), C("Teorico", FormatoColumna.Dinero), C("Realizado", FormatoColumna.Dinero),
                               C("Diferencia", FormatoColumna.Dinero))
        dinero.Agregar("Venta", comparativo.VentaTeoricaU6, comparativo.VentaRealU6, comparativo.DiferenciaVentaU6)
        dinero.Agregar("Costo de alimentos", comparativo.CostoTeoricoU6, comparativo.CostoRealU6, comparativo.DiferenciaCostoU6)
        Dim foodCost = r.Seccion("Food Cost (%)", C("Concepto"), C("Teorico", FormatoColumna.Cantidad), C("Realizado", FormatoColumna.Cantidad),
                                 C("Diferencia", FormatoColumna.Cantidad))
        foodCost.Agregar("Food Cost", comparativo.FoodCostTeoricoU6, comparativo.FoodCostRealU6,
                         If(comparativo.FoodCostTeoricoU6.HasValue AndAlso comparativo.FoodCostRealU6.HasValue, comparativo.FoodCostRealU6.Value - comparativo.FoodCostTeoricoU6.Value, CType(Nothing, Long?)))
        r.Notas.Add("Teorico = plan aprobado. Realizado = entregas, devoluciones, ventas y raciones cargadas. Lo que falte se deja vacio, nunca en cero.")
        Return r
    End Function

    ' ---------- Resultado mensual de alimentos (R27, parcial) ----------

    ''' <summary>Resultado del mes por servicio, con totales de costo, ingreso y food cost (lo que calcula el cierre mensual).</summary>
    Public Function ResultadoMensual(anio As Integer, mes As Integer) As Reporte
        Dim m = New ServicioCierres(CadenaConexion, Sesion).ReporteMensual(anio, mes)
        Dim r = Nuevo("Resultado mensual de alimentos")
        r.Dato("Periodo", $"{mes:00}/{anio}")
        r.Dato("Estado", m.Estado)
        Dim servicios = r.Seccion("Por servicio", C("Servicio"), C("Regimen"), C("Raciones servidas", FormatoColumna.Entero),
                                  C("Costo alimentos (S/)", FormatoColumna.Dinero), C("Costo por racion (S/)", FormatoColumna.Dinero),
                                  C("Ingreso (S/)", FormatoColumna.Dinero), C("Presupuesto (S/)", FormatoColumna.Dinero))
        For Each l In m.Servicios
            servicios.Agregar(l.Servicio, l.Regimen, l.RacionesServidas, l.CostoAlimentosU6, l.CostoPorRacionU6, l.IngresoU6, l.PresupuestoU6)
        Next
        Dim totales = r.Seccion("Totales (S/)", C("Concepto"), C("Monto", FormatoColumna.Dinero))
        totales.Agregar("Costo de alimentos", m.TotalCostoAlimentosU6)
        totales.Agregar("Ingreso", m.TotalIngresoU6)
        totales.Agregar("Bajas de almacen", m.BajasU6)
        totales.Agregar("Ajuste de inventario (+ sobrante, - faltante)", m.AjusteInventarioU6)
        Dim fc = r.Seccion("Food Cost (%)", C("Concepto"), C("Porcentaje", FormatoColumna.Cantidad))
        fc.Agregar("Food Cost del mes", m.FoodCostTotalU6)
        Return r
    End Function

    ' ---------- Boleta de ajuste (R25) y explicación de ajustes (R26) ----------

    ''' <summary>Boleta de ajuste de un inventario autorizado: línea por línea, con motivo, explicación, soporte, responsable y valor.</summary>
    Public Function BoletaAjustes(inventarioId As Long) As Reporte
        Dim filas = EnTransaccion(Permisos.InventarioVer,
            Function(u)
                If u.Escalar("SELECT 1 FROM inventario i JOIN almacen a ON a.id = i.almacen_id WHERE i.id = @i AND a.operacion_id = @o",
                             "i", inventarioId, "o", Sesion.Operacion.Id) Is Nothing Then
                    Throw New ReglaNegocioException("OPERACION_AJENA", "El inventario no pertenece a la operacion seleccionada.")
                End If
                Return u.Consultar(
                    "SELECT i.numero, i.fecha_corte, v.codigo, v.descripcion_comercial, ia.motivo_codigo, ia.explicacion, " &
                    "       COALESCE(ia.documento_soporte, ''), au.nombre, id.fisico_u6 - id.stock_sistema_u6, id.costo_corte_u6 " &
                    "FROM inventario_ajuste ia JOIN inventario_detalle id ON id.id = ia.inventario_detalle_id " &
                    "JOIN inventario i ON i.id = id.inventario_id JOIN variante_producto v ON v.id = id.variante_id " &
                    "JOIN usuario au ON au.id = ia.autorizador_id " &
                    "WHERE i.id = @i ORDER BY v.descripcion_comercial",
                    Function(rd) (Numero:=rd.GetString(0), Corte:=rd.GetDateTime(1), Codigo:=rd.GetString(2), Producto:=rd.GetString(3),
                                  Motivo:=rd.GetString(4), Explicacion:=rd.GetString(5), Soporte:=rd.GetString(6), Responsable:=rd.GetString(7),
                                  DifU6:=rd.GetInt64(8), CostoU6:=rd.GetInt64(9)),
                    "i", inventarioId)
            End Function)
        Dim r As New Reporte("Boleta de ajuste de inventario") With {.GeneradoPor = Sesion.NombreUsuario}
        r.Dato("Empresa", Sesion.EmpresaCodigo)
        r.Dato("Operacion", Sesion.Operacion?.ToString())
        If filas.Count > 0 Then
            r.Dato("Inventario", filas(0).Numero)
            r.Dato("Fecha de corte", filas(0).Corte.ToString("dd/MM/yyyy"))
        End If
        Dim s = r.Seccion("Ajustes", C("Codigo"), C("Producto"), C("Motivo"), C("Explicacion"), C("Documento de soporte"), C("Responsable"),
                          C("Cantidad", FormatoColumna.Cantidad), C("Costo unitario (S/)", FormatoColumna.Dinero), C("Valor (S/)", FormatoColumna.Dinero))
        Dim total As Long = 0
        For Each f In filas
            Dim valor = EscalaU6.Multiplicar(f.DifU6, f.CostoU6)
            s.Agregar(f.Codigo, f.Producto, MotivosAjuste.Texto(f.Motivo), f.Explicacion, f.Soporte, f.Responsable,
                      f.DifU6, f.CostoU6, valor)
            total += valor
        Next
        s.Totales = New Object() {"TOTAL", Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, total}
        r.Firmas.Add("Elaborado por")
        r.Firmas.Add("Autorizado por")
        r.Notas.Add("Cantidad positiva = sobrante; negativa = faltante. El valor usa el costo del corte.")
        Return r
    End Function

    ''' <summary>Ajustes autorizados del periodo, agrupados por motivo y responsable (explicación de ajustes para el cierre).</summary>
    Public Function ExplicacionAjustes(desde As Date, hasta As Date) As Reporte
        Dim filas = EnTransaccion(Permisos.InventarioVer,
            Function(u) u.Consultar(
                "SELECT ia.motivo_codigo, au.nombre, count(*), " &
                "       COALESCE(sum(round((id.fisico_u6 - id.stock_sistema_u6)::numeric * id.costo_corte_u6::numeric / 1000000)), 0)::bigint " &
                "FROM inventario_ajuste ia JOIN inventario_detalle id ON id.id = ia.inventario_detalle_id " &
                "JOIN documento_stock ds ON ds.id = ia.documento_stock_id JOIN almacen a ON a.id = ds.almacen_id " &
                "JOIN usuario au ON au.id = ia.autorizador_id " &
                "WHERE a.operacion_id = @o AND ds.fecha BETWEEN @d AND @h " &
                "GROUP BY ia.motivo_codigo, au.nombre ORDER BY ia.motivo_codigo, au.nombre",
                Function(rd) (Motivo:=rd.GetString(0), Responsable:=rd.GetString(1), Lineas:=rd.GetInt64(2), ValorU6:=rd.GetInt64(3)),
                "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date))
        Dim r = Nuevo("Explicacion de ajustes de inventario")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Por motivo y responsable", C("Motivo"), C("Responsable"), C("Lineas", FormatoColumna.Entero),
                          C("Valor neto (S/)", FormatoColumna.Dinero))
        For Each f In filas
            s.Agregar(MotivosAjuste.Texto(f.Motivo), f.Responsable, f.Lineas, f.ValorU6)
        Next
        r.Notas.Add("Valor neto: positivo = sobrante, negativo = faltante. Agrupado por motivo normalizado y por quien autorizó.")
        Return r
    End Function

    ' ---------- Costo resumido teórico (R02), previsión de consumo (R03), frecuencia (R04), requisición por servicio (R07), raciones (R21) ----------

    ''' <summary>Costo teórico por día y servicio, con su techo, en el periodo. Sin costo aprobado = vacío.</summary>
    Public Function CostoResumidoTeorico(desde As Date, hasta As Date) As Reporte
        Dim celdas = New ServicioMatrizMenu(CadenaConexion, Sesion).Celdas(desde, hasta)
        Dim r = Nuevo("Costo resumido teorico")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Por dia y servicio", C("Fecha", FormatoColumna.Fecha), C("Servicio"), C("Regimen"), C("Comensales", FormatoColumna.Entero),
                          C("Costo teorico (S/)", FormatoColumna.Dinero), C("Techo (S/)", FormatoColumna.Dinero), C("Desviacion (S/)", FormatoColumna.Dinero))
        Dim total As Long = 0
        For Each celda In celdas.OrderBy(Function(x) x.Fecha).ThenBy(Function(x) x.ServicioNombre)
            s.Agregar(celda.Fecha, celda.ServicioNombre, celda.RegimenNombre, celda.Comensales, celda.CostoPrevistoU6, celda.TechoU6, celda.DesviacionU6)
            If celda.CostoPrevistoU6.HasValue Then total += celda.CostoPrevistoU6.Value
        Next
        s.Totales = New Object() {"TOTAL", Nothing, Nothing, Nothing, total, Nothing, Nothing}
        r.Notas.Add("Costo teorico = minutas aprobadas. Una minuta sin aprobar no tiene costo y queda vacia.")
        Return r
    End Function

    ''' <summary>Previsión de consumo: necesidades consolidadas de las minutas del periodo (producto, cantidad y unidad).</summary>
    Public Function PrevisionConsumo(desde As Date, hasta As Date) As Reporte
        Dim servicio As New ServicioMinutas(CadenaConexion, Sesion)
        Dim ids = servicio.ListarMinutas(desde, hasta).Select(Function(m) m.Id).ToList()
        Dim necesidades = If(ids.Count = 0, New List(Of NecesidadDto)(), servicio.Necesidades(ids))
        Dim r = Nuevo("Prevision de consumo")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Necesidades", C("Codigo"), C("Producto"), C("Cantidad", FormatoColumna.Cantidad), C("Unidad"), C("Minutas o platos que la originan", FormatoColumna.Entero))
        For Each n In necesidades.OrderBy(Function(x) x.ProductoDescripcion)
            s.Agregar(n.ProductoCodigo, n.ProductoDescripcion, n.CantidadU6, n.Unidad, n.Origenes)
        Next
        Return r
    End Function

    ''' <summary>
    ''' Minuta teórica frente a la real (plan del SGP, vistas V034): por día y servicio, y por estructura y receta.
    ''' Los importes y las raciones se pasan a U6 para el formato del reporte.
    ''' </summary>
    Public Function MinutaTeoricoReal(desde As Date, hasta As Date) As Reporte
        Dim aU6 = Function(x As Decimal) CLng(Math.Round(x * 1000000D, 0, MidpointRounding.AwayFromZero))
        Dim dias = EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT fecha, servicio, comensales_teorico, comensales_real, costo_bandeja_teorico, costo_bandeja_real, costo_total_teorico, costo_total_real " &
                "FROM v_minuta_teorico_real_dia WHERE operacion_id = @o AND fecha BETWEEN @d AND @h ORDER BY fecha, servicio",
                Function(rd) (Fecha:=rd.GetDateTime(0), Servicio:=rd.GetString(1),
                              ComTeo:=If(rd.IsDBNull(2), 0D, Convert.ToDecimal(rd.GetValue(2))), ComReal:=If(rd.IsDBNull(3), 0D, Convert.ToDecimal(rd.GetValue(3))),
                              BandejaTeo:=If(rd.IsDBNull(4), 0D, rd.GetDecimal(4)), BandejaReal:=If(rd.IsDBNull(5), 0D, rd.GetDecimal(5)),
                              TotalTeo:=If(rd.IsDBNull(6), 0D, rd.GetDecimal(6)), TotalReal:=If(rd.IsDBNull(7), 0D, rd.GetDecimal(7))),
                "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date))
        Dim platos = EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT fecha, servicio, estructura, receta_codigo_sgp, raciones_teorico, raciones_real, costo_teorico, costo_real, diferencia_costo, en_teorico, en_real " &
                "FROM v_minuta_teorico_real_plato WHERE operacion_id = @o AND fecha BETWEEN @d AND @h ORDER BY fecha, servicio, estructura, receta_codigo_sgp",
                Function(rd) (Fecha:=rd.GetDateTime(0), Servicio:=rd.GetString(1), Estructura:=rd.GetString(2), Receta:=rd.GetString(3),
                              RacTeo:=If(rd.IsDBNull(4), 0D, rd.GetDecimal(4)), RacReal:=If(rd.IsDBNull(5), 0D, rd.GetDecimal(5)),
                              CostoTeo:=If(rd.IsDBNull(6), 0D, rd.GetDecimal(6)), CostoReal:=If(rd.IsDBNull(7), 0D, rd.GetDecimal(7)),
                              Dif:=If(rd.IsDBNull(8), 0D, rd.GetDecimal(8)), EnTeo:=rd.GetBoolean(9), EnReal:=rd.GetBoolean(10)),
                "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date))
        Dim r = Nuevo("Minuta teorico vs real (plan SGP)")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim sd = r.Seccion("Por dia y servicio", C("Fecha", FormatoColumna.Fecha), C("Servicio"),
                           C("Comensales teorico", FormatoColumna.Entero), C("Comensales real", FormatoColumna.Entero),
                           C("Costo por bandeja teorico", FormatoColumna.Dinero), C("Costo por bandeja real", FormatoColumna.Dinero),
                           C("Costo total teorico", FormatoColumna.Dinero), C("Costo total real", FormatoColumna.Dinero))
        For Each f In dias
            sd.Agregar(f.Fecha, f.Servicio, CLng(Math.Round(f.ComTeo, 0)), CLng(Math.Round(f.ComReal, 0)), aU6(f.BandejaTeo), aU6(f.BandejaReal), aU6(f.TotalTeo), aU6(f.TotalReal))
        Next
        Dim sp = r.Seccion("Por estructura y receta", C("Fecha", FormatoColumna.Fecha), C("Servicio"), C("Estructura"), C("Receta SGP"),
                           C("Raciones teorico", FormatoColumna.Cantidad), C("Raciones real", FormatoColumna.Cantidad),
                           C("Costo teorico", FormatoColumna.Dinero), C("Costo real", FormatoColumna.Dinero),
                           C("Diferencia costo", FormatoColumna.Dinero), C("Situacion"))
        For Each f In platos
            Dim situacion = If(f.EnTeo AndAlso f.EnReal, "planificado", If(f.EnReal, "no planificado", "sin salida"))
            sp.Agregar(f.Fecha, f.Servicio, f.Estructura, f.Receta, aU6(f.RacTeo), aU6(f.RacReal), aU6(f.CostoTeo), aU6(f.CostoReal), aU6(f.Dif), situacion)
        Next
        r.Notas.Add("Teorico = plan del SGP (TEORICO). Real = plan real del SGP (REAL). Costo por bandeja = costo por comensal del dia. " &
                    "'No planificado' = receta que salio sin estar en lo teorico; 'sin salida' = planificado y no registrado en lo real.")
        Return r
    End Function

    ''' <summary>Frecuencia de recetas en el periodo: en cuántos días se usó cada receta, cuántos platos y cuántas raciones.</summary>
    Public Function FrecuenciaRecetas(desde As Date, hasta As Date) As Reporte
        Dim filas = EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT r.codigo, r.nombre, count(DISTINCT m.fecha), count(*), COALESCE(sum(d.raciones), 0)::bigint " &
                "FROM minuta_detalle d JOIN minuta m ON m.id = d.minuta_id " &
                "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
                "JOIN receta_version rv ON rv.id = d.receta_version_id JOIN receta r ON r.id = rv.receta_id " &
                "WHERE os.operacion_id = @o AND m.fecha BETWEEN @d AND @h " &
                "GROUP BY r.codigo, r.nombre ORDER BY count(DISTINCT m.fecha) DESC, r.codigo",
                Function(rd) (Codigo:=rd.GetString(0), Nombre:=rd.GetString(1), Dias:=rd.GetInt64(2), Platos:=rd.GetInt64(3), Raciones:=rd.GetInt64(4)),
                "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date))
        Dim r = Nuevo("Frecuencia de recetas")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Recetas", C("Codigo"), C("Receta"), C("Dias de uso", FormatoColumna.Entero), C("Platos", FormatoColumna.Entero),
                          C("Raciones", FormatoColumna.Entero))
        For Each f In filas
            s.Agregar(f.Codigo, f.Nombre, f.Dias, f.Platos, f.Raciones)
        Next
        r.Notas.Add("Ordenado por dias de uso. Una receta repetida en muchos dias se revisa contra la politica de rotacion del menu.")
        Return r
    End Function

    ''' <summary>Requisición por servicio: lo atendido del periodo, por servicio y producto (cantidad solicitada).</summary>
    Public Function RequisicionPorServicio(desde As Date, hasta As Date) As Reporte
        Dim filas = EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT s.nombre, p.descripcion, um.codigo, sum(d.cantidad_solicitada_u6)::bigint " &
                "FROM requerimiento q JOIN requerimiento_detalle d ON d.requerimiento_id = q.id " &
                "JOIN operacion_servicio os ON os.id = q.operacion_servicio_id JOIN servicio s ON s.id = os.servicio_id " &
                "JOIN producto_base p ON p.id = d.producto_base_id JOIN unidad_medida um ON um.id = p.unidad_base_id " &
                "WHERE os.operacion_id = @o AND q.estado = 'atendido' AND q.fecha BETWEEN @d AND @h " &
                "GROUP BY s.nombre, p.descripcion, um.codigo ORDER BY s.nombre, p.descripcion",
                Function(rd) (Servicio:=rd.GetString(0), Producto:=rd.GetString(1), Unidad:=rd.GetString(2), Cantidad:=rd.GetInt64(3)),
                "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date))
        Dim r = Nuevo("Requisicion por servicio")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Requisicion atendida", C("Servicio"), C("Producto"), C("Unidad"), C("Cantidad", FormatoColumna.Cantidad))
        For Each f In filas
            s.Agregar(f.Servicio, f.Producto, f.Unidad, f.Cantidad)
        Next
        r.Notas.Add("Solo requerimientos ya entregados por el almacen. Los borradores y adicionales sin aprobar no cuentan.")
        Return r
    End Function

    ''' <summary>Comparativo de raciones por día y servicio: teóricas, operativas y servidas (reales).</summary>
    Public Function ComparativoRaciones(desde As Date, hasta As Date) As Reporte
        Dim celdas = New ServicioMatrizMenu(CadenaConexion, Sesion).Celdas(desde, hasta)
        Dim servidas = EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT p.minuta_id, p.raciones_servidas FROM produccion p JOIN minuta m ON m.id = p.minuta_id " &
                "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id WHERE os.operacion_id = @o AND m.fecha BETWEEN @d AND @h",
                Function(rd) (Minuta:=rd.GetInt64(0), Servidas:=rd.GetInt64(1)),
                "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date).ToDictionary(Function(x) x.Minuta, Function(x) x.Servidas))
        Dim r = Nuevo("Comparativo de raciones")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Por dia y servicio", C("Fecha", FormatoColumna.Fecha), C("Servicio"), C("Teoricas", FormatoColumna.Entero),
                          C("Operativas", FormatoColumna.Entero), C("Servidas (reales)", FormatoColumna.Entero), C("Diferencia (servidas - operativas)", FormatoColumna.Entero))
        For Each celda In celdas.OrderBy(Function(x) x.Fecha).ThenBy(Function(x) x.ServicioNombre)
            Dim real As Long? = If(servidas.ContainsKey(celda.MinutaId), servidas(celda.MinutaId), CType(Nothing, Long?))
            s.Agregar(celda.Fecha, celda.ServicioNombre, celda.RacionesTeoricas, celda.RacionesOperativas, real,
                      If(real.HasValue, real.Value - celda.RacionesOperativas, CType(Nothing, Long?)))
        Next
        r.Notas.Add("Servidas = raciones registradas en Produccion. Lo que no se ha registrado queda vacio, nunca en cero.")
        Return r
    End Function

    ' ---------- Pendientes del Sprint 3: R01, R06, R08, R09, R10, R12, R13, R15 ----------

    ''' <summary>Consumo teórico por plato e ingrediente de las minutas del periodo (cantidad de receta × raciones ÷ rendimiento) con su costo si está costeado.</summary>
    Private Function ConsumosTeoricos(desde As Date, hasta As Date) As List(Of (Estructura As String, Codigo As String, Producto As String, Unidad As String, ConsumoU6 As Long, CostoU6 As Long?, SinCosto As Boolean))
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                Dim filas = u.Consultar(
                    "SELECT es.nombre, p.codigo, p.descripcion, um.codigo, " &
                    "       ROUND(ri.cantidad_base_bruta_u6::numeric * d.raciones * 1000000 / rv.rendimiento_raciones_u6)::bigint, " &
                    "       ci.costo_unitario_base_u6 " &
                    "FROM minuta_detalle d JOIN minuta m ON m.id = d.minuta_id " &
                    "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
                    "JOIN estructura_servicio es ON es.id = d.estructura_id " &
                    "JOIN receta_version rv ON rv.id = d.receta_version_id " &
                    "JOIN receta_ingrediente ri ON ri.receta_version_id = rv.id " &
                    "JOIN producto_base p ON p.id = ri.producto_base_id JOIN unidad_medida um ON um.id = p.unidad_base_id " &
                    "LEFT JOIN costeo_ingrediente ci ON ci.minuta_detalle_id = d.id AND ci.ingrediente_id = ri.id " &
                    "WHERE os.operacion_id = @o AND m.fecha BETWEEN @d AND @h AND m.estado <> 'borrador'",
                    Function(rd) (Estructura:=rd.GetString(0), Codigo:=rd.GetString(1), Producto:=rd.GetString(2), Unidad:=rd.GetString(3),
                                  ConsumoU6:=rd.GetInt64(4), CostoU6:=If(rd.IsDBNull(5), CType(Nothing, Long?), rd.GetInt64(5))),
                    "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date)
                Return filas.Select(Function(x) (x.Estructura, x.Codigo, x.Producto, x.Unidad, x.ConsumoU6,
                                                 If(x.CostoU6.HasValue, EscalaU6.Multiplicar(x.ConsumoU6, x.CostoU6.Value), CType(Nothing, Long?)),
                                                 Not x.CostoU6.HasValue)).ToList()
            End Function)
    End Function

    ''' <summary>Costo detallado teórico por producto: consumo del periodo, costo unitario promedio y costo total (vacío si falta costo).</summary>
    Public Function CostoDetalladoTeorico(desde As Date, hasta As Date) As Reporte
        Dim lineas = ConsumosTeoricos(desde, hasta)
        Dim r = Nuevo("Costo detallado teorico")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Por producto", C("Codigo"), C("Producto"), C("Unidad"), C("Consumo", FormatoColumna.Cantidad),
                          C("Costo unitario promedio (S/)", FormatoColumna.Dinero), C("Costo total (S/)", FormatoColumna.Dinero), C("Lineas sin costo", FormatoColumna.Entero))
        For Each g In lineas.GroupBy(Function(x) New With {x.Codigo, x.Producto, x.Unidad}).OrderBy(Function(x) x.Key.Producto)
            Dim consumo = g.Sum(Function(x) x.ConsumoU6)
            Dim sinCosto = g.Count(Function(x) x.SinCosto)
            Dim costo As Long? = If(sinCosto = 0, g.Sum(Function(x) x.CostoU6.GetValueOrDefault()), CType(Nothing, Long?))
            Dim unitario As Long? = If(costo.HasValue AndAlso consumo > 0, EscalaU6.MultiplicarDividir(costo.Value, EscalaU6.Factor, consumo), CType(Nothing, Long?))
            s.Agregar(g.Key.Codigo, g.Key.Producto, g.Key.Unidad, consumo, unitario, costo, sinCosto)
        Next
        r.Notas.Add("Costo total vacio cuando alguna linea del producto no tiene precio: no se completa con cero.")
        Return r
    End Function

    ''' <summary>Requisición detallada por estructura: cuánto de cada producto necesita cada estructura del servicio.</summary>
    Public Function RequisicionPorEstructura(desde As Date, hasta As Date) As Reporte
        Dim lineas = ConsumosTeoricos(desde, hasta)
        Dim r = Nuevo("Requisicion detallada por estructura")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Por estructura y producto", C("Estructura"), C("Codigo"), C("Producto"), C("Unidad"), C("Cantidad", FormatoColumna.Cantidad))
        For Each g In lineas.GroupBy(Function(x) New With {x.Estructura, x.Codigo, x.Producto, x.Unidad}) _
                            .OrderBy(Function(x) x.Key.Estructura).ThenBy(Function(x) x.Key.Producto)
            s.Agregar(g.Key.Estructura, g.Key.Codigo, g.Key.Producto, g.Key.Unidad, g.Sum(Function(x) x.ConsumoU6))
        Next
        Return r
    End Function

    ''' <summary>Mapa de solicitud de compras: la fórmula de la última previsión del almacén, línea por línea.</summary>
    Public Function MapaSolicitudCompras(almacenId As Long) As Reporte
        Dim compras = New ServicioCompras(CadenaConexion, Sesion)
        Dim ultima = compras.ListarPrevisiones(almacenId).FirstOrDefault()
        Dim r = Nuevo("Mapa de solicitud de compras")
        If ultima Is Nothing Then
            r.Notas.Add("No hay previsiones calculadas para este almacen.")
            Return r
        End If
        r.Dato("Periodo", $"{ultima.FechaDesde:dd/MM/yyyy} a {ultima.FechaHasta:dd/MM/yyyy}")
        r.Dato("Fecha de corte", ultima.FechaCorte.ToString("dd/MM/yyyy"))
        r.Dato("Estado de la previsión", ultima.Estado)
        Dim s = r.Seccion("Formula por producto", C("Codigo"), C("Producto"), C("Unidad"), C("Demanda", FormatoColumna.Cantidad),
                          C("Consumo hasta llegada", FormatoColumna.Cantidad), C("Stock", FormatoColumna.Cantidad), C("Reserva", FormatoColumna.Cantidad),
                          C("OC por recibir", FormatoColumna.Cantidad), C("Necesidad neta", FormatoColumna.Cantidad), C("Fecha de quiebre", FormatoColumna.Fecha))
        For Each l In compras.Desglose(ultima.Id)
            s.Agregar(l.ProductoCodigo, l.ProductoDescripcion, l.Unidad, l.DemandaU6, l.ConsumoPuenteU6, l.StockU6, l.ReservaU6,
                      l.PendienteRecibirU6, l.NecesidadNetaU6, l.FechaQuiebre)
        Next
        r.Notas.Add("Necesidad neta segun la previsión guardada: demanda y consumo hasta llegada, menos stock, reserva y OC por recibir.")
        Return r
    End Function

    ''' <summary>Resumen de compras: recepciones confirmadas del periodo por proveedor y producto, con total.</summary>
    Public Function ResumenCompras(desde As Date, hasta As Date) As Reporte
        Dim filas = EnTransaccion(Permisos.ComprasVer,
            Function(u) u.Consultar(
                "SELECT r.fecha_recepcion, r.tipo_documento, r.numero_documento, pr.nombre, p.descripcion, d.unidad_recibida, " &
                "       d.cantidad_recibida_u6, d.precio_unidad_recibida_u6 " &
                "FROM recepcion r JOIN recepcion_detalle d ON d.recepcion_id = r.id " &
                "JOIN almacen a ON a.id = r.almacen_id JOIN proveedor pr ON pr.id = r.proveedor_id " &
                "JOIN variante_producto v ON v.id = d.variante_id JOIN producto_base p ON p.id = v.producto_base_id " &
                "WHERE a.operacion_id = @o AND r.estado = 'confirmada' AND r.fecha_recepcion BETWEEN @d AND @h " &
                "ORDER BY r.fecha_recepcion, r.numero_documento, p.descripcion",
                Function(rd) (Fecha:=rd.GetDateTime(0), Tipo:=rd.GetString(1), Documento:=rd.GetString(2), Proveedor:=rd.GetString(3),
                              Producto:=rd.GetString(4), Unidad:=rd.GetString(5), CantidadU6:=rd.GetInt64(6), PrecioU6:=rd.GetInt64(7)),
                "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date))
        Dim r = Nuevo("Resumen de compras")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Recepciones", C("Fecha", FormatoColumna.Fecha), C("Documento"), C("Numero"), C("Proveedor"), C("Producto"), C("Unidad"),
                          C("Cantidad", FormatoColumna.Cantidad), C("Precio unitario (S/)", FormatoColumna.Dinero), C("Total (S/)", FormatoColumna.Dinero))
        Dim total As Long = 0
        For Each f In filas
            Dim importe = EscalaU6.Multiplicar(f.CantidadU6, f.PrecioU6)
            s.Agregar(f.Fecha, f.Tipo, f.Documento, f.Proveedor, f.Producto, f.Unidad, f.CantidadU6, f.PrecioU6, importe)
            total += importe
        Next
        s.Totales = New Object() {"TOTAL", Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, total}
        r.Notas.Add("Precios sin IGV. Solo recepciones confirmadas.")
        Return r
    End Function

    ''' <summary>Resumen de traspasos del periodo: salidas y entradas entre almacenes de la operación.</summary>
    Public Function ResumenTraspasos(desde As Date, hasta As Date) As Reporte
        Dim filas = EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar(
                "SELECT d.fecha, d.tipo, d.numero, a.nombre, p.descripcion, um.codigo, l.cantidad_base_u6, l.valor_u6 " &
                "FROM documento_stock d JOIN documento_stock_detalle l ON l.documento_id = d.id " &
                "JOIN almacen a ON a.id = d.almacen_id JOIN variante_producto v ON v.id = l.variante_id " &
                "JOIN producto_base p ON p.id = v.producto_base_id JOIN unidad_medida um ON um.id = p.unidad_base_id " &
                "WHERE a.operacion_id = @o AND d.estado = 'confirmado' AND d.tipo IN ('traspaso_salida', 'traspaso_entrada') " &
                "  AND d.fecha BETWEEN @d AND @h ORDER BY d.fecha, d.numero, p.descripcion",
                Function(rd) (Fecha:=rd.GetDateTime(0), Tipo:=rd.GetString(1), Numero:=rd.GetString(2), Almacen:=rd.GetString(3),
                              Producto:=rd.GetString(4), Unidad:=rd.GetString(5), CantidadU6:=rd.GetInt64(6), ValorU6:=rd.GetInt64(7)),
                "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date))
        Dim r = Nuevo("Resumen de traspasos")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Traspasos", C("Fecha", FormatoColumna.Fecha), C("Tipo"), C("Documento"), C("Almacen"), C("Producto"), C("Unidad"),
                          C("Cantidad", FormatoColumna.Cantidad), C("Valor (S/)", FormatoColumna.Dinero))
        For Each f In filas
            s.Agregar(f.Fecha, If(f.Tipo = "traspaso_salida", "Salida", "Entrada"), f.Numero, f.Almacen, f.Producto, f.Unidad, f.CantidadU6, f.ValorU6)
        Next
        Return r
    End Function

    ''' <summary>Control de raciones por día y servicio: teóricas, operativas, servidas (producción) y vendidas (ventas registradas).</summary>
    Public Function ControlRaciones(desde As Date, hasta As Date) As Reporte
        Dim celdas = New ServicioMatrizMenu(CadenaConexion, Sesion).Celdas(desde, hasta)
        Dim datos = EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT m.id, COALESCE(p.raciones_servidas, -1), COALESCE(v.raciones_vendidas, -1), COALESCE(v.importe_u6, -1) " &
                "FROM minuta m JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
                "LEFT JOIN produccion p ON p.minuta_id = m.id LEFT JOIN venta_servicio v ON v.minuta_id = m.id " &
                "WHERE os.operacion_id = @o AND m.fecha BETWEEN @d AND @h",
                Function(rd) (Minuta:=rd.GetInt64(0), Servidas:=rd.GetInt64(1), Vendidas:=rd.GetInt64(2), Importe:=rd.GetInt64(3)),
                "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date).ToDictionary(Function(x) x.Minuta))
        Dim r = Nuevo("Control de raciones")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Por dia y servicio", C("Fecha", FormatoColumna.Fecha), C("Servicio"), C("Teoricas", FormatoColumna.Entero),
                          C("Operativas", FormatoColumna.Entero), C("Servidas", FormatoColumna.Entero), C("Vendidas", FormatoColumna.Entero),
                          C("Venta registrada (S/)", FormatoColumna.Dinero))
        For Each celda In celdas.OrderBy(Function(x) x.Fecha).ThenBy(Function(x) x.ServicioNombre)
            Dim d = datos(celda.MinutaId)
            s.Agregar(celda.Fecha, celda.ServicioNombre, celda.RacionesTeoricas, celda.RacionesOperativas,
                      If(d.Servidas >= 0, d.Servidas, CType(Nothing, Long?)), If(d.Vendidas >= 0, d.Vendidas, CType(Nothing, Long?)),
                      If(d.Importe >= 0, d.Importe, CType(Nothing, Long?)))
        Next
        r.Notas.Add("Vacio = no registrado (no es cero). Servidas viene de la producción; vendidas y venta, del registro de venta del servicio.")
        Return r
    End Function

    ''' <summary>Venta de servicio por fuente de registro (contado y demás): importe y raciones vendidas por servicio.</summary>
    Public Function VentaServicioContado(desde As Date, hasta As Date) As Reporte
        Dim filas = EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT v.fuente, s.nombre, sum(v.raciones_vendidas)::bigint, sum(v.importe_u6)::bigint, count(*) " &
                "FROM venta_servicio v JOIN minuta m ON m.id = v.minuta_id " &
                "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id JOIN servicio s ON s.id = os.servicio_id " &
                "WHERE os.operacion_id = @o AND m.fecha BETWEEN @d AND @h GROUP BY v.fuente, s.nombre ORDER BY v.fuente, s.nombre",
                Function(rd) (Fuente:=rd.GetString(0), Servicio:=rd.GetString(1), Raciones:=rd.GetInt64(2), ImporteU6:=rd.GetInt64(3), Dias:=rd.GetInt64(4)),
                "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date))
        Dim r = Nuevo("Venta de servicio contado")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Por fuente y servicio", C("Fuente"), C("Servicio"), C("Minutas", FormatoColumna.Entero),
                          C("Raciones vendidas", FormatoColumna.Entero), C("Importe (S/)", FormatoColumna.Dinero))
        For Each f In filas
            s.Agregar(f.Fuente, f.Servicio, f.Dias, f.Raciones, f.ImporteU6)
        Next
        Return r
    End Function

    ''' <summary>Costo realizado por producto de cada minuta del periodo: previsto, entregado, devuelto, neto y costo real.</summary>
    Public Function CostoRealizado(desde As Date, hasta As Date) As Reporte
        Dim servicio As New ServicioMinutas(CadenaConexion, Sesion)
        Dim produccion As New ServicioProduccion(CadenaConexion, Sesion)
        Dim r = Nuevo("Costo realizado por periodo")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Consumo real por producto", C("Fecha", FormatoColumna.Fecha), C("Servicio"), C("Producto"), C("Unidad"),
                          C("Previsto", FormatoColumna.Cantidad), C("Entregado", FormatoColumna.Cantidad), C("Devuelto", FormatoColumna.Cantidad),
                          C("Neto", FormatoColumna.Cantidad), C("Costo real (S/)", FormatoColumna.Dinero))
        For Each m In servicio.ListarMinutas(desde, hasta).Where(Function(x) x.Estado <> "borrador")
            For Each l In produccion.Reporte(m.Id).Consumo
                s.Agregar(m.Fecha, m.ServicioNombre, l.ProductoDescripcion, l.Unidad, l.PrevistoU6, l.EntregadoU6, l.DevueltoU6, l.NetoU6, l.CostoRealU6)
            Next
        Next
        r.Notas.Add("Costo real = entregas menos devoluciones, a costo historico de la entrega.")
        Return r
    End Function

    ' ---------- Sprint 6: comparación de resultados (presupuesto, teórico y real) ----------

    ''' <summary>
    ''' Resultado del mes comparado por servicio: presupuesto, costo teórico frente a costo real, ingreso y Food Cost frente
    ''' al objetivo. La alerta de Food Cost es texto ("Sobre objetivo" o "En objetivo"), no solo color.
    ''' </summary>
    Public Function ComparativoResultado(anio As Integer, mes As Integer) As Reporte
        Dim mensual = New ServicioCierres(CadenaConexion, Sesion).ReporteMensual(anio, mes)
        Dim comparativos = New ServicioComparativo(CadenaConexion, Sesion)
        Dim r = Nuevo("Resultado comparado: presupuesto, teorico y real")
        r.Dato("Periodo", $"{mes:00}/{anio}")
        r.Dato("Estado", mensual.Estado)
        Dim dinero = r.Seccion("Presupuesto, teorico y real (S/)", C("Servicio"), C("Presupuesto", FormatoColumna.Dinero),
                               C("Ingreso real", FormatoColumna.Dinero), C("Costo teorico", FormatoColumna.Dinero),
                               C("Costo real", FormatoColumna.Dinero), C("Costo real - teorico", FormatoColumna.Dinero))
        Dim foodCost = r.Seccion("Food Cost frente al objetivo (%)", C("Servicio"), C("Food Cost real", FormatoColumna.Cantidad),
                                 C("Objetivo", FormatoColumna.Cantidad), C("Alerta"))
        For Each linea In mensual.Servicios
            Dim cmp = comparativos.ComparativoMes(linea.OperacionServicioId, anio, mes)
            Dim nombre = $"{linea.Servicio} - {linea.Regimen}"
            dinero.Agregar(nombre, linea.PresupuestoU6, linea.IngresoU6, cmp.CostoTeoricoU6,
                           If(cmp.CostoRealU6 > 0, cmp.CostoRealU6, CType(Nothing, Long?)), cmp.DiferenciaCostoU6)
            Dim alerta = If(Not linea.FoodCostU6.HasValue OrElse Not linea.ObjetivoU6.HasValue, "Sin dato",
                            If(linea.FoodCostU6.Value > linea.ObjetivoU6.Value, "Sobre objetivo", "En objetivo"))
            foodCost.Agregar(nombre, linea.FoodCostU6, linea.ObjetivoU6, alerta)
        Next
        r.Notas.Add("Costo teorico = plan aprobado. Costo real = entregas menos devoluciones. Lo que no existe queda vacio.")
        Return r
    End Function

    ' ---------- Sprint 7: auditoría de solo lectura, exportable ----------

    ''' <summary>Auditoría del periodo (antes y después de cada cambio). Solo lectura: se exporta, no se edita.</summary>
    Public Function Auditoria(desde As Date, hasta As Date, tabla As String, login As String) As Reporte
        Dim filas = New ServicioAdministracion(CadenaConexion, Sesion).ConsultarAuditoria(desde, hasta, tabla, login, 5000)
        Dim r = Nuevo("Auditoria de cambios")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        r.Dato("Tabla", If(String.IsNullOrWhiteSpace(tabla), "todas", tabla))
        r.Dato("Usuario", If(String.IsNullOrWhiteSpace(login), "todos", login))
        Dim s = r.Seccion("Cambios", C("Fecha y hora", FormatoColumna.Fecha), C("Usuario"), C("Tabla"), C("Registro"), C("Accion"),
                          C("Antes"), C("Despues"))
        For Each f In filas
            s.Agregar(f.Fecha.Date, f.Usuario, f.Tabla, f.RegistroId, f.Accion, f.Antes, f.Despues)
        Next
        r.Notas.Add("Auditoria de solo lectura. Las claves y hashes no se muestran.")
        Return r
    End Function

    ' ---------- Sprint 8: monitor de tránsitos (versión 1: recepciones marcadas como traspaso) ----------

    ''' <summary>
    ''' Traspasos entre almacenes enviados en el periodo: "En transito" hasta que el destino los recibe; después "Recibido".
    ''' El tránsito entre operaciones por la central todavía no existe (fase 3b de la arquitectura).
    ''' </summary>
    Public Function Transitos(desde As Date, hasta As Date) As Reporte
        Dim filas = EnTransaccion(Permisos.ComprasVer,
            Function(u) u.Consultar(
                "SELECT t.fecha_envio, t.numero, ao.nombre, ad.nombre, t.estado, t.fecha_recepcion, t.valor_u6 " &
                "FROM traspaso_transito t JOIN almacen ao ON ao.id = t.almacen_origen_id JOIN almacen ad ON ad.id = t.almacen_destino_id " &
                "WHERE t.operacion_id = @o AND t.fecha_envio BETWEEN @d AND @h ORDER BY t.fecha_envio, t.numero",
                Function(rd) (Envio:=rd.GetDateTime(0), Numero:=rd.GetString(1), Origen:=rd.GetString(2), Destino:=rd.GetString(3),
                              Estado:=rd.GetString(4), Recibido:=If(rd.IsDBNull(5), CType(Nothing, Date?), rd.GetDateTime(5)), ValorU6:=rd.GetInt64(6)),
                "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date))
        Dim r = Nuevo("Monitor de transitos")
        r.Dato("Desde", desde.ToString("dd/MM/yyyy"))
        r.Dato("Hasta", hasta.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("Traspasos", C("Envio", FormatoColumna.Fecha), C("Documento"), C("Origen"), C("Destino"), C("Estado"),
                          C("Recibido", FormatoColumna.Fecha), C("Valor (S/)", FormatoColumna.Dinero))
        For Each f In filas
            s.Agregar(f.Envio, f.Numero, f.Origen, f.Destino, If(f.Estado = "enviado", "En transito", "Recibido"), f.Recibido, f.ValorU6)
        Next
        r.Notas.Add("En transito = enviado y aun no recibido por el destino: el stock ya salio del origen y no esta en ningun almacen. " &
                    "El transito entre operaciones por la central llega en la fase 3b.")
        Return r
    End Function

End Class
