Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>
''' Abastecimiento: consolidado de compras de un periodo entre todas las operaciones a las que la persona tiene acceso.
''' La demanda sale de las minutas que arma Planificación. Arriba va el total por producto y abajo el detalle por
''' operación del producto elegido.
''' </summary>
Public Class FormConsolidado
    Inherits Form

    Private ReadOnly _servicio As ServicioConsolidadoCompras
    Private ReadOnly _desde As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 110}
    Private ReadOnly _hasta As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 110}
    Private ReadOnly _borradores As New CheckBox With {.Text = "Incluir minutas en borrador", .AutoSize = True, .Margin = New Padding(6, 8, 3, 3)}
    Private ReadOnly _lineas As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _detalle As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _total As New Label With {.Dock = DockStyle.Bottom, .Height = 28, .Padding = New Padding(6)}
    Private _resultado As ConsolidadoComprasDto

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _servicio = New ServicioConsolidadoCompras(cadena, sesion)
        Text = "Consolidado de compras"
        ' Por defecto, el mes siguiente completo.
        Dim inicio = New Date(Date.Today.Year, Date.Today.Month, 1).AddMonths(1)
        _desde.Value = inicio
        _hasta.Value = inicio.AddMonths(1).AddDays(-1)
        Dim division As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal, .SplitterDistance = 300}
        division.Panel1.Controls.Add(_lineas)
        division.Panel2.Controls.Add(_detalle)
        division.Panel2.Controls.Add(New Label With {.Text = "Detalle por operacion del producto elegido", .Dock = DockStyle.Top, .Padding = New Padding(4)})
        Controls.Add(division)
        Controls.Add(_total)
        Controls.Add(New Label With {.Dock = DockStyle.Top, .Height = 34, .Padding = New Padding(4),
            .Text = "A comprar = demanda de las minutas + reserva - stock - pendiente de recibir, por operacion. Costo con el producto activo de cada operacion, sin IGV."})
        Controls.Add(Ui.BarraBotones(New Label With {.Text = "Desde", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _desde,
                                     New Label With {.Text = "Hasta", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _hasta, _borradores,
                                     Ui.Boton("Calcular", AddressOf Calcular),
                                     Ui.Boton("Imprimir consolidado...", Sub() SalidaReporte.Emitir(Me, Function() _servicio.Reporte(_desde.Value, _hasta.Value, _borradores.Checked)))))
        AddHandler _lineas.SelectionChanged, Sub() MostrarDetalle()
    End Sub

    Private Sub Calcular()
        If Not Ui.Ejecutar(Me, Sub() _resultado = _servicio.Calcular(_desde.Value, _hasta.Value, _borradores.Checked)) Then Return
        Ui.Mostrar(_lineas, _resultado.Lineas, "Familia|Familia", "ProductoCodigo|Codigo", "Producto|Producto", "Unidad|Unidad", "DemandaU6|Demanda",
                   "StockU6|Stock", "ReservaU6|Reserva", "PendienteU6|Pendiente de recibir", "AComprarU6|A comprar", "CostoEstimadoU6|Costo estimado",
                   "Operaciones|Operaciones")
        _total.Text = $"{_resultado.Operaciones.Count} operacion(es): {String.Join(", ", _resultado.Operaciones)}  |  {_resultado.Lineas.Count} productos  |  " &
                      $"Costo estimado {Ui.Dinero(_resultado.CostoTotalU6)}" & If(_resultado.ProductosSinPrecio > 0, $"  |  {_resultado.ProductosSinPrecio} sin precio", "")
        MostrarDetalle()
    End Sub

    Private Sub MostrarDetalle()
        Dim l = Ui.Seleccionado(Of ConsolidadoLineaDto)(_lineas)
        If l Is Nothing OrElse _resultado Is Nothing Then Return
        Ui.Mostrar(_detalle, _resultado.Detalle.Where(Function(d) d.ProductoBaseId = l.ProductoBaseId).ToList(),
                   "Operacion|Operacion", "DemandaU6|Demanda", "StockU6|Stock", "ReservaU6|Reserva", "PendienteU6|Pendiente de recibir",
                   "AComprarU6|A comprar", "CostoUnitarioU6|Costo unitario", "CostoEstimadoU6|Costo estimado")
    End Sub

End Class
