Imports System.IO
Imports System.Text.Json.Nodes
Imports Npgsql
Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Datos

''' <summary>
''' Reporte "Minuta teórico vs real (plan SGP)" sobre la base local con el plan del SGP cargado (vistas V034).
''' Solo corre si APPSISTEMA_BD_LOCAL trae la conexión del propietario de esa base; en otro caso se omite.
''' Deja el resultado en artifacts/reportes para revisarlo.
''' </summary>
Public Class MinutaRealTests

    <Fact>
    Public Sub Reporte_minuta_teorico_real_sobre_la_base_local()
        Dim cadena = Environment.GetEnvironmentVariable("APPSISTEMA_BD_LOCAL")
        If String.IsNullOrWhiteSpace(cadena) Then Return          ' se omite: sin base local configurada

        Dim operacionId As Long
        Using cn As New NpgsqlConnection(cadena)
            cn.Open()
            Using cmd As New NpgsqlCommand("SELECT o.id FROM empresa e JOIN operacion o ON o.empresa_id = e.id AND o.codigo = 'ORC' WHERE e.codigo = 'DEMO'", cn)
                operacionId = Convert.ToInt64(cmd.ExecuteScalar())
            End Using
            Using cmd As New NpgsqlCommand("SELECT count(*) FROM information_schema.views WHERE table_name = 'v_minuta_teorico_real_plato'", cn)
                Assert.True(Convert.ToInt64(cmd.ExecuteScalar()) = 1L, "La vista v_minuta_teorico_real_plato no existe (V034).")
            End Using
        End Using

        ' Sesión como la de la aplicación: usuario y clave de la base local en variables de entorno (no se guardan).
        Dim login = Environment.GetEnvironmentVariable("APPSISTEMA_BD_LOCAL_LOGIN")
        Dim clave = Environment.GetEnvironmentVariable("APPSISTEMA_BD_LOCAL_CLAVE")
        Assert.False(String.IsNullOrWhiteSpace(login) OrElse String.IsNullOrWhiteSpace(clave), "Defina APPSISTEMA_BD_LOCAL_LOGIN y APPSISTEMA_BD_LOCAL_CLAVE.")
        Dim acceso As New ServicioAcceso(cadena)
        Dim sesion = acceso.SeleccionarOperacion(acceso.IniciarSesion("DEMO", login, clave), operacionId)

        Dim reporte = New ServicioReportes(cadena, sesion).MinutaTeoricoReal(New Date(2026, 8, 1), New Date(2026, 10, 31))
        Assert.Equal("Minuta teorico vs real (plan SGP)", reporte.Titulo)
        Assert.Equal(92 * 3, reporte.Secciones(0).Filas.Count)       ' 92 días × desayuno, almuerzo y cena
        Assert.True(reporte.Secciones(1).Filas.Count > 0)

        Dim carpeta = Path.Combine("..", "..", "..", "..", "..", "artifacts", "reportes")
        Directory.CreateDirectory(carpeta)
        File.WriteAllBytes(Path.Combine(carpeta, "minuta_teorico_vs_real.xlsx"), ExportadorReporte.AExcel(reporte))
        File.WriteAllBytes(Path.Combine(carpeta, "minuta_teorico_vs_real.pdf"), ExportadorReporte.APdf(reporte))
    End Sub

    <Fact>
    Public Sub Vista_de_la_planilla_del_sgp_cuadra_con_la_base_local()
        Dim cadena = Environment.GetEnvironmentVariable("APPSISTEMA_BD_LOCAL")
        If String.IsNullOrWhiteSpace(cadena) Then Return          ' se omite: sin base local configurada
        Dim login = Environment.GetEnvironmentVariable("APPSISTEMA_BD_LOCAL_LOGIN")
        Dim clave = Environment.GetEnvironmentVariable("APPSISTEMA_BD_LOCAL_CLAVE")
        Assert.False(String.IsNullOrWhiteSpace(login) OrElse String.IsNullOrWhiteSpace(clave), "Defina APPSISTEMA_BD_LOCAL_LOGIN y APPSISTEMA_BD_LOCAL_CLAVE.")

        Dim acceso As New ServicioAcceso(cadena)
        Dim operacionId As Long
        Using cn As New NpgsqlConnection(cadena)
            cn.Open()
            Using cmd As New NpgsqlCommand("SELECT o.id FROM empresa e JOIN operacion o ON o.empresa_id = e.id AND o.codigo = 'ORC' WHERE e.codigo = 'DEMO'", cn)
                operacionId = Convert.ToInt64(cmd.ExecuteScalar())
            End Using
        End Using
        Dim sesion = acceso.SeleccionarOperacion(acceso.IniciarSesion("DEMO", login, clave), operacionId)
        Dim plan As New ServicioPlanSgp(cadena, sesion)
        Dim desde = New Date(2026, 10, 1)
        Dim hasta = New Date(2026, 10, 31)

        Dim servicios = plan.ListarServicios()
        Assert.Contains("ALMUERZO", servicios)
        For Each nivel In {"TEORICO", "REAL"}
            For Each servicio In servicios
                Dim v = plan.Vista(nivel, servicio, desde, hasta)
                Assert.Equal(31, v.Dias.Count)
                ' Cada día: Σ(raciones × costo por ración) / comensales = costo por bandeja de la minuta (tolerancia de céntimos).
                For Each d In v.Dias
                    Dim suma As Decimal = 0D
                    For Each est In v.Estructuras
                        Dim lista As List(Of PlatoPlanSgpDto) = Nothing
                        If v.Platos.TryGetValue(VistaPlanSgpDto.Clave(est, d.Fecha), lista) Then
                            For Each p In lista
                                suma += (p.RacionesU6 / 1000000D) * (p.CostoRacionU6 / 1000000D)
                            Next
                        End If
                    Next
                    If d.Comensales > 0 Then
                        Assert.True(Math.Abs(suma / d.Comensales - d.CostoMinutaDiaU6 / 1000000D) < 0.02D,
                                    $"{nivel} {servicio} {d.Fecha:dd/MM}: suma por comensal {suma / d.Comensales:0.00} y costo del dia {d.CostoMinutaDiaU6 / 1000000D:0.00}")
                    End If
                Next
            Next
        Next

        ' Texto con el mismo diseño de la planilla del SGP (primeros 6 días), para revisar.
        Dim carpeta = Path.Combine("..", "..", "..", "..", "..", "artifacts", "reportes")
        Directory.CreateDirectory(carpeta)
        Dim v1 = plan.Vista("TEORICO", "ALMUERZO", desde, hasta)
        Dim lineas As New List(Of String) From {"ALMUERZO - TEORICO - 01/10/2026 a 31/10/2026"}
        Dim cab = "Estructura".PadRight(22)
        For Each d In v1.Dias.Take(6)
            cab &= $"| {d.Fecha:dd/MM} N.Rac. Por.% Costo  Cod "
        Next
        lineas.Add(cab)
        For Each est In v1.Estructuras
            Dim maxK = 0
            For Each d In v1.Dias
                Dim lista As List(Of PlatoPlanSgpDto) = Nothing
                If v1.Platos.TryGetValue(VistaPlanSgpDto.Clave(est, d.Fecha), lista) Then maxK = Math.Max(maxK, lista.Count)
            Next
            For k = 0 To maxK - 1
                Dim linea = (If(k = 0, est, "")).PadRight(22)
                For Each d In v1.Dias.Take(6)
                    Dim lista As List(Of PlatoPlanSgpDto) = Nothing
                    If v1.Platos.TryGetValue(VistaPlanSgpDto.Clave(est, d.Fecha), lista) AndAlso k < lista.Count Then
                        Dim p = lista(k)
                        Dim rac = p.RacionesU6 / 1000000D
                        linea &= $"| {rac,5:0} {(If(d.Comensales > 0, rac * 100D / d.Comensales, 0D)),6:0.00} {p.CostoRacionU6 / 1000000D,8:0.000000} {p.Receta,-6}"
                    Else
                        linea &= "|".PadRight(31)
                    End If
                Next
                lineas.Add(linea)
            Next
        Next
        lineas.Add("Comensales".PadRight(22) & String.Join("", v1.Dias.Take(6).Select(Function(d) ($"| {d.Comensales,5}").PadRight(31))))
        lineas.Add("Costo minuta dia".PadRight(22) & String.Join("", v1.Dias.Take(6).Select(Function(d) $"| {d.CostoMinutaDiaU6 / 1000000D,8:0.00}".PadRight(31))))
        File.WriteAllLines(Path.Combine(carpeta, "planilla_sgp_almuerzo_oct.txt"), lineas)
    End Sub

    <Fact>
    Public Sub Menu_mensual_json_sigue_el_esquema_acordado_con_el_plan_real_de_octubre()
        Dim cadena = Environment.GetEnvironmentVariable("APPSISTEMA_BD_LOCAL")
        If String.IsNullOrWhiteSpace(cadena) Then Return          ' se omite: sin base local configurada
        Dim login = Environment.GetEnvironmentVariable("APPSISTEMA_BD_LOCAL_LOGIN")
        Dim clave = Environment.GetEnvironmentVariable("APPSISTEMA_BD_LOCAL_CLAVE")
        Assert.False(String.IsNullOrWhiteSpace(login) OrElse String.IsNullOrWhiteSpace(clave), "Defina APPSISTEMA_BD_LOCAL_LOGIN y APPSISTEMA_BD_LOCAL_CLAVE.")

        Dim acceso As New ServicioAcceso(cadena)
        Dim operacionId As Long
        Using cn As New NpgsqlConnection(cadena)
            cn.Open()
            Using cmd As New NpgsqlCommand("SELECT o.id FROM empresa e JOIN operacion o ON o.empresa_id = e.id AND o.codigo = 'ORC' WHERE e.codigo = 'DEMO'", cn)
                operacionId = Convert.ToInt64(cmd.ExecuteScalar())
            End Using
        End Using
        Dim sesion = acceso.SeleccionarOperacion(acceso.IniciarSesion("DEMO", login, clave), operacionId)

        Dim json = New ServicioPlanSgp(cadena, sesion).MenuMensualJson("ALMUERZO", New Date(2026, 10, 1), New Date(2026, 10, 31))
        Dim raiz = JsonNode.Parse(json)

        ' Encabezado: código del contrato (V036) y régimen/servicio como los usa el comparativo del SGP.
        Assert.Equal("PE017401", raiz("operacion")("codigo").GetValue(Of String)())
        Assert.Equal("REGIMEN 3", raiz("operacion")("regimen").GetValue(Of String)())
        Assert.Equal("ALMUERZO NORMAL 1", raiz("operacion")("servicio").GetValue(Of String)())
        ' El realizado no tiene datos: va en null, no en cero.
        Assert.Null(raiz("resumen_mes")("realizado"))

        Dim dias = raiz("programacion_diaria").AsArray()
        Assert.Equal(31, dias.Count)
        Dim primero = dias(0)
        Assert.Equal("2026-10-01", primero("fecha").GetValue(Of String)())
        Assert.Equal("Jueves", primero("dia_semana").GetValue(Of String)())

        ' Los seis platos del ejemplo del programador: estructura (categoria_id = orden), raciones y costo por ración.
        Dim items = primero("items").AsArray()
        Dim ejemplo() As (Orden As Integer, Estructura As String, Raciones As Decimal, Costo As Decimal) = {
            (1, "ENTRADA FRIA 1", 442D, 0.457382D), (6, "SOPA NORMAL 1", 473D, 2.704629D),
            (8, "PLATO DE FONDO NORMAL 1", 196D, 5.208646D), (13, "GUARNICION ARROZ", 480D, 0.593380D),
            (28, "FRUTA 1", 260D, 0.909D), (32, "BEBIDA FRIA 1", 884D, 0.217802D)}
        For Each e In ejemplo
            Dim it = items.First(Function(x) x("categoria_nombre").GetValue(Of String)() = e.Estructura)
            Assert.Equal(e.Orden, it("categoria_id").GetValue(Of Integer)())
            Assert.Equal(e.Raciones, it("raciones").GetValue(Of Decimal)())
            Assert.Equal(e.Costo, it("costo_unitario").GetValue(Of Decimal)())
            Assert.Null(it("estado"))   ' R/H sin fuente en los datos: null, no se inventa
        Next

        ' Materia prima del día (Σ raciones × costo): cuadra con el real de la base (4 485,89).
        Dim materia = 0D
        For Each it In items
            materia += it("raciones").GetValue(Of Decimal)() * it("costo_unitario").GetValue(Of Decimal)()
        Next
        Assert.InRange(materia, 4485.88D, 4485.90D)

        Dim carpeta = Path.Combine("..", "..", "..", "..", "..", "artifacts", "reportes")
        Directory.CreateDirectory(carpeta)
        File.WriteAllText(Path.Combine(carpeta, "menu_mensual_almuerzo_oct.json"), json)
    End Sub

End Class
