Imports System.Diagnostics
Imports System.Globalization
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports Npgsql
Imports AppSistema.Dominio

Public NotInheritable Class ResultadoEnvio
    Public Property Aplicados As Integer
    Public Property Retenidos As Integer
    Public Property Duplicados As Integer
    Public Property Conflictos As Integer
    ''' <summary>Eventos que siguen en la cola de la sede (por falta de conexión, espera o límite).</summary>
    Public Property Pendientes As Long
    ''' <summary>Motivo por el que se detuvo el envío (sin conexión, credencial rechazada); Nothing si terminó.</summary>
    Public Property Problema As String

    Public ReadOnly Property Enviados As Integer
        Get
            Return Aplicados + Retenidos + Duplicados
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return $"{Aplicados} aplicados, {Retenidos} retenidos en la central, {Duplicados} ya estaban, {Conflictos} en conflicto; " &
               $"{Pendientes} pendientes en la sede" & If(Problema Is Nothing, "", " — " & Problema)
    End Function
End Class

Public NotInheritable Class EstadoColaDto
    Public Property Origen As String
    Public Property Pendientes As Long
    Public Property ConError As Long
    Public Property EnConflicto As Long
    Public Property Enviados As Long
    Public Property UltimoEnvio As DateTimeOffset?
    Public Property UltimoError As String
End Class

Public NotInheritable Class SedeCentralDto
    Public Property Sede As String
    Public Property Nombre As String
    Public Property Activa As Boolean
    Public Property UltimaSincronizacion As DateTimeOffset?
    Public Property UltimaSecuenciaAplicada As Long
    Public Property Retenidos As Long
    Public Property Conflictos As Long
    Public Property Documentos As Long
    Public Property ValorStockU6 As Long
    Public Property MotivoRetencion As String
End Class

