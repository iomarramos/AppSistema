Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>
''' Salida común de los reportes: imprimir (se abre en el navegador, que imprime o guarda en PDF) o exportar a Excel (CSV
''' con separador ';' y UTF-8 con BOM para que Excel respete las tildes).
''' </summary>
Public Module SalidaReporte

    Public Sub Emitir(dueno As Form, obtener As Func(Of Reporte))
        Dim r As Reporte = Nothing
        If Not Ui.Ejecutar(dueno, Sub() r = obtener()) OrElse r Is Nothing Then Return
        Select Case Elegir(dueno, r.Titulo)
            Case DialogResult.Yes
                Ui.Ejecutar(dueno, Sub() Imprimir(r))
            Case DialogResult.No
                Using d As New SaveFileDialog With {.Filter = "CSV para Excel (*.csv)|*.csv", .FileName = r.NombreArchivo("csv")}
                    If d.ShowDialog(dueno) = DialogResult.OK Then
                        Ui.Ejecutar(dueno, Sub() File.WriteAllText(d.FileName, r.ACsv(), New UTF8Encoding(True)))
                    End If
                End Using
        End Select
    End Sub

    ''' <summary>Guarda el HTML en la carpeta temporal del usuario y lo abre con el navegador predeterminado.</summary>
    Private Sub Imprimir(r As Reporte)
        Dim carpeta = Path.Combine(Path.GetTempPath(), "AppSistema", "reportes")
        Directory.CreateDirectory(carpeta)
        Dim archivo = Path.Combine(carpeta, r.NombreArchivo("html"))
        File.WriteAllText(archivo, r.AHtml(), New UTF8Encoding(True))
        Process.Start(New ProcessStartInfo(archivo) With {.UseShellExecute = True})?.Dispose()
    End Sub

    Private Function Elegir(dueno As Form, titulo As String) As DialogResult
        Using f As New Form With {.Text = "Reporte", .FormBorderStyle = FormBorderStyle.FixedDialog, .StartPosition = FormStartPosition.CenterParent,
                                  .MinimizeBox = False, .MaximizeBox = False, .ShowInTaskbar = False, .AutoSize = True,
                                  .AutoSizeMode = AutoSizeMode.GrowAndShrink, .Padding = New Padding(10)}
            Dim imprimir As New Button With {.Text = "Imprimir", .AutoSize = True, .DialogResult = DialogResult.Yes}
            Dim exportar As New Button With {.Text = "Exportar a Excel (CSV)", .AutoSize = True, .DialogResult = DialogResult.No}
            Dim cancelar As New Button With {.Text = "Cancelar", .AutoSize = True, .DialogResult = DialogResult.Cancel}
            Dim panel As New FlowLayoutPanel With {.FlowDirection = FlowDirection.TopDown, .AutoSize = True, .WrapContents = False}
            panel.Controls.Add(New Label With {.Text = titulo, .AutoSize = True, .Margin = New Padding(3, 3, 3, 10)})
            Dim botones = Ui.BarraBotones(imprimir, exportar, cancelar)
            botones.Dock = DockStyle.None
            panel.Controls.Add(botones)
            f.Controls.Add(panel)
            f.AcceptButton = imprimir
            f.CancelButton = cancelar
            Tema.Aplicar(f)
            Return f.ShowDialog(dueno)
        End Using
    End Function

End Module
