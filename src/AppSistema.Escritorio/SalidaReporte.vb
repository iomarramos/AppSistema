Imports System.Diagnostics
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>
''' Salida común de los reportes. Un solo modelo (<see cref="Reporte"/>) sale a: impresión (HTML en el navegador, que
''' imprime o guarda PDF), Excel (.xlsx), PDF y CSV para Excel. Quien llama pasa cómo obtener el reporte; la salida
''' muestra antes la elección de formato.
''' </summary>
Public Module SalidaReporte

    Public Enum FormatoSalida
        Imprimir
        Excel
        Pdf
        Csv
    End Enum

    Public Sub Emitir(dueno As Form, obtener As Func(Of Reporte))
        Dim r As Reporte = Nothing
        If Not Ui.Ejecutar(dueno, Sub() r = obtener()) OrElse r Is Nothing Then Return
        Dim formato = Elegir(dueno, r.Titulo)
        If formato Is Nothing Then Return
        Select Case formato.Value
            Case FormatoSalida.Imprimir
                Ui.Ejecutar(dueno, Sub() Imprimir(r))
            Case FormatoSalida.Excel
                Guardar(dueno, r, "xlsx", "Libro de Excel (*.xlsx)|*.xlsx", Function() ExportadorReporte.AExcel(r))
            Case FormatoSalida.Pdf
                Guardar(dueno, r, "pdf", "Documento PDF (*.pdf)|*.pdf", Function() ExportadorReporte.APdf(r))
            Case FormatoSalida.Csv
                Guardar(dueno, r, "csv", "CSV para Excel (*.csv)|*.csv", Function() New UTF8Encoding(True).GetBytes(r.ACsv()))
        End Select
    End Sub

    ''' <summary>Pide el archivo y escribe los bytes del formato elegido. Un error se muestra al usuario, no se traga.</summary>
    Private Sub Guardar(dueno As Form, r As Reporte, extension As String, filtro As String, generar As Func(Of Byte()))
        Using d As New SaveFileDialog With {.Filter = filtro, .FileName = r.NombreArchivo(extension)}
            If d.ShowDialog(dueno) <> DialogResult.OK Then Return
            Dim destino = d.FileName
            Ui.Ejecutar(dueno, Sub() File.WriteAllBytes(destino, generar()))
        End Using
    End Sub

    ''' <summary>Guarda el HTML en la carpeta temporal del usuario y lo abre con el navegador predeterminado.</summary>
    Private Sub Imprimir(r As Reporte)
        Dim carpeta = Path.Combine(Path.GetTempPath(), "AppSistema", "reportes")
        Directory.CreateDirectory(carpeta)
        Dim archivo = Path.Combine(carpeta, r.NombreArchivo("html"))
        File.WriteAllText(archivo, r.AHtml(), New UTF8Encoding(True))
        Process.Start(New ProcessStartInfo(archivo) With {.UseShellExecute = True})?.Dispose()
    End Sub

    ''' <summary>Cuadro con un botón por formato. Nothing si el usuario cancela.</summary>
    Private Function Elegir(dueno As Form, titulo As String) As FormatoSalida?
        Dim elegido As FormatoSalida? = Nothing
        Using f As New Form With {.Text = "Reporte", .FormBorderStyle = FormBorderStyle.FixedDialog, .StartPosition = FormStartPosition.CenterParent,
                                  .MinimizeBox = False, .MaximizeBox = False, .ShowInTaskbar = False, .AutoSize = True,
                                  .AutoSizeMode = AutoSizeMode.GrowAndShrink, .Padding = New Padding(10)}
            Dim panel As New FlowLayoutPanel With {.FlowDirection = FlowDirection.TopDown, .AutoSize = True, .WrapContents = False}
            panel.Controls.Add(New Label With {.Text = titulo, .AutoSize = True, .Margin = New Padding(3, 3, 3, 10)})
            Dim alElegir = Function(formato As FormatoSalida) Sub()
                                                                elegido = formato
                                                                f.DialogResult = DialogResult.OK
                                                            End Sub
            Dim botones = Ui.BarraBotones(
                Ui.Boton("Imprimir", alElegir(FormatoSalida.Imprimir)),
                Ui.Boton("Excel (.xlsx)", alElegir(FormatoSalida.Excel)),
                Ui.Boton("PDF", alElegir(FormatoSalida.Pdf)),
                Ui.Boton("CSV para Excel", alElegir(FormatoSalida.Csv)),
                Ui.Boton("Cancelar", Sub() f.DialogResult = DialogResult.Cancel))
            botones.Dock = DockStyle.None
            panel.Controls.Add(botones)
            f.Controls.Add(panel)
            Tema.Aplicar(f)
            If f.ShowDialog(dueno) <> DialogResult.OK Then Return Nothing
        End Using
        Return elegido
    End Function

End Module
