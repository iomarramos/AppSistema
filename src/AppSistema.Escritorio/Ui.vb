Imports System.Windows.Forms
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico

''' <summary>Utilidades comunes de la interfaz: mensajes de error, formato de cantidades y grillas.</summary>
Public Module Ui

    Public Const Titulo As String = "AppSistema"

    ''' <summary>Ejecuta una acción mostrando los errores de negocio de forma comprensible.</summary>
    Public Function Ejecutar(dueno As IWin32Window, accion As Action) As Boolean
        Try
            Cursor.Current = Cursors.WaitCursor
            accion()
            Return True
        Catch ex As Exception
            MostrarError(dueno, ex)
            Return False
        Finally
            Cursor.Current = Cursors.Default
        End Try
    End Function

    Public Sub MostrarError(dueno As IWin32Window, ex As Exception)
        Dim negocio = TryCast(ex, ReglaNegocioException)
        If negocio IsNot Nothing Then
            Dim texto = negocio.Message
            Dim i = texto.IndexOf(": ", StringComparison.Ordinal)
            If i > 0 Then texto = texto.Substring(i + 2)
            MessageBox.Show(dueno, texto & Environment.NewLine & Environment.NewLine & "(" & negocio.Codigo & ")", Titulo,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Else
            RegistroErrores.Registrar(ex)
            MessageBox.Show(dueno, "Ocurrio un error inesperado. Se guardo el detalle en el registro de errores." &
                            Environment.NewLine & ex.Message, Titulo, MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If
    End Sub

    Public Sub Informar(dueno As IWin32Window, texto As String)
        MessageBox.Show(dueno, texto, Titulo, MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Public Function Confirmar(dueno As IWin32Window, texto As String) As Boolean
        Return MessageBox.Show(dueno, texto, Titulo, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes
    End Function

    ''' <summary>Cantidad en escala u6 para mostrar (hasta 6 decimales, sin ceros sobrantes).</summary>
    Public Function Cantidad(valorU6 As Long) As String
        Return EscalaU6.ADecimal(valorU6).ToString("0.######", Globalization.CultureInfo.CurrentCulture)
    End Function

    Public Function Dinero(valorU6 As Long) As String
        Return EscalaU6.ADecimal(valorU6).ToString("N2", Globalization.CultureInfo.CurrentCulture)
    End Function

    ''' <summary>Lee un número escrito por el usuario ("4", "4,5" o "4.5"). CANTIDAD_INVALIDA si no es válido.</summary>
    Public Function LeerU6(texto As String, campo As String) As Long
        Dim r As Long
        If Not Importacion.LectorCsvCatalogo.LeerDecimalU6(If(texto, ""), r) Then
            Throw New ReglaNegocioException("CANTIDAD_INVALIDA", $"El valor de '{campo}' no es un numero valido.")
        End If
        Return r
    End Function

    Public Function LeerEntero(texto As String, campo As String) As Long
        Dim r As Long
        If Not Long.TryParse(If(texto, "").Trim(), r) Then
            Throw New ReglaNegocioException("CANTIDAD_INVALIDA", $"El valor de '{campo}' debe ser un numero entero.")
        End If
        Return r
    End Function

    ''' <summary>
    ''' Grilla de solo lectura, selección por fila, navegable con teclado. Las columnas cuyo nombre termina en
    ''' "U6" se muestran como cantidades (o como dinero si empiezan con "Precio").
    ''' </summary>
    Public Function NuevaGrilla() As DataGridView
        Dim g As New DataGridView With {
            .Dock = DockStyle.Fill, .ReadOnly = True, .AllowUserToAddRows = False, .AllowUserToDeleteRows = False,
            .AllowUserToResizeRows = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .MultiSelect = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, .RowHeadersVisible = False,
            .BackgroundColor = Drawing.SystemColors.Window, .StandardTab = True}
        AddHandler g.CellFormatting,
            Sub(s, e)
                If e.ColumnIndex < 0 Then Return
                Dim prop = g.Columns(e.ColumnIndex).DataPropertyName
                If Not prop.EndsWith("U6", StringComparison.Ordinal) Then Return
                Dim esMonto = {"Precio", "Costo", "Importe", "Total", "Valor"}.Any(Function(x) prop.StartsWith(x, StringComparison.Ordinal))
                If e.Value Is Nothing OrElse TypeOf e.Value Is DBNull Then
                    If Not esMonto Then Return
                    e.Value = "pendiente"   ' costo sin precio de referencia: nunca se muestra como cero
                ElseIf TypeOf e.Value Is Long Then
                    e.Value = If(esMonto, Dinero(CLng(e.Value)), Cantidad(CLng(e.Value)))
                Else
                    Return
                End If
                e.FormattingApplied = True
            End Sub
        AddHandler g.DataBindingComplete, Sub() AplicarColumnas(g)
        Return g
    End Function

    ''' <summary>
    ''' Enlaza la lista y muestra solo las columnas indicadas como "Propiedad|Encabezado", en ese orden.
    ''' </summary>
    Public Sub Mostrar(Of T)(grilla As DataGridView, datos As IList(Of T), ParamArray columnas() As String)
        grilla.Tag = columnas
        grilla.DataSource = New System.ComponentModel.BindingList(Of T)(datos)
        AplicarColumnas(grilla)
    End Sub

    Private Sub AplicarColumnas(grilla As DataGridView)
        Dim columnas = TryCast(grilla.Tag, String())
        If columnas Is Nothing OrElse grilla.Columns.Count = 0 Then Return
        For Each c As DataGridViewColumn In grilla.Columns
            c.Visible = False
        Next
        For i As Integer = 0 To columnas.Length - 1
            Dim partes = columnas(i).Split("|"c)
            Dim c = grilla.Columns(partes(0))
            If c Is Nothing Then Continue For
            c.Visible = True
            c.HeaderText = If(partes.Length > 1, partes(1), partes(0))
            c.DisplayIndex = i
        Next
    End Sub

    Public Function Boton(texto As String, accion As Action) As Button
        Dim b As New Button With {.Text = texto, .AutoSize = True, .Margin = New Padding(4)}
        AddHandler b.Click, Sub() accion()
        Return b
    End Function

    Public Function BarraBotones(ParamArray botones() As Control) As FlowLayoutPanel
        Dim p As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .Padding = New Padding(4), .WrapContents = True}
        p.Controls.AddRange(botones)
        Return p
    End Function

    ''' <summary>Ventana de solo lectura con una lista (reportes: necesidades, costos).</summary>
    Public Sub MostrarLista(Of T)(dueno As Form, titulo As String, encabezado As String, datos As IList(Of T), ParamArray columnas() As String)
        Dim f As New Form With {.Text = titulo, .Width = 900, .Height = 500, .StartPosition = FormStartPosition.CenterParent}
        Dim g = NuevaGrilla()
        f.Controls.Add(g)
        f.Controls.Add(New Label With {.Text = encabezado, .Dock = DockStyle.Top, .AutoSize = False, .Height = 40, .Padding = New Padding(6)})
        AddHandler f.Load, Sub() Mostrar(g, datos, columnas)
        f.ShowDialog(dueno)
    End Sub

    ''' <summary>Objeto seleccionado en una grilla enlazada a una lista, o Nothing.</summary>
    Public Function Seleccionado(Of T As Class)(grilla As DataGridView) As T
        If grilla.CurrentRow Is Nothing Then Return Nothing
        Return TryCast(grilla.CurrentRow.DataBoundItem, T)
    End Function

End Module

''' <summary>Registro local de errores inesperados (sin datos sensibles) en %LOCALAPPDATA%\AppSistema.</summary>
Public Module RegistroErrores
    Public Sub Registrar(ex As Exception)
        Try
            Dim carpeta = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppSistema")
            IO.Directory.CreateDirectory(carpeta)
            IO.File.AppendAllText(IO.Path.Combine(carpeta, "errores.log"),
                                  $"{DateTimeOffset.Now:O} {ex.GetType().FullName}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{Environment.NewLine}")
        Catch
            ' El registro no debe provocar otro error.
        End Try
    End Sub
End Module
