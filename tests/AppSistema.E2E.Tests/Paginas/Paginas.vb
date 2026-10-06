Imports FlaUI.Core.AutomationElements
Imports FlaUI.Core.Definitions
Imports FlaUI.Core.Input
Imports FlaUI.Core.Tools
Imports FlaUI.Core.WindowsAPI

''' <summary>Pantalla de acceso (FormAcceso): empresa, usuario, clave y Entrar.</summary>
Public NotInheritable Class PaginaAcceso
    Private ReadOnly _app As AplicacionE2E

    Public Sub New(app As AplicacionE2E)
        _app = app
        _app.Esperar("FormAcceso")
    End Sub

    ''' <summary>Escribe las credenciales y pulsa Entrar. Devuelve True si la ventana de acceso se cerró (sesión iniciada).</summary>
    Public Function IniciarSesion(usuario As (Login As String, Clave As String), Optional empresa As String = UsuariosPrueba.Empresa) As Boolean
        _app.Escribir("txtEmpresa", empresa)
        _app.Escribir("txtUsuario", usuario.Login)
        _app.Escribir("txtClave", usuario.Clave)
        _app.Pulsar("btnEntrar")
        Dim r = Retry.WhileTrue(Function() _app.Existe("FormAcceso") AndAlso _app.Mensaje() Is Nothing, AplicacionE2E.Espera, TimeSpan.FromMilliseconds(250))
        Return Not _app.Existe("FormAcceso")
    End Function
End Class

