Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

''' <summary>
''' Planificado (teórico) vs realizado: factores por operación, comensales que recalculan raciones, venta real cargada,
''' raciones preparadas/consumidas por componente, costo teórico vs real y productos (incluidos los no planificados).
''' </summary>
Public Class TeoricoRealTests

    Private Shared ReadOnly Fecha As New Date(2026, 10, 5)

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>Desayuno de 500 (como en VentaEstructuraDatosTests): costo previsto S/ 1 510, venta S/ 3 145,833333 al 48 %.</summary>
    Private NotInheritable Class Escenario
        Public Bd As BaseDatosPrueba
        Public Minutas As ServicioMinutas
        Public Comparativo As ServicioComparativo
        Public Recetas As ServicioRecetas
        Public OperacionServicio, Minuta, Bebida, Jugo, Complemento1, Complemento2 As Long
        Public Cafe, Papaya, Pina, Jamon, Palta As Long

        Public Sub New(bd As BaseDatosPrueba)
            Me.Bd = bd
            Dim s = bd.Sesion("A")
            Minutas = New ServicioMinutas(bd.CadenaAplicacion, s)
            Comparativo = New ServicioComparativo(bd.CadenaAplicacion, s)
            Recetas = New ServicioRecetas(bd.CadenaAplicacion, s)
            Dim prov As New ServicioProveedores(bd.CadenaAplicacion, s)
            prov.RegistrarPrecio(prov.VincularEmpaque(prov.CrearProveedor(New ProveedorDto With {.Codigo = "P1", .Nombre = "Distribuidora"}), bd.EmpaqueCajaId, 2),
                                 New Date(2026, 1, 1), Nothing, "PEN", U(128D), False)
            Call New ServicioCatalogo(bd.CadenaAplicacion, s).ActivarEnOperacion(bd.VarianteAceiteId)   ' D02: producto activo en la operación
            Dim desayuno = Minutas.CrearServicio("DES", "Desayuno")
            Bebida = Minutas.CrearEstructura(desayuno, "BEB", "Bebida caliente", 1)
            Jugo = Minutas.CrearEstructura(desayuno, "JUG", "Jugo", 2)
            Complemento1 = Minutas.CrearEstructura(desayuno, "CO1", "Complemento 1", 3, factorConsumoBp:=7000)
            Complemento2 = Minutas.CrearEstructura(desayuno, "CO2", "Complemento 2", 4, factorConsumoBp:=3000)
            OperacionServicio = Minutas.AsignarServicio(desayuno, Minutas.CrearRegimen("GEN", "General"), Nothing)
            Cafe = Receta("CAFE", 0.05D) : Papaya = Receta("JUGO-PAPAYA", 0.1D) : Pina = Receta("JUGO-PINA", 0.125D)
            Jamon = Receta("PAN-JAMON", 0.2D) : Palta = Receta("PAN-PALTA", 0.25D)
            Minuta = Planificar(Fecha, 500)
        End Sub

        Public Function Planificar(fecha As Date, comensales As Long) As Long
            Dim m = Minutas.CrearMinuta(OperacionServicio, fecha, comensales)
            Minutas.AgregarPlatoPorFactor(m, Bebida, Cafe)
            Minutas.AgregarPlatoPorFactor(m, Jugo, Papaya, repartoBp:=5000)
            Minutas.AgregarPlatoPorFactor(m, Jugo, Pina, repartoBp:=5000)
            Minutas.AgregarPlatoPorFactor(m, Complemento1, Jamon)
            Minutas.AgregarPlatoPorFactor(m, Complemento2, Palta)
            Return m
        End Function

        Private Function Receta(codigo As String, litrosPorRacion As Decimal) As Long
            Dim v = Recetas.CrearReceta(codigo, codigo, Nothing, U(1D), Nothing)
            Recetas.AgregarIngrediente(v, Bd.ProductoAceiteId, U(litrosPorRacion), Nothing, 1)
            Recetas.Aprobar(v)
            Return v
        End Function

        Public Function Plato(minuta As Long, receta As Long) As Long
            Return Minutas.ListarPlatos(minuta).Single(Function(p) p.RecetaVersionId = receta).Id
        End Function
    End Class

    <FactPostgres>
    Public Sub Comensales_recalculan_raciones_y_la_operacion_ajusta_sus_factores()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.Minutas.ActualizarComensales(e.Minuta, 400)                         ' la estructura se recalcula con el total
            Assert.Equal({400L, 200L, 200L, 280L, 120L}, e.Minutas.ListarPlatos(e.Minuta).Select(Function(p) p.Raciones))

            ' La operación ajusta su factor de complemento 1 (teórico 70 %) a 60 %: aplica a lo que se planifique después.
            e.Minutas.FijarFactorOperacion(e.OperacionServicio, e.Complemento1, 6000)
            Dim f = e.Minutas.FactoresDeOperacion(e.OperacionServicio).Single(Function(x) x.EstructuraId = e.Complemento1)
            Assert.Equal(7000L, f.FactorTeoricoBp)
            Assert.Equal(6000L, f.FactorVigenteBp)
            Assert.Equal(280L, e.Minutas.ListarPlatos(e.Minuta).Single(Function(p) p.EstructuraId = e.Complemento1).Raciones)   ' lo ya planificado no cambia solo
            Dim otra = e.Planificar(Fecha.AddDays(1), 500)
            Assert.Equal(300L, e.Minutas.ListarPlatos(otra).Single(Function(p) p.EstructuraId = e.Complemento1).Raciones)
            e.Minutas.FijarFactorOperacion(e.OperacionServicio, e.Complemento1, Nothing)                                       ' vuelve al teórico
            Assert.Equal(7000L, e.Minutas.FactoresDeOperacion(e.OperacionServicio).Single(Function(x) x.EstructuraId = e.Complemento1).FactorVigenteBp)
            ' Otra empresa no ve ni cambia esos factores.
            Assert.Equal("OPERACION_AJENA", Assert.Throws(Of ReglaNegocioException)(
                Sub() Call New ServicioMinutas(bd.CadenaAplicacion, bd.Sesion("B")).FijarFactorOperacion(e.OperacionServicio, e.Complemento1, 1)).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Teorico_vs_real_en_raciones_venta_costo_y_productos()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Assert.Equal("MINUTA_NO_APROBADA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Comparativo.RegistrarVenta(e.Minuta, 1, Nothing, Nothing)).Codigo)
            e.Minutas.Aprobar(e.Minuta, "PEN")
            Dim s = bd.Sesion("A")

            ' Lo que salió del almacén para el servicio: 200 L de aceite (S/ 1 600) y 10 kg de azúcar no planificados (S/ 30).
            Dim cat As New ServicioCatalogo(bd.CadenaAplicacion, s)
            Dim azucar = cat.CrearProducto("AZU", "Azucar", Nothing, bd.UnidadLitroId, Nothing)
            Dim bolsa = cat.CrearVariante(azucar, Nothing, "AZU-1", "Azucar bolsa", "bolsa", U(1D))
            Call New ServicioStock(bd.CadenaAplicacion, s).Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Apertura, New Date(2026, 10, 1), "AP-1",
                {New LineaDocumentoStock(bd.VarianteAceiteId, U(600D), U(8D)), New LineaDocumentoStock(bolsa, U(50D), U(3D))}))
            Dim almacen As New ServicioAlmacen(bd.CadenaAplicacion, s)
            almacen.SalidaProduccion(bd.A.AlmacenId, Fecha, {New LineaSalida With {.VarianteId = bd.VarianteAceiteId, .CantidadBaseU6 = U(200D)},
                                                             New LineaSalida With {.VarianteId = bolsa, .CantidadBaseU6 = U(10D)}}, Nothing, e.OperacionServicio)
            ' Real del servicio.
            Call New ServicioProduccion(bd.CadenaAplicacion, s).RegistrarProduccion(e.Minuta, 480, 470, 10, Nothing)
            e.Comparativo.RegistrarConsumo(e.Plato(e.Minuta, e.Cafe), 480, 470)
            e.Comparativo.RegistrarConsumo(e.Plato(e.Minuta, e.Papaya), 240, 230)
            e.Comparativo.RegistrarConsumo(e.Plato(e.Minuta, e.Pina), 240, 235)
            e.Comparativo.RegistrarConsumo(e.Plato(e.Minuta, e.Jamon), 350, 300)
            e.Comparativo.RegistrarConsumo(e.Plato(e.Minuta, e.Palta), 150, 100)
            Assert.Equal("RACIONES_INCOHERENTES", Assert.Throws(Of ReglaNegocioException)(Sub() e.Comparativo.RegistrarConsumo(e.Plato(e.Minuta, e.Palta), 10, 11)).Codigo)
            e.Comparativo.RegistrarVenta(e.Minuta, 470, Nothing, Nothing)        ' 470 raciones al precio por comensal

            Dim c = e.Comparativo.Comparativo(e.Minuta)
            Assert.Equal(500L, c.ComensalesPlan)
            Assert.Equal(480L, c.RacionesPreparadas)
            Assert.Equal(470L, c.RacionesConsumidas)
            Assert.Equal(470L, c.RacionesVendidas)
            Assert.Equal(30L, c.RacionesNoVendidas)
            Assert.Equal(U(3145.833333D), c.VentaTeoricaU6)
            Assert.Equal(U(2957.083333D), c.VentaRealU6)                           ' 3 145,833333 × 470 / 500
            Assert.Equal(U(-188.75D), c.DiferenciaVentaU6)
            Assert.Equal(U(1510D), c.CostoTeoricoU6)
            Assert.Equal(U(1287D), c.CostoTeoricoConsumidoU6)                      ' 0,40×470 + 0,80×230 + 1,00×235 + 1,60×300 + 2,00×100
            Assert.Equal(U(1630D), c.CostoRealU6)
            Assert.Equal(U(120D), c.DiferenciaCostoU6)
            Assert.Equal(U(48D), c.FoodCostTeoricoU6)
            Assert.Equal(EscalaU6.MultiplicarDividir(U(1630D), 100L * EscalaU6.Factor, U(2957.083333D)), c.FoodCostRealU6)

            Dim jamon = c.Componentes.Single(Function(x) x.Receta = "PAN-JAMON")
            Assert.Equal(7000L, jamon.FactorPlanBp)
            Assert.Equal(350L, jamon.RacionesPlan)
            Assert.Equal(300L, jamon.RacionesConsumidas)
            Assert.Equal(6383L, jamon.FactorRealBp)                                 ' 300 de 470 comensales reales

            Dim aceite = c.Productos.Single(Function(x) x.Producto = "Aceite vegetal")
            Assert.Equal("planificado", aceite.Estado)
            Assert.Equal(U(188.75D), aceite.CantidadTeoricaU6)
            Assert.Equal(U(1510D), aceite.CostoTeoricoU6)
            Assert.Equal(U(200D), aceite.CantidadRealU6)
            Assert.Equal(U(11.25D), aceite.DiferenciaCantidadU6)
            Assert.Equal(U(90D), aceite.DiferenciaCostoU6)
            Dim az = c.Productos.Single(Function(x) x.Producto = "Azucar")
            Assert.Equal("no planificado", az.Estado)                               ' salió para el servicio sin estar en la minuta
            Assert.Equal(0L, az.CantidadTeoricaU6)
            Assert.Equal(U(30D), az.CostoRealU6)

            ' El mes da lo mismo con una sola minuta, y la venta del mes usa la venta real cargada.
            Dim mes = e.Comparativo.ComparativoMes(e.OperacionServicio, 2026, 10)
            Assert.Equal(c.CostoRealU6, mes.CostoRealU6)
            Assert.Equal(c.VentaRealU6, mes.VentaRealU6)
            Assert.Equal(5, mes.Componentes.Count)
            Dim cierres As New ServicioCierres(bd.CadenaAplicacion, s)
            Assert.Equal(U(2957.083333D), cierres.GenerarVentaDesdeMinutas(2026, 10).Single().ImporteU6)

            ' Factores reales del período para que la operación los actualice.
            Dim fr = e.Comparativo.FactoresReales(e.OperacionServicio, New Date(2026, 10, 1), New Date(2026, 10, 31))
            Assert.Equal(6383L, fr.Single(Function(x) x.EstructuraId = e.Complemento1).FactorRealBp)
            Assert.Equal(9894L, fr.Single(Function(x) x.EstructuraId = e.Jugo).FactorRealBp)       ' (230 + 235) / 470
            Assert.Equal(470L, fr.Single(Function(x) x.EstructuraId = e.Jugo).ComensalesReales)    ' comensales una sola vez

            ' Día cerrado: lo real ya no cambia.
            Assert.True(cierres.CerrarDia(Fecha).Cerrado)
            Assert.Equal("DIA_CERRADO", Assert.Throws(Of ReglaNegocioException)(Sub() e.Comparativo.RegistrarVenta(e.Minuta, 400, Nothing, Nothing)).Codigo)
            Assert.Equal("DIA_CERRADO", Assert.Throws(Of ReglaNegocioException)(Sub() e.Comparativo.RegistrarConsumo(e.Plato(e.Minuta, e.Cafe), 480, 480)).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Planilla_cambia_raciones_solo_en_minuta_en_borrador()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim plato = e.Plato(e.Minuta, e.Cafe)
            Assert.Equal("CANTIDAD_INVALIDA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.FijarRaciones(plato, 0)).Codigo)
            e.Minutas.FijarRaciones(plato, 450)
            Assert.Equal(450L, e.Minutas.ListarPlatos(e.Minuta).Single(Function(p) p.Id = plato).Raciones)
            e.Minutas.Aprobar(e.Minuta, "PEN")
            Assert.Equal("MINUTA_APROBADA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.FijarRaciones(plato, 400)).Codigo)
        End Using
    End Sub

End Class
