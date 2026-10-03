Imports System.Globalization
Imports AppSistema.Dominio.Numerico

Namespace Importacion

    Public NotInheritable Class IngredienteImportado
        Public Property Linea As Integer
        Public Property Nombre As String
        ''' <summary>KG, L o UND.</summary>
        Public Property Unidad As String
        Public Property CantidadU6 As Long
        Public Property Tecnica As String
    End Class

    Public NotInheritable Class RecetaImportada
        Public Property Linea As Integer
        Public Property Codigo As String
        Public Property Nombre As String
        Public Property Categoria As String
        Public Property Fuente As String
        Public Property RendimientoU6 As Long
        Public Property Instrucciones As String
        Public ReadOnly Property Ingredientes As New List(Of IngredienteImportado)
    End Class

    Public NotInheritable Class LecturaRecetas
        Public ReadOnly Property Recetas As New List(Of RecetaImportada)
        Public ReadOnly Property Errores As New List(Of ErrorFila)

        Public ReadOnly Property EsValida As Boolean
            Get
                Return Errores.Count = 0
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Lee recetas_normalizadas.csv (una fila por ingrediente; separador ";"):
    ''' receta_codigo;receta_nombre;categoria;fuente;rendimiento;ingrediente;unidad;cantidad;tecnica;instrucciones.
    ''' La cantidad es para el rendimiento indicado (en el SGP, 1 ración). Las filas de una receta van juntas.
    ''' </summary>
    Public Module LectorCsvRecetas

        Public ReadOnly Property Columnas As IReadOnlyList(Of String) = New String() {
            "receta_codigo", "receta_nombre", "categoria", "fuente", "rendimiento", "ingrediente", "unidad", "cantidad", "tecnica", "instrucciones"}

        Public ReadOnly Property Unidades As IReadOnlyList(Of String) = New String() {"KG", "L", "UND"}

        Public Function EsArchivoRecetas(texto As String) As Boolean
            If texto Is Nothing Then Return False
            Dim primera = texto.TrimStart(ChrW(&HFEFF)).Split({vbLf}, StringSplitOptions.None)(0).ToLowerInvariant()
            Return primera.Contains("receta_codigo") AndAlso primera.Contains("ingrediente")
        End Function

        Public Function Leer(texto As String) As LecturaRecetas
            Dim r As New LecturaRecetas()
            If String.IsNullOrWhiteSpace(texto) Then r.Errores.Add(New ErrorFila(1, "El archivo esta vacio.")) : Return r
            Dim registros = LectorCsvCatalogo.Separar(texto.TrimStart(ChrW(&HFEFF)), ";"c)
            Dim cab = registros(0).Campos.Select(Function(c) c.Trim().ToLowerInvariant()).ToList()
            Dim faltan = Columnas.Take(8).Where(Function(c) Not cab.Contains(c)).ToList()
            If faltan.Count > 0 Then r.Errores.Add(New ErrorFila(1, "Faltan columnas: " & String.Join(", ", faltan))) : Return r

            Dim porCodigo As New Dictionary(Of String, RecetaImportada)(StringComparer.Ordinal)
            Dim cerradas As New HashSet(Of String)(StringComparer.Ordinal)
            Dim actual As RecetaImportada = Nothing
            For Each reg In registros.Skip(1)
                If reg.Campos.All(Function(c) c.Trim() = "") Then Continue For
                Dim valor = Function(col As String) As String
                                Dim i = cab.IndexOf(col)
                                Return If(i < 0 OrElse i >= reg.Campos.Count, "", reg.Campos(i).Trim())
                            End Function
                Dim errores As New List(Of String)
                Dim codigo = valor("receta_codigo")
                If codigo = "" Then errores.Add("'receta_codigo' es obligatorio")
                Dim rend As Long
                If Not LectorCsvCatalogo.LeerDecimalU6(valor("rendimiento"), rend) OrElse rend <= 0 Then errores.Add("rendimiento debe ser mayor que cero")
                Dim cant As Long
                If Not LectorCsvCatalogo.LeerDecimalU6(valor("cantidad"), cant) OrElse cant <= 0 Then errores.Add($"cantidad '{valor("cantidad")}' debe ser un numero positivo con hasta 6 decimales")
                Dim unidad = valor("unidad").ToUpperInvariant()
                If Not Unidades.Contains(unidad) Then errores.Add($"unidad '{valor("unidad")}' debe ser KG, L o UND")
                Dim ingrediente = Normalizar(valor("ingrediente"))
                If ingrediente = "" Then errores.Add("'ingrediente' es obligatorio")

                If errores.Count = 0 Then
                    If actual Is Nothing OrElse actual.Codigo <> codigo Then
                        If cerradas.Contains(codigo) Then
                            errores.Add($"las filas de la receta '{codigo}' deben ir juntas")
                        Else
                            If actual IsNot Nothing Then cerradas.Add(actual.Codigo)
                            actual = New RecetaImportada With {
                                .Linea = reg.Linea, .Codigo = codigo, .Nombre = Normalizar(valor("receta_nombre")), .Categoria = valor("categoria"),
                                .Fuente = valor("fuente"), .RendimientoU6 = rend, .Instrucciones = valor("instrucciones")}
                            If actual.Nombre = "" Then errores.Add("'receta_nombre' es obligatorio")
                            porCodigo(codigo) = actual
                            r.Recetas.Add(actual)
                        End If
                    ElseIf actual.RendimientoU6 <> rend OrElse actual.Nombre <> Normalizar(valor("receta_nombre")) Then
                        errores.Add($"la receta '{codigo}' cambia de nombre o rendimiento entre filas")
                    End If
                    If errores.Count = 0 AndAlso actual.Ingredientes.Any(Function(x) x.Nombre = ingrediente) Then
                        errores.Add($"'{ingrediente}' ya aparece en la receta '{codigo}'")
                    End If
                    If errores.Count = 0 Then
                        If actual.Instrucciones = "" Then actual.Instrucciones = valor("instrucciones")
                        actual.Ingredientes.Add(New IngredienteImportado With {
                            .Linea = reg.Linea, .Nombre = ingrediente, .Unidad = unidad, .CantidadU6 = cant, .Tecnica = valor("tecnica")})
                    End If
                End If
                If errores.Count > 0 Then r.Errores.Add(New ErrorFila(reg.Linea, String.Join("; ", errores)))
            Next

            ' Un mismo ingrediente siempre con la misma unidad (es un producto base con una sola unidad).
            Dim unidadDe As New Dictionary(Of String, IngredienteImportado)(StringComparer.Ordinal)
            For Each i In r.Recetas.SelectMany(Function(x) x.Ingredientes)
                Dim previo As IngredienteImportado = Nothing
                If unidadDe.TryGetValue(i.Nombre, previo) Then
                    If previo.Unidad <> i.Unidad Then
                        r.Errores.Add(New ErrorFila(i.Linea, $"'{i.Nombre}' esta en {i.Unidad} y en la fila {previo.Linea} en {previo.Unidad}"))
                    End If
                Else
                    unidadDe(i.Nombre) = i
                End If
            Next
            r.Errores.Sort(Function(a, b) a.Numero.CompareTo(b.Numero))
            If r.Recetas.Count = 0 AndAlso r.Errores.Count = 0 Then r.Errores.Add(New ErrorFila(1, "El archivo no tiene recetas."))
            Return r
        End Function

        Private Function Normalizar(s As String) As String
            Return String.Join(" ", s.ToUpperInvariant().Split({" "c, ChrW(160), vbTab(0)}, StringSplitOptions.RemoveEmptyEntries))
        End Function

    End Module

End Namespace
