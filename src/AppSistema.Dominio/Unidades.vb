Namespace Catalogo

    Public Enum Dimension
        Masa = 1
        Volumen = 2
        Conteo = 3
    End Enum

    ''' <summary>
    ''' Unidad de medida. FactorABaseU6 es cuántas unidades base de su dimensión equivale una unidad
    ''' (p. ej. con base kg: kg = 1000000, g = 1000). PROPUESTA: el esquema v0.3 aún no guarda este factor.
    ''' </summary>
    Public NotInheritable Class UnidadMedida
        Public ReadOnly Property Codigo As String
        Public ReadOnly Property Dimension As Dimension
        Public ReadOnly Property FactorABaseU6 As Long

        Public Sub New(codigo As String, dimension As Dimension, factorABaseU6 As Long)
            If String.IsNullOrWhiteSpace(codigo) Then Throw New ArgumentException("El codigo es obligatorio.", NameOf(codigo))
            If factorABaseU6 <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(factorABaseU6), "El factor debe ser positivo.")
            Me.Codigo = codigo
            Me.Dimension = dimension
            Me.FactorABaseU6 = factorABaseU6
        End Sub
    End Class

    Public Module ConversorUnidades

        ''' <summary>Convierte entre unidades de la MISMA dimensión. Masa↔volumen no se convierte sin una regla explícita.</summary>
        Public Function Convertir(cantidadU6 As Long, desde As UnidadMedida, hasta As UnidadMedida) As Long
            If desde.Dimension <> hasta.Dimension Then
                Throw New ReglaNegocioException("CONVERSION_INVALIDA",
                    $"No se convierte {desde.Codigo} ({desde.Dimension}) a {hasta.Codigo} ({hasta.Dimension}) sin una regla explicita.")
            End If
            Return Numerico.EscalaU6.MultiplicarDividir(cantidadU6, desde.FactorABaseU6, hasta.FactorABaseU6)
        End Function

    End Module

End Namespace
