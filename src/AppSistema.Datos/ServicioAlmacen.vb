Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad
Imports AppSistema.Dominio.Stock

Public NotInheritable Class LineaPendienteDto
    Public Property PedidoDetalleId As Long
    Public Property EmpaqueId As Long
    Public Property VarianteId As Long
    Public Property ProductoDescripcion As String
    Public Property EmpaqueDescripcion As String
    Public Property Unidad As String
    Public Property FactorBasePorEmpaqueU6 As Long
    Public Property PedidoEmpaques As Long
    Public Property PendienteBaseU6 As Long
    Public Property PrecioEmpaqueU6 As Long
End Class

''' <summary>Cantidad recibida de una línea del pedido, en empaques (admite fracción, p. ej. 7,5 kg de un saco por kg).</summary>
Public NotInheritable Class LineaRecepcion
    Public Property PedidoDetalleId As Long
    Public Property EmpaquesU6 As Long
    ''' <summary>Precio por empaque según comprobante; Nothing = el del pedido.</summary>
    Public Property PrecioEmpaqueU6 As Long?
End Class

Public NotInheritable Class LineaSalida
    Public Property VarianteId As Long
    Public Property CantidadBaseU6 As Long
End Class

Public NotInheritable Class DocumentoStockDto
    Public Property Id As Long
    Public Property Numero As String
    Public Property Tipo As String
    Public Property Fecha As Date
    Public Property Motivo As String
    Public Property ValorU6 As Long
End Class

Public NotInheritable Class KardexDto
    Public Property Fecha As Date
    Public Property Secuencia As Long
    Public Property Documento As String
    Public Property Tipo As String
    Public Property EntradaU6 As Long?
    Public Property SalidaU6 As Long?
    Public Property ValorMovimientoU6 As Long
    Public Property SaldoCantidadU6 As Long
    Public Property SaldoValorU6 As Long
    Public Property CostoPromedioU6 As Long
End Class

