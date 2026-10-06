<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormConsolidado
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
        Me.barraFiltros = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblDesde = New System.Windows.Forms.Label()
        Me.dtDesde = New System.Windows.Forms.DateTimePicker()
        Me.lblHasta = New System.Windows.Forms.Label()
        Me.dtHasta = New System.Windows.Forms.DateTimePicker()
        Me.chkBorradores = New System.Windows.Forms.CheckBox()
        Me.lblDescripcion = New System.Windows.Forms.Label()
        Me.divide = New System.Windows.Forms.SplitContainer()
        Me.gridLineas = New System.Windows.Forms.DataGridView()
        Me.lblDetalle = New System.Windows.Forms.Label()
        Me.gridDetalle = New System.Windows.Forms.DataGridView()
        Me.lblTotal = New System.Windows.Forms.Label()
        Me.barraFiltros.SuspendLayout()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divide.Panel1.SuspendLayout()
        Me.divide.Panel2.SuspendLayout()
        Me.divide.SuspendLayout()
        CType(Me.gridLineas, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridDetalle, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'barraFiltros
        '
        Me.barraFiltros.AutoSize = True
        Me.barraFiltros.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraFiltros.Controls.Add(Me.lblDesde)
        Me.barraFiltros.Controls.Add(Me.dtDesde)
        Me.barraFiltros.Controls.Add(Me.lblHasta)
        Me.barraFiltros.Controls.Add(Me.dtHasta)
        Me.barraFiltros.Controls.Add(Me.chkBorradores)
        Me.barraFiltros.Name = "barraFiltros"
        '
        'lblDesde
        '
        Me.lblDesde.AutoSize = True
        Me.lblDesde.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblDesde.Name = "lblDesde"
        Me.lblDesde.Text = "Desde"
        '
        'dtDesde
        '
        Me.dtDesde.Format = System.Windows.Forms.DateTimePickerFormat.Short
        Me.dtDesde.Name = "dtDesde"
        Me.dtDesde.AccessibleName = "Desde"
        Me.dtDesde.Width = 110
        '
        'lblHasta
        '
        Me.lblHasta.AutoSize = True
        Me.lblHasta.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblHasta.Name = "lblHasta"
        Me.lblHasta.Text = "Hasta"
        '
        'dtHasta
        '
        Me.dtHasta.Format = System.Windows.Forms.DateTimePickerFormat.Short
        Me.dtHasta.Name = "dtHasta"
        Me.dtHasta.AccessibleName = "Hasta"
        Me.dtHasta.Width = 110
        '
        'chkBorradores
        '
        Me.chkBorradores.AutoSize = True
        Me.chkBorradores.Margin = New System.Windows.Forms.Padding(6, 8, 3, 3)
        Me.chkBorradores.Name = "chkBorradores"
        Me.chkBorradores.AccessibleName = "Borradores"
        Me.chkBorradores.Text = "Incluir minutas en borrador"
        '
        'lblDescripcion
        '
        Me.lblDescripcion.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblDescripcion.Height = 34
        Me.lblDescripcion.Name = "lblDescripcion"
        Me.lblDescripcion.Padding = New System.Windows.Forms.Padding(4)
        Me.lblDescripcion.Text = "A comprar = demanda de las minutas + reserva - stock - pendiente de recibir, por operacion. Costo con el producto activo de cada operacion, sin IGV."
        '
        'divide
        '
        Me.divide.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divide.Name = "divide"
        Me.divide.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divide.SplitterDistance = 300
        Me.divide.Panel1.Controls.Add(Me.gridLineas)
        Me.divide.Panel2.Controls.Add(Me.gridDetalle)
        Me.divide.Panel2.Controls.Add(Me.lblDetalle)
        '
        'gridLineas
        '
        Me.gridLineas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridLineas.Name = "gridLineas"
        Me.gridLineas.AccessibleName = "Lineas"
        '
        'lblDetalle
        '
        Me.lblDetalle.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblDetalle.Name = "lblDetalle"
        Me.lblDetalle.Padding = New System.Windows.Forms.Padding(4)
        Me.lblDetalle.Text = "Detalle por operacion del producto elegido"
        '
        'gridDetalle
        '
        Me.gridDetalle.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridDetalle.Name = "gridDetalle"
        Me.gridDetalle.AccessibleName = "Detalle"
        '
        'lblTotal
        '
        Me.lblTotal.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lblTotal.Height = 28
        Me.lblTotal.Name = "lblTotal"
        Me.lblTotal.Padding = New System.Windows.Forms.Padding(6)
        '
        'FormConsolidado
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.divide)
        Me.Controls.Add(Me.lblTotal)
        Me.Controls.Add(Me.lblDescripcion)
        Me.Controls.Add(Me.barraFiltros)
        Me.Name = "FormConsolidado"
        Me.Text = "Consolidado de compras"
        Me.barraFiltros.ResumeLayout(False)
        Me.barraFiltros.PerformLayout()
        Me.divide.Panel1.ResumeLayout(False)
        Me.divide.Panel2.ResumeLayout(False)
        Me.divide.Panel2.PerformLayout()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divide.ResumeLayout(False)
        CType(Me.gridLineas, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridDetalle, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents barraFiltros As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblDesde As System.Windows.Forms.Label
    Friend WithEvents dtDesde As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblHasta As System.Windows.Forms.Label
    Friend WithEvents dtHasta As System.Windows.Forms.DateTimePicker
    Friend WithEvents chkBorradores As System.Windows.Forms.CheckBox
    Friend WithEvents lblDescripcion As System.Windows.Forms.Label
    Friend WithEvents divide As System.Windows.Forms.SplitContainer
    Friend WithEvents gridLineas As System.Windows.Forms.DataGridView
    Friend WithEvents lblDetalle As System.Windows.Forms.Label
    Friend WithEvents gridDetalle As System.Windows.Forms.DataGridView
    Friend WithEvents lblTotal As System.Windows.Forms.Label
End Class
