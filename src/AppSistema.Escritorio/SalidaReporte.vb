Imports System.Windows.Forms
Imports AppSistema.Datos

''' <summary>
''' Salida común de los reportes: abre la vista previa (formato de las capturas del SGP), que permite imprimir, exportar a
''' Excel (CSV con ';' y UTF-8 con BOM para que Excel respete las tildes), cambiar el zoom y pasar de página.
''' </summary>
Public Module SalidaReporte

    Public Sub Emitir(dueno As Form, obtener As Func(Of Reporte))
        Dim r As Reporte = Nothing
        If Not Ui.Ejecutar(dueno, Sub() r = obtener()) OrElse r Is Nothing Then Return
        VistaReporte.Mostrar(dueno, r)
    End Sub

End Module
