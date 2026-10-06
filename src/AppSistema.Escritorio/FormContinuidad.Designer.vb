<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormContinuidad
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
        Me.gridSedes = New System.Windows.Forms.DataGridView()
        Me.barraCentral = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblCentral = New System.Windows.Forms.Label()
        Me.lblEstado = New System.Windows.Forms.Label()
        Me.barraSede = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblSede = New System.Windows.Forms.Label()
        Me.lblAviso = New System.Windows.Forms.Label()
        CType(Me.gridSedes, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraCentral.SuspendLayout()
        Me.barraSede.SuspendLayout()
        Me.SuspendLayout()
        '
        'gridSedes
        '
        Me.gridSedes.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridSedes.Name = "gridSedes"
        Me.gridSedes.AccessibleName = "Sedes"
        '
        'barraCentral
        '
        Me.barraCentral.AutoSize = True
        Me.barraCentral.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraCentral.Name = "barraCentral"
        Me.barraCentral.Padding = New System.Windows.Forms.Padding(4)
        Me.barraCentral.WrapContents = True
        Me.barraCentral.Controls.Add(Me.lblCentral)
        '
        'lblCentral
        '
        Me.lblCentral.AutoSize = True
        Me.lblCentral.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblCentral.Name = "lblCentral"
        Me.lblCentral.Text = "Central:"
        '
        'lblEstado
        '
        Me.lblEstado.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblEstado.Height = 64
        Me.lblEstado.Name = "lblEstado"
        Me.lblEstado.Padding = New System.Windows.Forms.Padding(6)
        '
        'barraSede
        '
        Me.barraSede.AutoSize = True
        Me.barraSede.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraSede.Name = "barraSede"
        Me.barraSede.Padding = New System.Windows.Forms.Padding(4)
        Me.barraSede.WrapContents = True
        Me.barraSede.Controls.Add(Me.lblSede)
        '
        'lblSede
        '
        Me.lblSede.AutoSize = True
        Me.lblSede.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblSede.Name = "lblSede"
        Me.lblSede.Text = "Esta sede:"
        '
        'lblAviso
        '
        Me.lblAviso.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblAviso.Height = 50
        Me.lblAviso.Name = "lblAviso"
        Me.lblAviso.Padding = New System.Windows.Forms.Padding(4)
        Me.lblAviso.Text = "Para TI. La sincronizacion programada la hace herramientas/windows/programar_sede.ps1; aqui se revisa y se fuerza. Respaldar requiere pg_dump en esta PC (normalmente, el servidor de la sede). Restaurar y actualizar: solo con el Instalador y sin usuarios conectados."
        '
        'FormContinuidad
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.gridSedes)
        Me.Controls.Add(Me.barraCentral)
        Me.Controls.Add(Me.lblEstado)
        Me.Controls.Add(Me.barraSede)
        Me.Controls.Add(Me.lblAviso)
        Me.Name = "FormContinuidad"
        Me.Text = "Sincronizacion y respaldo"
        CType(Me.gridSedes, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraCentral.ResumeLayout(False)
        Me.barraCentral.PerformLayout()
        Me.barraSede.ResumeLayout(False)
        Me.barraSede.PerformLayout()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents gridSedes As System.Windows.Forms.DataGridView
    Friend WithEvents barraCentral As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblCentral As System.Windows.Forms.Label
    Friend WithEvents lblEstado As System.Windows.Forms.Label
    Friend WithEvents barraSede As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblSede As System.Windows.Forms.Label
    Friend WithEvents lblAviso As System.Windows.Forms.Label
End Class
