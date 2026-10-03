Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>Ventana principal (MDI): menú según permisos y barra de estado con empresa, operación y usuario.</summary>
Public Class FormPrincipal
    Inherits Form

    Private _config As Configuracion
    Private _sesion As SesionUsuario
    Private ReadOnly _menu As New MenuStrip()
    Private ReadOnly _estado As New StatusStrip()
    Private ReadOnly _etiquetaEstado As New ToolStripStatusLabel With {.Spring = True, .TextAlign = Drawing.ContentAlignment.MiddleLeft}

    Public Sub New()
        Text = "AppSistema"
        IsMdiContainer = True
        WindowState = FormWindowState.Maximized
        MainMenuStrip = _menu
        _estado.Items.Add(_etiquetaEstado)
        _estado.Items.Add(New ToolStripStatusLabel("Version " & GetType(FormPrincipal).Assembly.GetName().Version.ToString(3)))
        Controls.Add(_menu) : Controls.Add(_estado)
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
            If f.ShowDialog(Me) <> DialogResult.OK Then
                Close()
                Return
            End If
            _config = f.ConfiguracionUsada
            _sesion = f.Sesion
        End Using
        ConstruirMenu()
        _etiquetaEstado.Text = $"Empresa {_sesion.EmpresaCodigo}  |  Operacion {_sesion.Operacion}  |  Usuario {_sesion.NombreUsuario} ({_sesion.Login})"
    End Sub

    Private Sub ConstruirMenu()
        _menu.Items.Clear()
        Dim cadena = _config.CadenaConexion()

        Dim catalogo As New ToolStripMenuItem("&Catalogo")
        Agregar(catalogo, "&Productos, variantes y empaques", Permisos.CatalogoVer, Function() New FormCatalogo(cadena, _sesion))
        Agregar(catalogo, "Pro&veedores y precios", Permisos.CatalogoVer, Function() New FormProveedores(cadena, _sesion))
        Agregar(catalogo, "&Importar catalogo...", Permisos.CatalogoImportar, Function() New FormImportacion(cadena, _sesion))

        Dim menus As New ToolStripMenuItem("&Menus")
        Agregar(menus, "&Recetas", Permisos.MenusVer, Function() New FormRecetas(cadena, _sesion))
        Agregar(menus, "&Minutas y necesidades", Permisos.MenusVer, Function() New FormMinutas(cadena, _sesion))
        Agregar(menus, "&Servicios y estructuras", Permisos.MenusConfigurar, Function() New FormServicios(cadena, _sesion))
        Agregar(menus, "&Importar recetas...", Permisos.RecetasEditar, Function() New FormImportacion(cadena, _sesion))
        Agregar(menus, "&Produccion", Permisos.MenusVer, Function() New FormProduccion(cadena, _sesion))

        Dim almacen As New ToolStripMenuItem("A&lmacen")
        Agregar(almacen, "&Stock e inventario inicial", Permisos.CatalogoVer, Function() New FormStock(cadena, _sesion))

        Dim compras As New ToolStripMenuItem("C&ompras")
        Agregar(compras, "&Prevision y pedidos", Permisos.ComprasVer, Function() New FormCompras(cadena, _sesion))

        Dim admin As New ToolStripMenuItem("&Administracion")
        Agregar(admin, "&Usuarios y roles", Permisos.UsuariosAdministrar, Function() New FormUsuarios(cadena, _sesion))

        Dim sesionMenu As New ToolStripMenuItem("&Sesion")
        sesionMenu.DropDownItems.Add("Cambiar &clave...", Nothing, Sub() CambiarClave())
        sesionMenu.DropDownItems.Add("&Conexion con el servidor...", Nothing, Sub()
                                                                                  Dim nueva = EditarConexion(Me, _config)
                                                                                  If nueva IsNot Nothing Then Ui.Informar(Me, "La nueva conexion se usara al volver a iniciar la aplicacion.")
                                                                              End Sub)
        sesionMenu.DropDownItems.Add(New ToolStripSeparator())
        sesionMenu.DropDownItems.Add("&Salir", Nothing, Sub() Close())

        Dim ventanas As New ToolStripMenuItem("&Ventanas")
        _menu.MdiWindowListItem = ventanas
        _menu.Items.AddRange({catalogo, menus, compras, almacen, admin, sesionMenu, ventanas})
    End Sub

    ''' <summary>Opción de menú visible solo si el usuario tiene el permiso. Reutiliza la ventana si ya está abierta.</summary>
    Private Sub Agregar(menu As ToolStripMenuItem, texto As String, permiso As String, crear As Func(Of Form))
        If Not _sesion.Tiene(permiso) Then Return
        Dim item As New ToolStripMenuItem(texto)
        Dim abierta As Form = Nothing
        AddHandler item.Click, Sub()
                                   If abierta IsNot Nothing AndAlso Not abierta.IsDisposed Then
                                       abierta.Activate()
                                       Return
                                   End If
                                   Ui.Ejecutar(Me, Sub()
                                                       abierta = crear()
                                                       abierta.MdiParent = Me
                                                       abierta.WindowState = FormWindowState.Maximized
                                                       abierta.Show()
                                                   End Sub)
                               End Sub
        menu.DropDownItems.Add(item)
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
