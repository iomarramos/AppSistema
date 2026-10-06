Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

''' <summary>
''' Fase 1 de Planificación y Abastecimiento Central (V021): seguridad de cuatro niveles (módulo, pantalla, acción y
''' alcance), roles centrales y acciones críticas validadas también en la base, no solo ocultando botones.
''' </summary>
Public Class SeguridadCentralTests

    Private Const ClaveDueno As String = "Duenio-Clave-2026"
    Private Const Clave As String = "Clave-Persona-2026"

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>Orcopampa y Arequipa (Sierra), Lima (Costa) y una sede sin zona; y el superusuario. Solo hay Costa, Sierra y Selva.</summary>
    Private Shared Function CrearSedes(bd As BaseDatosPrueba) As (Are As Long, Lim As Long, SinZona As Long)
        Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
        admin.FijarZona(bd.A.OperacionId, " sierra ")
        Dim r = (Are:=admin.CrearOperacion("ARE", "Arequipa", "SIERRA"), Lim:=admin.CrearOperacion("LIM", "Lima", "COSTA"), SinZona:=admin.CrearOperacion("ZZZ", "Sin zona"))
        Call New ServicioInstalacion(bd.CadenaAdmin).CrearDueno("A", "dueno", "Superusuario", ClaveDueno)
        Return r
    End Function

    Private Shared Function Entrar(bd As BaseDatosPrueba, login As String, Optional operacionId As Long? = Nothing) As SesionUsuario
        Dim acceso As New ServicioAcceso(bd.CadenaAplicacion)
        Dim s = acceso.IniciarSesion("A", login, If(login = "dueno", ClaveDueno, Clave))
        Return acceso.SeleccionarOperacion(s, If(operacionId, s.Operaciones(0).Id))
    End Function

    <FactPostgres>
    Public Sub T61_el_superusuario_entra_a_todos_los_modulos_de_todas_las_operaciones()
        Using bd = BaseDatosPrueba.Crear()
            Dim sedes = CrearSedes(bd)
            Dim acceso As New ServicioAcceso(bd.CadenaAplicacion)
            Dim s = acceso.IniciarSesion("A", "dueno", ClaveDueno)
            Assert.True(s.EsDueno)
            Assert.Equal(4, s.Operaciones.Count)
            For Each op In s.Operaciones
                Dim en = acceso.SeleccionarOperacion(s, op.Id)
                Assert.True(Permisos.Todos.All(Function(p) en.Tiene(p)))
            Next
            ' La matriz del superusuario: todo efectivo, en cada módulo, pantalla y acción.
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, acceso.SeleccionarOperacion(s, sedes.Lim))
            Dim matriz = admin.MatrizDeAcceso(s.UsuarioId, sedes.Lim)
            Assert.Equal(Permisos.Todos.Count, matriz.Count)
            Assert.True(matriz.All(Function(m) m.Efectivo))
            Assert.Equal(Modulos.Todos.Count, matriz.Select(Function(m) m.Modulo).Distinct().Count())
            Assert.True(matriz.All(Function(m) Pantallas.Acciones.Contains(m.Accion)))
            ' Cada permiso tiene su pantalla (nivel 2).
            Assert.DoesNotContain(matriz, Function(m) m.Pantalla = m.Permiso)
        End Using
    End Sub

    <FactPostgres>
    Public Sub T53_T54_T62_las_acciones_criticas_se_validan_en_el_servicio_y_en_la_base()
        Using bd = BaseDatosPrueba.Crear()
            CrearSedes(bd)
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim serv = minutas.CrearServicio("ALM", "Almuerzo")
            Dim sopa = minutas.CrearEstructura(serv, "SOPA", "Sopa", 1, 8000)
            Dim os = minutas.AsignarServicio(serv, minutas.CrearRegimen("GEN", "General"), Nothing)
            Dim orc = bd.A.OperacionId
            admin.CrearUsuario("almacen", "Almacenero", Clave, orc, "ALMACEN")
            admin.CrearUsuario("plan", "Planificadora central", Clave, orc, RolesBase.PlanificadorCentral)
            admin.CrearUsuario("chef", "Chef", Clave, orc, RolesBase.Chef)
            admin.CrearUsuario("cocina", "Cocinero", Clave, orc, "COCINA")
            Dim idAlmacen = admin.ListarUsuarios().Single(Function(x) x.Login = "almacen").Id
            Dim idPlan = admin.ListarUsuarios().Single(Function(x) x.Login = "plan").Id

            ' T53: almacén no cambia factores, ni por el servicio ni directo en la base.
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(
                Sub() Call New ServicioMinutas(bd.CadenaAplicacion, Entrar(bd, "almacen")).FijarFactorOperacion(os, sopa, 5000)).Codigo)
            Assert.Contains("SIN_PERMISO", Assert.Throws(Of Npgsql.PostgresException)(
                Sub() ComoUsuario(bd, idAlmacen, $"INSERT INTO factor_consumo_operacion(empresa_id, operacion_servicio_id, estructura_id, factor_consumo_bp, usuario_id) " &
                                                 $"VALUES ({bd.A.EmpresaId}, {os}, {sopa}, 5000, {idAlmacen})")).MessageText)
            Assert.Contains("SIN_PERMISO", Assert.Throws(Of Npgsql.PostgresException)(
                Sub() ComoUsuario(bd, idAlmacen, $"UPDATE estructura_servicio SET factor_consumo_bp = 1 WHERE id = {sopa}")).MessageText)

            ' El chef sí ajusta el factor de su operación; el factor teórico (central) no cambia.
            Call New ServicioMinutas(bd.CadenaAplicacion, Entrar(bd, "chef")).FijarFactorOperacion(os, sopa, 6000)
            Assert.Equal(6000L, Convert.ToInt64(bd.Escalar($"SELECT factor_consumo_bp FROM factor_consumo_operacion WHERE estructura_id = {sopa}")))
            Assert.Equal(8000L, Convert.ToInt64(bd.Escalar($"SELECT factor_consumo_bp FROM estructura_servicio WHERE id = {sopa}")))

            ' T54: el planificador central cambia el factor teórico pero no mueve stock.
            Dim plan = Entrar(bd, "plan")
            Call New ServicioMinutas(bd.CadenaAplicacion, plan).FijarFactorConsumo(sopa, 9000)
            Dim apertura = New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Apertura, New Date(2026, 10, 1), "AP-1",
                                                   {New LineaDocumentoStock(bd.VarianteAceiteId, U(4D), U(8D))})
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(
                Function() New ServicioStock(bd.CadenaAplicacion, plan).Contabilizar(apertura)).Codigo)
            Assert.Contains("SIN_PERMISO", Assert.Throws(Of Npgsql.PostgresException)(
                Sub() ComoUsuario(bd, idPlan, "INSERT INTO movimiento_stock(empresa_id, documento_detalle_id, almacen_id, variante_id, fecha, secuencia, signo, " &
                                              "cantidad_base_u6, costo_unitario_base_u6, valor_u6, usuario_id) " &
                                              $"VALUES ({bd.A.EmpresaId}, 0, {bd.A.AlmacenId}, {bd.VarianteAceiteId}, DATE '2026-10-01', 1, 1, 1, 0, 0, {idPlan})")).MessageText)
            ' Almacén sí mueve stock.
            Call New ServicioStock(bd.CadenaAplicacion, Entrar(bd, "almacen")).Contabilizar(apertura)

            ' T62: sin el permiso no se entra a la función aunque se llame al servicio directamente (sin pantalla).
            Dim cocina = Entrar(bd, "cocina")
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(
                Function() New ServicioAdministracion(bd.CadenaAplicacion, cocina).MatrizDeAcceso(idPlan, orc)).Codigo)
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(
                Function() New ServicioConsolidadoCompras(bd.CadenaAplicacion, cocina).Calcular(Date.Today, Date.Today)).Codigo)
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(
                Sub() Call New ServicioMinutas(bd.CadenaAplicacion, cocina).FijarFactorOperacion(os, sopa, 5000)).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Alcance_por_zona_o_todas_y_excepciones_de_la_matriz_solo_las_da_el_superusuario_y_quedan_auditadas()
        Using bd = BaseDatosPrueba.Crear()
            Dim sedes = CrearSedes(bd)
            Dim orc = bd.A.OperacionId
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            admin.CrearUsuario("compras", "Compras central", Clave, orc, RolesBase.ComprasCentral)
            admin.CrearUsuario("super", "Supervisor de zona", Clave, orc, RolesBase.Operaciones)
            ' El administrador de Orcopampa no da roles en una sede donde no tiene permisos; el superusuario sí.
            Assert.Equal("ESCALADA_NO_PERMITIDA", Assert.Throws(Of ReglaNegocioException)(
                Function() admin.CrearUsuario("otro", "Supervisor sin zona", Clave, sedes.SinZona, RolesBase.Operaciones)).Codigo)
            Dim dueno As New ServicioAdministracion(bd.CadenaAplicacion, Entrar(bd, "dueno"))
            dueno.CrearUsuario("otro", "Supervisor sin zona", Clave, sedes.SinZona, RolesBase.Operaciones)
            Dim usuarios = admin.ListarUsuarios()
            Dim idCompras = usuarios.Single(Function(x) x.Login = "compras").Id
            Dim idSuper = usuarios.Single(Function(x) x.Login = "super").Id
            Dim idOtro = usuarios.Single(Function(x) x.Login = "otro").Id
            Dim idAdmin = usuarios.Single(Function(x) x.Login = "admin").Id

            ' Un administrador que no es superusuario no amplía alcances, ni por la aplicación ni directo en la base.
            Assert.Equal("SOLO_DUENO", Assert.Throws(Of ReglaNegocioException)(
                Sub() admin.FijarAlcance(idCompras, orc, RolesBase.ComprasCentral, Alcances.Todas)).Codigo)
            Assert.Contains("SOLO_DUENO", Assert.Throws(Of Npgsql.PostgresException)(
                Sub() ComoUsuario(bd, idAdmin, $"UPDATE usuario_operacion_rol SET alcance = 'TODAS' WHERE usuario_id = {idCompras}")).MessageText)
            Assert.Single(New ServicioAcceso(bd.CadenaAplicacion).IniciarSesion("A", "compras", Clave).Operaciones)

            ' El superusuario da TODAS a compras central y ZONA al supervisor.
            dueno.FijarAlcance(idCompras, orc, RolesBase.ComprasCentral, Alcances.Todas)
            dueno.FijarAlcance(idSuper, orc, RolesBase.Operaciones, Alcances.Zona)
            Assert.Equal("ZONA_NO_DEFINIDA", Assert.Throws(Of ReglaNegocioException)(
                Sub() dueno.FijarAlcance(idOtro, sedes.SinZona, RolesBase.Operaciones, Alcances.Zona)).Codigo)
            Assert.Equal("DATO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(
                Sub() dueno.FijarAlcance(idOtro, sedes.SinZona, RolesBase.Operaciones, "REGION")).Codigo)

            ' Zonas del usuario: solo Costa, Sierra y Selva.
            Assert.Equal("DATO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(Sub() admin.FijarZona(sedes.SinZona, "NORTE")).Codigo)

            Dim acceso As New ServicioAcceso(bd.CadenaAplicacion)
            Assert.Equal(4, acceso.IniciarSesion("A", "compras", Clave).Operaciones.Count)
            Assert.Equal(4, New ServicioConsolidadoCompras(bd.CadenaAplicacion, Entrar(bd, "compras")).Calcular(Date.Today, Date.Today).Operaciones.Count)
            Dim zonaSur = acceso.IniciarSesion("A", "super", Clave)
            Assert.Equal("ARE,ORC", String.Join(",", zonaSur.Operaciones.Select(Function(o) o.Codigo).OrderBy(Function(c) c)))
            Assert.True(Entrar(bd, "super", sedes.Are).Tiene(Permisos.FactoresEditar))
            Assert.Contains(dueno.ListarAsignaciones(), Function(a) a.Login = "super" AndAlso a.Alcance = Alcances.Zona AndAlso a.Zona = Zonas.Sierra)
            Assert.Contains(dueno.AccesosPorModulo(), Function(a) a.Login = "super" AndAlso a.Operacion.StartsWith("ARE") AndAlso a.Roles = "OPERACIONES (ZONA)")

            ' Matriz: el superusuario niega FACTORES_EDITAR al supervisor solo en Arequipa y le concede la auditoría en todas.
            Assert.Equal("SOLO_DUENO", Assert.Throws(Of ReglaNegocioException)(
                Sub() admin.FijarExcepcion(idSuper, Permisos.AuditoriaVer, Nothing, True)).Codigo)
            Assert.Contains("SOLO_DUENO", Assert.Throws(Of Npgsql.PostgresException)(
                Sub() ComoUsuario(bd, idAdmin, $"INSERT INTO usuario_permiso(empresa_id, usuario_id, permiso_id, concedido, registrado_por) " &
                                               $"SELECT {bd.A.EmpresaId}, {idSuper}, id, true, {idAdmin} FROM permiso WHERE empresa_id = {bd.A.EmpresaId} AND codigo = 'AUDITORIA_VER'")).MessageText)
            dueno.FijarExcepcion(idSuper, Permisos.FactoresEditar, sedes.Are, False, "Arequipa la ajusta la zona")
            dueno.FijarExcepcion(idSuper, Permisos.AuditoriaVer, Nothing, True)
            dueno.FijarExcepcion(idSuper, Permisos.AuditoriaVer, Nothing, True)                 ' repetir no duplica
            Assert.False(Entrar(bd, "super", sedes.Are).Tiene(Permisos.FactoresEditar))
            Assert.True(Entrar(bd, "super", orc).Tiene(Permisos.FactoresEditar))
            Assert.True(Entrar(bd, "super", sedes.Are).Tiene(Permisos.AuditoriaVer))
            Dim matriz = dueno.MatrizDeAcceso(idSuper, sedes.Are)
            Dim factores = matriz.Single(Function(m) m.Permiso = Permisos.FactoresEditar)
            Assert.Equal("True|NEGADO|False|Factores de la operacion|EDITAR",
                         String.Join("|", factores.PorRol, factores.Excepcion, factores.Efectivo, factores.Pantalla, factores.Accion))
            Dim auditoria = matriz.Single(Function(m) m.Permiso = Permisos.AuditoriaVer)
            Assert.Equal("False|CONCEDIDO (todas)|True", String.Join("|", auditoria.PorRol, auditoria.Excepcion, auditoria.Efectivo))

            ' Quitar la excepción vuelve a lo que da el rol.
            dueno.FijarExcepcion(idSuper, Permisos.FactoresEditar, sedes.Are, Nothing)
            Assert.True(Entrar(bd, "super", sedes.Are).Tiene(Permisos.FactoresEditar))

            ' T63: cada cambio de acceso queda en la auditoría con quién lo hizo.
            ' Dos altas, la repetición (actualiza) y la baja.
            Assert.Equal(4L, Convert.ToInt64(bd.Escalar(
                "SELECT count(*) FROM auditoria a JOIN usuario d ON d.id = a.usuario_id AND d.login = 'dueno' WHERE a.tabla = 'usuario_permiso'")))
            Assert.Equal(2L, Convert.ToInt64(bd.Escalar(
                "SELECT count(*) FROM auditoria a JOIN usuario d ON d.id = a.usuario_id AND d.login = 'dueno' " &
                "WHERE a.tabla = 'usuario_operacion_rol' AND a.accion = 'UPDATE'")))
        End Using
    End Sub

    ''' <summary>
    ''' Perfiles de la operación (V022): cada perfil tiene lo que le toca y no lo que corresponde a otro. El almacenero
    ''' ejecuta; el jefe de almacén aprueba inventarios y ajustes; el jefe de operación aprueba y cierra sin mover stock;
    ''' el chef programa y produce sin precios ni stock.
    ''' </summary>
    <FactPostgres>
    Public Sub Perfiles_de_la_operacion_tienen_solo_lo_que_les_corresponde()
        Using bd = BaseDatosPrueba.Crear()
            Call New ServicioInstalacion(bd.CadenaAdmin).CrearDueno("A", "dueno", "Superusuario", ClaveDueno)
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            For Each rol In {"ALMACEN", "JEFE_ALMACEN", "OPERACIONES", "CHEF", "PLANIFICADOR_CENTRAL", "COMPRAS_CENTRAL"}
                admin.CrearUsuario(rol.ToLower(), rol, Clave, bd.A.OperacionId, rol)
            Next

            Dim almacenero = Entrar(bd, "almacen")
            Assert.True(almacenero.Tiene(Permisos.StockContabilizar))
            Assert.True(almacenero.Tiene(Permisos.InventarioContar))
            Assert.False(almacenero.Tiene(Permisos.InventarioAprobar))
            Assert.False(almacenero.Tiene(Permisos.MinutasAprobar))
            Assert.False(almacenero.Tiene(Permisos.ComprasEditar))
            Assert.False(almacenero.Tiene(Permisos.AdicionalAprobar))
            Assert.True(almacenero.Tiene(Permisos.InventarioVer))

            Dim jefeAlmacen = Entrar(bd, "jefe_almacen")
            Assert.True(jefeAlmacen.Tiene(Permisos.StockContabilizar))
            Assert.True(jefeAlmacen.Tiene(Permisos.InventarioAprobar))
            Assert.True(jefeAlmacen.Tiene(Permisos.ReportesVer))
            Assert.False(jefeAlmacen.Tiene(Permisos.CierreEjecutar))
            Assert.False(jefeAlmacen.Tiene(Permisos.MinutasAprobar))
            Assert.False(jefeAlmacen.Tiene(Permisos.ComprasEditar))

            Dim jefeOperacion = Entrar(bd, "operaciones")
            Assert.True(jefeOperacion.Tiene(Permisos.CierreEjecutar))
            Assert.True(jefeOperacion.Tiene(Permisos.MinutasAprobar))
            Assert.True(jefeOperacion.Tiene(Permisos.ResultadosVer))
            Assert.False(jefeOperacion.Tiene(Permisos.StockContabilizar))
            Assert.False(jefeOperacion.Tiene(Permisos.InventarioAprobar))
            Assert.True(jefeOperacion.Tiene(Permisos.AdicionalAprobar))
            Assert.True(jefeOperacion.Tiene(Permisos.InventarioVer))
            Assert.False(jefeOperacion.Tiene(Permisos.PreciosEditar))

            Dim chef = Entrar(bd, "chef")
            Assert.True(chef.Tiene(Permisos.FactoresEditar))
            Assert.True(chef.Tiene(Permisos.MinutasEditar))
            Assert.True(chef.Tiene(Permisos.ProduccionEditar))
            Assert.False(chef.Tiene(Permisos.StockContabilizar))
            Assert.False(chef.Tiene(Permisos.PreciosEditar))
            Assert.False(chef.Tiene(Permisos.CatalogoEditar))
            Assert.False(chef.Tiene(Permisos.CierreEjecutar))
            Assert.False(chef.Tiene(Permisos.ComprasAprobar))
            Assert.False(chef.Tiene(Permisos.AdicionalAprobar))

            Dim planificador = Entrar(bd, "planificador_central")
            Assert.False(planificador.Tiene(Permisos.StockContabilizar))
            Assert.False(planificador.Tiene(Permisos.InventarioContar))
            Assert.False(planificador.Tiene(Permisos.ComprasAprobar))

            Dim compras = Entrar(bd, "compras_central")
            Assert.True(compras.Tiene(Permisos.PreciosEditar))
            Assert.False(compras.Tiene(Permisos.StockContabilizar))
            Assert.False(compras.Tiene(Permisos.MinutasEditar))
        End Using
    End Sub

    ''' <summary>SQL con el rol de la aplicación y la sesión de un usuario (como lo haría la aplicación).</summary>
    Private Shared Sub ComoUsuario(bd As BaseDatosPrueba, usuarioId As Long, sql As String)
        Using cn As New Npgsql.NpgsqlConnection(bd.CadenaAplicacion)
            cn.Open()
            Using cmd As New Npgsql.NpgsqlCommand($"SELECT set_config('app.empresa_id', '{bd.A.EmpresaId}', false), set_config('app.usuario_id', '{usuarioId}', false); {sql}", cn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Class
