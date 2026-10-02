Imports System.Diagnostics
Imports System.IO
Imports Npgsql

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

    Private Sub Sembrar()
        Dim sentencias() As String = {
            "INSERT INTO empresa(id,codigo,nombre) VALUES (1,'A','Empresa ejemplo'),(2,'B','Otra empresa')",
            "INSERT INTO usuario(id,empresa_id,nombre,login,password_hash) VALUES (1,1,'Ejemplo','ejemplo','NO_ES_CREDENCIAL')",
            "INSERT INTO operacion(id,empresa_id,codigo,nombre) VALUES (1,1,'ORC','Orcopampa'),(2,2,'OTR','Otra')",
            "INSERT INTO almacen(id,empresa_id,operacion_id,codigo,nombre) VALUES (1,1,1,'P','Principal')",
            "INSERT INTO unidad_medida(id,empresa_id,codigo,nombre,dimension) VALUES (1,1,'L','Litro','volumen')",
            "INSERT INTO producto_base(id,empresa_id,codigo,descripcion,unidad_base_id) VALUES (1,1,'ACE','Aceite vegetal',1)",
            "INSERT INTO variante_producto(id,empresa_id,producto_base_id,codigo,descripcion_comercial,tipo_envase,contenido_base_por_envase_u6) " &
                "VALUES (1,1,1,'ACE4','Aceite A 4 L','envase',4000000)"
        }
        For Each s In sentencias
            EjecutarAdmin(s)
        Next
    End Sub

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
