Imports AppSistema.Dominio.Inventario
Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock
Imports AppSistema.Datos

''' <summary>Etapa 6: inventario físico — fotografía, conteo, diferencias sin ajuste, autorización independiente y ajuste único.</summary>
Public Class InventariosTests

    Private Shared ReadOnly Fecha As New Date(2026, 10, 2)

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>Aceite bidón 4 L: 27 L; aceite botella 1 L: 5 L; ambos a S/8 por L. El usuario "almacen" cuenta; el admin autoriza.</summary>
    Private NotInheritable Class Escenario
        Public Bd As BaseDatosPrueba
        Public Admin, Contador As ServicioInventarios
        Public Botella As Long

        Public Sub New(bd As BaseDatosPrueba)
            Me.Bd = bd
            Dim s = bd.Sesion("A")
            Botella = New ServicioCatalogo(bd.CadenaAplicacion, s).CrearVariante(bd.ProductoAceiteId, Nothing, "ACE-1L", "Aceite botella 1 L", "botella", U(1D))
            Call New ServicioStock(bd.CadenaAplicacion, s).Contabilizar(New DocumentoStockNuevo(bd.A.AlmacenId, TipoDocumentoStock.Apertura, Fecha, "AP-1",
                {New LineaDocumentoStock(bd.VarianteAceiteId, U(27D), U(8D)), New LineaDocumentoStock(Botella, U(5D), U(8D))}))
            Call New ServicioAdministracion(bd.CadenaAplicacion, s).CrearUsuario("almacen", "Almacenero", "Almacen-Clave-2026", bd.A.OperacionId, "ALMACEN")
            Admin = New ServicioInventarios(bd.CadenaAplicacion, s)
            Contador = New ServicioInventarios(bd.CadenaAplicacion, bd.Sesion("A", "almacen", "Almacen-Clave-2026"))
        End Sub

        Public Function Linea(inventario As Long, variante As Long) As LineaConteoDto
            Return Admin.Hoja(inventario).Single(Function(l) l.VarianteId = variante)
        End Function
    End Class

    <FactPostgres>
    Public Sub T33_T34_conteo_26_contra_27_da_menos_1_sin_tocar_el_saldo_y_celda_vacia_no_es_cero()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim inv = e.Contador.Abrir(bd.A.AlmacenId, Fecha, "general")
            Dim aceite = e.Linea(inv, bd.VarianteAceiteId)
            e.Contador.RegistrarConteo(aceite.Id, U(6D), U(2D))                 ' 6 bidones de 4 L + 2 L sueltos = 26 L
            Dim l = e.Linea(inv, bd.VarianteAceiteId)
            Assert.Equal(U(26D), l.FisicoU6)
            Assert.Equal(U(-1D), l.DiferenciaU6)
            Assert.Equal(U(-8D), l.ValorDiferenciaU6)
            Assert.Equal("faltante", l.Resultado)
            Assert.Equal(U(27D), bd.SaldoU6(bd.VarianteAceiteId))              ' T33: el saldo no cambia por contar

            Dim botella = e.Linea(inv, e.Botella)
            Assert.Null(botella.FisicoU6)                                        ' T34: pendiente, no cero
            Assert.Equal("sin contar", botella.Resultado)
            Assert.Equal("CONTEO_INCOMPLETO", Assert.Throws(Of ReglaNegocioException)(Sub() e.Contador.CerrarConteo(inv)).Codigo)
            Assert.Equal(1, e.Contador.Resumen(inv).SinContar)
            ' Conteo ciego: no muestra el sistema.
            Assert.All(e.Contador.Hoja(inv, ciego:=True), Sub(x) Assert.Null(x.SistemaU6))
        End Using
    End Sub

    <FactPostgres>
    Public Sub T35_T36_movimientos_bloqueados_al_contar_autorizacion_independiente_y_ajuste_una_sola_vez()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim inv = e.Contador.Abrir(bd.A.AlmacenId, Fecha, "general")
            Dim almacen As New ServicioAlmacen(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim salida = {New LineaSalida With {.VarianteId = e.Botella, .CantidadBaseU6 = U(1D)}}
            Assert.Equal("INVENTARIO_EN_CURSO", Assert.Throws(Of ReglaNegocioException)(Function() almacen.SalidaProduccion(bd.A.AlmacenId, Fecha, salida)).Codigo)   ' T36

            Assert.Equal(2, e.Contador.ImportarConteo(inv, "variante_codigo;envases;parcial" & vbLf & "ACE-A-4L;6;2" & vbLf & "ACE-1L;7;" & vbLf))
            e.Contador.CerrarConteo(inv)
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Sub() e.Contador.Revisar(inv)).Codigo)
            e.Admin.Revisar(inv)

            ' Tras cerrar el conteo se puede mover; el ajuste es por la diferencia (no reemplaza el saldo).
            almacen.SalidaProduccion(bd.A.AlmacenId, Fecha, salida)
            Dim r = e.Admin.AutorizarAjuste(inv, Fecha, "Inventario mensual")
            Assert.Equal(U(8D), r.FaltanteValorU6)
            Assert.Equal(U(16D), r.SobranteValorU6)
            Assert.Equal(U(26D), bd.SaldoU6(bd.VarianteAceiteId))              ' 27 − 1
            Assert.Equal(U(6D), bd.SaldoU6(e.Botella))                          ' 5 + 2 − 1 (salida posterior al conteo)
            Assert.Equal(0L, bd.FilasSinConciliar())

            Assert.Equal("INVENTARIO_CERRADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Admin.AutorizarAjuste(inv, Fecha, "otra vez")).Codigo)   ' T35
            Assert.Equal(2L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM documento_stock WHERE tipo IN ('ajuste_positivo','ajuste_negativo')")))
            Assert.Equal(2L, Convert.ToInt64(bd.Escalar("SELECT count(*) FROM inventario_ajuste")))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Quien_conto_no_autoriza_reconteo_y_rotativo_con_un_solo_inventario_abierto()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)
            Dim inv = e.Admin.Abrir(bd.A.AlmacenId, Fecha, "rotativo", {e.Botella})
            Assert.Single(e.Admin.Hoja(inv))
            Assert.Equal("CODIGO_DUPLICADO", Assert.Throws(Of ReglaNegocioException)(Function() e.Admin.Abrir(bd.A.AlmacenId, Fecha, "general")).Codigo)
            e.Admin.RegistrarConteo(e.Linea(inv, e.Botella).Id, U(4D), Nothing)  ' el admin cuenta
            e.Admin.CerrarConteo(inv)
            e.Admin.Recontar(inv)                                                 ' reconteo
            e.Admin.RegistrarConteo(e.Linea(inv, e.Botella).Id, U(5D), Nothing)
            e.Admin.CerrarConteo(inv)
            e.Admin.Revisar(inv)
            Assert.Equal("APROBACION_NO_INDEPENDIENTE", Assert.Throws(Of ReglaNegocioException)(Function() e.Admin.AutorizarAjuste(inv, Fecha, "Rotativo")).Codigo)
            Dim ex = Assert.ThrowsAny(Of Npgsql.PostgresException)(Sub() bd.EjecutarAdmin($"UPDATE inventario_detalle SET stock_sistema_u6 = 1 WHERE inventario_id = {inv}"))
            Assert.Contains("FOTOGRAFIA_INMUTABLE", ex.MessageText)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Sprint4_ABC_motivo_normalizado_boleta_y_explicacion_de_ajustes()
        Using bd = BaseDatosPrueba.Crear()
            Dim e As New Escenario(bd)

            ' ABC: el consumo de la botella (1 L) la hace A; el bidón sin consumo queda C.
            Dim almacen As New ServicioAlmacen(bd.CadenaAplicacion, bd.Sesion("A"))
            almacen.SalidaProduccion(bd.A.AlmacenId, Fecha, {New LineaSalida With {.VarianteId = e.Botella, .CantidadBaseU6 = U(1D)}})
            Dim clases = e.Admin.ClasificarAbc(bd.A.AlmacenId, Fecha, Fecha)
            Assert.Equal("A", clases.Single(Function(x) x.VarianteId = e.Botella).Clase)
            Assert.Equal("C", clases.Single(Function(x) x.VarianteId = bd.VarianteAceiteId).Clase)

            ' Motivo fuera de la lista: se rechaza antes de tocar el inventario.
            Dim inv = e.Contador.Abrir(bd.A.AlmacenId, Fecha, "general")
            Assert.Equal("DATO_INVALIDO", Assert.Throws(Of ReglaNegocioException)(Function() e.Admin.AutorizarAjusteNormalizado(inv, Fecha, "INVENTADO", "x", Nothing)).Codigo)
            Assert.Throws(Of ReglaNegocioException)(Function() e.Admin.AutorizarAjusteNormalizado(inv, Fecha, MotivosAjuste.ErrorConteo, "   ", Nothing))

            Assert.Equal(2, e.Contador.ImportarConteo(inv, "variante_codigo;envases;parcial" & vbLf & "ACE-A-4L;6;2" & vbLf & "ACE-1L;7;" & vbLf))
            e.Contador.CerrarConteo(inv)
            e.Admin.Revisar(inv)
            e.Admin.AutorizarAjusteNormalizado(inv, Fecha, MotivosAjuste.ErrorConteo, "Conteo mal digitado", "Planilla 12")

            Assert.Equal(MotivosAjuste.ErrorConteo, Convert.ToString(bd.Escalar("SELECT min(motivo_codigo) FROM inventario_ajuste WHERE documento_soporte = 'Planilla 12'")))
            Assert.Equal("Conteo mal digitado", Convert.ToString(bd.Escalar("SELECT min(explicacion) FROM inventario_ajuste WHERE documento_soporte = 'Planilla 12'")))

            Dim boleta = New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A")).BoletaAjustes(inv)
            Assert.Equal("Boleta de ajuste de inventario", boleta.Titulo)
            Assert.Equal(2, boleta.Secciones(0).Filas.Count)
            Assert.Equal("Planilla 12", CStr(boleta.Secciones(0).Filas(0)(4)))

            Dim explicacion = New ServicioReportes(bd.CadenaAplicacion, bd.Sesion("A")).ExplicacionAjustes(Fecha, Fecha)
            Assert.NotEmpty(explicacion.Secciones(0).Filas)
            Assert.Equal("Error de conteo previo", CStr(explicacion.Secciones(0).Filas(0)(0)))
        End Using
    End Sub

End Class
