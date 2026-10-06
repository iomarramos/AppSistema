Namespace Sunat

    ''' <summary>
    ''' Códigos de las tablas de SUNAT que usa el Formato 13.1 "Registro de inventario permanente valorizado - detalle del
    ''' inventario valorizado" (pedido del usuario, 2026-10-03). Se muestran con código y nombre; contabilidad los confirma.
    ''' </summary>
    Public Module TablasSunat

        Public Const MetodoValuacion As String = "PROMEDIO PONDERADO (MOVIL)"

        ''' <summary>Tabla 12, tipo de operación, según el tipo de documento de stock.</summary>
        Public Function TipoOperacion(tipoDocumento As String) As String
            Select Case tipoDocumento
                Case "apertura" : Return "16 SALDO INICIAL"
                Case "recepcion" : Return "02 COMPRA"
                Case "salida_produccion" : Return "10 SALIDA A PRODUCCION"
                Case "devolucion_produccion" : Return "20 ENTRADA POR DEVOLUCION DE PRODUCCION"
                Case "traspaso_entrada", "traspaso_salida" : Return "11 TRANSFERENCIA ENTRE ALMACENES"
                Case "baja" : Return "13 MERMAS"
                Case "ajuste_positivo", "ajuste_negativo" : Return "28 AJUSTE POR DIFERENCIA DE INVENTARIO"
                Case Else : Return "99 OTROS"
            End Select
        End Function

        ''' <summary>Tabla 10, tipo de comprobante. Los documentos internos del almacén son "00 OTROS".</summary>
        Public Function TipoComprobante(comprobante As String) As String
            Select Case If(comprobante, "").Trim().ToUpperInvariant()
                Case "FACTURA" : Return "01"
                Case "BOLETA" : Return "03"
                Case "GUIA", "GUIA DE REMISION" : Return "09"
                Case Else : Return "00"
            End Select
        End Function

        ''' <summary>Tabla 6, unidad de medida, según el código de la unidad base del producto.</summary>
        Public Function UnidadMedida(codigoUnidad As String) As String
            Select Case If(codigoUnidad, "").Trim().ToUpperInvariant()
                Case "KG" : Return "01 KILOGRAMOS"
                Case "G", "GR" : Return "06 GRAMOS"
                Case "UND", "UN", "U" : Return "07 UNIDADES"
                Case "L", "LT" : Return "08 LITROS"
                Case "ML" : Return "10 MILILITROS"
                Case Else : Return "99 OTROS"
            End Select
        End Function

        ''' <summary>Tabla 5, tipo de existencia. Por defecto los alimentos son materia prima de la preparación.</summary>
        Public Function TipoExistencia(codigo As String) As String
            Select Case If(codigo, "")
                Case "01" : Return "01 MERCADERIAS"
                Case "02" : Return "02 PRODUCTOS TERMINADOS"
                Case "03" : Return "03 MATERIAS PRIMAS Y AUXILIARES - MATERIALES"
                Case "04" : Return "04 ENVASES Y EMBALAJES"
                Case "05" : Return "05 SUMINISTROS DIVERSOS"
                Case "99" : Return "99 OTROS"
                Case Else : Throw New ReglaNegocioException("DATO_INVALIDO", $"Tipo de existencia desconocido: {codigo}. Use 01, 02, 03, 04, 05 o 99.")
            End Select
        End Function

        ''' <summary>Serie y número del comprobante ("F001-123" → F001 y 123). Sin guion, todo va al número.</summary>
        Public Function SerieYNumero(numero As String) As (Serie As String, Numero As String)
            Dim t = If(numero, "").Trim()
            Dim i = t.IndexOf("-"c)
            If i <= 0 OrElse i = t.Length - 1 Then Return ("", t)
            Return (t.Substring(0, i), t.Substring(i + 1))
        End Function

    End Module

End Namespace
