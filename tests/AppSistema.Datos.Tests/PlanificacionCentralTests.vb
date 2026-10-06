Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad
Imports AppSistema.Datos

''' <summary>
''' Fase 2 de Planificación y Abastecimiento Central (V022): planificación con versiones. Una versión aprobada es inmutable
''' (T49) y crear una versión nueva no toca la aprobada (T56). Las reglas se verifican en la base con la sesión de la aplicación.
''' </summary>
Public Class PlanificacionCentralTests

    Private Const Clave As String = "Clave-Persona-2026"
    Private Const CodigoNov As String = "PLAN-2026-11-ORC"

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>Conexión como un usuario de la aplicación (la base aplica sus permisos y el alcance).</summary>
    Private Shared Function Abrir(bd As BaseDatosPrueba, usuarioId As Long) As Npgsql.NpgsqlConnection
        Dim cn As New Npgsql.NpgsqlConnection(bd.CadenaAplicacion)
        cn.Open()
        Using cmd As New Npgsql.NpgsqlCommand($"SELECT set_config('app.empresa_id', '{bd.A.EmpresaId}', false), set_config('app.usuario_id', '{usuarioId}', false)", cn)
            cmd.ExecuteNonQuery()
        End Using
        Return cn
    End Function

    Private Shared Sub ComoUsuario(bd As BaseDatosPrueba, usuarioId As Long, sql As String)
        Using cn = Abrir(bd, usuarioId)
            Using cmd As New Npgsql.NpgsqlCommand(sql, cn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    Private Shared Function ValorComoUsuario(bd As BaseDatosPrueba, usuarioId As Long, sql As String) As Long
        Using cn = Abrir(bd, usuarioId)
            Using cmd As New Npgsql.NpgsqlCommand(sql, cn)
                Return Convert.ToInt64(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    Private Shared Function Falla(bd As BaseDatosPrueba, usuarioId As Long, sql As String) As Npgsql.PostgresException
        Return Assert.Throws(Of Npgsql.PostgresException)(Sub() ComoUsuario(bd, usuarioId, sql))
    End Function

    Private Class Escenario
        Public Property Empresa As Long
        Public Property Orc As Long
        Public Property Servicio As Long
        Public Property Estructura As Long
        Public Property Regimen As Long
        Public Property Receta As Long
        Public Property RecetaVersion As Long
        Public Property UsuarioPlan As Long
        Public Property UsuarioAlmacen As Long
    End Class

    ''' <summary>Una operación con su servicio, estructura, régimen, receta y dos usuarios: el planificador central y un almacenero.</summary>
    Private Shared Function Preparar(bd As BaseDatosPrueba) As Escenario
        Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
        Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, bd.Sesion("A"))
        Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, bd.Sesion("A"))
        Dim e As New Escenario With {.Empresa = bd.A.EmpresaId, .Orc = bd.A.OperacionId}
        e.Servicio = minutas.CrearServicio("ALM", "Almuerzo")
        e.Estructura = minutas.CrearEstructura(e.Servicio, "SOPA", "Sopa", 1, 8000)
        e.Regimen = minutas.CrearRegimen("GEN", "General")
        e.Receta = recetas.CrearReceta("SOPA", "Sopa", Nothing, U(1D), Nothing)
        e.RecetaVersion = Convert.ToInt64(bd.Escalar($"SELECT id FROM receta_version WHERE receta_id = {e.Receta} ORDER BY version LIMIT 1"))
        admin.CrearUsuario("plan", "Planificador central", Clave, e.Orc, RolesBase.PlanificadorCentral)
        admin.CrearUsuario("almacen", "Almacenero", Clave, e.Orc, "ALMACEN")
        Dim usuarios = admin.ListarUsuarios()
        e.UsuarioPlan = usuarios.Single(Function(x) x.Login = "plan").Id
        e.UsuarioAlmacen = usuarios.Single(Function(x) x.Login = "almacen").Id
        Return e
    End Function

    Private Shared Function NuevaPlanificacion(bd As BaseDatosPrueba, e As Escenario, codigo As String, desde As String, hasta As String) As Long
        ComoUsuario(bd, e.UsuarioPlan, $"INSERT INTO planificacion(empresa_id, codigo, operacion_id, periodo_desde, periodo_hasta, regimen_id, creado_por) " &
                                       $"VALUES ({e.Empresa}, '{codigo}', {e.Orc}, '{desde}', '{hasta}', {e.Regimen}, {e.UsuarioPlan})")
        Return Convert.ToInt64(bd.Escalar($"SELECT id FROM planificacion WHERE codigo = '{codigo}'"))
    End Function

    Private Shared Function NuevaVersion(bd As BaseDatosPrueba, e As Escenario, planificacion As Long, numero As Long,
                                         Optional anterior As Long? = Nothing, Optional motivo As String = Nothing) As Long
        Return ValorComoUsuario(bd, e.UsuarioPlan,
            $"INSERT INTO planificacion_version(empresa_id, planificacion_id, numero, version_anterior_id, motivo_cambio, creado_por) " &
            $"VALUES ({e.Empresa}, {planificacion}, {numero}, {If(anterior.HasValue, anterior.Value.ToString(), "NULL")}, " &
            $"{If(motivo Is Nothing, "NULL", "'" & motivo & "'")}, {e.UsuarioPlan}) RETURNING id")
    End Function

    Private Shared Function NuevoServicio(bd As BaseDatosPrueba, e As Escenario, version As Long, comensales As Long) As Long
        Return ValorComoUsuario(bd, e.UsuarioPlan,
            $"INSERT INTO planificacion_servicio(empresa_id, version_id, fecha, servicio_id, estructura_id, comensales_previstos) " &
            $"VALUES ({e.Empresa}, {version}, '2026-11-03', {e.Servicio}, {e.Estructura}, {comensales}) RETURNING id")
    End Function

    Private Shared Sub NuevoPlato(bd As BaseDatosPrueba, e As Escenario, version As Long, servicioPlanificado As Long, raciones As Long)
        ComoUsuario(bd, e.UsuarioPlan,
            $"INSERT INTO planificacion_plato(empresa_id, version_id, servicio_planificado_id, receta_id, receta_version_id, " &
            $"factor_teorico_bp, reparto_bp, raciones_u6) VALUES ({e.Empresa}, {version}, {servicioPlanificado}, {e.Receta}, {e.RecetaVersion}, " &
            $"10000, 10000, {raciones})")
    End Sub

    ''' <summary>Planificación con una versión 1 de un servicio y un plato, aprobada por el planificador.</summary>
    Private Shared Function AprobarPrimera(bd As BaseDatosPrueba, e As Escenario, codigo As String, desde As String, hasta As String) As Long
        Dim planificacion = NuevaPlanificacion(bd, e, codigo, desde, hasta)
        Dim v1 = NuevaVersion(bd, e, planificacion, 1)
        Dim sp = NuevoServicio(bd, e, v1, 120)
        NuevoPlato(bd, e, v1, sp, U(120D))
        ComoUsuario(bd, e.UsuarioPlan, $"UPDATE planificacion_version SET estado = 'APROBADO', aprobado_por = {e.UsuarioPlan}, aprobado_en = now() WHERE id = {v1}")
        Return v1
    End Function

    <FactPostgres>
    Public Sub T49_una_version_aprobada_no_cambia_y_su_historial_queda()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = Preparar(bd)
            Dim v1 = AprobarPrimera(bd, e, CodigoNov, "2026-11-01", "2026-11-30")

            Assert.Contains("VERSION_INMUTABLE", Falla(bd, e.UsuarioPlan, $"UPDATE planificacion_version SET costo_total_u6 = 1 WHERE id = {v1}").MessageText)
            Assert.Contains("VERSION_INMUTABLE", Falla(bd, e.UsuarioPlan, $"UPDATE planificacion_version SET estado = 'BORRADOR' WHERE id = {v1}").MessageText)
            Assert.Contains("VERSION_INMUTABLE", Falla(bd, e.UsuarioPlan, $"DELETE FROM planificacion_version WHERE id = {v1}").MessageText)
            Assert.Contains("VERSION_INMUTABLE", Falla(bd, e.UsuarioPlan,
                $"INSERT INTO planificacion_servicio(empresa_id, version_id, fecha, servicio_id, estructura_id, comensales_previstos) " &
                $"VALUES ({e.Empresa}, {v1}, '2026-11-04', {e.Servicio}, {e.Estructura}, 50)").MessageText)
            Assert.Contains("VERSION_INMUTABLE", Falla(bd, e.UsuarioPlan, $"UPDATE planificacion_plato SET raciones_u6 = 1 WHERE version_id = {v1}").MessageText)
            Assert.Contains("VERSION_INMUTABLE", Falla(bd, e.UsuarioPlan, $"DELETE FROM planificacion_servicio WHERE version_id = {v1}").MessageText)

            ' El historial lo escribe la base y no se edita.
            Assert.Equal(2L, Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM planificacion_estado WHERE version_id = {v1}")))
            Assert.Contains("HISTORIAL_INMUTABLE", Falla(bd, e.UsuarioPlan, $"UPDATE planificacion_estado SET observacion = 'x' WHERE version_id = {v1}").MessageText)
            Assert.Contains("HISTORIAL_INMUTABLE", Falla(bd, e.UsuarioPlan, $"DELETE FROM planificacion_estado WHERE version_id = {v1}").MessageText)
        End Using
    End Sub

    <FactPostgres>
    Public Sub T49_el_almacen_no_prepara_ni_aprueba_planificaciones()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = Preparar(bd)
            Assert.Contains("SIN_PERMISO", Falla(bd, e.UsuarioAlmacen,
                $"INSERT INTO planificacion(empresa_id, codigo, operacion_id, periodo_desde, periodo_hasta, regimen_id, creado_por) " &
                $"VALUES ({e.Empresa}, 'PLAN-2026-11-ORC', {e.Orc}, '2026-11-01', '2026-11-30', {e.Regimen}, {e.UsuarioAlmacen})").MessageText)

            Dim planificacion = NuevaPlanificacion(bd, e, CodigoNov, "2026-11-01", "2026-11-30")
            Dim v1 = NuevaVersion(bd, e, planificacion, 1)
            Dim sp = NuevoServicio(bd, e, v1, 120)
            Assert.Contains("SIN_PERMISO", Falla(bd, e.UsuarioAlmacen,
                $"UPDATE planificacion_version SET estado = 'APROBADO', aprobado_por = {e.UsuarioAlmacen}, aprobado_en = now() WHERE id = {v1}").MessageText)
            Assert.Contains("SIN_PERMISO", Falla(bd, e.UsuarioAlmacen,
                $"INSERT INTO planificacion_servicio(empresa_id, version_id, fecha, servicio_id, estructura_id, comensales_previstos) " &
                $"VALUES ({e.Empresa}, {v1}, '2026-11-04', {e.Servicio}, {e.Estructura}, 50)").MessageText)
        End Using
    End Sub

    <FactPostgres>
    Public Sub T56_una_version_nueva_no_toca_la_aprobada_y_solo_la_retira_en_la_misma_transaccion()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = Preparar(bd)
            Dim v1 = AprobarPrimera(bd, e, CodigoNov, "2026-11-01", "2026-11-30")
            Dim planificacion = Convert.ToInt64(bd.Escalar($"SELECT planificacion_id FROM planificacion_version WHERE id = {v1}"))

            ' La versión 2 se prepara mientras la 1 sigue aprobada.
            Dim v2 = NuevaVersion(bd, e, planificacion, 2, v1, "Sube el factor de la sopa")
            NuevoServicio(bd, e, v2, 150)
            Assert.Equal(1L, Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM planificacion_servicio WHERE version_id = {v1}")))
            Assert.Equal(120000000L, Convert.ToInt64(bd.Escalar($"SELECT raciones_u6 FROM planificacion_plato WHERE version_id = {v1}")))

            ' No pueden quedar dos versiones aprobadas.
            Assert.Equal("23505", Falla(bd, e.UsuarioPlan,
                $"UPDATE planificacion_version SET estado = 'APROBADO', aprobado_por = {e.UsuarioPlan}, aprobado_en = now() WHERE id = {v2}").SqlState)

            ' Retirar la 1 no puede cambiarle nada más que el estado.
            Assert.Contains("VERSION_INMUTABLE", Falla(bd, e.UsuarioPlan,
                $"UPDATE planificacion_version SET estado = 'REEMPLAZADO', costo_total_u6 = 5 WHERE id = {v1}").MessageText)

            ' Retirar y aprobar en la misma transacción.
            ComoUsuario(bd, e.UsuarioPlan,
                $"UPDATE planificacion_version SET estado = 'REEMPLAZADO' WHERE id = {v1}; " &
                $"UPDATE planificacion_version SET estado = 'APROBADO', aprobado_por = {e.UsuarioPlan}, aprobado_en = now() WHERE id = {v2};")

            Assert.Equal("REEMPLAZADO", CStr(bd.Escalar($"SELECT estado FROM planificacion_version WHERE id = {v1}")))
            Assert.Equal("APROBADO", CStr(bd.Escalar($"SELECT estado FROM planificacion_version WHERE id = {v2}")))
            Assert.Equal(1L, Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM planificacion_servicio WHERE version_id = {v1}")))
            Assert.Equal(120000000L, Convert.ToInt64(bd.Escalar($"SELECT raciones_u6 FROM planificacion_plato WHERE version_id = {v1}")))
            Assert.Equal(3L, Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM planificacion_estado WHERE version_id = {v1}")))
            Assert.Contains("VERSION_INMUTABLE", Falla(bd, e.UsuarioPlan, $"UPDATE planificacion_version SET estado = 'APROBADO' WHERE id = {v1}").MessageText)
        End Using
    End Sub

    <FactPostgres>
    Public Sub T56_no_se_retira_una_version_si_no_hay_otra_mas_nueva()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = Preparar(bd)
            Dim v1 = AprobarPrimera(bd, e, CodigoNov, "2026-11-01", "2026-11-30")
            Assert.Contains("SIN_VERSION_NUEVA", Falla(bd, e.UsuarioPlan, $"UPDATE planificacion_version SET estado = 'REEMPLAZADO' WHERE id = {v1}").MessageText)
            Assert.Equal("APROBADO", CStr(bd.Escalar($"SELECT estado FROM planificacion_version WHERE id = {v1}")))
        End Using
    End Sub

    <FactPostgres>
    Public Sub T49_una_version_sin_servicios_no_se_aprueba()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = Preparar(bd)
            Dim planificacion = NuevaPlanificacion(bd, e, CodigoNov, "2026-11-01", "2026-11-30")
            Dim v1 = NuevaVersion(bd, e, planificacion, 1)
            Assert.Contains("VERSION_VACIA", Falla(bd, e.UsuarioPlan,
                $"UPDATE planificacion_version SET estado = 'APROBADO', aprobado_por = {e.UsuarioPlan}, aprobado_en = now() WHERE id = {v1}").MessageText)
        End Using
    End Sub
End Class
