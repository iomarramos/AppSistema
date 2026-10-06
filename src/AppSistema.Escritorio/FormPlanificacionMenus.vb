Imports System.Data
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Matriz de planificación: un servicio por fila y un día por columna. Cada celda muestra los comensales y el costo por
''' comensal de la minuta. Debajo, el resumen por día (costo del día, costo por bandeja, techo y desviación). Solo se
''' cambian comensales de minutas en borrador; lo aprobado no se edita aquí.
''' </summary>
Partial Public Class FormPlanificacionMenus

    Private ReadOnly _servicio As ServicioMatrizMenu
    Private ReadOnly _minutas As ServicioMinutas
    Private _celdas As New Dictionary(Of String, CeldaMatrizDto)

    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridMatriz)
        Ui.Configurar(gridResumen)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridMatriz)
        Ui.Configurar(gridResumen)
        _servicio = New ServicioMatrizMenu(cadena, sesion)
        _minutas = New ServicioMinutas(cadena, sesion)
        Text = "Planificacion de menus (matriz) - " & sesion.Operacion.Nombre
        dtDesde.Value = Date.Today.AddDays(-Date.Today.Day + 1)
        dtHasta.Value = dtDesde.Value.AddMonths(1).AddDays(-1)
        barraAcciones.Controls.Add(Ui.Boton("Ver", AddressOf Cargar))
        barraAcciones.Controls.Add(Ui.BotonSi(sesion.Tiene(Permisos.MinutasEditar), "Actualizar comensales...", AddressOf ActualizarComensales))
        Cargar()
    End Sub

    Private Sub Cargar()
        Ui.Ejecutar(Me,
            Sub()
                Dim desde = dtDesde.Value.Date
                Dim hasta = dtHasta.Value.Date
                Dim celdas = _servicio.Celdas(desde, hasta)
                _celdas = celdas.ToDictionary(Function(c) Clave(c.OperacionServicioId, c.Fecha), Function(c) c)
                MostrarMatriz(celdas, desde, hasta)
                Ui.Mostrar(gridResumen, _servicio.ResumenPorDia(celdas).Select(Function(r) PresentarDia(r)).ToList(),
                           "Dia|Dia", "Minutas|Minutas", "Comensales|Comensales", "MinutasSinCosto|Sin costo", "Costo|Costo del dia (S/)",
                           "CostoPorBandeja|Costo por bandeja (S/)", "Techo|Techo (S/)", "Desviacion|Desviacion (S/)")
                lblEstado.Text = $"{celdas.Count} minutas. Costo y techo salen del snapshot aprobado; una minuta sin aprobar muestra 'sin costo', nunca cero."
            End Sub)
    End Sub

    ''' <summary>Filas = servicio de la operación; columnas = días del rango. Cada celda: comensales y costo por comensal.</summary>
    Private Sub MostrarMatriz(celdas As List(Of CeldaMatrizDto), desde As Date, hasta As Date)
        Dim tabla As New DataTable()
        tabla.Columns.Add("Clave", GetType(String))
        tabla.Columns.Add("Servicio", GetType(String))
        Dim dias As New List(Of Date)
        For i = 0 To (hasta - desde).Days
            Dim d = desde.AddDays(i)
            dias.Add(d)
            Dim columna = tabla.Columns.Add("D" & d.ToString("yyyyMMdd"), GetType(String))
            columna.Caption = d.ToString("dd ddd", Globalization.CultureInfo.CurrentCulture)
        Next
        For Each grupo In celdas.GroupBy(Function(c) c.OperacionServicioId)
            Dim fila = tabla.NewRow()
            Dim primera = grupo.First()
            fila("Clave") = primera.OperacionServicioId.ToString()
            fila("Servicio") = $"{primera.ServicioNombre} - {primera.RegimenNombre}"
            For Each d In dias
                Dim c As CeldaMatrizDto = Nothing
                If _celdas.TryGetValue(Clave(grupo.Key, d), c) Then
                    fila("D" & d.ToString("yyyyMMdd")) = $"{c.Comensales} com. | {If(c.CostoPorComensalU6.HasValue, "S/ " & Ui.Dinero(c.CostoPorComensalU6.Value), "sin costo")}"
                End If
            Next
            tabla.Rows.Add(fila)
        Next
        gridMatriz.DataSource = tabla
        gridMatriz.Columns("Clave").Visible = False
        ' Las columnas inmovilizadas van al inicio: la oculta "Clave" también se inmoviliza, si no, WinForms lanza
        ' "La columna no se puede agregar porque está inmovilizada y situada después de una columna que no está inmovilizada".
        gridMatriz.Columns("Clave").Frozen = True
        gridMatriz.Columns("Servicio").Frozen = True
        ' Una columna por día con ancho fijo (con "Ajustar a la grilla" la matriz se encoge hasta ilegible y el ancho no aplica).
        gridMatriz.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        gridMatriz.Columns("Servicio").Width = 220
        For Each columna As DataGridViewColumn In gridMatriz.Columns
            If columna.Name.StartsWith("D", StringComparison.Ordinal) Then columna.HeaderText = tabla.Columns(columna.Name).Caption : columna.Width = 130
        Next
    End Sub

    ''' <summary>Celda seleccionada de la matriz, o Nothing si no hay minuta ahí.</summary>
    Private Function CeldaSeleccionada() As CeldaMatrizDto
        Dim actual = gridMatriz.CurrentCell
        If actual Is Nothing OrElse actual.ColumnIndex < 0 Then Return Nothing
        Dim nombre = gridMatriz.Columns(actual.ColumnIndex).Name
        If Not nombre.StartsWith("D", StringComparison.Ordinal) Then Return Nothing
        Dim clave = TryCast(gridMatriz.Rows(actual.RowIndex).Cells("Clave").Value, String)
        Dim celda As CeldaMatrizDto = Nothing
        If clave Is Nothing Then Return Nothing
        _celdas.TryGetValue(clave & "|" & nombre.Substring(1), celda)
        Return celda
    End Function

    ''' <summary>Cambia los comensales de la minuta de la celda elegida, solo si está en borrador.</summary>
    Private Sub ActualizarComensales()
        Dim celda = CeldaSeleccionada()
        If celda Is Nothing Then Ui.Informar(Me, "Seleccione una celda con minuta.") : Return
        If celda.Estado <> "borrador" Then
            Ui.Informar(Me, $"Solo se cambian los comensales de minutas en borrador. Esta minuta esta {celda.Estado}.") : Return
        End If
        Using d As New DialogoCampos("Comensales del servicio")
            d.Texto("comensales", $"Comensales de {celda.ServicioNombre} ({celda.Fecha:dd/MM/yyyy})")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim comensales As Long
            If Not Long.TryParse(d.Valor("comensales"), comensales) OrElse comensales < 0 Then
                Ui.Informar(Me, "Los comensales deben ser un numero entero mayor o igual que cero.") : Return
            End If
            Ui.Ejecutar(Me, Sub() _minutas.ActualizarComensales(celda.MinutaId, comensales))
        End Using
        Cargar()
    End Sub

    Private Shared Function Clave(operacionServicioId As Long, fecha As Date) As String
        Return operacionServicioId.ToString() & "|" & fecha.Date.ToString("yyyyMMdd", Globalization.CultureInfo.InvariantCulture)
    End Function

    Private Shared Function PresentarDia(r As ResumenDiaDto) As FilaDia
        Return New FilaDia With {
            .Dia = r.Fecha.ToString("ddd dd/MM/yyyy", Globalization.CultureInfo.CurrentCulture),
            .Minutas = r.Minutas.ToString(), .Comensales = r.Comensales.ToString(), .MinutasSinCosto = r.MinutasSinCosto.ToString(),
            .Costo = If(r.CostoDiaU6.HasValue, Ui.Dinero(r.CostoDiaU6.Value), "sin costo"),
            .CostoPorBandeja = If(r.CostoPorComensalU6.HasValue, Ui.Dinero(r.CostoPorComensalU6.Value), "-"),
            .Techo = If(r.TechoU6.HasValue, Ui.Dinero(r.TechoU6.Value), "-"),
            .Desviacion = Firmado(r.DesviacionU6)}
    End Function

    ''' <summary>Desviación con signo visible: + por encima del techo, - por debajo (no depende solo del color).</summary>
    Private Shared Function Firmado(valorU6 As Long?) As String
        If Not valorU6.HasValue Then Return "-"
        Dim signo = If(valorU6.Value > 0, "+", If(valorU6.Value < 0, "-", ""))
        Return signo & Ui.Dinero(Math.Abs(valorU6.Value))
    End Function

    ''' <summary>Fila del resumen con los importes ya en texto.</summary>
    Public NotInheritable Class FilaDia
        Public Property Dia As String
        Public Property Minutas As String
        Public Property Comensales As String
        Public Property MinutasSinCosto As String
        Public Property Costo As String
        Public Property CostoPorBandeja As String
        Public Property Techo As String
        Public Property Desviacion As String
    End Class
End Class
