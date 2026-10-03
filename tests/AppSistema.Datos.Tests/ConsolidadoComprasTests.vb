Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

''' <summary>
''' Abastecimiento: consolidado de compras del periodo entre todas las operaciones. La demanda sale de las minutas de
''' Planificación; por operación se descuentan su stock y lo pendiente, y se costea con su producto activo.
''' </summary>
Public Class ConsolidadoComprasTests

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    <FactPostgres>
    Public Sub Suma_las_operaciones_del_periodo_descuenta_stock_y_costea_con_el_producto_activo()
        Using bd = BaseDatosPrueba.Crear()
            Dim admin As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim are = admin.CrearOperacion("ARE", "Arequipa")
            Dim almacenAre = admin.CrearAlmacen(are, "P2", "Principal Arequipa")
            Call New ServicioInstalacion(bd.CadenaAdmin).CrearDueno("A", "dueno", "Dueno", "Duenio-Clave-2026")
            Dim acceso As New ServicioAcceso(bd.CadenaAplicacion)
            Dim login = acceso.IniciarSesion("A", "dueno", "Duenio-Clave-2026")
            Dim enOrc = acceso.SeleccionarOperacion(login, bd.A.OperacionId)
            Dim enAre = acceso.SeleccionarOperacion(login, are)

            ' Aceite a S/ 8 por L (caja 4 × 4 L a S/ 128). Receta de sopa: 0,1 L por ración.
            Dim prov As New ServicioProveedores(bd.CadenaAplicacion, enOrc)
            prov.RegistrarPrecio(prov.VincularEmpaque(prov.CrearProveedor(New ProveedorDto With {.Codigo = "P1", .Nombre = "Mayorista"}), bd.EmpaqueCajaId, 2),
                                 New Date(2026, 1, 1), Nothing, "PEN", U(128D), False)
            Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, enOrc)
            Dim sopa = recetas.CrearReceta("SOPA", "Sopa", Nothing, U(1D), Nothing)
            recetas.AgregarIngrediente(sopa, bd.ProductoAceiteId, U(0.1D), Nothing, 1)
            recetas.Aprobar(sopa)

            ' Planificación arma el menú de cada operación: Orcopampa 100 pax; Arequipa 50 pax aprobados y 20 en borrador.
            Dim Minuta = Function(s As SesionUsuario, fecha As Date, pax As Long, aprobar As Boolean)
                             Dim m As New ServicioMinutas(bd.CadenaAplicacion, s)
                             Dim serv = m.ListarServicios().FirstOrDefault()?.Id
                             If Not serv.HasValue Then serv = m.CrearServicio("ALM", "Almuerzo")
                             Dim est = m.ListarEstructuras(serv.Value).FirstOrDefault()?.Id
                             If Not est.HasValue Then est = m.CrearEstructura(serv.Value, "SOPA", "Sopa", 1)
                             Dim os = m.ListarServiciosDeOperacion().FirstOrDefault()?.Id
                             If Not os.HasValue Then os = m.AsignarServicio(serv.Value, If(m.ListarRegimenes().FirstOrDefault()?.Id, m.CrearRegimen("GEN", "General")), Nothing)
                             Dim id = m.CrearMinuta(os.Value, fecha, pax)
                             m.AgregarPlato(id, est.Value, sopa, pax)
                             If aprobar Then m.Aprobar(id, "PEN")
                             Return id
                         End Function
            Minuta(enOrc, New Date(2026, 11, 3), 100, True)
            Minuta(enAre, New Date(2026, 11, 4), 50, True)
            Minuta(enAre, New Date(2026, 11, 5), 20, False)
            Minuta(enOrc, New Date(2026, 12, 1), 999, True)                                  ' fuera del periodo

            ' Stock: Orcopampa tiene 4 L (el bidón es su producto en uso); Arequipa no tiene stock y libera el bidón.
            Call New ServicioStock(bd.CadenaAplicacion, enOrc).Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Apertura, New Date(2026, 10, 1), "AP-1",
                {New LineaDocumentoStock(bd.VarianteAceiteId, U(4D), U(8D))}))
            Call New ServicioCatalogo(bd.CadenaAplicacion, enAre).ActivarEnOperacion(bd.VarianteAceiteId)

            Dim consolidado As New ServicioConsolidadoCompras(bd.CadenaAplicacion, enOrc)
            Dim c = consolidado.Calcular(New Date(2026, 11, 1), New Date(2026, 11, 30))
            Assert.Equal(2, c.Operaciones.Count)
            Dim aceite = c.Lineas.Single()
            ' Orcopampa: 10 L - 4 L de stock = 6 L; Arequipa: 5 L. Total 15 L de demanda, 11 L a comprar, S/ 88.
            Assert.Equal(U(15D), aceite.DemandaU6)
            Assert.Equal(U(4D), aceite.StockU6)
            Assert.Equal(U(11D), aceite.AComprarU6)
            Assert.Equal(U(88D), aceite.CostoEstimadoU6)
            Assert.Equal(2, aceite.Operaciones)
            Assert.Equal(U(6D), c.Detalle.Single(Function(d) d.Operacion.StartsWith("ORC")).AComprarU6)
            Assert.Equal(U(88D), c.CostoTotalU6)

            ' Con las minutas en borrador (menú aún no aprobado): 2 L más en Arequipa.
            Assert.Equal(U(13D), consolidado.Calcular(New Date(2026, 11, 1), New Date(2026, 11, 30), incluirBorradores:=True).Lineas.Single().AComprarU6)
            Assert.Contains("TOTAL", consolidado.Reporte(New Date(2026, 11, 1), New Date(2026, 11, 30)).ACsv())

            ' El comprador con rol de Abastecimiento solo en Orcopampa consolida solo esa operación; Cocina no consolida.
            admin.CrearUsuario("compras", "Comprador", "Compras-Clave-2026", bd.A.OperacionId, RolesBase.Abastecimiento)
            Dim comprador As New ServicioConsolidadoCompras(bd.CadenaAplicacion, bd.Sesion("A", "compras", "Compras-Clave-2026"))
            Dim soloOrc = comprador.Calcular(New Date(2026, 11, 1), New Date(2026, 11, 30))
            Assert.Equal(1, soloOrc.Operaciones.Count)
            Assert.Equal(U(6D), soloOrc.Lineas.Single().AComprarU6)
            admin.CrearUsuario("cocina", "Cocinero", "Cocina-Clave-2026", bd.A.OperacionId, "COCINA")
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(
                Function() New ServicioConsolidadoCompras(bd.CadenaAplicacion, bd.Sesion("A", "cocina", "Cocina-Clave-2026")).Calcular(Date.Today, Date.Today)).Codigo)
        End Using
    End Sub

End Class
