Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>Calendario de minutas de la operación: platos por estructura, fijos, aprobación con costo y necesidades consolidadas.</summary>
Public Class FormMinutas
    Inherits Form

    Private ReadOnly _servicio As ServicioMinutas
    Private ReadOnly _recetas As ServicioRecetas
    Private ReadOnly _catalogo As ServicioCatalogo
    Private ReadOnly _desde As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 110}
    Private ReadOnly _hasta As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 110}
    Private ReadOnly _minutas As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _platos As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _fijos As DataGridView = Ui.NuevaGrilla()

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _servicio = New ServicioMinutas(cadena, sesion)
        _recetas = New ServicioRecetas(cadena, sesion)
        _catalogo = New ServicioCatalogo(cadena, sesion)
        Text = "Minutas - " & sesion.Operacion.Nombre
        _desde.Value = Date.Today.AddDays(-Date.Today.Day + 1)
        _hasta.Value = _desde.Value.AddMonths(1).AddDays(-1)

        Dim edita = sesion.Tiene(Permisos.MinutasEditar)
        Dim barraMinutas = Ui.BarraBotones(New Label With {.Text = "Desde", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _desde,
                                           New Label With {.Text = "hasta", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _hasta,
                                           Ui.Boton("Ver", AddressOf CargarMinutas), Ui.Boton("Nueva minuta", AddressOf NuevaMinuta),
                                           Ui.Boton("Aprobar", AddressOf Aprobar), Ui.Boton("Necesidades del periodo...", AddressOf Necesidades))
        barraMinutas.Controls(5).Visible = edita
        barraMinutas.Controls(6).Visible = sesion.Tiene(Permisos.MinutasAprobar)
        Dim barraPlatos = Ui.BarraBotones(Ui.Boton("Agregar plato", AddressOf AgregarPlato), Ui.Boton("Quitar plato", AddressOf QuitarPlato),
                                          Ui.Boton("Agregar fijo", AddressOf AgregarFijo), Ui.Boton("Quitar fijo", AddressOf QuitarFijo))
        barraPlatos.Visible = edita

        Dim arriba As New Panel With {.Dock = DockStyle.Fill}
        arriba.Controls.Add(_minutas) : arriba.Controls.Add(barraMinutas)
        Dim abajo As New SplitContainer With {.Dock = DockStyle.Fill}
        abajo.Panel1.Controls.Add(_platos)
        abajo.Panel1.Controls.Add(New Label With {.Text = "Platos (costo previsto fijado al aprobar; 'pendiente' = falta precio)", .Dock = DockStyle.Top, .Padding = New Padding(4)})
        abajo.Panel2.Controls.Add(_fijos)
        abajo.Panel2.Controls.Add(New Label With {.Text = "Fijos (productos fuera de recetas, cantidad total)", .Dock = DockStyle.Top, .Padding = New Padding(4)})
        Dim inferior As New Panel With {.Dock = DockStyle.Fill}
        inferior.Controls.Add(abajo) : inferior.Controls.Add(barraPlatos)
        Dim division As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal}
        division.Panel1.Controls.Add(arriba) : division.Panel2.Controls.Add(inferior)
        Controls.Add(division)

        AddHandler _minutas.SelectionChanged, Sub() CargarDetalle()
        AddHandler Load, Sub() CargarMinutas()
    End Sub

    Private ReadOnly Property Minuta As MinutaDto
        Get
            Return Ui.Seleccionado(Of MinutaDto)(_minutas)
        End Get
    End Property

    Private Sub CargarMinutas()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_minutas, _servicio.ListarMinutas(_desde.Value, _hasta.Value),
                                         "Fecha|Fecha", "ServicioNombre|Servicio", "RegimenNombre|Regimen", "Comensales|Comensales",
                                         "Estado|Estado", "MonedaCosteo|Moneda", "CostoPrevistoU6|Costo previsto", "CostoComensalU6|Costo por comensal",
                                         "VentaPrevistaU6|Venta (costo / FC objetivo)", "PrecioVentaComensalU6|Precio de venta por comensal"))
    End Sub

    Private Sub CargarDetalle()
        Dim m = Minuta
        If m Is Nothing Then _platos.DataSource = Nothing : _fijos.DataSource = Nothing : Return
        Ui.Ejecutar(Me,
            Sub()
                Ui.Mostrar(_platos, _servicio.ListarPlatos(m.Id), "EstructuraNombre|Estructura", "RecetaCodigo|Receta", "RecetaNombre|Nombre",
                           "Version|Version", "Raciones|Raciones", "CostoPrevistoRacionU6|Costo por racion", "IngredientesSinCosto|Sin precio")
                Ui.Mostrar(_fijos, _servicio.ListarFijos(m.Id), "ProductoDescripcion|Producto", "CantidadBaseU6|Cantidad", "Unidad|Unidad",
                           "CostoPrevistoUnitarioU6|Costo unitario")
            End Sub)
    End Sub

    Private Function MinutaBorrador() As MinutaDto
        Dim m = Minuta
        If m Is Nothing Then Ui.Informar(Me, "Seleccione una minuta.") : Return Nothing
        If m.Estado <> "borrador" Then Ui.Informar(Me, "La minuta ya esta aprobada y no se modifica.") : Return Nothing
        Return m
    End Function

    Private Sub NuevaMinuta()
        Ui.Ejecutar(Me,
            Sub()
                Dim servicios = _servicio.ListarServiciosDeOperacion()
                If servicios.Count = 0 Then Ui.Informar(Me, "La operacion no tiene servicios asignados (Menus > Servicios y estructuras).") : Return
                Using d As New DialogoCampos("Nueva minuta")
                    d.Opciones("servicio", "Servicio", servicios.Select(Function(s) CObj(New Opcion(Of OperacionServicioDto)(s, $"{s.ServicioNombre} - {s.RegimenNombre}")))) _
                     .Fecha("fecha", "Fecha", Date.Today.AddDays(1)).Texto("comensales", "Comensales", "0")
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    _servicio.CrearMinuta(d.Elegido(Of Opcion(Of OperacionServicioDto))("servicio").Valor.Id, d.FechaElegida("fecha").Value,
                                          Ui.LeerEntero(d.Valor("comensales"), "comensales"))
                End Using
            End Sub)
        CargarMinutas()
    End Sub

    Private Sub AgregarPlato()
        Dim m = MinutaBorrador()
        If m Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Dim servicioId = _servicio.ListarServiciosDeOperacion().Single(Function(s) s.Id = m.OperacionServicioId).ServicioId
                Dim estructuras = _servicio.ListarEstructuras(servicioId)
                Dim recetas = _recetas.BuscarRecetas("").Where(Function(r) r.VersionAprobadaId.HasValue).ToList()
                If estructuras.Count = 0 OrElse recetas.Count = 0 Then Ui.Informar(Me, "Faltan estructuras del servicio o recetas aprobadas.") : Return
                Using d As New DialogoCampos("Agregar plato")
                    d.Opciones("estructura", "Estructura", estructuras.Select(Function(x) CObj(New Opcion(Of EstructuraDto)(x, $"{x.Nombre} (factor {x.FactorConsumoTexto})")))) _
                     .Opciones("receta", "Receta (version aprobada)", recetas.Select(Function(x) CObj(New Opcion(Of RecetaDto)(x, $"{x.Codigo} - {x.Nombre} (v{x.VersionAprobada})")))) _
                     .Texto("reparto", "Reparto de la alternativa % (jugo A 50 + jugo B 50)", "100") _
                     .Texto("raciones", "Raciones (vacio = comensales x factor x reparto)", "")
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    Dim estructura = d.Elegido(Of Opcion(Of EstructuraDto))("estructura").Valor.Id
                    Dim version = d.Elegido(Of Opcion(Of RecetaDto))("receta").Valor.VersionAprobadaId.Value
                    If d.Valor("raciones") = "" Then
                        _servicio.AgregarPlatoPorFactor(m.Id, estructura, version, FormServicios.PorcentajeABp(d.Valor("reparto"), "reparto"))
                    Else
                        _servicio.AgregarPlato(m.Id, estructura, version, Ui.LeerEntero(d.Valor("raciones"), "raciones"))
                    End If
                End Using
            End Sub)
        CargarDetalle()
    End Sub

    Private Sub QuitarPlato()
        Dim p = Ui.Seleccionado(Of PlatoDto)(_platos)
        If p Is Nothing OrElse MinutaBorrador() Is Nothing Then Return
        Ui.Ejecutar(Me, Sub() _servicio.QuitarPlato(p.Id))
        CargarDetalle()
    End Sub

    Private Sub AgregarFijo()
        Dim m = MinutaBorrador()
        If m Is Nothing Then Return
        Using d As New DialogoCampos("Agregar fijo")
            d.Texto("buscar", "Producto (codigo o parte del nombre)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me,
                Sub()
                    Dim productos = _catalogo.BuscarProductos(d.Valor("buscar"))
                    If productos.Count = 0 Then Ui.Informar(Me, "No se encontro ningun producto.") : Return
                    Using d2 As New DialogoCampos("Fijo")
                        d2.Opciones("producto", "Producto", productos.Take(200).Select(Function(p) CObj(New Opcion(Of ProductoBaseDto)(p, $"{p.Codigo} - {p.Descripcion} ({p.UnidadCodigo})")))) _
                          .Texto("cantidad", "Cantidad total (unidad base)")
                        If d2.ShowDialog(Me) <> DialogResult.OK Then Return
                        _servicio.AgregarFijo(m.Id, d2.Elegido(Of Opcion(Of ProductoBaseDto))("producto").Valor.Id, Ui.LeerU6(d2.Valor("cantidad"), "cantidad"))
                    End Using
                End Sub)
        End Using
        CargarDetalle()
    End Sub

    Private Sub QuitarFijo()
        Dim f = Ui.Seleccionado(Of FijoMinutaDto)(_fijos)
        If f Is Nothing OrElse MinutaBorrador() Is Nothing Then Return
        Ui.Ejecutar(Me, Sub() _servicio.QuitarFijo(f.Id))
        CargarDetalle()
    End Sub

    Private Sub Aprobar()
        Dim m = MinutaBorrador()
        If m Is Nothing Then Return
        Using d As New DialogoCampos("Aprobar minuta del " & m.Fecha.ToShortDateString())
            d.Texto("moneda", "Moneda de los precios", "PEN")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            If Not Ui.Confirmar(Me, "Al aprobar se fija el costo previsto con los precios vigentes a la fecha de la minuta y ya no se puede modificar. Continuar?") Then Return
            Ui.Ejecutar(Me, Sub() _servicio.Aprobar(m.Id, d.Valor("moneda")))
        End Using
        CargarMinutas()
    End Sub

    Private Sub Necesidades()
        Dim ids = _minutas.Rows.Cast(Of DataGridViewRow)().Select(Function(r) DirectCast(r.DataBoundItem, MinutaDto).Id).ToList()
        If ids.Count = 0 Then Ui.Informar(Me, "No hay minutas en el periodo.") : Return
        Ui.Ejecutar(Me, Sub() Ui.MostrarLista(Me, "Necesidades consolidadas",
                                               $"{ids.Count} minuta(s) del {_desde.Value.ToShortDateString()} al {_hasta.Value.ToShortDateString()} (borradores incluidos). " &
                                               "Cantidad bruta en unidad base; cada producto aparece una sola vez.",
                                               _servicio.Necesidades(ids), "ProductoCodigo|Codigo", "ProductoDescripcion|Producto", "CantidadU6|Cantidad",
                                               "Unidad|Unidad", "Origenes|Platos y fijos"))
    End Sub
End Class
