Imports Xunit
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico

Public Class ContratosTests

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    <Fact>
    Public Sub Mes_completo_reconoce_el_importe_mensual_entero()
        Dim l As New LineaContrato With {.ImporteMensualU6 = U(30000D), .FechaDesde = New Date(2026, 1, 1)}
        Assert.Equal(U(30000D), IngresoContrato.ImporteDelMesU6(l, 2026, 2))   ' febrero de 28 días: igual el mes completo
    End Sub

    <Fact>
    Public Sub Inicio_a_mitad_de_mes_se_prorratea_por_dias()
        Dim l As New LineaContrato With {.ImporteMensualU6 = U(30000D), .FechaDesde = New Date(2026, 9, 16)}
        Assert.Equal(15, IngresoContrato.DiasCubiertos(l, 2026, 9))
        Assert.Equal(U(15000D), IngresoContrato.ImporteDelMesU6(l, 2026, 9))
        Assert.Equal(0L, IngresoContrato.ImporteDelMesU6(l, 2026, 8))
    End Sub

    <Fact>
    Public Sub Ajuste_a_mitad_de_mes_suma_las_dos_lineas_sin_perder_ni_duplicar_dias()
        Dim antes As New LineaContrato With {.ImporteMensualU6 = U(31000D), .FechaDesde = New Date(2026, 1, 1), .FechaHasta = New Date(2026, 10, 10)}
        Dim despues As New LineaContrato With {.ImporteMensualU6 = U(62000D), .FechaDesde = New Date(2026, 10, 11)}
        Assert.Equal(31, IngresoContrato.DiasCubiertos(antes, 2026, 10) + IngresoContrato.DiasCubiertos(despues, 2026, 10))
        ' 10 días a 31 000/31 + 21 días a 62 000/31 = 10 000 + 42 000
        Assert.Equal(U(52000D), IngresoContrato.ImporteDelMesU6({antes, despues}, 2026, 10))
    End Sub

    <Fact>
    Public Sub Resultado_resta_alimentos_y_gastos_del_ingreso()
        Dim r As New ResultadoServicio With {.IngresoU6 = U(10000D), .CostoAlimentosU6 = U(4200D), .GastosPersonalU6 = U(3000D), .GastosOperacionU6 = U(800D)}
        Assert.Equal(U(8000D), r.TotalGastosU6)
        Assert.Equal(U(2000D), r.MargenU6)
        Assert.Equal(U(20D), r.MargenPorcentajeU6)
        Assert.Null(New ResultadoServicio With {.CostoAlimentosU6 = 1}.MargenPorcentajeU6)
    End Sub

End Class
