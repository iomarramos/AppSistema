Imports AppSistema.Dominio.Catalogo

Namespace Calculos

    ''' <summary>Cantidad en unidad base que sale (demanda) o entra (recepción pendiente) en una fecha.</summary>
    Public Structure EventoStock
        Public ReadOnly Property Fecha As Date
        Public ReadOnly Property CantidadU6 As Long

        Public Sub New(fecha As Date, cantidadU6 As Long)
            If cantidadU6 < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Las cantidades de la prevision no pueden ser negativas.")
            Me.Fecha = fecha.Date
            Me.CantidadU6 = cantidadU6
        End Sub
    End Structure

    ''' <summary>Desglose explicable de la previsión de un producto base (R07).</summary>
    Public NotInheritable Class ResultadoPrevisionProducto
        ''' <summary>Demanda de minutas aprobadas dentro del horizonte.</summary>
        Public Property DemandaHorizonteU6 As Long
        ''' <summary>Demanda entre la fecha de corte y el inicio del horizonte (se trata como demanda previa, una sola vez).</summary>
        Public Property ConsumoPuenteU6 As Long
        Public Property StockInicialU6 As Long
        Public Property ReservaU6 As Long
        ''' <summary>Pendiente de recibir que llega hasta el fin del horizonte.</summary>
        Public Property RecepcionesElegiblesU6 As Long
        ''' <summary>Saldo proyectado al final sin comprar = stock + recepciones − demanda (puede ser negativo).</summary>
        Public Property SaldoFinalSinCompraU6 As Long
        ''' <summary>Mayor faltante en alguna fecha antes de que llegue lo pendiente.</summary>
        Public Property MayorFaltanteU6 As Long
        ''' <summary>Primera fecha con saldo negativo sin comprar (Nothing si no se queda sin stock).</summary>
        Public Property FechaQuiebre As Date?
        Public Property NecesidadNetaU6 As Long
        ''' <summary>Recepciones pendientes que llegan después de la fecha de quiebre (no cubren la falta anterior, T16).</summary>
        Public Property RecepcionesTardiasU6 As Long
    End Class

    ''' <summary>
    ''' Previsión de compras por producto base (guía doc. 06 §3):
    '''   necesidad = máx(0, mayor faltante cronológico, reserva − saldo final sin compra)
    ''' que con todo a tiempo equivale a máx(0, demanda + reserva − stock − recepciones elegibles):
    ''' 50 + 10 − 27 − 8 = 25 L (T12). Recorre las fechas: una recepción que llega después de la falta
    ''' no la cubre (T16). Se supone que lo comprado llega antes de la fecha de quiebre.
    ''' </summary>
    Public Module Prevision

        Public Function CalcularProducto(stockInicialU6 As Long, reservaU6 As Long, fechaDesde As Date, fechaHasta As Date,
                                         demandas As IEnumerable(Of EventoStock), recepciones As IEnumerable(Of EventoStock)) As ResultadoPrevisionProducto
            If stockInicialU6 < 0 OrElse reservaU6 < 0 Then
                Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Las cantidades de la prevision no pueden ser negativas.")
            End If
            If fechaHasta.Date < fechaDesde.Date Then Throw New ReglaNegocioException("HORIZONTE_INVALIDO", "El fin del horizonte es anterior al inicio.")
            Dim hasta = fechaHasta.Date
            Dim dem = demandas.Where(Function(d) d.Fecha <= hasta).ToList()
            Dim rec = recepciones.Where(Function(x) x.Fecha <= hasta).ToList()

            Dim r As New ResultadoPrevisionProducto With {
                .StockInicialU6 = stockInicialU6, .ReservaU6 = reservaU6,
                .DemandaHorizonteU6 = dem.Where(Function(d) d.Fecha >= fechaDesde.Date).Sum(Function(d) d.CantidadU6),
                .ConsumoPuenteU6 = dem.Where(Function(d) d.Fecha < fechaDesde.Date).Sum(Function(d) d.CantidadU6),
                .RecepcionesElegiblesU6 = rec.Sum(Function(x) x.CantidadU6)}

            ' Recorrido cronológico: en una misma fecha lo que llega se recibe antes de consumir.
            Dim saldo As Decimal = stockInicialU6
            Dim faltante As Decimal = 0
            For Each fecha In dem.Select(Function(d) d.Fecha).Concat(rec.Select(Function(x) x.Fecha)).Distinct().OrderBy(Function(f) f)
                saldo += rec.Where(Function(x) x.Fecha = fecha).Sum(Function(x) CDec(x.CantidadU6))
                saldo -= dem.Where(Function(d) d.Fecha = fecha).Sum(Function(d) CDec(d.CantidadU6))
                If saldo < 0 Then
                    If Not r.FechaQuiebre.HasValue Then r.FechaQuiebre = fecha
                    faltante = Math.Max(faltante, -saldo)
                End If
            Next
            r.SaldoFinalSinCompraU6 = CLng(saldo)
            r.MayorFaltanteU6 = CLng(faltante)
            If r.FechaQuiebre.HasValue Then
                Dim quiebre = r.FechaQuiebre.Value
                r.RecepcionesTardiasU6 = rec.Where(Function(x) x.Fecha > quiebre).Sum(Function(x) x.CantidadU6)
            End If
            r.NecesidadNetaU6 = CLng(Math.Max(0D, Math.Max(faltante, CDec(reservaU6) - saldo)))
            Return r
        End Function

        ''' <summary>
        ''' Reparte una necesidad entre empaques (R08): las porciones deben sumar exactamente la necesidad, y cada
        ''' porción se redondea por mínimo y múltiplo de su empaque. El total se recalcula: la misma demanda no se
        ''' abastece dos veces (T17).
        ''' </summary>
        Public Function Asignar(necesidadU6 As Long, porciones As IList(Of (Empaque As EmpaqueCompra, PorcionU6 As Long))) As (Lineas As List(Of ResultadoCompra), TotalU6 As Long, ExcesoU6 As Long)
            If porciones Is Nothing OrElse porciones.Count = 0 Then Throw New ReglaNegocioException("ASIGNACION_VACIA", "Indique al menos un empaque.")
            If porciones.Any(Function(p) p.PorcionU6 < 0) Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Las porciones no pueden ser negativas.")
            Dim suma = porciones.Sum(Function(p) CDec(p.PorcionU6))
            If suma <> necesidadU6 Then
                Throw New ReglaNegocioException("ASIGNACION_DESCUADRADA",
                    $"Las porciones suman {Numerico.EscalaU6.ADecimal(CLng(suma))} y la necesidad es {Numerico.EscalaU6.ADecimal(necesidadU6)}: la demanda se asigna una sola vez.")
            End If
            Dim lineas = porciones.Select(Function(p) Compras.EmpaquesAComprar(p.PorcionU6, p.Empaque)).ToList()
            Dim total = lineas.Sum(Function(l) l.TotalBaseU6)
            Return (lineas, total, total - necesidadU6)
        End Function

    End Module

End Namespace
