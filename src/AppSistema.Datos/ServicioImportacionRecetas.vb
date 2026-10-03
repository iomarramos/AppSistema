Imports AppSistema.Dominio
Imports AppSistema.Dominio.Importacion
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Importa recetas (formato de recetas_normalizadas.csv) en dos pasos: vista previa y aplicar (todo o nada).
''' - Cada ingrediente es un producto base: se reutiliza el que tenga la misma descripción y unidad;
'''   si no existe se crea con código INGnnnnn y categoría INGREDIENTE.
''' - Cada receta nueva se crea con su versión 1 (borrador, o aprobada si se pide).
''' - Una receta ya existente con los mismos ingredientes no cambia; con otros ingredientes es un error
'''   (para cambiarla se crea una nueva versión desde Recetas). Repetir la importación no duplica nada.
''' </summary>
Public NotInheritable Class ServicioImportacionRecetas
    Inherits ServicioConSesion

    Public Const CategoriaIngrediente As String = "INGREDIENTE"

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    Public Function VistaPrevia(textoCsv As String) As ResultadoImportacionRecetas
        Return EnTransaccion(Permisos.RecetasEditar, Function(u) Procesar(u, textoCsv, aplicar:=False, aprobar:=False))
    End Function

    Public Function Aplicar(textoCsv As String, aprobar As Boolean) As ResultadoImportacionRecetas
        Sesion.Exigir(Permisos.CatalogoImportar)
        If aprobar Then Sesion.Exigir(Permisos.RecetasAprobar)
        Return EnTransaccion(Permisos.RecetasEditar, Function(u) Procesar(u, textoCsv, aplicar:=True, aprobar:=aprobar))
    End Function

    Private Shared ReadOnly UnidadesBase As (Codigo As String, Nombre As String, Dimension As String)() = {
        ("KG", "Kilogramo", "masa"), ("L", "Litro", "volumen"), ("UND", "Unidad", "conteo")}

    Private Function Procesar(u As UnidadDeTrabajo, texto As String, aplicar As Boolean, aprobar As Boolean) As ResultadoImportacionRecetas
        Dim r As New ResultadoImportacionRecetas()
        Dim lectura = LectorCsvRecetas.Leer(texto)
        For Each e In lectura.Errores
            r.Filas.Add(New FilaResultadoImportacion With {.Numero = e.Numero, .Estado = EstadoFilaImportacion.ConError, .Detalle = e.Mensaje})
        Next

        Dim unidades = u.Consultar("SELECT codigo, id FROM unidad_medida", Function(rd) (rd.GetString(0), rd.GetInt64(1))) _
                        .ToDictionary(Function(x) x.Item1, Function(x) x.Item2, StringComparer.Ordinal)
        Dim nuevasUnidades = UnidadesBase.Where(Function(x) Not unidades.ContainsKey(x.Codigo)).ToList()
        r.UnidadesNuevas = nuevasUnidades.Count

        ' Productos existentes por descripción (mayúsculas, espacios simples) y unidad.
        Dim productos As New Dictionary(Of String, (Id As Long, Unidad As String))(StringComparer.Ordinal)
        For Each p In u.Consultar("SELECT p.id, upper(p.descripcion), um.codigo FROM producto_base p JOIN unidad_medida um ON um.id = p.unidad_base_id ORDER BY p.id",
                                  Function(rd) (Id:=rd.GetInt64(0), Desc:=String.Join(" ", rd.GetString(1).Split({" "c}, StringSplitOptions.RemoveEmptyEntries)), Unidad:=rd.GetString(2)))
            If Not productos.ContainsKey(p.Desc & "|" & p.Unidad) Then productos(p.Desc & "|" & p.Unidad) = (p.Id, p.Unidad)
        Next
        Dim recetas = u.Consultar(
            "SELECT r.codigo, r.nombre, rv.id, rv.rendimiento_raciones_u6 FROM receta r " &
            "JOIN LATERAL (SELECT id, rendimiento_raciones_u6 FROM receta_version WHERE receta_id = r.id ORDER BY version DESC LIMIT 1) rv ON true",
            Function(rd) (Codigo:=rd.GetString(0), Nombre:=rd.GetString(1), Version:=rd.GetInt64(2), Rend:=rd.GetInt64(3))) _
            .ToDictionary(Function(x) x.Codigo, StringComparer.Ordinal)
        Dim ingredientesDe = u.Consultar(
            "SELECT i.receta_version_id, upper(p.descripcion), um.codigo, i.cantidad_base_bruta_u6 FROM receta_ingrediente i " &
            "JOIN producto_base p ON p.id = i.producto_base_id JOIN unidad_medida um ON um.id = p.unidad_base_id",
            Function(rd) (Version:=rd.GetInt64(0), Clave:=String.Join(" ", rd.GetString(1).Split({" "c}, StringSplitOptions.RemoveEmptyEntries)) & "|" & rd.GetString(2) & "|" & rd.GetInt64(3))) _
            .ToLookup(Function(x) x.Version, Function(x) x.Clave)

        Dim nuevosIngredientes As New List(Of IngredienteImportado)
        Dim nuevasRecetas As New List(Of RecetaImportada)
        Dim vistos As New HashSet(Of String)(StringComparer.Ordinal)
        For Each rec In lectura.Recetas
            For Each i In rec.Ingredientes
                Dim clave = i.Nombre & "|" & i.Unidad
                If Not productos.ContainsKey(clave) AndAlso vistos.Add(clave) Then nuevosIngredientes.Add(i)
            Next
            Dim fila As New FilaResultadoImportacion With {.Numero = rec.Linea}
            Dim existente As (Codigo As String, Nombre As String, Version As Long, Rend As Long) = Nothing
            If recetas.TryGetValue(rec.Codigo, existente) Then
                Dim actual = ingredientesDe(existente.Version).OrderBy(Function(x) x, StringComparer.Ordinal)
                Dim archivo = rec.Ingredientes.Select(Function(i) i.Nombre & "|" & i.Unidad & "|" & i.CantidadU6).OrderBy(Function(x) x, StringComparer.Ordinal)
                If existente.Nombre <> rec.Nombre OrElse existente.Rend <> rec.RendimientoU6 OrElse Not actual.SequenceEqual(archivo) Then
                    fila.Estado = EstadoFilaImportacion.ConError
                    fila.Detalle = $"la receta {rec.Codigo} ya existe con otros datos; para cambiarla cree una nueva version en Recetas"
                Else
                    fila.Estado = EstadoFilaImportacion.SinCambios : fila.Detalle = $"{rec.Codigo} ya existe igual"
                End If
            Else
                nuevasRecetas.Add(rec)
                fila.Estado = EstadoFilaImportacion.Nueva : fila.Detalle = $"Crea {rec.Codigo} {rec.Nombre} ({rec.Ingredientes.Count} ingredientes)"
            End If
            r.Filas.Add(fila)
        Next
        r.Filas.Sort(Function(a, b) a.Numero.CompareTo(b.Numero))
        r.IngredientesNuevos = nuevosIngredientes.Count
        r.RecetasNuevas = nuevasRecetas.Count
        r.IngredientesExistentes = lectura.Recetas.SelectMany(Function(x) x.Ingredientes).Select(Function(i) i.Nombre & "|" & i.Unidad) _
                                       .Distinct().Count() - nuevosIngredientes.Count

        If Not aplicar Then Return r
        If r.HayErrores Then
            Throw New ReglaNegocioException("IMPORTACION_CON_ERRORES", "El archivo tiene filas con error; corrijalas y vuelva a revisar la vista previa. No se importo nada.")
        End If

        For Each nu In nuevasUnidades
            unidades(nu.Codigo) = u.EscalarLong("INSERT INTO unidad_medida(empresa_id, codigo, nombre, dimension) VALUES (@e, @c, @n, @d) RETURNING id",
                                                "e", Sesion.EmpresaId, "c", nu.Codigo, "n", nu.Nombre, "d", nu.Dimension)
        Next
        Dim categoria = u.Escalar("SELECT id FROM categoria_producto WHERE codigo = @c", "c", CategoriaIngrediente)
        If categoria Is Nothing AndAlso nuevosIngredientes.Count > 0 Then
            categoria = u.EscalarLong("INSERT INTO categoria_producto(empresa_id, codigo, nombre) VALUES (@e, @c, 'Ingredientes de recetas') RETURNING id",
                                      "e", Sesion.EmpresaId, "c", CategoriaIngrediente)
        End If
        Dim siguiente = u.EscalarLong("SELECT COALESCE(max(substring(codigo from 4)::bigint), 0) + 1 FROM producto_base WHERE codigo ~ '^ING[0-9]+$'")
        For Each i In nuevosIngredientes.OrderBy(Function(x) x.Nombre, StringComparer.Ordinal)
            Dim id = u.EscalarLong("INSERT INTO producto_base(empresa_id, codigo, descripcion, unidad_base_id, categoria_id) VALUES (@e, @c, @d, @un, @cat) RETURNING id",
                                   "e", Sesion.EmpresaId, "c", "ING" & siguiente.ToString("00000"), "d", i.Nombre, "un", unidades(i.Unidad), "cat", categoria)
            productos(i.Nombre & "|" & i.Unidad) = (id, i.Unidad)
            siguiente += 1
        Next
        For Each rec In nuevasRecetas
            Dim recetaId = u.EscalarLong("INSERT INTO receta(empresa_id, codigo, nombre, categoria) VALUES (@e, @c, @n, @cat) RETURNING id",
                                         "e", Sesion.EmpresaId, "c", rec.Codigo, "n", rec.Nombre, "cat", ServicioRecetas.Opcional(rec.Categoria))
            Dim versionId = u.EscalarLong("INSERT INTO receta_version(empresa_id, receta_id, version, rendimiento_raciones_u6, instrucciones) VALUES (@e, @r, 1, @rend, @i) RETURNING id",
                                          "e", Sesion.EmpresaId, "r", recetaId, "rend", rec.RendimientoU6, "i", ServicioRecetas.Opcional(rec.Instrucciones))
            Dim orden = 1
            For Each i In rec.Ingredientes
                u.Ejecutar("INSERT INTO receta_ingrediente(empresa_id, receta_version_id, producto_base_id, cantidad_base_bruta_u6, orden, tecnica) VALUES (@e, @v, @p, @c, @o, @t)",
                           "e", Sesion.EmpresaId, "v", versionId, "p", productos(i.Nombre & "|" & i.Unidad).Id, "c", i.CantidadU6, "o", orden, "t", ServicioRecetas.Opcional(i.Tecnica))
                orden += 1
            Next
            If aprobar Then u.Ejecutar("UPDATE receta_version SET estado = 'aprobada' WHERE id = @v", "v", versionId)
        Next
        r.Aplicado = True
        Return r
    End Function

