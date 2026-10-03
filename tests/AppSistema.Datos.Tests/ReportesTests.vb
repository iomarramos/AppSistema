Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

''' <summary>Reportes imprimibles o exportables: minuta del día, requerimiento, kárdex, inventario y stock valorizado.</summary>
Public Class ReportesTests

    Private Shared ReadOnly Fecha As New Date(2026, 10, 2)

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    <Fact>
    Public Sub Csv_y_html_escapan_el_texto_y_dejan_vacio_lo_que_no_tiene_valor()
        Dim r As New Reporte("Prueba <1>") With {.GeneradoEn = New DateTime(2026, 10, 3, 8, 30, 0)}
        r.Dato("Operacion", "ORC; Lima")
        Dim s = r.Seccion("Tabla", New ColumnaReporte("Producto"), New ColumnaReporte("Cantidad", FormatoColumna.Cantidad),
                          New ColumnaReporte("Importe", FormatoColumna.Dinero))
        s.Agregar("Pan ""frances""", U(1234.5D), Nothing)
        s.Totales = {"TOTAL", Nothing, U(1D / 3D)}

        Dim csv = r.ACsv()
        Assert.Contains("Operacion;""ORC; Lima""", csv)
        Assert.Contains("""Pan """"frances"""""";1234.5;" & vbLf, csv)        ' sin precio: celda vacía, nunca 0
        Assert.Contains("TOTAL;;0.33", csv)

        Dim html = r.AHtml()
        Assert.Contains("<title>Prueba &lt;1&gt;</title>", html)
        Assert.Contains("<td class=""n"">1,234.5</td>", html)
        Assert.Contains("Pan &quot;frances&quot;", html)
        Assert.Equal("Prueba__1__20261003_0830.csv", r.NombreArchivo("csv"))
        Assert.Throws(Of ArgumentException)(Sub() s.Agregar("solo una celda"))
    End Sub

    <FactPostgres>
    Public Sub Los_cuatro_reportes_salen_con_los_datos_de_la_operacion_y_con_su_permiso()
        Using bd = BaseDatosPrueba.Crear()
            Dim s = bd.Sesion("A")
            Dim botella = New ServicioCatalogo(bd.CadenaAplicacion, s).CrearVariante(bd.ProductoAceiteId, Nothing, "ACE-1L", "Aceite botella 1 L", "botella", U(1D))
            Call New ServicioStock(bd.CadenaAplicacion, s).Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Apertura, Fecha, "AP-1",
                {New LineaDocumentoStock(bd.VarianteAceiteId, U(8D), U(8D)), New LineaDocumentoStock(botella, U(10D), U(8D))}))
            Dim prov As New ServicioProveedores(bd.CadenaAplicacion, s)
            prov.RegistrarPrecio(prov.VincularEmpaque(prov.CrearProveedor(New ProveedorDto With {.Codigo = "P1", .Nombre = "Mayorista"}), bd.EmpaqueCajaId, 1),
                                 New Date(2026, 1, 1), Nothing, "PEN", U(128D), False)
            Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, s)
            Dim version = recetas.CrearReceta("SOPA", "Sopa", Nothing, U(1D), Nothing)
            recetas.AgregarIngrediente(version, bd.ProductoAceiteId, U(0.1D), Nothing, 1)
            recetas.Aprobar(version)
            Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, s)
            Dim servicio = minutas.CrearServicio("ALM", "Almuerzo")
            Dim minuta = minutas.CrearMinuta(minutas.AsignarServicio(servicio, minutas.CrearRegimen("GEN", "General"), Nothing), Fecha, 50)
            minutas.AgregarPlato(minuta, minutas.CrearEstructura(servicio, "SOPA", "Sopa", 1), version, 50)
            minutas.Aprobar(minuta, "PEN")
            Dim produccion As New ServicioProduccion(bd.CadenaAplicacion, s)
            Dim req = produccion.CalcularRequerimiento(minuta, bd.A.AlmacenId)
            produccion.Atender(req, Fecha)

            Dim reportes As New ServicioReportes(bd.CadenaAplicacion, s)

            ' Minuta del día: plato con su costo (0,1 L × S/8 × 50 = S/40), venta al 48 % y necesidad para cocina.
            Dim m = reportes.MinutaDelDia(minuta)
            Assert.Equal("Minuta Almuerzo 2026-10-02", m.Titulo)
            Assert.Equal({"SOPA Sopa"}, m.Secciones(0).Filas.Select(Function(f) CStr(f(1))))
            Assert.Equal(U(40D), m.Secciones(0).Filas.Single()(5))
            Assert.Equal("40.00", Dato(m, "Costo previsto (S/)"))
            Assert.Equal("0.80", Dato(m, "Costo por comensal (S/)"))
            Assert.Equal("83.33", Dato(m, "Venta prevista (S/)"))
            Dim necesidad = m.Secciones.Single(Function(x) x.Titulo.StartsWith("Necesidad")).Filas.Single()
            Assert.Equal(U(5D), necesidad(3))
            Assert.Contains("Recibido por (cocina)", m.AHtml())

            ' Requerimiento: lo previsto y lo solicitado, con firmas de entrega.
            Dim q = reportes.Requerimiento(req)
            Assert.Equal("atendido", Dato(q, "Estado"))
            Assert.Equal(U(5D), q.Secciones(0).Filas.Single()(2))
            Assert.Contains("Entregado por (almacen)", q.Firmas)

            ' Kárdex de la botella: apertura 10 L, entrega 5 L, saldo 5 L a S/8.
            Dim k = reportes.Kardex(bd.A.AlmacenId, botella, Fecha, Fecha)
            Dim filas = k.Secciones(0).Filas
            Assert.Equal({"SALDO INICIAL", "AP-1"}, filas.Take(2).Select(Function(f) CStr(f(1))))
            Assert.Equal(3, filas.Count)
            Dim total = k.Secciones(0).Totales
            Assert.Equal({U(10D), U(5D), U(5D), U(40D), U(8D)}, {total(3), total(4), total(6), total(7), total(8)}.Select(Function(v) CLng(v)))
            Assert.Contains("TOTAL;;;10;5;;5;40.00;8.00", k.ACsv())

            ' Inventario: la hoja de conteo no muestra el stock del sistema; el resultado sí, con su diferencia valorizada.
            Dim inventarios As New ServicioInventarios(bd.CadenaAplicacion, s)
            Dim inv = inventarios.Abrir(bd.A.AlmacenId, Fecha, "general")
            Dim hoja = reportes.Inventario(inv, hojaDeConteo:=True)
            Assert.DoesNotContain(hoja.Secciones(0).Columnas, Function(c) c.Nombre = "Sistema")
            Assert.All(hoja.Secciones(0).Filas, Sub(f) Assert.Null(f(5)))
            inventarios.RegistrarConteo(inventarios.Hoja(inv).Single(Function(l) l.VarianteId = botella).Id, U(4D), Nothing)    ' 4 L contra 5 L
            Dim resultado = reportes.Inventario(inv, hojaDeConteo:=False)
            Dim filaBotella = resultado.Secciones(0).Filas.Single(Function(f) CStr(f(0)) = "ACE-1L")
            Assert.Equal({U(5D), U(4D), U(-1D), U(-8D)}, {filaBotella(3), filaBotella(4), filaBotella(5), filaBotella(6)}.Select(Function(v) CLng(v)))
            Assert.Equal("faltante", filaBotella(7))
            Assert.Equal("8.00", Dato(resultado, "Faltante (S/)"))
            Assert.Equal("1", Dato(resultado, "Lineas sin contar"))

            ' Stock valorizado: bidón 8 L + botella 5 L = 13 L a S/8.
            Dim st = reportes.StockValorizado(bd.A.AlmacenId)
            Assert.Equal(U(104D), CLng(st.Secciones(0).Totales(6)))

            ' Otra empresa no ve la minuta; cocina no tiene permiso de inventario.
            Dim otra As New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("B"))
            Assert.Equal("OPERACION_AJENA", Assert.Throws(Of ReglaNegocioException)(Function() otra.MinutaDelDia(minuta)).Codigo)
            Assert.Equal("OPERACION_AJENA", Assert.Throws(Of ReglaNegocioException)(Function() otra.Kardex(bd.A.AlmacenId, botella, Fecha, Fecha)).Codigo)
            Call New ServicioAdministracion(bd.CadenaAplicacion, s).CrearUsuario("cocina", "Cocinero", "Cocina-Clave-2026", bd.A.OperacionId, "COCINA")
            Dim cocina As New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A", "cocina", "Cocina-Clave-2026"))
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() cocina.Inventario(inv, True)).Codigo)
            Assert.Equal("Minuta Almuerzo 2026-10-02", cocina.MinutaDelDia(minuta).Titulo)
        End Using
    End Sub

    Private Shared Function Dato(r As Reporte, etiqueta As String) As String
        Return r.Encabezado.Single(Function(d) d.Key = etiqueta).Value
    End Function

End Class
