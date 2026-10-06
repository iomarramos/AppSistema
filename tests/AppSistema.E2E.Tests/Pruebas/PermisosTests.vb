Imports Xunit
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Lo que ve cada rol en la aplicación real. Complementa las pruebas de servicio y de base (SeguridadCentralTests): aquí
''' se comprueba que la interfaz muestra solo lo permitido y que un cambio de la matriz de acceso se refleja al volver a
''' entrar.
''' </summary>
<Collection("E2E")>
Public Class PermisosTests

    Private ReadOnly _bd As BaseDatosE2E

    Public Sub New(bd As BaseDatosE2E)
        _bd = bd
    End Sub

    <FactE2E>
    Public Sub El_chef_ve_minutas_y_produccion_pero_no_administracion_ni_compras()
        Using app As New AplicacionE2E(_bd)
            Assert.True(New PaginaAcceso(app).IniciarSesion(UsuariosPrueba.Chef), app.Mensaje())
            Dim p As New PaginaPrincipal(app)
            Assert.False(p.MenuVisible("mnuAdministracion"))
            Assert.False(p.MenuVisible("mnuCompras"))
            Dim menus = p.Opciones("mnuMenus")
            Assert.Contains("mnuMinutasYNecesidades", menus)
            Assert.Contains("mnuProduccion", menus)
            Assert.DoesNotContain("mnuServiciosYEstructuras", menus)          ' factor central: MENUS_CONFIGURAR
            Dim minutas = p.Abrir("mnuMenus", "mnuMinutasYNecesidades")
            Assert.True(app.Existe("btnFactoresDeLaOperacion"))                ' factor operativo: FACTORES_EDITAR
            Assert.True(app.Existe("btnNuevaMinuta"))
            app.Capturar("permisos-chef-minutas")
            p.Cerrar(minutas)
        End Using
    End Sub

    <FactE2E>
    Public Sub El_almacenero_ve_inventario_pero_no_puede_cambiar_factores_ni_planificar()
        Using app As New AplicacionE2E(_bd)
            Assert.True(New PaginaAcceso(app).IniciarSesion(UsuariosPrueba.Almacenero), app.Mensaje())
            Dim p As New PaginaPrincipal(app)
            Assert.False(p.MenuVisible("mnuAdministracion"))
            Assert.Contains("mnuInventarioFisico", p.Opciones("mnuAlmacen"))
            Dim minutas = p.Abrir("mnuMenus", "mnuMinutasYNecesidades")
            Assert.False(app.Existe("btnFactoresDeLaOperacion"))
            Assert.False(app.Existe("btnNuevaMinuta"))
            app.Capturar("permisos-almacen-minutas")
            p.Cerrar(minutas)
        End Using
    End Sub

    <FactE2E>
    Public Sub Lo_que_niega_el_superusuario_desaparece_de_la_pantalla_y_lo_rechaza_el_servicio()
        Dim dueno As New ServicioAdministracion(_bd.CadenaAplicacion, _bd.Sesion(UsuariosPrueba.Dueno))
        Dim chef = _bd.IdUsuario(UsuariosPrueba.Chef.Login)
        dueno.FijarExcepcion(chef, Permisos.FactoresEditar, Nothing, False, "Prueba E2E")
        Try
            Assert.False(_bd.Sesion(UsuariosPrueba.Chef).Tiene(Permisos.FactoresEditar))
            Using app As New AplicacionE2E(_bd)
                Assert.True(New PaginaAcceso(app).IniciarSesion(UsuariosPrueba.Chef), app.Mensaje())
                Dim p As New PaginaPrincipal(app)
                Dim minutas = p.Abrir("mnuMenus", "mnuMinutasYNecesidades")
                Assert.False(app.Existe("btnFactoresDeLaOperacion"))
                app.Capturar("permisos-chef-factor-negado")
                p.Cerrar(minutas)
            End Using
        Finally
            dueno.FijarExcepcion(chef, Permisos.FactoresEditar, Nothing, Nothing)
        End Try
        Assert.True(_bd.Sesion(UsuariosPrueba.Chef).Tiene(Permisos.FactoresEditar))
    End Sub

End Class
