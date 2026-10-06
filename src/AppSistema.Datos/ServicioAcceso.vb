Imports Npgsql
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Seguridad

''' <summary>Inicio de sesión, selección de operación y cambio de clave.</summary>
Public NotInheritable Class ServicioAcceso

    Private ReadOnly _cadena As String

    Public Sub New(cadenaConexion As String)
        If String.IsNullOrWhiteSpace(cadenaConexion) Then Throw New ArgumentException("Falta la cadena de conexion.", NameOf(cadenaConexion))
        _cadena = cadenaConexion
    End Sub

    ''' <summary>
    ''' Verifica credenciales. Errores: CREDENCIALES_INVALIDAS (genérico: no revela si el usuario existe),
    ''' USUARIO_BLOQUEADO. La sesión devuelta aún no tiene operación ni permisos.
    ''' </summary>
    Public Function IniciarSesion(empresaCodigo As String, login As String, clave As String) As SesionUsuario
        If String.IsNullOrWhiteSpace(empresaCodigo) OrElse String.IsNullOrWhiteSpace(login) OrElse String.IsNullOrEmpty(clave) Then
            Throw New ReglaNegocioException("CREDENCIALES_INVALIDAS", "Empresa, usuario y clave son obligatorios.")
        End If
        Try
            Dim usuarioId As Long, empresaId As Long, nombre As String = Nothing, hash As String = Nothing
            Dim activo As Boolean, bloqueado As Boolean, encontrado As Boolean
            Using u As New UnidadDeTrabajo(_cadena, Nothing, Nothing)
                Using cmd = u.Comando("SELECT usuario_id, empresa_id, nombre, password_hash, activo, " &
                                      "(bloqueado_hasta IS NOT NULL AND bloqueado_hasta > now()) AS bloqueado " &
                                      "FROM fn_datos_acceso(@e, @l)", "e", empresaCodigo.Trim(), "l", login.Trim())
                    Using rd = cmd.ExecuteReader()
                        If rd.Read() Then
                            encontrado = True
                            usuarioId = rd.GetInt64(0) : empresaId = rd.GetInt64(1) : nombre = rd.GetString(2)
                            hash = rd.GetString(3) : activo = rd.GetBoolean(4) : bloqueado = rd.GetBoolean(5)
                        End If
                    End Using
                End Using

                If Not encontrado Then
                    ClaveSegura.Verificar(clave, ClaveSegura.HashFicticio)
                    Throw New ReglaNegocioException("CREDENCIALES_INVALIDAS", "Empresa, usuario o clave incorrectos.")
                End If
                If bloqueado Then
                    Throw New ReglaNegocioException("USUARIO_BLOQUEADO", "Usuario bloqueado temporalmente por intentos fallidos. Intente mas tarde.")
                End If

                Dim correcto As Boolean = ClaveSegura.Verificar(clave, hash) AndAlso activo
                u.Ejecutar("SELECT fn_registrar_intento_acceso(@u, @ok)", "u", usuarioId, "ok", correcto)
                u.Confirmar()
                If Not correcto Then
                    Throw New ReglaNegocioException("CREDENCIALES_INVALIDAS", "Empresa, usuario o clave incorrectos.")
                End If
            End Using

            Using u As New UnidadDeTrabajo(_cadena, empresaId, usuarioId)
                ' El dueño del sistema entra a todas las operaciones activas; los demás, a las que tienen algún rol.
                ' to_jsonb: una base aún sin V020 (antes de actualizar) no tiene la columna; ahí nadie es dueño.
                Dim esDueno = CBool(u.Escalar("SELECT COALESCE((to_jsonb(us) ->> 'es_dueno')::boolean, false) FROM usuario us WHERE us.id = @u", "u", usuarioId))
                ' Con V021, las operaciones salen de los permisos efectivos (alcance por operación, zona o todas, y excepciones).
                Dim filtro = If(TieneSeguridadPorAlcance(u),
                    "o.id IN (SELECT fn_operaciones_usuario(@u))",
                    "(@d OR EXISTS (SELECT 1 FROM usuario_operacion_rol r WHERE r.operacion_id = o.id AND r.usuario_id = @u))")
                Dim operaciones = u.Consultar(
                    "SELECT DISTINCT o.id, o.codigo, o.nombre FROM operacion o WHERE o.activo = 1 AND " & filtro & " ORDER BY o.nombre",
                    Function(rd) New OperacionDisponible(rd.GetInt64(0), rd.GetString(1), rd.GetString(2)), "u", usuarioId, "d", esDueno)
                Return New SesionUsuario(usuarioId, login.Trim(), nombre, empresaId, empresaCodigo.Trim(), operaciones, Nothing, Nothing, esDueno)
            End Using
        Catch ex As PostgresException
            Throw ErroresBD.Traducir(ex)
        End Try
    End Function

    ''' <summary>Fija la operación de trabajo y carga los permisos efectivos del usuario en ella.</summary>
    Public Function SeleccionarOperacion(sesion As SesionUsuario, operacionId As Long) As SesionUsuario
        If sesion Is Nothing Then Throw New ArgumentNullException(NameOf(sesion))
        Dim op = sesion.Operaciones.FirstOrDefault(Function(o) o.Id = operacionId)
        If op Is Nothing Then Throw New ReglaNegocioException("SIN_PERMISO", "El usuario no tiene acceso a esa operacion.")
        If sesion.EsDueno Then Return sesion.ConOperacion(op, Permisos.Todos)
        Try
            Using u = UnidadDeTrabajo.ParaSesion(_cadena, sesion)
                If TieneSeguridadPorAlcance(u) Then
                    Return sesion.ConOperacion(op, u.Consultar("SELECT fn_permisos_usuario(@u, @o)", Function(rd) rd.GetString(0),
                                                               "u", sesion.UsuarioId, "o", operacionId))
                End If
                Dim permisos = u.Consultar(
                    "SELECT DISTINCT p.codigo FROM usuario_operacion_rol uor " &
                    "JOIN rol_permiso rp ON rp.empresa_id = uor.empresa_id AND rp.rol_id = uor.rol_id " &
                    "JOIN permiso p ON p.empresa_id = rp.empresa_id AND p.id = rp.permiso_id " &
                    "WHERE uor.usuario_id = @u AND uor.operacion_id = @o",
                    Function(rd) rd.GetString(0), "u", sesion.UsuarioId, "o", operacionId)
                Return sesion.ConOperacion(op, permisos)
            End Using
        Catch ex As PostgresException
            Throw ErroresBD.Traducir(ex)
        End Try
    End Function

    ''' <summary>True si la base ya tiene V021 (permisos efectivos en la base). Una base anterior, antes de actualizarse, no.</summary>
    Private Shared Function TieneSeguridadPorAlcance(u As UnidadDeTrabajo) As Boolean
        Return CBool(u.Escalar("SELECT to_regprocedure('fn_permisos_usuario(bigint,bigint)') IS NOT NULL"))
    End Function

    Public Sub CambiarClave(sesion As SesionUsuario, claveActual As String, claveNueva As String)
        If sesion Is Nothing Then Throw New ArgumentNullException(NameOf(sesion))
        PoliticaClave.Validar(claveNueva)
        Try
            Using u = UnidadDeTrabajo.ParaSesion(_cadena, sesion)
                Dim hash = CStr(u.Escalar("SELECT password_hash FROM usuario WHERE id = @u", "u", sesion.UsuarioId))
                If Not ClaveSegura.Verificar(claveActual, hash) Then
                    Throw New ReglaNegocioException("CREDENCIALES_INVALIDAS", "La clave actual no es correcta.")
                End If
                u.Ejecutar("UPDATE usuario SET password_hash = @h WHERE id = @u", "h", ClaveSegura.Crear(claveNueva), "u", sesion.UsuarioId)
                u.Confirmar()
            End Using
        Catch ex As PostgresException
            Throw ErroresBD.Traducir(ex)
        End Try
    End Sub

End Class
