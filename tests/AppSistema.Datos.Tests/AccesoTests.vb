Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Seguridad
Imports AppSistema.Datos

Public Class AccesoTests

    <FactPostgres>
    Public Sub Inicio_de_sesion_correcto_y_permisos_de_la_operacion()
        Using bd = BaseDatosPrueba.Crear()
            Dim acceso As New ServicioAcceso(bd.CadenaAplicacion)
            Dim s = acceso.IniciarSesion("A", "admin", BaseDatosPrueba.ClaveAdmin)
            Assert.Equal(bd.A.EmpresaId, s.EmpresaId)
            Assert.Null(s.Operacion)
            Assert.Equal("ORC", s.Operaciones.Single().Codigo)
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Sub() s.Exigir(Permisos.CatalogoVer))
            Assert.Equal("OPERACION_NO_SELECCIONADA", ex.Codigo)

            Dim conOp = acceso.SeleccionarOperacion(s, s.Operaciones(0).Id)
            Assert.Equal(Permisos.Todos.Count, conOp.Permisos.Count)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Clave_incorrecta_y_usuario_inexistente_dan_el_mismo_error()
        Using bd = BaseDatosPrueba.Crear()
            Dim acceso As New ServicioAcceso(bd.CadenaAplicacion)
            Dim e1 = Assert.Throws(Of ReglaNegocioException)(Function() acceso.IniciarSesion("A", "admin", "Clave-Mala-2026"))
            Dim e2 = Assert.Throws(Of ReglaNegocioException)(Function() acceso.IniciarSesion("A", "nadie", "Clave-Mala-2026"))
            Dim e3 = Assert.Throws(Of ReglaNegocioException)(Function() acceso.IniciarSesion("ZZ", "admin", BaseDatosPrueba.ClaveAdmin))
            Assert.Equal("CREDENCIALES_INVALIDAS", e1.Codigo)
            Assert.Equal(e1.Message, e2.Message)
            Assert.Equal(e1.Message, e3.Message)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Cinco_fallos_bloquean_aunque_luego_se_use_la_clave_correcta()
        Using bd = BaseDatosPrueba.Crear()
            Dim acceso As New ServicioAcceso(bd.CadenaAplicacion)
            For i = 1 To 5
                Assert.Throws(Of ReglaNegocioException)(Function() acceso.IniciarSesion("A", "admin", "Clave-Mala-2026"))
            Next
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() acceso.IniciarSesion("A", "admin", BaseDatosPrueba.ClaveAdmin))
            Assert.Equal("USUARIO_BLOQUEADO", ex.Codigo)
            ' La otra empresa no se ve afectada.
            Assert.NotNull(acceso.IniciarSesion("B", "admin", BaseDatosPrueba.ClaveAdmin))
        End Using
    End Sub

    <FactPostgres>
    Public Sub La_clave_se_guarda_con_hash_y_no_en_texto_plano()
        Using bd = BaseDatosPrueba.Crear()
            Dim hash = CStr(bd.Escalar($"SELECT password_hash FROM usuario WHERE id = {bd.A.AdminUsuarioId}"))
            Assert.StartsWith("pbkdf2-sha256$", hash)
            Assert.DoesNotContain(BaseDatosPrueba.ClaveAdmin, hash)
            Assert.Equal(0L, Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM auditoria WHERE coalesce(despues_json,'') LIKE '%pbkdf2%'")))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Cambio_de_clave()
        Using bd = BaseDatosPrueba.Crear()
            Dim acceso As New ServicioAcceso(bd.CadenaAplicacion)
            Dim s = bd.Sesion("A")
            Assert.Equal("CLAVE_DEBIL", Assert.Throws(Of ReglaNegocioException)(Sub() acceso.CambiarClave(s, BaseDatosPrueba.ClaveAdmin, "corta")).Codigo)
            Assert.Equal("CREDENCIALES_INVALIDAS", Assert.Throws(Of ReglaNegocioException)(Sub() acceso.CambiarClave(s, "Otra-Clave-123", "Nueva-Clave-2027")).Codigo)
            acceso.CambiarClave(s, BaseDatosPrueba.ClaveAdmin, "Nueva-Clave-2027")
            Assert.Throws(Of ReglaNegocioException)(Function() acceso.IniciarSesion("A", "admin", BaseDatosPrueba.ClaveAdmin))
            Assert.NotNull(acceso.IniciarSesion("A", "admin", "Nueva-Clave-2027"))
            Assert.Equal("CAMBIO_CLAVE", CStr(bd.Escalar("SELECT accion FROM auditoria WHERE tabla = 'usuario' ORDER BY id DESC LIMIT 1")))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Usuario_desactivado_no_entra()
        Using bd = BaseDatosPrueba.Crear()
            Dim adm As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim id = adm.CrearUsuario("almacen1", "Almacenero", "Almacen-2026-x", bd.A.OperacionId, "ALMACEN")
            Assert.NotNull(bd.Sesion("A", "almacen1", "Almacen-2026-x"))
            adm.DesactivarUsuario(id)
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() bd.Sesion("A", "almacen1", "Almacen-2026-x"))
            Assert.Equal("CREDENCIALES_INVALIDAS", ex.Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Instalacion_no_deja_usuarios_ni_claves_por_defecto()
        Using bd = BaseDatosPrueba.Crear()
            Assert.Equal(1L, Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM usuario WHERE empresa_id = {bd.A.EmpresaId}")))
            Dim inst As New ServicioInstalacion(bd.CadenaAdmin)
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() inst.CrearEmpresa(New DatosInstalacion With {
                .EmpresaCodigo = "C", .EmpresaNombre = "C", .OperacionCodigo = "O", .OperacionNombre = "O", .AlmacenCodigo = "A",
                .AlmacenNombre = "A", .AdminLogin = "admin", .AdminNombre = "Admin", .AdminClave = "admin"}))
            Assert.Equal("CLAVE_DEBIL", ex.Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Cocina_consulta_el_catalogo_pero_no_lo_modifica_ni_administra_usuarios()
        Using bd = BaseDatosPrueba.Crear()
            Dim adm As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            adm.CrearUsuario("cocina1", "Cocinero", "Cocina-2026-xyz", bd.A.OperacionId, "COCINA")
            Dim s = bd.Sesion("A", "cocina1", "Cocina-2026-xyz")
            Dim cat As New ServicioCatalogo(bd.CadenaAplicacion, s)
            Assert.NotEmpty(cat.BuscarProductos(""))
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() cat.CrearMarca("X")).Codigo)
            Dim admCocina As New ServicioAdministracion(bd.CadenaAplicacion, s)
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() admCocina.ListarUsuarios()).Codigo)
            Assert.Contains(adm.ListarUsuarios(), Function(u) u.Login = "cocina1" AndAlso u.Roles = "ORC:COCINA")
        End Using
    End Sub

    <FactPostgres>
    Public Sub Seleccionar_una_operacion_sin_acceso_se_rechaza()
        Using bd = BaseDatosPrueba.Crear()
            Dim acceso As New ServicioAcceso(bd.CadenaAplicacion)
            Dim s = acceso.IniciarSesion("A", "admin", BaseDatosPrueba.ClaveAdmin)
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() acceso.SeleccionarOperacion(s, bd.B.OperacionId))
            Assert.Equal("SIN_PERMISO", ex.Codigo)
        End Using
    End Sub

End Class
