Imports AppSistema.Dominio.Numerico

Namespace Catalogo

    ''' <summary>
    ''' Artículo comercial (marca + presentación) de un producto base. El contenido de cada envase está
    ''' declarado en la unidad base del producto: "galón" es solo una descripción comercial.
    ''' </summary>
    Public NotInheritable Class VarianteProducto
        Public ReadOnly Property Codigo As String
        Public ReadOnly Property ContenidoBasePorEnvaseU6 As Long

        Public Sub New(codigo As String, contenidoBasePorEnvaseU6 As Long)
            If String.IsNullOrWhiteSpace(codigo) Then Throw New ArgumentException("El codigo es obligatorio.", NameOf(codigo))
            If contenidoBasePorEnvaseU6 <= 0 Then
                Throw New ReglaNegocioException("CONVERSION_INVALIDA", "El contenido por envase debe ser positivo.")
            End If
            Me.Codigo = codigo
            Me.ContenidoBasePorEnvaseU6 = contenidoBasePorEnvaseU6
        End Sub
    End Class

    ''' <summary>
    ''' Forma de compra de una variante. Envases por empaque, mínimo y múltiplo de pedido son
    ''' parámetros distintos (la caja de 4 envases no implica pedir de 4 en 4).
    ''' </summary>
    Public NotInheritable Class EmpaqueCompra
        Public ReadOnly Property Variante As VarianteProducto
        Public ReadOnly Property EnvasesPorEmpaque As Long
        Public ReadOnly Property MinimoEmpaques As Long
        Public ReadOnly Property MultiploEmpaques As Long

        Public Sub New(variante As VarianteProducto, envasesPorEmpaque As Long,
                       Optional minimoEmpaques As Long = 1, Optional multiploEmpaques As Long = 1)
            If variante Is Nothing Then Throw New ArgumentNullException(NameOf(variante))
            If envasesPorEmpaque <= 0 Then Throw New ReglaNegocioException("CONVERSION_INVALIDA", "Los envases por empaque deben ser positivos.")
            If minimoEmpaques <= 0 OrElse multiploEmpaques <= 0 Then
                Throw New ReglaNegocioException("CONVERSION_INVALIDA", "Minimo y multiplo deben ser positivos.")
            End If
            Me.Variante = variante
            Me.EnvasesPorEmpaque = envasesPorEmpaque
            Me.MinimoEmpaques = minimoEmpaques
            Me.MultiploEmpaques = multiploEmpaques
        End Sub

        ''' <summary>Contenido de un empaque en unidad base = envases por empaque × contenido por envase.</summary>
        Public ReadOnly Property ContenidoBaseU6 As Long
            Get
                Return Checked(EnvasesPorEmpaque, Variante.ContenidoBasePorEnvaseU6)
            End Get
        End Property

        ''' <summary>Cantidad base recibida = empaques recibidos × contenido por empaque.</summary>
        Public Function CantidadBaseU6(empaques As Long) As Long
            If empaques < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Los empaques no pueden ser negativos.")
            Return Checked(empaques, ContenidoBaseU6)
        End Function

        Private Shared Function Checked(a As Long, b As Long) As Long
            Try
                Return System.Convert.ToInt64(Decimal.Multiply(a, b))
            Catch ex As OverflowException
                Throw New OverflowException("La cantidad excede el rango de un campo _u6.", ex)
            End Try
        End Function
    End Class

End Namespace
