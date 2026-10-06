Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

''' <summary>Etapa 3: previsión desde minutas aprobadas, reserva, pendientes, redondeo, obsolescencia y pedidos.</summary>
Public Class ComprasTests

    Private Shared ReadOnly Corte As New Date(2026, 3, 31)
    Private Shared ReadOnly Desde As New Date(2026, 4, 1)
    Private Shared ReadOnly Hasta As New Date(2026, 4, 30)

    Private Shared Function Lt(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>
    ''' Aceite de la empresa A: caja de 4 × 4 L (16 L, CAJA4) y bidón suelto de 4 L (UNO). Receta de 1 ración con 0,1 L.
    ''' Proveedor P1 ofrece la caja a S/128; proveedor P2 el bidón.
    ''' </summary>
    Private NotInheritable Class Escenario
        Public Bd As BaseDatosPrueba
        Public Compras As ServicioCompras
        Public Minutas As ServicioMinutas
        Public Proveedores As ServicioProveedores
        Public P1, P2, Bidon, Receta, OperacionServicio, Estructura As Long

        Public Sub New(bd As BaseDatosPrueba)
            Me.Bd = bd
            Dim s = bd.Sesion("A")
            Compras = New ServicioCompras(bd.CadenaAplicacion, s)
            Minutas = New ServicioMinutas(bd.CadenaAplicacion, s)
            Proveedores = New ServicioProveedores(bd.CadenaAplicacion, s)
            Bidon = New ServicioCatalogo(bd.CadenaAplicacion, s).CrearEmpaque(bd.VarianteAceiteId, "UNO", "Bidon 4 L", 1)
            P1 = Proveedores.CrearProveedor(New ProveedorDto With {.Codigo = "P1", .Nombre = "Mayorista"})
            Proveedores.RegistrarPrecio(Proveedores.VincularEmpaque(P1, bd.EmpaqueCajaId, 2), New Date(2026, 1, 1), Nothing, "PEN", Lt(128D), False)
            P2 = Proveedores.CrearProveedor(New ProveedorDto With {.Codigo = "P2", .Nombre = "Minorista"})
            Proveedores.VincularEmpaque(P2, Bidon, 1)

            Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, s)
            Receta = recetas.CrearReceta("SOPA", "Sopa", Nothing, Lt(1D), Nothing)
            recetas.AgregarIngrediente(Receta, bd.ProductoAceiteId, Lt(0.1D), Nothing, 1)
            recetas.Aprobar(Receta)
            Dim servicio = Minutas.CrearServicio("ALM", "Almuerzo")
            Estructura = Minutas.CrearEstructura(servicio, "SOPA", "Sopa", 1)
            OperacionServicio = Minutas.AsignarServicio(servicio, Minutas.CrearRegimen("GEN", "General"), Nothing)
        End Sub

        ''' <summary>Minuta aprobada con la sopa para tantas raciones (0,1 L de aceite cada una).</summary>
        Public Sub MinutaAprobada(fecha As Date, raciones As Long)
            Dim m = Minutas.CrearMinuta(OperacionServicio, fecha, raciones)
            Minutas.AgregarPlato(m, Estructura, Receta, raciones)
            Minutas.Aprobar(m, "PEN")
        End Sub

        Public Sub Stock(litros As Decimal, numero As String)
            Call New ServicioStock(Bd.CadenaAplicacion, Bd.Sesion("A")).Contabilizar(
                New DocumentoStockNuevo(Bd.A.AlmacenId, TipoDocumentoStock.Recepcion, Corte, numero, {New LineaDocumentoStock(Bd.VarianteAceiteId, Lt(litros), Lt(8D))}))
        End Sub

        ''' <summary>Pedido aprobado a P2 de bidones de 4 L que llega en la fecha indicada.</summary>
        Public Function PedidoPendiente(bidones As Long, llegada As Date) As Long
            Dim p = Compras.CrearPedido(Bd.A.AlmacenId, P2, "extra", "PEN")
            Compras.AgregarLinea(p, Bidon, bidones, llegada, Lt(32D))
            Compras.AprobarPedido(p)
            Return p
        End Function

        ''' <summary>Escenario de la guía: demanda 50 L, reserva 10, stock 27 y 8 L en tránsito a tiempo.</summary>
        Public Function PrevisionDeLaGuia() As Long
            MinutaAprobada(New Date(2026, 4, 10), 300)   ' 30 L
            MinutaAprobada(New Date(2026, 4, 20), 200)   ' 20 L
            Stock(27D, "INI")
            PedidoPendiente(2, New Date(2026, 4, 5))     ' 8 L
            Compras.FijarReserva(Bd.A.AlmacenId, Bd.ProductoAceiteId, Lt(10D))
            Return Compras.CalcularPrevision(Bd.A.AlmacenId, Corte, Desde, Hasta)
        End Function
    End Class

    <FactPostgres>
    Public Sub T12_T13_necesidad_25_L_y_pedido_de_2_cajas_32_L_con_exceso_7()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim prev = e.PrevisionDeLaGuia()
            Dim linea = e.Compras.Desglose(prev).Single()
            Assert.Equal(Lt(50D), linea.DemandaU6)
            Assert.Equal(Lt(27D), linea.StockU6)
            Assert.Equal(Lt(10D), linea.ReservaU6)
            Assert.Equal(Lt(8D), linea.PendienteRecibirU6)
            Assert.Equal(Lt(25D), linea.NecesidadNetaU6)
            Assert.Equal(New Date(2026, 4, 20), linea.FechaQuiebre)

            Assert.Equal("PREVISION_NO_VALIDADA", Assert.Throws(Of ReglaNegocioException)(Function() e.Compras.GenerarPedido(prev, e.P1, New Date(2026, 4, 3), "PEN")).Codigo)
            e.Compras.Validar(prev)
            Dim r = e.Compras.GenerarPedido(prev, e.P1, New Date(2026, 4, 3), "PEN")
            Assert.Equal(1, r.Lineas)
            Assert.Empty(r.SinPrecio)
            Dim l = e.Compras.ListarLineas(r.PedidoId).Single()
            Assert.Equal(2L, l.CantidadEmpaques)
            Assert.Equal(Lt(32D), l.CantidadBaseU6)
            Assert.Equal(Lt(7D), l.ExcesoU6)
            Assert.Equal(Lt(256D), l.ImporteU6)
            Assert.Matches("^PC-\d{4}-00002$", r.Numero)   ' el 00001 es el pedido en tránsito

            ' No se pide dos veces lo mismo: otro pedido de esta previsión ya no tiene nada que pedir.
            Assert.Equal("NADA_QUE_PEDIR", Assert.Throws(Of ReglaNegocioException)(Function() e.Compras.GenerarPedido(prev, e.P1, New Date(2026, 4, 3), "PEN")).Codigo)
            ' Aprobar el pedido propio no vuelve obsoleta la previsión.
            e.Compras.AprobarPedido(r.PedidoId)
            Assert.Empty(e.Compras.Diferencias(prev))
        End Using
    End Sub

    <FactPostgres>
    Public Sub T16_transito_tardio_no_oculta_el_faltante()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.MinutaAprobada(New Date(2026, 4, 5), 500)   ' 50 L el día 5
            e.Stock(27D, "INI")
            e.PedidoPendiente(10, New Date(2026, 4, 20))  ' 40 L el día 20: tarde
            e.Compras.FijarReserva(bd.A.AlmacenId, bd.ProductoAceiteId, Lt(10D))
            Dim l = e.Compras.Desglose(e.Compras.CalcularPrevision(bd.A.AlmacenId, Corte, Desde, Hasta)).Single()
            Assert.Equal(New Date(2026, 4, 5), l.FechaQuiebre)
            Assert.Equal(Lt(23D), l.NecesidadNetaU6)   ' 50 + 10 − 27 − 40 daría 0
        End Using
    End Sub

    <FactPostgres>
    Public Sub T18_cambio_de_minuta_o_stock_vuelve_obsoleta_la_prevision_y_bloquea_aprobar()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim prev = e.PrevisionDeLaGuia()
            e.Compras.Validar(prev)
            Dim r = e.Compras.GenerarPedido(prev, e.P1, New Date(2026, 4, 3), "PEN")

            e.MinutaAprobada(New Date(2026, 4, 25), 100)   ' +10 L de demanda
            Assert.Contains("demanda 50 -> 60", String.Join("|", e.Compras.Diferencias(prev)))
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Sub() e.Compras.AprobarPedido(r.PedidoId))
            Assert.Equal("PREVISION_OBSOLETA", ex.Codigo)

            e.Stock(5D, "AJ-1")
            Assert.Contains("stock 27 -> 32", String.Join("|", e.Compras.Diferencias(prev)))
            ' Recalcular reemplaza la anterior.
            Dim nueva = e.Compras.CalcularPrevision(bd.A.AlmacenId, Corte, Desde, Hasta)
            Assert.Equal("reemplazada,borrador", String.Join(",", e.Compras.ListarPrevisiones(bd.A.AlmacenId).Where(Function(p) p.Id = prev OrElse p.Id = nueva).OrderBy(Function(p) p.Id).Select(Function(p) p.Estado)))
            Assert.Equal(Lt(60D + 10D - 32D - 8D), e.Compras.Desglose(nueva).Single().NecesidadNetaU6)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Pedido_aprobado_es_inmutable_y_las_lineas_cumplen_minimo_multiplo_y_proveedor()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            bd.EjecutarAdmin($"UPDATE empaque_compra SET multiplo_empaques = 3 WHERE id = {e.Bidon}")
            Dim p = e.Compras.CrearPedido(bd.A.AlmacenId, e.P2, "extra", "PEN")
            Assert.Equal("MULTIPLO_INCOMPATIBLE", Assert.Throws(Of ReglaNegocioException)(Function() e.Compras.AgregarLinea(p, e.Bidon, 2, Desde, 0)).Codigo)
            Assert.Equal("EMPAQUE_NO_OFRECIDO", Assert.Throws(Of ReglaNegocioException)(Function() e.Compras.AgregarLinea(p, bd.EmpaqueCajaId, 1, Desde, 0)).Codigo)
            Assert.Equal("PEDIDO_VACIO", Assert.Throws(Of ReglaNegocioException)(Sub() e.Compras.AprobarPedido(p)).Codigo)
            Dim linea = e.Compras.AgregarLinea(p, e.Bidon, 3, Desde, Lt(30D))
            Assert.Equal(Lt(12D), e.Compras.ListarLineas(p).Single().CantidadBaseU6)
            e.Compras.AprobarPedido(p)

            Assert.Equal("PEDIDO_APROBADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Compras.AgregarLinea(p, e.Bidon, 3, Desde, 0)).Codigo)
            Assert.Equal("PEDIDO_APROBADO", Assert.Throws(Of ReglaNegocioException)(Sub() e.Compras.QuitarLinea(linea)).Codigo)
            For Each sql In {$"UPDATE pedido_detalle SET cantidad_empaques = 6, cantidad_base_u6 = 24000000 WHERE id = {linea}",
                             $"UPDATE pedido_compra SET proveedor_id = {e.P1} WHERE id = {p}", $"UPDATE pedido_compra SET estado = 'borrador' WHERE id = {p}",
                             $"DELETE FROM pedido_compra WHERE id = {p}"}
                Dim ex = Assert.ThrowsAny(Of Npgsql.PostgresException)(Sub() EjecutarComoApp(bd, sql))
                Assert.Matches("PEDIDO_APROBADO|TRANSICION_INVALIDA", ex.MessageText)
            Next
            e.Compras.AnularPedido(p)
            Assert.Equal("anulado", e.Compras.ListarPedidos(bd.A.AlmacenId).Single(Function(x) x.Id = p).Estado)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Almacen_no_prepara_ni_aprueba_pedidos_y_otra_empresa_no_ve_los_pedidos()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Call New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A")).CrearUsuario("almacen", "Almacenero", "Almacen-Clave-2026", bd.A.OperacionId, "ALMACEN")
            Dim almacen As New ServicioCompras(bd.CadenaAplicacion, bd.Sesion("A", "almacen", "Almacen-Clave-2026"))
            ' V023: el almacenero no prepara pedidos (lo hace compras); sí ve los pedidos para recibirlos contra la orden.
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() almacen.CrearPedido(bd.A.AlmacenId, e.P2, "extra", "PEN")).Codigo)
            Dim compras = New ServicioCompras(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim p = compras.CrearPedido(bd.A.AlmacenId, e.P2, "extra", "PEN")
            compras.AgregarLinea(p, e.Bidon, 1, Desde, 0)
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Sub() almacen.AprobarPedido(p)).Codigo)
            Assert.Contains(almacen.ListarPedidos(bd.A.AlmacenId), Function(x) x.Id = p)

            Dim b As New ServicioCompras(bd.CadenaAplicacion, bd.Sesion("B"))
            Assert.Empty(b.ListarPedidos(bd.A.AlmacenId))
            Assert.Equal("OPERACION_AJENA", Assert.Throws(Of ReglaNegocioException)(Function() b.CalcularPrevision(bd.A.AlmacenId, Corte, Desde, Hasta)).Codigo)
        End Using
    End Sub

    Private Shared Sub EjecutarComoApp(bd As BaseDatosPrueba, sql As String)
        Using cn As New Npgsql.NpgsqlConnection(bd.CadenaAplicacion)
            cn.Open()
            Using cmd As New Npgsql.NpgsqlCommand($"SELECT set_config('app.empresa_id', '{bd.A.EmpresaId}', false); {sql}", cn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Class
