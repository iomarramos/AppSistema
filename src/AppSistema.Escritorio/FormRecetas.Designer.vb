<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormRecetas
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
        Me.panelRecetas = New System.Windows.Forms.Panel()
        Me.gridRecetas = New System.Windows.Forms.DataGridView()
        Me.barraRecetas = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblBuscar = New System.Windows.Forms.Label()
        Me.txtBuscar = New System.Windows.Forms.TextBox()
        Me.divDetalle = New System.Windows.Forms.SplitContainer()
        Me.gridVersiones = New System.Windows.Forms.DataGridView()
        Me.barraVersiones = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblVersiones = New System.Windows.Forms.Label()
        Me.gridIngredientes = New System.Windows.Forms.DataGridView()
        Me.barraIngredientes = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblIngredientes = New System.Windows.Forms.Label()
        CType(Me.divPrincipal, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divPrincipal.Panel1.SuspendLayout()
        Me.divPrincipal.Panel2.SuspendLayout()
        Me.divPrincipal.SuspendLayout()
        Me.panelRecetas.SuspendLayout()
        CType(Me.gridRecetas, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraRecetas.SuspendLayout()
        CType(Me.divDetalle, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divDetalle.Panel1.SuspendLayout()
        Me.divDetalle.Panel2.SuspendLayout()
        Me.divDetalle.SuspendLayout()
        CType(Me.gridVersiones, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraVersiones.SuspendLayout()
        CType(Me.gridIngredientes, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraIngredientes.SuspendLayout()
        Me.SuspendLayout()
        '
        'divPrincipal
        '
        Me.divPrincipal.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divPrincipal.Name = "divPrincipal"
        Me.divPrincipal.Panel1.Controls.Add(Me.panelRecetas)
        '
        'divPrincipal.Panel2
        '
        Me.divPrincipal.Panel2.Controls.Add(Me.divDetalle)
        '
        'panelRecetas
        '
        Me.panelRecetas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.panelRecetas.Name = "panelRecetas"
        Me.panelRecetas.Controls.Add(Me.gridRecetas)
        Me.panelRecetas.Controls.Add(Me.barraRecetas)
        '
        'gridRecetas
        '
        Me.gridRecetas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridRecetas.Name = "gridRecetas"
        Me.gridRecetas.AccessibleName = "Recetas"
        '
        'barraRecetas
        '
        Me.barraRecetas.AutoSize = True
        Me.barraRecetas.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraRecetas.Name = "barraRecetas"
        Me.barraRecetas.Padding = New System.Windows.Forms.Padding(4)
        Me.barraRecetas.WrapContents = True
        Me.barraRecetas.Controls.Add(Me.lblBuscar)
        Me.barraRecetas.Controls.Add(Me.txtBuscar)
        '
        'lblBuscar
        '
        Me.lblBuscar.AutoSize = True
        Me.lblBuscar.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblBuscar.Name = "lblBuscar"
        Me.lblBuscar.Text = "Buscar:"
        '
        'txtBuscar
        '
        Me.txtBuscar.Name = "txtBuscar"
        Me.txtBuscar.AccessibleName = "Buscar"
        Me.txtBuscar.Width = 220
        '
        'divDetalle
        '
        Me.divDetalle.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divDetalle.Name = "divDetalle"
        Me.divDetalle.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divDetalle.Panel1.Controls.Add(Me.gridVersiones)
        Me.divDetalle.Panel1.Controls.Add(Me.barraVersiones)
        Me.divDetalle.Panel1.Controls.Add(Me.lblVersiones)
        Me.divDetalle.Panel2.Controls.Add(Me.gridIngredientes)
        Me.divDetalle.Panel2.Controls.Add(Me.barraIngredientes)
        Me.divDetalle.Panel2.Controls.Add(Me.lblIngredientes)
        '
        'gridVersiones
        '
        Me.gridVersiones.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridVersiones.Name = "gridVersiones"
        Me.gridVersiones.AccessibleName = "Versiones"
        '
        'barraVersiones
        '
        Me.barraVersiones.AutoSize = True
        Me.barraVersiones.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraVersiones.Name = "barraVersiones"
        Me.barraVersiones.Padding = New System.Windows.Forms.Padding(4)
        Me.barraVersiones.WrapContents = True
        '
        'lblVersiones
        '
        Me.lblVersiones.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblVersiones.Name = "lblVersiones"
        Me.lblVersiones.Padding = New System.Windows.Forms.Padding(4)
        Me.lblVersiones.Text = "Versiones (la aprobada no se modifica; para cambiarla cree una nueva version)"
        '
        'gridIngredientes
        '
        Me.gridIngredientes.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridIngredientes.Name = "gridIngredientes"
        Me.gridIngredientes.AccessibleName = "Ingredientes"
        '
        'barraIngredientes
        '
        Me.barraIngredientes.AutoSize = True
        Me.barraIngredientes.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraIngredientes.Name = "barraIngredientes"
        Me.barraIngredientes.Padding = New System.Windows.Forms.Padding(4)
        Me.barraIngredientes.WrapContents = True
        '
        'lblIngredientes
        '
        Me.lblIngredientes.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblIngredientes.Name = "lblIngredientes"
        Me.lblIngredientes.Padding = New System.Windows.Forms.Padding(4)
        Me.lblIngredientes.Text = "Ingredientes de la version (cantidad bruta para el rendimiento completo, en unidad base)"
        '
        'FormRecetas
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.divPrincipal)
        Me.Name = "FormRecetas"
        Me.Text = "Recetas"
        Me.divPrincipal.Panel1.ResumeLayout(False)
        Me.divPrincipal.Panel2.ResumeLayout(False)
        Me.divPrincipal.ResumeLayout(False)
        Me.panelRecetas.ResumeLayout(False)
        CType(Me.gridRecetas, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraRecetas.ResumeLayout(False)
        Me.barraRecetas.PerformLayout()
        Me.divDetalle.Panel1.ResumeLayout(False)
        Me.divDetalle.Panel2.ResumeLayout(False)
        Me.divDetalle.ResumeLayout(False)
        CType(Me.divDetalle, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridVersiones, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraVersiones.ResumeLayout(False)
        Me.barraVersiones.PerformLayout()
        CType(Me.gridIngredientes, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraIngredientes.ResumeLayout(False)
        Me.barraIngredientes.PerformLayout()
        CType(Me.divPrincipal, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents divPrincipal As System.Windows.Forms.SplitContainer
    Friend WithEvents panelRecetas As System.Windows.Forms.Panel
    Friend WithEvents gridRecetas As System.Windows.Forms.DataGridView
    Friend WithEvents barraRecetas As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblBuscar As System.Windows.Forms.Label
    Friend WithEvents txtBuscar As System.Windows.Forms.TextBox
    Friend WithEvents divDetalle As System.Windows.Forms.SplitContainer
    Friend WithEvents gridVersiones As System.Windows.Forms.DataGridView
    Friend WithEvents barraVersiones As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblVersiones As System.Windows.Forms.Label
    Friend WithEvents gridIngredientes As System.Windows.Forms.DataGridView
    Friend WithEvents barraIngredientes As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblIngredientes As System.Windows.Forms.Label
End Class
