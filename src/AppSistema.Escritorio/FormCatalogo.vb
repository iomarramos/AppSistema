Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Catalogo
Imports AppSistema.Dominio.Seguridad

''' <summary>Productos base (izquierda) con sus variantes y empaques (derecha).</summary>
Partial Public Class FormCatalogo

    Private ReadOnly _servicio As ServicioCatalogo
    Private ReadOnly _sesion As SesionUsuario
    Private ReadOnly _buscar As New TextBox With {.Width = 260}
    Private ReadOnly _inactivos As New CheckBox With {.Text = "Incluir inactivos", .AutoSize = True}
    Private ReadOnly _productos As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _variantes As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _empaques As DataGridView = Ui.NuevaGrilla()

    Public Sub New()
        InitializeComponent()
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Controls.Clear()
        _servicio = New ServicioCatalogo(cadena, sesion)
        _sesion = sesion
        Text = "Catalogo: productos, variantes y empaques"
        Dim edita = sesion.Tiene(Permisos.CatalogoEditar)

        ' Izquierda: búsqueda y productos.
        Dim busqueda As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .Padding = New Padding(4)}
        busqueda.Controls.AddRange({New Label With {.Text = "Buscar:", .AutoSize = True, .Margin = New Padding(3, 7, 3, 3)}, _buscar,
                                    Ui.Boton("Buscar", AddressOf CargarProductos), _inactivos})
        Dim barraProductos = Ui.BarraBotones(Ui.Boton("Nuevo producto", AddressOf NuevoProducto), Ui.Boton("Editar producto", AddressOf EditarProducto),
                                             Ui.Boton("Nueva unidad", AddressOf NuevaUnidad), Ui.Boton("Nueva categoria", AddressOf NuevaCategoria),
                                             Ui.Boton("Nueva marca", AddressOf NuevaMarca), Ui.Boton("Sin costo de compra...", AddressOf CambiarSinCosto))
        barraProductos.Visible = edita
        Dim izquierda As New Panel With {.Dock = DockStyle.Fill}
        izquierda.Controls.Add(_productos) : izquierda.Controls.Add(barraProductos) : izquierda.Controls.Add(busqueda)

        ' Derecha: variantes y empaques.
        Dim barraVariantes = Ui.BarraBotones(Ui.Boton("Nueva variante", AddressOf NuevaVariante), Ui.Boton("Editar variante", AddressOf EditarVariante),
                                             Ui.Boton("Corregir contenido...", AddressOf CorregirContenido),
                                             Ui.Boton("Producto activo en la operacion", AddressOf ActivarEnOperacion),
                                             Ui.Boton("Quitar producto activo", AddressOf QuitarActivo))
        barraVariantes.Visible = edita
        Dim barraEmpaques = Ui.BarraBotones(Ui.Boton("Nuevo empaque", AddressOf NuevoEmpaque))
        barraEmpaques.Visible = edita
        Dim derecha As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal}
        derecha.Panel1.Controls.Add(_variantes) : derecha.Panel1.Controls.Add(barraVariantes)
        derecha.Panel1.Controls.Add(New Label With {.Text = "Variantes (marca y presentacion)", .Dock = DockStyle.Top, .Padding = New Padding(4)})
        derecha.Panel2.Controls.Add(_empaques) : derecha.Panel2.Controls.Add(barraEmpaques)
        derecha.Panel2.Controls.Add(New Label With {.Text = "Empaques de compra", .Dock = DockStyle.Top, .Padding = New Padding(4)})

        Dim division As New SplitContainer With {.Dock = DockStyle.Fill}
        division.Panel1.Controls.Add(izquierda)
        division.Panel2.Controls.Add(derecha)
        Controls.Add(division)

        AddHandler _productos.SelectionChanged, Sub() CargarVariantes()
        AddHandler _variantes.SelectionChanged, Sub() CargarEmpaques()
        AddHandler _buscar.KeyDown, Sub(s, e)
                                        If e.KeyCode = Keys.Enter Then CargarProductos() : e.SuppressKeyPress = True
                                    End Sub
        AddHandler _productos.CellDoubleClick, Sub() If edita Then EditarProducto()
        AddHandler Load, Sub() CargarProductos()
    End Sub

    Private ReadOnly Property Producto As ProductoBaseDto
        Get
            Return Ui.Seleccionado(Of ProductoBaseDto)(_productos)
        End Get
    End Property

    Private ReadOnly Property Variante As VarianteDto
        Get
            Return Ui.Seleccionado(Of VarianteDto)(_variantes)
        End Get
    End Property

    Private Sub CargarProductos()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_productos, _servicio.BuscarProductos(_buscar.Text, _inactivos.Checked),
                                         "Codigo|Codigo", "Descripcion|Descripcion", "Especificacion|Especificacion",
                                         "UnidadCodigo|Unidad", "CategoriaCodigo|Categoria", "Activo|Activo", "SinCostoCompra|Sin costo de compra"))
    End Sub

    Private Sub CargarVariantes()
        Dim p = Producto
        If p Is Nothing Then
            _variantes.DataSource = Nothing
            Return
        End If
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_variantes, _servicio.ListarVariantes(p.Id),
                                         "Codigo|Codigo", "MarcaNombre|Marca", "DescripcionComercial|Descripcion comercial",
                                         "TipoEnvase|Envase", "ContenidoBasePorEnvaseU6|Contenido por envase (" & p.UnidadCodigo & ")", "Activo|Activo",
                                         "ActivoEnOperacion|Activo en la operacion (su precio se costea)", "Familia|Familia"))
    End Sub

    Private Sub CargarEmpaques()
        Dim v = Variante
        If v Is Nothing Then
            _empaques.DataSource = Nothing
            Return
        End If
        Dim unidad = If(Producto?.UnidadCodigo, "")
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_empaques, _servicio.ListarEmpaques(v.Id),
                                         "Codigo|Codigo", "Descripcion|Descripcion", "EnvasesPorEmpaque|Envases por empaque",
                                         "ContenidoBaseU6|Contenido total (" & unidad & ")", "MinimoEmpaques|Minimo", "MultiploEmpaques|Multiplo"))
    End Sub

    Private Sub NuevaUnidad()
        Using d As New DialogoCampos("Nueva unidad de medida")
            d.Texto("codigo", "Codigo (ej. kg, L, und)").Texto("nombre", "Nombre") _
             .Opciones("dim", "Dimension", [Enum].GetValues(GetType(Dimension)).Cast(Of Object)()) _
             .Texto("factor", "Equivale a (unidades base)", "1")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CrearUnidad(d.Valor("codigo"), d.Valor("nombre"), CType(DirectCast(d.Elegido(Of Object)("dim"), Dimension), Dimension),
                                                         Ui.LeerU6(d.Valor("factor"), "equivalencia")))
        End Using
    End Sub

    Private Sub NuevaCategoria()
        Using d As New DialogoCampos("Nueva categoria")
            d.Texto("codigo", "Codigo").Texto("nombre", "Nombre")
            If d.ShowDialog(Me) = DialogResult.OK Then Ui.Ejecutar(Me, Sub() _servicio.CrearCategoria(d.Valor("codigo"), d.Valor("nombre")))
        End Using
    End Sub

    Private Sub NuevaMarca()
        Using d As New DialogoCampos("Nueva marca")
            d.Texto("nombre", "Nombre")
            If d.ShowDialog(Me) = DialogResult.OK Then Ui.Ejecutar(Me, Sub() _servicio.CrearMarca(d.Valor("nombre")))
        End Using
    End Sub

    Private Function OpcionesCategorias() As IEnumerable(Of Object)
        Return _servicio.ListarCategorias().Select(Function(c) CObj(New Opcion(Of Long)(c.Id, c.Nombre)))
    End Function

    Private Shared Function IdElegido(d As DialogoCampos, clave As String) As Long?
        Dim o = d.Elegido(Of Opcion(Of Long))(clave)
        Return If(o Is Nothing, CType(Nothing, Long?), o.Valor)
    End Function

    Private Sub NuevoProducto()
        Ui.Ejecutar(Me,
            Sub()
                Dim unidades = _servicio.ListarUnidades()
                If unidades.Count = 0 Then
                    Ui.Informar(Me, "Primero cree al menos una unidad de medida.")
                    Return
                End If
                Using d As New DialogoCampos("Nuevo producto base")
                    d.Texto("codigo", "Codigo").Texto("descripcion", "Descripcion").Texto("espec", "Especificacion") _
                     .Opciones("unidad", "Unidad base", unidades.Select(Function(u) CObj(New Opcion(Of Long)(u.Id, $"{u.Codigo} - {u.Nombre}")))) _
                     .Opciones("categoria", "Categoria", OpcionesCategorias(), permitirVacio:=True)
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    _servicio.CrearProducto(d.Valor("codigo"), d.Valor("descripcion"), d.Valor("espec"),
                                            d.Elegido(Of Opcion(Of Long))("unidad").Valor, IdElegido(d, "categoria"))
                End Using
                _buscar.Text = ""
                CargarProductos()
            End Sub)
    End Sub

    Private Sub EditarProducto()
        Dim p = Producto
        If p Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Using d As New DialogoCampos("Editar producto " & p.Codigo)
                    Dim categorias = OpcionesCategorias().ToList()
                    d.Texto("descripcion", "Descripcion", p.Descripcion).Texto("espec", "Especificacion", If(p.Especificacion, "")) _
                     .Opciones("categoria", "Categoria", categorias, categorias.FirstOrDefault(Function(c) DirectCast(c, Opcion(Of Long)).Valor = p.CategoriaId.GetValueOrDefault()), permitirVacio:=True) _
                     .Marca("activo", "Activo", p.Activo)
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    ' La versión leída evita pisar cambios de otro usuario (VERSION_CONFLICTIVA).
                    _servicio.ActualizarProducto(p.Id, p.Version, d.Valor("descripcion"), d.Valor("espec"), IdElegido(d, "categoria"), d.Marcado("activo"))
                End Using
            End Sub)
        CargarProductos()
    End Sub

    Private Function OpcionesMarcas() As List(Of Object)
        Return _servicio.ListarMarcas().Select(Function(m) CObj(New Opcion(Of Long)(m.Id, m.Nombre))).ToList()
    End Function

    Private Sub NuevaVariante()
        Dim p = Producto
        If p Is Nothing Then
            Ui.Informar(Me, "Seleccione primero un producto.")
            Return
        End If
        Ui.Ejecutar(Me,
            Sub()
                Using d As New DialogoCampos("Nueva variante de " & p.Descripcion)
                    d.Texto("codigo", "Codigo de articulo").Opciones("marca", "Marca", OpcionesMarcas(), permitirVacio:=True) _
                     .Texto("descripcion", "Descripcion comercial").Texto("envase", "Tipo de envase (bidon, saco...)") _
                     .Texto("contenido", $"Contenido por envase ({p.UnidadCodigo})")
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    _servicio.CrearVariante(p.Id, IdElegido(d, "marca"), d.Valor("codigo"), d.Valor("descripcion"), d.Valor("envase"),
                                            Ui.LeerU6(d.Valor("contenido"), "contenido por envase"))
                End Using
                CargarVariantes()
            End Sub)
    End Sub

    Private Sub EditarVariante()
        Dim v = Variante
        If v Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Using d As New DialogoCampos("Editar variante " & v.Codigo)
                    Dim marcas = OpcionesMarcas()
                    d.Texto("descripcion", "Descripcion comercial", v.DescripcionComercial) _
                     .Opciones("marca", "Marca", marcas, marcas.FirstOrDefault(Function(m) DirectCast(m, Opcion(Of Long)).Valor = v.MarcaId.GetValueOrDefault()), permitirVacio:=True) _
                     .Marca("activo", "Activa", v.Activo)
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    _servicio.ActualizarVariante(v.Id, v.Version, d.Valor("descripcion"), IdElegido(d, "marca"), d.Marcado("activo"))
                End Using
            End Sub)
        CargarVariantes()
    End Sub

    Private Sub NuevoEmpaque()
        Dim v = Variante
        If v Is Nothing Then
            Ui.Informar(Me, "Seleccione primero una variante.")
            Return
        End If
        Using d As New DialogoCampos("Nuevo empaque de " & v.Codigo)
            d.Texto("codigo", "Codigo").Texto("descripcion", "Descripcion (ej. Caja 4 x 4 L)").Texto("envases", "Envases por empaque", "1") _
             .Texto("minimo", "Minimo de empaques por pedido", "1").Texto("multiplo", "Multiplo de pedido", "1")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CrearEmpaque(v.Id, d.Valor("codigo"), d.Valor("descripcion"), Ui.LeerEntero(d.Valor("envases"), "envases"),
                                                          Ui.LeerEntero(d.Valor("minimo"), "minimo"), Ui.LeerEntero(d.Valor("multiplo"), "multiplo")))
        End Using
        CargarEmpaques()
    End Sub
    ''' <summary>Marca o desmarca el producto como insumo sin costo de compra (agua de red de las recetas).</summary>
    Private Sub CambiarSinCosto()
        Dim p = Producto
        If p Is Nothing Then Return
        Dim nuevo = Not p.SinCostoCompra
        If Not Ui.Confirmar(Me, If(nuevo, $"Marcar '{p.Descripcion}' como insumo SIN costo de compra? Se costeara en S/ 0 en las recetas.",
                                         $"Quitar la marca 'sin costo de compra' de '{p.Descripcion}'? Volvera a necesitar precio.")) Then Return
        Ui.Ejecutar(Me, Sub() _servicio.MarcarSinCosto({p.Descripcion}, nuevo))
        CargarProductos()
    End Sub

    ''' <summary>D02: la presentacion elegida pasa a ser el producto activo del ingrediente en esta operacion.</summary>
    Private Sub ActivarEnOperacion()
        Dim v = Ui.Seleccionado(Of VarianteDto)(_variantes)
        If v Is Nothing Then Ui.Informar(Me, "Seleccione una variante.") : Return
        If Not Ui.Confirmar(Me, $"Usar '{v.DescripcionComercial}' como producto activo de '{Producto?.Descripcion}' en esta operacion? " &
                                "Las minutas que se aprueben desde ahora y los pedidos usaran su precio.") Then Return
        Ui.Ejecutar(Me, Sub() _servicio.ActivarEnOperacion(v.Id))
        CargarVariantes()
    End Sub

    Private Sub QuitarActivo()
        Dim p = Producto
        If p Is Nothing Then Return
        If Not Ui.Confirmar(Me, $"Quitar el producto activo de '{p.Descripcion}' en esta operacion? Se usara la presentacion con el ultimo ingreso al almacen.") Then Return
        Ui.Ejecutar(Me, Sub() _servicio.QuitarActivoEnOperacion(p.Id))
        CargarVariantes()
    End Sub

    ''' <summary>Corrige el contenido por envase de una presentacion que todavia no se uso (si ya se uso, la base lo impide).</summary>
    Private Sub CorregirContenido()
        Dim v = Ui.Seleccionado(Of VarianteDto)(_variantes)
        If v Is Nothing Then Ui.Informar(Me, "Seleccione una variante.") : Return
        Using d As New DialogoCampos("Contenido por envase de " & v.Codigo)
            d.Texto("contenido", $"Contenido por envase ({Producto?.UnidadCodigo})", Ui.Cantidad(v.ContenidoBasePorEnvaseU6))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CorregirContenidoVariante(v.Id, v.Version, Ui.LeerU6(d.Valor("contenido"), "contenido")))
        End Using
        CargarVariantes()
    End Sub
End Class
