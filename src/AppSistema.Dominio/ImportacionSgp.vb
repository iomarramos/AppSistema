Imports System.Globalization
Imports System.Text
Imports System.Text.RegularExpressions
Imports AppSistema.Dominio.Numerico

Namespace Importacion

    ''' <summary>Un producto del listado del SGP ya interpretado.</summary>
    Public NotInheritable Class ProductoSgp
        ''' <summary>Línea del archivo SGP (la cabecera es la 1).</summary>
        Public Property Linea As Integer
        Public Property Codigo As String
        Public Property Nombre As String
        Public Property CodUni As Integer
        ''' <summary>Presentación en que el almacén pide y recibe (unidad mínima de pedido).</summary>
        Public Property Presentacion As String
        ''' <summary>pro_facing del SGP tal cual: contenido de una presentación en la unidad base.</summary>
        Public Property FactorTexto As String
        Public Property FactorU6 As Long
        ''' <summary>KG, L o UND.</summary>
        Public Property UnidadBase As String
        ''' <summary>Vacío si el nombre confirma el factor; si no, por qué conviene revisarlo.</summary>
        Public Property Observacion As String
        Public Property Categoria As String
    End Class

    Public NotInheritable Class ConversionSgp
        Public ReadOnly Property Productos As New List(Of ProductoSgp)
        Public ReadOnly Property Errores As New List(Of ErrorFila)
        ''' <summary>Filas idénticas a otra anterior (se cargan una sola vez).</summary>
        Public Property RepetidasIdenticas As Integer

        Public ReadOnly Property ConObservacion As IEnumerable(Of ProductoSgp)
            Get
                Return Productos.Where(Function(p) p.Observacion <> "")
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Convierte el listado de productos exportado del SGP (pro_nombre, pro_coduni, pro_facing; separado por
    ''' tabulaciones o ";") al formato del importador de catálogo.
    ''' Reglas:
    ''' - pro_facing es el contenido de UNA presentación (pro_coduni) en la unidad base; se guarda tal cual.
    ''' - La unidad base (KG, L o UND) se deduce comparando el factor con el tamaño escrito en el nombre
    '''   (tolerancia 3 %). Si no hay tamaño: código 23 → KG, código 26 → L, factor 1 o entero → UND.
    ''' - Cuando el nombre contradice el factor se carga igual con el factor del SGP y se deja una observación.
    ''' - La presentación del SGP es la unidad mínima de pedido del almacén: empaque de 1 envase, mínimo 1, múltiplo 1.
    ''' - Códigos SGP00001… en el orden del archivo; mismo archivo → mismos códigos.
    ''' </summary>
    Public Module ConversorSgp

        Public Const UnidadKg As String = "KG"
        Public Const UnidadLitro As String = "L"
        Public Const UnidadConteo As String = "UND"

        ''' <summary>Significado de pro_coduni deducido del listado. Los no listados quedan como "PRES-SGP-n" hasta confirmarlos.</summary>
        Public ReadOnly Property Presentaciones As IReadOnlyDictionary(Of Integer, String) = New Dictionary(Of Integer, String) From {
            {4, "BIDON"}, {5, "BALDE"}, {8, "BOLSA"}, {9, "BOTELLA"}, {10, "CAJA"}, {14, "CAJETILLA"}, {18, "FRASCO"},
            {19, "GALON"}, {22, "KIT"}, {23, "KILOGRAMO"}, {24, "LATA"}, {26, "LITRO"}, {31, "PAQUETE"}, {32, "PAR"},
            {34, "POTE"}, {36, "ROLLO"}, {38, "SACHET"}, {39, "SACO"}, {41, "SIXPACK"}, {42, "SOBRE"}, {44, "TUBO"},
            {45, "UNIDAD"}, {46, "VASO"}}

        Public Function NombrePresentacion(codUni As Integer) As String
            Dim n As String = Nothing
            Return If(Presentaciones.TryGetValue(codUni, n), n, "PRES-SGP-" & codUni.ToString(CultureInfo.InvariantCulture))
        End Function

        Public Function EsListadoSgp(texto As String) As Boolean
            If texto Is Nothing Then Return False
            Dim primera = texto.TrimStart(ChrW(&HFEFF)).Split({vbLf}, StringSplitOptions.None)(0).ToLowerInvariant()
            Return primera.Contains("pro_nombre") AndAlso primera.Contains("pro_facing")
        End Function

        Public Function Convertir(texto As String) As ConversionSgp
            Dim r As New ConversionSgp()
            Dim lineas = If(texto, "").TrimStart(ChrW(&HFEFF)).Replace(vbCrLf, vbLf).Replace(vbCr, vbLf).Split({vbLf}, StringSplitOptions.None)
            Dim cab = lineas(0).ToLowerInvariant().Split({vbTab, ";"}, StringSplitOptions.None).Select(Function(c) c.Trim()).ToList()
            Dim iNom = cab.IndexOf("pro_nombre"), iUni = cab.IndexOf("pro_coduni"), iFac = cab.IndexOf("pro_facing")
            If iNom < 0 OrElse iUni < 0 OrElse iFac < 0 Then
                r.Errores.Add(New ErrorFila(1, "La cabecera debe tener pro_nombre, pro_coduni y pro_facing."))
                Return r
            End If
            Dim sep = If(lineas(0).Contains(vbTab), vbTab, ";")

            Dim vistos As New Dictionary(Of String, ProductoSgp)(StringComparer.Ordinal)
            Dim porNombre As New Dictionary(Of String, Integer)(StringComparer.Ordinal)
            For i = 1 To lineas.Length - 1
                If lineas(i).Trim() = "" Then Continue For
                Dim c = lineas(i).Split({sep}, StringSplitOptions.None)
                Dim linea = i + 1
                If c.Length <= Math.Max(iNom, Math.Max(iUni, iFac)) Then
                    r.Errores.Add(New ErrorFila(linea, "faltan columnas")) : Continue For
                End If
                Dim nombre = Normalizar(c(iNom))
                Dim codUni As Integer, factor As Decimal
                If nombre = "" Then r.Errores.Add(New ErrorFila(linea, "pro_nombre vacio")) : Continue For
                If Not Integer.TryParse(c(iUni).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, codUni) Then
                    r.Errores.Add(New ErrorFila(linea, $"pro_coduni '{c(iUni).Trim()}' no es un numero entero")) : Continue For
                End If
                Dim facTexto = c(iFac).Trim().Replace(","c, "."c)
                If Not Decimal.TryParse(facTexto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, factor) OrElse factor <= 0D OrElse Decimal.Round(factor, 6) <> factor Then
                    r.Errores.Add(New ErrorFila(linea, $"pro_facing '{c(iFac).Trim()}' debe ser un numero positivo con hasta 6 decimales")) : Continue For
                End If

                Dim clave = nombre & "|" & codUni.ToString(CultureInfo.InvariantCulture) & "|" & factor.ToString(CultureInfo.InvariantCulture)
                If vistos.ContainsKey(clave) Then r.RepetidasIdenticas += 1 : Continue For

                Dim p As New ProductoSgp With {
                    .Linea = linea, .Nombre = nombre, .CodUni = codUni, .Presentacion = NombrePresentacion(codUni),
                    .FactorTexto = factor.ToString(CultureInfo.InvariantCulture), .FactorU6 = EscalaU6.DesdeDecimal(factor),
                    .Categoria = If(nombre.StartsWith("CAJA CHICA", StringComparison.Ordinal), "CAJA CHICA", "")}
                Dim obs As String = ""
                p.UnidadBase = DeducirUnidadBase(nombre, codUni, factor, obs)
                p.Observacion = obs
                vistos(clave) = p
                p.Codigo = "SGP" & (r.Productos.Count + 1).ToString("00000", CultureInfo.InvariantCulture)
                r.Productos.Add(p)
                porNombre(nombre) = If(porNombre.ContainsKey(nombre), porNombre(nombre) + 1, 1)
            Next

            ' El mismo nombre con otra presentación o factor es otro producto: se distingue en la descripción.
            For Each p In r.Productos.Where(Function(x) porNombre(x.Nombre) > 1)
                p.Nombre = $"{p.Nombre} ({p.Presentacion} x {p.FactorTexto} {p.UnidadBase})"
                p.Observacion = If(p.Observacion = "", "", p.Observacion & "; ") & "el SGP tiene este nombre con otra presentacion o factor"
            Next
            Return r
        End Function

        ''' <summary>Texto en el formato de <see cref="LectorCsvCatalogo"/>; la fila i+2 del CSV es r.Productos(i).</summary>
        Public Function GenerarCsvCatalogo(r As ConversionSgp) As String
            Dim sb As New StringBuilder()
            sb.Append("producto_codigo;producto_descripcion;unidad_base;categoria;variante_codigo;marca;descripcion_comercial;tipo_envase;contenido_por_envase;")
            sb.Append("empaque_codigo;empaque_descripcion;envases_por_empaque;minimo;multiplo").Append(vbLf)
            For Each p In r.Productos
                Dim campos = {p.Codigo, p.Nombre, p.UnidadBase, p.Categoria, p.Codigo, "", p.Nombre, p.Presentacion, p.FactorTexto,
                              p.Presentacion, $"{p.Presentacion} x {p.FactorTexto} {p.UnidadBase}", "1", "1", "1"}
                sb.Append(String.Join(";", campos.Select(AddressOf Campo))).Append(vbLf)
            Next
            Return sb.ToString()
        End Function

        Private Function Campo(s As String) As String
            If s.IndexOfAny({";"c, """"c}) < 0 Then Return s
            Return """" & s.Replace("""", """""") & """"
        End Function

        Private Function Normalizar(s As String) As String
            Return String.Join(" ", s.Split({" "c, ChrW(160), vbTab(0)}, StringSplitOptions.RemoveEmptyEntries))
        End Function

        ' ---------- Deducción de la unidad base ----------

        Private Enum Dim3
            Masa
            Volumen
            Conteo
        End Enum

        Private Structure Tamano
            Public Dimension As Dim3
            Public EnBase As Decimal   ' kg, l o unidades
            Public Numero As Decimal   ' el número tal como aparece
        End Structure

        Private ReadOnly Masa As New Dictionary(Of String, Decimal)(StringComparer.Ordinal) From {
            {"KG", 1D}, {"KGS", 1D}, {"KGR", 1D}, {"KGM", 1D}, {"KILO", 1D}, {"KILOS", 1D}, {"KILOGRAMO", 1D}, {"KILOGRAMOS", 1D},
            {"GR", 0.001D}, {"GRS", 0.001D}, {"GRM", 0.001D}, {"GRAMOS", 0.001D}, {"G", 0.001D}, {"OZ", 0.0283495D}}
        Private ReadOnly Volumen As New Dictionary(Of String, Decimal)(StringComparer.Ordinal) From {
            {"LT", 1D}, {"LTS", 1D}, {"LTR", 1D}, {"L", 1D}, {"LITRO", 1D}, {"LITROS", 1D},
            {"ML", 0.001D}, {"MLT", 0.001D}, {"MLS", 0.001D}, {"CC", 0.001D},
            {"GL", 3.785D}, {"GLN", 3.785D}, {"GALON", 3.785D}, {"GALONES", 3.785D}}
        Private ReadOnly Conteo As New Dictionary(Of String, Decimal)(StringComparer.Ordinal) From {
            {"U", 1D}, {"UN", 1D}, {"UND", 1D}, {"UNDS", 1D}, {"UNI", 1D}, {"UNID", 1D}, {"UNIDS", 1D}, {"UNIDAD", 1D}, {"UNIDADES", 1D},
            {"SOBRE", 1D}, {"SOBRES", 1D}, {"PZA", 1D}, {"PZAS", 1D}, {"HOJAS", 1D}, {"MILLAR", 1000D}, {"MLL", 1000D}, {"MILL", 1000D}}

        Private ReadOnly RxTamano As New Regex("(?:(\d+)X)?(\d+(?:[.,]\d+)?)(?:\s*/\s*(\d+(?:[.,]\d+)?))?\s*([A-ZÑ]+)(?![A-ZÑ])", RegexOptions.CultureInvariant)
        Private ReadOnly RxMillar As New Regex("\b(MILLAR|MLL|MILL)\b", RegexOptions.CultureInvariant)
        Private ReadOnly RxPorN As New Regex("\bX\s*(\d+)(?![\d.,]|\s*[A-ZÑ])", RegexOptions.CultureInvariant)

        Private Function Tamanos(nombre As String) As List(Of Tamano)
            Dim t As New List(Of Tamano)
            For Each m As Match In RxTamano.Matches(nombre)
                Dim u = m.Groups(4).Value
                Dim tabla As Dictionary(Of String, Decimal) = Nothing, d As Dim3
                If Masa.ContainsKey(u) Then
                    tabla = Masa : d = Dim3.Masa
                ElseIf Volumen.ContainsKey(u) Then
                    tabla = Volumen : d = Dim3.Volumen
                ElseIf Conteo.ContainsKey(u) Then
                    tabla = Conteo : d = Dim3.Conteo
                Else
                    Continue For
                End If
                Dim mult = If(m.Groups(1).Success, Dec(m.Groups(1).Value), 1D)
                Dim a = Dec(m.Groups(2).Value)
                Dim valores As New List(Of Decimal) From {a}
                If m.Groups(3).Success Then
                    Dim b = Dec(m.Groups(3).Value)
                    valores.Add(b)
                    If b <> 0D AndAlso a < b Then valores.Add(a / b)
                End If
                For Each v In valores
                    t.Add(New Tamano With {.Dimension = d, .EnBase = mult * v * tabla(u), .Numero = v})
                Next
            Next
            If RxMillar.IsMatch(nombre) Then t.Add(New Tamano With {.Dimension = Dim3.Conteo, .EnBase = 1000D, .Numero = 1000D})
            For Each m As Match In RxPorN.Matches(nombre)
                Dim n = Dec(m.Groups(1).Value)
                t.Add(New Tamano With {.Dimension = Dim3.Conteo, .EnBase = n, .Numero = n})
            Next
            Return t
        End Function

        Private Function Dec(s As String) As Decimal
            Return Decimal.Parse(s.Replace(","c, "."c), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)
        End Function

        Private Function Cerca(a As Decimal, b As Decimal) As Boolean
            Return b > 0D AndAlso Math.Abs(a - b) / b <= 0.03D
        End Function

        Private Function Codigo(d As Dim3) As String
            Select Case d
                Case Dim3.Masa : Return UnidadKg
                Case Dim3.Volumen : Return UnidadLitro
                Case Else : Return UnidadConteo
            End Select
        End Function

        Friend Function DeducirUnidadBase(nombre As String, codUni As Integer, factor As Decimal, ByRef observacion As String) As String
            observacion = ""
            Dim t = Tamanos(nombre)
            Dim coincide = t.Where(Function(x) Cerca(factor, x.EnBase)).ToList()
            If coincide.Count > 0 Then Return Codigo(coincide(0).Dimension)
            If codUni = 23 AndAlso factor = 1D Then Return UnidadKg
            If codUni = 26 AndAlso factor = 1D Then Return UnidadLitro
            If factor = 1D Then Return UnidadConteo

            Dim medidas = t.Where(Function(x) x.Dimension <> Dim3.Conteo).ToList()
            Dim conteos = t.Where(Function(x) x.Dimension = Dim3.Conteo).ToList()
            Dim fTexto = factor.ToString(CultureInfo.InvariantCulture)
            If factor <> Decimal.Truncate(factor) Then
                Dim mismoNumero = medidas.Where(Function(x) Cerca(factor, x.Numero)).ToList()
                If mismoNumero.Count > 0 Then
                    Dim u = Codigo(mismoNumero(0).Dimension)
                    observacion = $"el nombre indica otra unidad que el factor {fTexto}; se toma {fTexto} {u}"
                    Return u
                End If
                If medidas.Count > 0 Then
                    Dim u = Codigo(medidas(0).Dimension)
                    observacion = $"el nombre indica {medidas(0).EnBase.ToString("0.######", CultureInfo.InvariantCulture)} {u} y el factor es {fTexto}; se toma {fTexto} {u}"
                    Return u
                End If
                Dim inverso = 1D / factor
                If Math.Abs(inverso - Math.Round(inverso)) < 0.01D Then
                    observacion = $"factor {fTexto} = 1/{Math.Round(inverso)}: la unidad base del SGP es un paquete de {Math.Round(inverso)}"
                    Return UnidadConteo
                End If
                observacion = $"factor decimal {fTexto} sin tamano en el nombre; se asume KG"
                Return UnidadKg
            End If

            If conteos.Count > 0 Then
                observacion = $"el nombre indica {conteos(0).EnBase.ToString("0.######", CultureInfo.InvariantCulture)} unidades y el factor es {fTexto}; se toma {fTexto} UND"
                Return UnidadConteo
            End If
            If medidas.Count > 0 Then
                Dim u = Codigo(medidas(0).Dimension)
                observacion = $"el nombre indica {medidas(0).EnBase.ToString("0.######", CultureInfo.InvariantCulture)} {u} y el factor es {fTexto}; se toma {fTexto} {u}"
                Return u
            End If
            Return UnidadConteo
        End Function

    End Module

End Namespace