''' <summary>
''' Continuidad (etapa 8). Cada sede trabaja con su propio servidor; lo confirmado queda en la cola de salida en la
''' misma transacción. El agente envía la cola en orden a la central, que aplica sin duplicar, retiene lo que llega
''' fuera de orden y rechaza credenciales inválidas. Se ejecuta con la conexión del PROPIETARIO de la sede (no de la aplicación).
''' </summary>
Public NotInheritable Class ServicioContinuidad

    Private ReadOnly _cadenaPropietario As String

    Public Sub New(cadenaPropietario As String)
        If String.IsNullOrWhiteSpace(cadenaPropietario) Then Throw New ArgumentException("Falta la cadena de conexion.", NameOf(cadenaPropietario))
        _cadenaPropietario = cadenaPropietario
    End Sub

    Private Function Unidad() As UnidadDeTrabajo
        Return New UnidadDeTrabajo(_cadenaPropietario, Nothing, Nothing)
    End Function

    Private Shared Function EmpresaId(u As UnidadDeTrabajo, empresaCodigo As String) As Long
        Dim id = u.Escalar("SELECT id FROM empresa WHERE codigo = @c", "c", empresaCodigo)
        If id Is Nothing Then Throw New ReglaNegocioException("DATO_INVALIDO", $"La empresa '{empresaCodigo}' no existe en esta base.")
        Return Convert.ToInt64(id, CultureInfo.InvariantCulture)
    End Function

    Private Shared Function ValidarCodigo(codigo As String) As String
        codigo = If(codigo, "").Trim()
        If codigo.Length = 0 OrElse codigo.Length > 30 OrElse Not codigo.All(Function(c) Char.IsLetterOrDigit(c) OrElse c = "_"c OrElse c = "-"c) Then
            Throw New ReglaNegocioException("DATO_INVALIDO", "El codigo de sede admite letras, numeros, guion y guion bajo (hasta 30).")
        End If
        Return codigo
    End Function

    ' ---------- Sede ----------

    ''' <summary>Activa la sincronización de la sede y anota en la cola la historia ya confirmada. Devuelve cuántos eventos anotó.</summary>
    Public Function ConfigurarOrigen(empresaCodigo As String, sedeCodigo As String) As Long
        sedeCodigo = ValidarCodigo(sedeCodigo)
        Try
            Using u = Unidad()
                Dim n = u.EscalarLong("SELECT fn_configurar_origen(@e, @s)", "e", EmpresaId(u, empresaCodigo), "s", sedeCodigo)
                u.Confirmar()
                Return n
            End Using
        Catch ex As PostgresException
            Throw ErroresBD.Traducir(ex)
        End Try
    End Function

    Public Function EstadoCola(empresaCodigo As String) As EstadoColaDto
        Using u = Unidad()
            Dim e = EmpresaId(u, empresaCodigo)
            Dim r = u.Consultar(
                "SELECT (SELECT codigo FROM origen_sincronizacion WHERE empresa_id = @e) AS origen, " &
                "count(*) FILTER (WHERE estado = 'pendiente') AS pendientes, count(*) FILTER (WHERE estado = 'error') AS errores, " &
                "count(*) FILTER (WHERE estado = 'conflicto') AS conflictos, count(*) FILTER (WHERE estado = 'enviado') AS enviados, " &
                "max(enviado_en) AS ultimo, (SELECT ultimo_error FROM sincronizacion_evento WHERE empresa_id = @e AND estado = 'error' ORDER BY secuencia LIMIT 1) AS error " &
                "FROM sincronizacion_evento WHERE empresa_id = @e",
                Function(rd) New EstadoColaDto With {
                    .Origen = rd.TextoONada("origen"), .Pendientes = rd.Largo("pendientes"), .ConError = rd.Largo("errores"),
                    .EnConflicto = rd.Largo("conflictos"), .Enviados = rd.Largo("enviados"),
                    .UltimoEnvio = If(rd.IsDBNull(rd.GetOrdinal("ultimo")), CType(Nothing, DateTimeOffset?), New DateTimeOffset(rd.GetDateTime(rd.GetOrdinal("ultimo")))),
                    .UltimoError = rd.TextoONada("error")}, "e", e)
            Return r.Single()
        End Using
    End Function

    Private NotInheritable Class EventoCola
        Public Id As Long, Uuid As String, Secuencia As Long, Tipo As String, Version As Integer
        Public Referencia As String, DependeDe As String, Payload As String, Intentos As Long
    End Class

    ''' <summary>
    ''' Envía la cola en orden de secuencia. Sin conexión: anota el error, programa el reintento (espera creciente) y se
    ''' detiene sin perder el orden. Un acuse perdido no duplica nada: la central responde DUPLICADO al reenvío.
    ''' </summary>
    ''' <param name="cadenaCentral">Conexión a la central con un usuario del rol app_sincronizacion.</param>
    ''' <param name="ignorarEspera">Reintenta ya, sin esperar el próximo intento programado.</param>
    Public Function Enviar(empresaCodigo As String, cadenaCentral As String, credencial As String,
                           Optional maximo As Integer = 1000, Optional ignorarEspera As Boolean = False) As ResultadoEnvio
        Dim r As New ResultadoEnvio()
        Dim e As Long, sede As String
        Dim cola As List(Of EventoCola)
        Using u = Unidad()
            e = EmpresaId(u, empresaCodigo)
            sede = CStr(u.Escalar("SELECT codigo FROM origen_sincronizacion WHERE empresa_id = @e", "e", e))
            If sede Is Nothing Then Throw New ReglaNegocioException("ORIGEN_NO_CONFIGURADO", "Configure primero la sede (configurar-sede).")
            ' El orden manda: si el primero de la cola espera su reintento, no se adelanta ninguno detrás de él.
            Dim espera = u.Escalar("SELECT proximo_intento > now() FROM sincronizacion_evento WHERE empresa_id = @e AND estado IN ('pendiente','error') " &
                                   "ORDER BY secuencia LIMIT 1", "e", e)
            If espera IsNot Nothing AndAlso CBool(espera) AndAlso Not ignorarEspera Then
                r.Problema = "En espera del proximo reintento."
                r.Pendientes = ContarPendientes(e)
                Return r
            End If
            cola = u.Consultar(
                "SELECT id, uuid, secuencia, tipo, version_payload, referencia, depende_de, payload_json, intentos FROM sincronizacion_evento " &
                "WHERE empresa_id = @e AND estado IN ('pendiente','error') ORDER BY secuencia LIMIT @m",
                Function(rd) New EventoCola With {
                    .Id = rd.Largo("id"), .Uuid = rd.Texto("uuid"), .Secuencia = rd.Largo("secuencia"), .Tipo = rd.Texto("tipo"),
                    .Version = rd.GetInt32(rd.GetOrdinal("version_payload")), .Referencia = rd.TextoONada("referencia"),
                    .DependeDe = rd.TextoONada("depende_de"), .Payload = rd.Texto("payload_json"), .Intentos = rd.Largo("intentos")},
                "e", e, "m", maximo)
        End Using

        If cola.Count > 0 Then
            Dim central As NpgsqlConnection = Nothing
            Try
                central = New NpgsqlConnection(cadenaCentral)
                central.Open()
            Catch ex As Exception When TypeOf ex Is NpgsqlException OrElse TypeOf ex Is TimeoutException OrElse TypeOf ex Is Net.Sockets.SocketException
                central?.Dispose()
                RegistrarFallo(cola(0), "SIN_CONEXION: " & ex.Message)
                r.Problema = "Sin conexion con la central; se reintentara."
                r.Pendientes = ContarPendientes(e)
                Return r
            End Try
            Using central
                For Each ev In cola
                    Dim respuesta As String
                    Try
                        Using cmd As New NpgsqlCommand("SELECT fn_recibir_evento(@em, @se, @cr, @uu, @sq, @ti, @ve, @re, @de, @pa)", central)
                            cmd.Parameters.AddWithValue("em", empresaCodigo)
                            cmd.Parameters.AddWithValue("se", sede)
                            cmd.Parameters.AddWithValue("cr", If(CObj(credencial), DBNull.Value))
                            cmd.Parameters.AddWithValue("uu", ev.Uuid)
                            cmd.Parameters.AddWithValue("sq", ev.Secuencia)
                            cmd.Parameters.AddWithValue("ti", ev.Tipo)
                            cmd.Parameters.AddWithValue("ve", ev.Version)
                            cmd.Parameters.AddWithValue("re", If(CObj(ev.Referencia), DBNull.Value))
                            cmd.Parameters.AddWithValue("de", If(CObj(ev.DependeDe), DBNull.Value))
                            cmd.Parameters.AddWithValue("pa", ev.Payload)
                            respuesta = CStr(cmd.ExecuteScalar())
                        End Using
                    Catch ex As Exception When TypeOf ex Is NpgsqlException OrElse TypeOf ex Is TimeoutException OrElse TypeOf ex Is IOException
                        ' Pudo aplicarse sin que llegara el acuse: el reenvío posterior responderá DUPLICADO.
                        RegistrarFallo(ev, "SIN_CONEXION: " & ex.Message)
                        r.Problema = "Se perdio la conexion con la central; se reintentara."
                        Exit For
                    End Try
                    Select Case respuesta
                        Case "APLICADO" : r.Aplicados += 1 : MarcarEnviado(ev, "enviado")
                        Case "RETENIDO" : r.Retenidos += 1 : MarcarEnviado(ev, "enviado")
                        Case "DUPLICADO" : r.Duplicados += 1 : MarcarEnviado(ev, "enviado")
                        Case "CONFLICTO" : r.Conflictos += 1 : MarcarEnviado(ev, "conflicto")
                        Case Else
                            RegistrarFallo(ev, "RECHAZADO: la central no reconoce la sede o su credencial")
                            r.Problema = "La central rechazo la credencial de la sede."
                            Exit For
                    End Select
                Next
            End Using
        End If
        r.Pendientes = ContarPendientes(e)
        Return r
    End Function

    Private Function ContarPendientes(empresaId As Long) As Long
        Using u = Unidad()
            Return u.EscalarLong("SELECT count(*) FROM sincronizacion_evento WHERE empresa_id = @e AND estado IN ('pendiente','error')", "e", empresaId)
        End Using
    End Function

    Private Sub MarcarEnviado(ev As EventoCola, estado As String)
        Using u = Unidad()
            u.Ejecutar("UPDATE sincronizacion_evento SET estado = @s, enviado_en = now(), intentos = intentos + 1, ultimo_error = NULL WHERE id = @i",
                       "s", estado, "i", ev.Id)
            u.Confirmar()
        End Using
    End Sub

    ''' <summary>Reintento con espera creciente: 1, 2, 4… hasta 60 minutos.</summary>
    Private Sub RegistrarFallo(ev As EventoCola, mensaje As String)
        Dim minutos = CInt(Math.Min(60, Math.Pow(2, Math.Min(ev.Intentos, 6))))
        Using u = Unidad()
            u.Ejecutar("UPDATE sincronizacion_evento SET estado = 'error', intentos = intentos + 1, ultimo_error = @m, " &
                       "proximo_intento = now() + make_interval(mins => @n) WHERE id = @i",
                       "m", Left(mensaje, 500), "n", minutos, "i", ev.Id)
            u.Confirmar()
        End Using
    End Sub

    ' ---------- Central ----------

    ''' <summary>
    ''' Registra (o renueva) la credencial de una sede en la central. La credencial se muestra una sola vez; en la base
    ''' solo queda su hash. Renovarla invalida la anterior.
    ''' </summary>
    Public Function RegistrarSede(empresaCodigo As String, sedeCodigo As String, nombre As String) As String
        sedeCodigo = ValidarCodigo(sedeCodigo)
        Dim credencial = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd("="c).Replace("+"c, "-"c).Replace("/"c, "_"c)
        Dim hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(credencial))).ToLowerInvariant()
        Try
            Using u = Unidad()
                u.Ejecutar("INSERT INTO sede_central(empresa_id, codigo, nombre, credencial_sha256) VALUES (@e, @c, @n, @h) " &
                           "ON CONFLICT (empresa_id, codigo) DO UPDATE SET credencial_sha256 = EXCLUDED.credencial_sha256, nombre = EXCLUDED.nombre, activa = true",
                           "e", EmpresaId(u, empresaCodigo), "c", sedeCodigo, "n", ServicioAdministracion.Requerido(nombre, "nombre de la sede"), "h", hash)
                u.Confirmar()
            End Using
        Catch ex As PostgresException
            Throw ErroresBD.Traducir(ex)
        End Try
        Return credencial
    End Function

    Public Sub DesactivarSede(empresaCodigo As String, sedeCodigo As String)
        Using u = Unidad()
            If u.Ejecutar("UPDATE sede_central SET activa = false WHERE empresa_id = @e AND codigo = @c", "e", EmpresaId(u, empresaCodigo), "c", sedeCodigo) = 0 Then
                Throw New ReglaNegocioException("DATO_INVALIDO", $"La sede '{sedeCodigo}' no esta registrada.")
            End If
            u.Confirmar()
        End Using
    End Sub

    ''' <summary>Reporte central: por sede, última sincronización, lo retenido o en conflicto y el stock valorizado recibido.</summary>
    Public Function ResumenCentral(empresaCodigo As String) As List(Of SedeCentralDto)
        Using u = Unidad()
            Return u.Consultar(
                "SELECT s.codigo, s.nombre, s.activa, s.ultima_sincronizacion, s.ultima_secuencia_aplicada, " &
                "(SELECT count(*) FROM evento_recibido r WHERE r.sede_id = s.id AND r.estado = 'retenido') AS retenidos, " &
                "(SELECT count(*) FROM evento_recibido r WHERE r.sede_id = s.id AND r.estado = 'conflicto') AS conflictos, " &
                "(SELECT count(*) FROM central_documento d WHERE d.sede_id = s.id) AS documentos, " &
                "(SELECT COALESCE(SUM(m.signo * m.valor_u6), 0) FROM central_movimiento m WHERE m.sede_id = s.id)::bigint AS valor, " &
                "(SELECT r.motivo FROM evento_recibido r WHERE r.sede_id = s.id AND r.estado <> 'aplicado' ORDER BY r.secuencia LIMIT 1) AS motivo " &
                "FROM sede_central s WHERE s.empresa_id = @e ORDER BY s.codigo",
                Function(rd) New SedeCentralDto With {
                    .Sede = rd.Texto("codigo"), .Nombre = rd.Texto("nombre"), .Activa = rd.GetBoolean(rd.GetOrdinal("activa")),
                    .UltimaSincronizacion = If(rd.IsDBNull(rd.GetOrdinal("ultima_sincronizacion")), CType(Nothing, DateTimeOffset?),
                                               New DateTimeOffset(rd.GetDateTime(rd.GetOrdinal("ultima_sincronizacion")))),
                    .UltimaSecuenciaAplicada = rd.Largo("ultima_secuencia_aplicada"), .Retenidos = rd.Largo("retenidos"),
                    .Conflictos = rd.Largo("conflictos"), .Documentos = rd.Largo("documentos"), .ValorStockU6 = rd.Largo("valor"),
                    .MotivoRetencion = rd.TextoONada("motivo")},
                "e", EmpresaId(u, empresaCodigo))
        End Using
    End Function

    ' ---------- Respaldo, restauración y conciliación ----------

    ''' <summary>
    ''' Fotografía para conciliar: filas por tabla, saldos (cantidad y valor), libro de movimientos y filas sin conciliar.
    ''' Dos bases con la misma fotografía tienen los mismos recuentos, saldos y referencias.
    ''' </summary>
    Public Function Instantanea() As SortedDictionary(Of String, String)
        Using cn As New NpgsqlConnection(_cadenaPropietario)
            cn.Open()
            Using tx = cn.BeginTransaction(Data.IsolationLevel.RepeatableRead)
                Return Instantanea(cn, tx)
            End Using
        End Using
    End Function

    Private Shared Function Instantanea(cn As NpgsqlConnection, tx As NpgsqlTransaction) As SortedDictionary(Of String, String)
        Dim r As New SortedDictionary(Of String, String)(StringComparer.Ordinal)
        Dim tablas As New List(Of String)
        Using cmd As New NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE' ORDER BY 1", cn, tx),
              rd = cmd.ExecuteReader()
            While rd.Read()
                tablas.Add(rd.GetString(0))
            End While
        End Using
        For Each t In tablas
            Using cmd As New NpgsqlCommand($"SELECT count(*) FROM ""{t.Replace("""", """""")}""", cn, tx)
                r("filas." & t) = Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
            End Using
        Next
        Dim controles = New Dictionary(Of String, String) From {
            {"saldo.cantidad_u6", "SELECT COALESCE(SUM(cantidad_base_u6),0) FROM saldo_stock"},
            {"saldo.valor_u6", "SELECT COALESCE(SUM(valor_u6),0) FROM saldo_stock"},
            {"libro.cantidad_u6", "SELECT COALESCE(SUM(signo*cantidad_base_u6),0) FROM movimiento_stock"},
            {"libro.valor_u6", "SELECT COALESCE(SUM(signo*valor_u6),0) FROM movimiento_stock"},
            {"libro.ultimo_id", "SELECT COALESCE(MAX(id),0) FROM movimiento_stock"},
            {"conciliacion.filas_sin_conciliar", "SELECT count(*) FROM v_conciliacion_saldo"},
            {"documentos.huella", "SELECT COALESCE(md5(string_agg(id || ':' || numero || ':' || estado || ':' || COALESCE(documento_origen_id::text, ''), ',' ORDER BY id)), '') FROM documento_stock"},
            {"migraciones", "SELECT string_agg(version, ',' ORDER BY version) FROM esquema_migracion"}}
        For Each c In controles
            Using cmd As New NpgsqlCommand(c.Value, cn, tx)
                r(c.Key) = Convert.ToString(cmd.ExecuteScalar(), CultureInfo.InvariantCulture)
            End Using
        Next
        Return r
    End Function

    Public Shared Function TextoInstantanea(i As SortedDictionary(Of String, String)) As String
        Return String.Join(vbLf, i.Select(Function(kv) kv.Key & "=" & kv.Value)) & vbLf
    End Function

    Public Shared Function LeerInstantanea(texto As String) As SortedDictionary(Of String, String)
        Dim r As New SortedDictionary(Of String, String)(StringComparer.Ordinal)
        For Each linea In texto.Split({vbLf, vbCr}, StringSplitOptions.RemoveEmptyEntries)
            Dim i = linea.IndexOf("="c)
            If i > 0 Then r(linea.Substring(0, i)) = linea.Substring(i + 1)
        Next
        Return r
    End Function

    ''' <summary>Diferencias entre dos fotografías (vacío = conciliado).</summary>
    Public Shared Function Comparar(esperada As SortedDictionary(Of String, String), obtenida As SortedDictionary(Of String, String)) As List(Of String)
        Dim d As New List(Of String)
        For Each k In esperada.Keys.Union(obtenida.Keys).OrderBy(Function(x) x, StringComparer.Ordinal)
            Dim a As String = Nothing, b As String = Nothing
            esperada.TryGetValue(k, a)
            obtenida.TryGetValue(k, b)
            If a <> b Then d.Add($"{k}: respaldo {If(a, "(no existe)")}, restaurada {If(b, "(no existe)")}")
        Next
        Return d
    End Function

    ''' <summary>
    ''' Respaldo consistente: pg_dump usa la MISMA fotografía de la transacción que calcula la conciliación, así el archivo
    ''' .conciliacion describe exactamente lo respaldado aunque la sede siga trabajando.
    ''' </summary>
    Public Function Respaldar(archivo As String) As SortedDictionary(Of String, String)
        Using cn As New NpgsqlConnection(_cadenaPropietario)
            cn.Open()
            Using tx = cn.BeginTransaction(Data.IsolationLevel.RepeatableRead)
                Dim snapshot As String
                Using cmd As New NpgsqlCommand("SELECT pg_export_snapshot()", cn, tx)
                    snapshot = CStr(cmd.ExecuteScalar())
                End Using
                Dim foto = Instantanea(cn, tx)
                EjecutarHerramienta("pg_dump", $"--format=custom --snapshot={snapshot} --file=""{archivo}"" " & ArgumentosConexion(_cadenaPropietario, incluirBase:=True))
                File.WriteAllText(archivo & ".conciliacion", TextoInstantanea(foto), New UTF8Encoding(False))
                Return foto
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Restaura un respaldo en una base VACÍA y la concilia con el archivo .conciliacion. Devuelve las diferencias (vacío = conciliada).
    ''' </summary>
    Public Function Restaurar(archivo As String) As List(Of String)
        If Not File.Exists(archivo) Then Throw New ReglaNegocioException("DATO_INVALIDO", "No existe el archivo de respaldo.")
        If Not File.Exists(archivo & ".conciliacion") Then Throw New ReglaNegocioException("DATO_INVALIDO", "Falta el archivo de conciliacion del respaldo (" & Path.GetFileName(archivo) & ".conciliacion).")
        Using cn As New NpgsqlConnection(_cadenaPropietario)
            cn.Open()
            Using cmd As New NpgsqlCommand("SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'", cn)
                If Convert.ToInt64(cmd.ExecuteScalar(), CultureInfo.InvariantCulture) > 0 Then
                    Throw New ReglaNegocioException("BASE_NO_VACIA", "La restauracion se hace sobre una base nueva y vacia; no se sobrescribe una base en uso.")
                End If
            End Using
            ' Los roles son del servidor, no de la base: se crean si es un servidor nuevo.
            Using cmd As New NpgsqlCommand(
                "DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_stock') THEN CREATE ROLE app_stock NOLOGIN; END IF; " &
                "IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_sincronizacion') THEN CREATE ROLE app_sincronizacion NOLOGIN; END IF; END $$", cn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
        EjecutarHerramienta("pg_restore", $"--exit-on-error --no-owner --role=""{New NpgsqlConnectionStringBuilder(_cadenaPropietario).Username}"" " &
                                          ArgumentosConexion(_cadenaPropietario, incluirBase:=False) &
                                          $" --dbname=""{New NpgsqlConnectionStringBuilder(_cadenaPropietario).Database}"" ""{archivo}""")
        Return Comparar(LeerInstantanea(File.ReadAllText(archivo & ".conciliacion")), Instantanea())
    End Function

    Private Shared Function ArgumentosConexion(cadena As String, incluirBase As Boolean) As String
        Dim b As New NpgsqlConnectionStringBuilder(cadena)
        Dim a As New StringBuilder()
        If Not String.IsNullOrEmpty(b.Host) Then a.Append($"--host=""{b.Host}"" ")
        If b.Port <> 0 Then a.Append($"--port={b.Port} ")
        If Not String.IsNullOrEmpty(b.Username) Then a.Append($"--username=""{b.Username}"" ")
        a.Append("--no-password ")
        If incluirBase Then a.Append($"--dbname=""{b.Database}""")
        Return a.ToString().TrimEnd()
    End Function

    ''' <summary>Ejecuta pg_dump/pg_restore (carpeta en APPSISTEMA_PG_BIN o en el PATH). La clave va por variable de entorno.</summary>
    Private Sub EjecutarHerramienta(nombre As String, argumentos As String)
        Dim carpeta = Environment.GetEnvironmentVariable("APPSISTEMA_PG_BIN")
        Dim exe = If(String.IsNullOrWhiteSpace(carpeta), nombre, Path.Combine(carpeta, nombre))
        Dim psi As New ProcessStartInfo(exe, argumentos) With {.UseShellExecute = False, .RedirectStandardError = True, .RedirectStandardOutput = True}
        Dim clave = New NpgsqlConnectionStringBuilder(_cadenaPropietario).Password
        If Not String.IsNullOrEmpty(clave) Then psi.Environment("PGPASSWORD") = clave
        Dim p As Process
        Try
            p = Process.Start(psi)
        Catch ex As ComponentModel.Win32Exception
            Throw New ReglaNegocioException("HERRAMIENTA_NO_ENCONTRADA", $"No se encontro {nombre}. Indique su carpeta en APPSISTEMA_PG_BIN.")
        End Try
        Using p
            Dim salidaError = p.StandardError.ReadToEndAsync()
            p.StandardOutput.ReadToEnd()
            p.WaitForExit()
            If p.ExitCode <> 0 Then Throw New ReglaNegocioException("RESPALDO_FALLIDO", $"{nombre} termino con error: {salidaError.Result.Trim()}")
        End Using
    End Sub

End Class
