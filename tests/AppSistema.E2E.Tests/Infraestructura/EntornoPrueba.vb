Imports System.IO
Imports System.Runtime.InteropServices

''' <summary>Prueba E2E: solo corre en Windows con APPSISTEMA_E2E=1 (necesita escritorio interactivo y PostgreSQL).</summary>
Public NotInheritable Class FactE2EAttribute
    Inherits Xunit.FactAttribute

    Public Sub New()
        If Not RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
            Skip = "Las pruebas E2E de WinForms solo corren en Windows."
        ElseIf Environment.GetEnvironmentVariable("APPSISTEMA_E2E") <> "1" Then
            Skip = "Defina APPSISTEMA_E2E=1 y APPSISTEMA_E2E_PG (conexion del propietario de PostgreSQL) para ejecutar las pruebas E2E."
        End If
    End Sub
End Class

''' <summary>Rutas y variables del entorno de las pruebas E2E.</summary>
Public Module EntornoPrueba

    ''' <summary>Conexión del propietario de PostgreSQL (crea y borra la base de prueba). Ej.: Host=localhost;Username=postgres;Password=...</summary>
    Public ReadOnly Property ConexionPropietario As String
        Get
            Dim c = Environment.GetEnvironmentVariable("APPSISTEMA_E2E_PG")
            If String.IsNullOrWhiteSpace(c) Then Throw New InvalidOperationException("Defina APPSISTEMA_E2E_PG con la conexion del propietario de PostgreSQL.")
            Return c
        End Get
    End Property

    ''' <summary>Raíz del repositorio (donde está AppSistema.sln).</summary>
    Public ReadOnly Property Raiz As String
        Get
            Dim d = New DirectoryInfo(AppContext.BaseDirectory)
            While d IsNot Nothing AndAlso Not File.Exists(Path.Combine(d.FullName, "AppSistema.sln"))
                d = d.Parent
            End While
            If d Is Nothing Then Throw New InvalidOperationException("No se encontro la raiz del repositorio (AppSistema.sln).")
            Return d.FullName
        End Get
    End Property

    ''' <summary>AppSistema.exe compilado (APPSISTEMA_E2E_EXE o la salida del proyecto en la misma configuración que las pruebas).</summary>
    Public ReadOnly Property Ejecutable As String
        Get
            Dim c = Environment.GetEnvironmentVariable("APPSISTEMA_E2E_EXE")
            If Not String.IsNullOrWhiteSpace(c) Then Return c
            Dim configuracion = If(AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}"), "Release", "Debug")
            Dim exe = Path.Combine(Raiz, "src", "AppSistema.Escritorio", "bin", configuracion, "net8.0-windows", "AppSistema.exe")
            If Not File.Exists(exe) Then Throw New FileNotFoundException("Compile AppSistema.Escritorio antes de las pruebas E2E.", exe)
            Return exe
        End Get
    End Property

    ''' <summary>Carpeta de capturas y registros (artifacts/ en la raíz, o APPSISTEMA_E2E_ARTEFACTOS).</summary>
    Public ReadOnly Property Artefactos As String
        Get
            Dim c = Environment.GetEnvironmentVariable("APPSISTEMA_E2E_ARTEFACTOS")
            Dim dir = If(String.IsNullOrWhiteSpace(c), Path.Combine(Raiz, "artifacts"), c)
            Directory.CreateDirectory(Path.Combine(dir, "screenshots"))
            Return dir
        End Get
    End Property

End Module
