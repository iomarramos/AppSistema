Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Catalogo
Imports AppSistema.Dominio.Numerico

Public Class PrevisionTests

    Private Shared ReadOnly Desde As New Date(2026, 4, 1)
    Private Shared ReadOnly Hasta As New Date(2026, 4, 30)

    Private Shared Function L(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    Private Shared Function Ev(dia As Integer, litros As Decimal) As EventoStock
        Return New EventoStock(New Date(2026, 4, dia), L(litros))
    End Function

    <Fact>
    Public Sub T12_demanda_50_reserva_10_stock_27_transito_8_a_tiempo_da_25()
        Dim r = Prevision.CalcularProducto(L(27D), L(10D), Desde, Hasta, {Ev(10, 30D), Ev(20, 20D)}, {Ev(5, 8D)})
        Assert.Equal(L(25D), r.NecesidadNetaU6)
        Assert.Equal(L(50D), r.DemandaHorizonteU6)
        Assert.Equal(L(8D), r.RecepcionesElegiblesU6)
        Assert.Equal(New Date(2026, 4, 20), r.FechaQuiebre)
        Assert.Equal(Compras.NecesidadNetaU6(L(50D), L(10D), L(27D), L(8D)), r.NecesidadNetaU6)
    End Sub

    <Fact>
    Public Sub T16_transito_que_llega_despues_de_la_falta_no_la_cubre()
        ' 50 L el día 5 con 27 en stock: faltan 23 ese día. Llegan 40 el día 20 (tarde).
        Dim r = Prevision.CalcularProducto(L(27D), L(10D), Desde, Hasta, {Ev(5, 50D)}, {Ev(20, 40D)})
        Assert.Equal(New Date(2026, 4, 5), r.FechaQuiebre)
        Assert.Equal(L(23D), r.MayorFaltanteU6)
        Assert.Equal(L(40D), r.RecepcionesTardiasU6)
        Assert.Equal(L(23D), r.NecesidadNetaU6)   ' la fórmula del total diría 0 (27 + 40 − 50 ≥ 10)
    End Sub

    <Fact>
    Public Sub Consumo_puente_se_cuenta_una_vez_y_lo_que_llega_despues_del_horizonte_no()
        Dim r = Prevision.CalcularProducto(L(27D), 0, Desde, Hasta,
                                           {New EventoStock(New Date(2026, 3, 28), L(7D)), Ev(10, 30D)}, {New EventoStock(New Date(2026, 5, 3), L(100D))})
        Assert.Equal(L(7D), r.ConsumoPuenteU6)
        Assert.Equal(L(30D), r.DemandaHorizonteU6)
        Assert.Equal(0L, r.RecepcionesElegiblesU6)
        Assert.Equal(L(10D), r.NecesidadNetaU6)   ' 27 − 7 − 30 = −10
    End Sub

    <Fact>
    Public Sub Sin_demanda_ni_reserva_no_hay_necesidad_ni_quiebre()
        Dim r = Prevision.CalcularProducto(L(5D), 0, Desde, Hasta, {}, {})
        Assert.Equal(0L, r.NecesidadNetaU6)
        Assert.Null(r.FechaQuiebre)
    End Sub

    <Fact>
    Public Sub T17_dos_variantes_para_25_L_asignacion_unica_con_excedente_explicado()
        Dim caja16 As New EmpaqueCompra(New VarianteProducto("A-4L", L(4D)), 4)
        Dim bidon5 As New EmpaqueCompra(New VarianteProducto("B-5L", L(5D)), 1)
        Dim a = Prevision.Asignar(L(25D), {(caja16, L(16D)), (bidon5, L(9D))})
        Assert.Equal(1L, a.Lineas(0).Empaques)
        Assert.Equal(2L, a.Lineas(1).Empaques)
        Assert.Equal(L(26D), a.TotalU6)
        Assert.Equal(L(1D), a.ExcesoU6)
        Assert.Equal("ASIGNACION_DESCUADRADA", Assert.Throws(Of ReglaNegocioException)(
            Function() Prevision.Asignar(L(25D), {(caja16, L(25D)), (bidon5, L(25D))})).Codigo)
    End Sub

End Class
