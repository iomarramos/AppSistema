<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormCompras
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
        Me.barraAlmacen = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblAlmacen = New System.Windows.Forms.Label()
        Me.cmbAlmacen = New System.Windows.Forms.ComboBox()
        Me.tabsCompras = New System.Windows.Forms.TabControl()
        Me.tabPrevision = New System.Windows.Forms.TabPage()
        Me.divPrevision = New System.Windows.Forms.SplitContainer()
        Me.gridPrevisiones = New System.Windows.Forms.DataGridView()
        Me.gridDesglose = New System.Windows.Forms.DataGridView()
        Me.lblNotaDesglose = New System.Windows.Forms.Label()
        Me.barraPrevision = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblCorte = New System.Windows.Forms.Label()
        Me.dtCorte = New System.Windows.Forms.DateTimePicker()
        Me.lblDesde = New System.Windows.Forms.Label()
        Me.dtDesde = New System.Windows.Forms.DateTimePicker()
        Me.lblHasta = New System.Windows.Forms.Label()
        Me.dtHasta = New System.Windows.Forms.DateTimePicker()
        Me.tabPedidos = New System.Windows.Forms.TabPage()
        Me.divPedidos = New System.Windows.Forms.SplitContainer()
        Me.gridPedidos = New System.Windows.Forms.DataGridView()
        Me.gridLineas = New System.Windows.Forms.DataGridView()
        Me.barraPedidos = New System.Windows.Forms.FlowLayoutPanel()
        Me.barraAlmacen.SuspendLayout()
        Me.tabsCompras.SuspendLayout()
        Me.tabPrevision.SuspendLayout()
        CType(Me.divPrevision, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divPrevision.Panel1.SuspendLayout()
        Me.divPrevision.Panel2.SuspendLayout()
        Me.divPrevision.SuspendLayout()
        CType(Me.gridPrevisiones, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridDesglose, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraPrevision.SuspendLayout()
        Me.tabPedidos.SuspendLayout()
        CType(Me.divPedidos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divPedidos.Panel1.SuspendLayout()
        Me.divPedidos.Panel2.SuspendLayout()
        Me.divPedidos.SuspendLayout()
        CType(Me.gridPedidos, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridLineas, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraPedidos.SuspendLayout()
        Me.SuspendLayout()
        '
        'barraAlmacen
        '
        Me.barraAlmacen.AutoSize = True
        Me.barraAlmacen.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraAlmacen.Name = "barraAlmacen"
        Me.barraAlmacen.Padding = New System.Windows.Forms.Padding(4)
        Me.barraAlmacen.WrapContents = True
        Me.barraAlmacen.Controls.Add(Me.lblAlmacen)
        Me.barraAlmacen.Controls.Add(Me.cmbAlmacen)
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
        'tabsCompras
        '
        Me.tabsCompras.Controls.Add(Me.tabPrevision)
        Me.tabsCompras.Controls.Add(Me.tabPedidos)
        Me.tabsCompras.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tabsCompras.Name = "tabsCompras"
        '
        'tabPrevision
        '
        Me.tabPrevision.Controls.Add(Me.divPrevision)
        Me.tabPrevision.Controls.Add(Me.barraPrevision)
        Me.tabPrevision.Name = "tabPrevision"
        Me.tabPrevision.Text = "Prevision"
        '
        'divPrevision
        '
        Me.divPrevision.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divPrevision.Name = "divPrevision"
        Me.divPrevision.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divPrevision.SplitterDistance = 120
        Me.divPrevision.Panel1.Controls.Add(Me.gridPrevisiones)
        Me.divPrevision.Panel2.Controls.Add(Me.gridDesglose)
        Me.divPrevision.Panel2.Controls.Add(Me.lblNotaDesglose)
        '
        'gridPrevisiones
        '
        Me.gridPrevisiones.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridPrevisiones.Name = "gridPrevisiones"
        Me.gridPrevisiones.AccessibleName = "Previsiones"
        '
        'gridDesglose
        '
        Me.gridDesglose.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridDesglose.Name = "gridDesglose"
        Me.gridDesglose.AccessibleName = "Desglose"
        '
        'lblNotaDesglose
        '
        Me.lblNotaDesglose.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblNotaDesglose.Height = 36
        Me.lblNotaDesglose.Name = "lblNotaDesglose"
        Me.lblNotaDesglose.Padding = New System.Windows.Forms.Padding(4)
        Me.lblNotaDesglose.Text = "Necesidad = max(0, mayor faltante por fecha, reserva - saldo final). Quiebre: primera fecha sin stock si no se compra (lo que llega despues no la cubre)."
        '
        'barraPrevision
        '
        Me.barraPrevision.AutoSize = True
        Me.barraPrevision.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraPrevision.Name = "barraPrevision"
        Me.barraPrevision.Padding = New System.Windows.Forms.Padding(4)
        Me.barraPrevision.WrapContents = True
        Me.barraPrevision.Controls.Add(Me.lblCorte)
        Me.barraPrevision.Controls.Add(Me.dtCorte)
        Me.barraPrevision.Controls.Add(Me.lblDesde)
        Me.barraPrevision.Controls.Add(Me.dtDesde)
        Me.barraPrevision.Controls.Add(Me.lblHasta)
        Me.barraPrevision.Controls.Add(Me.dtHasta)
        '
        'lblCorte
        '
        Me.lblCorte.AutoSize = True
        Me.lblCorte.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblCorte.Name = "lblCorte"
        Me.lblCorte.Text = "Corte"
        '
        'dtCorte
        '
        Me.dtCorte.Format = System.Windows.Forms.DateTimePickerFormat.Short
        Me.dtCorte.Name = "dtCorte"
        Me.dtCorte.AccessibleName = "Corte"
        Me.dtCorte.Width = 105
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
        Me.dtDesde.Width = 105
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
        Me.dtHasta.Width = 105
        '
        'tabPedidos
        '
        Me.tabPedidos.Controls.Add(Me.divPedidos)
        Me.tabPedidos.Controls.Add(Me.barraPedidos)
        Me.tabPedidos.Name = "tabPedidos"
        Me.tabPedidos.Text = "Pedidos"
        '
        'divPedidos
        '
        Me.divPedidos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divPedidos.Name = "divPedidos"
        Me.divPedidos.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divPedidos.Panel1.Controls.Add(Me.gridPedidos)
        Me.divPedidos.Panel2.Controls.Add(Me.gridLineas)
        '
        'gridPedidos
        '
        Me.gridPedidos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridPedidos.Name = "gridPedidos"
        Me.gridPedidos.AccessibleName = "Pedidos"
        '
        'gridLineas
        '
        Me.gridLineas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridLineas.Name = "gridLineas"
        Me.gridLineas.AccessibleName = "Lineas"
        '
        'barraPedidos
        '
        Me.barraPedidos.AutoSize = True
        Me.barraPedidos.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraPedidos.Name = "barraPedidos"
        Me.barraPedidos.Padding = New System.Windows.Forms.Padding(4)
        Me.barraPedidos.WrapContents = True
        '
        'FormCompras
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.tabsCompras)
        Me.Controls.Add(Me.barraAlmacen)
        Me.Name = "FormCompras"
        Me.Text = "Compras"
        Me.barraAlmacen.ResumeLayout(False)
        Me.barraAlmacen.PerformLayout()
        Me.tabsCompras.ResumeLayout(False)
        Me.tabPrevision.ResumeLayout(False)
        Me.divPrevision.Panel1.ResumeLayout(False)
        Me.divPrevision.Panel2.ResumeLayout(False)
        Me.divPrevision.Panel2.PerformLayout()
        CType(Me.divPrevision, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divPrevision.ResumeLayout(False)
        CType(Me.gridPrevisiones, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridDesglose, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraPrevision.ResumeLayout(False)
        Me.barraPrevision.PerformLayout()
        Me.tabPedidos.ResumeLayout(False)
        Me.divPedidos.Panel1.ResumeLayout(False)
        Me.divPedidos.Panel2.ResumeLayout(False)
        CType(Me.divPedidos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divPedidos.ResumeLayout(False)
        CType(Me.gridPedidos, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridLineas, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraPedidos.ResumeLayout(False)
        Me.barraPedidos.PerformLayout()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents barraAlmacen As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblAlmacen As System.Windows.Forms.Label
    Friend WithEvents cmbAlmacen As System.Windows.Forms.ComboBox
    Friend WithEvents tabsCompras As System.Windows.Forms.TabControl
    Friend WithEvents tabPrevision As System.Windows.Forms.TabPage
    Friend WithEvents divPrevision As System.Windows.Forms.SplitContainer
    Friend WithEvents gridPrevisiones As System.Windows.Forms.DataGridView
    Friend WithEvents gridDesglose As System.Windows.Forms.DataGridView
    Friend WithEvents lblNotaDesglose As System.Windows.Forms.Label
    Friend WithEvents barraPrevision As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblCorte As System.Windows.Forms.Label
    Friend WithEvents dtCorte As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblDesde As System.Windows.Forms.Label
    Friend WithEvents dtDesde As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblHasta As System.Windows.Forms.Label
    Friend WithEvents dtHasta As System.Windows.Forms.DateTimePicker
    Friend WithEvents tabPedidos As System.Windows.Forms.TabPage
    Friend WithEvents divPedidos As System.Windows.Forms.SplitContainer
    Friend WithEvents gridPedidos As System.Windows.Forms.DataGridView
    Friend WithEvents gridLineas As System.Windows.Forms.DataGridView
    Friend WithEvents barraPedidos As System.Windows.Forms.FlowLayoutPanel
End Class
