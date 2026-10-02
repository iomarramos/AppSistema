Imports AppSistema.Dominio
Imports AppSistema.Dominio.Importacion
Imports AppSistema.Dominio.Seguridad

Public Enum EstadoFilaImportacion
    Nueva
    SinCambios
    ConError
End Enum

Public NotInheritable Class FilaResultadoImportacion
    Public Property Numero As Integer
    Public Property Estado As EstadoFilaImportacion
    Public Property Detalle As String
End Class

Public NotInheritable Class ResultadoImportacion
    Public ReadOnly Property Filas As New List(Of FilaResultadoImportacion)
    Public Property CategoriasNuevas As Integer
    Public Property MarcasNuevas As Integer
    Public Property ProductosNuevos As Integer
    Public Property VariantesNuevas As Integer
    Public Property EmpaquesNuevos As Integer
    Public Property Aplicado As Boolean

    Public ReadOnly Property HayErrores As Boolean
        Get
            Return Filas.Any(Function(f) f.Estado = EstadoFilaImportacion.ConError)
        End Get
    End Property

    Public ReadOnly Property Resumen As String
        Get
            Return $"Productos {ProductosNuevos}, variantes {VariantesNuevas}, empaques {EmpaquesNuevos}, categorias {CategoriasNuevas}, marcas {MarcasNuevas} nuevos; " &
                   $"{Filas.Where(Function(f) f.Estado = EstadoFilaImportacion.SinCambios).Count()} filas sin cambios; " &
                   $"{Filas.Where(Function(f) f.Estado = EstadoFilaImportacion.ConError).Count()} con error."
        End Get
    End Property
End Class

