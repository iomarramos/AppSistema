Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>Pantalla de acceso: empresa, usuario y clave; después, elección de la operación de trabajo.</summary>
Public Class FormAcceso
    Inherits Form

    Private ReadOnly _empresa As New TextBox With {.Width = 260}
    Private ReadOnly _usuario As New TextBox With {.Width = 260}
    Private ReadOnly _clave As New TextBox With {.Width = 260, .UseSystemPasswordChar = True}
    Private _config As Configuracion

    Public Property Sesion As SesionUsuario
    Public ReadOnly Property ConfiguracionUsada As Configuracion
        Get
            Return _config
        End Get
    End Property

    Public Sub New(config As Configuracion)
        _config = config
        Text = "AppSistema - Acceso"
        Name = "FormAcceso"
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterScreen
        MaximizeBox = False : MinimizeBox = False
        AutoSize = True : AutoSizeMode = AutoSizeMode.GrowAndShrink

        Dim tabla As New TableLayoutPanel With {.ColumnCount = 2, .AutoSize = True, .Padding = New Padding(16), .Dock = DockStyle.Fill}
        tabla.Controls.Add(New Label With {.Text = "Sistema de menus, compras e inventarios", .AutoSize = True,
                                           .Font = New Drawing.Font(Font.FontFamily, 11, Drawing.FontStyle.Bold), .Margin = New Padding(3, 3, 3, 12)}, 0, 0)
        tabla.SetColumnSpan(tabla.GetControlFromPosition(0, 0), 2)
        _empresa.Name = "txtEmpresa" : _empresa.AccessibleName = "Empresa"
        _usuario.Name = "txtUsuario" : _usuario.AccessibleName = "Usuario"
        _clave.Name = "txtClave" : _clave.AccessibleName = "Clave"
        AgregarFila(tabla, "Empresa", _empresa, 1)
        AgregarFila(tabla, "Usuario", _usuario, 2)
        AgregarFila(tabla, "Clave", _clave, 3)
        _empresa.Text = If(config?.EmpresaPredeterminada, "")

        Dim entrar As New Button With {.Text = "Entrar", .AutoSize = True, .Name = "btnEntrar", .AccessibleName = "Iniciar sesion"}
        Dim salir As New Button With {.Text = "Salir", .AutoSize = True, .DialogResult = DialogResult.Cancel, .Name = "btnSalir", .AccessibleName = "Salir"}
        Dim conexion As New Button With {.Text = "Conexion...", .AutoSize = True, .Name = "btnConexion", .AccessibleName = "Conexion con el servidor"}
        AddHandler entrar.Click, AddressOf Entrar_Click
        AddHandler conexion.Click, Sub()
                                       Dim nueva = FormPrincipal.EditarConexion(Me, _config)
                                       If nueva IsNot Nothing Then _config = nueva
                                   End Sub
        AcceptButton = entrar : CancelButton = salir
        Dim botones As New FlowLayoutPanel With {.FlowDirection = FlowDirection.RightToLeft, .AutoSize = True, .Dock = DockStyle.Bottom, .Padding = New Padding(12)}
        botones.Controls.AddRange({salir, entrar, conexion})
        Controls.Add(tabla) : Controls.Add(botones)
        AddHandler Shown, Sub() If _empresa.Text = "" Then _empresa.Focus() Else _usuario.Focus()
    End Sub

    Private Shared Sub AgregarFila(tabla As TableLayoutPanel, etiqueta As String, control As Control, fila As Integer)
        tabla.Controls.Add(New Label With {.Text = etiqueta, .AutoSize = True, .Anchor = AnchorStyles.Left, .Margin = New Padding(3, 7, 12, 3)}, 0, fila)
        tabla.Controls.Add(control, 1, fila)
    End Sub

    Private Sub Entrar_Click(sender As Object, e As EventArgs)
        If _config Is Nothing Then
            Ui.Informar(Me, "Configure primero la conexion con el servidor de la sede.")
            Return
        End If
        Dim ok = Ui.Ejecutar(Me,
            Sub()
                Dim acceso As New ServicioAcceso(_config.CadenaConexion())
                Dim s = acceso.IniciarSesion(_empresa.Text, _usuario.Text, _clave.Text)
                If s.Operaciones.Count = 0 Then
                    Throw New AppSistema.Dominio.ReglaNegocioException("SIN_PERMISO", "El usuario no tiene operaciones asignadas. Consulte al administrador.")
                End If
                Dim op = s.Operaciones(0)
                If s.Operaciones.Count > 1 Then
                    Using d As New DialogoCampos("Operacion de trabajo")
                        d.Opciones("op", "Operacion", s.Operaciones)
                        If d.ShowDialog(Me) <> DialogResult.OK Then Return
                        op = d.Elegido(Of OperacionDisponible)("op")
                    End Using
                End If
                Sesion = acceso.SeleccionarOperacion(s, op.Id)
            End Sub)
        _clave.Clear()
        If ok AndAlso Sesion IsNot Nothing Then
            If _config.EmpresaPredeterminada <> _empresa.Text.Trim() Then
                _config.EmpresaPredeterminada = _empresa.Text.Trim()
                Try
                    _config.Guardar()
                Catch ex As Exception
                    RegistroErrores.Registrar(ex)   ' recordar la empresa es una comodidad, no un requisito
                End Try
            End If
            DialogResult = DialogResult.OK
        Else
            _clave.Focus()
        End If
    End Sub
End Class
