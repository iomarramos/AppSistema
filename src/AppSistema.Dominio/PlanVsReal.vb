Imports AppSistema.Dominio.Numerico

Namespace Calculos

    ''' <summary>Un nivel del comparativo (teórico, plan real o realizado): costo total y raciones del servicio en el día.</summary>
    Public Structure NivelCosto
        Public ReadOnly Property CostoTotalU6 As Long
        Public ReadOnly Property Raciones As Long

        Public Sub New(costoTotalU6 As Long, raciones As Long)
            If costoTotalU6 < 0 OrElse raciones < 0 Then Throw New ReglaNegocioException("DATO_INVALIDO", "Costo y raciones no pueden ser negativos.")
            Me.CostoTotalU6 = costoTotalU6
            Me.Raciones = raciones
        End Sub

        ''' <summary>Costo bandeja = costo total ÷ raciones; Nothing sin raciones (no hay dato, no es cero).</summary>
        Public ReadOnly Property CostoBandejaU6 As Long?
            Get
                Return If(Raciones = 0, CType(Nothing, Long?), EscalaU6.MultiplicarDividir(CostoTotalU6, 1, Raciones))
            End Get
        End Property
    End Structure

    Public NotInheritable Class ComparativoTresNiveles
        Public Property Teorico As NivelCosto
        Public Property PlanReal As NivelCosto
        Public Property Realizado As NivelCosto?
        ''' <summary>Costo bandeja del plan real − el teórico (lo que la operación movió al planificar).</summary>
        Public Property DesviacionPlanU6 As Long?
        ''' <summary>Costo bandeja realizado − el del plan real. Nothing mientras el día no tiene realizado.</summary>
        Public Property DesviacionRealizadoU6 As Long?
        ''' <summary>Costo bandeja realizado − el teórico.</summary>
        Public Property DesviacionTotalU6 As Long?
    End Class

    ''' <summary>
    ''' Planificación teórica → plan real (operativo) → realizado, como el reporte del SGP "Costo Plan. Teórico - Plan. Real -
    ''' Realizado" (datos/plan_real). Cada nivel queda separado; solo se comparan. En el SGP, un día sin realizado muestra
    ''' desviación 0; aquí queda vacía, porque no hay dato.
    ''' </summary>
    Public Module PlanVsReal

        ''' <summary>Costo minuta día = Σ(raciones × costo por ración) ÷ comensales. Nothing sin comensales.</summary>
        Public Function CostoMinutaDiaU6(platos As IEnumerable(Of (Raciones As Long, CostoRacionU6 As Long)), comensales As Long) As Long?
            If comensales <= 0 Then Return Nothing
            Return EscalaU6.MultiplicarDividir(CostoTotalU6(platos), 1, comensales)
        End Function

        ''' <summary>Costo total del servicio = Σ(raciones × costo por ración).</summary>
        Public Function CostoTotalU6(platos As IEnumerable(Of (Raciones As Long, CostoRacionU6 As Long))) As Long
            Dim total As Long = 0
            For Each p In platos
                If p.Raciones < 0 OrElse p.CostoRacionU6 < 0 Then Throw New ReglaNegocioException("DATO_INVALIDO", "Raciones y costo no pueden ser negativos.")
                total += p.Raciones * p.CostoRacionU6
            Next
            Return total
        End Function

        ''' <summary>Porcentaje del plan real (Por.% del SGP) = raciones ÷ comensales, en puntos básicos (75 % = 7500).</summary>
        Public Function PorcentajeBp(raciones As Long, comensales As Long) As Long?
            If comensales <= 0 OrElse raciones < 0 Then Return Nothing
            Return EscalaU6.MultiplicarDividir(raciones, 10000, comensales)
        End Function

        ''' <summary>
        ''' Costo piso y techo del servicio según sus factores (usuario, 2026-10-03): el factor de cada componente es
        ''' Σ raciones ÷ Σ comensales del periodo; piso = Σ factor × la ración más barata del componente y techo = Σ factor ×
        ''' la más cara. Con los menús del SGP de agosto a octubre, los 552 días quedan dentro de su banda
        ''' (datos/plan_real/costo_piso_techo.csv). Nothing sin comensales.
        ''' </summary>
        Public Function BandaCosto(componentes As IEnumerable(Of (Raciones As Long, CostoMinimoU6 As Long, CostoMaximoU6 As Long)),
                                   comensales As Long) As (PisoU6 As Long, TechoU6 As Long)?
            If comensales <= 0 Then Return Nothing
            Dim piso As Long = 0, techo As Long = 0
            For Each c In componentes
                If c.Raciones < 0 OrElse c.CostoMinimoU6 < 0 OrElse c.CostoMaximoU6 < c.CostoMinimoU6 Then
                    Throw New ReglaNegocioException("DATO_INVALIDO", "Raciones y costos deben ser validos (minimo <= maximo).")
                End If
                piso += c.Raciones * c.CostoMinimoU6
                techo += c.Raciones * c.CostoMaximoU6
            Next
            Return (EscalaU6.MultiplicarDividir(piso, 1, comensales), EscalaU6.MultiplicarDividir(techo, 1, comensales))
        End Function

        ''' <summary>True si el costo bandeja del día está entre el piso y el techo (incluidos). Sirve para alertar, no bloquea.</summary>
        Public Function DentroDeBanda(costoBandejaU6 As Long, banda As (PisoU6 As Long, TechoU6 As Long)) As Boolean
            Return costoBandejaU6 >= banda.PisoU6 AndAlso costoBandejaU6 <= banda.TechoU6
        End Function

        Public Function Comparar(teorico As NivelCosto, planReal As NivelCosto, realizado As NivelCosto?) As ComparativoTresNiveles
            Dim r As New ComparativoTresNiveles With {.Teorico = teorico, .PlanReal = planReal}
            Dim cbt = teorico.CostoBandejaU6, cbp = planReal.CostoBandejaU6
            If cbt.HasValue AndAlso cbp.HasValue Then r.DesviacionPlanU6 = cbp.Value - cbt.Value
            If realizado.HasValue AndAlso realizado.Value.Raciones > 0 Then
                r.Realizado = realizado
                Dim cbz = realizado.Value.CostoBandejaU6.Value
                If cbp.HasValue Then r.DesviacionRealizadoU6 = cbz - cbp.Value
                If cbt.HasValue Then r.DesviacionTotalU6 = cbz - cbt.Value
            End If
            Return r
        End Function

    End Module

End Namespace
