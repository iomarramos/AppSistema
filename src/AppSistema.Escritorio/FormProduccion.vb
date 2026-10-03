Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Producción por minuta: requerimiento calculado y adicionales, entrega del almacén en presentaciones completas,
''' raciones producidas/servidas, mermas y comparación previsto/real del servicio.
''' </summary>
Public Class FormProduccion
    Inherits Form

    Private ReadOnly _produccion As ServicioProduccion
    Private ReadOnly _minutas As ServicioMinutas
    Private ReadOnly _catalogo As ServicioCatalogo
    Private ReadOnly _almacenes As New List(Of AlmacenResumen)
    Private ReadOnly _fecha As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 110}
    Private ReadOnly _gMinutas As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _gRequerimientos As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _gLineas As DataGridView = Ui.NuevaGrilla()

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _produccion = New ServicioProduccion(cadena, sesion)
        _minutas = New ServicioMinutas(cadena, sesion)
        _catalogo = New ServicioCatalogo(cadena, sesion)
        Dim admin As New ServicioAdministracion(cadena, sesion)
        Text = "Produccion - " & sesion.Operacion.Nombre
        Dim cocina = sesion.Tiene(Permisos.ProduccionEditar)
        Dim barra = Ui.BarraBotones(New Label With {.Text = "Fecha", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _fecha,
                                    Ui.Boton("Ver", AddressOf CargarMinutas), Ui.Boton("Calcular requerimiento", AddressOf Calcular),
                                    Ui.Boton("Requerimiento adicional...", AddressOf Adicional), Ui.Boton("Cambiar cantidad...", AddressOf CambiarCantidad),
                                    Ui.Boton("Entregar (almacen)", AddressOf Atender), Ui.Boton("Registrar produccion...", AddressOf RegistrarProduccion),
                                    Ui.Boton("Merma...", AddressOf Merma), Ui.Boton("Previsto vs real", AddressOf Reporte))
        For Each i In {3, 4, 5, 7, 8}
            barra.Controls(i).Visible = cocina
        Next
        barra.Controls(6).Visible = sesion.Tiene(Permisos.StockContabilizar)

        Dim abajo As New SplitContainer With {.Dock = DockStyle.Fill}
        abajo.Panel1.Controls.Add(_gRequerimientos)
        abajo.Panel2.Controls.Add(_gLineas)
        Dim division As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal}
        division.Panel1.Controls.Add(_gMinutas)
        division.Panel2.Controls.Add(abajo)
        Controls.Add(division)
        Controls.Add(New Label With {.Dock = DockStyle.Top, .Height = 34, .Padding = New Padding(4),
            .Text = "El almacen entrega presentaciones completas y lo entregado se da por consumido (D12). El costo real es del servicio: entregas menos devoluciones."})
        Controls.Add(barra)

        AddHandler _gMinutas.SelectionChanged, Sub() CargarRequerimientos()
        AddHandler _gRequerimientos.SelectionChanged, Sub() CargarLineas()
        AddHandler Load, Sub() Ui.Ejecutar(Me, Sub()
                                                   _almacenes.AddRange(admin.ListarAlmacenes())
                                                   CargarMinutas()
                                               End Sub)
    End Sub

    Private ReadOnly Property Minuta As MinutaDto
        Get
            Return Ui.Seleccionado(Of MinutaDto)(_gMinutas)
        End Get
    End Property

    Private ReadOnly Property Requerimiento As RequerimientoDto
        Get
            Return Ui.Seleccionado(Of RequerimientoDto)(_gRequerimientos)
        End Get
    End Property

    Private Sub CargarMinutas()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_gMinutas, _minutas.ListarMinutas(_fecha.Value, _fecha.Value), "Fecha|Fecha", "ServicioNombre|Servicio",
                                         "RegimenNombre|Regimen", "Comensales|Comensales", "Estado|Estado"))
    End Sub

    Private Sub CargarRequerimientos()
        Dim m = Minuta
        If m Is Nothing Then _gRequerimientos.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_gRequerimientos, _produccion.ListarRequerimientos(m.Id), "Numero|Requerimiento", "Tipo|Tipo", "Estado|Estado"))
    End Sub

    Private Sub CargarLineas()
        Dim r = Requerimiento
        If r Is Nothing Then _gLineas.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_gLineas, _produccion.ListarLineas(r.Id), "ProductoDescripcion|Producto", "PrevistoU6|Previsto",
                                         "SolicitadoU6|Solicitado", "Unidad|Unidad"))
    End Sub

    Private Function ElegirAlmacen() As AlmacenResumen
        If _almacenes.Count = 1 Then Return _almacenes(0)
        Using d As New DialogoCampos("Almacen que entrega")
            d.Opciones("a", "Almacen", _almacenes.Select(Function(a) CObj(New Opcion(Of AlmacenResumen)(a, $"{a.Codigo} - {a.Nombre}"))))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return Nothing
            Return d.Elegido(Of Opcion(Of AlmacenResumen))("a").Valor
        End Using
    End Function

    Private Sub Calcular()
        Dim m = Minuta
        If m Is Nothing Then Ui.Informar(Me, "Seleccione una minuta aprobada.") : Return
        Dim a = ElegirAlmacen()
        If a Is Nothing Then Return
        Ui.Ejecutar(Me, Sub() _produccion.CalcularRequerimiento(m.Id, a.Id))
        CargarRequerimientos()
    End Sub

    Private Sub Adicional()
        Dim m = Minuta
        If m Is Nothing Then Ui.Informar(Me, "Seleccione una minuta.") : Return
        Dim a = ElegirAlmacen()
        If a Is Nothing Then Return
        Ui.Ejecutar(Me, Sub() _produccion.RequerimientoAdicional(m.Id, a.Id))
        CargarRequerimientos()
    End Sub

    Private Sub CambiarCantidad()
        Dim r = Requerimiento
        If r Is Nothing OrElse r.Estado <> "borrador" Then Ui.Informar(Me, "Seleccione un requerimiento en borrador.") : Return
        Using d As New DialogoCampos("Producto a solicitar")
            d.Texto("buscar", "Producto (codigo o parte del nombre)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me,
                Sub()
                    Dim productos = _catalogo.BuscarProductos(d.Valor("buscar"))
                    If productos.Count = 0 Then Ui.Informar(Me, "No se encontro ningun producto.") : Return
                    Using d2 As New DialogoCampos("Cantidad solicitada")
                        d2.Opciones("p", "Producto", productos.Take(200).Select(Function(p) CObj(New Opcion(Of ProductoBaseDto)(p, $"{p.Codigo} - {p.Descripcion} ({p.UnidadCodigo})")))) _
                          .Texto("c", "Cantidad en unidad base (0 = no entregar)")
                        If d2.ShowDialog(Me) <> DialogResult.OK Then Return
                        _produccion.Solicitar(r.Id, d2.Elegido(Of Opcion(Of ProductoBaseDto))("p").Valor.Id, Ui.LeerU6(d2.Valor("c"), "cantidad"))
                    End Using
                End Sub)
        End Using
        CargarLineas()
    End Sub

    Private Sub Atender()
        Dim r = Requerimiento
        If r Is Nothing OrElse r.Estado <> "borrador" Then Ui.Informar(Me, "Seleccione un requerimiento en borrador.") : Return
        If Not Ui.Confirmar(Me, $"Entregar el requerimiento {r.Numero}? Saldran presentaciones completas del almacen.") Then Return
        Ui.Ejecutar(Me,
            Sub()
                Dim e = _produccion.Atender(r.Id, r.Fecha)
                Ui.MostrarLista(Me, "Entrega " & e.Numero, $"Valor entregado {Ui.Dinero(e.ValorU6)}", e.Lineas, "ProductoDescripcion|Producto", "SolicitadoU6|Solicitado",
                                "EntregadoU6|Entregado", "ExcedentePresentacionU6|Excedente de presentacion", "FaltanteU6|Faltante", "Unidad|Unidad",
                                "Presentaciones|Presentaciones", "ValorU6|Valor")
            End Sub)
        CargarRequerimientos()
    End Sub

    Private Sub RegistrarProduccion()
        Dim m = Minuta
        If m Is Nothing Then Return
        Using d As New DialogoCampos("Produccion del " & m.Fecha.ToShortDateString())
            d.Texto("p", "Raciones producidas", m.Comensales.ToString()).Texto("s", "Raciones servidas", m.Comensales.ToString()) _
             .Texto("x", "Raciones excedentes", "0").Texto("o", "Observacion (opcional)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _produccion.RegistrarProduccion(m.Id, Ui.LeerEntero(d.Valor("p"), "producidas"), Ui.LeerEntero(d.Valor("s"), "servidas"),
                                                                  Ui.LeerEntero(d.Valor("x"), "excedentes"), d.Valor("o")))
        End Using
    End Sub

    Private Sub Merma()
        Dim m = Minuta
        If m Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Dim prod = _produccion.ProduccionDeMinuta(m.Id)
                If Not prod.HasValue Then Ui.Informar(Me, "Registre primero la produccion de la minuta.") : Return
                Using d As New DialogoCampos("Merma")
                    d.Texto("buscar", "Producto (codigo o parte del nombre)")
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    Dim variantes = _catalogo.BuscarProductos(d.Valor("buscar")).Take(50).SelectMany(Function(p) _catalogo.ListarVariantes(p.Id).Select(Function(v) (P:=p, V:=v))).ToList()
                    If variantes.Count = 0 Then Ui.Informar(Me, "No se encontro ninguna presentacion.") : Return
                    Using d2 As New DialogoCampos("Merma de produccion")
                        d2.Opciones("v", "Presentacion", variantes.Select(Function(x) CObj(New Opcion(Of VarianteDto)(x.V, $"{x.V.DescripcionComercial} ({x.P.UnidadCodigo})")))) _
                          .Opciones("etapa", "Etapa", {"preparacion", "coccion", "servicio", "almacen"}).Texto("c", "Cantidad (unidad base)").Texto("motivo", "Motivo") _
                          .Marca("incluida", "Ya esta incluida en lo entregado a cocina (no genera otra baja)", True)
                        If d2.ShowDialog(Me) <> DialogResult.OK Then Return
                        If Not d2.Marcado("incluida") Then
                            Ui.Informar(Me, "Una merma que no esta en lo entregado se registra como Baja en Almacen > Stock.") : Return
                        End If
                        _produccion.RegistrarMerma(prod.Value, d2.Elegido(Of Opcion(Of VarianteDto))("v").Valor.Id, Nothing, CStr(d2.Elegido(Of Object)("etapa")),
                                                   Ui.LeerU6(d2.Valor("c"), "cantidad"), d2.Valor("motivo"), yaIncluidaEnConsumo:=True)
                        Ui.Informar(Me, "Merma registrada.")
                    End Using
                End Using
            End Sub)
    End Sub

    Private Sub Reporte()
        Dim m = Minuta
        If m Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Dim r = _produccion.Reporte(m.Id)
                Dim cab = $"Raciones previstas {r.RacionesPrevistas}, producidas {If(r.RacionesProducidas?.ToString(), "-")}, servidas {If(r.RacionesServidas?.ToString(), "-")}, " &
                          $"excedentes {If(r.RacionesExcedentes?.ToString(), "-")}. Costo previsto {If(r.CostoPrevistoU6.HasValue, Ui.Dinero(r.CostoPrevistoU6.Value), "pendiente")}, " &
                          $"costo real {Ui.Dinero(r.CostoRealU6)}" & If(r.CostoRealPorRacionServidaU6.HasValue, $" ({Ui.Dinero(r.CostoRealPorRacionServidaU6.Value)} por racion servida)", "") &
                          If(r.Mermas.Count > 0, ". Mermas: " & String.Join("; ", r.Mermas), "")
                Ui.MostrarLista(Me, "Previsto vs real", cab, r.Consumo, "ProductoDescripcion|Producto", "PrevistoU6|Previsto", "EntregadoU6|Entregado",
                                "DevueltoU6|Devuelto", "NetoU6|Neto", "DiferenciaU6|Diferencia", "Unidad|Unidad", "CostoRealU6|Costo real")
            End Sub)
    End Sub
End Class
