Imports System.Numerics

Namespace Numerico

    ''' <summary>
    ''' Aritmética con enteros escalados por 1.000.000 (campos "_u6" de la base de datos).
    ''' 4 litros = 4000000; S/8,50 = 8500000. Nunca se usa Double para cantidades ni dinero.
    ''' Redondeo: mitad alejándose de cero (política provisional, pendiente de confirmar).
    ''' </summary>
    Public Module EscalaU6

        Public Const Factor As Long = 1000000L

        ''' <summary>Convierte un decimal a u6 con redondeo explícito; falla si no cabe en Int64.</summary>
        Public Function DesdeDecimal(valor As Decimal) As Long
            Dim escalado As Decimal = Math.Round(valor * Factor, 0, MidpointRounding.AwayFromZero)
            If escalado > Long.MaxValue OrElse escalado < Long.MinValue Then
                Throw New OverflowException("El valor excede el rango de un campo _u6.")
            End If
            Return CLng(escalado)
        End Function

        Public Function ADecimal(valorU6 As Long) As Decimal
            Return CDec(valorU6) / Factor
        End Function

        ''' <summary>a × b / 1.000.000 (producto de dos magnitudes u6) con redondeo explícito.</summary>
        Public Function Multiplicar(aU6 As Long, bU6 As Long) As Long
            Return DividirRedondeado(New BigInteger(aU6) * New BigInteger(bU6), New BigInteger(Factor))
        End Function

        ''' <summary>a × b / c, con a, b y c en u6 y resultado en u6 (p. ej. escalar una receta).</summary>
        Public Function MultiplicarDividir(aU6 As Long, bU6 As Long, cU6 As Long) As Long
            If cU6 = 0 Then Throw New DivideByZeroException("El divisor no puede ser cero.")
            Return DividirRedondeado(New BigInteger(aU6) * New BigInteger(bU6), New BigInteger(cU6))
        End Function

        Private Function DividirRedondeado(numerador As BigInteger, divisor As BigInteger) As Long
            Dim resto As BigInteger
            Dim cociente As BigInteger = BigInteger.DivRem(numerador, divisor, resto)
            If BigInteger.Abs(resto) * 2 >= BigInteger.Abs(divisor) Then
                ' Mitad (o más) alejándose de cero; el signo lo dan numerador y divisor.
                If (numerador.Sign * divisor.Sign) >= 0 Then
                    cociente += BigInteger.One
                Else
                    cociente -= BigInteger.One
                End If
            End If
            If cociente > Long.MaxValue OrElse cociente < Long.MinValue Then
                Throw New OverflowException("El resultado excede el rango de un campo _u6.")
            End If
            Return CLng(cociente)
        End Function

    End Module

End Namespace
