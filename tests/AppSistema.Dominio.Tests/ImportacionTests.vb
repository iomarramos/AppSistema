Imports Xunit
Imports AppSistema.Dominio.Importacion
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

Public Class ImportacionTests

    Private Const Cabecera As String =
        "producto_codigo;producto_descripcion;unidad_base;categoria;variante_codigo;marca;descripcion_comercial;tipo_envase;contenido_por_envase;empaque_codigo;empaque_descripcion;envases_por_empaque;minimo;multiplo"

    <Fact>
    Public Sub Lee_aceite_A_con_caja_y_aceite_B_sin_empaque()
        Dim csv = Cabecera & vbLf &
                  "ACE;Aceite vegetal;L;ABARROTES;ACE-A-4L;Marca A;Aceite A 4 L;bidon;4;CAJA4;Caja 4 x 4 L;4;1;1" & vbLf &
                  "ACE;Aceite vegetal;L;ABARROTES;ACE-B-5L;Marca B;Aceite B 5 L;bidon;5;;;;;" & vbLf
        Dim r = LectorCsvCatalogo.Leer(csv)
        Assert.True(r.EsValida, String.Join(" | ", r.Errores))
        Assert.Equal(2, r.Filas.Count)
        Assert.Equal(4 * EscalaU6.Factor, r.Filas(0).ContenidoPorEnvaseU6)
        Assert.True(r.Filas(0).TieneEmpaque)
        Assert.Equal(4L, r.Filas(0).EnvasesPorEmpaque)
        Assert.False(r.Filas(1).TieneEmpaque)
    End Sub

    <Fact>
    Public Sub Acepta_coma_como_separador_y_decimales_entre_comillas()
        Dim csv = "producto_codigo,producto_descripcion,unidad_base,variante_codigo,descripcion_comercial,tipo_envase,contenido_por_envase" & vbCrLf &
                  "ARZ,""Arroz, extra"",kg,ARZ-50,Arroz saco 50 kg,saco,""0,5""" & vbCrLf
        Dim r = LectorCsvCatalogo.Leer(csv)
        Assert.True(r.EsValida, String.Join(" | ", r.Errores))
        Assert.Equal("Arroz, extra", r.Filas(0).ProductoDescripcion)
        Assert.Equal(500000L, r.Filas(0).ContenidoPorEnvaseU6)
    End Sub

    <Fact>
    Public Sub Falta_una_columna_obligatoria()
        Dim r = LectorCsvCatalogo.Leer("producto_codigo;producto_descripcion" & vbLf & "A;B")
        Assert.False(r.EsValida)
        Assert.Contains("Faltan columnas obligatorias", r.Errores(0).Mensaje)
    End Sub

    <Fact>
    Public Sub Reporta_cada_fila_invalida_con_su_numero()
        Dim csv = Cabecera & vbLf &
                  "ACE;Aceite;L;;ACE-A-4L;;Aceite A;bidon;cuatro;;;;;" & vbLf &
                  "ACE;Aceite;L;;ACE-C;;Aceite C;bidon;1.000,5;;;;;" & vbLf &
                  ";Sin codigo;L;;X;;X;bidon;1;;;;;" & vbLf &
                  "ARZ;Arroz;kg;;ARZ-1;;Arroz;saco;50;SACO;Saco;0;1;1" & vbLf
        Dim r = LectorCsvCatalogo.Leer(csv)
        Assert.Equal("2,3,4,5", String.Join(",", r.Errores.Select(Function(e) e.Numero)))
        Assert.Contains("contenido_por_envase", r.Errores(0).Mensaje)
        Assert.Contains("producto_codigo", r.Errores(2).Mensaje)
        Assert.Contains("envases_por_empaque", r.Errores(3).Mensaje)
        Assert.Empty(r.Filas)
    End Sub

    <Fact>
    Public Sub Contradiccion_dentro_del_archivo_se_rechaza_y_la_repeticion_identica_no()
        Dim fila = "ACE;Aceite;L;;ACE-A-4L;;Aceite A;bidon;4;;;;;"
        Dim csv = Cabecera & vbLf & fila & vbLf & fila & vbLf & "ACE;Aceite;L;;ACE-A-4L;;Aceite A;bidon;5;;;;;" & vbLf
        Dim r = LectorCsvCatalogo.Leer(csv)
        Assert.Equal(2, r.Filas.Count)
        Assert.Single(r.Errores)
        Assert.Equal(4, r.Errores(0).Numero)
        Assert.Contains("ya aparece en la fila 2", r.Errores(0).Mensaje)
    End Sub

    <Fact>
    Public Sub Columna_desconocida_se_rechaza()
        Dim r = LectorCsvCatalogo.Leer(Cabecera & ";precio" & vbLf & "x")
        Assert.Contains("Columnas no reconocidas: precio", r.Errores(0).Mensaje)
    End Sub

    <Theory>
    <InlineData("corta1")>
    <InlineData("sinnumerosaaaa")>
    <InlineData("1234567890")>
    Public Sub Clave_debil_se_rechaza(clave As String)
        Dim ex = Assert.Throws(Of ReglaNegocioException)(Sub() PoliticaClave.Validar(clave))
        Assert.Equal("CLAVE_DEBIL", ex.Codigo)
    End Sub

    <Fact>
    Public Sub Roles_base_admin_tiene_todo_y_cocina_no_aprueba()
        Dim admin = RolesBase.Todos.Single(Function(r) r.Codigo = RolesBase.Administrador)
        Assert.Equal(Permisos.Todos.Count, admin.Permisos.Count)
        Assert.Equal("CATALOGO_VER,MENUS_VER,RECETAS_EDITAR,MINUTAS_EDITAR", String.Join(",", RolesBase.Todos.Single(Function(r) r.Codigo = "COCINA").Permisos))
    End Sub

End Class
