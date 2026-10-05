Imports AppSistema.Dominio.Numerico

Namespace Calculos

    ''' <summary>
    ''' Cálculos de la planificación de menús (matriz mensual). Importes y cantidades en u6 (1 000 000 = 1); porcentajes en
    ''' puntos básicos (100 % = 10 000). Un costo desconocido es Nothing (pendiente); el cero es un costo válido y se distingue.
    ''' </summary>
    Public Module PlanificacionMenu

        Public Const Cien As Long = 10000L

        ''' <summary>Costo total de una preparación = raciones × costo unitario por ración. Nothing si el costo está pendiente.</summary>
        Public Function CostoTotalU6(raciones As Long, costoRacionU6 As Long?) As Long?
            If raciones < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Las raciones no pueden ser negativas.")
            If Not costoRacionU6.HasValue Then Return Nothing
            Return raciones * costoRacionU6.Value
        End Function

        ''' <summary>
        ''' Costo por bandeja = costo ÷ comensales, con 6 decimales. "No calculable" (Nothing) si no hay comensales o el costo está pendiente.
        ''' Sirve tanto para un día como para el mes: el mensual se calcula con la suma de costos y de comensales, nunca como promedio de días.
        ''' </summary>
        Public Function CostoBandejaU6(costoU6 As Long?, comensales As Long) As Long?
            If Not costoU6.HasValue OrElse comensales <= 0 Then Return Nothing
            Return EscalaU6.MultiplicarDividir(costoU6.Value, 1L, comensales)
        End Function

        ''' <summary>Suma de costos. Si alguno está pendiente, el total queda pendiente (no se suma como cero).</summary>
        Public Function SumaCostosU6(costos As IEnumerable(Of Long?)) As Long?
            Dim total As Long = 0
            For Each c In costos
                If Not c.HasValue Then Return Nothing
                total += c.Value
            Next
            Return total
        End Function

        ''' <summary>Diferencia monetaria = realizado − planificado (u6). Nothing si falta alguno de los dos valores.</summary>
        Public Function DiferenciaMonetariaU6(planificadoU6 As Long?, realizadoU6 As Long?) As Long?
            If Not planificadoU6.HasValue OrElse Not realizadoU6.HasValue Then Return Nothing
            Return realizadoU6.Value - planificadoU6.Value
        End Function

        ''' <summary>
        ''' Diferencia porcentual en puntos básicos = (realizado − planificado) ÷ planificado × 100 %.
        ''' Nothing ("No calculable") si el planificado es cero o falta algún valor.
        ''' </summary>
        Public Function DiferenciaPorcentualBp(planificadoU6 As Long?, realizadoU6 As Long?) As Long?
            If Not planificadoU6.HasValue OrElse Not realizadoU6.HasValue OrElse planificadoU6.Value = 0 Then Return Nothing
            Return EscalaU6.MultiplicarDividir(realizadoU6.Value - planificadoU6.Value, Cien, planificadoU6.Value)
        End Function

        ''' <summary>
        ''' Valida los factores de un grupo de alternativas. Con cobertura obligatoria (p. ej. dos platos de fondo que
        ''' deben cubrir a todos los comensales) deben sumar exactamente 100 %. Sin ella, no pueden pasar del 100 %.
        ''' Devuelve el mensaje de advertencia, o Nothing si el grupo está bien. No normaliza ni redistribuye nada.
        ''' </summary>
        Public Function ValidarCobertura(factoresBp As IList(Of Long), coberturaObligatoria As Boolean) As String
            If factoresBp.Any(Function(f) f < 0 OrElse f > Cien) Then Return "Hay un factor fuera del rango de 0 % a 100 %."
            Dim suma = factoresBp.Sum()
            If coberturaObligatoria AndAlso suma <> Cien Then
                Return $"Las alternativas de este grupo deben sumar 100 % y suman {EnPorcentaje(suma)}."
            End If
            If suma > Cien Then Return $"Las alternativas de este grupo suman {EnPorcentaje(suma)}, más de 100 %."
            Return Nothing
        End Function

        ''' <summary>
        ''' Reparte un total de comensales entre alternativas que deben cubrirlo exactamente (regla documentada):
        ''' cada parte recibe la parte entera de total × peso; el residuo (menos de una unidad por alternativa) se entrega
        ''' de a una unidad a las alternativas de mayor fracción descartada, y a igual fracción, a la de menor posición.
        ''' Los pesos deben sumar 100 %. Así la suma siempre es exactamente el total y el resultado no depende del orden de cálculo.
        ''' </summary>
        Public Function RepartirResiduo(total As Long, pesosBp As IList(Of Long)) As Long()
            If total < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "El total no puede ser negativo.")
            If pesosBp.Sum() <> Cien OrElse pesosBp.Any(Function(p) p < 0) Then
                Throw New ReglaNegocioException("DATO_INVALIDO", "Los pesos de las alternativas deben sumar 100 %.")
            End If
            Dim partes(pesosBp.Count - 1) As Long
            Dim restos(pesosBp.Count - 1) As Long
            Dim asignado As Long = 0
            For i = 0 To pesosBp.Count - 1
                Dim exacto = total * pesosBp(i)
                partes(i) = exacto \ Cien
                restos(i) = exacto Mod Cien
                asignado += partes(i)
            Next
            Dim pendientes = total - asignado
            Dim orden = Enumerable.Range(0, pesosBp.Count).OrderByDescending(Function(i) restos(i)).ThenBy(Function(i) i).ToList()
            For k = 0 To CInt(pendientes) - 1
                partes(orden(k)) += 1
            Next
            Return partes
        End Function

        Private Function EnPorcentaje(bp As Long) As String
            Return (bp / 100D).ToString("0.##", System.Globalization.CultureInfo.GetCultureInfo("es-PE")) & " %"
        End Function

    End Module

    ''' <summary>Montos de un día (o de un periodo ya sumado) para el resumen de la matriz.</summary>
    Public NotInheritable Class ResumenPlanificacion

        Public Property Comensales As Long
        ''' <summary>Materia prima = suma de los costos totales de las preparaciones (u6). Nothing si hay costos pendientes.</summary>
        Public Property MateriaPrimaU6 As Long?
        ''' <summary>Estructura fija = productos fuera de recetas (u6). Nothing si hay costos pendientes.</summary>
        Public Property EstructuraFijaU6 As Long?

        ''' <summary>Costo total = materia prima + estructura fija. Nothing si alguno está pendiente.</summary>
        Public ReadOnly Property CostoTotalU6 As Long?
            Get
                If Not MateriaPrimaU6.HasValue OrElse Not EstructuraFijaU6.HasValue Then Return Nothing
                Return MateriaPrimaU6.Value + EstructuraFijaU6.Value
            End Get
        End Property

        ''' <summary>Costo por bandeja = costo total ÷ comensales (nunca promedio de costos diarios).</summary>
        Public ReadOnly Property CostoBandejaU6 As Long?
            Get
                Return PlanificacionMenu.CostoBandejaU6(CostoTotalU6, Comensales)
            End Get
        End Property

        ''' <summary>Suma de días: comensales y costos se suman; si un día tiene costo pendiente, el periodo queda pendiente.</summary>
        Public Shared Function Sumar(dias As IEnumerable(Of ResumenPlanificacion)) As ResumenPlanificacion
            Dim lista = dias.ToList()
            Return New ResumenPlanificacion With {
                .Comensales = lista.Sum(Function(d) d.Comensales),
                .MateriaPrimaU6 = PlanificacionMenu.SumaCostosU6(lista.Select(Function(d) d.MateriaPrimaU6)),
                .EstructuraFijaU6 = PlanificacionMenu.SumaCostosU6(lista.Select(Function(d) d.EstructuraFijaU6))}
        End Function

    End Class

End Namespace
