Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Inventario físico: corte (general o rotativo), hoja de conteo por envases y parciales (ciega opcional),
''' importación del conteo, reconteo, diferencias, revisión y ajuste autorizado por alguien que no contó.
''' </summary>
Public Class FormInventarios
    Inherits Form

    Private ReadOnly _servicio As ServicioInventarios
    Private ReadOnly _catalogo As ServicioCatalogo
    Private ReadOnly _almacen As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 220}
    Private ReadOnly _ciego As New CheckBox With {.Text = "Conteo ciego", .AutoSize = True, .Margin = New Padding(6, 8, 3, 3)}
    Private ReadOnly _inventarios As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _hoja As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _resumen As New Label With {.Dock = DockStyle.Bottom, .Height = 28, .Padding = New Padding(6)}

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _servicio = New ServicioInventarios(cadena, sesion)
        _catalogo = New ServicioCatalogo(cadena, sesion)
        Dim admin As New ServicioAdministracion(cadena, sesion)
        Text = "Inventario fisico - " & sesion.Operacion.Nombre
        Dim aprueba = sesion.Tiene(Permisos.InventarioAprobar)
        Dim barra = Ui.BarraBotones(New Label With {.Text = "Almacen", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _almacen, _ciego,
                                    Ui.Boton("Nuevo general", Sub() Abrir("general")), Ui.Boton("Nuevo rotativo...", Sub() Abrir("rotativo")),
                                    Ui.Boton("Contar linea...", AddressOf ContarLinea), Ui.Boton("Importar conteo...", AddressOf Importar),
                                    Ui.Boton("Cerrar conteo", Sub() Accion(Sub(i) _servicio.CerrarConteo(i))), Ui.Boton("Recontar", Sub() Accion(Sub(i) _servicio.Recontar(i))),
                                    Ui.BotonSi(aprueba, "Revisar", Sub() Accion(Sub(i) _servicio.Revisar(i))), Ui.BotonSi(aprueba, "Autorizar ajuste...", AddressOf Autorizar))
        Dim division As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal, .SplitterDistance = 140}
        division.Panel1.Controls.Add(_inventarios)
        division.Panel2.Controls.Add(_hoja)
        division.Panel2.Controls.Add(_resumen)
        Controls.Add(division)
        Controls.Add(New Label With {.Dock = DockStyle.Top, .Height = 34, .Padding = New Padding(4),
            .Text = "Mientras se cuenta, esos productos no admiten movimientos. Celda vacia = sin contar (no es cero). El saldo solo cambia al autorizar el ajuste."})
        Controls.Add(barra)
        AddHandler _almacen.SelectedIndexChanged, Sub() CargarInventarios()
        AddHandler _inventarios.SelectionChanged, Sub() CargarHoja()
        AddHandler _ciego.CheckedChanged, Sub() CargarHoja()
        AddHandler Load, Sub() Ui.Ejecutar(Me, Sub()
                                                   For Each a In admin.ListarAlmacenes()
                                                       _almacen.Items.Add(New Opcion(Of AlmacenResumen)(a, $"{a.Codigo} - {a.Nombre}"))
                                                   Next
                                                   If _almacen.Items.Count > 0 Then _almacen.SelectedIndex = 0
                                               End Sub)
    End Sub

    Private ReadOnly Property AlmacenId As Long
        Get
            Dim o = TryCast(_almacen.SelectedItem, Opcion(Of AlmacenResumen))
            Return If(o Is Nothing, 0L, o.Valor.Id)
        End Get
    End Property

    Private ReadOnly Property Inventario As InventarioDto
        Get
            Return Ui.Seleccionado(Of InventarioDto)(_inventarios)
        End Get
    End Property

    Private Sub CargarInventarios()
        If AlmacenId = 0 Then Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_inventarios, _servicio.Listar(AlmacenId), "Numero|Inventario", "Tipo|Tipo", "FechaCorte|Corte", "Estado|Estado"))
    End Sub

    Private Sub CargarHoja()
        Dim i = Inventario
        If i Is Nothing Then _hoja.DataSource = Nothing : _resumen.Text = "" : Return
        Ui.Ejecutar(Me,
            Sub()
                Ui.Mostrar(_hoja, _servicio.Hoja(i.Id, _ciego.Checked), "Descripcion|Producto", "Presentacion|Presentacion", "ContenidoEnvaseU6|Contenido por envase",
                           "Unidad|Unidad", "SistemaU6|Sistema al corte", "FisicoU6|Fisico", "DiferenciaU6|Diferencia", "ValorDiferenciaU6|Valor diferencia", "Resultado|Resultado")
                Dim r = _servicio.Resumen(i.Id)
                _resumen.Text = $"{r.Lineas} lineas, {r.SinContar} sin contar, {r.ConDiferencia} con diferencia" &
                                If(_ciego.Checked, "", $"; faltante {Ui.Dinero(r.FaltanteValorU6)}, sobrante {Ui.Dinero(r.SobranteValorU6)}")
            End Sub)
    End Sub

    Private Sub Abrir(tipo As String)
        If AlmacenId = 0 Then Return
        Using d As New DialogoCampos(If(tipo = "general", "Inventario general", "Inventario rotativo"))
            d.Fecha("corte", "Fecha de corte", Date.Today)
            If tipo = "rotativo" Then d.Texto("buscar", "Productos a contar (codigo o parte del nombre)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me,
                Sub()
                    Dim variantes As IEnumerable(Of Long) = Nothing
                    If tipo = "rotativo" Then
                        variantes = _catalogo.BuscarProductos(d.Valor("buscar")).Take(100).SelectMany(Function(p) _catalogo.ListarVariantes(p.Id)).Select(Function(v) v.Id).ToList()
                        If Not variantes.Any() Then Ui.Informar(Me, "No se encontraron productos.") : Return
                    End If
                    _servicio.Abrir(AlmacenId, d.FechaElegida("corte").Value, tipo, variantes)
                End Sub)
        End Using
        CargarInventarios()
    End Sub

    Private Sub ContarLinea()
        Dim l = Ui.Seleccionado(Of LineaConteoDto)(_hoja)
        If l Is Nothing Then Ui.Informar(Me, "Seleccione una linea de la hoja.") : Return
        Using d As New DialogoCampos("Conteo de " & l.Descripcion)
            d.Texto("envases", $"Envases completos ({l.Presentacion} de {Ui.Cantidad(l.ContenidoEnvaseU6)} {l.Unidad})").Texto("parcial", $"Parcial suelto ({l.Unidad})") _
             .Marca("pendiente", "Dejar sin contar (pendiente)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub()
                                If d.Marcado("pendiente") Then
                                    _servicio.RegistrarConteo(l.Id, Nothing, Nothing)
                                Else
                                    _servicio.RegistrarConteo(l.Id, If(d.Valor("envases") = "", 0L, Ui.LeerU6(d.Valor("envases"), "envases")),
                                                              If(d.Valor("parcial") = "", 0L, Ui.LeerU6(d.Valor("parcial"), "parcial")))
                                End If
                            End Sub)
        End Using
        CargarHoja()
    End Sub

    Private Sub Importar()
        Dim i = Inventario
        If i Is Nothing Then Return
        Using a As New OpenFileDialog With {.Filter = "Conteo (*.csv)|*.csv"}
            If a.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() Ui.Informar(Me, $"{_servicio.ImportarConteo(i.Id, File.ReadAllText(a.FileName, Encoding.UTF8))} lineas contadas."))
        End Using
        CargarHoja()
    End Sub

    Private Sub Accion(accion As Action(Of Long))
        Dim i = Inventario
        If i Is Nothing Then Return
        Ui.Ejecutar(Me, Sub() accion(i.Id))
        CargarInventarios()
    End Sub

    Private Sub Autorizar()
        Dim i = Inventario
        If i Is Nothing Then Return
        Using d As New DialogoCampos("Autorizar ajuste de " & i.Numero)
            d.Fecha("fecha", "Fecha del ajuste", Date.Today).Texto("motivo", "Motivo")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub()
                                Dim r = _servicio.AutorizarAjuste(i.Id, d.FechaElegida("fecha").Value, d.Valor("motivo"))
                                Ui.Informar(Me, $"Ajuste aplicado: faltante {Ui.Dinero(r.FaltanteValorU6)}, sobrante {Ui.Dinero(r.SobranteValorU6)}.")
                            End Sub)
        End Using
        CargarInventarios()
    End Sub
End Class
