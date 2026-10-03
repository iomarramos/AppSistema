Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico

''' <summary>
''' Teórico → plan real → realizado con cifras del SGP (datos/plan_real, Orcopampa, desayuno del 1/08/2026). El conversor
''' verifica las mismas fórmulas en todos los días del archivo (validacion.txt).
''' </summary>
Public Class PlanVsRealTests

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    Private Shared Function Centimos(u6 As Long?) As Decimal
        Return Math.Round(EscalaU6.ADecimal(u6.Value), 2, MidpointRounding.AwayFromZero)
    End Function

    ''' <summary>Desayuno teórico del 1/08/2026: raciones y costo por ración de cada plato, 250 comensales.</summary>
    Private Shared ReadOnly DesayunoTeorico As (Raciones As Long, CostoRacionU6 As Long)() = {
        (149, U(2.65D)), (150, U(0.53D)), (149, U(0.53D)), (89, U(1.28D)), (65, U(0.87D)), (248, U(0.18D)), (60, U(0.33D)),
        (50, U(0.08D)), (65, U(0.66D)), (65, U(1.08D)), (120, U(0.45D)), (76, U(0.13D)), (76, U(0.86D)),
        (110, U(0.08D)), (110, U(0.08D)), (110, U(0.04D)), (110, U(0.11D)), (110, U(0.08D))}

    <Fact>
    Public Sub El_costo_minuta_dia_del_SGP_sale_de_raciones_por_costo_entre_comensales()
        Assert.Equal(U(1077.47D), PlanVsReal.CostoTotalU6(DesayunoTeorico))         ' costo total del comparativo
        Assert.Equal(4.31D, Centimos(PlanVsReal.CostoMinutaDiaU6(DesayunoTeorico, 250)))
        Assert.Null(PlanVsReal.CostoMinutaDiaU6(DesayunoTeorico, 0))
    End Sub

    <Fact>
    Public Sub El_porcentaje_del_plan_real_es_raciones_entre_comensales()
        Assert.Equal(7083L, PlanVsReal.PorcentajeBp(170, 240))                      ' saltado de atún: 0,7083
        Assert.Equal(4167L, PlanVsReal.PorcentajeBp(100, 240))                      ' arroz blanco: 0,4167
        Assert.Null(PlanVsReal.PorcentajeBp(10, 0))
    End Sub

    <Fact>
    Public Sub Compara_los_tres_niveles_como_el_reporte_del_SGP()
        Dim c = PlanVsReal.Comparar(New NivelCosto(U(1077.47D), 250), New NivelCosto(U(1175.56D), 240), New NivelCosto(U(959.53D), 284))
        Assert.Equal(4.31D, Centimos(c.Teorico.CostoBandejaU6))
        Assert.Equal(4.9D, Centimos(c.PlanReal.CostoBandejaU6))
        Assert.Equal(3.38D, Centimos(c.Realizado.Value.CostoBandejaU6))
        Assert.Equal(0.59D, Centimos(c.DesviacionPlanU6))                           ' C.Band.Plan.
        Assert.Equal(-1.52D, Centimos(c.DesviacionRealizadoU6))                     ' C.Band.Realizado
        Assert.Equal(-0.93D, Centimos(c.DesviacionTotalU6))
    End Sub

    <Fact>
    Public Sub Sin_realizado_la_desviacion_queda_vacia_y_no_en_cero()
        ' Desayuno del 1/10/2026: el SGP muestra realizado 0 y desviación 0 porque el día aún no ocurrió.
        Dim c = PlanVsReal.Comparar(New NivelCosto(U(1556.03D), 520), New NivelCosto(U(1609.94D), 500), New NivelCosto(0, 0))
        Assert.Equal(0.23D, Centimos(c.DesviacionPlanU6))
        Assert.Null(c.Realizado)
        Assert.Null(c.DesviacionRealizadoU6)
        Assert.Null(c.DesviacionTotalU6)
        Assert.Null(New NivelCosto(0, 0).CostoBandejaU6)
    End Sub

    <Fact>
    Public Sub El_costo_piso_y_techo_sale_de_los_factores_y_la_racion_mas_barata_y_mas_cara()
        ' 300 comensales: el fondo lo toma todo el comedor (factor 100 %, de S/ 2,00 a S/ 3,00) y el postre la mitad (50 %, de S/ 0,40 a S/ 0,80).
        Dim banda = PlanVsReal.BandaCosto({(300L, U(2D), U(3D)), (150L, U(0.4D), U(0.8D))}, 300).Value
        Assert.Equal(U(2.2D), banda.PisoU6)
        Assert.Equal(U(3.4D), banda.TechoU6)
        Assert.True(PlanVsReal.DentroDeBanda(U(2.5D), banda))
        Assert.True(PlanVsReal.DentroDeBanda(U(3.4D), banda))
        Assert.False(PlanVsReal.DentroDeBanda(U(3.41D), banda))
        Assert.Null(PlanVsReal.BandaCosto({(1L, U(1D), U(2D))}, 0))
        Assert.Equal("DATO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(
            Function() PlanVsReal.BandaCosto({(1L, U(2D), U(1D))}, 10)).Codigo)
    End Sub

    <Fact>
    Public Sub Las_tablas_de_SUNAT_del_formato_13_1()
        Assert.Equal("10 SALIDA A PRODUCCION", Sunat.TablasSunat.TipoOperacion("salida_produccion"))
        Assert.Equal("02 COMPRA", Sunat.TablasSunat.TipoOperacion("recepcion"))
        Assert.Equal("13 MERMAS", Sunat.TablasSunat.TipoOperacion("baja"))
        Assert.Equal("01", Sunat.TablasSunat.TipoComprobante("factura"))
        Assert.Equal("00", Sunat.TablasSunat.TipoComprobante(Nothing))
        Assert.Equal("01 KILOGRAMOS", Sunat.TablasSunat.UnidadMedida("KG"))
        Assert.Equal("F001|123", String.Join("|", Sunat.TablasSunat.SerieYNumero("F001-123").Serie, Sunat.TablasSunat.SerieYNumero("F001-123").Numero))
        Assert.Equal("|RC-", String.Join("|", Sunat.TablasSunat.SerieYNumero("RC-").Serie, Sunat.TablasSunat.SerieYNumero("RC-").Numero))
    End Sub

    <Fact>
    Public Sub El_total_general_del_mes_da_el_costo_bandeja_de_cada_nivel()
        Dim c = PlanVsReal.Comparar(New NivelCosto(U(483236.68D), 139087), New NivelCosto(U(521956.37D), 140513), New NivelCosto(U(533600.06D), 149803))
        Assert.Equal("3.47|3.71|3.56", String.Join("|", Centimos(c.Teorico.CostoBandejaU6), Centimos(c.PlanReal.CostoBandejaU6),
                                                    Centimos(c.Realizado.Value.CostoBandejaU6)))
        Assert.Equal("DATO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(Function() New NivelCosto(-1, 1)).Codigo)
    End Sub

End Class
