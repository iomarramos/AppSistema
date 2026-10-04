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
        r.Notas.Add("Bulto = solicitado / contenido de la presentacion activa, en decimales (como la requisicion del SGP).")
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
        Dim cab = EnTransaccion(Permisos.InventarioContar,
            Function(u) u.Consultar(
                "SELECT i.numero, i.tipo, i.fecha_corte, i.estado, a.codigo || ' - ' || a.nombre FROM inventario i JOIN almacen a ON a.id = i.almacen_id " &
                "WHERE i.id = @i AND a.operacion_id = @o",
                Function(rd) (Numero:=rd.GetString(0), Tipo:=rd.GetString(1), Corte:=rd.GetDateTime(2), Estado:=rd.GetString(3), Almacen:=rd.GetString(4)),
                "i", inventarioId, "o", op).ToList())
        If cab.Count = 0 Then Throw New ReglaNegocioException("OPERACION_AJENA", "El inventario no pertenece a la operacion seleccionada.")
        Dim i = cab(0)
        Dim servicio As New ServicioInventarios(CadenaConexion, Sesion)
        Dim r = Nuevo(If(hojaDeConteo, $"Hoja de conteo {i.Numero}", $"Inventario {i.Numero}"))
        r.Dato("Numero", i.Numero)
        r.Dato("Almacen", i.Almacen)
        r.Dato("Tipo", i.Tipo)
        r.Dato("Fecha de corte", i.Corte.ToString("dd/MM/yyyy"))
        r.Dato("Estado", i.Estado)
        If hojaDeConteo Then
            Dim s = r.Seccion("", C("Codigo"), C("Descripcion"), C("Presentacion"), C("Contenido", FormatoColumna.Cantidad), C("Unidad"), C("Envases"), C("Parcial"))
            For Each l In servicio.Hoja(inventarioId, ciego:=True)
                s.Agregar(l.VarianteCodigo, l.Descripcion, l.Presentacion, l.ContenidoEnvaseU6, l.Unidad, Nothing, Nothing)
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
        Dim diasMes = hasta.Day
        Dim consumoDiario = consumo \ diasMes

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
        r.Notas.Add("Dias de stock calculado con el mes completo (dias del calendario del periodo).")
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

End Class
