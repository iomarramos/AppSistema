Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Producción por minuta: requerimiento calculado y adicionales, entrega del almacén en presentaciones completas,
''' raciones producidas/servidas, mermas, venta real, consumo por componente y comparación teórico vs real.
''' </summary>
Partial Public Class FormProduccion

    Private ReadOnly _produccion As ServicioProduccion
    Private ReadOnly _comparativo As ServicioComparativo
    Private ReadOnly _minutas As ServicioMinutas
    Private ReadOnly _catalogo As ServicioCatalogo
    Private ReadOnly _reportes As ServicioReportes
    Private ReadOnly _almacenes As New List(Of AlmacenResumen)

    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridMinutas)
        Ui.Configurar(gridRequerimientos)
        Ui.Configurar(gridLineas)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridMinutas)
        Ui.Configurar(gridRequerimientos)
        Ui.Configurar(gridLineas)
        _produccion = New ServicioProduccion(cadena, sesion)
        _comparativo = New ServicioComparativo(cadena, sesion)
        _minutas = New ServicioMinutas(cadena, sesion)
        _catalogo = New ServicioCatalogo(cadena, sesion)
        _reportes = New ServicioReportes(cadena, sesion)
        Dim admin As New ServicioAdministracion(cadena, sesion)
        Text = "Produccion - " & sesion.Operacion.Nombre
        Dim cocina = sesion.Tiene(Permisos.ProduccionEditar)
        barraFecha.Controls.Add(Ui.Boton("Ver", AddressOf CargarMinutas))
        barraFecha.Controls.Add(Ui.BotonSi(cocina, "Calcular requerimiento", AddressOf Calcular))
        barraFecha.Controls.Add(Ui.BotonSi(cocina, "Requerimiento adicional...", AddressOf Adicional))
        barraFecha.Controls.Add(Ui.BotonSi(cocina, "Cambiar cantidad...", AddressOf CambiarCantidad))
        barraFecha.Controls.Add(Ui.BotonSi(cocina, "Anular requerimiento", AddressOf AnularRequerimiento))
        barraFecha.Controls.Add(Ui.BotonSi(sesion.Tiene(Permisos.AdicionalAprobar), "Aprobar adicional", AddressOf AprobarAdicional))
        barraFecha.Controls.Add(Ui.BotonSi(sesion.Tiene(Permisos.StockContabilizar), "Entregar (almacen)", AddressOf Atender))
        barraFecha.Controls.Add(Ui.BotonSi(cocina, "Registrar produccion...", AddressOf RegistrarProduccion))
        barraFecha.Controls.Add(Ui.BotonSi(cocina, "Merma...", AddressOf Merma))
        barraFecha.Controls.Add(Ui.BotonSi(cocina, "Pedir devolucion a almacen...", AddressOf SolicitarDevolucion))
        barraFecha.Controls.Add(Ui.Boton("Imprimir requerimiento...", AddressOf ImprimirRequerimiento))
        barraReal.Controls.Add(Ui.BotonSi(cocina, "Venta real...", AddressOf VentaReal))
        barraReal.Controls.Add(Ui.BotonSi(cocina, "Consumo por componente...", AddressOf ConsumoComponente))
        barraReal.Controls.Add(Ui.Boton("Teorico vs real", Sub() Comparar(False)))
        barraReal.Controls.Add(Ui.Boton("Teorico vs real del mes", Sub() Comparar(True)))

        AddHandler gridMinutas.SelectionChanged, Sub() CargarRequerimientos()
        AddHandler gridRequerimientos.SelectionChanged, Sub() CargarLineas()
        AddHandler Load, Sub() Ui.Ejecutar(Me, Sub()
                                                   _almacenes.AddRange(admin.ListarAlmacenes())
                                                   CargarMinutas()
                                               End Sub)
    End Sub

    Private ReadOnly Property Minuta As MinutaDto
        Get
            Return Ui.Seleccionado(Of MinutaDto)(gridMinutas)
        End Get
    End Property

    Private ReadOnly Property Requerimiento As RequerimientoDto
        Get
            Return Ui.Seleccionado(Of RequerimientoDto)(gridRequerimientos)
        End Get
    End Property

    Private Sub ImprimirRequerimiento()
        Dim r = Requerimiento
        If r Is Nothing Then Ui.Informar(Me, "Seleccione un requerimiento.") : Return
        SalidaReporte.Emitir(Me, Function() _reportes.Requerimiento(r.Id))
    End Sub

    Private Sub CargarMinutas()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(gridMinutas, _minutas.ListarMinutas(dtFecha.Value, dtFecha.Value), "Fecha|Fecha", "ServicioNombre|Servicio",
                                         "RegimenNombre|Regimen", "Comensales|Comensales", "Estado|Estado",
                                         "VentaPrevistaU6|Venta teorica", "PrecioVentaComensalU6|Precio por comensal"))
    End Sub

    Private Sub CargarRequerimientos()
        Dim m = Minuta
        If m Is Nothing Then gridRequerimientos.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(gridRequerimientos, _produccion.ListarRequerimientos(m.Id), "Numero|Requerimiento", "Tipo|Tipo", "Estado|Estado"))
    End Sub

    Private Sub CargarLineas()
        Dim r = Requerimiento
        If r Is Nothing Then gridLineas.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(gridLineas, _produccion.ListarLineas(r.Id), "ProductoDescripcion|Producto", "PrevistoU6|Previsto",
                                         "SolicitadoU6|Solicitado", "Unidad|Unidad"))
    End Sub

    ''' <summary>Motivo obligatorio del adicional (Nothing si el usuario cancela o lo deja vacío).</summary>
    Private Function PedirMotivo() As String
        Using d As New DialogoCampos("Requerimiento adicional")
            d.Texto("motivo", "Motivo del adicional (obligatorio)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return Nothing
            If String.IsNullOrWhiteSpace(d.Valor("motivo")) Then
                Ui.Informar(Me, "El requerimiento adicional necesita un motivo.") : Return Nothing
            End If
            Return d.Valor("motivo").Trim()
        End Using
    End Function

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
        Dim motivo = PedirMotivo()
        If motivo Is Nothing Then Return
        Ui.Ejecutar(Me, Sub() _produccion.RequerimientoAdicional(m.Id, a.Id, motivo))
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

    Private Sub AprobarAdicional()
        Dim r = Requerimiento
        If r Is Nothing OrElse r.Tipo <> "adicional" OrElse r.Estado <> "borrador" Then Ui.Informar(Me, "Seleccione un requerimiento adicional en borrador.") : Return
        Ui.Ejecutar(Me, Sub() _produccion.AprobarAdicional(r.Id))
        CargarRequerimientos()
    End Sub

    ''' <summary>La cocina pide devolver parte de una entrega. No mueve stock: el almacén lo atiende.</summary>
    Private Sub SolicitarDevolucion()
        Dim m = Minuta
        If m Is Nothing Then Ui.Informar(Me, "Seleccione una minuta.") : Return
        Ui.Ejecutar(Me,
            Sub()
                Dim entregas = _produccion.ListarEntregasDevolubles(m.Id)
                If entregas.Count = 0 Then Ui.Informar(Me, "No hay entregas de esta minuta que se puedan devolver.") : Return
                Using d As New DialogoCampos("Pedir devolucion a almacen")
                    d.Opciones("entrega", "Producto entregado", entregas.Select(Function(x) CObj(New Opcion(Of EntregaDevolubleDto)(x,
                        $"{x.Producto} - entrega {x.Numero} (se puede devolver {EscalaU6.ADecimal(x.DisponibleU6)})")))) _
                     .Texto("cantidad", "Cantidad a devolver (unidad base)").Texto("motivo", "Motivo")
                    If d.ShowDialog(Me) <> DialogResult.OK Then Return
                    Dim e = d.Elegido(Of Opcion(Of EntregaDevolubleDto))("entrega").Valor
                    _produccion.SolicitarDevolucion(e.DocumentoId, e.VarianteId, Ui.LeerU6(d.Valor("cantidad"), "cantidad"), d.Valor("motivo"))
                    Ui.Informar(Me, "Solicitud enviada. El almacen la atiende y recien ahi sale el stock.")
                End Using
            End Sub)
    End Sub

    Private Sub Atender()
        Dim r = Requerimiento
        Dim listo = r IsNot Nothing AndAlso (r.Estado = "aprobado" OrElse (r.Estado = "borrador" AndAlso r.Tipo <> "adicional"))
        If Not listo Then Ui.Informar(Me, "Seleccione un requerimiento listo para entregar: calculado en borrador, o adicional ya aprobado.") : Return
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

    Private Sub AnularRequerimiento()
        Dim r = Requerimiento
        If r Is Nothing Then Ui.Informar(Me, "Seleccione un requerimiento.") : Return
        If Not Ui.Confirmar(Me, $"Anular el requerimiento {r.Numero}? Solo se anula si aun no fue entregado.") Then Return
        Ui.Ejecutar(Me, Sub() _produccion.Anular(r.Id))
        CargarRequerimientos()
    End Sub

    Private Sub VentaReal()
        Dim m = Minuta
        If m Is Nothing Then Ui.Informar(Me, "Seleccione una minuta.") : Return
        Using d As New DialogoCampos($"Venta real de {m.ServicioNombre} {m.Fecha:dd/MM/yyyy}")
            d.Texto("raciones", "Raciones vendidas", m.Comensales.ToString()) _
             .Texto("importe", "Importe vendido S/ (vacio = raciones x precio por comensal)") _
             .Texto("fuente", "Fuente (opcional: parte de comedor, liquidacion...)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _comparativo.RegistrarVenta(m.Id, Ui.LeerEntero(d.Valor("raciones"), "raciones"),
                                                               If(d.Valor("importe") = "", CType(Nothing, Long?), Ui.LeerU6(d.Valor("importe"), "importe")), d.Valor("fuente")))
        End Using
    End Sub

    Private Sub ConsumoComponente()
        Dim m = Minuta
        If m Is Nothing Then Ui.Informar(Me, "Seleccione una minuta.") : Return
        Dim platos As List(Of PlatoDto) = Nothing
        If Not Ui.Ejecutar(Me, Sub() platos = _minutas.ListarPlatos(m.Id)) Then Return
        Using d As New DialogoCampos("Consumo real por componente")
            d.Opciones("plato", "Componente", platos.Select(Function(p) CObj(New Opcion(Of PlatoDto)(p, $"{p.EstructuraNombre}: {p.RecetaNombre} ({p.Raciones} planificadas)")))) _
             .Texto("preparadas", "Raciones preparadas").Texto("consumidas", "Raciones consumidas")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _comparativo.RegistrarConsumo(d.Elegido(Of Opcion(Of PlatoDto))("plato").Valor.Id,
                                                                 Ui.LeerEntero(d.Valor("preparadas"), "preparadas"), Ui.LeerEntero(d.Valor("consumidas"), "consumidas")))
        End Using
    End Sub

    Private Sub Comparar(delMes As Boolean)
        Dim m = Minuta
        If m Is Nothing Then Ui.Informar(Me, "Seleccione una minuta.") : Return
        Dim c As ComparativoDto = Nothing
        If Not Ui.Ejecutar(Me, Sub() c = If(delMes, _comparativo.ComparativoMes(m.OperacionServicioId, m.Fecha.Year, m.Fecha.Month), _comparativo.Comparativo(m.Id))) Then Return
        ' Ventana no modal: la comparación queda abierta junto a la producción (como las demás consultas) y se libera al cerrarla.
        Dim f As New FormComparativo(c)
        Tema.Aplicar(f)
        AddHandler f.FormClosed, Sub() f.Dispose()
        f.Show(Me)
    End Sub
End Class
