Imports System.Globalization
Imports Npgsql
Imports AppSistema.Dominio

''' <summary>
''' Conexión + transacción con el contexto de sesión (app.empresa_id / app.usuario_id) fijado para
''' la transacción. El RLS de la base usa ese contexto: sin él no se ve ningún dato (falla cerrado).
''' Si no se confirma, se revierte al liberarse.
''' </summary>
Friend NotInheritable Class UnidadDeTrabajo
    Implements IDisposable

    Public ReadOnly Property Conexion As NpgsqlConnection
    Public ReadOnly Property Transaccion As NpgsqlTransaction
    Private _confirmada As Boolean

    Public Sub New(cadenaConexion As String, empresaId As Long?, usuarioId As Long?)
        Conexion = New NpgsqlConnection(cadenaConexion)
        Try
            Conexion.Open()
            Transaccion = Conexion.BeginTransaction()
            Ejecutar("SELECT set_config('app.empresa_id', @e, true), set_config('app.usuario_id', @u, true)",
                     "e", If(empresaId.HasValue, empresaId.Value.ToString(CultureInfo.InvariantCulture), ""),
                     "u", If(usuarioId.HasValue, usuarioId.Value.ToString(CultureInfo.InvariantCulture), ""))
        Catch
            Conexion.Dispose()
            Throw
        End Try
    End Sub

    Public Shared Function ParaSesion(cadenaConexion As String, sesion As SesionUsuario) As UnidadDeTrabajo
        Return New UnidadDeTrabajo(cadenaConexion, sesion.EmpresaId, sesion.UsuarioId)
    End Function

    ''' <summary>Parámetros en pares nombre, valor. Nothing se envía como NULL.</summary>
    Public Function Comando(sql As String, ParamArray pares() As Object) As NpgsqlCommand
        If pares.Length Mod 2 <> 0 Then Throw New ArgumentException("Los parametros van en pares nombre, valor.")
        Dim cmd As New NpgsqlCommand(sql, Conexion, Transaccion)
        For i As Integer = 0 To pares.Length - 1 Step 2
            cmd.Parameters.AddWithValue(CStr(pares(i)), If(pares(i + 1), DBNull.Value))
        Next
        Return cmd
    End Function

    Public Function Ejecutar(sql As String, ParamArray pares() As Object) As Integer
        Using cmd = Comando(sql, pares)
            Return cmd.ExecuteNonQuery()
        End Using
    End Function

    ''' <summary>Primer valor de la primera fila, o Nothing si no hay filas o es NULL.</summary>
    Public Function Escalar(sql As String, ParamArray pares() As Object) As Object
        Using cmd = Comando(sql, pares)
            Dim v = cmd.ExecuteScalar()
            Return If(v Is DBNull.Value, Nothing, v)
        End Using
    End Function

    Public Function EscalarLong(sql As String, ParamArray pares() As Object) As Long
        Return Convert.ToInt64(Escalar(sql, pares), CultureInfo.InvariantCulture)
    End Function

    Public Function Consultar(Of T)(sql As String, mapear As Func(Of NpgsqlDataReader, T), ParamArray pares() As Object) As List(Of T)
        Dim lista As New List(Of T)
        Using cmd = Comando(sql, pares)
            Using rd = cmd.ExecuteReader()
                While rd.Read()
                    lista.Add(mapear(rd))
                End While
            End Using
        End Using
        Return lista
    End Function

    Public Sub Confirmar()
        Transaccion.Commit()
        _confirmada = True
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            If Not _confirmada AndAlso Transaccion IsNot Nothing AndAlso Conexion.State = Data.ConnectionState.Open Then Transaccion.Rollback()
        Catch
            ' Una conexión rota ya revirtió la transacción en el servidor.
        Finally
            Conexion.Dispose()
        End Try
    End Sub
End Class

''' <summary>Lectura segura de columnas opcionales.</summary>
Friend Module Lector
    <Runtime.CompilerServices.Extension>
    Public Function TextoONada(rd As NpgsqlDataReader, columna As String) As String
        Dim i = rd.GetOrdinal(columna)
        Return If(rd.IsDBNull(i), Nothing, rd.GetString(i))
    End Function

    <Runtime.CompilerServices.Extension>
    Public Function LongONada(rd As NpgsqlDataReader, columna As String) As Long?
        Dim i = rd.GetOrdinal(columna)
        Return If(rd.IsDBNull(i), CType(Nothing, Long?), rd.GetInt64(i))
    End Function

    <Runtime.CompilerServices.Extension>
    Public Function Largo(rd As NpgsqlDataReader, columna As String) As Long
        Return rd.GetInt64(rd.GetOrdinal(columna))
    End Function

    <Runtime.CompilerServices.Extension>
    Public Function Texto(rd As NpgsqlDataReader, columna As String) As String
        Return rd.GetString(rd.GetOrdinal(columna))
    End Function
