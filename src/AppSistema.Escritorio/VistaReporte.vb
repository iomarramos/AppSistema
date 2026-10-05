Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>
''' Vista previa de un reporte con el formato de las capturas del SGP: barra de impresión, Excel, zoom y páginas. La hoja
''' tiene el título, los datos de cabecera en pares etiqueta-valor, tablas con la fila de encabezado en amarillo, totales en
''' negrita, notas y firmas, y repite el encabezado de cada tabla cuando continúa en otra hoja.
''' </summary>
Public NotInheritable Class VistaReporte
    Inherits Form

    Private ReadOnly _reporte As Reporte
    Private _paginador As PaginadorReporte
    Private _paginaImpresa As Integer
    Private ReadOnly _documento As New PrintDocument()
    Private ReadOnly _vista As New PrintPreviewControl With {.Dock = DockStyle.Fill, .UseAntiAlias = True, .BackColor = Color.FromArgb(120, 120, 120)}
    Private ReadOnly _etiquetaPagina As New ToolStripLabel("Pagina 1 de 1")
    Private ReadOnly _estado As New Label With {.Dock = DockStyle.Bottom, .Height = 26, .Padding = New Padding(6, 6, 6, 0)}

    Public Sub New(r As Reporte)
        _reporte = r
        Text = "Vista previa - " & r.Titulo
        Size = New Size(1000, 760)
        StartPosition = FormStartPosition.CenterParent
        ShowInTaskbar = False

        Orientar(r.Horizontal)

        _documento.DocumentName = r.NombreArchivo("pdf")
        AddHandler _documento.BeginPrint, Sub(s, e) _paginaImpresa = 0
        AddHandler _documento.PrintPage, Sub(s, e)
                                              _paginaImpresa += 1
                                              _paginador.Dibujar(e.Graphics, _paginaImpresa)
                                              e.HasMorePages = _paginaImpresa < _paginador.Paginas
                                          End Sub
        _vista.Document = _documento
        _estado.Text = $"Generado por {r.GeneradoPor} el {r.GeneradoEn:dd/MM/yyyy HH:mm}. Celda vacia = sin dato (no es cero)."

        Dim barra As New ToolStrip With {.GripStyle = ToolStripGripStyle.Hidden, .Dock = DockStyle.Top}
        barra.Items.Add(New ToolStripButton("Imprimir...", Nothing, Sub() Imprimir()))
        barra.Items.Add(New ToolStripButton("Exportar a Excel (CSV)...", Nothing, Sub() ExportarCsv()))
        barra.Items.Add(New ToolStripSeparator())
        barra.Items.Add(New ToolStripButton("Pagina anterior", Nothing, Sub(s, e) IrAPagina(-1)))
        barra.Items.Add(_etiquetaPagina)
        barra.Items.Add(New ToolStripButton("Pagina siguiente", Nothing, Sub(s, e) IrAPagina(1)))
        barra.Items.Add(New ToolStripSeparator())
        barra.Items.Add(New ToolStripButton("Girar hoja (vertical / horizontal)", Nothing, Sub(s, e) Orientar(Not _documento.DefaultPageSettings.Landscape)))
        barra.Items.Add(New ToolStripButton("Ajustar a la ventana", Nothing, Sub() _vista.AutoZoom = True))
        barra.Items.Add(New ToolStripButton("100 %", Nothing, Sub() Zoom(1.0 / _vista.Zoom)))
        barra.Items.Add(New ToolStripButton("Acercar", Nothing, Sub() Zoom(1.25)))
        barra.Items.Add(New ToolStripButton("Alejar", Nothing, Sub() Zoom(0.8)))
        barra.Items.Add(New ToolStripSeparator())
        barra.Items.Add(New ToolStripButton("Cerrar", Nothing, Sub() Close()))

        Controls.Add(_vista)
        Controls.Add(_estado)
        Controls.Add(barra)
        AddHandler Load, Sub() ActualizarPagina()
        AddHandler _vista.StartPageChanged, Sub() ActualizarPagina()
        Tema.Aplicar(Me)
    End Sub

    Public Shared Sub Mostrar(dueno As Form, r As Reporte)
        Using v As New VistaReporte(r)
            v.ShowDialog(dueno)
        End Using
    End Sub

    ''' <summary>Pone la hoja vertical u horizontal (la impresión usa la misma) y vuelve a paginar el reporte.</summary>
    Private Sub Orientar(horizontal As Boolean)
        _documento.DefaultPageSettings.Landscape = horizontal
        ' Las hojas horizontales usan márgenes angostos (0,4 pulgadas) para que quepan los días del mes.
        If horizontal Then _documento.DefaultPageSettings.Margins = New Margins(40, 40, 40, 40)
        Dim pagina = _documento.DefaultPageSettings
        Dim bounds = pagina.Bounds
        Dim margenes = pagina.Margins
        Dim area = New Rectangle(margenes.Left, margenes.Top, bounds.Width - margenes.Left - margenes.Right, bounds.Height - margenes.Top - margenes.Bottom)
        If _paginador IsNot Nothing Then _paginador.Liberar()
        _paginador = New PaginadorReporte(_reporte, area)
        ' Hoja horizontal: a tamaño fijo (1,25) para que la barra de desplazamiento horizontal permita recorrer la tabla completa.
        ' Hoja vertical: ajustada a la ventana.
        _vista.AutoZoom = Not horizontal
        If horizontal Then _vista.Zoom = 1.25
        _vista.StartPage = 0
        _vista.InvalidatePreview()
        ActualizarPagina()
    End Sub

    Private Sub ActualizarPagina()
        Dim total = Math.Max(1, _paginador.Paginas)
        _etiquetaPagina.Text = $"Pagina {_vista.StartPage + 1} de {total}"
    End Sub

    Private Sub IrAPagina(desplazamiento As Integer)
        Dim nueva = Math.Max(0, Math.Min(_paginador.Paginas - 1, _vista.StartPage + desplazamiento))
        _vista.StartPage = nueva
        ActualizarPagina()
    End Sub

    Private Sub Zoom(factor As Double)
        _vista.AutoZoom = False
        _vista.Zoom = Math.Max(0.25, Math.Min(4.0, _vista.Zoom * factor))
    End Sub

    Private Sub Imprimir()
        Using d As New PrintDialog With {.Document = _documento, .AllowSomePages = False}
            If d.ShowDialog(Me) = DialogResult.OK Then _documento.Print()
        End Using
    End Sub

    Private Sub ExportarCsv()
        Using d As New SaveFileDialog With {.Filter = "CSV para Excel (*.csv)|*.csv", .FileName = _reporte.NombreArchivo("csv")}
            If d.ShowDialog(Me) = DialogResult.OK Then File.WriteAllText(d.FileName, _reporte.ACsv(), New UTF8Encoding(True))
        End Using
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        _paginador.Liberar()
        _documento.Dispose()
        MyBase.OnFormClosed(e)
    End Sub

End Class

''' <summary>Tipo de cada bloque de la hoja.</summary>
Friend Enum TipoBloque
    Titulo
    Dato
    Seccion
    Cabecera
    Fila
    Totales
    Nota
    Firmas
    Espacio
