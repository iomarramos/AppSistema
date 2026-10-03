Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>Servicios (desayuno, almuerzo…), su estructura (sopa, fondo, bebida…), regímenes y servicios de la operación.</summary>
Public Class FormServicios
    Inherits Form

    Private ReadOnly _servicio As ServicioMinutas
    Private ReadOnly _comparativo As ServicioComparativo
    Private ReadOnly _servicios As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _estructuras As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _operacion As DataGridView = Ui.NuevaGrilla()

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _servicio = New ServicioMinutas(cadena, sesion)
        _comparativo = New ServicioComparativo(cadena, sesion)
        Text = "Servicios y estructuras"

        Dim izquierda As New Panel With {.Dock = DockStyle.Fill}
        izquierda.Controls.Add(_servicios)
        izquierda.Controls.Add(Ui.BarraBotones(Ui.Boton("Nuevo servicio", AddressOf NuevoServicio), Ui.Boton("Nuevo regimen", AddressOf NuevoRegimen)))
        Dim derecha As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal}
        derecha.Panel1.Controls.Add(_estructuras)
        derecha.Panel1.Controls.Add(Ui.BarraBotones(Ui.Boton("Nueva estructura", AddressOf NuevaEstructura), Ui.Boton("Factor de consumo...", AddressOf CambiarFactor)))
        derecha.Panel1.Controls.Add(New Label With {.Text = "Estructura del servicio (orden en que se sirve). Factor de consumo = parte de los comensales que toma el componente.", .Dock = DockStyle.Top, .Padding = New Padding(4)})
        derecha.Panel2.Controls.Add(_operacion)
        derecha.Panel2.Controls.Add(Ui.BarraBotones(Ui.Boton("Asignar servicio a la operacion", AddressOf Asignar), Ui.Boton("Food Cost objetivo...", AddressOf FijarObjetivo),
                                                    Ui.Boton("Factores de la operacion...", AddressOf FactoresOperacion)))
        derecha.Panel2.Controls.Add(New Label With {.Text = "Servicios que presta " & sesion.Operacion.Nombre, .Dock = DockStyle.Top, .Padding = New Padding(4)})
        Dim division As New SplitContainer With {.Dock = DockStyle.Fill}
        division.Panel1.Controls.Add(izquierda) : division.Panel2.Controls.Add(derecha)
        Controls.Add(division)

        AddHandler _servicios.SelectionChanged, Sub() CargarEstructuras()
        AddHandler Load, Sub() Cargar()
    End Sub

    Private Sub Cargar()
        Ui.Ejecutar(Me,
            Sub()
                Ui.Mostrar(_servicios, _servicio.ListarServicios(), "Codigo|Codigo", "Nombre|Servicio")
                Ui.Mostrar(_operacion, _servicio.ListarServiciosDeOperacion(), "ServicioNombre|Servicio", "RegimenNombre|Regimen",
                           "CostoObjetivoRacionU6|Costo objetivo por racion", "FoodCostObjetivoTexto|Food Cost objetivo")
            End Sub)
    End Sub

    Private Sub CargarEstructuras()
        Dim s = Ui.Seleccionado(Of ServicioDto)(_servicios)
        If s Is Nothing Then _estructuras.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_estructuras, _servicio.ListarEstructuras(s.Id), "Orden|Orden", "Codigo|Codigo", "Nombre|Nombre", "FactorConsumoTexto|Factor de consumo"))
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
        Dim s = Ui.Seleccionado(Of ServicioDto)(_servicios)
        If s Is Nothing Then Ui.Informar(Me, "Seleccione un servicio.") : Return
        Using d As New DialogoCampos("Estructura de " & s.Nombre)
            d.Texto("codigo", "Codigo").Texto("nombre", "Nombre (bebida, jugo, pan, fondo, complemento...)").Texto("orden", "Orden", (_estructuras.Rows.Count + 1).ToString()) _
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
        Dim e = Ui.Seleccionado(Of EstructuraDto)(_estructuras)
        If e Is Nothing Then Ui.Informar(Me, "Seleccione un componente de la estructura.") : Return
        Using d As New DialogoCampos("Factor de consumo de " & e.Nombre)
            d.Texto("factor", "Factor de consumo %", (e.FactorConsumoBp / 100D).ToString("0.##"))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.FijarFactorConsumo(e.Id, PorcentajeABp(d.Valor("factor"), "factor")))
        End Using
        CargarEstructuras()
    End Sub

    Private Sub FijarObjetivo()
        Dim os = Ui.Seleccionado(Of OperacionServicioDto)(_operacion)
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
    ''' <summary>
    ''' Factores de la operación: muestra teórico, vigente y el real de los últimos 30 días, y permite fijar el de la
    ''' operación (vacío = volver al teórico).
    ''' </summary>
    Private Sub FactoresOperacion()
        Dim os = Ui.Seleccionado(Of OperacionServicioDto)(_operacion)
        If os Is Nothing Then Ui.Informar(Me, "Seleccione un servicio de la operacion.") : Return
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
                                    _servicio.FijarFactorOperacion(os.Id, f.EstructuraId, If(v = "", CType(Nothing, Long?), PorcentajeABp(v, "factor")))
                                Next
                            End Sub)
        End Using
    End Sub
End Class
