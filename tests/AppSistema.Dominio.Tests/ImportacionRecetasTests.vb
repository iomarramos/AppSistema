Imports System.IO
Imports Xunit
Imports AppSistema.Dominio.Importacion

Public Class ImportacionRecetasTests

    Private Const Cabecera As String = "receta_codigo;receta_nombre;categoria;fuente;rendimiento;ingrediente;unidad;cantidad;tecnica;instrucciones"

    <Fact>
    Public Sub Agrupa_ingredientes_por_receta_y_acepta_instrucciones_de_varias_lineas()
        Dim csv = Cabecera & vbLf &
                  "F1;Arroz blanco;GUARN;FICHA;1;ACEITE VEGETAL;L;0.006;;""1. Calentar; dorar." & vbLf & "2. Servir""" & vbLf &
                  "F1;Arroz blanco;GUARN;FICHA;1;arroz  extra;KG;0.12;Lavar;" & vbLf &
                  "F2;Limonada;BEB;FICHA;1;AGUA PARA RECETA;L;0.22;;" & vbLf
        Dim r = LectorCsvRecetas.Leer(csv)
        Assert.True(r.EsValida, String.Join(" | ", r.Errores))
        Assert.Equal(2, r.Recetas.Count)
        Assert.Equal("ARROZ BLANCO", r.Recetas(0).Nombre)
        Assert.Equal("1. Calentar; dorar." & vbLf & "2. Servir", r.Recetas(0).Instrucciones)
        Assert.Equal("ARROZ EXTRA", r.Recetas(0).Ingredientes(1).Nombre)
        Assert.Equal(120000L, r.Recetas(0).Ingredientes(1).CantidadU6)
        Assert.Equal("Lavar", r.Recetas(0).Ingredientes(1).Tecnica)
    End Sub

    <Fact>
    Public Sub Rechaza_filas_separadas_unidad_invalida_repetidos_y_unidades_distintas_del_mismo_ingrediente()
        Dim csv = Cabecera & vbLf &
                  "F1;A;;;1;SAL;KG;0.002;;" & vbLf &
                  "F2;B;;;1;SAL;L;0.002;;" & vbLf &
                  "F1;A;;;1;AJO;KG;0.002;;" & vbLf &
                  "F3;C;;;1;AZUCAR;PQT;0.01;;" & vbLf &
                  "F4;D;;;0;AZUCAR;KG;0.01;;" & vbLf &
                  "F5;E;;;1;AZUCAR;KG;0.01;;" & vbLf &
                  "F5;E;;;1;AZUCAR;KG;0.02;;" & vbLf
        Dim r = LectorCsvRecetas.Leer(csv)
        Assert.Equal("3,4,5,6,8", String.Join(",", r.Errores.Select(Function(e) e.Numero)))
        Assert.Contains("en la fila 2 en KG", r.Errores(0).Mensaje)
        Assert.Contains("deben ir juntas", r.Errores(1).Mensaje)
        Assert.Contains("KG, L o UND", r.Errores(2).Mensaje)
        Assert.Contains("rendimiento", r.Errores(3).Mensaje)
        Assert.Contains("ya aparece", r.Errores(4).Mensaje)
    End Sub

    <Fact>
    Public Sub El_archivo_normalizado_del_repositorio_se_lee_completo()
        Dim dir = New DirectoryInfo(AppContext.BaseDirectory)
        Do While Not File.Exists(Path.Combine(dir.FullName, "datos", "recetas", "recetas_normalizadas.csv"))
            dir = dir.Parent
        Loop
        Dim r = LectorCsvRecetas.Leer(File.ReadAllText(Path.Combine(dir.FullName, "datos", "recetas", "recetas_normalizadas.csv")))
        Assert.True(r.EsValida, String.Join(" | ", r.Errores.Take(5)))
        Assert.Equal(946, r.Recetas.Count)
        Assert.Equal(8874, r.Recetas.Sum(Function(x) x.Ingredientes.Count))
    End Sub

End Class
