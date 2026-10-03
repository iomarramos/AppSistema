Imports System.Windows.Forms

Public Module Programa

    <STAThread>
    Public Sub Main()
        Application.SetHighDpiMode(HighDpiMode.SystemAware)
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        AddHandler Application.ThreadException, Sub(s, e) Ui.MostrarError(Nothing, e.Exception)
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException)
        Application.Run(New FormPrincipal())
    End Sub

End Module
