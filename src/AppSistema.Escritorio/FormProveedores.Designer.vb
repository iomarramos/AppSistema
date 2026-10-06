<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormProveedores
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
        Me.divide = New System.Windows.Forms.SplitContainer()
        Me.gridProveedores = New System.Windows.Forms.DataGridView()
        Me.barraProveedores = New System.Windows.Forms.FlowLayoutPanel()
        Me.divideDerecha = New System.Windows.Forms.SplitContainer()
        Me.gridEmpaques = New System.Windows.Forms.DataGridView()
        Me.barraEmpaques = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblEmpaques = New System.Windows.Forms.Label()
        Me.gridPrecios = New System.Windows.Forms.DataGridView()
        Me.barraPrecios = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblPrecios = New System.Windows.Forms.Label()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divide.Panel1.SuspendLayout()
        Me.divide.Panel2.SuspendLayout()
        Me.divide.SuspendLayout()
        CType(Me.divideDerecha, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divideDerecha.Panel1.SuspendLayout()
        Me.divideDerecha.Panel2.SuspendLayout()
        Me.divideDerecha.SuspendLayout()
        CType(Me.gridProveedores, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridEmpaques, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridPrecios, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'divide
        '
        Me.divide.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divide.Name = "divide"
        Me.divide.Panel1.Controls.Add(Me.gridProveedores)
        Me.divide.Panel1.Controls.Add(Me.barraProveedores)
        Me.divide.Panel2.Controls.Add(Me.divideDerecha)
        '
        'gridProveedores
        '
        Me.gridProveedores.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridProveedores.Name = "gridProveedores"
        Me.gridProveedores.AccessibleName = "Proveedores"
        '
        'barraProveedores
        '
        Me.barraProveedores.AutoSize = True
        Me.barraProveedores.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraProveedores.Name = "barraProveedores"
        '
        'divideDerecha
        '
        Me.divideDerecha.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divideDerecha.Name = "divideDerecha"
        Me.divideDerecha.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divideDerecha.Panel1.Controls.Add(Me.gridEmpaques)
        Me.divideDerecha.Panel1.Controls.Add(Me.barraEmpaques)
        Me.divideDerecha.Panel1.Controls.Add(Me.lblEmpaques)
        Me.divideDerecha.Panel2.Controls.Add(Me.gridPrecios)
        Me.divideDerecha.Panel2.Controls.Add(Me.barraPrecios)
        Me.divideDerecha.Panel2.Controls.Add(Me.lblPrecios)
        '
        'gridEmpaques
        '
        Me.gridEmpaques.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridEmpaques.Name = "gridEmpaques"
        Me.gridEmpaques.AccessibleName = "Empaques"
        '
        'barraEmpaques
        '
        Me.barraEmpaques.AutoSize = True
        Me.barraEmpaques.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraEmpaques.Name = "barraEmpaques"
        '
        'lblEmpaques
        '
        Me.lblEmpaques.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblEmpaques.Name = "lblEmpaques"
        Me.lblEmpaques.Padding = New System.Windows.Forms.Padding(4)
        Me.lblEmpaques.Text = "Empaques que ofrece"
        '
        'gridPrecios
        '
        Me.gridPrecios.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridPrecios.Name = "gridPrecios"
        Me.gridPrecios.AccessibleName = "Precios"
        '
        'barraPrecios
        '
        Me.barraPrecios.AutoSize = True
        Me.barraPrecios.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraPrecios.Name = "barraPrecios"
        '
        'lblPrecios
        '
        Me.lblPrecios.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblPrecios.Name = "lblPrecios"
        Me.lblPrecios.Padding = New System.Windows.Forms.Padding(4)
        Me.lblPrecios.Text = "Precios por empaque (vigencias sin superposicion)"
        '
        'FormProveedores
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.divide)
        Me.Name = "FormProveedores"
        Me.Text = "Proveedores y precios"
        Me.divide.Panel1.ResumeLayout(False)
        Me.divide.Panel1.PerformLayout()
        Me.divide.Panel2.ResumeLayout(False)
        CType(Me.divide, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divide.ResumeLayout(False)
        Me.divideDerecha.Panel1.ResumeLayout(False)
        Me.divideDerecha.Panel1.PerformLayout()
        Me.divideDerecha.Panel2.ResumeLayout(False)
        Me.divideDerecha.Panel2.PerformLayout()
        CType(Me.divideDerecha, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divideDerecha.ResumeLayout(False)
        CType(Me.gridProveedores, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridEmpaques, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridPrecios, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents divide As System.Windows.Forms.SplitContainer
    Friend WithEvents gridProveedores As System.Windows.Forms.DataGridView
    Friend WithEvents barraProveedores As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents divideDerecha As System.Windows.Forms.SplitContainer
    Friend WithEvents gridEmpaques As System.Windows.Forms.DataGridView
    Friend WithEvents barraEmpaques As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblEmpaques As System.Windows.Forms.Label
    Friend WithEvents gridPrecios As System.Windows.Forms.DataGridView
    Friend WithEvents barraPrecios As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblPrecios As System.Windows.Forms.Label
End Class
