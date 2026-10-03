Imports System.Threading.Tasks
Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

''' <summary>Etapa 7: pendientes, cierre diario serializado, ingreso, Food Cost y cierre de mes repetible.</summary>
Public Class CierresTests

    Private Shared ReadOnly Fecha As New Date(2026, 10, 2)

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>600 botellas de aceite de 1 L a S/8; minuta aprobada de 5250 raciones × 0,1 L = 525 L (S/4 200).</summary>
    Private NotInheritable Class Escenario
        Public Bd As BaseDatosPrueba
        Public Cierres As ServicioCierres
        Public Produccion As ServicioProduccion
        Public Minuta, OperacionServicio, Botella As Long

        Public Sub New(bd As BaseDatosPrueba)
            Me.Bd = bd
            Dim s = bd.Sesion("A")
            Cierres = New ServicioCierres(bd.CadenaAplicacion, s)
            Produccion = New ServicioProduccion(bd.CadenaAplicacion, s)
            Botella = New ServicioCatalogo(bd.CadenaAplicacion, s).CrearVariante(bd.ProductoAceiteId, Nothing, "ACE-1L", "Aceite botella 1 L", "botella", U(1D))
            Call New ServicioStock(bd.CadenaAplicacion, s).Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Apertura, New Date(2026, 10, 1), "AP-1",
                {New LineaDocumentoStock(Botella, U(600D), U(8D))}))
            Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, s)
            Dim v = recetas.CrearReceta("SOPA", "Sopa", Nothing, U(1D), Nothing)
            recetas.AgregarIngrediente(v, bd.ProductoAceiteId, U(0.1D), Nothing, 1)
            recetas.Aprobar(v)
            Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, s)
            Dim servicio = minutas.CrearServicio("ALM", "Almuerzo")
            Dim estructura = minutas.CrearEstructura(servicio, "SOPA", "Sopa", 1)
            OperacionServicio = minutas.AsignarServicio(servicio, minutas.CrearRegimen("GEN", "General"), Nothing)
            Minuta = minutas.CrearMinuta(OperacionServicio, Fecha, 5250)
            minutas.AgregarPlato(Minuta, estructura, v, 5250)
            minutas.Aprobar(Minuta, "PEN")
        End Sub

        ''' <summary>Requerimiento, entrega (525 L = S/4 200) y producción registrada.</summary>
        Public Sub OperarElDia()
            Produccion.Atender(Produccion.CalcularRequerimiento(Minuta, Bd.A.AlmacenId), Fecha)
            Produccion.RegistrarProduccion(Minuta, 5250, 5200, 50, Nothing)
        End Sub

        Public Function Salida(fecha As Date) As Long
            Return New ServicioAlmacen(Bd.CadenaAplicacion, Bd.Sesion("A")).SalidaProduccion(Bd.A.AlmacenId, fecha,
                {New LineaSalida With {.VarianteId = Botella, .CantidadBaseU6 = U(1D)}})
        End Function
    End Class

    <FactPostgres>
    Public Sub T37_T39_no_cierra_con_pendientes_y_el_dia_cerrado_rechaza_contabilizar()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim r = e.Cierres.CerrarDia(Fecha)
            Assert.False(r.Cerrado)
            Assert.Contains(r.Pendientes, Function(p) p.Codigo = "PRODUCCION_SIN_REGISTRAR" AndAlso p.Bloqueante)
            Assert.True(Convert.ToInt64(bd.Escalar("SELECT count(*) FROM cierre_validacion WHERE resultado = 'error'")) >= 1)

            Dim req = e.Produccion.CalcularRequerimiento(e.Minuta, bd.A.AlmacenId)
            Assert.Contains(e.Cierres.Pendientes(Fecha), Function(p) p.Codigo = "REQUERIMIENTO_PENDIENTE")
            e.Produccion.Atender(req, Fecha)
            e.Produccion.RegistrarProduccion(e.Minuta, 5250, 5200, 50, Nothing)
            Assert.Empty(e.Cierres.Pendientes(Fecha).Where(Function(p) p.Bloqueante))
            Assert.True(e.Cierres.CerrarDia(Fecha).Cerrado)
            Assert.True(e.Cierres.DiaCerrado(Fecha))

            Assert.Equal("DIA_CERRADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Salida(Fecha)).Codigo)          ' T39
            Assert.Equal("PERIODO_CERRADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Cierres.CerrarDia(Fecha)).Codigo)
            Dim ex = Assert.ThrowsAny(Of Npgsql.PostgresException)(Sub() bd.EjecutarAdmin("UPDATE cierre_diario SET estado = 'abierto'"))
            Assert.Contains("PERIODO_CERRADO", ex.MessageText)
            e.Salida(Fecha.AddDays(1))                                                                                       ' el día siguiente sigue abierto
        End Using
    End Sub

    <FactPostgres>
    Public Sub T38_salidas_simultaneas_con_el_cierre_quedan_incluidas_antes_o_rechazadas_despues()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim dia = Fecha.AddDays(5)                                    ' día sin minuta: sin pendientes bloqueantes
            Dim tareas As New List(Of Task(Of String))
            For i = 1 To 8
                tareas.Add(Task.Run(Function()
                                        Try
                                            e.Salida(dia)
                                            Return "ok"
                                        Catch ex As ReglaNegocioException
                                            Return ex.Codigo
                                        End Try
                                    End Function))
                If i = 4 Then tareas.Add(Task.Run(Function() If(New ServicioCierres(bd.CadenaAplicacion, bd.Sesion("A")).CerrarDia(dia).Cerrado, "cierre", "no_cerro")))
            Next
            Task.WaitAll(tareas.ToArray())
            Dim resultados = tareas.Select(Function(t) t.Result).ToList()
            Assert.Contains("cierre", resultados)
            Assert.All(resultados, Sub(x) Assert.Contains(x, {"ok", "DIA_CERRADO", "cierre"}))
            ' Ninguna salida quedó registrada después del cierre.
            Assert.Equal(0L, Convert.ToInt64(bd.Escalar(
                $"SELECT count(*) FROM movimiento_stock m JOIN cierre_diario c ON c.fecha = m.fecha WHERE m.fecha = '{dia:yyyy-MM-dd}' AND m.creado_en > c.fecha_cierre")))
            Assert.Equal(CLng(resultados.Where(Function(x) x = "ok").Count()), Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM movimiento_stock WHERE fecha = '{dia:yyyy-MM-dd}'")))
            Assert.Equal(0L, bd.FilasSinConciliar())
        End Using
    End Sub

    <FactPostgres>
    Public Sub T40_T41_T48_food_cost_42_por_ciento_y_mes_cerrado_repetible()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            e.OperarElDia()
            e.Cierres.RegistrarIngreso(e.OperacionServicio, 2026, 10, U(10000D), 0, "Factura mensual al cliente")
            e.Cierres.FijarObjetivo(e.OperacionServicio, 4000)
            ' Un segundo servicio sin ingreso: Food Cost no calculable (T41).
            Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, bd.Sesion("A"))
            minutas.AsignarServicio(minutas.CrearServicio("CEN", "Cena"), minutas.ListarRegimenes().Single().Id, Nothing)

            Dim r = e.Cierres.ReporteMensual(2026, 10)
            Dim alm = r.Servicios.Single(Function(s) s.Servicio = "Almuerzo")
            Assert.Equal(U(4200D), alm.CostoAlimentosU6)
            Assert.Equal(U(42D), alm.FoodCostU6)
            Assert.Equal(U(2D), alm.DesviacionPuntosU6)
            Assert.Equal(U(200D), alm.DiferenciaPresupuestoU6)
            Assert.Equal(5200L, alm.RacionesServidas)
            Dim cena = r.Servicios.Single(Function(s) s.Servicio = "Cena")
            Assert.Null(cena.FoodCostU6)
            Assert.Contains("no calculable", cena.Observacion)

            Dim mes = e.Cierres.CerrarMes(2026, 10)
            Assert.False(mes.Cerrado)
            Assert.Contains(mes.Pendientes, Function(p) p.Codigo = "DIA_ABIERTO")   ' 01/10 (apertura) y 02/10
            Assert.True(e.Cierres.CerrarDia(New Date(2026, 10, 1)).Cerrado)
            Assert.True(e.Cierres.CerrarDia(Fecha).Cerrado)
            Assert.True(e.Cierres.CerrarMes(2026, 10).Cerrado)

            Dim antes = Newtonsoft(e.Cierres.ReporteMensual(2026, 10))
            Assert.Equal("PERIODO_CERRADO", Assert.Throws(Of ReglaNegocioException)(Sub() e.Cierres.RegistrarIngreso(e.OperacionServicio, 2026, 10, U(1D), 0, "x")).Codigo)
            Assert.ThrowsAny(Of Npgsql.PostgresException)(Sub() bd.EjecutarAdmin("UPDATE ingreso_servicio SET importe_neto_u6 = 1"))
            Assert.Equal("DIA_CERRADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Salida(New Date(2026, 10, 3))).Codigo.Replace("PERIODO_CERRADO", "DIA_CERRADO"))
            Assert.Equal(antes, Newtonsoft(e.Cierres.ReporteMensual(2026, 10)))   ' T48
            Assert.Equal("cerrado", e.Cierres.ReporteMensual(2026, 10).Estado)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Almacen_no_cierra_ni_registra_ingresos()
        Using bd = BaseDatosPrueba.Crear()
            Call New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A")).CrearUsuario("almacen", "Almacenero", "Almacen-Clave-2026", bd.A.OperacionId, "ALMACEN")
            Dim c As New ServicioCierres(bd.CadenaAplicacion, bd.Sesion("A", "almacen", "Almacen-Clave-2026"))
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() c.CerrarDia(Fecha)).Codigo)
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() c.Pendientes(Fecha)).Codigo)
        End Using
    End Sub

    ''' <summary>Serialización simple y estable del reporte para comparar dos ejecuciones.</summary>
    Private Shared Function Newtonsoft(r As ReporteMensualDto) As String
        Return String.Join("|", {r.Estado, r.BajasU6.ToString(), r.AjusteInventarioU6.ToString(), r.TotalCostoAlimentosU6.ToString(), r.TotalIngresoU6.ToString(), r.FoodCostTotalU6.ToString()}.Concat(
            r.Servicios.Select(Function(s) $"{s.Servicio};{s.RacionesServidas};{s.CostoAlimentosU6};{s.IngresoU6};{s.FoodCostU6};{s.DesviacionPuntosU6};{s.DiferenciaPresupuestoU6}")))
    End Function

End Class
