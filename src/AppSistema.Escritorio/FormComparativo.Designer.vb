<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormComparativo
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
        Me.gridResumen = New System.Windows.Forms.DataGridView()
        Me.divideAbajo = New System.Windows.Forms.SplitContainer()
        Me.gridComponentes = New System.Windows.Forms.DataGridView()
        Me.gridProductos = New System.Windows.Forms.DataGridView()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divide.Panel1.SuspendLayout()
        Me.divide.Panel2.SuspendLayout()
        Me.divide.SuspendLayout()
        CType(Me.divideAbajo, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divideAbajo.Panel1.SuspendLayout()
        Me.divideAbajo.Panel2.SuspendLayout()
        Me.divideAbajo.SuspendLayout()
        CType(Me.gridResumen, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridComponentes, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridProductos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'divide
        '
        Me.divide.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divide.Name = "divide"
        Me.divide.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divide.SplitterDistance = 230
        Me.divide.Panel1.Controls.Add(Me.gridResumen)
        Me.divide.Panel2.Controls.Add(Me.divideAbajo)
        '
        'gridResumen
        '
        Me.gridResumen.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridResumen.Name = "gridResumen"
        Me.gridResumen.AccessibleName = "Resumen"
        '
        'divideAbajo
        '
        Me.divideAbajo.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divideAbajo.Name = "divideAbajo"
        Me.divideAbajo.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divideAbajo.Panel1.Controls.Add(Me.gridComponentes)
        Me.divideAbajo.Panel2.Controls.Add(Me.gridProductos)
        '
        'gridComponentes
        '
        Me.gridComponentes.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridComponentes.Name = "gridComponentes"
        Me.gridComponentes.AccessibleName = "Componentes"
        '
        'gridProductos
        '
        Me.gridProductos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridProductos.Name = "gridProductos"
        Me.gridProductos.AccessibleName = "Productos"
        '
        'lblDescripcion
        '
        Me.lblDescripcion.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblDescripcion.Height = 34
        Me.lblDescripcion.Name = "lblDescripcion"
        Me.lblDescripcion.Padding = New System.Windows.Forms.Padding(4)
        '
        'FormComparativo
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 700)
        Me.Controls.Add(Me.divide)
        Me.Controls.Add(Me.lblDescripcion)
        Me.Name = "FormComparativo"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Teorico vs real"
        Me.divide.Panel1.ResumeLayout(False)
        Me.divide.Panel2.ResumeLayout(False)
        CType(Me.divide, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divide.ResumeLayout(False)
        Me.divideAbajo.Panel1.ResumeLayout(False)
        Me.divideAbajo.Panel2.ResumeLayout(False)
        CType(Me.divideAbajo, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divideAbajo.ResumeLayout(False)
        CType(Me.gridResumen, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridComponentes, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridProductos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents lblDescripcion As System.Windows.Forms.Label
    Friend WithEvents divide As System.Windows.Forms.SplitContainer
    Friend WithEvents gridResumen As System.Windows.Forms.DataGridView
    Friend WithEvents divideAbajo As System.Windows.Forms.SplitContainer
    Friend WithEvents gridComponentes As System.Windows.Forms.DataGridView
    Friend WithEvents gridProductos As System.Windows.Forms.DataGridView
End Class
