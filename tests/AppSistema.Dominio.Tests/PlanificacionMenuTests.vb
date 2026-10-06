Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico

''' <summary>
''' Planificación de menús: raciones por factor, costos, costo por bandeja, diferencias, cobertura de alternativas y reparto
''' de residuos. Incluye la prueba numérica del encargo (A 100 × S/ 2,50 + B 80 × S/ 1,25 = S/ 350; 100 comensales → S/ 3,50).
''' </summary>
Public Class PlanificacionMenuTests

    Private Shared Function Soles(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    <Fact>
    Public Sub Con_520_comensales_el_factor_60_y_40_da_312_y_208_raciones()
        Assert.Equal(312L, VentaEstructura.Raciones(520, 6000, 10000))
        Assert.Equal(208L, VentaEstructura.Raciones(520, 4000, 10000))
    End Sub

    <Fact>
    Public Sub Al_cambiar_a_70_y_30_las_raciones_son_364_y_156_y_los_comensales_siguen_en_520()
        Assert.Equal(364L, VentaEstructura.Raciones(520, 7000, 10000))
        Assert.Equal(156L, VentaEstructura.Raciones(520, 3000, 10000))
    End Sub

    <Fact>
    Public Sub Una_sopa_al_100_por_ciento_genera_520_raciones_y_el_porcentaje_2_5_es_el_factor_0_025()
        Assert.Equal(520L, VentaEstructura.Raciones(520, 10000, 10000))
        Assert.Equal(13L, VentaEstructura.Raciones(520, 250, 10000))
    End Sub

    <Fact>
    Public Sub Prueba_numerica_del_encargo_materia_prima_350_y_bandeja_3_50()
        Dim costoA = PlanificacionMenu.CostoTotalU6(100, Soles(2.5D))
        Dim costoB = PlanificacionMenu.CostoTotalU6(80, Soles(1.25D))
        Assert.Equal(Soles(250D), costoA)
        Assert.Equal(Soles(100D), costoB)

        Dim materiaPrima = PlanificacionMenu.SumaCostosU6({costoA, costoB})
        Assert.Equal(Soles(350D), materiaPrima)
        Assert.Equal(Soles(3.5D), PlanificacionMenu.CostoBandejaU6(materiaPrima, 100))
    End Sub

    <Fact>
    Public Sub Costo_por_bandeja_sin_comensales_es_no_calculable_y_costo_cero_es_un_valor_valido()
        Assert.Null(PlanificacionMenu.CostoBandejaU6(Soles(350D), 0))
        Assert.Equal(0L, PlanificacionMenu.CostoBandejaU6(0L, 100).Value)
    End Sub

    <Fact>
    Public Sub Un_costo_pendiente_deja_el_total_pendiente_y_no_se_suma_como_cero()
        Dim costoPendiente As Long? = PlanificacionMenu.CostoTotalU6(100, Nothing)
        Assert.Null(costoPendiente)
        Assert.Null(PlanificacionMenu.SumaCostosU6({Soles(250D), costoPendiente}))
        Assert.Null(PlanificacionMenu.CostoBandejaU6(Nothing, 100))
    End Sub

    <Fact>
    Public Sub Costo_mensual_por_bandeja_usa_totales_y_no_promedio_de_dias()
        ' Día 1: 100 comensales y S/ 350. Día 2: 300 comensales y S/ 300.
        ' Promedio simple de bandejas = (3,50 + 1,00) / 2 = 2,25. Correcto = (350 + 300) / 400 = 1,625.
        Dim dia1 As New ResumenPlanificacion With {.Comensales = 100, .MateriaPrimaU6 = Soles(350D), .EstructuraFijaU6 = 0L}
        Dim dia2 As New ResumenPlanificacion With {.Comensales = 300, .MateriaPrimaU6 = Soles(300D), .EstructuraFijaU6 = 0L}
        Dim mes = ResumenPlanificacion.Sumar({dia1, dia2})
        Assert.Equal(400L, mes.Comensales)
        Assert.Equal(Soles(650D), mes.CostoTotalU6)
        Assert.Equal(Soles(1.625D), mes.CostoBandejaU6)
    End Sub

    <Fact>
    Public Sub Un_dia_con_costo_pendiente_deja_el_resumen_del_mes_pendiente()
        Dim dia1 As New ResumenPlanificacion With {.Comensales = 100, .MateriaPrimaU6 = Soles(350D), .EstructuraFijaU6 = 0L}
        Dim dia2 As New ResumenPlanificacion With {.Comensales = 100, .MateriaPrimaU6 = Nothing, .EstructuraFijaU6 = 0L}
        Dim mes = ResumenPlanificacion.Sumar({dia1, dia2})
        Assert.Null(mes.MateriaPrimaU6)
        Assert.Null(mes.CostoTotalU6)
        Assert.Null(mes.CostoBandejaU6)
        Assert.Equal(200L, mes.Comensales)
    End Sub

    <Fact>
    Public Sub Diferencia_monetaria_es_realizado_menos_planificado_y_porcentual_no_se_calcula_con_plan_cero()
        Assert.Equal(Soles(35D), PlanificacionMenu.DiferenciaMonetariaU6(Soles(350D), Soles(385D)))
        ' (385 − 350) ÷ 350 = 10 % = 1000 puntos básicos.
        Assert.Equal(1000L, PlanificacionMenu.DiferenciaPorcentualBp(Soles(350D), Soles(385D)).Value)
        Assert.Null(PlanificacionMenu.DiferenciaPorcentualBp(0L, Soles(10D)))
        Assert.Null(PlanificacionMenu.DiferenciaPorcentualBp(Nothing, Soles(10D)))
    End Sub

    <Fact>
    Public Sub Alternativas_con_cobertura_obligatoria_deben_sumar_100_por_ciento()
        Assert.Null(PlanificacionMenu.ValidarCobertura({6000L, 4000L}, True))
        Assert.NotNull(PlanificacionMenu.ValidarCobertura({5000L, 4000L}, True))
        Assert.NotNull(PlanificacionMenu.ValidarCobertura({6000L, 5000L}, False))
        ' Una entrada, sopa o guarnición puede ser del 100 % o menos: no se exige que toda la matriz sume 100 %.
        Assert.Null(PlanificacionMenu.ValidarCobertura({10000L}, False))
        Assert.Null(PlanificacionMenu.ValidarCobertura({2500L}, False))
        Assert.NotNull(PlanificacionMenu.ValidarCobertura({-1L}, False))
    End Sub

    <Fact>
    Public Sub El_reparto_de_residuos_suma_exactamente_el_total_y_es_determinista()
        ' 100 × 33,33 % = 33 y 33 y 33 con residuo 1: la alternativa de mayor fracción (la 3ª, 34 %) lo recibe.
        Dim cien = PlanificacionMenu.RepartirResiduo(100, {3333L, 3333L, 3334L})
        Assert.Equal(33L, cien(0))
        Assert.Equal(33L, cien(1))
        Assert.Equal(34L, cien(2))
        Dim diez = PlanificacionMenu.RepartirResiduo(10, {3333L, 3333L, 3334L})
        Assert.Equal(3L, diez(0))
        Assert.Equal(3L, diez(1))
        Assert.Equal(4L, diez(2))
        Dim mitades = PlanificacionMenu.RepartirResiduo(520, {5000L, 5000L})
        Assert.Equal(260L, mitades(0))
        Assert.Equal(260L, mitades(1))
        Dim reparto = PlanificacionMenu.RepartirResiduo(521, {3333L, 3333L, 3334L})
        Assert.Equal(521L, reparto.Sum())
    End Sub

    <Fact>
    Public Sub El_reparto_rechaza_pesos_que_no_suman_100_por_ciento()
        Assert.Throws(Of ReglaNegocioException)(Sub() PlanificacionMenu.RepartirResiduo(100, {5000L, 4000L}))
    End Sub

End Class
