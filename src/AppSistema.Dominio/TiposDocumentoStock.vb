Namespace Stock

    ''' <summary>Tipos de documento de stock (columna documento_stock.tipo). 'reversion' se incorporará con su flujo propio.</summary>
    Public Enum TipoDocumentoStock
        Apertura
        Recepcion
        DevolucionProduccion
        TraspasoEntrada
        AjustePositivo
        SalidaProduccion
        Baja
        TraspasoSalida
        AjusteNegativo
    End Enum

    Public Module TiposDocumentoStockInfo

        ''' <summary>Código tal como lo guarda la base de datos.</summary>
        Public Function Codigo(tipo As TipoDocumentoStock) As String
            Select Case tipo
                Case TipoDocumentoStock.Apertura : Return "apertura"
                Case TipoDocumentoStock.Recepcion : Return "recepcion"
                Case TipoDocumentoStock.DevolucionProduccion : Return "devolucion_produccion"
                Case TipoDocumentoStock.TraspasoEntrada : Return "traspaso_entrada"
                Case TipoDocumentoStock.AjustePositivo : Return "ajuste_positivo"
                Case TipoDocumentoStock.SalidaProduccion : Return "salida_produccion"
                Case TipoDocumentoStock.Baja : Return "baja"
                Case TipoDocumentoStock.TraspasoSalida : Return "traspaso_salida"
                Case TipoDocumentoStock.AjusteNegativo : Return "ajuste_negativo"
                Case Else : Throw New ArgumentOutOfRangeException(NameOf(tipo))
            End Select
        End Function

        ''' <summary>+1 entrada, -1 salida. El servidor lo vuelve a validar (SIGNO_NO_PERMITIDO).</summary>
        Public Function Signo(tipo As TipoDocumentoStock) As Integer
            Select Case tipo
                Case TipoDocumentoStock.Apertura, TipoDocumentoStock.Recepcion, TipoDocumentoStock.DevolucionProduccion,
                     TipoDocumentoStock.TraspasoEntrada, TipoDocumentoStock.AjustePositivo
                    Return 1
                Case Else
                    Return -1
            End Select
        End Function

    End Module

End Namespace
