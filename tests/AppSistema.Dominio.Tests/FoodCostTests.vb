Imports Xunit
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico

Public Class FoodCostTests

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    <Fact>
    Public Sub T40_4200_sobre_10000_con_objetivo_40_da_42_por_ciento_mas_2_pp_y_200_soles()
        Dim r = FoodCost.Calcular(U(4200D), U(10000D), 4000)
        Assert.True(r.Calculable)
        Assert.Equal(U(42D), r.PorcentajeU6)
        Assert.Equal(U(2D), r.DesviacionPuntosU6)
        Assert.Equal(U(4000D), r.PresupuestoU6)
        Assert.Equal(U(200D), r.DiferenciaPresupuestoU6)
    End Sub

    <Fact>
    Public Sub T41_ingreso_cero_no_es_calculable()
        Dim r = FoodCost.Calcular(U(4200D), 0, 4000)
        Assert.False(r.Calculable)
        Assert.Null(r.PorcentajeU6)
        Assert.False(FoodCost.Calcular(U(1D), U(-5D), Nothing).Calculable)
    End Sub

    <Fact>
    Public Sub Sin_objetivo_solo_calcula_el_porcentaje()
        Dim r = FoodCost.Calcular(U(1D), U(3D), Nothing)
        Assert.Equal(U(33.333333D), r.PorcentajeU6)
        Assert.Null(r.DesviacionPuntosU6)
    End Sub

End Class
