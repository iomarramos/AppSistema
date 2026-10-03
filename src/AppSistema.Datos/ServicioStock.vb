Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Stock

    Public NotInheritable Class LineaDocumentoStock
        Public ReadOnly Property VarianteId As Long
        Public ReadOnly Property CantidadBaseU6 As Long
        Public ReadOnly Property CostoUnitarioBaseU6 As Long

        Public Sub New(varianteId As Long, cantidadBaseU6 As Long, costoUnitarioBaseU6 As Long)
            If cantidadBaseU6 <= 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "La cantidad debe ser positiva.")
            If costoUnitarioBaseU6 < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "El costo no puede ser negativo.")
            Me.VarianteId = varianteId
            Me.CantidadBaseU6 = cantidadBaseU6
            Me.CostoUnitarioBaseU6 = costoUnitarioBaseU6
        End Sub

        Private ReadOnly _valorU6 As Long?

        ''' <summary>
        ''' Línea con valor exacto conocido (p. ej. apertura: envases × precio del envase). El costo unitario base
        ''' se deriva del valor; el valor no se recalcula para no introducir residuos de redondeo.
        ''' </summary>
        Public Sub New(varianteId As Long, cantidadBaseU6 As Long, costoUnitarioBaseU6 As Long, valorU6 As Long)
            Me.New(varianteId, cantidadBaseU6, costoUnitarioBaseU6)
            If valorU6 < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "El valor no puede ser negativo.")
            _valorU6 = valorU6
        End Sub

        ''' <summary>Línea de recepción que origina esta entrada (opcional).</summary>
        Public Property RecepcionDetalleId As Long?

        Friend ReadOnly Property ValorU6 As Long
            Get
                Return If(_valorU6, EscalaU6.Multiplicar(CantidadBaseU6, CostoUnitarioBaseU6))
            End Get
        End Property
    End Class

    Public NotInheritable Class DocumentoStockNuevo
        Public ReadOnly Property AlmacenId As Long
        Public ReadOnly Property Tipo As TipoDocumentoStock
        Public ReadOnly Property Fecha As Date
        Public ReadOnly Property Numero As String
        Public ReadOnly Property Lineas As IReadOnlyList(Of LineaDocumentoStock)
        Public Property Motivo As String
        Public Property RecepcionId As Long?
        Public Property DocumentoOrigenId As Long?
        Public Property AlmacenDestinoId As Long?

        Public Sub New(almacenId As Long, tipo As TipoDocumentoStock,
                       fecha As Date, numero As String, lineas As IEnumerable(Of LineaDocumentoStock))
            If String.IsNullOrWhiteSpace(numero) Then Throw New ArgumentException("El numero es obligatorio.", NameOf(numero))
            Dim copia = New List(Of LineaDocumentoStock)(lineas)
            If copia.Count = 0 Then Throw New ReglaNegocioException("DOCUMENTO_VACIO", "El documento no tiene lineas.")
            Me.AlmacenId = almacenId
            Me.Tipo = tipo
            Me.Fecha = fecha
            Me.Numero = numero
            Me.Lineas = copia
        End Sub
    End Class

    ''' <summary>
    ''' Único publicador de movimientos de stock. Confirma documento, movimientos y (por trigger) saldo
    ''' en UNA transacción: todo o nada. NO actualiza saldo_stock: lo hace la base de datos.
    ''' Empresa, usuario y operación salen de la sesión; el almacén debe pertenecer a la operación de la sesión.
    ''' </summary>
    Public NotInheritable Class ServicioStock
        Inherits ServicioConSesion

        Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
            MyBase.New(cadenaConexion, sesion)
        End Sub

        ''' <summary>Devuelve el id del documento confirmado. Lanza ReglaNegocioException ante una regla violada.</summary>
        Public Function Contabilizar(doc As DocumentoStockNuevo) As Long
            If doc Is Nothing Then Throw New ArgumentNullException(NameOf(doc))
            Return EnTransaccion(Seguridad.Permisos.StockContabilizar, Function(u) Ejecutar(u, doc))
        End Function

        ''' <summary>Saldo por variante del almacén (cantidad en unidad base y valor).</summary>
        Public Function ConsultarSaldos(almacenId As Long, texto As String) As List(Of SaldoStockDto)
            Return EnTransaccion(Seguridad.Permisos.CatalogoVer,
                Function(u) u.Consultar(
                    "SELECT pb.codigo, pb.descripcion, v.codigo, v.descripcion_comercial, um.codigo, s.cantidad_base_u6, s.valor_u6, v.id FROM saldo_stock s " &
                    "JOIN almacen a ON a.id = s.almacen_id JOIN variante_producto v ON v.id = s.variante_id " &
                    "JOIN producto_base pb ON pb.id = v.producto_base_id JOIN unidad_medida um ON um.id = pb.unidad_base_id " &
                    "WHERE s.almacen_id = @a AND a.operacion_id = @o AND s.cantidad_base_u6 > 0 " &
                    "  AND (@t = '' OR pb.descripcion ILIKE '%' || @t || '%' OR v.descripcion_comercial ILIKE '%' || @t || '%') " &
                    "ORDER BY pb.descripcion, v.codigo",
                    Function(rd)
                        Dim d As New SaldoStockDto With {.ProductoCodigo = rd.GetString(0), .ProductoDescripcion = rd.GetString(1), .VarianteCodigo = rd.GetString(2),
                                                         .VarianteDescripcion = rd.GetString(3), .Unidad = rd.GetString(4), .CantidadBaseU6 = rd.GetInt64(5), .ValorU6 = rd.GetInt64(6), .VarianteId = rd.GetInt64(7)}
                        d.CostoPromedioU6 = EscalaU6.MultiplicarDividir(d.ValorU6, EscalaU6.Factor, d.CantidadBaseU6)
                        Return d
                    End Function, "a", almacenId, "o", Sesion.OperacionId, "t", If(texto, "").Trim()))
        End Function

        Friend Function ContabilizarEn(u As UnidadDeTrabajo, doc As DocumentoStockNuevo) As Long
            Sesion.Exigir(Seguridad.Permisos.StockContabilizar)
            Return Ejecutar(u, doc)
        End Function

        Private Shared Function Nulo(valor As Long?) As Object
            Return If(valor.HasValue, CType(valor.Value, Object), Nothing)
        End Function

        Private Function Ejecutar(u As UnidadDeTrabajo, doc As DocumentoStockNuevo) As Long
            Dim e As Long = Sesion.EmpresaId
            ' 1. Serializa las confirmaciones del almacén (secuencia sin colisiones) y valida que sea de la operación.
            If u.Escalar("SELECT id FROM almacen WHERE empresa_id = @e AND id = @a AND operacion_id = @o FOR UPDATE",
                         "e", e, "a", doc.AlmacenId, "o", Sesion.OperacionId) Is Nothing Then
                Throw New ReglaNegocioException("ALMACEN_NO_ENCONTRADO", "El almacen no existe en la operacion de trabajo.")
            End If
            Dim secuencia As Long = u.EscalarLong("SELECT COALESCE(MAX(secuencia), 0) FROM movimiento_stock WHERE empresa_id = @e AND almacen_id = @a",
                                                  "e", e, "a", doc.AlmacenId)

            ' 2. Documento en borrador y 3. líneas.
            Dim docId As Long = u.EscalarLong(
                "INSERT INTO documento_stock(empresa_id, almacen_id, numero, fecha, tipo, estado, usuario_id, motivo, recepcion_id, documento_origen_id, almacen_destino_id) " &
                "VALUES (@e, @a, @n, @f, @t, 'borrador', @u, @m, @r, @o, @ad) RETURNING id",
                "e", e, "a", doc.AlmacenId, "n", doc.Numero, "f", doc.Fecha.Date, "t", TiposDocumentoStockInfo.Codigo(doc.Tipo), "u", Sesion.UsuarioId,
                "m", doc.Motivo, "r", Nulo(doc.RecepcionId), "o", Nulo(doc.DocumentoOrigenId), "ad", Nulo(doc.AlmacenDestinoId))

            ' Valoración (D01): una salida vale cantidad × costo vigente del saldo. El saldo se lee sin bloquearlo:
            ' toda contabilización del almacén ya está serializada por el bloqueo de su fila (paso 1). Varias líneas
            ' de la misma variante descuentan del saldo en memoria.
            Dim signo As Long = TiposDocumentoStockInfo.Signo(doc.Tipo)
            Dim valores As New List(Of (Costo As Long, Valor As Long))
            Dim saldos As New Dictionary(Of Long, (Cantidad As Long, Valor As Long))
            For Each l In doc.Lineas
                If signo = 1 Then
                    valores.Add((l.CostoUnitarioBaseU6, l.ValorU6))
                Else
                    Dim saldo As (Cantidad As Long, Valor As Long) = Nothing
                    If Not saldos.TryGetValue(l.VarianteId, saldo) Then
                        saldo = u.Consultar("SELECT cantidad_base_u6, valor_u6 FROM saldo_stock WHERE empresa_id = @e AND almacen_id = @a AND variante_id = @v",
                                            Function(rd) (rd.GetInt64(0), rd.GetInt64(1)), "e", e, "a", doc.AlmacenId, "v", l.VarianteId).FirstOrDefault()
                    End If
                    Dim valor = Valoracion.ValorSalidaU6(saldo.Cantidad, saldo.Valor, l.CantidadBaseU6)
                    saldos(l.VarianteId) = (saldo.Cantidad - l.CantidadBaseU6, saldo.Valor - valor)
                    valores.Add((Valoracion.CostoUnitarioU6(valor, l.CantidadBaseU6), valor))
                End If
            Next

            Dim detalleIds As New List(Of Long)
            For i As Integer = 0 To doc.Lineas.Count - 1
                Dim l = doc.Lineas(i)
                detalleIds.Add(u.EscalarLong(
                    "INSERT INTO documento_stock_detalle(empresa_id, documento_id, variante_id, cantidad_base_u6, costo_unitario_base_u6, valor_u6, recepcion_detalle_id) " &
                    "VALUES (@e, @d, @v, @c, @k, @val, @rd) RETURNING id",
                    "e", e, "d", docId, "v", l.VarianteId, "c", l.CantidadBaseU6, "k", valores(i).Costo, "val", valores(i).Valor, "rd", Nulo(l.RecepcionDetalleId)))
            Next

            ' 4. Confirmar y 5. un movimiento por línea (el trigger valida y actualiza el saldo).
            u.Ejecutar("UPDATE documento_stock SET estado = 'confirmado' WHERE empresa_id = @e AND id = @d", "e", e, "d", docId)
            For i As Integer = 0 To doc.Lineas.Count - 1
                Dim l = doc.Lineas(i)
                secuencia += 1
                u.Ejecutar(
                    "INSERT INTO movimiento_stock(empresa_id, documento_detalle_id, almacen_id, variante_id, fecha, secuencia, signo, " &
                    "cantidad_base_u6, costo_unitario_base_u6, valor_u6, usuario_id) VALUES (@e, @dd, @a, @v, @f, @s, @sg, @c, @k, @val, @u)",
                    "e", e, "dd", detalleIds(i), "a", doc.AlmacenId, "v", l.VarianteId, "f", doc.Fecha.Date, "s", secuencia, "sg", signo,
                    "c", l.CantidadBaseU6, "k", valores(i).Costo, "val", valores(i).Valor, "u", Sesion.UsuarioId)
            Next
            Return docId
        End Function

    End Class

Public NotInheritable Class SaldoStockDto
    Public Property VarianteId As Long
    Public Property ProductoCodigo As String
    Public Property ProductoDescripcion As String
    Public Property VarianteCodigo As String
    Public Property VarianteDescripcion As String
    Public Property Unidad As String
    Public Property CantidadBaseU6 As Long
    Public Property ValorU6 As Long
    Public Property CostoPromedioU6 As Long
End Class
