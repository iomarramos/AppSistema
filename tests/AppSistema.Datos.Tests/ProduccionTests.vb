Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

''' <summary>Etapa 5: requerimiento, entrega en presentaciones completas (D12), adicional, devolución, producción, mermas y costo real.</summary>
Public Class ProduccionDatosTests

    Private Shared ReadOnly Fecha As New Date(2026, 10, 2)

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>
    ''' Aceite en bidón de 4 L (8 L en stock) y en botella de 1 L (10 L en stock), todo a S/8 por L.
    ''' Minuta aprobada de 50 raciones de una receta con 0,1 L por ración (5 L).
    ''' </summary>
    Private NotInheritable Class Escenario
        Public Bd As BaseDatosPrueba
        Public Produccion As ServicioProduccion
        Public Almacen As ServicioAlmacen
        Public Minuta, Botella, VersionReceta As Long

        Public Sub New(bd As BaseDatosPrueba)
            Me.Bd = bd
            Dim s = bd.Sesion("A")
            Produccion = New ServicioProduccion(bd.CadenaAplicacion, s)
            Almacen = New ServicioAlmacen(bd.CadenaAplicacion, s)
            Botella = New ServicioCatalogo(bd.CadenaAplicacion, s).CrearVariante(bd.ProductoAceiteId, Nothing, "ACE-1L", "Aceite botella 1 L", "botella", U(1D))
            Call New ServicioStock(bd.CadenaAplicacion, s).Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Apertura, Fecha, "AP-1",
                {New LineaDocumentoStock(bd.VarianteAceiteId, U(8D), U(8D)), New LineaDocumentoStock(Botella, U(10D), U(8D))}))
            Dim prov As New ServicioProveedores(bd.CadenaAplicacion, s)
            prov.RegistrarPrecio(prov.VincularEmpaque(prov.CrearProveedor(New ProveedorDto With {.Codigo = "P1", .Nombre = "Mayorista"}), bd.EmpaqueCajaId, 1),
                                 New Date(2026, 1, 1), Nothing, "PEN", U(128D), False)

            Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, s)
            VersionReceta = recetas.CrearReceta("SOPA", "Sopa", Nothing, U(1D), Nothing)
            recetas.AgregarIngrediente(VersionReceta, bd.ProductoAceiteId, U(0.1D), Nothing, 1)
            recetas.Aprobar(VersionReceta)
            Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, s)
            Dim servicio = minutas.CrearServicio("ALM", "Almuerzo")
            Dim estructura = minutas.CrearEstructura(servicio, "SOPA", "Sopa", 1)
            Minuta = minutas.CrearMinuta(minutas.AsignarServicio(servicio, minutas.CrearRegimen("GEN", "General"), Nothing), Fecha, 50)
            minutas.AgregarPlato(Minuta, estructura, VersionReceta, 50)
            minutas.Aprobar(Minuta, "PEN")
        End Sub
    End Class

    <FactPostgres>
    Public Sub T31_entrega_5_mas_adicional_1_menos_devolucion_0_5_da_neto_5_5_y_costo_real_44()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim req = e.Produccion.CalcularRequerimiento(e.Minuta, bd.A.AlmacenId)
            Assert.Equal(U(5D), e.Produccion.ListarLineas(req).Single().PrevistoU6)
            Dim entrega = e.Produccion.Atender(req, Fecha)
            Dim l = entrega.Lineas.Single()
            Assert.Equal("5 x Aceite botella 1 L", l.Presentaciones)   ' 5 botellas: menos excedente que 2 bidones de 4 L
            Assert.Equal(U(5D), l.EntregadoU6)
            Assert.Equal(U(40D), entrega.ValorU6)

            Dim adicional = e.Produccion.RequerimientoAdicional(e.Minuta, bd.A.AlmacenId, "Motivo de prueba")
            e.Produccion.Solicitar(adicional, bd.ProductoAceiteId, U(1D))
            e.Produccion.AprobarAdicional(adicional)
            e.Produccion.Atender(adicional, Fecha)
            e.Almacen.DevolucionProduccion(entrega.DocumentoId.Value, Fecha, {New LineaSalida With {.VarianteId = e.Botella, .CantidadBaseU6 = U(0.5D)}})

            Dim prod = e.Produccion.RegistrarProduccion(e.Minuta, 50, 45, 3, Nothing)
            ' T32: merma de 0,2 L ya incluida en lo entregado: dato analítico, sin segunda baja.
            e.Produccion.RegistrarMerma(prod, bd.VarianteAceiteId, Nothing, "coccion", U(0.2D), "Derrame", yaIncluidaEnConsumo:=True)
            Assert.Equal(0L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM documento_stock WHERE tipo = 'baja'")))
            Assert.Equal("BAJA_REQUERIDA", Assert.Throws(Of ReglaNegocioException)(
                Function() e.Produccion.RegistrarMerma(prod, bd.VarianteAceiteId, Nothing, "almacen", U(0.2D), "Vencido", yaIncluidaEnConsumo:=False)).Codigo)

            Dim r = e.Produccion.Reporte(e.Minuta)
            Dim c = r.Consumo.Single()
            Assert.Equal(U(6D), c.EntregadoU6)
            Assert.Equal(U(0.5D), c.DevueltoU6)
            Assert.Equal(U(5.5D), c.NetoU6)
            Assert.Equal(U(0.5D), c.DiferenciaU6)
            Assert.Equal(U(44D), r.CostoRealU6)
            Assert.Equal(U(40D), r.CostoPrevistoU6)               ' 50 raciones × 0,1 L × S/8
            Assert.Equal(U(0.977778D), r.CostoRealPorRacionServidaU6)   ' 44 / 45
            Assert.Equal(45L, r.RacionesServidas)
            Assert.Single(r.Mermas)
            Assert.Equal(3L, Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM produccion_documento WHERE produccion_id = {prod}")))
            Assert.Equal(0L, bd.FilasSinConciliar())
        End Using
    End Sub

    <FactPostgres>
    Public Sub Se_descarga_la_presentacion_completa_y_lo_que_no_alcanza_se_informa()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            ' Primero se agotan las botellas de 1 L con otra entrega.
            Dim r1 = e.Produccion.RequerimientoAdicional(e.Minuta, bd.A.AlmacenId, "Motivo de prueba")
            e.Produccion.Solicitar(r1, bd.ProductoAceiteId, U(10D))
            e.Produccion.AprobarAdicional(r1)
            Assert.Equal("10 x Aceite botella 1 L", e.Produccion.Atender(r1, Fecha).Lineas.Single().Presentaciones)

            Dim r2 = e.Produccion.RequerimientoAdicional(e.Minuta, bd.A.AlmacenId, "Motivo de prueba")
            e.Produccion.Solicitar(r2, bd.ProductoAceiteId, U(0.5D))
            e.Produccion.AprobarAdicional(r2)
            Dim l = e.Produccion.Atender(r2, Fecha).Lineas.Single()
            Assert.Equal(U(4D), l.EntregadoU6)                      ' un bidón completo de 4 L
            Assert.Equal(U(3.5D), l.ExcedentePresentacionU6)

            Dim r3 = e.Produccion.RequerimientoAdicional(e.Minuta, bd.A.AlmacenId, "Motivo de prueba")
            e.Produccion.Solicitar(r3, bd.ProductoAceiteId, U(6D))   ' queda 1 bidón (4 L)
            e.Produccion.AprobarAdicional(r3)
            Dim l3 = e.Produccion.Atender(r3, Fecha).Lineas.Single()
            Assert.Equal(U(4D), l3.EntregadoU6)
            Assert.Equal(U(2D), l3.FaltanteU6)
            Assert.Equal(0L, bd.SaldoU6(bd.VarianteAceiteId))
            Assert.Equal(0L, bd.ValorU6(bd.VarianteAceiteId))       ' sin residuo de valor
        End Using
    End Sub

    <FactPostgres>
    Public Sub Reglas_requerimiento_atendido_inmutable_uno_calculado_por_minuta_y_raciones_coherentes()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim req = e.Produccion.CalcularRequerimiento(e.Minuta, bd.A.AlmacenId)
            Assert.Equal("REQUERIMIENTO_EXISTENTE", Assert.Throws(Of ReglaNegocioException)(Function() e.Produccion.CalcularRequerimiento(e.Minuta, bd.A.AlmacenId)).Codigo)
            e.Produccion.Atender(req, Fecha)
            Assert.Equal("REQUERIMIENTO_ATENDIDO", Assert.Throws(Of ReglaNegocioException)(Sub() e.Produccion.Solicitar(req, bd.ProductoAceiteId, U(1D))).Codigo)
            Assert.Equal("REQUERIMIENTO_ATENDIDO", Assert.Throws(Of ReglaNegocioException)(Function() e.Produccion.Atender(req, Fecha)).Codigo)
            Assert.Equal("RACIONES_INCOHERENTES", Assert.Throws(Of ReglaNegocioException)(Function() e.Produccion.RegistrarProduccion(e.Minuta, 40, 38, 5, Nothing)).Codigo)
            e.Produccion.RegistrarProduccion(e.Minuta, 50, 50, 0, Nothing)
            Assert.Equal("CODIGO_DUPLICADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Produccion.RegistrarProduccion(e.Minuta, 50, 50, 0, Nothing)).Codigo)

            ' Cocina pide y registra, pero no entrega (eso es del almacén).
            Call New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A")).CrearUsuario("cocina", "Cocinero", "Cocina-Clave-2026", bd.A.OperacionId, "COCINA")
            Dim cocina As New ServicioProduccion(bd.CadenaAplicacion, bd.Sesion("A", "cocina", "Cocina-Clave-2026"))
            Dim adicional = cocina.RequerimientoAdicional(e.Minuta, bd.A.AlmacenId, "Motivo de prueba")
            cocina.Solicitar(adicional, bd.ProductoAceiteId, U(1D))
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() cocina.Atender(adicional, Fecha)).Codigo)
        End Using
    End Sub

    ''' <summary>
    ''' El adicional lo aprueba el jefe de operación (la cocina no puede). La cocina pide devolver una entrega; el almacén la
    ''' atiende y recién entonces hay devolución de stock. Una solicitud atendida o anulada no vuelve a atenderse.
    ''' </summary>
    <FactPostgres>
    Public Sub Adicional_lo_aprueba_el_jefe_y_la_devolucion_de_cocina_la_atiende_el_almacen()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            admin.CrearUsuario("cocina", "Cocinero", "Cocina-Clave-2026", bd.A.OperacionId, "COCINA")
            admin.CrearUsuario("jefe", "Jefe de operacion", "Jefe-Clave-2026", bd.A.OperacionId, "OPERACIONES")
            Dim cocina As New ServicioProduccion(bd.CadenaAplicacion, bd.Sesion("A", "cocina", "Cocina-Clave-2026"))
            Dim jefe As New ServicioProduccion(bd.CadenaAplicacion, bd.Sesion("A", "jefe", "Jefe-Clave-2026"))

            ' El adicional no sale sin la aprobación del jefe; la cocina no puede aprobarlo.
            Dim adicional = cocina.RequerimientoAdicional(e.Minuta, bd.A.AlmacenId, "Motivo de prueba")
            cocina.Solicitar(adicional, bd.ProductoAceiteId, U(1D))
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Sub() cocina.AprobarAdicional(adicional)).Codigo)
            Assert.Equal("ADICIONAL_NO_APROBADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Produccion.Atender(adicional, Fecha)).Codigo)
            jefe.AprobarAdicional(adicional)
            Dim entrega = e.Produccion.Atender(adicional, Fecha)
            Assert.Equal(U(1D), entrega.Lineas.Single().EntregadoU6)

            ' La cocina pide devolver: sin stock hasta que el almacén la atienda, y no más de lo entregado.
            Dim solicitud = cocina.SolicitarDevolucion(entrega.DocumentoId.Value, e.Botella, U(0.5D), "Sobro en la cocina")
            Assert.Equal("DEVOLUCION_EXCEDIDA", Assert.Throws(Of ReglaNegocioException)(Function() cocina.SolicitarDevolucion(entrega.DocumentoId.Value, e.Botella, U(0.6D), "Mas")).Codigo)
            Assert.Equal(0L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM documento_stock WHERE tipo = 'devolucion_produccion'")))
            Assert.Single(cocina.ListarEntregasDevolubles(e.Minuta).Where(Function(x) x.VarianteId = e.Botella))
            Assert.Equal(U(0.5D), cocina.ListarEntregasDevolubles(e.Minuta).Single(Function(x) x.VarianteId = e.Botella).DisponibleU6)

            Assert.Single(e.Almacen.ListarDevolucionesPendientes(bd.A.AlmacenId))
            Dim documento = e.Almacen.AtenderDevolucion(solicitud, Fecha)
            Assert.Equal("devolucion_produccion", Convert.ToString(bd.Escalar($"SELECT tipo FROM documento_stock WHERE id = {documento}")))
            Assert.Equal("atendida", Convert.ToString(bd.Escalar($"SELECT estado FROM devolucion_solicitud WHERE id = {solicitud}")))
            Assert.Empty(e.Almacen.ListarDevolucionesPendientes(bd.A.AlmacenId))
            Assert.Equal("SOLICITUD_NO_PENDIENTE", Assert.Throws(Of ReglaNegocioException)(Function() e.Almacen.AtenderDevolucion(solicitud, Fecha)).Codigo)
            Assert.Equal("DEVOLUCION_EXCEDIDA", Assert.Throws(Of ReglaNegocioException)(Function() cocina.SolicitarDevolucion(entrega.DocumentoId.Value, e.Botella, U(0.6D), "Otra vez")).Codigo)

            ' Una solicitud pendiente la cocina la puede anular; el almacén ya no la atiende.
            Dim otra = cocina.SolicitarDevolucion(entrega.DocumentoId.Value, e.Botella, U(0.5D), "Dudoso")
            cocina.AnularDevolucion(otra)
            Assert.Empty(e.Almacen.ListarDevolucionesPendientes(bd.A.AlmacenId))
            Assert.Equal("SOLICITUD_NO_PENDIENTE", Assert.Throws(Of ReglaNegocioException)(Function() e.Almacen.AtenderDevolucion(otra, Fecha)).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Adicional_exige_motivo_y_lo_guarda()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Assert.Throws(Of ReglaNegocioException)(Function() e.Produccion.RequerimientoAdicional(e.Minuta, bd.A.AlmacenId, "   "))
            Dim adicional = e.Produccion.RequerimientoAdicional(e.Minuta, bd.A.AlmacenId, "Llegaron 20 invitados")
            Assert.Equal("Llegaron 20 invitados", Convert.ToString(bd.Escalar($"SELECT motivo FROM requerimiento WHERE id = {adicional}")))
            Assert.Equal("borrador", Convert.ToString(bd.Escalar($"SELECT estado FROM requerimiento WHERE id = {adicional}")))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Minuta_despachada_no_admite_ajuste_operativo()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim plato = minutas.ListarPlatos(e.Minuta).First().Id
            Dim req = e.Produccion.CalcularRequerimiento(e.Minuta, bd.A.AlmacenId)
            e.Produccion.Atender(req, Fecha)
            Assert.Equal("DESPACHADA", Assert.Throws(Of ReglaNegocioException)(Sub() minutas.AjustarRacionesOperativas(plato, 40, "Menos comensales")).Codigo)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Reporte_salida_a_produccion_suma_lo_entregado_al_costo_de_la_entrega()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim req = e.Produccion.CalcularRequerimiento(e.Minuta, bd.A.AlmacenId)
            Dim entrega = e.Produccion.Atender(req, Fecha)

            Dim reporte = New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A")).SalidasAProduccion(Fecha, Fecha)
            Assert.Equal("Salida a produccion", reporte.Titulo)
            Assert.Equal(1, reporte.Secciones(0).Filas.Count)
            Assert.Equal(entrega.ValorU6, CType(reporte.Secciones(0).Totales(8), Long))
            Assert.Empty(New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A")).SalidasAProduccion(Fecha.AddDays(1), Fecha.AddDays(1)).Secciones(0).Filas)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Requisicion_por_servicio_solo_cuenta_lo_atendido()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim reportes = New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A"))
            Assert.Empty(reportes.RequisicionPorServicio(Fecha, Fecha).Secciones(0).Filas)   ' todavia no hay entrega

            Dim req = e.Produccion.CalcularRequerimiento(e.Minuta, bd.A.AlmacenId)
            e.Produccion.Atender(req, Fecha)
            Dim filas = reportes.RequisicionPorServicio(Fecha, Fecha).Secciones(0).Filas
            Assert.NotEmpty(filas)
            Assert.True(filas.All(Function(f) CType(f(3), Long) > 0))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Costo_realizado_sale_con_lo_entregado_de_la_minuta()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim req = e.Produccion.CalcularRequerimiento(e.Minuta, bd.A.AlmacenId)
            e.Produccion.Atender(req, Fecha)
            Dim realizado = New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A")).CostoRealizado(Fecha, Fecha)
            Assert.NotEmpty(realizado.Secciones(0).Filas)
            Assert.True(realizado.Secciones(0).Filas.All(Function(f) CType(f(5), Long) > 0))
        End Using
    End Sub

End Class
