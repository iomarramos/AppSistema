Imports Xunit
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico

''' <summary>Venta desde la estructura del menú: factores de consumo, reparto de alternativas y Food Cost objetivo 48 %.</summary>
Public Class VentaEstructuraTests

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    <Fact>
    Public Sub Raciones_por_factor_y_reparto_para_500_comensales()
        Assert.Equal(500L, VentaEstructura.Raciones(500, 10000, 10000))     ' plato caliente 100 %
        Assert.Equal(250L, VentaEstructura.Raciones(500, 10000, 5000))      ' jugo A 50 % del jugo
        Assert.Equal(350L, VentaEstructura.Raciones(500, 7000, 10000))      ' complemento 1 al 70 %
        Assert.Equal(150L, VentaEstructura.Raciones(500, 3000, 10000))      ' complemento 2 al 30 %
        Assert.Equal(53L, VentaEstructura.Raciones(175, 3000, 10000))       ' 52,5 → 53
    End Sub

    <Fact>
    Public Sub Venta_es_costo_entre_48_por_ciento_y_deja_52_de_margen()
        Dim venta = VentaEstructura.Venta(U(2400D), VentaEstructura.ObjetivoPorDefectoBp)
        Assert.Equal(U(5000D), venta)
        Assert.Equal(U(48D), FoodCost.Calcular(U(2400D), venta, 4800).PorcentajeU6)
        Assert.Equal(U(2600D), venta - U(2400D))                              ' margen 52 %
        ' Si el consumo real sube a S/ 2 600, el Food Cost real es 52 %: 4 puntos sobre el objetivo.
        Dim real = FoodCost.Calcular(U(2600D), venta, 4800)
        Assert.Equal(U(52D), real.PorcentajeU6)
        Assert.Equal(U(4D), real.DesviacionPuntosU6)
        Assert.Throws(Of AppSistema.Dominio.ReglaNegocioException)(Function() VentaEstructura.Venta(1, 0))
    End Sub

End Class
