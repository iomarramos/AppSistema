Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

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
        Dim s = r.Seccion("", C("Producto"), C("Unidad"), C("Previsto", FormatoColumna.Cantidad), C("Solicitado", FormatoColumna.Cantidad), C("Entregado"))
        For Each l In New ServicioProduccion(CadenaConexion, Sesion).ListarLineas(requerimientoId)
            s.Agregar(l.ProductoDescripcion, l.Unidad, l.PrevistoU6, l.SolicitadoU6, Nothing)
        Next
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
            Dim s = r.Seccion("", C("Codigo"), C("Descripcion"), C("Unidad"), C("Sistema", FormatoColumna.Cantidad), C("Fisico", FormatoColumna.Cantidad),
                              C("Diferencia", FormatoColumna.Cantidad), C("Valor diferencia", FormatoColumna.Dinero), C("Resultado"))
            For Each l In servicio.Hoja(inventarioId)
                s.Agregar(l.VarianteCodigo, l.Descripcion, l.Unidad, l.SistemaU6, l.FisicoU6, l.DiferenciaU6, l.ValorDiferenciaU6, l.Resultado)
            Next
            Dim res = servicio.Resumen(inventarioId)
            s.Totales = {"TOTAL", $"{res.Lineas} lineas", Nothing, Nothing, Nothing, Nothing, res.SobranteValorU6 - res.FaltanteValorU6, Nothing}
            r.Dato("Lineas sin contar", res.SinContar.ToString())
            r.Dato("Lineas con diferencia", res.ConDiferencia.ToString())
            r.Dato("Faltante (S/)", Soles(res.FaltanteValorU6))
            r.Dato("Sobrante (S/)", Soles(res.SobranteValorU6))
            r.Firmas.AddRange({"Contado por", "Revisado por", "Autorizado por"})
        End If
        Return r
    End Function

    ' ---------- Stock valorizado ----------

    ''' <summary>Stock con saldo de un almacén, valorizado al costo promedio.</summary>
    Public Function StockValorizado(almacenId As Long) As Reporte
        Dim nombres = NombresAlmacenVariante(almacenId, Nothing, Permisos.CatalogoVer)
        Dim saldos = New ServicioStock(CadenaConexion, Sesion).ConsultarSaldos(almacenId, "")
        Dim r = Nuevo($"Stock valorizado {nombres.Almacen}")
        r.Dato("Almacen", nombres.Almacen)
        r.Dato("Fecha", Date.Today.ToString("dd/MM/yyyy"))
        Dim s = r.Seccion("", C("Codigo"), C("Producto"), C("Presentacion"), C("Unidad"), C("Cantidad", FormatoColumna.Cantidad),
                          C("Costo promedio", FormatoColumna.Dinero), C("Valor", FormatoColumna.Dinero))
        For Each x In saldos
            s.Agregar(x.VarianteCodigo, x.ProductoDescripcion, x.VarianteDescripcion, x.Unidad, x.CantidadBaseU6, x.CostoPromedioU6, x.ValorU6)
        Next
        s.Totales = {"TOTAL", $"{saldos.Count} presentaciones", Nothing, Nothing, Nothing, Nothing, saldos.Sum(Function(x) x.ValorU6)}
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
