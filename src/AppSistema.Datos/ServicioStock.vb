Imports Npgsql
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
        Public ReadOnly Property EmpresaId As Long
        Public ReadOnly Property AlmacenId As Long
        Public ReadOnly Property UsuarioId As Long
        Public ReadOnly Property Tipo As TipoDocumentoStock
        Public ReadOnly Property Fecha As Date
        Public ReadOnly Property Numero As String
        Public ReadOnly Property Lineas As IReadOnlyList(Of LineaDocumentoStock)

        Public Sub New(empresaId As Long, almacenId As Long, usuarioId As Long, tipo As TipoDocumentoStock,
                       fecha As Date, numero As String, lineas As IEnumerable(Of LineaDocumentoStock))
            If String.IsNullOrWhiteSpace(numero) Then Throw New ArgumentException("El numero es obligatorio.", NameOf(numero))
            Dim copia = New List(Of LineaDocumentoStock)(lineas)
            If copia.Count = 0 Then Throw New ReglaNegocioException("DOCUMENTO_VACIO", "El documento no tiene lineas.")
            Me.EmpresaId = empresaId
            Me.AlmacenId = almacenId
            Me.UsuarioId = usuarioId
            Me.Tipo = tipo
            Me.Fecha = fecha
            Me.Numero = numero
            Me.Lineas = copia
        End Sub
    End Class

    ''' <summary>
    ''' Único publicador de movimientos de stock. Confirma documento, movimientos y (por trigger) saldo
    ''' en UNA transacción: todo o nada. NO actualiza saldo_stock: lo hace la base de datos.
    ''' La empresa se valida contra el almacén; la aplicación debe obtenerla de la sesión autorizada.
    ''' </summary>
    Public NotInheritable Class ServicioStock

        Private ReadOnly _cadenaConexion As String

        Public Sub New(cadenaConexion As String)
            If String.IsNullOrWhiteSpace(cadenaConexion) Then Throw New ArgumentException("Falta la cadena de conexion.", NameOf(cadenaConexion))
            _cadenaConexion = cadenaConexion
        End Sub

        ''' <summary>Devuelve el id del documento confirmado. Lanza ReglaNegocioException ante una regla violada.</summary>
        Public Function Contabilizar(doc As DocumentoStockNuevo) As Long
            Using cn As New NpgsqlConnection(_cadenaConexion)
                cn.Open()
                Using tx As NpgsqlTransaction = cn.BeginTransaction()
                    Try
                        Dim id As Long = Ejecutar(cn, tx, doc)
                        tx.Commit()
                        Return id
                    Catch ex As PostgresException
                        tx.Rollback()
                        Throw Traducir(ex)
                    End Try
                End Using
            End Using
        End Function

        Private Shared Function Ejecutar(cn As NpgsqlConnection, tx As NpgsqlTransaction, doc As DocumentoStockNuevo) As Long
            ' 1. Serializa las confirmaciones del almacén (asigna secuencias sin colisión) y valida la empresa.
            Using cmd As New NpgsqlCommand("SELECT id FROM almacen WHERE empresa_id = @e AND id = @a FOR UPDATE", cn, tx)
                cmd.Parameters.AddWithValue("e", doc.EmpresaId)
                cmd.Parameters.AddWithValue("a", doc.AlmacenId)
                If cmd.ExecuteScalar() Is Nothing Then
                    Throw New ReglaNegocioException("ALMACEN_NO_ENCONTRADO", "El almacen no existe para la empresa.")
                End If
            End Using

            Dim secuencia As Long
            Using cmd As New NpgsqlCommand("SELECT COALESCE(MAX(secuencia), 0) FROM movimiento_stock WHERE empresa_id = @e AND almacen_id = @a", cn, tx)
                cmd.Parameters.AddWithValue("e", doc.EmpresaId)
                cmd.Parameters.AddWithValue("a", doc.AlmacenId)
                secuencia = Convert.ToInt64(cmd.ExecuteScalar())
            End Using

            ' 2. Documento en borrador.
            Dim docId As Long
            Using cmd As New NpgsqlCommand(
                "INSERT INTO documento_stock(empresa_id, almacen_id, numero, fecha, tipo, estado, usuario_id) " &
                "VALUES (@e, @a, @n, @f, @t, 'borrador', @u) RETURNING id", cn, tx)
                cmd.Parameters.AddWithValue("e", doc.EmpresaId)
                cmd.Parameters.AddWithValue("a", doc.AlmacenId)
                cmd.Parameters.AddWithValue("n", doc.Numero)
                cmd.Parameters.AddWithValue("f", doc.Fecha)
                cmd.Parameters.AddWithValue("t", TiposDocumentoStockInfo.Codigo(doc.Tipo))
                cmd.Parameters.AddWithValue("u", doc.UsuarioId)
                docId = Convert.ToInt64(cmd.ExecuteScalar())
            End Using

            ' 3. Líneas.
            Dim detalleIds As New List(Of Long)
            For Each l In doc.Lineas
                Using cmd As New NpgsqlCommand(
                    "INSERT INTO documento_stock_detalle(empresa_id, documento_id, variante_id, cantidad_base_u6, costo_unitario_base_u6, valor_u6) " &
                    "VALUES (@e, @d, @v, @c, @k, @val) RETURNING id", cn, tx)
                    cmd.Parameters.AddWithValue("e", doc.EmpresaId)
                    cmd.Parameters.AddWithValue("d", docId)
                    cmd.Parameters.AddWithValue("v", l.VarianteId)
                    cmd.Parameters.AddWithValue("c", l.CantidadBaseU6)
                    cmd.Parameters.AddWithValue("k", l.CostoUnitarioBaseU6)
                    cmd.Parameters.AddWithValue("val", l.ValorU6)
                    detalleIds.Add(Convert.ToInt64(cmd.ExecuteScalar()))
                End Using
            Next

            ' 4. Confirmar y 5. un movimiento por línea.
            Using cmd As New NpgsqlCommand("UPDATE documento_stock SET estado = 'confirmado' WHERE empresa_id = @e AND id = @d", cn, tx)
                cmd.Parameters.AddWithValue("e", doc.EmpresaId)
                cmd.Parameters.AddWithValue("d", docId)
                cmd.ExecuteNonQuery()
            End Using

            Dim signo As Integer = TiposDocumentoStockInfo.Signo(doc.Tipo)
            For i As Integer = 0 To doc.Lineas.Count - 1
                Dim l = doc.Lineas(i)
                secuencia += 1
                Using cmd As New NpgsqlCommand(
                    "INSERT INTO movimiento_stock(empresa_id, documento_detalle_id, almacen_id, variante_id, fecha, secuencia, signo, " &
                    "cantidad_base_u6, costo_unitario_base_u6, valor_u6, usuario_id) " &
                    "VALUES (@e, @dd, @a, @v, @f, @s, @sg, @c, @k, @val, @u)", cn, tx)
                    cmd.Parameters.AddWithValue("e", doc.EmpresaId)
                    cmd.Parameters.AddWithValue("dd", detalleIds(i))
                    cmd.Parameters.AddWithValue("a", doc.AlmacenId)
                    cmd.Parameters.AddWithValue("v", l.VarianteId)
                    cmd.Parameters.AddWithValue("f", doc.Fecha)
                    cmd.Parameters.AddWithValue("s", secuencia)
                    cmd.Parameters.AddWithValue("sg", CShort(signo))
                    cmd.Parameters.AddWithValue("c", l.CantidadBaseU6)
                    cmd.Parameters.AddWithValue("k", l.CostoUnitarioBaseU6)
                    cmd.Parameters.AddWithValue("val", l.ValorU6)
                    cmd.Parameters.AddWithValue("u", doc.UsuarioId)
                    cmd.ExecuteNonQuery()
                End Using
            Next
            Return docId
        End Function

        ''' <summary>Los mensajes de las reglas de la base empiezan con un código estable ("STOCK_INSUFICIENTE: ...").</summary>
        Private Shared Function Traducir(ex As PostgresException) As Exception
            Dim texto As String = ex.MessageText
            Dim i As Integer = texto.IndexOf(":"c)
            If i > 0 Then
                Dim codigo As String = texto.Substring(0, i)
                If codigo.Length > 3 AndAlso codigo = codigo.ToUpperInvariant() AndAlso Not codigo.Contains(" ") Then
                    Return New ReglaNegocioException(codigo, texto.Substring(i + 1).Trim())
                End If
            End If
            Return ex
        End Function

    End Class
