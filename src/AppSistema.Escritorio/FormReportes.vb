Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Menú de reportes del SGP y del plan: se elige el reporte y el periodo (rango de fechas o mes) y se abre la vista previa,
''' desde la que se imprime o se exporta a Excel. Reúne los reportes que no tienen botón propio en otra pantalla.
''' </summary>
Public Class FormReportes
    Inherits Form

    ''' <summary>Un reporte de la lista: si se pide por mes o por rango, y cómo se genera.</summary>
    Private NotInheritable Class Elemento
        Public Property Nombre As String
        Public Property PorMes As Boolean
        Public Property Generar As Func(Of ServicioReportes, Date, Date, Reporte)

        Public Overrides Function ToString() As String
            Return Nombre
        End Function
    End Class

    Private ReadOnly _cadena As String
    Private ReadOnly _sesion As SesionUsuario
    Private ReadOnly _lista As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = 380, .Name = "cboReporte"}
    Private ReadOnly _desde As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 110, .Name = "dtpDesde"}
    Private ReadOnly _hasta As New DateTimePicker With {.Format = DateTimePickerFormat.Short, .Width = 110, .Name = "dtpHasta"}
    Private ReadOnly _mes As New DateTimePicker With {.Format = DateTimePickerFormat.Custom, .CustomFormat = "MM/yyyy", .ShowUpDown = True, .Width = 90, .Name = "dtpMes"}
    Private ReadOnly _nota As New Label With {.Dock = DockStyle.Top, .Height = 34, .Padding = New Padding(6)}

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _cadena = cadena
        _sesion = sesion
        Text = "Reportes (SGP y plan) - " & sesion.Operacion.Nombre
        _desde.Value = New Date(Date.Today.Year, Date.Today.Month, 1)
        _hasta.Value = Date.Today
        _mes.Value = Date.Today
        _lista.Items.AddRange(Elementos().Cast(Of Object)().ToArray())
        _lista.SelectedIndex = 0

        Dim barra = Ui.BarraBotones(New Label With {.Text = "Reporte", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _lista,
                                    New Label With {.Text = "Desde", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _desde,
                                    New Label With {.Text = "Hasta", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _hasta,
                                    New Label With {.Text = "Mes", .AutoSize = True, .Margin = New Padding(3, 9, 3, 3)}, _mes,
                                    Ui.Boton("Vista previa...", AddressOf Ver))
        Controls.Add(_nota)
        Controls.Add(barra)
        AddHandler _lista.SelectedIndexChanged, Sub() ActualizarPeriodo()
        ActualizarPeriodo()
    End Sub

    ''' <summary>Los reportes del menú. Los del SGP salen de los datos importados; los demás, de las minutas, las ventas y los movimientos.</summary>
    Private Shared Function Elementos() As List(Of Elemento)
        Return New List(Of Elemento) From {
            New Elemento With {.Nombre = "Requisicion detallada (SGP, por rango)", .PorMes = False,
                               .Generar = Function(s, d, h) s.RequisicionRango(d, h)},
            New Elemento With {.Nombre = "Salidas a produccion por servicio (por rango)", .PorMes = False,
                               .Generar = Function(s, d, h) s.SalidasPorServicio(d, h, False)},
            New Elemento With {.Nombre = "Devoluciones de produccion por servicio (por rango)", .PorMes = False,
                               .Generar = Function(s, d, h) s.SalidasPorServicio(d, h, True)},
            New Elemento With {.Nombre = "Resumen de traspasos (por rango)", .PorMes = False,
                               .Generar = Function(s, d, h) s.Traspasos(d, h)},
            New Elemento With {.Nombre = "Boleta de ajuste R-AL (por rango)", .PorMes = False,
                               .Generar = Function(s, d, h) s.BoletaAjuste(d, h)},
            New Elemento With {.Nombre = "Frecuencia de la planificacion teorica (mes)", .PorMes = True,
                               .Generar = Function(s, d, h) s.FrecuenciaTeorica(d.Year, d.Month)},
            New Elemento With {.Nombre = "Costo piso y techo (mes, SGP)", .PorMes = True,
                               .Generar = Function(s, d, h) s.CostoPisoTecho(d.Year, d.Month)},
            New Elemento With {.Nombre = "Menu teorico - planificacion (mes)", .PorMes = True,
                               .Generar = Function(s, d, h) s.MenuMes(d.Year, d.Month, False)},
            New Elemento With {.Nombre = "Menu real - chef (mes)", .PorMes = True,
                               .Generar = Function(s, d, h) s.MenuMes(d.Year, d.Month, True)},
            New Elemento With {.Nombre = "Food cost alimento (mes)", .PorMes = True,
                               .Generar = Function(s, d, h) s.FoodCost(d.Year, d.Month)},
            New Elemento With {.Nombre = "Comparativo de tres niveles (mes)", .PorMes = True,
                               .Generar = Function(s, d, h) s.ComparativoTresNiveles(d.Year, d.Month)},
            New Elemento With {.Nombre = "Resultado operacional A13 (mes)", .PorMes = True,
                               .Generar = Function(s, d, h) s.ResultadoA13(d.Year, d.Month)}}
    End Function

    ''' <summary>Solo se pide el mes cuando el reporte es mensual; si no, el rango de fechas.</summary>
    Private Sub ActualizarPeriodo()
        Dim e = TryCast(_lista.SelectedItem, Elemento)
        If e Is Nothing Then Return
        _mes.Enabled = e.PorMes
        _desde.Enabled = Not e.PorMes
        _hasta.Enabled = Not e.PorMes
    End Sub

    Private Sub Ver()
        Dim e = TryCast(_lista.SelectedItem, Elemento)
        If e Is Nothing Then Return
        Dim desde As Date, hasta As Date
        If e.PorMes Then
            desde = New Date(_mes.Value.Year, _mes.Value.Month, 1)
            hasta = desde.AddMonths(1).AddDays(-1)
        Else
            desde = _desde.Value.Date
            hasta = _hasta.Value.Date
            If hasta < desde Then
                Ui.Informar(Me, "La fecha final es anterior a la inicial.")
                Return
            End If
        End If
        Dim cadena = _cadena, sesion = _sesion, generar = e.Generar
        SalidaReporte.Emitir(Me, Function() generar(New ServicioReportes(cadena, sesion), desde, hasta))
    End Sub

End Class
