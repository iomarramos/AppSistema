Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Ventana principal (MDI): menú según permisos y barra de estado con empresa, operación y usuario. El menú y la barra
''' se diseñan en FormPrincipal.Designer.vb (Diseñador de Visual Studio); aquí van los permisos, el tema y las acciones.
''' </summary>
Partial Public Class FormPrincipal

    Private _config As Configuracion
    Private _sesion As SesionUsuario
    Private _lblContexto As Label
    Private _lblRol As Label

    Public Sub New()
        InitializeComponent()
        lblVersion.Text = "Version " & GetType(FormPrincipal).Assembly.GetName().Version.ToString(3)
        Tema.AplicarShell(_menu, _estado)
        CrearEncabezado()
        For Each mdi In Controls.OfType(Of MdiClient)()
            mdi.BackColor = Tema.Fondo
        Next
        ' Toda ventana de trabajo recibe la franja con la operación y el estilo común (también las que abre otra pantalla).
        AddHandler MdiChildActivate, Sub()
                                         If ActiveMdiChild IsNot Nothing AndAlso _sesion IsNot Nothing Then Tema.AplicarVentana(ActiveMdiChild, _sesion.Operacion?.ToString())
                                     End Sub
        AddHandler Shown, Sub() IniciarSesion()
    End Sub

    Private Sub IniciarSesion()
        Try
            _config = Configuracion.Cargar()
        Catch ex As Exception
            Ui.MostrarError(Me, ex)
        End Try
        If _config Is Nothing Then _config = EditarConexion(Me, Nothing)
        Using f As New FormAcceso(_config)
            Tema.Aplicar(f)
            If f.ShowDialog(Me) <> DialogResult.OK Then
                Close()
                Return
            End If
            _config = f.ConfiguracionUsada
            _sesion = f.Sesion
        End Using
        ConstruirMenu()
        ActualizarEncabezado()
        _etiquetaEstado.Text = $"Empresa {_sesion.EmpresaCodigo}  |  Operacion {_sesion.Operacion}  |  Usuario {_sesion.NombreUsuario} ({_sesion.Login})" &
                               If(_sesion.EsDueno, "  |  DUENO DEL SISTEMA", "")
        If _sesion.Tiene(Permisos.ReportesVer) Then
            ' Distingue lo confirmado en la sede de lo ya recibido por la central; se refresca cada 5 minutos.
            Dim envio As New ToolStripStatusLabel()
            _estado.Items.Insert(1, envio)
            Dim refrescar = Sub()
                                Try
                                    envio.Text = New ServicioCierres(_config.CadenaConexion(), _sesion).EstadoEnvio().ToString()
                                Catch ex As Exception
                                    envio.Text = "Central: estado no disponible"
                                End Try
                            End Sub
            refrescar()
            Dim reloj As New Timer With {.Interval = 300000}
            AddHandler reloj.Tick, Sub() refrescar()
            reloj.Start()
        End If
    End Sub


    Private Sub CrearEncabezado()
        Dim cabecera As New Panel With {
            .Name = "pnlCabecera",
            .Dock = DockStyle.Top,
            .Height = 82,
            .BackColor = Tema.Superficie,
            .Padding = New Padding(22, 12, 22, 10)
        }

        Dim marca As New Panel With {
            .Dock = DockStyle.Left,
            .Width = 6,
            .BackColor = Tema.Acento
        }

        Dim textos As New Panel With {
            .Dock = DockStyle.Left,
            .Width = 440,
            .Padding = New Padding(16, 2, 0, 0)
        }

        Dim titulo As New Label With {
            .Text = "AppSistema",
            .Dock = DockStyle.Top,
            .Height = 31,
            .Font = Tema.FuenteHero,
            .ForeColor = Tema.Texto,
            .TextAlign = Drawing.ContentAlignment.MiddleLeft
        }

        Dim subtitulo As New Label With {
            .Text = "Planificación · Compras · Inventarios · Producción · Control",
            .Dock = DockStyle.Top,
            .Height = 24,
            .Font = Tema.Fuente,
            .ForeColor = Tema.TextoSecundario,
            .TextAlign = Drawing.ContentAlignment.MiddleLeft
        }

        textos.Controls.Add(subtitulo)
        textos.Controls.Add(titulo)

        Dim contexto As New Panel With {
            .Dock = DockStyle.Right,
            .Width = 440,
            .Padding = New Padding(0, 4, 0, 0)
        }

        _lblRol = New Label With {
            .Dock = DockStyle.Top,
            .Height = 26,
            .Font = Tema.FuenteSemibold,
            .ForeColor = Tema.AcentoOscuro,
            .TextAlign = Drawing.ContentAlignment.MiddleRight
        }

        _lblContexto = New Label With {
            .Dock = DockStyle.Top,
            .Height = 24,
            .Font = Tema.Fuente,
            .ForeColor = Tema.TextoSecundario,
            .TextAlign = Drawing.ContentAlignment.MiddleRight
        }

        contexto.Controls.Add(_lblContexto)
        contexto.Controls.Add(_lblRol)

        Dim borde As New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 1,
            .BackColor = Tema.Borde
        }

        cabecera.Controls.Add(contexto)
        cabecera.Controls.Add(textos)
        cabecera.Controls.Add(marca)
        cabecera.Controls.Add(borde)
        Controls.Add(cabecera)
        cabecera.BringToFront()
        _menu.BringToFront()
        _estado.BringToFront()
    End Sub

    Private Sub ActualizarEncabezado()
        If _sesion Is Nothing Then Return
        _lblContexto.Text = $"{_sesion.EmpresaCodigo}  ·  {_sesion.Operacion}  ·  {_sesion.Login}"
        _lblRol.Text = If(_sesion.EsDueno, "SUPERUSUARIO", _sesion.NombreUsuario)
    End Sub

    ''' <summary>Activa cada opción del menú según el permiso de su pantalla; un menú sin opciones permitidas no se muestra.</summary>
    Private Sub ConstruirMenu()
        Dim cadena = _config.CadenaConexion()

        Opcion(mnuProductosVariantesYEmpaques, Permisos.CatalogoVer, Function() New FormCatalogo(cadena, _sesion))
        Opcion(mnuProveedoresYPrecios, Permisos.CatalogoVer, Function() New FormProveedores(cadena, _sesion))
        Opcion(mnuImportarCatalogo, Permisos.CatalogoImportar, Function() New FormImportacion(cadena, _sesion, ModoImportacion.Catalogo))

        Opcion(mnuRecetas, Permisos.MenusVer, Function() New FormRecetas(cadena, _sesion))
        Opcion(mnuMinutasYNecesidades, Permisos.MenusVer, Function() New FormMinutas(cadena, _sesion))
        Opcion(mnuServiciosYEstructuras, Permisos.MenusConfigurar, Function() New FormServicios(cadena, _sesion))
        Opcion(mnuImportarRecetas, Permisos.RecetasEditar, Function() New FormImportacion(cadena, _sesion, ModoImportacion.Recetas))
        Opcion(mnuProduccion, Permisos.MenusVer, Function() New FormProduccion(cadena, _sesion))

        Opcion(mnuStockEInventarioInicial, Permisos.CatalogoVer, Function() New FormStock(cadena, _sesion))
        Opcion(mnuInventarioFisico, Permisos.InventarioContar, Function() New FormInventarios(cadena, _sesion))

        Opcion(mnuPrevisionYPedidos, Permisos.ComprasVer, Function() New FormCompras(cadena, _sesion))
        Opcion(mnuConsolidadoDeComprasTodasLasOperaciones, Permisos.ComprasConsolidar, Function() New FormConsolidado(cadena, _sesion))

        Opcion(mnuPendientesCierresYFoodCost, Permisos.ReportesVer, Function() New FormCierres(cadena, _sesion))
        Opcion(mnuContratosYClientes, Permisos.ContratosVer, Function() New FormContratos(cadena, _sesion))
        Opcion(mnuGastosYResultadoMensual, Permisos.ResultadosVer, Function() New FormResultados(cadena, _sesion))

        Opcion(mnuUsuariosYRoles, Permisos.UsuariosAdministrar, Function() New FormUsuarios(cadena, _sesion))
        Opcion(mnuMatrizDeAcceso, Permisos.UsuariosAdministrar, Function() New FormMatrizAcceso(cadena, _sesion))
        Opcion(mnuOperacionesYAlmacenes, Permisos.UsuariosAdministrar, Function() New FormOperaciones(cadena, _sesion))
        Opcion(mnuAuditoria, Permisos.AuditoriaVer, Function() New FormAuditoria(cadena, _sesion))
        Opcion(mnuCargaDeDatosReales, Permisos.CatalogoImportar, Function() New FormCargaReal(cadena, _sesion))
        Opcion(mnuSincronizacionYRespaldoTI, Permisos.UsuariosAdministrar, Function() New FormContinuidad(_config, _sesion))

        ' Solo se muestran los menús en los que el usuario tiene alguna opción (Sesión y Ventanas siempre).
        For Each m In {mnuCatalogo, mnuMenus, mnuCompras, mnuAlmacen, mnuCierresYControl, mnuAdministracion}
            m.Available = m.DropDownItems.OfType(Of ToolStripMenuItem)().Any(Function(i) i.Available)
        Next
        ' Nombre accesible = texto visible (sin "&" ni "..."): es lo que lee UI Automation (pruebas E2E y lectores de pantalla).
        For Each item In TodasLasOpciones(_menu.Items)
            item.AccessibleName = Identificadores.NombreAccesible(item.Text)
        Next
    End Sub

    Private Shared Iterator Function TodasLasOpciones(items As ToolStripItemCollection) As IEnumerable(Of ToolStripMenuItem)
        For Each item In items.OfType(Of ToolStripMenuItem)()
            Yield item
            For Each hijo In TodasLasOpciones(item.DropDownItems)
                Yield hijo
            Next
        Next
    End Function

    ''' <summary>Opción de menú disponible solo si el usuario tiene el permiso. Reutiliza la ventana si ya está abierta.</summary>
    Private Sub Opcion(item As ToolStripMenuItem, permiso As String, crear As Func(Of Form))
        item.Available = _sesion.Tiene(permiso)
        If Not item.Available Then Return
        Dim abierta As Form = Nothing
        AddHandler item.Click, Sub()
                                   If abierta IsNot Nothing AndAlso Not abierta.IsDisposed Then
                                       abierta.Activate()
                                       Return
                                   End If
                                   Ui.Ejecutar(Me, Sub()
                                                       abierta = crear()
                                                       abierta.MdiParent = Me
                                                       Tema.AplicarVentana(abierta, _sesion.Operacion?.ToString())
                                                       abierta.WindowState = FormWindowState.Maximized
                                                       abierta.Show()
                                                   End Sub)
                               End Sub
    End Sub

    Private Sub mnuCambiarClave_Click(sender As Object, e As EventArgs) Handles mnuCambiarClave.Click
        CambiarClave()
    End Sub

    Private Sub mnuConexionConElServidor_Click(sender As Object, e As EventArgs) Handles mnuConexionConElServidor.Click
        Dim nueva = EditarConexion(Me, _config)
        If nueva IsNot Nothing Then Ui.Informar(Me, "La nueva conexion se usara al volver a iniciar la aplicacion.")
    End Sub

    Private Sub mnuSalir_Click(sender As Object, e As EventArgs) Handles mnuSalir.Click
        Close()
    End Sub

    Private Sub CambiarClave()
        Using d As New DialogoCampos("Cambiar clave")
            d.Texto("actual", "Clave actual", esClave:=True).Texto("nueva", "Clave nueva", esClave:=True).Texto("repetir", "Repetir clave nueva", esClave:=True)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            If d.ValorSinRecortar("nueva") <> d.ValorSinRecortar("repetir") Then
                Ui.Informar(Me, "Las claves nuevas no coinciden.")
                Return
            End If
            If Ui.Ejecutar(Me, Sub() Call New ServicioAcceso(_config.CadenaConexion()).CambiarClave(_sesion, d.ValorSinRecortar("actual"), d.ValorSinRecortar("nueva"))) Then
                Ui.Informar(Me, "Clave actualizada.")
            End If
        End Using
    End Sub

    ''' <summary>Pide los datos de conexión, los prueba y los guarda. Devuelve Nothing si se cancela.</summary>
    Public Shared Function EditarConexion(dueno As IWin32Window, actual As Configuracion) As Configuracion
        Dim c = If(actual, New Configuracion())
        Using d As New DialogoCampos("Conexion con el servidor de la sede")
            d.Texto("servidor", "Servidor", c.Servidor).Texto("puerto", "Puerto", c.Puerto.ToString()).Texto("base", "Base de datos", c.BaseDatos) _
             .Texto("usuario", "Usuario de la base", c.UsuarioBD).Texto("clave", "Clave de la base", esClave:=True) _
             .Texto("empresa", "Empresa predeterminada", If(c.EmpresaPredeterminada, ""))
            While d.ShowDialog(dueno) = DialogResult.OK
                Dim nueva As New Configuracion With {.Servidor = d.Valor("servidor"), .BaseDatos = d.Valor("base"), .UsuarioBD = d.Valor("usuario"),
                                                     .EmpresaPredeterminada = d.Valor("empresa"), .ClaveBDProtegida = c.ClaveBDProtegida}
                Dim guardada = Ui.Ejecutar(dueno,
                    Sub()
                        nueva.Puerto = CInt(Ui.LeerEntero(d.Valor("puerto"), "puerto"))
                        If d.ValorSinRecortar("clave") <> "" Then nueva.FijarClave(d.ValorSinRecortar("clave"))
                        nueva.Probar()
                        nueva.Guardar()
                    End Sub)
                If guardada Then Return nueva
            End While
        End Using
        Return Nothing
    End Function
End Class
