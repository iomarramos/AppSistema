Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

''' <summary>Etapa 9: contratos con vigencia y ajustes, ingreso desde contrato, gastos, resultado trazable y roles a medida.</summary>
Public Class ExtensionesTests

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>Servicio "Almuerzo - General" de la operación ORC de la empresa A con un cliente y un contrato abierto desde enero.</summary>
    Private NotInheritable Class Escenario
        Public Bd As BaseDatosPrueba
        Public Contratos As ServicioContratos
        Public Resultados As ServicioResultados
        Public Cierres As ServicioCierres
        Public Servicio, Contrato As Long

        Public Sub New(bd As BaseDatosPrueba)
            Me.Bd = bd
            Dim s = bd.Sesion("A")
            Contratos = New ServicioContratos(bd.CadenaAplicacion, s)
            Resultados = New ServicioResultados(bd.CadenaAplicacion, s)
            Cierres = New ServicioCierres(bd.CadenaAplicacion, s)
            Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, s)
            Servicio = minutas.AsignarServicio(minutas.CrearServicio("ALM", "Almuerzo"), minutas.CrearRegimen("GEN", "General"), Nothing)
            Contrato = Contratos.CrearContrato(Contratos.CrearCliente("MINA", "Minera Ejemplo", "20123456789"), "C-2026-01", New Date(2026, 1, 1), Nothing, "Pago mensual")
        End Sub
    End Class

    <FactPostgres>
    Public Sub Ajuste_a_mitad_de_mes_prorratea_el_ingreso_sin_reescribir_la_historia()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim linea = e.Contratos.AgregarServicio(e.Contrato, e.Servicio, U(31000D), New Date(2026, 1, 1), Nothing)
            e.Contratos.Ajustar(linea, U(62000D), New Date(2026, 10, 11))
            Dim lineas = e.Contratos.Lineas(e.Contrato)
            Assert.Equal(2, lineas.Count)
            Assert.Equal(New Date(2026, 10, 10), lineas(0).FechaHasta)
            Assert.Equal(U(31000D), lineas(0).ImporteMensualU6)                          ' el importe anterior se conserva
            Assert.Null(lineas(1).FechaHasta)

            Dim g = e.Contratos.GenerarIngresos(2026, 10).Single()
            Assert.Equal("generado", g.Estado)
            Assert.Equal(U(52000D), g.ImporteU6)                                         ' 10 días a 31 000 + 21 días a 62 000 (mes de 31)
            Assert.Contains("C-2026-01", g.Detalle)
            e.Contratos.GenerarIngresos(2026, 10)                                         ' repetir no duplica
            Assert.Equal(1L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM ingreso_servicio")))
            Assert.Equal(U(52000D), e.Cierres.ReporteMensual(2026, 10).Servicios.Single().IngresoU6)
            Assert.Equal(U(31000D), e.Contratos.GenerarIngresos(2026, 9).Single().ImporteU6)

            ' El importe no se edita (se ajusta) y no se superponen líneas del mismo servicio.
            Dim ex = Assert.ThrowsAny(Of Npgsql.PostgresException)(Sub() bd.EjecutarAdmin($"UPDATE contrato_servicio SET importe_mensual_u6 = 1 WHERE id = {linea}"))
            Assert.Contains("CONTRATO_INMUTABLE", ex.MessageText)
            Assert.Equal("VIGENCIA_SUPERPUESTA", Assert.Throws(Of ReglaNegocioException)(
                Function() e.Contratos.AgregarServicio(e.Contrato, e.Servicio, U(1D), New Date(2026, 12, 1), Nothing)).Codigo)
            Assert.Equal("FUERA_DE_VIGENCIA", Assert.Throws(Of ReglaNegocioException)(Function() e.Contratos.Ajustar(linea, U(1D), New Date(2026, 11, 1))).Codigo)

            ' Un ingreso registrado a mano no se pisa.
            e.Cierres.RegistrarIngreso(e.Servicio, 2026, 11, U(40000D), 0, "Liquidacion del cliente")
            Assert.Equal("manual conservado", e.Contratos.GenerarIngresos(2026, 11).Single().Estado)
            Assert.Equal(U(40000D), e.Cierres.ReporteMensual(2026, 11).Servicios.Single().IngresoU6)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Un_mes_cerrado_no_cambia_por_contratos_ni_gastos()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim linea = e.Contratos.AgregarServicio(e.Contrato, e.Servicio, U(30000D), New Date(2026, 9, 1), Nothing)
            e.Contratos.GenerarIngresos(2026, 9)
            Dim gasto = e.Resultados.RegistrarGasto(2026, 9, e.Servicio, "Planilla de cocina", "personal", U(9000D), False)
            Assert.True(e.Cierres.CerrarMes(2026, 9).Cerrado)

            Assert.Equal("PERIODO_CERRADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Contratos.Ajustar(linea, U(33000D), New Date(2026, 9, 15))).Codigo)
            Assert.Equal("PERIODO_CERRADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Contratos.GenerarIngresos(2026, 9)).Codigo)
            Assert.Equal("PERIODO_CERRADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Resultados.RegistrarGasto(2026, 9, Nothing, "x", "otros", 1, False)).Codigo)
            Assert.Equal("PERIODO_CERRADO", Assert.Throws(Of ReglaNegocioException)(Sub() e.Resultados.EliminarGasto(gasto)).Codigo)
            Dim ex = Assert.ThrowsAny(Of Npgsql.PostgresException)(Sub() bd.EjecutarAdmin($"DELETE FROM contrato_servicio WHERE id = {linea}"))
            Assert.Contains("PERIODO_CERRADO", ex.MessageText)
            ex = Assert.ThrowsAny(Of Npgsql.PostgresException)(Sub() bd.EjecutarAdmin($"UPDATE contrato_servicio SET fecha_hasta = '2026-09-20' WHERE id = {linea}"))
            Assert.Contains("PERIODO_CERRADO", ex.MessageText)
            ' Desde el mes siguiente sí se puede ajustar.
            e.Contratos.Ajustar(linea, U(33000D), New Date(2026, 10, 1))
            Assert.Equal(U(33000D), e.Contratos.GenerarIngresos(2026, 10).Single().ImporteU6)
            Assert.Equal(U(30000D), e.Resultados.ResultadoMensual(2026, 9).Lineas.First().IngresoU6)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Resultado_mensual_ingreso_menos_alimentos_y_gastos_rastreable_y_exportable()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.Contratos.AgregarServicio(e.Contrato, e.Servicio, U(10000D), New Date(2026, 1, 1), Nothing)
            e.Contratos.GenerarIngresos(2026, 10)
            Dim stock As New ServicioStock(bd.CadenaAplicacion, bd.Sesion("A"))
            stock.Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Apertura, New Date(2026, 10, 1), "AP-1",
                {New LineaDocumentoStock(bd.VarianteAceiteId, U(600D), U(8D))}))
            stock.Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.SalidaProduccion, New Date(2026, 10, 2), "SP-1",
                {New LineaDocumentoStock(bd.VarianteAceiteId, U(525D), 0)}) With {.OperacionServicioId = e.Servicio})   ' 525 L × S/8 = S/4 200
            e.Resultados.RegistrarGasto(2026, 10, e.Servicio, "Planilla cocina", "personal", U(3000D), False, "6211")
            e.Resultados.RegistrarGasto(2026, 10, e.Servicio, "Gas y limpieza", "operacion", U(800D), False)
            e.Resultados.RegistrarGasto(2026, 10, Nothing, "Supervision", "administracion", U(500D), False)
            e.Resultados.RegistrarGasto(2026, 10, e.Servicio, "Planilla presupuestada", "personal", U(2800D), True)
            Assert.Equal("DATO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(Function() e.Resultados.RegistrarGasto(2026, 10, Nothing, "x", "viajes", 1, False)).Codigo)

            Dim r = e.Resultados.ResultadoMensual(2026, 10)
            Dim alm = r.Lineas.Single(Function(l) l.Servicio = "Almuerzo - General")
            Assert.Equal(U(10000D), alm.IngresoU6)
            Assert.StartsWith("Contrato C-2026-01", alm.FuenteIngreso)
            Assert.Equal(U(4200D), alm.CostoAlimentosU6)
            Assert.Equal(U(3000D), alm.GastosPersonalU6)                                  ' lo proyectado no entra al real
            Assert.Equal(U(800D), alm.GastosOperacionU6)
            Assert.Equal(U(2000D), alm.MargenU6)
            Assert.Equal(U(20D), alm.MargenPorcentajeU6)
            Assert.Equal(U(2800D), alm.PresupuestoGastosU6)
            Dim comun = r.Lineas.Single(Function(l) l.Servicio = ServicioResultados.NoAsignado)
            Assert.Equal(U(500D), comun.OtrosGastosU6)
            Assert.Equal(U(1500D), r.Total.MargenU6)
            Assert.Equal(U(15D), r.Total.MargenPorcentajeU6)
            Assert.Equal(4, e.Resultados.ListarGastos(2026, 10).Count)

            Dim csv = e.Resultados.ExportarCsv(2026, 10).Split(vbLf, StringSplitOptions.RemoveEmptyEntries)
            Assert.Equal(4, csv.Length)                                                    ' cabecera + servicio + no asignado + total
            Assert.StartsWith("version;empresa;operacion;periodo", csv(0))
            Assert.Contains("1;A;ORC;2026-10;abierto;Almuerzo - General;10000.00;", csv(1))
            Assert.EndsWith(";1500.00;15.00;2800.00", csv(3))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Roles_a_medida_sin_escalar_permisos_ni_quedarse_sin_administrador()
        Using bd = BaseDatosPrueba.Crear()
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            Assert.Contains(admin.ListarRoles(), Function(r) r.Codigo = "FINANZAS" AndAlso r.EsBase AndAlso r.Permisos.Contains(Permisos.ResultadosVer))
            admin.GuardarRol("auditor", "Auditor de resultados", {Permisos.ResultadosVer, Permisos.ContratosVer})
            Dim rol = admin.ListarRoles().Single(Function(r) r.Codigo = "AUDITOR")
            Assert.False(rol.EsBase)
            Assert.Equal({Permisos.ContratosVer, Permisos.ResultadosVer}, rol.Permisos)
            Assert.Equal("ROL_BASE", Assert.Throws(Of ReglaNegocioException)(Sub() admin.GuardarRol("ADMIN", "x", {Permisos.ReportesVer})).Codigo)
            Assert.Equal("DATO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(Sub() admin.GuardarRol("X", "x", {"BORRAR_TODO"})).Codigo)

            Dim auditor = admin.CrearUsuario("auditor", "Auditora", "Auditor-Clave-2026", bd.A.OperacionId, "AUDITOR")
            Dim s = bd.Sesion("A", "auditor", "Auditor-Clave-2026")
            Assert.NotNull(New ServicioResultados(bd.CadenaAplicacion, s).ResultadoMensual(2026, 10))
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(
                Function() New ServicioResultados(bd.CadenaAplicacion, s).RegistrarGasto(2026, 10, Nothing, "x", "otros", 1, False)).Codigo)
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Sub() Call New ServicioAdministracion(bd.CadenaAplicacion, s).GuardarRol("Y", "y", {Permisos.UsuariosAdministrar})).Codigo)

            ' Cambiar permisos del rol propio y quitar el rol al usuario.
            admin.GuardarRol("AUDITOR", "Auditor de resultados", {Permisos.ContratosVer})
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(
                Function() New ServicioResultados(bd.CadenaAplicacion, bd.Sesion("A", "auditor", "Auditor-Clave-2026")).ResultadoMensual(2026, 10)).Codigo)
            admin.QuitarRol(auditor, bd.A.OperacionId, "AUDITOR")
            ' El único administrador no puede quitarse su rol.
            Dim adminId = Convert.ToInt64(bd.Escalar("SELECT u.id FROM usuario u JOIN empresa e ON e.id = u.empresa_id WHERE e.codigo = 'A' AND u.login = 'admin'"))
            Assert.Equal("SIN_ADMINISTRADOR", Assert.Throws(Of ReglaNegocioException)(Sub() admin.QuitarRol(adminId, bd.A.OperacionId, "ADMIN")).Codigo)

            ' Una empresa instalada antes recibe los roles base nuevos al actualizar.
            bd.EjecutarAdmin("DELETE FROM rol_permiso WHERE rol_id IN (SELECT id FROM rol WHERE codigo = 'FINANZAS'); DELETE FROM rol WHERE codigo = 'FINANZAS'")
            Call New ServicioInstalacion(bd.CadenaAdmin).SincronizarPermisos()
            Assert.Equal(2L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM rol WHERE codigo = 'FINANZAS'")))
            Assert.Equal(5L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM rol_permiso rp JOIN rol r ON r.id = rp.rol_id JOIN empresa e ON e.id = r.empresa_id WHERE r.codigo = 'FINANZAS' AND e.codigo = 'A'")))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Contratos_aislados_por_empresa_y_operacion()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim b As New ServicioContratos(bd.CadenaAplicacion, bd.Sesion("B"))
            Assert.Empty(b.ListarClientes())
            Assert.Empty(b.ListarContratos())
            Assert.Equal("OPERACION_AJENA", Assert.Throws(Of ReglaNegocioException)(Function() b.Lineas(e.Contrato)).Codigo)
            Assert.Equal("OPERACION_AJENA", Assert.Throws(Of ReglaNegocioException)(
                Function() b.AgregarServicio(e.Contrato, e.Servicio, U(1D), New Date(2026, 1, 1), Nothing)).Codigo)
            ' Un servicio de otra operación de la misma empresa no entra al contrato.
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim otraOp = admin.CrearOperacion("ARE", "Arequipa")
            Dim os2 = Convert.ToInt64(bd.Escalar(
                $"INSERT INTO operacion_servicio(empresa_id, operacion_id, servicio_id, regimen_id) SELECT empresa_id, {otraOp}, servicio_id, regimen_id FROM operacion_servicio WHERE id = {e.Servicio} RETURNING id"))
            Assert.Equal("OPERACION_AJENA", Assert.Throws(Of ReglaNegocioException)(
                Function() e.Contratos.AgregarServicio(e.Contrato, os2, U(1D), New Date(2026, 1, 1), Nothing)).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Sprint6_comparacion_con_presupuesto_por_rubro_mes_anterior_y_acumulado()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.Resultados.RegistrarGasto(2026, 9, e.Servicio, "Planilla de septiembre", "personal", U(1000D), False)
            e.Resultados.RegistrarGasto(2026, 10, e.Servicio, "Planilla de octubre", "personal", U(3000D), False)
            e.Resultados.RegistrarGasto(2026, 10, e.Servicio, "Planilla presupuestada", "personal", U(2800D), True)
            e.Resultados.RegistrarGasto(2026, 10, e.Servicio, "Gas presupuestado", "operacion", U(800D), True)
            e.Resultados.RegistrarGasto(2026, 10, Nothing, "Supervision presupuestada", "administracion", U(500D), True)

            Dim filas = e.Resultados.Comparacion(2026, 10)
            Dim personal = filas.Single(Function(f) f.Concepto = "Gastos de personal")
            Assert.Equal("2800.00", personal.PresupuestoTexto)
            Assert.Equal(U(3000D), personal.ValorRealU6)                 ' lo proyectado no entra al real
            Assert.Equal(U(1000D), personal.ValorMesAnteriorU6)
            Assert.Equal(U(4000D), personal.ValorAcumuladoU6)            ' enero a octubre
            Assert.Equal("800.00", filas.Single(Function(f) f.Concepto = "Gastos de operacion").PresupuestoTexto)
            Assert.Equal("500.00", filas.Single(Function(f) f.Concepto.StartsWith("Otros gastos")).PresupuestoTexto)
            Assert.Equal("4100.00", filas.Single(Function(f) f.Concepto = "Total gastos").PresupuestoTexto)
            Assert.Equal("-", filas.Single(Function(f) f.Concepto = "Ingreso").PresupuestoTexto)
            Assert.Equal("-", filas.Single(Function(f) f.Concepto = "Margen").PresupuestoTexto)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Sprint5_calendario_por_semanas_muestra_cada_dia_del_mes_una_vez_en_su_dia()
        Using bd = BaseDatosPrueba.Crear()
            Dim semanas = New ServicioCierres(bd.CadenaAplicacion, bd.Sesion("A")).CalendarioSemanal(2026, 10)
            Dim celdas = semanas.SelectMany(Function(s) {s.Lunes, s.Martes, s.Miercoles, s.Jueves, s.Viernes, s.Sabado, s.Domingo}).Where(Function(t) t <> "").ToList()
            Assert.Equal(31, celdas.Count)
            ' 1 de octubre de 2026 cae en la primera semana, en su día (lunes = 0).
            Dim primera = semanas.First()
            Dim porDia As String() = {primera.Lunes, primera.Martes, primera.Miercoles, primera.Jueves, primera.Viernes, primera.Sabado, primera.Domingo}
            Dim indice = (CInt(New Date(2026, 10, 1).DayOfWeek) + 6) Mod 7
            Assert.StartsWith("1 ", porDia(indice))
            For k = 0 To indice - 1
                Assert.Equal("", porDia(k))                                   ' antes del 1, casillas vacías
            Next
        End Using
    End Sub

End Class
