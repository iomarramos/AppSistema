Imports System.Globalization
Imports System.Linq
Imports System.Text.Encodings.Web
Imports System.Text.Json
Imports System.Text.Json.Nodes
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

''' <summary>Un plato de una estructura en un día: receta SGP, raciones, porcentaje y costo por ración (U6).</summary>
Public NotInheritable Class PlatoPlanSgpDto
    Public Property Estructura As String
    Public Property Orden As Long
    Public Property Receta As String
    Public Property RacionesU6 As Long
    Public Property CostoRacionU6 As Long
End Class

''' <summary>Un día del plan: comensales, costo por bandeja (U6) y si existe el plan real de ese día.</summary>
Public NotInheritable Class DiaPlanSgpDto
    Public Property Fecha As Date
    Public Property Comensales As Long
    Public Property CostoMinutaDiaU6 As Long
    Public Property TieneReal As Boolean
End Class

''' <summary>
''' Vista de la minuta en el formato del SGP: estructuras con sus platos por día. Los platos de cada estructura y día
''' están en el orden del plan; la pantalla los pone en filas sucesivas.
''' </summary>
Public NotInheritable Class VistaPlanSgpDto
    Public Property Nivel As String
    Public Property Servicio As String
    Public Property Estructuras As New List(Of String)()
    Public Property Dias As New List(Of DiaPlanSgpDto)()
    ''' <summary>Clave: estructura|yyyyMMdd. Valor: platos de esa estructura ese día, en orden.</summary>
    Public Property Platos As New Dictionary(Of String, List(Of PlatoPlanSgpDto))()

    Public Shared Function Clave(estructura As String, fecha As Date) As String
        Return estructura & "|" & fecha.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
    End Function
End Class

''' <summary>Lectura del plan teórico y real del SGP cargado en la base (sgp_plan_dia y sgp_plan_plato).</summary>
Public NotInheritable Class ServicioPlanSgp
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    ''' <summary>Servicios que tienen plan cargado en esta operación.</summary>
    Public Function ListarServicios() As List(Of String)
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                If Not PlanCargado(u) Then Return New List(Of String)()
                Return u.Consultar("SELECT DISTINCT servicio FROM sgp_plan_dia WHERE operacion_id = @o ORDER BY servicio",
                                   Function(rd) rd.GetString(0), "o", Sesion.Operacion.Id)
            End Function)
    End Function

    ''' <summary>Una base sin el plan del SGP (por ejemplo, una base limpia de esta rama) no tiene estas tablas: no es un error.</summary>
    Private Shared Function PlanCargado(u As UnidadDeTrabajo) As Boolean
        Return CBool(u.Escalar("SELECT to_regclass('public.sgp_plan_dia') IS NOT NULL AND to_regclass('public.sgp_plan_plato') IS NOT NULL"))
    End Function

    ''' <summary>Vista de un nivel (TEORICO o REAL) y servicio en el rango de fechas.</summary>
    Public Function Vista(nivel As String, servicio As String, desde As Date, hasta As Date) As VistaPlanSgpDto
        If nivel <> "TEORICO" AndAlso nivel <> "REAL" Then Throw New ReglaNegocioException("DATO_INVALIDO", "El nivel es TEORICO o REAL.")
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                Dim v As New VistaPlanSgpDto With {.Nivel = nivel, .Servicio = servicio}
                If Not PlanCargado(u) Then Return v
                Dim conReal = New HashSet(Of Date)(u.Consultar(
                    "SELECT fecha FROM sgp_plan_dia WHERE operacion_id = @o AND nivel = 'REAL' AND servicio = @s AND fecha BETWEEN @d AND @h",
                    Function(rd) rd.GetDateTime(0).Date, "o", Sesion.Operacion.Id, "s", servicio, "d", desde.Date, "h", hasta.Date))
                v.Dias = u.Consultar(
                    "SELECT fecha, comensales, costo_minuta_dia_u6 FROM sgp_plan_dia WHERE operacion_id = @o AND nivel = @n AND servicio = @s " &
                    "AND fecha BETWEEN @d AND @h ORDER BY fecha",
                    Function(rd) New DiaPlanSgpDto With {.Fecha = rd.GetDateTime(0).Date,
                                                         .Comensales = If(rd.IsDBNull(1), 0L, rd.GetInt64(1)),
                                                         .CostoMinutaDiaU6 = If(rd.IsDBNull(2), 0L, rd.GetInt64(2)),
                                                         .TieneReal = conReal.Contains(rd.GetDateTime(0).Date)},
                    "o", Sesion.Operacion.Id, "n", nivel, "s", servicio, "d", desde.Date, "h", hasta.Date)
                Dim platos = u.Consultar(
                    "SELECT fecha, estructura, orden, receta_codigo_sgp, raciones_u6, costo_racion_u6 FROM sgp_plan_plato " &
                    "WHERE operacion_id = @o AND nivel = @n AND servicio = @s AND fecha BETWEEN @d AND @h ORDER BY fecha, orden",
                    Function(rd) (Fecha:=rd.GetDateTime(0).Date, Estructura:=rd.GetString(1), Orden:=rd.GetInt64(2),
                                  Receta:=rd.GetString(3), Raciones:=If(rd.IsDBNull(4), 0L, rd.GetInt64(4)),
                                  Costo:=If(rd.IsDBNull(5), 0L, rd.GetInt64(5))),
                    "o", Sesion.Operacion.Id, "n", nivel, "s", servicio, "d", desde.Date, "h", hasta.Date)
                ' El orden de las estructuras es el primero en que aparecen en el plan (como en el SGP).
                For Each p In platos
                    If Not v.Estructuras.Contains(p.Estructura) Then v.Estructuras.Add(p.Estructura)
                    Dim clave = VistaPlanSgpDto.Clave(p.Estructura, p.Fecha)
                    If Not v.Platos.ContainsKey(clave) Then v.Platos(clave) = New List(Of PlatoPlanSgpDto)()
                    v.Platos(clave).Add(New PlatoPlanSgpDto With {.Estructura = p.Estructura, .Orden = p.Orden, .Receta = p.Receta,
                                                                  .RacionesU6 = p.Raciones, .CostoRacionU6 = p.Costo})
                Next
                Return v
            End Function)
    End Function

    ''' <summary>
    ''' Menú mensual en el JSON acordado con el programador: operación, resumen del mes y programación diaria.
    ''' El bloque planificado sale del plan real del SGP (recetas, raciones y costo por ración). El realizado va en null:
    ''' la base no tiene ejecución. estado (R = Realizado/Bloqueado, H = Habilitado) va en null: no hay fuente para decidirlo.
    ''' categoria_id es el número de estructura en el plan.
    ''' El código del contrato sale de la operación (V036); si falta, no se exporta.
    ''' </summary>
    Public Function MenuMensualJson(servicioPlan As String, desde As Date, hasta As Date) As String
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                If Not PlanCargado(u) Then Throw New ReglaNegocioException("PLAN_NO_CARGADO", "No hay plan del SGP cargado en la base.")
                Dim contrato = TryCast(u.Escalar("SELECT codigo_contrato FROM operacion WHERE id = @o", "o", Sesion.Operacion.Id), String)
                If String.IsNullOrWhiteSpace(contrato) Then
                    Throw New ReglaNegocioException("CONTRATO_PENDIENTE", "La operacion no tiene codigo de contrato. Registrelo antes de exportar.")
                End If

                ' Nombre del servicio y régimen como los usa el comparativo (ALMUERZO NORMAL 1, REGIMEN 3). Si no hay una sola
                ' coincidencia, se usa el nombre del plan y el régimen queda vacío: no se adivina.
                Dim nombres = u.Consultar(
                    "SELECT DISTINCT regimen, servicio FROM sgp_comparativo_dia WHERE operacion_id = @o AND servicio LIKE @p ORDER BY 1, 2",
                    Function(rd) (Regimen:=rd.GetString(0), Servicio:=rd.GetString(1)),
                    "o", Sesion.Operacion.Id, "p", servicioPlan & " NORMAL %")
                Dim etiquetaServicio = If(nombres.Count = 1, nombres(0).Servicio, servicioPlan)
                Dim regimen = If(nombres.Count = 1, nombres(0).Regimen, "")

                Dim dias = u.Consultar(
                    "SELECT fecha, comensales, costo_minuta_dia_u6 FROM sgp_plan_dia WHERE operacion_id = @o AND nivel = 'REAL' AND servicio = @s " &
                    "AND fecha BETWEEN @d AND @h ORDER BY fecha",
                    Function(rd) (Fecha:=rd.GetDateTime(0).Date, Comensales:=If(rd.IsDBNull(1), 0L, rd.GetInt64(1)),
                                  CostoU6:=If(rd.IsDBNull(2), 0L, rd.GetInt64(2))),
                    "o", Sesion.Operacion.Id, "s", servicioPlan, "d", desde.Date, "h", hasta.Date)
                Dim items = u.Consultar(
                    "SELECT p.fecha, p.orden, p.estructura, COALESCE(c.nombre_sgp, p.receta_codigo_sgp), p.raciones_u6, p.costo_racion_u6 " &
                    "FROM sgp_plan_plato p LEFT JOIN sgp_codigo_receta c ON c.empresa_id = p.empresa_id AND c.codigo_sgp = p.receta_codigo_sgp " &
                    "WHERE p.operacion_id = @o AND p.nivel = 'REAL' AND p.servicio = @s AND p.fecha BETWEEN @d AND @h ORDER BY p.fecha, p.orden",
                    Function(rd) (Fecha:=rd.GetDateTime(0).Date, Linea:=rd.GetInt64(1), Categoria:=rd.GetString(2),
                                  Platillo:=rd.GetString(3), RacionesU6:=If(rd.IsDBNull(4), 0L, rd.GetInt64(4)),
                                  CostoU6:=If(rd.IsDBNull(5), 0L, rd.GetInt64(5))),
                    "o", Sesion.Operacion.Id, "s", servicioPlan, "d", desde.Date, "h", hasta.Date)
                Return MenuJson(contrato, Sesion.Operacion.Nombre, etiquetaServicio, regimen, dias, items)
            End Function)
    End Function

    ''' <summary>Número sin ceros de relleno (442 y no 442.000000), con hasta 6 decimales.</summary>
    Private Shared Function Numero(valor As Decimal) As JsonNode
        Return JsonValue.Create(Decimal.Parse(valor.ToString("0.######", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture))
    End Function

    Private Shared Function MenuJson(contrato As String, nombreOperacion As String, servicio As String, regimen As String,
                                     dias As List(Of (Fecha As Date, Comensales As Long, CostoU6 As Long)),
                                     items As List(Of (Fecha As Date, Linea As Long, Categoria As String, Platillo As String, RacionesU6 As Long, CostoU6 As Long))) As String
        Dim nombresDia = {"Lunes", "Martes", "Miercoles", "Jueves", "Viernes", "Sabado", "Domingo"}
        ' Materia prima del mes: Σ raciones × costo por ración, en decimal (sin Double).
        Dim materia As Decimal = 0D
        For Each it In items
            materia += EscalaU6.ADecimal(it.RacionesU6) * EscalaU6.ADecimal(it.CostoU6)
        Next
        Dim comensales = dias.Sum(Function(d) d.Comensales)

        Dim programacion As New JsonArray()
        For Each d In dias
            Dim itemsDia As New JsonArray()
            For Each it In items.Where(Function(x) x.Fecha = d.Fecha)
                itemsDia.Add(New JsonObject From {
                    {"categoria_id", JsonValue.Create(it.Linea)},
                    {"categoria_nombre", JsonValue.Create(it.Categoria)},
                    {"platillo", JsonValue.Create(it.Platillo)},
                    {"raciones", Numero(EscalaU6.ADecimal(it.RacionesU6))},
                    {"costo_unitario", Numero(EscalaU6.ADecimal(it.CostoU6))},
                    {"estado", Nothing}})
            Next
            programacion.Add(New JsonObject From {
                {"fecha", JsonValue.Create(d.Fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))},
                {"dia_semana", JsonValue.Create(nombresDia((CInt(d.Fecha.DayOfWeek) + 6) Mod 7))},
                {"costo_minuta_dia", JsonValue.Create(Math.Round(EscalaU6.ADecimal(d.CostoU6), 2))},
                {"comensales", JsonValue.Create(d.Comensales)},
                {"items", itemsDia}})
        Next

        Dim raiz As New JsonObject From {
            {"operacion", New JsonObject From {
                {"codigo", JsonValue.Create(contrato)},
                {"nombre", JsonValue.Create(nombreOperacion)},
                {"regimen", JsonValue.Create(regimen)},
                {"servicio", JsonValue.Create(servicio)}}},
            {"resumen_mes", New JsonObject From {
                {"planificado", New JsonObject From {
                    {"materia_prima", JsonValue.Create(Math.Round(materia, 2))},
                    {"raciones", JsonValue.Create(comensales)},
                    {"costo_bandeja", JsonValue.Create(If(comensales > 0, Math.Round(materia / comensales, 2), 0D))}}},
                {"realizado", Nothing}}},
            {"programacion_diaria", programacion}}
        Return raiz.ToJsonString(New JsonSerializerOptions With {.WriteIndented = True, .Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping})
    End Function

End Class
