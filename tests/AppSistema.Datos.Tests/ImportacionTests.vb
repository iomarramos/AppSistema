Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Datos

Public Class ImportacionCatalogoTests

    Private Const Cabecera As String =
        "producto_codigo;producto_descripcion;unidad_base;categoria;variante_codigo;marca;descripcion_comercial;tipo_envase;contenido_por_envase;empaque_codigo;empaque_descripcion;envases_por_empaque;minimo;multiplo"

    Private Shared ReadOnly Archivo As String = Cabecera & vbLf &
        "ARZ;Arroz extra;L;ABARROTES;ARZ-50;Costeño;Arroz saco 50;saco;50;;;;;" & vbLf &
        "ACE;Aceite vegetal;L;;ACE-A-4L;;Aceite A 4 L;bidon;4;CAJA4;Caja 4 x 4 L;4;1;1" & vbLf &
        "ACE;Aceite vegetal;L;;ACE-B-5L;Marca B;Aceite B 5 L;bidon;5;CAJA2;Caja 2 x 5 L;2;1;1" & vbLf

    Private Shared Function Contar(bd As BaseDatosPrueba, tabla As String) As Long
        Return Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM {tabla} WHERE empresa_id = {bd.A.EmpresaId}"))
    End Function

    <FactPostgres>
    Public Sub Vista_previa_no_escribe_y_aplicar_crea_solo_lo_nuevo()
        Using bd = BaseDatosPrueba.Crear()
            Dim imp As New ServicioImportacionCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim antes = Contar(bd, "variante_producto")

            Dim prev = imp.VistaPrevia(Archivo)
            Assert.False(prev.HayErrores, String.Join(" | ", prev.Filas.Select(Function(f) f.Detalle)))
            Assert.Equal(1, prev.ProductosNuevos)        ' ACE ya existía
            Assert.Equal(2, prev.VariantesNuevas)        ' ACE-A-4L ya existía
            Assert.Equal(1, prev.EmpaquesNuevos)         ' CAJA4 ya existía
            Assert.Equal(EstadoFilaImportacion.SinCambios, prev.Filas(1).Estado)
            Assert.Equal(antes, Contar(bd, "variante_producto"))

            Dim r = imp.Aplicar(Archivo)
            Assert.True(r.Aplicado)
            Assert.Equal(antes + 2, Contar(bd, "variante_producto"))
            Assert.Equal(1L, Contar(bd, "categoria_producto"))
            Assert.Equal(2L, Contar(bd, "marca"))
        End Using
    End Sub

    <FactPostgres>
    Public Sub T47_Repetir_la_importacion_no_duplica_nada()
        Using bd = BaseDatosPrueba.Crear()
            Dim imp As New ServicioImportacionCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            imp.Aplicar(Archivo)
            Dim conteos = {"producto_base", "variante_producto", "empaque_compra", "marca", "categoria_producto"}.Select(Function(t) Contar(bd, t)).ToArray()

            Dim r = imp.Aplicar(Archivo)
            Assert.True(r.Filas.All(Function(f) f.Estado = EstadoFilaImportacion.SinCambios))
            Assert.Equal(0, r.ProductosNuevos + r.VariantesNuevas + r.EmpaquesNuevos + r.MarcasNuevas + r.CategoriasNuevas)
            Assert.Equal(String.Join(",", conteos),
                         String.Join(",", {"producto_base", "variante_producto", "empaque_compra", "marca", "categoria_producto"}.Select(Function(t) Contar(bd, t))))
        End Using
    End Sub

    <FactPostgres>
    Public Sub T47_Archivo_con_una_fila_invalida_no_importa_nada_y_reporta_la_fila()
        Using bd = BaseDatosPrueba.Crear()
            Dim imp As New ServicioImportacionCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim malo = Archivo & "FRJ;Frijol;KG;;FRJ-1;;Frijol saco;saco;25;;;;;" & vbLf &      ' unidad KG no existe en A
                                 "ACE;Aceite de oliva;L;;ACE-X;;Oliva;botella;1;;;;;" & vbLf       ' contradice la fila 3 del mismo archivo
            Dim prev = imp.VistaPrevia(malo)
            Dim conError = prev.Filas.Where(Function(f) f.Estado = EstadoFilaImportacion.ConError).ToList()
            Assert.Equal("5,6", String.Join(",", conError.Select(Function(f) f.Numero)))
            Assert.Contains("unidad 'KG' no existe", conError(0).Detalle)
            Assert.Contains("ya aparece en la fila 3", conError(1).Detalle)

            Dim antes = Contar(bd, "variante_producto")
            Dim ex = Assert.Throws(Of ReglaNegocioException)(Function() imp.Aplicar(malo))
            Assert.Equal("IMPORTACION_CON_ERRORES", ex.Codigo)
            Assert.Equal(antes, Contar(bd, "variante_producto"))
            Assert.Equal(0L, Contar(bd, "marca"))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Una_variante_existente_con_otro_contenido_no_se_sobrescribe()
        Using bd = BaseDatosPrueba.Crear()
            Dim imp As New ServicioImportacionCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim prev = imp.VistaPrevia(Cabecera & vbLf & "ACE;Aceite vegetal;L;;ACE-A-4L;;Aceite A 4 L;bidon;5;;;;;" & vbLf)
            Assert.Equal(EstadoFilaImportacion.ConError, prev.Filas.Single().Estado)
            Assert.Contains("use un codigo nuevo", prev.Filas.Single().Detalle)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Un_producto_existente_con_otra_descripcion_no_se_sobrescribe()
        Using bd = BaseDatosPrueba.Crear()
            Dim imp As New ServicioImportacionCatalogo(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim prev = imp.VistaPrevia(Cabecera & vbLf & "ACE;Aceite de oliva;L;;ACE-X;;Oliva;botella;1;;;;;" & vbLf)
            Assert.Equal(EstadoFilaImportacion.ConError, prev.Filas.Single().Estado)
            Assert.Contains("ya existe con la descripcion 'Aceite vegetal'", prev.Filas.Single().Detalle)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Importar_requiere_permiso()
        Using bd = BaseDatosPrueba.Crear()
            Dim adm As New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            adm.CrearUsuario("alm", "Almacen", "Almacen-2026-x", bd.A.OperacionId, "ALMACEN")
            Dim imp As New ServicioImportacionCatalogo(bd.CadenaAplicacion, bd.Sesion("A", "alm", "Almacen-2026-x"))
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() imp.VistaPrevia(Archivo)).Codigo)
        End Using
    End Sub

End Class
