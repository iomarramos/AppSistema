Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Matriz de acceso (superusuario): para una persona y una operación muestra módulo → pantalla → acción, si su rol lo
''' da, la excepción (concedido o negado) y el resultado efectivo. Solo el superusuario concede o niega fuera del rol;
''' la base también lo exige, y lo que muestra esta pantalla es lo mismo que valida la base.
''' </summary>
Public Class FormMatrizAcceso
    Inherits Form

    Private ReadOnly _servicio As ServicioAdministracion
    Private ReadOnly _usuario As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 220}
    Private ReadOnly _operacion As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 240}
    Private ReadOnly _matriz As DataGridView = Ui.NuevaGrilla()

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _servicio = New ServicioAdministracion(cadena, sesion)
        Text = "Matriz de acceso"
        Dim dueno = sesion.EsDueno
        Controls.Add(_matriz)
        Controls.Add(New Label With {.Dock = DockStyle.Top, .Height = 34, .Padding = New Padding(4),
            .Text = "Por rol = lo da su rol segun el alcance (operacion, zona o todas). Excepcion = lo que el superusuario concede o niega a la persona. Lo negado gana."})
        Controls.Add(Ui.BarraBotones(New Label With {.Text = "Usuario", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _usuario,
                                     New Label With {.Text = "Operacion", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _operacion,
                                     Ui.Boton("Ver", AddressOf Cargar),
                                     Ui.BotonSi(dueno, "Conceder aqui", Sub() Fijar(True, False)),
                                     Ui.BotonSi(dueno, "Negar aqui", Sub() Fijar(False, False)),
                                     Ui.BotonSi(dueno, "Conceder en todas", Sub() Fijar(True, True)),
                                     Ui.BotonSi(dueno, "Negar en todas", Sub() Fijar(False, True)),
                                     Ui.BotonSi(dueno, "Quitar excepcion...", AddressOf Quitar)))
        AddHandler Load, Sub() Iniciar()
    End Sub

    Private Sub Iniciar()
        Ui.Ejecutar(Me, Sub()
                            _usuario.Items.AddRange(_servicio.ListarUsuarios().Where(Function(u) u.Activo) _
                                .Select(Function(u) CObj(New Opcion(Of Long)(u.Id, $"{u.Login} - {u.Nombre}"))).ToArray())
                            _operacion.Items.AddRange(_servicio.ListarOperaciones() _
                                .Select(Function(o) CObj(New Opcion(Of Long)(o.Id, $"{o.Codigo} - {o.Nombre}" & If(String.IsNullOrEmpty(o.Zona), "", $" ({o.Zona})")))).ToArray())
                        End Sub)
        If _usuario.Items.Count > 0 Then _usuario.SelectedIndex = 0
        If _operacion.Items.Count > 0 Then _operacion.SelectedIndex = 0
    End Sub

    Private Function Elegidos() As (Usuario As Long, Operacion As Long)?
        Dim u = TryCast(_usuario.SelectedItem, Opcion(Of Long))
        Dim o = TryCast(_operacion.SelectedItem, Opcion(Of Long))
        If u Is Nothing OrElse o Is Nothing Then Ui.Informar(Me, "Elija usuario y operacion.") : Return Nothing
        Return (u.Valor, o.Valor)
    End Function

    Private Sub Cargar()
        Dim e = Elegidos()
        If Not e.HasValue Then Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_matriz, _servicio.MatrizDeAcceso(e.Value.Usuario, e.Value.Operacion),
                                         "Modulo|Modulo", "Pantalla|Pantalla", "Accion|Accion", "Descripcion|Permite",
                                         "PorRol|Por rol", "Excepcion|Excepcion", "Efectivo|Efectivo"))
    End Sub

    Private Sub Fijar(conceder As Boolean, enTodas As Boolean)
        Dim e = Elegidos()
        Dim fila = Ui.Seleccionado(Of MatrizAccesoDto)(_matriz)
        If Not e.HasValue OrElse fila Is Nothing Then Return
        Using d As New DialogoCampos($"{If(conceder, "Conceder", "Negar")} {fila.Pantalla} - {fila.Accion}{If(enTodas, " en todas las operaciones", "")}")
            d.Texto("motivo", "Motivo")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.FijarExcepcion(e.Value.Usuario, fila.Permiso, If(enTodas, CType(Nothing, Long?), e.Value.Operacion), conceder, d.Valor("motivo")))
        End Using
        Cargar()
    End Sub

    Private Sub Quitar()
        Dim e = Elegidos()
        Dim fila = Ui.Seleccionado(Of MatrizAccesoDto)(_matriz)
        If Not e.HasValue OrElse fila Is Nothing OrElse fila.Excepcion = "" Then Return
        Dim enTodas = fila.Excepcion.EndsWith("(todas)")
        If Not Ui.Confirmar(Me, $"Quitar la excepcion de {fila.Pantalla} - {fila.Accion}? Vuelve a lo que da el rol.") Then Return
        Ui.Ejecutar(Me, Sub() _servicio.FijarExcepcion(e.Value.Usuario, fila.Permiso, If(enTodas, CType(Nothing, Long?), e.Value.Operacion), Nothing))
        Cargar()
    End Sub

End Class