''' <summary>Ventana principal (FormPrincipal): menú según permisos, barra de estado y ventanas de trabajo.</summary>
Public NotInheritable Class PaginaPrincipal
    Private ReadOnly _app As AplicacionE2E

    ''' <summary>Menús de trabajo, en el orden en que aparecen.</summary>
    Public Shared ReadOnly MenusDeTrabajo As String() = {"mnuCatalogo", "mnuMenus", "mnuCompras", "mnuAlmacen", "mnuCierresYControl", "mnuAdministracion"}

    Public Sub New(app As AplicacionE2E)
        _app = app
        _app.Esperar("menuPrincipal")
    End Sub

    ''' <summary>Texto de la barra de estado (empresa, operación, usuario y "DUENO DEL SISTEMA").</summary>
    ''' <summary>Texto de la barra de estado. Espera a que tenga contenido: el texto se pinta después de que aparece la ventana.</summary>
    Public Function Estado() As String
        Dim barra = _app.Esperar("barraEstado")
        Dim texto As String = ""
        For intento = 1 To 50
            texto = String.Join(" | ", barra.FindAllDescendants().Select(Function(e) e.Name).Where(Function(t) Not String.IsNullOrWhiteSpace(t)))
            If texto <> "" Then Exit For
            Threading.Thread.Sleep(200)
        Next
        Return texto
    End Function

    Public Function MenuVisible(menuId As String) As Boolean
        Return AplicacionE2E.OpcionesDeMenu(_app.Esperar("menuPrincipal")).Any(Function(m) AplicacionE2E.IdDeMenu(m) = menuId)
    End Function

    ''' <summary>Opciones (AutomationId) de un menú de trabajo; vacío si el menú no está.</summary>
    Public Function Opciones(menuId As String) As List(Of String)
        If Not MenuVisible(menuId) Then Return New List(Of String)()
        Desplegar(menuId)
        Dim ids As New List(Of String)
        ' Las ventanas también exponen el menú de sistema de Windows (Restaurar, Mover, Cerrar…) en barras "SystemMenuBar":
        ' sus opciones se descartan.
        Dim sistema As New HashSet(Of String)
        For Each v In _app.Ventanas()
            For Each barra In v.FindAllDescendants(Function(cf) cf.ByAutomationId("SystemMenuBar"))
                For Each item In AplicacionE2E.OpcionesDeMenu(barra)
                    sistema.Add(AplicacionE2E.IdDeMenu(item))
                Next
            Next
        Next
        For Each v In _app.Ventanas()
            For Each e In AplicacionE2E.OpcionesDeMenu(v).Where(Function(m) Not sistema.Contains(AplicacionE2E.IdDeMenu(m)))
                Dim id = AplicacionE2E.IdDeMenu(e)
                If id IsNot Nothing AndAlso id.StartsWith("mnu", StringComparison.Ordinal) AndAlso Not MenusDeTrabajo.Contains(id) AndAlso
                   id <> "mnuSesion" AndAlso id <> "mnuVentanas" AndAlso Not ids.Contains(id) Then ids.Add(id)
            Next
        Next
        Plegar()
        Return ids
    End Function

    Private Sub Desplegar(menuId As String)
        Dim m = _app.Esperar(menuId)
        If m.Patterns.ExpandCollapse.IsSupported Then
            ' Un menú que quedó desplegado de una apertura anterior no vuelve a abrirse con Expand: se contrae antes.
            Dim patron = m.Patterns.ExpandCollapse.Pattern
            If patron.ExpandCollapseState.Value = FlaUI.Core.Definitions.ExpandCollapseState.Expanded Then patron.Collapse()
            patron.Expand()
        Else
            AplicacionE2E.Pulsar(m)
        End If
        Wait.UntilInputIsProcessed()
    End Sub

    ''' <summary>Cierra los menús desplegados contrayéndolos por UI Automation (sin teclado: la sesión puede no tener entrada).</summary>
    Private Sub Plegar()
        For Each v In _app.Ventanas()
            For Each m In AplicacionE2E.OpcionesDeMenu(v)
                If m.Patterns.ExpandCollapse.IsSupported Then
                    If m.Patterns.ExpandCollapse.Pattern.ExpandCollapseState.Value = FlaUI.Core.Definitions.ExpandCollapseState.Expanded Then
                        m.Patterns.ExpandCollapse.Pattern.Collapse()
                    End If
                End If
            Next
        Next
        Wait.UntilInputIsProcessed()
    End Sub

    ''' <summary>Abre una opción de menú y devuelve la ventana de trabajo (MDI) o el diálogo que aparezca.</summary>
    Public Function Abrir(menuId As String, opcionId As String) As AutomationElement
        Dim antes = VentanasDeTrabajo().Select(Function(w) w.AutomationId).ToList()
        Desplegar(menuId)
        AplicacionE2E.Pulsar(_app.Esperar(opcionId))
        Dim r = Retry.WhileNull(Function()
                                    If _app.Mensaje() IsNot Nothing Then Return _app.Principal()
                                    Return If(VentanasDeTrabajo().FirstOrDefault(Function(w) Not antes.Contains(w.AutomationId)), Dialogo())
                                End Function, AplicacionE2E.Espera, TimeSpan.FromMilliseconds(250))
        Wait.UntilInputIsProcessed()
        Return r.Result
    End Function

    ''' <summary>Ventanas de trabajo (hijas MDI) abiertas.</summary>
    Public Function VentanasDeTrabajo() As List(Of AutomationElement)
        Return _app.Principal().FindAllDescendants(Function(cf) cf.ByControlType(ControlType.Window)) _
            .Where(Function(w) w.AutomationId IsNot Nothing AndAlso w.AutomationId.StartsWith("Form", StringComparison.Ordinal) AndAlso w.AutomationId <> "FormPrincipal").ToList()
    End Function

    ''' <summary>Diálogo modal abierto (DialogoCampos u otra ventana de primer nivel distinta de la principal y de los mensajes).</summary>
    Public Function Dialogo() As AutomationElement
        Dim primerNivel = _app.Ventanas().FirstOrDefault(Function(v) v.AutomationId <> "FormPrincipal" AndAlso v.ClassName <> "#32770" AndAlso
                                                                     v.AutomationId IsNot Nothing AndAlso (v.AutomationId.StartsWith("Form", StringComparison.Ordinal) OrElse v.AutomationId = "DialogoCampos"))
        If primerNivel IsNot Nothing Then Return primerNivel
        ' Un diálogo con dueño (ShowDialog(Me)) aparece en UIA como hijo de su dueño, p. ej. la conexión del propietario que
        ' pide la pantalla de TI al abrirse.
        Return _app.Buscar("DialogoCampos")
    End Function

    ''' <summary>Cierra la ventana de trabajo o el diálogo indicado.</summary>
    Public Sub Cerrar(ventana As AutomationElement)
        If ventana Is Nothing OrElse ventana.AutomationId = "FormPrincipal" Then Return
        Dim w = ventana.AsWindow()
        If w.Patterns.Window.IsSupported Then
            w.Patterns.Window.Pattern.Close()
        Else
            w.Focus()
            Keyboard.Press(VirtualKeyShort.ESCAPE)
        End If
        Retry.WhileTrue(Function() _app.Existe(ventana.AutomationId), TimeSpan.FromSeconds(10))
    End Sub
End Class
