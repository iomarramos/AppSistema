Imports System.IO
Imports System.Reflection
Imports System.Security.Cryptography
Imports System.Text
Imports Npgsql

Public NotInheritable Class MigracionAplicada
    Public Property Version As String
    Public Property Archivo As String
    Public Property Aplicada As Boolean
End Class

''' <summary>
''' Aplica las migraciones V*.sql incluidas en esta biblioteca, en orden, cada una en su transacción.
''' Registra versión y hash en esquema_migracion. Una migración ya aplicada cuyo contenido cambió
''' detiene el proceso (MIGRACION_MODIFICADA): las migraciones aplicadas nunca se editan.
''' Debe ejecutarse con el rol PROPIETARIO de la base.
''' </summary>
Public NotInheritable Class Migrador

    Private ReadOnly _cadenaPropietario As String

    Public Sub New(cadenaPropietario As String)
        _cadenaPropietario = cadenaPropietario
    End Sub

    Public Shared Function Disponibles() As List(Of (Version As String, Archivo As String, Sql As String))
        Dim asm = GetType(Migrador).Assembly
        Dim lista As New List(Of (String, String, String))
        For Each nombre In asm.GetManifestResourceNames().Where(Function(n) n.StartsWith("Migraciones.V", StringComparison.Ordinal))
            Dim archivo = nombre.Substring("Migraciones.".Length)
            Dim version = archivo.Substring(0, archivo.IndexOf("__", StringComparison.Ordinal))
            Using s = asm.GetManifestResourceStream(nombre), r As New StreamReader(s, Encoding.UTF8)
                lista.Add((version, archivo, r.ReadToEnd()))
            End Using
        Next
        Return lista.OrderBy(Function(m) Integer.Parse(m.Item1.Substring(1))).
                     Select(Function(m) (Version:=m.Item1, Archivo:=m.Item2, Sql:=m.Item3)).ToList()
    End Function

    Private Shared Function Hash(sql As String) As String
        Return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql.Replace(vbCrLf, vbLf))))
    End Function

    ''' <param name="hastaVersion">Solo para ensayos de actualización (p. ej. "V011"): se detiene en esa versión.</param>
    Public Function Migrar(Optional hastaVersion As String = Nothing) As List(Of MigracionAplicada)
        Dim resultado As New List(Of MigracionAplicada)
        Using cn As New NpgsqlConnection(_cadenaPropietario)
            cn.Open()
            Using cmd As New NpgsqlCommand(
                "CREATE TABLE IF NOT EXISTS esquema_migracion (version TEXT PRIMARY KEY, archivo TEXT NOT NULL, sha256 TEXT NOT NULL, " &
                "aplicada_en TIMESTAMPTZ NOT NULL DEFAULT now())", cn)
                cmd.ExecuteNonQuery()
            End Using
            ' Un solo migrador a la vez.
            Using cmd As New NpgsqlCommand("SELECT pg_advisory_lock(727274)", cn)
                cmd.ExecuteNonQuery()
            End Using
            Try
                Dim aplicadas As New Dictionary(Of String, String)
                Using cmd As New NpgsqlCommand("SELECT version, sha256 FROM esquema_migracion", cn), rd = cmd.ExecuteReader()
                    While rd.Read()
                        aplicadas(rd.GetString(0)) = rd.GetString(1)
                    End While
                End Using

                For Each m In Disponibles()
                    If hastaVersion IsNot Nothing AndAlso Integer.Parse(m.Version.Substring(1)) > Integer.Parse(hastaVersion.Substring(1)) Then Exit For
                    Dim h = Hash(m.Sql)
                    Dim previo As String = Nothing
                    If aplicadas.TryGetValue(m.Version, previo) Then
                        If previo <> h Then
                            Throw New AppSistema.Dominio.ReglaNegocioException("MIGRACION_MODIFICADA",
                                $"La migracion {m.Archivo} ya se aplico con otro contenido. Las migraciones aplicadas no se editan: cree una nueva.")
                        End If
                        resultado.Add(New MigracionAplicada With {.Version = m.Version, .Archivo = m.Archivo, .Aplicada = False})
                        Continue For
                    End If
                    Using tx = cn.BeginTransaction()
                        Using cmd As New NpgsqlCommand(m.Sql, cn, tx)
                            cmd.ExecuteNonQuery()
                        End Using
                        Using cmd As New NpgsqlCommand("INSERT INTO esquema_migracion(version, archivo, sha256) VALUES (@v, @a, @h)", cn, tx)
                            cmd.Parameters.AddWithValue("v", m.Version)
                            cmd.Parameters.AddWithValue("a", m.Archivo)
                            cmd.Parameters.AddWithValue("h", h)
                            cmd.ExecuteNonQuery()
                        End Using
                        tx.Commit()
                    End Using
                    resultado.Add(New MigracionAplicada With {.Version = m.Version, .Archivo = m.Archivo, .Aplicada = True})
                Next
            Finally
                Using cmd As New NpgsqlCommand("SELECT pg_advisory_unlock(727274)", cn)
                    cmd.ExecuteNonQuery()
                End Using
            End Try
        End Using
        Return resultado
    End Function

    ''' <summary>
    ''' Crea (o actualiza la clave de) el usuario de base de datos que usan las computadoras de una sede.
    ''' Es miembro de app_stock: hereda sus permisos y queda sujeto al aislamiento por empresa.
    ''' </summary>
    Public Sub CrearUsuarioSede(nombre As String, clave As String)
        If String.IsNullOrWhiteSpace(nombre) OrElse Not nombre.All(Function(c) Char.IsLetterOrDigit(c) OrElse c = "_"c) Then
            Throw New AppSistema.Dominio.ReglaNegocioException("DATO_INVALIDO", "El nombre solo admite letras, numeros y guion bajo.")
        End If
        AppSistema.Dominio.Seguridad.PoliticaClave.Validar(clave)
        Using cn As New NpgsqlConnection(_cadenaPropietario)
            cn.Open()
            Dim existe As Boolean
            Using cmd As New NpgsqlCommand("SELECT 1 FROM pg_roles WHERE rolname = @n", cn)
                cmd.Parameters.AddWithValue("n", nombre)
                existe = cmd.ExecuteScalar() IsNot Nothing
            End Using
            ' La clave va como literal escapado: CREATE/ALTER ROLE no admiten parámetros.
            Dim literal = "'" & clave.Replace("'", "''") & "'"
            Dim sql = If(existe, $"ALTER ROLE {nombre} WITH LOGIN PASSWORD {literal}",
                                 $"CREATE ROLE {nombre} LOGIN PASSWORD {literal} IN ROLE app_stock")
            Using cmd As New NpgsqlCommand(sql, cn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' Usuario de base de datos del agente de sincronización en la CENTRAL. Solo puede llamar a fn_recibir_evento:
    ''' no lee ni escribe tablas. Cada sede usa además su propia credencial (registrar-sede).
    ''' </summary>
    Public Sub CrearUsuarioSincronizacion(nombre As String, clave As String)
        If String.IsNullOrWhiteSpace(nombre) OrElse Not nombre.All(Function(c) Char.IsLetterOrDigit(c) OrElse c = "_"c) Then
            Throw New AppSistema.Dominio.ReglaNegocioException("DATO_INVALIDO", "El nombre solo admite letras, numeros y guion bajo.")
        End If
        AppSistema.Dominio.Seguridad.PoliticaClave.Validar(clave)
        Using cn As New NpgsqlConnection(_cadenaPropietario)
            cn.Open()
            Dim existe As Boolean
            Using cmd As New NpgsqlCommand("SELECT 1 FROM pg_roles WHERE rolname = @n", cn)
                cmd.Parameters.AddWithValue("n", nombre)
                existe = cmd.ExecuteScalar() IsNot Nothing
            End Using
            Dim literal = "'" & clave.Replace("'", "''") & "'"
            Dim sql = If(existe, $"ALTER ROLE {nombre} WITH LOGIN PASSWORD {literal}",
                                 $"CREATE ROLE {nombre} LOGIN PASSWORD {literal} IN ROLE app_sincronizacion")
            Using cmd As New NpgsqlCommand(sql, cn)
                cmd.ExecuteNonQuery()
            End Using
        End Using
    End Sub

End Class
