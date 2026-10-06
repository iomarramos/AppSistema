Imports System.Data
Imports System.Globalization
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Minuta teórica o real en el formato de la planilla del SGP: estructuras con sus platos, y por cada día cuatro columnas
''' (N.Rac., Por.(%), Costo y Cod. Receta). Al final, comensales, costo de la minuta del día, el costo del otro nivel y la
''' diferencia (real − teórico). Solo lectura: lo que se cambia está en la planilla del menú.
''' </summary>
Partial Public Class FormMinutaVista

    Private _servicio As ServicioPlanSgp
    Private _sesion As SesionUsuario

    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridVista)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridVista)
        _servicio = New ServicioPlanSgp(cadena, sesion)
        _sesion = sesion
        Text = "Minuta teorica y real - " & sesion.Operacion.Nombre
        cmbNivel.Items.Add(New Opcion(Of String)("TEORICO", "Teorico (planificado)"))
        cmbNivel.Items.Add(New Opcion(Of String)("REAL", "Real (ejecutado)"))
        cmbNivel.SelectedIndex = 0
        dtDesde.Value = Date.Today.AddDays(-Date.Today.Day + 1)
        dtHasta.Value = dtDesde.Value.AddMonths(1).AddDays(-1)
        barraAcciones.Controls.Add(Ui.Boton("Ver", AddressOf Cargar))
        barraAcciones.Controls.Add(Ui.Boton("Exportar JSON...", AddressOf ExportarJson))
        AddHandler cmbServicio.SelectedIndexChanged, Sub() Cargar()
        AddHandler cmbNivel.SelectedIndexChanged, Sub() Cargar()
        AddHandler dtDesde.ValueChanged, Sub() Cargar()
        AddHandler dtHasta.ValueChanged, Sub() Cargar()
        AddHandler Load, Sub() CargarServicios()
    End Sub

    Private Sub CargarServicios()
        Dim servicios As List(Of String) = Nothing
        If Not Ui.Ejecutar(Me, Sub() servicios = _servicio.ListarServicios()) Then Return
        cmbServicio.Items.Clear()
        For Each s In servicios
            cmbServicio.Items.Add(New Opcion(Of String)(s, s))
        Next
        If cmbServicio.Items.Count = 0 Then
            lblEstado.Text = "No hay plan del SGP cargado en esta operacion."
            Return
        End If
        cmbServicio.SelectedIndex = 0
    End Sub

    Private Function Elegido(combo As ComboBox) As String
        Dim o = TryCast(combo.SelectedItem, Opcion(Of String))
        Return If(o Is Nothing, Nothing, o.Valor)
    End Function

    Private Sub Cargar()
        Dim servicio = Elegido(cmbServicio)
        Dim nivel = Elegido(cmbNivel)
        If servicio Is Nothing OrElse nivel Is Nothing Then Return
        Dim desde = dtDesde.Value.Date
        Dim hasta = dtHasta.Value.Date
        Dim vista As VistaPlanSgpDto = Nothing
        Dim otra As VistaPlanSgpDto = Nothing
        Dim otroNivel = If(nivel = "TEORICO", "REAL", "TEORICO")
        If Not Ui.Ejecutar(Me,
            Sub()
                vista = _servicio.Vista(nivel, servicio, desde, hasta)
                otra = _servicio.Vista(otroNivel, servicio, desde, hasta)
            End Sub) Then Return
        Mostrar(vista, otra)
    End Sub

    ''' <summary>Guarda el menú mensual real del servicio en JSON (esquema acordado con el programador).</summary>
    Private Sub ExportarJson()
        Dim servicio = Elegido(cmbServicio)
        If servicio Is Nothing Then Return
        Dim desde = dtDesde.Value.Date
        Dim hasta = dtHasta.Value.Date
        Using a As New SaveFileDialog With {.Filter = "JSON (*.json)|*.json", .FileName = $"menu_mensual_{desde:yyyyMM}.json"}
            If a.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim json As String = Nothing
            If Not Ui.Ejecutar(Me, Sub() json = _servicio.MenuMensualJson(servicio, desde, hasta)) Then Return
            System.IO.File.WriteAllText(a.FileName, json, New System.Text.UTF8Encoding(False))
            Ui.Informar(Me, "Menu exportado: " & a.FileName)
        End Using
    End Sub

    Private Shared Function NombreColumna(fecha As Date, parte As String) As String
        Return "D" & fecha.ToString("yyyyMMdd", CultureInfo.InvariantCulture) & "_" & parte
    End Function

    Private Sub Mostrar(v As VistaPlanSgpDto, otra As VistaPlanSgpDto)
        Dim tabla As New DataTable()
        tabla.Columns.Add("Estructura", GetType(String))
        For Each d In v.Dias
            Dim cap = d.Fecha.ToString("dd ddd", CultureInfo.CurrentCulture)
            tabla.Columns.Add(NombreColumna(d.Fecha, "R")).Caption = cap & " N.Rac."
            tabla.Columns.Add(NombreColumna(d.Fecha, "P")).Caption = cap & " Por.(%)"
            tabla.Columns.Add(NombreColumna(d.Fecha, "C")).Caption = cap & " Costo"
            tabla.Columns.Add(NombreColumna(d.Fecha, "Q")).Caption = cap & " Cod. Receta"
        Next
        Dim diaOtro = New Dictionary(Of Date, DiaPlanSgpDto)()
        If otra IsNot Nothing Then
            For Each d In otra.Dias
                diaOtro(d.Fecha) = d
            Next
        End If

        For Each est In v.Estructuras
            ' Filas de la estructura: el día con más platos define cuántas filas ocupa el bloque.
            Dim filas = 0
            For Each d In v.Dias
                Dim lista As List(Of PlatoPlanSgpDto) = Nothing
                If v.Platos.TryGetValue(VistaPlanSgpDto.Clave(est, d.Fecha), lista) Then filas = Math.Max(filas, lista.Count)
            Next
            For k = 0 To filas - 1
                Dim fila = tabla.NewRow()
                fila("Estructura") = If(k = 0, est, "")
                For Each d In v.Dias
                    Dim lista As List(Of PlatoPlanSgpDto) = Nothing
                    If v.Platos.TryGetValue(VistaPlanSgpDto.Clave(est, d.Fecha), lista) AndAlso k < lista.Count Then
                        Dim p = lista(k)
                        Dim raciones = p.RacionesU6 / 1000000.0
                        fila(NombreColumna(d.Fecha, "R")) = raciones.ToString("0", CultureInfo.InvariantCulture)
                        If d.Comensales > 0 Then
                            fila(NombreColumna(d.Fecha, "P")) = (raciones * 100.0 / d.Comensales).ToString("0.00", CultureInfo.InvariantCulture)
                        End If
                        fila(NombreColumna(d.Fecha, "C")) = (p.CostoRacionU6 / 1000000.0).ToString("0.000000", CultureInfo.InvariantCulture)
                        fila(NombreColumna(d.Fecha, "Q")) = p.Receta
                    End If
                Next
                tabla.Rows.Add(fila)
            Next
        Next

        ' Resumen por día: comensales, costo de la minuta y comparación con el otro nivel.
        Dim comensales = tabla.NewRow()
        comensales("Estructura") = "Comensales"
        Dim costo = tabla.NewRow()
        costo("Estructura") = "Costo minuta del dia (S/)"
        Dim estado = tabla.NewRow()
        estado("Estructura") = "Estado"
        Dim costoOtro = tabla.NewRow()
        costoOtro("Estructura") = If(v.Nivel = "TEORICO", "Costo real del dia (S/)", "Costo teorico del dia (S/)")
        Dim diferencia = tabla.NewRow()
        diferencia("Estructura") = "Diferencia real - teorico (S/)"
        For Each d In v.Dias
            comensales(NombreColumna(d.Fecha, "R")) = d.Comensales.ToString(CultureInfo.InvariantCulture)
            costo(NombreColumna(d.Fecha, "C")) = (d.CostoMinutaDiaU6 / 1000000.0).ToString("0.00", CultureInfo.InvariantCulture)
            estado(NombreColumna(d.Fecha, "R")) = If(v.Nivel = "REAL", "Real", If(d.TieneReal, "Con real", "Sin real"))
            Dim dOtro As DiaPlanSgpDto = Nothing
            If diaOtro.TryGetValue(d.Fecha, dOtro) Then
                costoOtro(NombreColumna(d.Fecha, "C")) = (dOtro.CostoMinutaDiaU6 / 1000000.0).ToString("0.00", CultureInfo.InvariantCulture)
                Dim real = If(v.Nivel = "REAL", d.CostoMinutaDiaU6, dOtro.CostoMinutaDiaU6)
                Dim teorico = If(v.Nivel = "REAL", dOtro.CostoMinutaDiaU6, d.CostoMinutaDiaU6)
                diferencia(NombreColumna(d.Fecha, "C")) = ((real - teorico) / 1000000.0).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture)
            End If
        Next
        tabla.Rows.Add(comensales)
        tabla.Rows.Add(costo)
        tabla.Rows.Add(estado)
        tabla.Rows.Add(costoOtro)
        tabla.Rows.Add(diferencia)

        gridVista.DataSource = tabla
        gridVista.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        gridVista.Columns("Estructura").Frozen = True
        gridVista.Columns("Estructura").Width = 230
        For Each col As DataGridViewColumn In gridVista.Columns
            If col.Name = "Estructura" Then Continue For
            col.HeaderText = tabla.Columns(col.Name).Caption
            col.Width = If(col.Name.EndsWith("_Q", StringComparison.Ordinal), 90, If(col.Name.EndsWith("_C", StringComparison.Ordinal), 75, 62))
        Next
        lblEstado.Text = $"{v.Servicio} - {If(v.Nivel = "TEORICO", "teorico", "real")}: {v.Dias.Count} dias, {v.Estructuras.Count} estructuras. " &
                         "Por.(%) = raciones / comensales del dia. Costo por racion en soles. Solo lectura."
    End Sub

End Class
