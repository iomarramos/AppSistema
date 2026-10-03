Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json
Imports Npgsql

''' <summary>
''' Conexión al servidor de la sede. Se guarda por equipo en %PROGRAMDATA%\AppSistema\conexion.json.
''' La clave de la base se cifra con DPAPI (ámbito del equipo): no queda en texto plano en el archivo.
''' </summary>
Public NotInheritable Class Configuracion
    Public Property Servidor As String = "localhost"
    Public Property Puerto As Integer = 5432
    Public Property BaseDatos As String = "appsistema"
    Public Property UsuarioBD As String = "app_sede"
    Public Property ClaveBDProtegida As String
    Public Property EmpresaPredeterminada As String

    ''' <summary>
    ''' %PROGRAMDATA%\AppSistema\conexion.json, o el archivo que indique APPSISTEMA_CONFIG (lo usan las pruebas E2E para
    ''' apuntar a una base de prueba sin tocar la configuración del equipo).
    ''' </summary>
    Public Shared ReadOnly Property RutaArchivo As String
        Get
            Dim otra = Environment.GetEnvironmentVariable("APPSISTEMA_CONFIG")
            Return If(String.IsNullOrWhiteSpace(otra),
                      Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AppSistema", "conexion.json"), otra)
        End Get
    End Property

    Public Shared Function Cargar() As Configuracion
        If Not File.Exists(RutaArchivo) Then Return Nothing
        Return JsonSerializer.Deserialize(Of Configuracion)(File.ReadAllText(RutaArchivo))
    End Function

    Public Sub Guardar()
        Directory.CreateDirectory(Path.GetDirectoryName(RutaArchivo))
        File.WriteAllText(RutaArchivo, JsonSerializer.Serialize(Me, New JsonSerializerOptions With {.WriteIndented = True}))
    End Sub

    Public Sub FijarClave(clave As String)
        Dim datos = ProtectedData.Protect(Encoding.UTF8.GetBytes(clave), Nothing, DataProtectionScope.LocalMachine)
        ClaveBDProtegida = Convert.ToBase64String(datos)
    End Sub

    Private Function Clave() As String
        If String.IsNullOrEmpty(ClaveBDProtegida) Then Return ""
        Return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(ClaveBDProtegida), Nothing, DataProtectionScope.LocalMachine))
    End Function

    ''' <summary>Cadena de conexión de la aplicación (rol app_stock vía el usuario de la sede).</summary>
    Public Function CadenaConexion() As String
        Dim b As New NpgsqlConnectionStringBuilder With {
            .Host = Servidor, .Port = Puerto, .Database = BaseDatos, .Username = UsuarioBD, .Password = Clave(),
            .ApplicationName = "AppSistema", .Timeout = 10, .MaxPoolSize = 10}
        Return b.ConnectionString
    End Function

    ''' <summary>Comprueba que el servidor responde con estas credenciales.</summary>
    Public Sub Probar()
        Using cn As New NpgsqlConnection(CadenaConexion())
            cn.Open()
        End Using
    End Sub
End Class
