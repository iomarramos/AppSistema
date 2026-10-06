Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>
''' Pantalla de acceso: empresa, usuario y clave; después, elección de la operación de trabajo. Los controles se diseñan
''' en FormAcceso.Designer.vb (Diseñador de Visual Studio); aquí va la lógica.
''' </summary>
Partial Public Class FormAcceso

    Private _config As Configuracion

    Public Property Sesion As SesionUsuario
    Public ReadOnly Property ConfiguracionUsada As Configuracion
        Get
            Return _config
        End Get
    End Property

    ''' <summary>Solo para el Diseñador de Visual Studio.</summary>
    Public Sub New()
        Me.New(Nothing)
    End Sub

    Public Sub New(config As Configuracion)
        InitializeComponent()
        _config = config
        Text = "AppSistema · Acceso seguro"
        BackColor = Tema.Fondo
        tabla.BackColor = Tema.Superficie
        tabla.Padding = New Padding(28, 24, 28, 18)
        botones.BackColor = Tema.Superficie
        botones.Padding = New Padding(18, 12, 18, 18)
        lblTitulo.Text = "Bienvenido a AppSistema"
        lblTitulo.Font = Tema.FuenteHero
        lblTitulo.ForeColor = Tema.Navegacion
        lblTitulo.Margin = New Padding(3, 3, 3, 18)
        _empresa.Width = 280
        _usuario.Width = 280
        _clave.Width = 280
        btnEntrar.MinimumSize = New Drawing.Size(100, 36)
        btnConexion.MinimumSize = New Drawing.Size(110, 36)
        btnSalir.MinimumSize = New Drawing.Size(90, 36)
        _empresa.Text = If(config?.EmpresaPredeterminada, "")
        AddHandler Shown, Sub() If _empresa.Text = "" Then _empresa.Focus() Else _usuario.Focus()
    End Sub

    Private Sub btnConexion_Click(sender As Object, e As EventArgs) Handles btnConexion.Click
        Dim nueva = FormPrincipal.EditarConexion(Me, _config)
        If nueva IsNot Nothing Then _config = nueva
    End Sub

    Private Sub Entrar_Click(sender As Object, e As EventArgs) Handles btnEntrar.Click
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
