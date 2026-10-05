Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Apariencia común de todas las ventanas, aplicada en un solo lugar (no en cada formulario). Toma como base las pantallas
''' del SGP (manual "SGP Local – Para Operaciones": franja con el contrato y la pantalla, barra de acciones, grilla de
''' detalle, totales al pie y leyenda de colores) y las ordena con las pautas de Windows 11 / Fluent:
''' <list type="bullet">
''' <item>Segoe UI 9,75 pt para el texto y seminegrita para títulos y encabezados de columna;</item>
''' <item>superficies claras, un solo color de acento y bordes suaves;</item>
''' <item>grillas sin cabecera de fila, filas de 32 px alternadas, números a la derecha y estados como etiquetas de color;</item>
''' <item>botones planos; los que confirman (aprobar, autorizar, entregar, cerrar) en el color de acento y los que anulan en rojo;</item>
''' <item>escala por DPI: los tamaños fijos en píxeles se ajustan a 125 %, 150 %… (AutoScaleMode.Dpi).</item>
''' </list>
''' Para cambiar la apariencia se edita este módulo; los formularios no fijan colores ni fuentes.
''' </summary>
Public Module Tema

    ' Paleta (Fluent 2, tema claro).
    Public ReadOnly Acento As Color = Color.FromArgb(15, 108, 189)        ' #0F6CBD
    Public ReadOnly AcentoOscuro As Color = Color.FromArgb(17, 94, 163)   ' #115EA3
    Public ReadOnly AcentoSuave As Color = Color.FromArgb(235, 243, 252)  ' #EBF3FC
    Public ReadOnly Navegacion As Color = Color.FromArgb(11, 31, 51)      ' #0B1F33
    Public ReadOnly NavegacionHover As Color = Color.FromArgb(24, 53, 78) ' #18354E
    Public ReadOnly Texto As Color = Color.FromArgb(36, 36, 36)           ' #242424
    Public ReadOnly TextoSecundario As Color = Color.FromArgb(97, 97, 97) ' #616161
    Public ReadOnly Fondo As Color = Color.White
    Public ReadOnly Superficie As Color = Color.FromArgb(246, 248, 251)   ' barras de acciones
    Public ReadOnly SuperficieFuerte As Color = Color.FromArgb(234, 239, 245)
    Public ReadOnly Borde As Color = Color.FromArgb(224, 224, 224)
    Public ReadOnly FilaAlterna As Color = Color.FromArgb(248, 249, 251)
    Public ReadOnly Seleccion As Color = Color.FromArgb(207, 228, 250)    ' #CFE4FA
    Public ReadOnly AvisoFondo As Color = Color.FromArgb(255, 249, 230)   ' franja de ayuda (el amarillo del SGP, suavizado)
    Public ReadOnly AvisoTexto As Color = Color.FromArgb(92, 68, 0)
    Public ReadOnly Peligro As Color = Color.FromArgb(196, 49, 75)        ' #C4314B

    Public ReadOnly Fuente As New Font("Segoe UI", 10.0F)
    Public ReadOnly FuenteSemibold As New Font("Segoe UI Semibold", 10.0F)
    Public ReadOnly FuenteTitulo As New Font("Segoe UI Semibold", 14.5F)
    Public ReadOnly FuenteHero As New Font("Segoe UI Semibold", 18.0F)

    Private Const Marca As String = "tema-aplicado"

    ''' <summary>Tipo de estado para colorear una celda de estado como etiqueta.</summary>
    Public Enum TonoEstado
        Ninguno
        Bien
        Atencion
        Problema
        Informacion
        Inactivo
    End Enum

    ''' <summary>Estados que usa el sistema, agrupados por su significado (minúsculas, sin tildes).</summary>
    Private ReadOnly Tonos As New Dictionary(Of String, TonoEstado)(StringComparer.OrdinalIgnoreCase) From {
        {"aprobada", TonoEstado.Bien}, {"aprobado", TonoEstado.Bien}, {"atendido", TonoEstado.Bien}, {"cerrado", TonoEstado.Bien},
        {"cerrada", TonoEstado.Bien}, {"contado", TonoEstado.Bien}, {"revisado", TonoEstado.Bien}, {"recibido", TonoEstado.Bien},
        {"sin diferencia", TonoEstado.Bien}, {"planificado", TonoEstado.Bien}, {"vigente", TonoEstado.Bien}, {"activa", TonoEstado.Bien},
        {"enviado", TonoEstado.Bien}, {"Nueva", TonoEstado.Bien},
        {"borrador", TonoEstado.Atencion}, {"pendiente", TonoEstado.Atencion}, {"sin contar", TonoEstado.Atencion},
        {"abierto", TonoEstado.Atencion}, {"parcial", TonoEstado.Atencion}, {"Advertencia", TonoEstado.Atencion},
        {"sin salida", TonoEstado.Atencion}, {"recibido parcial", TonoEstado.Atencion},
        {"anulado", TonoEstado.Inactivo}, {"anulada", TonoEstado.Inactivo}, {"retirada", TonoEstado.Inactivo},
        {"obsoleta", TonoEstado.Inactivo}, {"inactiva", TonoEstado.Inactivo}, {"Sin cambios", TonoEstado.Inactivo},
        {"faltante", TonoEstado.Problema}, {"error", TonoEstado.Problema}, {"conflicto", TonoEstado.Problema},
        {"no planificado", TonoEstado.Problema}, {"rechazado", TonoEstado.Problema}, {"Bloquea", TonoEstado.Problema},
        {"Con error", TonoEstado.Problema}, {"vencido", TonoEstado.Problema},
        {"sobrante", TonoEstado.Informacion}, {"calculado", TonoEstado.Informacion}, {"adicional", TonoEstado.Informacion}}

    ''' <summary>Columnas cuyo valor se muestra como etiqueta de estado.</summary>
    Private ReadOnly ColumnasEstado As New HashSet(Of String)(StringComparer.Ordinal) From {"Estado", "Resultado", "EstadoTexto", "Gravedad", "TipoPendiente"}

    Public Function Tono(valor As String) As TonoEstado
        Dim t As TonoEstado
        Return If(valor IsNot Nothing AndAlso Tonos.TryGetValue(valor.Trim(), t), t, TonoEstado.Ninguno)
    End Function

    Public Function ColoresEstado(t As TonoEstado) As (Fondo As Color, Texto As Color)
        Select Case t
            Case TonoEstado.Bien : Return (Color.FromArgb(223, 246, 221), Color.FromArgb(14, 112, 14))
            Case TonoEstado.Atencion : Return (Color.FromArgb(255, 244, 206), Color.FromArgb(138, 83, 0))
            Case TonoEstado.Problema : Return (Color.FromArgb(253, 231, 233), Color.FromArgb(177, 14, 28))
            Case TonoEstado.Informacion : Return (AcentoSuave, AcentoOscuro)
            Case TonoEstado.Inactivo : Return (SuperficieFuerte, TextoSecundario)
            Case Else : Return (Color.Empty, Color.Empty)
        End Select
    End Function

    ''' <summary>
    ''' Ventana de trabajo dentro de la principal: franja superior con la operación y el nombre de la pantalla (como el
    ''' encabezado del SGP) y el estilo del resto de controles.
    ''' </summary>
    Public Sub AplicarVentana(f As Form, operacion As String)
        If YaAplicado(f) Then Return
        Identificadores.NombrarCampos(f)
        Dim titulo = If(f.Text, "")
        Dim guion = titulo.LastIndexOf(" - ", StringComparison.Ordinal)
        If guion > 0 Then titulo = titulo.Substring(0, guion)
        Dim franja As New TableLayoutPanel With {.Name = "barraContexto", .Dock = DockStyle.Top, .Height = 52,
            .BackColor = Fondo, .Padding = New Padding(14, 0, 14, 0), .ColumnCount = 2, .RowCount = 1}
        franja.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 65))
        franja.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35))
        Dim nombre As New Label With {.Name = "lblPantalla", .Text = titulo, .Dock = DockStyle.Fill, .AutoSize = False,
            .AutoEllipsis = True, .Font = FuenteTitulo, .ForeColor = Texto, .TextAlign = ContentAlignment.MiddleLeft}
        Dim donde As New Label With {.Name = "lblOperacion", .Text = operacion, .Dock = DockStyle.Fill, .AutoSize = False,
            .AutoEllipsis = True, .ForeColor = TextoSecundario, .TextAlign = ContentAlignment.MiddleRight}
        Dim linea As New Panel With {.Dock = DockStyle.Fill, .Height = 2, .Margin = Padding.Empty, .BackColor = Acento}
        franja.Controls.Add(nombre, 0, 0) : franja.Controls.Add(donde, 1, 0)
        ' Una fila propia evita que la línea invada el título o la operación.
        franja.Controls.Add(linea, 0, 1)
        franja.SetColumnSpan(linea, 2)
        franja.RowCount = 2
        franja.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        franja.RowStyles.Add(New RowStyle(SizeType.Absolute, 2))
        f.Controls.Add(franja)   ' agregada al final: se acopla primero y queda arriba de todo
        Aplicar(f)
    End Sub

    ''' <summary>Aplica fuente, colores, escala por DPI y el estilo de cada control (una sola vez por ventana).</summary>
    Public Sub Aplicar(f As Form)
        If YaAplicado(f) Then Return
        Identificadores.NombrarCampos(f)
        f.Tag = Marca
        f.Font = Fuente
        f.BackColor = Fondo
        f.ForeColor = Texto
        ' Las ventanas se arman en código con medidas pensadas a 96 ppp; se escalan a los ppp reales de la pantalla.
        f.AutoScaleDimensions = New SizeF(96.0F, 96.0F)
        f.AutoScaleMode = AutoScaleMode.Dpi
        Estilo(f)
        f.PerformAutoScale()
    End Sub

    Private Function YaAplicado(f As Form) As Boolean
        Return Object.Equals(f.Tag, Marca)
    End Function

    Private Sub Estilo(padre As Control)
        For Each c As Control In padre.Controls
            Select Case True
                Case TypeOf c Is DataGridView   ' ya tiene el estilo desde Ui.NuevaGrilla
                Case TypeOf c Is Button : Boton(DirectCast(c, Button))
                Case TypeOf c Is FlowLayoutPanel AndAlso (c.Dock = DockStyle.Top OrElse c.Dock = DockStyle.Bottom)
                    c.BackColor = Superficie
                    c.Padding = New Padding(8, 6, 8, 6)
                    DirectCast(c, FlowLayoutPanel).WrapContents = True
                Case TypeOf c Is MenuStrip
                    Dim menu = DirectCast(c, MenuStrip)
                    menu.BackColor = Fondo
                    menu.Padding = New Padding(8, 6, 8, 6)
                    EstiloMenu(menu.Items)
                Case TypeOf c Is StatusStrip
                    c.BackColor = SuperficieFuerte
                    c.Padding = New Padding(8, 4, 8, 4)
                Case TypeOf c Is MdiClient
                    c.BackColor = Superficie
                Case TypeOf c Is GroupBox
                    c.ForeColor = Texto
                    c.Padding = New Padding(10)
                Case TypeOf c Is TabControl
                    DirectCast(c, TabControl).Padding = New Point(14, 7)
                Case TypeOf c Is ComboBox
                    Dim combo = DirectCast(c, ComboBox)
                    combo.IntegralHeight = False
                    combo.DropDownHeight = 240
                Case TypeOf c Is CheckBox OrElse TypeOf c Is RadioButton
                    c.Margin = New Padding(6)
                Case TypeOf c Is Label AndAlso Not c.AutoSize AndAlso c.Dock = DockStyle.Top
                    ' Franja de ayuda de la pantalla.
                    c.BackColor = AvisoFondo
                    c.ForeColor = AvisoTexto
                    c.Padding = New Padding(12, 6, 12, 6)
                    EtiquetaAdaptable(DirectCast(c, Label))
                Case TypeOf c Is Label AndAlso Not c.AutoSize AndAlso c.Dock = DockStyle.Bottom
                    ' Totales y resúmenes al pie, como en el SGP.
                    c.BackColor = SuperficieFuerte
                    c.Font = FuenteSemibold
                    c.Padding = New Padding(12, 6, 12, 6)
                    EtiquetaAdaptable(DirectCast(c, Label))
                Case TypeOf c Is SplitContainer
                    Dim s = DirectCast(c, SplitContainer)
                    s.BackColor = Borde
                    s.SplitterWidth = 6
                    s.Panel1.BackColor = Fondo
                    s.Panel2.BackColor = Fondo
                Case TypeOf c Is TextBox
                    Dim t = DirectCast(c, TextBox)
                    t.BorderStyle = BorderStyle.FixedSingle
                    t.BackColor = If(t.ReadOnly, SuperficieFuerte, Superficie)
                    t.ForeColor = Texto
                Case TypeOf c Is ComboBox
                    Dim combo = DirectCast(c, ComboBox)
                    combo.FlatStyle = FlatStyle.Flat
                    combo.BackColor = Superficie
                    combo.ForeColor = Texto
                Case TypeOf c Is GroupBox
                    c.Font = FuenteSemibold
                    c.ForeColor = Texto
                    c.BackColor = Superficie
                Case TypeOf c Is TabControl
                    c.Font = FuenteSemibold
            End Select
            If c.HasChildren AndAlso Not TypeOf c Is DataGridView Then Estilo(c)
        Next
    End Sub

    Private Sub EtiquetaAdaptable(etiqueta As Label)
        Dim ajustar As Action = Sub()
                                    Dim ancho = etiqueta.ClientSize.Width - etiqueta.Padding.Horizontal
                                    If ancho <= 0 Then Return
                                    Dim medida = TextRenderer.MeasureText(etiqueta.Text, etiqueta.Font,
                                        New Size(ancho, Integer.MaxValue), TextFormatFlags.WordBreak Or TextFormatFlags.NoPrefix)
                                    Dim alto = Math.Max(CInt(32 * etiqueta.DeviceDpi / 96.0), medida.Height + etiqueta.Padding.Vertical)
                                    If etiqueta.Height <> alto Then etiqueta.Height = alto
                                End Sub
        AddHandler etiqueta.SizeChanged, Sub() ajustar()
        AddHandler etiqueta.TextChanged, Sub() ajustar()
        ajustar()
    End Sub

    Private Sub EstiloMenu(items As ToolStripItemCollection)
        For Each item As ToolStripItem In items
            item.ForeColor = Texto
            Dim opcion = TryCast(item, ToolStripMenuItem)
            If opcion Is Nothing Then Continue For
            opcion.Padding = New Padding(8, 5, 8, 5)
            EstiloMenu(opcion.DropDownItems)
        Next
    End Sub

    Private Sub Boton(b As Button)
        b.FlatStyle = FlatStyle.Flat
        b.FlatAppearance.BorderColor = Color.FromArgb(209, 209, 209)
        b.FlatAppearance.MouseOverBackColor = SuperficieFuerte
        b.FlatAppearance.MouseDownBackColor = Borde
        b.BackColor = Fondo
        b.ForeColor = Texto
        b.Padding = New Padding(8, 2, 8, 2)
        b.MinimumSize = New Size(b.MinimumSize.Width, Math.Max(b.MinimumSize.Height, 34))
        b.Cursor = Cursors.Hand
        Dim primera = b.Text.Split(" "c)(0).TrimEnd("."c)
        If {"Aprobar", "Autorizar", "Entregar", "Cerrar", "Guardar", "Aceptar", "Importar", "Ingresar", "Entrar"}.Contains(primera) AndAlso
           Not b.Text.StartsWith("Cerrar vigencia", StringComparison.Ordinal) Then
            ' Acción que confirma: color de acento, como el botón principal de Fluent.
            b.BackColor = Acento
            b.ForeColor = Color.White
            b.FlatAppearance.BorderColor = Acento
            b.FlatAppearance.MouseOverBackColor = AcentoOscuro
            b.FlatAppearance.MouseDownBackColor = AcentoOscuro
        ElseIf {"Anular", "Eliminar", "Quitar", "Desactivar", "Retirar"}.Contains(primera) Then
            b.ForeColor = Peligro
        End If
    End Sub

    ''' <summary>Grilla al estilo de tabla de datos: encabezado sobrio, filas alternas, números a la derecha y estados en color.</summary>
    Public Sub Grilla(g As DataGridView)
        g.BorderStyle = BorderStyle.None
        g.BackgroundColor = Fondo
        g.GridColor = Color.FromArgb(237, 237, 237)
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        g.EnableHeadersVisualStyles = False
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        g.ColumnHeadersHeight = 38
        With g.ColumnHeadersDefaultCellStyle
            .BackColor = SuperficieFuerte
            .ForeColor = Texto
            .Font = FuenteSemibold
            .SelectionBackColor = SuperficieFuerte
            .SelectionForeColor = Texto
            .Padding = New Padding(6, 0, 6, 0)
            .WrapMode = DataGridViewTriState.True
        End With
        With g.DefaultCellStyle
            .Font = Fuente
            .ForeColor = Texto
            .BackColor = Fondo
            .SelectionBackColor = Seleccion
            .SelectionForeColor = Texto
            .Padding = New Padding(6, 0, 6, 0)
        End With
        g.AlternatingRowsDefaultCellStyle.BackColor = FilaAlterna
        g.RowTemplate.Height = 32
        g.RowHeadersVisible = False
        g.ShowCellToolTips = True
        AddHandler g.DataBindingComplete, Sub() AlinearNumeros(g)
        AddHandler g.CellPainting, Sub(s, e) PintarEstado(g, e)
        AddHandler g.Paint, Sub(s, e)
                                If g.Rows.Count > 0 Then Return
                                Dim area As New Rectangle(16, g.ColumnHeadersHeight + 24,
                                    Math.Max(0, g.ClientSize.Width - 32), Math.Max(0, g.ClientSize.Height - g.ColumnHeadersHeight - 24))
                                TextRenderer.DrawText(e.Graphics, "Sin registros para mostrar. Revise los filtros o seleccione un elemento en la lista anterior.",
                                    Fuente, area, TextoSecundario, TextFormatFlags.HorizontalCenter Or TextFormatFlags.WordBreak)
                            End Sub
        AlinearNumeros(g)
    End Sub

    Private Sub AlinearNumeros(g As DataGridView)
        For Each c As DataGridViewColumn In g.Columns
            Dim tipo = If(c.ValueType, GetType(Object))
            tipo = If(Nullable.GetUnderlyingType(tipo), tipo)
            Dim numerico = tipo Is GetType(Long) OrElse tipo Is GetType(Integer) OrElse tipo Is GetType(Decimal) OrElse tipo Is GetType(Double)
            If numerico AndAlso Not c.DataPropertyName.EndsWith("Id", StringComparison.Ordinal) Then
                c.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                c.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight
            End If
        Next
    End Sub

    ''' <summary>Dibuja el valor de las columnas de estado como una etiqueta redondeada de color (leyenda del SGP).</summary>
    Private Sub PintarEstado(g As DataGridView, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Return
        If Not ColumnasEstado.Contains(g.Columns(e.ColumnIndex).DataPropertyName) Then Return
        Dim texto = If(e.FormattedValue, "").ToString()
        Dim t = Tono(texto)
        If t = TonoEstado.Ninguno Then Return
        If e.CellBounds.Width < 28 OrElse e.CellBounds.Height < 20 Then Return
        e.Paint(e.CellBounds, DataGridViewPaintParts.Background Or DataGridViewPaintParts.Border Or DataGridViewPaintParts.SelectionBackground)
        Dim colores = ColoresEstado(t)
        Dim medida = TextRenderer.MeasureText(texto, Fuente)
        Dim r As New Rectangle(e.CellBounds.X + 6, e.CellBounds.Y + (e.CellBounds.Height - medida.Height - 4) \ 2,
                               Math.Min(medida.Width + 10, e.CellBounds.Width - 12), medida.Height + 4)
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        Using camino = Redondeado(r, 8), pincel As New SolidBrush(colores.Fondo)
            e.Graphics.FillPath(pincel, camino)
        End Using
        TextRenderer.DrawText(e.Graphics, texto, Fuente, r, colores.Texto, TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)
        ' Mantener visible el foco de teclado incluso en las celdas pintadas a mano.
        e.Paint(e.CellBounds, DataGridViewPaintParts.Focus)
        e.Handled = True
    End Sub

    Private Function Redondeado(r As Rectangle, radio As Integer) As Drawing2D.GraphicsPath
        Dim p As New Drawing2D.GraphicsPath()
        Dim d = radio * 2
        p.AddArc(r.X, r.Y, d, d, 180, 90)
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90)
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
        p.CloseFigure()
        Return p
    End Function


    Private Sub EstiloSubmenu(item As ToolStripMenuItem)
        For Each hijo In item.DropDownItems.OfType(Of ToolStripMenuItem)()
            hijo.BackColor = Superficie
            hijo.ForeColor = Texto
            hijo.Font = Fuente
            hijo.Padding = New Padding(8, 4, 8, 4)
            If hijo.HasDropDownItems Then EstiloSubmenu(hijo)
        Next
    End Sub

    Private NotInheritable Class ColoresShell
        Inherits ProfessionalColorTable

        Public Overrides ReadOnly Property MenuStripGradientBegin As Color
            Get
                Return Navegacion
            End Get
        End Property
        Public Overrides ReadOnly Property MenuStripGradientEnd As Color
            Get
                Return Navegacion
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemSelected As Color
            Get
                Return NavegacionHover
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemSelectedGradientBegin As Color
            Get
                Return NavegacionHover
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemSelectedGradientEnd As Color
            Get
                Return NavegacionHover
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemPressedGradientBegin As Color
            Get
                Return Superficie
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemPressedGradientEnd As Color
            Get
                Return Superficie
            End Get
        End Property
        Public Overrides ReadOnly Property MenuItemBorder As Color
            Get
                Return Acento
            End Get
        End Property
        Public Overrides ReadOnly Property ToolStripDropDownBackground As Color
            Get
                Return Superficie
            End Get
        End Property
        Public Overrides ReadOnly Property ImageMarginGradientBegin As Color
            Get
                Return SuperficieFuerte
            End Get
        End Property
        Public Overrides ReadOnly Property ImageMarginGradientMiddle As Color
            Get
                Return SuperficieFuerte
            End Get
        End Property
        Public Overrides ReadOnly Property ImageMarginGradientEnd As Color
            Get
                Return SuperficieFuerte
            End Get
        End Property
        Public Overrides ReadOnly Property MenuBorder As Color
            Get
                Return Borde
            End Get
        End Property
        Public Overrides ReadOnly Property SeparatorDark As Color
            Get
                Return Borde
            End Get
        End Property
        Public Overrides ReadOnly Property SeparatorLight As Color
            Get
                Return Superficie
            End Get
        End Property
    End Class

End Module
