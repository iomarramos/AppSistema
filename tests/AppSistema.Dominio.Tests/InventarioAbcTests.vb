Imports System.Collections.Generic
Imports AppSistema.Dominio.Inventario
Imports Xunit

''' <summary>Motivos normalizados del ajuste y clasificación ABC del rotativo (sin base de datos).</summary>
Public Class InventarioAbcTests

    <Fact>
    Public Sub Motivos_tienen_los_diez_codigos_de_la_especificacion_y_un_texto()
        Assert.Equal(10, MotivosAjuste.Todos.Count)
        Assert.Equal("Error de conteo previo", MotivosAjuste.Texto(MotivosAjuste.ErrorConteo))
        Assert.Equal("Otro", MotivosAjuste.Texto(MotivosAjuste.Otro))
    End Sub

    <Fact>
    Public Sub Motivo_desconocido_se_rechaza()
        Assert.Equal("DATO_INVALIDO", Assert.Throws(Of AppSistema.Dominio.ReglaNegocioException)(Sub() MotivosAjuste.Texto("INVENTADO")).Codigo)
    End Sub

    <Fact>
    Public Sub ABC_clasifica_por_consumo_acumulado_y_sin_consumo_es_C()
        ' Consumo total 100: 50 (50 %), 30 (80 %), 15 (95 %), 5 (100 %), 0 sin consumo.
        Dim clases = ClasificacionAbc.Clasificar(New List(Of KeyValuePair(Of Long, Long)) From {
            New KeyValuePair(Of Long, Long)(1, 50), New KeyValuePair(Of Long, Long)(2, 30),
            New KeyValuePair(Of Long, Long)(3, 15), New KeyValuePair(Of Long, Long)(4, 5),
            New KeyValuePair(Of Long, Long)(5, 0)})
        Assert.Equal("A", clases(1))
        Assert.Equal("A", clases(2))
        Assert.Equal("B", clases(3))
        Assert.Equal("C", clases(4))
        Assert.Equal("C", clases(5))
    End Sub

    <Fact>
    Public Sub ABC_sin_consumo_total_deja_todo_en_C()
        Dim clases = ClasificacionAbc.Clasificar(New List(Of KeyValuePair(Of Long, Long)) From {New KeyValuePair(Of Long, Long)(1, 0)})
        Assert.Equal("C", clases(1))
    End Sub
End Class
