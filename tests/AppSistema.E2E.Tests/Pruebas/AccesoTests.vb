Imports Xunit

''' <summary>Inicio de sesión real sobre AppSistema.exe.</summary>
<Collection("E2E")>
Public Class AccesoTests

    Private ReadOnly _bd As BaseDatosE2E

    Public Sub New(bd As BaseDatosE2E)
        _bd = bd
    End Sub

    <FactE2E>
    Public Sub El_superusuario_entra_y_la_barra_de_estado_lo_indica()
        Using app As New AplicacionE2E(_bd)
            Assert.True(New PaginaAcceso(app).IniciarSesion(UsuariosPrueba.Dueno), app.Mensaje())
            Dim principal As New PaginaPrincipal(app)
            Dim estado = principal.Estado()
            Assert.Contains("DUENO DEL SISTEMA", estado)
            Assert.Contains(UsuariosPrueba.Empresa, estado)
            Assert.All(PaginaPrincipal.MenusDeTrabajo, Sub(m) Assert.True(principal.MenuVisible(m), m))
            app.Capturar("01-inicio-superusuario")
        End Using
    End Sub

    <FactE2E>
    Public Sub Una_clave_incorrecta_muestra_el_aviso_y_no_entra()
        Using app As New AplicacionE2E(_bd)
            Dim acceso As New PaginaAcceso(app)
            Assert.False(acceso.IniciarSesion((UsuariosPrueba.Chef.Login, "Clave-Equivocada-1")))
            Dim mensaje = app.Mensaje()
            Assert.NotNull(mensaje)
            Assert.Contains("incorrectos", mensaje)
            app.Capturar("00-acceso-clave-incorrecta")
            app.CerrarMensaje()
            Assert.True(app.Existe("FormAcceso"))
        End Using
    End Sub

End Class
