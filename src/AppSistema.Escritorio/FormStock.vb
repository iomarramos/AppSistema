Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Stock por variante del almacén (cantidad base, valor y costo promedio) y movimientos: inventario inicial,
''' recepción de pedidos, salida a cocina, baja, traspaso, devolución, kárdex y documentos.
''' </summary>
Partial Public Class FormStock

    Private ReadOnly _stock As ServicioStock
    Private ReadOnly _apertura As ServicioInventarioInicial
    Private ReadOnly _almacenes As ServicioAlmacen
    Private ReadOnly _compras As ServicioCompras
    Private ReadOnly _reportes As ServicioReportes
    Private ReadOnly _listaAlmacenes As New List(Of AlmacenResumen)
    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridSaldos)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridSaldos)
        _stock = New ServicioStock(cadena, sesion)
        _apertura = New ServicioInventarioInicial(cadena, sesion)
        _almacenes = New ServicioAlmacen(cadena, sesion)
        _compras = New ServicioCompras(cadena, sesion)
        _reportes = New ServicioReportes(cadena, sesion)
        Dim admin As New ServicioAdministracion(cadena, sesion)
        Text = "Stock - " & sesion.Operacion.Nombre
        Dim mueve = sesion.Tiene(Permisos.StockContabilizar)
        barraAcciones.Controls.Add(Ui.Boton("Recibir pedido...", AddressOf RecibirPedido))
        barraAcciones.Controls.Add(Ui.Boton("Salida a cocina...", Sub() Salida(False)))
        barraAcciones.Controls.Add(Ui.Boton("Baja...", Sub() Salida(True)))
        barraAcciones.Controls.Add(Ui.Boton("Traspaso...", AddressOf Traspaso))
        barraAcciones.Controls.Add(Ui.Boton("Recibir traspaso...", AddressOf RecibirTraspaso))
        barraAcciones.Controls.Add(Ui.Boton("Devolucion de cocina...", AddressOf Devolucion))
        barraAcciones.Controls.Add(Ui.BotonSi(mueve, "Atender devoluciones pedidas por cocina...", AddressOf AtenderDevoluciones))
        barraAcciones.Controls.Add(Ui.Boton("Inventario inicial...", AddressOf InventarioInicial))
        barraAcciones.Visible = mueve
        barraFiltros.Controls.Add(Ui.Boton("Ver", AddressOf Cargar))
        barraFiltros.Controls.Add(Ui.Boton("Kardex...", AddressOf Kardex))
        barraFiltros.Controls.Add(Ui.Boton("Documentos...", AddressOf Documentos))
        barraFiltros.Controls.Add(Ui.Boton("Imprimir stock...", AddressOf ImprimirStock))
        barraFiltros.Controls.Add(Ui.Boton("Registro SUNAT 13.1...", AddressOf RegistroSunat))
        AddHandler cmbAlmacen.SelectedIndexChanged, Sub() Cargar()
        AddHandler txtBuscar.KeyDown, Sub(s, e) If e.KeyCode = Keys.Enter Then Cargar()
        AddHandler Load, Sub() Ui.Ejecutar(Me,
                                    Sub()
                                        _listaAlmacenes.AddRange(admin.ListarAlmacenes())
                                        For Each a In _listaAlmacenes
                                            cmbAlmacen.Items.Add(New Opcion(Of AlmacenResumen)(a, $"{a.Codigo} - {a.Nombre}"))
                                        Next
                                        If cmbAlmacen.Items.Count > 0 Then cmbAlmacen.SelectedIndex = 0
                                    End Sub)
    End Sub

    Private ReadOnly Property Almacen As AlmacenResumen
        Get
            Return TryCast(cmbAlmacen.SelectedItem, Opcion(Of AlmacenResumen))?.Valor
        End Get
    End Property

    Private ReadOnly Property Saldo As SaldoStockDto
        Get
            Return Ui.Seleccionado(Of SaldoStockDto)(gridSaldos)
        End Get
    End Property

    Private Sub Cargar()
        If Almacen Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Dim saldos = _stock.ConsultarSaldos(Almacen.Id, txtBuscar.Text)
                Ui.Mostrar(gridSaldos, saldos, "ProductoDescripcion|Producto", "VarianteDescripcion|Presentacion comercial", "CantidadBaseU6|Cantidad",
                           "Unidad|Unidad", "ValorU6|Valor", "CostoPromedioU6|Costo promedio")
                lblTotal.Text = $"{saldos.Count} variantes con stock; valor total {Ui.Dinero(saldos.Sum(Function(x) x.ValorU6))}"
            End Sub)
    End Sub

    ' ---------- Movimientos ----------

    Private Sub RecibirPedido()
        If Almacen Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Dim pedidos = _compras.ListarPedidos(Almacen.Id).Where(Function(p) p.Estado = "aprobado" OrElse p.Estado = "enviado" OrElse p.Estado = "parcial").ToList()
                If pedidos.Count = 0 Then Ui.Informar(Me, "No hay pedidos aprobados pendientes de recibir.") : Return
                Dim pedido As PedidoDto
                Dim tipo, numero As String
                Dim fechaDoc, fechaRec As Date
                Using d As New DialogoCampos("Recibir pedido")
                    d.Opciones("pedido", "Pedido", pedidos.Select(Function(p) CObj(New Opcion(Of PedidoDto)(p, $"{p.Numero} - {p.ProveedorNombre} ({p.Estado})")))) _
                     .Opciones("tipo", "Comprobante", {"FACTURA", "GUIA", "BOLETA"}).Texto("numero", "Numero de comprobante") _
                     .Fecha("fdoc", "Fecha del comprobante", Date.Today).Fecha("frec", "Fecha de recepcion", Date.Today)
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    pedido = d.Elegido(Of Opcion(Of PedidoDto))("pedido").Valor
                    tipo = CStr(d.Elegido(Of Object)("tipo")) : numero = d.Valor("numero")
                    fechaDoc = d.FechaElegida("fdoc").Value : fechaRec = d.FechaElegida("frec").Value
                End Using
                Dim pendientes = _almacenes.PendientesDePedido(pedido.Id).Where(Function(l) l.PendienteBaseU6 > 0).ToList()
                Using d As New DialogoCampos("Cantidades recibidas (en empaques)")
                    For Each l In pendientes
                        Dim pendEmpaques = EscalaU6.MultiplicarDividir(l.PendienteBaseU6, EscalaU6.Factor, l.FactorBasePorEmpaqueU6)
                        d.Texto("c" & l.PedidoDetalleId, $"{l.ProductoDescripcion} - {l.EmpaqueDescripcion} (pendiente {Ui.Cantidad(pendEmpaques)})", Ui.Cantidad(pendEmpaques)) _
                         .Texto("p" & l.PedidoDetalleId, "   precio por empaque", Ui.Cantidad(l.PrecioEmpaqueU6))
                    Next
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    Dim lineas = pendientes.Select(Function(l) New LineaRecepcion With {
                        .PedidoDetalleId = l.PedidoDetalleId, .EmpaquesU6 = If(d.Valor("c" & l.PedidoDetalleId) = "", 0, Ui.LeerU6(d.Valor("c" & l.PedidoDetalleId), "cantidad")),
                        .PrecioEmpaqueU6 = Ui.LeerU6(d.Valor("p" & l.PedidoDetalleId), "precio")}).ToList()
                    _almacenes.RecibirPedido(pedido.Id, tipo, numero, fechaDoc, fechaRec, lineas)
                    Ui.Informar(Me, "Recepcion registrada.")
                End Using
            End Sub)
        Cargar()
    End Sub

    Private Sub Salida(esBaja As Boolean)
        Dim s = Saldo
        If s Is Nothing OrElse Almacen Is Nothing Then Ui.Informar(Me, "Seleccione un producto con stock.") : Return
        Using d As New DialogoCampos(If(esBaja, "Baja de ", "Salida a cocina de ") & s.VarianteDescripcion)
            d.Fecha("fecha", "Fecha", Date.Today).Texto("cantidad", $"Cantidad ({s.Unidad}; hay {Ui.Cantidad(s.CantidadBaseU6)})").Texto("motivo", If(esBaja, "Motivo", "Observacion (opcional)"))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim linea = {New LineaSalida With {.VarianteId = s.VarianteId, .CantidadBaseU6 = Ui.LeerU6(d.Valor("cantidad"), "cantidad")}}
            Ui.Ejecutar(Me, Sub()
                                If esBaja Then
                                    _almacenes.Baja(Almacen.Id, d.FechaElegida("fecha").Value, linea, d.Valor("motivo"))
                                Else
                                    _almacenes.SalidaProduccion(Almacen.Id, d.FechaElegida("fecha").Value, linea, d.Valor("motivo"))
                                End If
                            End Sub)
        End Using
        Cargar()
    End Sub

    Private Sub Traspaso()
        Dim s = Saldo
        If s Is Nothing OrElse Almacen Is Nothing Then Ui.Informar(Me, "Seleccione un producto con stock.") : Return
        Dim destinos = _listaAlmacenes.Where(Function(a) a.Id <> Almacen.Id).ToList()
        If destinos.Count = 0 Then Ui.Informar(Me, "La operacion no tiene otro almacen.") : Return
        Using d As New DialogoCampos("Traspaso de " & s.VarianteDescripcion)
            d.Opciones("destino", "Almacen destino", destinos.Select(Function(a) CObj(New Opcion(Of AlmacenResumen)(a, $"{a.Codigo} - {a.Nombre}")))) _
             .Fecha("fecha", "Fecha", Date.Today).Texto("cantidad", $"Cantidad ({s.Unidad})")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            If Ui.Ejecutar(Me, Sub() _almacenes.Traspasar(Almacen.Id, d.Elegido(Of Opcion(Of AlmacenResumen))("destino").Valor.Id, d.FechaElegida("fecha").Value,
                                                       {New LineaSalida With {.VarianteId = s.VarianteId, .CantidadBaseU6 = Ui.LeerU6(d.Valor("cantidad"), "cantidad")}})) Then Ui.Informar(Me, "Traspaso enviado. Queda en transito hasta que el almacen destino lo reciba (boton Recibir traspaso).")
        End Using
        Cargar()
    End Sub

    ''' <summary>Recibe en este almacén un traspaso que venía en tránsito: entra el mismo stock y valor que salió del origen.</summary>
    Private Sub RecibirTraspaso()
        If Almacen Is Nothing Then Return
        Dim enviados As List(Of TraspasoTransitoDto) = Nothing
        If Not Ui.Ejecutar(Me, Sub() enviados = _almacenes.ListarTransitos(True).Where(Function(t) t.AlmacenDestinoId = Almacen.Id).ToList()) Then Return
        If enviados.Count = 0 Then Ui.Informar(Me, "No hay traspasos en transito para este almacen.") : Return
        Using d As New DialogoCampos("Recibir traspaso en " & Almacen.Nombre)
            d.Opciones("t", "Traspaso enviado", enviados.Select(Function(t) CObj(New Opcion(Of TraspasoTransitoDto)(t,
                $"{t.Numero} desde {t.AlmacenOrigen}, enviado el {t.FechaEnvio:dd/MM/yyyy} ({Ui.Dinero(t.ValorU6)})")))) _
             .Fecha("fecha", "Fecha de recepcion", Date.Today)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim elegido = d.Elegido(Of Opcion(Of TraspasoTransitoDto))("t").Valor
            Dim fecha = d.FechaElegida("fecha").Value
            If Not Ui.Ejecutar(Me, Sub() _almacenes.Recibir(elegido.Id, fecha)) Then Return
            Ui.Informar(Me, $"Traspaso {elegido.Numero} recibido. El stock ya esta en este almacen.")
        End Using
        Cargar()
    End Sub

    Private Sub Devolucion()
        If Almacen Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Dim salidas = _almacenes.ListarDocumentos(Almacen.Id, Date.Today.AddDays(-31), Date.Today).Where(Function(x) x.Tipo = "salida_produccion").ToList()
                If salidas.Count = 0 Then Ui.Informar(Me, "No hay salidas a cocina en los ultimos 31 dias.") : Return
                Dim s = Saldo
                If s Is Nothing Then Ui.Informar(Me, "Seleccione en la grilla el producto devuelto.") : Return
                Using d As New DialogoCampos("Devolucion de cocina")
                    d.Opciones("salida", "Entrega que se devuelve", salidas.Select(Function(x) CObj(New Opcion(Of DocumentoStockDto)(x, $"{x.Numero} del {x.Fecha:d}")))) _
                     .Texto("cantidad", $"Cantidad devuelta de {s.VarianteDescripcion} ({s.Unidad})").Fecha("fecha", "Fecha", Date.Today)
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    _almacenes.DevolucionProduccion(d.Elegido(Of Opcion(Of DocumentoStockDto))("salida").Valor.Id, d.FechaElegida("fecha").Value,
                                                    {New LineaSalida With {.VarianteId = s.VarianteId, .CantidadBaseU6 = Ui.LeerU6(d.Valor("cantidad"), "cantidad")}})
                End Using
            End Sub)
        Cargar()
    End Sub

    ''' <summary>Atiende lo que cocina pidió devolver: sale del stock recién cuando el almacén lo confirma.</summary>
    Private Sub AtenderDevoluciones()
        If Almacen Is Nothing Then Return
        Ui.Ejecutar(Me,
            Sub()
                Dim pendientes = _almacenes.ListarDevolucionesPendientes(Almacen.Id)
                If pendientes.Count = 0 Then Ui.Informar(Me, "No hay devoluciones de cocina pendientes en este almacen.") : Return
                Using d As New DialogoCampos("Atender devolucion de cocina")
                    d.Opciones("solicitud", "Solicitud", pendientes.Select(Function(x) CObj(New Opcion(Of SolicitudDevolucionDto)(x,
                        $"{x.Producto} - {EscalaU6.ADecimal(x.CantidadU6)} de la entrega {x.NumeroEntrega} ({x.Motivo})")))) _
                     .Fecha("fecha", "Fecha de la devolucion", Date.Today)
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    Dim s = d.Elegido(Of Opcion(Of SolicitudDevolucionDto))("solicitud").Valor
                    _almacenes.AtenderDevolucion(s.Id, d.FechaElegida("fecha").Value)
                End Using
            End Sub)
        Cargar()
    End Sub

    ' ---------- Consultas ----------

    Private Sub Kardex()
        Dim s = Saldo
        If s Is Nothing OrElse Almacen Is Nothing Then Ui.Informar(Me, "Seleccione un producto.") : Return
        Using d As New DialogoCampos("Kardex de " & s.VarianteDescripcion)
            d.Fecha("desde", "Desde", Date.Today.AddDays(-Date.Today.Day + 1)).Fecha("hasta", "Hasta", Date.Today)
            d.Marca("imprimir", "Imprimir o exportar", False)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim almacenId = Almacen.Id, desde = d.FechaElegida("desde").Value, hasta = d.FechaElegida("hasta").Value
            If d.Marcado("imprimir") Then
                SalidaReporte.Emitir(Me, Function() _reportes.Kardex(almacenId, s.VarianteId, desde, hasta))
                Return
            End If
            Ui.Ejecutar(Me, Sub() Ui.MostrarLista(Me, "Kardex", $"{s.VarianteDescripcion} ({s.Unidad}) en {Almacen.Nombre}",
                                                   _almacenes.Kardex(Almacen.Id, s.VarianteId, d.FechaElegida("desde").Value, d.FechaElegida("hasta").Value),
                                                   "Fecha|Fecha", "Documento|Documento", "Tipo|Tipo", "EntradaU6|Entrada", "SalidaU6|Salida",
                                                   "ValorMovimientoU6|Valor", "SaldoCantidadU6|Saldo", "SaldoValorU6|Valor del saldo", "CostoPromedioU6|Costo promedio"))
        End Using
    End Sub

    Private Sub ImprimirStock()
        If Almacen Is Nothing Then Return
        Dim almacenId = Almacen.Id
        SalidaReporte.Emitir(Me, Function() _reportes.StockValorizado(almacenId))
    End Sub

    Private Sub RegistroSunat()
        If Almacen Is Nothing Then Return
        Dim almacenId = Almacen.Id
        Dim inicio = New Date(Date.Today.Year, Date.Today.Month, 1)
        Using d As New DialogoCampos("Registro de inventario permanente valorizado (formato 13.1)")
            d.Fecha("desde", "Desde", inicio).Fecha("hasta", "Hasta", inicio.AddMonths(1).AddDays(-1))
            d.Opciones("tipo", "Tipo de existencia (tabla 5)", {"03", "01", "02", "04", "05", "99"}.Select(Function(c) CObj(New Opcion(Of String)(c, AppSistema.Dominio.Sunat.TablasSunat.TipoExistencia(c)))),
                       Nothing)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim desde = d.FechaElegida("desde").Value, hasta = d.FechaElegida("hasta").Value
            Dim tipo = If(d.Elegido(Of Opcion(Of String))("tipo")?.Valor, "03")
            SalidaReporte.Emitir(Me, Function() _reportes.RegistroInventarioPermanente(almacenId, desde, hasta, tipo))
        End Using
    End Sub

    Private Sub Documentos()
        If Almacen Is Nothing Then Return
        Using d As New DialogoCampos("Documentos de " & Almacen.Nombre)
            d.Fecha("desde", "Desde", Date.Today.AddDays(-Date.Today.Day + 1)).Fecha("hasta", "Hasta", Date.Today)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() Ui.MostrarLista(Me, "Documentos de almacen", Almacen.Nombre,
                                                   _almacenes.ListarDocumentos(Almacen.Id, d.FechaElegida("desde").Value, d.FechaElegida("hasta").Value),
                                                   "Fecha|Fecha", "Numero|Numero", "Tipo|Tipo", "Motivo|Motivo", "ValorU6|Valor"))
        End Using
    End Sub

    Private Sub InventarioInicial()
        If Almacen Is Nothing Then Return
        Dim texto As String = Nothing
        Using a As New OpenFileDialog With {.Filter = "Inventario inicial (*.csv)|*.csv"}
            If a.ShowDialog(Me) <> DialogResult.OK Then Return
            texto = File.ReadAllText(a.FileName, Encoding.UTF8)
        End Using
        Using d As New DialogoCampos("Inventario inicial de " & Almacen.Nombre)
            d.Fecha("fecha", "Fecha del inventario", Date.Today)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me,
                Sub()
                    Dim vista = _apertura.VistaPrevia(Almacen.Id, texto)
                    Ui.MostrarLista(Me, "Vista previa del inventario inicial", vista.Resumen, vista.Lineas,
                                    "Linea|Fila", "VarianteCodigo|Codigo", "Descripcion|Producto", "Presentacion|Presentacion", "StockEnvasesU6|Envases",
                                    "PrecioEnvaseU6|Precio envase", "CantidadBaseU6|Cantidad base", "Unidad|Unidad", "ValorU6|Valor", "Problema|Problema")
                    If vista.HayErrores Then Ui.Informar(Me, "No se puede registrar: " & vista.Resumen) : Return
                    If Not Ui.Confirmar(Me, "Registrar la apertura? " & vista.Resumen) Then Return
                    Dim r = _apertura.Aplicar(Almacen.Id, d.FechaElegida("fecha").Value, texto)
                    Ui.Informar(Me, "Inventario inicial registrado. " & r.Resumen)
                End Sub)
        End Using
        Cargar()
    End Sub
End Class
