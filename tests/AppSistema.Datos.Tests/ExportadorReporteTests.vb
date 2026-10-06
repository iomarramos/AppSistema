Imports System.IO
Imports System.Text
Imports ClosedXML.Excel
Imports AppSistema.Datos
Imports AppSistema.Dominio.Numerico
Imports Xunit

''' <summary>Excel y PDF salen del mismo <see cref="Reporte"/>: cifras numéricas en Excel, totales y notas, y un PDF válido.</summary>
Public Class ExportadorReporteTests

    Private Shared Function ReporteDePrueba() As Reporte
        Dim r As New Reporte("Costo de prueba")
        r.Dato("Operacion", "ARE")
        Dim s = r.Seccion("Detalle", New ColumnaReporte("Producto"), New ColumnaReporte("Cantidad", FormatoColumna.Cantidad),
                          New ColumnaReporte("Costo", FormatoColumna.Dinero), New ColumnaReporte("Fecha", FormatoColumna.Fecha))
        s.Agregar("Arroz", EscalaU6.DesdeDecimal(2.5D), EscalaU6.DesdeDecimal(10D), New Date(2026, 10, 6))
        s.Totales = {"TOTAL", EscalaU6.DesdeDecimal(2.5D), EscalaU6.DesdeDecimal(10D), Nothing}
        r.Notas.Add("Nota de prueba")
        Return r
    End Function

    <Fact>
    Public Sub Excel_guarda_cantidades_y_dinero_como_numeros_y_el_total_en_su_fila()
        Dim bytes = ExportadorReporte.AExcel(ReporteDePrueba())
        Using libro As New XLWorkbook(New MemoryStream(bytes))
            Dim hoja = libro.Worksheet("Reporte")
            Assert.Equal("Costo de prueba", hoja.Cell(1, 1).GetString())
            Assert.Equal("ARE", hoja.Cell(2, 2).GetString())
            ' Fila 4: título de la sección; fila 5: cabecera; fila 6: dato; fila 7: total; fila 9: nota.
            Assert.Equal("Detalle", hoja.Cell(4, 1).GetString())
            Assert.Equal("Arroz", hoja.Cell(6, 1).GetString())
            Assert.Equal(2.5D, hoja.Cell(6, 2).GetValue(Of Decimal)())
            Assert.Equal(10D, hoja.Cell(6, 3).GetValue(Of Decimal)())
            Assert.Equal(New Date(2026, 10, 6), hoja.Cell(6, 4).GetDateTime())
            Assert.Equal("TOTAL", hoja.Cell(7, 1).GetString())
            Assert.Equal(2.5D, hoja.Cell(7, 2).GetValue(Of Decimal)())
            Assert.Equal("Nota de prueba", hoja.Cell(9, 1).GetString())
        End Using
    End Sub

    <Fact>
    Public Sub Pdf_es_un_documento_pdf_con_contenido()
        Dim bytes = ExportadorReporte.APdf(ReporteDePrueba())
        Assert.True(bytes.Length > 1000)
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4))
    End Sub
End Class
