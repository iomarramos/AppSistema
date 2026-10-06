Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>Operaciones (sedes) de la empresa y sus almacenes. Los usuarios se asignan a una operación en Usuarios y roles.</summary>
Partial Public Class FormOperaciones

    Private ReadOnly _servicio As ServicioAdministracion

    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridOperaciones)
        Ui.Configurar(gridAlmacenes)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridOperaciones)
        Ui.Configurar(gridAlmacenes)
        _servicio = New ServicioAdministracion(cadena, sesion)
        Text = "Operaciones y almacenes"
        barraOperaciones.Controls.Add(Ui.Boton("Nueva operacion...", AddressOf NuevaOperacion))
        barraOperaciones.Controls.Add(Ui.Boton("Zona o region...", AddressOf CambiarZona))
        barraAlmacenes.Controls.Add(Ui.Boton("Nuevo almacen...", AddressOf NuevoAlmacen))
        AddHandler gridOperaciones.SelectionChanged, Sub() CargarAlmacenes()
        AddHandler Load, Sub() Cargar()
    End Sub

    Private ReadOnly Property Operacion As OperacionDto
        Get
            Return Ui.Seleccionado(Of OperacionDto)(gridOperaciones)
        End Get
    End Property

    Private Sub Cargar()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(gridOperaciones, _servicio.ListarOperaciones(), "Codigo|Codigo", "Nombre|Operacion", "Zona|Zona o region", "Ubicacion|Ubicacion",
                                         "Almacenes|Almacenes", "Usuarios|Usuarios"))
    End Sub

    Private Sub CargarAlmacenes()
        Dim o = Operacion
        If o Is Nothing Then gridAlmacenes.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(gridAlmacenes, _servicio.ListarAlmacenesDeOperacion(o.Id), "Codigo|Codigo", "Nombre|Almacen"))
    End Sub

    Private Sub NuevaOperacion()
        Using d As New DialogoCampos("Nueva operacion")
            d.Texto("codigo", "Codigo").Texto("nombre", "Nombre").Opciones("zona", "Zona o region", OpcionesZona())
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CrearOperacion(d.Valor("codigo"), d.Valor("nombre"), d.Elegido(Of Opcion(Of String))("zona").Valor))
        End Using
        Cargar()
    End Sub

    ''' <summary>La zona agrupa sedes: un rol con alcance ZONA vale en todas las operaciones de la misma zona.</summary>
    Private Sub CambiarZona()
        Dim o = Operacion
        If o Is Nothing Then Ui.Informar(Me, "Seleccione una operacion.") : Return
        Using d As New DialogoCampos("Zona o region de " & o.Nombre)
            Dim ops = OpcionesZona().ToList()
            d.Opciones("zona", "Zona o region", ops, ops.FirstOrDefault(Function(x) DirectCast(x, Opcion(Of String)).Valor = If(o.Zona, "")))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.FijarZona(o.Id, d.Elegido(Of Opcion(Of String))("zona").Valor))
        End Using
        Cargar()
    End Sub

    ''' <summary>Costa, Sierra o Selva, más "sin zona".</summary>
    Private Shared Function OpcionesZona() As IEnumerable(Of Object)
        Return {CObj(New Opcion(Of String)("", "(sin zona)"))}.Concat(Zonas.Todas.Select(Function(z) CObj(New Opcion(Of String)(z, z))))
    End Function

    Private Sub NuevoAlmacen()
        Dim o = Operacion
        If o Is Nothing Then Ui.Informar(Me, "Seleccione una operacion.") : Return
        Using d As New DialogoCampos("Nuevo almacen de " & o.Nombre)
            d.Texto("codigo", "Codigo").Texto("nombre", "Nombre")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CrearAlmacen(o.Id, d.Valor("codigo"), d.Valor("nombre")))
        End Using
        Cargar()
    End Sub
End Class
