Imports System.IO
Imports Npgsql
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad
Imports AppSistema.Escritorio

''' <summary>Usuarios de prueba (claves solo de esta base temporal, que se borra al terminar).</summary>
Public Module UsuariosPrueba
    Public Const Empresa As String = "DEMO"
    Public ReadOnly Dueno As (Login As String, Clave As String) = ("dueno", "Dueno-E2E-2026")
    Public ReadOnly Admin As (Login As String, Clave As String) = ("admin", "Admin-E2E-2026")
    Public ReadOnly Chef As (Login As String, Clave As String) = ("chef", "Chef-E2E-2026")
    Public ReadOnly Almacenero As (Login As String, Clave As String) = ("almacen", "Almacen-E2E-2026")
    Public ReadOnly Planificador As (Login As String, Clave As String) = ("plan", "Plan-E2E-2026")
End Module

''' <summary>
''' Base PostgreSQL temporal para las pruebas E2E: migraciones del repositorio, empresa DEMO con su operación, el
''' superusuario, un usuario de sede (rol app_stock) y usuarios por rol. Escribe un conexion.json propio (APPSISTEMA_CONFIG)
''' para que la aplicación se conecte a esta base sin tocar la configuración del equipo. Se borra al terminar.
''' </summary>
Public NotInheritable Class BaseDatosE2E
    Implements IDisposable

    Private ReadOnly _servidor As NpgsqlConnectionStringBuilder
    Public ReadOnly Property Nombre As String = "appsistema_e2e_" & Guid.NewGuid().ToString("N").Substring(0, 10)
    Public ReadOnly Property UsuarioSede As String = "e2e_sede_" & Guid.NewGuid().ToString("N").Substring(0, 8)
    Private ReadOnly _claveSede As String = "Sede-" & Guid.NewGuid().ToString("N").Substring(0, 12) & "-9"
    Public ReadOnly Property ArchivoConfiguracion As String
    Public ReadOnly Property Instalacion As ResultadoInstalacion

    Public Sub New()
        _servidor = New NpgsqlConnectionStringBuilder(EntornoPrueba.ConexionPropietario)
        Using cn As New NpgsqlConnection(Cadena("postgres"))
            cn.Open()
            Using cmd As New NpgsqlCommand($"CREATE DATABASE {Nombre}", cn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
        Call New Migrador(CadenaPropietario).Migrar()
        Dim inst As New ServicioInstalacion(CadenaPropietario)
        Instalacion = inst.CrearEmpresa(New DatosInstalacion With {
            .EmpresaCodigo = UsuariosPrueba.Empresa, .EmpresaNombre = "Empresa de pruebas E2E", .OperacionCodigo = "ORC", .OperacionNombre = "Orcopampa",
            .AlmacenCodigo = "P", .AlmacenNombre = "Principal", .AdminLogin = UsuariosPrueba.Admin.Login, .AdminNombre = "Administrador",
            .AdminClave = UsuariosPrueba.Admin.Clave})
        inst.CrearDueno(UsuariosPrueba.Empresa, UsuariosPrueba.Dueno.Login, "Superusuario", UsuariosPrueba.Dueno.Clave)
        Call New Migrador(CadenaPropietario).CrearUsuarioSede(UsuarioSede, _claveSede)

        Dim admin As New ServicioAdministracion(CadenaAplicacion, Sesion(UsuariosPrueba.Admin))
        Dim op = Instalacion.OperacionId
        admin.CrearUsuario(UsuariosPrueba.Chef.Login, "Chef de pruebas", UsuariosPrueba.Chef.Clave, op, RolesBase.Chef)
        admin.CrearUsuario(UsuariosPrueba.Almacenero.Login, "Almacenero de pruebas", UsuariosPrueba.Almacenero.Clave, op, "ALMACEN")
        admin.CrearUsuario(UsuariosPrueba.Planificador.Login, "Planificador de pruebas", UsuariosPrueba.Planificador.Clave, op, RolesBase.PlanificadorCentral)

        ' Conexión de la aplicación: archivo propio, con la clave cifrada (DPAPI) como lo hace la aplicación.
        ArchivoConfiguracion = Path.Combine(Path.GetTempPath(), $"{Nombre}.json")
        Environment.SetEnvironmentVariable("APPSISTEMA_CONFIG", ArchivoConfiguracion)
        Dim config As New Configuracion With {.Servidor = _servidor.Host, .Puerto = _servidor.Port, .BaseDatos = Nombre, .UsuarioBD = UsuarioSede,
                                              .EmpresaPredeterminada = UsuariosPrueba.Empresa}
        config.FijarClave(_claveSede)
        config.Guardar()
    End Sub

    Private Function Cadena(baseDatos As String, Optional usuario As String = Nothing, Optional clave As String = Nothing) As String
        Dim b As New NpgsqlConnectionStringBuilder(_servidor.ConnectionString) With {.Database = baseDatos, .Pooling = False}
        If usuario IsNot Nothing Then b.Username = usuario : b.Password = clave
        Return b.ConnectionString
    End Function

    Public ReadOnly Property CadenaPropietario As String
        Get
            Return Cadena(Nombre)
        End Get
    End Property

    ''' <summary>Como la aplicación: usuario de sede (rol app_stock, con RLS).</summary>
    Public ReadOnly Property CadenaAplicacion As String
        Get
            Return Cadena(Nombre, UsuarioSede, _claveSede)
        End Get
    End Property

    ''' <summary>Sesión por servicio (para preparar datos o comprobar lo que hizo la interfaz).</summary>
    Public Function Sesion(usuario As (Login As String, Clave As String)) As SesionUsuario
        Dim acceso As New ServicioAcceso(CadenaAplicacion)
        Dim s = acceso.IniciarSesion(UsuariosPrueba.Empresa, usuario.Login, usuario.Clave)
        Return acceso.SeleccionarOperacion(s, s.Operaciones(0).Id)
    End Function

    Public Function IdUsuario(login As String) As Long
        Return New ServicioAdministracion(CadenaAplicacion, Sesion(UsuariosPrueba.Dueno)).ListarUsuarios().Single(Function(u) u.Login = login).Id
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            NpgsqlConnection.ClearAllPools()
            Using cn As New NpgsqlConnection(Cadena("postgres"))
                cn.Open()
                For Each sql In {$"DROP DATABASE IF EXISTS {Nombre} WITH (FORCE)", $"DROP ROLE IF EXISTS {UsuarioSede}"}
                    Using cmd As New NpgsqlCommand(sql, cn)
                        cmd.ExecuteNonQuery()
                    End Using
                Next
            End Using
        Finally
            If File.Exists(ArchivoConfiguracion) Then File.Delete(ArchivoConfiguracion)
        End Try
    End Sub

End Class

''' <summary>Una sola base para toda la colección E2E (las pruebas corren en serie: hay un solo escritorio).</summary>
<Xunit.CollectionDefinition("E2E", DisableParallelization:=True)>
Public NotInheritable Class ColeccionE2E
    Implements Xunit.ICollectionFixture(Of BaseDatosE2E)
End Class