''' <summary>
''' Movimientos de almacén de la operación de la sesión (módulo 3). Todos pasan por ServicioStock (una transacción,
''' saldo solo por trigger). Valoración D01: entradas a su costo; salidas a cantidad × costo vigente del saldo.
''' </summary>
Public NotInheritable Class ServicioAlmacen
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    Private Function Stock() As ServicioStock
        Return New ServicioStock(CadenaConexion, Sesion)
    End Function

    ' ---------- Recepción de pedidos (R09) ----------

    Public Function PendientesDePedido(pedidoId As Long) As List(Of LineaPendienteDto)
        Return EnTransaccion(Permisos.ComprasVer, Function(u) LeerPendientes(u, pedidoId))
    End Function

    ''' <summary>
    ''' Recibe (total o parcialmente) un pedido aprobado: recepción confirmada + documento de recepción en una
    ''' transacción. Cantidad base = empaques × factor del pedido (conversión histórica). Costo = empaques × precio.
    ''' No se recibe más de lo pendiente (D11: por defecto se bloquea, EXCESO_RECEPCION). El comprobante del
    ''' proveedor se registra una sola vez. El pedido pasa a parcial o recibido.
    ''' </summary>
    Public Function RecibirPedido(pedidoId As Long, tipoDocumento As String, numeroDocumento As String, fechaDocumento As Date,
                                  fechaRecepcion As Date, lineas As IEnumerable(Of LineaRecepcion), Optional observacion As String = Nothing) As Long
        Dim tipoDoc = ServicioAdministracion.Requerido(tipoDocumento, "tipo de comprobante")
        Dim numDoc = ServicioAdministracion.Requerido(numeroDocumento, "numero de comprobante")
        Dim recibidas = lineas.Where(Function(l) l.EmpaquesU6 <> 0).ToList()
        If recibidas.Count = 0 Then Throw New ReglaNegocioException("DOCUMENTO_VACIO", "Indique al menos una cantidad recibida.")
        If recibidas.Any(Function(l) l.EmpaquesU6 < 0 OrElse (l.PrecioEmpaqueU6.HasValue AndAlso l.PrecioEmpaqueU6.Value < 0)) Then
            Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Cantidades y precios no pueden ser negativos.")
        End If
        Return EnTransaccion(Permisos.StockContabilizar,
            Function(u)
                Dim ped = u.Consultar("SELECT p.almacen_id, p.proveedor_id, p.moneda, p.estado, p.tipo FROM pedido_compra p JOIN almacen a ON a.id = p.almacen_id " &
                                      "WHERE p.id = @p AND a.operacion_id = @o FOR UPDATE OF p",
                                      Function(rd) (Almacen:=rd.GetInt64(0), Proveedor:=rd.GetInt64(1), Moneda:=rd.GetString(2), Estado:=rd.GetString(3), Tipo:=rd.GetString(4)),
                                      "p", pedidoId, "o", Sesion.Operacion.Id).SingleOrDefault()
                If ped.Almacen = 0 Then Throw New ReglaNegocioException("OPERACION_AJENA", "El pedido no pertenece a la operacion seleccionada.")
                If ped.Estado <> "aprobado" AndAlso ped.Estado <> "enviado" AndAlso ped.Estado <> "parcial" Then
                    Throw New ReglaNegocioException("PEDIDO_NO_RECIBIBLE", $"El pedido esta {ped.Estado}; solo se reciben pedidos aprobados, enviados o parciales.")
                End If
                Dim pendientes = LeerPendientes(u, pedidoId).ToDictionary(Function(x) x.PedidoDetalleId)

                Dim numero = SiguienteNumero(u, "RC")
                Dim recepcionId = u.EscalarLong(
                    "INSERT INTO recepcion(empresa_id, almacen_id, proveedor_id, pedido_id, numero, tipo_documento, numero_documento, fecha_documento, " &
                    "fecha_recepcion, moneda, tipo_ingreso, usuario_id, observacion) VALUES (@e, @a, @pr, @p, @n, @td, @nd, @fd, @fr, @m, @ti, @u, @ob) RETURNING id",
                    "e", Sesion.EmpresaId, "a", ped.Almacen, "pr", ped.Proveedor, "p", pedidoId, "n", numero, "td", tipoDoc, "nd", numDoc,
                    "fd", fechaDocumento.Date, "fr", fechaRecepcion.Date, "m", ped.Moneda, "ti", If(ped.Tipo = "caja_chica", "caja_chica", "compra"),
                    "u", Sesion.UsuarioId, "ob", ServicioRecetas.Opcional(observacion))

                Dim lineasStock As New List(Of LineaDocumentoStock)
                For Each l In recibidas
                    Dim pend As LineaPendienteDto = Nothing
                    If Not pendientes.TryGetValue(l.PedidoDetalleId, pend) Then
                        Throw New ReglaNegocioException("LINEA_AJENA", "La linea no pertenece al pedido.")
                    End If
                    Dim cantidadBase = EscalaU6.Multiplicar(l.EmpaquesU6, pend.FactorBasePorEmpaqueU6)
                    If cantidadBase > pend.PendienteBaseU6 Then
                        Throw New ReglaNegocioException("EXCESO_RECEPCION",
                            $"{pend.ProductoDescripcion}: se reciben {EscalaU6.ADecimal(cantidadBase)} {pend.Unidad} y quedan pendientes {EscalaU6.ADecimal(pend.PendienteBaseU6)}.")
                    End If
                    Dim precio = If(l.PrecioEmpaqueU6, pend.PrecioEmpaqueU6)
                    Dim costo = EscalaU6.Multiplicar(l.EmpaquesU6, precio)
                    Dim costoUnitario = Valoracion.CostoUnitarioU6(costo, cantidadBase)
                    Dim detalleId = u.EscalarLong(
                        "INSERT INTO recepcion_detalle(empresa_id, recepcion_id, variante_id, pedido_detalle_id, empaque_id, unidad_recibida, cantidad_recibida_u6, " &
                        "factor_conversion_u6, cantidad_base_u6, precio_unidad_recibida_u6, costo_adquisicion_u6, costo_unitario_base_u6) " &
                        "VALUES (@e, @r, @v, @pd, @em, @ur, @cr, @f, @cb, @pu, @ca, @cu) RETURNING id",
                        "e", Sesion.EmpresaId, "r", recepcionId, "v", pend.VarianteId, "pd", l.PedidoDetalleId, "em", pend.EmpaqueId,
                        "ur", pend.EmpaqueDescripcion, "cr", l.EmpaquesU6, "f", pend.FactorBasePorEmpaqueU6, "cb", cantidadBase,
                        "pu", precio, "ca", costo, "cu", costoUnitario)
                    lineasStock.Add(New LineaDocumentoStock(pend.VarianteId, cantidadBase, costoUnitario, costo) With {.RecepcionDetalleId = detalleId})
                    pend.PendienteBaseU6 -= cantidadBase
                Next
                u.Ejecutar("UPDATE recepcion SET estado = 'confirmada' WHERE id = @r", "r", recepcionId)
                Stock().ContabilizarEn(u, New DocumentoStockNuevo(ped.Almacen, TipoDocumentoStock.Recepcion, fechaRecepcion, numero, lineasStock) With {.RecepcionId = recepcionId})

                Dim nuevoEstado = If(pendientes.Values.All(Function(x) x.PendienteBaseU6 <= 0), "recibido", "parcial")
                If nuevoEstado <> ped.Estado Then u.Ejecutar("UPDATE pedido_compra SET estado = @s WHERE id = @p", "s", nuevoEstado, "p", pedidoId)
                Return recepcionId
            End Function)
    End Function

    ' ---------- Salidas, bajas, devoluciones y traspasos ----------

    ''' <summary>Entrega a producción (cocina). Valor = cantidad × costo vigente.</summary>
    Public Function SalidaProduccion(almacenId As Long, fecha As Date, lineas As IEnumerable(Of LineaSalida), Optional motivo As String = Nothing) As Long
        Return Salida(almacenId, fecha, lineas, TipoDocumentoStock.SalidaProduccion, "SP", motivo)
    End Function

    ''' <summary>Baja (merma, vencido, dañado). Exige motivo.</summary>
    Public Function Baja(almacenId As Long, fecha As Date, lineas As IEnumerable(Of LineaSalida), motivo As String) As Long
        Return Salida(almacenId, fecha, lineas, TipoDocumentoStock.Baja, "BJ", ServicioAdministracion.Requerido(motivo, "motivo de la baja"))
    End Function

    Private Function Salida(almacenId As Long, fecha As Date, lineas As IEnumerable(Of LineaSalida), tipo As TipoDocumentoStock, prefijo As String, motivo As String) As Long
        Dim copia = lineas.ToList()
        Return EnTransaccion(Permisos.StockContabilizar,
            Function(u) Stock().ContabilizarEn(u, New DocumentoStockNuevo(almacenId, tipo, fecha, SiguienteNumero(u, prefijo),
                                                    copia.Select(Function(l) New LineaDocumentoStock(l.VarianteId, l.CantidadBaseU6, 0))) With {.Motivo = motivo}))
    End Function

    ''' <summary>
    ''' Devolución de cocina de una entrega: se valoriza al costo histórico de esa salida y no puede superar lo
    ''' entregado menos lo ya devuelto (DEVOLUCION_EXCEDIDA).
    ''' </summary>
    Public Function DevolucionProduccion(salidaDocumentoId As Long, fecha As Date, lineas As IEnumerable(Of LineaSalida)) As Long
        Dim copia = lineas.ToList()
        Return EnTransaccion(Permisos.StockContabilizar,
            Function(u)
                Dim sal = u.Consultar("SELECT d.almacen_id, d.tipo, d.requerimiento_id, d.operacion_servicio_id FROM documento_stock d JOIN almacen a ON a.id = d.almacen_id " &
                                      "WHERE d.id = @d AND a.operacion_id = @o FOR UPDATE OF d",
                                      Function(rd) (Almacen:=rd.GetInt64(0), Tipo:=rd.GetString(1), Req:=If(rd.IsDBNull(2), CType(Nothing, Long?), rd.GetInt64(2)),
                                                    Servicio:=If(rd.IsDBNull(3), CType(Nothing, Long?), rd.GetInt64(3))), "d", salidaDocumentoId, "o", Sesion.Operacion.Id).SingleOrDefault()
                If sal.Almacen = 0 Then Throw New ReglaNegocioException("OPERACION_AJENA", "La salida no pertenece a la operacion seleccionada.")
                If sal.Tipo <> "salida_produccion" Then Throw New ReglaNegocioException("ORIGEN_REQUERIDO", "Solo se devuelve sobre una salida a produccion.")
                Dim entregado = u.Consultar(
                    "SELECT l.variante_id, sum(l.cantidad_base_u6)::bigint, sum(l.valor_u6)::bigint, " &
                    "  COALESCE((SELECT sum(x.cantidad_base_u6) FROM documento_stock_detalle x JOIN documento_stock dv ON dv.id = x.documento_id " &
                    "            WHERE dv.documento_origen_id = @d AND dv.tipo = 'devolucion_produccion' AND x.variante_id = l.variante_id), 0)::bigint " &
                    "FROM documento_stock_detalle l WHERE l.documento_id = @d GROUP BY l.variante_id",
                    Function(rd) (Variante:=rd.GetInt64(0), Cantidad:=rd.GetInt64(1), Valor:=rd.GetInt64(2), Devuelto:=rd.GetInt64(3)), "d", salidaDocumentoId) _
                    .ToDictionary(Function(x) x.Variante)
                Dim lineasStock As New List(Of LineaDocumentoStock)
                For Each g In copia.GroupBy(Function(l) l.VarianteId)
                    Dim e As (Variante As Long, Cantidad As Long, Valor As Long, Devuelto As Long) = Nothing
                    If Not entregado.TryGetValue(g.Key, e) Then Throw New ReglaNegocioException("DEVOLUCION_EXCEDIDA", "La variante no estaba en la entrega.")
                    Dim cantidad = g.Sum(Function(l) l.CantidadBaseU6)
                    If cantidad > e.Cantidad - e.Devuelto Then
                        Throw New ReglaNegocioException("DEVOLUCION_EXCEDIDA",
                            $"Se entregaron {EscalaU6.ADecimal(e.Cantidad)}, ya se devolvieron {EscalaU6.ADecimal(e.Devuelto)} y se intenta devolver {EscalaU6.ADecimal(cantidad)}.")
                    End If
                    Dim valor = EscalaU6.MultiplicarDividir(e.Valor, cantidad, e.Cantidad)   ' costo histórico de la entrega
                    lineasStock.Add(New LineaDocumentoStock(g.Key, cantidad, Valoracion.CostoUnitarioU6(valor, cantidad), valor))
                Next
                Return Stock().ContabilizarEn(u, New DocumentoStockNuevo(sal.Almacen, TipoDocumentoStock.DevolucionProduccion, fecha, SiguienteNumero(u, "DV"), lineasStock) _
                                                 With {.DocumentoOrigenId = salidaDocumentoId, .RequerimientoId = sal.Req, .OperacionServicioId = sal.Servicio})
            End Function)
    End Function

    ''' <summary>Traspaso entre almacenes de la operación: salida al costo vigente y entrada en destino por el mismo valor.</summary>
    Public Function Traspasar(origenId As Long, destinoId As Long, fecha As Date, lineas As IEnumerable(Of LineaSalida)) As Long
        If origenId = destinoId Then Throw New ReglaNegocioException("DATO_INVALIDO", "El almacen de destino debe ser distinto del de origen.")
        Dim copia = lineas.ToList()
        Return EnTransaccion(Permisos.StockContabilizar,
            Function(u)
                Dim numero = SiguienteNumero(u, "TR")
                Dim salidaId = Stock().ContabilizarEn(u, New DocumentoStockNuevo(origenId, TipoDocumentoStock.TraspasoSalida, fecha, numero & "-S",
                                                          copia.Select(Function(l) New LineaDocumentoStock(l.VarianteId, l.CantidadBaseU6, 0))) With {.AlmacenDestinoId = destinoId})
                Dim salidas = u.Consultar("SELECT variante_id, cantidad_base_u6, costo_unitario_base_u6, valor_u6 FROM documento_stock_detalle WHERE documento_id = @d ORDER BY id",
                                          Function(rd) New LineaDocumentoStock(rd.GetInt64(0), rd.GetInt64(1), rd.GetInt64(2), rd.GetInt64(3)), "d", salidaId)
                Stock().ContabilizarEn(u, New DocumentoStockNuevo(destinoId, TipoDocumentoStock.TraspasoEntrada, fecha, numero & "-E", salidas) With {.DocumentoOrigenId = salidaId})
                Return salidaId
            End Function)
    End Function

    ' ---------- Consultas ----------

    Public Function ListarDocumentos(almacenId As Long, desde As Date, hasta As Date) As List(Of DocumentoStockDto)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar(
                "SELECT d.id, d.numero, d.tipo, d.fecha, d.motivo, COALESCE((SELECT sum(valor_u6) FROM documento_stock_detalle l WHERE l.documento_id = d.id), 0)::bigint " &
                "FROM documento_stock d JOIN almacen a ON a.id = d.almacen_id WHERE d.almacen_id = @a AND a.operacion_id = @o AND d.fecha BETWEEN @d AND @h ORDER BY d.fecha, d.id",
                Function(rd) New DocumentoStockDto With {.Id = rd.GetInt64(0), .Numero = rd.GetString(1), .Tipo = rd.GetString(2), .Fecha = rd.GetDateTime(3),
                                                         .Motivo = rd.TextoONada("motivo"), .ValorU6 = rd.GetInt64(5)},
                "a", almacenId, "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date))
    End Function

    ''' <summary>Kárdex valorizado de una variante en el almacén (R10): saldo inicial antes de 'desde' y saldo corrido.</summary>
    Public Function Kardex(almacenId As Long, varianteId As Long, desde As Date, hasta As Date) As List(Of KardexDto)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u)
                If u.Escalar("SELECT 1 FROM almacen WHERE id = @a AND operacion_id = @o", "a", almacenId, "o", Sesion.Operacion.Id) Is Nothing Then
                    Throw New ReglaNegocioException("OPERACION_AJENA", "El almacen no pertenece a la operacion seleccionada.")
                End If
                Dim inicial = u.Consultar("SELECT COALESCE(sum(signo * cantidad_base_u6), 0)::bigint, COALESCE(sum(signo * valor_u6), 0)::bigint FROM movimiento_stock " &
                                          "WHERE almacen_id = @a AND variante_id = @v AND fecha < @d", Function(rd) (rd.GetInt64(0), rd.GetInt64(1)),
                                          "a", almacenId, "v", varianteId, "d", desde.Date).Single()
                Dim cant = inicial.Item1, valor = inicial.Item2
                Dim r As New List(Of KardexDto) From {
                    New KardexDto With {.Fecha = desde.Date, .Documento = "SALDO INICIAL", .Tipo = "", .SaldoCantidadU6 = cant, .SaldoValorU6 = valor,
                                        .CostoPromedioU6 = Valoracion.CostoUnitarioU6(valor, cant)}}
                For Each m In u.Consultar(
                    "SELECT m.fecha, m.secuencia, d.numero, d.tipo, m.signo, m.cantidad_base_u6, m.valor_u6 FROM movimiento_stock m " &
                    "JOIN documento_stock_detalle l ON l.id = m.documento_detalle_id JOIN documento_stock d ON d.id = l.documento_id " &
                    "WHERE m.almacen_id = @a AND m.variante_id = @v AND m.fecha BETWEEN @d AND @h ORDER BY m.fecha, m.secuencia",
                    Function(rd) (Fecha:=rd.GetDateTime(0), Sec:=rd.GetInt64(1), Numero:=rd.GetString(2), Tipo:=rd.GetString(3), Signo:=rd.GetInt64(4),
                                  Cant:=rd.GetInt64(5), Valor:=rd.GetInt64(6)), "a", almacenId, "v", varianteId, "d", desde.Date, "h", hasta.Date)
                    cant += m.Signo * m.Cant
                    valor += m.Signo * m.Valor
                    r.Add(New KardexDto With {
                        .Fecha = m.Fecha, .Secuencia = m.Sec, .Documento = m.Numero, .Tipo = m.Tipo,
                        .EntradaU6 = If(m.Signo > 0, m.Cant, CType(Nothing, Long?)), .SalidaU6 = If(m.Signo < 0, m.Cant, CType(Nothing, Long?)),
                        .ValorMovimientoU6 = m.Signo * m.Valor, .SaldoCantidadU6 = cant, .SaldoValorU6 = valor, .CostoPromedioU6 = Valoracion.CostoUnitarioU6(valor, cant)})
                Next
                Return r
            End Function)
    End Function

    ' ---------- Auxiliares ----------

    Private Shared Function LeerPendientes(u As UnidadDeTrabajo, pedidoId As Long) As List(Of LineaPendienteDto)
        Return u.Consultar(
            "SELECT d.id, d.empaque_id, v.id, pb.descripcion, e.descripcion, um.codigo, d.factor_base_por_empaque_u6, d.cantidad_empaques, " &
            "       d.cantidad_base_u6 - COALESCE((SELECT sum(rd.cantidad_base_u6) FROM recepcion_detalle rd JOIN recepcion r ON r.id = rd.recepcion_id " &
            "                                       WHERE rd.pedido_detalle_id = d.id AND r.estado = 'confirmada'), 0), d.precio_empaque_u6 " &
            "FROM pedido_detalle d JOIN empaque_compra e ON e.id = d.empaque_id JOIN variante_producto v ON v.id = e.variante_id " &
            "JOIN producto_base pb ON pb.id = v.producto_base_id JOIN unidad_medida um ON um.id = pb.unidad_base_id WHERE d.pedido_id = @p ORDER BY pb.descripcion, d.id",
            Function(rd) New LineaPendienteDto With {
                .PedidoDetalleId = rd.GetInt64(0), .EmpaqueId = rd.GetInt64(1), .VarianteId = rd.GetInt64(2), .ProductoDescripcion = rd.GetString(3),
                .EmpaqueDescripcion = rd.GetString(4), .Unidad = rd.GetString(5), .FactorBasePorEmpaqueU6 = rd.GetInt64(6), .PedidoEmpaques = rd.GetInt64(7),
                .PendienteBaseU6 = Convert.ToInt64(rd.GetValue(8)), .PrecioEmpaqueU6 = rd.GetInt64(9)}, "p", pedidoId)
    End Function

    ''' <summary>Correlativo por prefijo y año (RC recepción, SP salida, BJ baja, DV devolución, TR traspaso).</summary>
    Private Function SiguienteNumero(u As UnidadDeTrabajo, prefijo As String) As String
        u.Ejecutar("SELECT pg_advisory_xact_lock(hashtext('documento_' || @p), @e::int)", "p", prefijo, "e", Sesion.EmpresaId)
        Dim base = $"{prefijo}-{Date.Today.Year}-"
        Dim n = u.EscalarLong("SELECT COALESCE(max(substring(numero from @l for 5)::bigint), 0) + 1 FROM documento_stock WHERE numero LIKE @b",
                              "l", base.Length + 1, "b", base & "%")
        Return $"{base}{n:00000}"
    End Function

End Class
