Imports Xunit
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico

Public Class ProduccionTests

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    <Fact>
    Public Sub Una_presentacion_que_alcanza_con_el_menor_excedente()
        ' 3,2 kg: bolsa de 5 kg (excedente 1,8) o 4 bolsas de 1 kg (excedente 0,8).
        Dim r = Produccion.PlanificarEntrega(U(3.2D), {New PresentacionDisponible(1, U(5D), U(20D), U(3D)), New PresentacionDisponible(2, U(1D), U(10D), U(4D))})
        Assert.Equal("2x4", String.Join(",", r.Lineas.Select(Function(l) $"{l.VarianteId}x{l.Envases}")))
        Assert.Equal(U(0.8D), r.ExcedentePresentacionU6)
        Assert.Equal(0L, r.FaltanteU6)
    End Sub

    <Fact>
    Public Sub Se_descarga_la_presentacion_completa_aunque_se_necesite_menos()
        ' 0,5 L de aceite con bidones de 5 L: sale un bidón completo.
        Dim r = Produccion.PlanificarEntrega(U(0.5D), {New PresentacionDisponible(7, U(5D), U(210D), U(6.824D))})
        Assert.Equal(1L, r.Lineas.Single().Envases)
        Assert.Equal(U(5D), r.EntregadoU6)
        Assert.Equal(U(4.5D), r.ExcedentePresentacionU6)
    End Sub

    <Fact>
    Public Sub Combina_presentaciones_si_ninguna_alcanza_sola_y_reporta_faltante()
        ' 12 L con 2 bidones de 5 L y 1 botella de 1 L: 11 L entregados, falta 1 L.
        Dim r = Produccion.PlanificarEntrega(U(12D), {New PresentacionDisponible(1, U(5D), U(10D), U(6D)), New PresentacionDisponible(2, U(1D), U(1D), U(7D))})
        Assert.Equal(U(11D), r.EntregadoU6)
        Assert.Equal(U(1D), r.FaltanteU6)
        ' 7 L con bidones de 5 L (3 en stock) y botellas de 1 L (1 en stock): 1 bidón + 1 botella no alcanza, mejor 2 bidones.
        Dim r2 = Produccion.PlanificarEntrega(U(7D), {New PresentacionDisponible(1, U(5D), U(15D), U(6D)), New PresentacionDisponible(2, U(1D), U(1D), U(7D))})
        Assert.Equal("1x2", String.Join(",", r2.Lineas.Select(Function(l) $"{l.VarianteId}x{l.Envases}")))
        ' Stock parcial de un envase no cuenta (no se abre una presentación a medias).
        Dim r3 = Produccion.PlanificarEntrega(U(1D), {New PresentacionDisponible(1, U(5D), U(4.5D), U(6D))})
        Assert.Equal(U(1D), r3.FaltanteU6)
    End Sub

End Class
