Imports Xunit
Imports AppSistema.Dominio.Importacion

Public Class ImportacionInventarioTests

    <Fact>
    Public Sub Lee_stock_y_precio_y_rechaza_cero_negativos_y_repetidos()
        Dim csv = "variante_codigo;descripcion;stock_envases;precio_envase" & vbLf &
                  "PRD00010;ACEITE;42;34.12" & vbLf &
                  "PRD00020;HIERBA BUENA;7.04;6,91" & vbLf &
                  "PRD00030;X;0;1" & vbLf &
                  "PRD00010;ACEITE;1;1" & vbLf &
                  "PRD00040;Y;1;-2" & vbLf
        Dim r = LectorInventarioInicial.Leer(csv)
        Assert.Equal(2, r.Lineas.Count)
        Assert.Equal(7040000L, r.Lineas(1).StockEnvasesU6)
        Assert.Equal(6910000L, r.Lineas(1).PrecioEnvaseU6)
        Assert.Equal("4,5,6", String.Join(",", r.Errores.Select(Function(e) e.Numero)))
        Assert.Contains("ya aparece en la fila 2", r.Errores(1).Mensaje)
        Assert.True(LectorInventarioInicial.EsArchivoInventario(csv))
    End Sub

End Class
