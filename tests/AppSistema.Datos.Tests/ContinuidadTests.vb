Imports System.IO
Imports Npgsql
Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

''' <summary>Etapa 8: cola de salida de la sede, recepción idempotente y ordenada en la central, respaldo y restauración.</summary>
Public Class ContinuidadTests

    Private Shared ReadOnly Fecha As New Date(2026, 10, 2)

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>Sede ORC (empresa A) registrada en una central; apertura 27 L, salida 2 L y devolución 1 L de esa salida.</summary>
    Private NotInheritable Class Escenario
        Implements IDisposable
        Public Sede, Central As BaseDatosPrueba
        Public Continuidad, ContinuidadCentral As ServicioContinuidad
        Public Credencial As String
        Public Salida As Long

        Public Sub New(Optional conDocumentos As Boolean = True)
            Sede = BaseDatosPrueba.Crear()
            Central = BaseDatosPrueba.Crear()
            Continuidad = New ServicioContinuidad(Sede.CadenaAdmin)
            ContinuidadCentral = New ServicioContinuidad(Central.CadenaAdmin)
            Credencial = ContinuidadCentral.RegistrarSede("A", "ORC", "Orcopampa")
            Assert.Equal(0L, Continuidad.ConfigurarOrigen("A", "ORC"))
            If conDocumentos Then
                Dim s = Sede.Sesion("A")
                Call New ServicioStock(Sede.CadenaAplicacion, s).Contabilizar(New DocumentoStockNuevo(Sede.A.AlmacenId, TipoDocumentoStock.Apertura, Fecha, "AP-1",
                    {New LineaDocumentoStock(Sede.VarianteAceiteId, U(27D), U(8D))}))
                Dim almacen As New ServicioAlmacen(Sede.CadenaAplicacion, s)
                Salida = almacen.SalidaProduccion(Sede.A.AlmacenId, Fecha, {New LineaSalida With {.VarianteId = Sede.VarianteAceiteId, .CantidadBaseU6 = U(2D)}})
                almacen.DevolucionProduccion(Salida, Fecha, {New LineaSalida With {.VarianteId = Sede.VarianteAceiteId, .CantidadBaseU6 = U(1D)}})
            End If
        End Sub

        Public Function Enviar(Optional ignorarEspera As Boolean = True) As ResultadoEnvio
            Return Continuidad.Enviar("A", Central.CadenaSincronizacion, Credencial, ignorarEspera:=ignorarEspera)
        End Function

        Public Function EnCentral(sql As String) As Long
            Return Convert.ToInt64(Central.Escalar(sql))
        End Function

        ''' <summary>Stock que la central conoce de la sede, por variante (cantidad|valor).</summary>
        Public Function SaldoCentral() As String
            Return CStr(Central.Escalar("SELECT string_agg(variante_codigo || '=' || cantidad_u6 || '|' || valor_u6, ',' ORDER BY variante_codigo) FROM v_central_saldo WHERE cantidad_u6 <> 0 OR valor_u6 <> 0"))
        End Function

        Public Function SaldoSede() As String
            Return CStr(Sede.Escalar("SELECT string_agg(v.codigo || '=' || s.cantidad_base_u6 || '|' || s.valor_u6, ',' ORDER BY v.codigo) FROM saldo_stock s " &
                                     "JOIN variante_producto v ON v.id = s.variante_id WHERE s.cantidad_base_u6 <> 0 OR s.valor_u6 <> 0"))
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            Sede.Dispose()
            Central.Dispose()
        End Sub
    End Class

    Private NotInheritable Class Evento
        Public Uuid, Tipo, Referencia, DependeDe, Payload As String
        Public Secuencia As Long
        Public Version As Integer
    End Class

    Private Shared Function LeerCola(bd As BaseDatosPrueba) As List(Of Evento)
        Dim r As New List(Of Evento)
        Using cn As New NpgsqlConnection(bd.CadenaAdmin)
            cn.Open()
            Using cmd As New NpgsqlCommand("SELECT uuid, tipo, referencia, depende_de, payload_json, secuencia, version_payload FROM sincronizacion_evento ORDER BY secuencia", cn),
                  rd = cmd.ExecuteReader()
                While rd.Read()
                    r.Add(New Evento With {.Uuid = rd.GetString(0), .Tipo = rd.GetString(1), .Referencia = If(rd.IsDBNull(2), Nothing, rd.GetString(2)),
                                           .DependeDe = If(rd.IsDBNull(3), Nothing, rd.GetString(3)), .Payload = rd.GetString(4),
                                           .Secuencia = rd.GetInt64(5), .Version = rd.GetInt32(6)})
                End While
            End Using
        End Using
        Return r
    End Function

    ''' <summary>Entrega directa a la central, como lo hace el agente (rol app_sincronizacion).</summary>
    Private Shared Function Recibir(central As BaseDatosPrueba, sede As String, credencial As String, e As Evento,
                                    Optional payload As String = Nothing, Optional version As Integer? = Nothing) As String
        Using cn As New NpgsqlConnection(central.CadenaSincronizacion)
            cn.Open()
            Using cmd As New NpgsqlCommand("SELECT fn_recibir_evento('A', @s, @c, @u, @q, @t, @v, @r, @d, @p)", cn)
                cmd.Parameters.AddWithValue("s", sede)
                cmd.Parameters.AddWithValue("c", credencial)
                cmd.Parameters.AddWithValue("u", e.Uuid)
                cmd.Parameters.AddWithValue("q", e.Secuencia)
                cmd.Parameters.AddWithValue("t", e.Tipo)
                cmd.Parameters.AddWithValue("v", If(version, e.Version))
                cmd.Parameters.AddWithValue("r", If(CObj(e.Referencia), DBNull.Value))
                cmd.Parameters.AddWithValue("d", If(CObj(e.DependeDe), DBNull.Value))
                cmd.Parameters.AddWithValue("p", If(payload, e.Payload))
                Return CStr(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    <FactPostgres>
    Public Sub T42_reenvio_tras_perder_el_acuse_no_duplica_y_la_central_concilia_con_la_sede()
        Using e As New Escenario()
            Dim cola = LeerCola(e.Sede)
            Assert.Equal({1L, 2L, 3L}, cola.Select(Function(x) x.Secuencia))
            Assert.Equal(cola(1).Referencia, cola(2).DependeDe)                       ' la devolución depende de su salida

            Dim r = e.Enviar()
            Assert.Equal(3, r.Aplicados)
            Assert.Equal(0L, r.Pendientes)
            Assert.Null(r.Problema)
            Assert.Equal(e.SaldoSede(), e.SaldoCentral())                              ' 26 L, S/208
            Assert.Equal($"ACE-A-4L={U(26D)}|{U(208D)}", e.SaldoCentral())

            ' Acuse perdido: la sede no supo que se entregó y reenvía todo.
            e.Sede.EjecutarAdmin("UPDATE sincronizacion_evento SET estado = 'pendiente', enviado_en = NULL")
            r = e.Enviar()
            Assert.Equal(3, r.Duplicados)
            Assert.Equal(0, r.Aplicados)
            Assert.Equal(3L, e.EnCentral("SELECT count(*) FROM central_documento"))
            Assert.Equal(3L, e.EnCentral("SELECT count(*) FROM central_movimiento"))
            Assert.Equal(e.SaldoSede(), e.SaldoCentral())

            Dim resumen = e.ContinuidadCentral.ResumenCentral("A").Single()
            Assert.Equal("ORC", resumen.Sede)
            Assert.NotNull(resumen.UltimaSincronizacion)
            Assert.Equal(3L, resumen.UltimaSecuenciaAplicada)
            Assert.Equal(U(208D), resumen.ValorStockU6)
            Assert.Equal(0L, e.Continuidad.EstadoCola("A").Pendientes)
        End Using
    End Sub

    <FactPostgres>
    Public Sub T43_evento_que_llega_antes_que_su_antecesor_se_retiene_y_se_aplica_despues()
        Using e As New Escenario()
            Dim cola = LeerCola(e.Sede)
            Assert.Equal("RETENIDO", Recibir(e.Central, "ORC", e.Credencial, cola(2)))   ' devolución antes que la salida
            Assert.Equal(0L, e.EnCentral("SELECT count(*) FROM central_documento"))
            Assert.Equal("RETENIDO", Recibir(e.Central, "ORC", e.Credencial, cola(1)))
            Assert.Equal(0L, e.EnCentral("SELECT count(*) FROM central_documento"))        ' falta la apertura
            Assert.Equal("APLICADO", Recibir(e.Central, "ORC", e.Credencial, cola(0)))
            Assert.Equal(3L, e.EnCentral("SELECT count(*) FROM central_documento"))        ' se aplicó la cadena completa, en orden
            Assert.Equal(e.SaldoSede(), e.SaldoCentral())

            ' Un contenido de versión desconocida no se pierde ni se aplica mal: queda retenido con su motivo.
            Dim futuro As New Evento With {.Uuid = Guid.NewGuid().ToString(), .Tipo = "documento_stock", .Secuencia = 4, .Version = 2, .Payload = "{""nuevo"": true}"}
            Assert.Equal("RETENIDO", Recibir(e.Central, "ORC", e.Credencial, futuro))
            Dim resumen = e.ContinuidadCentral.ResumenCentral("A").Single()
            Assert.Equal(1L, resumen.Retenidos)
            Assert.StartsWith("VERSION_NO_SOPORTADA", resumen.MotivoRetencion)
        End Using
    End Sub

    <FactPostgres>
    Public Sub T44_T22_sede_no_autorizada_clave_repetida_y_ninguna_sede_sobrescribe_a_otra()
        Using e As New Escenario()
            ' Credencial inválida: rechazo registrado sin contenido; nada entra.
            Dim r = e.Continuidad.Enviar("A", e.Central.CadenaSincronizacion, "credencial-falsa", ignorarEspera:=True)
            Assert.NotNull(r.Problema)
            Assert.Equal(3L, r.Pendientes)
            Assert.Equal(0L, e.EnCentral("SELECT count(*) FROM evento_recibido"))
            Assert.Equal(1L, e.EnCentral("SELECT count(*) FROM rechazo_sincronizacion WHERE motivo = 'SEDE_NO_AUTORIZADA' AND sede_codigo = 'ORC'"))
            Assert.Equal(0L, e.EnCentral("SELECT count(*) FROM information_schema.columns WHERE table_name = 'rechazo_sincronizacion' AND column_name LIKE '%payload%'"))
            Assert.Equal("RECHAZADO", Recibir(e.Central, "NOEXISTE", e.Credencial, LeerCola(e.Sede)(0)))

            ' El usuario de sincronización no puede leer ni escribir tablas de la central.
            Using cn As New NpgsqlConnection(e.Central.CadenaSincronizacion)
                cn.Open()
                Using cmd As New NpgsqlCommand("SELECT count(*) FROM central_documento", cn)
                    Assert.Equal("42501", Assert.Throws(Of PostgresException)(Function() cmd.ExecuteScalar()).SqlState)
                End Using
            End Using

            Assert.Equal(3, e.Enviar().Aplicados)
            Dim cola = LeerCola(e.Sede)
            ' T22: misma clave, otro contenido → conflicto, sin movimiento.
            Assert.Equal("CONFLICTO", Recibir(e.Central, "ORC", e.Credencial, cola(1), payload:=cola(1).Payload.Replace("2000000", "9000000")))
            ' Otra sede que reenvía un documento de ORC (o su clave) no escribe sobre ORC.
            Dim credencialOtra = e.ContinuidadCentral.RegistrarSede("A", "OTRA", "Otra sede")
            Assert.Equal("CONFLICTO", Recibir(e.Central, "OTRA", credencialOtra, cola(0)))
            Dim copia As New Evento With {.Uuid = Guid.NewGuid().ToString(), .Tipo = cola(0).Tipo, .Referencia = cola(0).Referencia, .Secuencia = 1, .Version = 1, .Payload = cola(0).Payload}
            Assert.Equal("CONFLICTO", Recibir(e.Central, "OTRA", credencialOtra, copia))
            Assert.Equal(3L, e.EnCentral("SELECT count(*) FROM central_documento"))
            Assert.Equal(3L, e.EnCentral("SELECT count(*) FROM central_documento d JOIN sede_central s ON s.id = d.sede_id WHERE s.codigo = 'ORC'"))
            Assert.Equal(e.SaldoSede(), e.SaldoCentral())
            Dim ex = Assert.ThrowsAny(Of PostgresException)(Sub() e.Central.EjecutarAdmin("UPDATE central_movimiento SET valor_u6 = 0"))
            Assert.Contains("CONSOLIDADO_INMUTABLE", ex.MessageText)

            ' Sede desactivada: rechazada.
            e.ContinuidadCentral.DesactivarSede("A", "ORC")
            Assert.Equal("RECHAZADO", Recibir(e.Central, "ORC", e.Credencial, cola(0)))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Sin_conexion_la_sede_sigue_operando_reintenta_en_orden_y_no_deja_media_transaccion()
        Using e As New Escenario()
            Dim sinRed = "Host=127.0.0.1;Port=1;Username=x;Password=x;Database=x;Timeout=2;Pooling=false"
            Dim r = e.Continuidad.Enviar("A", sinRed, e.Credencial)
            Assert.Contains("Sin conexion", r.Problema)
            Assert.Equal(3L, r.Pendientes)
            Dim estado = e.Continuidad.EstadoCola("A")
            Assert.Equal(1L, estado.ConError)
            Assert.StartsWith("SIN_CONEXION", estado.UltimoError)

            ' La sede sigue trabajando: otra salida se confirma y se anota en la cola.
            Dim almacen As New ServicioAlmacen(e.Sede.CadenaAplicacion, e.Sede.Sesion("A"))
            almacen.SalidaProduccion(e.Sede.A.AlmacenId, Fecha, {New LineaSalida With {.VarianteId = e.Sede.VarianteAceiteId, .CantidadBaseU6 = U(4D)}})
            ' Una transacción que falla no deja documento ni evento.
            Dim antes = Convert.ToInt64(e.Sede.Escalar("SELECT count(*) FROM sincronizacion_evento"))
            Assert.Equal("STOCK_INSUFICIENTE", Assert.Throws(Of ReglaNegocioException)(
                Function() almacen.SalidaProduccion(e.Sede.A.AlmacenId, Fecha, {New LineaSalida With {.VarianteId = e.Sede.VarianteAceiteId, .CantidadBaseU6 = U(999D)}})).Codigo)
            Assert.Equal(antes, Convert.ToInt64(e.Sede.Escalar("SELECT count(*) FROM sincronizacion_evento")))
            Assert.Equal(4L, antes)

            ' Respeta la espera del reintento y no adelanta eventos posteriores.
            r = e.Enviar(ignorarEspera:=False)
            Assert.Equal(0, r.Enviados)
            Assert.Equal(0L, e.EnCentral("SELECT count(*) FROM evento_recibido"))
            ' Vuelve la conexión.
            r = e.Enviar()
            Assert.Equal(4, r.Aplicados)
            Assert.Equal(e.SaldoSede(), e.SaldoCentral())

            ' El cierre del día también viaja.
            Call New ServicioCierres(e.Sede.CadenaAplicacion, e.Sede.Sesion("A")).CerrarDia(Fecha)
            Assert.Equal(1, e.Enviar().Aplicados)
            Assert.Equal(1L, e.EnCentral($"SELECT count(*) FROM central_cierre WHERE tipo = 'cierre_diario' AND periodo = '{Fecha:yyyy-MM-dd}'"))
        End Using
    End Sub

    <FactPostgres>
    Public Sub T45_restaurar_un_respaldo_da_los_mismos_recuentos_saldos_y_referencias()
        Using e As New Escenario()
            e.Enviar()
            Dim archivo = Path.Combine(Path.GetTempPath(), "appsistema_" & Guid.NewGuid().ToString("N") & ".dump")
            Try
                Dim foto = e.Continuidad.Respaldar(archivo)
                Assert.True(File.Exists(archivo & ".conciliacion"))
                Assert.Equal(CStr(U(208D)), foto("saldo.valor_u6"))
                Assert.Equal("0", foto("conciliacion.filas_sin_conciliar"))
                Using vacia = BaseDatosPrueba.CrearVacia()
                    Dim restaurada As New ServicioContinuidad(vacia.CadenaAdmin)
                    Assert.Empty(restaurada.Restaurar(archivo))
                    Assert.Equal(foto, restaurada.Instantanea())
                    Assert.Equal(e.SaldoSede(), CStr(vacia.Escalar("SELECT string_agg(v.codigo || '=' || s.cantidad_base_u6 || '|' || s.valor_u6, ',' ORDER BY v.codigo) FROM saldo_stock s " &
                                                                  "JOIN variante_producto v ON v.id = s.variante_id WHERE s.cantidad_base_u6 <> 0 OR s.valor_u6 <> 0")))
                    ' Las reglas siguen vigentes en la copia.
                    Dim ex = Assert.ThrowsAny(Of PostgresException)(Sub() vacia.EjecutarAdmin("DELETE FROM movimiento_stock"))
                    Assert.Equal("BASE_NO_VACIA", Assert.Throws(Of ReglaNegocioException)(Function() restaurada.Restaurar(archivo)).Codigo)
                End Using
                ' Una conciliación alterada se detecta.
                File.WriteAllText(archivo & ".conciliacion", File.ReadAllText(archivo & ".conciliacion").Replace("saldo.valor_u6=" & U(208D), "saldo.valor_u6=1"))
                Using vacia = BaseDatosPrueba.CrearVacia()
                    Assert.Contains(New ServicioContinuidad(vacia.CadenaAdmin).Restaurar(archivo), Function(d) d.StartsWith("saldo.valor_u6"))
                End Using
            Finally
                File.Delete(archivo)
                File.Delete(archivo & ".conciliacion")
            End Try
        End Using
    End Sub

    <FactPostgres>
    Public Sub T46_migrar_una_base_con_documentos_historicos_conserva_historia_y_conciliacion()
        Using sede = BaseDatosPrueba.Crear("V011"), central = BaseDatosPrueba.Crear()
            Dim s = sede.Sesion("A")
            Call New ServicioStock(sede.CadenaAplicacion, s).Contabilizar(New DocumentoStockNuevo(sede.A.AlmacenId, TipoDocumentoStock.Apertura, Fecha, "AP-1",
                {New LineaDocumentoStock(sede.VarianteAceiteId, U(27D), U(8D))}))
            Call New ServicioAlmacen(sede.CadenaAplicacion, s).SalidaProduccion(sede.A.AlmacenId, Fecha, {New LineaSalida With {.VarianteId = sede.VarianteAceiteId, .CantidadBaseU6 = U(3D)}})
            Dim antes = New ServicioContinuidad(sede.CadenaAdmin).Instantanea()
            Assert.Equal("V001,V002,V003,V004,V005,V006,V007,V008,V009,V010,V011", antes("migraciones"))

            ' Actualización segura: respaldo + migraciones + conciliación de saldos e historia.
            Dim archivo = Path.Combine(Path.GetTempPath(), "appsistema_" & Guid.NewGuid().ToString("N") & ".dump")
            Try
                Dim act = New ServicioContinuidad(sede.CadenaAdmin).Actualizar(archivo)
                Assert.Empty(act.Diferencias)
                Assert.Contains(act.Migraciones, Function(m) m.Version = "V012" AndAlso m.Aplicada)
                Assert.True(File.Exists(archivo) AndAlso File.Exists(archivo & ".conciliacion"))
            Finally
                File.Delete(archivo)
                File.Delete(archivo & ".conciliacion")
            End Try
            Dim despues = New ServicioContinuidad(sede.CadenaAdmin).Instantanea()
            For Each clave In {"filas.documento_stock", "filas.documento_stock_detalle", "filas.movimiento_stock", "filas.saldo_stock",
                               "saldo.cantidad_u6", "saldo.valor_u6", "libro.cantidad_u6", "libro.valor_u6", "libro.ultimo_id", "conciliacion.filas_sin_conciliar"}
                Assert.Equal(antes(clave), despues(clave))
            Next
            Assert.Equal(2L, Convert.ToInt64(sede.Escalar("SELECT count(DISTINCT uuid) FROM documento_stock")))

            ' La historia entra a la cola en orden y llega completa a la central.
            Dim credencial = New ServicioContinuidad(central.CadenaAdmin).RegistrarSede("A", "ORC", "Orcopampa")
            Assert.Equal(2L, New ServicioContinuidad(sede.CadenaAdmin).ConfigurarOrigen("A", "ORC"))
            Assert.Equal(0L, New ServicioContinuidad(sede.CadenaAdmin).ConfigurarOrigen("A", "ORC"))     ' repetir no duplica
            Assert.Equal("ORIGEN_CONFIGURADO", Assert.Throws(Of ReglaNegocioException)(Function() New ServicioContinuidad(sede.CadenaAdmin).ConfigurarOrigen("A", "OTRA")).Codigo)
            Assert.Equal({"apertura", "salida_produccion"}, LeerCola(sede).Select(Function(x) CStr(Newtonsoft(x.Payload, "tipo"))))
            Dim r = New ServicioContinuidad(sede.CadenaAdmin).Enviar("A", central.CadenaSincronizacion, credencial)
            Assert.Equal(2, r.Aplicados)
            Assert.Equal($"ACE-A-4L={U(24D)}|{U(192D)}", CStr(central.Escalar("SELECT string_agg(variante_codigo || '=' || cantidad_u6 || '|' || valor_u6, ',') FROM v_central_saldo")))
        End Using
    End Sub

    Private Shared Function Newtonsoft(json As String, campo As String) As String
        Using d = Text.Json.JsonDocument.Parse(json)
            Return d.RootElement.GetProperty(campo).GetString()
        End Using
    End Function

End Class
