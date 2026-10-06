Imports AppSistema.Dominio.Inventario
Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Inventario físico: corte (general o rotativo), hoja de conteo por envases y parciales (ciega opcional),
''' importación del conteo, reconteo, diferencias, revisión y ajuste autorizado por alguien que no contó.
''' </summary>
Partial Public Class FormInventarios

    Private ReadOnly _servicio As ServicioInventarios
    Private ReadOnly _catalogo As ServicioCatalogo
    Private ReadOnly _reportes As ServicioReportes
    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridInventarios)
        Ui.Configurar(gridHoja)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridInventarios)
        Ui.Configurar(gridHoja)
        _servicio = New ServicioInventarios(cadena, sesion)
        _catalogo = New ServicioCatalogo(cadena, sesion)
        _reportes = New ServicioReportes(cadena, sesion)
        Dim admin As New ServicioAdministracion(cadena, sesion)
        Text = "Inventario fisico - " & sesion.Operacion.Nombre
        Dim aprueba = sesion.Tiene(Permisos.InventarioAprobar)
        Dim cuenta = sesion.Tiene(Permisos.InventarioContar)
        barraAcciones.Controls.Add(Ui.BotonSi(cuenta, "Nuevo general", Sub() Abrir("general")))
        barraAcciones.Controls.Add(Ui.BotonSi(cuenta, "Nuevo rotativo...", Sub() Abrir("rotativo")))
        barraAcciones.Controls.Add(Ui.BotonSi(cuenta, "Contar linea...", AddressOf ContarLinea))
        barraAcciones.Controls.Add(Ui.BotonSi(cuenta, "Importar conteo...", AddressOf Importar))
        barraAcciones.Controls.Add(Ui.BotonSi(cuenta, "Cerrar conteo", Sub() Accion(Sub(i) _servicio.CerrarConteo(i))))
        barraAcciones.Controls.Add(Ui.BotonSi(cuenta, "Recontar", Sub() Accion(Sub(i) _servicio.Recontar(i))))
        barraAcciones.Controls.Add(Ui.BotonSi(aprueba, "Revisar", Sub() Accion(Sub(i) _servicio.Revisar(i))))
        barraAcciones.Controls.Add(Ui.BotonSi(aprueba, "Autorizar ajuste...", AddressOf Autorizar))
        barraAcciones.Controls.Add(Ui.Boton("Imprimir hoja de conteo...", Sub() Imprimir(True)))
        barraAcciones.Controls.Add(Ui.Boton("Imprimir resultado...", Sub() Imprimir(False)))
        AddHandler cmbAlmacen.SelectedIndexChanged, Sub() CargarInventarios()
        AddHandler gridInventarios.SelectionChanged, Sub() CargarHoja()
        AddHandler chkCiego.CheckedChanged, Sub() CargarHoja()
        AddHandler Load, Sub() Ui.Ejecutar(Me, Sub()
                                                   For Each a In admin.ListarAlmacenes()
                                                       cmbAlmacen.Items.Add(New Opcion(Of AlmacenResumen)(a, $"{a.Codigo} - {a.Nombre}"))
                                                   Next
                                                   If cmbAlmacen.Items.Count > 0 Then cmbAlmacen.SelectedIndex = 0
                                               End Sub)
    End Sub

    Private ReadOnly Property AlmacenId As Long
        Get
            Dim o = TryCast(cmbAlmacen.SelectedItem, Opcion(Of AlmacenResumen))
            Return If(o Is Nothing, 0L, o.Valor.Id)
        End Get
    End Property

    Private ReadOnly Property Inventario As InventarioDto
        Get
            Return Ui.Seleccionado(Of InventarioDto)(gridInventarios)
        End Get
    End Property

    Private Sub CargarInventarios()
        If AlmacenId = 0 Then Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(gridInventarios, _servicio.Listar(AlmacenId), "Numero|Inventario", "Tipo|Tipo", "FechaCorte|Corte", "Estado|Estado"))
    End Sub

    Private Sub CargarHoja()
        Dim i = Inventario
        If i Is Nothing Then gridHoja.DataSource = Nothing : lblResumen.Text = "" : Return
        Ui.Ejecutar(Me,
            Sub()
                Ui.Mostrar(gridHoja, _servicio.Hoja(i.Id, chkCiego.Checked), "Descripcion|Producto", "Presentacion|Presentacion", "ContenidoEnvaseU6|Contenido por envase",
                           "Unidad|Unidad", "SistemaU6|Sistema al corte", "FisicoU6|Fisico", "DiferenciaU6|Diferencia", "ValorDiferenciaU6|Valor diferencia", "Resultado|Resultado")
                Dim r = _servicio.Resumen(i.Id)
                lblResumen.Text = $"{r.Lineas} lineas, {r.SinContar} sin contar, {r.ConDiferencia} con diferencia" &
                                If(chkCiego.Checked, "", $"; faltante {Ui.Dinero(r.FaltanteValorU6)}, sobrante {Ui.Dinero(r.SobranteValorU6)}")
            End Sub)
    End Sub

    Private Sub Abrir(tipo As String)
        If AlmacenId = 0 Then Return
        Using d As New DialogoCampos(If(tipo = "general", "Inventario general", "Inventario rotativo"))
            d.Fecha("corte", "Fecha de corte", Date.Today)
            If tipo = "rotativo" Then
                d.Opciones("seleccion", "Productos a contar", {
                    CObj(New Opcion(Of String)("texto", "Buscar por codigo o nombre")),
                    CObj(New Opcion(Of String)("A", "Clase A (80 % del consumo)")),
                    CObj(New Opcion(Of String)("B", "Clase B (15 % siguiente)")),
                    CObj(New Opcion(Of String)("C", "Clase C (resto y sin consumo)"))})
                d.Texto("buscar", "Texto a buscar (solo si elige buscar por codigo o nombre)")
            End If
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me,
                Sub()
                    Dim variantes As IEnumerable(Of Long) = Nothing
                    If tipo = "rotativo" Then
                        Dim seleccion = d.Elegido(Of Opcion(Of String))("seleccion").Valor
                        If seleccion = "texto" Then
                            variantes = _catalogo.BuscarProductos(d.Valor("buscar")).Take(100).SelectMany(Function(p) _catalogo.ListarVariantes(p.Id)).Select(Function(v) v.Id).ToList()
                        Else
                            variantes = _servicio.ClasificarAbc(AlmacenId, Date.Today.AddDays(-90), Date.Today).Where(Function(x) x.Clase = seleccion).Select(Function(x) x.VarianteId).ToList()
                        End If
                        If Not variantes.Any() Then Ui.Informar(Me, "No se encontraron productos para esa seleccion.") : Return
                    End If
                    _servicio.Abrir(AlmacenId, d.FechaElegida("corte").Value, tipo, variantes)
                End Sub)
        End Using
        CargarInventarios()
    End Sub

    Private Sub ContarLinea()
        Dim l = Ui.Seleccionado(Of LineaConteoDto)(gridHoja)
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
            d.Fecha("fecha", "Fecha del ajuste", Date.Today)
            d.Opciones("motivo", "Motivo del ajuste", MotivosAjuste.Todos.Select(Function(m) CObj(New Opcion(Of String)(m.Key, m.Value))))
            d.Texto("explicacion", "Explicacion (obligatoria)")
            d.Texto("soporte", "Documento de soporte (opcional)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub()
                                Dim r = _servicio.AutorizarAjusteNormalizado(i.Id, d.FechaElegida("fecha").Value,
                                                                             d.Elegido(Of Opcion(Of String))("motivo").Valor, d.Valor("explicacion"), d.Valor("soporte"))
                                Ui.Informar(Me, $"Ajuste aplicado: faltante {Ui.Dinero(r.FaltanteValorU6)}, sobrante {Ui.Dinero(r.SobranteValorU6)}.")
                            End Sub)
        End Using
        CargarInventarios()
    End Sub
    Private Sub Imprimir(hojaDeConteo As Boolean)
        Dim i = Inventario
        If i Is Nothing Then Ui.Informar(Me, "Seleccione un inventario.") : Return
        SalidaReporte.Emitir(Me, Function() _reportes.Inventario(i.Id, hojaDeConteo))
    End Sub

End Class
