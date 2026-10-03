Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

Public NotInheritable Class PendienteDto
    Public Property Codigo As String
    Public Property Detalle As String
    ''' <summary>True = impide cerrar; False = advertencia.</summary>
    Public Property Bloqueante As Boolean
End Class

Public NotInheritable Class ResultadoCierre
    Public Property Cerrado As Boolean
    Public ReadOnly Property Pendientes As New List(Of PendienteDto)
End Class

Public NotInheritable Class LineaReporteServicio
    Public Property OperacionServicioId As Long
    Public Property Servicio As String
    Public Property Regimen As String
    Public Property RacionesServidas As Long
    ''' <summary>Consumo neto de alimentos (entregas − devoluciones) a costo histórico.</summary>
    Public Property CostoAlimentosU6 As Long
    Public Property CostoPorRacionU6 As Long?
    Public Property IngresoU6 As Long?
    Public Property FoodCostU6 As Long?
    Public Property ObjetivoU6 As Long?
    Public Property DesviacionPuntosU6 As Long?
    Public Property PresupuestoU6 As Long?
    Public Property DiferenciaPresupuestoU6 As Long?
    Public Property Observacion As String
End Class

''' <summary>Envíos a la central de esta empresa (cola de la sede): lo guardado localmente aún no sincronizado.</summary>
Public NotInheritable Class EstadoEnvioDto
    Public Property Configurada As Boolean
    Public Property Pendientes As Long
    Public Property ConError As Long
    Public Property EnConflicto As Long
    Public Property UltimoEnvio As DateTimeOffset?

    Public Overrides Function ToString() As String
        If Not Configurada Then Return "Central: sede sin sincronizacion configurada"
        Return $"Central: {Pendientes} por enviar" & If(ConError > 0, $" ({ConError} con error de envio)", "") &
               If(EnConflicto > 0, $", {EnConflicto} en conflicto", "") &
               $"; ultimo envio {If(UltimoEnvio.HasValue, UltimoEnvio.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), "nunca")}"
    End Function
End Class

Public NotInheritable Class ReporteMensualDto
    Public Property Anio As Integer
    Public Property Mes As Integer
    Public Property Estado As String
    Public ReadOnly Property Servicios As New List(Of LineaReporteServicio)
    ''' <summary>Bajas de almacén (no atribuidas a un servicio).</summary>
    Public Property BajasU6 As Long
    ''' <summary>Ajustes de inventario: positivo = sobrante, negativo = faltante.</summary>
    Public Property AjusteInventarioU6 As Long
    Public Property TotalCostoAlimentosU6 As Long
    Public Property TotalIngresoU6 As Long
    Public Property FoodCostTotalU6 As Long?
End Class

''' <summary>
''' Cierres y control (módulo 6). Pendientes accionables por día; cierre diario serializado con las contabilizaciones
''' (bloquea los almacenes de la operación: una salida simultánea queda incluida antes o rechazada después, T38);
''' ingreso mensual por servicio (D13: importe neto mensual) y objetivo de Food Cost; reporte mensual rastreable a
''' documentos; cierre de mes con todos sus días con actividad cerrados. Un período cerrado no cambia (T48).
''' </summary>
Public NotInheritable Class ServicioCierres
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    Private ReadOnly Property Op As Long
        Get
            If Sesion.Operacion Is Nothing Then Throw New ReglaNegocioException("OPERACION_NO_SELECCIONADA", "Seleccione una operacion.")
            Return Sesion.Operacion.Id
        End Get
    End Property

    ' ---------- Día ----------

    Public Function Pendientes(fecha As Date) As List(Of PendienteDto)
        Dim o = Op
        Return EnTransaccion(Permisos.ReportesVer, Function(u) LeerPendientes(u, o, fecha.Date, fecha.Date))
    End Function

    ''' <summary>Cierra el día si no hay pendientes bloqueantes (T37). Guarda las validaciones del cierre.</summary>
    Public Function CerrarDia(fecha As Date) As ResultadoCierre
        Dim o = Op
        Return EnTransaccion(Permisos.CierreEjecutar,
            Function(u)
                ' T38: mismo bloqueo que toma cada contabilización → serializa cierre y salidas del día.
                u.Ejecutar("SELECT id FROM almacen WHERE operacion_id = @o ORDER BY id FOR UPDATE", "o", o)
                Dim r As New ResultadoCierre()
                Dim id = u.Escalar("SELECT id FROM cierre_diario WHERE operacion_id = @o AND fecha = @f FOR UPDATE", "o", o, "f", fecha.Date)
                If id IsNot Nothing AndAlso CStr(u.Escalar("SELECT estado FROM cierre_diario WHERE id = @i", "i", id)) = "cerrado" Then
                    Throw New ReglaNegocioException("PERIODO_CERRADO", $"El dia {fecha:dd/MM/yyyy} ya esta cerrado.")
                End If
                If id Is Nothing Then
                    id = u.EscalarLong("INSERT INTO cierre_diario(empresa_id, operacion_id, fecha) VALUES (@e, @o, @f) RETURNING id", "e", Sesion.EmpresaId, "o", o, "f", fecha.Date)
                End If
                r.Pendientes.AddRange(LeerPendientes(u, o, fecha.Date, fecha.Date))
                u.Ejecutar("DELETE FROM cierre_validacion WHERE cierre_diario_id = @c", "c", id)
                For Each p In r.Pendientes
                    u.Ejecutar("INSERT INTO cierre_validacion(empresa_id, cierre_diario_id, codigo, resultado, detalle) VALUES (@e, @c, @cod, @res, @d)",
                               "e", Sesion.EmpresaId, "c", id, "cod", p.Codigo, "res", If(p.Bloqueante, "error", "advertencia"), "d", p.Detalle)
                Next
                If r.Pendientes.Any(Function(p) p.Bloqueante) Then Return r
                u.Ejecutar("UPDATE cierre_diario SET estado = 'cerrado', usuario_cierre_id = @u, fecha_cierre = now() WHERE id = @c", "u", Sesion.UsuarioId, "c", id)
                r.Cerrado = True
                Return r
            End Function)
    End Function

    Public Function DiaCerrado(fecha As Date) As Boolean
        Dim o = Op
        Return EnTransaccion(Permisos.ReportesVer,
            Function(u) u.Escalar("SELECT 1 FROM cierre_diario WHERE operacion_id = @o AND fecha = @f AND estado = 'cerrado'", "o", o, "f", fecha.Date) IsNot Nothing)
    End Function

    ''' <summary>Estado de la cola de envío a la central (distingue confirmado localmente de sincronizado).</summary>
    Public Function EstadoEnvio() As EstadoEnvioDto
        Return EnTransaccion(Permisos.ReportesVer,
            Function(u) u.Consultar(
                "SELECT count(*) > 0, count(*) FILTER (WHERE estado IN ('pendiente','error')), count(*) FILTER (WHERE estado = 'error'), " &
                "count(*) FILTER (WHERE estado = 'conflicto'), max(enviado_en) FROM sincronizacion_evento",
                Function(rd) New EstadoEnvioDto With {
                    .Configurada = rd.GetBoolean(0), .Pendientes = rd.GetInt64(1), .ConError = rd.GetInt64(2), .EnConflicto = rd.GetInt64(3),
                    .UltimoEnvio = If(rd.IsDBNull(4), CType(Nothing, DateTimeOffset?), New DateTimeOffset(rd.GetDateTime(4)))}).Single())
    End Function

    ' ---------- Mes ----------

    ''' <summary>Ingreso neto mensual del servicio (D13). Solo en un mes abierto.</summary>
    Public Sub RegistrarIngreso(operacionServicioId As Long, anio As Integer, mes As Integer, importeNetoU6 As Long, ajustesU6 As Long, fuente As String)
        If importeNetoU6 < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "El importe no puede ser negativo.")
        Dim o = Op
        EnTransaccion(Permisos.CierreEjecutar,
            Function(u)
                ExigirServicio(u, operacionServicioId, o)
                Dim periodoId = Periodo(u, o, anio, mes)
                Return u.Ejecutar("INSERT INTO ingreso_servicio(empresa_id, operacion_servicio_id, periodo_id, importe_neto_u6, ajustes_u6, moneda, fuente) " &
                                  "VALUES (@e, @os, @p, @i, @a, 'PEN', @f) ON CONFLICT (empresa_id, operacion_servicio_id, periodo_id) " &
                                  "DO UPDATE SET importe_neto_u6 = EXCLUDED.importe_neto_u6, ajustes_u6 = EXCLUDED.ajustes_u6, fuente = EXCLUDED.fuente, origen = 'manual'",
                                  "e", Sesion.EmpresaId, "os", operacionServicioId, "p", periodoId, "i", importeNetoU6, "a", ajustesU6,
                                  "f", ServicioAdministracion.Requerido(fuente, "fuente del ingreso"))
            End Function)
    End Sub

    ''' <summary>Objetivo de Food Cost del servicio en puntos básicos (40 % = 4000).</summary>
    Public Sub FijarObjetivo(operacionServicioId As Long, objetivoBp As Long?)
        If objetivoBp.HasValue AndAlso (objetivoBp.Value < 0 OrElse objetivoBp.Value > 10000) Then Throw New ReglaNegocioException("DATO_INVALIDO", "El objetivo va de 0 % a 100 %.")
        Dim o = Op
        EnTransaccion(Permisos.CierreEjecutar,
            Function(u)
                ExigirServicio(u, operacionServicioId, o)
                Return u.Ejecutar("UPDATE operacion_servicio SET food_cost_objetivo_bp = @b WHERE id = @os",
                                  "b", If(objetivoBp.HasValue, CType(objetivoBp.Value, Object), Nothing), "os", operacionServicioId)
            End Function)
    End Sub

    ''' <summary>Reporte mensual por servicio: raciones, costo de alimentos, ingreso y Food Cost (T40/T41).</summary>
    Public Function ReporteMensual(anio As Integer, mes As Integer) As ReporteMensualDto
        Dim o = Op
        Return EnTransaccion(Permisos.ReportesVer, Function(u) LeerReporte(u, o, anio, mes))
    End Function

    ''' <summary>
    ''' Cierra el mes: todos los días con actividad deben estar cerrados y sin pendientes. Después, movimientos,
    ''' ingresos y gastos del mes no cambian y el reporte se repite igual (T48).
    ''' </summary>
    Public Function CerrarMes(anio As Integer, mes As Integer) As ResultadoCierre
        Dim o = Op
        Return EnTransaccion(Permisos.CierreEjecutar,
            Function(u)
                u.Ejecutar("SELECT id FROM almacen WHERE operacion_id = @o ORDER BY id FOR UPDATE", "o", o)
                Dim id = Periodo(u, o, anio, mes)
                Dim desde As New Date(anio, mes, 1), hasta = New Date(anio, mes, 1).AddMonths(1).AddDays(-1)
                Dim r As New ResultadoCierre()
                r.Pendientes.AddRange(LeerPendientes(u, o, desde, hasta))
                For Each f In u.Consultar(
                    "SELECT DISTINCT f FROM (SELECT m.fecha AS f FROM movimiento_stock m JOIN almacen a ON a.id = m.almacen_id WHERE a.operacion_id = @o AND m.fecha BETWEEN @d AND @h " &
                    "UNION SELECT mi.fecha FROM minuta mi JOIN operacion_servicio os ON os.id = mi.operacion_servicio_id WHERE os.operacion_id = @o AND mi.fecha BETWEEN @d AND @h) x " &
                    "WHERE NOT EXISTS (SELECT 1 FROM cierre_diario c WHERE c.operacion_id = @o AND c.fecha = x.f AND c.estado = 'cerrado') ORDER BY f",
                    Function(rd) rd.GetDateTime(0), "o", o, "d", desde, "h", hasta)
                    r.Pendientes.Add(New PendienteDto With {.Codigo = "DIA_ABIERTO", .Detalle = $"El dia {f:dd/MM/yyyy} tiene actividad y no esta cerrado", .Bloqueante = True})
                Next
                For Each s In LeerReporte(u, o, anio, mes).Servicios.Where(Function(x) x.IngresoU6 Is Nothing AndAlso x.CostoAlimentosU6 > 0)
                    r.Pendientes.Add(New PendienteDto With {.Codigo = "INGRESO_FALTANTE", .Detalle = $"{s.Servicio} - {s.Regimen}: sin ingreso del mes (Food Cost no calculable)", .Bloqueante = False})
                Next
                u.Ejecutar("DELETE FROM cierre_validacion WHERE periodo_id = @p", "p", id)
                For Each p In r.Pendientes
                    u.Ejecutar("INSERT INTO cierre_validacion(empresa_id, periodo_id, codigo, resultado, detalle) VALUES (@e, @p, @c, @r, @d)",
                               "e", Sesion.EmpresaId, "p", id, "c", p.Codigo, "r", If(p.Bloqueante, "error", "advertencia"), "d", p.Detalle)
                Next
                If r.Pendientes.Any(Function(p) p.Bloqueante) Then Return r
                u.Ejecutar("UPDATE periodo_mensual SET estado = 'cerrado', metodo_valoracion = 'promedio_movil', usuario_cierre_id = @u, fecha_cierre = now() WHERE id = @p",
                           "u", Sesion.UsuarioId, "p", id)
                r.Cerrado = True
                Return r
            End Function)
    End Function

    ' ---------- Auxiliares ----------

    Private Function LeerPendientes(u As UnidadDeTrabajo, o As Long, desde As Date, hasta As Date) As List(Of PendienteDto)
        Dim p As New List(Of PendienteDto)
        Dim agregar = Sub(codigo As String, bloqueante As Boolean, sql As String)
                          For Each d In u.Consultar(sql, Function(rd) rd.GetString(0), "o", o, "d", desde, "h", hasta)
                              p.Add(New PendienteDto With {.Codigo = codigo, .Detalle = d, .Bloqueante = bloqueante})
                          Next
                      End Sub
        agregar("MINUTA_SIN_APROBAR", True,
            "SELECT 'Minuta ' || s.nombre || ' del ' || to_char(m.fecha, 'DD/MM/YYYY') || ' en borrador' FROM minuta m JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
            "JOIN servicio s ON s.id = os.servicio_id WHERE os.operacion_id = @o AND m.fecha BETWEEN @d AND @h AND m.estado = 'borrador' ORDER BY m.fecha")
        agregar("PRODUCCION_SIN_REGISTRAR", True,
            "SELECT 'Minuta ' || s.nombre || ' del ' || to_char(m.fecha, 'DD/MM/YYYY') || ' sin raciones producidas/servidas' FROM minuta m " &
            "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id JOIN servicio s ON s.id = os.servicio_id " &
            "WHERE os.operacion_id = @o AND m.fecha BETWEEN @d AND @h AND m.estado = 'aprobada' AND NOT EXISTS (SELECT 1 FROM produccion pr WHERE pr.minuta_id = m.id) ORDER BY m.fecha")
        agregar("REQUERIMIENTO_PENDIENTE", True,
            "SELECT 'Requerimiento ' || q.numero || ' sin entregar' FROM requerimiento q JOIN almacen a ON a.id = q.almacen_id " &
            "WHERE a.operacion_id = @o AND q.fecha BETWEEN @d AND @h AND q.estado = 'borrador' ORDER BY q.numero")
        agregar("DOCUMENTO_BORRADOR", True,
            "SELECT 'Documento ' || d.numero || ' en borrador' FROM documento_stock d JOIN almacen a ON a.id = d.almacen_id " &
            "WHERE a.operacion_id = @o AND d.fecha BETWEEN @d AND @h AND d.estado = 'borrador'")
        agregar("RECEPCION_BORRADOR", True,
            "SELECT 'Recepcion ' || r.numero || ' en borrador' FROM recepcion r JOIN almacen a ON a.id = r.almacen_id " &
            "WHERE a.operacion_id = @o AND r.fecha_recepcion BETWEEN @d AND @h AND r.estado = 'borrador'")
        agregar("INVENTARIO_ABIERTO", True,
            "SELECT 'Inventario ' || i.numero || ' (' || i.estado || ') con diferencias sin tratar' FROM inventario i JOIN almacen a ON a.id = i.almacen_id " &
            "WHERE a.operacion_id = @o AND i.fecha_corte <= @h AND i.estado <> 'cerrado'")
        agregar("CONCILIACION", True,
            "SELECT 'Saldo y movimientos no coinciden: almacen ' || a.codigo || ', variante ' || v.codigo FROM v_conciliacion_saldo c " &
            "JOIN almacen a ON a.id = c.almacen_id JOIN variante_producto v ON v.id = c.variante_id WHERE a.operacion_id = @o")
        agregar("PEDIDO_VENCIDO", False,
            "SELECT 'Pedido ' || p.numero || ' con entrega vencida y saldo pendiente' FROM pedido_compra p JOIN almacen a ON a.id = p.almacen_id " &
            "WHERE a.operacion_id = @o AND p.estado IN ('aprobado','enviado','parcial') AND EXISTS (SELECT 1 FROM pedido_detalle d WHERE d.pedido_id = p.id AND d.fecha_entrega <= @h)")
        Return p
    End Function

    Friend Shared Function LeerReporte(u As UnidadDeTrabajo, o As Long, anio As Integer, mes As Integer) As ReporteMensualDto
        Dim desde As New Date(anio, mes, 1), hasta = New Date(anio, mes, 1).AddMonths(1).AddDays(-1)
        Dim r As New ReporteMensualDto With {.Anio = anio, .Mes = mes}
        r.Estado = CStr(If(u.Escalar("SELECT estado FROM periodo_mensual WHERE operacion_id = @o AND anio = @a AND mes = @m", "o", o, "a", anio, "m", mes), "abierto"))
        For Each s In u.Consultar(
            "SELECT os.id, s.nombre, rg.nombre, os.food_cost_objetivo_bp, " &
            "  COALESCE((SELECT sum(pr.raciones_servidas) FROM produccion pr WHERE pr.operacion_servicio_id = os.id AND pr.fecha BETWEEN @d AND @h), 0)::bigint, " &
            "  COALESCE((SELECT sum(CASE WHEN dc.tipo = 'salida_produccion' THEN l.valor_u6 ELSE -l.valor_u6 END) FROM documento_stock dc " &
            "            JOIN documento_stock_detalle l ON l.documento_id = dc.id WHERE dc.operacion_servicio_id = os.id AND dc.estado = 'confirmado' " &
            "            AND dc.tipo IN ('salida_produccion','devolucion_produccion') AND dc.fecha BETWEEN @d AND @h), 0)::bigint, " &
            "  (SELECT i.importe_neto_u6 + i.ajustes_u6 FROM ingreso_servicio i JOIN periodo_mensual pm ON pm.id = i.periodo_id " &
            "    WHERE i.operacion_servicio_id = os.id AND pm.anio = @a AND pm.mes = @m) " &
            "FROM operacion_servicio os JOIN servicio s ON s.id = os.servicio_id JOIN regimen rg ON rg.id = os.regimen_id WHERE os.operacion_id = @o ORDER BY s.nombre, rg.nombre",
            Function(rd) (Id:=rd.GetInt64(0), Servicio:=rd.GetString(1), Regimen:=rd.GetString(2), Objetivo:=If(rd.IsDBNull(3), CType(Nothing, Long?), rd.GetInt64(3)),
                          Raciones:=rd.GetInt64(4), Costo:=rd.GetInt64(5), Ingreso:=If(rd.IsDBNull(6), CType(Nothing, Long?), rd.GetInt64(6))),
            "o", o, "d", desde, "h", hasta, "a", anio, "m", mes)
            Dim fc = FoodCost.Calcular(s.Costo, If(s.Ingreso, 0L), s.Objetivo)
            r.Servicios.Add(New LineaReporteServicio With {
                .OperacionServicioId = s.Id, .Servicio = s.Servicio, .Regimen = s.Regimen, .RacionesServidas = s.Raciones, .CostoAlimentosU6 = s.Costo,
                .CostoPorRacionU6 = If(s.Raciones > 0, EscalaU6.MultiplicarDividir(s.Costo, 1, s.Raciones), CType(Nothing, Long?)),
                .IngresoU6 = s.Ingreso, .FoodCostU6 = fc.PorcentajeU6, .ObjetivoU6 = If(s.Objetivo.HasValue, s.Objetivo.Value * 10000L, CType(Nothing, Long?)),
                .DesviacionPuntosU6 = fc.DesviacionPuntosU6, .PresupuestoU6 = fc.PresupuestoU6, .DiferenciaPresupuestoU6 = fc.DiferenciaPresupuestoU6,
                .Observacion = If(fc.Calculable, "", If(s.Ingreso.HasValue, "Ingreso cero o negativo: Food Cost no calculable", "Sin ingreso registrado: Food Cost no calculable"))})
        Next
        Dim otros = u.Consultar(
            "SELECT COALESCE(sum(l.valor_u6) FILTER (WHERE d.tipo = 'baja'), 0)::bigint, " &
            "       COALESCE(sum(CASE d.tipo WHEN 'ajuste_positivo' THEN l.valor_u6 WHEN 'ajuste_negativo' THEN -l.valor_u6 ELSE 0 END), 0)::bigint " &
            "FROM documento_stock d JOIN almacen a ON a.id = d.almacen_id JOIN documento_stock_detalle l ON l.documento_id = d.id " &
            "WHERE a.operacion_id = @o AND d.estado = 'confirmado' AND d.fecha BETWEEN @d AND @h",
            Function(rd) (rd.GetInt64(0), rd.GetInt64(1)), "o", o, "d", desde, "h", hasta).Single()
        r.BajasU6 = otros.Item1
        r.AjusteInventarioU6 = otros.Item2
        r.TotalCostoAlimentosU6 = r.Servicios.Sum(Function(s) s.CostoAlimentosU6)
        r.TotalIngresoU6 = r.Servicios.Sum(Function(s) If(s.IngresoU6, 0L))
        r.FoodCostTotalU6 = FoodCost.Calcular(r.TotalCostoAlimentosU6, r.TotalIngresoU6, Nothing).PorcentajeU6
        Return r
    End Function

    Private Function Periodo(u As UnidadDeTrabajo, o As Long, anio As Integer, mes As Integer) As Long
        Return PeriodoAbierto(u, Sesion.EmpresaId, o, anio, mes)
    End Function

    ''' <summary>Id del período (lo crea si no existe) bloqueado; PERIODO_CERRADO si ya se cerró.</summary>
    Friend Shared Function PeriodoAbierto(u As UnidadDeTrabajo, empresaId As Long, o As Long, anio As Integer, mes As Integer) As Long
        If mes < 1 OrElse mes > 12 Then Throw New ReglaNegocioException("DATO_INVALIDO", "Mes invalido.")
        u.Ejecutar("INSERT INTO periodo_mensual(empresa_id, operacion_id, anio, mes) VALUES (@e, @o, @a, @m) ON CONFLICT (empresa_id, operacion_id, anio, mes) DO NOTHING",
                   "e", empresaId, "o", o, "a", anio, "m", mes)
        Dim p = u.Consultar("SELECT id, estado FROM periodo_mensual WHERE operacion_id = @o AND anio = @a AND mes = @m FOR UPDATE",
                            Function(rd) (rd.GetInt64(0), rd.GetString(1)), "o", o, "a", anio, "m", mes).Single()
        If p.Item2 = "cerrado" Then Throw New ReglaNegocioException("PERIODO_CERRADO", $"El mes {mes:00}/{anio} ya esta cerrado.")
        Return p.Item1
    End Function

    Friend Shared Sub ExigirServicio(u As UnidadDeTrabajo, operacionServicioId As Long, o As Long)
        If u.Escalar("SELECT 1 FROM operacion_servicio WHERE id = @os AND operacion_id = @o", "os", operacionServicioId, "o", o) Is Nothing Then
            Throw New ReglaNegocioException("OPERACION_AJENA", "El servicio no pertenece a la operacion seleccionada.")
        End If
    End Sub

End Class
