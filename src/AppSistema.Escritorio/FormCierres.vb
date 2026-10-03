Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Cierres y control: tablero de pendientes del dia, cierre diario, ingreso mensual y objetivo de Food Cost por
''' servicio, reporte mensual y cierre de mes. Un periodo cerrado ya no cambia.
''' </summary>
Partial Public Class FormCierres

    Private ReadOnly _servicio As ServicioCierres
    Private ReadOnly _fecha As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 110}
    Private ReadOnly _mes As New DateTimePicker With {.Format = DateTimePickerFormat.Custom, .CustomFormat = "MM/yyyy", .ShowUpDown = True, .Width = 90}
    Private ReadOnly _pendientes As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _servicios As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _estadoDia As New Label With {.Dock = DockStyle.Bottom, .Height = 28, .Padding = New Padding(6)}
    Private ReadOnly _totales As New Label With {.Dock = DockStyle.Bottom, .Height = 28, .Padding = New Padding(6)}
    Private ReadOnly _envio As New Label With {.Dock = DockStyle.Bottom, .Height = 28, .Padding = New Padding(6)}

    Public Sub New()
        InitializeComponent()
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Controls.Clear()
        _servicio = New ServicioCierres(cadena, sesion)
        Text = "Cierres y Food Cost - " & sesion.Operacion.Nombre
        Dim cierra = sesion.Tiene(Permisos.CierreEjecutar)

        Dim barraDia = Ui.BarraBotones(New Label With {.Text = "Dia", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _fecha,
                                       Ui.Boton("Actualizar", AddressOf CargarDia), Ui.BotonSi(cierra, "Cerrar dia", AddressOf CerrarDia))
        Dim barraMes = Ui.BarraBotones(New Label With {.Text = "Mes", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _mes,
                                       Ui.Boton("Reporte", AddressOf CargarMes), Ui.BotonSi(cierra, "Generar venta (estructura)", AddressOf GenerarVenta),
                                       Ui.BotonSi(cierra, "Registrar ingreso...", AddressOf RegistrarIngreso),
                                       Ui.BotonSi(cierra, "Objetivo Food Cost...", AddressOf FijarObjetivo), Ui.BotonSi(cierra, "Cerrar mes", AddressOf CerrarMes))

        Dim division As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal, .SplitterDistance = 220}
        division.Panel1.Controls.Add(_pendientes)
        division.Panel1.Controls.Add(_estadoDia)
        division.Panel1.Controls.Add(_envio)
        division.Panel1.Controls.Add(barraDia)
        division.Panel2.Controls.Add(_servicios)
        division.Panel2.Controls.Add(_totales)
        division.Panel2.Controls.Add(barraMes)
        Controls.Add(division)
        Controls.Add(New Label With {.Dock = DockStyle.Top, .Height = 34, .Padding = New Padding(4),
            .Text = "Food Cost = costo de alimentos consumidos / venta del servicio. La venta sale de la estructura: costo previsto de las minutas / Food Cost objetivo (48 % por defecto). Un dia o mes cerrado ya no admite cambios."})
        AddHandler _fecha.ValueChanged, Sub() CargarDia()
        AddHandler _mes.ValueChanged, Sub() CargarMes()
        AddHandler Load, Sub()
                             CargarDia()
                             CargarMes()
                         End Sub
    End Sub

    Private ReadOnly Property Anio As Integer
        Get
            Return _mes.Value.Year
        End Get
    End Property

    Private ReadOnly Property Mes As Integer
        Get
            Return _mes.Value.Month
        End Get
    End Property

    Private Sub CargarDia()
        Ui.Ejecutar(Me,
            Sub()
                Dim pendientes = _servicio.Pendientes(_fecha.Value.Date).Select(Function(p) New With {
                    .Tipo = If(p.Bloqueante, "Bloquea", "Advertencia"), p.Codigo, p.Detalle}).ToList()
                Ui.Mostrar(_pendientes, pendientes, "Tipo|Tipo", "Codigo|Codigo", "Detalle|Detalle")
                Dim cerrado = _servicio.DiaCerrado(_fecha.Value.Date)
                _envio.Text = _servicio.EstadoEnvio().ToString()
                _estadoDia.Text = If(cerrado, "Dia CERRADO.", If(pendientes.Any(Function(p) p.Tipo = "Bloquea"),
                    "Dia abierto: resuelva los pendientes que bloquean antes de cerrar.", "Dia abierto: listo para cerrar."))
            End Sub)
    End Sub

    Private Sub CargarMes()
        Ui.Ejecutar(Me,
            Sub()
                Dim r = _servicio.ReporteMensual(Anio, Mes)
                Ui.Mostrar(_servicios, r.Servicios, "Servicio|Servicio", "Regimen|Regimen", "RacionesServidas|Raciones servidas",
                           "CostoAlimentosU6|Costo alimentos", "CostoPorRacionU6|Costo por racion", "IngresoU6|Ingreso neto",
                           "FoodCostU6|Food Cost %", "ObjetivoU6|Objetivo %", "DesviacionPuntosU6|Desviacion (puntos)",
                           "PresupuestoU6|Presupuesto", "DiferenciaPresupuestoU6|Diferencia vs presupuesto", "Observacion|Observacion")
                _totales.Text = $"Mes {Mes:00}/{Anio} ({r.Estado}): costo {Ui.Dinero(r.TotalCostoAlimentosU6)}, ingreso {Ui.Dinero(r.TotalIngresoU6)}, " &
                                $"Food Cost {If(r.FoodCostTotalU6.HasValue, Ui.Cantidad(r.FoodCostTotalU6.Value) & " %", "no calculable")}; " &
                                $"bajas {Ui.Dinero(r.BajasU6)}, ajuste de inventario {Ui.Dinero(r.AjusteInventarioU6)}"
            End Sub)
    End Sub

    Private Sub CerrarDia()
        Dim fecha = _fecha.Value.Date
        If Not Ui.Confirmar(Me, $"Cerrar el dia {fecha:dd/MM/yyyy}? Despues no se podran registrar movimientos con esa fecha.") Then Return
        Ui.Ejecutar(Me, Sub()
                            Dim r = _servicio.CerrarDia(fecha)
                            Ui.Informar(Me, If(r.Cerrado, "Dia cerrado.",
                                "No se cerro el dia:" & vbCrLf & String.Join(vbCrLf, r.Pendientes.Where(Function(p) p.Bloqueante).Select(Function(p) $"- {p.Detalle}"))))
                        End Sub)
        CargarDia()
    End Sub

    Private Sub GenerarVenta()
        Ui.Ejecutar(Me, Sub() Ui.MostrarLista(Me, "Venta desde la estructura", $"Venta de {Mes:00}/{Anio}", _servicio.GenerarVentaDesdeMinutas(Anio, Mes),
                                              "Servicio|Servicio", "ImporteU6|Venta", "Estado|Estado", "Detalle|Detalle"))
        CargarMes()
    End Sub

    Private Function ServicioElegido() As LineaReporteServicio
        Dim s = Ui.Seleccionado(Of LineaReporteServicio)(_servicios)
        If s Is Nothing Then Ui.Informar(Me, "Seleccione un servicio del reporte.")
        Return s
    End Function

    Private Sub RegistrarIngreso()
        Dim s = ServicioElegido()
        If s Is Nothing Then Return
        Using d As New DialogoCampos($"Ingreso de {s.Servicio} - {Mes:00}/{Anio}")
            d.Texto("importe", "Importe neto del mes (S/)", If(s.IngresoU6.HasValue, Ui.Cantidad(s.IngresoU6.Value), "")) _
             .Texto("ajustes", "Ajustes incluidos (S/)", "0").Texto("fuente", "Fuente (factura, liquidacion...)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.RegistrarIngreso(s.OperacionServicioId, Anio, Mes, Ui.LeerU6(d.Valor("importe"), "importe"),
                                                             If(d.Valor("ajustes") = "", 0L, Ui.LeerU6(d.Valor("ajustes"), "ajustes")), d.Valor("fuente")))
        End Using
        CargarMes()
    End Sub

    Private Sub FijarObjetivo()
        Dim s = ServicioElegido()
        If s Is Nothing Then Return
        Using d As New DialogoCampos("Objetivo de Food Cost de " & s.Servicio)
            d.Texto("objetivo", "Objetivo en % (vacio = sin objetivo)", If(s.ObjetivoU6.HasValue, Ui.Cantidad(s.ObjetivoU6.Value), ""))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            ' % con escala 1e6 → puntos básicos (1 % = 100 bp).
            Ui.Ejecutar(Me, Sub() _servicio.FijarObjetivo(s.OperacionServicioId,
                                                          If(d.Valor("objetivo") = "", CType(Nothing, Long?), Ui.LeerU6(d.Valor("objetivo"), "objetivo") \ 10000L)))
        End Using
        CargarMes()
    End Sub

    Private Sub CerrarMes()
        If Not Ui.Confirmar(Me, $"Cerrar el mes {Mes:00}/{Anio}? Movimientos, ingresos y gastos del mes quedaran fijos.") Then Return
        Ui.Ejecutar(Me, Sub()
                            Dim r = _servicio.CerrarMes(Anio, Mes)
                            Dim avisos = r.Pendientes.Select(Function(p) $"- {If(p.Bloqueante, "", "(aviso) ")}{p.Detalle}")
                            Ui.Informar(Me, If(r.Cerrado, "Mes cerrado." & If(avisos.Any(), vbCrLf & String.Join(vbCrLf, avisos), ""),
                                               "No se cerro el mes:" & vbCrLf & String.Join(vbCrLf, avisos)))
                        End Sub)
        CargarMes()
    End Sub
End Class
