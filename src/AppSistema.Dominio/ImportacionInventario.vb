Namespace Importacion

    Public NotInheritable Class LineaInventarioInicial
        Public Property Linea As Integer
        Public Property VarianteCodigo As String
        ''' <summary>Cantidad en envases de la presentación (puede tener decimales: 7,04 kg).</summary>
        Public Property StockEnvasesU6 As Long
        Public Property PrecioEnvaseU6 As Long
    End Class

    Public NotInheritable Class LecturaInventarioInicial
        Public ReadOnly Property Lineas As New List(Of LineaInventarioInicial)
        Public ReadOnly Property Errores As New List(Of ErrorFila)
    End Class

    ''' <summary>
    ''' Lee inventario_inicial.csv (separador ";"): variante_codigo;stock_envases;precio_envase (otras columnas se ignoran).
    ''' El sistema calcula la cantidad base con el contenido del envase registrado en el catálogo, no la del archivo.
    ''' </summary>
    Public Module LectorInventarioInicial

        Public Function EsArchivoInventario(texto As String) As Boolean
            If texto Is Nothing Then Return False
            Dim primera = texto.TrimStart(ChrW(&HFEFF)).Split({vbLf}, StringSplitOptions.None)(0).ToLowerInvariant()
            Return primera.Contains("variante_codigo") AndAlso primera.Contains("stock_envases")
        End Function

        Public Function Leer(texto As String) As LecturaInventarioInicial
            Dim r As New LecturaInventarioInicial()
            If String.IsNullOrWhiteSpace(texto) Then r.Errores.Add(New ErrorFila(1, "El archivo esta vacio.")) : Return r
            Dim registros = LectorCsvCatalogo.Separar(texto.TrimStart(ChrW(&HFEFF)), ";"c)
            Dim cab = registros(0).Campos.Select(Function(c) c.Trim().ToLowerInvariant()).ToList()
            Dim faltan = {"variante_codigo", "stock_envases", "precio_envase"}.Where(Function(c) Not cab.Contains(c)).ToList()
            If faltan.Count > 0 Then r.Errores.Add(New ErrorFila(1, "Faltan columnas: " & String.Join(", ", faltan))) : Return r
            Dim vistos As New Dictionary(Of String, Integer)(StringComparer.Ordinal)
            For Each reg In registros.Skip(1)
                If reg.Campos.All(Function(c) c.Trim() = "") Then Continue For
                Dim valor = Function(col As String) As String
                                Dim i = cab.IndexOf(col)
                                Return If(i < 0 OrElse i >= reg.Campos.Count, "", reg.Campos(i).Trim())
                            End Function
                Dim errores As New List(Of String)
                Dim l As New LineaInventarioInicial With {.Linea = reg.Linea, .VarianteCodigo = valor("variante_codigo")}
                If l.VarianteCodigo = "" Then errores.Add("'variante_codigo' es obligatorio")
                Dim s, p As Long
                If Not LectorCsvCatalogo.LeerDecimalU6(valor("stock_envases"), s) OrElse s <= 0 Then errores.Add($"stock '{valor("stock_envases")}' debe ser mayor que cero")
                If Not LectorCsvCatalogo.LeerDecimalU6(valor("precio_envase"), p) OrElse p < 0 Then errores.Add($"precio '{valor("precio_envase")}' debe ser un numero no negativo")
                Dim previa As Integer
                If l.VarianteCodigo <> "" AndAlso vistos.TryGetValue(l.VarianteCodigo, previa) Then errores.Add($"'{l.VarianteCodigo}' ya aparece en la fila {previa}")
                If errores.Count > 0 Then
                    r.Errores.Add(New ErrorFila(reg.Linea, String.Join("; ", errores)))
                Else
                    l.StockEnvasesU6 = s : l.PrecioEnvaseU6 = p
                    vistos(l.VarianteCodigo) = reg.Linea
                    r.Lineas.Add(l)
                End If
            Next
            If r.Lineas.Count = 0 AndAlso r.Errores.Count = 0 Then r.Errores.Add(New ErrorFila(1, "El archivo no tiene lineas."))
            Return r
        End Function

    End Module

End Namespace
