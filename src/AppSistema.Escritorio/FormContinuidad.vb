Imports System.Windows.Forms
Imports Npgsql
Imports AppSistema.Datos
Imports AppSistema.Dominio.Numerico

''' <summary>
''' Sincronización y respaldo para TI (etapa 8). Usa la conexión del PROPIETARIO de la base, que se pide al abrir y no
''' se guarda. En la sede: estado de la cola, configurar la sede, enviar ahora, respaldar y ver la conciliación. En la
''' central: sedes registradas, registrar (la credencial se muestra una sola vez) y desactivar. Restaurar y actualizar se
''' hacen solo con el Instalador, con los usuarios fuera del sistema.
''' </summary>
Partial Public Class FormContinuidad

    Private ReadOnly _config As Configuracion
    Private ReadOnly _sesion As SesionUsuario
    Private _servicio As ServicioContinuidad
    Private ReadOnly _estado As New Label With {.Dock = DockStyle.Top, .Height = 64, .Padding = New Padding(6)}
    Private ReadOnly _sedes As DataGridView = Ui.NuevaGrilla()

    Public Sub New()
        InitializeComponent()
    End Sub

    Public Sub New(config As Configuracion, sesion As SesionUsuario)
        InitializeComponent()
        Controls.Clear()
        _config = config
        _sesion = sesion
        Text = "Sincronizacion y respaldo"
        Dim sede = Ui.BarraBotones(New Label With {.Text = "Esta sede:", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)},
                                   Ui.Boton("Actualizar estado", AddressOf RefrescarEstado), Ui.Boton("Configurar sede...", AddressOf ConfigurarSede),
                                   Ui.Boton("Enviar ahora...", AddressOf EnviarAhora), Ui.Boton("Respaldar ahora...", AddressOf Respaldar),
                                   Ui.Boton("Ver conciliacion", AddressOf VerConciliacion))
        Dim central = Ui.BarraBotones(New Label With {.Text = "Central:", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)},
                                      Ui.Boton("Ver sedes", AddressOf CargarSedes), Ui.Boton("Registrar sede...", AddressOf RegistrarSede),
                                      Ui.Boton("Desactivar sede", AddressOf DesactivarSede))
        Controls.Add(_sedes)
        Controls.Add(central)
        Controls.Add(_estado)
        Controls.Add(sede)
        Controls.Add(New Label With {.Dock = DockStyle.Top, .Height = 50, .Padding = New Padding(4),
            .Text = "Para TI. La sincronizacion programada la hace herramientas/windows/programar_sede.ps1; aqui se revisa y se fuerza. " &
                    "Respaldar requiere pg_dump en esta PC (normalmente, el servidor de la sede). Restaurar y actualizar: solo con el Instalador y sin usuarios conectados."})
        AddHandler Load, Sub()
                             If Not PedirConexionPropietario() Then
                                 BeginInvoke(New Action(AddressOf Close))
                                 Return
                             End If
                             RefrescarEstado()
                         End Sub
    End Sub

    ''' <summary>Pide el usuario propietario de la base (no se guarda) y comprueba que conecta.</summary>
    Private Function PedirConexionPropietario() As Boolean
        Using d As New DialogoCampos("Conexion del propietario de la base")
            d.Texto("servidor", "Servidor", _config.Servidor).Texto("puerto", "Puerto", _config.Puerto.ToString()) _
             .Texto("base", "Base de datos", _config.BaseDatos).Texto("usuario", "Usuario propietario").Texto("clave", "Clave", esClave:=True)
            While d.ShowDialog(Me) = DialogResult.OK
                Dim ok = Ui.Ejecutar(Me, Sub()
                                             Dim b As New NpgsqlConnectionStringBuilder With {
                                                 .Host = d.Valor("servidor"), .Port = CInt(Ui.LeerEntero(d.Valor("puerto"), "puerto")), .Database = d.Valor("base"),
                                                 .Username = d.Valor("usuario"), .Password = d.ValorSinRecortar("clave"), .ApplicationName = "AppSistema TI", .Timeout = 15}
                                             Using cn As New NpgsqlConnection(b.ConnectionString)
                                                 cn.Open()
                                             End Using
                                             _servicio = New ServicioContinuidad(b.ConnectionString)
                                         End Sub)
                If ok Then Return True
            End While
        End Using
        Return False
    End Function

    Private Sub RefrescarEstado()
        If _servicio Is Nothing Then Return
        Dim e As EstadoColaDto = Nothing
        If Not Ui.Ejecutar(Me, Sub() e = _servicio.EstadoCola(_sesion.EmpresaCodigo)) Then Return
        _estado.Text = $"Sede: {If(e.Origen, "(sin configurar: use 'Configurar sede...')")}. Cola: {e.Pendientes:N0} pendientes, {e.ConError:N0} con error, " &
                       $"{e.EnConflicto:N0} en conflicto, {e.Enviados:N0} enviados." & vbCrLf &
                       $"Ultimo envio: {If(e.UltimoEnvio.HasValue, e.UltimoEnvio.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm"), "nunca")}" &
                       If(e.UltimoError Is Nothing, "", $". Ultimo error: {e.UltimoError}")
    End Sub

    Private Sub ConfigurarSede()
        If _servicio Is Nothing Then Return
        Using d As New DialogoCampos("Configurar esta sede")
            d.Texto("sede", "Codigo de la sede (el mismo registrado en la central)", _sesion.Operacion?.Codigo)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim n As Long
            If Ui.Ejecutar(Me, Sub() n = _servicio.ConfigurarOrigen(_sesion.EmpresaCodigo, d.Valor("sede"))) Then
                Ui.Informar(Me, $"Sede configurada. {n:N0} eventos de la historia quedaron en la cola para enviar.")
            End If
        End Using
        RefrescarEstado()
    End Sub

    Private Sub EnviarAhora()
        If _servicio Is Nothing Then Return
        Using d As New DialogoCampos("Enviar la cola a la central")
            d.Texto("central", "Conexion de la central (usuario de sincronizacion)", esClave:=True).Texto("credencial", "Credencial de la sede", esClave:=True)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim r As ResultadoEnvio = Nothing
            If Ui.Ejecutar(Me, Sub() r = _servicio.Enviar(_sesion.EmpresaCodigo, d.ValorSinRecortar("central"), d.ValorSinRecortar("credencial"), ignorarEspera:=True)) Then
                Ui.Informar(Me, "Envio: " & r.ToString())
            End If
        End Using
        RefrescarEstado()
    End Sub

    Private Sub Respaldar()
        If _servicio Is Nothing Then Return
        Using a As New SaveFileDialog With {.Filter = "Respaldo PostgreSQL (*.backup)|*.backup", .FileName = $"appsistema_{Date.Now:yyyyMMdd_HHmm}.backup"}
            If a.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim foto As SortedDictionary(Of String, String) = Nothing
            If Ui.Ejecutar(Me, Sub() foto = _servicio.Respaldar(a.FileName)) Then
                Ui.Informar(Me, $"Respaldo listo: {a.FileName}" & vbCrLf &
                                $"Filas: {foto.Where(Function(kv) kv.Key.StartsWith("filas.")).Sum(Function(kv) Long.Parse(kv.Value)):N0}; " &
                                $"valor del stock: S/ {EscalaU6.ADecimal(Long.Parse(foto("saldo.valor_u6"))):N2}." & vbCrLf &
                                $"Guarde junto con el respaldo el archivo {IO.Path.GetFileName(a.FileName)}.conciliacion: sirve para comprobar la restauracion.")
            End If
        End Using
    End Sub

    Private Sub VerConciliacion()
        If _servicio Is Nothing Then Return
        Dim foto As SortedDictionary(Of String, String) = Nothing
        If Not Ui.Ejecutar(Me, Sub() foto = _servicio.Instantanea()) Then Return
        Ui.MostrarLista(Me, "Conciliacion de la base", "Recuentos, saldos y libro de stock. 'conciliacion.filas_sin_conciliar' debe ser 0.",
                        foto.Select(Function(kv) New ParClaveValor With {.Clave = kv.Key, .Valor = kv.Value}).ToList(), "Clave|Clave", "Valor|Valor")
    End Sub

    Private Sub CargarSedes()
        If _servicio Is Nothing Then Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_sedes, _servicio.ResumenCentral(_sesion.EmpresaCodigo), "Sede|Sede", "Nombre|Nombre", "Activa|Activa",
                                         "UltimaSincronizacion|Ultima sincronizacion", "UltimaSecuenciaAplicada|Secuencia", "Documentos|Documentos",
                                         "ValorStockU6|Valor stock", "Retenidos|Retenidos", "Conflictos|Conflictos", "MotivoRetencion|Motivo de retencion"))
    End Sub

    Private Sub RegistrarSede()
        If _servicio Is Nothing Then Return
        Using d As New DialogoCampos("Registrar una sede en la central")
            d.Texto("sede", "Codigo de la sede").Texto("nombre", "Nombre")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim credencial As String = Nothing
            If Not Ui.Ejecutar(Me, Sub() credencial = _servicio.RegistrarSede(_sesion.EmpresaCodigo, d.Valor("sede"), d.Valor("nombre"))) Then Return
            Clipboard.SetText(credencial)
            MessageBox.Show(Me, "Credencial de la sede (se muestra solo esta vez y ya se copio al portapapeles; configurela en el servidor de la sede):" &
                            vbCrLf & vbCrLf & credencial, "Sede registrada", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Using
        CargarSedes()
    End Sub

    Private Sub DesactivarSede()
        Dim s = Ui.Seleccionado(Of SedeCentralDto)(_sedes)
        If s Is Nothing OrElse _servicio Is Nothing Then Ui.Informar(Me, "Seleccione una sede (Ver sedes).") : Return
        If Not Ui.Confirmar(Me, $"Desactivar la sede {s.Sede}? La central rechazara sus envios hasta registrarla de nuevo.") Then Return
        Ui.Ejecutar(Me, Sub() _servicio.DesactivarSede(_sesion.EmpresaCodigo, s.Sede))
        CargarSedes()
    End Sub

    Public NotInheritable Class ParClaveValor
        Public Property Clave As String
        Public Property Valor As String
    End Class

End Class
