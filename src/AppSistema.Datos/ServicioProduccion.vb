Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad
Imports AppSistema.Dominio.Stock

Public NotInheritable Class RequerimientoDto
    Public Property Id As Long
    Public Property Numero As String
    Public Property Tipo As String
    Public Property Fecha As Date
    Public Property Estado As String
    Public Property MinutaId As Long?
End Class

Public NotInheritable Class RequerimientoLineaDto
    Public Property Id As Long
    Public Property ProductoBaseId As Long
    Public Property ProductoDescripcion As String
    Public Property Unidad As String
    Public Property PrevistoU6 As Long
    Public Property SolicitadoU6 As Long
End Class

Public NotInheritable Class ResultadoEntrega
    Public Property DocumentoId As Long?
    Public Property Numero As String
    Public ReadOnly Property Lineas As New List(Of EntregaLineaDto)
    Public ReadOnly Property ValorU6 As Long
        Get
            Return Lineas.Sum(Function(l) l.ValorU6)
        End Get
    End Property
End Class

Public NotInheritable Class EntregaLineaDto
    Public Property ProductoDescripcion As String
    Public Property Unidad As String
    Public Property SolicitadoU6 As Long
    Public Property EntregadoU6 As Long
    ''' <summary>Entregado de más por descargar presentaciones completas (D12).</summary>
    Public Property ExcedentePresentacionU6 As Long
    Public Property FaltanteU6 As Long
    Public Property Presentaciones As String
    Public Property ValorU6 As Long
End Class

''' <summary>Consumo previsto y real de un producto en el servicio.</summary>
Public NotInheritable Class ConsumoProductoDto
    Public Property ProductoDescripcion As String
    Public Property Unidad As String
    Public Property PrevistoU6 As Long
    Public Property EntregadoU6 As Long
    Public Property DevueltoU6 As Long
    Public Property NetoU6 As Long
    Public Property DiferenciaU6 As Long
    Public Property CostoRealU6 As Long
End Class

Public NotInheritable Class ReporteServicioDto
    Public Property RacionesPrevistas As Long
    Public Property RacionesProducidas As Long?
    Public Property RacionesServidas As Long?
    Public Property RacionesExcedentes As Long?
    ''' <summary>Snapshot de la minuta aprobada; Nothing si algún plato quedó con costo pendiente.</summary>
    Public Property CostoPrevistoU6 As Long?
    ''' <summary>Entregas − devoluciones de los requerimientos de la minuta, a su costo histórico.</summary>
    Public Property CostoRealU6 As Long
    Public Property CostoRealPorRacionServidaU6 As Long?
    Public ReadOnly Property Consumo As New List(Of ConsumoProductoDto)
    Public ReadOnly Property Mermas As New List(Of String)
End Class

