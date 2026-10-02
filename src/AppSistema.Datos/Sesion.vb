Imports AppSistema.Dominio

Public NotInheritable Class OperacionDisponible
    Public ReadOnly Property Id As Long
    Public ReadOnly Property Codigo As String
    Public ReadOnly Property Nombre As String

    Public Sub New(id As Long, codigo As String, nombre As String)
        Me.Id = id
        Me.Codigo = codigo
        Me.Nombre = nombre
    End Sub

    Public Overrides Function ToString() As String
        Return $"{Codigo} - {Nombre}"
    End Function
End Class

''' <summary>
''' Identidad autenticada. Solo la crea ServicioAcceso: la empresa, el usuario y los permisos
''' nunca provienen de datos que el usuario pueda alterar en la pantalla.
''' </summary>
Public NotInheritable Class SesionUsuario
    Public ReadOnly Property UsuarioId As Long
    Public ReadOnly Property Login As String
    Public ReadOnly Property NombreUsuario As String
    Public ReadOnly Property EmpresaId As Long
    Public ReadOnly Property EmpresaCodigo As String
    Public ReadOnly Property Operaciones As IReadOnlyList(Of OperacionDisponible)
    Public ReadOnly Property Operacion As OperacionDisponible
    Public ReadOnly Property Permisos As IReadOnlyCollection(Of String)

    Friend Sub New(usuarioId As Long, login As String, nombreUsuario As String, empresaId As Long, empresaCodigo As String,
                   operaciones As IReadOnlyList(Of OperacionDisponible), operacion As OperacionDisponible, permisos As IEnumerable(Of String))
        Me.UsuarioId = usuarioId
        Me.Login = login
        Me.NombreUsuario = nombreUsuario
        Me.EmpresaId = empresaId
        Me.EmpresaCodigo = empresaCodigo
        Me.Operaciones = operaciones
        Me.Operacion = operacion
        Me.Permisos = New HashSet(Of String)(If(permisos, Enumerable.Empty(Of String)()), StringComparer.Ordinal)
    End Sub

    Public ReadOnly Property OperacionId As Long?
        Get
            Return If(Operacion Is Nothing, CType(Nothing, Long?), Operacion.Id)
        End Get
    End Property

    Public Function Tiene(permiso As String) As Boolean
        Return Operacion IsNot Nothing AndAlso Permisos.Contains(permiso)
    End Function

    Public Sub Exigir(permiso As String)
        If Operacion Is Nothing Then
            Throw New ReglaNegocioException("OPERACION_NO_SELECCIONADA", "Seleccione una operacion antes de continuar.")
        End If
        If Not Permisos.Contains(permiso) Then
            Throw New ReglaNegocioException("SIN_PERMISO", $"El usuario no tiene el permiso {permiso} en esta operacion.")
        End If
    End Sub

    Friend Function ConOperacion(operacion As OperacionDisponible, permisos As IEnumerable(Of String)) As SesionUsuario
        Return New SesionUsuario(UsuarioId, Login, NombreUsuario, EmpresaId, EmpresaCodigo, Operaciones, operacion, permisos)
    End Function
End Class
