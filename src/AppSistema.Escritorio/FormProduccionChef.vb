Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Plan operativo del chef: por cada servicio del rango, las raciones teóricas frente a los comensales, la producción
''' registrada y el último requerimiento. El chef cambia comensales en minutas editables (el servicio de minutas bloquea
''' lo aprobado). La producción y el requerimiento se operan en Produccion.
''' </summary>
Partial Public Class FormProduccionChef

    Private ReadOnly _servicio As ServicioMatrizMenu
    Private ReadOnly _minutas As ServicioMinutas
    Private ReadOnly _produccion As ServicioProduccion
    Private ReadOnly _recetas As ServicioRecetas
    Private ReadOnly _almacenes As List(Of AlmacenResumen)

    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridPlan)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridPlan)
        _servicio = New ServicioMatrizMenu(cadena, sesion)
        _minutas = New ServicioMinutas(cadena, sesion)
        _produccion = New ServicioProduccion(cadena, sesion)
        _recetas = New ServicioRecetas(cadena, sesion)
        _almacenes = New ServicioAdministracion(cadena, sesion).ListarAlmacenes()
        Text = "Plan operativo del chef - " & sesion.Operacion.Nombre
        dtDesde.Value = Date.Today
        dtHasta.Value = Date.Today.AddDays(6)
        barraAcciones.Controls.Add(Ui.Boton("Ver", AddressOf Cargar))
        barraAcciones.Controls.Add(Ui.BotonSi(sesion.Tiene(Permisos.MinutasEditar), "Actualizar comensales...", AddressOf ActualizarComensales))
        barraAcciones.Controls.Add(Ui.BotonSi(sesion.Tiene(Permisos.ProduccionEditar), "Pedir adicional...", AddressOf PedirAdicional))
        barraAcciones.Controls.Add(Ui.BotonSi(sesion.Tiene(Permisos.MinutasEditar), "Sustituir plato...", AddressOf SustituirPlato))
        barraAcciones.Controls.Add(Ui.BotonSi(sesion.Tiene(Permisos.ProduccionEditar), "Ajustar raciones operativas...", AddressOf AjustarRaciones))
        Cargar()
    End Sub

    Private Sub Cargar()
        Ui.Ejecutar(Me,
            Sub()
                Dim celdas = _servicio.Celdas(dtDesde.Value.Date, dtHasta.Value.Date)
                Ui.Mostrar(gridPlan, celdas.Select(Function(c) Presentar(c)).ToList(),
                           "Fecha|Dia", "Servicio|Servicio", "Regimen|Regimen", "Estado|Estado", "Comensales|Comensales",
                           "RacionesTeoricas|Raciones teoricas", "RacionesOperativas|Raciones operativas", "Diferencia|Diferencia (operativa - teorica)", "CostoPrevisto|Costo previsto (S/)",
                           "Produccion|Produccion registrada", "Requerimiento|Ultimo requerimiento")
                lblEstado.Text = $"{celdas.Count} {If(celdas.Count = 1, "servicio", "servicios")}. + = mas raciones que comensales; - = menos."
            End Sub)
    End Sub

    Private Shared Function Presentar(c As CeldaMatrizDto) As FilaPlan
        Dim diferencia = c.RacionesOperativas - c.RacionesTeoricas
        Return New FilaPlan With {
            .MinutaId = c.MinutaId,
            .Fecha = c.Fecha.ToString("ddd dd/MM/yyyy", Globalization.CultureInfo.CurrentCulture),
            .Servicio = c.ServicioNombre, .Regimen = c.RegimenNombre, .Estado = c.Estado,
            .Comensales = c.Comensales.ToString(),
            .RacionesTeoricas = c.RacionesTeoricas.ToString(),
            .RacionesOperativas = c.RacionesOperativas.ToString(),
            .Diferencia = diferencia.ToString("+0;-0;0"),
            .CostoPrevisto = If(c.CostoPrevistoU6.HasValue, Ui.Dinero(c.CostoPrevistoU6.Value), "-"),
            .Produccion = If(c.ProduccionRegistrada, "Si", "No"),
            .Requerimiento = If(String.IsNullOrEmpty(c.EstadoRequerimiento), "-", c.EstadoRequerimiento)}
    End Function

    ''' <summary>Cambia los comensales de la minuta seleccionada. El servicio rechaza lo que ya no se puede tocar.</summary>
    Private Sub ActualizarComensales()
        Dim fila = TryCast(gridPlan.CurrentRow?.DataBoundItem, FilaPlan)
        If fila Is Nothing Then Ui.Informar(Me, "Seleccione un servicio de la lista.") : Return
        Using d As New DialogoCampos("Comensales del servicio")
            d.Texto("comensales", $"Comensales de {fila.Servicio} ({fila.Fecha})")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim comensales As Long
            If Not Long.TryParse(d.Valor("comensales"), comensales) OrElse comensales < 0 Then
                Ui.Informar(Me, "Los comensales deben ser un numero entero mayor o igual que cero.") : Return
            End If
            Ui.Ejecutar(Me, Sub() _minutas.ActualizarComensales(fila.MinutaId, comensales))
        End Using
        Cargar()
    End Sub

    ''' <summary>
    ''' Pide un requerimiento adicional para el servicio elegido. Nace en borrador: el jefe de operación lo aprueba
    ''' antes de que el almacén lo entregue (regla de la base, V023).
    ''' </summary>
    Private Sub PedirAdicional()
        Dim fila = TryCast(gridPlan.CurrentRow?.DataBoundItem, FilaPlan)
        If fila Is Nothing Then Ui.Informar(Me, "Seleccione un servicio de la lista.") : Return
        If _almacenes.Count = 0 Then Ui.Informar(Me, "La operacion no tiene almacenes activos.") : Return
        Dim almacen = If(_almacenes.Count = 1, _almacenes(0), ElegirAlmacen())
        If almacen Is Nothing Then Return
        Dim motivo = PedirMotivo()
        If motivo Is Nothing Then Return
        Ui.Ejecutar(Me, Sub() _produccion.RequerimientoAdicional(fila.MinutaId, almacen.Id, motivo))
        Ui.Informar(Me, "Requerimiento adicional creado en borrador. Lo aprueba el jefe de operacion antes de la entrega.")
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
        Using d As New DialogoCampos("Almacen que entrega")
            d.Opciones("a", "Almacen", _almacenes.Select(Function(a) CObj(New Opcion(Of AlmacenResumen)(a, $"{a.Codigo} - {a.Nombre}"))))
            If d.ShowDialog(Me) <> DialogResult.OK Then Return Nothing
            Return d.Elegido(Of Opcion(Of AlmacenResumen))("a").Valor
        End Using
    End Function

    ''' <summary>
    ''' Sustituye la receta de un plato de la minuta seleccionada por una receta aprobada. Solo en borrador; la base y el
    ''' servicio de minutas lo vuelven a exigir.
    ''' </summary>
    Private Sub SustituirPlato()
        Dim fila = TryCast(gridPlan.CurrentRow?.DataBoundItem, FilaPlan)
        If fila Is Nothing Then Ui.Informar(Me, "Seleccione un servicio de la lista.") : Return
        If Not fila.Estado.Equals("borrador", StringComparison.OrdinalIgnoreCase) Then
            Ui.Informar(Me, $"Solo se sustituyen platos de minutas en borrador. Esta minuta esta {fila.Estado}.") : Return
        End If
        Dim platos = _minutas.ListarPlatos(fila.MinutaId)
        Dim plato = Elegir(Me, "Plato", platos, Function(p As PlatoDto) $"{p.EstructuraNombre}: {p.RecetaNombre} x {p.Raciones}")
        If plato Is Nothing Then Return
        Dim texto As String = Nothing
        Using d As New DialogoCampos("Receta que lo reemplaza")
            d.Texto("buscar", "Codigo o nombre de la receta")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            texto = d.Valor("buscar")
        End Using
        Dim aprobadas As New List(Of RecetaDto)
        Ui.Ejecutar(Me, Sub() aprobadas = _recetas.BuscarRecetas(texto).Where(Function(r) r.VersionAprobadaId.HasValue).ToList())
        Dim receta = Elegir(Me, "Receta aprobada", aprobadas, Function(r As RecetaDto) $"{r.Codigo} - {r.Nombre}")
        If receta Is Nothing Then Return
        Ui.Ejecutar(Me, Sub() _minutas.SustituirReceta(plato.Id, receta.VersionAprobadaId.Value))
        Cargar()
    End Sub

    ''' <summary>
    ''' Ajusta las raciones operativas de un plato del plan aprobado, con su motivo. El plan teórico no cambia; después del
    ''' despacho el servicio lo rechaza y el cambio se pide como adicional.
    ''' </summary>
    Private Sub AjustarRaciones()
        Dim fila = TryCast(gridPlan.CurrentRow?.DataBoundItem, FilaPlan)
        If fila Is Nothing Then Ui.Informar(Me, "Seleccione un servicio de la lista.") : Return
        Dim platos = _minutas.ListarPlatos(fila.MinutaId)
        Dim plato = Elegir(Me, "Plato", platos, Function(p As PlatoDto) $"{p.EstructuraNombre}: {p.RecetaNombre} (teoricas {p.Raciones})")
        If plato Is Nothing Then Return
        Using d As New DialogoCampos("Raciones operativas")
            d.Texto("raciones", $"Raciones operativas (teoricas: {plato.Raciones})").Texto("motivo", "Motivo (obligatorio)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim raciones As Long
            If Not Long.TryParse(d.Valor("raciones"), raciones) Then Ui.Informar(Me, "Las raciones deben ser un numero entero.") : Return
            Dim motivo = d.Valor("motivo")
            Ui.Ejecutar(Me, Sub() _minutas.AjustarRacionesOperativas(plato.Id, raciones, motivo))
        End Using
        Cargar()
    End Sub

    ''' <summary>Elige un elemento de una lista (Nothing si no hay ninguno o se cancela).</summary>
    Private Function Elegir(Of T As Class)(dueno As Form, titulo As String, items As IList(Of T), texto As Func(Of T, String)) As T
        If items.Count = 0 Then Ui.Informar(dueno, $"No hay {titulo.ToLowerInvariant()} disponible.") : Return Nothing
        Using d As New DialogoCampos($"Elegir {titulo.ToLowerInvariant()}")
            d.Opciones("e", titulo, items.Select(Function(i) CObj(New Opcion(Of T)(i, texto(i)))))
            If d.ShowDialog(dueno) <> DialogResult.OK Then Return Nothing
            Return d.Elegido(Of Opcion(Of T))("e").Valor
        End Using
    End Function

    ''' <summary>Fila de la grilla: el id de la minuta va oculto para poder cambiar sus comensales.</summary>
    Public NotInheritable Class FilaPlan
        Public Property MinutaId As Long
        Public Property Fecha As String
        Public Property Servicio As String
        Public Property Regimen As String
        Public Property Estado As String
        Public Property Comensales As String
        Public Property RacionesTeoricas As String
        Public Property RacionesOperativas As String
        Public Property Diferencia As String
        Public Property CostoPrevisto As String
        Public Property Produccion As String
        Public Property Requerimiento As String
    End Class
End Class
