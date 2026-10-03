Imports System.IO
Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Datos

Public Class ImportacionRecetasDatosTests

    Private Shared Function ArchivoReal() As String
        Dim dir = New DirectoryInfo(AppContext.BaseDirectory)
        Do While Not File.Exists(Path.Combine(dir.FullName, "datos", "recetas", "recetas_normalizadas.csv"))
            dir = dir.Parent
        Loop
        Return File.ReadAllText(Path.Combine(dir.FullName, "datos", "recetas", "recetas_normalizadas.csv"))
    End Function

    Private Shared Function Contar(bd As BaseDatosPrueba, sql As String) As Long
        Return Convert.ToInt64(bd.Escalar(sql))
    End Function

    <FactPostgres>
    Public Sub Importa_las_946_recetas_reutiliza_ingredientes_existentes_y_repetir_no_duplica()
        Using bd = BaseDatosPrueba.Crear()
            bd.EjecutarAdmin("UPDATE producto_base SET descripcion = 'Aceite vegetal' WHERE codigo = 'ACE'")   ' "ACEITE VEGETAL" en L ya existe
            Dim imp As New ServicioImportacionRecetas(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim texto = ArchivoReal()

            Dim prev = imp.VistaPrevia(texto)
            Assert.False(prev.HayErrores, String.Join(" | ", prev.Filas.Where(Function(f) f.Estado = EstadoFilaImportacion.ConError).Take(5).Select(Function(f) $"{f.Numero}: {f.Detalle}")))
            Assert.Equal(946, prev.RecetasNuevas)
            Assert.Equal(411, prev.IngredientesNuevos)
            Assert.Equal(1, prev.IngredientesExistentes)
            Assert.Equal(0L, Contar(bd, "SELECT count(*) FROM receta"))

            Dim r = imp.Aplicar(texto, aprobar:=True)
            Assert.True(r.Aplicado)
            Assert.Equal(946L, Contar(bd, $"SELECT count(*) FROM receta WHERE empresa_id = {bd.A.EmpresaId}"))
            Assert.Equal(8874L, Contar(bd, "SELECT count(*) FROM receta_ingrediente"))
            Assert.Equal(946L, Contar(bd, "SELECT count(*) FROM receta_version WHERE estado = 'aprobada' AND version = 1"))
            Assert.Equal(0L, Contar(bd, $"SELECT count(*) FROM receta WHERE empresa_id = {bd.B.EmpresaId}"))
            ' El aceite de las recetas es el producto que ya existía.
            Assert.Equal(508L, Contar(bd, $"SELECT count(*) FROM receta_ingrediente WHERE producto_base_id = {bd.ProductoAceiteId}"))
            ' Arroz blanco: 0,12 kg de arroz por ración, con técnica e instrucciones de la ficha.
            Assert.Equal("120000", bd.Escalar("SELECT i.cantidad_base_bruta_u6::text FROM receta r JOIN receta_version v ON v.receta_id = r.id " &
                                              "JOIN receta_ingrediente i ON i.receta_version_id = v.id JOIN producto_base p ON p.id = i.producto_base_id " &
                                              "WHERE r.nombre = 'ARROZ BLANCO' AND p.descripcion = 'ARROZ EXTRA'").ToString())
            Assert.NotNull(bd.Escalar("SELECT 1 FROM receta_ingrediente WHERE tecnica = 'Brunoise' LIMIT 1"))

            Dim otra = imp.Aplicar(texto, aprobar:=True)
            Assert.Equal(0, otra.RecetasNuevas + otra.IngredientesNuevos)
            Assert.Equal(946L, Contar(bd, "SELECT count(*) FROM receta"))

            ' Una receta importada se planifica: 150 raciones de arroz blanco → 18 kg de arroz.
            Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim servicio = minutas.CrearServicio("ALM", "Almuerzo")
            Dim estructura = minutas.CrearEstructura(servicio, "GUARN", "Guarnicion", 1)
            Dim os = minutas.AsignarServicio(servicio, minutas.CrearRegimen("GEN", "General"), Nothing)
            Dim m = minutas.CrearMinuta(os, New Date(2026, 3, 10), 150)
            Dim version = Contar(bd, "SELECT v.id FROM receta r JOIN receta_version v ON v.receta_id = r.id WHERE r.nombre = 'ARROZ BLANCO'")
            minutas.AgregarPlato(m, estructura, version, 150)
            Assert.Equal(EscalaU6.DesdeDecimal(18D), minutas.Necesidades({m}).Single(Function(n) n.ProductoDescripcion = "ARROZ EXTRA").CantidadU6)
        End Using
    End Sub

    <FactPostgres>
    Public Sub Receta_existente_con_otros_ingredientes_es_error_y_no_importa_nada()
        Using bd = BaseDatosPrueba.Crear()
            Dim imp As New ServicioImportacionRecetas(bd.CadenaAplicacion, bd.Sesion("A"))
            Const Cab As String = "receta_codigo;receta_nombre;categoria;fuente;rendimiento;ingrediente;unidad;cantidad;tecnica;instrucciones"
            imp.Aplicar(Cab & vbLf & "F1;ARROZ;;;1;ARROZ EXTRA;KG;0.12;;" & vbLf, aprobar:=False)
            Assert.Equal("borrador", bd.Escalar("SELECT estado FROM receta_version").ToString())
            Dim cambiado = Cab & vbLf & "F1;ARROZ;;;1;ARROZ EXTRA;KG;0.15;;" & vbLf & "F2;SOPA;;;1;SAL;KG;0.002;;" & vbLf
            Dim prev = imp.VistaPrevia(cambiado)
            Assert.Contains("ya existe con otros datos", prev.Filas.Single(Function(f) f.Estado = EstadoFilaImportacion.ConError).Detalle)
            Assert.Equal("IMPORTACION_CON_ERRORES", Assert.Throws(Of ReglaNegocioException)(Function() imp.Aplicar(cambiado, False)).Codigo)
            Assert.Equal(1L, Contar(bd, "SELECT count(*) FROM receta"))
            Assert.Null(bd.Escalar("SELECT 1 FROM producto_base WHERE descripcion = 'SAL'"))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Regresion_update_sin_cambios_no_falla_en_la_auditoria_ni_la_registra()
        Using bd = BaseDatosPrueba.Crear()
            Dim antes = Contar(bd, "SELECT count(*) FROM auditoria")
            bd.EjecutarAdmin("UPDATE producto_base SET descripcion = descripcion; UPDATE usuario SET intentos_fallidos = intentos_fallidos")
            Assert.Equal(antes, Contar(bd, "SELECT count(*) FROM auditoria"))
            bd.EjecutarAdmin("UPDATE usuario SET password_hash = password_hash || 'x' WHERE login = 'admin' AND empresa_id = " & bd.B.EmpresaId)
            Assert.Equal(1L, Contar(bd, "SELECT count(*) FROM auditoria WHERE accion = 'CAMBIO_CLAVE' AND antes_json IS NULL"))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Aprobar_al_importar_exige_permiso_de_aprobar()
        Using bd = BaseDatosPrueba.Crear()
            Call New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A")).CrearUsuario("cocina", "Cocinero", "Cocina-Clave-2026", bd.A.OperacionId, "COCINA")
            Dim imp As New ServicioImportacionRecetas(bd.CadenaAplicacion, bd.Sesion("A", "cocina", "Cocina-Clave-2026"))
            Dim csv = "receta_codigo;receta_nombre;categoria;fuente;rendimiento;ingrediente;unidad;cantidad;tecnica;instrucciones" & vbLf & "F1;A;;;1;SAL;KG;0.002;;" & vbLf
            Assert.Equal("SIN_PERMISO", Assert.Throws(Of ReglaNegocioException)(Function() imp.Aplicar(csv, False)).Codigo)   ' no importa catálogo
        End Using
    End Sub

End Class
