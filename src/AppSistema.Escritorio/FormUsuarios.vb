Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>Usuarios de la empresa y sus roles en la operación de trabajo.</summary>
Public Class FormUsuarios
    Inherits Form

    Private ReadOnly _servicio As ServicioAdministracion
    Private ReadOnly _sesion As SesionUsuario
    Private ReadOnly _usuarios As DataGridView = Ui.NuevaGrilla()

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _servicio = New ServicioAdministracion(cadena, sesion)
        _sesion = sesion
        Text = "Usuarios y roles"
        Controls.Add(_usuarios)
        Controls.Add(Ui.BarraBotones(Ui.Boton("Nuevo usuario", AddressOf NuevoUsuario), Ui.Boton("Asignar rol", AddressOf AsignarRol),
                                     Ui.Boton("Quitar rol", AddressOf QuitarRol), Ui.Boton("Desactivar", AddressOf Desactivar),
                                     Ui.Boton("Roles y permisos", AddressOf VerRoles), Ui.Boton("Rol propio...", AddressOf EditarRol)))
        AddHandler Load, Sub() Cargar()
    End Sub

    Private Sub Cargar()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_usuarios, _servicio.ListarUsuarios(), "Login|Usuario", "Nombre|Nombre", "Roles|Operacion:rol", "Activo|Activo"))
    End Sub

    ''' <summary>Roles de la empresa (base y propios) leídos de la base.</summary>
    Private Function OpcionesRoles() As IEnumerable(Of Object)
        Dim roles As List(Of RolDto) = Nothing
        If Not Ui.Ejecutar(Me, Sub() roles = _servicio.ListarRoles()) Then Return Enumerable.Empty(Of Object)()
        Return roles.Select(Function(r) CObj(New Opcion(Of String)(r.Codigo, $"{r.Nombre} ({r.PermisosTexto})")))
    End Function

    Private Sub VerRoles()
        Ui.Ejecutar(Me, Sub() Ui.MostrarLista(Me, "Roles y permisos", "Roles de la empresa (los del sistema no se editan)", _servicio.ListarRoles(),
                                              "Codigo|Rol", "Nombre|Nombre", "EsBase|Del sistema", "PermisosTexto|Permisos"))
    End Sub

    ''' <summary>Crea un rol propio o cambia sus permisos (marcando cada permiso).</summary>
    Private Sub EditarRol()
        Using d As New DialogoCampos("Rol propio")
            d.Texto("codigo", "Codigo (nuevo o existente propio)").Texto("nombre", "Nombre")
            For Each p In Permisos.Todos
                d.Marca(p, Permisos.Descripcion(p))
            Next
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.GuardarRol(d.Valor("codigo"), d.Valor("nombre"), Permisos.Todos.Where(Function(p) d.Marcado(p)).ToList()))
        End Using
    End Sub

    Private Sub QuitarRol()
        Dim u = Ui.Seleccionado(Of UsuarioResumen)(_usuarios)
        If u Is Nothing Then Return
        Using d As New DialogoCampos($"Quitar rol a {u.Login} en {_sesion.Operacion}")
            d.Opciones("rol", "Rol", OpcionesRoles())
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.QuitarRol(u.Id, _sesion.OperacionId.Value, d.Elegido(Of Opcion(Of String))("rol").Valor))
        End Using
        Cargar()
    End Sub

    Private Sub NuevoUsuario()
        Using d As New DialogoCampos("Nuevo usuario en " & _sesion.Operacion.ToString())
            d.Texto("login", "Usuario").Texto("nombre", "Nombre completo").Texto("clave", "Clave inicial", esClave:=True) _
             .Texto("repetir", "Repetir clave", esClave:=True).Opciones("rol", "Rol", OpcionesRoles())
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            If d.ValorSinRecortar("clave") <> d.ValorSinRecortar("repetir") Then Ui.Informar(Me, "Las claves no coinciden.") : Return
            Ui.Ejecutar(Me, Sub() _servicio.CrearUsuario(d.Valor("login"), d.Valor("nombre"), d.ValorSinRecortar("clave"),
                                                          _sesion.OperacionId.Value, d.Elegido(Of Opcion(Of String))("rol").Valor))
        End Using
        Cargar()
    End Sub

    Private Sub AsignarRol()
        Dim u = Ui.Seleccionado(Of UsuarioResumen)(_usuarios)
        If u Is Nothing Then Return
        Using d As New DialogoCampos($"Asignar rol a {u.Login} en {_sesion.Operacion}")
            d.Opciones("rol", "Rol", OpcionesRoles())
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.AsignarRol(u.Id, _sesion.OperacionId.Value, d.Elegido(Of Opcion(Of String))("rol").Valor))
        End Using
        Cargar()
    End Sub

    Private Sub Desactivar()
        Dim u = Ui.Seleccionado(Of UsuarioResumen)(_usuarios)
        If u Is Nothing OrElse Not Ui.Confirmar(Me, $"Desactivar al usuario {u.Login}? No podra iniciar sesion.") Then Return
        Ui.Ejecutar(Me, Sub() _servicio.DesactivarUsuario(u.Id))
        Cargar()
    End Sub
End Class
