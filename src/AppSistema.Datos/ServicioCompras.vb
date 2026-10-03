Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Catalogo
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

Public NotInheritable Class PrevisionDto
    Public Property Id As Long
    Public Property AlmacenId As Long
    Public Property FechaCorte As Date
    Public Property FechaDesde As Date
    Public Property FechaHasta As Date
    Public Property Estado As String
    Public Property FechaCalculo As DateTime
End Class

Public NotInheritable Class PrevisionLineaDto
    Public Property Id As Long
    Public Property ProductoBaseId As Long
    Public Property ProductoCodigo As String
    Public Property ProductoDescripcion As String
    Public Property Unidad As String
    Public Property DemandaU6 As Long
    Public Property ConsumoPuenteU6 As Long
    Public Property StockU6 As Long
    Public Property ReservaU6 As Long
    Public Property PendienteRecibirU6 As Long
    Public Property NecesidadNetaU6 As Long
    Public Property FechaQuiebre As Date?
End Class

Public NotInheritable Class PedidoDto
    Public Property Id As Long
    Public Property Numero As String
    Public Property ProveedorId As Long
    Public Property ProveedorNombre As String
    Public Property Tipo As String
    Public Property Fecha As Date
    Public Property Moneda As String
    Public Property Estado As String
    Public Property PrevisionId As Long?
    Public Property TotalU6 As Long
End Class

Public NotInheritable Class PedidoLineaDto
    Public Property Id As Long
    Public Property ProductoDescripcion As String
    Public Property Unidad As String
    Public Property EmpaqueDescripcion As String
    Public Property VarianteCodigo As String
    Public Property CantidadEmpaques As Long
    Public Property CantidadBaseU6 As Long
    Public Property NecesidadU6 As Long?
    Public Property ExcesoU6 As Long?
    Public Property PrecioEmpaqueU6 As Long
    Public Property ImporteU6 As Long
    Public Property FechaEntrega As Date
    Public Property PendienteU6 As Long
End Class

Public NotInheritable Class ResultadoGenerarPedido
    Public Property PedidoId As Long
    Public Property Numero As String
    Public Property Lineas As Integer
    ''' <summary>Productos con necesidad que el proveedor no ofrece (quedan para otro pedido).</summary>
    Public ReadOnly Property SinEmpaqueDelProveedor As New List(Of String)
    ''' <summary>Líneas sin precio vigente (se registran a 0 y deben revisarse antes de aprobar).</summary>
    Public ReadOnly Property SinPrecio As New List(Of String)
End Class

