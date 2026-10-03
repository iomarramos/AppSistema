Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico

Public Class CosteoTests

    <Fact>
    Public Sub Caja_de_4_x_4_L_a_128_cuesta_8_por_litro()
        Assert.Equal(8 * EscalaU6.Factor, Costeo.CostoUnitarioBaseU6(128 * EscalaU6.Factor, 4, 4 * EscalaU6.Factor))
    End Sub

    <Fact>
    Public Sub Costo_por_racion_suma_lineas_y_divide_por_rendimiento()
        ' 1 L a S/8 + 0,5 kg a S/3,40 para 10 raciones = (8 + 1,7) / 10 = 0,97
        Dim c = Costeo.CostoRacionU6({(EscalaU6.DesdeDecimal(1D), CType(EscalaU6.DesdeDecimal(8D), Long?)),
                                      (EscalaU6.DesdeDecimal(0.5D), CType(EscalaU6.DesdeDecimal(3.4D), Long?))}, EscalaU6.DesdeDecimal(10D))
        Assert.Equal(EscalaU6.DesdeDecimal(0.97D), c)
    End Sub

    <Fact>
    Public Sub T11_un_ingrediente_sin_costo_deja_el_total_pendiente()
        Assert.Null(Costeo.CostoRacionU6({(EscalaU6.DesdeDecimal(1D), CType(EscalaU6.DesdeDecimal(8D), Long?)),
                                          (EscalaU6.DesdeDecimal(1D), CType(Nothing, Long?))}, EscalaU6.DesdeDecimal(10D)))
    End Sub

    <Fact>
    Public Sub Rendimiento_cero_y_empaque_sin_contenido_se_rechazan()
        Assert.Equal("RENDIMIENTO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(
            Function() Costeo.CostoRacionU6(New (Long, Long?)() {}, 0)).Codigo)
        Assert.Equal("CONTENIDO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(
            Function() Costeo.CostoUnitarioBaseU6(100, 0, 1)).Codigo)
    End Sub

End Class
