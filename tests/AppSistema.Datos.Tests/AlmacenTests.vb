Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Datos

''' <summary>Etapa 4: recepción de pedidos, valoración "cantidad × precio" (promedio móvil), salidas, devoluciones, bajas, traspasos y kárdex.</summary>
Public Class AlmacenTests

    Private Shared ReadOnly Fecha As New Date(2026, 10, 2)

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    Private NotInheritable Class Escenario
        Public Bd As BaseDatosPrueba
        Public Almacen As ServicioAlmacen
        Public Compras As ServicioCompras
        Public Proveedor, Pedido, Linea As Long

        ''' <summary>Pedido aprobado al proveedor P1 de 3 cajas de 4 × 4 L (48 L) a S/128 la caja.</summary>
        Public Sub New(bd As BaseDatosPrueba)
            Me.Bd = bd
            Dim s = bd.Sesion("A")
            Almacen = New ServicioAlmacen(bd.CadenaAplicacion, s)
            Compras = New ServicioCompras(bd.CadenaAplicacion, s)
            Dim prov As New ServicioProveedores(bd.CadenaAplicacion, s)
            Proveedor = prov.CrearProveedor(New ProveedorDto With {.Codigo = "P1", .Nombre = "Mayorista"})
            prov.VincularEmpaque(Proveedor, bd.EmpaqueCajaId, 2)
            Pedido = Compras.CrearPedido(bd.A.AlmacenId, Proveedor, "normal", "PEN")
            Linea = Compras.AgregarLinea(Pedido, bd.EmpaqueCajaId, 3, Fecha, U(128D))
            Compras.AprobarPedido(Pedido)
        End Sub

        Public Function Recibir(numero As String, cajas As Decimal, Optional precioCaja As Decimal? = Nothing) As Long
            Return Almacen.RecibirPedido(Pedido, "FACTURA", numero, Fecha, Fecha,
                                         {New LineaRecepcion With {.PedidoDetalleId = Linea, .EmpaquesU6 = U(cajas),
                                                                   .PrecioEmpaqueU6 = If(precioCaja.HasValue, U(precioCaja.Value), CType(Nothing, Long?))}})
        End Function

        Public Function Salida(litros As Decimal) As Long
            Return Almacen.SalidaProduccion(Bd.A.AlmacenId, Fecha, {New LineaSalida With {.VarianteId = Bd.VarianteAceiteId, .CantidadBaseU6 = U(litros)}})
        End Function

        Public Function EstadoPedido() As String
            Return Compras.ListarPedidos(Bd.A.AlmacenId).Single(Function(p) p.Id = Pedido).Estado
        End Function
    End Class

    <FactPostgres>
    Public Sub Recepcion_parcial_y_ejemplo_de_valoracion_32_L_a_8_mas_16_L_a_10_salida_6_L_vale_52()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.Recibir("F001-1", 2D)                          ' 32 L a S/8 = S/256
            Assert.Equal("parcial", e.EstadoPedido())
            Assert.Equal(U(16D), e.Compras.ListarLineas(e.Pedido).Single().PendienteU6)
            Assert.Equal(U(256D), bd.ValorU6(bd.VarianteAceiteId))

            e.Recibir("F001-2", 1D, 160D)                    ' 16 L a S/10 según el comprobante
            Assert.Equal("recibido", e.EstadoPedido())
            Assert.Equal(U(48D), bd.SaldoU6(bd.VarianteAceiteId))
            Assert.Equal(U(416D), bd.ValorU6(bd.VarianteAceiteId))

            Dim salida = e.Salida(6D)
            Assert.Equal("52.000000", bd.Escalar($"SELECT (sum(valor_u6) / 1000000.0)::numeric(18,6)::text FROM documento_stock_detalle WHERE documento_id = {salida}").ToString())
            Assert.Equal(U(42D), bd.SaldoU6(bd.VarianteAceiteId))
            Assert.Equal(U(364D), bd.ValorU6(bd.VarianteAceiteId))
            Assert.Equal(0L, bd.FilasSinConciliar())

            Dim k = e.Almacen.Kardex(bd.A.AlmacenId, bd.VarianteAceiteId, Fecha, Fecha)
            Assert.Equal(",recepcion,recepcion,salida_produccion", String.Join(",", k.Select(Function(x) x.Tipo)))
            Assert.Equal("SALDO INICIAL", k(0).Documento)
            Assert.Equal(U(8.666667D), k(2).CostoPromedioU6)
            Assert.Equal(U(-52D), k(3).ValorMovimientoU6)
            Assert.Equal(U(364D), k(3).SaldoValorU6)
        End Using
    End Sub

    <FactPostgres>
    Public Sub No_se_recibe_mas_de_lo_pendiente_ni_dos_veces_el_mismo_comprobante_ni_un_pedido_sin_aprobar()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Assert.Equal("EXCESO_RECEPCION", Assert.Throws(Of ReglaNegocioException)(Function() e.Recibir("F001-1", 4D)).Codigo)
            e.Recibir("F001-1", 1D)
            Assert.Equal("CODIGO_DUPLICADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Recibir("F001-1", 1D)).Codigo)
            Assert.Equal(U(16D), bd.SaldoU6(bd.VarianteAceiteId))   ' la repetición no duplicó
            Assert.Equal("EXCESO_RECEPCION", Assert.Throws(Of ReglaNegocioException)(Function() e.Recibir("F001-9", 2.5D)).Codigo)

            Dim borrador = e.Compras.CrearPedido(bd.A.AlmacenId, e.Proveedor, "extra", "PEN")
            Dim lin = e.Compras.AgregarLinea(borrador, bd.EmpaqueCajaId, 1, Fecha, U(128D))
            Assert.Equal("PEDIDO_NO_RECIBIBLE", Assert.Throws(Of ReglaNegocioException)(
                Function() e.Almacen.RecibirPedido(borrador, "FACTURA", "F002-1", Fecha, Fecha, {New LineaRecepcion With {.PedidoDetalleId = lin, .EmpaquesU6 = U(1D)}})).Codigo)
            Assert.Equal("STOCK_INSUFICIENTE", Assert.Throws(Of ReglaNegocioException)(Function() e.Salida(17D)).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Devolucion_al_costo_de_la_entrega_sin_exceder_lo_entregado()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.Recibir("F001-1", 2D)                          ' 32 L a S/8
            Dim salida = e.Salida(10D)                       ' S/80
            e.Recibir("F001-2", 1D, 160D)                    ' el promedio sube: 38 L por S/336
            Dim dev = e.Almacen.DevolucionProduccion(salida, Fecha, {New LineaSalida With {.VarianteId = bd.VarianteAceiteId, .CantidadBaseU6 = U(4D)}})
            Assert.Equal(U(32D), Convert.ToInt64(bd.Escalar($"SELECT sum(valor_u6) FROM documento_stock_detalle WHERE documento_id = {dev}")))   ' 4 L × S/8, no al promedio nuevo
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() e.Almacen.DevolucionProduccion(salida, Fecha, {New LineaSalida With {.VarianteId = bd.VarianteAceiteId, .CantidadBaseU6 = U(6.5D)}}))
            Assert.Equal("DEVOLUCION_EXCEDIDA", ex.Codigo)
            Assert.Equal(U(42D), bd.SaldoU6(bd.VarianteAceiteId))
            Assert.Equal(U(368D), bd.ValorU6(bd.VarianteAceiteId))
            Assert.Equal(0L, bd.FilasSinConciliar())
        End Using
    End Sub

    <FactPostgres>
    Public Sub Baja_exige_motivo_y_traspaso_mueve_cantidad_y_valor_al_otro_almacen()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.Recibir("F001-1", 3D, 144D)                    ' 48 L a S/9 = S/432
            Dim linea = {New LineaSalida With {.VarianteId = bd.VarianteAceiteId, .CantidadBaseU6 = U(2D)}}
            Assert.Equal("DATO_OBLIGATORIO", Assert.Throws(Of ReglaNegocioException)(Function() e.Almacen.Baja(bd.A.AlmacenId, Fecha, linea, " ")).Codigo)
            e.Almacen.Baja(bd.A.AlmacenId, Fecha, linea, "Envase roto")
            Assert.Equal(U(414D), bd.ValorU6(bd.VarianteAceiteId))

            Dim cocina = New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A")).CrearAlmacen(bd.A.OperacionId, "COC", "Cocina")
            Dim transito = e.Almacen.Traspasar(bd.A.AlmacenId, cocina, Fecha, {New LineaSalida With {.VarianteId = bd.VarianteAceiteId, .CantidadBaseU6 = U(10D)}})

            ' Enviado: sale del origen y todavía no está en el destino (queda en tránsito, fuera de los almacenes).
            Assert.Equal(U(36D), bd.SaldoU6(bd.VarianteAceiteId))
            Assert.Equal(U(324D), Convert.ToInt64(bd.Escalar("SELECT sum(valor_u6) FROM saldo_stock")))
            Assert.Empty(e.Almacen.ListarDocumentos(cocina, Fecha, Fecha))
            Assert.Equal(1, e.Almacen.ListarTransitos(True).Count)

            ' Recibido en el destino: entra el mismo stock y valor, y solo una vez.
            e.Almacen.Recibir(transito, Fecha)
            Assert.Equal(U(90D), Convert.ToInt64(bd.Escalar($"SELECT valor_u6 FROM saldo_stock WHERE almacen_id = {cocina}")))
            Assert.Equal(U(46D), bd.SaldoU6(bd.VarianteAceiteId))
            Assert.Equal(U(414D), Convert.ToInt64(bd.Escalar("SELECT sum(valor_u6) FROM saldo_stock")))   ' el traspaso no crea ni pierde valor
            Assert.Equal(0L, bd.FilasSinConciliar())
            Assert.Equal("recepcion,baja,traspaso_salida", String.Join(",", e.Almacen.ListarDocumentos(bd.A.AlmacenId, Fecha, Fecha).Select(Function(d) d.Tipo)))
            Assert.Equal("traspaso_entrada", e.Almacen.ListarDocumentos(cocina, Fecha, Fecha).Single().Tipo)
            Assert.Empty(e.Almacen.ListarTransitos(True))
            Assert.Equal("TRASPASO_RECIBIDO", Assert.Throws(Of ReglaNegocioException)(Sub() e.Almacen.Recibir(transito, Fecha)).Codigo)
            ' Un traspaso recibido no se borra ni se cambia, ni siquiera desde la base (trigger de V029).
            Assert.Contains("TRASPASO_TRANSITO", Assert.ThrowsAny(Of Exception)(Sub() bd.EjecutarAdmin("DELETE FROM traspaso_transito")).Message)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Traspaso_en_transito_bloquea_el_cierre_del_dia_hasta_recibirlo()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.Recibir("F001-2", 3D, 144D)
            Dim cocina = New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A")).CrearAlmacen(bd.A.OperacionId, "COC", "Cocina")
            Dim transito = e.Almacen.Traspasar(bd.A.AlmacenId, cocina, Fecha, {New LineaSalida With {.VarianteId = bd.VarianteAceiteId, .CantidadBaseU6 = U(10D)}})
            Dim cierres = New ServicioCierres(bd.CadenaAplicacion, bd.Sesion("A"))
            Assert.Contains(cierres.Pendientes(Fecha), Function(p) p.Codigo = "TRANSITO_PENDIENTE" AndAlso p.Bloqueante)
            e.Almacen.Recibir(transito, Fecha)
            Assert.DoesNotContain(cierres.Pendientes(Fecha), Function(p) p.Codigo = "TRANSITO_PENDIENTE")
        End Using
    End Sub

End Class
