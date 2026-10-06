Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock

Public Class ValoracionTests

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    <Fact>
    Public Sub Ejemplo_de_la_guia_48_L_por_416_salida_6_L_vale_52()
        Assert.Equal(U(52D), Valoracion.ValorSalidaU6(U(48D), U(416D), U(6D)))
        Assert.Equal(U(8.666667D), Valoracion.CostoUnitarioU6(U(416D), U(48D)))
    End Sub

    <Fact>
    Public Sub Salida_que_agota_el_saldo_se_lleva_todo_el_valor_sin_residuo()
        ' 3 L por S/10: una salida de 1 L vale 3,333333; la última se lleva el resto exacto.
        Dim v1 = Valoracion.ValorSalidaU6(U(3D), U(10D), U(1D))
        Dim v2 = Valoracion.ValorSalidaU6(U(2D), U(10D) - v1, U(1D))
        Dim v3 = Valoracion.ValorSalidaU6(U(1D), U(10D) - v1 - v2, U(1D))
        Assert.Equal(U(10D), v1 + v2 + v3)
        Assert.Equal(U(3.333334D), v2)   ' 6,666667 / 2 redondeado
    End Sub

    <Fact>
    Public Sub Salida_mayor_al_saldo_es_stock_insuficiente()
        Assert.Equal("STOCK_INSUFICIENTE", Assert.Throws(Of ReglaNegocioException)(Function() Valoracion.ValorSalidaU6(U(27D), U(216D), U(28D))).Codigo)
    End Sub

End Class
