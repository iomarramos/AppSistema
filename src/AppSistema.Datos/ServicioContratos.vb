Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Seguridad

Public NotInheritable Class ClienteDto
    Public Property Id As Long
    Public Property Codigo As String
    Public Property Nombre As String
    Public Property IdentificacionFiscal As String
End Class

Public NotInheritable Class ContratoDto
    Public Property Id As Long
    Public Property Codigo As String
    Public Property Cliente As String
    Public Property FechaDesde As Date
    Public Property FechaHasta As Date?
    Public Property Condiciones As String
    Public Property Moneda As String
End Class

Public NotInheritable Class LineaContratoDto
    Public Property Id As Long
    Public Property OperacionServicioId As Long
    Public Property Servicio As String
    Public Property ImporteMensualU6 As Long
    Public Property FechaDesde As Date
    Public Property FechaHasta As Date?
End Class

Public NotInheritable Class IngresoGeneradoDto
    Public Property Servicio As String
    Public Property ImporteU6 As Long?
    Public Property Estado As String
    Public Property Detalle As String
End Class

''' <summary>
''' Clientes y contratos mensuales de la operación (etapa 9). Una línea de contrato fija el importe mensual de un servicio
''' con su vigencia; un ajuste cierra la línea y abre otra (la historia no se reescribe). El ingreso mensual del servicio
''' se genera desde las líneas vigentes, prorrateando por días (supuesto D13), sin pisar un ingreso registrado a mano.
''' </summary>
Public NotInheritable Class ServicioContratos
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

    ' ---------- Clientes ----------

    Public Function CrearCliente(codigo As String, nombre As String, identificacionFiscal As String) As Long
        Return EnTransaccion(Permisos.ContratosEditar,
            Function(u) u.EscalarLong("INSERT INTO cliente(empresa_id, codigo, nombre, identificacion_fiscal) VALUES (@e, @c, @n, @i) RETURNING id",
                                      "e", Sesion.EmpresaId, "c", ServicioAdministracion.Requerido(codigo, "codigo"),
                                      "n", ServicioAdministracion.Requerido(nombre, "nombre"),
                                      "i", If(String.IsNullOrWhiteSpace(identificacionFiscal), Nothing, identificacionFiscal.Trim())))
    End Function

    Public Function ListarClientes() As List(Of ClienteDto)
        Return EnTransaccion(Permisos.ContratosVer,
            Function(u) u.Consultar("SELECT id, codigo, nombre, identificacion_fiscal FROM cliente ORDER BY codigo",
                Function(rd) New ClienteDto With {.Id = rd.GetInt64(0), .Codigo = rd.GetString(1), .Nombre = rd.GetString(2), .IdentificacionFiscal = rd.TextoONada("identificacion_fiscal")}))
    End Function

    ' ---------- Contratos ----------

    Public Function CrearContrato(clienteId As Long, codigo As String, desde As Date, hasta As Date?, condiciones As String) As Long
        Dim o = Op
        Return EnTransaccion(Permisos.ContratosEditar,
            Function(u)
                Dim moneda = CStr(u.Escalar("SELECT moneda FROM empresa WHERE id = @e", "e", Sesion.EmpresaId))
                Return u.EscalarLong("INSERT INTO contrato(empresa_id, cliente_id, operacion_id, codigo, fecha_desde, fecha_hasta, moneda, condiciones) " &
                                     "VALUES (@e, @c, @o, @cod, @d, @h, @m, @cond) RETURNING id",
                                     "e", Sesion.EmpresaId, "c", clienteId, "o", o, "cod", ServicioAdministracion.Requerido(codigo, "codigo"),
                                     "d", desde.Date, "h", If(hasta.HasValue, CObj(hasta.Value.Date), Nothing), "m", moneda,
                                     "cond", If(String.IsNullOrWhiteSpace(condiciones), Nothing, condiciones.Trim()))
            End Function)
    End Function

    Public Function ListarContratos() As List(Of ContratoDto)
        Dim o = Op
        Return EnTransaccion(Permisos.ContratosVer,
            Function(u) u.Consultar(
                "SELECT c.id, c.codigo, cl.nombre, c.fecha_desde, c.fecha_hasta, c.condiciones, c.moneda FROM contrato c " &
                "JOIN cliente cl ON cl.id = c.cliente_id WHERE c.operacion_id = @o ORDER BY c.fecha_desde DESC, c.codigo",
                Function(rd) New ContratoDto With {.Id = rd.GetInt64(0), .Codigo = rd.GetString(1), .Cliente = rd.GetString(2), .FechaDesde = rd.GetDateTime(3),
                                                   .FechaHasta = If(rd.IsDBNull(4), CType(Nothing, Date?), rd.GetDateTime(4)),
                                                   .Condiciones = rd.TextoONada("condiciones"), .Moneda = rd.GetString(6)}, "o", o))
    End Function

    ''' <summary>Fin de vigencia: cierra las líneas abiertas del contrato y el contrato en esa fecha.</summary>
    Public Sub CerrarContrato(contratoId As Long, hasta As Date)
        Dim o = Op
        EnTransaccion(Permisos.ContratosEditar,
            Function(u)
                ExigirContrato(u, contratoId, o)
                u.Ejecutar("UPDATE contrato_servicio SET fecha_hasta = @h WHERE contrato_id = @c AND (fecha_hasta IS NULL OR fecha_hasta > @h)",
                           "h", hasta.Date, "c", contratoId)
                Return u.Ejecutar("UPDATE contrato SET fecha_hasta = @h WHERE id = @c", "h", hasta.Date, "c", contratoId)
            End Function)
    End Sub

    Public Function AgregarServicio(contratoId As Long, operacionServicioId As Long, importeMensualU6 As Long, desde As Date, hasta As Date?) As Long
        If importeMensualU6 < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "El importe no puede ser negativo.")
        Dim o = Op
        Return EnTransaccion(Permisos.ContratosEditar,
            Function(u)
                ExigirContrato(u, contratoId, o)
                Return u.EscalarLong("INSERT INTO contrato_servicio(empresa_id, contrato_id, operacion_servicio_id, importe_mensual_u6, fecha_desde, fecha_hasta) " &
                                     "VALUES (@e, @c, @os, @i, @d, @h) RETURNING id",
                                     "e", Sesion.EmpresaId, "c", contratoId, "os", operacionServicioId, "i", importeMensualU6,
                                     "d", desde.Date, "h", If(hasta.HasValue, CObj(hasta.Value.Date), Nothing))
            End Function)
    End Function

    ''' <summary>
    ''' Ajuste de importe desde una fecha: la línea vigente termina el día anterior y una nueva línea toma el importe nuevo
    ''' hasta el fin que tenía la anterior. Lo ya ocurrido (y los meses cerrados) no cambia.
    ''' </summary>
    Public Function Ajustar(lineaId As Long, nuevoImporteMensualU6 As Long, desde As Date) As Long
        If nuevoImporteMensualU6 < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "El importe no puede ser negativo.")
        Dim o = Op
        Return EnTransaccion(Permisos.ContratosEditar,
            Function(u)
                Dim l = u.Consultar("SELECT cs.contrato_id, cs.operacion_servicio_id, cs.fecha_desde, cs.fecha_hasta FROM contrato_servicio cs " &
                                    "JOIN contrato c ON c.id = cs.contrato_id WHERE cs.id = @l AND c.operacion_id = @o FOR UPDATE OF cs",
                                    Function(rd) (Contrato:=rd.GetInt64(0), Servicio:=rd.GetInt64(1), Desde:=rd.GetDateTime(2),
                                                  Hasta:=If(rd.IsDBNull(3), CType(Nothing, Date?), rd.GetDateTime(3))), "l", lineaId, "o", o).SingleOrDefault()
                If l.Contrato = 0 Then Throw New ReglaNegocioException("NO_ENCONTRADO", "La linea de contrato no existe en esta operacion.")
                If desde.Date <= l.Desde OrElse (l.Hasta.HasValue AndAlso desde.Date > l.Hasta.Value) Then
                    Throw New ReglaNegocioException("FUERA_DE_VIGENCIA", "El ajuste debe empezar despues del inicio de la linea y dentro de su vigencia.")
                End If
                u.Ejecutar("UPDATE contrato_servicio SET fecha_hasta = @h WHERE id = @l", "h", desde.Date.AddDays(-1), "l", lineaId)
                Return u.EscalarLong("INSERT INTO contrato_servicio(empresa_id, contrato_id, operacion_servicio_id, importe_mensual_u6, fecha_desde, fecha_hasta) " &
                                     "VALUES (@e, @c, @os, @i, @d, @h) RETURNING id",
                                     "e", Sesion.EmpresaId, "c", l.Contrato, "os", l.Servicio, "i", nuevoImporteMensualU6, "d", desde.Date,
                                     "h", If(l.Hasta.HasValue, CObj(l.Hasta.Value), Nothing))
            End Function)
    End Function

    Public Function Lineas(contratoId As Long) As List(Of LineaContratoDto)
        Dim o = Op
        Return EnTransaccion(Permisos.ContratosVer,
            Function(u)
                ExigirContrato(u, contratoId, o)
                Return u.Consultar(
                    "SELECT cs.id, cs.operacion_servicio_id, s.nombre || ' - ' || rg.nombre, cs.importe_mensual_u6, cs.fecha_desde, cs.fecha_hasta " &
                    "FROM contrato_servicio cs JOIN operacion_servicio os ON os.id = cs.operacion_servicio_id JOIN servicio s ON s.id = os.servicio_id " &
                    "JOIN regimen rg ON rg.id = os.regimen_id WHERE cs.contrato_id = @c ORDER BY s.nombre, cs.fecha_desde",
                    Function(rd) New LineaContratoDto With {.Id = rd.GetInt64(0), .OperacionServicioId = rd.GetInt64(1), .Servicio = rd.GetString(2),
                                                            .ImporteMensualU6 = rd.GetInt64(3), .FechaDesde = rd.GetDateTime(4),
                                                            .FechaHasta = If(rd.IsDBNull(5), CType(Nothing, Date?), rd.GetDateTime(5))}, "c", contratoId)
            End Function)
    End Function

    ''' <summary>
    ''' Genera el ingreso del mes de cada servicio desde sus líneas de contrato vigentes. Un ingreso registrado a mano se
    ''' conserva (y se informa). Repetir la generación da el mismo resultado.
    ''' </summary>
    Public Function GenerarIngresos(anio As Integer, mes As Integer) As List(Of IngresoGeneradoDto)
        Dim o = Op
        Return EnTransaccion(Permisos.ContratosEditar,
            Function(u)
                Dim periodoId = ServicioCierres.PeriodoAbierto(u, Sesion.EmpresaId, o, anio, mes)
                Dim inicio As New Date(anio, mes, 1), fin = New Date(anio, mes, 1).AddMonths(1).AddDays(-1)
                Dim lineas = u.Consultar(
                    "SELECT cs.operacion_servicio_id, c.codigo, cs.importe_mensual_u6, cs.fecha_desde, cs.fecha_hasta FROM contrato_servicio cs " &
                    "JOIN contrato c ON c.id = cs.contrato_id WHERE c.operacion_id = @o AND cs.fecha_desde <= @f AND (cs.fecha_hasta IS NULL OR cs.fecha_hasta >= @i)",
                    Function(rd) (Servicio:=rd.GetInt64(0), Contrato:=rd.GetString(1),
                                  Linea:=New LineaContrato With {.ImporteMensualU6 = rd.GetInt64(2), .FechaDesde = rd.GetDateTime(3),
                                                                 .FechaHasta = If(rd.IsDBNull(4), CType(Nothing, Date?), rd.GetDateTime(4))}),
                    "o", o, "i", inicio, "f", fin)
                Dim r As New List(Of IngresoGeneradoDto)
                For Each s In u.Consultar(
                    "SELECT os.id, s.nombre || ' - ' || rg.nombre, i.origen FROM operacion_servicio os JOIN servicio s ON s.id = os.servicio_id " &
                    "JOIN regimen rg ON rg.id = os.regimen_id LEFT JOIN ingreso_servicio i ON i.operacion_servicio_id = os.id AND i.periodo_id = @p " &
                    "WHERE os.operacion_id = @o ORDER BY s.nombre, rg.nombre",
                    Function(rd) (Id:=rd.GetInt64(0), Nombre:=rd.GetString(1), Origen:=rd.TextoONada("origen")), "p", periodoId, "o", o)
                    Dim propias = lineas.Where(Function(l) l.Servicio = s.Id).ToList()
                    If propias.Count = 0 Then
                        r.Add(New IngresoGeneradoDto With {.Servicio = s.Nombre, .Estado = "sin contrato", .Detalle = "No hay lineas de contrato vigentes en el mes"})
                        Continue For
                    End If
                    Dim importe = IngresoContrato.ImporteDelMesU6(propias.Select(Function(l) l.Linea), anio, mes)
                    If s.Origen = "manual" Then
                        r.Add(New IngresoGeneradoDto With {.Servicio = s.Nombre, .ImporteU6 = importe, .Estado = "manual conservado",
                                                           .Detalle = "Ya hay un ingreso registrado a mano; no se reemplaza"})
                        Continue For
                    End If
                    Dim fuente = "Contrato " & String.Join(", ", propias.Select(Function(l) $"{l.Contrato} ({IngresoContrato.DiasCubiertos(l.Linea, anio, mes)} dias)").Distinct())
                    u.Ejecutar("INSERT INTO ingreso_servicio(empresa_id, operacion_servicio_id, periodo_id, importe_neto_u6, ajustes_u6, moneda, fuente, origen) " &
                               "VALUES (@e, @os, @p, @i, 0, (SELECT moneda FROM empresa WHERE id = @e), @f, 'contrato') " &
                               "ON CONFLICT (empresa_id, operacion_servicio_id, periodo_id) DO UPDATE SET importe_neto_u6 = EXCLUDED.importe_neto_u6, fuente = EXCLUDED.fuente " &
                               "WHERE ingreso_servicio.origen = 'contrato'",
                               "e", Sesion.EmpresaId, "os", s.Id, "p", periodoId, "i", importe, "f", fuente)
                    r.Add(New IngresoGeneradoDto With {.Servicio = s.Nombre, .ImporteU6 = importe, .Estado = "generado", .Detalle = fuente})
                Next
                Return r
            End Function)
    End Function

    ''' <summary>Servicios de la operación (para elegir en contratos y gastos), con el permiso de contratos.</summary>
    Public Function ServiciosDeOperacion() As List(Of OperacionServicioDto)
        Dim o = Op
        Return EnTransaccion(Permisos.ContratosVer, Function(u) LeerServiciosDeOperacion(u, o))
    End Function

    Friend Shared Function LeerServiciosDeOperacion(u As UnidadDeTrabajo, operacionId As Long) As List(Of OperacionServicioDto)
        Return u.Consultar(
            "SELECT os.id, os.servicio_id, s.nombre, os.regimen_id, rg.nombre FROM operacion_servicio os JOIN servicio s ON s.id = os.servicio_id " &
            "JOIN regimen rg ON rg.id = os.regimen_id WHERE os.operacion_id = @o ORDER BY s.nombre, rg.nombre",
            Function(rd) New OperacionServicioDto With {.Id = rd.GetInt64(0), .ServicioId = rd.GetInt64(1), .ServicioNombre = rd.GetString(2),
                                                        .RegimenId = rd.GetInt64(3), .RegimenNombre = rd.GetString(4)}, "o", operacionId)
    End Function

    Private Shared Sub ExigirContrato(u As UnidadDeTrabajo, contratoId As Long, o As Long)
        If u.Escalar("SELECT 1 FROM contrato WHERE id = @c AND operacion_id = @o", "c", contratoId, "o", o) Is Nothing Then
            Throw New ReglaNegocioException("OPERACION_AJENA", "El contrato no pertenece a la operacion seleccionada.")
        End If
    End Sub

End Class
