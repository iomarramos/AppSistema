<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormStock
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
        Me.lblAlmacen = New System.Windows.Forms.Label()
        Me.cmbAlmacen = New System.Windows.Forms.ComboBox()
        Me.lblBuscar = New System.Windows.Forms.Label()
        Me.txtBuscar = New System.Windows.Forms.TextBox()
        Me.barraAcciones = New System.Windows.Forms.FlowLayoutPanel()
        Me.gridSaldos = New System.Windows.Forms.DataGridView()
        Me.lblTotal = New System.Windows.Forms.Label()
        Me.barraFiltros.SuspendLayout()
        Me.barraAcciones.SuspendLayout()
        CType(Me.gridSaldos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'barraFiltros
        '
        Me.barraFiltros.AutoSize = True
        Me.barraFiltros.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraFiltros.Controls.Add(Me.lblAlmacen)
        Me.barraFiltros.Controls.Add(Me.cmbAlmacen)
        Me.barraFiltros.Controls.Add(Me.lblBuscar)
        Me.barraFiltros.Controls.Add(Me.txtBuscar)
        Me.barraFiltros.Name = "barraFiltros"
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
        'lblBuscar
        '
        Me.lblBuscar.AutoSize = True
        Me.lblBuscar.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblBuscar.Name = "lblBuscar"
        Me.lblBuscar.Text = "Buscar"
        '
        'txtBuscar
        '
        Me.txtBuscar.Name = "txtBuscar"
        Me.txtBuscar.AccessibleName = "Buscar"
        Me.txtBuscar.Width = 200
        '
        'barraAcciones
        '
        Me.barraAcciones.AutoSize = True
        Me.barraAcciones.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraAcciones.Name = "barraAcciones"
        '
        'gridSaldos
        '
        Me.gridSaldos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridSaldos.Name = "gridSaldos"
        Me.gridSaldos.AccessibleName = "Saldos"
        '
        'lblTotal
        '
        Me.lblTotal.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lblTotal.Height = 28
        Me.lblTotal.Name = "lblTotal"
        Me.lblTotal.Padding = New System.Windows.Forms.Padding(6)
        '
        'FormStock
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.gridSaldos)
        Me.Controls.Add(Me.lblTotal)
        Me.Controls.Add(Me.barraAcciones)
        Me.Controls.Add(Me.barraFiltros)
        Me.Name = "FormStock"
        Me.Text = "Stock e inventario inicial"
        Me.barraFiltros.ResumeLayout(False)
        Me.barraFiltros.PerformLayout()
        Me.barraAcciones.ResumeLayout(False)
        Me.barraAcciones.PerformLayout()
        CType(Me.gridSaldos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents barraFiltros As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblAlmacen As System.Windows.Forms.Label
    Friend WithEvents cmbAlmacen As System.Windows.Forms.ComboBox
    Friend WithEvents lblBuscar As System.Windows.Forms.Label
    Friend WithEvents txtBuscar As System.Windows.Forms.TextBox
    Friend WithEvents barraAcciones As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents gridSaldos As System.Windows.Forms.DataGridView
    Friend WithEvents lblTotal As System.Windows.Forms.Label
End Class
