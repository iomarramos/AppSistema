<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormCatalogo
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
        Me.divPrincipal = New System.Windows.Forms.SplitContainer()
        Me.panelProductos = New System.Windows.Forms.Panel()
        Me.gridProductos = New System.Windows.Forms.DataGridView()
        Me.barraProductos = New System.Windows.Forms.FlowLayoutPanel()
        Me.barraBusqueda = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblBuscar = New System.Windows.Forms.Label()
        Me.txtBuscar = New System.Windows.Forms.TextBox()
        Me.chkInactivos = New System.Windows.Forms.CheckBox()
        Me.divDerecha = New System.Windows.Forms.SplitContainer()
        Me.gridVariantes = New System.Windows.Forms.DataGridView()
        Me.barraVariantes = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblVariantes = New System.Windows.Forms.Label()
        Me.gridEmpaques = New System.Windows.Forms.DataGridView()
        Me.barraEmpaques = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblEmpaques = New System.Windows.Forms.Label()
        CType(Me.divPrincipal, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divPrincipal.Panel1.SuspendLayout()
        Me.divPrincipal.Panel2.SuspendLayout()
        Me.divPrincipal.SuspendLayout()
        Me.panelProductos.SuspendLayout()
        CType(Me.gridProductos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraProductos.SuspendLayout()
        Me.barraBusqueda.SuspendLayout()
        CType(Me.divDerecha, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divDerecha.Panel1.SuspendLayout()
        Me.divDerecha.Panel2.SuspendLayout()
        Me.divDerecha.SuspendLayout()
        CType(Me.gridVariantes, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraVariantes.SuspendLayout()
        CType(Me.gridEmpaques, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraEmpaques.SuspendLayout()
        Me.SuspendLayout()
        '
        'divPrincipal
        '
        Me.divPrincipal.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divPrincipal.Name = "divPrincipal"
        Me.divPrincipal.Panel1.Controls.Add(Me.panelProductos)
        Me.divPrincipal.Panel2.Controls.Add(Me.divDerecha)
        '
        'panelProductos
        '
        Me.panelProductos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.panelProductos.Name = "panelProductos"
        Me.panelProductos.Controls.Add(Me.gridProductos)
        Me.panelProductos.Controls.Add(Me.barraProductos)
        Me.panelProductos.Controls.Add(Me.barraBusqueda)
        '
        'gridProductos
        '
        Me.gridProductos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridProductos.Name = "gridProductos"
        Me.gridProductos.AccessibleName = "Productos"
        '
        'barraProductos
        '
        Me.barraProductos.AutoSize = True
        Me.barraProductos.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraProductos.Name = "barraProductos"
        Me.barraProductos.Padding = New System.Windows.Forms.Padding(4)
        Me.barraProductos.WrapContents = True
        '
        'barraBusqueda
        '
        Me.barraBusqueda.AutoSize = True
        Me.barraBusqueda.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraBusqueda.Name = "barraBusqueda"
        Me.barraBusqueda.Padding = New System.Windows.Forms.Padding(4)
        Me.barraBusqueda.Controls.Add(Me.lblBuscar)
        Me.barraBusqueda.Controls.Add(Me.txtBuscar)
        Me.barraBusqueda.Controls.Add(Me.chkInactivos)
        '
        'lblBuscar
        '
        Me.lblBuscar.AutoSize = True
        Me.lblBuscar.Margin = New System.Windows.Forms.Padding(3, 7, 3, 3)
        Me.lblBuscar.Name = "lblBuscar"
        Me.lblBuscar.Text = "Buscar:"
        '
        'txtBuscar
        '
        Me.txtBuscar.Name = "txtBuscar"
        Me.txtBuscar.AccessibleName = "Buscar"
        Me.txtBuscar.Width = 260
        '
        'chkInactivos
        '
        Me.chkInactivos.AutoSize = True
        Me.chkInactivos.Name = "chkInactivos"
        Me.chkInactivos.AccessibleName = "Inactivos"
        Me.chkInactivos.Text = "Incluir inactivos"
        '
        'divDerecha
        '
        Me.divDerecha.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divDerecha.Name = "divDerecha"
        Me.divDerecha.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divDerecha.Panel1.Controls.Add(Me.gridVariantes)
        Me.divDerecha.Panel1.Controls.Add(Me.barraVariantes)
        Me.divDerecha.Panel1.Controls.Add(Me.lblVariantes)
        Me.divDerecha.Panel2.Controls.Add(Me.gridEmpaques)
        Me.divDerecha.Panel2.Controls.Add(Me.barraEmpaques)
        Me.divDerecha.Panel2.Controls.Add(Me.lblEmpaques)
        '
        'gridVariantes
        '
        Me.gridVariantes.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridVariantes.Name = "gridVariantes"
        Me.gridVariantes.AccessibleName = "Variantes"
        '
        'barraVariantes
        '
        Me.barraVariantes.AutoSize = True
        Me.barraVariantes.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraVariantes.Name = "barraVariantes"
        Me.barraVariantes.Padding = New System.Windows.Forms.Padding(4)
        Me.barraVariantes.WrapContents = True
        '
        'lblVariantes
        '
        Me.lblVariantes.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblVariantes.Name = "lblVariantes"
        Me.lblVariantes.Padding = New System.Windows.Forms.Padding(4)
        Me.lblVariantes.Text = "Variantes (marca y presentacion)"
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
        Me.barraEmpaques.Padding = New System.Windows.Forms.Padding(4)
        Me.barraEmpaques.WrapContents = True
        '
        'lblEmpaques
        '
        Me.lblEmpaques.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblEmpaques.Name = "lblEmpaques"
        Me.lblEmpaques.Padding = New System.Windows.Forms.Padding(4)
        Me.lblEmpaques.Text = "Empaques de compra"
        '
        'FormCatalogo
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.divPrincipal)
        Me.Name = "FormCatalogo"
        Me.Text = "Catalogo: productos, variantes y empaques"
        Me.divPrincipal.Panel1.ResumeLayout(False)
        Me.divPrincipal.Panel2.ResumeLayout(False)
        CType(Me.divPrincipal, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divPrincipal.ResumeLayout(False)
        Me.panelProductos.ResumeLayout(False)
        CType(Me.gridProductos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraProductos.ResumeLayout(False)
        Me.barraProductos.PerformLayout()
        Me.barraBusqueda.ResumeLayout(False)
        Me.barraBusqueda.PerformLayout()
        Me.divDerecha.Panel1.ResumeLayout(False)
        Me.divDerecha.Panel2.ResumeLayout(False)
        CType(Me.divDerecha, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divDerecha.ResumeLayout(False)
        CType(Me.gridVariantes, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraVariantes.ResumeLayout(False)
        Me.barraVariantes.PerformLayout()
        CType(Me.gridEmpaques, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraEmpaques.ResumeLayout(False)
        Me.barraEmpaques.PerformLayout()
        CType(Me.divPrincipal, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents divPrincipal As System.Windows.Forms.SplitContainer
    Friend WithEvents panelProductos As System.Windows.Forms.Panel
    Friend WithEvents gridProductos As System.Windows.Forms.DataGridView
    Friend WithEvents barraProductos As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents barraBusqueda As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblBuscar As System.Windows.Forms.Label
    Friend WithEvents txtBuscar As System.Windows.Forms.TextBox
    Friend WithEvents chkInactivos As System.Windows.Forms.CheckBox
    Friend WithEvents divDerecha As System.Windows.Forms.SplitContainer
    Friend WithEvents gridVariantes As System.Windows.Forms.DataGridView
    Friend WithEvents barraVariantes As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblVariantes As System.Windows.Forms.Label
    Friend WithEvents gridEmpaques As System.Windows.Forms.DataGridView
    Friend WithEvents barraEmpaques As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblEmpaques As System.Windows.Forms.Label
End Class
