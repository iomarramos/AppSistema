Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Configuración de servicios y minutas de la operación de la sesión. La minuta se planifica en borrador; al aprobarla
''' se guarda el costo previsto con su fuente y fecha (snapshot): cambios de precio posteriores no la alteran (T10).
''' </summary>
Public NotInheritable Class ServicioMinutas
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

    ' ---------- Configuración ----------

    Public Function CrearServicio(codigo As String, nombre As String) As Long
        Return EnTransaccion(Permisos.MenusConfigurar,
            Function(u) u.EscalarLong("INSERT INTO servicio(empresa_id, codigo, nombre) VALUES (@e, @c, @n) RETURNING id",
                                      "e", Sesion.EmpresaId, "c", ServicioAdministracion.Requerido(codigo, "codigo"), "n", ServicioAdministracion.Requerido(nombre, "nombre")))
    End Function

    Public Function CrearRegimen(codigo As String, nombre As String) As Long
        Return EnTransaccion(Permisos.MenusConfigurar,
            Function(u) u.EscalarLong("INSERT INTO regimen(empresa_id, codigo, nombre) VALUES (@e, @c, @n) RETURNING id",
                                      "e", Sesion.EmpresaId, "c", ServicioAdministracion.Requerido(codigo, "codigo"), "n", ServicioAdministracion.Requerido(nombre, "nombre")))
    End Function

    ''' <summary>Componente del servicio (sopa, fondo, bebida…) en el orden en que se sirve.</summary>
    ''' <param name="factorConsumoBp">Qué parte de los comensales consume el componente (100 % = 10000; complementos 30–70 %).</param>
    Public Function CrearEstructura(servicioId As Long, codigo As String, nombre As String, orden As Long, Optional factorConsumoBp As Long = 10000) As Long
        Return EnTransaccion(Permisos.MenusConfigurar,
            Function(u) u.EscalarLong("INSERT INTO estructura_servicio(empresa_id, servicio_id, codigo, nombre, orden, factor_consumo_bp) VALUES (@e, @s, @c, @n, @o, @f) RETURNING id",
                                      "e", Sesion.EmpresaId, "s", servicioId, "c", ServicioAdministracion.Requerido(codigo, "codigo"),
                                      "n", ServicioAdministracion.Requerido(nombre, "nombre"), "o", orden, "f", factorConsumoBp))
    End Function

    ''' <summary>Cambia el factor de consumo del componente (aplica a las minutas que se planifiquen después).</summary>
    Public Sub FijarFactorConsumo(estructuraId As Long, factorConsumoBp As Long)
        EnTransaccion(Permisos.MenusConfigurar,
            Function(u) ServicioRecetas.ExigirFila(u.Ejecutar("UPDATE estructura_servicio SET factor_consumo_bp = @f WHERE id = @id", "f", factorConsumoBp, "id", estructuraId)))
    End Sub

    ''' <summary>Food Cost objetivo del servicio en la operación (48 % = 4800; vacío = 48 % por defecto).</summary>
    Public Sub FijarFoodCostObjetivo(operacionServicioId As Long, objetivoBp As Long?)
        If objetivoBp.HasValue AndAlso (objetivoBp.Value <= 0 OrElse objetivoBp.Value > 10000) Then Throw New ReglaNegocioException("DATO_INVALIDO", "El objetivo va de 0,01 % a 100 %.")
        Dim op = OperacionId
        EnTransaccion(Permisos.MenusConfigurar,
            Function(u)
                ExigirServicioDeOperacion(u, operacionServicioId, op)
                Return u.Ejecutar("UPDATE operacion_servicio SET food_cost_objetivo_bp = @b WHERE id = @os",
                                  "b", If(objetivoBp.HasValue, CObj(objetivoBp.Value), Nothing), "os", operacionServicioId)
            End Function)
    End Sub

    ''' <summary>La operación de la sesión presta este servicio con este régimen.</summary>
    Public Function AsignarServicio(servicioId As Long, regimenId As Long, costoObjetivoRacionU6 As Long?) As Long
        Dim op = Sesion.Operacion
        Return EnTransaccion(Permisos.MenusConfigurar,
            Function(u)
                If op Is Nothing Then Throw New ReglaNegocioException("OPERACION_NO_SELECCIONADA", "Seleccione una operacion.")
                Return u.EscalarLong("INSERT INTO operacion_servicio(empresa_id, operacion_id, servicio_id, regimen_id, costo_objetivo_racion_u6) " &
                                     "VALUES (@e, @o, @s, @r, @c) RETURNING id",
                                     "e", Sesion.EmpresaId, "o", op.Id, "s", servicioId, "r", regimenId,
                                     "c", If(costoObjetivoRacionU6.HasValue, CType(costoObjetivoRacionU6.Value, Object), Nothing))
            End Function)
    End Function

    Public Function ListarServicios() As List(Of ServicioDto)
        Return EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar("SELECT id, codigo, nombre FROM servicio ORDER BY nombre",
                                    Function(rd) New ServicioDto With {.Id = rd.GetInt64(0), .Codigo = rd.GetString(1), .Nombre = rd.GetString(2)}))
    End Function

    Public Function ListarRegimenes() As List(Of ServicioDto)
        Return EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar("SELECT id, codigo, nombre FROM regimen ORDER BY nombre",
                                    Function(rd) New ServicioDto With {.Id = rd.GetInt64(0), .Codigo = rd.GetString(1), .Nombre = rd.GetString(2)}))
    End Function

    Public Function ListarEstructuras(servicioId As Long) As List(Of EstructuraDto)
        Return EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar("SELECT id, servicio_id, codigo, nombre, orden, factor_consumo_bp FROM estructura_servicio WHERE servicio_id = @s ORDER BY orden, id",
                                    Function(rd) New EstructuraDto With {.Id = rd.GetInt64(0), .ServicioId = rd.GetInt64(1), .Codigo = rd.GetString(2),
                                                                         .Nombre = rd.GetString(3), .Orden = rd.GetInt64(4), .FactorConsumoBp = rd.GetInt64(5)}, "s", servicioId))
    End Function

    Public Function ListarServiciosDeOperacion() As List(Of OperacionServicioDto)
        Dim op = OperacionId
        Return EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT os.id, s.id, s.nombre, r.id, r.nombre, os.costo_objetivo_racion_u6, os.food_cost_objetivo_bp FROM operacion_servicio os " &
                "JOIN servicio s ON s.id = os.servicio_id JOIN regimen r ON r.id = os.regimen_id WHERE os.operacion_id = @o ORDER BY s.nombre, r.nombre",
                Function(rd) New OperacionServicioDto With {.Id = rd.GetInt64(0), .ServicioId = rd.GetInt64(1), .ServicioNombre = rd.GetString(2),
                                                           .RegimenId = rd.GetInt64(3), .RegimenNombre = rd.GetString(4),
                                                           .CostoObjetivoRacionU6 = If(rd.IsDBNull(5), CType(Nothing, Long?), rd.GetInt64(5)),
                                                           .FoodCostObjetivoBp = If(rd.IsDBNull(6), CType(Nothing, Long?), rd.GetInt64(6))}, "o", op))
    End Function

    ' ---------- Minutas ----------

    ''' <summary>Minuta teórica (planificada) del día para un servicio de la operación de la sesión.</summary>
    Public Function CrearMinuta(operacionServicioId As Long, fecha As Date, comensales As Long) As Long
        If comensales < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Los comensales no pueden ser negativos.")
        Dim op = OperacionId
        Return EnTransaccion(Permisos.MinutasEditar,
            Function(u)
                ExigirServicioDeOperacion(u, operacionServicioId, op)
                Return u.EscalarLong("INSERT INTO minuta(empresa_id, operacion_servicio_id, fecha, tipo, comensales, usuario_id) " &
                                     "VALUES (@e, @os, @f, 'teorica', @c, @u) RETURNING id",
                                     "e", Sesion.EmpresaId, "os", operacionServicioId, "f", fecha.Date, "c", comensales, "u", Sesion.UsuarioId)
            End Function)
    End Function

    Public Function AgregarPlato(minutaId As Long, estructuraId As Long, recetaVersionId As Long, raciones As Long) As Long
        If raciones <= 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Las raciones deben ser mayores que cero.")
        Dim op = OperacionId
        Return EnTransaccion(Permisos.MinutasEditar,
            Function(u)
                ExigirMinutaDeOperacion(u, minutaId, op)
                Return u.EscalarLong("INSERT INTO minuta_detalle(empresa_id, minuta_id, estructura_id, receta_version_id, raciones) " &
                                     "VALUES (@e, @m, @s, @v, @r) RETURNING id",
                                     "e", Sesion.EmpresaId, "m", minutaId, "s", estructuraId, "v", recetaVersionId, "r", raciones)
            End Function)
    End Function

    ''' <summary>
    ''' Agrega la alternativa con raciones = comensales × factor del componente × reparto (p. ej. jugo A 50 % y jugo B 50 %).
    ''' </summary>
    Public Function AgregarPlatoPorFactor(minutaId As Long, estructuraId As Long, recetaVersionId As Long, Optional repartoBp As Long = 10000) As Long
        Dim op = OperacionId
        Dim raciones = EnTransaccion(Permisos.MinutasEditar,
            Function(u)
                ExigirMinutaDeOperacion(u, minutaId, op)
                Dim comensales = u.EscalarLong("SELECT comensales FROM minuta WHERE id = @m", "m", minutaId)
                Dim factor = u.Escalar("SELECT es.factor_consumo_bp FROM estructura_servicio es JOIN operacion_servicio os ON os.servicio_id = es.servicio_id " &
                                       "JOIN minuta m ON m.operacion_servicio_id = os.id WHERE m.id = @m AND es.id = @es", "m", minutaId, "es", estructuraId)
                If factor Is Nothing Then Throw New ReglaNegocioException("ESTRUCTURA_DE_OTRO_SERVICIO", "El componente no pertenece al servicio de la minuta.")
                Return VentaEstructura.Raciones(comensales, CLng(factor), repartoBp)
            End Function)
        If raciones <= 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Con ese factor y reparto no quedan raciones.")
        Return AgregarPlato(minutaId, estructuraId, recetaVersionId, raciones)
    End Function

    Public Sub QuitarPlato(platoId As Long)
        Dim op = OperacionId
        EnTransaccion(Permisos.MinutasEditar,
            Function(u)
                ExigirMinutaDeOperacion(u, u.EscalarLong("SELECT minuta_id FROM minuta_detalle WHERE id = @d", "d", platoId), op)
                Return ServicioRecetas.ExigirFila(u.Ejecutar("DELETE FROM minuta_detalle WHERE id = @d", "d", platoId))
            End Function)
    End Sub

    ''' <summary>Producto que se entrega fuera de recetas (pan, fruta, descartables…) con cantidad total en unidad base.</summary>
    Public Function AgregarFijo(minutaId As Long, productoBaseId As Long, cantidadBaseU6 As Long) As Long
        If cantidadBaseU6 <= 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "La cantidad debe ser mayor que cero.")
        Dim op = OperacionId
        Return EnTransaccion(Permisos.MinutasEditar,
            Function(u)
                ExigirMinutaDeOperacion(u, minutaId, op)
                Return u.EscalarLong("INSERT INTO minuta_estructura_fija(empresa_id, minuta_id, producto_base_id, cantidad_base_u6) VALUES (@e, @m, @p, @c) RETURNING id",
                                     "e", Sesion.EmpresaId, "m", minutaId, "p", productoBaseId, "c", cantidadBaseU6)
            End Function)
    End Function

    Public Sub QuitarFijo(fijoId As Long)
        Dim op = OperacionId
        EnTransaccion(Permisos.MinutasEditar,
            Function(u)
                ExigirMinutaDeOperacion(u, u.EscalarLong("SELECT minuta_id FROM minuta_estructura_fija WHERE id = @d", "d", fijoId), op)
                Return ServicioRecetas.ExigirFila(u.Ejecutar("DELETE FROM minuta_estructura_fija WHERE id = @d", "d", fijoId))
            End Function)
    End Sub

    ''' <summary>
    ''' Aprueba la minuta y guarda el costo previsto de cada plato y fijo con su fuente y fecha de precio.
    ''' Un plato con algún ingrediente sin precio queda con costo pendiente (no se inventa un total).
    ''' </summary>
    Public Sub Aprobar(minutaId As Long, moneda As String)
        Dim op = OperacionId
        EnTransaccion(Permisos.MinutasAprobar,
            Function(u)
                ExigirMinutaDeOperacion(u, minutaId, op)
                Dim fecha = CDate(u.Escalar("SELECT fecha FROM minuta WHERE id = @m FOR UPDATE", "m", minutaId))
                Dim platos = u.Consultar("SELECT id, receta_version_id FROM minuta_detalle WHERE minuta_id = @m ORDER BY id",
                                         Function(rd) (Id:=rd.GetInt64(0), Version:=rd.GetInt64(1)), "m", minutaId)
                For Each p In platos
                    Dim costo = CosteoBD.CostearVersion(u, p.Version, fecha, moneda)
                    For Each i In costo.Ingredientes.Where(Function(x) x.CostoUnitarioBaseU6.HasValue)
                        u.Ejecutar("INSERT INTO costeo_ingrediente(empresa_id, minuta_detalle_id, ingrediente_id, costo_unitario_base_u6, fuente_precio, fecha_precio) " &
                                   "VALUES (@e, @d, @i, @c, @f, @fp)",
                                   "e", Sesion.EmpresaId, "d", p.Id, "i", i.IngredienteId, "c", i.CostoUnitarioBaseU6.Value, "f", i.Fuente, "fp", i.FechaPrecio.Value.Date)
                    Next
                    u.Ejecutar("UPDATE minuta_detalle SET costo_previsto_racion_u6 = @c, fecha_costeo = @f WHERE id = @d",
                               "c", If(costo.CostoRacionU6.HasValue, CType(costo.CostoRacionU6.Value, Object), Nothing), "f", fecha, "d", p.Id)
                Next
                Dim fijos = u.Consultar("SELECT id, producto_base_id FROM minuta_estructura_fija WHERE minuta_id = @m",
                                        Function(rd) (Id:=rd.GetInt64(0), Producto:=rd.GetInt64(1)), "m", minutaId)
                For Each f In fijos
                    u.Ejecutar("UPDATE minuta_estructura_fija SET costo_previsto_unitario_u6 = @c WHERE id = @id",
                               "c", CosteoBD.CostoProducto(u, f.Producto, fecha, moneda), "id", f.Id)
                Next
                ' Costo previsto de la estructura y venta = costo / Food Cost objetivo (D13). Con algún costo pendiente no se inventa la venta.
                Dim costoTotal = u.Escalar(
                    "SELECT CASE WHEN bool_and(x.c IS NOT NULL) THEN sum(x.c) END FROM (" &
                    "  SELECT d.costo_previsto_racion_u6 * d.raciones AS c FROM minuta_detalle d WHERE d.minuta_id = @m " &
                    "  UNION ALL SELECT round(f.costo_previsto_unitario_u6::numeric * f.cantidad_base_u6 / 1000000)::bigint FROM minuta_estructura_fija f WHERE f.minuta_id = @m) x",
                    "m", minutaId)
                Dim objetivo = CLng(If(u.Escalar("SELECT os.food_cost_objetivo_bp FROM minuta m JOIN operacion_servicio os ON os.id = m.operacion_servicio_id WHERE m.id = @m",
                                                 "m", minutaId), VentaEstructura.ObjetivoPorDefectoBp))
                Dim costoU6 = If(costoTotal Is Nothing, CType(Nothing, Long?), Convert.ToInt64(costoTotal))
                Return u.Ejecutar("UPDATE minuta SET estado = 'aprobada', moneda_costeo = @mo, costo_previsto_u6 = @c, venta_prevista_u6 = @v, food_cost_objetivo_bp = @o WHERE id = @m",
                                  "mo", moneda.Trim(), "c", If(costoU6.HasValue, CObj(costoU6.Value), Nothing),
                                  "v", If(costoU6.HasValue, CObj(VentaEstructura.Venta(costoU6.Value, objetivo)), Nothing), "o", objetivo, "m", minutaId)
            End Function)
    End Sub

    Public Function ListarMinutas(desde As Date, hasta As Date) As List(Of MinutaDto)
        Dim op = OperacionId
        Return EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT m.id, m.operacion_servicio_id, s.nombre, r.nombre, m.fecha, m.comensales, m.estado, m.moneda_costeo, m.costo_previsto_u6, m.venta_prevista_u6, " &
                "m.food_cost_objetivo_bp FROM minuta m " &
                "JOIN operacion_servicio os ON os.id = m.operacion_servicio_id JOIN servicio s ON s.id = os.servicio_id JOIN regimen r ON r.id = os.regimen_id " &
                "WHERE os.operacion_id = @o AND m.fecha BETWEEN @d AND @h ORDER BY m.fecha, s.nombre",
                Function(rd) New MinutaDto With {.Id = rd.GetInt64(0), .OperacionServicioId = rd.GetInt64(1), .ServicioNombre = rd.GetString(2),
                                                 .RegimenNombre = rd.GetString(3), .Fecha = rd.GetDateTime(4), .Comensales = rd.GetInt64(5),
                                                 .Estado = rd.GetString(6), .MonedaCosteo = rd.TextoONada("moneda_costeo"),
                                                 .CostoPrevistoU6 = rd.LongONada("costo_previsto_u6"), .VentaPrevistaU6 = rd.LongONada("venta_prevista_u6"),
                                                 .FoodCostObjetivoBp = rd.LongONada("food_cost_objetivo_bp")},
                "o", op, "d", desde.Date, "h", hasta.Date))
    End Function

    Public Function ListarPlatos(minutaId As Long) As List(Of PlatoDto)
        Return EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT d.id, d.estructura_id, es.nombre AS estructura, es.orden, d.receta_version_id, r.codigo, r.nombre, rv.version, d.raciones, " &
                "       d.costo_previsto_racion_u6, d.fecha_costeo, " &
                "       CASE WHEN m.estado = 'borrador' THEN 0 ELSE " &
                "         (SELECT count(*) FROM receta_ingrediente i WHERE i.receta_version_id = d.receta_version_id " &
                "           AND NOT EXISTS (SELECT 1 FROM costeo_ingrediente c WHERE c.minuta_detalle_id = d.id AND c.ingrediente_id = i.id)) END AS sin_costo " &
                "FROM minuta_detalle d JOIN minuta m ON m.id = d.minuta_id JOIN estructura_servicio es ON es.id = d.estructura_id " &
                "JOIN receta_version rv ON rv.id = d.receta_version_id JOIN receta r ON r.id = rv.receta_id " &
                "WHERE d.minuta_id = @m ORDER BY es.orden, d.id",
                Function(rd) New PlatoDto With {
                    .Id = rd.Largo("id"), .EstructuraId = rd.Largo("estructura_id"), .EstructuraNombre = rd.Texto("estructura"), .EstructuraOrden = rd.Largo("orden"),
                    .RecetaVersionId = rd.Largo("receta_version_id"), .RecetaCodigo = rd.Texto("codigo"), .RecetaNombre = rd.Texto("nombre"),
                    .Version = rd.Largo("version"), .Raciones = rd.Largo("raciones"), .CostoPrevistoRacionU6 = rd.LongONada("costo_previsto_racion_u6"),
                    .FechaCosteo = If(rd.IsDBNull(rd.GetOrdinal("fecha_costeo")), CType(Nothing, Date?), rd.GetDateTime(rd.GetOrdinal("fecha_costeo"))),
                    .IngredientesSinCosto = rd.Largo("sin_costo")},
                "m", minutaId))
    End Function

    Public Function ListarFijos(minutaId As Long) As List(Of FijoMinutaDto)
        Return EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT f.id, f.producto_base_id, p.descripcion, um.codigo, f.cantidad_base_u6, f.costo_previsto_unitario_u6 FROM minuta_estructura_fija f " &
                "JOIN producto_base p ON p.id = f.producto_base_id JOIN unidad_medida um ON um.id = p.unidad_base_id WHERE f.minuta_id = @m ORDER BY p.descripcion",
                Function(rd) New FijoMinutaDto With {.Id = rd.GetInt64(0), .ProductoBaseId = rd.GetInt64(1), .ProductoDescripcion = rd.GetString(2),
                                                     .Unidad = rd.GetString(3), .CantidadBaseU6 = rd.GetInt64(4),
                                                     .CostoPrevistoUnitarioU6 = If(rd.IsDBNull(5), CType(Nothing, Long?), rd.GetInt64(5))}, "m", minutaId))
    End Function

    ''' <summary>
    ''' Necesidad consolidada por producto base de las minutas indicadas: Σ cantidad bruta × raciones / rendimiento
    ''' (cada ingrediente redondeado con la política u6) más los fijos. Un producto usado por varias recetas sale una sola vez.
    ''' </summary>
    Public Function Necesidades(minutaIds As IEnumerable(Of Long)) As List(Of NecesidadDto)
        Dim ids = minutaIds.Distinct().ToArray()
        Dim op = OperacionId
        Return EnTransaccion(Permisos.MenusVer,
            Function(u)
                For Each id In ids
                    ExigirMinutaDeOperacion(u, id, op, soloBorrador:=False)
                Next
                Dim lineas = u.Consultar(
                    "SELECT p.id, p.codigo, p.descripcion, um.codigo, i.cantidad_base_bruta_u6, rv.rendimiento_raciones_u6, d.raciones " &
                    "FROM minuta_detalle d JOIN receta_version rv ON rv.id = d.receta_version_id " &
                    "JOIN receta_ingrediente i ON i.receta_version_id = rv.id JOIN producto_base p ON p.id = i.producto_base_id " &
                    "JOIN unidad_medida um ON um.id = p.unidad_base_id WHERE d.minuta_id = ANY(@ids) " &
                    "UNION ALL " &
                    "SELECT p.id, p.codigo, p.descripcion, um.codigo, f.cantidad_base_u6, NULL, NULL FROM minuta_estructura_fija f " &
                    "JOIN producto_base p ON p.id = f.producto_base_id JOIN unidad_medida um ON um.id = p.unidad_base_id WHERE f.minuta_id = ANY(@ids)",
                    Function(rd)
                        Dim cantidad = rd.GetInt64(4)
                        If Not rd.IsDBNull(5) Then
                            cantidad = Recetas.NecesidadIngredienteU6(cantidad, rd.GetInt64(5), rd.GetInt64(6) * Numerico.EscalaU6.Factor)
                        End If
                        Return (Id:=rd.GetInt64(0), Codigo:=rd.GetString(1), Descripcion:=rd.GetString(2), Unidad:=rd.GetString(3), Cantidad:=cantidad)
                    End Function, "ids", ids)
                Return lineas.GroupBy(Function(l) l.Id) _
                    .Select(Function(g) New NecesidadDto With {.ProductoBaseId = g.Key, .ProductoCodigo = g.First().Codigo,
                                                               .ProductoDescripcion = g.First().Descripcion, .Unidad = g.First().Unidad,
                                                               .CantidadU6 = g.Sum(Function(l) l.Cantidad), .Origenes = g.Count()}) _
                    .OrderBy(Function(n) n.ProductoDescripcion, StringComparer.Ordinal).ToList()
            End Function)
    End Function

    ' ---------- Verificaciones de alcance ----------

    Private Shared Sub ExigirServicioDeOperacion(u As UnidadDeTrabajo, operacionServicioId As Long, operacionId As Long)
        If u.Escalar("SELECT 1 FROM operacion_servicio WHERE id = @os AND operacion_id = @o", "os", operacionServicioId, "o", operacionId) Is Nothing Then
            Throw New ReglaNegocioException("OPERACION_AJENA", "El servicio no pertenece a la operacion seleccionada.")
        End If
    End Sub

    Private Shared Sub ExigirMinutaDeOperacion(u As UnidadDeTrabajo, minutaId As Long, operacionId As Long, Optional soloBorrador As Boolean = True)
        Dim estado = u.Escalar("SELECT m.estado FROM minuta m JOIN operacion_servicio os ON os.id = m.operacion_servicio_id " &
                               "WHERE m.id = @m AND os.operacion_id = @o", "m", minutaId, "o", operacionId)
        If estado Is Nothing Then Throw New ReglaNegocioException("OPERACION_AJENA", "La minuta no pertenece a la operacion seleccionada.")
        If soloBorrador AndAlso CStr(estado) <> "borrador" Then
            Throw New ReglaNegocioException("MINUTA_APROBADA", "La minuta ya esta aprobada y su contenido no se modifica.")
        End If
    End Sub

End Class
