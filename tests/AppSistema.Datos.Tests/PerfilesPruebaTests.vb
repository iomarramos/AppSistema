Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Datos

''' <summary>Perfiles de prueba del instalador: una cuenta por rol, repetible, con clave que sirve para entrar.</summary>
Public Class PerfilesPruebaTests

    <FactPostgres>
    Public Sub Perfiles_de_prueba_una_cuenta_por_rol_se_crean_una_sola_vez_y_entran()
        Using bd = BaseDatosPrueba.Crear()
            Dim inst As New ServicioInstalacion(bd.CadenaAdmin)
            Dim primera = inst.CrearPerfilesPrueba("A", "ORC")
            Assert.Equal(6, primera.Count)
            Assert.All(primera, Sub(p) Assert.True(p.Creado, p.Login))
            Assert.Equal(6L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM usuario_operacion_rol uor JOIN usuario u ON u.id = uor.usuario_id " &
                                                       "WHERE u.login LIKE '%_prueba' AND uor.operacion_id = " & bd.A.OperacionId)))

            ' Cada perfil tiene el rol que le corresponde y su clave generada entra en la operación.
            For Each p In primera
                Assert.Equal(p.Rol, Convert.ToString(bd.Escalar("SELECT r.codigo FROM usuario u JOIN usuario_operacion_rol uor ON uor.usuario_id = u.id " &
                                                                "JOIN rol r ON r.id = uor.rol_id WHERE u.login = '" & p.Login & "'")))
                Dim sesion = New ServicioAcceso(bd.CadenaAplicacion).IniciarSesion("A", p.Login, p.Clave)
                Assert.NotNull(sesion)
            Next

            ' Repetir no cambia nada: las cuentas existentes se reportan y no reciben clave nueva.
            Dim segunda = inst.CrearPerfilesPrueba("A", "ORC")
            Assert.All(segunda, Sub(p) Assert.False(p.Creado, p.Login))
            Assert.Equal(6L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM usuario WHERE login LIKE '%_prueba'")))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Perfiles_de_prueba_rechazan_empresa_u_operacion_inexistentes()
        Using bd = BaseDatosPrueba.Crear()
            Dim inst As New ServicioInstalacion(bd.CadenaAdmin)
            Assert.Equal("DATO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(Function() inst.CrearPerfilesPrueba("NO-EXISTE", "ORC")).Codigo)
            Assert.Equal("DATO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(Function() inst.CrearPerfilesPrueba("A", "NO-EXISTE")).Codigo)
            Assert.Equal(0L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM usuario WHERE login LIKE '%_prueba'")))
        End Using
    End Sub

End Class
