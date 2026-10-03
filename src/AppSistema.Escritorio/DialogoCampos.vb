Imports System.Windows.Forms

''' <summary>
''' Diálogo de captura generado en código: etiquetas + campos en una tabla, Aceptar/Cancelar con
''' Enter/Escape. Evita repetir formularios casi idénticos para cada alta.
''' </summary>
Public Class DialogoCampos
    Inherits Form

    Private ReadOnly _tabla As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 2, .AutoSize = True, .Padding = New Padding(8)}
    Private ReadOnly _campos As New Dictionary(Of String, Control)(StringComparer.Ordinal)
    Private ReadOnly _validar As Func(Of DialogoCampos, Boolean)

    Public Sub New(titulo As String, Optional validar As Func(Of DialogoCampos, Boolean) = Nothing)
        Text = titulo
        _validar = validar
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterParent
        MaximizeBox = False : MinimizeBox = False
        AutoSize = True : AutoSizeMode = AutoSizeMode.GrowAndShrink
        _tabla.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        _tabla.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, 320))

        Dim aceptar As New Button With {.Text = "Aceptar", .AutoSize = True}
        Dim cancelar As New Button With {.Text = "Cancelar", .AutoSize = True, .DialogResult = DialogResult.Cancel}
        AddHandler aceptar.Click, Sub()
                                      If _validar Is Nothing OrElse _validar(Me) Then DialogResult = DialogResult.OK
                                  End Sub
        AcceptButton = aceptar : CancelButton = cancelar
        Dim botones As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .FlowDirection = FlowDirection.RightToLeft, .AutoSize = True, .Padding = New Padding(8)}
        botones.Controls.Add(cancelar) : botones.Controls.Add(aceptar)
        Controls.Add(_tabla) : Controls.Add(botones)
    End Sub

    Private Sub Agregar(clave As String, etiqueta As String, control As Control)
        control.Dock = DockStyle.Fill
        _tabla.Controls.Add(New Label With {.Text = etiqueta, .AutoSize = True, .Anchor = AnchorStyles.Left, .Margin = New Padding(3, 7, 3, 3)})
        _tabla.Controls.Add(control)
        _campos(clave) = control
    End Sub

    Public Function Texto(clave As String, etiqueta As String, Optional valor As String = "", Optional esClave As Boolean = False) As DialogoCampos
        Agregar(clave, etiqueta, New TextBox With {.Text = valor, .UseSystemPasswordChar = esClave})
        Return Me
    End Function

    Public Function Opciones(clave As String, etiqueta As String, elementos As IEnumerable(Of Object), Optional seleccionado As Object = Nothing,
                             Optional permitirVacio As Boolean = False) As DialogoCampos
        Dim cb As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList}
        If permitirVacio Then cb.Items.Add("(ninguno)")
        For Each e In elementos
            cb.Items.Add(e)
        Next
        If seleccionado IsNot Nothing AndAlso cb.Items.Contains(seleccionado) Then
            cb.SelectedItem = seleccionado
        ElseIf cb.Items.Count > 0 Then
            cb.SelectedIndex = 0
        End If
        Agregar(clave, etiqueta, cb)
        Return Me
    End Function

    Public Function Fecha(clave As String, etiqueta As String, valor As Date, Optional opcional As Boolean = False) As DialogoCampos
        Agregar(clave, etiqueta, New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Value = valor, .ShowCheckBox = opcional, .Checked = Not opcional})
        Return Me
    End Function

    Public Function Marca(clave As String, etiqueta As String, Optional valor As Boolean = False) As DialogoCampos
        Agregar(clave, etiqueta, New CheckBox With {.Checked = valor})
        Return Me
    End Function

    Public Function Valor(clave As String) As String
        Return _campos(clave).Text.Trim()
    End Function

    Public Function ValorSinRecortar(clave As String) As String
        Return _campos(clave).Text
    End Function

    ''' <summary>Elemento elegido, o Nothing si se eligió "(ninguno)".</summary>
    Public Function Elegido(Of T As Class)(clave As String) As T
        Return TryCast(DirectCast(_campos(clave), ComboBox).SelectedItem, T)
    End Function

    Public Function FechaElegida(clave As String) As Date?
        Dim dtp = DirectCast(_campos(clave), DateTimePicker)
        If dtp.ShowCheckBox AndAlso Not dtp.Checked Then Return Nothing
        Return dtp.Value.Date
    End Function

    Public Function Marcado(clave As String) As Boolean
        Return DirectCast(_campos(clave), CheckBox).Checked
    End Function
End Class

''' <summary>Envoltorio para mostrar objetos en un ComboBox con un texto propio.</summary>
Public NotInheritable Class Opcion(Of T)
    Public ReadOnly Property Valor As T
    Private ReadOnly _texto As String

    Public Sub New(valor As T, texto As String)
        Me.Valor = valor
        _texto = texto
    End Sub

    Public Overrides Function ToString() As String
        Return _texto
    End Function
End Class
