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
            Dim aud = admin.ConsultarAuditoria(Date.Today, Date.Today, "operacion", "admin")
            Assert.Contains(aud, Function(a) a.Accion = "INSERT" AndAlso a.Despues.Contains("Arequipa"))
            Assert.DoesNotContain(admin.ConsultarAuditoria(Date.Today, Date.Today, "", ""), Function(a) If(a.Despues, "").Contains("Otra operacion"))
            Assert.DoesNotContain(admin.ConsultarAuditoria(Date.Today, Date.Today, "usuario", ""), Function(a) If(a.Despues, "").Contains("password_hash"))
            Assert.Contains("operacion", admin.TablasAuditadas())

            admin.CrearUsuario("almacen", "Almacenero", "Almacen-Clave-2026", bd.A.OperacionId, "ALMACEN")
            Dim almacenero As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A", "almacen", "Almacen-Clave-2026"))
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() almacenero.ConsultarAuditoria(Date.Today, Date.Today, "", "")).Codigo)
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() almacenero.ListarOperaciones()).Codigo)
        End Using
    End Sub

End Class
