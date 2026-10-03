Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Usuarios de la empresa, sus roles por operación y hasta qué nivel llega cada uno en cada módulo. El dueño del sistema
''' ve y asigna en todas las operaciones; los demás administradores solo pueden dar permisos que ellos mismos tienen.
''' </summary>
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
                                     Ui.Boton("Roles y permisos", AddressOf VerRoles), Ui.Boton("Rol propio...", AddressOf EditarRol),
                                     Ui.Boton("Accesos por modulo", AddressOf VerAccesos),
                                     Ui.Boton("Asignaciones y alcance", AddressOf VerAsignaciones),
                                     Ui.BotonSi(sesion.EsDueno, "Alcance...", AddressOf CambiarAlcance),
                                     Ui.BotonSi(sesion.EsDueno, "Dar rango de dueno", Sub() Dueno(True)),
                                     Ui.BotonSi(sesion.EsDueno, "Quitar rango de dueno", Sub() Dueno(False))))
        AddHandler Load, Sub() Cargar()
    End Sub

    Private Sub Cargar()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_usuarios, _servicio.ListarUsuarios(), "Login|Usuario", "Nombre|Nombre", "EsDueno|Dueno del sistema", "Roles|Operacion:rol", "Activo|Activo"))
    End Sub

    ''' <summary>Roles de la empresa (base y propios) leídos de la base.</summary>
    Private Function OpcionesRoles() As IEnumerable(Of Object)
        Dim roles As List(Of RolDto) = Nothing
        If Not Ui.Ejecutar(Me, Sub() roles = _servicio.ListarRoles()) Then Return Enumerable.Empty(Of Object)()
        Return roles.Select(Function(r) CObj(New Opcion(Of String)(r.Codigo, $"{r.Nombre} - {ResumenModulos(r.Permisos)}")))
    End Function

    ''' <summary>"Planificacion: Aprobar; Catalogo: Ver"...: hasta dónde llega el rol en cada módulo.</summary>
    Private Shared Function ResumenModulos(permisos As IEnumerable(Of String)) As String
        Return String.Join("; ", Modulos.Todos.Select(Function(m) (m, Modulos.NivelEnModulo(permisos, m))).Where(Function(x) x.Item2 <> "") _
                                                .Select(Function(x) $"{x.Item1}: {x.Item2}"))
    End Function

    ''' <summary>Operaciones donde se puede asignar: el dueño, todas; los demás, las suyas.</summary>
    Private Function OpcionesOperaciones() As IEnumerable(Of Object)
        Return _sesion.Operaciones.Select(Function(o) CObj(New Opcion(Of Long)(o.Id, o.ToString())))
    End Function

    Private Sub VerRoles()
        Ui.Ejecutar(Me, Sub() Ui.MostrarLista(Me, "Roles y permisos", "Roles de la empresa (los del sistema no se editan)", _servicio.ListarRoles(),
                                              "Codigo|Rol", "Nombre|Nombre", "EsBase|Del sistema", "PermisosTexto|Permisos"))
    End Sub

    Private Sub VerAccesos()
        Ui.Ejecutar(Me, Sub() Ui.MostrarLista(Me, "Accesos por modulo",
                                              "Hasta que nivel llega cada persona en cada modulo y operacion: Ver, Editar, Aprobar o Administrar (vacio = sin acceso).",
                                              _servicio.AccesosPorModulo(), "Login|Usuario", "Nombre|Nombre", "Operacion|Operacion", "Roles|Roles",
                                              "Planificacion|Planificacion", "Abastecimiento|Abastecimiento", "Catalogo|Catalogo", "Produccion|Produccion",
                                              "Almacen|Almacen", "Inventario|Inventario", "Cierres|Cierres y Food Cost", "Resultados|Resultados",
                                              "Administracion|Administracion"))
    End Sub

    Private Sub VerAsignaciones()
        Ui.Ejecutar(Me, Sub() Ui.MostrarLista(Me, "Asignaciones y alcance",
                                              "Alcance: OPERACION (solo esa sede), ZONA (todas las sedes de su zona o region) o TODAS (toda la empresa).",
                                              _servicio.ListarAsignaciones(), "Login|Usuario", "Operacion|Operacion", "Zona|Zona", "Rol|Rol", "Alcance|Alcance"))
    End Sub

    ''' <summary>Solo el superusuario amplía el alcance de una asignación (por zona o a todas las operaciones).</summary>
    Private Sub CambiarAlcance()
        Dim u = Ui.Seleccionado(Of UsuarioResumen)(_usuarios)
        If u Is Nothing Then Return
        Using d As New DialogoCampos($"Alcance de un rol de {u.Login}")
            Dim ops = OpcionesOperaciones().ToList()
            d.Opciones("operacion", "Operacion de la asignacion", ops, ops.FirstOrDefault(Function(o) DirectCast(o, Opcion(Of Long)).Valor = _sesion.OperacionId.Value))
            d.Opciones("rol", "Rol", OpcionesRoles())
            d.Opciones("alcance", "Alcance", Alcances.Todos.Select(Function(a) CObj(New Opcion(Of String)(a, a))))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.FijarAlcance(u.Id, d.Elegido(Of Opcion(Of Long))("operacion").Valor, d.Elegido(Of Opcion(Of String))("rol").Valor,
                                                          d.Elegido(Of Opcion(Of String))("alcance").Valor))
        End Using
        Cargar()
    End Sub

    Private Sub Dueno(darRango As Boolean)
        Dim u = Ui.Seleccionado(Of UsuarioResumen)(_usuarios)
        If u Is Nothing Then Return
        Dim texto = If(darRango, $"Dar a {u.Login} el rango de DUENO DEL SISTEMA? Tendra todos los permisos en todas las operaciones.",
                                 $"Quitar a {u.Login} el rango de dueno del sistema? Quedara solo con sus roles.")
        If Not Ui.Confirmar(Me, texto) Then Return
        Ui.Ejecutar(Me, Sub() _servicio.MarcarDueno(u.Id, darRango))
        Cargar()
    End Sub

    ''' <summary>Crea un rol propio o cambia sus permisos, marcados por módulo y nivel.</summary>
    Private Sub EditarRol()
        Using d As New DialogoCampos("Rol propio: marque hasta donde llega en cada modulo")
            d.Texto("codigo", "Codigo (nuevo o existente propio)").Texto("nombre", "Nombre")
            For Each p In Permisos.Todos.OrderBy(Function(x) Modulos.Todos.ToList().IndexOf(Modulos.ModuloDe(x))) _
                                        .ThenBy(Function(x) Modulos.Niveles.ToList().IndexOf(Modulos.NivelDe(x)))
                d.Marca(p, $"{Modulos.ModuloDe(p)} - {Modulos.NivelDe(p)}: {Permisos.Descripcion(p)}")
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
        Using d As New DialogoCampos($"Asignar rol a {u.Login}")
            Dim ops = OpcionesOperaciones().ToList()
            d.Opciones("operacion", "Operacion", ops, ops.FirstOrDefault(Function(o) DirectCast(o, Opcion(Of Long)).Valor = _sesion.OperacionId.Value))
            d.Opciones("rol", "Rol", OpcionesRoles())
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.AsignarRol(u.Id, d.Elegido(Of Opcion(Of Long))("operacion").Valor, d.Elegido(Of Opcion(Of String))("rol").Valor))
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
