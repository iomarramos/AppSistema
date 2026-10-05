Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Matriz mensual de la planificación de menús (plan teórico: minutas tipo 'teorica'). Lee el mes completo en pocas
''' consultas y calcula en el dominio. Las ediciones validan operación y estado de la jornada dentro de la transacción,
''' así que una jornada aprobada no se cambia aunque la pantalla lo permita.
''' </summary>
Public NotInheritable Class ServicioPlanificacionMenu
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    Private ReadOnly Property OperacionId As Long
        Get
            Sesion.Exigir(Permisos.MenusVer)
            Return Sesion.Operacion.Id
        End Get
    End Property

    ''' <summary>Matriz de un servicio de la operación para un mes: jornadas, platos por componente, fijos y resúmenes.</summary>
    Public Function Matriz(operacionServicioId As Long, anio As Integer, mes As Integer) As MatrizMensual
        Dim op = OperacionId
        Dim desde = New Date(anio, mes, 1)
        Dim hasta = desde.AddMonths(1).AddDays(-1)
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                Dim filtro = "m.operacion_servicio_id = @os AND os.operacion_id = @o AND m.tipo = 'teorica' AND m.fecha BETWEEN @d AND @h"
                Dim jornadas = u.Consultar("SELECT m.id, m.fecha, m.comensales, m.estado FROM minuta m JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
                                           "WHERE " & filtro & " ORDER BY m.fecha",
                                           Function(rd) New JornadaMatriz With {.MinutaId = rd.GetInt64(0), .Fecha = rd.GetDateTime(1).Date,
                                                                                .Comensales = rd.GetInt64(2), .Estado = rd.GetString(3)},
                                           "os", operacionServicioId, "o", op, "d", desde, "h", hasta)
                Dim platos = u.Consultar(
                    "SELECT d.id, m.id, m.fecha, es.id, es.nombre, es.orden, r.codigo, r.nombre, d.raciones, d.factor_consumo_bp, d.reparto_bp, d.costo_previsto_racion_u6 " &
                    "FROM minuta_detalle d JOIN minuta m ON m.id = d.minuta_id JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
                    "JOIN estructura_servicio es ON es.id = d.estructura_id JOIN receta_version rv ON rv.id = d.receta_version_id JOIN receta r ON r.id = rv.receta_id " &
                    "WHERE " & filtro & " ORDER BY m.fecha, es.orden, es.id, d.id",
                    Function(rd) New PlatoMatriz With {
                        .Id = rd.GetInt64(0), .MinutaId = rd.GetInt64(1), .Fecha = rd.GetDateTime(2).Date,
                        .EstructuraId = rd.GetInt64(3), .EstructuraNombre = rd.GetString(4), .EstructuraOrden = rd.GetInt64(5),
                        .RecetaCodigo = rd.GetString(6), .RecetaNombre = rd.GetString(7), .Raciones = rd.GetInt64(8),
                        .FactorComponenteBp = rd.LongONada("factor_consumo_bp"), .FactorBp = rd.LongONada("reparto_bp"),
                        .CostoRacionU6 = rd.LongONada("costo_previsto_racion_u6")},
                    "os", operacionServicioId, "o", op, "d", desde, "h", hasta)
                Dim fijos = u.Consultar(
                    "SELECT f.minuta_id, f.cantidad_base_u6, f.costo_previsto_unitario_u6 FROM minuta_estructura_fija f " &
                    "JOIN minuta m ON m.id = f.minuta_id JOIN operacion_servicio os ON os.id = m.operacion_servicio_id WHERE " & filtro,
                    Function(rd) New FijoMatriz With {.MinutaId = rd.GetInt64(0), .CantidadBaseU6 = rd.GetInt64(1), .CostoUnitarioU6 = rd.LongONada("costo_previsto_unitario_u6")},
                    "os", operacionServicioId, "o", op, "d", desde, "h", hasta)
                Return ArmarMatriz(anio, mes, operacionServicioId, jornadas, platos, fijos)
            End Function)
    End Function

    ''' <summary>Arma las filas y los días. Cada alternativa de un componente ocupa una fila (la k-ésima del componente en ese día).</summary>
    Private Shared Function ArmarMatriz(anio As Integer, mes As Integer, operacionServicioId As Long,
                                        jornadas As List(Of JornadaMatriz), platos As List(Of PlatoMatriz), fijos As List(Of FijoMatriz)) As MatrizMensual
        Dim matriz As New MatrizMensual With {.Anio = anio, .Mes = mes, .OperacionServicioId = operacionServicioId}
        Dim filas As New Dictionary(Of String, FilaMatriz)()
        For Each j In jornadas
            Dim platosDia = platos.Where(Function(p) p.MinutaId = j.MinutaId).ToList()
            Dim fijosDia = fijos.Where(Function(f) f.MinutaId = j.MinutaId).ToList()
            Dim conteo As New Dictionary(Of Long, Integer)()
            For Each p In platosDia
                If Not conteo.ContainsKey(p.EstructuraId) Then conteo(p.EstructuraId) = 0
                p.Fila = conteo(p.EstructuraId)
                conteo(p.EstructuraId) += 1
                Dim clave = $"{p.EstructuraId}#{p.Fila}"
                If Not filas.ContainsKey(clave) Then
                    filas(clave) = New FilaMatriz With {.EstructuraId = p.EstructuraId, .Estructura = p.EstructuraNombre, .Orden = p.EstructuraOrden, .Alternativa = p.Fila}
                End If
                p.CostoTotalU6 = PlanificacionMenu.CostoTotalU6(p.Raciones, p.CostoRacionU6)
            Next
            Dim dia As New JornadaDia With {.MinutaId = j.MinutaId, .Fecha = j.Fecha, .Estado = j.Estado, .Comensales = j.Comensales}
            dia.Platos.AddRange(platosDia)
            dia.EstructuraFijaU6 = ValorFijos(fijosDia)
            dia.MateriaPrimaU6 = PlanificacionMenu.SumaCostosU6(platosDia.Select(Function(p) p.CostoTotalU6))
            matriz.Dias.Add(dia)
        Next
        matriz.Filas.AddRange(filas.Values.OrderBy(Function(f) f.Orden).ThenBy(Function(f) f.Alternativa))
        Return matriz
    End Function

    ''' <summary>Estructura fija del día = Σ cantidad × costo unitario. Sin fijos vale cero; con un fijo sin precio queda pendiente.</summary>
    Private Shared Function ValorFijos(fijos As List(Of FijoMatriz)) As Long?
        Dim valores As New List(Of Long?)()
        For Each f In fijos
            valores.Add(If(f.CostoUnitarioU6.HasValue, CType(EscalaU6.Multiplicar(f.CantidadBaseU6, f.CostoUnitarioU6.Value), Long?), Nothing))
        Next
        Return PlanificacionMenu.SumaCostosU6(valores)
    End Function

    ' ---------- Ediciones (solo jornadas en borrador; la operación y el estado se validan en la transacción) ----------

    ''' <summary>
    ''' Cambia el factor de participación de una preparación dentro de su componente (100 % = toda la sopa; 60 % y 40 % = dos platos
    ''' de fondo). Raciones = comensales × factor del componente × factor de participación. No cambia los comensales.
    ''' </summary>
    Public Sub FijarFactor(platoId As Long, factorBp As Long)
        If factorBp < 0 OrElse factorBp > PlanificacionMenu.Cien Then Throw New ReglaNegocioException("DATO_INVALIDO", "El factor va de 0 % a 100 %.")
        Dim op = OperacionId
        EnTransaccion(Permisos.MinutasEditar,
            Function(u)
                Dim p = PlatoEditable(u, platoId, op)
                Dim factorComponente = If(p.FactorComponenteBp, ServicioMinutas.FactorVigente(u, p.OperacionServicioId, p.EstructuraId))
                Dim raciones = VentaEstructura.Raciones(p.Comensales, factorComponente, factorBp)
                Return u.Ejecutar("UPDATE minuta_detalle SET factor_consumo_bp = @fc, reparto_bp = @rp, raciones = @r WHERE id = @d",
                                  "fc", factorComponente, "rp", factorBp, "r", raciones, "d", platoId)
            End Function)
    End Sub

    ''' <summary>Raciones escritas a mano (sin factor). Queda como dato manual y un cambio de comensales no la recalcula.</summary>
    Public Sub FijarRaciones(platoId As Long, raciones As Long)
        If raciones < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Las raciones no pueden ser negativas.")
        Dim op = OperacionId
        EnTransaccion(Permisos.MinutasEditar,
            Function(u)
                PlatoEditable(u, platoId, op)
                Return u.Ejecutar("UPDATE minuta_detalle SET raciones = @r, factor_consumo_bp = NULL, reparto_bp = NULL WHERE id = @d", "r", raciones, "d", platoId)
            End Function)
    End Sub

    ''' <summary>Total de comensales de referencia de la jornada. Editar un factor no lo cambia.</summary>
    Public Sub FijarComensales(minutaId As Long, comensales As Long)
        Dim minutas As New ServicioMinutas(CadenaConexion, Sesion)
        minutas.ActualizarComensales(minutaId, comensales)
    End Sub

    Private Function PlatoEditable(u As UnidadDeTrabajo, platoId As Long, operacionId As Long) As PlatoEditableDto
        Dim p = u.Consultar("SELECT d.minuta_id, m.comensales, m.estado, d.factor_consumo_bp, m.operacion_servicio_id, d.estructura_id, os.operacion_id " &
                            "FROM minuta_detalle d JOIN minuta m ON m.id = d.minuta_id JOIN operacion_servicio os ON os.id = m.operacion_servicio_id WHERE d.id = @d",
                            Function(rd) New PlatoEditableDto With {.MinutaId = rd.GetInt64(0), .Comensales = rd.GetInt64(1), .Estado = rd.GetString(2),
                                                                    .FactorComponenteBp = rd.LongONada("factor_consumo_bp"), .OperacionServicioId = rd.GetInt64(4),
                                                                    .EstructuraId = rd.GetInt64(5), .OperacionId = rd.GetInt64(6)}, "d", platoId)
        If p.Count = 0 Then Throw New ReglaNegocioException("PLATO_NO_ENCONTRADO", "La preparacion ya no existe.")
        If p(0).OperacionId <> operacionId Then Throw New ReglaNegocioException("OPERACION_AJENA", "La preparacion no pertenece a la operacion seleccionada.")
        If p(0).Estado <> "borrador" Then Throw New ReglaNegocioException("MINUTA_APROBADA", "La jornada ya esta aprobada y su contenido no se modifica.")
        Return p(0)
    End Function

    Private Class PlatoEditableDto
        Public Property MinutaId As Long
        Public Property Comensales As Long
        Public Property Estado As String
        Public Property FactorComponenteBp As Long?
        Public Property OperacionServicioId As Long
        Public Property EstructuraId As Long
        Public Property OperacionId As Long
    End Class

End Class

Public NotInheritable Class MatrizMensual

    Public Property Anio As Integer
    Public Property Mes As Integer
    Public Property OperacionServicioId As Long
    ''' <summary>Filas fijas de la matriz: componente y alternativa (orden del servicio).</summary>
    Public ReadOnly Property Filas As New List(Of FilaMatriz)()
    ''' <summary>Solo los días con jornada (minuta) en el mes.</summary>
    Public ReadOnly Property Dias As New List(Of JornadaDia)()

    Public Function ResumenMes() As ResumenPlanificacion
        Return ResumenPlanificacion.Sumar(Dias.Select(Function(d) d.Resumen))
    End Function

    ''' <summary>Acumulado desde el inicio del mes hasta la fecha (inclusive).</summary>
    Public Function ResumenHasta(fecha As Date) As ResumenPlanificacion
        Return ResumenPlanificacion.Sumar(Dias.Where(Function(d) d.Fecha <= fecha.Date).Select(Function(d) d.Resumen))
    End Function

End Class

Public NotInheritable Class FilaMatriz
    Public Property EstructuraId As Long
    Public Property Estructura As String
    Public Property Orden As Long
    ''' <summary>0 para el primer plato del componente en el día; 1 para la alternativa siguiente, etc.</summary>
    Public Property Alternativa As Integer
End Class

Public NotInheritable Class JornadaDia

    Public Property MinutaId As Long
    Public Property Fecha As Date
    Public Property Estado As String
    Public Property Comensales As Long
    Public ReadOnly Property Platos As New List(Of PlatoMatriz)()
    ''' <summary>Materia prima del día (Nothing si hay un costo pendiente).</summary>
    Public Property MateriaPrimaU6 As Long?
    ''' <summary>Estructura fija del día (Nothing si hay un fijo sin precio).</summary>
    Public Property EstructuraFijaU6 As Long?

    Public ReadOnly Property Editable As Boolean
        Get
            Return Estado = "borrador"
        End Get
    End Property

    Public ReadOnly Property Resumen As ResumenPlanificacion
        Get
            Return New ResumenPlanificacion With {.Comensales = Comensales, .MateriaPrimaU6 = MateriaPrimaU6, .EstructuraFijaU6 = EstructuraFijaU6}
        End Get
    End Property

End Class

Public NotInheritable Class PlatoMatriz
    Public Property Id As Long
    Public Property MinutaId As Long
    Public Property Fecha As Date
    Public Property EstructuraId As Long
    Public Property EstructuraNombre As String
    Public Property EstructuraOrden As Long
    Public Property RecetaCodigo As String
    Public Property RecetaNombre As String
    Public Property Raciones As Long
    ''' <summary>Factor de consumo del componente con el que se calcularon las raciones (100 % = 10 000).</summary>
    Public Property FactorComponenteBp As Long?
    ''' <summary>Factor de participación de la preparación dentro de su componente (reparto de las alternativas).</summary>
    Public Property FactorBp As Long?
    Public Property CostoRacionU6 As Long?
    ''' <summary>Costo total de la preparación (raciones × costo unitario). Nothing si el costo está pendiente.</summary>
    Public Property CostoTotalU6 As Long?
    ''' <summary>Posición de la alternativa dentro del componente en el día (ver FilaMatriz.Alternativa).</summary>
    Public Property Fila As Integer
End Class

Friend NotInheritable Class JornadaMatriz
    Public Property MinutaId As Long
    Public Property Fecha As Date
    Public Property Comensales As Long
    Public Property Estado As String
End Class

Friend NotInheritable Class FijoMatriz
    Public Property MinutaId As Long
    Public Property CantidadBaseU6 As Long
    Public Property CostoUnitarioU6 As Long?
End Class
