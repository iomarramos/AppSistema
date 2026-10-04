Imports System.Globalization
Imports System.Net
Imports System.Text
Imports AppSistema.Dominio.Numerico

''' <summary>Cómo se muestra el valor de una columna. Cantidad y Dinero son enteros ×1e6.</summary>
Public Enum FormatoColumna
    Texto
    Entero
    Cantidad
    Dinero
    Fecha
End Enum

Public NotInheritable Class ColumnaReporte
    Public ReadOnly Property Nombre As String
    Public ReadOnly Property Formato As FormatoColumna

    Public Sub New(nombre As String, Optional formato As FormatoColumna = FormatoColumna.Texto)
        Me.Nombre = nombre
        Me.Formato = formato
    End Sub
End Class

''' <summary>Una tabla del reporte. Las celdas vacías se pasan como Nothing (no como cero).</summary>
Public NotInheritable Class SeccionReporte
    Public ReadOnly Property Titulo As String
    Public ReadOnly Property Columnas As IReadOnlyList(Of ColumnaReporte)
    Public ReadOnly Property Filas As New List(Of Object())
    ''' <summary>Fila de totales (opcional), con el mismo número de celdas que las columnas.</summary>
    Public Property Totales As Object()

    Public Sub New(titulo As String, ParamArray columnas As ColumnaReporte())
        Me.Titulo = titulo
        Me.Columnas = columnas
    End Sub

    Public Sub Agregar(ParamArray celdas As Object())
        If celdas.Length <> Columnas.Count Then Throw New ArgumentException($"Se esperaban {Columnas.Count} celdas y llegaron {celdas.Length}.")
        Filas.Add(celdas)
    End Sub
End Class

''' <summary>
''' Reporte imprimible o exportable: encabezado (etiqueta y valor), secciones con tablas y un pie (notas o firmas).
''' Se exporta a CSV (separador ';', números sin separador de miles y con punto decimal) o a HTML listo para imprimir.
''' </summary>
Public NotInheritable Class Reporte
    Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
    ''' <summary>Formato de Perú (miles con coma, decimales con punto) fijo, sin depender de la configuración regional de la PC.</summary>
    Private Shared ReadOnly Peru As CultureInfo = CrearFormatoPeru()

    Public ReadOnly Property Titulo As String
    Public ReadOnly Property Encabezado As New List(Of KeyValuePair(Of String, String))
    Public ReadOnly Property Secciones As New List(Of SeccionReporte)
    Public ReadOnly Property Notas As New List(Of String)
    ''' <summary>Cuadros de firma al pie (por ejemplo "Entregado por", "Recibido por").</summary>
    Public ReadOnly Property Firmas As New List(Of String)
    Public Property GeneradoEn As DateTime = DateTime.Now
    Public Property GeneradoPor As String

    Public Sub New(titulo As String)
        Me.Titulo = titulo
    End Sub

    Public Sub Dato(etiqueta As String, valor As String)
        Encabezado.Add(New KeyValuePair(Of String, String)(etiqueta, If(valor, "")))
    End Sub

    Public Function Seccion(titulo As String, ParamArray columnas As ColumnaReporte()) As SeccionReporte
        Dim s As New SeccionReporte(titulo, columnas)
        Secciones.Add(s)
        Return s
    End Function

    ''' <summary>Nombre de archivo sugerido, sin caracteres no válidos en Windows.</summary>
    Public Function NombreArchivo(extension As String) As String
        Dim limpio = New String(Titulo.Select(Function(c) If(Char.IsLetterOrDigit(c) OrElse c = "-"c, c, "_"c)).ToArray())
        Return $"{limpio}_{GeneradoEn:yyyyMMdd_HHmm}.{extension.TrimStart("."c)}"
    End Function

    Private Shared Function CrearFormatoPeru() As CultureInfo
        Dim c = DirectCast(CultureInfo.InvariantCulture.Clone(), CultureInfo)
        c.NumberFormat.NumberGroupSeparator = ","
        c.NumberFormat.NumberDecimalSeparator = "."
        Return c
    End Function

    ' ---------- CSV ----------

    Public Function ACsv() As String
        Dim sb As New StringBuilder()
        Linea(sb, {Titulo})
        For Each d In Encabezado
            Linea(sb, {d.Key, d.Value})
        Next
        For Each s In Secciones
            sb.Append(vbLf)
            If Not String.IsNullOrEmpty(s.Titulo) Then Linea(sb, {s.Titulo})
            Linea(sb, s.Columnas.Select(Function(c) c.Nombre))
            For Each f In s.Filas
                Linea(sb, f.Select(Function(v, i) Valor(v, s.Columnas(i).Formato, Invariante, miles:=False)))
            Next
            If s.Totales IsNot Nothing Then Linea(sb, s.Totales.Select(Function(v, i) Valor(v, s.Columnas(i).Formato, Invariante, miles:=False)))
        Next
        If Notas.Count > 0 Then
            sb.Append(vbLf)
            For Each n In Notas
                Linea(sb, {n})
            Next
        End If
        Return sb.ToString()
    End Function

    Private Shared Sub Linea(sb As StringBuilder, celdas As IEnumerable(Of String))
        sb.Append(String.Join(";", celdas.Select(AddressOf CeldaCsv))).Append(vbLf)
    End Sub

    Private Shared Function CeldaCsv(texto As String) As String
        Dim t = If(texto, "").Replace(vbCr, " ").Replace(vbLf, " ")
        If t.IndexOfAny({";"c, """"c}) >= 0 Then Return """" & t.Replace("""", """""") & """"
        Return t
    End Function

    ' ---------- HTML ----------

    ''' <summary>Página autónoma (sin recursos externos) con estilos de impresión A4.</summary>
    Public Function AHtml() As String
        Dim h = Function(t As String) WebUtility.HtmlEncode(If(t, ""))
        Dim sb As New StringBuilder()
        sb.Append("<!doctype html><html lang=""es""><head><meta charset=""utf-8""><title>").Append(h(Titulo)).Append("</title><style>")
        sb.Append("body{font-family:Segoe UI,Arial,sans-serif;font-size:11px;color:#000;background:#fff;margin:16px}")
        sb.Append("h1{font-size:16px;margin:0 0 6px}h2{font-size:13px;margin:14px 0 4px}")
        sb.Append(".enc{border-collapse:collapse;margin-bottom:6px}.enc td{padding:1px 10px 1px 0}.enc td:first-child{font-weight:600}")
        sb.Append("table.t{border-collapse:collapse;width:100%}table.t th,table.t td{border:1px solid #888;padding:2px 4px}")
        sb.Append("table.t th{background:#e8e8e8;text-align:left}td.n{text-align:right;white-space:nowrap}tr.tot td{font-weight:700;background:#f4f4f4}")
        sb.Append(".firmas{display:flex;gap:24px;margin-top:48px}.firmas div{flex:1;border-top:1px solid #000;text-align:center;padding-top:4px}")
        sb.Append(".pie{margin-top:12px;color:#444}.notas{margin-top:8px}")
        sb.Append("@media print{body{margin:0}.noimp{display:none}thead{display:table-header-group}tr{page-break-inside:avoid}}")
        sb.Append("@page{size:A4;margin:12mm}</style></head><body>")
        sb.Append("<p class=""noimp""><button onclick=""window.print()"">Imprimir</button></p>")
        sb.Append("<h1>").Append(h(Titulo)).Append("</h1>")
        If Encabezado.Count > 0 Then
            sb.Append("<table class=""enc"">")
            For Each d In Encabezado
                sb.Append("<tr><td>").Append(h(d.Key)).Append("</td><td>").Append(h(d.Value)).Append("</td></tr>")
            Next
            sb.Append("</table>")
        End If
        For Each s In Secciones
            If Not String.IsNullOrEmpty(s.Titulo) Then sb.Append("<h2>").Append(h(s.Titulo)).Append("</h2>")
            sb.Append("<table class=""t""><thead><tr>")
            For Each c In s.Columnas
                sb.Append("<th>").Append(h(c.Nombre)).Append("</th>")
            Next
            sb.Append("</tr></thead><tbody>")
            If s.Filas.Count = 0 Then
                sb.Append("<tr><td colspan=""").Append(s.Columnas.Count).Append(""">Sin datos.</td></tr>")
            End If
            For Each f In s.Filas
                FilaHtml(sb, s, f, "")
            Next
            If s.Totales IsNot Nothing Then FilaHtml(sb, s, s.Totales, " class=""tot""")
            sb.Append("</tbody></table>")
        Next
        If Notas.Count > 0 Then
            sb.Append("<div class=""notas"">")
            For Each n In Notas
                sb.Append("<p>").Append(h(n)).Append("</p>")
            Next
            sb.Append("</div>")
        End If
        If Firmas.Count > 0 Then
            sb.Append("<div class=""firmas"">")
            For Each f In Firmas
                sb.Append("<div>").Append(h(f)).Append("</div>")
            Next
            sb.Append("</div>")
        End If
        sb.Append("<p class=""pie"">Generado el ").Append(h(GeneradoEn.ToString("dd/MM/yyyy HH:mm", Peru)))
        If Not String.IsNullOrEmpty(GeneradoPor) Then sb.Append(" por ").Append(h(GeneradoPor))
        sb.Append(" · AppSistema</p></body></html>")
        Return sb.ToString()
    End Function

    Private Shared Sub FilaHtml(sb As StringBuilder, s As SeccionReporte, celdas As Object(), atributos As String)
        sb.Append("<tr").Append(atributos).Append(">")
        For i = 0 To celdas.Length - 1
            Dim f = s.Columnas(i).Formato
            Dim numerico = f <> FormatoColumna.Texto AndAlso f <> FormatoColumna.Fecha
            sb.Append(If(numerico, "<td class=""n"">", "<td>")).Append(WebUtility.HtmlEncode(Valor(celdas(i), f, Peru, miles:=True))).Append("</td>")
        Next
        sb.Append("</tr>")
    End Sub

    ''' <summary>Texto de una celda. Nothing queda vacío; un texto en columna numérica (por ejemplo "TOTAL") se respeta.</summary>
    Public Shared Function Valor(v As Object, formato As FormatoColumna, cultura As CultureInfo, miles As Boolean) As String
        If v Is Nothing OrElse TypeOf v Is DBNull Then Return ""
        If TypeOf v Is String Then Return DirectCast(v, String)
        Select Case formato
            Case FormatoColumna.Cantidad
                Return EscalaU6.ADecimal(Convert.ToInt64(v)).ToString(If(miles, "#,##0.###", "0.######"), cultura)
            Case FormatoColumna.Dinero
                Return EscalaU6.ADecimal(Convert.ToInt64(v)).ToString(If(miles, "#,##0.00", "0.00"), cultura)
            Case FormatoColumna.Entero
                Return Convert.ToInt64(v).ToString(If(miles, "#,##0", "0"), cultura)
            Case FormatoColumna.Fecha
                Return If(TypeOf v Is Date, DirectCast(v, Date).ToString("dd/MM/yyyy", Invariante), v.ToString())
            Case Else
                Return Convert.ToString(v, cultura)
        End Select
    End Function

End Class
