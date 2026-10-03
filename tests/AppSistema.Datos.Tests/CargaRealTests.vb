Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Datos

''' <summary>Carga del juego de datos real: precios por presentación, agua sin costo, estructuras y ciclo de minutas, repetibles.</summary>
Public Class CargaRealTests

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    <FactPostgres>
    Public Sub Precios_estructuras_y_ciclo_se_cargan_una_vez_y_el_agua_no_deja_el_costo_pendiente()
        Using bd = BaseDatosPrueba.Crear()
            Dim s = bd.Sesion("A")
            Dim carga As New ServicioCargaReal(bd.CadenaAplicacion, s)
            ' Precio de la caja de aceite (4 × 4 L): S/ 128 la caja → S/ 32 por bidón de 4 L.
            Dim precios = "variante_codigo;descripcion_comercial;producto_codigo;producto_descripcion;unidad_base;contenido_por_envase;empaque_codigo;precio_envase;fecha_precio;fuente" & vbLf &
                          "ACE-A-4L;Aceite A 4 L;ACE;Aceite vegetal;L;4;CAJA4;32;2026-01-01;inventario" & vbLf &
                          "NOEXISTE;x;x;x;L;1;X;1;2026-01-01;x" & vbLf
            Dim r = carga.ImportarPrecios(precios)
            Assert.Equal(1, r.Nuevos)
            Assert.Single(r.Problemas)
            Assert.Equal(1, carga.ImportarPrecios(precios).YaEstaban)                   ' repetir no duplica ni pisa
            Assert.Equal(U(128D), Convert.ToInt64(bd.Escalar("SELECT precio_empaque_u6 FROM precio_compra")))   ' 32 × 4 envases por caja
            ' D02: el bidón es el producto activo de la operación; repetir no cambia lo ya liberado.
            Dim activos = "variante_codigo;descripcion_comercial;producto_codigo;motivo" & vbLf & "ACE-A-4L;Aceite A 4 L;ACE;ultima compra SGP 2026-01-01" & vbLf & "NOEXISTE;x;x;x" & vbLf
            Dim la = carga.LiberarProductos(activos)
            Assert.Equal(1, la.Nuevos)
            Assert.Single(la.Problemas)
            Assert.Equal(1, carga.LiberarProductos(activos).YaEstaban)

            ' Familias del SGP: jerarquía familia › subfamilia › grupo; el ingrediente sin categoría toma la familia.
            Dim familias = "variante_codigo;descripcion_comercial;familia;subfamilia;grupo" & vbLf &
                           "ACE-A-4L;Aceite A 4 L;ABARROTES;ABARROTES 1ERA NECESIDAD;ACEITE" & vbLf & "NOEXISTE;x;LIMPIEZA;;" & vbLf
            Dim fa = carga.CargarFamilias(familias)
            Assert.Equal(1, fa.Nuevos)
            Assert.Single(fa.Problemas)
            Assert.Equal(1, carga.CargarFamilias(familias).YaEstaban)
            Dim catalogo As New ServicioCatalogo(bd.CadenaAplicacion, s)
            Assert.Equal("ABARROTES › ABARROTES 1ERA NECESIDAD › ACEITE", catalogo.ListarVariantes(bd.ProductoAceiteId).Single(Function(x) x.Id = bd.VarianteAceiteId).Familia)
            Assert.Equal("ABARROTES", bd.Escalar($"SELECT c.codigo FROM producto_base p JOIN categoria_producto c ON c.id = p.categoria_id WHERE p.id = {bd.ProductoAceiteId}").ToString())
            Assert.Equal("ABARROTES 1ERA NECESIDAD", bd.Escalar("SELECT p.nombre FROM categoria_producto c JOIN categoria_producto p ON p.id = c.padre_id WHERE c.nombre = 'ACEITE'").ToString())

            ' Receta con agua: sin marcarla, el costo queda pendiente; marcada sin costo, se costea en S/ 0.
            Dim cat As New ServicioCatalogo(bd.CadenaAplicacion, s)
            Dim agua = cat.CrearProducto("AGU", "AGUA PARA RECETA", Nothing, bd.UnidadLitroId, Nothing)
            Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, s)
            Dim v = recetas.CrearReceta("F00001", "CALDO", Nothing, U(1D), Nothing)
            recetas.AgregarIngrediente(v, bd.ProductoAceiteId, U(0.01D), Nothing, 1)
            recetas.AgregarIngrediente(v, agua, U(0.3D), Nothing, 2)
            recetas.Aprobar(v)

            Dim estructuras = "servicio;orden;codigo;componente;nombre;factor_consumo_pct;alternativas_reparto_pct" & vbLf &
                              "ALMUERZO;1;SOP;SOPA;Sopa;100;100" & vbLf
            Assert.Equal(1, carga.CargarEstructuras(estructuras).Nuevos)
            Assert.Equal(1, carga.CargarEstructuras(estructuras).YaEstaban)
            Dim ciclo = "dia;servicio;orden;estructura_codigo;estructura;receta_codigo;receta_nombre;reparto_pct;gramaje_g_racion;costo_estimado_racion;costo_completo" & vbLf &
                        "1;ALMUERZO;1;SOP;Sopa;F00001;CALDO;100;0;0.08;SI" & vbLf &
                        "2;ALMUERZO;1;SOP;Sopa;F00001;CALDO;100;0;0.08;SI" & vbLf
            Dim comensales As New Dictionary(Of String, Long) From {{"ALMUERZO", 200}}
            Assert.Equal(1, carga.CargarCiclo(ciclo, New Date(2026, 10, 5), comensales, aprobar:=True, dias:=1).Nuevos)
            Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, s)
            Assert.Null(minutas.ListarMinutas(New Date(2026, 10, 5), New Date(2026, 10, 5)).Single().VentaPrevistaU6)   ' agua sin precio: pendiente

            Assert.Equal(1, carga.MarcarInsumosSinCosto("producto;motivo" & vbLf & "AGUA PARA RECETA;agua de red" & vbLf).Nuevos)
            Dim r2 = carga.CargarCiclo(ciclo, New Date(2026, 10, 5), comensales, aprobar:=True)
            Assert.Equal(1, r2.Nuevos)
            Assert.Equal(1, r2.YaEstaban)                                                 ' el día 1 ya existía y no se toca
            Dim m = minutas.ListarMinutas(New Date(2026, 10, 6), New Date(2026, 10, 6)).Single()
            Assert.Equal(U(16D), m.CostoPrevistoU6)                                       ' 200 × 0,01 L × S/ 8 + agua S/ 0
            Assert.Equal(U(33.333333D), m.VentaPrevistaU6)                                 ' 16 / 0,48
            Assert.Equal(200L, minutas.ListarPlatos(m.Id).Single().Raciones)

            Dim estado = carga.Estado()
            Assert.Equal("1 1 1 2 2", String.Join(" ", estado.PreciosSgp, estado.InsumosSinCosto, estado.ServiciosAsignados, estado.Minutas, estado.MinutasAprobadas))
            Assert.Equal(0L, estado.AlmacenesConApertura)

            ' Un archivo equivocado (por ejemplo, el de estructuras en el paso del ciclo) se rechaza sin cargar nada.
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() carga.CargarCiclo(estructuras, New Date(2026, 11, 1), comensales, aprobar:=False))
            Assert.Equal("ARCHIVO_INVALIDO", ex.Codigo)
            Assert.Contains("receta_codigo", ex.Message)
            Assert.Equal("ARCHIVO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(Function() carga.ImportarPrecios(ciclo)).Codigo)
            Assert.Equal("ARCHIVO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(Function() carga.CargarEstructuras("")).Codigo)
            Assert.Empty(minutas.ListarMinutas(New Date(2026, 11, 1), New Date(2026, 11, 30)))
        End Using
    End Sub

End Class
