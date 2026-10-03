Imports System.IO
Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Datos

Public Class ImportacionSgpTests

    Private Shared Function ListadoReal() As String
        Dim dir = New DirectoryInfo(AppContext.BaseDirectory)
        Do While Not File.Exists(Path.Combine(dir.FullName, "datos", "sgp", "productos_sgp_original.tsv"))
            dir = dir.Parent
        Loop
        Return File.ReadAllText(Path.Combine(dir.FullName, "datos", "sgp", "productos_sgp_original.tsv"))
    End Function

    Private Shared Function Contar(bd As BaseDatosPrueba, tabla As String, empresaId As Long) As Long
        Return Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM {tabla} WHERE empresa_id = {empresaId}"))
    End Function

    <FactPostgres>
    Public Sub Carga_el_listado_real_con_factor_y_unidad_minima_de_pedido_y_repetirlo_no_duplica()
        Using bd = BaseDatosPrueba.Crear()
            Dim imp As New ServicioImportacionCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim texto = ListadoReal()

            Dim prev = imp.VistaPreviaSgp(texto)
            Assert.False(prev.HayErrores, String.Join(" | ", prev.Filas.Where(Function(f) f.Estado = EstadoFilaImportacion.ConError).Take(5).Select(Function(f) $"{f.Numero}: {f.Detalle}")))
            Assert.Equal(2, prev.UnidadesNuevas)          ' L ya existía en A; se crean KG y UND
            Assert.Equal(4158, prev.ProductosNuevos)
            Assert.Equal(1L, Contar(bd, "producto_base", bd.A.EmpresaId))   ' la vista previa no escribe

            Dim r = imp.AplicarSgp(texto)
            Assert.True(r.Aplicado)
            Assert.Equal(4159L, Contar(bd, "producto_base", bd.A.EmpresaId))
            Assert.Equal(4159L, Contar(bd, "empaque_compra", bd.A.EmpresaId))   ' + la caja del aceite sembrado
            Assert.Equal(0L, Contar(bd, "producto_base", bd.B.EmpresaId))

            ' Arveja en bolsa de 500 g: base KG, 0.5 por bolsa; se pide de a 1 bolsa.
            Dim fila = bd.Escalar(
                "SELECT um.codigo || '|' || v.tipo_envase || '|' || v.contenido_base_por_envase_u6 || '|' || e.envases_por_empaque || '|' || e.minimo_empaques || '|' || e.multiplo_empaques " &
                "FROM producto_base p JOIN unidad_medida um ON um.id = p.unidad_base_id JOIN variante_producto v ON v.producto_base_id = p.id " &
                "JOIN empaque_compra e ON e.variante_id = v.id WHERE p.descripcion = 'ARVEJA VERDE PARTIDA CANTA CLARO BOLSA 500 GR'")
            Assert.Equal("KG|BOLSA|500000|1|1|1", fila)

            Dim otra = imp.AplicarSgp(texto)
            Assert.Equal(0, otra.ProductosNuevos + otra.VariantesNuevas + otra.EmpaquesNuevos + otra.UnidadesNuevas + otra.CategoriasNuevas)
            Assert.Equal(4159L, Contar(bd, "producto_base", bd.A.EmpresaId))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Una_linea_invalida_no_importa_nada_ni_las_unidades_y_reporta_la_linea_del_SGP()
        Using bd = BaseDatosPrueba.Crear()
            Dim imp As New ServicioImportacionCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim texto = "pro_nombre" & vbTab & "pro_coduni" & vbTab & "pro_facing" & vbLf &
                        "ARROZ EXTRA" & vbTab & "23" & vbTab & "1" & vbLf &
                        "AZUCAR" & vbTab & "8" & vbTab & "cero" & vbLf
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Sub() imp.AplicarSgp(texto))
            Assert.Equal("IMPORTACION_CON_ERRORES", ex.Codigo)
            Assert.Equal(3, imp.VistaPreviaSgp(texto).Filas.Single(Function(f) f.Estado = EstadoFilaImportacion.ConError).Numero)
            Assert.Equal(1L, Contar(bd, "unidad_medida", bd.A.EmpresaId))
            Assert.Equal(1L, Contar(bd, "producto_base", bd.A.EmpresaId))
        End Using
    End Sub

End Class
