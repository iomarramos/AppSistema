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
        End Using
    End Sub

End Class
