Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

Public NotInheritable Class ComponenteComparadoDto
    Public Property PlatoId As Long?
    Public Property Estructura As String
    Public Property Receta As String
    Public Property FactorPlanBp As Long?
    Public Property RacionesPlan As Long
    Public Property RacionesPreparadas As Long?
    Public Property RacionesConsumidas As Long?
    ''' <summary>Consumidas / comensales reales (raciones servidas, o vendidas si no hay producción).</summary>
    Public Property FactorRealBp As Long?
    Public Property CostoRacionU6 As Long?
    Public Property CostoPlanU6 As Long?
    ''' <summary>Costo teórico de lo realmente consumido (costo por ración × consumidas).</summary>
    Public Property CostoTeoricoConsumidoU6 As Long?
    Public ReadOnly Property FactorPlanTexto As String
        Get
            Return If(FactorPlanBp.HasValue, $"{FactorPlanBp.Value / 100D:0.##} %", "")
        End Get
    End Property
    Public ReadOnly Property FactorRealTexto As String
        Get
            Return If(FactorRealBp.HasValue, $"{FactorRealBp.Value / 100D:0.##} %", "")
        End Get
    End Property
End Class

Public NotInheritable Class ProductoComparadoDto
    Public Property ProductoBaseId As Long
    Public Property Producto As String
    Public Property Unidad As String
    Public Property CantidadTeoricaU6 As Long
    Public Property CostoTeoricoU6 As Long?
    ''' <summary>Lo que salió del almacén para el servicio y lo que volvió (devoluciones de cocina).</summary>
    Public Property EntregadoU6 As Long
    Public Property DevueltoU6 As Long
    ''' <summary>Consumo real neto = entregado − devuelto.</summary>
    Public Property CantidadRealU6 As Long
    Public Property CostoRealU6 As Long
    Public ReadOnly Property DiferenciaCantidadU6 As Long
        Get
            Return CantidadRealU6 - CantidadTeoricaU6
        End Get
    End Property
    Public ReadOnly Property DiferenciaCostoU6 As Long?
        Get
            Return If(CostoTeoricoU6.HasValue, CostoRealU6 - CostoTeoricoU6.Value, CType(Nothing, Long?))
        End Get
    End Property
    ''' <summary>"planificado", "no planificado" (salió del almacén sin estar en la minuta) o "sin salida" (planificado y no entregado).</summary>
    Public Property Estado As String
End Class

''' <summary>Planificado (teórico) vs realizado de un servicio: una minuta o un mes.</summary>
Public NotInheritable Class ComparativoDto
    Public Property Titulo As String
    Public Property Minutas As Integer
    ' Raciones
    Public Property ComensalesPlan As Long
    Public Property RacionesPreparadas As Long?
    Public Property RacionesConsumidas As Long?
    Public Property RacionesExcedentes As Long?
    Public Property RacionesVendidas As Long?
    Public ReadOnly Property RacionesNoVendidas As Long?
        Get
            Return If(RacionesVendidas.HasValue, ComensalesPlan - RacionesVendidas.Value, CType(Nothing, Long?))
        End Get
    End Property
    ' Venta
    Public Property VentaTeoricaU6 As Long?
    Public Property VentaRealU6 As Long?
    Public ReadOnly Property DiferenciaVentaU6 As Long?
        Get
            Return If(VentaTeoricaU6.HasValue AndAlso VentaRealU6.HasValue, VentaRealU6.Value - VentaTeoricaU6.Value, CType(Nothing, Long?))
        End Get
    End Property
    ' Costo
    Public Property CostoTeoricoU6 As Long?
    Public Property CostoTeoricoConsumidoU6 As Long?
    Public Property CostoRealU6 As Long
    Public ReadOnly Property DiferenciaCostoU6 As Long?
        Get
            Return If(CostoTeoricoU6.HasValue, CostoRealU6 - CostoTeoricoU6.Value, CType(Nothing, Long?))
        End Get
    End Property
    Public Property FoodCostObjetivoBp As Long
    Public ReadOnly Property FoodCostTeoricoU6 As Long?
        Get
            If Not CostoTeoricoU6.HasValue OrElse Not VentaTeoricaU6.HasValue Then Return Nothing
            Return FoodCost.Calcular(CostoTeoricoU6.Value, VentaTeoricaU6.Value, Nothing).PorcentajeU6
        End Get
    End Property
    ''' <summary>Costo real / venta real (si no se cargó la venta real, contra la venta teórica).</summary>
    Public ReadOnly Property FoodCostRealU6 As Long?
        Get
            Dim venta = If(VentaRealU6, VentaTeoricaU6)
            If Not venta.HasValue Then Return Nothing
            Return FoodCost.Calcular(CostoRealU6, venta.Value, Nothing).PorcentajeU6
        End Get
    End Property
    Public ReadOnly Property Componentes As New List(Of ComponenteComparadoDto)
    Public ReadOnly Property Productos As New List(Of ProductoComparadoDto)
    ''' <summary>Mermas registradas en producción (analíticas o con baja de almacén).</summary>
    Public ReadOnly Property Mermas As New List(Of String)
