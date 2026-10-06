Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>
''' Abastecimiento: consolidado de compras de un periodo entre todas las operaciones a las que la persona tiene acceso.
''' La demanda sale de las minutas que arma Planificación. Arriba va el total por producto y abajo el detalle por
''' operación del producto elegido.
''' </summary>
Partial Public Class FormConsolidado

    Private ReadOnly _servicio As ServicioConsolidadoCompras
    Private _resultado As ConsolidadoComprasDto

    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridLineas)
        Ui.Configurar(gridDetalle)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridLineas)
        Ui.Configurar(gridDetalle)
        _servicio = New ServicioConsolidadoCompras(cadena, sesion)
        Text = "Consolidado de compras"
        ' Por defecto, el mes siguiente completo.
        Dim inicio = New Date(Date.Today.Year, Date.Today.Month, 1).AddMonths(1)
        dtDesde.Value = inicio
        dtHasta.Value = inicio.AddMonths(1).AddDays(-1)
        barraFiltros.Controls.Add(Ui.Boton("Calcular", AddressOf Calcular))
        barraFiltros.Controls.Add(Ui.Boton("Imprimir consolidado...", Sub() SalidaReporte.Emitir(Me, Function() _servicio.Reporte(dtDesde.Value, dtHasta.Value, chkBorradores.Checked))))
        AddHandler gridLineas.SelectionChanged, Sub() MostrarDetalle()
    End Sub

    Private Sub Calcular()
        If Not Ui.Ejecutar(Me, Sub() _resultado = _servicio.Calcular(dtDesde.Value, dtHasta.Value, chkBorradores.Checked)) Then Return
        Ui.Mostrar(gridLineas, _resultado.Lineas, "Familia|Familia", "ProductoCodigo|Codigo", "Producto|Producto", "Unidad|Unidad", "DemandaU6|Demanda",
                   "StockU6|Stock", "ReservaU6|Reserva", "PendienteU6|Pendiente de recibir", "AComprarU6|A comprar", "CostoEstimadoU6|Costo estimado",
                   "Operaciones|Operaciones")
        lblTotal.Text = $"{_resultado.Operaciones.Count} operacion(es): {String.Join(", ", _resultado.Operaciones)}  |  {_resultado.Lineas.Count} productos  |  " &
                      $"Costo estimado {Ui.Dinero(_resultado.CostoTotalU6)}" & If(_resultado.ProductosSinPrecio > 0, $"  |  {_resultado.ProductosSinPrecio} sin precio", "")
        MostrarDetalle()
    End Sub

    Private Sub MostrarDetalle()
        Dim l = Ui.Seleccionado(Of ConsolidadoLineaDto)(gridLineas)
        If l Is Nothing OrElse _resultado Is Nothing Then Return
        Ui.Mostrar(gridDetalle, _resultado.Detalle.Where(Function(d) d.ProductoBaseId = l.ProductoBaseId).ToList(),
                   "Operacion|Operacion", "DemandaU6|Demanda", "StockU6|Stock", "ReservaU6|Reserva", "PendienteU6|Pendiente de recibir",
                   "AComprarU6|A comprar", "CostoUnitarioU6|Costo unitario", "CostoEstimadoU6|Costo estimado")
    End Sub

End Class
