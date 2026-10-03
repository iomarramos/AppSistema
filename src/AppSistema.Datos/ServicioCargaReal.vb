Imports System.Globalization
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

Public NotInheritable Class ResultadoCargaReal
    Public Property Nuevos As Integer
    Public Property YaEstaban As Integer
    Public ReadOnly Property Problemas As New List(Of String)

    Public Overrides Function ToString() As String
        Return $"{Nuevos} nuevos, {YaEstaban} ya estaban, {Problemas.Count} con problema"
    End Function
End Class

Public NotInheritable Class EstadoCargaReal
    Public Property Productos As Long
    Public Property Presentaciones As Long
    Public Property PreciosSgp As Long
    Public Property RecetasAprobadas As Long
    Public Property InsumosSinCosto As Long
    Public Property ProductosActivos As Long
    Public Property PresentacionesConFamilia As Long
    Public Property AlmacenesConApertura As Long
    Public Property Almacenes As Long
    Public Property ServiciosAsignados As Long
    Public Property Minutas As Long
    Public Property MinutasAprobadas As Long
End Class

''' <summary>
''' Carga del juego de datos real ordenado por herramientas/ordenar_datos_reales.py (datos/real/): precios por presentación,
''' estructuras de menú con factores y el ciclo de minutas. Cada paso es repetible: lo que ya existe no se duplica ni se pisa.
''' </summary>
Public NotInheritable Class ServicioCargaReal
    Inherits ServicioConSesion

    Public Const ProveedorPrecios As String = "SGP"

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    ''' <summary>Columnas que debe traer cada archivo: un archivo equivocado se rechaza antes de cargar nada.</summary>
    Public Shared ReadOnly ColumnasPrecios As String() = {"variante_codigo", "empaque_codigo", "precio_envase", "fecha_precio"}
    Public Shared ReadOnly ColumnasSinCosto As String() = {"producto"}
    Public Shared ReadOnly ColumnasProductosActivos As String() = {"variante_codigo", "motivo"}
    Public Shared ReadOnly ColumnasFamilias As String() = {"variante_codigo", "familia", "subfamilia", "grupo"}
    Public Shared ReadOnly ColumnasEstructuras As String() = {"servicio", "orden", "codigo", "nombre", "factor_consumo_pct"}
    Public Shared ReadOnly ColumnasCiclo As String() = {"dia", "servicio", "estructura_codigo", "receta_codigo", "receta_nombre", "reparto_pct"}

    Private Shared Function Filas(texto As String, obligatorias As String()) As List(Of Dictionary(Of String, String))
        Dim lineas = If(texto, "").Replace(vbCr, "").Split(ChrW(10)).Where(Function(l) l.Trim() <> "").ToList()
        If lineas.Count = 0 Then Throw New ReglaNegocioException("ARCHIVO_INVALIDO", "El archivo esta vacio.")
        Dim cab = lineas(0).TrimStart(ChrW(&HFEFF)).Split(";"c).Select(Function(c) c.Trim()).ToArray()
        Dim faltan = obligatorias.Where(Function(o) Not cab.Contains(o, StringComparer.OrdinalIgnoreCase)).ToList()
        If faltan.Count > 0 Then
            Throw New ReglaNegocioException("ARCHIVO_INVALIDO", $"El archivo no es el esperado: le faltan las columnas {String.Join(", ", faltan)}.")
        End If
        Return lineas.Skip(1).Select(Function(l)
                                         Dim v = l.Split(";"c)
                                         Dim d As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
                                         For i = 0 To cab.Length - 1
                                             d(cab(i)) = If(i < v.Length, v(i).Trim(), "")
                                         Next
                                         Return d
                                     End Function).ToList()
    End Function

    Private Shared Function U6(texto As String) As Long
        Return EscalaU6.DesdeDecimal(Decimal.Parse(texto, CultureInfo.InvariantCulture))
    End Function

    ' ---------- Precios ----------

    ''' <summary>
    ''' precios_sgp.csv: un precio por presentación (variante SGP) desde su fecha, del proveedor "SGP". Una presentación que ya
    ''' tiene precio de ese proveedor no se toca (regla del usuario: sin precio no se inventa; con precio no se pisa).
    ''' </summary>
    Public Function ImportarPrecios(texto As String) As ResultadoCargaReal
        Dim filas = ServicioCargaReal.Filas(texto, ColumnasPrecios)
        Return EnTransaccion(Permisos.PreciosEditar,
            Function(u)
                Dim r As New ResultadoCargaReal()
                Dim proveedor = u.Escalar("SELECT id FROM proveedor WHERE codigo = @c", "c", ProveedorPrecios)
                If proveedor Is Nothing Then
                    proveedor = u.EscalarLong("INSERT INTO proveedor(empresa_id, codigo, nombre, es_caja_chica) VALUES (@e, @c, @n, 0) RETURNING id",
                                              "e", Sesion.EmpresaId, "c", ProveedorPrecios, "n", "Precios SGP (ultimo precio de compra e inventario)")
                End If
                Dim empaques = u.Consultar("SELECT v.codigo, e.codigo, e.id, e.envases_por_empaque FROM empaque_compra e JOIN variante_producto v ON v.id = e.variante_id",
                                           Function(rd) (Clave:=rd.GetString(0) & "|" & rd.GetString(1), Id:=rd.GetInt64(2), Envases:=rd.GetInt64(3))).
                                 ToDictionary(Function(x) x.Clave)
                Dim conPrecio = New HashSet(Of Long)(u.Consultar(
                    "SELECT pe.empaque_id FROM proveedor_empaque pe JOIN precio_compra p ON p.proveedor_empaque_id = pe.id WHERE pe.proveedor_id = @p",
                    Function(rd) rd.GetInt64(0), "p", proveedor))
                For Each f In filas
                    Dim e As (Clave As String, Id As Long, Envases As Long) = Nothing
                    If Not empaques.TryGetValue(f("variante_codigo") & "|" & f("empaque_codigo"), e) Then
                        r.Problemas.Add($"{f("variante_codigo")}: la presentacion no esta en el catalogo (cargue antes catalogo_por_ingrediente.csv)")
                        Continue For
                    End If
                    If conPrecio.Contains(e.Id) Then r.YaEstaban += 1 : Continue For
                    Dim pe = u.Escalar("SELECT id FROM proveedor_empaque WHERE proveedor_id = @p AND empaque_id = @e", "p", proveedor, "e", e.Id)
                    If pe Is Nothing Then
                        pe = u.EscalarLong("INSERT INTO proveedor_empaque(empresa_id, proveedor_id, empaque_id, plazo_entrega_dias) VALUES (@em, @p, @e, 1) RETURNING id",
                                           "em", Sesion.EmpresaId, "p", proveedor, "e", e.Id)
                    End If
                    u.Ejecutar("INSERT INTO precio_compra(empresa_id, proveedor_empaque_id, fecha_desde, fecha_hasta, moneda, precio_empaque_u6, incluye_impuesto) " &
                               "VALUES (@em, @pe, @d, NULL, 'PEN', @p, 0)",
                               "em", Sesion.EmpresaId, "pe", pe, "d", Date.ParseExact(f("fecha_precio"), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                               "p", U6(f("precio_envase")) * e.Envases)
                    conPrecio.Add(e.Id)
                    r.Nuevos += 1
                Next
                Return r
            End Function)
    End Function

    ''' <summary>
    ''' D02: productos activos (liberados) de la operación de la sesión desde productos_activos.csv: por ingrediente, la
    ''' presentación que la operación usa (la que tiene stock o la de compra más reciente en el SGP). Si el ingrediente ya
    ''' tiene producto activo en la operación, se respeta: el archivo no lo cambia.
    ''' </summary>
    Public Function LiberarProductos(texto As String) As ResultadoCargaReal
        Dim filas = ServicioCargaReal.Filas(texto, ColumnasProductosActivos)
        Dim op = Sesion.OperacionId
        If Not op.HasValue Then Throw New ReglaNegocioException("OPERACION_NO_SELECCIONADA", "Seleccione una operacion.")
        Return EnTransaccion(Permisos.CatalogoEditar,
            Function(u)
                Dim r As New ResultadoCargaReal()
                For Each f In filas
                    Dim v = u.Consultar("SELECT id, producto_base_id FROM variante_producto WHERE codigo = @c AND activo = 1",
                                        Function(rd) (Id:=rd.GetInt64(0), Producto:=rd.GetInt64(1)), "c", f("variante_codigo")).SingleOrDefault()
                    If v.Id = 0 Then
                        r.Problemas.Add($"Presentacion {f("variante_codigo")}: no existe en el catalogo o esta inactiva")
                        Continue For
                    End If
                    Dim n = u.Ejecutar(
                        "INSERT INTO producto_operacion(empresa_id, operacion_id, producto_base_id, variante_id, motivo, usuario_id) " &
                        "VALUES (@e, @o, @p, @v, @m, @u) ON CONFLICT (empresa_id, operacion_id, producto_base_id) DO NOTHING",
                        "e", Sesion.EmpresaId, "o", op.Value, "p", v.Producto, "v", v.Id, "m", f("motivo"), "u", Sesion.UsuarioId)
                    If n = 1 Then r.Nuevos += 1 Else r.YaEstaban += 1
                Next
                Return r
            End Function)
    End Function

    ''' <summary>
    ''' Familias del SGP (familias_sgp.csv): crea la jerarquía familia › subfamilia › grupo y asigna a cada presentación su
    ''' nivel más detallado. El ingrediente sin categoría toma la familia más frecuente de sus presentaciones. Lo que ya
    ''' tiene categoría no se cambia.
    ''' </summary>
    Public Function CargarFamilias(texto As String) As ResultadoCargaReal
        Dim filas = ServicioCargaReal.Filas(texto, ColumnasFamilias)
        Return EnTransaccion(Permisos.CatalogoEditar,
            Function(u)
                Dim r As New ResultadoCargaReal()
                Dim categorias = u.Consultar("SELECT codigo, id FROM categoria_producto", Function(rd) (rd.GetString(0), rd.GetInt64(1))) _
                                  .ToDictionary(Function(x) x.Item1, Function(x) x.Item2, StringComparer.Ordinal)
                Dim nodo = Function(codigo As String, nombre As String, padre As Long?) As Long
                               Dim id As Long
                               If categorias.TryGetValue(codigo, id) Then Return id
                               id = u.EscalarLong("INSERT INTO categoria_producto(empresa_id, codigo, nombre, padre_id) VALUES (@e, @c, @n, @p) RETURNING id",
                                                  "e", Sesion.EmpresaId, "c", codigo, "n", nombre, "p", If(padre.HasValue, CObj(padre.Value), Nothing))
                               categorias(codigo) = id
                               Return id
                           End Function
                For Each f In filas
                    Dim id As Long = nodo(f("familia"), f("familia"), Nothing)
                    Dim ruta = f("familia")
                    For Each nivel In {f("subfamilia"), f("grupo")}.Where(Function(x) x <> "")
                        ruta &= " / " & nivel
                        id = nodo(ruta, nivel, id)
                    Next
                    Dim n = u.Ejecutar("UPDATE variante_producto SET categoria_id = @c WHERE codigo = @v AND categoria_id IS NULL", "c", id, "v", f("variante_codigo"))
                    If n = 1 Then
                        r.Nuevos += 1
                    ElseIf u.Escalar("SELECT 1 FROM variante_producto WHERE codigo = @v", "v", f("variante_codigo")) Is Nothing Then
                        r.Problemas.Add($"Presentacion {f("variante_codigo")}: no existe en el catalogo")
                    Else
                        r.YaEstaban += 1
                    End If
                Next
                ' Ingredientes sin categoría: la familia (raíz) más frecuente entre sus presentaciones.
                u.Ejecutar(
                    "WITH RECURSIVE raiz AS (SELECT id, id AS raiz_id FROM categoria_producto WHERE padre_id IS NULL " &
                    "  UNION ALL SELECT c.id, r.raiz_id FROM categoria_producto c JOIN raiz r ON c.padre_id = r.id), " &
                    "conteo AS (SELECT v.producto_base_id, r.raiz_id, count(*) AS n, row_number() OVER (PARTITION BY v.producto_base_id ORDER BY count(*) DESC, r.raiz_id) AS k " &
                    "  FROM variante_producto v JOIN raiz r ON r.id = v.categoria_id GROUP BY v.producto_base_id, r.raiz_id) " &
                    "UPDATE producto_base p SET categoria_id = c.raiz_id FROM conteo c WHERE c.producto_base_id = p.id AND c.k = 1 AND p.categoria_id IS NULL")
                Return r
            End Function)
    End Function

    ''' <summary>Insumos que no se compran (agua de red): se costean en S/ 0. Los nombres vienen de datos/real/insumos_sin_costo.csv.</summary>
    Public Function MarcarInsumosSinCosto(texto As String) As ResultadoCargaReal
        Dim nombres = ServicioCargaReal.Filas(texto, ColumnasSinCosto).Select(Function(f) f("producto")).ToList()
        Dim r As New ResultadoCargaReal With {.Nuevos = New ServicioCatalogo(CadenaConexion, Sesion).MarcarSinCosto(nombres, True)}
        r.YaEstaban = nombres.Count - r.Nuevos
        Return r
    End Function

    ' ---------- Estructuras de menú ----------

    ''' <summary>
    ''' estructuras_menu.csv: crea los servicios (Desayuno, Almuerzo, Cena), sus componentes con el factor teórico, el régimen
    ''' General y los asigna a la operación de la sesión. Lo existente se conserva (incluido un factor ya ajustado).
    ''' </summary>
    Public Function CargarEstructuras(texto As String) As ResultadoCargaReal
        Dim filas = ServicioCargaReal.Filas(texto, ColumnasEstructuras)
        Dim op = Sesion.Operacion
        If op Is Nothing Then Throw New ReglaNegocioException("OPERACION_NO_SELECCIONADA", "Seleccione una operacion.")
        Return EnTransaccion(Permisos.MenusConfigurar,
            Function(u)
                Dim r As New ResultadoCargaReal()
                Dim regimen = u.Escalar("SELECT id FROM regimen WHERE codigo = 'GEN'")
                If regimen Is Nothing Then regimen = u.EscalarLong("INSERT INTO regimen(empresa_id, codigo, nombre) VALUES (@e, 'GEN', 'General') RETURNING id", "e", Sesion.EmpresaId)
                For Each grupo In filas.GroupBy(Function(f) f("servicio"))
                    Dim codServicio = CodigoServicio(grupo.Key)
                    Dim servicio = u.Escalar("SELECT id FROM servicio WHERE codigo = @c", "c", codServicio)
                    If servicio Is Nothing Then
                        servicio = u.EscalarLong("INSERT INTO servicio(empresa_id, codigo, nombre) VALUES (@e, @c, @n) RETURNING id",
                                                 "e", Sesion.EmpresaId, "c", codServicio, "n", Titulo(grupo.Key))
                    End If
                    For Each f In grupo
                        If u.Escalar("SELECT 1 FROM estructura_servicio WHERE servicio_id = @s AND codigo = @c", "s", servicio, "c", f("codigo")) IsNot Nothing Then
                            r.YaEstaban += 1
                            Continue For
                        End If
                        u.Ejecutar("INSERT INTO estructura_servicio(empresa_id, servicio_id, codigo, nombre, orden, factor_consumo_bp) VALUES (@e, @s, @c, @n, @o, @f)",
                                   "e", Sesion.EmpresaId, "s", servicio, "c", f("codigo"), "n", f("nombre"), "o", Long.Parse(f("orden"), CultureInfo.InvariantCulture),
                                   "f", Long.Parse(f("factor_consumo_pct"), CultureInfo.InvariantCulture) * 100L)
                        r.Nuevos += 1
                    Next
                    u.Ejecutar("INSERT INTO operacion_servicio(empresa_id, operacion_id, servicio_id, regimen_id) VALUES (@e, @o, @s, @r) " &
                               "ON CONFLICT (empresa_id, operacion_id, servicio_id, regimen_id) DO NOTHING",
                               "e", Sesion.EmpresaId, "o", op.Id, "s", servicio, "r", regimen)
                Next
                Return r
            End Function)
    End Function

    Private Shared Function CodigoServicio(nombre As String) As String
        Select Case nombre.ToUpperInvariant()
            Case "DESAYUNO" : Return "DES"
            Case "ALMUERZO" : Return "ALM"
            Case "CENA" : Return "CEN"
            Case Else : Return nombre.ToUpperInvariant().Substring(0, Math.Min(3, nombre.Length))
        End Select
    End Function

    Private Shared Function Titulo(nombre As String) As String
        Return CultureInfo.GetCultureInfo("es-PE").TextInfo.ToTitleCase(nombre.ToLowerInvariant())
    End Function

    ' ---------- Ciclo de minutas ----------

    ''' <summary>
    ''' ciclo_menu.csv: crea las minutas del ciclo a partir de la fecha indicada (día 1 = desde), con los comensales de cada
    ''' servicio; cada receta entra con el factor vigente del componente y su reparto. Un día y servicio que ya tiene minuta
    ''' se deja como está. Con aprobar, las minutas nuevas se aprueban (costo y venta previstos con los precios vigentes).
    ''' </summary>
    Public Function CargarCiclo(texto As String, desde As Date, comensales As IDictionary(Of String, Long), aprobar As Boolean,
                                Optional dias As Integer = Integer.MaxValue) As ResultadoCargaReal
        Dim filas = ServicioCargaReal.Filas(texto, ColumnasCiclo).Where(Function(f) Integer.Parse(f("dia"), CultureInfo.InvariantCulture) <= dias).ToList()
        Dim minutas As New ServicioMinutas(CadenaConexion, Sesion)
        Dim r As New ResultadoCargaReal()
        Dim servicios = minutas.ListarServiciosDeOperacion().Where(Function(s) s.RegimenNombre = "General").
                        ToDictionary(Function(s) s.ServicioNombre.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase)
        Dim recetas = EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar("SELECT r.codigo, va.id FROM receta r JOIN receta_version va ON va.receta_id = r.id AND va.estado = 'aprobada'",
                                    Function(rd) (rd.GetString(0), rd.GetInt64(1))).ToDictionary(Function(x) x.Item1, Function(x) x.Item2))
        Dim existentes = New HashSet(Of String)(minutas.ListarMinutas(desde, desde.AddDays(400)).Select(Function(m) $"{m.Fecha:yyyy-MM-dd}|{m.OperacionServicioId}"))
        For Each grupo In filas.GroupBy(Function(f) (Dia:=Integer.Parse(f("dia"), CultureInfo.InvariantCulture), Servicio:=f("servicio")))
            Dim os As OperacionServicioDto = Nothing
            If Not servicios.TryGetValue(grupo.Key.Servicio, os) Then
                r.Problemas.Add($"Dia {grupo.Key.Dia}: el servicio {grupo.Key.Servicio} no esta asignado a la operacion (cargue antes las estructuras)")
                Continue For
            End If
            Dim fecha = desde.Date.AddDays(grupo.Key.Dia - 1)
            If existentes.Contains($"{fecha:yyyy-MM-dd}|{os.Id}") Then r.YaEstaban += 1 : Continue For
            Dim n As Long = 0
            If Not comensales.TryGetValue(grupo.Key.Servicio.ToUpperInvariant(), n) OrElse n <= 0 Then Continue For
            Dim estructuras = minutas.ListarEstructuras(os.ServicioId).ToDictionary(Function(e) e.Codigo)
            Dim minuta = minutas.CrearMinuta(os.Id, fecha, n)
            For Each f In grupo
                Dim version As Long
                If Not recetas.TryGetValue(f("receta_codigo"), version) Then
                    r.Problemas.Add($"{fecha:dd/MM}: receta {f("receta_codigo")} sin version aprobada (importe antes recetas_reales.csv --aprobar)")
                    Continue For
                End If
                Dim estructura As EstructuraDto = Nothing
                If Not estructuras.TryGetValue(f("estructura_codigo"), estructura) Then Continue For
                Try
                    minutas.AgregarPlatoPorFactor(minuta, estructura.Id, version, Long.Parse(f("reparto_pct"), CultureInfo.InvariantCulture) * 100L)
                Catch ex As ReglaNegocioException
                    r.Problemas.Add($"{fecha:dd/MM} {f("receta_nombre")}: {ex.Message}")
                End Try
            Next
            If aprobar Then
                Try
                    minutas.Aprobar(minuta, "PEN")
                Catch ex As ReglaNegocioException
                    r.Problemas.Add($"{fecha:dd/MM} {grupo.Key.Servicio}: no se aprobo ({ex.Message})")
                End Try
            End If
            r.Nuevos += 1
        Next
        Return r
    End Function

    ' ---------- Estado ----------

    ''' <summary>Qué hay cargado en la empresa y en la operación de la sesión, para guiar los pasos de la carga.</summary>
    Public Function Estado() As EstadoCargaReal
        Dim op = Sesion.OperacionId
        Return EnTransaccion(Permisos.CatalogoImportar,
            Function(u) u.Consultar(
                "SELECT (SELECT count(*) FROM producto_base), (SELECT count(*) FROM variante_producto), " &
                "       (SELECT count(*) FROM precio_compra pc JOIN proveedor_empaque pe ON pe.id = pc.proveedor_empaque_id " &
                "          JOIN proveedor p ON p.id = pe.proveedor_id WHERE p.codigo = @sgp), " &
                "       (SELECT count(*) FROM receta_version WHERE estado = 'aprobada'), " &
                "       (SELECT count(*) FROM producto_base WHERE sin_costo_compra), " &
                "       (SELECT count(*) FROM producto_operacion WHERE operacion_id = @o), " &
                "       (SELECT count(*) FROM variante_producto WHERE categoria_id IS NOT NULL), " &
                "       (SELECT count(DISTINCT d.almacen_id) FROM documento_stock d JOIN almacen a ON a.id = d.almacen_id WHERE d.tipo = 'apertura' AND a.operacion_id = @o), " &
                "       (SELECT count(*) FROM almacen WHERE operacion_id = @o), " &
                "       (SELECT count(*) FROM operacion_servicio WHERE operacion_id = @o), " &
                "       (SELECT count(*) FROM minuta m JOIN operacion_servicio os ON os.id = m.operacion_servicio_id WHERE os.operacion_id = @o), " &
                "       (SELECT count(*) FROM minuta m JOIN operacion_servicio os ON os.id = m.operacion_servicio_id WHERE os.operacion_id = @o AND m.estado <> 'borrador')",
                Function(rd) New EstadoCargaReal With {
                    .Productos = rd.GetInt64(0), .Presentaciones = rd.GetInt64(1), .PreciosSgp = rd.GetInt64(2), .RecetasAprobadas = rd.GetInt64(3),
                    .InsumosSinCosto = rd.GetInt64(4), .ProductosActivos = rd.GetInt64(5), .PresentacionesConFamilia = rd.GetInt64(6),
                    .AlmacenesConApertura = rd.GetInt64(7), .Almacenes = rd.GetInt64(8),
                    .ServiciosAsignados = rd.GetInt64(9), .Minutas = rd.GetInt64(10), .MinutasAprobadas = rd.GetInt64(11)},
                "sgp", ProveedorPrecios, "o", op).Single())
    End Function

End Class
