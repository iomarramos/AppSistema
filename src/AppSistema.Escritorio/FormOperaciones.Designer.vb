<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormOperaciones
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
        Me.gridOperaciones = New System.Windows.Forms.DataGridView()
        Me.barraOperaciones = New System.Windows.Forms.FlowLayoutPanel()
        Me.gridAlmacenes = New System.Windows.Forms.DataGridView()
        Me.barraAlmacenes = New System.Windows.Forms.FlowLayoutPanel()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divide.Panel1.SuspendLayout()
        Me.divide.Panel2.SuspendLayout()
        Me.divide.SuspendLayout()
        CType(Me.gridOperaciones, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridAlmacenes, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'lblDescripcion
        '
        Me.lblDescripcion.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblDescripcion.Height = 34
        Me.lblDescripcion.Name = "lblDescripcion"
        Me.lblDescripcion.Padding = New System.Windows.Forms.Padding(4)
        Me.lblDescripcion.Text = "Una operacion es una sede o unidad de servicio. Despues de crearla, asigne usuarios con rol en esa operacion (Usuarios y roles) para que puedan entrar a ella."
        '
        'divide
        '
        Me.divide.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divide.Name = "divide"
        Me.divide.Panel1.Controls.Add(Me.gridOperaciones)
        Me.divide.Panel1.Controls.Add(Me.barraOperaciones)
        Me.divide.Panel2.Controls.Add(Me.gridAlmacenes)
        Me.divide.Panel2.Controls.Add(Me.barraAlmacenes)
        '
        'gridOperaciones
        '
        Me.gridOperaciones.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridOperaciones.Name = "gridOperaciones"
        Me.gridOperaciones.AccessibleName = "Operaciones"
        '
        'barraOperaciones
        '
        Me.barraOperaciones.AutoSize = True
        Me.barraOperaciones.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraOperaciones.Name = "barraOperaciones"
        '
        'gridAlmacenes
        '
        Me.gridAlmacenes.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridAlmacenes.Name = "gridAlmacenes"
        Me.gridAlmacenes.AccessibleName = "Almacenes"
        '
        'barraAlmacenes
        '
        Me.barraAlmacenes.AutoSize = True
        Me.barraAlmacenes.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraAlmacenes.Name = "barraAlmacenes"
        '
        'FormOperaciones
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.divide)
        Me.Controls.Add(Me.lblDescripcion)
        Me.Name = "FormOperaciones"
        Me.Text = "Operaciones y almacenes"
        Me.divide.Panel1.ResumeLayout(False)
        Me.divide.Panel1.PerformLayout()
        Me.divide.Panel2.ResumeLayout(False)
        Me.divide.Panel2.PerformLayout()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divide.ResumeLayout(False)
        CType(Me.gridOperaciones, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridAlmacenes, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents lblDescripcion As System.Windows.Forms.Label
    Friend WithEvents divide As System.Windows.Forms.SplitContainer
    Friend WithEvents gridOperaciones As System.Windows.Forms.DataGridView
    Friend WithEvents barraOperaciones As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents gridAlmacenes As System.Windows.Forms.DataGridView
    Friend WithEvents barraAlmacenes As System.Windows.Forms.FlowLayoutPanel
End Class