End Module

''' <summary>Convierte errores de PostgreSQL en errores de negocio con código estable.</summary>
Public Module ErroresBD

    Public Function Traducir(ex As PostgresException) As Exception
        Select Case ex.SqlState
            Case "P0001"
                ' Reglas propias: el mensaje empieza con un código ("STOCK_INSUFICIENTE: ...").
                Dim texto As String = ex.MessageText
                Dim i As Integer = texto.IndexOf(":"c)
                If i > 0 Then
                    Dim codigo As String = texto.Substring(0, i)
                    If codigo.Length > 3 AndAlso codigo = codigo.ToUpperInvariant() AndAlso Not codigo.Contains(" ") Then
                        Return New ReglaNegocioException(codigo, texto.Substring(i + 1).Trim(), ex)
                    End If
                End If
                Return ex
            Case "23505"
                Return New ReglaNegocioException("CODIGO_DUPLICADO", "Ya existe un registro con ese codigo.", ex)
            Case "23P01"
                Return New ReglaNegocioException("VIGENCIA_SUPERPUESTA", "Ya existe un precio vigente en ese periodo.", ex)
            Case "23503"
                Return New ReglaNegocioException("REFERENCIA_INVALIDA", "El registro referenciado no existe o esta en uso.", ex)
            Case "23514", "22P02", "22003"
                Return New ReglaNegocioException("DATO_INVALIDO", "Un dato no cumple las reglas de la base de datos.", ex)
            Case "42501"
                Return New ReglaNegocioException("SIN_PERMISO", "La operacion no esta permitida.", ex)
            Case Else
                Return ex
        End Select
    End Function

End Module

''' <summary>Base de los servicios que actúan en nombre de un usuario autenticado.</summary>
Public MustInherit Class ServicioConSesion

    Protected ReadOnly Property CadenaConexion As String
    Protected ReadOnly Property Sesion As SesionUsuario

    Protected Sub New(cadenaConexion As String, sesion As SesionUsuario)
        If String.IsNullOrWhiteSpace(cadenaConexion) Then Throw New ArgumentException("Falta la cadena de conexion.", NameOf(cadenaConexion))
        If sesion Is Nothing Then Throw New ArgumentNullException(NameOf(sesion))
        Me.CadenaConexion = cadenaConexion
        Me.Sesion = sesion
    End Sub

    ''' <summary>Exige el permiso, abre la transacción con el contexto de sesión y confirma si todo sale bien.</summary>
    Friend Function EnTransaccion(Of T)(permiso As String, accion As Func(Of UnidadDeTrabajo, T)) As T
        Sesion.Exigir(permiso)
        Try
            Using u = UnidadDeTrabajo.ParaSesion(CadenaConexion, Sesion)
                Dim resultado As T = accion(u)
                u.Confirmar()
                Return resultado
            End Using
        Catch ex As PostgresException
            Throw ErroresBD.Traducir(ex)
        End Try
    End Function

End Class
