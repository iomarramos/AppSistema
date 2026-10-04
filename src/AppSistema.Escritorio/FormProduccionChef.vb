Imports System.Collections.Generic
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Seguridad

''' <summary>
''' Producción del chef: las raciones a producir de cada plato, desde 3 días atrás en adelante, mientras el día no esté
''' cerrado. Solo se edita la columna de raciones a producir; la base vuelve a validar cada cambio y guarda el historial.
''' </summary>
Public Class FormProduccionChef
    Inherits Form

    Private ReadOnly _servicio As ServicioProduccion
    Private ReadOnly _grilla As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _estado As New Label With {.Dock = DockStyle.Bottom, .Height = 28, .Padding = New Padding(6)}
    Private ReadOnly _originales As New Dictionary(Of Long, Long)()
    Private _platos As List(Of PlatoProduccionDto) = New List(Of PlatoProduccionDto)()

    Public Sub New(cadena As String, sesion As SesionUsuario)
        _servicio = New ServicioProduccion(cadena, sesion)
        Text = "Produccion del chef - " & sesion.Operacion.Nombre
        _grilla.Name = "gridProduccionChef"
        _grilla.Dock = DockStyle.Fill

        Dim barra = Ui.BarraBotones(Ui.Boton("Actualizar", AddressOf Cargar), Ui.Boton("Guardar raciones a producir", AddressOf Guardar))
        barra.Dock = DockStyle.Top
        Controls.Add(_grilla)
        Controls.Add(_estado)
        Controls.Add(barra)
        AddHandler Load, Sub() Cargar()
    End Sub

    Private Sub Cargar()
        Ui.Ejecutar(Me, Sub() _platos = _servicio.PlatosProducibles())
        _originales.Clear()
        For Each p In _platos
            _originales(p.MinutaDetalleId) = p.RacionesProducir
        Next
        Ui.Mostrar(_grilla, _platos, "Fecha|Fecha", "Servicio|Servicio", "Estructura|Estructura", "RecetaCodigo|Codigo",
                   "Receta|Receta", "RacionesMinuta|Raciones de la minuta", "RacionesProducir|Raciones a producir", "DiaCerrado|Dia cerrado")
        For Each c As DataGridViewColumn In _grilla.Columns
            c.ReadOnly = c.Name <> "RacionesProducir"
        Next
        For Each fila As DataGridViewRow In _grilla.Rows
            Dim p = TryCast(fila.DataBoundItem, PlatoProduccionDto)
            If p IsNot Nothing AndAlso p.DiaCerrado Then fila.Cells("RacionesProducir").ReadOnly = True
        Next
        _estado.Text = $"{_platos.Count} platos desde 3 dias atras (fecha de Lima). Los dias cerrados no se cambian."
    End Sub

    ''' <summary>Guarda solo los platos cuyas raciones a producir cambiaron. Un cambio rechazado por la base no detiene a los demás.</summary>
    Private Sub Guardar()
        _grilla.EndEdit()
        Dim guardados As Integer = 0
        Dim rechazos As New List(Of String)()
        For Each p In _platos
            If p.DiaCerrado OrElse p.RacionesProducir = _originales(p.MinutaDetalleId) Then Continue For
            Try
                _servicio.FijarRacionesProducir(p.MinutaDetalleId, p.RacionesProducir)
                guardados += 1
            Catch ex As ReglaNegocioException
                rechazos.Add($"{p.Fecha:dd/MM} {p.Receta}: {ex.Message}")
            End Try
        Next
        If rechazos.Count > 0 Then Ui.Informar(Me, "No se guardaron algunos cambios:" & Environment.NewLine & String.Join(Environment.NewLine, rechazos))
        Cargar()
        If guardados > 0 Then Ui.Informar(Me, $"{guardados} cambio(s) guardado(s).")
    End Sub

End Class
