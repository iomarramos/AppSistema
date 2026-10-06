<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormImportacion
    Inherits System.Windows.Forms.Form
    Private components As System.ComponentModel.IContainer
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then components.Dispose()
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.gridFilas = New System.Windows.Forms.DataGridView()
        Me.lblResumen = New System.Windows.Forms.Label()
        Me.barraAcciones = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblArchivo = New System.Windows.Forms.Label()
        CType(Me.gridFilas, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraAcciones.SuspendLayout()
        Me.SuspendLayout()
        '
        'gridFilas
        '
        Me.gridFilas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridFilas.Name = "gridFilas"
        Me.gridFilas.AccessibleName = "Filas"
        '
        'lblResumen
        '
        Me.lblResumen.AutoSize = False
        Me.lblResumen.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lblResumen.Height = 40
        Me.lblResumen.Name = "lblResumen"
        Me.lblResumen.Padding = New System.Windows.Forms.Padding(6)
        '
        'barraAcciones
        '
        Me.barraAcciones.AutoSize = True
        Me.barraAcciones.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraAcciones.Name = "barraAcciones"
        '
        'lblArchivo
        '
        Me.lblArchivo.AutoSize = True
        Me.lblArchivo.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblArchivo.Name = "lblArchivo"
        Me.lblArchivo.Text = "(sin archivo)"
        '
        'FormImportacion
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.gridFilas)
        Me.Controls.Add(Me.lblResumen)
        Me.Controls.Add(Me.barraAcciones)
        Me.Name = "FormImportacion"
        Me.Text = "Importar"
        CType(Me.gridFilas, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraAcciones.ResumeLayout(False)
        Me.barraAcciones.PerformLayout()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents gridFilas As System.Windows.Forms.DataGridView
    Friend WithEvents lblResumen As System.Windows.Forms.Label
    Friend WithEvents barraAcciones As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblArchivo As System.Windows.Forms.Label
End Class
