Imports AppSistema.Dominio
Imports AppSistema.Dominio.Seguridad

''' <summary>Usuarios, roles por operación, operaciones y almacenes de la empresa de la sesión.</summary>
Public NotInheritable Class ServicioAdministracion
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    Public Function CrearUsuario(login As String, nombre As String, clave As String, operacionId As Long, rolCodigo As String) As Long
        If String.IsNullOrWhiteSpace(login) OrElse String.IsNullOrWhiteSpace(nombre) Then
            Throw New ReglaNegocioException("DATO_OBLIGATORIO", "Usuario y nombre son obligatorios.")
        End If
        PoliticaClave.Validar(clave)
        Dim hash = ClaveSegura.Crear(clave)
        Return EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u)
                Dim id = u.EscalarLong("INSERT INTO usuario(empresa_id, nombre, login, password_hash) VALUES (@e, @n, @l, @h) RETURNING id",
                                       "e", Sesion.EmpresaId, "n", nombre.Trim(), "l", login.Trim(), "h", hash)
                AsignarRolInterno(u, id, operacionId, rolCodigo)
                Return id
            End Function)
    End Function

    Public Sub AsignarRol(usuarioId As Long, operacionId As Long, rolCodigo As String)
        EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u)
                AsignarRolInterno(u, usuarioId, operacionId, rolCodigo)
                Return 0
            End Function)
    End Sub

    Private Sub AsignarRolInterno(u As UnidadDeTrabajo, usuarioId As Long, operacionId As Long, rolCodigo As String)
        Dim rolId = u.Escalar("SELECT id FROM rol WHERE codigo = @c", "c", rolCodigo)
        If rolId Is Nothing Then Throw New ReglaNegocioException("ROL_NO_ENCONTRADO", $"No existe el rol {rolCodigo}.")
        u.Ejecutar("INSERT INTO usuario_operacion_rol(empresa_id, usuario_id, operacion_id, rol_id) VALUES (@e, @u, @o, @r)",
                   "e", Sesion.EmpresaId, "u", usuarioId, "o", operacionId, "r", rolId)
    End Sub

    Public Sub DesactivarUsuario(usuarioId As Long)
        If usuarioId = Sesion.UsuarioId Then Throw New ReglaNegocioException("OPERACION_NO_PERMITIDA", "No puede desactivar su propio usuario.")
        EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u)
                If u.Ejecutar("UPDATE usuario SET activo = 0 WHERE id = @u", "u", usuarioId) = 0 Then
                    Throw New ReglaNegocioException("NO_ENCONTRADO", "El usuario no existe.")
                End If
                Return 0
            End Function)
    End Sub

    Public Function ListarUsuarios() As List(Of UsuarioResumen)
        Return EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u) u.Consultar(
                "SELECT us.id, us.login, us.nombre, us.activo = 1 AS activo, " &
                "COALESCE(string_agg(DISTINCT o.codigo || ':' || r.codigo, ', '), '') AS roles " &
                "FROM usuario us LEFT JOIN usuario_operacion_rol uor ON uor.empresa_id = us.empresa_id AND uor.usuario_id = us.id " &
                "LEFT JOIN rol r ON r.empresa_id = uor.empresa_id AND r.id = uor.rol_id " &
                "LEFT JOIN operacion o ON o.empresa_id = uor.empresa_id AND o.id = uor.operacion_id " &
                "GROUP BY us.id, us.login, us.nombre, us.activo ORDER BY us.login",
                Function(rd) New UsuarioResumen With {.Id = rd.GetInt64(0), .Login = rd.GetString(1), .Nombre = rd.GetString(2),
                                                     .Activo = rd.GetBoolean(3), .Roles = rd.GetString(4)}))
    End Function

    Public Function CrearOperacion(codigo As String, nombre As String) As Long
        Return EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u) u.EscalarLong("INSERT INTO operacion(empresa_id, codigo, nombre) VALUES (@e, @c, @n) RETURNING id",
                                      "e", Sesion.EmpresaId, "c", Requerido(codigo, "codigo"), "n", Requerido(nombre, "nombre")))
    End Function

    Public Function CrearAlmacen(operacionId As Long, codigo As String, nombre As String) As Long
        Return EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u) u.EscalarLong("INSERT INTO almacen(empresa_id, operacion_id, codigo, nombre) VALUES (@e, @o, @c, @n) RETURNING id",
                                      "e", Sesion.EmpresaId, "o", operacionId, "c", Requerido(codigo, "codigo"), "n", Requerido(nombre, "nombre")))
    End Function

    ''' <summary>Almacenes de la operación de la sesión (lo que puede usar el usuario en su trabajo diario).</summary>
    Public Function ListarAlmacenes() As List(Of AlmacenResumen)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar(
                "SELECT id, operacion_id, codigo, nombre FROM almacen WHERE operacion_id = @o AND activo = 1 ORDER BY codigo",
                Function(rd) New AlmacenResumen With {.Id = rd.GetInt64(0), .OperacionId = rd.GetInt64(1), .Codigo = rd.GetString(2), .Nombre = rd.GetString(3)},
                "o", Sesion.OperacionId))
    End Function

    Friend Shared Function Requerido(valor As String, campo As String) As String
        If String.IsNullOrWhiteSpace(valor) Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", $"El campo {campo} es obligatorio.")
        Return valor.Trim()
    End Function

End Class
