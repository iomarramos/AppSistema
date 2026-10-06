Imports System.Globalization
Imports System.Text
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

Public NotInheritable Class GastoDto
    Public Property Id As Long
    Public Property Concepto As String
    Public Property Categoria As String
    Public Property Servicio As String
    Public Property CuentaContable As String
    Public Property ImporteU6 As Long
    Public Property Proyectado As Boolean
End Class

Public NotInheritable Class LineaResultadoDto
    Public Property Servicio As String
    Public Property IngresoU6 As Long
    Public Property FuenteIngreso As String
    Public Property CostoAlimentosU6 As Long
    Public Property GastosPersonalU6 As Long
    Public Property GastosOperacionU6 As Long
    Public Property OtrosGastosU6 As Long
    Public Property TotalGastosU6 As Long
    Public Property MargenU6 As Long
    Public Property MargenPorcentajeU6 As Long?
    ''' <summary>Gastos proyectados (presupuesto) del servicio en el mes.</summary>
    Public Property PresupuestoGastosU6 As Long
    ''' <summary>Presupuesto por rubro (gastos proyectados): personal, operación y otros (administración incluida).</summary>
    Public Property PresupuestoPersonalU6 As Long
    Public Property PresupuestoOperacionU6 As Long
    Public Property PresupuestoOtrosU6 As Long
End Class

''' <summary>Una fila de la comparación presupuesto, mes anterior y acumulado del año (importes en unidad U6).</summary>
Public NotInheritable Class FilaComparacionDto
    Public Property Concepto As String
    ''' <summary>Texto del presupuesto: importe, o "-" cuando el concepto no tiene presupuesto (ingresos, costos, margen).</summary>
    Public Property PresupuestoTexto As String
    Public Property ValorRealU6 As Long
    Public Property ValorMesAnteriorU6 As Long
    Public Property ValorAcumuladoU6 As Long
End Class

Public NotInheritable Class ResultadoMensualDto
    Public Property Anio As Integer
    Public Property Mes As Integer
    Public Property Estado As String
    Public ReadOnly Property Lineas As New List(Of LineaResultadoDto)
    Public Property Total As LineaResultadoDto
End Class

