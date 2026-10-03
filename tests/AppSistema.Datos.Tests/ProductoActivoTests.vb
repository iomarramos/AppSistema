Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

''' <summary>
''' Decisiones del usuario. D02: el costo usa el precio del producto ACTIVO en la operación (liberado o en uso), no el más
''' barato. D03: los precios no incluyen IGV.
''' </summary>
Public Class ProductoActivoTests

    Private Shared ReadOnly Fecha As New Date(2026, 10, 2)

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    <FactPostgres>
    Public Sub El_costo_usa_el_producto_activo_en_la_operacion_y_no_el_mas_barato()
        Using bd = BaseDatosPrueba.Crear()
            Dim s = bd.Sesion("A")
            Dim cat As New ServicioCatalogo(bd.CadenaAplicacion, s)
            Dim prov As New ServicioProveedores(bd.CadenaAplicacion, s)
            Dim p1 = prov.CrearProveedor(New ProveedorDto With {.Codigo = "P1", .Nombre = "Mayorista"})
            ' Bidón de 4 L: caja de 4 a S/ 128 → S/ 8 por L. Botella de 1 L: caja de 12 a S/ 72 → S/ 6 por L (más barata).
            prov.RegistrarPrecio(prov.VincularEmpaque(p1, bd.EmpaqueCajaId, 1), New Date(2026, 1, 1), Nothing, "PEN", U(128D), False)
            Dim botella = cat.CrearVariante(bd.ProductoAceiteId, Nothing, "ACE-1L", "Aceite botella 1 L", "botella", U(1D))
            prov.RegistrarPrecio(prov.VincularEmpaque(p1, cat.CrearEmpaque(botella, "CAJA12", "Caja 12 x 1 L", 12), 1), New Date(2026, 1, 1), Nothing, "PEN", U(72D), False)

            Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, s)
            Dim v = recetas.CrearReceta("SOPA", "Sopa", Nothing, U(10D), Nothing)
            recetas.AgregarIngrediente(v, bd.ProductoAceiteId, U(1D), Nothing, 1)
            recetas.Aprobar(v)
            Dim costo = Function() recetas.CostoSimulado(v, Fecha, "PEN").Ingredientes.Single()

            ' Sin producto liberado ni ingresos al almacén: pendiente, aunque haya precios (no se elige el más barato).
            Assert.Null(costo().CostoUnitarioBaseU6)
            Assert.Contains("sin producto activo", costo().Fuente)

            ' El bidón entró al almacén de la operación: es el que se usa, aunque la botella sea más barata.
            Call New ServicioStock(bd.CadenaAplicacion, s).Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Apertura, New Date(2026, 9, 1), "AP-1",
                {New LineaDocumentoStock(bd.VarianteAceiteId, U(8D), U(8D))}))
            Assert.Equal(U(8D), costo().CostoUnitarioBaseU6)
            Assert.Contains("producto en uso ACE-A-4L", costo().Fuente)

            ' Se libera la botella en la operación: desde ahora se costea con su precio.
            cat.ActivarEnOperacion(botella)
            Assert.Equal(U(6D), costo().CostoUnitarioBaseU6)
            Assert.Contains("producto activo ACE-1L liberado en la operacion", costo().Fuente)
            Assert.True(cat.ListarVariantes(bd.ProductoAceiteId).Single(Function(x) x.Id = botella).ActivoEnOperacion)
            Assert.False(cat.ListarVariantes(bd.ProductoAceiteId).Single(Function(x) x.Id = bd.VarianteAceiteId).ActivoEnOperacion)

            ' Un precio nuevo del producto activo reemplaza al anterior (vigencia más reciente), aunque sea más caro.
            Dim p2 = prov.CrearProveedor(New ProveedorDto With {.Codigo = "P2", .Nombre = "Otro"})
            prov.RegistrarPrecio(prov.VincularEmpaque(p2, cat.ListarEmpaques(botella).Single().Id, 1), New Date(2026, 9, 15), Nothing, "PEN", U(84D), False)
            Assert.Equal(U(7D), costo().CostoUnitarioBaseU6)

            ' Quitar el producto liberado vuelve al que está en uso (el bidón).
            cat.QuitarActivoEnOperacion(bd.ProductoAceiteId)
            Assert.Equal(U(8D), costo().CostoUnitarioBaseU6)

            ' La otra operación de la empresa no ve lo liberado aquí; la otra empresa no ve nada.
            Assert.Equal(0L, Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM producto_operacion WHERE empresa_id = {bd.B.EmpresaId}")))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Los_precios_se_registran_sin_igv()
        Using bd = BaseDatosPrueba.Crear()
            Dim prov As New ServicioProveedores(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim pe = prov.VincularEmpaque(prov.CrearProveedor(New ProveedorDto With {.Codigo = "P1", .Nombre = "Mayorista"}), bd.EmpaqueCajaId, 1)
            Assert.Equal("PRECIO_CON_IGV", Assert.Throws(Of ReglaNegocioException)(
                Function() prov.RegistrarPrecio(pe, New Date(2026, 1, 1), Nothing, "PEN", U(151.04D), True)).Codigo)
            Dim id = prov.RegistrarPrecio(pe, New Date(2026, 1, 1), Nothing, "PEN", U(128D), False)
            ' La base también lo impide (restricción precio_sin_igv).
            Dim ex = Assert.Throws(Of Npgsql.PostgresException)(Sub() bd.Escalar($"UPDATE precio_compra SET incluye_impuesto = 1 WHERE id = {id}"))
            Assert.Equal("23514", ex.SqlState)
            Assert.Equal("precio_sin_igv", ex.ConstraintName)
        End Using
    End Sub

End Class
