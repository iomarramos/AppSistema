Imports System.Security.Cryptography
Imports System.Text

''' <summary>
''' Hash de claves con PBKDF2-SHA256. Formato: pbkdf2-sha256$iteraciones$sal$hash (Base64).
''' Nunca se guardan ni registran claves en texto plano.
''' </summary>
Public Module ClaveSegura

    Private Const Prefijo As String = "pbkdf2-sha256"
    Private Const Iteraciones As Integer = 210000
    Private Const BytesSal As Integer = 16
    Private Const BytesHash As Integer = 32

    Public Function Crear(clave As String) As String
        If clave Is Nothing Then Throw New ArgumentNullException(NameOf(clave))
        Dim sal = RandomNumberGenerator.GetBytes(BytesSal)
        Dim hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(clave), sal, Iteraciones, HashAlgorithmName.SHA256, BytesHash)
        Return $"{Prefijo}${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}"
    End Function

    Public Function Verificar(clave As String, almacenado As String) As Boolean
        If clave Is Nothing OrElse String.IsNullOrEmpty(almacenado) Then Return False
        Dim partes = almacenado.Split("$"c)
        If partes.Length <> 4 OrElse partes(0) <> Prefijo Then Return False
        Dim iter As Integer
        If Not Integer.TryParse(partes(1), iter) OrElse iter < 10000 Then Return False
        Dim sal As Byte(), esperado As Byte()
        Try
            sal = Convert.FromBase64String(partes(2))
            esperado = Convert.FromBase64String(partes(3))
        Catch ex As FormatException
            Return False
        End Try
        Dim calculado = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(clave), sal, iter, HashAlgorithmName.SHA256, esperado.Length)
        Return CryptographicOperations.FixedTimeEquals(calculado, esperado)
    End Function

    ''' <summary>Se verifica contra este hash cuando el usuario no existe, para no revelar su existencia por el tiempo de respuesta.</summary>
    Friend ReadOnly HashFicticio As String = Crear("clave-ficticia-sin-uso-0")

End Module
