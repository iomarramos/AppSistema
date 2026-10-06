<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormUsuarios
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
        Me.gridUsuarios = New System.Windows.Forms.DataGridView()
        Me.barraAcciones = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblDetalle = New System.Windows.Forms.Label()
        CType(Me.gridUsuarios, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'gridUsuarios
        '
        Me.gridUsuarios.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridUsuarios.Name = "gridUsuarios"
        Me.gridUsuarios.AccessibleName = "Usuarios"
        '
        'barraAcciones
        '
        Me.barraAcciones.AutoSize = True
        Me.barraAcciones.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraAcciones.Name = "barraAcciones"
        Me.barraAcciones.Padding = New System.Windows.Forms.Padding(4)
        Me.barraAcciones.WrapContents = True
        '
        'lblDetalle
        '
        Me.lblDetalle.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lblDetalle.Height = 56
        Me.lblDetalle.Name = "lblDetalle"
        Me.lblDetalle.Padding = New System.Windows.Forms.Padding(6)
        Me.lblDetalle.Text = "Elija un usuario."
        '
        'FormUsuarios
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.gridUsuarios)
        Me.Controls.Add(Me.barraAcciones)
        Me.Controls.Add(Me.lblDetalle)
        Me.Name = "FormUsuarios"
        Me.Text = "Usuarios y roles"
        CType(Me.gridUsuarios, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents gridUsuarios As System.Windows.Forms.DataGridView
    Friend WithEvents barraAcciones As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblDetalle As System.Windows.Forms.Label
End Class
