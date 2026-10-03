Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

''' <summary>
''' Venta desde la estructura (D13 del usuario): desayuno para 500 comensales con factores de consumo y reparto de
''' alternativas; venta = costo previsto / 48 %; Food Cost real = consumo real / venta.
''' </summary>
Public Class VentaEstructuraDatosTests

    Private Shared ReadOnly Fecha As New Date(2026, 10, 5)

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>
    ''' Todo a base de aceite a S/ 8 por litro (único producto con precio en la base de prueba):
    ''' bebida caliente 0,05 L (S/ 0,40) al 100 %; jugo al 100 % repartido 50/50 entre 0,10 L (S/ 0,80) y 0,125 L (S/ 1,00);
    ''' complemento 1 al 70 % con 0,20 L (S/ 1,60); complemento 2 al 30 % con 0,25 L (S/ 2,00).
    ''' Costo: 200 + 200 + 250 + 560 + 300 = S/ 1 510 → venta S/ 1 510 / 0,48 = S/ 3 145,833333.
    ''' </summary>
    Private NotInheritable Class Escenario
        Public Bd As BaseDatosPrueba
        Public Minutas As ServicioMinutas
        Public Cierres As ServicioCierres
        Public Recetas As ServicioRecetas
        Public OperacionServicio, Minuta, Bebida, Jugo, Complemento1, Complemento2 As Long

        Public Sub New(bd As BaseDatosPrueba)
            Me.Bd = bd
            Dim s = bd.Sesion("A")
            Minutas = New ServicioMinutas(bd.CadenaAplicacion, s)
            Cierres = New ServicioCierres(bd.CadenaAplicacion, s)
            Recetas = New ServicioRecetas(bd.CadenaAplicacion, s)
            Dim prov As New ServicioProveedores(bd.CadenaAplicacion, s)
            prov.RegistrarPrecio(prov.VincularEmpaque(prov.CrearProveedor(New ProveedorDto With {.Codigo = "P1", .Nombre = "Distribuidora"}), bd.EmpaqueCajaId, 2),
                                 New Date(2026, 1, 1), Nothing, "PEN", U(128D), False)          ' caja 4 × 4 L = 16 L → S/ 8 por L
            Dim desayuno = Minutas.CrearServicio("DES", "Desayuno")
            Bebida = Minutas.CrearEstructura(desayuno, "BEB", "Bebida caliente", 1)
            Jugo = Minutas.CrearEstructura(desayuno, "JUG", "Jugo", 2)
            Complemento1 = Minutas.CrearEstructura(desayuno, "CO1", "Complemento 1", 3, factorConsumoBp:=7000)
            Complemento2 = Minutas.CrearEstructura(desayuno, "CO2", "Complemento 2", 4, factorConsumoBp:=3000)
            OperacionServicio = Minutas.AsignarServicio(desayuno, Minutas.CrearRegimen("GEN", "General"), Nothing)
            Minuta = Minutas.CrearMinuta(OperacionServicio, Fecha, 500)
            Minutas.AgregarPlatoPorFactor(Minuta, Bebida, Receta("CAFE", 0.05D))
            Minutas.AgregarPlatoPorFactor(Minuta, Jugo, Receta("JUGO-PAPAYA", 0.1D), repartoBp:=5000)
            Minutas.AgregarPlatoPorFactor(Minuta, Jugo, Receta("JUGO-PINA", 0.125D), repartoBp:=5000)
            Minutas.AgregarPlatoPorFactor(Minuta, Complemento1, Receta("PAN-JAMON", 0.2D))
            Minutas.AgregarPlatoPorFactor(Minuta, Complemento2, Receta("PAN-PALTA", 0.25D))
        End Sub

        Public Function Receta(codigo As String, litrosPorRacion As Decimal) As Long
            Dim v = Recetas.CrearReceta(codigo, codigo, Nothing, U(1D), Nothing)
            Recetas.AgregarIngrediente(v, Bd.ProductoAceiteId, U(litrosPorRacion), Nothing, 1)
            Recetas.Aprobar(v)
            Return v
        End Function
    End Class

    <FactPostgres>
    Public Sub Desayuno_de_500_con_factores_da_venta_al_48_por_ciento_y_food_cost_real_contra_esa_venta()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim raciones = e.Minutas.ListarPlatos(e.Minuta).Select(Function(p) p.Raciones).ToList()
            Assert.Equal({500L, 250L, 250L, 350L, 150L}, raciones)
            Assert.Equal(7000L, e.Minutas.ListarEstructuras(e.Minutas.ListarServicios().Single().Id).Single(Function(x) x.Codigo = "CO1").FactorConsumoBp)

            e.Minutas.Aprobar(e.Minuta, "PEN")
            Dim m = e.Minutas.ListarMinutas(Fecha, Fecha).Single()
            Assert.Equal(U(1510D), m.CostoPrevistoU6)
            Assert.Equal(4800L, m.FoodCostObjetivoBp)                                        ' 48 % por defecto
            Assert.Equal(U(3145.833333D), m.VentaPrevistaU6)
            Assert.Equal(U(6.291667D), m.PrecioVentaComensalU6)
            Assert.Equal(U(3.02D), m.CostoComensalU6)
            Dim ex = Assert.ThrowsAny(Of Npgsql.PostgresException)(Sub() bd.EjecutarAdmin($"UPDATE minuta SET venta_prevista_u6 = 1 WHERE id = {e.Minuta}"))
            Assert.Contains("MINUTA_APROBADA", ex.MessageText)

            ' La venta del mes sale de la estructura; repetir no duplica.
            Dim g = e.Cierres.GenerarVentaDesdeMinutas(2026, 10).Single()
            Assert.Equal("generado", g.Estado)
            Assert.Equal(U(3145.833333D), g.ImporteU6)
            Assert.StartsWith("Estructura: 1 minutas (0 con venta real cargada, el resto con venta teorica), Food Cost objetivo 48.00 %", g.Detalle)
            e.Cierres.GenerarVentaDesdeMinutas(2026, 10)
            Assert.Equal(1L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM ingreso_servicio WHERE origen = 'estructura'")))

            ' Consumo real: 200 L (S/ 1 600) contra la venta → Food Cost real 50,86 %, 2,86 puntos sobre el 48 %.
            Dim stock As New ServicioStock(bd.CadenaAplicacion, bd.Sesion("A"))
            stock.Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Apertura, New Date(2026, 10, 1), "AP-1",
                {New LineaDocumentoStock(bd.VarianteAceiteId, U(600D), U(8D))}))
            stock.Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.SalidaProduccion, Fecha, "SP-1",
                {New LineaDocumentoStock(bd.VarianteAceiteId, U(200D), 0)}) With {.OperacionServicioId = e.OperacionServicio})
            Dim r = e.Cierres.ReporteMensual(2026, 10).Servicios.Single()
            Assert.Equal(U(1600D), r.CostoAlimentosU6)
            Assert.Equal(U(3145.833333D), r.IngresoU6)
            Dim esperado = EscalaU6.MultiplicarDividir(U(1600D), 100L * EscalaU6.Factor, U(3145.833333D))
            Assert.Equal(esperado, r.FoodCostU6)
            Assert.Equal(U(48D), r.ObjetivoU6)
            Assert.Equal(esperado - U(48D), r.DesviacionPuntosU6)
            Assert.InRange(r.FoodCostU6.Value, U(50.86D), U(50.87D))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Objetivo_propio_costo_pendiente_e_ingreso_manual()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.Minutas.FijarFoodCostObjetivo(e.OperacionServicio, 5000)                    ' este servicio trabaja al 50 %
            e.Minutas.Aprobar(e.Minuta, "PEN")
            Assert.Equal(U(3020D), e.Minutas.ListarMinutas(Fecha, Fecha).Single().VentaPrevistaU6)   ' 1 510 / 0,50

            ' Una minuta con un producto sin precio no inventa venta y se informa.
            Dim cat As New ServicioCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim sinPrecio = cat.CrearProducto("MER", "Mermelada", Nothing, bd.UnidadLitroId, Nothing)
            Dim v = e.Recetas.CrearReceta("MERM", "Mermelada", Nothing, U(1D), Nothing)
            e.Recetas.AgregarIngrediente(v, sinPrecio, U(0.02D), Nothing, 1)
            e.Recetas.Aprobar(v)
            Dim otra = e.Minutas.CrearMinuta(e.OperacionServicio, Fecha.AddDays(1), 500)
            e.Minutas.AgregarPlatoPorFactor(otra, e.Bebida, v)
            e.Minutas.Aprobar(otra, "PEN")
            Assert.Null(e.Minutas.ListarMinutas(Fecha.AddDays(1), Fecha.AddDays(1)).Single().VentaPrevistaU6)
            Dim g = e.Cierres.GenerarVentaDesdeMinutas(2026, 10).Single()
            Assert.Equal("generado con pendientes", g.Estado)
            Assert.Equal(U(3020D), g.ImporteU6)
            Assert.Contains("1 minutas sin venta por costo pendiente", g.Detalle)

            ' Un ingreso registrado a mano tiene prioridad.
            e.Cierres.RegistrarIngreso(e.OperacionServicio, 2026, 10, U(9999D), 0, "Liquidacion")
            Assert.Equal("manual conservado", e.Cierres.GenerarVentaDesdeMinutas(2026, 10).Single().Estado)
            Assert.Equal(U(9999D), e.Cierres.ReporteMensual(2026, 10).Servicios.Single().IngresoU6)
            Assert.Equal("DATO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.FijarFoodCostObjetivo(e.OperacionServicio, 0)).Codigo)
        End Using
    End Sub

End Class
