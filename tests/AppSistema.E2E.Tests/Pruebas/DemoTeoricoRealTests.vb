Imports System.Globalization
Imports System.IO
Imports FlaUI.Core.AutomationElements
Imports FlaUI.Core.Definitions
Imports FlaUI.Core.Tools
Imports Xunit
Imports AppSistema.Datos
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Catalogo
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock
Imports AppSistema.Escritorio

''' <summary>
''' Demostración del menú teórico y del real. Carga un desayuno de 500 comensales (teórico), su producción (470 raciones),
''' el consumo por componente y la venta real; exporta los reportes a artifacts/demo (Excel y PDF) y recorre la aplicación
''' real: la matriz de planificación, el plan operativo del chef y la comparación teórico vs real de la minuta.
''' </summary>
<Collection("E2E")>
Public Class DemoTeoricoRealTests

    Private ReadOnly _bd As BaseDatosE2E

    Public Sub New(bd As BaseDatosE2E)
        _bd = bd
    End Sub

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    Private Shared Function IdentificadorDe(e As AutomationElement) As String
        Try
            Return e.AutomationId
        Catch
            Return ""
        End Try
    End Function

    Private Shared Function Cifra(x As Long?) As Long
        Return If(x.HasValue, x.Value, 0L)
    End Function

    Private Shared Function Nombre(titulo As String) As String
        Return New String(titulo.Select(Function(c) If(Char.IsLetterOrDigit(c), c, "_"c)).ToArray()).Trim("_"c)
    End Function

    <FactE2E>
    Public Sub Demo_menu_teorico_y_real_se_muestra_en_la_aplicacion()
        Dim carpeta = Path.Combine(EntornoPrueba.Artefactos, "demo")
        Directory.CreateDirectory(carpeta)
        Dim fecha = Date.Today
        Dim s = _bd.Sesion(UsuariosPrueba.Dueno)
        Dim cadena = _bd.CadenaAplicacion
        Dim almacenId = _bd.Instalacion.AlmacenId
        Dim operacionId = _bd.Instalacion.OperacionId

        ' ---------- Catálogo y compras ----------
        Dim cat As New ServicioCatalogo(cadena, s)
        Dim litro = cat.CrearUnidad("L", "Litro", Dimension.Volumen, 1000000)
        Dim aceite = cat.CrearProducto("ACE", "Aceite vegetal", Nothing, litro, Nothing)
        Dim varianteAceite = cat.CrearVariante(aceite, Nothing, "ACE-A-4L", "Aceite A 4 L", "bidon", 4000000)
        Dim caja = cat.CrearEmpaque(varianteAceite, "CAJA4", "Caja 4 x 4 L", 4)
        cat.ActivarEnOperacion(varianteAceite)
        Dim prov As New ServicioProveedores(cadena, s)
        prov.RegistrarPrecio(prov.VincularEmpaque(prov.CrearProveedor(New ProveedorDto With {.Codigo = "P1", .Nombre = "Distribuidora"}), caja, 2),
                             New Date(2026, 1, 1), Nothing, "PEN", U(128D), False)

        ' ---------- Menú: desayuno de 500 comensales ----------
        Dim minutas As New ServicioMinutas(cadena, s)
        Dim recetas As New ServicioRecetas(cadena, s)
        Dim comparativo As New ServicioComparativo(cadena, s)
        Dim desayuno = minutas.CrearServicio("DES", "Desayuno")
        Dim bebida = minutas.CrearEstructura(desayuno, "BEB", "Bebida caliente", 1)
        Dim jugo = minutas.CrearEstructura(desayuno, "JUG", "Jugo", 2)
        Dim complemento1 = minutas.CrearEstructura(desayuno, "CO1", "Complemento 1", 3, factorConsumoBp:=7000)
        Dim complemento2 = minutas.CrearEstructura(desayuno, "CO2", "Complemento 2", 4, factorConsumoBp:=3000)
        Dim opServicio = minutas.AsignarServicio(desayuno, minutas.CrearRegimen("GEN", "General"), Nothing)

        Dim receta = Function(codigo As String, litrosPorRacion As Decimal) As Long
                         Dim v = recetas.CrearReceta(codigo, codigo, Nothing, U(1D), Nothing)
                         recetas.AgregarIngrediente(v, aceite, U(litrosPorRacion), Nothing, 1)
                         recetas.Aprobar(v)
                         Return v
                     End Function
        Dim cafe = receta("CAFE", 0.05D)
        Dim papaya = receta("JUGO-PAPAYA", 0.1D)
        Dim pina = receta("JUGO-PINA", 0.125D)
        Dim jamon = receta("PAN-JAMON", 0.2D)
        Dim palta = receta("PAN-PALTA", 0.25D)

        Dim minuta = minutas.CrearMinuta(opServicio, fecha, 500)
        minutas.AgregarPlatoPorFactor(minuta, bebida, cafe)
        minutas.AgregarPlatoPorFactor(minuta, jugo, papaya, repartoBp:=5000)
        minutas.AgregarPlatoPorFactor(minuta, jugo, pina, repartoBp:=5000)
        minutas.AgregarPlatoPorFactor(minuta, complemento1, jamon)
        minutas.AgregarPlatoPorFactor(minuta, complemento2, palta)
        minutas.Aprobar(minuta, "PEN")

        Dim plato = Function(recetaId As Long) minutas.ListarPlatos(minuta).Single(Function(p) p.RecetaVersionId = recetaId).Id

        ' ---------- Lo real: 200 L de aceite y 10 kg de azúcar no planificados, producción, consumo y venta ----------
        Dim azucar = cat.CrearProducto("AZU", "Azucar", Nothing, litro, Nothing)
        Dim bolsa = cat.CrearVariante(azucar, Nothing, "AZU-1", "Azucar bolsa", "bolsa", U(1D))
        Call New ServicioStock(cadena, s).Contabilizar(New DocumentoStockNuevo(almacenId, TipoDocumentoStock.Apertura, fecha.AddDays(-5), "AP-1",
            {New LineaDocumentoStock(varianteAceite, U(600D), U(8D)), New LineaDocumentoStock(bolsa, U(50D), U(3D))}))
        Dim almacen As New ServicioAlmacen(cadena, s)
        almacen.SalidaProduccion(almacenId, fecha, {New LineaSalida With {.VarianteId = varianteAceite, .CantidadBaseU6 = U(200D)},
                                                    New LineaSalida With {.VarianteId = bolsa, .CantidadBaseU6 = U(10D)}}, Nothing, opServicio)
        Call New ServicioProduccion(cadena, s).RegistrarProduccion(minuta, 480, 470, 10, Nothing)
        comparativo.RegistrarConsumo(plato(cafe), 480, 470)
        comparativo.RegistrarConsumo(plato(papaya), 240, 230)
        comparativo.RegistrarConsumo(plato(pina), 240, 235)
        comparativo.RegistrarConsumo(plato(jamon), 350, 300)
        comparativo.RegistrarConsumo(plato(palta), 150, 100)
        comparativo.RegistrarVenta(minuta, 470, Nothing, Nothing)

        ' Comprobación de que el escenario es el esperado (mismas cifras que TeoricoRealTests).
        Dim c = comparativo.Comparativo(minuta)
        Assert.Equal(U(3145.833333D), Cifra(c.VentaTeoricaU6))
        Assert.Equal(U(1510D), Cifra(c.CostoTeoricoU6))
        Assert.Equal(U(1630D), Cifra(c.CostoRealU6))

        ' ---------- Reportes exportados (Excel y PDF) ----------
        Dim reportes = New ServicioReportes(cadena, s)
        Dim lista = {reportes.MatrizDelPeriodo(fecha, fecha), reportes.CostoResumidoTeorico(fecha, fecha), reportes.CostoDetalladoTeorico(fecha, fecha),
                     reportes.ComparativoRaciones(fecha, fecha), reportes.ControlRaciones(fecha, fecha),
                     reportes.ComparativoMensual(opServicio, fecha.Year, fecha.Month), reportes.CostoRealizado(fecha, fecha)}
        For Each r In lista
            File.WriteAllBytes(Path.Combine(carpeta, Nombre(r.Titulo) & ".xlsx"), ExportadorReporte.AExcel(r))
            File.WriteAllBytes(Path.Combine(carpeta, Nombre(r.Titulo) & ".pdf"), ExportadorReporte.APdf(r))
        Next

        ' Teórico vs real en texto, con las cifras de la minuta.
        Dim cultura = CultureInfo.InvariantCulture
        Dim importe = Function(v As Long) EscalaU6.ADecimal(v).ToString("N2", cultura)
        Dim texto As New List(Of String) From {
            $"Teorico vs real - minuta del {fecha:dd/MM/yyyy} (desayuno)",
            $"Comensales planificados: {c.ComensalesPlan}   Preparadas: {c.RacionesPreparadas}   Consumidas: {c.RacionesConsumidas}   Vendidas: {c.RacionesVendidas}",
            $"Venta teorica S/ {importe(Cifra(c.VentaTeoricaU6))}   Venta real S/ {importe(Cifra(c.VentaRealU6))}   Diferencia S/ {importe(Cifra(c.DiferenciaVentaU6))}",
            $"Costo teorico S/ {importe(Cifra(c.CostoTeoricoU6))}   Costo teorico de lo consumido S/ {importe(Cifra(c.CostoTeoricoConsumidoU6))}   Costo real S/ {importe(Cifra(c.CostoRealU6))}",
            $"Diferencia de costo S/ {importe(Cifra(c.DiferenciaCostoU6))}   Food cost teorico {importe(Cifra(c.FoodCostTeoricoU6))} %   Food cost real {importe(Cifra(c.FoodCostRealU6))} %",
            "",
            "Componentes (factor planificado y real):"}
        For Each k In c.Componentes
            texto.Add($"  {k.Receta}: plan {k.RacionesPlan} raciones ({Cifra(k.FactorPlanBp) / 100D:0.##} %), consumidas {k.RacionesConsumidas} ({Cifra(k.FactorRealBp) / 100D:0.##} %)")
        Next
        texto.Add("")
        texto.Add("Productos (teorico vs real):")
        For Each p In c.Productos
            texto.Add($"  {p.Producto} [{p.Estado}]: teorico {EscalaU6.ADecimal(Cifra(p.CantidadTeoricaU6)):0.00} {p.Unidad}, real {EscalaU6.ADecimal(Cifra(p.CantidadRealU6)):0.00} {p.Unidad}, costo teorico S/ {importe(Cifra(p.CostoTeoricoU6))}, costo real S/ {importe(Cifra(p.CostoRealU6))}")
        Next
        File.WriteAllLines(Path.Combine(carpeta, "teorico_vs_real.txt"), texto)

        ' ---------- Recorrido de la aplicación real ----------
        Using app As New AplicacionE2E(_bd)
            ' Ingreso: se pulsa "Iniciar sesion" por el patrón Invoke (sin clic de mouse, que esta sesión no permite) y se
            ' espera a que la ventana de acceso se cierre.
            Dim acceso As New PaginaAcceso(app)
            app.Escribir("txtEmpresa", UsuariosPrueba.Empresa)
            app.Escribir("txtUsuario", UsuariosPrueba.Dueno.Login)
            app.Escribir("txtClave", UsuariosPrueba.Dueno.Clave)
            app.Esperar("btnEntrar").Patterns.Invoke.Pattern.Invoke()
            Retry.WhileTrue(Function() app.Existe("FormAcceso") AndAlso app.Mensaje() Is Nothing, AplicacionE2E.Espera, TimeSpan.FromMilliseconds(250))
            Assert.False(app.Existe("FormAcceso"), "El superusuario no entro: " & app.Mensaje())
            Dim p As New PaginaPrincipal(app)

            p.Abrir("mnuMenus", "mnuMatrizDePlanificacion")
            Assert.Null(app.Mensaje())
            app.Capturar("01-menu-teorico-matriz-de-planificacion")
            For Each w In p.VentanasDeTrabajo()
                p.Cerrar(w)
            Next

            p.Abrir("mnuMenus", Identificadores.DesdeTexto("mnu", "Plan operativo del chef"))
            Assert.Null(app.Mensaje())
            app.Capturar("02-plan-operativo-del-chef")
            For Each w In p.VentanasDeTrabajo()
                p.Cerrar(w)
            Next

            p.Abrir("mnuMenus", "mnuProduccion")
            ' La minuta del día queda en la fila activa de la grilla (la primera); el botón la usa sin más selección.
            Assert.Null(app.Mensaje())
            AplicacionE2E.Pulsar(app.Esperar(Identificadores.DesdeTexto("btn", "Teorico vs real")))
            Dim comparacion = app.Esperar("FormComparativo")
            Assert.Null(app.Mensaje())
            app.Capturar("03-teorico-vs-real-de-la-minuta")
            File.WriteAllLines(Path.Combine(carpeta, "pantalla_teorico_vs_real.txt"),
                               comparacion.FindAllDescendants(Function(cf) cf.ByControlType(ControlType.Text)).Select(Function(t) t.Name).Where(Function(t) Not String.IsNullOrWhiteSpace(t)))
        End Using
    End Sub

End Class
