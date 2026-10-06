Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Seguridad
Imports AppSistema.Datos

''' <summary>
''' Dueño del sistema (V020) y privilegios por módulo. El dueño entra con su clave personal a todas las operaciones con
''' todos los permisos. Nadie da más permisos de los que tiene, y solo el dueño otorga el rango de dueño.
''' </summary>
Public Class DuenoTests

    Private Const ClaveDueno As String = "Duenio-Clave-2026"

    <FactPostgres>
    Public Sub El_dueno_entra_a_todas_las_operaciones_y_decide_hasta_donde_llega_cada_persona()
        Using bd = BaseDatosPrueba.Crear()
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim nueva = admin.CrearOperacion("ARE", "Arequipa")

            ' Clave personal: se crea con el instalador (conexión del propietario).
            Assert.Equal("CLAVE_DEBIL", Assert.Throws(Of ReglaNegocioException)(
                Function() New ServicioInstalacion(bd.CadenaAdmin).CrearDueno("A", "dueno", "Dueno", "corta")).Codigo)
            Call New ServicioInstalacion(bd.CadenaAdmin).CrearDueno("A", "dueno", "Dueno del sistema", ClaveDueno)

            ' Entra a todas las operaciones (también a la creada antes de existir su usuario) con todos los permisos.
            Dim acceso As New ServicioAcceso(bd.CadenaAplicacion)
            Dim s = acceso.IniciarSesion("A", "dueno", ClaveDueno)
            Assert.True(s.EsDueno)
            Assert.Contains(s.Operaciones, Function(o) o.Id = nueva)
            Dim enNueva = acceso.SeleccionarOperacion(s, nueva)
            Assert.True(Permisos.Todos.All(Function(p) enNueva.Tiene(p)))
            Dim dueno As New ServicioAdministracion(bd.CadenaAplicacion, enNueva)

            ' Áreas: Planificación (menú, factores, costo y pax) y Abastecimiento (compras y consolidado) son roles distintos.
            dueno.CrearUsuario("plan", "Planificadora", "Plan-Clave-2026", nueva, RolesBase.Planificacion)
            dueno.CrearUsuario("compras", "Comprador", "Compras-Clave-2026", nueva, RolesBase.Abastecimiento)
            Dim accesos = dueno.AccesosPorModulo()
            Dim plan = accesos.Single(Function(a) a.Login = "plan")
            Assert.Equal("Administrar||Ver", String.Join("|", plan.Planificacion, plan.Abastecimiento, plan.Catalogo))
            Dim compras = accesos.Single(Function(a) a.Login = "compras")
            Assert.Equal("Ver|Aprobar|", String.Join("|", compras.Planificacion, compras.Abastecimiento, compras.Administracion))
            Assert.Equal("DUENO DEL SISTEMA", accesos.First(Function(a) a.Login = "dueno").Roles)
            Assert.Equal(2, accesos.Where(Function(a) a.Login = "dueno").Count())          ' una fila por operación

            ' Un jefe que administra usuarios pero solo ve el catálogo no puede dar más de lo que tiene.
            admin.GuardarRol("JEFE_LOCAL", "Jefe local", {Permisos.UsuariosAdministrar, Permisos.CatalogoVer})
            admin.GuardarRol("SOLO_CATALOGO", "Solo catalogo", {Permisos.CatalogoVer})
            admin.CrearUsuario("jefe", "Jefe", "Jefe-Clave-2026", bd.A.OperacionId, "JEFE_LOCAL")
            Dim jefe As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A", "jefe", "Jefe-Clave-2026"))
            Dim usuarioPlan = jefe.ListarUsuarios().Single(Function(x) x.Login = "plan").Id
            Assert.Equal("ESCALADA_NO_PERMITIDA", Assert.Throws(Of ReglaNegocioException)(
                Sub() jefe.AsignarRol(usuarioPlan, bd.A.OperacionId, RolesBase.Administrador)).Codigo)
            Assert.Equal("ESCALADA_NO_PERMITIDA", Assert.Throws(Of ReglaNegocioException)(
                Sub() jefe.GuardarRol("MAS", "Mas", {Permisos.CatalogoVer, Permisos.PreciosEditar})).Codigo)
            jefe.AsignarRol(usuarioPlan, bd.A.OperacionId, "SOLO_CATALOGO")                  ' lo que sí tiene

            ' Solo el dueño otorga el rango de dueño: ni un administrador por la aplicación ni directo en la base.
            Dim idAdmin = admin.ListarUsuarios().Single(Function(x) x.Login = "admin").Id
            Assert.Equal("SOLO_DUENO", Assert.Throws(Of ReglaNegocioException)(Sub() admin.MarcarDueno(idAdmin, True)).Codigo)
            Dim idDueno = admin.ListarUsuarios().Single(Function(x) x.Login = "dueno").Id
            Assert.Equal("SOLO_DUENO", Assert.Throws(Of ReglaNegocioException)(Sub() admin.DesactivarUsuario(idDueno)).Codigo)
            Dim ex = Assert.Throws(Of Npgsql.PostgresException)(Sub() ComoUsuario(bd, idAdmin, $"UPDATE usuario SET es_dueno = true WHERE id = {idAdmin}"))
            Assert.Contains("SOLO_DUENO", ex.MessageText)
            dueno.MarcarDueno(idAdmin, True)
            Assert.True(admin.ListarUsuarios().Single(Function(x) x.Login = "admin").EsDueno)
            Assert.Equal("OPERACION_NO_PERMITIDA", Assert.Throws(Of ReglaNegocioException)(Sub() dueno.MarcarDueno(idDueno, False)).Codigo)
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
