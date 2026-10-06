<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormResultados
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
        Me.lblDescripcion = New System.Windows.Forms.Label()
        Me.divide = New System.Windows.Forms.SplitContainer()
        Me.gridResultado = New System.Windows.Forms.DataGridView()
        Me.lblEstado = New System.Windows.Forms.Label()
        Me.gridGastos = New System.Windows.Forms.DataGridView()
        Me.barraMes = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblMes = New System.Windows.Forms.Label()
        Me.dtMes = New System.Windows.Forms.DateTimePicker()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divide.Panel1.SuspendLayout()
        Me.divide.Panel2.SuspendLayout()
        Me.divide.SuspendLayout()
        CType(Me.gridResultado, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridGastos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraMes.SuspendLayout()
        Me.SuspendLayout()
        '
        'divide
        '
        Me.divide.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divide.Name = "divide"
        Me.divide.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divide.SplitterDistance = 220
        Me.divide.Panel1.Controls.Add(Me.gridResultado)
        Me.divide.Panel1.Controls.Add(Me.lblEstado)
        Me.divide.Panel2.Controls.Add(Me.gridGastos)
        '
        'gridResultado
        '
        Me.gridResultado.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridResultado.Name = "gridResultado"
        Me.gridResultado.AccessibleName = "Resultado"
        '
        'lblEstado
        '
        Me.lblEstado.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lblEstado.Height = 28
        Me.lblEstado.Name = "lblEstado"
        Me.lblEstado.Padding = New System.Windows.Forms.Padding(6)
        '
        'gridGastos
        '
        Me.gridGastos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridGastos.Name = "gridGastos"
        Me.gridGastos.AccessibleName = "Gastos"
        '
        'lblDescripcion
        '
        Me.lblDescripcion.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblDescripcion.Height = 34
        Me.lblDescripcion.Name = "lblDescripcion"
        Me.lblDescripcion.Padding = New System.Windows.Forms.Padding(4)
        Me.lblDescripcion.Text = "Margen = ingreso - alimentos consumidos - gastos reales. Los gastos presupuestados solo se comparan. Bajas y ajustes de inventario van en 'No asignado'."
        '
        'barraMes
        '
        Me.barraMes.AutoSize = True
        Me.barraMes.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraMes.Controls.Add(Me.lblMes)
        Me.barraMes.Controls.Add(Me.dtMes)
        Me.barraMes.Name = "barraMes"
        '
        'lblMes
        '
        Me.lblMes.AutoSize = True
        Me.lblMes.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblMes.Name = "lblMes"
        Me.lblMes.Text = "Mes"
        '
        'dtMes
        '
        Me.dtMes.CustomFormat = "MM/yyyy"
        Me.dtMes.Format = System.Windows.Forms.DateTimePickerFormat.Custom
        Me.dtMes.Name = "dtMes"
        Me.dtMes.AccessibleName = "Mes"
        Me.dtMes.ShowUpDown = True
        Me.dtMes.Width = 90
        '
        'FormResultados
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.divide)
        Me.Controls.Add(Me.lblDescripcion)
        Me.Controls.Add(Me.barraMes)
        Me.Name = "FormResultados"
        Me.Text = "Gastos y resultado mensual"
        Me.divide.Panel1.ResumeLayout(False)
        Me.divide.Panel1.PerformLayout()
        Me.divide.Panel2.ResumeLayout(False)
        CType(Me.divide, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divide.ResumeLayout(False)
        CType(Me.gridResultado, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridGastos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraMes.ResumeLayout(False)
        Me.barraMes.PerformLayout()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents lblDescripcion As System.Windows.Forms.Label
    Friend WithEvents divide As System.Windows.Forms.SplitContainer
    Friend WithEvents gridResultado As System.Windows.Forms.DataGridView
    Friend WithEvents lblEstado As System.Windows.Forms.Label
    Friend WithEvents gridGastos As System.Windows.Forms.DataGridView
    Friend WithEvents barraMes As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblMes As System.Windows.Forms.Label
    Friend WithEvents dtMes As System.Windows.Forms.DateTimePicker
End Class
