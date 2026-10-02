Imports System.Globalization
Imports System.Text
Imports AppSistema.Dominio.Numerico

Namespace Importacion

    ''' <summary>Una fila válida del archivo de catálogo (producto base + variante + empaque opcional).</summary>
    Public NotInheritable Class FilaCatalogo
        Public Property Numero As Integer
        Public Property ProductoCodigo As String
        Public Property ProductoDescripcion As String
        Public Property UnidadBase As String
        Public Property Categoria As String
        Public Property VarianteCodigo As String
        Public Property Marca As String
        Public Property DescripcionComercial As String
        Public Property TipoEnvase As String
        Public Property ContenidoPorEnvaseU6 As Long
        Public Property EmpaqueCodigo As String
        Public Property EmpaqueDescripcion As String
        Public Property EnvasesPorEmpaque As Long
        Public Property Minimo As Long
        Public Property Multiplo As Long

        Public ReadOnly Property TieneEmpaque As Boolean
            Get
                Return Not String.IsNullOrEmpty(EmpaqueCodigo)
            End Get
        End Property
    End Class

    Public NotInheritable Class ErrorFila
        Public ReadOnly Property Numero As Integer
        Public ReadOnly Property Mensaje As String

        Public Sub New(numero As Integer, mensaje As String)
            Me.Numero = numero
            Me.Mensaje = mensaje
        End Sub

        Public Overrides Function ToString() As String
            Return $"Fila {Numero}: {Mensaje}"
        End Function
    End Class

    Public NotInheritable Class LecturaCatalogo
        Public ReadOnly Property Filas As New List(Of FilaCatalogo)
        Public ReadOnly Property Errores As New List(Of ErrorFila)

        Public ReadOnly Property EsValida As Boolean
            Get
                Return Errores.Count = 0
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Lee el archivo CSV del catálogo. Separador ";" (Excel en español) o ",", detectado en la cabecera.
    ''' Campos entre comillas admiten el separador y comillas dobles. No accede a la base de datos:
    ''' valida formato, obligatorios, números y contradicciones dentro del propio archivo.
    ''' </summary>
    Public Module LectorCsvCatalogo

        Public ReadOnly Property ColumnasObligatorias As IReadOnlyList(Of String) = New String() {
            "producto_codigo", "producto_descripcion", "unidad_base", "variante_codigo",
            "descripcion_comercial", "tipo_envase", "contenido_por_envase"}

        Public ReadOnly Property ColumnasOpcionales As IReadOnlyList(Of String) = New String() {
            "categoria", "marca", "empaque_codigo", "empaque_descripcion", "envases_por_empaque", "minimo", "multiplo"}

        Public Function Leer(texto As String) As LecturaCatalogo
            Dim r As New LecturaCatalogo()
            If String.IsNullOrWhiteSpace(texto) Then
                r.Errores.Add(New ErrorFila(1, "El archivo esta vacio."))
                Return r
            End If

            Dim primeraLinea As String = texto.Split({vbLf}, StringSplitOptions.None)(0)
            Dim separador As Char = If(primeraLinea.Contains(";"c), ";"c, ","c)
            Dim registros = Separar(texto.TrimStart(ChrW(&HFEFF)), separador)

            Dim cabecera = registros(0).Campos.Select(Function(c) c.Trim().ToLowerInvariant()).ToList()
            Dim faltan = ColumnasObligatorias.Where(Function(c) Not cabecera.Contains(c)).ToList()
            If faltan.Count > 0 Then
                r.Errores.Add(New ErrorFila(1, "Faltan columnas obligatorias: " & String.Join(", ", faltan)))
                Return r
            End If
            Dim desconocidas = cabecera.Where(Function(c) c <> "" AndAlso Not ColumnasObligatorias.Contains(c) AndAlso Not ColumnasOpcionales.Contains(c)).ToList()
            If desconocidas.Count > 0 Then
                r.Errores.Add(New ErrorFila(1, "Columnas no reconocidas: " & String.Join(", ", desconocidas)))
                Return r
            End If

            Dim productos As New Dictionary(Of String, FilaCatalogo)(StringComparer.Ordinal)
            Dim variantes As New Dictionary(Of String, FilaCatalogo)(StringComparer.Ordinal)
            Dim empaques As New Dictionary(Of String, FilaCatalogo)(StringComparer.Ordinal)

            For Each reg In registros.Skip(1)
                If reg.Campos.All(Function(c) c.Trim() = "") Then Continue For
                Dim valor = Function(col As String) As String
                                Dim i = cabecera.IndexOf(col)
                                If i < 0 OrElse i >= reg.Campos.Count Then Return ""
                                Return reg.Campos(i).Trim()
                            End Function

                Dim errores As New List(Of String)
                Dim f As New FilaCatalogo With {
                    .Numero = reg.Linea,
                    .ProductoCodigo = valor("producto_codigo"),
                    .ProductoDescripcion = valor("producto_descripcion"),
                    .UnidadBase = valor("unidad_base"),
                    .Categoria = valor("categoria"),
                    .VarianteCodigo = valor("variante_codigo"),
                    .Marca = valor("marca"),
                    .DescripcionComercial = valor("descripcion_comercial"),
                    .TipoEnvase = valor("tipo_envase"),
                    .EmpaqueCodigo = valor("empaque_codigo"),
                    .EmpaqueDescripcion = valor("empaque_descripcion")}

                For Each col In ColumnasObligatorias
                    If valor(col) = "" Then errores.Add($"'{col}' es obligatorio")
                Next

                Dim contenido As Long
                If valor("contenido_por_envase") <> "" Then
                    If LeerDecimalU6(valor("contenido_por_envase"), contenido) AndAlso contenido > 0 Then
                        f.ContenidoPorEnvaseU6 = contenido
                    Else
                        errores.Add($"contenido_por_envase '{valor("contenido_por_envase")}' debe ser un numero positivo")
                    End If
                End If

                If f.TieneEmpaque Then
                    f.EnvasesPorEmpaque = LeerEnteroPositivo(valor("envases_por_empaque"), "envases_por_empaque", 0, errores)
                    f.Minimo = LeerEnteroPositivo(valor("minimo"), "minimo", 1, errores)
                    f.Multiplo = LeerEnteroPositivo(valor("multiplo"), "multiplo", 1, errores)
                    If f.EmpaqueDescripcion = "" Then f.EmpaqueDescripcion = f.EmpaqueCodigo
                ElseIf valor("envases_por_empaque") <> "" OrElse valor("minimo") <> "" OrElse valor("multiplo") <> "" Then
                    errores.Add("hay datos de empaque pero falta 'empaque_codigo'")
                End If

                If errores.Count = 0 Then
                    ' Contradicciones dentro del archivo (las repeticiones idénticas se aceptan).
                    Dim prev As FilaCatalogo = Nothing
                    If productos.TryGetValue(f.ProductoCodigo, prev) AndAlso
                       (prev.ProductoDescripcion <> f.ProductoDescripcion OrElse prev.UnidadBase <> f.UnidadBase OrElse prev.Categoria <> f.Categoria) Then
                        errores.Add($"el producto '{f.ProductoCodigo}' ya aparece en la fila {prev.Numero} con otros datos")
                    End If
                    If variantes.TryGetValue(f.VarianteCodigo, prev) AndAlso
                       (prev.ProductoCodigo <> f.ProductoCodigo OrElse prev.Marca <> f.Marca OrElse prev.DescripcionComercial <> f.DescripcionComercial OrElse
                        prev.TipoEnvase <> f.TipoEnvase OrElse prev.ContenidoPorEnvaseU6 <> f.ContenidoPorEnvaseU6) Then
                        errores.Add($"la variante '{f.VarianteCodigo}' ya aparece en la fila {prev.Numero} con otros datos")
                    End If
                    Dim claveEmpaque = f.VarianteCodigo & "|" & f.EmpaqueCodigo
                    If f.TieneEmpaque AndAlso empaques.TryGetValue(claveEmpaque, prev) AndAlso
                       (prev.EnvasesPorEmpaque <> f.EnvasesPorEmpaque OrElse prev.Minimo <> f.Minimo OrElse prev.Multiplo <> f.Multiplo OrElse
                        prev.EmpaqueDescripcion <> f.EmpaqueDescripcion) Then
                        errores.Add($"el empaque '{f.EmpaqueCodigo}' de '{f.VarianteCodigo}' ya aparece en la fila {prev.Numero} con otros datos")
                    End If
                End If

                If errores.Count > 0 Then
                    r.Errores.Add(New ErrorFila(reg.Linea, String.Join("; ", errores)))
                Else
                    If Not productos.ContainsKey(f.ProductoCodigo) Then productos(f.ProductoCodigo) = f
                    If Not variantes.ContainsKey(f.VarianteCodigo) Then variantes(f.VarianteCodigo) = f
                    If f.TieneEmpaque AndAlso Not empaques.ContainsKey(f.VarianteCodigo & "|" & f.EmpaqueCodigo) Then empaques(f.VarianteCodigo & "|" & f.EmpaqueCodigo) = f
                    r.Filas.Add(f)
                End If
            Next

            If r.Filas.Count = 0 AndAlso r.Errores.Count = 0 Then r.Errores.Add(New ErrorFila(1, "El archivo no tiene filas de datos."))
            Return r
        End Function

        ''' <summary>Acepta "4", "4.5" o "4,5". Rechaza separadores de miles y más de 6 decimales.</summary>
        Public Function LeerDecimalU6(texto As String, ByRef resultadoU6 As Long) As Boolean
            Dim t = texto.Trim()
            If t.Contains(".") AndAlso t.Contains(",") Then Return False
            t = t.Replace(","c, "."c)
            Dim d As Decimal
            If Not Decimal.TryParse(t, NumberStyles.AllowDecimalPoint Or NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, d) Then Return False
            If Decimal.Round(d, 6) <> d Then Return False
            Try
                resultadoU6 = EscalaU6.DesdeDecimal(d)
            Catch ex As OverflowException
                Return False
            End Try
            Return True
        End Function

        Private Function LeerEnteroPositivo(texto As String, columna As String, porDefecto As Long, errores As List(Of String)) As Long
            If texto = "" Then
                If porDefecto > 0 Then Return porDefecto
                errores.Add($"'{columna}' es obligatorio cuando hay empaque")
                Return 0
            End If
            Dim n As Long
            If Long.TryParse(texto, NumberStyles.None, CultureInfo.InvariantCulture, n) AndAlso n > 0 Then Return n
            errores.Add($"'{columna}' debe ser un entero positivo")
            Return 0
        End Function

        Private NotInheritable Class Registro
            Public Property Linea As Integer
            Public Property Campos As New List(Of String)
        End Class

        ''' <summary>Separa registros respetando comillas (pueden contener separador y saltos de línea).</summary>
        Private Function Separar(texto As String, sep As Char) As List(Of Registro)
            Dim registros As New List(Of Registro)
            Dim actual As New Registro With {.Linea = 1}
            Dim campo As New StringBuilder()
            Dim enComillas As Boolean = False
            Dim linea As Integer = 1
            Dim i As Integer = 0
            While i < texto.Length
                Dim c As Char = texto(i)
                If enComillas Then
                    If c = """"c Then
                        If i + 1 < texto.Length AndAlso texto(i + 1) = """"c Then
                            campo.Append(""""c) : i += 1
                        Else
                            enComillas = False
                        End If
                    Else
                        If c = ChrW(10) Then linea += 1
                        campo.Append(c)
                    End If
                ElseIf c = """"c Then
                    enComillas = True
                ElseIf c = sep Then
                    actual.Campos.Add(campo.ToString()) : campo.Clear()
                ElseIf c = ChrW(13) Then
                    ' se ignora: el fin de línea lo marca \n
                ElseIf c = ChrW(10) Then
                    actual.Campos.Add(campo.ToString()) : campo.Clear()
                    registros.Add(actual)
                    linea += 1
                    actual = New Registro With {.Linea = linea}
                Else
                    campo.Append(c)
                End If
                i += 1
            End While
            If campo.Length > 0 OrElse actual.Campos.Count > 0 Then
                actual.Campos.Add(campo.ToString())
                registros.Add(actual)
            End If
            If registros.Count = 0 Then registros.Add(New Registro With {.Linea = 1})
            Return registros
        End Function

    End Module

End Namespace