''' <summary>
''' Importa el catálogo desde CSV en dos pasos: vista previa (no escribe) y aplicar (todo o nada).
''' Nunca sobrescribe registros existentes: un código existente con otros datos es un error de la fila.
''' Repetir la misma importación no duplica nada (T47).
''' </summary>
Public NotInheritable Class ServicioImportacionCatalogo
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    Public Function VistaPrevia(textoCsv As String) As ResultadoImportacion
        Return EnTransaccion(Permisos.CatalogoImportar, Function(u) Procesar(u, textoCsv, aplicar:=False))
    End Function

    ''' <summary>Aplica la importación. Si alguna fila tiene error no se escribe nada (IMPORTACION_CON_ERRORES).</summary>
    Public Function Aplicar(textoCsv As String) As ResultadoImportacion
        Return EnTransaccion(Permisos.CatalogoImportar, Function(u) Procesar(u, textoCsv, aplicar:=True))
    End Function

    Private NotInheritable Class ProductoExistente
        Public Id As Long, Descripcion As String, Unidad As String, Categoria As String
    End Class
    Private NotInheritable Class VarianteExistente
        Public Id As Long, Producto As String, Marca As String, Descripcion As String, Envase As String, Contenido As Long
    End Class
    Private NotInheritable Class EmpaqueExistente
        Public Id As Long, Descripcion As String, Envases As Long, Minimo As Long, Multiplo As Long
    End Class

    Private Function Procesar(u As UnidadDeTrabajo, textoCsv As String, aplicar As Boolean) As ResultadoImportacion
        Dim r As New ResultadoImportacion()
        Dim lectura = LectorCsvCatalogo.Leer(textoCsv)
        For Each e In lectura.Errores
            r.Filas.Add(New FilaResultadoImportacion With {.Numero = e.Numero, .Estado = EstadoFilaImportacion.ConError, .Detalle = e.Mensaje})
        Next

        ' Estado actual del catálogo (solo de la empresa de la sesión, por RLS).
        Dim unidades = u.Consultar("SELECT codigo, id FROM unidad_medida", Function(rd) (rd.GetString(0), rd.GetInt64(1))) _
                        .ToDictionary(Function(x) x.Item1, Function(x) x.Item2, StringComparer.Ordinal)
        Dim categorias = u.Consultar("SELECT codigo, id FROM categoria_producto", Function(rd) (rd.GetString(0), rd.GetInt64(1))) _
                        .ToDictionary(Function(x) x.Item1, Function(x) x.Item2, StringComparer.Ordinal)
        Dim marcas = u.Consultar("SELECT nombre, id FROM marca", Function(rd) (rd.GetString(0), rd.GetInt64(1))) _
                        .ToDictionary(Function(x) x.Item1, Function(x) x.Item2, StringComparer.Ordinal)
        Dim productos = u.Consultar(
            "SELECT p.codigo, p.id, p.descripcion, um.codigo, c.codigo FROM producto_base p " &
            "JOIN unidad_medida um ON um.empresa_id = p.empresa_id AND um.id = p.unidad_base_id " &
            "LEFT JOIN categoria_producto c ON c.empresa_id = p.empresa_id AND c.id = p.categoria_id",
            Function(rd) (rd.GetString(0), New ProductoExistente With {.Id = rd.GetInt64(1), .Descripcion = rd.GetString(2), .Unidad = rd.GetString(3),
                                                                       .Categoria = If(rd.IsDBNull(4), "", rd.GetString(4))})) _
            .ToDictionary(Function(x) x.Item1, Function(x) x.Item2, StringComparer.Ordinal)
        Dim variantes = u.Consultar(
            "SELECT v.codigo, v.id, p.codigo, m.nombre, v.descripcion_comercial, v.tipo_envase, v.contenido_base_por_envase_u6 FROM variante_producto v " &
            "JOIN producto_base p ON p.empresa_id = v.empresa_id AND p.id = v.producto_base_id " &
            "LEFT JOIN marca m ON m.empresa_id = v.empresa_id AND m.id = v.marca_id",
            Function(rd) (rd.GetString(0), New VarianteExistente With {.Id = rd.GetInt64(1), .Producto = rd.GetString(2), .Marca = If(rd.IsDBNull(3), "", rd.GetString(3)),
                                                                       .Descripcion = rd.GetString(4), .Envase = rd.GetString(5), .Contenido = rd.GetInt64(6)})) _
            .ToDictionary(Function(x) x.Item1, Function(x) x.Item2, StringComparer.Ordinal)
        Dim empaques = u.Consultar(
            "SELECT v.codigo || '|' || e.codigo, e.id, e.descripcion, e.envases_por_empaque, e.minimo_empaques, e.multiplo_empaques FROM empaque_compra e " &
            "JOIN variante_producto v ON v.empresa_id = e.empresa_id AND v.id = e.variante_id",
            Function(rd) (rd.GetString(0), New EmpaqueExistente With {.Id = rd.GetInt64(1), .Descripcion = rd.GetString(2), .Envases = rd.GetInt64(3),
                                                                      .Minimo = rd.GetInt64(4), .Multiplo = rd.GetInt64(5)})) _
            .ToDictionary(Function(x) x.Item1, Function(x) x.Item2, StringComparer.Ordinal)

        ' Lo que se crearía (claves), para no contar ni crear dos veces.
        Dim nuevasCategorias As New HashSet(Of String)(StringComparer.Ordinal)
        Dim nuevasMarcas As New HashSet(Of String)(StringComparer.Ordinal)
        Dim nuevosProductos As New List(Of FilaCatalogo)
        Dim nuevasVariantes As New List(Of FilaCatalogo)
        Dim nuevosEmpaques As New List(Of FilaCatalogo)
        Dim vistos As New HashSet(Of String)(StringComparer.Ordinal)

        For Each f In lectura.Filas
            Dim errores As New List(Of String)
            Dim crea As New List(Of String)

            If Not unidades.ContainsKey(f.UnidadBase) Then errores.Add($"la unidad '{f.UnidadBase}' no existe en el catalogo")

            Dim p As ProductoExistente = Nothing
            If productos.TryGetValue(f.ProductoCodigo, p) Then
                If p.Descripcion <> f.ProductoDescripcion Then errores.Add($"el producto '{f.ProductoCodigo}' ya existe con la descripcion '{p.Descripcion}'")
                If p.Unidad <> f.UnidadBase Then errores.Add($"el producto '{f.ProductoCodigo}' ya existe con la unidad '{p.Unidad}'")
                If f.Categoria <> "" AndAlso p.Categoria <> f.Categoria Then errores.Add($"el producto '{f.ProductoCodigo}' ya existe con la categoria '{p.Categoria}'")
            ElseIf vistos.Add("P|" & f.ProductoCodigo) Then
                nuevosProductos.Add(f) : crea.Add("producto")
            End If

            Dim v As VarianteExistente = Nothing
            If variantes.TryGetValue(f.VarianteCodigo, v) Then
                If v.Producto <> f.ProductoCodigo OrElse v.Contenido <> f.ContenidoPorEnvaseU6 OrElse v.Envase <> f.TipoEnvase OrElse
                   v.Descripcion <> f.DescripcionComercial OrElse (f.Marca <> "" AndAlso v.Marca <> f.Marca) Then
                    errores.Add($"la variante '{f.VarianteCodigo}' ya existe con otros datos; para otra presentacion use un codigo nuevo")
                End If
            ElseIf vistos.Add("V|" & f.VarianteCodigo) Then
                nuevasVariantes.Add(f) : crea.Add("variante")
            End If

            If f.TieneEmpaque Then
                Dim clave = f.VarianteCodigo & "|" & f.EmpaqueCodigo
                Dim e As EmpaqueExistente = Nothing
                If empaques.TryGetValue(clave, e) Then
                    If e.Envases <> f.EnvasesPorEmpaque OrElse e.Minimo <> f.Minimo OrElse e.Multiplo <> f.Multiplo OrElse e.Descripcion <> f.EmpaqueDescripcion Then
                        errores.Add($"el empaque '{f.EmpaqueCodigo}' de '{f.VarianteCodigo}' ya existe con otros datos")
                    End If
                ElseIf vistos.Add("E|" & clave) Then
                    nuevosEmpaques.Add(f) : crea.Add("empaque")
                End If
            End If

            If f.Categoria <> "" AndAlso Not categorias.ContainsKey(f.Categoria) AndAlso nuevasCategorias.Add(f.Categoria) Then crea.Add("categoria")
            If f.Marca <> "" AndAlso Not marcas.ContainsKey(f.Marca) AndAlso nuevasMarcas.Add(f.Marca) Then crea.Add("marca")

            Dim fila As New FilaResultadoImportacion With {.Numero = f.Numero}
            If errores.Count > 0 Then
                fila.Estado = EstadoFilaImportacion.ConError : fila.Detalle = String.Join("; ", errores)
            ElseIf crea.Count > 0 Then
                fila.Estado = EstadoFilaImportacion.Nueva : fila.Detalle = "Crea: " & String.Join(", ", crea)
            Else
                fila.Estado = EstadoFilaImportacion.SinCambios : fila.Detalle = "Ya existe igual"
            End If
            r.Filas.Add(fila)
        Next
        r.Filas.Sort(Function(a, b) a.Numero.CompareTo(b.Numero))

        r.CategoriasNuevas = nuevasCategorias.Count
        r.MarcasNuevas = nuevasMarcas.Count
        r.ProductosNuevos = nuevosProductos.Count
        r.VariantesNuevas = nuevasVariantes.Count
        r.EmpaquesNuevos = nuevosEmpaques.Count

        If Not aplicar Then Return r
        If r.HayErrores Then
            Throw New ReglaNegocioException("IMPORTACION_CON_ERRORES", "El archivo tiene filas con error; corrijalas y vuelva a revisar la vista previa. No se importo nada.")
        End If

        For Each c In nuevasCategorias
            categorias(c) = u.EscalarLong("INSERT INTO categoria_producto(empresa_id, codigo, nombre) VALUES (@e, @c, @c) RETURNING id", "e", Sesion.EmpresaId, "c", c)
        Next
        For Each m In nuevasMarcas
            marcas(m) = u.EscalarLong("INSERT INTO marca(empresa_id, nombre) VALUES (@e, @n) RETURNING id", "e", Sesion.EmpresaId, "n", m)
        Next
        Dim idProducto = productos.ToDictionary(Function(x) x.Key, Function(x) x.Value.Id, StringComparer.Ordinal)
        For Each f In nuevosProductos
            idProducto(f.ProductoCodigo) = u.EscalarLong(
                "INSERT INTO producto_base(empresa_id, codigo, descripcion, unidad_base_id, categoria_id) VALUES (@e, @c, @d, @un, @cat) RETURNING id",
                "e", Sesion.EmpresaId, "c", f.ProductoCodigo, "d", f.ProductoDescripcion, "un", unidades(f.UnidadBase),
                "cat", If(f.Categoria = "", Nothing, CType(categorias(f.Categoria), Object)))
        Next
        Dim idVariante = variantes.ToDictionary(Function(x) x.Key, Function(x) x.Value.Id, StringComparer.Ordinal)
        For Each f In nuevasVariantes
            idVariante(f.VarianteCodigo) = u.EscalarLong(
                "INSERT INTO variante_producto(empresa_id, producto_base_id, marca_id, codigo, descripcion_comercial, tipo_envase, contenido_base_por_envase_u6) " &
                "VALUES (@e, @p, @m, @c, @d, @t, @k) RETURNING id",
                "e", Sesion.EmpresaId, "p", idProducto(f.ProductoCodigo), "m", If(f.Marca = "", Nothing, CType(marcas(f.Marca), Object)),
                "c", f.VarianteCodigo, "d", f.DescripcionComercial, "t", f.TipoEnvase, "k", f.ContenidoPorEnvaseU6)
        Next
        For Each f In nuevosEmpaques
            u.Ejecutar("INSERT INTO empaque_compra(empresa_id, variante_id, codigo, descripcion, envases_por_empaque, minimo_empaques, multiplo_empaques) " &
                       "VALUES (@e, @v, @c, @d, @n, @min, @mul)",
                       "e", Sesion.EmpresaId, "v", idVariante(f.VarianteCodigo), "c", f.EmpaqueCodigo, "d", f.EmpaqueDescripcion,
                       "n", f.EnvasesPorEmpaque, "min", f.Minimo, "mul", f.Multiplo)
        Next
        r.Aplicado = True
        Return r
    End Function

End Class
