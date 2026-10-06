Imports System.Drawing
Imports System.Runtime.ExceptionServices
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.Windows.Forms
Imports AppSistema.Escritorio
Imports Xunit

''' <summary>Controles reales en STA, sin conexión a una base ni credenciales.</summary>
Public Class FactInterfazAttribute
    Inherits FactAttribute

    Public Sub New()
        If Not RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then Skip = "Requiere WinForms en Windows."
    End Sub
End Class

Public Class UsabilidadTests
    Public Class FilaPrueba
        Public Property Producto As String
        Public Property CostoU6 As Long?
        Public Property Estado As String
    End Class
    Private Shared Sub Capturar(formulario As Form, nombre As String)
        If Environment.GetEnvironmentVariable("APPSISTEMA_UI_CAPTURAS") <> "1" Then Return
        formulario.Show()
        Try
            formulario.PerformLayout()
            Application.DoEvents()
            Using imagen As New Bitmap(formulario.Width, formulario.Height)
                formulario.DrawToBitmap(imagen, New Rectangle(Point.Empty, imagen.Size))
                imagen.Save(IO.Path.Combine(EntornoPrueba.Artefactos, "screenshots", nombre & ".png"), Imaging.ImageFormat.Png)
            End Using
        Finally
            formulario.Hide()
        End Try
    End Sub
    Private Shared Sub EnSta(accion As Action)
        Dim errorCapturado As Exception = Nothing
        Dim hilo As New Thread(Sub()
                                   Try
                                       accion()
                                   Catch ex As Exception
                                       errorCapturado = ex
                                   End Try
                               End Sub)
        hilo.SetApartmentState(ApartmentState.STA)
        hilo.Start()
        Assert.True(hilo.Join(TimeSpan.FromSeconds(20)), "La prueba de controles no terminó.")
        If errorCapturado IsNot Nothing Then ExceptionDispatchInfo.Capture(errorCapturado).Throw()
    End Sub

    <FactInterfaz>
    Public Sub ContextoNoDuplicaControlesNiSolapaTituloYOperacion()
        EnSta(Sub()
                  Using f As New Form With {.ClientSize = New Size(640, 400), .Text = "Pantalla con un título muy largo - Operación"}
                      Tema.AplicarVentana(f, "Operación con nombre muy largo")
                      Tema.AplicarVentana(f, "Otra operación")
                      f.CreateControl()
                      f.PerformLayout()
                      Dim barra = Assert.IsType(Of TableLayoutPanel)(Assert.Single(f.Controls.Cast(Of Control)()))
                      barra.PerformLayout()
                      Dim titulo = barra.Controls("lblPantalla")
                      Dim operacion = barra.Controls("lblOperacion")
                      Assert.False(titulo.Bounds.IntersectsWith(operacion.Bounds))
                      Assert.True(titulo.Width > 0)
                      Assert.True(operacion.Width > 0)
                      Capturar(f, "ui-contexto")
                  End Using
              End Sub)
    End Sub

    <FactInterfaz>
    Public Sub AccionNoSeRepiteYBotonSeRecuperaTrasUnError()
        EnSta(Sub()
                  Dim llamadas = 0
                  Dim boton As Button = Nothing
                  boton = Ui.Boton("Guardar", Sub()
                                                  llamadas += 1
                                                  boton.PerformClick()
                                                  Throw New InvalidOperationException("Error de prueba")
                                              End Sub)
                  Using boton
                      Assert.Throws(Of InvalidOperationException)(Sub() boton.PerformClick())
                      Assert.Equal(1, llamadas)
                      Assert.True(boton.Enabled)
                      Assert.Equal("btnGuardar", boton.Name)
                  End Using
              End Sub)
    End Sub

    <FactInterfaz>
    Public Sub TablaVaciaYEstadoEnColumnaEstrechaSePintanSinError()
        EnSta(Sub()
                  Using f As New Form With {.ClientSize = New Size(640, 300)}, g = Ui.NuevaGrilla()
                      f.Controls.Add(g)
                      f.CreateControl()
                      g.CreateControl()
                      Using imagen As New Bitmap(640, 300)
                          g.DrawToBitmap(imagen, New Rectangle(0, 0, 640, 300))
                          g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
                          g.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Estado", .DataPropertyName = "Estado", .Width = 20, .MinimumWidth = 2})
                          g.Rows.Add("aprobada")
                          g.DrawToBitmap(imagen, New Rectangle(0, 0, 640, 300))
                      End Using
                  End Using
              End Sub)
    End Sub

    <FactInterfaz>
    Public Sub DialogoConservaClaveLiteralYOrdenDeTeclado()
        EnSta(Sub()
                  Using d As New DialogoCampos("Campos")
                      d.Texto("usuario", "Usuario").Texto("clave", "Clave", "  clave  ", esClave:=True)
                      Tema.Aplicar(d)
                      Dim campos = d.Controls.OfType(Of TableLayoutPanel)().Single().Controls.OfType(Of TextBox)().OrderBy(Function(c) c.TabIndex).ToArray()
                      Assert.Equal("txtUsuario", campos(0).Name)
                      Assert.Equal("txtClave", campos(1).Name)
                      Assert.True(campos(1).UseSystemPasswordChar)
                      Assert.Equal("  clave  ", d.ValorSinRecortar("clave"))
                      Assert.NotNull(d.AcceptButton)
                      Assert.NotNull(d.CancelButton)
                      Capturar(d, "ui-dialogo")
                  End Using
              End Sub)
    End Sub

    <FactInterfaz>
    Public Sub AyudaSeAjustaAlAnchoYAccesoConservaSusControles()
        EnSta(Sub()
                  Using f As New Form With {.ClientSize = New Size(640, 400)}, ayuda As New Label With {
                      .Text = String.Concat(Enumerable.Repeat("Seleccione el almacén y revise las cantidades antes de continuar. ", 4)),
                      .Dock = DockStyle.Top, .AutoSize = False}
                      f.Controls.Add(ayuda)
                      Tema.Aplicar(f)
                      f.CreateControl()
                      f.PerformLayout()
                      Dim altoAncho = ayuda.Height
                      f.ClientSize = New Size(320, 400)
                      f.PerformLayout()
                      Assert.True(ayuda.Height > altoAncho, "La ayuda debe crecer cuando la ventana se estrecha.")
                  End Using
                  Using acceso As New FormAcceso()
                      Tema.Aplicar(acceso)
                      Dim entrar = acceso.Controls.Find("btnEntrar", True).Single()
                      Assert.Equal(Tema.Acento, entrar.BackColor)
                      Assert.True(acceso.Controls.Find("txtClave", True).OfType(Of TextBox)().Single().UseSystemPasswordChar)
                      Capturar(acceso, "ui-acceso")
                  End Using
              End Sub)
    End Sub

    <FactInterfaz>
    Public Sub TablaConservaCostoPendienteYTextoDeEstado()
        EnSta(Sub()
                  Using f As New Form With {.ClientSize = New Size(800, 340)}, g = Ui.NuevaGrilla()
                      f.Controls.Add(g)
                      Tema.AplicarVentana(f, "Operación de prueba")
                      f.Show()
                      Try
                          Ui.Mostrar(g, New List(Of FilaPrueba) From {
                              New FilaPrueba With {.Producto = "Producto de prueba sin precio", .CostoU6 = Nothing, .Estado = "pendiente"},
                              New FilaPrueba With {.Producto = "Producto de prueba aprobado", .CostoU6 = 12500000L, .Estado = "aprobada"}},
                              "Producto|Producto", "CostoU6|Costo (S/)", "Estado|Estado")
                          Assert.Equal("pendiente", g.Rows(0).Cells("CostoU6").FormattedValue)
                          Assert.Equal(Ui.Dinero(12500000L), g.Rows(1).Cells("CostoU6").FormattedValue)
                          Assert.Equal("aprobada", g.Rows(1).Cells("Estado").FormattedValue)
                          Assert.True(g.Columns("Producto").Width > g.Columns("Estado").Width)
                          Capturar(f, "ui-tabla")
                      Finally
                          f.Hide()
                      End Try
                  End Using
              End Sub)
    End Sub
End Class
