Imports System.Collections.Generic
Imports System.Linq

Namespace Inventario

    ''' <summary>
    ''' Motivos normalizados de un ajuste de inventario (especificación 03, FormAjustesInventario). El código es lo que se
    ''' guarda y se agrupa en los reportes; el texto es lo que ve el usuario. Una explicación libre acompaña al motivo.
    ''' </summary>
    Public Module MotivosAjuste
        Public Const ErrorConteo As String = "ERROR_CONTEO"
        Public Const IngresoOmitido As String = "INGRESO_OMITIDO"
        Public Const SalidaOmitida As String = "SALIDA_OMITIDA"
        Public Const ErrorUnidad As String = "ERROR_UNIDAD"
        Public Const MermaNoRegistrada As String = "MERMA_NO_REGISTRADA"
        Public Const Digitacion As String = "DIGITACION"
        Public Const Vencimiento As String = "VENCIMIENTO"
        Public Const Perdida As String = "PERDIDA"
        Public Const Sobrante As String = "SOBRANTE"
        Public Const Otro As String = "OTRO"

        Private ReadOnly _motivos As IReadOnlyList(Of KeyValuePair(Of String, String)) = New List(Of KeyValuePair(Of String, String)) From {
            New KeyValuePair(Of String, String)(ErrorConteo, "Error de conteo previo"),
            New KeyValuePair(Of String, String)(IngresoOmitido, "Ingreso omitido"),
            New KeyValuePair(Of String, String)(SalidaOmitida, "Salida omitida"),
            New KeyValuePair(Of String, String)(ErrorUnidad, "Error de unidad"),
            New KeyValuePair(Of String, String)(MermaNoRegistrada, "Merma no registrada"),
            New KeyValuePair(Of String, String)(Digitacion, "Digitacion"),
            New KeyValuePair(Of String, String)(Vencimiento, "Vencimiento"),
            New KeyValuePair(Of String, String)(Perdida, "Perdida"),
            New KeyValuePair(Of String, String)(Sobrante, "Sobrante"),
            New KeyValuePair(Of String, String)(Otro, "Otro")}

        ''' <summary>Lista para elegir: código y texto, en el orden de la especificación.</summary>
        Public ReadOnly Property Todos As IReadOnlyList(Of KeyValuePair(Of String, String))
            Get
                Return _motivos
            End Get
        End Property

        ''' <summary>Texto del motivo para un código; lanza DATO_INVALIDO si el código no está en la lista.</summary>
        Public Function Texto(codigo As String) As String
            Dim encontrado = _motivos.FirstOrDefault(Function(m) m.Key = codigo)
            If encontrado.Key Is Nothing Then Throw New ReglaNegocioException("DATO_INVALIDO", $"Motivo de ajuste desconocido: {codigo}.")
            Return encontrado.Value
        End Function
    End Module

    ''' <summary>
    ''' Clasificación ABC de productos por consumo (política del inventario rotativo, especificación 03): A mientras lo
    ''' acumulado antes del producto sea menos del 80 %, B menos del 95 % y C el resto. Sin consumo = C. Todo en enteros, sin Double.
    ''' </summary>
    Public Module ClasificacionAbc

        Public Const UmbralA As Long = 80L
        Public Const UmbralB As Long = 95L

        ''' <summary>Clase por producto. Los valores son consumo (costo) de cada producto; el orden no importa.</summary>
        Public Function Clasificar(consumo As IEnumerable(Of KeyValuePair(Of Long, Long))) As Dictionary(Of Long, String)
            Dim lista = consumo.OrderByDescending(Function(c) c.Value).ThenBy(Function(c) c.Key).ToList()
            Dim total = lista.Sum(Function(c) c.Value)
            Dim resultado As New Dictionary(Of Long, String)()
            Dim acumulado As Long = 0
            For Each c In lista
                If c.Value <= 0 OrElse total <= 0 Then
                    resultado(c.Key) = "C"
                    Continue For
                End If
                ' Se mide lo acumulado ANTES del producto: el primero siempre entra en A (aunque sea el único).
                Dim previo = acumulado * 100L \ total
                resultado(c.Key) = If(previo < UmbralA, "A", If(previo < UmbralB, "B", "C"))
                acumulado += c.Value
            Next
            Return resultado
        End Function
    End Module

End Namespace
