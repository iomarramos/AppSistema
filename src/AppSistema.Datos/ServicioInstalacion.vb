Imports Npgsql
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Seguridad

Public NotInheritable Class DatosInstalacion
    Public Property EmpresaCodigo As String
    Public Property EmpresaNombre As String
    Public Property Moneda As String = "PEN"
    Public Property OperacionCodigo As String
    Public Property OperacionNombre As String
    Public Property AlmacenCodigo As String
    Public Property AlmacenNombre As String
    Public Property AdminLogin As String
    Public Property AdminNombre As String
    Public Property AdminClave As String
End Class

''' <summary>Resultado de un perfil de prueba: la clave solo viene cuando la cuenta se creó en esta ejecución.</summary>
Public NotInheritable Class PerfilPruebaDto
    Public Property Login As String
    Public Property Nombre As String
    Public Property Rol As String
    Public Property Creado As Boolean
    Public Property Clave As String
End Class

Public NotInheritable Class ResultadoInstalacion
    Public Property EmpresaId As Long
    Public Property OperacionId As Long
    Public Property AlmacenId As Long
    Public Property AdminUsuarioId As Long
End Class

''' <summary>
''' Alta de una empresa nueva con su primera operación, almacén, permisos, roles base y administrador.
''' Se ejecuta con la conexión del PROPIETARIO de la base (instalador), no con la de la aplicación.
''' No existen usuarios ni claves por defecto: la clave del administrador la define quien instala.
''' </summary>
Public NotInheritable Class ServicioInstalacion

    Private ReadOnly _cadenaPropietario As String

    Public Sub New(cadenaPropietario As String)
        If String.IsNullOrWhiteSpace(cadenaPropietario) Then Throw New ArgumentException("Falta la cadena de conexion.", NameOf(cadenaPropietario))
        _cadenaPropietario = cadenaPropietario
    End Sub

    Public Function CrearEmpresa(d As DatosInstalacion) As ResultadoInstalacion
        If d Is Nothing Then Throw New ArgumentNullException(NameOf(d))
        For Each par In {("empresa", d.EmpresaCodigo), ("nombre de empresa", d.EmpresaNombre), ("operacion", d.OperacionCodigo),
                         ("nombre de operacion", d.OperacionNombre), ("almacen", d.AlmacenCodigo), ("nombre de almacen", d.AlmacenNombre),
                         ("usuario administrador", d.AdminLogin), ("nombre del administrador", d.AdminNombre)}
            If String.IsNullOrWhiteSpace(par.Item2) Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", $"Falta {par.Item1}.")
        Next
        PoliticaClave.Validar(d.AdminClave)

        Try
            Using u As New UnidadDeTrabajo(_cadenaPropietario, Nothing, Nothing)
                Dim r As New ResultadoInstalacion()
                r.EmpresaId = u.EscalarLong("INSERT INTO empresa(codigo, nombre, moneda) VALUES (@c, @n, @m) RETURNING id",
                                            "c", d.EmpresaCodigo.Trim(), "n", d.EmpresaNombre.Trim(), "m", d.Moneda)
                Dim permisoIds As New Dictionary(Of String, Long)
                For Each p In Permisos.Todos
                    permisoIds(p) = u.EscalarLong("INSERT INTO permiso(empresa_id, codigo, descripcion) VALUES (@e, @c, @d) RETURNING id",
                                                  "e", r.EmpresaId, "c", p, "d", Permisos.Descripcion(p))
                Next
                Dim rolAdmin As Long
                For Each rol In RolesBase.Todos
                    Dim rolId = u.EscalarLong("INSERT INTO rol(empresa_id, codigo, nombre) VALUES (@e, @c, @n) RETURNING id",
                                              "e", r.EmpresaId, "c", rol.Codigo, "n", rol.Nombre)
                    If rol.Codigo = RolesBase.Administrador Then rolAdmin = rolId
                    For Each p In rol.Permisos
                        u.Ejecutar("INSERT INTO rol_permiso(empresa_id, rol_id, permiso_id) VALUES (@e, @r, @p)",
                                   "e", r.EmpresaId, "r", rolId, "p", permisoIds(p))
                    Next
                Next
                r.OperacionId = u.EscalarLong("INSERT INTO operacion(empresa_id, codigo, nombre) VALUES (@e, @c, @n) RETURNING id",
                                              "e", r.EmpresaId, "c", d.OperacionCodigo.Trim(), "n", d.OperacionNombre.Trim())
                r.AlmacenId = u.EscalarLong("INSERT INTO almacen(empresa_id, operacion_id, codigo, nombre) VALUES (@e, @o, @c, @n) RETURNING id",
                                            "e", r.EmpresaId, "o", r.OperacionId, "c", d.AlmacenCodigo.Trim(), "n", d.AlmacenNombre.Trim())
                r.AdminUsuarioId = u.EscalarLong("INSERT INTO usuario(empresa_id, nombre, login, password_hash) VALUES (@e, @n, @l, @h) RETURNING id",
                                                 "e", r.EmpresaId, "n", d.AdminNombre.Trim(), "l", d.AdminLogin.Trim(), "h", ClaveSegura.Crear(d.AdminClave))
                u.Ejecutar("INSERT INTO usuario_operacion_rol(empresa_id, usuario_id, operacion_id, rol_id) VALUES (@e, @u, @o, @r)",
                           "e", r.EmpresaId, "u", r.AdminUsuarioId, "o", r.OperacionId, "r", rolAdmin)
                u.Confirmar()
                Return r
            End Using
        Catch ex As PostgresException
            Throw ErroresBD.Traducir(ex)
        End Try
    End Function

    ''' <summary>
    ''' Perfiles de prueba: una cuenta por rol de la operación, para probar cada menú con sus permisos. La clave se genera
    ''' y se devuelve una sola vez. Las cuentas que ya existen no se tocan (la ejecución es repetible).
    ''' </summary>
    Public Function CrearPerfilesPrueba(empresaCodigo As String, operacionCodigo As String) As List(Of PerfilPruebaDto)
        Dim resultado As New List(Of PerfilPruebaDto)()
        Try
            Using u As New UnidadDeTrabajo(_cadenaPropietario, Nothing, Nothing)
                Dim empresa = u.Escalar("SELECT id FROM empresa WHERE codigo = @c", "c", empresaCodigo.Trim())
                If empresa Is Nothing Then Throw New ReglaNegocioException("DATO_INVALIDO", $"La empresa '{empresaCodigo}' no existe.")
                If u.Escalar("SELECT 1 FROM operacion WHERE empresa_id = @e AND codigo = @o", "e", empresa, "o", operacionCodigo.Trim()) Is Nothing Then
                    Throw New ReglaNegocioException("DATO_INVALIDO", $"La operacion '{operacionCodigo}' no existe en la empresa.")
                End If
                For Each perfil In PerfilesDePrueba
                    If u.Escalar("SELECT 1 FROM rol WHERE empresa_id = @e AND codigo = @r", "e", empresa, "r", perfil.Rol) Is Nothing Then
                        Throw New ReglaNegocioException("ROL_NO_ENCONTRADO", $"No existe el rol {perfil.Rol} en la empresa.")
                    End If
                Next
                For Each perfil In PerfilesDePrueba
                    If u.Escalar("SELECT 1 FROM usuario WHERE empresa_id = @e AND login = @l", "e", empresa, "l", perfil.Login) IsNot Nothing Then
                        resultado.Add(New PerfilPruebaDto With {.Login = perfil.Login, .Nombre = perfil.Nombre, .Rol = perfil.Rol, .Creado = False})
                        Continue For
                    End If
                    Dim clave = GenerarClavePrueba()
                    Dim usuarioId = u.EscalarLong(
                        "INSERT INTO usuario(empresa_id, nombre, login, password_hash) VALUES (@e, @n, @l, @h) RETURNING id",
                        "e", empresa, "n", perfil.Nombre, "l", perfil.Login, "h", ClaveSegura.Crear(clave))
                    u.Ejecutar("INSERT INTO usuario_operacion_rol(empresa_id, usuario_id, operacion_id, rol_id) " &
                               "SELECT @e, @u, o.id, r.id FROM operacion o, rol r " &
                               "WHERE o.empresa_id = @e AND o.codigo = @oc AND r.empresa_id = @e AND r.codigo = @rc",
                               "e", empresa, "u", usuarioId, "oc", operacionCodigo.Trim(), "rc", perfil.Rol)
                    resultado.Add(New PerfilPruebaDto With {.Login = perfil.Login, .Nombre = perfil.Nombre, .Rol = perfil.Rol, .Creado = True, .Clave = clave})
                Next
                u.Confirmar()
            End Using
        Catch ex As PostgresException
            Throw ErroresBD.Traducir(ex)
        End Try
        Return resultado
    End Function

    Private Shared ReadOnly PerfilesDePrueba As (Login As String, Nombre As String, Rol As String)() = {
        ("chef_prueba", "Chef de prueba", "CHEF"),
        ("almacen_prueba", "Almacenero de prueba", "ALMACEN"),
        ("jefe_almacen_prueba", "Jefe de almacen de prueba", "JEFE_ALMACEN"),
        ("operaciones_prueba", "Jefe de operacion de prueba", "OPERACIONES"),
        ("planificacion_prueba", "Planificador de prueba", "PLANIFICADOR_CENTRAL"),
        ("compras_prueba", "Compras de prueba", "COMPRAS_CENTRAL")}

    ''' <summary>Clave aleatoria que cumple la política de claves (mayúscula, minúscula y número).</summary>
    Private Shared Function GenerarClavePrueba() As String
        Const alfabeto As String = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789"
        Do
            Dim chars(13) As Char
            For i = 0 To chars.Length - 1
                chars(i) = alfabeto(Security.Cryptography.RandomNumberGenerator.GetInt32(alfabeto.Length))
            Next
            Dim clave = New String(chars)
            If ClaveCumple(clave) Then Return clave
        Loop
    End Function

    Private Shared Function ClaveCumple(clave As String) As Boolean
        Try
            PoliticaClave.Validar(clave)
            Return True
        Catch ex As ReglaNegocioException
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Crea el dueño del sistema (administrador general) con su clave personal, o da ese rango a un usuario existente y
    ''' le fija la clave. El dueño entra a todas las operaciones con todos los permisos. Se hace con la conexión del
    ''' propietario: desde la aplicación solo otro dueño puede otorgarlo.
    ''' </summary>
    Public Function CrearDueno(empresaCodigo As String, login As String, nombre As String, clave As String) As Long
        If String.IsNullOrWhiteSpace(login) OrElse String.IsNullOrWhiteSpace(nombre) Then
            Throw New ReglaNegocioException("DATO_OBLIGATORIO", "Usuario y nombre son obligatorios.")
        End If
        PoliticaClave.Validar(clave)
        Try
            Using u As New UnidadDeTrabajo(_cadenaPropietario, Nothing, Nothing)
                Dim empresa = u.Escalar("SELECT id FROM empresa WHERE codigo = @c", "c", empresaCodigo.Trim())
                If empresa Is Nothing Then Throw New ReglaNegocioException("DATO_INVALIDO", $"La empresa '{empresaCodigo}' no existe.")
                Dim id = u.EscalarLong(
                    "INSERT INTO usuario(empresa_id, nombre, login, password_hash, es_dueno) VALUES (@e, @n, @l, @h, true) " &
                    "ON CONFLICT (empresa_id, login) DO UPDATE SET es_dueno = true, nombre = EXCLUDED.nombre, password_hash = EXCLUDED.password_hash, activo = 1 " &
                    "RETURNING id",
                    "e", empresa, "n", nombre.Trim(), "l", login.Trim(), "h", ClaveSegura.Crear(clave))
                u.Confirmar()
                Return id
            End Using
        Catch ex As PostgresException
            Throw ErroresBD.Traducir(ex)
        End Try
    End Function

    ''' <summary>
    ''' Tras una actualización: crea en cada empresa los permisos nuevos del sistema y los asigna al rol ADMIN.
    ''' Los demás roles no cambian (lo decide el administrador). Idempotente; devuelve cuántos permisos creó.
    ''' </summary>
    Public Function SincronizarPermisos() As Integer
        Try
            Using u As New UnidadDeTrabajo(_cadenaPropietario, Nothing, Nothing)
                Dim creados = 0
                For Each p In Permisos.Todos
                    creados += u.Ejecutar(
                        "INSERT INTO permiso(empresa_id, codigo, descripcion) SELECT e.id, @c, @d FROM empresa e " &
                        "WHERE NOT EXISTS (SELECT 1 FROM permiso p WHERE p.empresa_id = e.id AND p.codigo = @c)",
                        "c", p, "d", Permisos.Descripcion(p))
                Next
                u.Ejecutar("INSERT INTO rol_permiso(empresa_id, rol_id, permiso_id) " &
                           "SELECT r.empresa_id, r.id, p.id FROM rol r JOIN permiso p ON p.empresa_id = r.empresa_id " &
                           "WHERE r.codigo = @admin ON CONFLICT (empresa_id, rol_id, permiso_id) DO NOTHING",
                           "admin", RolesBase.Administrador)
                ' Roles base nuevos (p. ej. FINANZAS) en empresas ya instaladas; los existentes no se tocan.
                For Each rol In RolesBase.Todos
                    Dim nuevos = u.Consultar("INSERT INTO rol(empresa_id, codigo, nombre) SELECT e.id, @c, @n FROM empresa e " &
                                             "WHERE NOT EXISTS (SELECT 1 FROM rol r WHERE r.empresa_id = e.id AND r.codigo = @c) RETURNING empresa_id, id",
                                             Function(rd) (rd.GetInt64(0), rd.GetInt64(1)), "c", rol.Codigo, "n", rol.Nombre)
                    For Each n In nuevos
                        u.Ejecutar("INSERT INTO rol_permiso(empresa_id, rol_id, permiso_id) SELECT @e, @r, p.id FROM permiso p WHERE p.empresa_id = @e AND p.codigo = ANY(@ps)",
                                   "e", n.Item1, "r", n.Item2, "ps", rol.Permisos.ToArray())
                    Next
                Next
                u.Confirmar()
                Return creados
            End Using
        Catch ex As PostgresException
            Throw ErroresBD.Traducir(ex)
        End Try
    End Function

End Class
