Imports AppSistema.Dominio.Numerico

Namespace Calculos

    Public NotInheritable Class ResultadoFoodCost
        ''' <summary>False si el ingreso es cero o negativo (T41): no se calcula y se revisa la causa.</summary>
        Public Property Calculable As Boolean
        ''' <summary>Food Cost en % con 6 decimales (u6): 42 % = 42000000.</summary>
        Public Property PorcentajeU6 As Long?
        ''' <summary>Desviación contra el objetivo en puntos porcentuales (u6): +2 pp = 2000000. No es "subió 2 %".</summary>
        Public Property DesviacionPuntosU6 As Long?
        ''' <summary>Presupuesto de alimentos = ingreso × objetivo.</summary>
        Public Property PresupuestoU6 As Long?
        ''' <summary>Costo − presupuesto (positivo = gasto por encima del objetivo).</summary>
        Public Property DiferenciaPresupuestoU6 As Long?
    End Class

    ''' <summary>
    ''' Food Cost real = costo de alimentos reconocido / ingreso neto del mismo servicio y período × 100 (guía §9).
    ''' Ejemplo T40: S/4 200 / S/10 000 = 42 %; objetivo 40 % → +2 pp y S/200 sobre el presupuesto de S/4 000.
    ''' </summary>
    Public Module FoodCost

        ''' <param name="objetivoBp">Objetivo en puntos básicos (40 % = 4000), o Nothing si no hay objetivo.</param>
        Public Function Calcular(costoU6 As Long, ingresoU6 As Long, objetivoBp As Long?) As ResultadoFoodCost
            Dim r As New ResultadoFoodCost()
            If ingresoU6 <= 0 Then Return r
            r.Calculable = True
            r.PorcentajeU6 = EscalaU6.MultiplicarDividir(costoU6, 100L * EscalaU6.Factor, ingresoU6)
            If objetivoBp.HasValue Then
                Dim objetivoU6 = objetivoBp.Value * (EscalaU6.Factor \ 100)          ' 4000 bp → 40 %
                r.DesviacionPuntosU6 = r.PorcentajeU6.Value - objetivoU6
                r.PresupuestoU6 = EscalaU6.MultiplicarDividir(ingresoU6, objetivoBp.Value, 10000)
                r.DiferenciaPresupuestoU6 = costoU6 - r.PresupuestoU6.Value
            End If
            Return r
        End Function

    End Module

End Namespace
