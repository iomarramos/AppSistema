Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

Public Class CatalogoTests

    <FactPostgres>
    Public Sub T02_Una_empresa_no_ve_el_catalogo_de_otra_ni_buscando_por_texto()
        Using bd = BaseDatosPrueba.Crear()
            Dim catB As New ServicioCatalogo(bd.CadenaAplicacion, bd.Sesion("B"))
            Dim kg = catB.CrearUnidad("KG", "Kilogramo", AppSistema.Dominio.Catalogo.Dimension.Masa, 1000000)
            catB.CrearProducto("SEC", "Producto SECRETO de B", Nothing, kg, Nothing)

            Dim catA As New ServicioCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Assert.Empty(catA.BuscarProductos("secreto", incluirInactivos:=True))
            Assert.DoesNotContain(catA.BuscarProductos(""), Function(p) p.Codigo = "SEC")
            Assert.DoesNotContain(catA.ListarUnidades(), Function(u) u.Codigo = "KG")
            Assert.Single(catB.BuscarProductos(""))
        End Using
    End Sub

    <FactPostgres>
    Public Sub T03_Codigo_de_variante_repetido_en_la_empresa_se_rechaza_y_otra_empresa_puede_usarlo()
        Using bd = BaseDatosPrueba.Crear()
            Dim catA As New ServicioCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() catA.CrearVariante(bd.ProductoAceiteId, Nothing, "ACE-A-4L", "Otra", "bidon", 4000000))
            Assert.Equal("CODIGO_DUPLICADO", ex.Codigo)

            Dim catB As New ServicioCatalogo(bd.CadenaAplicacion, bd.Sesion("B"))
            Dim l = catB.CrearUnidad("L", "Litro", AppSistema.Dominio.Catalogo.Dimension.Volumen, 1000000)
            Dim p = catB.CrearProducto("ACE", "Aceite", Nothing, l, Nothing)
            Assert.True(catB.CrearVariante(p, Nothing, "ACE-A-4L", "Aceite de B", "bidon", 4000000) > 0)
        End Using
    End Sub

    <FactPostgres>
    Public Sub T04_El_empaque_muestra_16_L_por_caja_y_5_L_es_otra_variante()
        Using bd = BaseDatosPrueba.Crear()
            Dim cat As New ServicioCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim marcaB = cat.CrearMarca("Marca B")
            cat.CrearVariante(bd.ProductoAceiteId, marcaB, "ACE-B-5L", "Aceite B 5 L", "bidon", 5000000)
            Assert.Equal(16 * EscalaU6.Factor, cat.ListarEmpaques(bd.VarianteAceiteId).Single().ContenidoBaseU6)
            Dim variantes = cat.ListarVariantes(bd.ProductoAceiteId)
            Assert.Equal(2, variantes.Count)
            Assert.Equal("Marca B", variantes.Single(Function(v) v.Codigo = "ACE-B-5L").MarcaNombre)
        End Using
    End Sub

    <FactPostgres>
    Public Sub T06_Una_variante_usada_no_cambia_su_contenido_pero_una_sin_uso_si()
        Using bd = BaseDatosPrueba.Crear()
            Dim s = bd.Sesion("A")
            Dim cat As New ServicioCatalogo(bd.CadenaAplicacion, s)
            Dim stock As New ServicioStock(bd.CadenaAplicacion, s)
            stock.Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Recepcion, New Date(2026, 10, 1), "R-1",
                               {New LineaDocumentoStock(bd.VarianteAceiteId, 32 * EscalaU6.Factor, 8 * EscalaU6.Factor)}))

            Dim usada = cat.ListarVariantes(bd.ProductoAceiteId).Single()
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() cat.CorregirContenidoVariante(usada.Id, usada.Version, 5000000))
            Assert.Equal("PRESENTACION_EN_USO", ex.Codigo)
            Assert.Equal(32 * EscalaU6.Factor, Convert.ToInt64(bd.Escalar("SELECT cantidad_base_u6 FROM documento_stock_detalle")))

            Dim nueva = cat.CrearVariante(bd.ProductoAceiteId, Nothing, "ACE-NUEVA", "Aceite sin uso", "bidon", 3000000)
            Dim dto = cat.ListarVariantes(bd.ProductoAceiteId).Single(Function(v) v.Id = nueva)
            cat.CorregirContenidoVariante(nueva, dto.Version, 5000000)
            Assert.Equal(5000000L, cat.ListarVariantes(bd.ProductoAceiteId).Single(Function(v) v.Id = nueva).ContenidoBasePorEnvaseU6)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Edicion_con_version_antigua_da_conflicto_y_no_pisa_el_cambio_ajeno()
        Using bd = BaseDatosPrueba.Crear()
            Dim cat1 As New ServicioCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim cat2 As New ServicioCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim leido1 = cat1.ObtenerProducto(bd.ProductoAceiteId)
            Dim leido2 = cat2.ObtenerProducto(bd.ProductoAceiteId)

            cat1.ActualizarProducto(leido1.Id, leido1.Version, "Aceite vegetal refinado", Nothing, Nothing, True)
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() cat2.ActualizarProducto(leido2.Id, leido2.Version, "Aceite de soya", Nothing, Nothing, True))
            Assert.Equal("VERSION_CONFLICTIVA", ex.Codigo)
            Assert.Equal("Aceite vegetal refinado", cat2.ObtenerProducto(bd.ProductoAceiteId).Descripcion)
        End Using
    End Sub

    <FactPostgres>
    Public Sub La_auditoria_registra_quien_cambio_el_producto()
        Using bd = BaseDatosPrueba.Crear()
            Dim cat As New ServicioCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim p = cat.ObtenerProducto(bd.ProductoAceiteId)
            cat.ActualizarProducto(p.Id, p.Version, "Aceite vegetal refinado", "Botella PET", Nothing, True)
            Dim usuario = Convert.ToInt64(bd.Escalar($"SELECT usuario_id FROM auditoria WHERE tabla = 'producto_base' AND accion = 'UPDATE' AND registro_id = {p.Id}"))
            Assert.Equal(bd.A.AdminUsuarioId, usuario)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Precios_con_vigencia_sin_superposicion_y_precio_vigente_por_fecha()
        Using bd = BaseDatosPrueba.Crear()
            Dim prov As New ServicioProveedores(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim p = prov.CrearProveedor(New ProveedorDto With {.Codigo = "P1", .Nombre = "Distribuidora ejemplo"})
            Dim pe = prov.VincularEmpaque(p, bd.EmpaqueCajaId, 3)
            prov.RegistrarPrecio(pe, New Date(2026, 1, 1), New Date(2026, 6, 30), "PEN", EscalaU6.DesdeDecimal(128D), False)
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() prov.RegistrarPrecio(pe, New Date(2026, 6, 1), Nothing, "PEN", EscalaU6.DesdeDecimal(130D), False))
            Assert.Equal("VIGENCIA_SUPERPUESTA", ex.Codigo)
            prov.RegistrarPrecio(pe, New Date(2026, 7, 1), Nothing, "PEN", EscalaU6.DesdeDecimal(130D), False)

            Assert.Equal(EscalaU6.DesdeDecimal(128D), prov.PrecioVigente(pe, New Date(2026, 3, 15), "PEN").PrecioEmpaqueU6)
            Assert.Equal(EscalaU6.DesdeDecimal(130D), prov.PrecioVigente(pe, New Date(2026, 10, 2), "PEN").PrecioEmpaqueU6)
            Assert.Null(prov.PrecioVigente(pe, New Date(2025, 12, 31), "PEN"))
            Assert.Single(prov.ListarEmpaquesDeProveedor(p))
        End Using
    End Sub

End Class
