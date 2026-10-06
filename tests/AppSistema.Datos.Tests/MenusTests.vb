Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Datos

''' <summary>Etapa 2: recetas versionadas, minutas, costeo con snapshot y necesidades consolidadas.</summary>
Public Class MenusTests

    Private Shared ReadOnly Fecha As New Date(2026, 3, 10)

    Private Shared Function U6(valor As Decimal) As Long
        Return EscalaU6.DesdeDecimal(valor)
    End Function

    ''' <summary>Servicio ALMUERZO (sopa, fondo, bebida) para la operación de A; aceite a S/128 la caja de 4 × 4 L (S/8 por L).</summary>
    Private NotInheritable Class Escenario
        Public Bd As BaseDatosPrueba
        Public Recetas As ServicioRecetas
        Public Minutas As ServicioMinutas
        Public Catalogo As ServicioCatalogo
        Public Proveedores As ServicioProveedores
        Public ServicioId, OperacionServicioId, Sopa, Fondo, Bebida, ArrozId, ArrozVarianteId As Long

        Public Sub New(bd As BaseDatosPrueba)
            Me.Bd = bd
            Dim s = bd.Sesion("A")
            Recetas = New ServicioRecetas(bd.CadenaAplicacion, s)
            Minutas = New ServicioMinutas(bd.CadenaAplicacion, s)
            Catalogo = New ServicioCatalogo(bd.CadenaAplicacion, s)
            Proveedores = New ServicioProveedores(bd.CadenaAplicacion, s)
            ServicioId = Minutas.CrearServicio("ALM", "Almuerzo")
            Dim regimen = Minutas.CrearRegimen("GEN", "General")
            Sopa = Minutas.CrearEstructura(ServicioId, "SOPA", "Sopa", 1)
            Fondo = Minutas.CrearEstructura(ServicioId, "FONDO", "Fondo", 2)
            Bebida = Minutas.CrearEstructura(ServicioId, "BEBIDA", "Bebida", 3)
            OperacionServicioId = Minutas.AsignarServicio(ServicioId, regimen, U6(6D))

            Dim kg = Catalogo.CrearUnidad("KG", "Kilogramo", AppSistema.Dominio.Catalogo.Dimension.Masa, EscalaU6.Factor)
            ArrozId = Catalogo.CrearProducto("ARZ", "Arroz extra", Nothing, kg, Nothing)
            ArrozVarianteId = Catalogo.CrearVariante(ArrozId, Nothing, "ARZ-50", "Arroz saco 50 kg", "saco", U6(50D))

            Dim p = Proveedores.CrearProveedor(New ProveedorDto With {.Codigo = "P1", .Nombre = "Distribuidora"})
            Dim pe = Proveedores.VincularEmpaque(p, bd.EmpaqueCajaId, 2)
            Proveedores.RegistrarPrecio(pe, New Date(2026, 1, 1), Nothing, "PEN", U6(128D), False)
            Catalogo.ActivarEnOperacion(bd.VarianteAceiteId)   ' D02: el aceite es el producto activo en la operación
        End Sub

        ''' <summary>Receta aprobada con los ingredientes dados (producto, cantidad bruta) para el rendimiento indicado.</summary>
        Public Function RecetaAprobada(codigo As String, rendimiento As Decimal, ParamArray ingredientes() As (Producto As Long, Cantidad As Decimal)) As Long
            Dim v = Recetas.CrearReceta(codigo, "Receta " & codigo, Nothing, U6(rendimiento), Nothing)
            Dim orden = 1
            For Each i In ingredientes
                Recetas.AgregarIngrediente(v, i.Producto, U6(i.Cantidad), Nothing, orden)
                orden += 1
            Next
            Recetas.Aprobar(v)
            Return v
        End Function
    End Class

    <FactPostgres>
    Public Sub Receta_de_10_raciones_con_1_L_planificada_para_150_requiere_15_L_y_consolida_sin_duplicar()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim sopa = e.RecetaAprobada("SOPA1", 10D, (bd.ProductoAceiteId, 1D), (e.ArrozId, 0.5D))
            Dim fondo = e.RecetaAprobada("FONDO1", 20D, (bd.ProductoAceiteId, 1D), (e.ArrozId, 2D))
            Dim refresco = e.RecetaAprobada("REF1", 50D, (e.ArrozId, 0.1D))

            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 150)
            e.Minutas.AgregarPlato(m, e.Sopa, sopa, 150)
            e.Minutas.AgregarPlato(m, e.Fondo, fondo, 150)
            e.Minutas.AgregarPlato(m, e.Bebida, refresco, 150)
            e.Minutas.AgregarFijo(m, e.ArrozId, U6(1D))

            Dim n = e.Minutas.Necesidades({m})
            Assert.Equal(2, n.Count)
            Dim aceite = n.Single(Function(x) x.ProductoBaseId = bd.ProductoAceiteId)
            Assert.Equal(U6(15D + 7.5D), aceite.CantidadU6)          ' sopa 15 L + fondo 7,5 L
            Assert.Equal(2, aceite.Origenes)
            Dim arroz = n.Single(Function(x) x.ProductoBaseId = e.ArrozId)
            Assert.Equal(U6(7.5D + 15D + 0.3D + 1D), arroz.CantidadU6)   ' 3 recetas + fijo
            Assert.Equal("KG", arroz.Unidad)
        End Using
    End Sub

    <FactPostgres>
    Public Sub T07_variante_de_otro_producto_se_rechaza_y_la_propia_se_acepta()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim v = e.Recetas.CrearReceta("R1", "Receta", Nothing, U6(10D), Nothing)
            Dim ing = e.Recetas.AgregarIngrediente(v, bd.ProductoAceiteId, U6(1D), Nothing, 1)
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Sub() e.Recetas.PermitirVariante(ing, e.ArrozVarianteId))
            Assert.Equal("VARIANTE_INCOMPATIBLE", ex.Codigo)
            e.Recetas.PermitirVariante(ing, bd.VarianteAceiteId)
            Assert.Equal("ACE-A-4L", e.Recetas.ListarIngredientes(v).Single().VariantesPermitidas)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Rendimiento_cero_y_receta_sin_ingredientes_se_rechazan()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Assert.Equal("RENDIMIENTO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(
                Function() e.Recetas.CrearReceta("R0", "Sin rendimiento", Nothing, 0, Nothing)).Codigo)
            ' Directo en la base con el rol de la aplicación: lo impide el CHECK.
            Dim v = e.Recetas.CrearReceta("R1", "Receta", Nothing, U6(10D), Nothing)
            Dim ex = Assert.Throws(Of Npgsql.PostgresException)(Sub() EjecutarComoApp(bd, $"UPDATE receta_version SET rendimiento_raciones_u6 = 0 WHERE id = {v}"))
            Assert.Equal("23514", ex.SqlState)
            Assert.Equal("RECETA_SIN_INGREDIENTES", Assert.Throws(Of ReglaNegocioException)(Sub() e.Recetas.Aprobar(v)).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub T11_producto_sin_precio_muestra_costo_pendiente_y_no_un_total()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim v = e.RecetaAprobada("R1", 10D, (bd.ProductoAceiteId, 1D), (e.ArrozId, 1D))   ' el arroz no tiene precio
            e.Catalogo.ActivarEnOperacion(e.ArrozVarianteId)   ' el arroz es el producto activo, pero sin precio
            Dim sim = e.Recetas.CostoSimulado(v, Fecha, "PEN")
            Assert.Null(sim.CostoRacionU6)
            Assert.Equal(1, sim.Pendientes)
            Assert.Equal(U6(8D), sim.Ingredientes(0).CostoUnitarioBaseU6)
            Assert.Contains("sin precio vigente", sim.Ingredientes(1).Fuente)

            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 100)
            e.Minutas.AgregarPlato(m, e.Fondo, v, 100)
            e.Minutas.Aprobar(m, "PEN")
            Dim plato = e.Minutas.ListarPlatos(m).Single()
            Assert.Null(plato.CostoPrevistoRacionU6)
            Assert.Equal(1L, plato.IngredientesSinCosto)
        End Using
    End Sub

    <FactPostgres>
    Public Sub T10_actualizar_precio_no_modifica_la_minuta_aprobada()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim v = e.RecetaAprobada("R1", 10D, (bd.ProductoAceiteId, 1D))
            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 150)
            e.Minutas.AgregarPlato(m, e.Sopa, v, 150)
            e.Minutas.Aprobar(m, "PEN")
            Assert.Equal(U6(0.8D), e.Minutas.ListarPlatos(m).Single().CostoPrevistoRacionU6)   ' 1 L × S/8 / 10
            Assert.Contains("proveedor P1", bd.Escalar($"SELECT fuente_precio FROM costeo_ingrediente").ToString())

            ' Un proveedor más barato desde antes de la fecha de la minuta.
            Dim p2 = e.Proveedores.CrearProveedor(New ProveedorDto With {.Codigo = "P2", .Nombre = "Otro"})
            e.Proveedores.RegistrarPrecio(e.Proveedores.VincularEmpaque(p2, bd.EmpaqueCajaId, 1), New Date(2026, 2, 1), Nothing, "PEN", U6(96D), False)

            Assert.Equal(U6(0.6D), e.Recetas.CostoSimulado(v, Fecha, "PEN").CostoRacionU6)   ' la simulación sí cambia
            Assert.Equal(U6(0.8D), e.Minutas.ListarPlatos(m).Single().CostoPrevistoRacionU6) ' el snapshot no
            Dim ex = Assert.Throws(Of Npgsql.PostgresException)(Sub() EjecutarComoApp(bd, "UPDATE costeo_ingrediente SET costo_unitario_base_u6 = 1"))
            Assert.Contains("MINUTA_APROBADA", ex.MessageText)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Version_aprobada_es_inmutable_y_la_nueva_version_reemplaza_sin_tocar_minutas()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim v1 = e.RecetaAprobada("R1", 10D, (bd.ProductoAceiteId, 1D))
            Dim ing = e.Recetas.ListarIngredientes(v1).Single().Id

            Assert.Equal("RECETA_APROBADA", Assert.Throws(Of ReglaNegocioException)(Function() e.Recetas.AgregarIngrediente(v1, e.ArrozId, U6(1D), Nothing, 2)).Codigo)
            Assert.Equal("RECETA_APROBADA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Recetas.QuitarIngrediente(ing)).Codigo)
            Assert.Equal("RECETA_APROBADA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Recetas.ActualizarBorrador(v1, U6(20D), Nothing)).Codigo)
            Assert.Equal("RECETA_APROBADA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Recetas.PermitirVariante(ing, bd.VarianteAceiteId)).Codigo)

            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 10)
            e.Minutas.AgregarPlato(m, e.Sopa, v1, 10)
            e.Minutas.Aprobar(m, "PEN")

            Dim recetaId = e.Recetas.ListarVersiones(CLng(bd.Escalar($"SELECT receta_id FROM receta_version WHERE id = {v1}"))).Single().RecetaId
            Dim v2 = e.Recetas.NuevaVersion(recetaId)
            Assert.Equal("BORRADOR_EXISTENTE", Assert.Throws(Of ReglaNegocioException)(Function() e.Recetas.NuevaVersion(recetaId)).Codigo)
            Assert.Single(e.Recetas.ListarIngredientes(v2))
            e.Recetas.AgregarIngrediente(v2, e.ArrozId, U6(1D), Nothing, 2)
            e.Recetas.Aprobar(v2)

            Dim versiones = e.Recetas.ListarVersiones(recetaId)
            Assert.Equal("retirada,aprobada", String.Join(",", versiones.Select(Function(x) x.Estado)))
            Assert.Equal(v1, e.Minutas.ListarPlatos(m).Single().RecetaVersionId)
            Assert.Equal(2L, e.Recetas.BuscarRecetas("R1").Single().VersionAprobada)

            ' Una versión retirada no se planifica.
            Dim m2 = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha.AddDays(1), 10)
            Assert.Equal("RECETA_NO_APROBADA", Assert.Throws(Of ReglaNegocioException)(Function() e.Minutas.AgregarPlato(m2, e.Sopa, v1, 10)).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Minuta_aprobada_es_inmutable_y_las_reglas_de_planificacion_se_cumplen()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim v = e.RecetaAprobada("R1", 10D, (bd.ProductoAceiteId, 1D))
            Dim borrador = e.Recetas.CrearReceta("R2", "En borrador", Nothing, U6(10D), Nothing)
            Dim otroServicio = e.Minutas.CrearServicio("CEN", "Cena")
            Dim estructuraCena = e.Minutas.CrearEstructura(otroServicio, "SOPA", "Sopa", 1)

            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 10)
            Assert.Equal("MINUTA_VACIA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.Aprobar(m, "PEN")).Codigo)
            Assert.Equal("RECETA_NO_APROBADA", Assert.Throws(Of ReglaNegocioException)(Function() e.Minutas.AgregarPlato(m, e.Sopa, borrador, 10)).Codigo)
            Assert.Equal("ESTRUCTURA_DE_OTRO_SERVICIO", Assert.Throws(Of ReglaNegocioException)(Function() e.Minutas.AgregarPlato(m, estructuraCena, v, 10)).Codigo)
            Assert.Equal("CODIGO_DUPLICADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 5)).Codigo)

            Dim plato = e.Minutas.AgregarPlato(m, e.Sopa, v, 10)
            e.Minutas.Aprobar(m, "PEN")
            Assert.Equal("MINUTA_APROBADA", Assert.Throws(Of ReglaNegocioException)(Function() e.Minutas.AgregarPlato(m, e.Fondo, v, 10)).Codigo)
            Assert.Equal("MINUTA_APROBADA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.QuitarPlato(plato)).Codigo)
            Assert.Equal("MINUTA_APROBADA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.Aprobar(m, "PEN")).Codigo)
            For Each sql In {$"UPDATE minuta_detalle SET raciones = 99 WHERE id = {plato}", $"UPDATE minuta SET comensales = 99 WHERE id = {m}",
                             $"DELETE FROM minuta WHERE id = {m}", $"UPDATE minuta SET estado = 'borrador' WHERE id = {m}"}
                Dim ex = Assert.Throws(Of Npgsql.PostgresException)(Sub() EjecutarComoApp(bd, sql))
                Assert.Contains("MINUTA_APROBADA", ex.MessageText)
            Next
            EjecutarComoApp(bd, $"UPDATE minuta SET estado = 'cerrada' WHERE id = {m}")   ' aprobada → cerrada sí
            Assert.Equal("cerrada", e.Minutas.ListarMinutas(Fecha, Fecha).Single().Estado)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Cocina_planifica_pero_no_aprueba_y_otra_empresa_no_ve_las_recetas()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim v = e.RecetaAprobada("R1", 10D, (bd.ProductoAceiteId, 1D))
            Call New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A")).CrearUsuario("cocina", "Cocinero", "Cocina-Clave-2026", bd.A.OperacionId, "COCINA")
            Dim cocina As New ServicioMinutas(bd.CadenaAplicacion, bd.Sesion("A", "cocina", "Cocina-Clave-2026"))
            Dim m = cocina.CrearMinuta(e.OperacionServicioId, Fecha, 10)
            cocina.AgregarPlato(m, e.Sopa, v, 10)
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Sub() cocina.Aprobar(m, "PEN")).Codigo)
            Dim recetasCocina As New ServicioRecetas(bd.CadenaAplicacion, bd.Sesion("A", "cocina", "Cocina-Clave-2026"))
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Sub() recetasCocina.Aprobar(v)).Codigo)

            Dim recetasB As New ServicioRecetas(bd.CadenaAplicacion, bd.Sesion("B"))
            Assert.Empty(recetasB.BuscarRecetas(""))
            Assert.Empty(recetasB.ListarIngredientes(v))
            Dim minutasB As New ServicioMinutas(bd.CadenaAplicacion, bd.Sesion("B"))
            Assert.Equal("OPERACION_AJENA", Assert.Throws(Of ReglaNegocioException)(Function() minutasB.Necesidades({m})).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Sincronizar_permisos_crea_los_nuevos_para_ADMIN_y_es_idempotente()
        Using bd = BaseDatosPrueba.Crear()
            bd.EjecutarAdmin("DELETE FROM rol_permiso WHERE permiso_id IN (SELECT id FROM permiso WHERE codigo LIKE 'MINUTAS_%'); " &
                             "DELETE FROM permiso WHERE codigo LIKE 'MINUTAS_%'")
            Dim inst As New ServicioInstalacion(bd.CadenaAdmin)
            Assert.Equal(4, inst.SincronizarPermisos())   ' 2 permisos × 2 empresas
            Assert.Equal(0, inst.SincronizarPermisos())
            Assert.True(bd.Sesion("A").Permisos.Contains("MINUTAS_APROBAR"))
            Assert.Equal(0L, Convert.ToInt64(bd.Escalar(
                "SELECT count(*) FROM rol_permiso rp JOIN rol r ON r.id = rp.rol_id JOIN permiso p ON p.id = rp.permiso_id " &
                "WHERE p.codigo LIKE 'MINUTAS_%' AND r.codigo <> 'ADMIN'")))
        End Using
    End Sub

    ''' <summary>SQL directo con el rol de la aplicación y el contexto de empresa A (como lo haría un cliente que se salta los servicios).</summary>
    Private Shared Sub EjecutarComoApp(bd As BaseDatosPrueba, sql As String)
        Using cn As New Npgsql.NpgsqlConnection(bd.CadenaAplicacion)
            cn.Open()
            Using cmd As New Npgsql.NpgsqlCommand($"SELECT set_config('app.empresa_id', '{bd.A.EmpresaId}', false); {sql}", cn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    <FactPostgres>
    Public Sub Matriz_muestra_costo_solo_si_esta_aprobada_y_el_chef_no_cambia_comensales_aprobados()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim sopa = e.RecetaAprobada("SOPA1", 10D, (bd.ProductoAceiteId, 1D))
            Dim fondo = e.RecetaAprobada("FONDO1", 20D, (bd.ProductoAceiteId, 1D))
            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 150)
            e.Minutas.AgregarPlato(m, e.Sopa, sopa, 150)
            e.Minutas.AgregarPlato(m, e.Fondo, fondo, 100)
            Dim matriz As New ServicioMatrizMenu(bd.CadenaAplicacion, bd.Sesion("A"))

            ' En borrador: raciones y platos, pero sin costo previsto (no se inventa).
            Dim borrador = matriz.Celdas(Fecha, Fecha).Single()
            Assert.Equal(250L, borrador.RacionesTeoricas)
            Assert.Equal(150L, borrador.Comensales)
            Assert.Null(borrador.CostoPrevistoU6)
            Assert.Null(borrador.CostoPorComensalU6)
            Assert.Contains("x 100", borrador.Platos)
            Assert.False(borrador.ProduccionRegistrada)
            Assert.Equal("", borrador.EstadoRequerimiento)

            ' El chef cambia comensales mientras la minuta está en borrador.
            e.Minutas.ActualizarComensales(m, 160)
            Assert.Equal(160L, matriz.Celdas(Fecha, Fecha).Single().Comensales)

            ' Aprobada: aparece el costo y el costo por comensal; ya no se cambian los comensales.
            e.Minutas.Aprobar(m, "PEN")
            Dim aprobada = matriz.Celdas(Fecha, Fecha).Single()
            Assert.NotNull(aprobada.CostoPrevistoU6)
            Assert.Equal(EscalaU6.MultiplicarDividir(aprobada.CostoPrevistoU6.Value, 1, 160), aprobada.CostoPorComensalU6.Value)
            Assert.Equal("MINUTA_APROBADA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.ActualizarComensales(m, 170)).Codigo)
            Assert.Equal(160L, matriz.Celdas(Fecha, Fecha).Single().Comensales)

            ' Techo = costo patrón por comensal del servicio (S/ 6) × comensales; la desviación es costo − techo.
            Assert.Equal(U6(960D), aprobada.TechoU6)
            Assert.Equal(aprobada.CostoPrevistoU6.Value - U6(960D), aprobada.DesviacionU6)
            Dim dia = matriz.ResumenPorDia(matriz.Celdas(Fecha, Fecha)).Single()
            Assert.Equal(1L, dia.Minutas)
            Assert.Equal(160L, dia.Comensales)
            Assert.Equal(0L, dia.MinutasSinCosto)
            Assert.Equal(aprobada.CostoPrevistoU6, dia.CostoDiaU6)
            Assert.Equal(U6(960D), dia.TechoU6)
            Assert.Equal(aprobada.DesviacionU6, dia.DesviacionU6)
            Assert.Equal("DATO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(Function() matriz.Celdas(Fecha.AddDays(1), Fecha)).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Reporte_de_la_matriz_tiene_minutas_y_resumen_por_dia_con_techo_y_desviacion()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim sopa = e.RecetaAprobada("SOPA1", 10D, (bd.ProductoAceiteId, 1D))
            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 160)
            e.Minutas.AgregarPlato(m, e.Sopa, sopa, 150)
            e.Minutas.Aprobar(m, "PEN")

            Dim r = New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A")).MatrizDelPeriodo(Fecha, Fecha)
            Assert.Equal("Matriz de planificacion de menus", r.Titulo)
            Assert.Equal(2, r.Secciones.Count)
            Assert.Single(r.Secciones(0).Filas)
            Assert.Single(r.Secciones(1).Filas)
            ' Columnas de la sección Minutas: 4 comensales, 6 costo previsto, 8 techo, 9 desviación.
            Dim fila = r.Secciones(0).Filas(0)
            Assert.Equal(160L, CType(fila(4), Long))
            Assert.Equal(U6(960D), CType(fila(8), Long))
            Assert.Equal(CType(fila(6), Long) - U6(960D), CType(fila(9), Long))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Chef_sustituye_una_receta_aprobada_solo_en_borrador()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim sopa = e.RecetaAprobada("SOPA1", 10D, (bd.ProductoAceiteId, 1D))
            Dim fondo = e.RecetaAprobada("FONDO1", 20D, (bd.ProductoAceiteId, 1D))
            Dim sinAprobar = e.Recetas.CrearReceta("BORR1", "Receta sin aprobar", Nothing, U6(5D), Nothing)
            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 150)
            e.Minutas.AgregarPlato(m, e.Sopa, sopa, 150)
            Dim plato = e.Minutas.ListarPlatos(m).Single().Id

            Assert.Equal("RECETA_NO_APROBADA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.SustituirReceta(plato, sinAprobar)).Codigo)
            e.Minutas.SustituirReceta(plato, fondo)
            Assert.Equal("FONDO1", e.Minutas.ListarPlatos(m).Single().RecetaCodigo)
            Assert.Equal("PLATO_REPETIDO", Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.SustituirReceta(plato, fondo)).Codigo)

            e.Minutas.Aprobar(m, "PEN")
            Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.SustituirReceta(plato, sopa))
            Assert.Equal("FONDO1", e.Minutas.ListarPlatos(m).Single().RecetaCodigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Chef_ajusta_raciones_operativas_del_plan_aprobado_y_la_teorica_no_cambia()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim sopa = e.RecetaAprobada("SOPA1", 10D, (bd.ProductoAceiteId, 1D))
            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 150)
            e.Minutas.AgregarPlato(m, e.Sopa, sopa, 150)
            Dim plato = e.Minutas.ListarPlatos(m).Single().Id
            Dim matriz As New ServicioMatrizMenu(bd.CadenaAplicacion, bd.Sesion("A"))

            ' Solo sobre plan aprobado.
            Assert.Equal("MINUTA_NO_APROBADA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.AjustarRacionesOperativas(plato, 120, "Llegaron menos")).Codigo)
            e.Minutas.Aprobar(m, "PEN")

            Assert.Equal("CANTIDAD_INVALIDA", Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.AjustarRacionesOperativas(plato, -1, "Negativo")).Codigo)
            Assert.Throws(Of ReglaNegocioException)(Sub() e.Minutas.AjustarRacionesOperativas(plato, 120, "   "))

            e.Minutas.AjustarRacionesOperativas(plato, 120, "Llegaron menos")
            Dim c = matriz.Celdas(Fecha, Fecha).Single()
            Assert.Equal(150L, c.RacionesTeoricas)
            Assert.Equal(120L, c.RacionesOperativas)

            ' Un nuevo ajuste reemplaza el anterior; la teórica sigue igual.
            e.Minutas.AjustarRacionesOperativas(plato, 130, "Corregido")
            Assert.Equal(130L, matriz.Celdas(Fecha, Fecha).Single().RacionesOperativas)
            Assert.Equal(150L, matriz.Celdas(Fecha, Fecha).Single().RacionesTeoricas)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Comparativo_y_resultado_mensual_salen_con_sus_secciones()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim sopa = e.RecetaAprobada("SOPA1", 10D, (bd.ProductoAceiteId, 1D))
            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 150)
            e.Minutas.AgregarPlato(m, e.Sopa, sopa, 150)
            e.Minutas.Aprobar(m, "PEN")
            Dim reportes = New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A"))

            Dim comparativo = reportes.ComparativoMensual(e.OperacionServicioId, Fecha.Year, Fecha.Month)
            Assert.Equal(3, comparativo.Secciones.Count)
            Assert.Equal(150L, CType(comparativo.Secciones(0).Filas(0)(1), Long))

            Dim resultado = reportes.ResultadoMensual(Fecha.Year, Fecha.Month)
            Assert.Equal("Resultado mensual de alimentos", resultado.Titulo)
            Assert.Equal(3, resultado.Secciones.Count)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Reportes_de_costo_previsión_frecuencia_y_raciones_salen_con_datos_reales()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim sopa = e.RecetaAprobada("SOPA1", 10D, (bd.ProductoAceiteId, 1D))
            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 150)
            e.Minutas.AgregarPlato(m, e.Sopa, sopa, 150)
            e.Minutas.Aprobar(m, "PEN")
            Dim reportes = New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A"))

            Dim costo = reportes.CostoResumidoTeorico(Fecha, Fecha)
            Assert.Single(costo.Secciones(0).Filas)
            Assert.Equal(CType(costo.Secciones(0).Filas(0)(4), Long), CType(costo.Secciones(0).Totales(4), Long))

            Dim previsto = reportes.PrevisionConsumo(Fecha, Fecha)
            Assert.Contains(previsto.Secciones(0).Filas, Function(f) CStr(f(1)).Contains("aceite", StringComparison.OrdinalIgnoreCase) OrElse CStr(f(1)).Contains("Aceite"))

            Dim frecuencia = reportes.FrecuenciaRecetas(Fecha, Fecha)
            Assert.Single(frecuencia.Secciones(0).Filas)
            Assert.Equal("SOPA1", CStr(frecuencia.Secciones(0).Filas(0)(0)))

            Dim raciones = reportes.ComparativoRaciones(Fecha, Fecha)
            Assert.Single(raciones.Secciones(0).Filas)
            Assert.Equal(150L, CType(raciones.Secciones(0).Filas(0)(2), Long))
            Assert.Equal(150L, CType(raciones.Secciones(0).Filas(0)(3), Long))
            Assert.Null(raciones.Secciones(0).Filas(0)(4))   ' sin produccion registrada: vacio, no cero
        End Using
    End Sub

    <FactPostgres>
    Public Sub Pendientes_sprint3_costo_teorico_requisicion_por_estructura_y_control_de_raciones()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim sopa = e.RecetaAprobada("SOPA1", 10D, (bd.ProductoAceiteId, 1D))
            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 150)
            e.Minutas.AgregarPlato(m, e.Sopa, sopa, 150)
            e.Minutas.Aprobar(m, "PEN")
            Dim reportes = New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A"))

            ' 150 raciones con 1 L cada 10 raciones = 15 L de aceite a S/ 8 por litro = S/ 120.
            Dim costo = reportes.CostoDetalladoTeorico(Fecha, Fecha)
            Assert.Single(costo.Secciones(0).Filas)
            Assert.Equal(U6(15D), CType(costo.Secciones(0).Filas(0)(3), Long))
            Assert.Equal(U6(120D), CType(costo.Secciones(0).Filas(0)(5), Long))

            Dim requisicion = reportes.RequisicionPorEstructura(Fecha, Fecha)
            Assert.Single(requisicion.Secciones(0).Filas)
            Assert.Equal("Sopa", CStr(requisicion.Secciones(0).Filas(0)(0)))
            Assert.Equal(U6(15D), CType(requisicion.Secciones(0).Filas(0)(4), Long))

            Dim control = reportes.ControlRaciones(Fecha, Fecha)
            Assert.Single(control.Secciones(0).Filas)
            Assert.Equal(150L, CType(control.Secciones(0).Filas(0)(2), Long))
            Assert.Null(control.Secciones(0).Filas(0)(4))            ' sin produccion: vacio
            Assert.Null(control.Secciones(0).Filas(0)(5))            ' sin venta: vacio

            ' Sin datos: vacios, no ceros.
            Assert.Empty(reportes.VentaServicioContado(Fecha, Fecha).Secciones(0).Filas)
            Assert.Empty(reportes.ResumenCompras(Fecha, Fecha).Secciones(0).Filas)
            Assert.Empty(reportes.ResumenTraspasos(Fecha, Fecha).Secciones(0).Filas)
            Assert.Empty(reportes.MapaSolicitudCompras(bd.A.AlmacenId).Secciones)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Sprint6_resultado_comparado_tiene_presupuesto_teorico_real_y_alerta_de_food_cost()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim sopa = e.RecetaAprobada("SOPA1", 10D, (bd.ProductoAceiteId, 1D))
            Dim m = e.Minutas.CrearMinuta(e.OperacionServicioId, Fecha, 150)
            e.Minutas.AgregarPlato(m, e.Sopa, sopa, 150)
            e.Minutas.Aprobar(m, "PEN")
            Dim comparado = New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A")).ComparativoResultado(Fecha.Year, Fecha.Month)
            Assert.Equal(2, comparado.Secciones.Count)
            Assert.NotEmpty(comparado.Secciones(0).Filas)
            Assert.True(comparado.Secciones(1).Filas.All(Function(f) {"Sin dato", "Sobre objetivo", "En objetivo"}.Contains(CStr(f(3)))))
        End Using
    End Sub

End Class
