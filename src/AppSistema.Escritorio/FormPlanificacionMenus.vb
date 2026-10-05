Imports System.Collections.Generic
Imports System.Drawing
Imports System.Globalization
Imports System.Linq
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Calculos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Planificación de menús: matriz mensual del plan teórico. Por cada día: receta, factor de participación, raciones,
''' costo unitario y costo total. Las filas son los componentes del servicio (estructura) y sus alternativas.
''' Cada cambio va directo a la base, que valida la operación y el estado de la jornada; no hay cambios pendientes de guardar.
''' Los totales y el resumen salen del dominio; la pantalla solo presenta.
''' </summary>
Public Class FormPlanificacionMenus
    Inherits Form

    Private Shared ReadOnly Cultura As CultureInfo = CultureInfo.GetCultureInfo("es-PE")
    Private Const ColumnasFijas As Integer = 2
    Private Const ColumnasPorDia As Integer = 5
    ' Desplazamiento de cada columna dentro del grupo de un día.
    Private Const DesplReceta As Integer = 0
    Private Const DesplFactor As Integer = 1
    Private Const DesplRaciones As Integer = 2
    Private Const DesplCostoUnitario As Integer = 3
    Private Const DesplCostoTotal As Integer = 4

    Private ReadOnly _menu As ServicioPlanificacionMenu
    Private ReadOnly _minutas As ServicioMinutas
    Private ReadOnly _puedeEditar As Boolean
    Private ReadOnly _puedeAprobar As Boolean
    Private ReadOnly _servicios As New ComboBox With {.Name = "cboServicioRegimen", .DropDownStyle = ComboBoxStyle.DropDownList, .DisplayMember = "Texto", .Width = 340}
    Private ReadOnly _mes As New DateTimePicker With {.Name = "dtpMes", .Format = DateTimePickerFormat.Custom, .CustomFormat = "MMMM yyyy", .ShowUpDown = True, .Width = 150}
    Private ReadOnly _grilla As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _resumenMes As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _resumenDia As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _resumenAcum As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _estado As New Label With {.Name = "lblEstado", .Dock = DockStyle.Bottom, .Height = 26, .Padding = New Padding(6, 5, 6, 0)}
    Private _matriz As MatrizMensual
    Private _fechaSeleccionada As Date
    Private _listo As Boolean

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _menu = New ServicioPlanificacionMenu(cadena, sesion)
        _minutas = New ServicioMinutas(cadena, sesion)
        _puedeEditar = sesion.Tiene(Permisos.MinutasEditar)
        _puedeAprobar = sesion.Tiene(Permisos.MinutasAprobar)
        Name = "FormPlanificacionMenus"
        Text = "Planificacion de menus - " & sesion.Operacion.Nombre
        _mes.Value = New Date(Date.Today.Year, Date.Today.Month, 1)
        _fechaSeleccionada = Date.Today

        Dim titulo As New Label With {.Name = "lblTitulo", .Text = "Planificacion de menus", .Font = Tema.FuenteTitulo,
                                      .Dock = DockStyle.Top, .Height = 40, .Padding = New Padding(8, 8, 0, 0)}
        Dim operacion As New Label With {.Name = "lblOperacion", .Text = "Operacion: " & sesion.Operacion.Nombre,
                                         .Dock = DockStyle.Top, .Height = 24, .Padding = New Padding(8, 0, 0, 0)}

        Dim btnConsultar = Ui.Boton("Consultar", AddressOf Cargar)
        btnConsultar.Name = "btnConsultar"
        Dim btnHoy = Ui.Boton("Ir a hoy", Sub() IrAFecha(Date.Today))
        btnHoy.Name = "btnIrAHoy"
        Dim btnFecha = Ui.Boton("Ir a fecha...", AddressOf PedirFecha)
        btnFecha.Name = "btnIrAFecha"
        Dim btnAprobar = Ui.BotonSi(_puedeAprobar, "Aprobar jornada...", AddressOf AprobarJornada)
        btnAprobar.Name = "btnAprobarJornada"
        Dim barra = Ui.BarraBotones(
            New Label With {.Text = "Servicio y regimen", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _servicios,
            New Label With {.Text = "Mes", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _mes,
            btnConsultar, btnHoy, btnFecha, btnAprobar)
        barra.Dock = DockStyle.Top

        Dim franja As New FlowLayoutPanel With {.Dock = DockStyle.Top, .AutoSize = True, .Padding = New Padding(6, 4, 6, 4), .BackColor = Tema.AvisoFondo}
        franja.Controls.Add(New Label With {.Name = "lblNota", .Text = "Las raciones deben incluir las raciones del personal.", .AutoSize = True,
                                            .ForeColor = Tema.AvisoTexto, .Margin = New Padding(0, 4, 18, 0)})
        franja.Controls.Add(Leyenda("Estructura del servicio", Tema.EstructuraFondo))
        franja.Controls.Add(Leyenda("Celda habilitada (jornada en borrador)", Tema.CeldaHabilitada))
        franja.Controls.Add(Leyenda("Celda bloqueada (jornada aprobada o cerrada)", Tema.CeldaBloqueada))

        _grilla.Name = "gridPlanificacionMenus"
        _grilla.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        _grilla.ColumnHeadersHeight = 46
        _grilla.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True
        _grilla.SelectionMode = DataGridViewSelectionMode.CellSelect
        _grilla.MultiSelect = False
        _grilla.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2
        _grilla.Dock = DockStyle.Fill

        Dim pie As New TableLayoutPanel With {.Name = "pieResumenes", .Dock = DockStyle.Bottom, .Height = 230, .ColumnCount = 3, .RowCount = 1}
        pie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 30))
        pie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35))
        pie.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 35))
        pie.Controls.Add(Grupo("Total del mes", _resumenMes, "gbTotalMes"), 0, 0)
        pie.Controls.Add(Grupo("Dia seleccionado: planificado y realizado", _resumenDia, "gbDia"), 1, 0)
        pie.Controls.Add(Grupo("Acumulado hasta la fecha seleccionada", _resumenAcum, "gbAcumulado"), 2, 0)

        Controls.Add(_grilla)
        Controls.Add(_estado)
        Controls.Add(pie)
        Controls.Add(franja)
        Controls.Add(barra)
        Controls.Add(operacion)
        Controls.Add(titulo)

        AddHandler _grilla.CellEndEdit, Sub(s, e) Editar(e.RowIndex, e.ColumnIndex)
        AddHandler _grilla.CellEnter, Sub(s, e) AlEntrarCelda(e.RowIndex, e.ColumnIndex)
        AddHandler _mes.ValueChanged, Sub() If _listo Then Cargar()
        AddHandler _servicios.SelectedIndexChanged, Sub() If _listo Then Cargar()
        AddHandler Load, Sub() CargarServicios()
    End Sub

    Private Shared Function Leyenda(texto As String, fondo As Color) As Label
        Return New Label With {.Text = texto, .AutoSize = True, .BackColor = fondo, .ForeColor = Tema.Texto,
                               .Padding = New Padding(6, 3, 6, 3), .Margin = New Padding(0, 4, 10, 0)}
    End Function

    Private Shared Function Grupo(titulo As String, grilla As DataGridView, nombre As String) As GroupBox
        Dim caja As New GroupBox With {.Text = titulo, .Name = nombre, .Dock = DockStyle.Fill}
        grilla.Dock = DockStyle.Fill
        caja.Controls.Add(grilla)
        Return caja
    End Function

    ' ---------- Carga ----------

    Private Sub CargarServicios()
        Ui.Ejecutar(Me, Sub()
                            Dim servicios = _minutas.ListarServiciosDeOperacion()
                            _servicios.DataSource = servicios.Select(Function(s) New OpcionServicio With {.Id = s.Id, .Texto = s.ServicioNombre & " - " & s.RegimenNombre}).ToList()
                        End Sub)
        _listo = True
        Cargar()
    End Sub

    Private Sub Cargar()
        Dim elegido = TryCast(_servicios.SelectedItem, OpcionServicio)
        If elegido Is Nothing Then Return
        Dim servicioId = elegido.Id
        Dim anio = _mes.Value.Year
        Dim mes = _mes.Value.Month
        Ui.Ejecutar(Me, Sub() _matriz = _menu.Matriz(servicioId, anio, mes))
        If _matriz Is Nothing Then Return
        If _fechaSeleccionada.Year <> anio OrElse _fechaSeleccionada.Month <> mes Then _fechaSeleccionada = New Date(anio, mes, 1)
        ConstruirGrilla()
        ActualizarResumenes()
        _estado.Text = $"{_matriz.Filas.Count} filas de preparaciones y {_matriz.Dias.Count} jornadas en el mes. " &
                       "Los cambios se guardan al confirmar cada celda; la base no permite cambiar jornadas aprobadas."
    End Sub

    ''' <summary>Arma las columnas (fijas + cinco por día) y las filas de la matriz. Cada celda se marca una sola vez.</summary>
    Private Sub ConstruirGrilla()
        _grilla.Rows.Clear()
        _grilla.Columns.Clear()
        Dim anio = _mes.Value.Year
        Dim mes = _mes.Value.Month
        Dim dias = DateTime.DaysInMonth(anio, mes)

        AgregarColumna("colOrden", "Orden", 50, True)
        AgregarColumna("colEstructura", "Estructura del servicio", 210, True)
        For d = 1 To dias
            Dim etiqueta = New Date(anio, mes, d).ToString("ddd dd/MM", Cultura)
            AgregarColumna($"colReceta{d}", etiqueta & vbLf & "Receta", 210)
            AgregarColumna($"colFactor{d}", etiqueta & vbLf & "Factor %", 74)
            AgregarColumna($"colRaciones{d}", etiqueta & vbLf & "Raciones", 80)
            AgregarColumna($"colCostoUnitario{d}", etiqueta & vbLf & "Costo unitario", 96)
            AgregarColumna($"colCostoTotal{d}", etiqueta & vbLf & "Costo total", 104)
        Next

        Dim porFecha As New Dictionary(Of Date, JornadaDia)()
        For Each j In _matriz.Dias
            porFecha(j.Fecha) = j
        Next

        For Each fila In _matriz.Filas
            Dim valores(_grilla.Columns.Count - 1) As Object
            valores(0) = fila.Orden
            valores(1) = If(fila.Alternativa > 0, $"{fila.Estructura} (alternativa {fila.Alternativa + 1})", fila.Estructura)
            For d = 1 To dias
                Dim j As JornadaDia = Nothing
                If Not porFecha.TryGetValue(New Date(anio, mes, d), j) Then Continue For
                Dim p = j.Platos.FirstOrDefault(Function(x) x.EstructuraId = fila.EstructuraId AndAlso x.Fila = fila.Alternativa)
                If p Is Nothing Then Continue For
                Dim b = Desplazamiento(d)
                valores(b + DesplReceta) = $"{p.RecetaCodigo} - {p.RecetaNombre}"
                valores(b + DesplFactor) = Porcentaje(p.FactorBp)
                valores(b + DesplRaciones) = p.Raciones
                valores(b + DesplCostoUnitario) = Monto(p.CostoRacionU6, 6)
                valores(b + DesplCostoTotal) = Monto(p.CostoTotalU6, 2)
            Next
            Dim indice = _grilla.Rows.Add(valores)
            Dim filaGrilla = _grilla.Rows(indice)
            filaGrilla.Tag = "plato"
            For d = 1 To dias
                Dim j As JornadaDia = Nothing
                If Not porFecha.TryGetValue(New Date(anio, mes, d), j) Then Continue For
                Dim p = j.Platos.FirstOrDefault(Function(x) x.EstructuraId = fila.EstructuraId AndAlso x.Fila = fila.Alternativa)
                If p IsNot Nothing Then MarcarPlato(filaGrilla, Desplazamiento(d), j, p)
            Next
            MarcarEstructura(filaGrilla)
        Next

        AgregarFilasDeResumen(dias, porFecha)
        AplicarEdicion(dias, porFecha)
    End Sub

    ''' <summary>
    ''' Decide celda por celda qué se puede escribir: factor y raciones de una preparación, y comensales del día, solo en jornadas
    ''' en borrador y con permiso. Es el único lugar que fija la edición, para que ninguna celda quede editable por defecto.
    ''' </summary>
    Private Sub AplicarEdicion(dias As Integer, porFecha As Dictionary(Of Date, JornadaDia))
        Dim anio = _mes.Value.Year
        Dim mes = _mes.Value.Month
        For Each fila As DataGridViewRow In _grilla.Rows
            Dim tipo = Convert.ToString(fila.Tag)
            For c = 0 To _grilla.Columns.Count - 1
                Dim editable = False
                If c >= ColumnasFijas AndAlso _puedeEditar Then
                    Dim dia = (c - ColumnasFijas) \ ColumnasPorDia + 1
                    Dim posicion = (c - ColumnasFijas) Mod ColumnasPorDia
                    Dim j As JornadaDia = Nothing
                    If porFecha.TryGetValue(New Date(anio, mes, dia), j) AndAlso j.Editable Then
                        If tipo = "plato" AndAlso fila.Cells(c).Tag IsNot Nothing AndAlso (posicion = DesplFactor OrElse posicion = DesplRaciones) Then editable = True
                        If tipo = "comensales" AndAlso posicion = DesplRaciones Then editable = True
                    End If
                End If
                fila.Cells(c).ReadOnly = Not editable
            Next
        Next
    End Sub

    ''' <summary>Comensales, costo del día, costo por bandeja y estado de la jornada (una fila cada uno, bajo las preparaciones).</summary>
    Private Sub AgregarFilasDeResumen(dias As Integer, porFecha As Dictionary(Of Date, JornadaDia))
        Dim anio = _mes.Value.Year
        Dim mes = _mes.Value.Month
        Dim etiquetas = {"Comensales", "Costo del dia (materia prima + estructura fija)", "Costo por bandeja", "Estado de la jornada"}
        For Each etiqueta In etiquetas
            Dim valores(_grilla.Columns.Count - 1) As Object
            valores(1) = etiqueta
            For d = 1 To dias
                Dim j As JornadaDia = Nothing
                Dim b = Desplazamiento(d)
                If Not porFecha.TryGetValue(New Date(anio, mes, d), j) Then
                    If etiqueta = "Estado de la jornada" Then valores(b + DesplReceta) = "Sin jornada"
                    Continue For
                End If
                Dim r = j.Resumen
                Select Case etiqueta
                    Case "Comensales" : valores(b + DesplRaciones) = j.Comensales
                    Case "Costo del dia (materia prima + estructura fija)" : valores(b + DesplCostoTotal) = Monto(r.CostoTotalU6, 2)
                    Case "Costo por bandeja" : valores(b + DesplCostoUnitario) = Bandeja(r)
                    Case "Estado de la jornada" : valores(b + DesplReceta) = If(j.Editable, "Borrador: editable", "Aprobada: bloqueada")
                End Select
            Next
            Dim indice = _grilla.Rows.Add(valores)
            Dim fila = _grilla.Rows(indice)
            fila.Tag = If(etiqueta = "Comensales", "comensales", "resumen")
            fila.Cells(0).Style.BackColor = Tema.SuperficieFuerte
            fila.Cells(1).Style.BackColor = Tema.SuperficieFuerte
            For d = 1 To dias
                Dim j As JornadaDia = Nothing
                If Not porFecha.TryGetValue(New Date(anio, mes, d), j) Then Continue For
                Dim b = Desplazamiento(d)
                For k = 0 To ColumnasPorDia - 1
                    fila.Cells(b + k).Style.BackColor = If(j.Editable, Tema.CeldaHabilitada, Tema.CeldaBloqueada)
                Next
                If etiqueta = "Comensales" Then fila.Cells(b + DesplRaciones).Tag = j
            Next
        Next
    End Sub

    Private Sub AgregarColumna(nombre As String, encabezado As String, ancho As Integer, Optional congelada As Boolean = False)
        Dim columna As New DataGridViewTextBoxColumn With {.Name = nombre, .HeaderText = encabezado, .Width = ancho,
                                                          .SortMode = DataGridViewColumnSortMode.NotSortable, .Frozen = congelada, .ReadOnly = False}
        If nombre.StartsWith("colFactor", StringComparison.Ordinal) OrElse nombre.StartsWith("colRaciones", StringComparison.Ordinal) OrElse
           nombre.StartsWith("colCosto", StringComparison.Ordinal) OrElse nombre = "colOrden" Then
            columna.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        End If
        _grilla.Columns.Add(columna)
    End Sub

    Private Sub MarcarEstructura(fila As DataGridViewRow)
        For c = 0 To ColumnasFijas - 1
            fila.Cells(c).Style.BackColor = Tema.EstructuraFondo
            fila.Cells(c).ReadOnly = True
        Next
    End Sub

    ''' <summary>Colores y etiqueta de ayuda de una preparación. La edición la fija AplicarEdicion.</summary>
    Private Sub MarcarPlato(fila As DataGridViewRow, b As Integer, j As JornadaDia, p As PlatoMatriz)
        Dim fondo = If(j.Editable, Tema.CeldaHabilitada, Tema.CeldaBloqueada)
        For k = 0 To ColumnasPorDia - 1
            fila.Cells(b + k).Style.BackColor = fondo
            fila.Cells(b + k).Tag = p
        Next
        fila.Cells(b + DesplReceta).ToolTipText = $"{p.RecetaCodigo} - {p.RecetaNombre}"
    End Sub

    Private Shared Function Desplazamiento(dia As Integer) As Integer
        Return ColumnasFijas + (dia - 1) * ColumnasPorDia
    End Function

    ' ---------- Resúmenes ----------

    Private Sub ActualizarResumenes()
        Dim mes = _matriz.ResumenMes()
        Ui.Mostrar(_resumenMes, FilasMes(mes), "Concepto|Concepto", "Valor|Valor (S/)")
        Dim jornada = JornadaDe(_fechaSeleccionada)
        Dim dia = If(jornada Is Nothing, New ResumenPlanificacion(), jornada.Resumen)
        Ui.Mostrar(_resumenDia, FilasComparativas(dia, jornada IsNot Nothing), "Concepto|Concepto", "Planificado|Planificado", "Realizado|Realizado")
        Dim acumulado = _matriz.ResumenHasta(_fechaSeleccionada)
        Ui.Mostrar(_resumenAcum, FilasComparativas(acumulado, True), "Concepto|Concepto", "Planificado|Planificado", "Realizado|Realizado")
    End Sub

    Private Shared Function FilasMes(r As ResumenPlanificacion) As List(Of FilaValor)
        Return New List(Of FilaValor) From {
            New FilaValor With {.Concepto = "Materia prima", .Valor = Monto(r.MateriaPrimaU6, 2)},
            New FilaValor With {.Concepto = "Estructura fija", .Valor = Monto(r.EstructuraFijaU6, 2)},
            New FilaValor With {.Concepto = "Costo total", .Valor = Monto(r.CostoTotalU6, 2)},
            New FilaValor With {.Concepto = "Comensales", .Valor = r.Comensales.ToString("N0", Cultura)},
            New FilaValor With {.Concepto = "Costo por bandeja", .Valor = Bandeja(r)}}
    End Function

    ''' <summary>Planificado y realizado. El realizado todavía no se registra en esta pantalla: se muestra como pendiente y no como cero.</summary>
    Private Shared Function FilasComparativas(r As ResumenPlanificacion, hayJornada As Boolean) As List(Of FilaComparativa)
        If Not hayJornada Then
            Return New List(Of FilaComparativa) From {New FilaComparativa With {.Concepto = "Sin jornada en la fecha", .Planificado = "-", .Realizado = "-"}}
        End If
        Const pendiente = "pendiente"
        Return New List(Of FilaComparativa) From {
            New FilaComparativa With {.Concepto = "Materia prima", .Planificado = Monto(r.MateriaPrimaU6, 2), .Realizado = pendiente},
            New FilaComparativa With {.Concepto = "Estructura fija", .Planificado = Monto(r.EstructuraFijaU6, 2), .Realizado = pendiente},
            New FilaComparativa With {.Concepto = "Costo total", .Planificado = Monto(r.CostoTotalU6, 2), .Realizado = pendiente},
            New FilaComparativa With {.Concepto = "Comensales", .Planificado = r.Comensales.ToString("N0", Cultura), .Realizado = pendiente},
            New FilaComparativa With {.Concepto = "Costo por bandeja", .Planificado = Bandeja(r), .Realizado = pendiente},
            New FilaComparativa With {.Concepto = "Diferencia monetaria", .Planificado = "-",
                                      .Realizado = Monto(PlanificacionMenu.DiferenciaMonetariaU6(r.CostoTotalU6, Nothing), 2)},
            New FilaComparativa With {.Concepto = "Diferencia porcentual", .Planificado = "-",
                                      .Realizado = Porcentaje(PlanificacionMenu.DiferenciaPorcentualBp(r.CostoTotalU6, Nothing))}}
    End Function

    ' ---------- Presentación de cantidades (la precisión interna se conserva; aquí solo se muestra) ----------

    ''' <summary>Monto en u6 con los decimales indicados. Un costo pendiente se muestra como "pendiente", nunca como cero.</summary>
    Private Shared Function Monto(valorU6 As Long?, decimales As Integer) As String
        If Not valorU6.HasValue Then Return "pendiente"
        Return (CDec(valorU6.Value) / 1000000D).ToString("N" & decimales, Cultura)
    End Function

    ''' <summary>Costo por bandeja del día o del acumulado. Sin comensales no es calculable (no se divide entre cero).</summary>
    Private Shared Function Bandeja(r As ResumenPlanificacion) As String
        If r.Comensales <= 0 Then Return "No calculable"
        Return Monto(r.CostoBandejaU6, 2)
    End Function

    Private Shared Function Porcentaje(bp As Long?) As String
        If Not bp.HasValue Then Return "No calculable"
        Return (CDec(bp.Value) / 100D).ToString("0.##", Cultura) & " %"
    End Function

    ''' <summary>Factor de participación en puntos básicos a partir de lo escrito (60 o 2,5 o 60 %). Hasta dos decimales.</summary>
    Private Shared Function FactorDesdeTexto(texto As String) As Long
        Dim limpio = If(texto, "").Replace("%", "").Trim()
        Dim valor As Decimal
        If Not Decimal.TryParse(limpio, NumberStyles.Number, Cultura, valor) Then
            Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "El factor debe ser un porcentaje, por ejemplo 60 o 2,5.")
        End If
        Dim bp = valor * 100D
        If bp <> Math.Round(bp, 0, MidpointRounding.AwayFromZero) Then
            Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "El factor admite hasta dos decimales (por ejemplo 2,50 %).")
        End If
        Return CLng(Math.Round(bp, 0, MidpointRounding.AwayFromZero))
    End Function

    ' ---------- Edición ----------

    ''' <summary>Confirma una celda editada: factor o raciones de una preparación, o comensales del día. La base valida.</summary>
    Private Sub Editar(fila As Integer, columna As Integer)
        If fila < 0 OrElse columna < ColumnasFijas Then Return
        Dim fila_ = _grilla.Rows(fila)
        Dim celda = fila_.Cells(columna)
        Dim texto = Convert.ToString(celda.Value, Cultura)
        Dim posicion = (columna - ColumnasFijas) Mod ColumnasPorDia
        If Convert.ToString(fila_.Tag) = "comensales" Then
            Dim jornada = TryCast(celda.Tag, JornadaDia)
            If jornada Is Nothing Then Return
            Ui.Ejecutar(Me, Sub() _menu.FijarComensales(jornada.MinutaId, Ui.LeerEntero(texto, "comensales")))
        ElseIf Convert.ToString(fila_.Tag) = "plato" AndAlso TypeOf celda.Tag Is PlatoMatriz Then
            Dim plato = CType(celda.Tag, PlatoMatriz)
            If posicion = DesplFactor Then
                Ui.Ejecutar(Me, Sub() _menu.FijarFactor(plato.Id, FactorDesdeTexto(texto)))
            ElseIf posicion = DesplRaciones Then
                Ui.Ejecutar(Me, Sub() _menu.FijarRaciones(plato.Id, Ui.LeerEntero(texto, "raciones")))
            Else
                Return
            End If
        Else
            Return
        End If
        Cargar()
    End Sub

    Private Sub AlEntrarCelda(fila As Integer, columna As Integer)
        If columna < ColumnasFijas OrElse _matriz Is Nothing Then Return
        Dim dia = (columna - ColumnasFijas) \ ColumnasPorDia + 1
        Dim fecha = New Date(_mes.Value.Year, _mes.Value.Month, dia)
        If fecha = _fechaSeleccionada Then Return
        _fechaSeleccionada = fecha
        ActualizarResumenes()
    End Sub

    Private Function JornadaDe(fecha As Date) As JornadaDia
        If _matriz Is Nothing Then Return Nothing
        Return _matriz.Dias.FirstOrDefault(Function(j) j.Fecha = fecha.Date)
    End Function

    ' ---------- Acciones ----------

    Private Sub PedirFecha()
        Using d As New DialogoCampos("Ir a fecha")
            d.Fecha("fecha", "Fecha", _fechaSeleccionada)
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            IrAFecha(d.FechaElegida("fecha").Value)
        End Using
    End Sub

    ''' <summary>Cambia de mes si hace falta y desplaza la matriz para mostrar el día (las columnas fijas quedan visibles).</summary>
    Private Sub IrAFecha(fecha As Date)
        If fecha.Year <> _mes.Value.Year OrElse fecha.Month <> _mes.Value.Month Then
            _mes.Value = New Date(fecha.Year, fecha.Month, 1)
        End If
        _fechaSeleccionada = fecha.Date
        Dim columna = Desplazamiento(fecha.Day)
        If _grilla.Columns.Count > columna Then _grilla.FirstDisplayedScrollingColumnIndex = columna
        ActualizarResumenes()
    End Sub

    Private Sub AprobarJornada()
        Dim jornada = JornadaDe(_fechaSeleccionada)
        If jornada Is Nothing Then Ui.Informar(Me, "El dia seleccionado no tiene jornada en este servicio.") : Return
        If Not jornada.Editable Then Ui.Informar(Me, "La jornada ya esta aprobada y no se modifica.") : Return
        Using d As New DialogoCampos("Aprobar jornada del " & jornada.Fecha.ToShortDateString())
            d.Texto("moneda", "Moneda de los precios", "PEN")
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            If Not Ui.Confirmar(Me, "Al aprobar se fija el costo previsto con los precios vigentes y la jornada ya no se modifica. Continuar?") Then Return
            Ui.Ejecutar(Me, Sub() _minutas.Aprobar(jornada.MinutaId, d.Valor("moneda")))
        End Using
        Cargar()
    End Sub

    Private NotInheritable Class OpcionServicio
        Public Property Id As Long
        Public Property Texto As String
    End Class

    Private Class FilaValor
        Public Property Concepto As String
        Public Property Valor As String
    End Class

    Private Class FilaComparativa
        Public Property Concepto As String
        Public Property Planificado As String
        Public Property Realizado As String
    End Class

End Class
