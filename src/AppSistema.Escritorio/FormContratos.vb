Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Clientes y contratos mensuales de la operación: servicios con importe y vigencia, ajustes desde una fecha (la línea
''' anterior se cierra, no se edita) y generación del ingreso del mes desde contrato.
''' </summary>
Public Class FormContratos
    Inherits Form

    Private ReadOnly _servicio As ServicioContratos
    Private ReadOnly _contratos As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _lineas As DataGridView = Ui.NuevaGrilla()

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _servicio = New ServicioContratos(cadena, sesion)
        Text = "Contratos - " & sesion.Operacion.Nombre
        Dim edita = sesion.Tiene(Permisos.ContratosEditar)
        Dim barra = Ui.BarraBotones(Ui.Boton("Clientes", AddressOf VerClientes), Ui.BotonSi(edita, "Nuevo cliente...", AddressOf NuevoCliente),
                                    Ui.BotonSi(edita, "Nuevo contrato...", AddressOf NuevoContrato), Ui.BotonSi(edita, "Cerrar vigencia...", AddressOf CerrarContrato),
                                    Ui.BotonSi(edita, "Agregar servicio...", AddressOf AgregarServicio), Ui.BotonSi(edita, "Ajustar importe...", AddressOf Ajustar),
                                    Ui.BotonSi(edita, "Generar ingresos del mes...", AddressOf GenerarIngresos))
        Dim division As New SplitContainer With {.Dock = DockStyle.Fill, .Orientation = Orientation.Horizontal, .SplitterDistance = 200}
        division.Panel1.Controls.Add(_contratos)
        division.Panel2.Controls.Add(_lineas)
        Controls.Add(division)
        Controls.Add(New Label With {.Dock = DockStyle.Top, .Height = 34, .Padding = New Padding(4),
            .Text = "Un ajuste cierra la linea vigente el dia anterior y abre otra con el importe nuevo. El ingreso del mes se prorratea por dias y no reemplaza un ingreso registrado a mano."})
        Controls.Add(barra)
        AddHandler _contratos.SelectionChanged, Sub() CargarLineas()
        AddHandler Load, Sub() CargarContratos()
    End Sub

    Private ReadOnly Property Contrato As ContratoDto
        Get
            Return Ui.Seleccionado(Of ContratoDto)(_contratos)
        End Get
    End Property

    Private Sub CargarContratos()
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_contratos, _servicio.ListarContratos(), "Codigo|Contrato", "Cliente|Cliente", "FechaDesde|Desde", "FechaHasta|Hasta",
                                         "Moneda|Moneda", "Condiciones|Condiciones"))
    End Sub

    Private Sub CargarLineas()
        Dim c = Contrato
        If c Is Nothing Then _lineas.DataSource = Nothing : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(_lineas, _servicio.Lineas(c.Id), "Servicio|Servicio", "ImporteMensualU6|Importe mensual", "FechaDesde|Desde", "FechaHasta|Hasta"))
    End Sub

    Private Sub VerClientes()
        Ui.Ejecutar(Me, Sub() Ui.MostrarLista(Me, "Clientes", "Clientes de la empresa", _servicio.ListarClientes(), "Codigo|Codigo", "Nombre|Nombre", "IdentificacionFiscal|RUC"))
    End Sub

    Private Sub NuevoCliente()
        Using d As New DialogoCampos("Nuevo cliente")
            d.Texto("codigo", "Codigo").Texto("nombre", "Nombre o razon social").Texto("ruc", "RUC (opcional)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CrearCliente(d.Valor("codigo"), d.Valor("nombre"), d.Valor("ruc")))
        End Using
    End Sub

    Private Sub NuevoContrato()
        Dim clientes As List(Of ClienteDto) = Nothing
        If Not Ui.Ejecutar(Me, Sub() clientes = _servicio.ListarClientes()) Then Return
        If clientes.Count = 0 Then Ui.Informar(Me, "Registre primero el cliente.") : Return
        Using d As New DialogoCampos("Nuevo contrato")
            d.Opciones("cliente", "Cliente", clientes.Select(Function(c) CObj(New Opcion(Of ClienteDto)(c, $"{c.Codigo} - {c.Nombre}")))) _
             .Texto("codigo", "Codigo del contrato").Fecha("desde", "Vigente desde", Date.Today).Fecha("hasta", "Vigente hasta (opcional)", Date.Today, opcional:=True) _
             .Texto("condiciones", "Condiciones (opcional)")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CrearContrato(d.Elegido(Of Opcion(Of ClienteDto))("cliente").Valor.Id, d.Valor("codigo"),
                                                           d.FechaElegida("desde").Value, d.FechaElegida("hasta"), d.Valor("condiciones")))
        End Using
        CargarContratos()
    End Sub

    Private Sub CerrarContrato()
        Dim c = Contrato
        If c Is Nothing Then Return
        Using d As New DialogoCampos("Fin de vigencia de " & c.Codigo)
            d.Fecha("hasta", "Ultimo dia de vigencia", Date.Today)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.CerrarContrato(c.Id, d.FechaElegida("hasta").Value))
        End Using
        CargarContratos()
    End Sub

    Private Sub AgregarServicio()
        Dim c = Contrato
        If c Is Nothing Then Ui.Informar(Me, "Seleccione un contrato.") : Return
        Dim servicios As List(Of OperacionServicioDto) = Nothing
        If Not Ui.Ejecutar(Me, Sub() servicios = _servicio.ServiciosDeOperacion()) Then Return
        Using d As New DialogoCampos("Servicio del contrato " & c.Codigo)
            d.Opciones("servicio", "Servicio", servicios.Select(Function(s) CObj(New Opcion(Of OperacionServicioDto)(s, $"{s.ServicioNombre} - {s.RegimenNombre}")))) _
             .Texto("importe", "Importe mensual (S/)").Fecha("desde", "Desde", If(c.FechaDesde > Date.Today, c.FechaDesde, Date.Today)) _
             .Fecha("hasta", "Hasta (opcional)", Date.Today, opcional:=True)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.AgregarServicio(c.Id, d.Elegido(Of Opcion(Of OperacionServicioDto))("servicio").Valor.Id,
                                                             Ui.LeerU6(d.Valor("importe"), "importe"), d.FechaElegida("desde").Value, d.FechaElegida("hasta")))
        End Using
        CargarLineas()
    End Sub

    Private Sub Ajustar()
        Dim l = Ui.Seleccionado(Of LineaContratoDto)(_lineas)
        If l Is Nothing Then Ui.Informar(Me, "Seleccione la linea a ajustar.") : Return
        Using d As New DialogoCampos("Ajuste de " & l.Servicio)
            d.Texto("importe", "Nuevo importe mensual (S/)").Fecha("desde", "Vigente desde", Date.Today)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() _servicio.Ajustar(l.Id, Ui.LeerU6(d.Valor("importe"), "importe"), d.FechaElegida("desde").Value))
        End Using
        CargarLineas()
    End Sub

    Private Sub GenerarIngresos()
        Using d As New DialogoCampos("Generar ingresos desde contratos")
            d.Fecha("mes", "Mes (cualquier dia del mes)", Date.Today)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim f = d.FechaElegida("mes").Value
            Ui.Ejecutar(Me, Sub() Ui.MostrarLista(Me, "Ingresos generados", $"Ingresos de {f:MM/yyyy}", _servicio.GenerarIngresos(f.Year, f.Month),
                                                  "Servicio|Servicio", "ImporteU6|Importe", "Estado|Estado", "Detalle|Detalle"))
        End Using
    End Sub
End Class
