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
            ' Bulto en decimales con la presentación activa (bidón de 4 L): 5 L = 1,25 bidones. Sin activa, vacío.
            Call New ServicioCatalogo(bd.CadenaAplicacion, s).ActivarEnOperacion(bd.VarianteAceiteId)
            Dim conBulto = reportes.Requerimiento(req).Secciones(0).Filas.Single()
            Assert.Equal(U(1.25D), conBulto(5))
            Assert.Equal("bidon", conBulto(6))

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
            ' Formato SGP: P.M.P. (costo) S/8, stock fisico 4 L y total 32; sistema 5 L y total 40; diferencia -1 L y total -8.
            Dim filaBotella = resultado.Secciones(0).Filas.Single(Function(f) CStr(f(0)) = "ACE-1L")
            Assert.Equal({U(8D), U(4D), U(32D), U(5D), U(40D), U(-1D), U(-8D)},
                         {filaBotella(3), filaBotella(4), filaBotella(5), filaBotella(6), filaBotella(7), filaBotella(8), filaBotella(9)}.Select(Function(v) CLng(v)))
            Assert.Equal("8.00", Dato(resultado, "Faltante (S/)"))
            Assert.Equal("1", Dato(resultado, "Lineas sin contar"))

            ' Inventario fisico valorizado (formato SGP): bidón 8 L + botella 5 L = 13 L a S/8, en una sola familia sin categoria.
            Dim st = reportes.StockValorizado(bd.A.AlmacenId)
            Assert.Equal("SIN FAMILIA", st.Secciones(0).Titulo)
            Assert.Equal(U(104D), CLng(st.Secciones(0).Totales(5)))
            Assert.Equal("104.00", Dato(st, "Total general (S/)"))

            ' Movimiento de stock sintetico (formato SGP): la apertura es la implantacion (S/144), la produccion la retirada (S/40)
            ' y el saldo actual es el valorizado (S/104). Sin movimientos antes del periodo, saldo anterior cero.
            Dim mov = reportes.MovimientoStockSintetico(bd.A.AlmacenId, Fecha, Fecha).Secciones(0).Filas.Single()
            Assert.Equal({U(0D), U(0D), U(144D), U(40D), U(0D), U(0D), U(0D), U(104D)}, {mov(1), mov(2), mov(3), mov(4), mov(5), mov(6), mov(7), mov(8)}.Select(Function(v) CLng(v)))

            ' Resumen de salidas para produccion: 5 L por S/40 en el dia; sin devoluciones.
            Dim salidas = reportes.SalidasConsolidadas(bd.A.AlmacenId, Fecha, Fecha, devoluciones:=False)
            Dim salida = salidas.Secciones(0).Filas.Single()
            Assert.Equal(U(5D), CLng(salida(2)))
            Assert.Equal(U(40D), CLng(salida(4)))
            Assert.Equal(U(40D), CLng(salidas.Secciones(0).Totales(4)))
            Assert.Empty(reportes.SalidasConsolidadas(bd.A.AlmacenId, Fecha, Fecha, devoluciones:=True).Secciones(0).Filas)

            ' A13 (formato SGP): consumo = inicial + entradas (apertura S/144) - traspasos enviados - final (S/104) = S/40, lo producido.
            Dim a13 = reportes.ResultadoA13(Fecha.Year, Fecha.Month)
            Assert.Equal(U(40D), CLng(a13.Secciones(1).Totales(1)))
            ' Dias de stock con el criterio del contrato: 104 de stock entre 40 de consumo: 78 dias con 30 dias base; 55 con 21.
            Assert.Equal("78", CStr(a13.Secciones(2).Filas(3)(1)))
            Call New ServicioAdministracion(bd.CadenaAplicacion, s).FijarDiasStock(bd.A.OperacionId, 21)
            Assert.Equal("55", CStr(reportes.ResultadoA13(Fecha.Year, Fecha.Month).Secciones(2).Filas(3)(1)))

            ' Menu de reportes: frecuencia de la teorica (una receta, una vez), requisicion y piso y techo en horizontal (sin datos SGP en el fixture).
            Assert.Single(reportes.FrecuenciaTeorica(Fecha.Year, Fecha.Month).Secciones(0).Filas)
            Assert.True(reportes.RequisicionRango(Fecha, Fecha).Horizontal)
            Assert.True(reportes.CostoPisoTecho(Fecha.Year, Fecha.Month).Horizontal)
            Assert.True(reportes.SalidasPorServicio(Fecha, Fecha, False).Horizontal)

            ' Menu y venta en hoja horizontal. Food cost y comparativo: una minuta aprobada del mes, con su teorico y sus raciones.
            Assert.True(reportes.MinutaDelDia(minuta).Horizontal)
            Dim food = reportes.FoodCost(Fecha.Year, Fecha.Month)
            Assert.True(food.Horizontal)
            Assert.Single(food.Secciones(0).Filas)
            Dim comparativo = reportes.ComparativoTresNiveles(Fecha.Year, Fecha.Month)
            Assert.True(comparativo.Horizontal)
            Assert.Equal(50L, CLng(comparativo.Secciones(0).Filas.Single()(2)))

            ' Traspasos: ninguno en el periodo de la prueba, pero las dos tablas (entrada y salida) salen.
            Assert.Equal(2, reportes.Traspasos(Fecha, Fecha).Secciones.Count)

            ' Menu del mes (teorico y real): horizontal, con una columna por dia y el servicio en su seccion.
            Dim menuTeorico = reportes.MenuMes(Fecha.Year, Fecha.Month, real:=False)
            Assert.True(menuTeorico.Horizontal)
            Assert.Equal(1 + 31, menuTeorico.Secciones(0).Columnas.Count)
            Assert.Equal("Almuerzo - General", menuTeorico.Secciones(0).Titulo)
            Assert.True(reportes.MenuMes(Fecha.Year, Fecha.Month, real:=True).Horizontal)

            ' Boleta R-AL: dos tablas (entrada y salida). Explicacion R-AI: la diferencia del inventario, horizontal, con su motivo.
            Dim boleta = reportes.BoletaAjuste(Fecha, Fecha)
            Assert.Equal(2, boleta.Secciones.Count)
            Dim explica = reportes.ExplicacionAjustes(inv)
            Assert.True(explica.Horizontal)
            Assert.Equal("ACE-1L", CStr(explica.Secciones(0).Filas.Single()(0)))

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

    <FactPostgres>
    Public Sub El_registro_de_inventario_permanente_valorizado_sale_en_el_formato_13_1_de_SUNAT()
        Using bd = BaseDatosPrueba.Crear()
            bd.EjecutarAdmin("UPDATE empresa SET identificacion_fiscal = '20100000001' WHERE codigo = 'A'")
            Dim s = bd.Sesion("A")
            Dim stock As New ServicioStock(bd.CadenaAplicacion, s)
            ' Saldo inicial al 30/09: 10 L a S/ 8. En octubre entra 5 L a S/ 10 y salen 4 L a producción.
            stock.Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Apertura, New Date(2026, 9, 30), "AP-1",
                                                       {New LineaDocumentoStock(bd.VarianteAceiteId, U(10D), U(8D))}))
            stock.Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Recepcion, New Date(2026, 10, 1), "RC-1",
                                                       {New LineaDocumentoStock(bd.VarianteAceiteId, U(5D), U(10D))}))
            Call New ServicioAlmacen(bd.CadenaAplicacion, s).SalidaProduccion(bd.A.AlmacenId, New Date(2026, 10, 2),
                                                                             {New LineaSalida With {.VarianteId = bd.VarianteAceiteId, .CantidadBaseU6 = U(4D)}})
            Dim reportes As New ServicioReportes(bd.CadenaAplicacion, s)
            Dim r = reportes.RegistroInventarioPermanente(bd.A.AlmacenId, New Date(2026, 10, 1), New Date(2026, 10, 31))

            Assert.StartsWith("FORMATO 13.1", Dato(r, "Formato"))
            Assert.Equal("10/2026", Dato(r, "Periodo"))
            Assert.Equal("20100000001", Dato(r, "RUC"))
            Assert.Equal("PROMEDIO PONDERADO (MOVIL)", Dato(r, "Metodo de valuacion"))
            Dim sec = r.Secciones.Single()
            Assert.Contains("ACE-A-4L", sec.Titulo)
            Assert.Contains("03 MATERIAS PRIMAS", sec.Titulo)
            Assert.Contains("08 LITROS", sec.Titulo)
            Assert.Equal(3, sec.Filas.Count)
            ' Saldo inicial (tabla 12: 16), compra (02) y salida a producción (10), con saldo corrido al promedio móvil.
            Assert.Equal("SALDO INICIAL|16 SALDO INICIAL|" & U(10D) & "|" & U(80D), String.Join("|", sec.Filas(0)(3), sec.Filas(0)(4), sec.Filas(0)(11), sec.Filas(0)(13)))
            Assert.Equal("02 COMPRA|" & U(5D) & "|" & U(50D) & "|" & U(15D) & "|" & U(130D),
                         String.Join("|", sec.Filas(1)(4), sec.Filas(1)(5), sec.Filas(1)(7), sec.Filas(1)(11), sec.Filas(1)(13)))
            Dim salida = sec.Filas(2)
            Assert.Equal("10 SALIDA A PRODUCCION", salida(4))
            Assert.Equal(U(4D), salida(8))
            Assert.Equal(34.67D, Math.Round(EscalaU6.ADecimal(CLng(salida(10))), 2))      ' 4 L × 8,6667
            Assert.Equal(U(11D), salida(11))
            Assert.Equal(U(130D) - CLng(salida(10)), salida(13))
            Assert.Equal(U(5D), sec.Totales(5))
            Assert.Equal(U(4D), sec.Totales(8))
            Assert.Contains("FORMATO 13.1", r.ACsv())

            ' Sin movimientos ni saldo en el periodo anterior al primer documento: no hay existencias.
            Assert.Empty(reportes.RegistroInventarioPermanente(bd.A.AlmacenId, New Date(2026, 8, 1), New Date(2026, 8, 31)).Secciones)
            Assert.Equal("DATO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(
                Function() reportes.RegistroInventarioPermanente(bd.A.AlmacenId, New Date(2026, 10, 1), New Date(2026, 10, 31), "07")).Codigo)
            Assert.Equal("OPERACION_AJENA", Assert.Throws(Of ReglaNegocioException)(
                Function() New ServicioReportes(bd.CadenaAplicacion, s).RegistroInventarioPermanente(bd.B.AlmacenId, New Date(2026, 10, 1), New Date(2026, 10, 31))).Codigo)
        End Using
    End Sub

    Private Shared Function Dato(r As Reporte, etiqueta As String) As String
        Return r.Encabezado.Single(Function(d) d.Key = etiqueta).Value
    End Function

End Class
