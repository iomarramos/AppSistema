Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Catálogo de reportes por categoría. Cada reporte pide sus parámetros (minuta, almacén, inventario, periodo) y sale
''' por <see cref="SalidaReporte"/> en impresión, Excel, PDF o CSV. Solo aparecen los reportes que el usuario puede ver.
''' </summary>
Partial Public Class FormReportes

    Private ReadOnly _catalogo As New List(Of Entrada)

    Public Sub New()
        InitializeComponent()
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Text = "Reportes - " & sesion.Operacion.Nombre
        dtDesde.Value = Date.Today.AddDays(-Date.Today.Day + 1)
        dtHasta.Value = Date.Today

        Dim reportes As New ServicioReportes(cadena, sesion)
        Dim minutas As New ServicioMinutas(cadena, sesion)
        Dim produccion As New ServicioProduccion(cadena, sesion)
        Dim administracion As New ServicioAdministracion(cadena, sesion)
        Dim inventarios As New ServicioInventarios(cadena, sesion)
        Dim stock As New ServicioStock(cadena, sesion)

        Agregar("Planificacion", "Matriz de planificacion (periodo)", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.MatrizDelPeriodo(desde, hasta)
                End Function)

        Agregar("Planificacion", "Minuta del dia", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Dim minuta = Elegir(dueno, "Minuta", minutas.ListarMinutas(desde, hasta),
                                        Function(m As MinutaDto) $"{m.Fecha:dd/MM/yyyy} - {m.ServicioNombre} ({m.Estado})")
                    If minuta Is Nothing Then Return Nothing
                    Return Function() reportes.MinutaDelDia(minuta.Id)
                End Function)

        Agregar("Produccion", "Requerimiento a almacen", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Dim minuta = Elegir(dueno, "Minuta", minutas.ListarMinutas(desde, hasta),
                                        Function(m As MinutaDto) $"{m.Fecha:dd/MM/yyyy} - {m.ServicioNombre}")
                    If minuta Is Nothing Then Return Nothing
                    Dim requerimiento = Elegir(dueno, "Requerimiento", produccion.ListarRequerimientos(minuta.Id),
                                               Function(q As RequerimientoDto) $"{q.Numero} ({q.Tipo}, {q.Estado})")
                    If requerimiento Is Nothing Then Return Nothing
                    Return Function() reportes.Requerimiento(requerimiento.Id)
                End Function)

        Agregar("Almacen", "Stock valorizado", Permisos.CatalogoVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim almacen = Elegir(dueno, "Almacen", administracion.ListarAlmacenes(),
                                         Function(a As AlmacenResumen) $"{a.Codigo} - {a.Nombre}")
                    If almacen Is Nothing Then Return Nothing
                    Return Function() reportes.StockValorizado(almacen.Id)
                End Function)

        Agregar("Almacen", "Registro de inventario permanente (formato 13.1)", Permisos.CatalogoVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim almacen = Elegir(dueno, "Almacen", administracion.ListarAlmacenes(),
                                         Function(a As AlmacenResumen) $"{a.Codigo} - {a.Nombre}")
                    If almacen Is Nothing Then Return Nothing
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.RegistroInventarioPermanente(almacen.Id, desde, hasta)
                End Function)

        Agregar("Inventario", "Inventario fisico (resultado)", Permisos.InventarioVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim almacen = Elegir(dueno, "Almacen", administracion.ListarAlmacenes(),
                                         Function(a As AlmacenResumen) $"{a.Codigo} - {a.Nombre}")
                    If almacen Is Nothing Then Return Nothing
                    Dim inventario = Elegir(dueno, "Inventario", inventarios.Listar(almacen.Id),
                                            Function(i As InventarioDto) $"{i.Numero} ({i.Tipo}, {i.Estado})")
                    If inventario Is Nothing Then Return Nothing
                    Return Function() reportes.Inventario(inventario.Id, False)
                End Function)

        Agregar("Inventario", "Hoja de conteo (ciega, sin stock del sistema)", Permisos.InventarioVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim almacen = Elegir(dueno, "Almacen", administracion.ListarAlmacenes(),
                                         Function(a As AlmacenResumen) $"{a.Codigo} - {a.Nombre}")
                    If almacen Is Nothing Then Return Nothing
                    Dim inventario = Elegir(dueno, "Inventario", inventarios.Listar(almacen.Id),
                                            Function(i As InventarioDto) $"{i.Numero} ({i.Tipo}, {i.Estado})")
                    If inventario Is Nothing Then Return Nothing
                    Return Function() reportes.Inventario(inventario.Id, True)
                End Function)

        Agregar("Almacen", "Salida a produccion", Permisos.CatalogoVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.SalidasAProduccion(desde, hasta)
                End Function)

        Agregar("Planificacion", "Comparativo mensual (teorico frente a realizado)", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim servicio = Elegir(dueno, "Servicio", minutas.ListarServiciosDeOperacion(),
                                          Function(o As OperacionServicioDto) $"{o.ServicioNombre} - {o.RegimenNombre}")
                    If servicio Is Nothing Then Return Nothing
                    Dim anio = dtDesde.Value.Year
                    Dim mes = dtDesde.Value.Month
                    Return Function() reportes.ComparativoMensual(servicio.Id, anio, mes)
                End Function)

        Agregar("Cierres", "Resultado mensual de alimentos", Permisos.ReportesVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim anio = dtDesde.Value.Year
                    Dim mes = dtDesde.Value.Month
                    Return Function() reportes.ResultadoMensual(anio, mes)
                End Function)

        Agregar("Almacen", "Kardex de un producto (movimientos y saldo)", Permisos.CatalogoVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim almacen = Elegir(dueno, "Almacen", administracion.ListarAlmacenes(),
                                         Function(a As AlmacenResumen) $"{a.Codigo} - {a.Nombre}")
                    If almacen Is Nothing Then Return Nothing
                    Dim texto As String = Nothing
                    Using d As New DialogoCampos("Buscar producto")
                        d.Texto("buscar", "Codigo o nombre del producto (vacio = todos)")
                        If d.ShowDialog(dueno) <> DialogResult.OK Then Return Nothing
                        texto = d.Valor("buscar")
                    End Using
                    Dim saldos = stock.ConsultarSaldos(almacen.Id, texto)
                    Dim saldo = Elegir(dueno, "Producto", saldos,
                                       Function(x As SaldoStockDto) $"{x.ProductoDescripcion} {x.VarianteDescripcion} (saldo {x.Unidad})")
                    If saldo Is Nothing Then Return Nothing
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.Kardex(almacen.Id, saldo.VarianteId, desde, hasta)
                End Function)

        Agregar("Inventario", "Boleta de ajuste (motivo, explicacion y responsable)", Permisos.InventarioVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim almacen = Elegir(dueno, "Almacen", administracion.ListarAlmacenes(),
                                         Function(a As AlmacenResumen) $"{a.Codigo} - {a.Nombre}")
                    If almacen Is Nothing Then Return Nothing
                    Dim inventario = Elegir(dueno, "Inventario", inventarios.Listar(almacen.Id),
                                            Function(i As InventarioDto) $"{i.Numero} ({i.Tipo}, {i.Estado})")
                    If inventario Is Nothing Then Return Nothing
                    Return Function() reportes.BoletaAjustes(inventario.Id)
                End Function)

        Agregar("Cierres", "Explicacion de ajustes por motivo y responsable", Permisos.InventarioVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.ExplicacionAjustes(desde, hasta)
                End Function)

        Agregar("Planificacion", "Costo resumido teorico por dia y servicio", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.CostoResumidoTeorico(desde, hasta)
                End Function)

        Agregar("Planificacion", "Prevision de consumo (necesidades consolidadas)", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.PrevisionConsumo(desde, hasta)
                End Function)

        Agregar("Planificacion", "Minuta teorico vs real (plan SGP)", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.MinutaTeoricoReal(desde, hasta)
                End Function)
        Agregar("Planificacion", "Frecuencia de recetas", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.FrecuenciaRecetas(desde, hasta)
                End Function)

        Agregar("Planificacion", "Requisicion por servicio (atendida)", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.RequisicionPorServicio(desde, hasta)
                End Function)

        Agregar("Planificacion", "Comparativo de raciones (teoricas, operativas, servidas)", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.ComparativoRaciones(desde, hasta)
                End Function)

        Agregar("Planificacion", "Costo detallado teorico por producto", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.CostoDetalladoTeorico(desde, hasta)
                End Function)

        Agregar("Planificacion", "Requisicion detallada por estructura", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.RequisicionPorEstructura(desde, hasta)
                End Function)

        Agregar("Abastecimiento", "Resumen de compras (recepciones)", Permisos.ComprasVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.ResumenCompras(desde, hasta)
                End Function)

        Agregar("Almacen", "Resumen de traspasos", Permisos.CatalogoVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.ResumenTraspasos(desde, hasta)
                End Function)

        Agregar("Planificacion", "Control de raciones (teoricas, operativas, servidas, vendidas)", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.ControlRaciones(desde, hasta)
                End Function)

        Agregar("Planificacion", "Venta de servicio por fuente", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.VentaServicioContado(desde, hasta)
                End Function)

        Agregar("Planificacion", "Costo realizado por periodo", Permisos.MenusVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.CostoRealizado(desde, hasta)
                End Function)

        Agregar("Abastecimiento", "Mapa de solicitud de compras (ultima prevision)", Permisos.ComprasVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim almacen = Elegir(dueno, "Almacen", administracion.ListarAlmacenes(),
                                         Function(a As AlmacenResumen) $"{a.Codigo} - {a.Nombre}")
                    If almacen Is Nothing Then Return Nothing
                    Return Function() reportes.MapaSolicitudCompras(almacen.Id)
                End Function)

        Agregar("Cierres", "Resultado comparado: presupuesto, teorico y real", Permisos.ReportesVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim anio = dtDesde.Value.Year
                    Dim mes = dtDesde.Value.Month
                    Return Function() reportes.ComparativoResultado(anio, mes)
                End Function)

        Agregar("Administracion", "Auditoria de cambios (exportable)", Permisos.AuditoriaVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Dim login As String = Nothing
                    Using d As New DialogoCampos("Auditoria")
                        d.Texto("login", "Usuario (vacio = todos)")
                        d.Texto("tabla", "Tabla (vacio = todas, p. ej. usuario o minuta)")
                        If d.ShowDialog(dueno) <> DialogResult.OK Then Return Nothing
                        login = d.Valor("login")
                        Dim tabla = d.Valor("tabla")
                        Return Function() reportes.Auditoria(desde, hasta, tabla, login)
                    End Using
                End Function)

        Agregar("Almacen", "Monitor de transitos (traspasos entre almacenes)", Permisos.ComprasVer,
                Function(dueno As Form) As Func(Of Reporte)
                    Dim desde = dtDesde.Value.Date
                    Dim hasta = dtHasta.Value.Date
                    Return Function() reportes.Transitos(desde, hasta)
                End Function)

        lstReportes.DisplayMember = NameOf(Entrada.Titulo)
        For Each e In _catalogo.Where(Function(x) sesion.Tiene(x.Permiso))
            lstReportes.Items.Add(e)
        Next
        If lstReportes.Items.Count > 0 Then lstReportes.SelectedIndex = 0
        barraAcciones.Controls.Add(Ui.Boton("Generar reporte...", AddressOf Generar))
        AddHandler lstReportes.SelectedIndexChanged, Sub() MostrarDescripcion()
        MostrarDescripcion()
    End Sub

    Private Sub Agregar(categoria As String, nombre As String, permiso As String, preparar As Func(Of Form, Func(Of Reporte)))
        _catalogo.Add(New Entrada With {.Categoria = categoria, .Nombre = nombre, .Permiso = permiso, .Preparar = preparar})
    End Sub

    Private Sub MostrarDescripcion()
        Dim e = TryCast(lstReportes.SelectedItem, Entrada)
        lblDescripcion.Text = If(e Is Nothing, "No tiene reportes disponibles con su perfil.",
                                 $"{e.Categoria}: {e.Nombre}. Elija el formato al generarlo: imprimir, Excel, PDF o CSV.")
    End Sub

    ''' <summary>Pide los parámetros del reporte elegido y lo emite. Cancelar en cualquier paso no hace nada.</summary>
    Private Sub Generar()
        Dim e = TryCast(lstReportes.SelectedItem, Entrada)
        If e Is Nothing Then Ui.Informar(Me, "Seleccione un reporte de la lista.") : Return
        Dim obtener As Func(Of Reporte) = Nothing
        Ui.Ejecutar(Me, Sub() obtener = e.Preparar(Me))
        If obtener Is Nothing Then Return
        SalidaReporte.Emitir(Me, obtener)
    End Sub

    ''' <summary>Elige un elemento de una lista (Nothing si no hay ninguno o el usuario cancela).</summary>
    Private Shared Function Elegir(Of T As Class)(dueno As Form, titulo As String, items As IList(Of T), texto As Func(Of T, String)) As T
        If items.Count = 0 Then Ui.Informar(dueno, $"No hay {titulo.ToLowerInvariant()} disponible para este reporte.") : Return Nothing
        Using d As New DialogoCampos($"Elegir {titulo.ToLowerInvariant()}")
            d.Opciones("e", titulo, items.Select(Function(i) CObj(New Opcion(Of T)(i, texto(i)))))
            If d.ShowDialog(dueno) <> DialogResult.OK Then Return Nothing
            Return d.Elegido(Of Opcion(Of T))("e").Valor
        End Using
    End Function

    ''' <summary>Un reporte del catálogo: categoría, nombre, permiso mínimo y cómo pedir sus parámetros.</summary>
    Private NotInheritable Class Entrada
        Public Property Categoria As String
        Public Property Nombre As String
        Public Property Permiso As String
        Public Property Preparar As Func(Of Form, Func(Of Reporte))
        Public ReadOnly Property Titulo As String
            Get
                Return $"{Categoria} > {Nombre}"
            End Get
        End Property
    End Class
End Class
