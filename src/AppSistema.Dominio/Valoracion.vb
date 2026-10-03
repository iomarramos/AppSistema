Imports AppSistema.Dominio.Numerico

Namespace Stock

    ''' <summary>
    ''' Valoración de inventario (D01, decidido por el usuario el 03/10/2026: "cantidad × precio").
    ''' Entrada: cantidad × precio de compra. Salida: cantidad × costo vigente del saldo (valor ÷ cantidad),
    ''' es decir, promedio ponderado móvil por almacén y variante. Ejemplo de la guía: 32 L a S/8 + 16 L a S/10 =
    ''' 48 L por S/416; una salida de 6 L vale S/52 y quedan 42 L por S/364.
    ''' </summary>
    Public Module Valoracion

        ''' <summary>
        ''' Valor de una salida. Se calcula con numerador y denominador (no con el costo unitario redondeado) y,
        ''' si la salida agota el saldo, se lleva el valor restante: nunca queda cantidad cero con dinero residual.
        ''' </summary>
        Public Function ValorSalidaU6(cantidadSaldoU6 As Long, valorSaldoU6 As Long, cantidadSalidaU6 As Long) As Long
            If cantidadSalidaU6 <= 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "La cantidad debe ser positiva.")
            If cantidadSalidaU6 > cantidadSaldoU6 Then
                Throw New ReglaNegocioException("STOCK_INSUFICIENTE",
                    $"Disponible {EscalaU6.ADecimal(cantidadSaldoU6)}, solicitado {EscalaU6.ADecimal(cantidadSalidaU6)}.")
            End If
            If cantidadSalidaU6 = cantidadSaldoU6 Then Return valorSaldoU6
            Return EscalaU6.MultiplicarDividir(valorSaldoU6, cantidadSalidaU6, cantidadSaldoU6)
        End Function

        ''' <summary>Costo por unidad base para mostrar (valor ÷ cantidad), redondeado a 6 decimales.</summary>
        Public Function CostoUnitarioU6(valorU6 As Long, cantidadU6 As Long) As Long
            If cantidadU6 <= 0 Then Return 0
            Return EscalaU6.MultiplicarDividir(valorU6, EscalaU6.Factor, cantidadU6)
        End Function

    End Module

End Namespace
