Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>Auditoría: quién cambió qué y cuándo (solo lectura). Las claves de usuario nunca se registran.</summary>
Partial Public Class FormAuditoria

    Private ReadOnly _servicio As ServicioAdministracion

    Public Sub New()
        InitializeComponent()
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        _servicio = New ServicioAdministracion(cadena, sesion)
        _desde.Value = Date.Today.AddDays(-7)

        AddHandler btnBuscar.Click, Sub() Buscar()
        AddHandler _filas.SelectionChanged,
            Sub()
                Dim a = Ui.Seleccionado(Of AuditoriaDto)(_filas)
                _detalle.Text = If(a Is Nothing, "", $"Antes:{vbCrLf}{a.Antes}{vbCrLf}{vbCrLf}Despues:{vbCrLf}{a.Despues}")
            End Sub
        AddHandler Load,
            Sub()
                Ui.Ejecutar(Me,
                    Sub()
                        _tabla.Items.Add("(todas)")
                        For Each t In _servicio.TablasAuditadas()
                            _tabla.Items.Add(t)
                        Next
                        _tabla.SelectedIndex = 0
                        Buscar()
                    End Sub)
            End Sub
    End Sub

    Private Sub Buscar()
        Dim tabla = If(_tabla.SelectedIndex <= 0, "", CStr(_tabla.SelectedItem))
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_filas, _servicio.ConsultarAuditoria(_desde.Value, _hasta.Value, tabla, _usuario.Text),
                                         "Fecha|Fecha", "Usuario|Usuario", "Tabla|Tabla", "RegistroId|Registro", "Accion|Accion"))
    End Sub
End Class
