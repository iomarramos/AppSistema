Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Gastos del mes (personal, operación, administración, otros; reales o presupuestados) y resultado mensual por servicio:
''' ingreso − alimentos − gastos = margen. Exporta el resultado en CSV para contabilidad.
''' </summary>
Partial Public Class FormResultados

    Private ReadOnly _servicio As ServicioResultados
    Private _btnRegistrar As Button
    Private _btnEliminar As Button
    Private ReadOnly _sesion As SesionUsuario
    Private _reportes As ServicioReportes

    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridResultado)
        Ui.Configurar(gridGastos)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridResultado)
        Ui.Configurar(gridGastos)
        _servicio = New ServicioResultados(cadena, sesion)
        _reportes = New ServicioReportes(cadena, sesion)
        _sesion = sesion
        Text = "Gastos y resultado mensual - " & sesion.Operacion.Nombre
        Dim edita = sesion.Tiene(Permisos.GastosEditar)
        _btnRegistrar = Ui.BotonSi(edita, "Registrar gasto...", AddressOf RegistrarGasto)
        _btnEliminar = Ui.BotonSi(edita, "Eliminar gasto", AddressOf EliminarGasto)
        barraMes.Controls.Add(Ui.Boton("Actualizar", AddressOf Cargar))
        barraMes.Controls.Add(_btnRegistrar)
        barraMes.Controls.Add(_btnEliminar)
        barraMes.Controls.Add(Ui.Boton("Exportar CSV...", AddressOf Exportar))
        barraMes.Controls.Add(Ui.BotonSi(sesion.Tiene(Permisos.ReportesVer), "Comparado con el presupuesto...", AddressOf ImprimirComparado))
        barraMes.Controls.Add(Ui.Boton("Presupuesto, mes anterior y acumulado...", AddressOf VerComparacion))
        AddHandler dtMes.ValueChanged, Sub() Cargar()
        AddHandler Load, Sub() Cargar()
    End Sub

    Private Sub Cargar()
        Dim a = dtMes.Value.Year, m = dtMes.Value.Month
        Ui.Ejecutar(Me,
            Sub()
                Dim r = _servicio.ResultadoMensual(a, m)
                Ui.Mostrar(gridResultado, r.Lineas.Concat({r.Total}).ToList(), "Servicio|Servicio", "IngresoU6|Ingreso", "FuenteIngreso|Fuente del ingreso",
                           "CostoAlimentosU6|Alimentos", "GastosPersonalU6|Personal", "GastosOperacionU6|Operacion", "OtrosGastosU6|Otros",
                           "TotalGastosU6|Total gastos", "MargenU6|Margen", "MargenPorcentajeU6|Margen %", "PresupuestoGastosU6|Gastos presupuestados")
                lblEstado.Text = $"Mes {m:00}/{a}: {r.Estado}" & If(r.Estado = "cerrado", " (no admite cambios)", "")
                ' Mes cerrado = solo lectura: los botones de edición se deshabilitan (la base también lo impide).
                If _btnRegistrar IsNot Nothing Then _btnRegistrar.Enabled = r.Estado <> "cerrado"
                If _btnEliminar IsNot Nothing Then _btnEliminar.Enabled = r.Estado <> "cerrado"
                Ui.Mostrar(gridGastos, _servicio.ListarGastos(a, m), "Concepto|Concepto", "Categoria|Categoria", "Servicio|Servicio", "CuentaContable|Cuenta",
                           "ImporteU6|Importe", "Proyectado|Presupuestado")
            End Sub)
    End Sub

    Private Sub RegistrarGasto()
        Dim servicios As List(Of OperacionServicioDto) = Nothing
        If Not Ui.Ejecutar(Me, Sub() servicios = _servicio.ServiciosDeOperacion()) Then Return
        Using d As New DialogoCampos($"Gasto de {dtMes.Value:MM/yyyy}")
            d.Texto("concepto", "Concepto").Opciones("categoria", "Categoria", ServicioResultados.Categorias.Select(Function(c) CObj(c))) _
             .Opciones("servicio", "Servicio (vacio = comun a la operacion)", servicios.Select(Function(s) CObj(New Opcion(Of OperacionServicioDto)(s, $"{s.ServicioNombre} - {s.RegimenNombre}"))), permitirVacio:=True) _
             .Texto("importe", "Importe (S/)").Texto("cuenta", "Cuenta contable (opcional)").Marca("proyectado", "Es presupuesto (no gasto real)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim elegido = d.Elegido(Of Opcion(Of OperacionServicioDto))("servicio")
            Ui.Ejecutar(Me, Sub() _servicio.RegistrarGasto(dtMes.Value.Year, dtMes.Value.Month, If(elegido Is Nothing, CType(Nothing, Long?), elegido.Valor.Id), d.Valor("concepto"),
                                                            CStr(d.Elegido(Of String)("categoria")), Ui.LeerU6(d.Valor("importe"), "importe"), d.Marcado("proyectado"), d.Valor("cuenta")))
        End Using
        Cargar()
    End Sub

    Private Sub EliminarGasto()
        Dim g = Ui.Seleccionado(Of GastoDto)(gridGastos)
        If g Is Nothing OrElse Not Ui.Confirmar(Me, $"Eliminar el gasto '{g.Concepto}'?") Then Return
        Ui.Ejecutar(Me, Sub() _servicio.EliminarGasto(g.Id))
        Cargar()
    End Sub

    ''' <summary>Real del mes frente a su presupuesto por rubro, al mes anterior y al acumulado del año.</summary>
    Private Sub VerComparacion()
        Dim filas As List(Of FilaComparacionDto) = Nothing
        If Not Ui.Ejecutar(Me, Sub() filas = _servicio.Comparacion(dtMes.Value.Year, dtMes.Value.Month)) Then Return
        Ui.MostrarLista(Me, "Presupuesto, mes anterior y acumulado",
                        $"Mes {dtMes.Value:MM/yyyy}, operacion {_sesion.Operacion}. Presupuesto = gastos proyectados; '-' = no aplica. Importes en S/.",
                        filas, "Concepto|Concepto", "PresupuestoTexto|Presupuesto", "ValorRealU6|Real del mes",
                        "ValorMesAnteriorU6|Mes anterior", "ValorAcumuladoU6|Acumulado del año")
    End Sub

    ''' <summary>Comparado por servicio (presupuesto, costo real, ingreso y alerta de Food Cost) en impresora, Excel, PDF o CSV.</summary>
    Private Sub ImprimirComparado()
        SalidaReporte.Emitir(Me, Function() _reportes.ComparativoResultado(dtMes.Value.Year, dtMes.Value.Month))
    End Sub

    Private Sub Exportar()
        Using a As New SaveFileDialog With {.Filter = "CSV (*.csv)|*.csv", .FileName = $"resultado_{_sesion.Operacion.Codigo}_{dtMes.Value:yyyy-MM}.csv"}
            If a.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub()
                                File.WriteAllText(a.FileName, _servicio.ExportarCsv(dtMes.Value.Year, dtMes.Value.Month), New UTF8Encoding(True))
                                Ui.Informar(Me, "Resultado exportado.")
                            End Sub)
        End Using
    End Sub
End Class
