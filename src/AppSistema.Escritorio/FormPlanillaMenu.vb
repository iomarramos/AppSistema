Imports System.Data
Imports System.Globalization
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Planilla del menú en el formato del SGP: estructuras en filas y días en columnas. Cada celda es el plato del día
''' (receta, raciones y % sobre los comensales). Doble clic (o "Cambiar receta o raciones") cambia la receta o las
''' raciones de la celda, o crea el plato si está vacía; con la minuta del día en borrador. Al final, comensales y costo
''' de cada día.
''' </summary>
Partial Public Class FormPlanillaMenu

    Private _minutas As ServicioMinutas
    Private _recetas As ServicioRecetas
    Private _sesion As SesionUsuario
    Private ReadOnly _operaciones As New List(Of OperacionServicioDto)()
    Private _estructuras As New List(Of EstructuraDto)()
    Private _minutaPorDia As New Dictionary(Of Date, MinutaDto)()
    Private _platos As New Dictionary(Of String, PlatoDto)()

    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridPlanilla)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridPlanilla)
        _minutas = New ServicioMinutas(cadena, sesion)
        _recetas = New ServicioRecetas(cadena, sesion)
        _sesion = sesion
        Text = "Planilla del menu - " & sesion.Operacion.Nombre
        dtDesde.Value = Date.Today.AddDays(-Date.Today.Day + 1)
        dtHasta.Value = dtDesde.Value.AddMonths(1).AddDays(-1)
        Dim edita = sesion.Tiene(Permisos.MinutasEditar)
        barraAcciones.Controls.Add(Ui.Boton("Ver", AddressOf Cargar))
        barraAcciones.Controls.Add(Ui.BotonSi(edita, "Cambiar receta o raciones...", AddressOf EditarSeleccion))
        barraAcciones.Controls.Add(Ui.BotonSi(edita, "Comensales del dia...", AddressOf EditarComensales))
        AddHandler cmbServicio.SelectedIndexChanged, Sub() Cargar()
        AddHandler dtDesde.ValueChanged, Sub() Cargar()
        AddHandler dtHasta.ValueChanged, Sub() Cargar()
        AddHandler gridPlanilla.CellDoubleClick, Sub(s, e)
                                                     If edita AndAlso e.RowIndex >= 0 AndAlso e.ColumnIndex >= 1 Then EditarCelda(e.RowIndex, e.ColumnIndex)
                                                 End Sub
        AddHandler Load, Sub() CargarServicios()
    End Sub

    Private Sub CargarServicios()
        Ui.Ejecutar(Me,
            Sub()
                _operaciones.Clear()
                _operaciones.AddRange(_minutas.ListarServiciosDeOperacion())
                cmbServicio.Items.Clear()
                For Each o In _operaciones
                    cmbServicio.Items.Add(New Opcion(Of OperacionServicioDto)(o, $"{o.ServicioNombre} - {o.RegimenNombre}"))
                Next
                If cmbServicio.Items.Count > 0 Then cmbServicio.SelectedIndex = 0
            End Sub)
    End Sub

    Private Function ServicioElegido() As OperacionServicioDto
        Dim sel = TryCast(cmbServicio.SelectedItem, Opcion(Of OperacionServicioDto))
        Return If(sel Is Nothing, Nothing, sel.Valor)
    End Function

    ''' <summary>Lee las minutas del periodo y sus platos; luego arma la planilla.</summary>
    Private Sub Cargar()
        Dim os = ServicioElegido()
        If os Is Nothing Then Return
        Dim desde = dtDesde.Value.Date
        Dim hasta = dtHasta.Value.Date
        Ui.Ejecutar(Me,
            Sub()
                _estructuras = _minutas.ListarEstructuras(os.ServicioId)
                Dim minutas = _minutas.ListarMinutas(desde, hasta).Where(Function(m) m.OperacionServicioId = os.Id).ToList()
                _minutaPorDia = New Dictionary(Of Date, MinutaDto)()
                _platos = New Dictionary(Of String, PlatoDto)()
                For Each m In minutas
                    _minutaPorDia(m.Fecha.Date) = m
                    For Each p In _minutas.ListarPlatos(m.Id)
                        _platos(p.EstructuraId & "|" & m.Fecha.ToString("yyyyMMdd", CultureInfo.InvariantCulture)) = p
                    Next
                Next
                Mostrar(desde, hasta)
            End Sub)
    End Sub

    Private Shared Function NombreColumna(dia As Date) As String
        Return "D" & dia.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
    End Function

    Private Shared Function DiaDeColumna(nombre As String) As Date
        Return Date.ParseExact(nombre.Substring(1), "yyyyMMdd", CultureInfo.InvariantCulture)
    End Function

    Private Sub Mostrar(desde As Date, hasta As Date)
        Dim tabla As New DataTable()
        tabla.Columns.Add("Estructura", GetType(String))
        Dim dias As New List(Of Date)()
        For i = 0 To (hasta - desde).Days
            dias.Add(desde.AddDays(i))
        Next
        For Each d In dias
            Dim c = tabla.Columns.Add(NombreColumna(d), GetType(String))
            c.Caption = d.ToString("dd ddd", CultureInfo.CurrentCulture)
        Next
        For Each e In _estructuras
            Dim fila = tabla.NewRow()
            fila("Estructura") = e.Nombre
            For Each d In dias
                Dim p As PlatoDto = Nothing
                If _platos.TryGetValue(e.Id & "|" & d.ToString("yyyyMMdd", CultureInfo.InvariantCulture), p) Then
                    Dim comensales = If(_minutaPorDia.ContainsKey(d), _minutaPorDia(d).Comensales, 0L)
                    Dim pct = If(comensales > 0, p.Raciones * 100.0 / comensales, 0.0)
                    fila(NombreColumna(d)) = $"{p.RecetaCodigo}  {p.Raciones} ({pct:0}%)"
                End If
            Next
            tabla.Rows.Add(fila)
        Next
        Dim comensalesFila = tabla.NewRow()
        comensalesFila("Estructura") = "Comensales"
        Dim costoFila = tabla.NewRow()
        costoFila("Estructura") = "Costo minuta del dia (S/)"
        For Each d In dias
            If _minutaPorDia.ContainsKey(d) Then
                Dim m = _minutaPorDia(d)
                comensalesFila(NombreColumna(d)) = m.Comensales.ToString(CultureInfo.InvariantCulture)
                costoFila(NombreColumna(d)) = If(m.CostoPrevistoU6.HasValue, Ui.Dinero(m.CostoPrevistoU6.Value), "sin costo")
            End If
        Next
        tabla.Rows.Add(comensalesFila)
        tabla.Rows.Add(costoFila)

        gridPlanilla.DataSource = tabla
        gridPlanilla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        gridPlanilla.Columns("Estructura").Frozen = True
        gridPlanilla.Columns("Estructura").Width = 230
        For Each col As DataGridViewColumn In gridPlanilla.Columns
            If col.Name <> "Estructura" Then
                col.HeaderText = tabla.Columns(col.Name).Caption
                col.Width = 150
            End If
        Next
        lblEstado.Text = $"{dias.Count} dias. Cada celda: receta, raciones y % sobre comensales. Doble clic para cambiarla; solo en minutas en borrador."
    End Sub

    ''' <summary>Cambia o crea el plato de la celda (estructura × día). Pide la receta aprobada y las raciones.</summary>
    Private Sub EditarCelda(fila As Integer, columna As Integer)
        If fila >= _estructuras.Count Then Return
        Dim os = ServicioElegido()
        Dim est = _estructuras(fila)
        Dim dia = DiaDeColumna(gridPlanilla.Columns(columna).Name)
        Dim p As PlatoDto = Nothing
        _platos.TryGetValue(est.Id & "|" & dia.ToString("yyyyMMdd", CultureInfo.InvariantCulture), p)

        Dim minuta As MinutaDto = Nothing
        If Not _minutaPorDia.TryGetValue(dia, minuta) Then
            Using d As New DialogoCampos($"Minuta del {dia:dd/MM/yyyy}")
                d.Texto("comensales", "Comensales del dia", "0")
                If d.ShowDialog(Me) <> DialogResult.OK Then Return
                Dim comensales = Ui.LeerEntero(d.Valor("comensales"), "comensales")
                If Not Ui.Ejecutar(Me, Sub() _minutas.CrearMinuta(os.Id, dia, comensales)) Then Return
            End Using
            Cargar()
            If Not _minutaPorDia.TryGetValue(dia, minuta) Then Return
        End If
        If minuta.Estado <> "borrador" Then
            Ui.Informar(Me, "La minuta del dia ya esta aprobada: su contenido no se modifica.")
            Return
        End If

        Dim recetas = _recetas.BuscarRecetas("").Where(Function(r) r.VersionAprobadaId.HasValue).ToList()
        If recetas.Count = 0 Then
            Ui.Informar(Me, "No hay recetas aprobadas.")
            Return
        End If
        Dim opciones = recetas.Select(Function(r) CObj(New Opcion(Of RecetaDto)(r, $"{r.Codigo} - {r.Nombre}"))).ToList()
        Dim actual As Object = Nothing
        If p IsNot Nothing Then
            actual = opciones.FirstOrDefault(Function(o As Object) DirectCast(o, Opcion(Of RecetaDto)).Valor.VersionAprobadaId = p.RecetaVersionId)
        End If
        Using d As New DialogoCampos($"{est.Nombre} - {dia:dd/MM/yyyy}")
            d.Opciones("receta", "Receta (version aprobada)", opciones, actual)
            d.Texto("raciones", "Raciones", If(p Is Nothing, minuta.Comensales, p.Raciones).ToString(CultureInfo.InvariantCulture))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim receta = d.Elegido(Of Opcion(Of RecetaDto))("receta").Valor
            Dim raciones = Ui.LeerEntero(d.Valor("raciones"), "raciones")
            Ui.Ejecutar(Me,
                Sub()
                    If p Is Nothing Then
                        _minutas.AgregarPlato(minuta.Id, est.Id, receta.VersionAprobadaId.Value, raciones)
                    Else
                        If receta.VersionAprobadaId.Value <> p.RecetaVersionId Then _minutas.SustituirReceta(p.Id, receta.VersionAprobadaId.Value)
                        If raciones <> p.Raciones Then _minutas.FijarRaciones(p.Id, raciones)
                    End If
                End Sub)
        End Using
        Cargar()
    End Sub

    Private Sub EditarSeleccion()
        If gridPlanilla.CurrentCell Is Nothing Then
            Ui.Informar(Me, "Seleccione una celda de la planilla.")
            Return
        End If
        EditarCelda(gridPlanilla.CurrentCell.RowIndex, gridPlanilla.CurrentCell.ColumnIndex)
    End Sub

    Private Sub EditarComensales()
        If gridPlanilla.CurrentCell Is Nothing Then
            Ui.Informar(Me, "Seleccione una celda del dia que quiere cambiar.")
            Return
        End If
        Dim dia = DiaDeColumna(gridPlanilla.Columns(gridPlanilla.CurrentCell.ColumnIndex).Name)
        Dim minuta As MinutaDto = Nothing
        If Not _minutaPorDia.TryGetValue(dia, minuta) Then
            Ui.Informar(Me, "Ese dia no tiene minuta: cree el plato de una celda del dia para crearla.")
            Return
        End If
        Using d As New DialogoCampos($"Comensales del {dia:dd/MM/yyyy}")
            d.Texto("comensales", "Total de comensales", minuta.Comensales.ToString(CultureInfo.InvariantCulture))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim comensales = Ui.LeerEntero(d.Valor("comensales"), "comensales")
            If Not Ui.Ejecutar(Me, Sub() _minutas.ActualizarComensales(minuta.Id, comensales)) Then Return
        End Using
        Cargar()
    End Sub

End Class
