Imports System.Threading.Tasks
Imports Npgsql
Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

' Requieren PostgreSQL: APPSISTEMA_PG_PRUEBAS=1. Datos ficticios; costo fijo de S/8 por litro (valoración real pendiente, D01).
Public Class ServicioStockTests

    Private Shared Function L(litros As Long) As Long
        Return litros * EscalaU6.Factor
    End Function

    Private Shared ReadOnly CostoPorLitro As Long = EscalaU6.DesdeDecimal(8D)

    Private Shared Function Doc(tipo As TipoDocumentoStock, numero As String, litros As Long,
                                Optional fecha As Date = Nothing, Optional empresaId As Long = 1) As DocumentoStockNuevo
        Dim f As Date = If(fecha = Nothing, New Date(2026, 10, 2), fecha)
        Return New DocumentoStockNuevo(empresaId, 1, 1, tipo, f, numero,
                                       {New LineaDocumentoStock(1, L(litros), CostoPorLitro)})
    End Function

    <FactPostgres>
    Public Sub T19_T20_Recibir_32_L_a_S_8_y_sacar_5_L_deja_27_L_y_S_216()
        Using bd = BaseDatosPrueba.Crear()
            Dim svc As New ServicioStock(bd.CadenaAplicacion)
            svc.Contabilizar(Doc(TipoDocumentoStock.Recepcion, "R-1", 32, New Date(2026, 10, 1)))
            Assert.Equal(L(32), bd.SaldoU6())
            Assert.Equal(EscalaU6.DesdeDecimal(256D), bd.ValorU6())

            svc.Contabilizar(Doc(TipoDocumentoStock.SalidaProduccion, "S-1", 5))
            Assert.Equal(L(27), bd.SaldoU6())
            Assert.Equal(EscalaU6.DesdeDecimal(216D), bd.ValorU6())
            Assert.Equal(0L, bd.FilasSinConciliar())
        End Using
    End Sub

    <FactPostgres>
    Public Sub T23_Error_en_la_segunda_linea_revierte_documento_movimientos_y_saldo()
        Using bd = BaseDatosPrueba.Crear()
            Dim svc As New ServicioStock(bd.CadenaAplicacion)
            svc.Contabilizar(Doc(TipoDocumentoStock.Recepcion, "R-1", 32, New Date(2026, 10, 1)))

            Dim lineas = {New LineaDocumentoStock(1, L(5), CostoPorLitro), New LineaDocumentoStock(1, L(100), CostoPorLitro)}
            Dim malo As New DocumentoStockNuevo(1, 1, 1, TipoDocumentoStock.SalidaProduccion, New Date(2026, 10, 2), "S-MALO", lineas)

            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() svc.Contabilizar(malo))
            Assert.Equal("STOCK_INSUFICIENTE", ex.Codigo)
            Assert.Equal(0L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM documento_stock WHERE numero = 'S-MALO'")))
            Assert.Equal(1L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM movimiento_stock")))
            Assert.Equal(L(32), bd.SaldoU6())
        End Using
    End Sub

    <FactPostgres>
    Public Sub T24_Dos_salidas_simultaneas_de_20_L_con_27_L_solo_permiten_una()
        Using bd = BaseDatosPrueba.Crear()
            Dim svc As New ServicioStock(bd.CadenaAplicacion)
            svc.Contabilizar(Doc(TipoDocumentoStock.Apertura, "A-1", 27, New Date(2026, 10, 1)))

            Dim barrera As New Threading.Barrier(2)
            Dim tareas = Enumerable.Range(1, 2).Select(
                Function(i) Task.Run(Function() As String
                                         barrera.SignalAndWait()
                                         Try
                                             svc.Contabilizar(Doc(TipoDocumentoStock.SalidaProduccion, "S-" & i, 20))
                                             Return "ok"
                                         Catch ex As ReglaNegocioException
                                             Return ex.Codigo
                                         End Try
                                     End Function)).ToArray()
            Task.WaitAll(tareas)
            Dim resultados = tareas.Select(Function(t) t.Result).OrderBy(Function(r) r, StringComparer.Ordinal).ToList()

            Assert.Equal(New List(Of String) From {"STOCK_INSUFICIENTE", "ok"}, resultados)
            Assert.Equal(L(7), bd.SaldoU6())
            Assert.Equal(0L, bd.FilasSinConciliar())
        End Using
    End Sub

    <FactPostgres>
    Public Sub Carrera_de_10_hilos_con_5_L_cada_uno_y_27_L_confirma_exactamente_5()
        Using bd = BaseDatosPrueba.Crear()
            Dim svc As New ServicioStock(bd.CadenaAplicacion)
            svc.Contabilizar(Doc(TipoDocumentoStock.Apertura, "A-1", 27, New Date(2026, 10, 1)))

            Dim barrera As New Threading.Barrier(10)
            Dim tareas = Enumerable.Range(1, 10).Select(
                Function(i) Task.Run(Function() As Boolean
                                         barrera.SignalAndWait()
                                         Try
                                             svc.Contabilizar(Doc(TipoDocumentoStock.SalidaProduccion, "S-" & i, 5))
                                             Return True
                                         Catch ex As ReglaNegocioException When ex.Codigo = "STOCK_INSUFICIENTE"
                                             Return False
                                         End Try
                                     End Function)).ToArray()
            Task.WaitAll(tareas)

            Assert.Equal(5, tareas.Count(Function(t) t.Result))
            Assert.Equal(L(2), bd.SaldoU6())
            Assert.Equal(0L, bd.FilasSinConciliar())
        End Using
    End Sub

    <FactPostgres>
    Public Sub T39_Dia_cerrado_rechaza_la_contabilizacion()
        Using bd = BaseDatosPrueba.Crear()
            bd.EjecutarAdmin("INSERT INTO cierre_diario(empresa_id, operacion_id, fecha, estado, usuario_cierre_id) VALUES (1,1,'2026-10-02','cerrado',1)")
            Dim svc As New ServicioStock(bd.CadenaAplicacion)
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() svc.Contabilizar(Doc(TipoDocumentoStock.Apertura, "A-1", 10)))
            Assert.Equal("DIA_CERRADO", ex.Codigo)
            Assert.Equal(0L, bd.SaldoU6())
        End Using
    End Sub

    <FactPostgres>
    Public Sub T26_El_servidor_rechaza_un_signo_incoherente_con_el_tipo()
        ' Se fuerza el caso saltando el servicio: insertar un movimiento positivo en una salida a producción.
        Using bd = BaseDatosPrueba.Crear()
            bd.EjecutarAdmin("INSERT INTO documento_stock(id,empresa_id,almacen_id,numero,fecha,tipo,estado,usuario_id) VALUES (50,1,1,'X','2026-10-02','salida_produccion','borrador',1)")
            bd.EjecutarAdmin("INSERT INTO documento_stock_detalle(id,empresa_id,documento_id,variante_id,cantidad_base_u6,costo_unitario_base_u6,valor_u6) VALUES (50,1,50,1,1000000,8000000,8000000)")
            bd.EjecutarAdmin("UPDATE documento_stock SET estado='confirmado' WHERE id=50")
            Dim ex = Assert.Throws(Of PostgresException)(Sub() bd.EjecutarAdmin(
                "INSERT INTO movimiento_stock(empresa_id,documento_detalle_id,almacen_id,variante_id,fecha,secuencia,signo,cantidad_base_u6,costo_unitario_base_u6,valor_u6,usuario_id) " &
                "VALUES (1,50,1,1,'2026-10-02',1,1,1000000,8000000,8000000,1)"))
            Assert.Contains("SIGNO_NO_PERMITIDO", ex.MessageText)
        End Using
    End Sub

    <FactPostgres>
    Public Sub T01_Una_empresa_no_contabiliza_en_el_almacen_de_otra()
        Using bd = BaseDatosPrueba.Crear()
            Dim svc As New ServicioStock(bd.CadenaAplicacion)
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() svc.Contabilizar(Doc(TipoDocumentoStock.Apertura, "A-1", 10, empresaId:=2)))
            Assert.Equal("ALMACEN_NO_ENCONTRADO", ex.Codigo)
            Assert.Equal(0L, bd.SaldoU6())
        End Using
    End Sub

    <FactPostgres>
    Public Sub El_rol_de_la_aplicacion_no_puede_escribir_el_saldo_directamente()
        Using bd = BaseDatosPrueba.Crear()
            Dim svc As New ServicioStock(bd.CadenaAplicacion)
            svc.Contabilizar(Doc(TipoDocumentoStock.Apertura, "A-1", 10, New Date(2026, 10, 1)))

            Using cn As New NpgsqlConnection(bd.CadenaAplicacion)
                cn.Open()
                Using cmd As New NpgsqlCommand("UPDATE saldo_stock SET cantidad_base_u6 = 999000000", cn)
                    Dim ex = Assert.Throws(Of PostgresException)(Function() cmd.ExecuteNonQuery())
                    Assert.Equal("42501", ex.SqlState)   ' insufficient_privilege
                End Using
            End Using
            Assert.Equal(L(10), bd.SaldoU6())
        End Using
    End Sub

    <FactPostgres>
    Public Sub Un_documento_confirmado_no_se_edita_ni_con_el_rol_de_la_aplicacion()
        Using bd = BaseDatosPrueba.Crear()
            Dim svc As New ServicioStock(bd.CadenaAplicacion)
            Dim id = svc.Contabilizar(Doc(TipoDocumentoStock.Apertura, "A-1", 10, New Date(2026, 10, 1)))
            Using cn As New NpgsqlConnection(bd.CadenaAplicacion)
                cn.Open()
                Using cmd As New NpgsqlCommand($"UPDATE documento_stock_detalle SET cantidad_base_u6 = 1 WHERE documento_id = {id}", cn)
                    Dim ex = Assert.Throws(Of PostgresException)(Function() cmd.ExecuteNonQuery())
                    Assert.Contains("DOCUMENTO_CONFIRMADO", ex.MessageText)
                End Using
            End Using
        End Using
    End Sub

End Class
