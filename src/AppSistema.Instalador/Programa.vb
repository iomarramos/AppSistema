Imports System.Text
Imports AppSistema.Datos
Imports AppSistema.Dominio

''' <summary>
''' Herramienta de instalación de una sede. Usa la conexión del PROPIETARIO de la base
''' (variable APPSISTEMA_CONEXION_PROPIETARIO o se solicita). No guarda claves.
'''   migrar                      aplica las migraciones pendientes
'''   crear-empresa               crea empresa, primera operación, almacén y administrador
'''   crear-usuario-sede NOMBRE   crea el usuario de base que usan las computadoras de la sede
''' </summary>
Public Module Programa

    Public Function Main(args As String()) As Integer
        Console.OutputEncoding = Encoding.UTF8
        If args.Length = 0 OrElse args(0) = "-h" OrElse args(0) = "--ayuda" Then
            Ayuda()
            Return If(args.Length = 0, 1, 0)
        End If
        Try
            Dim conexion = Environment.GetEnvironmentVariable("APPSISTEMA_CONEXION_PROPIETARIO")
            If String.IsNullOrWhiteSpace(conexion) Then conexion = Pedir("Conexion del propietario (Host=...;Database=...;Username=...;Password=...)", oculto:=True)

            Select Case args(0)
                Case "migrar"
                    For Each m In New Migrador(conexion).Migrar()
                        Console.WriteLine($"  {m.Archivo,-45} {If(m.Aplicada, "APLICADA", "ya estaba")}")
                    Next
                    Console.WriteLine("Migraciones al dia.")

                Case "crear-empresa"
                    Dim d As New DatosInstalacion With {
                        .EmpresaCodigo = Pedir("Codigo de empresa"), .EmpresaNombre = Pedir("Nombre de empresa"),
                        .OperacionCodigo = Pedir("Codigo de la primera operacion"), .OperacionNombre = Pedir("Nombre de la operacion"),
                        .AlmacenCodigo = Pedir("Codigo del almacen"), .AlmacenNombre = Pedir("Nombre del almacen"),
                        .AdminLogin = Pedir("Usuario administrador"), .AdminNombre = Pedir("Nombre del administrador")}
                    d.AdminClave = PedirClaveConfirmada("Clave del administrador")
                    Dim r = New ServicioInstalacion(conexion).CrearEmpresa(d)
                    Console.WriteLine($"Empresa creada (id {r.EmpresaId}). El administrador ya puede iniciar sesion.")

                Case "crear-usuario-sede"
                    If args.Length < 2 Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", "Indique el nombre del usuario de sede.")
                    Call New Migrador(conexion).CrearUsuarioSede(args(1), PedirClaveConfirmada("Clave del usuario de sede"))
                    Console.WriteLine($"Usuario de sede '{args(1)}' listo. Configure esa cuenta en cada computadora (menu Sesion > Conexion).")

                Case Else
                    Ayuda()
                    Return 1
            End Select
            Return 0
        Catch ex As ReglaNegocioException
            Console.Error.WriteLine("ERROR: " & ex.Message)
            Return 2
        Catch ex As Exception
            Console.Error.WriteLine("ERROR inesperado: " & ex.Message)
            Return 3
        End Try
    End Function

    Private Sub Ayuda()
        Console.WriteLine("Uso: AppSistema.Instalador <migrar | crear-empresa | crear-usuario-sede NOMBRE>")
        Console.WriteLine("La conexion del propietario se toma de APPSISTEMA_CONEXION_PROPIETARIO o se solicita.")
    End Sub

    Private Function Pedir(texto As String, Optional oculto As Boolean = False) As String
        Console.Write(texto & ": ")
        If Not oculto OrElse Console.IsInputRedirected Then Return If(Console.ReadLine(), "").Trim()
        Dim sb As New StringBuilder()
        Do
            Dim k = Console.ReadKey(True)
            If k.Key = ConsoleKey.Enter Then Exit Do
            If k.Key = ConsoleKey.Backspace Then
                If sb.Length > 0 Then sb.Length -= 1
            ElseIf Not Char.IsControl(k.KeyChar) Then
                sb.Append(k.KeyChar)
            End If
        Loop
        Console.WriteLine()
        Return sb.ToString()
    End Function

    Private Function PedirClaveConfirmada(texto As String) As String
        Dim c1 = Pedir(texto, oculto:=True)
        Dim c2 = Pedir("Repita la clave", oculto:=True)
        If c1 <> c2 Then Throw New ReglaNegocioException("DATO_INVALIDO", "Las claves no coinciden.")
        Return c1
    End Function

End Module
