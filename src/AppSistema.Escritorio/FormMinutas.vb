Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>Calendario de minutas de la operación: platos por estructura, fijos, aprobación con costo y necesidades consolidadas.</summary>
Partial Public Class FormMinutas

    Private ReadOnly _servicio As ServicioMinutas
    Private ReadOnly _recetas As ServicioRecetas
    Private ReadOnly _catalogo As ServicioCatalogo
    Private ReadOnly _comparativo As ServicioComparativo
    Private ReadOnly _reportes As ServicioReportes

    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridMinutas)
        Ui.Configurar(gridPlatos)
        Ui.Configurar(gridFijos)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridMinutas)
        Ui.Configurar(gridPlatos)
        Ui.Configurar(gridFijos)
        _servicio = New ServicioMinutas(cadena, sesion)
        _recetas = New ServicioRecetas(cadena, sesion)
        _catalogo = New ServicioCatalogo(cadena, sesion)
        _comparativo = New ServicioComparativo(cadena, sesion)
        _reportes = New ServicioReportes(cadena, sesion)
        Text = "Minutas - " & sesion.Operacion.Nombre
        dtDesde.Value = Date.Today.AddDays(-Date.Today.Day + 1)
        dtHasta.Value = dtDesde.Value.AddMonths(1).AddDays(-1)

        Dim edita = sesion.Tiene(Permisos.MinutasEditar)
        Dim aprueba = sesion.Tiene(Permisos.MinutasAprobar)
        barraMinutas.Controls.Add(Ui.Boton("Ver", AddressOf CargarMinutas))
        barraMinutas.Controls.Add(Ui.BotonSi(edita, "Nueva minuta", AddressOf NuevaMinuta))
        barraMinutas.Controls.Add(Ui.BotonSi(edita, "Cambiar comensales...", AddressOf CambiarComensales))
        barraMinutas.Controls.Add(Ui.BotonSi(aprueba, "Aprobar", AddressOf Aprobar))
        barraMinutas.Controls.Add(Ui.BotonSi(sesion.Tiene(Permisos.FactoresEditar), "Factores de la operacion...", AddressOf FactoresOperacion))
        barraMinutas.Controls.Add(Ui.Boton("Necesidades del periodo...", AddressOf Necesidades))
        barraMinutas.Controls.Add(Ui.Boton("Imprimir minuta...", AddressOf ImprimirMinuta))
        barraPlatos.Controls.Add(Ui.Boton("Agregar plato", AddressOf AgregarPlato))
        barraPlatos.Controls.Add(Ui.Boton("Quitar plato", AddressOf QuitarPlato))
        barraPlatos.Controls.Add(Ui.Boton("Agregar fijo", AddressOf AgregarFijo))
        barraPlatos.Controls.Add(Ui.Boton("Quitar fijo", AddressOf QuitarFijo))
        barraPlatos.Visible = edita

        AddHandler gridMinutas.SelectionChanged, Sub() CargarDetalle()
        AddHandler Load, Sub() CargarMinutas()
    End Sub

    Private ReadOnly Property Minuta As MinutaDto
        Get
            Return Ui.Seleccionado(Of MinutaDto)(gridMinutas)
        End Get
    End Property

    Private Sub CargarMinutas()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(gridMinutas, _servicio.ListarMinutas(dtDesde.Value, dtHasta.Value),
                                         "Fecha|Fecha", "ServicioNombre|Servicio", "RegimenNombre|Regimen", "Comensales|Comensales",
                                         "Estado|Estado", "MonedaCosteo|Moneda", "CostoPrevistoU6|Costo previsto", "CostoComensalU6|Costo por comensal",
                                         "VentaPrevistaU6|Venta (costo / FC objetivo)", "PrecioVentaComensalU6|Precio de venta por comensal"))
    End Sub

    Private Sub CargarDetalle()
        Dim m = Minuta
        If m Is Nothing Then gridPlatos.DataSource = Nothing : gridFijos.DataSource = Nothing : Return
        Ui.Ejecutar(Me,
            Sub()
                Ui.Mostrar(gridPlatos, _servicio.ListarPlatos(m.Id), "EstructuraNombre|Estructura", "RecetaCodigo|Receta", "RecetaNombre|Nombre",
                           "Version|Version", "Raciones|Raciones", "CostoPrevistoRacionU6|Costo por racion", "IngredientesSinCosto|Sin precio")
                Ui.Mostrar(gridFijos, _servicio.ListarFijos(m.Id), "ProductoDescripcion|Producto", "CantidadBaseU6|Cantidad", "Unidad|Unidad",
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

    ''' <summary>Cambia el total de comensales y recalcula las raciones de los platos agregados por factor.</summary>
    Private Sub CambiarComensales()
        Dim m = MinutaBorrador()
        If m Is Nothing Then Return
        Using d As New DialogoCampos("Comensales de la minuta")
            d.Texto("comensales", "Total de comensales", m.Comensales.ToString())
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.ActualizarComensales(m.Id, Ui.LeerEntero(d.Valor("comensales"), "comensales")))
        End Using
        CargarMinutas()
        CargarDetalle()
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
        Dim p = Ui.Seleccionado(Of PlatoDto)(gridPlatos)
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
        Dim f = Ui.Seleccionado(Of FijoMinutaDto)(gridFijos)
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

    Private Sub ImprimirMinuta()
        Dim m = Minuta
        If m Is Nothing Then Ui.Informar(Me, "Seleccione una minuta.") : Return
        SalidaReporte.Emitir(Me, Function() _reportes.MinutaDelDia(m.Id))
    End Sub

    Private Sub Necesidades()
        Dim ids = gridMinutas.Rows.Cast(Of DataGridViewRow)().Select(Function(r) DirectCast(r.DataBoundItem, MinutaDto).Id).ToList()
        If ids.Count = 0 Then Ui.Informar(Me, "No hay minutas en el periodo.") : Return
        Ui.Ejecutar(Me, Sub() Ui.MostrarLista(Me, "Necesidades consolidadas",
                                               $"{ids.Count} minuta(s) del {dtDesde.Value.ToShortDateString()} al {dtHasta.Value.ToShortDateString()} (borradores incluidos). " &
                                               "Cantidad bruta en unidad base; cada producto aparece una sola vez.",
                                               _servicio.Necesidades(ids), "ProductoCodigo|Codigo", "ProductoDescripcion|Producto", "CantidadU6|Cantidad",
                                               "Unidad|Unidad", "Origenes|Platos y fijos"))
    End Sub
    ''' <summary>
    ''' Factores de la operación: muestra teórico, vigente y el real de los últimos 30 días, y permite fijar el de la
    ''' operación (vacío = volver al teórico).
    ''' </summary>
    Private Sub FactoresOperacion()
        Dim servicios As List(Of OperacionServicioDto) = Nothing
        If Not Ui.Ejecutar(Me, Sub() servicios = _servicio.ListarServiciosDeOperacion()) OrElse servicios.Count = 0 Then Return
        Dim os As OperacionServicioDto = servicios(0)
        If servicios.Count > 1 Then
            Using elegir As New DialogoCampos("Servicio")
                elegir.Opciones("s", "Servicio", servicios.Select(Function(x) CObj(New Opcion(Of OperacionServicioDto)(x, $"{x.ServicioNombre} - {x.RegimenNombre}"))))
                If elegir.ShowDialog(Me) <> DialogResult.OK Then Return
                os = elegir.Elegido(Of Opcion(Of OperacionServicioDto))("s").Valor
            End Using
        End If
        Dim reales As List(Of FactorRealDto) = Nothing
        If Not Ui.Ejecutar(Me, Sub() reales = _comparativo.FactoresReales(os.Id, Date.Today.AddDays(-30), Date.Today)) Then Return
        Using d As New DialogoCampos($"Factores de {os.ServicioNombre} - {os.RegimenNombre} (vacio = teorico)")
            For Each f In reales
                d.Texto("f" & f.EstructuraId, $"{f.Estructura}: teorico {f.FactorTeoricoBp / 100D:0.##} %, real 30 dias " &
                        If(f.FactorRealBp.HasValue, $"{f.FactorRealBp.Value / 100D:0.##} %", "sin datos"),
                        If(f.FactorVigenteBp <> f.FactorTeoricoBp, (f.FactorVigenteBp / 100D).ToString("0.##"), ""))
            Next
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub()
                                For Each f In reales
                                    Dim v = d.Valor("f" & f.EstructuraId)
                                    _servicio.FijarFactorOperacion(os.Id, f.EstructuraId, If(v = "", CType(Nothing, Long?), FormServicios.PorcentajeABp(v, "factor")))
                                Next
                            End Sub)
        End Using
    End Sub
End Class
