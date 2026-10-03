Imports System.IO
Imports System.Text
Imports System.Windows.Forms
Imports AppSistema.Datos
Imports AppSistema.Dominio.Importacion

Public Enum ModoImportacion
    Catalogo
    Recetas
End Enum

''' <summary>
''' Importación del catálogo desde CSV: plantilla, vista previa por fila e importación todo o nada.
''' En modo Recetas importa recetas normalizadas. En modo Catálogo también acepta el listado de productos del SGP (pro_nombre, pro_coduni, pro_facing), que se convierte al vuelo.
''' </summary>
Partial Public Class FormImportacion

    Private ReadOnly _modo As ModoImportacion
    Private ReadOnly _servicio As ServicioImportacionCatalogo
    Private ReadOnly _recetas As ServicioImportacionRecetas
    Private ReadOnly _sesion As SesionUsuario
    Private _vistaRecetas As ResultadoImportacionRecetas
    Private ReadOnly _archivo As New Label With {.AutoSize = True, .Text = "(sin archivo)", .Margin = New Padding(3, 9, 3, 3)}
    Private ReadOnly _resumen As New Label With {.Dock = DockStyle.Bottom, .AutoSize = False, .Height = 40, .Padding = New Padding(6)}
    Private ReadOnly _filas As DataGridView = Ui.NuevaGrilla()
    Private ReadOnly _importar As Button
    Private ReadOnly _observaciones As Button
    Private _texto As String
    Private _ultimaVista As ResultadoImportacion

    ''' <param name="modo">Catálogo (Catálogo > Importar catálogo, permiso CATALOGO_IMPORTAR) o Recetas (Menús > Importar recetas,
    ''' permiso RECETAS_EDITAR). Cada menú abre su propia ventana y solo acepta su tipo de archivo.</param>
    Public Sub New()
        InitializeComponent()
    End Sub

    Public Sub New(cadena As String, sesion As SesionUsuario, modo As ModoImportacion)
        InitializeComponent()
        Controls.Clear()
        _servicio = New ServicioImportacionCatalogo(cadena, sesion)
        _recetas = New ServicioImportacionRecetas(cadena, sesion)
        _sesion = sesion
        _modo = modo
        Text = If(modo = ModoImportacion.Recetas, "Importar recetas", "Importar catalogo")
        _importar = Ui.Boton("Importar", AddressOf Importar)
        _importar.Enabled = False
        _observaciones = Ui.Boton("Observaciones...", AddressOf GuardarObservaciones)
        _observaciones.Enabled = False
        Controls.Add(_filas)
        Controls.Add(_resumen)
        Dim barra = Ui.BarraBotones(Ui.Boton("Descargar plantilla...", AddressOf GuardarPlantilla), Ui.Boton("Elegir archivo...", AddressOf ElegirArchivo),
                                    Ui.Boton("Vista previa", AddressOf VistaPrevia), _importar, _observaciones, _archivo)
        barra.Controls(0).Visible = modo = ModoImportacion.Catalogo          ' la plantilla es del catálogo
        Controls.Add(barra)
        AddHandler _filas.RowPrePaint,
            Sub(s, e)
                Dim f = TryCast(_filas.Rows(e.RowIndex).DataBoundItem, FilaResultadoImportacion)
                If f Is Nothing Then Return
                _filas.Rows(e.RowIndex).DefaultCellStyle.BackColor =
                    If(f.Estado = EstadoFilaImportacion.ConError, Drawing.Color.MistyRose,
                       If(f.Estado = EstadoFilaImportacion.Nueva, Drawing.Color.Honeydew, Drawing.SystemColors.Window))
            End Sub
    End Sub

    Private Sub GuardarPlantilla()
        Using d As New SaveFileDialog With {.Filter = "CSV (*.csv)|*.csv", .FileName = "plantilla_catalogo.csv"}
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Dim cabecera = String.Join(";", LectorCsvCatalogo.ColumnasObligatorias.Take(3).Concat({"categoria"}) _
                                            .Concat(LectorCsvCatalogo.ColumnasObligatorias.Skip(3).Take(1)).Concat({"marca"}) _
                                            .Concat(LectorCsvCatalogo.ColumnasObligatorias.Skip(4)) _
                                            .Concat({"empaque_codigo", "empaque_descripcion", "envases_por_empaque", "minimo", "multiplo"}))
            Dim ejemplo = "ACE;Aceite vegetal;L;ABARROTES;ACE-A-4L;Marca A;Aceite A 4 L;bidon;4;CAJA4;Caja 4 x 4 L;4;1;1"
            Ui.Ejecutar(Me, Sub() File.WriteAllText(d.FileName, cabecera & Environment.NewLine & ejemplo & Environment.NewLine, New UTF8Encoding(True)))
        End Using
    End Sub

    Private Sub GuardarObservaciones()
        If _ultimaVista Is Nothing Then Return
        Using d As New SaveFileDialog With {.Filter = "Texto (*.txt)|*.txt", .FileName = "observaciones_importacion.txt"}
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            Ui.Ejecutar(Me, Sub() File.WriteAllLines(d.FileName, _ultimaVista.Observaciones, New UTF8Encoding(True)))
        End Using
    End Sub

    Private Sub ElegirArchivo()
        Using d As New OpenFileDialog With {.Filter = "CSV o listado SGP (*.csv;*.tsv;*.txt)|*.csv;*.tsv;*.txt|Todos los archivos (*.*)|*.*"}
            If d.ShowDialog(Me) <> DialogResult.OK Then Return
            If Ui.Ejecutar(Me, Sub() _texto = LeerTexto(d.FileName)) Then
                _archivo.Text = Path.GetFileName(d.FileName)
                VistaPrevia()
            End If
        End Using
    End Sub

    ''' <summary>UTF-8 si es válido; si no, Latin-1 (CSV guardado por Excel en Windows en español).</summary>
    Private Shared Function LeerTexto(ruta As String) As String
        Dim bytes = File.ReadAllBytes(ruta)
        Try
            Return New UTF8Encoding(False, True).GetString(bytes).TrimStart(ChrW(&HFEFF))
        Catch ex As DecoderFallbackException
            Return Encoding.Latin1.GetString(bytes)
        End Try
    End Function

    Private Sub VistaPrevia()
        If _texto Is Nothing Then Ui.Informar(Me, "Elija primero un archivo.") : Return
        _importar.Enabled = False
        Dim esRecetas = LectorCsvRecetas.EsArchivoRecetas(_texto)
        If esRecetas AndAlso _modo = ModoImportacion.Catalogo Then
            Ui.Informar(Me, "Este archivo es de recetas. Importelo desde Menus > Importar recetas.") : Return
        End If
        If Not esRecetas AndAlso _modo = ModoImportacion.Recetas Then
            Ui.Informar(Me, "Este archivo no es de recetas (formato receta_codigo;receta_nombre;...). El catalogo se importa desde Catalogo > Importar catalogo.") : Return
        End If
        If esRecetas Then VistaPreviaRecetas() : Return
        _vistaRecetas = Nothing
        Ui.Ejecutar(Me,
            Sub()
                _ultimaVista = If(ConversorSgp.EsListadoSgp(_texto), _servicio.VistaPreviaSgp(_texto), _servicio.VistaPrevia(_texto, crearUnidadesBase:=True))
                Ui.Mostrar(_filas, _ultimaVista.Filas, "Numero|Fila", "Estado|Estado", "Detalle|Detalle")
                _resumen.Text = "Vista previa: " & _ultimaVista.Resumen &
                                If(_ultimaVista.Observaciones.Count > 0, $" {_ultimaVista.Observaciones.Count} observaciones (boton Observaciones).", "")
                _observaciones.Enabled = _ultimaVista.Observaciones.Count > 0
                _importar.Enabled = Not _ultimaVista.HayErrores AndAlso _ultimaVista.Filas.Any(Function(f) f.Estado = EstadoFilaImportacion.Nueva)
            End Sub)
    End Sub

    Private Sub VistaPreviaRecetas()
        _ultimaVista = Nothing
        _observaciones.Enabled = False
        Ui.Ejecutar(Me,
            Sub()
                _vistaRecetas = _recetas.VistaPrevia(_texto)
                Ui.Mostrar(_filas, _vistaRecetas.Filas, "Numero|Fila", "Estado|Estado", "Detalle|Detalle")
                _resumen.Text = "Vista previa de recetas: " & _vistaRecetas.Resumen
                _importar.Enabled = Not _vistaRecetas.HayErrores AndAlso _vistaRecetas.RecetasNuevas > 0
            End Sub)
    End Sub

    Private Sub ImportarRecetas()
        If Not Ui.Confirmar(Me, "Se importara: " & _vistaRecetas.Resumen & Environment.NewLine & "Desea continuar?") Then Return
        Dim aprobar = _sesion.Tiene(Dominio.Seguridad.Permisos.RecetasAprobar) AndAlso
                      MessageBox.Show(Me, "Aprobar las recetas nuevas? (Si responde No quedan en borrador para revisarlas)", Text,
                                      MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes
        If Ui.Ejecutar(Me, Sub() _vistaRecetas = _recetas.Aplicar(_texto, aprobar)) Then
            Ui.Informar(Me, "Importacion terminada. " & _vistaRecetas.Resumen)
        End If
        VistaPrevia()
    End Sub

    Private Sub Importar()
        If _vistaRecetas IsNot Nothing Then ImportarRecetas() : Return
        If _ultimaVista Is Nothing OrElse Not Ui.Confirmar(Me, "Se importara: " & _ultimaVista.Resumen & Environment.NewLine & "Desea continuar?") Then Return
        If Ui.Ejecutar(Me, Sub() _ultimaVista = If(ConversorSgp.EsListadoSgp(_texto), _servicio.AplicarSgp(_texto), _servicio.Aplicar(_texto, crearUnidadesBase:=True))) Then
            Ui.Informar(Me, "Importacion terminada. " & _ultimaVista.Resumen)
        End If
        VistaPrevia()
    End Sub
End Class
