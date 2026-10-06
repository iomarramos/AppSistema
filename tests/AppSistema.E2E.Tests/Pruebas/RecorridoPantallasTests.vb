Imports System.IO
Imports Xunit

''' <summary>
''' Recorrido de humo: el superusuario abre cada opción de cada menú, comprueba que no salga un error y guarda una
''' captura real de la pantalla (artifacts/screenshots/NN-mnuOpcion.png). Al final lista todas las que fallaron.
''' </summary>
<Collection("E2E")>
Public Class RecorridoPantallasTests

    Private ReadOnly _bd As BaseDatosE2E

    Public Sub New(bd As BaseDatosE2E)
        _bd = bd
    End Sub

    <FactE2E>
    Public Sub El_superusuario_abre_todas_las_pantallas_sin_errores()
        Dim fallas As New List(Of String)
        Dim indice As New List(Of String)
        Using app As New AplicacionE2E(_bd)
            Assert.True(New PaginaAcceso(app).IniciarSesion(UsuariosPrueba.Dueno), app.Mensaje())
            Dim p As New PaginaPrincipal(app)
            Dim n = 1
            For Each menu In PaginaPrincipal.MenusDeTrabajo
                For Each opcion In p.Opciones(menu)
                    n += 1
                    Dim nombre = $"{n:00}-{opcion}"
                    Try
                        Dim ventana = p.Abrir(menu, opcion)
                        Dim mensaje = app.Mensaje()
                        app.Capturar(nombre)
                        If mensaje IsNot Nothing Then
                            fallas.Add($"{opcion}: {mensaje}")
                            app.CerrarMensaje()
                        ElseIf ventana Is Nothing Then
                            fallas.Add($"{opcion}: no se abrio ninguna ventana")
                        Else
                            ' Especificación: las pantallas deben caber en 1366x768 (la ventana completa, con su barra de título).
                            Dim area = ventana.BoundingRectangle
                            If area.Width > 1366 OrElse area.Height > 768 Then
                                fallas.Add($"{opcion}: la ventana mide {area.Width}x{area.Height} y no cabe en 1366x768")
                            End If
                        End If
                        indice.Add($"{nombre}.png;{opcion};{If(ventana?.AutomationId, "")}")
                        ' Cerrar lo que haya quedado abierto: diálogos primero y luego la ventana de trabajo.
                        Dim dialogo = p.Dialogo()
                        If dialogo IsNot Nothing Then p.Cerrar(dialogo)
                        For Each w In p.VentanasDeTrabajo()
                            p.Cerrar(w)
                        Next
                    Catch ex As Exception
                        app.Capturar(nombre & "-error")
                        fallas.Add($"{opcion}: {ex.Message}")
                    End Try
                Next
            Next
        End Using
        File.WriteAllLines(Path.Combine(EntornoPrueba.Artefactos, "screenshots", "indice.csv"), {"archivo;opcion;ventana"}.Concat(indice))
        Assert.True(indice.Count >= 20, $"Se esperaban al menos 20 pantallas y se recorrieron {indice.Count}.")
        Assert.True(fallas.Count = 0, "Pantallas con error:" & Environment.NewLine & String.Join(Environment.NewLine, fallas))
    End Sub

End Class
