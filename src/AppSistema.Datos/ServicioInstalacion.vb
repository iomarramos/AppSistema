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
                u.Confirmar()
                Return creados
            End Using
        Catch ex As PostgresException
            Throw ErroresBD.Traducir(ex)
        End Try
    End Function

End Class