End Class

Public NotInheritable Class ResultadoImportacionRecetas
    Public ReadOnly Property Filas As New List(Of FilaResultadoImportacion)
    Public Property RecetasNuevas As Integer
    Public Property IngredientesNuevos As Integer
    ''' <summary>Ingredientes del archivo que ya existían en el catálogo (misma descripción y unidad).</summary>
    Public Property IngredientesExistentes As Integer
    Public Property UnidadesNuevas As Integer
    Public Property Aplicado As Boolean

    Public ReadOnly Property HayErrores As Boolean
        Get
            Return Filas.Any(Function(f) f.Estado = EstadoFilaImportacion.ConError)
        End Get
    End Property

    Public ReadOnly Property Resumen As String
        Get
            Return $"Recetas nuevas {RecetasNuevas}; ingredientes nuevos {IngredientesNuevos} (ya en el catalogo: {IngredientesExistentes})" &
                   If(UnidadesNuevas > 0, $"; unidades nuevas {UnidadesNuevas}", "") & "; " &
                   $"{Filas.Where(Function(f) f.Estado = EstadoFilaImportacion.SinCambios).Count()} recetas sin cambios; " &
                   $"{Filas.Where(Function(f) f.Estado = EstadoFilaImportacion.ConError).Count()} filas con error."
        End Get
    End Property
End Class
