Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

''' <summary>Una minuta vista como celda de la matriz de planificación: un día y un servicio.</summary>
Public NotInheritable Class CeldaMatrizDto
    Public Property MinutaId As Long
    Public Property OperacionServicioId As Long
    Public Property Fecha As Date
    Public Property ServicioNombre As String
    Public Property RegimenNombre As String
    Public Property Estado As String
    Public Property Comensales As Long
    ''' <summary>Suma de raciones de todos los platos (teóricas).</summary>
    Public Property RacionesTeoricas As Long
    ''' <summary>Raciones operativas: la teórica, o el ajuste del chef si lo hay.</summary>
    Public Property RacionesOperativas As Long
    ''' <summary>Costo previsto de la minuta (snapshot al aprobar); Nothing si todavía no hay.</summary>
    Public Property CostoPrevistoU6 As Long?
    Public Property CostoPorComensalU6 As Long?
    Public Property VentaPrevistaU6 As Long?
    ''' <summary>Costo patrón o techo por comensal del servicio (lo fija la gerencia); Nothing si no está definido.</summary>
    Public Property CostoObjetivoRacionU6 As Long?
    ''' <summary>Techo de la minuta = techo por comensal × comensales.</summary>
    Public Property TechoU6 As Long?
    ''' <summary>Costo previsto − techo (positivo = por encima del techo).</summary>
    Public Property DesviacionU6 As Long?
    ''' <summary>Platos sin costo previsto (quedan pendientes de precio).</summary>
    Public Property PlatosSinCosto As Long
    Public Property Platos As String
    ''' <summary>Producción registrada para la minuta (se usa en el plan del chef).</summary>
    Public Property ProduccionRegistrada As Boolean
    ''' <summary>Estado del último requerimiento de la minuta; vacío si no tiene.</summary>
    Public Property EstadoRequerimiento As String
End Class

''' <summary>Totales de un día: suma de las minutas del día con costo, y su techo y desviación.</summary>
Public NotInheritable Class ResumenDiaDto
    Public Property Fecha As Date
    Public Property Minutas As Long
    Public Property Comensales As Long
    Public Property MinutasSinCosto As Long
    ''' <summary>Costo del día: suma de las minutas con costo previsto; Nothing si ninguna lo tiene.</summary>
    Public Property CostoDiaU6 As Long?
    ''' <summary>Costo por bandeja del día: costo del día ÷ comensales de esas minutas.</summary>
    Public Property CostoPorComensalU6 As Long?
    ''' <summary>Techo del día para las mismas minutas con costo; Nothing si alguna no tiene techo definido.</summary>
    Public Property TechoU6 As Long?
    Public Property DesviacionU6 As Long?
End Class

