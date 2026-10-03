Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

''' <summary>Ficha de receta: versiones (borrador, aprobada, retirada), ingredientes, variantes permitidas y costo simulado.</summary>
Public Class FormRecetas
    Inherits Form

    Private ReadOnly _sesion As SesionUsuario
    Private ReadOnly _servicio As ServicioRecetas
    Private ReadOnly _catalogo As ServicioCatalogo
    Private ReadOnly _buscar As New TextBox With {.Width = 220}
    Private ReadOnly _recetas As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _versiones As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _ingredientes As DataGridView = Ui.NuevaGrilla()

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _sesion = sesion
        _servicio = New ServicioRecetas(cadena, sesion)
        _catalogo = New ServicioCatalogo(cadena, sesion)
        Text = "Recetas"

        Dim edita = sesion.Tiene(Permisos.RecetasEditar)
        Dim aprueba = sesion.Tiene(Permisos.RecetasAprobar)
        Dim barraRecetas = Ui.BarraBotones(New Label With {.Text = "Buscar:", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _buscar,
                                           Ui.Boton("Buscar", AddressOf CargarRecetas), Ui.BotonSi(edita, "Nueva receta", AddressOf NuevaReceta))
        Dim barraVersiones = Ui.BarraBotones(Ui.BotonSi(edita, "Nueva version", AddressOf NuevaVersion), Ui.BotonSi(edita, "Rendimiento e instrucciones", AddressOf EditarBorrador),
                                             Ui.BotonSi(aprueba, "Aprobar", AddressOf Aprobar), Ui.BotonSi(aprueba, "Retirar", AddressOf Retirar),
                                             Ui.Boton("Costo simulado...", AddressOf CostoSimulado))
        Dim barraIngredientes = Ui.BarraBotones(Ui.Boton("Agregar ingrediente", AddressOf AgregarIngrediente), Ui.Boton("Quitar", AddressOf QuitarIngrediente),
                                                Ui.Boton("Limitar a una variante", AddressOf PermitirVariante))
        barraIngredientes.Visible = edita

        Dim izquierda As New Panel With {.Dock = DockStyle.Fill}
        izquierda.Controls.Add(_recetas) : izquierda.Controls.Add(barraRecetas)
        Dim derecha As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal}
        derecha.Panel1.Controls.Add(_versiones) : derecha.Panel1.Controls.Add(barraVersiones)
        derecha.Panel1.Controls.Add(New Label With {.Text = "Versiones (la aprobada no se modifica; para cambiarla cree una nueva version)", .Dock = DockStyle.Top, .Padding = New Padding(4)})
        derecha.Panel2.Controls.Add(_ingredientes) : derecha.Panel2.Controls.Add(barraIngredientes)
        derecha.Panel2.Controls.Add(New Label With {.Text = "Ingredientes de la version (cantidad bruta para el rendimiento completo, en unidad base)", .Dock = DockStyle.Top, .Padding = New Padding(4)})
        Dim division As New SplitContainer With {.Dock = DockStyle.Fill}
        division.Panel1.Controls.Add(izquierda) : division.Panel2.Controls.Add(derecha)
        Controls.Add(division)

        AddHandler _buscar.KeyDown, Sub(s, e) If e.KeyCode = Keys.Enter Then CargarRecetas()
        AddHandler _recetas.SelectionChanged, Sub() CargarVersiones()
        AddHandler _versiones.SelectionChanged, Sub() CargarIngredientes()
        AddHandler Load, Sub() CargarRecetas()
    End Sub

    Private ReadOnly Property Receta As RecetaDto
        Get
            Return Ui.Seleccionado(Of RecetaDto)(_recetas)
        End Get
    End Property

    Private ReadOnly Property Version As RecetaVersionDto
        Get
            Return Ui.Seleccionado(Of RecetaVersionDto)(_versiones)
        End Get
    End Property

    Private Sub CargarRecetas()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_recetas, _servicio.BuscarRecetas(_buscar.Text),
                                         "Codigo|Codigo", "Nombre|Nombre", "Categoria|Categoria", "VersionAprobada|Version aprobada"))
    End Sub

    Private Sub CargarVersiones()
        Dim r = Receta
        If r Is Nothing Then _versiones.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_versiones, _servicio.ListarVersiones(r.Id),
                                         "Version|Version", "Estado|Estado", "RendimientoRacionesU6|Rendimiento (raciones)", "Instrucciones|Instrucciones"))
    End Sub

    Private Sub CargarIngredientes()
        Dim v = Version
        If v Is Nothing Then _ingredientes.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_ingredientes, _servicio.ListarIngredientes(v.Id),
                                         "ProductoCodigo|Codigo", "ProductoDescripcion|Producto", "CantidadBrutaU6|Cantidad bruta", "CantidadNetaU6|Cantidad neta",
                                         "Unidad|Unidad", "Tecnica|Tecnica", "VariantesPermitidas|Variantes permitidas"))
    End Sub

    Private Sub NuevaReceta()
        Using d As New DialogoCampos("Nueva receta")
            d.Texto("codigo", "Codigo").Texto("nombre", "Nombre").Texto("categoria", "Categoria (opcional)") _
             .Texto("rendimiento", "Rendimiento (raciones)", "10").Texto("instrucciones", "Instrucciones (opcional)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CrearReceta(d.Valor("codigo"), d.Valor("nombre"), d.Valor("categoria"),
                                                        Ui.LeerU6(d.Valor("rendimiento"), "rendimiento"), d.Valor("instrucciones")))
            _buscar.Text = d.Valor("codigo")
        End Using
        CargarRecetas()
    End Sub

    Private Sub NuevaVersion()
        Dim r = Receta
        If r Is Nothing Then Ui.Informar(Me, "Seleccione una receta.") : Return
        Ui.Ejecutar(Me, Sub() _servicio.NuevaVersion(r.Id))
        CargarVersiones()
    End Sub

    Private Function VersionBorrador() As RecetaVersionDto
        Dim v = Version
        If v Is Nothing Then Ui.Informar(Me, "Seleccione una version.") : Return Nothing
        If v.Estado <> "borrador" Then Ui.Informar(Me, "Solo se modifica una version en borrador. Use 'Nueva version'.") : Return Nothing
        Return v
    End Function

    Private Sub EditarBorrador()
        Dim v = VersionBorrador()
        If v Is Nothing Then Return
        Using d As New DialogoCampos($"Version {v.Version}")
            d.Texto("rendimiento", "Rendimiento (raciones)", Ui.Cantidad(v.RendimientoRacionesU6)).Texto("instrucciones", "Instrucciones", If(v.Instrucciones, ""))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.ActualizarBorrador(v.Id, Ui.LeerU6(d.Valor("rendimiento"), "rendimiento"), d.Valor("instrucciones")))
        End Using
        CargarVersiones()
    End Sub

    Private Sub AgregarIngrediente()
        Dim v = VersionBorrador()
        If v Is Nothing Then Return
        Using d As New DialogoCampos("Agregar ingrediente")
            d.Texto("buscar", "Producto (codigo o parte del nombre)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me,
                Sub()
                    Dim productos = _catalogo.BuscarProductos(d.Valor("buscar"))
                    If productos.Count = 0 Then Ui.Informar(Me, "No se encontro ningun producto.") : Return
                    Using d2 As New DialogoCampos("Ingrediente")
                        d2.Opciones("producto", "Producto", productos.Take(200).Select(Function(p) CObj(New Opcion(Of ProductoBaseDto)(p, $"{p.Codigo} - {p.Descripcion} ({p.UnidadCodigo})")))) _
                          .Texto("bruta", "Cantidad bruta (unidad base)").Texto("neta", "Cantidad neta (opcional)").Texto("tecnica", "Tecnica (opcional)").Texto("orden", "Orden", (_ingredientes.Rows.Count + 1).ToString())
                        If d2.ShowDialog(Me) <> DialogResult.OK Then Return
                        Dim neta As Long? = If(d2.Valor("neta") = "", CType(Nothing, Long?), Ui.LeerU6(d2.Valor("neta"), "cantidad neta"))
                        _servicio.AgregarIngrediente(v.Id, d2.Elegido(Of Opcion(Of ProductoBaseDto))("producto").Valor.Id,
                                                     Ui.LeerU6(d2.Valor("bruta"), "cantidad bruta"), neta, Ui.LeerEntero(d2.Valor("orden"), "orden"), d2.Valor("tecnica"))
                    End Using
                End Sub)
        End Using
        CargarIngredientes()
    End Sub

    Private Sub QuitarIngrediente()
        Dim i = Ui.Seleccionado(Of IngredienteDto)(_ingredientes)
        If i Is Nothing OrElse VersionBorrador() Is Nothing Then Return
        If Not Ui.Confirmar(Me, $"Quitar {i.ProductoDescripcion} de la receta?") Then Return
        Ui.Ejecutar(Me, Sub() _servicio.QuitarIngrediente(i.Id))
        CargarIngredientes()
    End Sub

    Private Sub PermitirVariante()
        Dim i = Ui.Seleccionado(Of IngredienteDto)(_ingredientes)
        If i Is Nothing Then Ui.Informar(Me, "Seleccione un ingrediente.") : Return
        If VersionBorrador() Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Dim variantes = _catalogo.ListarVariantes(i.ProductoBaseId)
                If variantes.Count = 0 Then Ui.Informar(Me, "El producto no tiene variantes.") : Return
                Using d As New DialogoCampos("Variante permitida para " & i.ProductoDescripcion)
                    d.Opciones("variante", "Variante", variantes.Select(Function(x) CObj(New Opcion(Of VarianteDto)(x, $"{x.Codigo} - {x.DescripcionComercial}"))))
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    _servicio.PermitirVariante(i.Id, d.Elegido(Of Opcion(Of VarianteDto))("variante").Valor.Id)
                End Using
            End Sub)
        CargarIngredientes()
    End Sub

    Private Sub Aprobar()
        Dim v = Version
        If v Is Nothing Then Return
        If Not Ui.Confirmar(Me, $"Aprobar la version {v.Version}? Quedara fija y la version aprobada anterior se retirara.") Then Return
        Ui.Ejecutar(Me, Sub() _servicio.Aprobar(v.Id))
        CargarRecetas()
    End Sub

    Private Sub Retirar()
        Dim v = Version
        If v Is Nothing Then Return
        If Not Ui.Confirmar(Me, $"Retirar la version {v.Version}? No podra planificarse en nuevas minutas.") Then Return
        Ui.Ejecutar(Me, Sub() _servicio.Retirar(v.Id))
        CargarVersiones()
    End Sub

    Private Sub CostoSimulado()
        Dim v = Version
        If v Is Nothing Then Ui.Informar(Me, "Seleccione una version.") : Return
        Using d As New DialogoCampos("Costo simulado")
            d.Fecha("fecha", "Precios vigentes al", Date.Today).Texto("moneda", "Moneda", "PEN")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me,
                Sub()
                    Dim c = _servicio.CostoSimulado(v.Id, d.FechaElegida("fecha").Value, d.Valor("moneda"))
                    Dim total = If(c.CostoRacionU6.HasValue, Ui.Dinero(c.CostoRacionU6.Value) & " por racion",
                                   $"PENDIENTE: {c.Pendientes} ingrediente(s) sin precio")
                    Ui.MostrarLista(Me, "Costo simulado (no se guarda)", $"Version {v.Version}, rendimiento {Ui.Cantidad(c.RendimientoRacionesU6)} raciones. Costo: {total}.",
                                    c.Ingredientes, "ProductoDescripcion|Producto", "CantidadBrutaU6|Cantidad", "Unidad|Unidad",
                                    "CostoUnitarioBaseU6|Costo unitario", "CostoLineaU6|Costo", "Fuente|Fuente del precio")
                End Sub)
        End Using
    End Sub
End Class
