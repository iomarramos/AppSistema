Imports Xunit
Imports AppSistema.Dominio.Catalogo
Imports AppSistema.Dominio.Numerico

' Datos ficticios. Los IDs T04.. corresponden a la matriz de pruebas de la guía (docs/guia_construccion/07).
Public Class CatalogoTests

    Private Shared Function CajaDe4x4L() As EmpaqueCompra
        Dim aceiteA4L As New VarianteProducto("ACE-A-4L", 4 * EscalaU6.Factor)
        Return New EmpaqueCompra(aceiteA4L, envasesPorEmpaque:=4)
    End Function

    <Fact>
    Public Sub T04_Caja_de_4_envases_de_4_L_contiene_16_L()
        Assert.Equal(16 * EscalaU6.Factor, CajaDe4x4L().ContenidoBaseU6)
    End Sub

    <Fact>
    Public Sub T04_Dos_cajas_equivalen_a_32_L()
        Assert.Equal(32 * EscalaU6.Factor, CajaDe4x4L().CantidadBaseU6(2))
    End Sub

    <Fact>
    Public Sub T04_Envase_de_5_L_es_otra_variante_con_su_propio_contenido()
        Dim aceiteB5L As New VarianteProducto("ACE-B-5L", 5 * EscalaU6.Factor)
        Dim dosEnvases As New EmpaqueCompra(aceiteB5L, envasesPorEmpaque:=1)
        Assert.Equal(10 * EscalaU6.Factor, dosEnvases.CantidadBaseU6(2))
        Assert.NotEqual(CajaDe4x4L().Variante.Codigo, aceiteB5L.Codigo)
    End Sub

    <Fact>
    Public Sub Contenido_por_envase_no_positivo_se_rechaza()
        Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() New VarianteProducto("X", 0))
        Assert.Equal("CONVERSION_INVALIDA", ex.Codigo)
    End Sub

    <Fact>
    Public Sub Minimo_y_multiplo_son_parametros_distintos_de_los_envases_por_caja()
        Dim e As New EmpaqueCompra(New VarianteProducto("V", EscalaU6.Factor), envasesPorEmpaque:=4, minimoEmpaques:=2, multiploEmpaques:=3)
        Assert.Equal(4L, e.EnvasesPorEmpaque)
        Assert.Equal(2L, e.MinimoEmpaques)
        Assert.Equal(3L, e.MultiploEmpaques)
    End Sub

    <Fact>
    Public Sub T05_Convertir_kg_a_litros_sin_regla_se_rechaza()
        Dim kg As New UnidadMedida("kg", Dimension.Masa, EscalaU6.Factor)
        Dim l As New UnidadMedida("L", Dimension.Volumen, EscalaU6.Factor)
        Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() ConversorUnidades.Convertir(EscalaU6.Factor, kg, l))
        Assert.Equal("CONVERSION_INVALIDA", ex.Codigo)
    End Sub

    <Fact>
    Public Sub Convertir_gramos_a_kilogramos_dentro_de_la_misma_dimension()
        Dim kg As New UnidadMedida("kg", Dimension.Masa, EscalaU6.Factor)
        Dim g As New UnidadMedida("g", Dimension.Masa, 1000L)
        Assert.Equal(1500000L, ConversorUnidades.Convertir(1500 * EscalaU6.Factor, g, kg)) ' 1500 g = 1,5 kg
    End Sub

    <Fact>
    Public Sub Cantidad_que_desborda_Int64_falla_en_vez_de_dar_un_valor_erroneo()
        Dim e As New EmpaqueCompra(New VarianteProducto("V", Long.MaxValue), envasesPorEmpaque:=2)
        Assert.Throws(Of OverflowException)(Function() e.ContenidoBaseU6)
    End Sub

    <Fact>
    Public Sub Escala_u6_redondea_mitad_alejandose_de_cero_y_no_usa_double()
        Assert.Equal(8500000L, EscalaU6.DesdeDecimal(8.5D))
        Assert.Equal(1L, EscalaU6.DesdeDecimal(0.0000005D))
        Assert.Equal(-1L, EscalaU6.DesdeDecimal(-0.0000005D))
        Assert.Equal(0.1D + 0.2D, EscalaU6.ADecimal(EscalaU6.DesdeDecimal(0.1D) + EscalaU6.DesdeDecimal(0.2D)))
        Assert.Equal(6000000L, EscalaU6.Multiplicar(EscalaU6.DesdeDecimal(2D), EscalaU6.DesdeDecimal(3D)))
    End Sub

End Class
