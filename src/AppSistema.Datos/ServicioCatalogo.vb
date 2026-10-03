Imports Npgsql
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Catalogo
Imports AppSistema.Dominio.Seguridad

''' <summary>Unidades, categorías, marcas, productos base, variantes y empaques de la empresa de la sesión.</summary>
Public NotInheritable Class ServicioCatalogo
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    Private Shared Function Req(valor As String, campo As String) As String
        Return ServicioAdministracion.Requerido(valor, campo)
    End Function

    Private Shared Function Opc(valor As String) As String
        Return If(String.IsNullOrWhiteSpace(valor), Nothing, valor.Trim())
    End Function

    ' ---------- Unidades ----------

    Public Function CrearUnidad(codigo As String, nombre As String, dimension As Dimension, factorABaseU6 As Long) As Long
        Dim unidad As New UnidadMedida(Req(codigo, "codigo"), dimension, factorABaseU6)   ' valida factor
        Return EnTransaccion(Permisos.CatalogoEditar,
            Function(u) u.EscalarLong("INSERT INTO unidad_medida(empresa_id, codigo, nombre, dimension, factor_a_base_u6) VALUES (@e, @c, @n, @d, @f) RETURNING id",
                                      "e", Sesion.EmpresaId, "c", unidad.Codigo, "n", Req(nombre, "nombre"),
                                      "d", dimension.ToString().ToLowerInvariant(), "f", factorABaseU6))
    End Function

    Public Function ListarUnidades() As List(Of UnidadMedidaDto)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar("SELECT id, codigo, nombre, dimension, factor_a_base_u6 FROM unidad_medida ORDER BY codigo",
                Function(rd) New UnidadMedidaDto With {.Id = rd.GetInt64(0), .Codigo = rd.GetString(1), .Nombre = rd.GetString(2),
                                                      .Dimension = rd.GetString(3), .FactorABaseU6 = rd.GetInt64(4)}))
    End Function

    ' ---------- Categorías y marcas ----------

    Public Function CrearCategoria(codigo As String, nombre As String) As Long
        Return EnTransaccion(Permisos.CatalogoEditar,
            Function(u) u.EscalarLong("INSERT INTO categoria_producto(empresa_id, codigo, nombre) VALUES (@e, @c, @n) RETURNING id",
                                      "e", Sesion.EmpresaId, "c", Req(codigo, "codigo"), "n", Req(nombre, "nombre")))
    End Function

    Public Function ListarCategorias() As List(Of CategoriaDto)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar("SELECT id, codigo, nombre FROM categoria_producto ORDER BY nombre",
                Function(rd) New CategoriaDto With {.Id = rd.GetInt64(0), .Codigo = rd.GetString(1), .Nombre = rd.GetString(2)}))
    End Function

    Public Function CrearMarca(nombre As String) As Long
        Return EnTransaccion(Permisos.CatalogoEditar,
            Function(u) u.EscalarLong("INSERT INTO marca(empresa_id, nombre) VALUES (@e, @n) RETURNING id",
                                      "e", Sesion.EmpresaId, "n", Req(nombre, "nombre")))
    End Function

    Public Function ListarMarcas() As List(Of MarcaDto)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar("SELECT id, nombre FROM marca ORDER BY nombre",
                Function(rd) New MarcaDto With {.Id = rd.GetInt64(0), .Nombre = rd.GetString(1)}))
    End Function

    ' ---------- Productos base ----------

    Public Function CrearProducto(codigo As String, descripcion As String, especificacion As String, unidadBaseId As Long, categoriaId As Long?) As Long
        Return EnTransaccion(Permisos.CatalogoEditar,
            Function(u) u.EscalarLong(
                "INSERT INTO producto_base(empresa_id, codigo, descripcion, especificacion, unidad_base_id, categoria_id) " &
                "VALUES (@e, @c, @d, @s, @un, @cat) RETURNING id",
                "e", Sesion.EmpresaId, "c", Req(codigo, "codigo"), "d", Req(descripcion, "descripcion"),
                "s", Opc(especificacion), "un", unidadBaseId, "cat", categoriaId))
    End Function

    Private Const SelectProducto As String =
        "SELECT p.id, p.codigo, p.descripcion, p.especificacion, p.unidad_base_id, um.codigo AS unidad, p.categoria_id, c.codigo AS categoria, " &
        "p.activo = 1 AS activo, p.sin_costo_compra, p.xmin::text AS version FROM producto_base p " &
        "JOIN unidad_medida um ON um.empresa_id = p.empresa_id AND um.id = p.unidad_base_id " &
        "LEFT JOIN categoria_producto c ON c.empresa_id = p.empresa_id AND c.id = p.categoria_id "

    Private Shared Function LeerProducto(rd As NpgsqlDataReader) As ProductoBaseDto
        Return New ProductoBaseDto With {
            .Id = rd.Largo("id"), .Codigo = rd.Texto("codigo"), .Descripcion = rd.Texto("descripcion"),
            .Especificacion = rd.TextoONada("especificacion"), .UnidadBaseId = rd.Largo("unidad_base_id"), .UnidadCodigo = rd.Texto("unidad"),
            .CategoriaId = rd.LongONada("categoria_id"), .CategoriaCodigo = rd.TextoONada("categoria"),
            .Activo = rd.GetBoolean(rd.GetOrdinal("activo")), .SinCostoCompra = rd.GetBoolean(rd.GetOrdinal("sin_costo_compra")), .Version = rd.Texto("version")}
    End Function

    ''' <summary>Busca por código, descripción o especificación (sin distinguir mayúsculas).</summary>
    Public Function BuscarProductos(texto As String, Optional incluirInactivos As Boolean = False) As List(Of ProductoBaseDto)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar(SelectProducto &
                "WHERE (@t = '' OR p.codigo ILIKE '%' || @t || '%' OR p.descripcion ILIKE '%' || @t || '%' OR COALESCE(p.especificacion,'') ILIKE '%' || @t || '%') " &
                "AND (@todos OR p.activo = 1) ORDER BY p.descripcion LIMIT 500",
                AddressOf LeerProducto, "t", If(texto, "").Trim(), "todos", incluirInactivos))
    End Function

    Public Function ObtenerProducto(id As Long) As ProductoBaseDto
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar(SelectProducto & "WHERE p.id = @id", AddressOf LeerProducto, "id", id).FirstOrDefault())
    End Function

    ''' <summary>Corrige datos descriptivos. La unidad base no cambia si ya hay variantes o recetas (PRESENTACION_EN_USO).</summary>
    Public Function ActualizarProducto(id As Long, version As String, descripcion As String, especificacion As String,
                                       categoriaId As Long?, activo As Boolean) As String
        Return EnTransaccion(Permisos.CatalogoEditar,
            Function(u)
                Dim nueva = u.Escalar("UPDATE producto_base SET descripcion = @d, especificacion = @s, categoria_id = @cat, activo = @a " &
                                      "WHERE id = @id AND xmin::text = @v RETURNING xmin::text",
                                      "d", Req(descripcion, "descripcion"), "s", Opc(especificacion), "cat", categoriaId,
                                      "a", If(activo, 1L, 0L), "id", id, "v", version)
                Return VerificarVersion(u, nueva, "producto_base", id)
            End Function)
    End Function

    Private Shared Function VerificarVersion(u As UnidadDeTrabajo, nueva As Object, tabla As String, id As Long) As String
        If nueva IsNot Nothing Then Return CStr(nueva)
        If u.Escalar($"SELECT 1 FROM {tabla} WHERE id = @id", "id", id) Is Nothing Then
            Throw New ReglaNegocioException("NO_ENCONTRADO", "El registro no existe.")
        End If
        Throw New ReglaNegocioException("VERSION_CONFLICTIVA", "Otro usuario modifico el registro. Recargue los datos y repita el cambio.")
    End Function

    ' ---------- Variantes ----------

    Public Function CrearVariante(productoBaseId As Long, marcaId As Long?, codigo As String, descripcionComercial As String,
                                  tipoEnvase As String, contenidoBasePorEnvaseU6 As Long) As Long
        Dim v As New VarianteProducto(Req(codigo, "codigo"), contenidoBasePorEnvaseU6)   ' valida contenido > 0
        Return EnTransaccion(Permisos.CatalogoEditar,
            Function(u) u.EscalarLong(
                "INSERT INTO variante_producto(empresa_id, producto_base_id, marca_id, codigo, descripcion_comercial, tipo_envase, contenido_base_por_envase_u6) " &
                "VALUES (@e, @p, @m, @c, @d, @t, @k) RETURNING id",
                "e", Sesion.EmpresaId, "p", productoBaseId, "m", marcaId, "c", v.Codigo, "d", Req(descripcionComercial, "descripcion comercial"),
                "t", Req(tipoEnvase, "tipo de envase"), "k", v.ContenidoBasePorEnvaseU6))
    End Function

    Public Function ListarVariantes(productoBaseId As Long) As List(Of VarianteDto)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar(
                "SELECT v.id, v.producto_base_id, v.marca_id, m.nombre AS marca, v.codigo, v.descripcion_comercial, v.tipo_envase, " &
                "v.contenido_base_por_envase_u6, v.activo = 1 AS activo, v.xmin::text AS version FROM variante_producto v " &
                "LEFT JOIN marca m ON m.empresa_id = v.empresa_id AND m.id = v.marca_id WHERE v.producto_base_id = @p ORDER BY v.codigo",
                Function(rd) New VarianteDto With {
                    .Id = rd.Largo("id"), .ProductoBaseId = rd.Largo("producto_base_id"), .MarcaId = rd.LongONada("marca_id"),
                    .MarcaNombre = rd.TextoONada("marca"), .Codigo = rd.Texto("codigo"), .DescripcionComercial = rd.Texto("descripcion_comercial"),
                    .TipoEnvase = rd.Texto("tipo_envase"), .ContenidoBasePorEnvaseU6 = rd.Largo("contenido_base_por_envase_u6"),
                    .Activo = rd.GetBoolean(rd.GetOrdinal("activo")), .Version = rd.Texto("version")},
                "p", productoBaseId))
    End Function

    ''' <summary>
    ''' Corrige descripción, marca o estado. El contenido por envase no se edita aquí: si cambia la presentación
    ''' se crea otra variante (la base lo impide de todos modos si ya fue usada).
    ''' </summary>
    Public Function ActualizarVariante(id As Long, version As String, descripcionComercial As String, marcaId As Long?, activo As Boolean) As String
        Return EnTransaccion(Permisos.CatalogoEditar,
            Function(u)
                Dim nueva = u.Escalar("UPDATE variante_producto SET descripcion_comercial = @d, marca_id = @m, activo = @a " &
                                      "WHERE id = @id AND xmin::text = @v RETURNING xmin::text",
                                      "d", Req(descripcionComercial, "descripcion comercial"), "m", marcaId, "a", If(activo, 1L, 0L), "id", id, "v", version)
                Return VerificarVersion(u, nueva, "variante_producto", id)
            End Function)
    End Function

    ''' <summary>Cambia el contenido de una variante NO usada. Si ya se usó: PRESENTACION_EN_USO (T06).</summary>
    Public Function CorregirContenidoVariante(id As Long, version As String, contenidoBasePorEnvaseU6 As Long) As String
        Dim validar As New VarianteProducto("x", contenidoBasePorEnvaseU6)
        Return EnTransaccion(Permisos.CatalogoEditar,
            Function(u)
                Dim nueva = u.Escalar("UPDATE variante_producto SET contenido_base_por_envase_u6 = @k WHERE id = @id AND xmin::text = @v RETURNING xmin::text",
                                      "k", validar.ContenidoBasePorEnvaseU6, "id", id, "v", version)
                Return VerificarVersion(u, nueva, "variante_producto", id)
            End Function)
    End Function

    ' ---------- Empaques ----------

    Public Function CrearEmpaque(varianteId As Long, codigo As String, descripcion As String,
                                 envasesPorEmpaque As Long, Optional minimoEmpaques As Long = 1, Optional multiploEmpaques As Long = 1) As Long
        ' Valida enteros positivos con la regla del dominio.
        Dim validar As New EmpaqueCompra(New VarianteProducto("x", 1), envasesPorEmpaque, minimoEmpaques, multiploEmpaques)
        Return EnTransaccion(Permisos.CatalogoEditar,
            Function(u) u.EscalarLong(
                "INSERT INTO empaque_compra(empresa_id, variante_id, codigo, descripcion, envases_por_empaque, minimo_empaques, multiplo_empaques) " &
                "VALUES (@e, @v, @c, @d, @n, @min, @mul) RETURNING id",
                "e", Sesion.EmpresaId, "v", varianteId, "c", Req(codigo, "codigo"), "d", Req(descripcion, "descripcion"),
                "n", validar.EnvasesPorEmpaque, "min", validar.MinimoEmpaques, "mul", validar.MultiploEmpaques))
    End Function

    Private Const SelectEmpaque As String =
        "SELECT e.id, e.variante_id, v.codigo AS variante_codigo, v.descripcion_comercial, e.codigo, e.descripcion, e.envases_por_empaque, " &
        "e.minimo_empaques, e.multiplo_empaques, e.contenido_base_total_u6, e.activo = 1 AS activo FROM v_empaque_conversion e " &
        "JOIN variante_producto v ON v.empresa_id = e.empresa_id AND v.id = e.variante_id "

    Private Shared Function LeerEmpaque(rd As NpgsqlDataReader) As EmpaqueDto
        Return New EmpaqueDto With {
            .Id = rd.Largo("id"), .VarianteId = rd.Largo("variante_id"), .VarianteCodigo = rd.Texto("variante_codigo"),
            .VarianteDescripcion = rd.Texto("descripcion_comercial"), .Codigo = rd.Texto("codigo"), .Descripcion = rd.Texto("descripcion"),
            .EnvasesPorEmpaque = rd.Largo("envases_por_empaque"), .MinimoEmpaques = rd.Largo("minimo_empaques"),
            .MultiploEmpaques = rd.Largo("multiplo_empaques"), .ContenidoBaseU6 = rd.Largo("contenido_base_total_u6"),
            .Activo = rd.GetBoolean(rd.GetOrdinal("activo"))}
    End Function

    Public Function ListarEmpaques(varianteId As Long) As List(Of EmpaqueDto)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar(SelectEmpaque & "WHERE e.variante_id = @v ORDER BY e.codigo", AddressOf LeerEmpaque, "v", varianteId))
    End Function

    ''' <summary>Empaques activos de toda la empresa (para vincularlos a proveedores).</summary>
    Public Function ListarEmpaquesActivos() As List(Of EmpaqueDto)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar(SelectEmpaque & "WHERE e.activo = 1 AND v.activo = 1 ORDER BY v.codigo, e.codigo", AddressOf LeerEmpaque))
    End Function

    ''' <summary>
    ''' Marca (o desmarca) productos sin costo de compra, como el agua de red de las recetas: se costean en S/ 0 en vez de quedar
    ''' "pendientes". Devuelve cuántos productos cambió.
    ''' </summary>
    Public Function MarcarSinCosto(descripciones As IEnumerable(Of String), sinCosto As Boolean) As Integer
        Dim lista = descripciones.Select(Function(d) d.Trim().ToUpperInvariant()).Where(Function(d) d <> "").Distinct().ToArray()
        Return EnTransaccion(Permisos.CatalogoEditar,
            Function(u) u.Ejecutar("UPDATE producto_base SET sin_costo_compra = @s WHERE upper(descripcion) = ANY(@d) AND sin_costo_compra <> @s",
                                   "s", sinCosto, "d", lista))
    End Function

End Class
