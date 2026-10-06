Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Datos

''' <summary>Administración: operaciones y almacenes de la empresa y consulta de auditoría con su permiso.</summary>
Public Class AdministracionTests

    <FactPostgres>
    Public Sub Operaciones_almacenes_y_auditoria_solo_de_la_empresa_y_con_permiso()
        Using bd = BaseDatosPrueba.Crear()
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim are = admin.CrearOperacion("ARE", "Arequipa")
            admin.CrearAlmacen(are, "P2", "Principal Arequipa")
            Dim ops = admin.ListarOperaciones()
            Assert.Equal({"ARE", "ORC"}, ops.Select(Function(o) o.Codigo))                 ' solo la empresa A (RLS)
            Assert.Equal(1L, ops.Single(Function(o) o.Codigo = "ORC").Usuarios)
            Assert.Equal("P2", admin.ListarAlmacenesDeOperacion(are).Single().Codigo)

            ' La auditoría registra quién creó la operación; no muestra datos de la empresa B ni claves.
            ' Rango de dos días: la base guarda la hora del servidor (UTC) y la del PC puede estar en otra zona horaria.
            Dim desde = Date.Today.AddDays(-1)
            Dim hasta = Date.Today.AddDays(1)
            Dim aud = admin.ConsultarAuditoria(desde, hasta, "operacion", "admin")
            Assert.Contains(aud, Function(a) a.Accion = "INSERT" AndAlso a.Despues.Contains("Arequipa"))
            Assert.DoesNotContain(admin.ConsultarAuditoria(desde, hasta, "", ""), Function(a) If(a.Despues, "").Contains("Otra operacion"))
            Assert.DoesNotContain(admin.ConsultarAuditoria(desde, hasta, "usuario", ""), Function(a) If(a.Despues, "").Contains("password_hash"))
            Assert.Contains("operacion", admin.TablasAuditadas())

            admin.CrearUsuario("almacen", "Almacenero", "Almacen-Clave-2026", bd.A.OperacionId, "ALMACEN")
            Dim almacenero As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A", "almacen", "Almacen-Clave-2026"))
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() almacenero.ConsultarAuditoria(Date.Today, Date.Today, "", "")).Codigo)
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() almacenero.ListarOperaciones()).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Sprint7_reinicio_de_clave_cumple_politica_y_reemplaza_la_anterior()
        Using bd = BaseDatosPrueba.Crear()
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim id = admin.CrearUsuario("cajero", "Cajero", "Clave-Inicial-2026", bd.A.OperacionId, "ALMACEN")
            Assert.Equal("CLAVE_DEBIL", Assert.Throws(Of ReglaNegocioException)(Sub() admin.ReiniciarClave(id, "corta")).Codigo)
            admin.ReiniciarClave(id, "Clave-Nueva-2026")
            Assert.NotNull(New ServicioAcceso(bd.CadenaAplicacion).IniciarSesion("A", "cajero", "Clave-Nueva-2026"))
            Assert.Throws(Of ReglaNegocioException)(Function() New ServicioAcceso(bd.CadenaAplicacion).IniciarSesion("A", "cajero", "Clave-Inicial-2026"))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Editar_usuario_desactiva_reactiva_y_protege_al_propio_usuario()
        Using bd = BaseDatosPrueba.Crear()
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim id = admin.CrearUsuario("editable", "Editable", "Clave-Editable-2026", bd.A.OperacionId, "ALMACEN")

            admin.EditarUsuario(id, "Nombre corregido", False)
            Assert.Throws(Of ReglaNegocioException)(Function() New ServicioAcceso(bd.CadenaAplicacion).IniciarSesion("A", "editable", "Clave-Editable-2026"))
            Assert.Equal("Nombre corregido", admin.ListarUsuarios().Single(Function(x) x.Login = "editable").Nombre)
            Assert.False(admin.ListarUsuarios().Single(Function(x) x.Login = "editable").Activo)

            admin.EditarUsuario(id, "Nombre corregido", True)
            Assert.NotNull(New ServicioAcceso(bd.CadenaAplicacion).IniciarSesion("A", "editable", "Clave-Editable-2026"))

            Assert.Equal("OPERACION_NO_PERMITIDA", Assert.Throws(Of ReglaNegocioException)(Sub() admin.EditarUsuario(bd.Sesion("A").UsuarioId, "Yo", False)).Codigo)
            Assert.Equal("DATO_OBLIGATORIO", Assert.Throws(Of ReglaNegocioException)(Sub() admin.EditarUsuario(id, "  ", True)).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Sprint7_8_auditoria_exportable_y_monitor_de_transitos()
        Using bd = BaseDatosPrueba.Crear()
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            admin.CrearUsuario("auditado", "Auditado", "Clave-Auditada-2026", bd.A.OperacionId, "ALMACEN")
            Dim reportes = New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim auditoria = reportes.Auditoria(Date.Today.AddDays(-1), Date.Today.AddDays(1), "usuario", "")
            Assert.Equal("Auditoria de cambios", auditoria.Titulo)
            Assert.NotEmpty(auditoria.Secciones(0).Filas)
            Assert.Empty(reportes.Transitos(Date.Today.AddDays(-30), Date.Today).Secciones(0).Filas)
        End Using
    End Sub

End Class
