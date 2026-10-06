Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>Proveedores, empaques que ofrece cada uno y su historial de precios.</summary>
Partial Public Class FormProveedores

    Private ReadOnly _servicio As ServicioProveedores
    Private ReadOnly _catalogo As ServicioCatalogo

    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridProveedores)
        Ui.Configurar(gridEmpaques)
        Ui.Configurar(gridPrecios)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridProveedores)
        Ui.Configurar(gridEmpaques)
        Ui.Configurar(gridPrecios)
        _servicio = New ServicioProveedores(cadena, sesion)
        _catalogo = New ServicioCatalogo(cadena, sesion)
        Text = "Proveedores y precios"

        barraProveedores.Controls.Add(Ui.Boton("Nuevo proveedor", AddressOf NuevoProveedor))
        barraProveedores.Visible = sesion.Tiene(Permisos.ProveedoresEditar)
        barraEmpaques.Controls.Add(Ui.Boton("Vincular empaque", AddressOf VincularEmpaque))
        barraEmpaques.Visible = sesion.Tiene(Permisos.ProveedoresEditar)
        barraPrecios.Controls.Add(Ui.Boton("Registrar precio", AddressOf RegistrarPrecio))
        barraPrecios.Visible = sesion.Tiene(Permisos.PreciosEditar)

        AddHandler gridProveedores.SelectionChanged, Sub() CargarEmpaques()
        AddHandler gridEmpaques.SelectionChanged, Sub() CargarPrecios()
        AddHandler Load, Sub() CargarProveedores()
    End Sub

    Private Sub CargarProveedores()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(gridProveedores, _servicio.ListarProveedores(),
                                         "Codigo|Codigo", "Nombre|Nombre", "IdentificacionFiscal|RUC", "Telefono|Telefono", "EsCajaChica|Caja chica"))
    End Sub

    Private Sub CargarEmpaques()
        Dim p = Ui.Seleccionado(Of ProveedorDto)(gridProveedores)
        If p Is Nothing Then gridEmpaques.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(gridEmpaques, _servicio.ListarEmpaquesDeProveedor(p.Id),
                                         "VarianteCodigo|Variante", "EmpaqueDescripcion|Empaque", "PlazoEntregaDias|Plazo (dias)"))
    End Sub

    Private Sub CargarPrecios()
        Dim pe = Ui.Seleccionado(Of ProveedorEmpaqueDto)(gridEmpaques)
        If pe Is Nothing Then gridPrecios.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(gridPrecios, _servicio.ListarPrecios(pe.Id),
                                         "FechaDesde|Desde", "FechaHasta|Hasta", "Moneda|Moneda", "PrecioEmpaqueU6|Precio por empaque (sin IGV)"))
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
        Dim p = Ui.Seleccionado(Of ProveedorDto)(gridProveedores)
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
        Dim pe = Ui.Seleccionado(Of ProveedorEmpaqueDto)(gridEmpaques)
        If pe Is Nothing Then Ui.Informar(Me, "Seleccione un empaque del proveedor.") : Return
        Using d As New DialogoCampos("Precio de " & pe.EmpaqueDescripcion)
            d.Fecha("desde", "Vigente desde", Date.Today).Fecha("hasta", "Vigente hasta (opcional)", Date.Today, opcional:=True) _
             .Texto("moneda", "Moneda", "PEN").Texto("precio", "Precio por empaque, sin IGV")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.RegistrarPrecio(pe.Id, d.FechaElegida("desde").Value, d.FechaElegida("hasta"), d.Valor("moneda"),
                                                             Ui.LeerU6(d.Valor("precio"), "precio"), False))
        End Using
        CargarPrecios()
    End Sub
End Class
