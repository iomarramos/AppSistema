Imports System.Diagnostics
Imports System.IO
Imports Npgsql
Imports AppSistema.Datos

''' <summary>Pruebas de integración contra un PostgreSQL real. Se activan con APPSISTEMA_PG_PRUEBAS=1.</summary>
Public NotInheritable Class FactPostgresAttribute
    Inherits Xunit.FactAttribute

    Public Sub New()
        If Environment.GetEnvironmentVariable("APPSISTEMA_PG_PRUEBAS") <> "1" Then
            Skip = "Defina APPSISTEMA_PG_PRUEBAS=1 (y, si hace falta, APPSISTEMA_PG_HOST / APPSISTEMA_PG_USER) para ejecutar las pruebas contra PostgreSQL."
        End If
    End Sub
End Class

''' <summary>Crea una base nueva con las migraciones del repositorio y datos ficticios; la elimina al terminar.</summary>
Public NotInheritable Class BaseDatosPrueba
    Implements IDisposable

    Private ReadOnly _host As String
    Private ReadOnly _usuario As String
    Public ReadOnly Property Nombre As String

    ''' <summary>Conexión del propietario (migraciones, siembra y consultas de verificación).</summary>
    Public ReadOnly Property CadenaAdmin As String
    ''' <summary>Conexión de la aplicación: rol app_stock, sin escritura en saldo_stock.</summary>
    Public ReadOnly Property CadenaAplicacion As String

    Private Sub New()
        _host = If(Environment.GetEnvironmentVariable("APPSISTEMA_PG_HOST"), "/var/run/postgresql")
        _usuario = If(Environment.GetEnvironmentVariable("APPSISTEMA_PG_USER"), "root")
        Nombre = "appsistema_net_" & Guid.NewGuid().ToString("N").Substring(0, 8)
        CadenaAdmin = $"Host={_host};Username={_usuario};Database={Nombre};Pooling=false"
        CadenaAplicacion = $"Host={_host};Username={_usuario};Database={Nombre};Options=-c role=app_stock;Maximum Pool Size=30"
    End Sub

    Public Shared Function Crear() As BaseDatosPrueba
        Dim bd As New BaseDatosPrueba()
        Try
            bd.Preparar()
        Catch
            bd.Dispose()
            Throw
        End Try
        Return bd
    End Function

    Private Sub Preparar()
        Using cn As New NpgsqlConnection($"Host={_host};Username={_usuario};Database=postgres;Pooling=false")
            cn.Open()
            Using cmd As New NpgsqlCommand($"CREATE DATABASE {Nombre}", cn)
                cmd.ExecuteNonQuery()
            End Using
        End Using

        Dim carpeta As String = BuscarMigraciones()
        For Each archivo In Directory.GetFiles(carpeta, "V*.sql").OrderBy(Function(f) f, StringComparer.Ordinal)
            Dim psi As New ProcessStartInfo("psql", $"-X -q -v ON_ERROR_STOP=1 -h ""{_host}"" -U {_usuario} -d {Nombre} -f ""{archivo}""") With {
                .RedirectStandardError = True, .RedirectStandardOutput = True, .UseShellExecute = False}
            Using p As Process = Process.Start(psi)
                Dim err As String = p.StandardError.ReadToEnd()
                p.WaitForExit()
                If p.ExitCode <> 0 Then Throw New InvalidOperationException($"Migracion {Path.GetFileName(archivo)} fallo: {err}")
            End Using
        Next

        Sembrar()
    End Sub

    Private Shared Function BuscarMigraciones() As String
        Dim dir As New DirectoryInfo(AppContext.BaseDirectory)
        While dir IsNot Nothing
            Dim candidata As String = Path.Combine(dir.FullName, "database", "postgresql", "migraciones")
            If Directory.Exists(candidata) Then Return candidata
            dir = dir.Parent
        End While
        Throw New DirectoryNotFoundException("No se encontro database/postgresql/migraciones.")
    End Function

    Public Const ClaveAdmin As String = "Clave-Segura-2026"

    ''' <summary>Empresa A (Orcopampa) y empresa B, instaladas con el servicio real de instalación.</summary>
    Public Property A As ResultadoInstalacion
    Public Property B As ResultadoInstalacion
    ''' <summary>Aceite vegetal de A: variante de 4 L con caja de 4 envases.</summary>
    Public Property VarianteAceiteId As Long
    Public Property EmpaqueCajaId As Long
    Public Property ProductoAceiteId As Long
    Public Property UnidadLitroId As Long

    Private Sub Sembrar()
        Dim inst As New ServicioInstalacion(CadenaAdmin)
        A = inst.CrearEmpresa(New DatosInstalacion With {
            .EmpresaCodigo = "A", .EmpresaNombre = "Empresa ejemplo A", .OperacionCodigo = "ORC", .OperacionNombre = "Orcopampa",
            .AlmacenCodigo = "P", .AlmacenNombre = "Principal", .AdminLogin = "admin", .AdminNombre = "Administrador A", .AdminClave = ClaveAdmin})
        B = inst.CrearEmpresa(New DatosInstalacion With {
            .EmpresaCodigo = "B", .EmpresaNombre = "Otra empresa", .OperacionCodigo = "OTR", .OperacionNombre = "Otra operacion",
            .AlmacenCodigo = "Q", .AlmacenNombre = "Almacen B", .AdminLogin = "admin", .AdminNombre = "Administrador B", .AdminClave = ClaveAdmin})

        Dim cat As New ServicioCatalogo(CadenaAplicacion, Sesion("A"))
        UnidadLitroId = cat.CrearUnidad("L", "Litro", AppSistema.Dominio.Catalogo.Dimension.Volumen, 1000000)
        ProductoAceiteId = cat.CrearProducto("ACE", "Aceite vegetal", Nothing, UnidadLitroId, Nothing)
        VarianteAceiteId = cat.CrearVariante(ProductoAceiteId, Nothing, "ACE-A-4L", "Aceite A 4 L", "bidon", 4000000)
        EmpaqueCajaId = cat.CrearEmpaque(VarianteAceiteId, "CAJA4", "Caja 4 x 4 L", 4)
    End Sub

    ''' <summary>Inicia sesión como lo hace la aplicación y selecciona la primera operación.</summary>
    Public Function Sesion(empresa As String, Optional login As String = "admin", Optional clave As String = ClaveAdmin) As SesionUsuario
        Dim acceso As New ServicioAcceso(CadenaAplicacion)
        Dim s = acceso.IniciarSesion(empresa, login, clave)
        Return acceso.SeleccionarOperacion(s, s.Operaciones(0).Id)
    End Function

    Public Sub EjecutarAdmin(sql As String)
        Using cn As New NpgsqlConnection(CadenaAdmin)
            cn.Open()
            Using cmd As New NpgsqlCommand(sql, cn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Public Function Escalar(sql As String) As Object
        Using cn As New NpgsqlConnection(CadenaAdmin)
            cn.Open()
            Using cmd As New NpgsqlCommand(sql, cn)
                Return cmd.ExecuteScalar()
            End Using
        End Using
    End Function

    Public Function SaldoU6(Optional varianteId As Long = 1) As Long
        Return Convert.ToInt64(Escalar($"SELECT COALESCE(SUM(cantidad_base_u6),0) FROM saldo_stock WHERE variante_id = {varianteId}"))
    End Function

    Public Function ValorU6(Optional varianteId As Long = 1) As Long
        Return Convert.ToInt64(Escalar($"SELECT COALESCE(SUM(valor_u6),0) FROM saldo_stock WHERE variante_id = {varianteId}"))
    End Function

    Public Function FilasSinConciliar() As Long
        Return Convert.ToInt64(Escalar("SELECT count(*) FROM v_conciliacion_saldo"))
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            NpgsqlConnection.ClearAllPools()
            Using cn As New NpgsqlConnection($"Host={_host};Username={_usuario};Database=postgres;Pooling=false")
                cn.Open()
                Using cmd As New NpgsqlCommand($"DROP DATABASE IF EXISTS {Nombre} WITH (FORCE)", cn)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch
            ' La limpieza no debe ocultar el resultado de la prueba.
        End Try
    End Sub

End Class
