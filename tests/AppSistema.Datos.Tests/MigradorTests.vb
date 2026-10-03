Imports Npgsql
Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Datos

Public Class MigradorTests

    <FactPostgres>
    Public Sub Aplica_todas_las_migraciones_una_vez_y_repetir_no_hace_nada()
        Using bd = BaseDatosPrueba.Crear()   ' el fixture ya migró
            Dim segunda = New Migrador(bd.CadenaAdmin).Migrar()
            Assert.Equal(Migrador.Disponibles().Count, segunda.Count)
            Assert.True(segunda.All(Function(m) Not m.Aplicada))
            Assert.Equal(CLng(Migrador.Disponibles().Count), Convert.ToInt64(bd.Escalar("SELECT count(*) FROM esquema_migracion")))
            Assert.Equal(60L, Convert.ToInt64(bd.Escalar(
                "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'")))  ' 59 del esquema + esquema_migracion
        End Using
    End Sub

    <FactPostgres>
    Public Sub Una_migracion_aplicada_que_cambia_detiene_el_proceso()
        Using bd = BaseDatosPrueba.Crear()
            bd.EjecutarAdmin("UPDATE esquema_migracion SET sha256 = 'otro' WHERE version = 'V002'")
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() New Migrador(bd.CadenaAdmin).Migrar())
            Assert.Equal("MIGRACION_MODIFICADA", ex.Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub El_usuario_de_sede_hereda_permisos_y_queda_aislado_por_empresa()
        Using bd = BaseDatosPrueba.Crear()
            Dim nombre = "sede_" & bd.Nombre.Substring(bd.Nombre.Length - 8)
            Try
                Call New Migrador(bd.CadenaAdmin).CrearUsuarioSede(nombre, "Sede-Clave-2026")
                Assert.Throws(Of ReglaNegocioException)(Sub() Call New Migrador(bd.CadenaAdmin).CrearUsuarioSede("x; DROP TABLE y", "Sede-Clave-2026"))
                ' Sin contexto de empresa el usuario de sede no ve datos; con contexto ve solo su empresa.
                bd.EjecutarAdmin($"GRANT CONNECT ON DATABASE {bd.Nombre} TO {nombre}")
                Using cn As New NpgsqlConnection(bd.CadenaAdmin & $";Options=-c role={nombre}")
                    cn.Open()
                    Using cmd As New NpgsqlCommand("SELECT count(*) FROM producto_base", cn)
                        Assert.Equal(0L, Convert.ToInt64(cmd.ExecuteScalar()))
                    End Using
                    Using cmd As New NpgsqlCommand($"SELECT set_config('app.empresa_id', '{bd.A.EmpresaId}', false); SELECT count(*) FROM producto_base", cn)
                        Assert.Equal(1L, Convert.ToInt64(cmd.ExecuteScalar()))
                    End Using
                    Using cmd As New NpgsqlCommand("UPDATE saldo_stock SET cantidad_base_u6 = 0", cn)
                        Assert.Equal("42501", Assert.Throws(Of PostgresException)(Function() cmd.ExecuteNonQuery()).SqlState)
                    End Using
                End Using
            Finally
                NpgsqlConnection.ClearAllPools()
                bd.EjecutarAdmin($"DROP OWNED BY {nombre}; DROP ROLE IF EXISTS {nombre}")
            End Try
        End Using
    End Sub

End Class
