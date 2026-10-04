Imports System.Globalization
Imports System.IO
Imports System.Text
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

''' <summary>Resultado de importar el plan del SGP: cuántas filas se guardaron y qué no se pudo enlazar.</summary>
Public NotInheritable Class ResultadoPlanSgp
    Public Property CodigosReceta As Integer
    Public Property CodigosProducto As Integer
    Public Property RecetasSinEnlace As Integer
    Public Property ProductosSinEnlace As Integer
    Public Property PlanDia As Integer
    Public Property PlanPlato As Integer
    Public Property Requisicion As Integer
    Public Property ComparativoDia As Integer
    Public Property ComparativoTotal As Integer
    Public Property CostoPisoTecho As Integer
    Public Property Preparaciones As Integer
    Public ReadOnly Property Problemas As New List(Of String)

    Public Overrides Function ToString() As String
        Return $"{PlanDia} dias-servicio, {PlanPlato} platos, {Requisicion} lineas de requisicion, {ComparativoDia} comparativos por dia, " &
               $"{ComparativoTotal} totales, {CostoPisoTecho} piso y techo, {Preparaciones} pasos de preparacion; " &
               $"codigos SGP: {CodigosReceta} recetas ({RecetasSinEnlace} sin receta en el sistema), {CodigosProducto} productos ({ProductosSinEnlace} sin producto)"
    End Function
End Class

''' <summary>
''' Importa el plan del SGP de la operación de la sesión (Excel de datos/plan_real convertido a CSV): plan teórico y real por
''' día y plato, requisición, comparativos, costo piso y techo y preparaciones (V023). Reimportar reemplaza los datos de la
''' operación; los códigos del SGP se actualizan. Los códigos sin enlace se guardan con la columna vacía.
''' </summary>
Public NotInheritable Class ServicioPlanSgp
    Inherits ServicioConSesion

    Public Const ArchivoCodigosRecetas As String = "codigos_sgp_recetas.csv"
    Public Const ArchivoCodigosProductos As String = "codigos_sgp_productos.csv"
    Public Const ArchivoMenuDias As String = "menu_dias.csv"
    Public Const ArchivoMenuPlanificado As String = "menu_planificado.csv"
    Public Const ArchivoRequisicion As String = "requisicion.csv"
    Public Const ArchivoComparativoCostos As String = "comparativo_costos.csv"
    Public Const ArchivoComparativoTotales As String = "comparativo_totales.csv"
    Public Const ArchivoCostoPisoTecho As String = "costo_piso_techo.csv"
    Public Const ArchivoPreparacion As String = "preparacion_recetas.csv"

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    ''' <summary>Importa todos los archivos de la carpeta del plan del SGP (datos\plan_real).</summary>
    Public Function Importar(carpeta As String) As ResultadoPlanSgp
        Dim codigosRecetas = Filas(Path.Combine(carpeta, ArchivoCodigosRecetas))
        Dim codigosProductos = Filas(Path.Combine(carpeta, ArchivoCodigosProductos))
        Dim menuDias = Filas(Path.Combine(carpeta, ArchivoMenuDias))
        Dim menu = Filas(Path.Combine(carpeta, ArchivoMenuPlanificado))
        Dim requisicion = Filas(Path.Combine(carpeta, ArchivoRequisicion))
        Dim comparativoDia = Filas(Path.Combine(carpeta, ArchivoComparativoCostos))
        Dim comparativoTotal = Filas(Path.Combine(carpeta, ArchivoComparativoTotales))
        Dim pisoTecho = Filas(Path.Combine(carpeta, ArchivoCostoPisoTecho))
        Dim preparaciones = Filas(Path.Combine(carpeta, ArchivoPreparacion))

        Return EnTransaccion(Permisos.CatalogoImportar,
            Function(u)
                Dim r As New ResultadoPlanSgp()
                Dim operacion = Sesion.OperacionId
                Dim empresa = Sesion.EmpresaId

                ' Recetas y productos de AppSistema, por su código (el enlace que dejó el conversor).
                Dim recetasApp = u.Consultar("SELECT codigo, id FROM receta", Function(rd) (Codigo:=rd.GetString(0), Id:=rd.GetInt64(1))).
                                   ToDictionary(Function(x) x.Codigo, Function(x) x.Id)
                ' El código PRD del SGP es la variante (presentación comprable); el ING es el producto base.
                Dim productosApp = u.Consultar("SELECT codigo, id FROM variante_producto", Function(rd) (Codigo:=rd.GetString(0), Id:=rd.GetInt64(1))).
                                     ToDictionary(Function(x) x.Codigo, Function(x) x.Id)

                ' Códigos del SGP: se actualizan (son de la empresa, no de la operación).
                For Each f In codigosRecetas
                    Dim codigoApp = f("receta_codigo_app")
                    Dim recetaId As Object = DBNull.Value
                    If codigoApp <> "" Then
                        If recetasApp.ContainsKey(codigoApp) Then recetaId = recetasApp(codigoApp) Else r.Problemas.Add($"Receta SGP {f("receta_codigo_sgp")}: el codigo {codigoApp} no existe en AppSistema")
                    End If
                    If recetaId Is DBNull.Value Then r.RecetasSinEnlace += 1
                    u.Ejecutar("INSERT INTO sgp_codigo_receta(empresa_id, codigo_sgp, nombre_sgp, receta_id) VALUES (@e, @c, @n, @r) " &
                               "ON CONFLICT (empresa_id, codigo_sgp) DO UPDATE SET nombre_sgp = EXCLUDED.nombre_sgp, receta_id = EXCLUDED.receta_id",
                               "e", empresa, "c", f("receta_codigo_sgp"), "n", f("receta"), "r", If(recetaId Is DBNull.Value, Nothing, recetaId))
                    r.CodigosReceta += 1
                Next

                For Each f In codigosProductos
                    Dim codigoApp = f("producto_codigo_app")
                    Dim productoId As Object = DBNull.Value
                    If codigoApp <> "" Then
                        If productosApp.ContainsKey(codigoApp) Then productoId = productosApp(codigoApp) Else r.Problemas.Add($"Producto SGP {f("producto_codigo_sgp")}: el codigo {codigoApp} no existe en AppSistema")
                    End If
                    If productoId Is DBNull.Value Then r.ProductosSinEnlace += 1
                    u.Ejecutar("INSERT INTO sgp_codigo_producto(empresa_id, codigo_sgp, descripcion_sgp, unidad_bulto, unidad_despacho, variante_id) " &
                               "VALUES (@e, @c, @d, @b, @s, @p) ON CONFLICT (empresa_id, codigo_sgp) DO UPDATE SET descripcion_sgp = EXCLUDED.descripcion_sgp, " &
                               "unidad_bulto = EXCLUDED.unidad_bulto, unidad_despacho = EXCLUDED.unidad_despacho, variante_id = EXCLUDED.variante_id",
                               "e", empresa, "c", f("producto_codigo_sgp"), "d", f("descripcion"), "b", NuloSiVacio(f("unidad_bulto")),
                               "s", NuloSiVacio(f("unidad_despacho")), "p", If(productoId Is DBNull.Value, Nothing, productoId))
                    r.CodigosProducto += 1
                Next

                ' La operación se reemplaza completa (hijos primero).
                For Each tabla In {"sgp_requisicion", "sgp_plan_plato", "sgp_plan_dia", "sgp_comparativo_dia", "sgp_comparativo_total", "sgp_costo_piso_techo"}
                    u.Ejecutar($"DELETE FROM {tabla} WHERE operacion_id = @o", "o", operacion)
                Next

                For Each f In menuDias
                    u.Ejecutar("INSERT INTO sgp_plan_dia(empresa_id, operacion_id, nivel, servicio, fecha, comensales, comensales_2, costo_minuta_dia_u6) " &
                               "VALUES (@e, @o, @n, @s, @f, @c, @c2, @k)",
                               "e", empresa, "o", operacion, "n", f("nivel"), "s", f("servicio"), "f", Fecha(f("fecha")),
                               "c", Entero(f("comensales")), "c2", EnteroONulo(f("comensales_2")), "k", U6(f("costo_minuta_dia")))
                    r.PlanDia += 1
                Next

                For Each f In menu
                    Dim recetaSgp = f("receta_codigo_sgp")
                    u.Ejecutar("INSERT INTO sgp_plan_plato(empresa_id, operacion_id, nivel, servicio, fecha, orden, estructura, receta_codigo_sgp, " &
                               "raciones_u6, porcentaje_bp, costo_racion_u6, marca) VALUES (@e, @o, @n, @s, @f, @ord, @est, @rc, @rac, @pct, @cr, @m)",
                               "e", empresa, "o", operacion, "n", f("nivel"), "s", f("servicio"), "f", Fecha(f("fecha")), "ord", Entero(f("orden")),
                               "est", f("estructura"), "rc", recetaSgp, "rac", U6(f("raciones")), "pct", Basispuntos(f("porcentaje")),
                               "cr", U6(f("costo_racion")), "m", NuloSiVacio(f("marca")))
                    r.PlanPlato += 1
                Next

                For Each f In requisicion
                    u.Ejecutar("INSERT INTO sgp_requisicion(empresa_id, operacion_id, regimen, servicio, fecha, estructura, receta_codigo_sgp, raciones_u6, " &
                               "producto_codigo_sgp, cantidad_bruta_racion_u6, cantidad_bulto_u6, unidad_bulto, cantidad_despacho_u6, unidad_despacho) " &
                               "VALUES (@e, @o, @rg, @s, @f, @est, @rc, @rac, @pc, @bruta, @bulto, @ub, @desp, @ud)",
                               "e", empresa, "o", operacion, "rg", f("regimen"), "s", f("servicio"), "f", Fecha(f("fecha")), "est", f("estructura"),
                               "rc", f("receta_codigo_sgp"), "rac", U6(f("raciones")), "pc", f("producto_codigo_sgp"),
                               "bruta", U6(f("cantidad_bruta_racion")), "bulto", U6(f("cantidad_bulto")), "ub", NuloSiVacio(f("unidad_bulto")),
                               "desp", U6(f("cantidad_despacho")), "ud", NuloSiVacio(f("unidad_despacho")))
                    r.Requisicion += 1
                Next

                For Each f In comparativoDia
                    u.Ejecutar("INSERT INTO sgp_comparativo_dia(empresa_id, operacion_id, contrato, regimen, servicio, fecha, " &
                               "teorico_costo_bandeja_u6, teorico_raciones_u6, teorico_costo_total_u6, real_costo_bandeja_u6, real_raciones_u6, real_costo_total_u6, " &
                               "desviacion_plan_u6, realizado_costo_bandeja_u6, realizado_raciones_u6, realizado_costo_total_u6, desviacion_realizado_u6) " &
                               "VALUES (@e, @o, @ct, @rg, @s, @f, @tcb, @tr, @tct, @rcb, @rr, @rct, @dp, @qcb, @qr, @qct, @dr)",
                               "e", empresa, "o", operacion, "ct", NuloSiVacio(f("contrato")), "rg", f("regimen"), "s", f("servicio"), "f", Fecha(f("fecha")),
                               "tcb", U6(f("teorico_costo_bandeja")), "tr", U6(f("teorico_raciones")), "tct", U6(f("teorico_costo_total")),
                               "rcb", U6(f("real_costo_bandeja")), "rr", U6(f("real_raciones")), "rct", U6(f("real_costo_total")),
                               "dp", U6(f("desviacion_plan")), "qcb", U6(f("realizado_costo_bandeja")), "qr", U6(f("realizado_raciones")),
                               "qct", U6(f("realizado_costo_total")), "dr", U6(f("desviacion_realizado")))
                    r.ComparativoDia += 1
                Next

                For Each f In comparativoTotal
                    u.Ejecutar("INSERT INTO sgp_comparativo_total(empresa_id, operacion_id, total, regimen, servicio, " &
                               "teorico_costo_bandeja_u6, teorico_raciones_u6, teorico_costo_total_u6, real_costo_bandeja_u6, real_raciones_u6, real_costo_total_u6, " &
                               "desviacion_plan_u6, realizado_costo_bandeja_u6, realizado_raciones_u6, realizado_costo_total_u6, desviacion_realizado_u6) " &
                               "VALUES (@e, @o, @t, @rg, @s, @tcb, @tr, @tct, @rcb, @rr, @rct, @dp, @qcb, @qr, @qct, @dr)",
                               "e", empresa, "o", operacion, "t", f("total"), "rg", f("regimen"), "s", f("servicio"),
                               "tcb", U6(f("teorico_costo_bandeja")), "tr", U6(f("teorico_raciones")), "tct", U6(f("teorico_costo_total")),
                               "rcb", U6(f("real_costo_bandeja")), "rr", U6(f("real_raciones")), "rct", U6(f("real_costo_total")),
                               "dp", U6(f("desviacion_plan")), "qcb", U6(f("realizado_costo_bandeja")), "qr", U6(f("realizado_raciones")),
                               "qct", U6(f("realizado_costo_total")), "dr", U6(f("desviacion_realizado")))
                    r.ComparativoTotal += 1
                Next

                For Each f In pisoTecho
                    u.Ejecutar("INSERT INTO sgp_costo_piso_techo(empresa_id, operacion_id, nivel, servicio, mes, componentes, costo_piso_u6, costo_medio_u6, " &
                               "costo_techo_u6, dia_mas_barato_u6, dia_mas_caro_u6, dias_dentro) VALUES (@e, @o, @n, @s, @m, @c, @p, @md, @t, @b, @x, @d)",
                               "e", empresa, "o", operacion, "n", f("nivel"), "s", f("servicio"), "m", f("mes"), "c", Entero(f("componentes")),
                               "p", U6(f("costo_piso")), "md", U6(f("costo_medio")), "t", U6(f("costo_techo")),
                               "b", U6(f("dia_mas_barato")), "x", U6(f("dia_mas_caro")), "d", f("dias_dentro"))
                    r.CostoPisoTecho += 1
                Next

                ' Preparaciones: por receta, el orden es el de llegada del archivo.
                Dim ordenes As New Dictionary(Of String, Long)()
                For Each f In preparaciones
                    Dim receta = f("receta_codigo_sgp")
                    ordenes(receta) = If(ordenes.ContainsKey(receta), ordenes(receta), 0L) + 1L
                    u.Ejecutar("INSERT INTO sgp_preparacion(empresa_id, receta_codigo_sgp, orden, paso) VALUES (@e, @c, @o, @p) " &
                               "ON CONFLICT (empresa_id, receta_codigo_sgp, orden) DO UPDATE SET paso = EXCLUDED.paso",
                               "e", empresa, "c", receta, "o", ordenes(receta), "p", f("paso"))
                    r.Preparaciones += 1
                Next

                Return r
            End Function)
    End Function

    ' ---------- Lectura de CSV (separador ;, encabezado en la primera línea) ----------

    Private Shared Function Filas(archivo As String) As List(Of Dictionary(Of String, String))
        If Not File.Exists(archivo) Then Throw New ReglaNegocioException("ARCHIVO_FALTANTE", "Falta el archivo " & archivo)
        Dim lineas = File.ReadAllText(archivo, Encoding.UTF8).Replace(vbCr, "").Split(ChrW(10)).Where(Function(l) l.Trim() <> "").ToList()
        Dim cabecera = lineas(0).TrimStart(ChrW(&HFEFF)).Split(";"c).Select(Function(c) c.Trim()).ToArray()
        Dim registros As New List(Of Dictionary(Of String, String))()
        For Each linea In lineas.Skip(1)
            Dim campos = linea.Split(";"c)
            Dim fila As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            For i = 0 To cabecera.Length - 1
                fila(cabecera(i)) = If(i < campos.Length, campos(i).Trim(), "")
            Next
            registros.Add(fila)
        Next
        Return registros
    End Function

    Private Shared Function NuloSiVacio(texto As String) As Object
        Return If(String.IsNullOrWhiteSpace(texto), CObj(Nothing), CObj(texto))
    End Function

    Private Shared Function Fecha(texto As String) As Date
        Return Date.ParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture)
    End Function

    Private Shared Function Decimal_(texto As String) As Decimal
        Return Decimal.Parse(texto, NumberStyles.AllowLeadingSign Or NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)
    End Function

    ''' <summary>Cantidad o dinero a _u6. Vacío = 0 (el SGP muestra 0 cuando no hay realizado).</summary>
    Private Shared Function U6(texto As String) As Long
        If String.IsNullOrWhiteSpace(texto) Then Return 0
        Return EscalaU6.DesdeDecimal(Decimal_(texto))
    End Function

    Private Shared Function Entero(texto As String) As Long
        Return CLng(Math.Round(Decimal_(texto), 0, MidpointRounding.AwayFromZero))
    End Function

    Private Shared Function EnteroONulo(texto As String) As Object
        If String.IsNullOrWhiteSpace(texto) Then Return Nothing
        Return Entero(texto)
    End Function

    ''' <summary>Porcentaje del SGP (fracción, p. ej. 0,5) a puntos básicos (5000). Vacío = sin porcentaje (teórico).</summary>
    Private Shared Function Basispuntos(texto As String) As Object
        If String.IsNullOrWhiteSpace(texto) Then Return Nothing
        Return CLng(Math.Round(Decimal_(texto) * 10000D, 0, MidpointRounding.AwayFromZero))
    End Function

End Class
