<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormInventarios
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
        Me.barraAcciones = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblAlmacen = New System.Windows.Forms.Label()
        Me.cmbAlmacen = New System.Windows.Forms.ComboBox()
        Me.chkCiego = New System.Windows.Forms.CheckBox()
        Me.lblDescripcion = New System.Windows.Forms.Label()
        Me.divide = New System.Windows.Forms.SplitContainer()
        Me.gridInventarios = New System.Windows.Forms.DataGridView()
        Me.lblResumen = New System.Windows.Forms.Label()
        Me.gridHoja = New System.Windows.Forms.DataGridView()
        Me.barraAcciones.SuspendLayout()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divide.Panel1.SuspendLayout()
        Me.divide.Panel2.SuspendLayout()
        Me.divide.SuspendLayout()
        CType(Me.gridInventarios, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridHoja, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'barraAcciones
        '
        Me.barraAcciones.AutoSize = True
        Me.barraAcciones.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraAcciones.Controls.Add(Me.lblAlmacen)
        Me.barraAcciones.Controls.Add(Me.cmbAlmacen)
        Me.barraAcciones.Controls.Add(Me.chkCiego)
        Me.barraAcciones.Name = "barraAcciones"
        '
        'lblAlmacen
        '
        Me.lblAlmacen.AutoSize = True
        Me.lblAlmacen.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblAlmacen.Name = "lblAlmacen"
        Me.lblAlmacen.Text = "Almacen"
        '
        'cmbAlmacen
        '
        Me.cmbAlmacen.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbAlmacen.Name = "cmbAlmacen"
        Me.cmbAlmacen.AccessibleName = "Almacen"
        Me.cmbAlmacen.Width = 220
        '
        'chkCiego
        '
        Me.chkCiego.AutoSize = True
        Me.chkCiego.Margin = New System.Windows.Forms.Padding(6, 8, 3, 3)
        Me.chkCiego.Name = "chkCiego"
        Me.chkCiego.AccessibleName = "Ciego"
        Me.chkCiego.Text = "Conteo ciego"
        '
        'lblDescripcion
        '
        Me.lblDescripcion.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblDescripcion.Height = 34
        Me.lblDescripcion.Name = "lblDescripcion"
        Me.lblDescripcion.Padding = New System.Windows.Forms.Padding(4)
        Me.lblDescripcion.Text = "Mientras se cuenta, esos productos no admiten movimientos. Celda vacia = sin contar (no es cero). El saldo solo cambia al autorizar el ajuste."
        '
        'divide
        '
        Me.divide.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divide.Name = "divide"
        Me.divide.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divide.SplitterDistance = 140
        '
        'divide.Panel1
        '
        Me.divide.Panel1.Controls.Add(Me.gridInventarios)
        '
        'divide.Panel2
        '
        Me.divide.Panel2.Controls.Add(Me.gridHoja)
        Me.divide.Panel2.Controls.Add(Me.lblResumen)
        '
        'gridInventarios
        '
        Me.gridInventarios.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridInventarios.Name = "gridInventarios"
        Me.gridInventarios.AccessibleName = "Inventarios"
        '
        'lblResumen
        '
        Me.lblResumen.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lblResumen.Height = 28
        Me.lblResumen.Name = "lblResumen"
        Me.lblResumen.Padding = New System.Windows.Forms.Padding(6)
        '
        'gridHoja
        '
        Me.gridHoja.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridHoja.Name = "gridHoja"
        Me.gridHoja.AccessibleName = "Hoja"
        '
        'FormInventarios
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.divide)
        Me.Controls.Add(Me.lblDescripcion)
        Me.Controls.Add(Me.barraAcciones)
        Me.Name = "FormInventarios"
        Me.Text = "Inventario fisico"
        Me.barraAcciones.ResumeLayout(False)
        Me.barraAcciones.PerformLayout()
        Me.divide.Panel1.ResumeLayout(False)
        Me.divide.Panel2.ResumeLayout(False)
        Me.divide.Panel2.PerformLayout()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divide.ResumeLayout(False)
        CType(Me.gridInventarios, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridHoja, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents barraAcciones As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblAlmacen As System.Windows.Forms.Label
    Friend WithEvents cmbAlmacen As System.Windows.Forms.ComboBox
    Friend WithEvents chkCiego As System.Windows.Forms.CheckBox
    Friend WithEvents lblDescripcion As System.Windows.Forms.Label
    Friend WithEvents divide As System.Windows.Forms.SplitContainer
    Friend WithEvents gridInventarios As System.Windows.Forms.DataGridView
    Friend WithEvents lblResumen As System.Windows.Forms.Label
    Friend WithEvents gridHoja As System.Windows.Forms.DataGridView
End Class
