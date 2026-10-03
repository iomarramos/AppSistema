Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>Planificado vs realizado: resumen (raciones, venta, costo, Food Cost), componentes y productos.</summary>
Partial Public Class FormComparativo

    Public NotInheritable Class FilaResumen
        Public Property Concepto As String
        Public Property Teorico As String
        Public Property Real As String
        Public Property Diferencia As String
    End Class

    Public Sub New()
        InitializeComponent()
    End Sub

    Public Sub New(c As ComparativoDto)
        InitializeComponent()
        Controls.Clear()
        Text = "Teorico vs real - " & c.Titulo
        Width = 1100 : Height = 700
        StartPosition = FormStartPosition.CenterParent
        Dim resumen = Ui.NuevaGrilla(), componentes = Ui.NuevaGrilla(), productos = Ui.NuevaGrilla()
        Dim n = Function(v As Long?) If(v.HasValue, v.Value.ToString("N0"), "-")
        Dim d = Function(v As Long?) If(v.HasValue, Ui.Dinero(v.Value), "pendiente")
        Dim p = Function(v As Long?) If(v.HasValue, Ui.Cantidad(v.Value) & " %", "-")
        Dim filas As New List(Of FilaResumen) From {
            New FilaResumen With {.Concepto = "Comensales / raciones planificadas", .Teorico = n(c.ComensalesPlan), .Real = "", .Diferencia = ""},
            New FilaResumen With {.Concepto = "Raciones preparadas", .Teorico = n(c.ComensalesPlan), .Real = n(c.RacionesPreparadas),
                                  .Diferencia = If(c.RacionesPreparadas.HasValue, n(c.RacionesPreparadas.Value - c.ComensalesPlan), "")},
            New FilaResumen With {.Concepto = "Raciones consumidas (servidas)", .Teorico = n(c.ComensalesPlan), .Real = n(c.RacionesConsumidas),
                                  .Diferencia = If(c.RacionesConsumidas.HasValue, n(c.RacionesConsumidas.Value - c.ComensalesPlan), "")},
            New FilaResumen With {.Concepto = "Raciones excedentes", .Teorico = "", .Real = n(c.RacionesExcedentes), .Diferencia = ""},
            New FilaResumen With {.Concepto = "Raciones vendidas / no vendidas", .Teorico = n(c.ComensalesPlan), .Real = n(c.RacionesVendidas),
                                  .Diferencia = If(c.RacionesNoVendidas.HasValue, $"{n(c.RacionesNoVendidas)} no vendidas", "venta real sin cargar")},
            New FilaResumen With {.Concepto = "Venta", .Teorico = d(c.VentaTeoricaU6), .Real = d(c.VentaRealU6), .Diferencia = d(c.DiferenciaVentaU6)},
            New FilaResumen With {.Concepto = "Costo de alimentos", .Teorico = d(c.CostoTeoricoU6), .Real = Ui.Dinero(c.CostoRealU6), .Diferencia = d(c.DiferenciaCostoU6)},
            New FilaResumen With {.Concepto = "Costo teorico de lo consumido", .Teorico = d(c.CostoTeoricoConsumidoU6), .Real = Ui.Dinero(c.CostoRealU6),
                                  .Diferencia = If(c.CostoTeoricoConsumidoU6.HasValue, Ui.Dinero(c.CostoRealU6 - c.CostoTeoricoConsumidoU6.Value), "")},
            New FilaResumen With {.Concepto = $"Food Cost (objetivo {c.FoodCostObjetivoBp / 100D:0.##} %)", .Teorico = p(c.FoodCostTeoricoU6), .Real = p(c.FoodCostRealU6), .Diferencia = ""}}
        Ui.Mostrar(resumen, filas, "Concepto|Concepto", "Teorico|Teorico (planificado)", "Real|Real", "Diferencia|Diferencia")
        Ui.Mostrar(componentes, c.Componentes, "Estructura|Componente", "Receta|Receta", "FactorPlanTexto|Factor plan", "RacionesPlan|Raciones plan",
                   "RacionesPreparadas|Preparadas", "RacionesConsumidas|Consumidas", "FactorRealTexto|Factor real", "CostoPlanU6|Costo plan",
                   "CostoTeoricoConsumidoU6|Costo teorico de lo consumido")
        Ui.Mostrar(productos, c.Productos, "Estado|Estado", "Producto|Producto", "Unidad|Unidad", "CantidadTeoricaU6|Cantidad teorica", "EntregadoU6|Entregado", "DevueltoU6|Devuelto", "CantidadRealU6|Consumo real (neto)",
                   "DiferenciaCantidadU6|Diferencia cantidad", "CostoTeoricoU6|Costo teorico", "CostoRealU6|Costo real", "DiferenciaCostoU6|Diferencia costo")
        Dim abajo As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal}
        abajo.Panel1.Controls.Add(componentes)
        abajo.Panel2.Controls.Add(productos)
        Dim division As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal, .SplitterDistance = 230}
        division.Panel1.Controls.Add(resumen)
        division.Panel2.Controls.Add(abajo)
        Controls.Add(division)
        Controls.Add(New Label With {.Dock = DockStyle.Top, .Height = 34, .Padding = New Padding(4),
            .Text = $"{c.Minutas} minuta(s). Productos 'no planificado': salieron del almacen para el servicio sin estar en la minuta; 'sin salida': planificados y no entregados." &
                    If(c.Mermas.Count > 0, " Mermas: " & String.Join("; ", c.Mermas), "")})
    End Sub
End Class