''' <summary>
''' Resultado mensual de la operación (etapa 9): ingreso de cada servicio − alimentos consumidos − gastos reales de
''' personal, operación y otros. Los gastos sin servicio, las bajas y los ajustes de inventario van en "No asignado".
''' Cada cifra se rastrea: ingreso a su fuente (contrato o manual), alimentos a los documentos, gastos a su registro.
''' </summary>
Public NotInheritable Class ServicioResultados
    Inherits ServicioConSesion

    Public Const NoAsignado As String = "No asignado (gastos comunes, bajas y ajustes de inventario)"
    Public Shared ReadOnly Categorias As String() = {"personal", "operacion", "administracion", "otros"}

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    Private ReadOnly Property Op As Long
        Get
            If Sesion.Operacion Is Nothing Then Throw New ReglaNegocioException("OPERACION_NO_SELECCIONADA", "Seleccione una operacion.")
            Return Sesion.Operacion.Id
        End Get
    End Property

    Public Function RegistrarGasto(anio As Integer, mes As Integer, operacionServicioId As Long?, concepto As String, categoria As String,
                                   importeU6 As Long, proyectado As Boolean, Optional cuentaContable As String = Nothing) As Long
        If importeU6 < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "El importe no puede ser negativo.")
        If Not Categorias.Contains(categoria) Then Throw New ReglaNegocioException("DATO_INVALIDO", "Categoria: " & String.Join(", ", Categorias) & ".")
        Dim o = Op
        Return EnTransaccion(Permisos.GastosEditar,
            Function(u)
                If operacionServicioId.HasValue Then ServicioCierres.ExigirServicio(u, operacionServicioId.Value, o)
                Dim periodoId = ServicioCierres.PeriodoAbierto(u, Sesion.EmpresaId, o, anio, mes)
                Return u.EscalarLong(
                    "INSERT INTO gasto(empresa_id, periodo_id, operacion_servicio_id, concepto, categoria, cuenta_contable, importe_u6, moneda, es_proyectado, usuario_id) " &
                    "VALUES (@e, @p, @os, @c, @cat, @cta, @i, (SELECT moneda FROM empresa WHERE id = @e), @pr, @u) RETURNING id",
                    "e", Sesion.EmpresaId, "p", periodoId, "os", If(operacionServicioId.HasValue, CObj(operacionServicioId.Value), Nothing),
                    "c", ServicioAdministracion.Requerido(concepto, "concepto"), "cat", categoria,
                    "cta", If(String.IsNullOrWhiteSpace(cuentaContable), Nothing, cuentaContable.Trim()), "i", importeU6, "pr", If(proyectado, 1L, 0L),
                    "u", Sesion.UsuarioId)
            End Function)
    End Function

    ''' <summary>Elimina un gasto de un mes abierto (en un mes cerrado la base lo impide).</summary>
    Public Sub EliminarGasto(gastoId As Long)
        Dim o = Op
        EnTransaccion(Permisos.GastosEditar,
            Function(u)
                If u.Ejecutar("DELETE FROM gasto g USING periodo_mensual pm WHERE g.id = @g AND pm.id = g.periodo_id AND pm.operacion_id = @o", "g", gastoId, "o", o) = 0 Then
                    Throw New ReglaNegocioException("NO_ENCONTRADO", "El gasto no existe en esta operacion.")
                End If
                Return 0
            End Function)
    End Sub

    ''' <summary>Servicios de la operación para asignar gastos (con el permiso de resultados, no el de contratos).</summary>
    Public Function ServiciosDeOperacion() As List(Of OperacionServicioDto)
        Dim o = Op
        Return EnTransaccion(Permisos.ResultadosVer, Function(u) ServicioContratos.LeerServiciosDeOperacion(u, o))
    End Function

    Public Function ListarGastos(anio As Integer, mes As Integer) As List(Of GastoDto)
        Dim o = Op
        Return EnTransaccion(Permisos.ResultadosVer,
            Function(u) u.Consultar(
                "SELECT g.id, g.concepto, g.categoria, COALESCE(s.nombre || ' - ' || rg.nombre, '') AS servicio, g.cuenta_contable, g.importe_u6, g.es_proyectado = 1 " &
                "FROM gasto g JOIN periodo_mensual pm ON pm.id = g.periodo_id LEFT JOIN operacion_servicio os ON os.id = g.operacion_servicio_id " &
                "LEFT JOIN servicio s ON s.id = os.servicio_id LEFT JOIN regimen rg ON rg.id = os.regimen_id " &
                "WHERE pm.operacion_id = @o AND pm.anio = @a AND pm.mes = @m ORDER BY g.es_proyectado, g.categoria, g.id",
                Function(rd) New GastoDto With {.Id = rd.GetInt64(0), .Concepto = rd.GetString(1), .Categoria = rd.GetString(2), .Servicio = rd.GetString(3),
                                                .CuentaContable = rd.TextoONada("cuenta_contable"), .ImporteU6 = rd.GetInt64(5), .Proyectado = rd.GetBoolean(6)},
                "o", o, "a", anio, "m", mes))
    End Function

    Public Function ResultadoMensual(anio As Integer, mes As Integer) As ResultadoMensualDto
        Dim o = Op
        Return EnTransaccion(Permisos.ResultadosVer,
            Function(u)
                Dim rep = ServicioCierres.LeerReporte(u, o, anio, mes)
                Dim r As New ResultadoMensualDto With {.Anio = anio, .Mes = mes, .Estado = rep.Estado}
                Dim gastos = u.Consultar(
                    "SELECT g.operacion_servicio_id, g.categoria, g.es_proyectado = 1, sum(g.importe_u6)::bigint FROM gasto g JOIN periodo_mensual pm ON pm.id = g.periodo_id " &
                    "WHERE pm.operacion_id = @o AND pm.anio = @a AND pm.mes = @m GROUP BY 1, 2, 3",
                    Function(rd) (Servicio:=If(rd.IsDBNull(0), CType(Nothing, Long?), rd.GetInt64(0)), Categoria:=rd.GetString(1),
                                  Proyectado:=rd.GetBoolean(2), Importe:=rd.GetInt64(3)), "o", o, "a", anio, "m", mes)
                Dim fuentes = u.Consultar(
                    "SELECT i.operacion_servicio_id, i.fuente FROM ingreso_servicio i JOIN periodo_mensual pm ON pm.id = i.periodo_id " &
                    "WHERE pm.operacion_id = @o AND pm.anio = @a AND pm.mes = @m",
                    Function(rd) (rd.GetInt64(0), rd.GetString(1)), "o", o, "a", anio, "m", mes).ToDictionary(Function(x) x.Item1, Function(x) x.Item2)

                Dim presupuesto = Function(servicioId As Long?, rubro As Func(Of String, Boolean)) As Long
                                      Return gastos.Where(Function(g) Nullable.Equals(g.Servicio, servicioId) AndAlso g.Proyectado AndAlso rubro(g.Categoria)).Sum(Function(g) g.Importe)
                                  End Function
                Dim armar = Function(nombre As String, servicioId As Long?, ingreso As Long, alimentos As Long, otrosExtra As Long, fuente As String)
                                Dim reales = gastos.Where(Function(g) Nullable.Equals(g.Servicio, servicioId) AndAlso Not g.Proyectado).ToList()
                                Dim calc As New ResultadoServicio With {
                                    .IngresoU6 = ingreso, .CostoAlimentosU6 = alimentos,
                                    .GastosPersonalU6 = reales.Where(Function(g) g.Categoria = "personal").Sum(Function(g) g.Importe),
                                    .GastosOperacionU6 = reales.Where(Function(g) g.Categoria = "operacion").Sum(Function(g) g.Importe),
                                    .OtrosGastosU6 = reales.Where(Function(g) g.Categoria <> "personal" AndAlso g.Categoria <> "operacion").Sum(Function(g) g.Importe) + otrosExtra}
                                Return New LineaResultadoDto With {
                                    .Servicio = nombre, .IngresoU6 = ingreso, .FuenteIngreso = fuente, .CostoAlimentosU6 = alimentos,
                                    .GastosPersonalU6 = calc.GastosPersonalU6, .GastosOperacionU6 = calc.GastosOperacionU6, .OtrosGastosU6 = calc.OtrosGastosU6,
                                    .TotalGastosU6 = calc.TotalGastosU6, .MargenU6 = calc.MargenU6, .MargenPorcentajeU6 = calc.MargenPorcentajeU6,
                                    .PresupuestoGastosU6 = gastos.Where(Function(g) Nullable.Equals(g.Servicio, servicioId) AndAlso g.Proyectado).Sum(Function(g) g.Importe),
                                    .PresupuestoPersonalU6 = presupuesto(servicioId, Function(c) c = "personal"),
                                    .PresupuestoOperacionU6 = presupuesto(servicioId, Function(c) c = "operacion"),
                                    .PresupuestoOtrosU6 = presupuesto(servicioId, Function(c) c <> "personal" AndAlso c <> "operacion")}
                            End Function

                For Each s In rep.Servicios
                    Dim fuente As String = Nothing
                    fuentes.TryGetValue(s.OperacionServicioId, fuente)
                    r.Lineas.Add(armar($"{s.Servicio} - {s.Regimen}", s.OperacionServicioId, If(s.IngresoU6, 0L), s.CostoAlimentosU6, 0L, If(fuente, "sin ingreso")))
                Next
                ' Bajas (costo) y ajustes de inventario (sobrante reduce costo, faltante lo aumenta) no se atribuyen a un servicio.
                r.Lineas.Add(armar(NoAsignado, Nothing, 0L, 0L, rep.BajasU6 - rep.AjusteInventarioU6, ""))
                Dim t As New ResultadoServicio With {
                    .IngresoU6 = r.Lineas.Sum(Function(l) l.IngresoU6), .CostoAlimentosU6 = r.Lineas.Sum(Function(l) l.CostoAlimentosU6),
                    .GastosPersonalU6 = r.Lineas.Sum(Function(l) l.GastosPersonalU6), .GastosOperacionU6 = r.Lineas.Sum(Function(l) l.GastosOperacionU6),
                    .OtrosGastosU6 = r.Lineas.Sum(Function(l) l.OtrosGastosU6)}
                r.Total = New LineaResultadoDto With {
                    .Servicio = "TOTAL", .IngresoU6 = t.IngresoU6, .CostoAlimentosU6 = t.CostoAlimentosU6, .GastosPersonalU6 = t.GastosPersonalU6,
                    .GastosOperacionU6 = t.GastosOperacionU6, .OtrosGastosU6 = t.OtrosGastosU6, .TotalGastosU6 = t.TotalGastosU6, .MargenU6 = t.MargenU6,
                    .MargenPorcentajeU6 = t.MargenPorcentajeU6, .PresupuestoGastosU6 = r.Lineas.Sum(Function(l) l.PresupuestoGastosU6),
                    .PresupuestoPersonalU6 = r.Lineas.Sum(Function(l) l.PresupuestoPersonalU6), .PresupuestoOperacionU6 = r.Lineas.Sum(Function(l) l.PresupuestoOperacionU6),
                    .PresupuestoOtrosU6 = r.Lineas.Sum(Function(l) l.PresupuestoOtrosU6)}
                Return r
            End Function)
    End Function

    ''' <summary>
    ''' Exportación para sistemas contables (contrato de datos en docs/04_ARQUITECTURA_Y_DATOS/INTEGRACION_RESULTADOS.md): CSV con punto y coma,
    ''' UTF-8, importes con punto decimal y 2 decimales, una fila por servicio más "No asignado" y TOTAL.
    ''' </summary>
    Public Function ExportarCsv(anio As Integer, mes As Integer) As String
        Dim r = ResultadoMensual(anio, mes)
        Dim inv = CultureInfo.InvariantCulture
        Dim d = Function(v As Long) EscalaU6.ADecimal(v).ToString("0.00", inv)
        Dim sb As New StringBuilder()
        sb.Append("version;empresa;operacion;periodo;estado_periodo;servicio;ingreso;fuente_ingreso;alimentos;personal;operacion_gastos;otros;total_gastos;margen;margen_pct;presupuesto_gastos").Append(vbLf)
        For Each l In r.Lineas.Concat({r.Total})
            sb.Append(String.Join(";", "1", Sesion.EmpresaCodigo, Sesion.Operacion.Codigo, $"{anio:0000}-{mes:00}", r.Estado, Limpio(l.Servicio), d(l.IngresoU6),
                                  Limpio(If(l.FuenteIngreso, "")), d(l.CostoAlimentosU6), d(l.GastosPersonalU6), d(l.GastosOperacionU6), d(l.OtrosGastosU6),
                                  d(l.TotalGastosU6), d(l.MargenU6), If(l.MargenPorcentajeU6.HasValue, d(l.MargenPorcentajeU6.Value), ""), d(l.PresupuestoGastosU6))).Append(vbLf)
        Next
        Return sb.ToString()
    End Function

    Private Shared Function Limpio(texto As String) As String
        Return texto.Replace(";", ",").Replace(vbCr, " ").Replace(vbLf, " ")
    End Function

    ''' <summary>
    ''' Comparación del mes con su presupuesto por rubro, con el mes anterior y con el acumulado del año (de enero al mes
    ''' elegido). Son los importes totales de la operación; el presupuesto solo existe para los gastos.
    ''' </summary>
    Public Function Comparacion(anio As Integer, mes As Integer) As List(Of FilaComparacionDto)
        Dim actual = ResultadoMensual(anio, mes).Total
        Dim anterior = If(mes = 1, ResultadoMensual(anio - 1, 12).Total, ResultadoMensual(anio, mes - 1).Total)
        Dim acumulado As New LineaResultadoDto()
        For m = 1 To mes
            Dim t = ResultadoMensual(anio, m).Total
            acumulado.IngresoU6 += t.IngresoU6
            acumulado.CostoAlimentosU6 += t.CostoAlimentosU6
            acumulado.GastosPersonalU6 += t.GastosPersonalU6
            acumulado.GastosOperacionU6 += t.GastosOperacionU6
            acumulado.OtrosGastosU6 += t.OtrosGastosU6
            acumulado.TotalGastosU6 += t.TotalGastosU6
            acumulado.MargenU6 += t.MargenU6
        Next
        Const sinPresupuesto As String = "-"
        Dim importe = Function(v As Long) EscalaU6.ADecimal(v).ToString("0.00", CultureInfo.InvariantCulture)
        Dim fila = Function(concepto As String, presupuestoTexto As String, real As Long, ant As Long, acum As Long) _
            New FilaComparacionDto With {.Concepto = concepto, .PresupuestoTexto = presupuestoTexto, .ValorRealU6 = real,
                                         .ValorMesAnteriorU6 = ant, .ValorAcumuladoU6 = acum}
        Return New List(Of FilaComparacionDto) From {
            fila("Ingreso", sinPresupuesto, actual.IngresoU6, anterior.IngresoU6, acumulado.IngresoU6),
            fila("Costo de alimentos", sinPresupuesto, actual.CostoAlimentosU6, anterior.CostoAlimentosU6, acumulado.CostoAlimentosU6),
            fila("Gastos de personal", importe(actual.PresupuestoPersonalU6), actual.GastosPersonalU6, anterior.GastosPersonalU6, acumulado.GastosPersonalU6),
            fila("Gastos de operacion", importe(actual.PresupuestoOperacionU6), actual.GastosOperacionU6, anterior.GastosOperacionU6, acumulado.GastosOperacionU6),
            fila("Otros gastos (incluye administracion)", importe(actual.PresupuestoOtrosU6), actual.OtrosGastosU6, anterior.OtrosGastosU6, acumulado.OtrosGastosU6),
            fila("Total gastos", importe(actual.PresupuestoGastosU6), actual.TotalGastosU6, anterior.TotalGastosU6, acumulado.TotalGastosU6),
            fila("Margen", sinPresupuesto, actual.MargenU6, anterior.MargenU6, acumulado.MargenU6)}
    End Function

End Class
