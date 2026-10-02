Imports Xunit
Imports AppSistema.Dominio.Catalogo
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico

Public Class CalculosTests

    Private Shared Function L(litros As Long) As Long
        Return litros * EscalaU6.Factor
    End Function

    Private Shared Function Caja16L(Optional minimo As Long = 1, Optional multiplo As Long = 1) As EmpaqueCompra
        Return New EmpaqueCompra(New VarianteProducto("ACE-A-4L", L(4)), 4, minimo, multiplo)
    End Function

    ' ---- Recetas (T08, T09) ----

    <Fact>
    Public Sub T08_Receta_de_10_raciones_con_1_L_para_150_raciones_requiere_15_L()
        Dim necesidad = Recetas.NecesidadIngredienteU6(cantidadBrutaU6:=L(1), rendimientoRacionesU6:=L(10), racionesU6:=L(150))
        Assert.Equal(L(15), necesidad)
    End Sub

    <Fact>
    Public Sub T08_Con_precio_de_S_8_por_litro_el_aceite_cuesta_S_120_y_S_0_80_por_racion()
        Dim litros = Recetas.NecesidadIngredienteU6(L(1), L(10), L(150))
        Dim costo = EscalaU6.Multiplicar(litros, EscalaU6.DesdeDecimal(8D))
        Assert.Equal(EscalaU6.DesdeDecimal(120D), costo)
        Assert.Equal(EscalaU6.DesdeDecimal(0.8D), EscalaU6.MultiplicarDividir(costo, L(1), L(150)))
    End Sub

    <Fact>
    Public Sub T09_Rendimiento_cero_se_rechaza()
        Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() Recetas.NecesidadIngredienteU6(L(1), 0, L(150)))
        Assert.Equal("RENDIMIENTO_INVALIDO", ex.Codigo)
    End Sub

    <Fact>
    Public Sub T09_Raciones_negativas_se_rechazan()
        Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() Recetas.NecesidadIngredienteU6(L(1), L(10), -L(1)))
        Assert.Equal("CANTIDAD_INVALIDA", ex.Codigo)
    End Sub

    ' ---- Previsión y empaques (T12–T15) ----

    <Fact>
    Public Sub T12_Demanda_50_reserva_10_stock_27_transito_8_da_25_L_netos()
        Assert.Equal(L(25), Compras.NecesidadNetaU6(L(50), L(10), L(27), L(8)))
    End Sub

    <Fact>
    Public Sub Necesidad_neta_nunca_es_negativa()
        Assert.Equal(0L, Compras.NecesidadNetaU6(L(10), L(0), L(50), L(0)))
    End Sub

    <Fact>
    Public Sub T13_Necesidad_25_L_con_cajas_de_16_L_pide_2_cajas_32_L_exceso_7_L()
        Dim r = Compras.EmpaquesAComprar(L(25), Caja16L())
        Assert.Equal(2L, r.Empaques)
        Assert.Equal(L(32), r.TotalBaseU6)
        Assert.Equal(L(7), r.ExcesoU6)
    End Sub

    <Fact>
    Public Sub T14_Con_multiplo_3_pide_3_cajas_48_L_exceso_23_L()
        Dim r = Compras.EmpaquesAComprar(L(25), Caja16L(multiplo:=3))
        Assert.Equal(3L, r.Empaques)
        Assert.Equal(L(48), r.TotalBaseU6)
        Assert.Equal(L(23), r.ExcesoU6)
    End Sub

    <Fact>
    Public Sub Minimo_4_y_multiplo_3_pide_6_cajas()
        Assert.Equal(6L, Compras.EmpaquesAComprar(L(25), Caja16L(minimo:=4, multiplo:=3)).Empaques)
    End Sub

    <Fact>
    Public Sub T15_Necesidad_cero_con_minimo_3_no_compra_nada()
        Dim r = Compras.EmpaquesAComprar(0, Caja16L(minimo:=3))
        Assert.Equal(0L, r.Empaques)
        Assert.Equal(0L, r.TotalBaseU6)
    End Sub

    <Theory>
    <InlineData(1, 1, 1)>      ' 1 mL de necesidad -> 1 caja
    <InlineData(16, 1, 1)>     ' exacto
    <InlineData(17, 1, 2)>     ' un poco más -> 2 cajas
    <InlineData(33, 1, 3)>
    Public Sub Redondeo_hacia_arriba_a_empaques_completos(necesidadLitros As Long, multiplo As Long, esperados As Long)
        Assert.Equal(esperados, Compras.EmpaquesAComprar(L(necesidadLitros), Caja16L(multiplo:=multiplo)).Empaques)
    End Sub

    <Fact>
    Public Sub Necesidad_negativa_se_rechaza()
        Assert.Throws(Of ReglaNegocioException)(Function() Compras.EmpaquesAComprar(-1, Caja16L()))
    End Sub

    <Fact>
    Public Sub Ejemplo_integrado_necesidad_neta_y_pedido()
        Dim neta = Compras.NecesidadNetaU6(L(50), L(10), L(27), L(8))
        Dim r = Compras.EmpaquesAComprar(neta, Caja16L())
        Assert.Equal(2L, r.Empaques)
        ' Stock final proyectado = 27 + 8 + 32 - 50 = 17 L
        Assert.Equal(L(17), L(27) + L(8) + r.TotalBaseU6 - L(50))
    End Sub

End Class
