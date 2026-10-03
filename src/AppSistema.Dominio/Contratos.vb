Imports AppSistema.Dominio.Numerico

Namespace Calculos

    ''' <summary>Línea de contrato: importe mensual de un servicio con su vigencia (FechaHasta Nothing = sin fin).</summary>
    Public NotInheritable Class LineaContrato
        Public Property ImporteMensualU6 As Long
        Public Property FechaDesde As Date
        Public Property FechaHasta As Date?
    End Class

    ''' <summary>
    ''' Ingreso mensual desde contrato (supuesto D13): el importe mensual se reconoce completo si la línea cubre todo el mes;
    ''' si cubre parte (inicio, fin o ajuste a mitad de mes), en proporción a los días de calendario cubiertos.
    ''' Ejemplo: S/30 000 al mes desde el día 16 de un mes de 30 días → 15/30 → S/15 000.
    ''' </summary>
    Public Module IngresoContrato

        Public Function DiasCubiertos(linea As LineaContrato, anio As Integer, mes As Integer) As Integer
            Dim inicioMes As New Date(anio, mes, 1)
            Dim finMes = inicioMes.AddMonths(1).AddDays(-1)
            Dim desde = If(linea.FechaDesde > inicioMes, linea.FechaDesde, inicioMes)
            Dim hasta = If(linea.FechaHasta.HasValue AndAlso linea.FechaHasta.Value < finMes, linea.FechaHasta.Value, finMes)
            Return If(hasta < desde, 0, CInt((hasta - desde).TotalDays) + 1)
        End Function

        Public Function ImporteDelMesU6(linea As LineaContrato, anio As Integer, mes As Integer) As Long
            Dim dias = DiasCubiertos(linea, anio, mes)
            Dim diasMes = Date.DaysInMonth(anio, mes)
            If dias = 0 Then Return 0
            If dias = diasMes Then Return linea.ImporteMensualU6
            Return EscalaU6.MultiplicarDividir(linea.ImporteMensualU6, dias, diasMes)
        End Function

        ''' <summary>Suma de las líneas del servicio en el mes (antes y después de un ajuste).</summary>
        Public Function ImporteDelMesU6(lineas As IEnumerable(Of LineaContrato), anio As Integer, mes As Integer) As Long
            Return lineas.Sum(Function(l) ImporteDelMesU6(l, anio, mes))
        End Function
    End Module

    Public NotInheritable Class ResultadoServicio
        Public Property IngresoU6 As Long
        Public Property CostoAlimentosU6 As Long
        Public Property GastosPersonalU6 As Long
        Public Property GastosOperacionU6 As Long
        Public Property OtrosGastosU6 As Long
        Public ReadOnly Property TotalGastosU6 As Long
            Get
                Return CostoAlimentosU6 + GastosPersonalU6 + GastosOperacionU6 + OtrosGastosU6
            End Get
        End Property
        Public ReadOnly Property MargenU6 As Long
            Get
                Return IngresoU6 - TotalGastosU6
            End Get
        End Property
        ''' <summary>Margen / ingreso × 100 (u6); Nothing si el ingreso es cero o negativo.</summary>
        Public ReadOnly Property MargenPorcentajeU6 As Long?
            Get
                If IngresoU6 <= 0 Then Return Nothing
                Return EscalaU6.MultiplicarDividir(MargenU6, 100L * EscalaU6.Factor, IngresoU6)
            End Get
        End Property
    End Class

End Namespace