''' <summary>
''' Previsión de compras desde minutas aprobadas y pedidos de compra (módulo 2).
''' Previsión: calcular (borrador) → revisar → validar (si sigue vigente) → generar pedidos.
''' Una previsión es obsoleta cuando, recalculada hoy, cambia algún número (minuta, stock, pedido o reserva: T18).
''' </summary>
Public NotInheritable Class ServicioCompras
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    Private ReadOnly Property OperacionId As Long
        Get
            If Sesion.Operacion Is Nothing Then Throw New ReglaNegocioException("OPERACION_NO_SELECCIONADA", "Seleccione una operacion.")
            Return Sesion.Operacion.Id
        End Get
    End Property

    ' ---------- Reserva (D08: parámetro por producto y almacén) ----------

    Public Sub FijarReserva(almacenId As Long, productoBaseId As Long, reservaU6 As Long)
        If reservaU6 < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "La reserva no puede ser negativa.")
        Dim op = OperacionId
        EnTransaccion(Permisos.ComprasEditar,
            Function(u)
                ExigirAlmacen(u, almacenId, op)
                Return u.Ejecutar("INSERT INTO politica_abastecimiento(empresa_id, almacen_id, producto_base_id, reserva_base_u6) VALUES (@e, @a, @p, @r) " &
                                  "ON CONFLICT (empresa_id, almacen_id, producto_base_id) DO UPDATE SET reserva_base_u6 = EXCLUDED.reserva_base_u6",
                                  "e", Sesion.EmpresaId, "a", almacenId, "p", productoBaseId, "r", reservaU6)
            End Function)
    End Sub

    ' ---------- Previsión ----------

    ''' <summary>
    ''' Calcula la previsión del almacén para [desde, hasta]. La demanda entre la fecha de corte y el inicio del
    ''' horizonte es consumo puente. El stock es el saldo actual. Reemplaza la previsión vigente del mismo horizonte.
    ''' </summary>
    Public Function CalcularPrevision(almacenId As Long, fechaCorte As Date, desde As Date, hasta As Date) As Long
        If hasta.Date < desde.Date OrElse desde.Date < fechaCorte.Date Then
            Throw New ReglaNegocioException("HORIZONTE_INVALIDO", "Debe cumplirse fecha de corte <= desde <= hasta.")
        End If
        Dim op = OperacionId
        Return EnTransaccion(Permisos.ComprasEditar,
            Function(u)
                ExigirAlmacen(u, almacenId, op)
                Dim lineas = Calcular(u, almacenId, op, fechaCorte.Date, desde.Date, hasta.Date)
                u.Ejecutar("UPDATE prevision SET estado = 'reemplazada' WHERE almacen_id = @a AND fecha_desde = @d AND fecha_hasta = @h AND estado IN ('borrador','validada')",
                           "a", almacenId, "d", desde.Date, "h", hasta.Date)
                Dim id = u.EscalarLong("INSERT INTO prevision(empresa_id, almacen_id, fecha_desde, fecha_hasta, fecha_calculo, usuario_id, fecha_corte) " &
                                       "VALUES (@e, @a, @d, @h, now(), @u, @c) RETURNING id",
                                       "e", Sesion.EmpresaId, "a", almacenId, "d", desde.Date, "h", hasta.Date, "u", Sesion.UsuarioId, "c", fechaCorte.Date)
                For Each l In lineas
                    u.Ejecutar("INSERT INTO prevision_detalle(empresa_id, prevision_id, producto_base_id, necesidad_menu_u6, consumo_puente_u6, stock_utilizable_u6, " &
                               "reserva_u6, pendiente_recibir_u6, necesidad_neta_u6, fecha_quiebre) VALUES (@e, @p, @pb, @d, @cp, @s, @r, @pr, @n, @q)",
                               "e", Sesion.EmpresaId, "p", id, "pb", l.ProductoBaseId, "d", l.DemandaU6, "cp", l.ConsumoPuenteU6, "s", l.StockU6,
                               "r", l.ReservaU6, "pr", l.PendienteRecibirU6, "n", l.NecesidadNetaU6,
                               "q", If(l.FechaQuiebre.HasValue, CType(l.FechaQuiebre.Value, Object), Nothing))
                Next
                Return id
            End Function)
    End Function

    Public Function Desglose(previsionId As Long) As List(Of PrevisionLineaDto)
        Return EnTransaccion(Permisos.ComprasVer, Function(u) LeerDetalle(u, previsionId))
    End Function

    ''' <summary>Diferencias entre la previsión guardada y la recalculada ahora (vacío = vigente).</summary>
    Public Function Diferencias(previsionId As Long) As List(Of String)
        Return EnTransaccion(Permisos.ComprasVer, Function(u) Comparar(u, previsionId))
    End Function

    Public Sub Validar(previsionId As Long)
        EnTransaccion(Permisos.ComprasEditar,
            Function(u)
                ExigirVigente(u, previsionId)
                Return u.Ejecutar("UPDATE prevision SET estado = 'validada' WHERE id = @p AND estado = 'borrador'", "p", previsionId)
            End Function)
    End Sub

    Public Function ListarPrevisiones(almacenId As Long) As List(Of PrevisionDto)
        Return EnTransaccion(Permisos.ComprasVer,
            Function(u) u.Consultar("SELECT id, almacen_id, COALESCE(fecha_corte, fecha_desde), fecha_desde, fecha_hasta, estado, fecha_calculo FROM prevision " &
                                    "WHERE almacen_id = @a ORDER BY id DESC LIMIT 200",
                Function(rd) New PrevisionDto With {.Id = rd.GetInt64(0), .AlmacenId = rd.GetInt64(1), .FechaCorte = rd.GetDateTime(2), .FechaDesde = rd.GetDateTime(3),
                                                    .FechaHasta = rd.GetDateTime(4), .Estado = rd.GetString(5), .FechaCalculo = rd.GetDateTime(6)}, "a", almacenId))
    End Function

    ' ---------- Pedidos ----------

    ''' <summary>
    ''' Pedido borrador al proveedor con las necesidades de una previsión validada y vigente. Por producto se elige
    ''' el empaque del producto activo en la operación (D02); si el proveedor no lo ofrece, el de menor costo por unidad base vigente (sin precio: el primero, a 0)
    ''' y se redondea por mínimo y múltiplo (D04 propuesto). Lo que ya está en otro pedido de esta previsión no se repite.
    ''' </summary>
    Public Function GenerarPedido(previsionId As Long, proveedorId As Long, fechaEntrega As Date, moneda As String) As ResultadoGenerarPedido
        Dim m = ServicioAdministracion.Requerido(moneda, "moneda")
        Dim op = OperacionId
        Return EnTransaccion(Permisos.ComprasEditar,
            Function(u)
                Dim prev = u.Consultar("SELECT almacen_id, estado FROM prevision WHERE id = @p FOR UPDATE",
                                       Function(rd) (Almacen:=rd.GetInt64(0), Estado:=rd.GetString(1)), "p", previsionId).SingleOrDefault()
                If prev.Almacen = 0 Then Throw New ReglaNegocioException("NO_ENCONTRADO", "La prevision no existe.")
                ExigirAlmacen(u, prev.Almacen, op)
                If prev.Estado <> "validada" Then Throw New ReglaNegocioException("PREVISION_NO_VALIDADA", "Valide la prevision antes de generar pedidos.")
                ExigirVigente(u, previsionId)

                Dim r As New ResultadoGenerarPedido()
                Dim yaPedido = New HashSet(Of Long)(u.Consultar(
                    "SELECT DISTINCT d.prevision_detalle_id FROM pedido_detalle d JOIN pedido_compra p ON p.id = d.pedido_id " &
                    "WHERE p.prevision_id = @p AND p.estado <> 'anulado' AND d.prevision_detalle_id IS NOT NULL", Function(rd) rd.GetInt64(0), "p", previsionId))
                Dim necesidades = LeerDetalle(u, previsionId).Where(Function(l) l.NecesidadNetaU6 > 0 AndAlso Not yaPedido.Contains(l.Id)).ToList()
                Dim ofertas = u.Consultar(
                    "SELECT v.producto_base_id, e.id, e.envases_por_empaque, e.minimo_empaques, e.multiplo_empaques, v.codigo, v.contenido_base_por_envase_u6, v.id, " &
                    "       (SELECT pc.precio_empaque_u6 FROM precio_compra pc WHERE pc.proveedor_empaque_id = pe.id AND pc.moneda = @m " &
                    "          AND pc.fecha_desde <= @f AND (pc.fecha_hasta IS NULL OR pc.fecha_hasta >= @f) ORDER BY pc.fecha_desde DESC LIMIT 1) " &
                    "FROM proveedor_empaque pe JOIN empaque_compra e ON e.id = pe.empaque_id AND e.activo = 1 " &
                    "JOIN variante_producto v ON v.id = e.variante_id AND v.activo = 1 WHERE pe.proveedor_id = @pr AND pe.activo = 1",
                    Function(rd) (Producto:=rd.GetInt64(0), Empaque:=New EmpaqueCompra(New VarianteProducto(rd.GetString(5), rd.GetInt64(6)), rd.GetInt64(2), rd.GetInt64(3), rd.GetInt64(4)),
                                  EmpaqueId:=rd.GetInt64(1), Variante:=rd.GetInt64(7), Precio:=If(rd.IsDBNull(8), CType(Nothing, Long?), rd.GetInt64(8))),
                    "m", m, "f", fechaEntrega.Date, "pr", proveedorId).ToLookup(Function(o) o.Producto)

                ' D02: se compra el producto activo en la operación (liberado o en uso); si el proveedor no lo ofrece, el de menor costo.
                Dim enUso = CosteoBD.VariantesEnUso(u, op, necesidades.Select(Function(n) n.ProductoBaseId).ToArray(), fechaEntrega)
                Dim lineas As New List(Of (Necesidad As PrevisionLineaDto, EmpaqueId As Long, Compra As ResultadoCompra, Factor As Long, Precio As Long?))
                For Each n In necesidades
                    Dim candidatas = ofertas(n.ProductoBaseId).ToList()
                    If candidatas.Count = 0 Then r.SinEmpaqueDelProveedor.Add($"{n.ProductoDescripcion} ({Ui(n.NecesidadNetaU6)} {n.Unidad})") : Continue For
                    Dim activa As Long = 0
                    enUso.TryGetValue(n.ProductoBaseId, activa)
                    Dim elegida = candidatas.OrderBy(Function(c) If(c.Variante = activa, 0, 1)).ThenBy(Function(c) If(c.Precio.HasValue, 0, 1)) _
                                            .ThenBy(Function(c) If(c.Precio.HasValue, Costeo.CostoUnitarioBaseU6(c.Precio.Value, c.Empaque.EnvasesPorEmpaque, c.Empaque.Variante.ContenidoBasePorEnvaseU6), 0L)) _
                                            .ThenBy(Function(c) c.EmpaqueId).First()
                    lineas.Add((n, elegida.EmpaqueId, Compras.EmpaquesAComprar(n.NecesidadNetaU6, elegida.Empaque), elegida.Empaque.ContenidoBaseU6, elegida.Precio))
                    If Not elegida.Precio.HasValue Then r.SinPrecio.Add(n.ProductoDescripcion)
                Next
                If lineas.Count = 0 Then Throw New ReglaNegocioException("NADA_QUE_PEDIR", "El proveedor no ofrece ninguno de los productos pendientes de la prevision.")

                r.Numero = SiguienteNumero(u)
                r.PedidoId = u.EscalarLong("INSERT INTO pedido_compra(empresa_id, almacen_id, proveedor_id, prevision_id, numero, tipo, fecha, moneda, usuario_id) " &
                                           "VALUES (@e, @a, @pr, @p, @n, 'normal', CURRENT_DATE, @m, @u) RETURNING id",
                                           "e", Sesion.EmpresaId, "a", prev.Almacen, "pr", proveedorId, "p", previsionId, "n", r.Numero, "m", m, "u", Sesion.UsuarioId)
                For Each l In lineas
                    u.Ejecutar("INSERT INTO pedido_detalle(empresa_id, pedido_id, empaque_id, cantidad_empaques, factor_base_por_empaque_u6, cantidad_base_u6, " &
                               "precio_empaque_u6, fecha_entrega, prevision_detalle_id) VALUES (@e, @p, @em, @c, @f, @b, @pr, @fe, @pd)",
                               "e", Sesion.EmpresaId, "p", r.PedidoId, "em", l.EmpaqueId, "c", l.Compra.Empaques, "f", l.Factor, "b", l.Compra.TotalBaseU6,
                               "pr", If(l.Precio, 0L), "fe", fechaEntrega.Date, "pd", l.Necesidad.Id)
                Next
                r.Lineas = lineas.Count
                Return r
            End Function)
    End Function

    ''' <summary>Pedido manual (adicional o de caja chica), sin previsión.</summary>
    Public Function CrearPedido(almacenId As Long, proveedorId As Long, tipo As String, moneda As String) As Long
        If tipo <> "normal" AndAlso tipo <> "extra" AndAlso tipo <> "caja_chica" Then Throw New ReglaNegocioException("DATO_INVALIDO", "Tipo de pedido: normal, extra o caja_chica.")
        Dim op = OperacionId
        Return EnTransaccion(Permisos.ComprasEditar,
            Function(u)
                ExigirAlmacen(u, almacenId, op)
                Return u.EscalarLong("INSERT INTO pedido_compra(empresa_id, almacen_id, proveedor_id, numero, tipo, fecha, moneda, usuario_id) " &
                                     "VALUES (@e, @a, @pr, @n, @t, CURRENT_DATE, @m, @u) RETURNING id",
                                     "e", Sesion.EmpresaId, "a", almacenId, "pr", proveedorId, "n", SiguienteNumero(u), "t", tipo,
                                     "m", ServicioAdministracion.Requerido(moneda, "moneda"), "u", Sesion.UsuarioId)
            End Function)
    End Function

    ''' <summary>Línea manual: la cantidad en empaques debe cumplir mínimo y múltiplo (MULTIPLO_INCOMPATIBLE).</summary>
    Public Function AgregarLinea(pedidoId As Long, empaqueId As Long, cantidadEmpaques As Long, fechaEntrega As Date, precioEmpaqueU6 As Long) As Long
        If cantidadEmpaques <= 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "La cantidad de empaques debe ser mayor que cero.")
        If precioEmpaqueU6 < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "El precio no puede ser negativo.")
        Dim op = OperacionId
        Return EnTransaccion(Permisos.ComprasEditar,
            Function(u)
                ExigirPedido(u, pedidoId, op)
                Dim factor = u.EscalarLong("SELECT e.envases_por_empaque * v.contenido_base_por_envase_u6 FROM empaque_compra e JOIN variante_producto v ON v.id = e.variante_id WHERE e.id = @e", "e", empaqueId)
                Return u.EscalarLong("INSERT INTO pedido_detalle(empresa_id, pedido_id, empaque_id, cantidad_empaques, factor_base_por_empaque_u6, cantidad_base_u6, precio_empaque_u6, fecha_entrega) " &
                                     "VALUES (@e, @p, @em, @c, @f, @b, @pr, @fe) RETURNING id",
                                     "e", Sesion.EmpresaId, "p", pedidoId, "em", empaqueId, "c", cantidadEmpaques, "f", factor, "b", cantidadEmpaques * factor,
                                     "pr", precioEmpaqueU6, "fe", fechaEntrega.Date)
            End Function)
    End Function

    Public Sub QuitarLinea(lineaId As Long)
        Dim op = OperacionId
        EnTransaccion(Permisos.ComprasEditar,
            Function(u)
                ExigirPedido(u, u.EscalarLong("SELECT pedido_id FROM pedido_detalle WHERE id = @l", "l", lineaId), op)
                Return ServicioRecetas.ExigirFila(u.Ejecutar("DELETE FROM pedido_detalle WHERE id = @l", "l", lineaId))
            End Function)
    End Sub

    ''' <summary>Aprueba el pedido. Si viene de una previsión, esta debe seguir vigente (PREVISION_OBSOLETA).</summary>
    Public Sub AprobarPedido(pedidoId As Long)
        Dim op = OperacionId
        EnTransaccion(Permisos.ComprasAprobar,
            Function(u)
                ExigirPedido(u, pedidoId, op)
                Dim prevision = u.Escalar("SELECT prevision_id FROM pedido_compra WHERE id = @p FOR UPDATE", "p", pedidoId)
                If prevision IsNot Nothing AndAlso Not TypeOf prevision Is DBNull Then ExigirVigente(u, CLng(prevision))
                Return u.Ejecutar("UPDATE pedido_compra SET estado = 'aprobado', aprobador_id = @u WHERE id = @p", "u", Sesion.UsuarioId, "p", pedidoId)
            End Function)
    End Sub

    Public Sub AnularPedido(pedidoId As Long)
        Dim op = OperacionId
        EnTransaccion(Permisos.ComprasAprobar,
            Function(u)
                ExigirPedido(u, pedidoId, op, soloBorrador:=False)
                Return u.Ejecutar("UPDATE pedido_compra SET estado = 'anulado' WHERE id = @p", "p", pedidoId)
            End Function)
    End Sub

    Public Function ListarPedidos(almacenId As Long) As List(Of PedidoDto)
        Return EnTransaccion(Permisos.ComprasVer,
            Function(u) u.Consultar(
                "SELECT p.id, p.numero, pr.nombre, p.tipo, p.fecha, p.moneda, p.estado, p.prevision_id, p.proveedor_id, " &
                "       COALESCE((SELECT sum(round(d.cantidad_empaques * d.precio_empaque_u6)) FROM pedido_detalle d WHERE d.pedido_id = p.id), 0)::bigint " &
                "FROM pedido_compra p JOIN proveedor pr ON pr.id = p.proveedor_id WHERE p.almacen_id = @a ORDER BY p.id DESC LIMIT 500",
                Function(rd) New PedidoDto With {.Id = rd.GetInt64(0), .Numero = rd.GetString(1), .ProveedorNombre = rd.GetString(2), .Tipo = rd.GetString(3),
                                                 .Fecha = rd.GetDateTime(4), .Moneda = rd.GetString(5), .Estado = rd.GetString(6),
                                                 .PrevisionId = If(rd.IsDBNull(7), CType(Nothing, Long?), rd.GetInt64(7)), .ProveedorId = rd.GetInt64(8), .TotalU6 = rd.GetInt64(9)}, "a", almacenId))
    End Function

    ''' <summary>Líneas con la necesidad que cubren, el exceso por redondeo y lo pendiente de recibir.</summary>
    Public Function ListarLineas(pedidoId As Long) As List(Of PedidoLineaDto)
        Return EnTransaccion(Permisos.ComprasVer,
            Function(u) u.Consultar(
                "SELECT d.id, pb.descripcion, um.codigo, e.descripcion, v.codigo, d.cantidad_empaques, d.cantidad_base_u6, pd.necesidad_neta_u6, " &
                "       d.precio_empaque_u6, d.fecha_entrega, " &
                "       d.cantidad_base_u6 - COALESCE((SELECT sum(rd.cantidad_base_u6) FROM recepcion_detalle rd JOIN recepcion r ON r.id = rd.recepcion_id " &
                "                                       WHERE rd.pedido_detalle_id = d.id AND r.estado = 'confirmada'), 0) " &
                "FROM pedido_detalle d JOIN empaque_compra e ON e.id = d.empaque_id JOIN variante_producto v ON v.id = e.variante_id " &
                "JOIN producto_base pb ON pb.id = v.producto_base_id JOIN unidad_medida um ON um.id = pb.unidad_base_id " &
                "LEFT JOIN prevision_detalle pd ON pd.id = d.prevision_detalle_id WHERE d.pedido_id = @p ORDER BY pb.descripcion, d.id",
                Function(rd)
                    Dim l As New PedidoLineaDto With {
                        .Id = rd.GetInt64(0), .ProductoDescripcion = rd.GetString(1), .Unidad = rd.GetString(2), .EmpaqueDescripcion = rd.GetString(3),
                        .VarianteCodigo = rd.GetString(4), .CantidadEmpaques = rd.GetInt64(5), .CantidadBaseU6 = rd.GetInt64(6),
                        .NecesidadU6 = If(rd.IsDBNull(7), CType(Nothing, Long?), rd.GetInt64(7)), .PrecioEmpaqueU6 = rd.GetInt64(8),
                        .FechaEntrega = rd.GetDateTime(9), .PendienteU6 = Convert.ToInt64(rd.GetValue(10))}
                    l.ExcesoU6 = If(l.NecesidadU6.HasValue, l.CantidadBaseU6 - l.NecesidadU6.Value, CType(Nothing, Long?))
                    l.ImporteU6 = l.CantidadEmpaques * l.PrecioEmpaqueU6
                    Return l
                End Function, "p", pedidoId))
    End Function

    ' ---------- Cálculo ----------

    Private Function Calcular(u As UnidadDeTrabajo, almacenId As Long, operacionId As Long, corte As Date, desde As Date, hasta As Date,
                              Optional excluirPrevision As Long = 0) As List(Of PrevisionLineaDto)
        ' Demanda por producto y fecha: minutas aprobadas o cerradas de la operación del almacén.
        Dim demanda = u.Consultar(
            "SELECT i.producto_base_id, m.fecha, i.cantidad_base_bruta_u6, rv.rendimiento_raciones_u6, d.raciones FROM minuta m " &
            "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id JOIN minuta_detalle d ON d.minuta_id = m.id " &
            "JOIN receta_version rv ON rv.id = d.receta_version_id JOIN receta_ingrediente i ON i.receta_version_id = rv.id " &
            "WHERE os.operacion_id = @o AND m.estado IN ('aprobada','cerrada') AND m.fecha BETWEEN @c AND @h " &
            "UNION ALL " &
            "SELECT f.producto_base_id, m.fecha, f.cantidad_base_u6, NULL, NULL FROM minuta m " &
            "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id JOIN minuta_estructura_fija f ON f.minuta_id = m.id " &
            "WHERE os.operacion_id = @o AND m.estado IN ('aprobada','cerrada') AND m.fecha BETWEEN @c AND @h",
            Function(rd) (Producto:=rd.GetInt64(0), Evento:=New EventoStock(rd.GetDateTime(1),
                            If(rd.IsDBNull(3), rd.GetInt64(2), Recetas.NecesidadIngredienteU6(rd.GetInt64(2), rd.GetInt64(3), rd.GetInt64(4) * EscalaU6.Factor)))),
            "o", operacionId, "c", corte, "h", hasta).ToLookup(Function(x) x.Producto, Function(x) x.Evento)

        Dim stock = u.Consultar("SELECT v.producto_base_id, sum(s.cantidad_base_u6)::bigint FROM saldo_stock s JOIN variante_producto v ON v.id = s.variante_id " &
                                "WHERE s.almacen_id = @a GROUP BY v.producto_base_id", Function(rd) (rd.GetInt64(0), rd.GetInt64(1)), "a", almacenId) _
                        .ToDictionary(Function(x) x.Item1, Function(x) x.Item2)
        Dim reservas = u.Consultar("SELECT producto_base_id, reserva_base_u6 FROM politica_abastecimiento WHERE almacen_id = @a",
                                   Function(rd) (rd.GetInt64(0), rd.GetInt64(1)), "a", almacenId).ToDictionary(Function(x) x.Item1, Function(x) x.Item2)
        ' Pendiente de recibir: pedidos aprobados/enviados/parciales, menos lo ya recibido en recepciones confirmadas.
        Dim pendientes = u.Consultar(
            "SELECT v.producto_base_id, d.fecha_entrega, d.cantidad_base_u6 - COALESCE((SELECT sum(rd.cantidad_base_u6) FROM recepcion_detalle rd " &
            "   JOIN recepcion r ON r.id = rd.recepcion_id WHERE rd.pedido_detalle_id = d.id AND r.estado = 'confirmada'), 0) " &
            "FROM pedido_detalle d JOIN pedido_compra p ON p.id = d.pedido_id JOIN empaque_compra e ON e.id = d.empaque_id " &
            "JOIN variante_producto v ON v.id = e.variante_id WHERE p.almacen_id = @a AND p.estado IN ('aprobado','enviado','parcial') " &
            "  AND COALESCE(p.prevision_id, -1) <> @ex",
            Function(rd) (Producto:=rd.GetInt64(0), Fecha:=rd.GetDateTime(1), Cantidad:=Convert.ToInt64(rd.GetValue(2))), "a", almacenId, "ex", excluirPrevision) _
            .Where(Function(x) x.Cantidad > 0).ToLookup(Function(x) x.Producto, Function(x) New EventoStock(x.Fecha, x.Cantidad))

        Dim productos = demanda.Select(Function(g) g.Key).Union(reservas.Where(Function(x) x.Value > 0).Select(Function(x) x.Key)).ToList()
        Dim lineas As New List(Of PrevisionLineaDto)
        For Each p In productos
            Dim s As Long = 0, res As Long = 0
            stock.TryGetValue(p, s)
            reservas.TryGetValue(p, res)
            Dim r = Prevision.CalcularProducto(s, res, desde, hasta, demanda(p), pendientes(p))
            lineas.Add(New PrevisionLineaDto With {
                .ProductoBaseId = p, .DemandaU6 = r.DemandaHorizonteU6, .ConsumoPuenteU6 = r.ConsumoPuenteU6, .StockU6 = s, .ReservaU6 = res,
                .PendienteRecibirU6 = r.RecepcionesElegiblesU6, .NecesidadNetaU6 = r.NecesidadNetaU6, .FechaQuiebre = r.FechaQuiebre})
        Next
        Return lineas
    End Function

    Private Shared Function LeerDetalle(u As UnidadDeTrabajo, previsionId As Long) As List(Of PrevisionLineaDto)
        Return u.Consultar(
            "SELECT d.id, d.producto_base_id, p.codigo, p.descripcion, um.codigo, d.necesidad_menu_u6, d.consumo_puente_u6, d.stock_utilizable_u6, " &
            "       d.reserva_u6, d.pendiente_recibir_u6, d.necesidad_neta_u6, d.fecha_quiebre FROM prevision_detalle d " &
            "JOIN producto_base p ON p.id = d.producto_base_id JOIN unidad_medida um ON um.id = p.unidad_base_id " &
            "WHERE d.prevision_id = @p ORDER BY (d.necesidad_neta_u6 > 0) DESC, p.descripcion",
            Function(rd) New PrevisionLineaDto With {
                .Id = rd.GetInt64(0), .ProductoBaseId = rd.GetInt64(1), .ProductoCodigo = rd.GetString(2), .ProductoDescripcion = rd.GetString(3),
                .Unidad = rd.GetString(4), .DemandaU6 = rd.GetInt64(5), .ConsumoPuenteU6 = rd.GetInt64(6), .StockU6 = rd.GetInt64(7),
                .ReservaU6 = rd.GetInt64(8), .PendienteRecibirU6 = rd.GetInt64(9), .NecesidadNetaU6 = rd.GetInt64(10),
                .FechaQuiebre = If(rd.IsDBNull(11), CType(Nothing, Date?), rd.GetDateTime(11))}, "p", previsionId)
    End Function

    Private Function Comparar(u As UnidadDeTrabajo, previsionId As Long) As List(Of String)
        Dim cab = u.Consultar("SELECT p.almacen_id, COALESCE(p.fecha_corte, p.fecha_desde), p.fecha_desde, p.fecha_hasta, a.operacion_id FROM prevision p " &
                              "JOIN almacen a ON a.id = p.almacen_id WHERE p.id = @p",
                              Function(rd) (Almacen:=rd.GetInt64(0), Corte:=rd.GetDateTime(1), Desde:=rd.GetDateTime(2), Hasta:=rd.GetDateTime(3), Operacion:=rd.GetInt64(4)),
                              "p", previsionId).SingleOrDefault()
        If cab.Almacen = 0 Then Throw New ReglaNegocioException("NO_ENCONTRADO", "La prevision no existe.")
        ' Los pedidos generados desde esta misma previsión no la vuelven obsoleta (ya cubren su necesidad).
        Dim guardado = LeerDetalle(u, previsionId).ToDictionary(Function(l) l.ProductoBaseId)
        Dim actual = Calcular(u, cab.Almacen, cab.Operacion, cab.Corte, cab.Desde, cab.Hasta, excluirPrevision:=previsionId).ToDictionary(Function(l) l.ProductoBaseId)
        Dim dif As New List(Of String)
        For Each p In guardado.Keys.Union(actual.Keys)
            Dim g As PrevisionLineaDto = Nothing, a As PrevisionLineaDto = Nothing
            guardado.TryGetValue(p, g) : actual.TryGetValue(p, a)
            Dim nombre = If(g IsNot Nothing, g.ProductoDescripcion, CStr(u.Escalar("SELECT descripcion FROM producto_base WHERE id = @p", "p", p)))
            If g Is Nothing Then dif.Add($"{nombre}: aparece demanda nueva") : Continue For
            If a Is Nothing Then dif.Add($"{nombre}: ya no tiene demanda") : Continue For
            For Each c In {("demanda", g.DemandaU6, a.DemandaU6), ("consumo puente", g.ConsumoPuenteU6, a.ConsumoPuenteU6), ("stock", g.StockU6, a.StockU6),
                           ("reserva", g.ReservaU6, a.ReservaU6), ("pendiente de recibir", g.PendienteRecibirU6, a.PendienteRecibirU6)}
                If c.Item2 <> c.Item3 Then dif.Add($"{nombre}: {c.Item1} {Ui(c.Item2)} -> {Ui(c.Item3)}")
            Next
        Next
        Return dif
    End Function

    Private Sub ExigirVigente(u As UnidadDeTrabajo, previsionId As Long)
        Dim dif = Comparar(u, previsionId)
        If dif.Count > 0 Then
            Throw New ReglaNegocioException("PREVISION_OBSOLETA",
                "La prevision ya no corresponde a los datos actuales; recalculela. Cambios: " & String.Join("; ", dif.Take(5)) & If(dif.Count > 5, $" (y {dif.Count - 5} mas)", ""))
        End If
    End Sub

    ' ---------- Auxiliares ----------

    Private Shared Function Ui(valorU6 As Long) As String
        Return EscalaU6.ADecimal(valorU6).ToString("0.######", Globalization.CultureInfo.InvariantCulture)
    End Function

    Private Function SiguienteNumero(u As UnidadDeTrabajo) As String
        u.Ejecutar("SELECT pg_advisory_xact_lock(hashtext('pedido_compra'), @e::int)", "e", Sesion.EmpresaId)
        Dim anio = Date.Today.Year
        Dim n = u.EscalarLong("SELECT COALESCE(max(substring(numero from 9)::bigint), 0) + 1 FROM pedido_compra WHERE numero LIKE @p",
                              "p", $"PC-{anio}-%")
        Return $"PC-{anio}-{n:00000}"
    End Function

    Private Shared Sub ExigirAlmacen(u As UnidadDeTrabajo, almacenId As Long, operacionId As Long)
        If u.Escalar("SELECT 1 FROM almacen WHERE id = @a AND operacion_id = @o", "a", almacenId, "o", operacionId) Is Nothing Then
            Throw New ReglaNegocioException("OPERACION_AJENA", "El almacen no pertenece a la operacion seleccionada.")
        End If
    End Sub

    Private Shared Sub ExigirPedido(u As UnidadDeTrabajo, pedidoId As Long, operacionId As Long, Optional soloBorrador As Boolean = True)
        Dim estado = u.Escalar("SELECT p.estado FROM pedido_compra p JOIN almacen a ON a.id = p.almacen_id WHERE p.id = @p AND a.operacion_id = @o",
                               "p", pedidoId, "o", operacionId)
        If estado Is Nothing Then Throw New ReglaNegocioException("OPERACION_AJENA", "El pedido no pertenece a la operacion seleccionada.")
        If soloBorrador AndAlso CStr(estado) <> "borrador" Then
            Throw New ReglaNegocioException("PEDIDO_APROBADO", $"El pedido esta {estado} y no se modifica.")
        End If
    End Sub

End Class
