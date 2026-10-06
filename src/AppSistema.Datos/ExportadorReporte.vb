Imports System.Globalization
Imports System.IO
Imports AppSistema.Dominio.Numerico
Imports ClosedXML.Excel
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

''' <summary>
''' Salidas de un <see cref="Reporte"/> a Excel (.xlsx) y PDF. Un solo modelo de reporte alimenta CSV, HTML, Excel y PDF:
''' las cifras salen de la base; estos archivos son presentación, no fuente de verdad.
''' </summary>
Public NotInheritable Class ExportadorReporte
    Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture

    Private Sub New()
    End Sub

    ''' <summary>Libro de Excel: encabezado, cada sección como tabla con cifras numéricas (no texto) y totales en negrita.</summary>
    Public Shared Function AExcel(r As Reporte) As Byte()
        Using libro As New XLWorkbook()
            Dim hoja = libro.Worksheets.Add("Reporte")
            Dim fila = 1
            hoja.Cell(fila, 1).Value = r.Titulo
            hoja.Cell(fila, 1).Style.Font.Bold = True
            hoja.Cell(fila, 1).Style.Font.FontSize = 13
            fila += 1
            For Each d In r.Encabezado
                hoja.Cell(fila, 1).Value = d.Key
                hoja.Cell(fila, 1).Style.Font.Bold = True
                hoja.Cell(fila, 2).Value = d.Value
                fila += 1
            Next
            For Each s In r.Secciones
                fila += 1
                If Not String.IsNullOrEmpty(s.Titulo) Then
                    hoja.Cell(fila, 1).Value = s.Titulo
                    hoja.Cell(fila, 1).Style.Font.Bold = True
                    fila += 1
                End If
                For i = 0 To s.Columnas.Count - 1
                    hoja.Cell(fila, i + 1).Value = s.Columnas(i).Nombre
                    hoja.Cell(fila, i + 1).Style.Font.Bold = True
                    hoja.Cell(fila, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray
                Next
                fila += 1
                For Each f In s.Filas
                    EscribirFila(hoja, fila, s, f, False)
                    fila += 1
                Next
                If s.Totales IsNot Nothing Then
                    EscribirFila(hoja, fila, s, s.Totales, True)
                    fila += 1
                End If
            Next
            If r.Notas.Count > 0 Then
                fila += 1
                For Each n In r.Notas
                    hoja.Cell(fila, 1).Value = n
                    fila += 1
                Next
            End If
            hoja.Columns().AdjustToContents()
            Using ms As New MemoryStream()
                libro.SaveAs(ms)
                Return ms.ToArray()
            End Using
        End Using
    End Function

    Private Shared Sub EscribirFila(hoja As IXLWorksheet, fila As Integer, s As SeccionReporte, celdas As Object(), negrita As Boolean)
        For i = 0 To celdas.Length - 1
            Dim celda = hoja.Cell(fila, i + 1)
            celda.Value = XLCellValue.FromObject(ValorExcel(celdas(i), s.Columnas(i).Formato))
            If negrita Then celda.Style.Font.Bold = True
        Next
    End Sub

    ''' <summary>Cifra numérica para Excel cuando la columna es numérica; texto en cualquier otro caso (incluido un "TOTAL").</summary>
    Private Shared Function ValorExcel(v As Object, formato As FormatoColumna) As Object
        If v Is Nothing OrElse TypeOf v Is DBNull Then Return Nothing
        If TypeOf v Is String Then Return v
        Select Case formato
            Case FormatoColumna.Cantidad, FormatoColumna.Dinero
                Return CDec(EscalaU6.ADecimal(Convert.ToInt64(v)))
            Case FormatoColumna.Entero
                Return Convert.ToInt64(v)
            Case FormatoColumna.Fecha
                Return If(TypeOf v Is Date, DirectCast(v, Date), Date.Parse(v.ToString(), Invariante))
            Case Else
                Return Convert.ToString(v, Invariante)
        End Select
    End Function

    ''' <summary>PDF A4 apaisado (las tablas de costos son anchas): encabezado, secciones, totales, notas y firmas.</summary>
    Public Shared Function APdf(r As Reporte) As Byte()
        QuestPDF.Settings.License = LicenseType.Community
        Dim documento = Document.Create(Sub(doc)
            doc.Page(Sub(pagina)
                pagina.Size(PageSizes.A4.Landscape())
                pagina.Margin(18)
                pagina.DefaultTextStyle(TextStyle.Default.FontSize(8))
                pagina.Header().Text(r.Titulo).FontSize(13).Bold()
                pagina.Content().Column(Sub(columna)
                    For Each d In r.Encabezado
                        columna.Item().Text($"{d.Key}: {d.Value}")
                    Next
                    For Each s In r.Secciones
                        columna.Item().PaddingTop(8).Text(If(s.Titulo, "")).Bold()
                        columna.Item().Table(Sub(tabla)
                            tabla.ColumnsDefinition(Sub(cols)
                                For Each c In s.Columnas
                                    cols.RelativeColumn()
                                Next
                            End Sub)
                            tabla.Header(Sub(cabecera)
                                For Each c In s.Columnas
                                    cabecera.Cell().Background(Colors.Grey.Lighten2).Padding(2).Text(c.Nombre).Bold()
                                Next
                            End Sub)
                            For Each f In s.Filas
                                FilaPdf(tabla, s, f, False)
                            Next
                            If s.Totales IsNot Nothing Then FilaPdf(tabla, s, s.Totales, True)
                        End Sub)
                    Next
                    For Each n In r.Notas
                        columna.Item().PaddingTop(4).Text(n)
                    Next
                    If r.Firmas.Count > 0 Then
                        columna.Item().PaddingTop(36).Row(Sub(fila)
                            For Each f In r.Firmas
                                fila.RelativeItem().BorderTop(1).PaddingTop(3).AlignCenter().Text(f)
                            Next
                        End Sub)
                    End If
                End Sub)
                pagina.Footer().AlignRight().Text(Sub(t)
                    t.Span($"Generado {r.GeneradoEn:dd/MM/yyyy HH:mm} · AppSistema · Pag. ")
                    t.CurrentPageNumber()
                End Sub)
            End Sub)
        End Sub)
        Return documento.GeneratePdf()
    End Function

    Private Shared Sub FilaPdf(tabla As TableDescriptor, s As SeccionReporte, celdas As Object(), negrita As Boolean)
        For i = 0 To celdas.Length - 1
            Dim formato = s.Columnas(i).Formato
            Dim texto = Reporte.Valor(celdas(i), formato, Invariante, miles:=True)
            Dim celda = tabla.Cell().BorderBottom(0.5F).Padding(2)
            If formato <> FormatoColumna.Texto AndAlso formato <> FormatoColumna.Fecha Then celda = celda.AlignRight()
            Dim bloque = celda.Text(texto)
            If negrita Then bloque.Bold()
        Next
    End Sub
End Class
