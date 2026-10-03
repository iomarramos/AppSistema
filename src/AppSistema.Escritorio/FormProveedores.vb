Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>Proveedores, empaques que ofrece cada uno y su historial de precios.</summary>
Public Class FormProveedores
    Inherits Form

    Private ReadOnly _servicio As ServicioProveedores
    Private ReadOnly _catalogo As ServicioCatalogo
    Private ReadOnly _proveedores As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _empaques As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _precios As DataGridView = Ui.NuevaGrilla()

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _servicio = New ServicioProveedores(cadena, sesion)
        _catalogo = New ServicioCatalogo(cadena, sesion)
        Text = "Proveedores y precios"

        Dim barraProv = Ui.BarraBotones(Ui.Boton("Nuevo proveedor", AddressOf NuevoProveedor))
        barraProv.Visible = sesion.Tiene(Permisos.ProveedoresEditar)
        Dim barraEmp = Ui.BarraBotones(Ui.Boton("Vincular empaque", AddressOf VincularEmpaque))
        barraEmp.Visible = sesion.Tiene(Permisos.ProveedoresEditar)
        Dim barraPre = Ui.BarraBotones(Ui.Boton("Registrar precio", AddressOf RegistrarPrecio))
        barraPre.Visible = sesion.Tiene(Permisos.PreciosEditar)

        Dim izquierda As New Panel With {.Dock = DockStyle.Fill}
        izquierda.Controls.Add(_proveedores) : izquierda.Controls.Add(barraProv)
        Dim derecha As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal}
        derecha.Panel1.Controls.Add(_empaques) : derecha.Panel1.Controls.Add(barraEmp)
        derecha.Panel1.Controls.Add(New Label With {.Text = "Empaques que ofrece", .Dock = DockStyle.Top, .Padding = New Padding(4)})
        derecha.Panel2.Controls.Add(_precios) : derecha.Panel2.Controls.Add(barraPre)
        derecha.Panel2.Controls.Add(New Label With {.Text = "Precios por empaque (vigencias sin superposicion)", .Dock = DockStyle.Top, .Padding = New Padding(4)})
        Dim division As New SplitContainer With {.Dock = DockStyle.Fill}
        division.Panel1.Controls.Add(izquierda) : division.Panel2.Controls.Add(derecha)
        Controls.Add(division)

        AddHandler _proveedores.SelectionChanged, Sub() CargarEmpaques()
        AddHandler _empaques.SelectionChanged, Sub() CargarPrecios()
        AddHandler Load, Sub() CargarProveedores()
    End Sub

    Private Sub CargarProveedores()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_proveedores, _servicio.ListarProveedores(),
                                         "Codigo|Codigo", "Nombre|Nombre", "IdentificacionFiscal|RUC", "Telefono|Telefono", "EsCajaChica|Caja chica"))
    End Sub

    Private Sub CargarEmpaques()
        Dim p = Ui.Seleccionado(Of ProveedorDto)(_proveedores)
        If p Is Nothing Then _empaques.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_empaques, _servicio.ListarEmpaquesDeProveedor(p.Id),
                                         "VarianteCodigo|Variante", "EmpaqueDescripcion|Empaque", "PlazoEntregaDias|Plazo (dias)"))
    End Sub

    Private Sub CargarPrecios()
        Dim pe = Ui.Seleccionado(Of ProveedorEmpaqueDto)(_empaques)
        If pe Is Nothing Then _precios.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_precios, _servicio.ListarPrecios(pe.Id),
                                         "FechaDesde|Desde", "FechaHasta|Hasta", "Moneda|Moneda", "PrecioEmpaqueU6|Precio por empaque", "IncluyeImpuesto|Incluye impuesto"))
    End Sub

    Private Sub NuevoProveedor()
        Using d As New DialogoCampos("Nuevo proveedor")
            d.Texto("codigo", "Codigo").Texto("nombre", "Razon social").Texto("ruc", "RUC").Texto("contacto", "Contacto") _
             .Texto("correo", "Correo").Texto("telefono", "Telefono").Marca("caja", "Proveedor de caja chica")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CrearProveedor(New ProveedorDto With {
                .Codigo = d.Valor("codigo"), .Nombre = d.Valor("nombre"), .IdentificacionFiscal = NadaSiVacio(d.Valor("ruc")),
                .Contacto = NadaSiVacio(d.Valor("contacto")), .Correo = NadaSiVacio(d.Valor("correo")), .Telefono = NadaSiVacio(d.Valor("telefono")),
                .EsCajaChica = d.Marcado("caja")}))
        End Using
        CargarProveedores()
    End Sub

    Private Shared Function NadaSiVacio(s As String) As String
        Return If(s = "", Nothing, s)
    End Function

    Private Sub VincularEmpaque()
        Dim p = Ui.Seleccionado(Of ProveedorDto)(_proveedores)
        If p Is Nothing Then Ui.Informar(Me, "Seleccione un proveedor.") : Return
        Ui.Ejecutar(Me,
            Sub()
                Dim empaques = _catalogo.ListarEmpaquesActivos()
                If empaques.Count = 0 Then Ui.Informar(Me, "No hay empaques en el catalogo.") : Return
                Using d As New DialogoCampos("Vincular empaque a " & p.Nombre)
                    d.Opciones("empaque", "Empaque", empaques).Texto("plazo", "Plazo de entrega (dias)", "0")
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    _servicio.VincularEmpaque(p.Id, d.Elegido(Of EmpaqueDto)("empaque").Id, Ui.LeerEntero(d.Valor("plazo"), "plazo"))
                End Using
            End Sub)
        CargarEmpaques()
    End Sub

    Private Sub RegistrarPrecio()
        Dim pe = Ui.Seleccionado(Of ProveedorEmpaqueDto)(_empaques)
        If pe Is Nothing Then Ui.Informar(Me, "Seleccione un empaque del proveedor.") : Return
        Using d As New DialogoCampos("Precio de " & pe.EmpaqueDescripcion)
            d.Fecha("desde", "Vigente desde", Date.Today).Fecha("hasta", "Vigente hasta (opcional)", Date.Today, opcional:=True) _
             .Texto("moneda", "Moneda", "PEN").Texto("precio", "Precio por empaque").Marca("impuesto", "El precio incluye impuesto")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.RegistrarPrecio(pe.Id, d.FechaElegida("desde").Value, d.FechaElegida("hasta"), d.Valor("moneda"),
                                                             Ui.LeerU6(d.Valor("precio"), "precio"), d.Marcado("impuesto")))
        End Using
        CargarPrecios()
    End Sub
End Class