''' <summary>
''' Matriz de planificación y plan operativo del chef, sobre las mismas minutas de la operación. Solo lee: la edición
''' sigue en ServicioMinutas (que bloquea lo aprobado). Los cálculos usan aritmética entera.
''' </summary>
Public NotInheritable Class ServicioMatrizMenu
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    ''' <summary>Minutas de la operación entre dos fechas, con sus platos, costos, techo y, si hay, producción y requerimiento.</summary>
    Public Function Celdas(desde As Date, hasta As Date) As List(Of CeldaMatrizDto)
        If hasta < desde Then Throw New ReglaNegocioException("DATO_INVALIDO", "La fecha final es anterior a la inicial.")
        Dim servicio As New ServicioMinutas(CadenaConexion, Sesion)
        Dim minutas = servicio.ListarMinutas(desde, hasta)
        Dim estados = EstadosOperativos(desde, hasta)
        Dim operativas = RacionesOperativasPorMinuta(desde, hasta)
        Dim resultado As New List(Of CeldaMatrizDto)
        For Each m In minutas
            Dim platos = servicio.ListarPlatos(m.Id)
            Dim e As (Produccion As Long, Requerimiento As String, Objetivo As Long?) = Nothing
            estados.TryGetValue(m.Id, e)
            Dim techo = If(e.Objetivo.HasValue, e.Objetivo.Value * m.Comensales, CType(Nothing, Long?))
            resultado.Add(New CeldaMatrizDto With {
                .MinutaId = m.Id, .OperacionServicioId = m.OperacionServicioId, .Fecha = m.Fecha,
                .ServicioNombre = m.ServicioNombre, .RegimenNombre = m.RegimenNombre,
                .Estado = m.Estado, .Comensales = m.Comensales,
                .RacionesTeoricas = platos.Sum(Function(p) p.Raciones),
                .RacionesOperativas = If(operativas.ContainsKey(m.Id), operativas(m.Id), platos.Sum(Function(p) p.Raciones)),
                .CostoPrevistoU6 = m.CostoPrevistoU6,
                .CostoPorComensalU6 = If(m.CostoPrevistoU6.HasValue AndAlso m.Comensales > 0,
                                         EscalaU6.MultiplicarDividir(m.CostoPrevistoU6.Value, 1, m.Comensales), CType(Nothing, Long?)),
                .VentaPrevistaU6 = m.VentaPrevistaU6,
                .CostoObjetivoRacionU6 = e.Objetivo,
                .TechoU6 = techo,
                .DesviacionU6 = If(m.CostoPrevistoU6.HasValue AndAlso techo.HasValue, m.CostoPrevistoU6.Value - techo.Value, CType(Nothing, Long?)),
                .PlatosSinCosto = platos.LongCount(Function(p) Not p.CostoPrevistoRacionU6.HasValue),
                .Platos = String.Join("; ", platos.Select(Function(p) $"{p.RecetaNombre} x {p.Raciones}")),
                .ProduccionRegistrada = e.Produccion > 0,
                .EstadoRequerimiento = If(e.Requerimiento, "")})
        Next
        Return resultado
    End Function

    ''' <summary>
    ''' Totales por día. Costo, techo y desviación se suman solo con las minutas que tienen costo y techo; una minuta sin
    ''' costo no se cuenta como cero (queda en MinutasSinCosto). Si alguna minuta con costo no tiene techo, el techo del día
    ''' queda sin valor en vez de ser parcial.
    ''' </summary>
    Public Function ResumenPorDia(celdas As IEnumerable(Of CeldaMatrizDto)) As List(Of ResumenDiaDto)
        Return celdas.GroupBy(Function(c) c.Fecha.Date).OrderBy(Function(g) g.Key).Select(Function(g)
            Dim con = g.Where(Function(c) c.CostoPrevistoU6.HasValue).ToList()
            Dim costo = If(con.Count > 0, con.Sum(Function(c) c.CostoPrevistoU6.Value), CType(Nothing, Long?))
            Dim comensalesCon = con.Sum(Function(c) c.Comensales)
            Dim techoCompleto = con.Count > 0 AndAlso con.All(Function(c) c.TechoU6.HasValue)
            Dim techo = If(techoCompleto, con.Sum(Function(c) c.TechoU6.Value), CType(Nothing, Long?))
            Return New ResumenDiaDto With {
                .Fecha = g.Key, .Minutas = g.LongCount(), .Comensales = g.Sum(Function(c) c.Comensales),
                .MinutasSinCosto = g.LongCount(Function(c) Not c.CostoPrevistoU6.HasValue),
                .CostoDiaU6 = costo,
                .CostoPorComensalU6 = If(costo.HasValue AndAlso comensalesCon > 0, EscalaU6.MultiplicarDividir(costo.Value, 1, comensalesCon), CType(Nothing, Long?)),
                .TechoU6 = techo,
                .DesviacionU6 = If(costo.HasValue AndAlso techo.HasValue, costo.Value - techo.Value, CType(Nothing, Long?))}
        End Function).ToList()
    End Function

    ''' <summary>Raciones operativas por minuta: suma de cada plato con su ajuste, o con la teórica si no lo tiene.</summary>
    Private Function RacionesOperativasPorMinuta(desde As Date, hasta As Date) As Dictionary(Of Long, Long)
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                Return u.Consultar(
                    "SELECT d.minuta_id, COALESCE(sum(COALESCE(a.raciones_operativas, d.raciones)), 0)::bigint " &
                    "FROM minuta_detalle d JOIN minuta m ON m.id = d.minuta_id " &
                    "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
                    "LEFT JOIN minuta_ajuste_operativo a ON a.minuta_detalle_id = d.id " &
                    "WHERE os.operacion_id = @o AND m.fecha BETWEEN @d AND @h GROUP BY d.minuta_id",
                    Function(rd) (Minuta:=rd.GetInt64(0), Raciones:=rd.GetInt64(1)),
                    "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date).ToDictionary(Function(x) x.Minuta, Function(x) x.Raciones)
            End Function)
    End Function

    ''' <summary>Producción registrada, último requerimiento y costo objetivo por minuta del rango.</summary>
    Private Function EstadosOperativos(desde As Date, hasta As Date) As Dictionary(Of Long, (Produccion As Long, Requerimiento As String, Objetivo As Long?))
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                Dim filas = u.Consultar(
                    "SELECT m.id, (SELECT count(*) FROM produccion p WHERE p.minuta_id = m.id), " &
                    "       (SELECT q.estado FROM requerimiento q WHERE q.minuta_id = m.id ORDER BY q.id DESC LIMIT 1), " &
                    "       os.costo_objetivo_racion_u6 " &
                    "FROM minuta m JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
                    "WHERE os.operacion_id = @o AND m.fecha BETWEEN @d AND @h",
                    Function(rd) (Id:=rd.GetInt64(0), Produccion:=rd.GetInt64(1),
                                  Requerimiento:=If(rd.IsDBNull(2), CType(Nothing, String), rd.GetString(2)),
                                  Objetivo:=If(rd.IsDBNull(3), CType(Nothing, Long?), rd.GetInt64(3))),
                    "o", Sesion.Operacion.Id, "d", desde.Date, "h", hasta.Date)
                Return filas.ToDictionary(Function(x) x.Id, Function(x) (Produccion:=x.Produccion, Requerimiento:=x.Requerimiento, Objetivo:=x.Objetivo))
            End Function)
    End Function
End Class
