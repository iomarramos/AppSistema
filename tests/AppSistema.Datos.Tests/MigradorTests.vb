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
            Assert.Equal(87L, Convert.ToInt64(bd.Escalar(
                "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_type = 'BASE TABLE'")))  ' 59 del esquema + esquema_migracion + 7 de sincronizacion (V012) + 3 de teorico vs real (V015) + producto_operacion (V018) + usuario_permiso (V021) + 6 de planificacion central (V022) + 9 del plan del SGP (V023)
        End Using
    End Sub

    ''' <summary>
    ''' Toda clave foránea tiene un índice cuyo prefijo son sus columnas (V017), salvo esta lista fija: quién creó, aprobó
    ''' o contó (un usuario no se borra, se desactiva) y catálogos que no se borran. Una FK nueva debe llevar su índice o
    ''' agregarse aquí a conciencia.
    ''' </summary>
    <FactPostgres>
    Public Sub Las_claves_foraneas_tienen_indice_salvo_las_de_usuario_y_catalogos_fijos()
        Dim permitidas = String.Join(" ", {
            "auditoria.empresa_id,usuario_id", "central_cierre.empresa_id", "central_movimiento.empresa_id",
            "cierre_diario.empresa_id,usuario_cierre_id", "consumo_plato.empresa_id,usuario_id", "documento_stock.empresa_id,aprobador_id",
            "documento_stock.empresa_id,usuario_id", "factor_consumo_operacion.empresa_id,usuario_id", "gasto.empresa_id,operacion_servicio_id",
            "gasto.empresa_id,usuario_id", "inventario.empresa_id,autorizador_id", "inventario.empresa_id,revisor_id", "inventario.empresa_id,usuario_id",
            "inventario_ajuste.empresa_id,autorizador_id", "inventario_detalle.empresa_id,contado_por", "merma_produccion.empresa_id,documento_baja_id",
            "merma_produccion.empresa_id,receta_version_id", "merma_produccion.empresa_id,unidad_id", "minuta.empresa_id,minuta_origen_id",
            "minuta.empresa_id,usuario_id", "movimiento_stock.empresa_id,usuario_id", "operacion_servicio.empresa_id,regimen_id",
            "pedido_compra.empresa_id,aprobador_id", "pedido_compra.empresa_id,usuario_id", "periodo_mensual.empresa_id,usuario_cierre_id",
            "prevision.empresa_id,usuario_id", "produccion.empresa_id,usuario_id", "producto_base.empresa_id,unidad_base_id", "producto_operacion.empresa_id,usuario_id",
            "recepcion.empresa_id,usuario_id", "recepcion_detalle.empresa_id,empaque_id", "requerimiento.empresa_id,aprobador_id",
            "requerimiento.empresa_id,usuario_id", "sincronizacion_evento.empresa_id,operacion_id", "variante_producto.empresa_id,marca_id",
            "venta_servicio.empresa_id,usuario_id"})
        Using bd = BaseDatosPrueba.Crear()
            Dim sinIndice = CStr(bd.Escalar(
                "WITH fk AS (SELECT c.conrelid, c.conkey, c.conrelid::regclass::text AS tabla, string_agg(a.attname, ',' ORDER BY x.n) AS cols " &
                "  FROM pg_constraint c CROSS JOIN LATERAL unnest(c.conkey) WITH ORDINALITY x(attnum, n) " &
                "  JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = x.attnum " &
                "  WHERE c.contype = 'f' AND c.connamespace = 'public'::regnamespace GROUP BY 1, 2, 3) " &
                "SELECT COALESCE(string_agg(tabla || '.' || cols, ' ' ORDER BY tabla COLLATE ""C"", cols COLLATE ""C""), '') FROM fk WHERE NOT EXISTS (" &
                "  SELECT 1 FROM pg_index i WHERE i.indrelid = fk.conrelid " &
                "  AND (i.indkey::int2[])[0:array_length(fk.conkey, 1) - 1] @> fk.conkey::int2[] " &
                "  AND (i.indkey::int2[])[0:array_length(fk.conkey, 1) - 1] <@ fk.conkey::int2[])"))
            Assert.Equal(permitidas, sinIndice)
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