End Class

Public NotInheritable Class FactorRealDto
    Public Property EstructuraId As Long
    Public Property Estructura As String
    Public Property FactorTeoricoBp As Long
    Public Property FactorVigenteBp As Long
    Public Property RacionesPlan As Long
    Public Property RacionesConsumidas As Long
    Public Property ComensalesReales As Long
    ''' <summary>Σ consumidas / Σ comensales reales en el período; Nothing si no hay consumo registrado.</summary>
    Public Property FactorRealBp As Long?
End Class

''' <summary>
''' Planificado vs realizado del servicio. Lo planificado es la minuta aprobada (comensales, raciones por factor,
''' costo y venta previstos). Lo real: producción (raciones preparadas/servidas), consumo por componente, venta cargada
''' y lo que salió del almacén para el servicio (entregas − devoluciones), incluidos productos no planificados.
''' </summary>
Public NotInheritable Class ServicioComparativo
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

    ' ---------- Registro de lo real ----------

    ''' <summary>
    ''' Venta real del servicio: raciones vendidas e importe. Sin importe, se valoriza con el precio por comensal de la
    ''' minuta (venta prevista / comensales).
    ''' </summary>
    Public Sub RegistrarVenta(minutaId As Long, racionesVendidas As Long, importeU6 As Long?, fuente As String)
        If racionesVendidas < 0 OrElse (importeU6.HasValue AndAlso importeU6.Value < 0) Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Raciones e importe no pueden ser negativos.")
        Dim o = Op
        EnTransaccion(Permisos.ProduccionEditar,
            Function(u)
                Dim m = Minuta(u, minutaId, o)
                Dim importe = importeU6
                If Not importe.HasValue Then
                    If Not m.Venta.HasValue OrElse m.Comensales = 0 Then
                        Throw New ReglaNegocioException("VENTA_SIN_PRECIO", "La minuta no tiene precio de venta (costo pendiente); indique el importe.")
                    End If
                    importe = EscalaU6.MultiplicarDividir(m.Venta.Value, racionesVendidas, m.Comensales)
                End If
                Return u.Ejecutar("INSERT INTO venta_servicio(empresa_id, minuta_id, raciones_vendidas, importe_u6, fuente, usuario_id) VALUES (@e, @m, @r, @i, @f, @u) " &
                                  "ON CONFLICT (empresa_id, minuta_id) DO UPDATE SET raciones_vendidas = EXCLUDED.raciones_vendidas, importe_u6 = EXCLUDED.importe_u6, " &
                                  "fuente = EXCLUDED.fuente, usuario_id = EXCLUDED.usuario_id",
                                  "e", Sesion.EmpresaId, "m", minutaId, "r", racionesVendidas, "i", importe.Value,
                                  "f", If(String.IsNullOrWhiteSpace(fuente), If(importeU6.HasValue, "Registro", "Raciones x precio por comensal"), fuente.Trim()), "u", Sesion.UsuarioId)
            End Function)
    End Sub

    ''' <summary>Raciones realmente preparadas y consumidas de un componente (plato) de la minuta.</summary>
    Public Sub RegistrarConsumo(platoId As Long, preparadas As Long, consumidas As Long)
        If preparadas < 0 OrElse consumidas < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Las raciones no pueden ser negativas.")
        If consumidas > preparadas Then Throw New ReglaNegocioException("RACIONES_INCOHERENTES", "No se consume mas de lo preparado.")
        Dim o = Op
        EnTransaccion(Permisos.ProduccionEditar,
            Function(u)
                Minuta(u, u.EscalarLong("SELECT minuta_id FROM minuta_detalle WHERE id = @d", "d", platoId), o)
                Return u.Ejecutar("INSERT INTO consumo_plato(empresa_id, minuta_detalle_id, raciones_preparadas, raciones_consumidas, usuario_id) VALUES (@e, @d, @p, @c, @u) " &
                                  "ON CONFLICT (empresa_id, minuta_detalle_id) DO UPDATE SET raciones_preparadas = EXCLUDED.raciones_preparadas, " &
                                  "raciones_consumidas = EXCLUDED.raciones_consumidas, usuario_id = EXCLUDED.usuario_id",
                                  "e", Sesion.EmpresaId, "d", platoId, "p", preparadas, "c", consumidas, "u", Sesion.UsuarioId)
            End Function)
    End Sub

    ' ---------- Comparativos ----------

    Public Function Comparativo(minutaId As Long) As ComparativoDto
        Dim o = Op
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                Dim m = Minuta(u, minutaId, o)
                Dim real = u.Consultar(
                    "SELECT v.producto_base_id, sum(CASE d.tipo WHEN 'salida_produccion' THEN l.cantidad_base_u6 ELSE -l.cantidad_base_u6 END)::bigint, " &
                    "       sum(CASE d.tipo WHEN 'salida_produccion' THEN l.valor_u6 ELSE -l.valor_u6 END)::bigint, " &
                    "       COALESCE(sum(l.cantidad_base_u6) FILTER (WHERE d.tipo = 'devolucion_produccion'), 0)::bigint " &
                    "FROM documento_stock d JOIN documento_stock_detalle l ON l.documento_id = d.id JOIN variante_producto v ON v.id = l.variante_id " &
                    "LEFT JOIN requerimiento q ON q.id = d.requerimiento_id " &
                    "WHERE d.estado = 'confirmado' AND d.tipo IN ('salida_produccion','devolucion_produccion') " &
                    "  AND (q.minuta_id = @m OR (d.requerimiento_id IS NULL AND d.operacion_servicio_id = @os AND d.fecha = @f)) GROUP BY v.producto_base_id",
                    Function(rd) (rd.GetInt64(0), rd.GetInt64(1), rd.GetInt64(2), rd.GetInt64(3)), "m", minutaId, "os", m.Servicio, "f", m.Fecha)
                Return Armar(u, $"Minuta {m.Fecha:dd/MM/yyyy}", {minutaId}, real)
            End Function)
    End Function

    ''' <summary>Comparativo del mes de un servicio: todas sus minutas aprobadas y todo lo que salió del almacén para él.</summary>
    Public Function ComparativoMes(operacionServicioId As Long, anio As Integer, mes As Integer) As ComparativoDto
        Dim o = Op
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                ServicioCierres.ExigirServicio(u, operacionServicioId, o)
                Dim desde As New Date(anio, mes, 1), hasta = New Date(anio, mes, 1).AddMonths(1).AddDays(-1)
                Dim ids = u.Consultar("SELECT id FROM minuta WHERE operacion_servicio_id = @os AND estado IN ('aprobada','cerrada') AND fecha BETWEEN @d AND @h ORDER BY fecha",
                                      Function(rd) rd.GetInt64(0), "os", operacionServicioId, "d", desde, "h", hasta).ToArray()
                Dim real = u.Consultar(
                    "SELECT v.producto_base_id, sum(CASE d.tipo WHEN 'salida_produccion' THEN l.cantidad_base_u6 ELSE -l.cantidad_base_u6 END)::bigint, " &
                    "       sum(CASE d.tipo WHEN 'salida_produccion' THEN l.valor_u6 ELSE -l.valor_u6 END)::bigint, " &
                    "       COALESCE(sum(l.cantidad_base_u6) FILTER (WHERE d.tipo = 'devolucion_produccion'), 0)::bigint " &
                    "FROM documento_stock d JOIN documento_stock_detalle l ON l.documento_id = d.id JOIN variante_producto v ON v.id = l.variante_id " &
                    "WHERE d.estado = 'confirmado' AND d.tipo IN ('salida_produccion','devolucion_produccion') AND d.operacion_servicio_id = @os " &
                    "  AND d.fecha BETWEEN @d AND @h GROUP BY v.producto_base_id",
                    Function(rd) (rd.GetInt64(0), rd.GetInt64(1), rd.GetInt64(2), rd.GetInt64(3)), "os", operacionServicioId, "d", desde, "h", hasta)
                Return Armar(u, $"Mes {mes:00}/{anio}", ids, real)
            End Function)
    End Function

    ''' <summary>
    ''' Factor real de cada componente en un período (Σ raciones consumidas / Σ comensales reales), para que la operación
    ''' actualice sus factores con lo que realmente se consume.
    ''' </summary>
    Public Function FactoresReales(operacionServicioId As Long, desde As Date, hasta As Date) As List(Of FactorRealDto)
        Dim o = Op
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                ServicioCierres.ExigirServicio(u, operacionServicioId, o)
                Dim filas = u.Consultar(
                    "SELECT es.id, es.nombre, es.factor_consumo_bp, COALESCE(fo.factor_consumo_bp, es.factor_consumo_bp), " &
                    "  COALESCE(sum(d.raciones), 0)::bigint, COALESCE(sum(c.raciones_consumidas), 0)::bigint, " &
                    "  COALESCE(sum(CASE WHEN c.id IS NOT NULL THEN COALESCE(pr.raciones_servidas, vs.raciones_vendidas, m.comensales) END), 0)::bigint " &
                    "FROM estructura_servicio es JOIN operacion_servicio os ON os.servicio_id = es.servicio_id AND os.id = @os " &
                    "LEFT JOIN factor_consumo_operacion fo ON fo.operacion_servicio_id = os.id AND fo.estructura_id = es.id " &
                    "LEFT JOIN minuta m ON m.operacion_servicio_id = os.id AND m.estado IN ('aprobada','cerrada') AND m.fecha BETWEEN @d AND @h " &
                    "LEFT JOIN minuta_detalle d ON d.minuta_id = m.id AND d.estructura_id = es.id " &
                    "LEFT JOIN consumo_plato c ON c.minuta_detalle_id = d.id " &
                    "LEFT JOIN produccion pr ON pr.minuta_id = m.id LEFT JOIN venta_servicio vs ON vs.minuta_id = m.id " &
                    "GROUP BY es.id, es.nombre, es.orden, es.factor_consumo_bp, fo.factor_consumo_bp ORDER BY es.orden, es.id",
                    Function(rd) New FactorRealDto With {
                        .EstructuraId = rd.GetInt64(0), .Estructura = rd.GetString(1), .FactorTeoricoBp = rd.GetInt64(2), .FactorVigenteBp = rd.GetInt64(3),
                        .RacionesPlan = rd.GetInt64(4), .RacionesConsumidas = rd.GetInt64(5), .ComensalesReales = rd.GetInt64(6)},
                    "os", operacionServicioId, "d", desde.Date, "h", hasta.Date)
                ' Los comensales reales se cuentan una vez por minuta, aunque el componente tenga varias alternativas.
                For Each f In filas
                    Dim base = u.EscalarLong(
                        "SELECT COALESCE(sum(COALESCE(pr.raciones_servidas, vs.raciones_vendidas, m.comensales)), 0)::bigint FROM minuta m " &
                        "LEFT JOIN produccion pr ON pr.minuta_id = m.id LEFT JOIN venta_servicio vs ON vs.minuta_id = m.id " &
                        "WHERE m.operacion_servicio_id = @os AND m.estado IN ('aprobada','cerrada') AND m.fecha BETWEEN @d AND @h " &
                        "AND EXISTS (SELECT 1 FROM minuta_detalle d JOIN consumo_plato c ON c.minuta_detalle_id = d.id WHERE d.minuta_id = m.id AND d.estructura_id = @es)",
                        "os", operacionServicioId, "d", desde.Date, "h", hasta.Date, "es", f.EstructuraId)
                    f.ComensalesReales = base
                    If base > 0 Then f.FactorRealBp = Math.Min(10000L, EscalaU6.MultiplicarDividir(f.RacionesConsumidas, 10000, base))
                Next
                Return filas
            End Function)
    End Function

    ' ---------- Auxiliares ----------

    Private Function Minuta(u As UnidadDeTrabajo, minutaId As Long, o As Long) As (Servicio As Long, Fecha As Date, Comensales As Long, Venta As Long?)
        Dim m = u.Consultar("SELECT m.operacion_servicio_id, m.fecha, m.comensales, m.venta_prevista_u6, m.estado FROM minuta m " &
                            "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id WHERE m.id = @m AND os.operacion_id = @o",
                            Function(rd) (Servicio:=rd.GetInt64(0), Fecha:=rd.GetDateTime(1), Comensales:=rd.GetInt64(2),
                                          Venta:=If(rd.IsDBNull(3), CType(Nothing, Long?), rd.GetInt64(3)), Estado:=rd.GetString(4)),
                            "m", minutaId, "o", o).SingleOrDefault()
        If m.Servicio = 0 Then Throw New ReglaNegocioException("OPERACION_AJENA", "La minuta no pertenece a la operacion seleccionada.")
        If m.Estado = "borrador" Then Throw New ReglaNegocioException("MINUTA_NO_APROBADA", "El comparativo y lo real se registran sobre una minuta aprobada.")
        Return (m.Servicio, m.Fecha, m.Comensales, m.Venta)
    End Function

    Private Shared Function Armar(u As UnidadDeTrabajo, titulo As String, minutaIds As Long(), real As List(Of (Long, Long, Long, Long))) As ComparativoDto
        Dim r As New ComparativoDto With {.Titulo = titulo, .Minutas = minutaIds.Length}
        Using cmd = u.Comando(
                "SELECT COALESCE(sum(m.comensales), 0)::bigint, COALESCE(bool_and(m.costo_previsto_u6 IS NOT NULL), true), sum(m.costo_previsto_u6)::bigint, " &
                "  COALESCE(bool_and(m.venta_prevista_u6 IS NOT NULL), true), sum(m.venta_prevista_u6)::bigint, max(m.food_cost_objetivo_bp), " &
                "  count(pr.id), sum(pr.raciones_producidas)::bigint, sum(pr.raciones_servidas)::bigint, sum(pr.raciones_excedentes)::bigint, " &
                "  count(vs.id), sum(vs.raciones_vendidas)::bigint, sum(vs.importe_u6)::bigint " &
                "FROM minuta m LEFT JOIN produccion pr ON pr.minuta_id = m.id LEFT JOIN venta_servicio vs ON vs.minuta_id = m.id WHERE m.id = ANY(@ids)",
                "ids", minutaIds),
              rd = cmd.ExecuteReader()
            rd.Read()
            Dim n = minutaIds.Length
            r.ComensalesPlan = rd.GetInt64(0)
            r.CostoTeoricoU6 = If(n > 0 AndAlso rd.GetBoolean(1), rd.GetInt64(2), CType(Nothing, Long?))
            r.VentaTeoricaU6 = If(n > 0 AndAlso rd.GetBoolean(3), rd.GetInt64(4), CType(Nothing, Long?))
            r.FoodCostObjetivoBp = If(rd.IsDBNull(5), VentaEstructura.ObjetivoPorDefectoBp, rd.GetInt64(5))
            If rd.GetInt64(6) > 0 Then r.RacionesPreparadas = rd.GetInt64(7) : r.RacionesConsumidas = rd.GetInt64(8) : r.RacionesExcedentes = rd.GetInt64(9)
            If rd.GetInt64(10) > 0 Then r.RacionesVendidas = rd.GetInt64(11) : r.VentaRealU6 = rd.GetInt64(12)
        End Using

        ' Componentes: plan vs real por plato.
        For Each c In u.Consultar(
            "SELECT d.id, es.nombre, rc.nombre, d.factor_consumo_bp, d.raciones, c.raciones_preparadas, c.raciones_consumidas, d.costo_previsto_racion_u6, " &
            "       COALESCE(pr.raciones_servidas, vs.raciones_vendidas) AS base " &
            "FROM minuta_detalle d JOIN minuta m ON m.id = d.minuta_id JOIN estructura_servicio es ON es.id = d.estructura_id " &
            "JOIN receta_version rv ON rv.id = d.receta_version_id JOIN receta rc ON rc.id = rv.receta_id " &
            "LEFT JOIN consumo_plato c ON c.minuta_detalle_id = d.id LEFT JOIN produccion pr ON pr.minuta_id = m.id LEFT JOIN venta_servicio vs ON vs.minuta_id = m.id " &
            "WHERE d.minuta_id = ANY(@ids) ORDER BY m.fecha, es.orden, d.id",
            Function(rd) (Id:=rd.GetInt64(0), Estructura:=rd.GetString(1), Receta:=rd.GetString(2), Factor:=rd.LongONada("factor_consumo_bp"), Plan:=rd.GetInt64(4),
                          Prep:=If(rd.IsDBNull(5), CType(Nothing, Long?), rd.GetInt64(5)), Cons:=If(rd.IsDBNull(6), CType(Nothing, Long?), rd.GetInt64(6)),
                          Costo:=rd.LongONada("costo_previsto_racion_u6"), Base:=rd.LongONada("base")), "ids", minutaIds)
            r.Componentes.Add(New ComponenteComparadoDto With {
                .PlatoId = If(minutaIds.Length = 1, c.Id, CType(Nothing, Long?)), .Estructura = c.Estructura, .Receta = c.Receta, .FactorPlanBp = c.Factor,
                .RacionesPlan = c.Plan, .RacionesPreparadas = c.Prep, .RacionesConsumidas = c.Cons,
                .FactorRealBp = If(c.Cons.HasValue AndAlso c.Base.HasValue AndAlso c.Base.Value > 0, EscalaU6.MultiplicarDividir(c.Cons.Value, 10000, c.Base.Value), CType(Nothing, Long?)),
                .CostoRacionU6 = c.Costo, .CostoPlanU6 = If(c.Costo.HasValue, c.Costo.Value * c.Plan, CType(Nothing, Long?)),
                .CostoTeoricoConsumidoU6 = If(c.Costo.HasValue, c.Costo.Value * If(c.Cons, c.Plan), CType(Nothing, Long?))})
        Next
        If minutaIds.Length > 1 Then
            ' En el mes se agrupa por componente y receta.
            Dim agrupado = r.Componentes.GroupBy(Function(c) (c.Estructura, c.Receta)).Select(Function(g) New ComponenteComparadoDto With {
                .Estructura = g.Key.Estructura, .Receta = g.Key.Receta, .RacionesPlan = g.Sum(Function(c) c.RacionesPlan),
                .RacionesPreparadas = If(g.Any(Function(c) c.RacionesPreparadas.HasValue), g.Sum(Function(c) If(c.RacionesPreparadas, 0L)), CType(Nothing, Long?)),
                .RacionesConsumidas = If(g.Any(Function(c) c.RacionesConsumidas.HasValue), g.Sum(Function(c) If(c.RacionesConsumidas, 0L)), CType(Nothing, Long?)),
                .CostoPlanU6 = If(g.All(Function(c) c.CostoPlanU6.HasValue), g.Sum(Function(c) c.CostoPlanU6.Value), CType(Nothing, Long?)),
                .CostoTeoricoConsumidoU6 = If(g.All(Function(c) c.CostoTeoricoConsumidoU6.HasValue), g.Sum(Function(c) c.CostoTeoricoConsumidoU6.Value), CType(Nothing, Long?))}).ToList()
            r.Componentes.Clear()
            r.Componentes.AddRange(agrupado)
        End If
        r.CostoTeoricoConsumidoU6 = If(r.Componentes.All(Function(c) c.CostoTeoricoConsumidoU6.HasValue), r.Componentes.Sum(Function(c) c.CostoTeoricoConsumidoU6.Value), CType(Nothing, Long?))

        ' Productos teóricos: recetas × raciones planificadas (costo con el precio fijado al aprobar) + productos fijos.
        Dim teorico As New Dictionary(Of Long, (Cant As Long, Costo As Long?))
        For Each l In u.Consultar(
            "SELECT i.producto_base_id, i.cantidad_base_bruta_u6, rv.rendimiento_raciones_u6, d.raciones, ci.costo_unitario_base_u6 " &
            "FROM minuta_detalle d JOIN receta_version rv ON rv.id = d.receta_version_id JOIN receta_ingrediente i ON i.receta_version_id = rv.id " &
            "LEFT JOIN costeo_ingrediente ci ON ci.minuta_detalle_id = d.id AND ci.ingrediente_id = i.id WHERE d.minuta_id = ANY(@ids) " &
            "UNION ALL SELECT f.producto_base_id, f.cantidad_base_u6, NULL, NULL, f.costo_previsto_unitario_u6 FROM minuta_estructura_fija f WHERE f.minuta_id = ANY(@ids)",
            Function(rd) (Producto:=rd.GetInt64(0), Cant:=If(rd.IsDBNull(2), rd.GetInt64(1), Recetas.NecesidadIngredienteU6(rd.GetInt64(1), rd.GetInt64(2), rd.GetInt64(3) * EscalaU6.Factor)),
                          Costo:=rd.LongONada("costo_unitario_base_u6")), "ids", minutaIds)
            Dim previo As (Cant As Long, Costo As Long?) = If(teorico.ContainsKey(l.Producto), teorico(l.Producto), (0L, CType(0L, Long?)))
            Dim costo = If(previo.Costo.HasValue AndAlso l.Costo.HasValue, previo.Costo.Value + EscalaU6.Multiplicar(l.Cant, l.Costo.Value), CType(Nothing, Long?))
            teorico(l.Producto) = (previo.Cant + l.Cant, costo)
        Next
        Dim reales = real.ToDictionary(Function(x) x.Item1, Function(x) (Cant:=x.Item2, Valor:=x.Item3, Devuelto:=x.Item4))
        Dim ids = teorico.Keys.Union(reales.Keys).ToArray()
        Dim nombres = u.Consultar("SELECT p.id, p.descripcion, um.codigo FROM producto_base p JOIN unidad_medida um ON um.id = p.unidad_base_id WHERE p.id = ANY(@ids)",
                                  Function(rd) (rd.GetInt64(0), rd.GetString(1), rd.GetString(2)), "ids", ids).ToDictionary(Function(x) x.Item1)
        For Each p In ids
            Dim t As (Cant As Long, Costo As Long?) = (0L, Nothing)
            Dim enPlan = teorico.TryGetValue(p, t)
            Dim re As (Cant As Long, Valor As Long, Devuelto As Long) = (0L, 0L, 0L)
            Dim salio = reales.TryGetValue(p, re)
            r.Productos.Add(New ProductoComparadoDto With {
                .ProductoBaseId = p, .Producto = nombres(p).Item2, .Unidad = nombres(p).Item3,
                .CantidadTeoricaU6 = If(enPlan, t.Cant, 0L), .CostoTeoricoU6 = If(enPlan, t.Costo, 0L),
                .CantidadRealU6 = re.Cant, .CostoRealU6 = re.Valor, .DevueltoU6 = re.Devuelto, .EntregadoU6 = re.Cant + re.Devuelto,
                .Estado = If(Not enPlan, "no planificado", If(Not salio OrElse re.Cant = 0, "sin salida", "planificado"))})
        Next
        r.Productos.Sort(Function(a, b) If(a.Estado = b.Estado, String.CompareOrdinal(a.Producto, b.Producto), String.CompareOrdinal(a.Estado, b.Estado)))
        r.CostoRealU6 = r.Productos.Sum(Function(x) x.CostoRealU6)
        For Each mm In u.Consultar(
            "SELECT to_char(p.fecha, 'DD/MM') || ' ' || m.etapa || ': ' || trim(to_char(m.cantidad_u6 / 1000000.0, 'FM999999990.000')) || ' ' || um.codigo || ' (' || m.motivo || ')' || " &
            "CASE WHEN m.ya_incluida_consumo = 1 THEN ' - incluida en lo entregado' ELSE ' - con baja de almacen' END " &
            "FROM merma_produccion m JOIN produccion p ON p.id = m.produccion_id JOIN unidad_medida um ON um.id = m.unidad_id " &
            "WHERE p.minuta_id = ANY(@ids) ORDER BY p.fecha, m.id", Function(rd) rd.GetString(0), "ids", minutaIds)
            r.Mermas.Add(mm)
        Next
        Return r
    End Function

End Class
