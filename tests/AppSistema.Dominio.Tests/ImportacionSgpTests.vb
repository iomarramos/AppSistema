Imports System.IO
Imports Xunit
Imports AppSistema.Dominio.Importacion

Public Class ImportacionSgpTests

    Private Const Cabecera As String = "pro_nombre" & vbTab & "pro_coduni" & vbTab & "pro_facing"

    Private Shared Function Uno(nombre As String, codUni As Integer, factor As String) As ProductoSgp
        Dim r = ConversorSgp.Convertir(Cabecera & vbLf & nombre & vbTab & codUni & vbTab & factor & vbLf)
        Assert.Empty(r.Errores)
        Return r.Productos.Single()
    End Function

    <Theory>
    <InlineData("ACEITE VEGETAL CIELO 5 LT", 4, "5", "L", "BIDON")>
    <InlineData("ARVEJA VERDE PARTIDA CANTA CLARO BOLSA 500 GR", 8, "0.5", "KG", "BOLSA")>
    <InlineData("LECHE CONDENSADA GLORIA 393 GR", 24, "0.395", "KG", "LATA")>
    <InlineData("ARROZ EXTRA PARBOILED", 23, "1", "KG", "KILOGRAMO")>
    <InlineData("CAJA CHICA - YEMA DE ALFALFA", 1, "1", "KG", "KILOGRAMO")>
    <InlineData("CAJA CHICA - VINO BLANCO COUSIÑO", 26, "1", "L", "LITRO")>
    <InlineData("INFUSION MANZANILLA DEL VALLE 100 SOBRES", 10, "100", "UND", "CAJA")>
    <InlineData("AZUCAR RUBIA PERSONAL ONZA PAQ 1 MILLAR", 38, "1000", "UND", "SACHET")>
    <InlineData("PANETON DONOFRIO CAJA 880 GR", 45, "1", "UND", "UNIDAD")>
    <InlineData("JABÓN LIQUIDO P/MANOS SPARTAN 6X4 LITROS NEUTRO", 10, "24", "L", "CAJA")>
    <InlineData("CONTENEDOR DE BAGAZO J1 #5 X 250 UND", 31, "250", "UND", "PAQUETE")>
    <InlineData("DM 500 H PLUS FP SPARTAN 3.875 LITROS", 19, "3.875", "L", "GALON")>
    Public Sub Deduce_unidad_base_y_presentacion_sin_observacion(nombre As String, codUni As Integer, factor As String, unidad As String, presentacion As String)
        Dim p = Uno(nombre, codUni, factor)
        Assert.Equal(unidad, p.UnidadBase)
        Assert.Equal(presentacion, p.Presentacion)
        Assert.Equal("", p.Observacion)
    End Sub

    <Theory>
    <InlineData(3, "GRANO")>
    <InlineData(6, "GRAMO")>
    <InlineData(20, "GRAMO")>
    <InlineData(28, "MILLAR")>
    <InlineData(33, "PAQUETE")>
    <InlineData(37, "PAQUETE")>
    <InlineData(1, "KILOGRAMO")>
    <InlineData(99, "PRES-SGP-99")>
    Public Sub Presentaciones_confirmadas_por_el_usuario(codUni As Integer, nombre As String)
        Assert.Equal(nombre, ConversorSgp.NombrePresentacion(codUni))
    End Sub

    <Theory>
    <InlineData("ATUN TROZOS FLORIDA 140 GR", 24, "0.17", "KG")>
    <InlineData("PAPA SECA LA SERRANITA 3 KG", 8, "5", "KG")>
    <InlineData("LEJIA SAPOLIO 3.785 ML", 9, "3.785", "L")>
    <InlineData("HUEVO ROSADO BANDEJA X 15 UNID", 45, "0.066667", "UND")>
    <InlineData("TENEDOR BLANCO DE PLASTICO DESCARTABLE D81110001 50 UND", 31, "100", "UND")>
    Public Sub Factor_que_el_nombre_contradice_se_carga_con_observacion(nombre As String, codUni As Integer, factor As String, unidad As String)
        Dim p = Uno(nombre, codUni, factor)
        Assert.Equal(unidad, p.UnidadBase)
        Assert.Equal(factor, p.FactorTexto)
        Assert.NotEqual("", p.Observacion)
    End Sub

    <Fact>
    Public Sub Repetidos_identicos_una_vez_y_mismo_nombre_con_otra_presentacion_se_distingue()
        Dim r = ConversorSgp.Convertir(Cabecera & vbLf &
                                       "CAJA CHICA - TOMATE CHERRY" & vbTab & "31" & vbTab & "1" & vbLf &
                                       "CAJA CHICA - TOMATE CHERRY" & vbTab & "23" & vbTab & "1" & vbLf &
                                       "CAJA CHICA - TOMATE CHERRY" & vbTab & "31" & vbTab & "1" & vbLf &
                                       "SAL  DE  COCINA" & vbTab & "x" & vbTab & "1" & vbLf)
        Assert.Equal(1, r.RepetidasIdenticas)
        Assert.Equal("PRD00001,PRD00002", String.Join(",", r.Productos.Select(Function(p) p.Codigo)))
        Assert.Equal("CAJA CHICA - TOMATE CHERRY (PAQUETE x 1 UND)", r.Productos(0).Nombre)
        Assert.Equal("CAJA CHICA - TOMATE CHERRY (KILOGRAMO x 1 KG)", r.Productos(1).Nombre)
        Assert.Equal("CAJA CHICA", r.Productos(0).Categoria)
        Assert.Equal(5, r.Errores.Single().Numero)
    End Sub

    <Fact>
    Public Sub El_csv_generado_lo_acepta_el_lector_del_catalogo_con_empaque_minimo_1()
        Dim r = ConversorSgp.Convertir(Cabecera & vbLf & "MAYONESA BASE EMIC 3.8 KG" & vbTab & "8" & vbTab & "3.8" & vbLf &
                                       "SALSA ""ESPECIAL""; PICANTE 1 KG" & vbTab & "8" & vbTab & "1" & vbLf)
        Dim l = LectorCsvCatalogo.Leer(ConversorSgp.GenerarCsvCatalogo(r))
        Assert.True(l.EsValida, String.Join(" | ", l.Errores))
        Dim f = l.Filas(0)
        Assert.Equal(3800000L, f.ContenidoPorEnvaseU6)
        Assert.Equal("BOLSA", f.EmpaqueCodigo)
        Assert.Equal("BOLSA x 3.8 KG", f.EmpaqueDescripcion)
        Assert.Equal(1L, f.EnvasesPorEmpaque) : Assert.Equal(1L, f.Minimo) : Assert.Equal(1L, f.Multiplo)
        Assert.Equal("SALSA ""ESPECIAL""; PICANTE 1 KG", l.Filas(1).ProductoDescripcion)
    End Sub

    <Fact>
    Public Sub Listado_real_del_SGP_se_convierte_completo()
        Dim dir = New DirectoryInfo(AppContext.BaseDirectory)
        Do While dir IsNot Nothing AndAlso Not File.Exists(Path.Combine(dir.FullName, "datos", "sgp", "productos_sgp_original.tsv"))
            dir = dir.Parent
        Loop
        Assert.NotNull(dir)
        Dim texto = File.ReadAllText(Path.Combine(dir.FullName, "datos", "sgp", "productos_sgp_original.tsv"))
        Dim r = ConversorSgp.Convertir(texto)
        Assert.Empty(r.Errores)
        Assert.Equal(4174, r.Productos.Count + r.RepetidasIdenticas)
        Dim l = LectorCsvCatalogo.Leer(ConversorSgp.GenerarCsvCatalogo(r))
        Assert.True(l.EsValida, String.Join(" | ", l.Errores.Take(5)))
        Assert.Equal(r.Productos.Count, l.Filas.Count)
    End Sub

End Class
