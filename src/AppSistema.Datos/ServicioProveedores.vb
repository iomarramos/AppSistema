Imports AppSistema.Dominio
Imports AppSistema.Dominio.Seguridad

''' <summary>Proveedores, empaques que ofrece cada uno y precios con vigencia.</summary>
Public NotInheritable Class ServicioProveedores
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    Public Function CrearProveedor(p As ProveedorDto) As Long
        If p Is Nothing Then Throw New ArgumentNullException(NameOf(p))
        Return EnTransaccion(Permisos.ProveedoresEditar,
            Function(u) u.EscalarLong(
                "INSERT INTO proveedor(empresa_id, codigo, nombre, identificacion_fiscal, contacto, correo, telefono, es_caja_chica) " &
                "VALUES (@e, @c, @n, @rf, @ct, @co, @t, @cc) RETURNING id",
                "e", Sesion.EmpresaId, "c", ServicioAdministracion.Requerido(p.Codigo, "codigo"), "n", ServicioAdministracion.Requerido(p.Nombre, "nombre"),
                "rf", p.IdentificacionFiscal, "ct", p.Contacto, "co", p.Correo, "t", p.Telefono, "cc", If(p.EsCajaChica, 1L, 0L)))
    End Function

    Public Function ListarProveedores() As List(Of ProveedorDto)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar(
                "SELECT id, codigo, nombre, identificacion_fiscal, contacto, correo, telefono, es_caja_chica = 1 AS caja FROM proveedor ORDER BY nombre",
                Function(rd) New ProveedorDto With {
                    .Id = rd.Largo("id"), .Codigo = rd.Texto("codigo"), .Nombre = rd.Texto("nombre"),
                    .IdentificacionFiscal = rd.TextoONada("identificacion_fiscal"), .Contacto = rd.TextoONada("contacto"),
                    .Correo = rd.TextoONada("correo"), .Telefono = rd.TextoONada("telefono"), .EsCajaChica = rd.GetBoolean(rd.GetOrdinal("caja"))}))
    End Function

    Public Function VincularEmpaque(proveedorId As Long, empaqueId As Long, plazoEntregaDias As Long) As Long
        If plazoEntregaDias < 0 Then Throw New ReglaNegocioException("DATO_INVALIDO", "El plazo de entrega no puede ser negativo.")
        Return EnTransaccion(Permisos.ProveedoresEditar,
            Function(u) u.EscalarLong(
                "INSERT INTO proveedor_empaque(empresa_id, proveedor_id, empaque_id, plazo_entrega_dias) VALUES (@e, @p, @em, @d) RETURNING id",
                "e", Sesion.EmpresaId, "p", proveedorId, "em", empaqueId, "d", plazoEntregaDias))
    End Function

    Public Function ListarEmpaquesDeProveedor(proveedorId As Long) As List(Of ProveedorEmpaqueDto)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar(
                "SELECT pe.id, pe.proveedor_id, pe.empaque_id, e.descripcion, v.codigo AS variante, pe.plazo_entrega_dias " &
                "FROM proveedor_empaque pe JOIN empaque_compra e ON e.empresa_id = pe.empresa_id AND e.id = pe.empaque_id " &
                "JOIN variante_producto v ON v.empresa_id = e.empresa_id AND v.id = e.variante_id " &
                "WHERE pe.proveedor_id = @p ORDER BY v.codigo, e.codigo",
                Function(rd) New ProveedorEmpaqueDto With {
                    .Id = rd.Largo("id"), .ProveedorId = rd.Largo("proveedor_id"), .EmpaqueId = rd.Largo("empaque_id"),
                    .EmpaqueDescripcion = rd.Texto("descripcion"), .VarianteCodigo = rd.Texto("variante"), .PlazoEntregaDias = rd.Largo("plazo_entrega_dias")},
                "p", proveedorId))
    End Function

    ''' <summary>Registra un precio con vigencia. Vigencias superpuestas: VIGENCIA_SUPERPUESTA (lo garantiza la base).</summary>
    Public Function RegistrarPrecio(proveedorEmpaqueId As Long, fechaDesde As Date, fechaHasta As Date?, moneda As String,
                                    precioEmpaqueU6 As Long, incluyeImpuesto As Boolean) As Long
        If precioEmpaqueU6 < 0 Then Throw New ReglaNegocioException("DATO_INVALIDO", "El precio no puede ser negativo.")
        If fechaHasta.HasValue AndAlso fechaHasta.Value < fechaDesde Then Throw New ReglaNegocioException("DATO_INVALIDO", "La vigencia termina antes de empezar.")
        Return EnTransaccion(Permisos.PreciosEditar,
            Function(u) u.EscalarLong(
                "INSERT INTO precio_compra(empresa_id, proveedor_empaque_id, fecha_desde, fecha_hasta, moneda, precio_empaque_u6, incluye_impuesto) " &
                "VALUES (@e, @pe, @d, @h, @m, @p, @i) RETURNING id",
                "e", Sesion.EmpresaId, "pe", proveedorEmpaqueId, "d", fechaDesde.Date, "h", If(fechaHasta.HasValue, CType(fechaHasta.Value.Date, Object), Nothing),
                "m", ServicioAdministracion.Requerido(moneda, "moneda"), "p", precioEmpaqueU6, "i", If(incluyeImpuesto, 1L, 0L)))
    End Function

    Private Shared Function LeerPrecio(rd As Npgsql.NpgsqlDataReader) As PrecioDto
        Dim iHasta = rd.GetOrdinal("fecha_hasta")
        Return New PrecioDto With {
            .Id = rd.Largo("id"), .ProveedorEmpaqueId = rd.Largo("proveedor_empaque_id"),
            .FechaDesde = rd.GetFieldValue(Of Date)(rd.GetOrdinal("fecha_desde")),
            .FechaHasta = If(rd.IsDBNull(iHasta), CType(Nothing, Date?), rd.GetFieldValue(Of Date)(iHasta)),
            .Moneda = rd.Texto("moneda"), .PrecioEmpaqueU6 = rd.Largo("precio_empaque_u6"), .IncluyeImpuesto = rd.Largo("incluye_impuesto") = 1}
    End Function

    Public Function ListarPrecios(proveedorEmpaqueId As Long) As List(Of PrecioDto)
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar("SELECT * FROM precio_compra WHERE proveedor_empaque_id = @pe ORDER BY fecha_desde",
                                    AddressOf LeerPrecio, "pe", proveedorEmpaqueId))
    End Function

    ''' <summary>Precio vigente en una fecha, o Nothing si no hay ninguno.</summary>
    Public Function PrecioVigente(proveedorEmpaqueId As Long, fecha As Date, moneda As String) As PrecioDto
        Return EnTransaccion(Permisos.CatalogoVer,
            Function(u) u.Consultar(
                "SELECT * FROM precio_compra WHERE proveedor_empaque_id = @pe AND moneda = @m " &
                "AND fecha_desde <= @f AND (fecha_hasta IS NULL OR fecha_hasta >= @f)",
                AddressOf LeerPrecio, "pe", proveedorEmpaqueId, "m", moneda, "f", fecha.Date).FirstOrDefault())
    End Function

End Class
