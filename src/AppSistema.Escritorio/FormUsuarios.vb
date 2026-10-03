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
                                     Ui.Boton("Desactivar", AddressOf Desactivar)))
        AddHandler Load, Sub() Cargar()
    End Sub

    Private Sub Cargar()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_usuarios, _servicio.ListarUsuarios(), "Login|Usuario", "Nombre|Nombre", "Roles|Operacion:rol", "Activo|Activo"))
    End Sub

    Private Shared Function OpcionesRoles() As IEnumerable(Of Object)
        Return RolesBase.Todos.Select(Function(r) CObj(New Opcion(Of String)(r.Codigo, $"{r.Nombre} ({String.Join(", ", r.Permisos)})")))
    End Function

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
