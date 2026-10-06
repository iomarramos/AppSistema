Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>Servicios (desayuno, almuerzo…), su estructura (sopa, fondo, bebida…), regímenes y servicios de la operación.</summary>
Partial Public Class FormServicios

    Private ReadOnly _servicio As ServicioMinutas

    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridServicios)
        Ui.Configurar(gridEstructuras)
        Ui.Configurar(gridOperacion)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridServicios)
        Ui.Configurar(gridEstructuras)
        Ui.Configurar(gridOperacion)
        _servicio = New ServicioMinutas(cadena, sesion)
        Text = "Servicios y estructuras"
        barraServicios.Controls.Add(Ui.Boton("Nuevo servicio", AddressOf NuevoServicio))
        barraServicios.Controls.Add(Ui.Boton("Nuevo regimen", AddressOf NuevoRegimen))
        barraEstructuras.Controls.Add(Ui.Boton("Nueva estructura", AddressOf NuevaEstructura))
        barraEstructuras.Controls.Add(Ui.Boton("Factor de consumo...", AddressOf CambiarFactor))
        barraOperacion.Controls.Add(Ui.Boton("Asignar servicio a la operacion", AddressOf Asignar))
        barraOperacion.Controls.Add(Ui.Boton("Food Cost objetivo...", AddressOf FijarObjetivo))
        lblOperacion.Text = "Servicios que presta " & sesion.Operacion.Nombre
        AddHandler gridServicios.SelectionChanged, Sub() CargarEstructuras()
        AddHandler Load, Sub() Cargar()
    End Sub

    Private Sub Cargar()
        Ui.Ejecutar(Me,
            Sub()
                Ui.Mostrar(gridServicios, _servicio.ListarServicios(), "Codigo|Codigo", "Nombre|Servicio")
                Ui.Mostrar(gridOperacion, _servicio.ListarServiciosDeOperacion(), "ServicioNombre|Servicio", "RegimenNombre|Regimen",
                           "CostoObjetivoRacionU6|Costo objetivo por racion", "FoodCostObjetivoTexto|Food Cost objetivo")
            End Sub)
    End Sub

    Private Sub CargarEstructuras()
        Dim s = Ui.Seleccionado(Of ServicioDto)(gridServicios)
        If s Is Nothing Then gridEstructuras.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(gridEstructuras, _servicio.ListarEstructuras(s.Id), "Orden|Orden", "Codigo|Codigo", "Nombre|Nombre", "FactorConsumoTexto|Factor de consumo"))
    End Sub

    Private Sub NuevoServicio()
        Using d As New DialogoCampos("Nuevo servicio")
            d.Texto("codigo", "Codigo").Texto("nombre", "Nombre")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CrearServicio(d.Valor("codigo"), d.Valor("nombre")))
        End Using
        Cargar()
    End Sub

    Private Sub NuevoRegimen()
        Using d As New DialogoCampos("Nuevo regimen")
            d.Texto("codigo", "Codigo").Texto("nombre", "Nombre")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CrearRegimen(d.Valor("codigo"), d.Valor("nombre")))
        End Using
    End Sub

    Private Sub NuevaEstructura()
        Dim s = Ui.Seleccionado(Of ServicioDto)(gridServicios)
        If s Is Nothing Then Ui.Informar(Me, "Seleccione un servicio.") : Return
        Using d As New DialogoCampos("Estructura de " & s.Nombre)
            d.Texto("codigo", "Codigo").Texto("nombre", "Nombre (bebida, jugo, pan, fondo, complemento...)").Texto("orden", "Orden", (gridEstructuras.Rows.Count + 1).ToString()) _
             .Texto("factor", "Factor de consumo % (100 plato caliente; 30-70 complementos)", "100")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CrearEstructura(s.Id, d.Valor("codigo"), d.Valor("nombre"), Ui.LeerEntero(d.Valor("orden"), "orden"),
                                                             PorcentajeABp(d.Valor("factor"), "factor")))
        End Using
        CargarEstructuras()
    End Sub

    ''' <summary>"70" o "70,5" (%) → puntos básicos.</summary>
    Friend Shared Function PorcentajeABp(texto As String, campo As String) As Long
        Return Ui.LeerU6(texto, campo) \ 10000L
    End Function

    Private Sub CambiarFactor()
        Dim e = Ui.Seleccionado(Of EstructuraDto)(gridEstructuras)
        If e Is Nothing Then Ui.Informar(Me, "Seleccione un componente de la estructura.") : Return
        Using d As New DialogoCampos("Factor de consumo de " & e.Nombre)
            d.Texto("factor", "Factor de consumo %", (e.FactorConsumoBp / 100D).ToString("0.##"))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.FijarFactorConsumo(e.Id, PorcentajeABp(d.Valor("factor"), "factor")))
        End Using
        CargarEstructuras()
    End Sub

    Private Sub FijarObjetivo()
        Dim os = Ui.Seleccionado(Of OperacionServicioDto)(gridOperacion)
        If os Is Nothing Then Ui.Informar(Me, "Seleccione un servicio de la operacion.") : Return
        Using d As New DialogoCampos($"Food Cost objetivo de {os.ServicioNombre} - {os.RegimenNombre}")
            d.Texto("objetivo", "Food Cost objetivo % (vacio = 48 %)", If(os.FoodCostObjetivoBp.HasValue, (os.FoodCostObjetivoBp.Value / 100D).ToString("0.##"), ""))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.FijarFoodCostObjetivo(os.Id, If(d.Valor("objetivo") = "", CType(Nothing, Long?), PorcentajeABp(d.Valor("objetivo"), "objetivo"))))
        End Using
        Cargar()
    End Sub

    Private Sub Asignar()
        Ui.Ejecutar(Me,
            Sub()
                Dim servicios = _servicio.ListarServicios()
                Dim regimenes = _servicio.ListarRegimenes()
                If servicios.Count = 0 OrElse regimenes.Count = 0 Then Ui.Informar(Me, "Cree primero un servicio y un regimen.") : Return
                Using d As New DialogoCampos("Asignar servicio")
                    d.Opciones("servicio", "Servicio", servicios.Select(Function(x) CObj(New Opcion(Of ServicioDto)(x, x.Nombre)))) _
                     .Opciones("regimen", "Regimen", regimenes.Select(Function(x) CObj(New Opcion(Of ServicioDto)(x, x.Nombre)))) _
                     .Texto("objetivo", "Costo objetivo por racion (opcional)")
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    Dim objetivo As Long? = If(d.Valor("objetivo") = "", CType(Nothing, Long?), Ui.LeerU6(d.Valor("objetivo"), "costo objetivo"))
                    _servicio.AsignarServicio(d.Elegido(Of Opcion(Of ServicioDto))("servicio").Valor.Id, d.Elegido(Of Opcion(Of ServicioDto))("regimen").Valor.Id, objetivo)
                End Using
            End Sub)
        Cargar()
    End Sub
End Class
