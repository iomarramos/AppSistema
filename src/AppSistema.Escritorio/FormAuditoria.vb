Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>Auditoría: quién cambió qué y cuándo (solo lectura). Las claves de usuario nunca se registran.</summary>
Public Class FormAuditoria
    Inherits Form

    Private ReadOnly _servicio As ServicioAdministracion
    Private ReadOnly _desde As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 110}
    Private ReadOnly _hasta As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 110}
    Private ReadOnly _tabla As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 200}
    Private ReadOnly _usuario As New TextBox With {.Width = 120}
    Private ReadOnly _filas As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _detalle As New TextBox With {.Dock = DockStyle.Bottom, .Height = 120, .Multiline = True, .ReadOnly = True, .ScrollBars = ScrollBars.Both}

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _servicio = New ServicioAdministracion(cadena, sesion)
        Text = "Auditoria"
        _desde.Value = Date.Today.AddDays(-7)
        Controls.Add(_filas)
        Controls.Add(_detalle)
        Controls.Add(Ui.BarraBotones(Etiqueta("Desde"), _desde, Etiqueta("Hasta"), _hasta, Etiqueta("Tabla"), _tabla, Etiqueta("Usuario"), _usuario,
                                     Ui.Boton("Buscar", AddressOf Buscar)))
        AddHandler _filas.SelectionChanged, Sub()
                                                Dim a = Ui.Seleccionado(Of AuditoriaDto)(_filas)
                                                _detalle.Text = If(a Is Nothing, "", $"Antes:{vbCrLf}{a.Antes}{vbCrLf}{vbCrLf}Despues:{vbCrLf}{a.Despues}")
                                            End Sub
        AddHandler Load, Sub() Ui.Ejecutar(Me, Sub()
                                                   _tabla.Items.Add("(todas)")
                                                   For Each t In _servicio.TablasAuditadas()
                                                       _tabla.Items.Add(t)
                                                   Next
                                                   _tabla.SelectedIndex = 0
                                                   Buscar()
                                               End Sub)
    End Sub

    Private Shared Function Etiqueta(texto As String) As Label
        Return New Label With {.Text = texto, .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}
    End Function

    Private Sub Buscar()
        Dim tabla = If(_tabla.SelectedIndex <= 0, "", CStr(_tabla.SelectedItem))
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_filas, _servicio.ConsultarAuditoria(_desde.Value, _hasta.Value, tabla, _usuario.Text),
                                         "Fecha|Fecha", "Usuario|Usuario", "Tabla|Tabla", "RegistroId|Registro", "Accion|Accion"))
    End Sub
End Class
