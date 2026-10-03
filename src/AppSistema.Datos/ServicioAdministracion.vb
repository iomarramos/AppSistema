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
        ' Nadie da más de lo que tiene: los permisos del rol deben estar entre los de quien asigna en esa operación.
        Dim delRol = u.Consultar("SELECT p.codigo FROM rol_permiso rp JOIN permiso p ON p.id = rp.permiso_id WHERE rp.rol_id = @r",
                                 Function(rd) rd.GetString(0), "r", rolId)
        ExigirSinEscalada(u, delRol, operacionId)
        u.Ejecutar("INSERT INTO usuario_operacion_rol(empresa_id, usuario_id, operacion_id, rol_id) VALUES (@e, @u, @o, @r)",
                   "e", Sesion.EmpresaId, "u", usuarioId, "o", operacionId, "r", rolId)
    End Sub

    ' ---------- Roles a medida (accesos avanzados) ----------

    Public Function ListarRoles() As List(Of RolDto)
        Return EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u) u.Consultar(
                "SELECT r.codigo, r.nombre, COALESCE(array_agg(p.codigo ORDER BY p.codigo) FILTER (WHERE p.codigo IS NOT NULL), '{}') " &
                "FROM rol r LEFT JOIN rol_permiso rp ON rp.empresa_id = r.empresa_id AND rp.rol_id = r.id " &
                "LEFT JOIN permiso p ON p.empresa_id = rp.empresa_id AND p.id = rp.permiso_id GROUP BY r.codigo, r.nombre ORDER BY r.codigo",
                Function(rd) New RolDto With {.Codigo = rd.GetString(0), .Nombre = rd.GetString(1), .Permisos = CType(rd.GetValue(2), String()).ToList(),
                                              .EsBase = RolesBase.EsRolBase(rd.GetString(0))}))
    End Function

    ''' <summary>Crea un rol propio de la empresa o cambia sus permisos. Los roles base no se modifican aquí.</summary>
    Public Sub GuardarRol(codigo As String, nombre As String, codigosPermiso As IEnumerable(Of String))
        codigo = Requerido(codigo, "codigo").ToUpperInvariant()
        If RolesBase.EsRolBase(codigo) Then Throw New ReglaNegocioException("ROL_BASE", $"El rol {codigo} es del sistema; cree un rol propio con otro codigo.")
        Dim lista = If(codigosPermiso, Enumerable.Empty(Of String)()).Distinct().ToArray()
        If lista.Length = 0 Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", "Elija al menos un permiso.")
        Dim desconocido = lista.FirstOrDefault(Function(p) Not Permisos.Todos.Contains(p))
        If desconocido IsNot Nothing Then Throw New ReglaNegocioException("DATO_INVALIDO", $"Permiso desconocido: {desconocido}.")
        EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u)
                ExigirSinEscalada(u, lista, Sesion.OperacionId.Value)
                Dim id = u.EscalarLong("INSERT INTO rol(empresa_id, codigo, nombre) VALUES (@e, @c, @n) " &
                                       "ON CONFLICT (empresa_id, codigo) DO UPDATE SET nombre = EXCLUDED.nombre RETURNING id",
                                       "e", Sesion.EmpresaId, "c", codigo, "n", Requerido(nombre, "nombre"))
                u.Ejecutar("DELETE FROM rol_permiso WHERE rol_id = @r AND permiso_id NOT IN (SELECT id FROM permiso WHERE codigo = ANY(@ps))", "r", id, "ps", lista)
                u.Ejecutar("INSERT INTO rol_permiso(empresa_id, rol_id, permiso_id) SELECT @e, @r, id FROM permiso WHERE codigo = ANY(@ps) " &
                           "ON CONFLICT (empresa_id, rol_id, permiso_id) DO NOTHING", "e", Sesion.EmpresaId, "r", id, "ps", lista)
                Return 0
            End Function)
    End Sub

    ''' <summary>Quita un rol de un usuario en una operación. Siempre debe quedar al menos un administrador activo.</summary>
    Public Sub QuitarRol(usuarioId As Long, operacionId As Long, rolCodigo As String)
        EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u)
                If u.Ejecutar("DELETE FROM usuario_operacion_rol WHERE usuario_id = @u AND operacion_id = @o AND rol_id = (SELECT id FROM rol WHERE codigo = @c)",
                              "u", usuarioId, "o", operacionId, "c", rolCodigo) = 0 Then
                    Throw New ReglaNegocioException("NO_ENCONTRADO", "El usuario no tiene ese rol en la operacion.")
                End If
                ExigirAdministrador(u)
                Return 0
            End Function)
    End Sub

    ''' <summary>
    ''' Privilegios hasta donde decide el dueño: quien asigna solo puede dar permisos que él mismo tiene en esa operación.
    ''' El dueño del sistema no tiene ese límite.
    ''' </summary>
    Private Sub ExigirSinEscalada(u As UnidadDeTrabajo, permisosPedidos As IEnumerable(Of String), operacionId As Long)
        If Sesion.EsDueno Then Return
        Dim propios = New HashSet(Of String)(u.Consultar(
            "SELECT DISTINCT p.codigo FROM usuario_operacion_rol uor JOIN rol_permiso rp ON rp.rol_id = uor.rol_id " &
            "JOIN permiso p ON p.id = rp.permiso_id WHERE uor.usuario_id = @u AND uor.operacion_id = @o",
            Function(rd) rd.GetString(0), "u", Sesion.UsuarioId, "o", operacionId))
        Dim faltan = permisosPedidos.Where(Function(p) Not propios.Contains(p)).Distinct().ToList()
        If faltan.Count > 0 Then
            Throw New ReglaNegocioException("ESCALADA_NO_PERMITIDA",
                $"No puede dar permisos que usted no tiene en esa operacion: {String.Join(", ", faltan.Select(AddressOf Permisos.Descripcion))}.")
        End If
    End Sub

    ''' <summary>Solo el dueño del sistema otorga o quita el rango de dueño (la base también lo exige, V020).</summary>
    Public Sub MarcarDueno(usuarioId As Long, esDueno As Boolean)
        If Not Sesion.EsDueno Then Throw New ReglaNegocioException("SOLO_DUENO", "Solo el dueno del sistema puede otorgar o quitar ese rango.")
        If usuarioId = Sesion.UsuarioId AndAlso Not esDueno Then
            Throw New ReglaNegocioException("OPERACION_NO_PERMITIDA", "No puede quitarse a si mismo el rango de dueno.")
        End If
        EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u)
                If u.Ejecutar("UPDATE usuario SET es_dueno = @d WHERE id = @u", "d", esDueno, "u", usuarioId) = 0 Then
                    Throw New ReglaNegocioException("NO_ENCONTRADO", "El usuario no existe.")
                End If
                Return 0
            End Function)
    End Sub

    ''' <summary>Hasta qué nivel llega cada persona en cada módulo y operación (el dueño, en todas).</summary>
    Public Function AccesosPorModulo() As List(Of AccesoModuloDto)
        Return EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u)
                Dim filas = u.Consultar(
                    "SELECT us.login, us.nombre, o.codigo || ' - ' || o.nombre, us.es_dueno, " &
                    "       COALESCE(string_agg(DISTINCT r.codigo, ', '), ''), COALESCE(array_agg(DISTINCT p.codigo) FILTER (WHERE p.codigo IS NOT NULL), '{}') " &
                    "FROM usuario us CROSS JOIN operacion o " &
                    "LEFT JOIN usuario_operacion_rol uor ON uor.usuario_id = us.id AND uor.operacion_id = o.id " &
                    "LEFT JOIN rol r ON r.id = uor.rol_id LEFT JOIN rol_permiso rp ON rp.rol_id = r.id LEFT JOIN permiso p ON p.id = rp.permiso_id " &
                    "WHERE us.activo = 1 AND o.activo = 1 GROUP BY us.login, us.nombre, o.codigo, o.nombre, us.es_dueno " &
                    "HAVING us.es_dueno OR count(uor.id) > 0 ORDER BY us.login, o.codigo",
                    Function(rd) (Login:=rd.GetString(0), Nombre:=rd.GetString(1), Operacion:=rd.GetString(2), Dueno:=rd.GetBoolean(3),
                                  Roles:=rd.GetString(4), Permisos:=DirectCast(rd.GetValue(5), String())))
                Return filas.Select(Function(f)
                                        Dim ps As IEnumerable(Of String) = If(f.Dueno, Permisos.Todos, f.Permisos)
                                        Dim n = Function(m As String) Modulos.NivelEnModulo(ps, m)
                                        Return New AccesoModuloDto With {
                                            .Login = f.Login, .Nombre = f.Nombre, .Operacion = f.Operacion, .Roles = If(f.Dueno, "DUENO DEL SISTEMA", f.Roles),
                                            .Catalogo = n(Modulos.Catalogo), .Planificacion = n(Modulos.Planificacion), .Produccion = n(Modulos.Produccion),
                                            .Abastecimiento = n(Modulos.Abastecimiento), .Almacen = n(Modulos.Almacen), .Inventario = n(Modulos.Inventario),
                                            .Cierres = n(Modulos.Cierres), .Resultados = n(Modulos.Resultados), .Administracion = n(Modulos.Administracion)}
                                    End Function).ToList()
            End Function)
    End Function

    ''' <summary>Evita dejar la empresa sin nadie que pueda administrar usuarios (el dueño cuenta).</summary>
    Private Shared Sub ExigirAdministrador(u As UnidadDeTrabajo)
        If u.Escalar("SELECT 1 FROM usuario WHERE es_dueno AND activo = 1 LIMIT 1") IsNot Nothing Then Return
        If u.Escalar("SELECT 1 FROM usuario_operacion_rol uor JOIN usuario us ON us.id = uor.usuario_id AND us.activo = 1 " &
                     "JOIN rol_permiso rp ON rp.rol_id = uor.rol_id JOIN permiso p ON p.id = rp.permiso_id WHERE p.codigo = @p LIMIT 1",
                     "p", Permisos.UsuariosAdministrar) Is Nothing Then
            Throw New ReglaNegocioException("SIN_ADMINISTRADOR", "La empresa quedaria sin ningun usuario que administre usuarios.")
        End If
    End Sub

    Public Sub DesactivarUsuario(usuarioId As Long)
        If usuarioId = Sesion.UsuarioId Then Throw New ReglaNegocioException("OPERACION_NO_PERMITIDA", "No puede desactivar su propio usuario.")
        EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u)
                If Not Sesion.EsDueno AndAlso CBool(If(u.Escalar("SELECT es_dueno FROM usuario WHERE id = @u", "u", usuarioId), False)) Then
                    Throw New ReglaNegocioException("SOLO_DUENO", "Solo el dueno del sistema puede desactivar a otro dueno.")
                End If
                If u.Ejecutar("UPDATE usuario SET activo = 0 WHERE id = @u", "u", usuarioId) = 0 Then
                    Throw New ReglaNegocioException("NO_ENCONTRADO", "El usuario no existe.")
                End If
                ExigirAdministrador(u)
                Return 0
            End Function)
    End Sub

    Public Function ListarUsuarios() As List(Of UsuarioResumen)
        Return EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u) u.Consultar(
                "SELECT us.id, us.login, us.nombre, us.activo = 1 AS activo, us.es_dueno, " &
                "COALESCE(string_agg(DISTINCT o.codigo || ':' || r.codigo, ', '), '') AS roles " &
                "FROM usuario us LEFT JOIN usuario_operacion_rol uor ON uor.empresa_id = us.empresa_id AND uor.usuario_id = us.id " &
                "LEFT JOIN rol r ON r.empresa_id = uor.empresa_id AND r.id = uor.rol_id " &
                "LEFT JOIN operacion o ON o.empresa_id = uor.empresa_id AND o.id = uor.operacion_id " &
                "GROUP BY us.id, us.login, us.nombre, us.activo, us.es_dueno ORDER BY us.login",
                Function(rd) New UsuarioResumen With {.Id = rd.GetInt64(0), .Login = rd.GetString(1), .Nombre = rd.GetString(2),
                                                     .Activo = rd.GetBoolean(3), .EsDueno = rd.GetBoolean(4), .Roles = rd.GetString(5)}))
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

    ''' <summary>Todas las operaciones (sedes) de la empresa con sus almacenes y usuarios asignados.</summary>
    Public Function ListarOperaciones() As List(Of OperacionDto)
        Return EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u) u.Consultar(
                "SELECT o.id, o.codigo, o.nombre, o.ubicacion, (SELECT count(*) FROM almacen a WHERE a.operacion_id = o.id AND a.activo = 1), " &
                "(SELECT count(DISTINCT uor.usuario_id) FROM usuario_operacion_rol uor WHERE uor.operacion_id = o.id) FROM operacion o ORDER BY o.codigo",
                Function(rd) New OperacionDto With {.Id = rd.GetInt64(0), .Codigo = rd.GetString(1), .Nombre = rd.GetString(2),
                                                    .Ubicacion = rd.TextoONada("ubicacion"), .Almacenes = rd.GetInt64(4), .Usuarios = rd.GetInt64(5)}))
    End Function

    ''' <summary>Almacenes de cualquier operación de la empresa (administración).</summary>
    Public Function ListarAlmacenesDeOperacion(operacionId As Long) As List(Of AlmacenResumen)
        Return EnTransaccion(Permisos.UsuariosAdministrar,
            Function(u) u.Consultar(
                "SELECT id, operacion_id, codigo, nombre FROM almacen WHERE operacion_id = @o AND activo = 1 ORDER BY codigo",
                Function(rd) New AlmacenResumen With {.Id = rd.GetInt64(0), .OperacionId = rd.GetInt64(1), .Codigo = rd.GetString(2), .Nombre = rd.GetString(3)},
                "o", operacionId))
    End Function

    ''' <summary>
    ''' Auditoría de la empresa (quién cambió qué y cuándo), con filtros. Las claves nunca se registran.
    ''' </summary>
    Public Function ConsultarAuditoria(desde As Date, hasta As Date, tabla As String, login As String, Optional limite As Integer = 500) As List(Of AuditoriaDto)
        Return EnTransaccion(Permisos.AuditoriaVer,
            Function(u) u.Consultar(
                "SELECT a.fecha, COALESCE(us.login, '(sistema)'), a.tabla, a.registro_id, a.accion, a.antes_json, a.despues_json FROM auditoria a " &
                "LEFT JOIN usuario us ON us.id = a.usuario_id " &
                "WHERE a.fecha >= @d AND a.fecha < @h AND (@t = '' OR a.tabla = @t) AND (@l = '' OR us.login = @l) ORDER BY a.fecha DESC, a.id DESC LIMIT @n",
                Function(rd) New AuditoriaDto With {.Fecha = rd.GetDateTime(0), .Usuario = rd.GetString(1), .Tabla = rd.GetString(2), .RegistroId = rd.GetInt64(3),
                                                    .Accion = rd.GetString(4), .Antes = rd.TextoONada("antes_json"), .Despues = rd.TextoONada("despues_json")},
                "d", desde.Date, "h", hasta.Date.AddDays(1), "t", If(tabla, "").Trim(), "l", If(login, "").Trim(), "n", limite))
    End Function

    Public Function TablasAuditadas() As List(Of String)
        Return EnTransaccion(Permisos.AuditoriaVer, Function(u) u.Consultar("SELECT DISTINCT tabla FROM auditoria ORDER BY 1", Function(rd) rd.GetString(0)))
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
