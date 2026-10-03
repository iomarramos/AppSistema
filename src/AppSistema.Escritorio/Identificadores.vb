Imports System.Globalization
Imports System.Reflection
Imports System.Text
Imports System.Windows.Forms

''' <summary>
''' Identificadores estables de los controles (Name y AccessibleName) para la automatización de interfaz (UI Automation,
''' FlaUI). Las pruebas E2E buscan por AutomationId (= Name), nunca por posición, coordenadas ni índice. Reglas:
''' botones "btn" + texto (Imprimir stock... → btnImprimirStock); opciones de menú "mnu" + texto; campos de un formulario
''' con el prefijo del tipo + el nombre del campo (_productos → gridProductos, _buscar → txtBuscar); ventanas con el
''' nombre de su clase (FormStock).
''' </summary>
Public Module Identificadores

    ''' <summary>Prefijo + texto en PascalCase, sin tildes, "&amp;" ni signos: ("btn", "Imprimir stock...") → "btnImprimirStock".</summary>
    Public Function DesdeTexto(prefijo As String, texto As String) As String
        Dim limpio = SinTildes(If(texto, "").Replace("&", ""))
        Dim sb As New StringBuilder(prefijo)
        For Each palabra In limpio.Split(limpio.Where(Function(c) Not Char.IsLetterOrDigit(c)).Distinct().ToArray(), StringSplitOptions.RemoveEmptyEntries)
            sb.Append(Char.ToUpperInvariant(palabra(0))).Append(palabra.Substring(1))
        Next
        Return sb.ToString()
    End Function

    ''' <summary>Texto visible sin "&amp;" ni puntos suspensivos, para AccessibleName.</summary>
    Public Function NombreAccesible(texto As String) As String
        Return If(texto, "").Replace("&", "").TrimEnd("."c).Trim()
    End Function

    ''' <summary>
    ''' Pone nombre a la ventana (su clase) y a los controles guardados en campos del formulario que aún no lo tienen
    ''' (prefijo del tipo + nombre del campo). Se llama al aplicar el tema, una vez por ventana.
    ''' </summary>
    Public Sub NombrarCampos(f As Form)
        If String.IsNullOrEmpty(f.Name) Then f.Name = f.GetType().Name
        For Each campo In f.GetType().GetFields(BindingFlags.Instance Or BindingFlags.NonPublic Or BindingFlags.Public)
            If Not GetType(Control).IsAssignableFrom(campo.FieldType) Then Continue For
            Dim c = TryCast(campo.GetValue(f), Control)
            If c Is Nothing OrElse Not String.IsNullOrEmpty(c.Name) Then Continue For
            c.Name = DesdeTexto(Prefijo(c), campo.Name.TrimStart("_"c))
        Next
    End Sub

    Public Function Prefijo(c As Control) As String
        Select Case True
            Case TypeOf c Is DataGridView : Return "grid"
            Case TypeOf c Is TextBox : Return "txt"
            Case TypeOf c Is ComboBox : Return "cmb"
            Case TypeOf c Is DateTimePicker : Return "fecha"
            Case TypeOf c Is CheckBox : Return "chk"
            Case TypeOf c Is NumericUpDown : Return "num"
            Case TypeOf c Is Button : Return "btn"
            Case TypeOf c Is Label : Return "lbl"
            Case TypeOf c Is TabControl : Return "tabs"
            Case Else : Return "ctl"
        End Select
    End Function

    Private Function SinTildes(texto As String) As String
        Dim sb As New StringBuilder()
        For Each ch In texto.Normalize(NormalizationForm.FormD)
            If CharUnicodeInfo.GetUnicodeCategory(ch) <> UnicodeCategory.NonSpacingMark Then sb.Append(ch)
        Next
        Return sb.ToString().Normalize(NormalizationForm.FormC)
    End Function

End Module