''' <summary>
''' Producción (módulo 4): requerimiento calculado desde la minuta aprobada, entregas a cocina en presentaciones
''' completas (D12: lo entregado se da por consumido), entregas adicionales, devoluciones, raciones producidas y
''' servidas, mermas analíticas y comparación previsto/real. El costo real es del SERVICIO (D14 pendiente: sin
''' consumo medido por receta no se atribuye costo real a cada receta).
''' </summary>
Public NotInheritable Class ServicioProduccion
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    ' ---------- Requerimientos ----------

    ''' <summary>Requerimiento calculado: necesidad de la minuta aprobada por producto base (prevista = solicitada).</summary>
    Public Function CalcularRequerimiento(minutaId As Long, almacenId As Long) As Long
        Return EnTransaccion(Permisos.ProduccionEditar,
            Function(u)
                Dim m = DatosMinuta(u, minutaId, almacenId)
                If m.Estado <> "aprobada" AndAlso m.Estado <> "cerrada" Then Throw New ReglaNegocioException("MINUTA_NO_APROBADA", "Solo se calcula el requerimiento de una minuta aprobada.")
                If u.Escalar("SELECT 1 FROM requerimiento WHERE minuta_id = @m AND tipo = 'calculado' AND estado <> 'anulado'", "m", minutaId) IsNot Nothing Then
                    Throw New ReglaNegocioException("REQUERIMIENTO_EXISTENTE", "La minuta ya tiene su requerimiento calculado; para mas cantidad use un requerimiento adicional.")
                End If
                Dim id = Insertar(u, almacenId, minutaId, m.Servicio, m.Fecha, "calculado")
                For Each n In NecesidadMinuta(u, minutaId)
                    u.Ejecutar("INSERT INTO requerimiento_detalle(empresa_id, requerimiento_id, producto_base_id, cantidad_prevista_u6, cantidad_solicitada_u6) VALUES (@e, @r, @p, @c, @c)",
                               "e", Sesion.EmpresaId, "r", id, "p", n.Key, "c", n.Value)
                Next
                Return id
            End Function)
    End Function

    ''' <summary>Requerimiento adicional (solicitud manual) para la misma minuta.</summary>
    Public Function RequerimientoAdicional(minutaId As Long, almacenId As Long) As Long
        Return EnTransaccion(Permisos.ProduccionEditar,
            Function(u)
                Dim m = DatosMinuta(u, minutaId, almacenId)
                Return Insertar(u, almacenId, minutaId, m.Servicio, m.Fecha, "adicional")
            End Function)
    End Function

    ''' <summary>Fija la cantidad solicitada de un producto (agrega la línea si no existe; 0 la quita).</summary>
    Public Sub Solicitar(requerimientoId As Long, productoBaseId As Long, cantidadU6 As Long)
        If cantidadU6 < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "La cantidad no puede ser negativa.")
        EnTransaccion(Permisos.ProduccionEditar,
            Function(u)
                ExigirRequerimiento(u, requerimientoId)
                If cantidadU6 = 0 Then
                    Return u.Ejecutar("DELETE FROM requerimiento_detalle WHERE requerimiento_id = @r AND producto_base_id = @p AND cantidad_prevista_u6 = 0",
                                      "r", requerimientoId, "p", productoBaseId) +
                           u.Ejecutar("UPDATE requerimiento_detalle SET cantidad_solicitada_u6 = 0 WHERE requerimiento_id = @r AND producto_base_id = @p",
                                      "r", requerimientoId, "p", productoBaseId)
                End If
                Return u.Ejecutar("INSERT INTO requerimiento_detalle(empresa_id, requerimiento_id, producto_base_id, cantidad_prevista_u6, cantidad_solicitada_u6) " &
                                  "VALUES (@e, @r, @p, 0, @c) ON CONFLICT (empresa_id, requerimiento_id, producto_base_id) DO UPDATE SET cantidad_solicitada_u6 = EXCLUDED.cantidad_solicitada_u6",
                                  "e", Sesion.EmpresaId, "r", requerimientoId, "p", productoBaseId, "c", cantidadU6)
            End Function)
    End Sub

    ''' <summary>
    ''' Atiende el requerimiento: por producto elige presentaciones completas del stock (menor excedente) y registra
    ''' una salida a producción valorizada. Lo que falte por stock se informa y no se entrega.
    ''' </summary>
    Public Function Atender(requerimientoId As Long, fecha As Date) As ResultadoEntrega
        Return EnTransaccion(Permisos.StockContabilizar,
            Function(u)
                Sesion.Exigir(Permisos.StockContabilizar)
                Dim req = ExigirRequerimiento(u, requerimientoId)
                Dim lineas = LeerLineas(u, requerimientoId).Where(Function(l) l.SolicitadoU6 > 0).ToList()
                If lineas.Count = 0 Then Throw New ReglaNegocioException("DOCUMENTO_VACIO", "El requerimiento no tiene cantidades solicitadas.")
                Dim disponibles = u.Consultar(
                    "SELECT v.producto_base_id, v.id, v.descripcion_comercial, v.contenido_base_por_envase_u6, s.cantidad_base_u6, s.valor_u6 FROM saldo_stock s " &
                    "JOIN variante_producto v ON v.id = s.variante_id WHERE s.almacen_id = @a AND s.cantidad_base_u6 > 0",
                    Function(rd) (Producto:=rd.GetInt64(0), Desc:=rd.GetString(2),
                                  P:=New PresentacionDisponible(rd.GetInt64(1), rd.GetInt64(3), rd.GetInt64(4), Valoracion.CostoUnitarioU6(rd.GetInt64(5), rd.GetInt64(4)))),
                    "a", req.Almacen).ToLookup(Function(x) x.Producto)

                Dim r As New ResultadoEntrega()
                Dim lineasStock As New List(Of LineaDocumentoStock)
                For Each l In lineas
                    Dim plan = Produccion.PlanificarEntrega(l.SolicitadoU6, disponibles(l.ProductoBaseId).Select(Function(x) x.P))
                    Dim nombres = disponibles(l.ProductoBaseId).ToDictionary(Function(x) x.P.VarianteId, Function(x) x.Desc)
                    r.Lineas.Add(New EntregaLineaDto With {
                        .ProductoDescripcion = l.ProductoDescripcion, .Unidad = l.Unidad, .SolicitadoU6 = l.SolicitadoU6, .EntregadoU6 = plan.EntregadoU6,
                        .ExcedentePresentacionU6 = plan.ExcedentePresentacionU6, .FaltanteU6 = plan.FaltanteU6,
                        .Presentaciones = String.Join(" + ", plan.Lineas.Select(Function(x) $"{x.Envases} x {nombres(x.VarianteId)}"))})
                    For Each x In plan.Lineas
                        lineasStock.Add(New LineaDocumentoStock(x.VarianteId, x.CantidadU6, 0))
                    Next
                Next
                If lineasStock.Count = 0 Then Throw New ReglaNegocioException("STOCK_INSUFICIENTE", "No hay stock en presentaciones completas para ningun producto.")
                r.Numero = req.Numero & "-E"
                r.DocumentoId = New ServicioStock(CadenaConexion, Sesion).ContabilizarEn(u,
                    New DocumentoStockNuevo(req.Almacen, TipoDocumentoStock.SalidaProduccion, fecha, r.Numero, lineasStock) With {
                        .RequerimientoId = requerimientoId, .OperacionServicioId = req.Servicio})
                ' Valor por producto desde el documento confirmado.
                Dim valores = u.Consultar("SELECT v.producto_base_id, sum(l.valor_u6)::bigint FROM documento_stock_detalle l JOIN variante_producto v ON v.id = l.variante_id " &
                                          "WHERE l.documento_id = @d GROUP BY v.producto_base_id", Function(rd) (rd.GetInt64(0), rd.GetInt64(1)), "d", r.DocumentoId.Value) _
                                .ToDictionary(Function(x) x.Item1, Function(x) x.Item2)
                For i = 0 To lineas.Count - 1
                    Dim v As Long = 0
                    valores.TryGetValue(lineas(i).ProductoBaseId, v)
                    r.Lineas(i).ValorU6 = v
                Next
                u.Ejecutar("UPDATE requerimiento SET estado = 'atendido', aprobador_id = @u WHERE id = @r", "u", Sesion.UsuarioId, "r", requerimientoId)
                Return r
            End Function)
    End Function

    Public Sub Anular(requerimientoId As Long)
        EnTransaccion(Permisos.ProduccionEditar,
            Function(u)
                ExigirRequerimiento(u, requerimientoId)
                Return u.Ejecutar("UPDATE requerimiento SET estado = 'anulado' WHERE id = @r", "r", requerimientoId)
            End Function)
    End Sub

    Public Function ListarRequerimientos(minutaId As Long) As List(Of RequerimientoDto)
        Return EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar("SELECT id, numero, tipo, fecha, estado, minuta_id FROM requerimiento WHERE minuta_id = @m ORDER BY id",
                Function(rd) New RequerimientoDto With {.Id = rd.GetInt64(0), .Numero = rd.GetString(1), .Tipo = rd.GetString(2), .Fecha = rd.GetDateTime(3),
                                                        .Estado = rd.GetString(4), .MinutaId = rd.GetInt64(5)}, "m", minutaId))
    End Function

    Public Function ListarLineas(requerimientoId As Long) As List(Of RequerimientoLineaDto)
        Return EnTransaccion(Permisos.MenusVer, Function(u) LeerLineas(u, requerimientoId))
    End Function

    ' ---------- Producción y mermas ----------

    ''' <summary>Registra las raciones del servicio y vincula sus entregas y devoluciones (una vez por minuta).</summary>
    Public Function RegistrarProduccion(minutaId As Long, producidas As Long, servidas As Long, excedentes As Long, observacion As String) As Long
        If producidas < 0 OrElse servidas < 0 OrElse excedentes < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Las raciones no pueden ser negativas.")
        If servidas + excedentes > producidas Then Throw New ReglaNegocioException("RACIONES_INCOHERENTES", "Servidas + excedentes no pueden superar las producidas.")
        Return EnTransaccion(Permisos.ProduccionEditar,
            Function(u)
                Dim m = DatosMinuta(u, minutaId, Nothing)
                Dim id = u.EscalarLong("INSERT INTO produccion(empresa_id, minuta_id, operacion_servicio_id, fecha, raciones_producidas, raciones_servidas, raciones_excedentes, usuario_id, observacion) " &
                                       "VALUES (@e, @m, @os, @f, @p, @s, @x, @u, @o) RETURNING id",
                                       "e", Sesion.EmpresaId, "m", minutaId, "os", m.Servicio, "f", m.Fecha, "p", producidas, "s", servidas, "x", excedentes,
                                       "u", Sesion.UsuarioId, "o", ServicioRecetas.Opcional(observacion))
                VincularDocumentos(u, id, minutaId)
                Return id
            End Function)
    End Function

    ''' <summary>
    ''' Merma de producción. Si ya está incluida en lo entregado (lo normal, D12) es solo un dato analítico y no genera
    ''' otra baja (T32). Si no lo está, debe indicarse la baja de almacén que la registra.
    ''' </summary>
    Public Function RegistrarMerma(produccionId As Long, varianteId As Long?, recetaVersionId As Long?, etapa As String, cantidadU6 As Long,
                                   motivo As String, yaIncluidaEnConsumo As Boolean, Optional documentoBajaId As Long? = Nothing) As Long
        If cantidadU6 <= 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "La cantidad debe ser positiva.")
        If Not varianteId.HasValue AndAlso Not recetaVersionId.HasValue Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", "Indique el producto o la receta de la merma.")
        If Not yaIncluidaEnConsumo AndAlso Not documentoBajaId.HasValue Then
            Throw New ReglaNegocioException("BAJA_REQUERIDA", "Una merma que no esta en lo entregado se registra con su baja de almacen.")
        End If
        Return EnTransaccion(Permisos.ProduccionEditar,
            Function(u)
                Dim unidad = If(varianteId.HasValue,
                    u.EscalarLong("SELECT p.unidad_base_id FROM variante_producto v JOIN producto_base p ON p.id = v.producto_base_id WHERE v.id = @v", "v", varianteId.Value),
                    u.EscalarLong("SELECT id FROM unidad_medida WHERE codigo = 'UND'"))
                Return u.EscalarLong("INSERT INTO merma_produccion(empresa_id, produccion_id, variante_id, receta_version_id, etapa, cantidad_u6, unidad_id, motivo, ya_incluida_consumo, documento_baja_id) " &
                                     "VALUES (@e, @p, @v, @r, @et, @c, @un, @m, @y, @b) RETURNING id",
                                     "e", Sesion.EmpresaId, "p", produccionId, "v", Nulo(varianteId), "r", Nulo(recetaVersionId),
                                     "et", ServicioAdministracion.Requerido(etapa, "etapa"), "c", cantidadU6, "un", unidad,
                                     "m", ServicioAdministracion.Requerido(motivo, "motivo"), "y", If(yaIncluidaEnConsumo, 1L, 0L), "b", Nulo(documentoBajaId))
            End Function)
    End Function

    Public Function ProduccionDeMinuta(minutaId As Long) As Long?
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                Dim id = u.Escalar("SELECT id FROM produccion WHERE minuta_id = @m", "m", minutaId)
                Return If(id Is Nothing, CType(Nothing, Long?), CLng(id))
            End Function)
    End Function

    ''' <summary>Previsto vs real del servicio: raciones, consumo neto por producto (T31) y costo real.</summary>
    Public Function Reporte(minutaId As Long) As ReporteServicioDto
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                DatosMinuta(u, minutaId, Nothing)
                Dim r As New ReporteServicioDto()
                r.RacionesPrevistas = u.EscalarLong("SELECT comensales FROM minuta WHERE id = @m", "m", minutaId)
                Dim prod = u.Consultar("SELECT raciones_producidas, raciones_servidas, raciones_excedentes FROM produccion WHERE minuta_id = @m",
                                       Function(rd) (rd.GetInt64(0), rd.GetInt64(1), rd.GetInt64(2)), "m", minutaId).ToList()
                If prod.Count > 0 Then r.RacionesProducidas = prod(0).Item1 : r.RacionesServidas = prod(0).Item2 : r.RacionesExcedentes = prod(0).Item3
                Dim previsto = u.Consultar(
                    "SELECT bool_and(d.costo_previsto_racion_u6 IS NOT NULL), sum(d.costo_previsto_racion_u6 * d.raciones)::bigint FROM minuta_detalle d WHERE d.minuta_id = @m",
                    Function(rd) (Completo:=Not rd.IsDBNull(0) AndAlso rd.GetBoolean(0), Total:=If(rd.IsDBNull(1), 0L, rd.GetInt64(1))), "m", minutaId).Single()
                Dim fijos = u.Consultar("SELECT bool_and(costo_previsto_unitario_u6 IS NOT NULL), COALESCE(sum(round(cantidad_base_u6::numeric * costo_previsto_unitario_u6 / 1000000)), 0)::bigint " &
                                        "FROM minuta_estructura_fija WHERE minuta_id = @m",
                                        Function(rd) (Completo:=rd.IsDBNull(0) OrElse rd.GetBoolean(0), Total:=rd.GetInt64(1)), "m", minutaId).Single()
                r.CostoPrevistoU6 = If(previsto.Completo AndAlso fijos.Completo, previsto.Total + fijos.Total, CType(Nothing, Long?))

                Dim real = u.Consultar(
                    "SELECT v.producto_base_id, d.tipo, sum(l.cantidad_base_u6)::bigint, sum(l.valor_u6)::bigint FROM documento_stock d " &
                    "JOIN requerimiento q ON q.id = d.requerimiento_id JOIN documento_stock_detalle l ON l.documento_id = d.id " &
                    "JOIN variante_producto v ON v.id = l.variante_id WHERE q.minuta_id = @m AND d.estado = 'confirmado' " &
                    "AND d.tipo IN ('salida_produccion','devolucion_produccion') GROUP BY v.producto_base_id, d.tipo",
                    Function(rd) (Producto:=rd.GetInt64(0), Tipo:=rd.GetString(1), Cant:=rd.GetInt64(2), Valor:=rd.GetInt64(3)), "m", minutaId)
                Dim prevPorProducto = NecesidadMinuta(u, minutaId)
                Dim nombres = u.Consultar("SELECT p.id, p.descripcion, um.codigo FROM producto_base p JOIN unidad_medida um ON um.id = p.unidad_base_id WHERE p.id = ANY(@ids)",
                                          Function(rd) (rd.GetInt64(0), rd.GetString(1), rd.GetString(2)), "ids",
                                          prevPorProducto.Keys.Union(real.Select(Function(x) x.Producto)).ToArray()).ToDictionary(Function(x) x.Item1)
                For Each p In nombres.Keys
                    Dim ent = real.Where(Function(x) x.Producto = p AndAlso x.Tipo = "salida_produccion")
                    Dim dev = real.Where(Function(x) x.Producto = p AndAlso x.Tipo = "devolucion_produccion")
                    Dim c As New ConsumoProductoDto With {
                        .ProductoDescripcion = nombres(p).Item2, .Unidad = nombres(p).Item3, .PrevistoU6 = If(prevPorProducto.ContainsKey(p), prevPorProducto(p), 0),
                        .EntregadoU6 = ent.Sum(Function(x) x.Cant), .DevueltoU6 = dev.Sum(Function(x) x.Cant),
                        .CostoRealU6 = ent.Sum(Function(x) x.Valor) - dev.Sum(Function(x) x.Valor)}
                    c.NetoU6 = c.EntregadoU6 - c.DevueltoU6
                    c.DiferenciaU6 = c.NetoU6 - c.PrevistoU6
                    r.Consumo.Add(c)
                Next
                r.Consumo.Sort(Function(a, b) String.CompareOrdinal(a.ProductoDescripcion, b.ProductoDescripcion))
                r.CostoRealU6 = r.Consumo.Sum(Function(c) c.CostoRealU6)
                If r.RacionesServidas.HasValue AndAlso r.RacionesServidas.Value > 0 Then
                    r.CostoRealPorRacionServidaU6 = r.CostoRealU6 \ r.RacionesServidas.Value + If((r.CostoRealU6 Mod r.RacionesServidas.Value) * 2 >= r.RacionesServidas.Value, 1, 0)
                End If
                For Each mm In u.Consultar(
                    "SELECT m.etapa, m.cantidad_u6, um.codigo, m.motivo, m.ya_incluida_consumo FROM merma_produccion m JOIN produccion p ON p.id = m.produccion_id " &
                    "JOIN unidad_medida um ON um.id = m.unidad_id WHERE p.minuta_id = @m ORDER BY m.id",
                    Function(rd) $"{rd.GetString(0)}: {EscalaU6.ADecimal(rd.GetInt64(1)):0.######} {rd.GetString(2)} ({rd.GetString(3)}){If(rd.GetInt64(4) = 1, " - incluida en lo entregado", " - con baja de almacen")}",
                    "m", minutaId)
                    r.Mermas.Add(mm)
                Next
                Return r
            End Function)
    End Function

    ' ---------- Auxiliares ----------

    Private Function DatosMinuta(u As UnidadDeTrabajo, minutaId As Long, almacenId As Long?) As (Servicio As Long, Fecha As Date, Estado As String)
        Dim m = u.Consultar("SELECT m.operacion_servicio_id, m.fecha, m.estado FROM minuta m JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
                            "WHERE m.id = @m AND os.operacion_id = @o", Function(rd) (Servicio:=rd.GetInt64(0), Fecha:=rd.GetDateTime(1), Estado:=rd.GetString(2)),
                            "m", minutaId, "o", Sesion.Operacion.Id).SingleOrDefault()
        If m.Servicio = 0 Then Throw New ReglaNegocioException("OPERACION_AJENA", "La minuta no pertenece a la operacion seleccionada.")
        If almacenId.HasValue AndAlso u.Escalar("SELECT 1 FROM almacen WHERE id = @a AND operacion_id = @o", "a", almacenId.Value, "o", Sesion.Operacion.Id) Is Nothing Then
            Throw New ReglaNegocioException("OPERACION_AJENA", "El almacen no pertenece a la operacion seleccionada.")
        End If
        Return m
    End Function

    Private Function Insertar(u As UnidadDeTrabajo, almacenId As Long, minutaId As Long, servicio As Long, fecha As Date, tipo As String) As Long
        u.Ejecutar("SELECT pg_advisory_xact_lock(hashtext('requerimiento'), @e::int)", "e", Sesion.EmpresaId)
        Dim base = $"RQ-{Date.Today.Year}-"
        Dim n = u.EscalarLong("SELECT COALESCE(max(substring(numero from 9 for 5)::bigint), 0) + 1 FROM requerimiento WHERE numero LIKE @b", "b", base & "%")
        Return u.EscalarLong("INSERT INTO requerimiento(empresa_id, almacen_id, minuta_id, operacion_servicio_id, fecha, numero, usuario_id, tipo) " &
                             "VALUES (@e, @a, @m, @os, @f, @n, @u, @t) RETURNING id",
                             "e", Sesion.EmpresaId, "a", almacenId, "m", minutaId, "os", servicio, "f", fecha, "n", $"{base}{n:00000}", "u", Sesion.UsuarioId, "t", tipo)
    End Function

    Private Function ExigirRequerimiento(u As UnidadDeTrabajo, requerimientoId As Long) As (Almacen As Long, Servicio As Long, Numero As String)
        Dim r = u.Consultar("SELECT q.almacen_id, q.operacion_servicio_id, q.numero, q.estado FROM requerimiento q JOIN almacen a ON a.id = q.almacen_id " &
                            "WHERE q.id = @r AND a.operacion_id = @o FOR UPDATE OF q",
                            Function(rd) (Almacen:=rd.GetInt64(0), Servicio:=rd.GetInt64(1), Numero:=rd.GetString(2), Estado:=rd.GetString(3)),
                            "r", requerimientoId, "o", Sesion.Operacion.Id).SingleOrDefault()
        If r.Almacen = 0 Then Throw New ReglaNegocioException("OPERACION_AJENA", "El requerimiento no pertenece a la operacion seleccionada.")
        If r.Estado <> "borrador" Then Throw New ReglaNegocioException("REQUERIMIENTO_ATENDIDO", $"El requerimiento esta {r.Estado}.")
        Return (r.Almacen, r.Servicio, r.Numero)
    End Function

    Private Shared Function LeerLineas(u As UnidadDeTrabajo, requerimientoId As Long) As List(Of RequerimientoLineaDto)
        Return u.Consultar(
            "SELECT d.id, d.producto_base_id, p.descripcion, um.codigo, d.cantidad_prevista_u6, d.cantidad_solicitada_u6 FROM requerimiento_detalle d " &
            "JOIN producto_base p ON p.id = d.producto_base_id JOIN unidad_medida um ON um.id = p.unidad_base_id WHERE d.requerimiento_id = @r ORDER BY p.descripcion",
            Function(rd) New RequerimientoLineaDto With {.Id = rd.GetInt64(0), .ProductoBaseId = rd.GetInt64(1), .ProductoDescripcion = rd.GetString(2), .Unidad = rd.GetString(3),
                                                         .PrevistoU6 = rd.GetInt64(4), .SolicitadoU6 = rd.GetInt64(5)}, "r", requerimientoId)
    End Function

    ''' <summary>Necesidad de la minuta por producto base: Σ bruta × raciones / rendimiento + fijos.</summary>
    Private Shared Function NecesidadMinuta(u As UnidadDeTrabajo, minutaId As Long) As Dictionary(Of Long, Long)
        Return u.Consultar(
            "SELECT i.producto_base_id, i.cantidad_base_bruta_u6, rv.rendimiento_raciones_u6, d.raciones FROM minuta_detalle d " &
            "JOIN receta_version rv ON rv.id = d.receta_version_id JOIN receta_ingrediente i ON i.receta_version_id = rv.id WHERE d.minuta_id = @m " &
            "UNION ALL SELECT f.producto_base_id, f.cantidad_base_u6, NULL, NULL FROM minuta_estructura_fija f WHERE f.minuta_id = @m",
            Function(rd) (Producto:=rd.GetInt64(0), Cantidad:=If(rd.IsDBNull(2), rd.GetInt64(1),
                            Recetas.NecesidadIngredienteU6(rd.GetInt64(1), rd.GetInt64(2), rd.GetInt64(3) * EscalaU6.Factor))), "m", minutaId) _
            .GroupBy(Function(x) x.Producto).ToDictionary(Function(g) g.Key, Function(g) g.Sum(Function(x) x.Cantidad))
    End Function

    Private Sub VincularDocumentos(u As UnidadDeTrabajo, produccionId As Long, minutaId As Long)
        u.Ejecutar("INSERT INTO produccion_documento(empresa_id, produccion_id, documento_stock_id) " &
                   "SELECT d.empresa_id, @p, d.id FROM documento_stock d JOIN requerimiento q ON q.id = d.requerimiento_id " &
                   "WHERE q.minuta_id = @m AND d.estado = 'confirmado' AND d.tipo IN ('salida_produccion','devolucion_produccion') " &
                   "ON CONFLICT (empresa_id, documento_stock_id) DO NOTHING", "p", produccionId, "m", minutaId)
    End Sub

    Private Shared Function Nulo(valor As Long?) As Object
        Return If(valor.HasValue, CType(valor.Value, Object), Nothing)
    End Function

    ''' <summary>
    ''' Plan de producción del chef: fija las raciones a producir de un plato de minuta. Solo desde 3 días atrás en adelante
    ''' y mientras el día no esté cerrado (la base lo exige y deja el historial en produccion_plan_cambio).
    ''' </summary>
    Public Sub FijarRacionesProducir(minutaDetalleId As Long, raciones As Long)
        If raciones < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Las raciones a producir no pueden ser negativas.")
        EnTransaccion(Permisos.ProduccionEditar,
            Function(u)
                Return u.Ejecutar(
                    "INSERT INTO produccion_plan(empresa_id, minuta_detalle_id, raciones_producir, usuario_id) VALUES (@e, @d, @r, @u) " &
                    "ON CONFLICT (empresa_id, minuta_detalle_id) DO UPDATE SET raciones_producir = EXCLUDED.raciones_producir, usuario_id = EXCLUDED.usuario_id",
                    "e", Sesion.EmpresaId, "d", minutaDetalleId, "r", raciones, "u", Sesion.UsuarioId)
            End Function)
    End Sub

End Class
