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

        Friend ReadOnly Property ValorU6 As Long
            Get
                Return EscalaU6.Multiplicar(CantidadBaseU6, CostoUnitarioBaseU6)
            End Get
        End Property
    End Class

    Public NotInheritable Class DocumentoStockNuevo
        Public ReadOnly Property AlmacenId As Long
        Public ReadOnly Property Tipo As TipoDocumentoStock
        Public ReadOnly Property Fecha As Date
        Public ReadOnly Property Numero As String
        Public ReadOnly Property Lineas As IReadOnlyList(Of LineaDocumentoStock)

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
                "INSERT INTO documento_stock(empresa_id, almacen_id, numero, fecha, tipo, estado, usuario_id) " &
                "VALUES (@e, @a, @n, @f, @t, 'borrador', @u) RETURNING id",
                "e", e, "a", doc.AlmacenId, "n", doc.Numero, "f", doc.Fecha.Date, "t", TiposDocumentoStockInfo.Codigo(doc.Tipo), "u", Sesion.UsuarioId)
            Dim detalleIds As New List(Of Long)
            For Each l In doc.Lineas
                detalleIds.Add(u.EscalarLong(
                    "INSERT INTO documento_stock_detalle(empresa_id, documento_id, variante_id, cantidad_base_u6, costo_unitario_base_u6, valor_u6) " &
                    "VALUES (@e, @d, @v, @c, @k, @val) RETURNING id",
                    "e", e, "d", docId, "v", l.VarianteId, "c", l.CantidadBaseU6, "k", l.CostoUnitarioBaseU6, "val", l.ValorU6))
            Next

            ' 4. Confirmar y 5. un movimiento por línea (el trigger valida y actualiza el saldo).
            u.Ejecutar("UPDATE documento_stock SET estado = 'confirmado' WHERE empresa_id = @e AND id = @d", "e", e, "d", docId)
            Dim signo As Long = TiposDocumentoStockInfo.Signo(doc.Tipo)
            For i As Integer = 0 To doc.Lineas.Count - 1
                Dim l = doc.Lineas(i)
                secuencia += 1
                u.Ejecutar(
                    "INSERT INTO movimiento_stock(empresa_id, documento_detalle_id, almacen_id, variante_id, fecha, secuencia, signo, " &
                    "cantidad_base_u6, costo_unitario_base_u6, valor_u6, usuario_id) VALUES (@e, @dd, @a, @v, @f, @s, @sg, @c, @k, @val, @u)",
                    "e", e, "dd", detalleIds(i), "a", doc.AlmacenId, "v", l.VarianteId, "f", doc.Fecha.Date, "s", secuencia, "sg", signo,
                    "c", l.CantidadBaseU6, "k", l.CostoUnitarioBaseU6, "val", l.ValorU6, "u", Sesion.UsuarioId)
            Next
            Return docId
        End Function

    End Class
