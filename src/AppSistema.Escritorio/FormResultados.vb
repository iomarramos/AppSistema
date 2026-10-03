Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Gastos del mes (personal, operación, administración, otros; reales o presupuestados) y resultado mensual por servicio:
''' ingreso − alimentos − gastos = margen. Exporta el resultado en CSV para contabilidad.
''' </summary>
Public Class FormResultados
    Inherits Form

    Private ReadOnly _servicio As ServicioResultados
    Private ReadOnly _sesion As SesionUsuario
    Private ReadOnly _mes As New DateTimePicker With {.Format = DateTimePickerFormat.Custom, .CustomFormat = "MM/yyyy", .ShowUpDown = True, .Width = 90}
    Private ReadOnly _resultado As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _gastos As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _estado As New Label With {.Dock = DockStyle.Bottom, .Height = 28, .Padding = New Padding(6)}

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _servicio = New ServicioResultados(cadena, sesion)
        _sesion = sesion
        Text = "Gastos y resultado mensual - " & sesion.Operacion.Nombre
        Dim edita = sesion.Tiene(Permisos.GastosEditar)
        Dim barra = Ui.BarraBotones(New Label With {.Text = "Mes", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _mes,
                                    Ui.Boton("Actualizar", AddressOf Cargar), Ui.BotonSi(edita, "Registrar gasto...", AddressOf RegistrarGasto),
                                    Ui.BotonSi(edita, "Eliminar gasto", AddressOf EliminarGasto), Ui.Boton("Exportar CSV...", AddressOf Exportar))
        Dim division As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal, .SplitterDistance = 220}
        division.Panel1.Controls.Add(_resultado)
        division.Panel1.Controls.Add(_estado)
        division.Panel2.Controls.Add(_gastos)
        Controls.Add(division)
        Controls.Add(New Label With {.Dock = DockStyle.Top, .Height = 34, .Padding = New Padding(4),
            .Text = "Margen = ingreso - alimentos consumidos - gastos reales. Los gastos presupuestados solo se comparan. Bajas y ajustes de inventario van en 'No asignado'."})
        Controls.Add(barra)
        AddHandler _mes.ValueChanged, Sub() Cargar()
        AddHandler Load, Sub() Cargar()
    End Sub

    Private Sub Cargar()
        Dim a = _mes.Value.Year, m = _mes.Value.Month
        Ui.Ejecutar(Me,
            Sub()
                Dim r = _servicio.ResultadoMensual(a, m)
                Ui.Mostrar(_resultado, r.Lineas.Concat({r.Total}).ToList(), "Servicio|Servicio", "IngresoU6|Ingreso", "FuenteIngreso|Fuente del ingreso",
                           "CostoAlimentosU6|Alimentos", "GastosPersonalU6|Personal", "GastosOperacionU6|Operacion", "OtrosGastosU6|Otros",
                           "TotalGastosU6|Total gastos", "MargenU6|Margen", "MargenPorcentajeU6|Margen %", "PresupuestoGastosU6|Gastos presupuestados")
                _estado.Text = $"Mes {m:00}/{a}: {r.Estado}" & If(r.Estado = "cerrado", " (no admite cambios)", "")
                Ui.Mostrar(_gastos, _servicio.ListarGastos(a, m), "Concepto|Concepto", "Categoria|Categoria", "Servicio|Servicio", "CuentaContable|Cuenta",
                           "ImporteU6|Importe", "Proyectado|Presupuestado")
            End Sub)
    End Sub

    Private Sub RegistrarGasto()
        Dim servicios As List(Of OperacionServicioDto) = Nothing
        If Not Ui.Ejecutar(Me, Sub() servicios = _servicio.ServiciosDeOperacion()) Then Return
        Using d As New DialogoCampos($"Gasto de {_mes.Value:MM/yyyy}")
            d.Texto("concepto", "Concepto").Opciones("categoria", "Categoria", ServicioResultados.Categorias.Select(Function(c) CObj(c))) _
             .Opciones("servicio", "Servicio (vacio = comun a la operacion)", servicios.Select(Function(s) CObj(New Opcion(Of OperacionServicioDto)(s, $"{s.ServicioNombre} - {s.RegimenNombre}"))), permitirVacio:=True) _
             .Texto("importe", "Importe (S/)").Texto("cuenta", "Cuenta contable (opcional)").Marca("proyectado", "Es presupuesto (no gasto real)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim elegido = d.Elegido(Of Opcion(Of OperacionServicioDto))("servicio")
            Ui.Ejecutar(Me, Sub() _servicio.RegistrarGasto(_mes.Value.Year, _mes.Value.Month, If(elegido Is Nothing, CType(Nothing, Long?), elegido.Valor.Id), d.Valor("concepto"),
                                                            CStr(d.Elegido(Of String)("categoria")), Ui.LeerU6(d.Valor("importe"), "importe"), d.Marcado("proyectado"), d.Valor("cuenta")))
        End Using
        Cargar()
    End Sub

    Private Sub EliminarGasto()
        Dim g = Ui.Seleccionado(Of GastoDto)(_gastos)
        If g Is Nothing OrElse Not Ui.Confirmar(Me, $"Eliminar el gasto '{g.Concepto}'?") Then Return
        Ui.Ejecutar(Me, Sub() _servicio.EliminarGasto(g.Id))
        Cargar()
    End Sub

    Private Sub Exportar()
        Using a As New SaveFileDialog With {.Filter = "CSV (*.csv)|*.csv", .FileName = $"resultado_{_sesion.Operacion.Codigo}_{_mes.Value:yyyy-MM}.csv"}
            If a.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub()
                                File.WriteAllText(a.FileName, _servicio.ExportarCsv(_mes.Value.Year, _mes.Value.Month), New UTF8Encoding(True))
                                Ui.Informar(Me, "Resultado exportado.")
                            End Sub)
        End Using
    End Sub
End Class
