Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Previsión de compras (desglose explicable, reserva, validación, obsolescencia) y pedidos de compra
''' (generados desde la previsión o manuales, aprobación y anulación).
''' </summary>
Public Class FormCompras
    Inherits Form

    Private ReadOnly _servicio As ServicioCompras
    Private ReadOnly _proveedores As ServicioProveedores
    Private ReadOnly _almacen As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 220}
    Private ReadOnly _corte As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 105}
    Private ReadOnly _desde As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 105}
    Private ReadOnly _hasta As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 105}
    Private ReadOnly _previsiones As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _desglose As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _pedidos As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _lineas As DataGridView = Ui.NuevaGrilla()

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _servicio = New ServicioCompras(cadena, sesion)
        _proveedores = New ServicioProveedores(cadena, sesion)
        Dim admin As New ServicioAdministracion(cadena, sesion)
        Text = "Compras - " & sesion.Operacion.Nombre
        _corte.Value = Date.Today
        _desde.Value = Date.Today.AddDays(1)
        _hasta.Value = Date.Today.AddDays(30)
        Dim edita = sesion.Tiene(Permisos.ComprasEditar)
        Dim aprueba = sesion.Tiene(Permisos.ComprasAprobar)

        ' --- Previsión ---
        Dim barraPrev = Ui.BarraBotones(Etiqueta("Corte"), _corte, Etiqueta("Desde"), _desde, Etiqueta("Hasta"), _hasta,
                                        Ui.Boton("Calcular", AddressOf Calcular), Ui.Boton("Validar", AddressOf Validar),
                                        Ui.Boton("Esta vigente?", AddressOf VerDiferencias), Ui.Boton("Generar pedido...", AddressOf GenerarPedido),
                                        Ui.Boton("Reserva del producto...", AddressOf FijarReserva))
        For Each i In {6, 7, 9, 10}
            barraPrev.Controls(i).Visible = edita
        Next
        Dim divPrev As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal, .SplitterDistance = 120}
        divPrev.Panel1.Controls.Add(_previsiones)
        divPrev.Panel2.Controls.Add(_desglose)
        divPrev.Panel2.Controls.Add(New Label With {.Dock = DockStyle.Top, .Height = 36, .Padding = New Padding(4),
            .Text = "Necesidad = max(0, mayor faltante por fecha, reserva - saldo final). Quiebre: primera fecha sin stock si no se compra (lo que llega despues no la cubre)."})
        Dim tabPrev As New TabPage("Prevision")
        tabPrev.Controls.Add(divPrev) : tabPrev.Controls.Add(barraPrev)

        ' --- Pedidos ---
        Dim barraPed = Ui.BarraBotones(Ui.Boton("Actualizar", AddressOf CargarPedidos), Ui.Boton("Pedido manual...", AddressOf PedidoManual),
                                       Ui.Boton("Agregar linea...", AddressOf AgregarLinea), Ui.Boton("Quitar linea", AddressOf QuitarLinea),
                                       Ui.Boton("Aprobar", AddressOf Aprobar), Ui.Boton("Anular", AddressOf Anular))
        For Each i In {1, 2, 3}
            barraPed.Controls(i).Visible = edita
        Next
        barraPed.Controls(4).Visible = aprueba : barraPed.Controls(5).Visible = aprueba
        Dim divPed As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal}
        divPed.Panel1.Controls.Add(_pedidos)
        divPed.Panel2.Controls.Add(_lineas)
        Dim tabPed As New TabPage("Pedidos")
        tabPed.Controls.Add(divPed) : tabPed.Controls.Add(barraPed)

        Dim tabs As New TabControl With {.Dock = DockStyle.Fill}
        tabs.TabPages.AddRange({tabPrev, tabPed})
        Controls.Add(tabs)
        Controls.Add(Ui.BarraBotones(Etiqueta("Almacen"), _almacen))

        AddHandler _almacen.SelectedIndexChanged, Sub()
                                                      CargarPrevisiones()
                                                      CargarPedidos()
                                                  End Sub
        AddHandler _previsiones.SelectionChanged, Sub() CargarDesglose()
        AddHandler _pedidos.SelectionChanged, Sub() CargarLineas()
        AddHandler Load, Sub() Ui.Ejecutar(Me,
                                    Sub()
                                        For Each a In admin.ListarAlmacenes()
                                            _almacen.Items.Add(New Opcion(Of AlmacenResumen)(a, $"{a.Codigo} - {a.Nombre}"))
                                        Next
                                        If _almacen.Items.Count > 0 Then _almacen.SelectedIndex = 0
                                    End Sub)
    End Sub

    Private Shared Function Etiqueta(texto As String) As Label
        Return New Label With {.Text = texto, .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}
    End Function

    Private ReadOnly Property AlmacenId As Long
        Get
            Dim o = TryCast(_almacen.SelectedItem, Opcion(Of AlmacenResumen))
            Return If(o Is Nothing, 0L, o.Valor.Id)
        End Get
    End Property

    Private ReadOnly Property PrevisionSel As PrevisionDto
        Get
            Return Ui.Seleccionado(Of PrevisionDto)(_previsiones)
        End Get
    End Property

    Private ReadOnly Property PedidoSel As PedidoDto
        Get
            Return Ui.Seleccionado(Of PedidoDto)(_pedidos)
        End Get
    End Property

    ' ---------- Previsión ----------

    Private Sub CargarPrevisiones()
        If AlmacenId = 0 Then Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_previsiones, _servicio.ListarPrevisiones(AlmacenId), "Id|N.", "FechaCorte|Corte", "FechaDesde|Desde",
                                         "FechaHasta|Hasta", "Estado|Estado", "FechaCalculo|Calculada"))
    End Sub

    Private Sub CargarDesglose()
        Dim p = PrevisionSel
        If p Is Nothing Then _desglose.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_desglose, _servicio.Desglose(p.Id), "ProductoCodigo|Codigo", "ProductoDescripcion|Producto", "Unidad|Unidad",
                                         "DemandaU6|Demanda", "ConsumoPuenteU6|Consumo puente", "StockU6|Stock", "ReservaU6|Reserva",
                                         "PendienteRecibirU6|Pendiente de recibir", "NecesidadNetaU6|Necesidad neta", "FechaQuiebre|Quiebre"))
    End Sub

    Private Sub Calcular()
        If AlmacenId = 0 Then Return
        Ui.Ejecutar(Me, Sub() _servicio.CalcularPrevision(AlmacenId, _corte.Value, _desde.Value, _hasta.Value))
        CargarPrevisiones()
    End Sub

    Private Sub Validar()
        Dim p = PrevisionSel
        If p Is Nothing Then Return
        Ui.Ejecutar(Me, Sub() _servicio.Validar(p.Id))
        CargarPrevisiones()
    End Sub

    Private Sub VerDiferencias()
        Dim p = PrevisionSel
        If p Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Dim d = _servicio.Diferencias(p.Id)
                Ui.Informar(Me, If(d.Count = 0, "La prevision sigue vigente.", "La prevision esta OBSOLETA. Cambios:" & Environment.NewLine & String.Join(Environment.NewLine, d.Take(30))))
            End Sub)
    End Sub

    Private Sub FijarReserva()
        Dim l = Ui.Seleccionado(Of PrevisionLineaDto)(_desglose)
        If l Is Nothing OrElse AlmacenId = 0 Then Ui.Informar(Me, "Seleccione un producto del desglose.") : Return
        Using d As New DialogoCampos("Reserva de " & l.ProductoDescripcion)
            d.Texto("reserva", $"Reserva al final del horizonte ({l.Unidad})", Ui.Cantidad(l.ReservaU6))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.FijarReserva(AlmacenId, l.ProductoBaseId, Ui.LeerU6(d.Valor("reserva"), "reserva")))
        End Using
        Ui.Informar(Me, "Reserva guardada. Recalcule la prevision para aplicarla.")
    End Sub

    Private Sub GenerarPedido()
        Dim p = PrevisionSel
        If p Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Using d As New DialogoCampos("Generar pedido desde la prevision " & p.Id)
                    d.Opciones("proveedor", "Proveedor", _proveedores.ListarProveedores().Select(Function(x) CObj(New Opcion(Of ProveedorDto)(x, x.Nombre)))) _
                     .Fecha("entrega", "Fecha de entrega", p.FechaDesde).Texto("moneda", "Moneda", "PEN")
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    Dim r = _servicio.GenerarPedido(p.Id, d.Elegido(Of Opcion(Of ProveedorDto))("proveedor").Valor.Id, d.FechaElegida("entrega").Value, d.Valor("moneda"))
                    Dim texto = $"Pedido {r.Numero} en borrador con {r.Lineas} lineas."
                    If r.SinPrecio.Count > 0 Then texto &= Environment.NewLine & "Sin precio vigente (a 0, revisar): " & String.Join(", ", r.SinPrecio.Take(10))
                    If r.SinEmpaqueDelProveedor.Count > 0 Then texto &= Environment.NewLine & $"El proveedor no ofrece {r.SinEmpaqueDelProveedor.Count} producto(s): " & String.Join(", ", r.SinEmpaqueDelProveedor.Take(10))
                    Ui.Informar(Me, texto)
                End Using
            End Sub)
        CargarPedidos()
    End Sub

    ' ---------- Pedidos ----------

    Private Sub CargarPedidos()
        If AlmacenId = 0 Then Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_pedidos, _servicio.ListarPedidos(AlmacenId), "Numero|Numero", "ProveedorNombre|Proveedor", "Tipo|Tipo",
                                         "Fecha|Fecha", "Estado|Estado", "Moneda|Moneda", "TotalU6|Total", "PrevisionId|Prevision"))
    End Sub

    Private Sub CargarLineas()
        Dim p = PedidoSel
        If p Is Nothing Then _lineas.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_lineas, _servicio.ListarLineas(p.Id), "ProductoDescripcion|Producto", "VarianteCodigo|Variante",
                                         "EmpaqueDescripcion|Empaque", "CantidadEmpaques|Empaques", "CantidadBaseU6|Cantidad", "Unidad|Unidad",
                                         "NecesidadU6|Necesidad", "ExcesoU6|Exceso por redondeo", "PrecioEmpaqueU6|Precio empaque",
                                         "ImporteU6|Importe", "FechaEntrega|Entrega", "PendienteU6|Pendiente"))
    End Sub

    Private Sub PedidoManual()
        If AlmacenId = 0 Then Return
        Ui.Ejecutar(Me,
            Sub()
                Using d As New DialogoCampos("Pedido manual")
                    d.Opciones("proveedor", "Proveedor", _proveedores.ListarProveedores().Select(Function(x) CObj(New Opcion(Of ProveedorDto)(x, x.Nombre)))) _
                     .Opciones("tipo", "Tipo", {"extra", "normal", "caja_chica"}).Texto("moneda", "Moneda", "PEN")
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    _servicio.CrearPedido(AlmacenId, d.Elegido(Of Opcion(Of ProveedorDto))("proveedor").Valor.Id, CStr(d.Elegido(Of Object)("tipo")), d.Valor("moneda"))
                End Using
            End Sub)
        CargarPedidos()
    End Sub

    Private Sub AgregarLinea()
        Dim p = PedidoSel
        If p Is Nothing OrElse p.Estado <> "borrador" Then Ui.Informar(Me, "Seleccione un pedido en borrador.") : Return
        Ui.Ejecutar(Me,
            Sub()
                Dim empaques = _proveedores.ListarEmpaquesDeProveedor(p.ProveedorId)
                If empaques.Count = 0 Then Ui.Informar(Me, "El proveedor no tiene empaques vinculados (Catalogo > Proveedores).") : Return
                Using d As New DialogoCampos("Agregar linea a " & p.Numero)
                    d.Opciones("empaque", "Empaque", empaques.Select(Function(x) CObj(New Opcion(Of ProveedorEmpaqueDto)(x, $"{x.VarianteCodigo} - {x.EmpaqueDescripcion}")))) _
                     .Texto("cantidad", "Cantidad de empaques", "1").Fecha("entrega", "Fecha de entrega", Date.Today.AddDays(1)).Texto("precio", "Precio por empaque", "0")
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    _servicio.AgregarLinea(p.Id, d.Elegido(Of Opcion(Of ProveedorEmpaqueDto))("empaque").Valor.EmpaqueId, Ui.LeerEntero(d.Valor("cantidad"), "cantidad"),
                                           d.FechaElegida("entrega").Value, Ui.LeerU6(d.Valor("precio"), "precio"))
                End Using
            End Sub)
        CargarLineas()
    End Sub

    Private Sub QuitarLinea()
        Dim l = Ui.Seleccionado(Of PedidoLineaDto)(_lineas)
        If l Is Nothing Then Return
        Ui.Ejecutar(Me, Sub() _servicio.QuitarLinea(l.Id))
        CargarLineas()
    End Sub

    Private Sub Aprobar()
        Dim p = PedidoSel
        If p Is Nothing OrElse Not Ui.Confirmar(Me, $"Aprobar el pedido {p.Numero}? Ya no podra modificarse.") Then Return
        Ui.Ejecutar(Me, Sub() _servicio.AprobarPedido(p.Id))
        CargarPedidos()
    End Sub

    Private Sub Anular()
        Dim p = PedidoSel
        If p Is Nothing OrElse Not Ui.Confirmar(Me, $"Anular el pedido {p.Numero}?") Then Return
        Ui.Ejecutar(Me, Sub() _servicio.AnularPedido(p.Id))
        CargarPedidos()
    End Sub
End Class
