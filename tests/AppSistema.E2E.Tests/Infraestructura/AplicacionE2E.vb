Imports System.Diagnostics
Imports System.IO
Imports FlaUI.Core
Imports FlaUI.Core.AutomationElements
Imports FlaUI.Core.Capturing
Imports FlaUI.Core.Definitions
Imports FlaUI.Core.Input
Imports FlaUI.Core.Tools
Imports FlaUI.Core.WindowsAPI
Imports FlaUI.UIA3

''' <summary>
''' AppSistema.exe en ejecución, automatizado con UI Automation (UIA3). Se busca siempre por AutomationId (= Name del
''' control, ver Identificadores.vb), nunca por posición ni coordenadas. Al cerrar se termina el proceso.
''' </summary>
Public NotInheritable Class AplicacionE2E
    Implements IDisposable

    Public Shared ReadOnly Espera As TimeSpan = TimeSpan.FromSeconds(30)
    Public ReadOnly Property Proceso As Application
    Public ReadOnly Property Automatizacion As New UIA3Automation()

    Public Sub New(bd As BaseDatosE2E)
        Dim psi As New ProcessStartInfo(EntornoPrueba.Ejecutable) With {.UseShellExecute = False, .WorkingDirectory = Path.GetDirectoryName(EntornoPrueba.Ejecutable)}
        psi.Environment("APPSISTEMA_CONFIG") = bd.ArchivoConfiguracion
        Proceso = Application.Launch(psi)
        Proceso.WaitWhileMainHandleIsMissing(Espera)
    End Sub

    Public Function Principal() As Window
        Return Proceso.GetMainWindow(Automatizacion, Espera)
    End Function

    ''' <summary>Ventanas de primer nivel de la aplicación (principal, diálogos, menús desplegables, mensajes).</summary>
    Public Function Ventanas() As Window()
        Return Proceso.GetAllTopLevelWindows(Automatizacion)
    End Function

    ''' <summary>
    ''' Busca por AutomationId en todas las ventanas de la aplicación (incluidos diálogos y menús desplegados). Las opciones
    ''' de menú de WinForms (ToolStripMenuItem) no exponen su Name como AutomationId: se reconocen por su nombre accesible
    ''' convertido con la misma regla (Identificadores.DesdeTexto("mnu", nombre)).
    ''' </summary>
    Public Function Buscar(automationId As String) As AutomationElement
        Dim esMenu = automationId.StartsWith("mnu", StringComparison.Ordinal)
        For Each v In Ventanas()
            If v.AutomationId = automationId Then Return v
            Dim e = If(esMenu, OpcionesDeMenu(v).FirstOrDefault(Function(m) IdDeMenu(m) = automationId),
                       v.FindFirstDescendant(Function(cf) cf.ByAutomationId(automationId)))
            If e IsNot Nothing Then Return e
        Next
        Return Nothing
    End Function

    Public Shared Function OpcionesDeMenu(ventana As AutomationElement) As AutomationElement()
        Return ventana.FindAllDescendants(Function(cf) cf.ByControlType(ControlType.MenuItem))
    End Function

    ''' <summary>Identificador estable de una opción de menú a partir de su nombre accesible.</summary>
    Public Shared Function IdDeMenu(opcion As AutomationElement) As String
        Return AppSistema.Escritorio.Identificadores.DesdeTexto("mnu", If(opcion.Name, ""))
    End Function

    Public Function Esperar(automationId As String, Optional tiempo As TimeSpan? = Nothing) As AutomationElement
        Dim r = Retry.WhileNull(Function() Buscar(automationId), If(tiempo, Espera), TimeSpan.FromMilliseconds(250))
        If r.Result Is Nothing Then
            Capturar($"no-encontrado-{automationId}")
            Throw New InvalidOperationException($"No aparecio el elemento '{automationId}'.")
        End If
        Return r.Result
    End Function

    Public Function Existe(automationId As String) As Boolean
        Return Buscar(automationId) IsNot Nothing
    End Function

    ''' <summary>Pulsa un botón o una opción de menú (patrón Invoke si lo tiene; si no, clic).</summary>
    Public Sub Pulsar(automationId As String)
        Pulsar(Esperar(automationId))
    End Sub

    ''' <remarks>
    ''' Invoke no vuelve mientras el control tenga abierto un diálogo modal (MessageBox, ShowDialog): se llama sin bloquear
    ''' y se espera como máximo 3 s; lo que se abra se busca después.
    ''' </remarks>
    Public Shared Sub Pulsar(e As AutomationElement)
        If e.Patterns.Invoke.IsSupported Then
            Dim patron = e.Patterns.Invoke.Pattern
            Dim tarea = Task.Run(Sub()
                                     Try
                                         patron.Invoke()
                                     Catch ex As TimeoutException
                                         ' Esperado si se abrió un diálogo modal.
                                     End Try
                                 End Sub)
            tarea.Wait(TimeSpan.FromSeconds(3))
        Else
            e.Click()
        End If
        Wait.UntilInputIsProcessed()
    End Sub

    Public Sub Escribir(automationId As String, texto As String)
        Dim caja = Esperar(automationId).AsTextBox()
        caja.Focus()
        If caja.Patterns.Value.IsSupported AndAlso Not caja.Patterns.Value.Pattern.IsReadOnly.ValueOrDefault Then
            caja.Patterns.Value.Pattern.SetValue(texto)
        Else
            caja.Text = ""
            Keyboard.Type(texto)
        End If
    End Sub

    ''' <summary>Cuadro de mensaje (MessageBox) abierto por la aplicación, o Nothing. Devuelve su texto.</summary>
    Public Function Mensaje() As String
        For Each v In Ventanas()
            If v.ClassName = "#32770" Then
                Dim textos = v.FindAllDescendants(Function(cf) cf.ByControlType(ControlType.Text)).Select(Function(t) t.Name).Where(Function(t) Not String.IsNullOrWhiteSpace(t))
                Return String.Join(" ", textos)
            End If
        Next
        Return Nothing
    End Function

    ''' <summary>Cierra el cuadro de mensaje abierto (Enter = botón por defecto).</summary>
    Public Sub CerrarMensaje()
        For Each v In Ventanas()
            If v.ClassName = "#32770" Then
                v.Focus()
                Keyboard.Press(VirtualKeyShort.ENTER)
                Retry.WhileTrue(Function() Mensaje() IsNot Nothing, TimeSpan.FromSeconds(5))
                Return
            End If
        Next
    End Sub

    ''' <summary>Captura PNG de todas las ventanas visibles de la aplicación en artifacts/screenshots.</summary>
    Public Function Capturar(nombre As String) As String
        Dim limpio = New String(nombre.Select(Function(c) If(Char.IsLetterOrDigit(c) OrElse c = "-"c, c, "_"c)).ToArray())
        Dim ruta = Path.Combine(EntornoPrueba.Artefactos, "screenshots", $"{limpio}.png")
        Try
            Capture.Screen().ToFile(ruta)
        Catch ex As Exception
            File.WriteAllText(Path.ChangeExtension(ruta, ".txt"), "No se pudo capturar: " & ex.Message)
        End Try
        Return ruta
    End Function

    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            If Not Proceso.HasExited Then Proceso.Kill()
        Catch
        End Try
        Proceso.Dispose()
        Automatizacion.Dispose()
    End Sub

End Class