End Enum

''' <summary>Un bloque de la hoja, con su altura medida y, en las tablas, el ancho de cada columna.</summary>
Friend NotInheritable Class Bloque
    Public Property Tipo As TipoBloque
    Public Property Textos As String()
    Public Property Anchos As Single()
    Public Property AlineaDerecha As Boolean()
    Public Property Alto As Single
    Public Property Etiqueta As String
    Public Property Valor As String
End Class

''' <summary>Una hoja: rango de bloques y, si la tabla continúa, el encabezado que se repite arriba.</summary>
Friend NotInheritable Class DefinicionPagina
    Public Property Inicio As Integer
    Public Property Fin As Integer
    Public Property Repetir As Bloque
End Class

''' <summary>
''' Pagina el reporte con las medidas reales del texto. Las medidas usan unidades de 1/100 de pulgada, las mismas que la
''' impresora, para que la vista previa y la impresión coincidan.
''' </summary>
Friend NotInheritable Class PaginadorReporte

    Private Shared ReadOnly Invariante As CultureInfo = CultureInfo.InvariantCulture
    Private ReadOnly _r As Reporte
    Private ReadOnly _area As Rectangle
    Private ReadOnly _pie As Single = 40
    Private ReadOnly _bloques As New List(Of Bloque)()
    Private ReadOnly _paginas As New List(Of DefinicionPagina)()
    Private ReadOnly _fTitulo As New Font("Arial", 13, FontStyle.Bold, GraphicsUnit.Point)
    Private ReadOnly _fSeccion As New Font("Arial", 9.5F, FontStyle.Bold, GraphicsUnit.Point)
    Private _fNormal As Font
    Private _fNegrita As Font
    Private _fPie As Font
    Private ReadOnly _amarillo As Brush = New SolidBrush(Color.FromArgb(255, 255, 153))
    Private ReadOnly _tinta As Brush = Brushes.Black
    Private ReadOnly _lapiz As New Pen(Color.FromArgb(150, 150, 150), 1)
    Private ReadOnly _lapizFuerte As New Pen(Color.Black, 2)

    Public Sub New(r As Reporte, area As Rectangle)
        _r = r
        _area = area
        ' Letra según el número de columnas: con muchas (menú del mes) va chica; con pocas, legible como en el SGP.
        Dim columnas = If(r.Secciones.Count = 0, 0, r.Secciones.Max(Function(x) x.Columnas.Count))
        Dim tamano = If(columnas > 12, 7.5F, 9.5F)
        _fNormal = New Font("Arial", tamano, FontStyle.Regular, GraphicsUnit.Point)
        _fNegrita = New Font("Arial", tamano, FontStyle.Bold, GraphicsUnit.Point)
        _fPie = New Font("Arial", 7.5F, FontStyle.Regular, GraphicsUnit.Point)
        Using bmp As New Bitmap(1, 1)
            bmp.SetResolution(100, 100)
            Using g = Graphics.FromImage(bmp)
                g.PageUnit = GraphicsUnit.Display
                Medir(g)
            End Using
        End Using
        Paginar()
    End Sub

    Public ReadOnly Property Paginas As Integer
        Get
            Return _paginas.Count
        End Get
    End Property

    Public Sub Liberar()
        _fTitulo.Dispose() : _fSeccion.Dispose() : _fNormal.Dispose() : _fNegrita.Dispose() : _fPie.Dispose()
        _amarillo.Dispose() : _lapiz.Dispose() : _lapizFuerte.Dispose()
    End Sub

    ' ---------- Medidas: se arman los bloques con su altura y sus anchos ----------

    Private Sub Medir(g As Graphics)
        Dim altoLinea = _fNormal.GetHeight(g) + 4
        Dim altoTitulo = _fTitulo.GetHeight(g) + 8
        Dim altoSeccion = _fSeccion.GetHeight(g) + 8

        _bloques.Add(New Bloque With {.Tipo = TipoBloque.Titulo, .Textos = {_r.Titulo}, .Alto = altoTitulo})
        For Each d In _r.Encabezado
            _bloques.Add(New Bloque With {.Tipo = TipoBloque.Dato, .Etiqueta = d.Key, .Valor = d.Value, .Alto = altoLinea})
        Next
        _bloques.Add(New Bloque With {.Tipo = TipoBloque.Espacio, .Alto = altoLinea})

        For Each s In _r.Secciones
            If Not String.IsNullOrWhiteSpace(s.Titulo) Then _bloques.Add(New Bloque With {.Tipo = TipoBloque.Seccion, .Textos = {s.Titulo}, .Alto = altoSeccion})
            Dim anchos = CalcularAnchos(g, s)
            Dim derecha = s.Columnas.Select(Function(c) c.Formato <> FormatoColumna.Texto AndAlso c.Formato <> FormatoColumna.Fecha).ToArray()
            Dim altoFila = Math.Max(altoLinea, CSng(s.AlturaFila))
            _bloques.Add(New Bloque With {.Tipo = TipoBloque.Cabecera, .Textos = s.Columnas.Select(Function(c) c.Nombre).ToArray(),
                                          .Anchos = anchos, .AlineaDerecha = derecha, .Alto = altoFila})
            For Each fila In s.Filas
                _bloques.Add(New Bloque With {.Tipo = TipoBloque.Fila, .Textos = Textos(s, fila), .Anchos = anchos, .AlineaDerecha = derecha, .Alto = altoFila})
            Next
            If s.Totales IsNot Nothing Then
                _bloques.Add(New Bloque With {.Tipo = TipoBloque.Totales, .Textos = Textos(s, s.Totales), .Anchos = anchos, .AlineaDerecha = derecha, .Alto = altoFila + 4})
            End If
            _bloques.Add(New Bloque With {.Tipo = TipoBloque.Espacio, .Alto = altoLinea})
        Next

        For Each n In _r.Notas
            _bloques.Add(New Bloque With {.Tipo = TipoBloque.Nota, .Textos = {n}, .Alto = altoLinea})
        Next
        If _r.Firmas.Count > 0 Then
            _bloques.Add(New Bloque With {.Tipo = TipoBloque.Espacio, .Alto = altoLinea * 2})
            _bloques.Add(New Bloque With {.Tipo = TipoBloque.Firmas, .Textos = _r.Firmas.ToArray(), .Alto = 60})
        End If
    End Sub

    ''' <summary>Ancho de cada columna: el mayor entre su encabezado y sus celdas; si no caben en la hoja, se reducen en proporción.</summary>
    Private Function CalcularAnchos(g As Graphics, s As SeccionReporte) As Single()
        Dim anchos(s.Columnas.Count - 1) As Single
        For i = 0 To s.Columnas.Count - 1
            Dim mayor = g.MeasureString(s.Columnas(i).Nombre, _fNegrita).Width
            For Each fila In s.Filas
                mayor = Math.Max(mayor, g.MeasureString(Celda(s, i, fila(i)), _fNormal).Width)
            Next
            If s.Totales IsNot Nothing AndAlso s.Totales(i) IsNot Nothing Then mayor = Math.Max(mayor, g.MeasureString(Celda(s, i, s.Totales(i)), _fNegrita).Width)
            anchos(i) = mayor + 10
        Next
        Dim total = anchos.Sum()
        If total > _area.Width Then
            ' La primera columna (nombres) conserva su ancho, hasta un tercio de la hoja; las demás se reducen para caber.
            Dim primera = Math.Min(anchos(0), _area.Width / 3.0F)
            Dim resto = anchos.Skip(1).Sum()
            Dim factor = (_area.Width - primera) / resto
            anchos(0) = primera
            For i = 1 To anchos.Length - 1 : anchos(i) = anchos(i) * factor : Next
        End If
        Return anchos
    End Function

    Private Shared Function Textos(s As SeccionReporte, fila As Object()) As String()
        Return fila.Select(Function(v, i) Celda(s, i, v)).ToArray()
    End Function

    Private Shared Function Celda(s As SeccionReporte, indice As Integer, valor As Object) As String
        Dim formato = s.Columnas(indice).Formato
        If valor Is Nothing Then Return ""
        ' Como en el SGP: las cantidades con tres decimales ("40.800"); el dinero y los precios con dos ("264.71").
        If formato = FormatoColumna.Cantidad AndAlso TypeOf valor Is Long Then
            Return (CLng(valor) / 1000000D).ToString("#,##0.000", Invariante)
        End If
        Return Reporte.Valor(valor, formato, Invariante, True)
    End Function

    ' ---------- Paginación: se corta la hoja cuando el siguiente bloque no cabe ----------

    Private Sub Paginar()
        Dim alturaHoja = _area.Height - _pie
        Dim actual As New DefinicionPagina With {.Inicio = 0}
        Dim y As Single = 0
        Dim cabecera As Bloque = Nothing
        For i = 0 To _bloques.Count - 1
            Dim b = _bloques(i)
            If y + b.Alto > alturaHoja AndAlso i > actual.Inicio Then
                actual.Fin = i - 1
                _paginas.Add(actual)
                Dim repetir = If(cabecera IsNot Nothing AndAlso (b.Tipo = TipoBloque.Fila OrElse b.Tipo = TipoBloque.Totales), cabecera, Nothing)
                actual = New DefinicionPagina With {.Inicio = i, .Repetir = repetir}
                y = If(repetir Is Nothing, 0, repetir.Alto)
            End If
            y += b.Alto
            Select Case b.Tipo
                Case TipoBloque.Cabecera : cabecera = b
                Case TipoBloque.Seccion, TipoBloque.Totales : cabecera = Nothing
            End Select
        Next
        actual.Fin = _bloques.Count - 1
        _paginas.Add(actual)
    End Sub

    ' ---------- Dibujo ----------

    Public Sub Dibujar(g As Graphics, numeroPagina As Integer)
        g.PageUnit = GraphicsUnit.Display
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit
        If numeroPagina < 1 OrElse numeroPagina > _paginas.Count Then Return
        Dim def = _paginas(numeroPagina - 1)
        Dim y As Single = _area.Top
        If def.Repetir IsNot Nothing Then
            DibujarBloque(g, def.Repetir, _area.Left, y)
            y += def.Repetir.Alto
        End If
        For i = def.Inicio To def.Fin
            DibujarBloque(g, _bloques(i), _area.Left, y)
            y += _bloques(i).Alto
        Next
        Dim pieY = _area.Bottom - _pie + 10
        g.DrawString($"Pagina {numeroPagina} de {_paginas.Count}", _fPie, _tinta, _area.Right, pieY, New StringFormat With {.Alignment = StringAlignment.Far})
        g.DrawString($"{_r.Titulo} - generado {_r.GeneradoEn:dd/MM/yyyy HH:mm}", _fPie, _tinta, _area.Left, pieY)
    End Sub

    Private Sub DibujarBloque(g As Graphics, b As Bloque, x As Single, y As Single)
        Select Case b.Tipo
            Case TipoBloque.Titulo
                g.DrawString(b.Textos(0), _fTitulo, _tinta, x, y + 2)
            Case TipoBloque.Dato
                g.DrawString(b.Etiqueta, _fNegrita, _tinta, x, y)
                g.DrawString(b.Valor, _fNormal, _tinta, x + 180, y)
            Case TipoBloque.Seccion
                g.DrawString(b.Textos(0), _fSeccion, _tinta, x, y + 2)
            Case TipoBloque.Cabecera
                DibujarFila(g, b, x, y, _amarillo, _fNegrita, True)
            Case TipoBloque.Fila
                DibujarFila(g, b, x, y, Nothing, _fNormal, False)
            Case TipoBloque.Totales
                g.DrawLine(_lapizFuerte, x, y, x + b.Anchos.Sum(), y)
                DibujarFila(g, b, x, y + 2, Nothing, _fNegrita, False)
            Case TipoBloque.Nota
                g.DrawString("- " & b.Textos(0), _fNormal, _tinta, x, y)
            Case TipoBloque.Firmas
                DibujarFirmas(g, b, x, y)
        End Select
    End Sub

    Private Sub DibujarFila(g As Graphics, b As Bloque, x As Single, y As Single, fondo As Brush, fuente As Font, conBorde As Boolean)
        Dim ancho = b.Anchos.Sum()
        If fondo IsNot Nothing Then g.FillRectangle(fondo, x, y, ancho, b.Alto)
        Dim cx = x
        For i = 0 To b.Anchos.Length - 1
            Dim formato As New StringFormat With {.Trimming = StringTrimming.EllipsisCharacter, .FormatFlags = StringFormatFlags.NoWrap,
                                                  .LineAlignment = StringAlignment.Center,
                                                  .Alignment = If(b.AlineaDerecha(i), StringAlignment.Far, StringAlignment.Near)}
            Dim rect As New RectangleF(cx + 4, y, b.Anchos(i) - 8, b.Alto)
            g.DrawString(If(i < b.Textos.Length, b.Textos(i), ""), fuente, _tinta, rect, formato)
            formato.Dispose()
            cx += b.Anchos(i)
        Next
        If conBorde Then g.DrawRectangle(_lapiz, x, y, ancho, b.Alto)
        g.DrawLine(_lapiz, x, y + b.Alto, x + ancho, y + b.Alto)
    End Sub

    Private Sub DibujarFirmas(g As Graphics, b As Bloque, x As Single, y As Single)
        Dim anchoCuadro As Single = 200
        Dim separacion As Single = 40
        Dim cx = x
        For Each etiqueta In b.Textos
            g.DrawLine(_lapiz, cx, y + 36, cx + anchoCuadro, y + 36)
            g.DrawString(etiqueta, _fNormal, _tinta, cx + anchoCuadro / 2, y + 40, New StringFormat With {.Alignment = StringAlignment.Center})
            cx += anchoCuadro + separacion
        Next
    End Sub

End Class
