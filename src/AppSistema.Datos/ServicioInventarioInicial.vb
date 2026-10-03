Imports AppSistema.Dominio
Imports AppSistema.Dominio.Importacion
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock

Public NotInheritable Class LineaAperturaDto
    Public Property Linea As Integer
    Public Property VarianteCodigo As String
    Public Property Descripcion As String
    Public Property Presentacion As String
    Public Property StockEnvasesU6 As Long
    Public Property PrecioEnvaseU6 As Long
    Public Property CantidadBaseU6 As Long
    Public Property Unidad As String
    Public Property ValorU6 As Long
    Public Property Problema As String
End Class

Public NotInheritable Class ResultadoApertura
    Public ReadOnly Property Lineas As New List(Of LineaAperturaDto)
    Public Property DocumentoId As Long?
    ''' <summary>Motivo por el que el almacén no admite apertura (ya tiene movimientos), si aplica.</summary>
    Public Property Bloqueo As String

    Public ReadOnly Property HayErrores As Boolean
        Get
            Return Bloqueo IsNot Nothing OrElse Lineas.Any(Function(l) l.Problema IsNot Nothing)
        End Get
    End Property

    Public ReadOnly Property ValorTotalU6 As Long
        Get
            Return Lineas.Where(Function(l) l.Problema Is Nothing).Sum(Function(l) l.ValorU6)
        End Get
    End Property

    Public ReadOnly Property Resumen As String
        Get
            Return $"{Lineas.Where(Function(l) l.Problema Is Nothing).Count()} lineas validas, {Lineas.Where(Function(l) l.Problema IsNot Nothing).Count()} con error; " &
                   $"valor total {EscalaU6.ADecimal(ValorTotalU6):N2}" & If(Bloqueo Is Nothing, "", ". " & Bloqueo)
        End Get
    End Property
End Class

''' <summary>
''' Inventario inicial valorizado de un almacén: un documento de APERTURA confirmado (todo o nada) con
''' cantidad base = envases × contenido del envase del catálogo y valor = envases × precio del envase (exacto).
''' Solo se admite en un almacén sin movimientos: repetirlo no duplica el stock.
''' </summary>
Public NotInheritable Class ServicioInventarioInicial
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    Public Function VistaPrevia(almacenId As Long, textoCsv As String) As ResultadoApertura
        Return EnTransaccion(Seguridad.Permisos.StockContabilizar, Function(u) Procesar(u, almacenId, textoCsv))
    End Function

    Public Function Aplicar(almacenId As Long, fecha As Date, textoCsv As String) As ResultadoApertura
        Return EnTransaccion(Seguridad.Permisos.StockContabilizar,
            Function(u)
                Dim r = Procesar(u, almacenId, textoCsv)
                If r.HayErrores Then
                    Throw New ReglaNegocioException("IMPORTACION_CON_ERRORES", If(r.Bloqueo, "El archivo tiene lineas con error; no se cargo nada."))
                End If
                Dim ids = u.Consultar("SELECT codigo, id FROM variante_producto", Function(rd) (rd.GetString(0), rd.GetInt64(1))) _
                           .ToDictionary(Function(x) x.Item1, Function(x) x.Item2, StringComparer.Ordinal)
                Dim codigoAlmacen = CStr(u.Escalar("SELECT codigo FROM almacen WHERE id = @a", "a", almacenId))
                Dim lineas = r.Lineas.Select(Function(l) New LineaDocumentoStock(ids(l.VarianteCodigo), l.CantidadBaseU6,
                                                 EscalaU6.MultiplicarDividir(l.ValorU6, EscalaU6.Factor, l.CantidadBaseU6), l.ValorU6))
                Dim doc As New DocumentoStockNuevo(almacenId, TipoDocumentoStock.Apertura, fecha, $"APERTURA-{codigoAlmacen}", lineas)
                r.DocumentoId = New ServicioStock(CadenaConexion, Sesion).ContabilizarEn(u, doc)
                Return r
            End Function)
    End Function

    Private Function Procesar(u As UnidadDeTrabajo, almacenId As Long, texto As String) As ResultadoApertura
        Dim r As New ResultadoApertura()
        If u.Escalar("SELECT 1 FROM almacen WHERE id = @a AND operacion_id = @o", "a", almacenId, "o", Sesion.OperacionId) Is Nothing Then
            Throw New ReglaNegocioException("OPERACION_AJENA", "El almacen no pertenece a la operacion seleccionada.")
        End If
        If u.Escalar("SELECT 1 FROM movimiento_stock WHERE almacen_id = @a LIMIT 1", "a", almacenId) IsNot Nothing Then
            r.Bloqueo = "El almacen ya tiene movimientos (o ya se cargo su inventario inicial): la apertura solo se registra una vez."
        End If
        Dim lectura = LectorInventarioInicial.Leer(texto)
        For Each e In lectura.Errores
            r.Lineas.Add(New LineaAperturaDto With {.Linea = e.Numero, .Problema = e.Mensaje})
        Next
        Dim variantes = u.Consultar(
            "SELECT v.codigo, v.descripcion_comercial, v.tipo_envase, v.contenido_base_por_envase_u6, um.codigo FROM variante_producto v " &
            "JOIN producto_base p ON p.id = v.producto_base_id JOIN unidad_medida um ON um.id = p.unidad_base_id",
            Function(rd) (Codigo:=rd.GetString(0), Desc:=rd.GetString(1), Envase:=rd.GetString(2), Contenido:=rd.GetInt64(3), Unidad:=rd.GetString(4))) _
            .ToDictionary(Function(x) x.Codigo, StringComparer.Ordinal)
        For Each l In lectura.Lineas
            Dim d As New LineaAperturaDto With {.Linea = l.Linea, .VarianteCodigo = l.VarianteCodigo, .StockEnvasesU6 = l.StockEnvasesU6, .PrecioEnvaseU6 = l.PrecioEnvaseU6}
            Dim v As (Codigo As String, Desc As String, Envase As String, Contenido As Long, Unidad As String) = Nothing
            If Not variantes.TryGetValue(l.VarianteCodigo, v) Then
                d.Problema = $"la variante '{l.VarianteCodigo}' no existe en el catalogo"
            Else
                d.Descripcion = v.Desc : d.Presentacion = v.Envase : d.Unidad = v.Unidad
                d.CantidadBaseU6 = EscalaU6.Multiplicar(l.StockEnvasesU6, v.Contenido)
                d.ValorU6 = EscalaU6.Multiplicar(l.StockEnvasesU6, l.PrecioEnvaseU6)
                If d.CantidadBaseU6 <= 0 Then d.Problema = "la cantidad en unidad base redondea a cero"
            End If
            r.Lineas.Add(d)
        Next
        r.Lineas.Sort(Function(a, b) a.Linea.CompareTo(b.Linea))
        Return r
    End Function

End Class
