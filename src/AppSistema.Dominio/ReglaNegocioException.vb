''' <summary>
''' Error de regla de negocio con código funcional estable (p. ej. CONVERSION_INVALIDA),
''' alineado con los códigos de la guía de construcción.
''' </summary>
Public Class ReglaNegocioException
    Inherits Exception

    Public ReadOnly Property Codigo As String

    Public Sub New(codigo As String, mensaje As String)
        MyBase.New(codigo & ": " & mensaje)
        Me.Codigo = codigo
    End Sub

    Public Sub New(codigo As String, mensaje As String, causa As Exception)
        MyBase.New(codigo & ": " & mensaje, causa)
        Me.Codigo = codigo
    End Sub
End Class
