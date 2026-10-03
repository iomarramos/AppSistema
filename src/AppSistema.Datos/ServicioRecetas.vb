Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Recetas versionadas: la versión nace en borrador, se aprueba (inmutable) y se retira cuando otra la reemplaza.
''' Los ingredientes son productos base en su unidad base; opcionalmente se limitan las variantes permitidas.
''' </summary>
Public NotInheritable Class ServicioRecetas
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    ''' <summary>Crea la receta y su versión 1 en borrador. Devuelve el id de la versión.</summary>
    Public Function CrearReceta(codigo As String, nombre As String, categoria As String, rendimientoRacionesU6 As Long, instrucciones As String) As Long
        If rendimientoRacionesU6 <= 0 Then Throw New ReglaNegocioException("RENDIMIENTO_INVALIDO", "El rendimiento de la receta debe ser mayor que cero.")
        Return EnTransaccion(Permisos.RecetasEditar,
            Function(u)
                Dim recetaId = u.EscalarLong("INSERT INTO receta(empresa_id, codigo, nombre, categoria) VALUES (@e, @c, @n, @cat) RETURNING id",
                                             "e", Sesion.EmpresaId, "c", ServicioAdministracion.Requerido(codigo, "codigo"),
                                             "n", ServicioAdministracion.Requerido(nombre, "nombre"), "cat", Opcional(categoria))
                Return u.EscalarLong("INSERT INTO receta_version(empresa_id, receta_id, version, rendimiento_raciones_u6, instrucciones) " &
                                     "VALUES (@e, @r, 1, @rend, @i) RETURNING id",
                                     "e", Sesion.EmpresaId, "r", recetaId, "rend", rendimientoRacionesU6, "i", Opcional(instrucciones))
            End Function)
    End Function

    ''' <summary>Nueva versión en borrador copiando ingredientes y variantes de la última. Falla si ya hay un borrador.</summary>
    Public Function NuevaVersion(recetaId As Long) As Long
        Return EnTransaccion(Permisos.RecetasEditar,
            Function(u)
                Dim ultima = u.Consultar("SELECT id, version, estado, rendimiento_raciones_u6, instrucciones FROM receta_version " &
                                         "WHERE receta_id = @r ORDER BY version DESC LIMIT 1 FOR UPDATE",
                                         Function(rd) (Id:=rd.GetInt64(0), Version:=rd.GetInt64(1), Estado:=rd.GetString(2),
                                                       Rend:=rd.GetInt64(3), Instr:=rd.TextoONada("instrucciones")), "r", recetaId).SingleOrDefault()
                If ultima.Id = 0 Then Throw New ReglaNegocioException("NO_ENCONTRADO", "La receta no existe.")
                If ultima.Estado = "borrador" Then
                    Throw New ReglaNegocioException("BORRADOR_EXISTENTE", $"La version {ultima.Version} sigue en borrador; editela en lugar de crear otra.")
                End If
                Dim nueva = u.EscalarLong("INSERT INTO receta_version(empresa_id, receta_id, version, rendimiento_raciones_u6, instrucciones) " &
                                          "VALUES (@e, @r, @v, @rend, @i) RETURNING id",
                                          "e", Sesion.EmpresaId, "r", recetaId, "v", ultima.Version + 1, "rend", ultima.Rend, "i", ultima.Instr)
                Dim mapa = u.Consultar(
                    "INSERT INTO receta_ingrediente(empresa_id, receta_version_id, producto_base_id, cantidad_base_bruta_u6, cantidad_base_neta_u6, orden) " &
                    "SELECT empresa_id, @n, producto_base_id, cantidad_base_bruta_u6, cantidad_base_neta_u6, orden FROM receta_ingrediente " &
                    "WHERE receta_version_id = @v ORDER BY orden, id RETURNING id, producto_base_id",
                    Function(rd) (rd.GetInt64(0), rd.GetInt64(1)), "n", nueva, "v", ultima.Id)
                u.Ejecutar("INSERT INTO ingrediente_variante_permitida(empresa_id, ingrediente_id, variante_id) " &
                           "SELECT nv.empresa_id, nv.id, ivp.variante_id FROM ingrediente_variante_permitida ivp " &
                           "JOIN receta_ingrediente vi ON vi.id = ivp.ingrediente_id AND vi.receta_version_id = @v " &
                           "JOIN receta_ingrediente nv ON nv.receta_version_id = @n AND nv.producto_base_id = vi.producto_base_id",
                           "v", ultima.Id, "n", nueva)
                Return nueva
            End Function)
    End Function

    Public Sub ActualizarBorrador(versionId As Long, rendimientoRacionesU6 As Long, instrucciones As String)
        If rendimientoRacionesU6 <= 0 Then Throw New ReglaNegocioException("RENDIMIENTO_INVALIDO", "El rendimiento de la receta debe ser mayor que cero.")
        EnTransaccion(Permisos.RecetasEditar,
            Function(u) ExigirFila(u.Ejecutar("UPDATE receta_version SET rendimiento_raciones_u6 = @r, instrucciones = @i WHERE id = @id",
                                              "r", rendimientoRacionesU6, "i", Opcional(instrucciones), "id", versionId)))
    End Sub

    Public Function AgregarIngrediente(versionId As Long, productoBaseId As Long, cantidadBrutaU6 As Long, cantidadNetaU6 As Long?, orden As Long) As Long
        If cantidadBrutaU6 <= 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "La cantidad bruta debe ser mayor que cero.")
        If cantidadNetaU6.HasValue AndAlso (cantidadNetaU6.Value < 0 OrElse cantidadNetaU6.Value > cantidadBrutaU6) Then
            Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "La cantidad neta debe estar entre cero y la cantidad bruta.")
        End If
        Return EnTransaccion(Permisos.RecetasEditar,
            Function(u) u.EscalarLong("INSERT INTO receta_ingrediente(empresa_id, receta_version_id, producto_base_id, cantidad_base_bruta_u6, cantidad_base_neta_u6, orden) " &
                                      "VALUES (@e, @v, @p, @b, @n, @o) RETURNING id",
                                      "e", Sesion.EmpresaId, "v", versionId, "p", productoBaseId, "b", cantidadBrutaU6,
                                      "n", If(cantidadNetaU6.HasValue, CType(cantidadNetaU6.Value, Object), Nothing), "o", orden))
    End Function

    Public Sub QuitarIngrediente(ingredienteId As Long)
        EnTransaccion(Permisos.RecetasEditar,
            Function(u)
                u.Ejecutar("DELETE FROM ingrediente_variante_permitida WHERE ingrediente_id = @i", "i", ingredienteId)
                Return ExigirFila(u.Ejecutar("DELETE FROM receta_ingrediente WHERE id = @i", "i", ingredienteId))
            End Function)
    End Sub

    ''' <summary>Limita el ingrediente a esa variante (puede llamarse varias veces). Otra variante de otro producto: VARIANTE_INCOMPATIBLE (T07).</summary>
    Public Sub PermitirVariante(ingredienteId As Long, varianteId As Long)
        EnTransaccion(Permisos.RecetasEditar,
            Function(u) u.Ejecutar("INSERT INTO ingrediente_variante_permitida(empresa_id, ingrediente_id, variante_id) VALUES (@e, @i, @v)",
                                   "e", Sesion.EmpresaId, "i", ingredienteId, "v", varianteId))
    End Sub

    ''' <summary>Aprueba la versión y retira la aprobada anterior de la misma receta. Las minutas ya aprobadas no cambian.</summary>
    Public Sub Aprobar(versionId As Long)
        EnTransaccion(Permisos.RecetasAprobar,
            Function(u)
                Dim recetaId = u.EscalarLong("SELECT receta_id FROM receta_version WHERE id = @v FOR UPDATE", "v", versionId)
                u.Ejecutar("UPDATE receta_version SET estado = 'retirada' WHERE receta_id = @r AND estado = 'aprobada'", "r", recetaId)
                Return ExigirFila(u.Ejecutar("UPDATE receta_version SET estado = 'aprobada' WHERE id = @v", "v", versionId))
            End Function)
    End Sub

    Public Sub Retirar(versionId As Long)
        EnTransaccion(Permisos.RecetasAprobar,
            Function(u) ExigirFila(u.Ejecutar("UPDATE receta_version SET estado = 'retirada' WHERE id = @v", "v", versionId)))
    End Sub

    ' ---------- Consultas ----------

    Public Function BuscarRecetas(texto As String) As List(Of RecetaDto)
        Return EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT r.id, r.codigo, r.nombre, r.categoria, r.activo = 1 AS activo, va.id AS va_id, va.version AS va " &
                "FROM receta r LEFT JOIN receta_version va ON va.receta_id = r.id AND va.estado = 'aprobada' " &
                "WHERE @t = '' OR r.codigo ILIKE '%' || @t || '%' OR r.nombre ILIKE '%' || @t || '%' ORDER BY r.nombre LIMIT 500",
                Function(rd) New RecetaDto With {
                    .Id = rd.Largo("id"), .Codigo = rd.Texto("codigo"), .Nombre = rd.Texto("nombre"), .Categoria = rd.TextoONada("categoria"),
                    .Activo = rd.GetBoolean(rd.GetOrdinal("activo")), .VersionAprobadaId = rd.LongONada("va_id"), .VersionAprobada = rd.LongONada("va")},
                "t", If(texto, "").Trim()))
    End Function

    Public Function ListarVersiones(recetaId As Long) As List(Of RecetaVersionDto)
        Return EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar("SELECT id, receta_id, version, rendimiento_raciones_u6, instrucciones, estado FROM receta_version WHERE receta_id = @r ORDER BY version",
                Function(rd) New RecetaVersionDto With {
                    .Id = rd.Largo("id"), .RecetaId = rd.Largo("receta_id"), .Version = rd.Largo("version"),
                    .RendimientoRacionesU6 = rd.Largo("rendimiento_raciones_u6"), .Instrucciones = rd.TextoONada("instrucciones"), .Estado = rd.Texto("estado")},
                "r", recetaId))
    End Function

    Public Function ListarIngredientes(versionId As Long) As List(Of IngredienteDto)
        Return EnTransaccion(Permisos.MenusVer,
            Function(u) u.Consultar(
                "SELECT i.id, i.producto_base_id, p.codigo, p.descripcion, um.codigo AS unidad, i.cantidad_base_bruta_u6, i.cantidad_base_neta_u6, i.orden, " &
                "  COALESCE((SELECT string_agg(v.codigo, ', ' ORDER BY v.codigo) FROM ingrediente_variante_permitida ivp " &
                "            JOIN variante_producto v ON v.id = ivp.variante_id WHERE ivp.ingrediente_id = i.id), '') AS variantes " &
                "FROM receta_ingrediente i JOIN producto_base p ON p.id = i.producto_base_id JOIN unidad_medida um ON um.id = p.unidad_base_id " &
                "WHERE i.receta_version_id = @v ORDER BY i.orden, i.id",
                Function(rd) New IngredienteDto With {
                    .Id = rd.Largo("id"), .ProductoBaseId = rd.Largo("producto_base_id"), .ProductoCodigo = rd.Texto("codigo"),
                    .ProductoDescripcion = rd.Texto("descripcion"), .Unidad = rd.Texto("unidad"), .CantidadBrutaU6 = rd.Largo("cantidad_base_bruta_u6"),
                    .CantidadNetaU6 = rd.LongONada("cantidad_base_neta_u6"), .Orden = rd.Largo("orden"), .VariantesPermitidas = rd.Texto("variantes")},
                "v", versionId))
    End Function

    ''' <summary>Simulación del costo de una versión a una fecha (no guarda nada). Regla de precio provisional D02.</summary>
    Public Function CostoSimulado(versionId As Long, fecha As Date, moneda As String) As CostoRecetaDto
        Return EnTransaccion(Permisos.MenusVer, Function(u) CosteoBD.CostearVersion(u, versionId, fecha, moneda))
    End Function

    Friend Shared Function Opcional(valor As String) As String
        Return If(String.IsNullOrWhiteSpace(valor), Nothing, valor.Trim())
    End Function

    Friend Shared Function ExigirFila(filas As Integer) As Integer
        If filas = 0 Then Throw New ReglaNegocioException("NO_ENCONTRADO", "El registro no existe o no pertenece a la empresa.")
        Return filas
    End Function

End Class

''' <summary>
''' Costo de ingredientes con fuente explícita. Regla PROVISIONAL (D02 pendiente): para cada ingrediente se toma
''' el menor costo por unidad base entre los precios de compra vigentes a la fecha, en la moneda indicada, de las
''' variantes permitidas (o de todas las del producto si no hay restricción). Sin precio → costo pendiente.
''' El precio se usa tal como se registró (con o sin impuesto, D03 pendiente).
''' </summary>
Friend Module CosteoBD

    Public Const Regla As String = "menor costo vigente (regla provisional D02)"

    Public Function CostearVersion(u As UnidadDeTrabajo, versionId As Long, fecha As Date, moneda As String) As CostoRecetaDto
        Dim m = ServicioAdministracion.Requerido(moneda, "moneda")
        Dim r As New CostoRecetaDto With {.RecetaVersionId = versionId}
        r.RendimientoRacionesU6 = u.EscalarLong("SELECT rendimiento_raciones_u6 FROM receta_version WHERE id = @v", "v", versionId)

        Dim ingredientes = u.Consultar(
            "SELECT i.id, p.descripcion, um.codigo, i.cantidad_base_bruta_u6 FROM receta_ingrediente i " &
            "JOIN producto_base p ON p.id = i.producto_base_id JOIN unidad_medida um ON um.id = p.unidad_base_id " &
            "WHERE i.receta_version_id = @v ORDER BY i.orden, i.id",
            Function(rd) New CostoIngredienteDto With {.IngredienteId = rd.GetInt64(0), .ProductoDescripcion = rd.GetString(1),
                                                       .Unidad = rd.GetString(2), .CantidadBrutaU6 = rd.GetInt64(3)}, "v", versionId)

        Dim precios = u.Consultar(
            "SELECT i.id AS ingrediente, pc.id AS precio, pc.precio_empaque_u6, e.envases_por_empaque, v.contenido_base_por_envase_u6, " &
            "       pr.codigo AS proveedor, e.codigo AS empaque, v.codigo AS variante, pc.fecha_desde " &
            "FROM receta_ingrediente i " &
            "JOIN variante_producto v ON v.producto_base_id = i.producto_base_id AND v.activo = 1 " &
            "JOIN empaque_compra e ON e.variante_id = v.id AND e.activo = 1 " &
            "JOIN proveedor_empaque pe ON pe.empaque_id = e.id AND pe.activo = 1 " &
            "JOIN proveedor pr ON pr.id = pe.proveedor_id " &
            "JOIN precio_compra pc ON pc.proveedor_empaque_id = pe.id AND pc.moneda = @m " &
            "     AND pc.fecha_desde <= @f AND (pc.fecha_hasta IS NULL OR pc.fecha_hasta >= @f) " &
            "WHERE i.receta_version_id = @v " &
            "  AND (NOT EXISTS (SELECT 1 FROM ingrediente_variante_permitida x WHERE x.ingrediente_id = i.id) " &
            "       OR EXISTS (SELECT 1 FROM ingrediente_variante_permitida x WHERE x.ingrediente_id = i.id AND x.variante_id = v.id))",
            Function(rd) (Ingrediente:=rd.GetInt64(0), Precio:=rd.GetInt64(1),
                          CostoU6:=Costeo.CostoUnitarioBaseU6(rd.GetInt64(2), rd.GetInt64(3), rd.GetInt64(4)),
                          Fuente:=$"precio {rd.GetInt64(1)}: proveedor {rd.GetString(5)}, empaque {rd.GetString(6)}, variante {rd.GetString(7)}",
                          Fecha:=rd.GetDateTime(8)),
            "v", versionId, "m", m, "f", fecha.Date)

        For Each ing In ingredientes
            Dim mejor = precios.Where(Function(p) p.Ingrediente = ing.IngredienteId) _
                               .OrderBy(Function(p) p.CostoU6).ThenBy(Function(p) p.Precio).ToList()
            If mejor.Count = 0 Then
                ing.Fuente = $"sin precio vigente en {m} al {fecha:dd/MM/yyyy}"
            Else
                ing.CostoUnitarioBaseU6 = mejor(0).CostoU6
                ing.CostoLineaU6 = Numerico.EscalaU6.Multiplicar(ing.CantidadBrutaU6, mejor(0).CostoU6)
                ing.FechaPrecio = mejor(0).Fecha
                ing.Fuente = mejor(0).Fuente & " (" & Regla & ")"
            End If
            r.Ingredientes.Add(ing)
        Next
        r.CostoRacionU6 = Costeo.CostoRacionU6(r.Ingredientes.Select(Function(i) (i.CantidadBrutaU6, i.CostoUnitarioBaseU6)), r.RendimientoRacionesU6)
        Return r
    End Function

    ''' <summary>Menor costo por unidad base vigente de cualquier variante del producto; Nothing si no hay precio.</summary>
    Public Function CostoProducto(u As UnidadDeTrabajo, productoBaseId As Long, fecha As Date, moneda As String) As Object
        Dim costos = u.Consultar(
            "SELECT pc.precio_empaque_u6, e.envases_por_empaque, v.contenido_base_por_envase_u6 FROM variante_producto v " &
            "JOIN empaque_compra e ON e.variante_id = v.id AND e.activo = 1 " &
            "JOIN proveedor_empaque pe ON pe.empaque_id = e.id AND pe.activo = 1 " &
            "JOIN precio_compra pc ON pc.proveedor_empaque_id = pe.id AND pc.moneda = @m " &
            "     AND pc.fecha_desde <= @f AND (pc.fecha_hasta IS NULL OR pc.fecha_hasta >= @f) " &
            "WHERE v.producto_base_id = @p AND v.activo = 1",
            Function(rd) Costeo.CostoUnitarioBaseU6(rd.GetInt64(0), rd.GetInt64(1), rd.GetInt64(2)),
            "p", productoBaseId, "m", ServicioAdministracion.Requerido(moneda, "moneda"), "f", fecha.Date)
        Return If(costos.Count = 0, Nothing, CType(costos.Min(), Object))
    End Function

End Module
