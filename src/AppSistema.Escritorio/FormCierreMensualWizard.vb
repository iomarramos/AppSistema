Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Asistente de cierre mensual: los 8 pasos con su estado, el calendario de días del mes y el checklist del día elegido,
''' con la columna "Ir a" que dice dónde se resuelve cada pendiente. El cierre del mes lo valida el servicio.
''' </summary>
Partial Public Class FormCierreMensualWizard

    Private ReadOnly _servicio As ServicioCierres
    Private ReadOnly _cierra As Boolean

    Public Sub New()
        InitializeComponent()
        Ui.Configurar(gridPasos)
        Ui.Configurar(gridCalendario)
        Ui.Configurar(gridControles)
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario)
        InitializeComponent()
        Ui.Configurar(gridPasos)
        Ui.Configurar(gridCalendario)
        Ui.Configurar(gridControles)
        _servicio = New ServicioCierres(cadena, sesion)
        _cierra = sesion.Tiene(Permisos.CierreEjecutar)
        Text = "Cierre mensual - " & sesion.Operacion.Nombre
        dtMes.Value = New Date(Date.Today.Year, Date.Today.Month, 1)
        barraAcciones.Controls.Add(Ui.Boton("Actualizar", AddressOf Cargar))
        barraAcciones.Controls.Add(Ui.BotonSi(_cierra, "Cerrar mes", AddressOf CerrarMes))
        barraAcciones.Controls.Add(Ui.Boton("Calendario por semanas...", AddressOf VerSemanas))
        AddHandler gridCalendario.SelectionChanged, Sub() MostrarControles()
        Cargar()
    End Sub

    Private Sub Cargar()
        Ui.Ejecutar(Me,
            Sub()
                Dim anio = dtMes.Value.Year
                Dim mes = dtMes.Value.Month
                Ui.Mostrar(gridPasos, _servicio.ChecklistMes(anio, mes), "Paso|Paso", "Nombre|Paso a paso", "Estado|Estado", "Detalle|Detalle")
                Ui.Mostrar(gridCalendario, _servicio.CalendarioMes(anio, mes), "Fecha|Dia", "Estado|Estado")
            End Sub)
        MostrarControles()
    End Sub

    ''' <summary>Checklist del día seleccionado en el calendario.</summary>
    Private Sub MostrarControles()
        Dim dia = Ui.Seleccionado(Of DiaCalendarioDto)(gridCalendario)
        If dia Is Nothing Then Ui.Mostrar(gridControles, New List(Of ControlCierreDto)(), "Control|Control") : Return
        Ui.Ejecutar(Me, Sub() Ui.Mostrar(gridControles, _servicio.ChecklistDia(dia.Fecha),
                                         "Control|Control", "Estado|Estado", "Cantidad|Cantidad", "Detalle|Detalle", "IrA|Ir a"))
    End Sub

    ''' <summary>El calendario del mes en semanas: cada celda es el día con su estado (Cerrado, Con pendientes, Listo, Abierto).</summary>
    Private Sub VerSemanas()
        Dim anio = dtMes.Value.Year
        Dim mes = dtMes.Value.Month
        Ui.Ejecutar(Me, Sub() Ui.MostrarLista(Me, $"Calendario {mes:00}/{anio} por semanas",
                                               "Cada celda es el dia del mes y su estado. Las casillas vacias no pertenecen al mes.",
                                               _servicio.CalendarioSemanal(anio, mes),
                                               "Semana|Semana", "Lunes|Lunes", "Martes|Martes", "Miercoles|Miercoles", "Jueves|Jueves",
                                               "Viernes|Viernes", "Sabado|Sabado", "Domingo|Domingo"))
    End Sub

    Private Sub CerrarMes()
        Dim anio = dtMes.Value.Year
        Dim mes = dtMes.Value.Month
        If Not Ui.Confirmar(Me, $"Cerrar el mes {mes:00}/{anio}? Movimientos, ingresos y gastos del mes quedaran fijos.") Then Return
        Dim resultado As ResultadoCierre = Nothing
        If Not Ui.Ejecutar(Me, Sub() resultado = _servicio.CerrarMes(anio, mes)) Then Return
        If resultado.Cerrado Then
            Ui.Informar(Me, "Mes cerrado.")
        Else
            Ui.Informar(Me, "No se cerro el mes. Pendientes:" & vbCrLf & String.Join(vbCrLf, resultado.Pendientes.Select(Function(p) "- " & p.Detalle)))
        End If
        Cargar()
    End Sub
End Class
