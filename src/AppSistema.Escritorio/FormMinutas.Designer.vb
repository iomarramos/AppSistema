<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormMinutas
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
        Me.panelArriba = New System.Windows.Forms.Panel()
        Me.gridMinutas = New System.Windows.Forms.DataGridView()
        Me.barraMinutas = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblDesde = New System.Windows.Forms.Label()
        Me.dtDesde = New System.Windows.Forms.DateTimePicker()
        Me.lblHasta = New System.Windows.Forms.Label()
        Me.dtHasta = New System.Windows.Forms.DateTimePicker()
        Me.panelInferior = New System.Windows.Forms.Panel()
        Me.divPlatos = New System.Windows.Forms.SplitContainer()
        Me.gridPlatos = New System.Windows.Forms.DataGridView()
        Me.lblPlatos = New System.Windows.Forms.Label()
        Me.gridFijos = New System.Windows.Forms.DataGridView()
        Me.lblFijos = New System.Windows.Forms.Label()
        Me.barraPlatos = New System.Windows.Forms.FlowLayoutPanel()
        CType(Me.divPrincipal, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divPrincipal.Panel1.SuspendLayout()
        Me.divPrincipal.Panel2.SuspendLayout()
        Me.divPrincipal.SuspendLayout()
        Me.panelArriba.SuspendLayout()
        CType(Me.gridMinutas, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraMinutas.SuspendLayout()
        Me.panelInferior.SuspendLayout()
        CType(Me.divPlatos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divPlatos.Panel1.SuspendLayout()
        Me.divPlatos.Panel2.SuspendLayout()
        Me.divPlatos.SuspendLayout()
        CType(Me.gridPlatos, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridFijos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraPlatos.SuspendLayout()
        Me.SuspendLayout()
        '
        'divPrincipal
        '
        Me.divPrincipal.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divPrincipal.Name = "divPrincipal"
        Me.divPrincipal.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divPrincipal.Panel1.Controls.Add(Me.panelArriba)
        Me.divPrincipal.Panel2.Controls.Add(Me.panelInferior)
        '
        'panelArriba
        '
        Me.panelArriba.Dock = System.Windows.Forms.DockStyle.Fill
        Me.panelArriba.Name = "panelArriba"
        Me.panelArriba.Controls.Add(Me.gridMinutas)
        Me.panelArriba.Controls.Add(Me.barraMinutas)
        '
        'gridMinutas
        '
        Me.gridMinutas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridMinutas.Name = "gridMinutas"
        Me.gridMinutas.AccessibleName = "Minutas"
        '
        'barraMinutas
        '
        Me.barraMinutas.AutoSize = True
        Me.barraMinutas.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraMinutas.Name = "barraMinutas"
        Me.barraMinutas.Padding = New System.Windows.Forms.Padding(4)
        Me.barraMinutas.WrapContents = True
        Me.barraMinutas.Controls.Add(Me.lblDesde)
        Me.barraMinutas.Controls.Add(Me.dtDesde)
        Me.barraMinutas.Controls.Add(Me.lblHasta)
        Me.barraMinutas.Controls.Add(Me.dtHasta)
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
        Me.lblHasta.Text = "hasta"
        '
        'dtHasta
        '
        Me.dtHasta.Format = System.Windows.Forms.DateTimePickerFormat.Short
        Me.dtHasta.Name = "dtHasta"
        Me.dtHasta.AccessibleName = "Hasta"
        Me.dtHasta.Width = 110
        '
        'panelInferior
        '
        Me.panelInferior.Dock = System.Windows.Forms.DockStyle.Fill
        Me.panelInferior.Name = "panelInferior"
        Me.panelInferior.Controls.Add(Me.divPlatos)
        Me.panelInferior.Controls.Add(Me.barraPlatos)
        '
        'divPlatos
        '
        Me.divPlatos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divPlatos.Name = "divPlatos"
        Me.divPlatos.Panel1.Controls.Add(Me.gridPlatos)
        Me.divPlatos.Panel1.Controls.Add(Me.lblPlatos)
        Me.divPlatos.Panel2.Controls.Add(Me.gridFijos)
        Me.divPlatos.Panel2.Controls.Add(Me.lblFijos)
        '
        'gridPlatos
        '
        Me.gridPlatos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridPlatos.Name = "gridPlatos"
        Me.gridPlatos.AccessibleName = "Platos"
        '
        'lblPlatos
        '
        Me.lblPlatos.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblPlatos.Name = "lblPlatos"
        Me.lblPlatos.Padding = New System.Windows.Forms.Padding(4)
        Me.lblPlatos.Text = "Platos (costo previsto fijado al aprobar; 'pendiente' = falta precio)"
        '
        'gridFijos
        '
        Me.gridFijos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridFijos.Name = "gridFijos"
        Me.gridFijos.AccessibleName = "Fijos"
        '
        'lblFijos
        '
        Me.lblFijos.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblFijos.Name = "lblFijos"
        Me.lblFijos.Padding = New System.Windows.Forms.Padding(4)
        Me.lblFijos.Text = "Fijos (productos fuera de recetas, cantidad total)"
        '
        'barraPlatos
        '
        Me.barraPlatos.AutoSize = True
        Me.barraPlatos.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraPlatos.Name = "barraPlatos"
        Me.barraPlatos.Padding = New System.Windows.Forms.Padding(4)
        Me.barraPlatos.WrapContents = True
        '
        'FormMinutas
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.divPrincipal)
        Me.Name = "FormMinutas"
        Me.Text = "Minutas"
        Me.divPrincipal.Panel1.ResumeLayout(False)
        Me.divPrincipal.Panel2.ResumeLayout(False)
        CType(Me.divPrincipal, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divPrincipal.ResumeLayout(False)
        Me.panelArriba.ResumeLayout(False)
        CType(Me.gridMinutas, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraMinutas.ResumeLayout(False)
        Me.barraMinutas.PerformLayout()
        Me.panelInferior.ResumeLayout(False)
        Me.divPlatos.Panel1.ResumeLayout(False)
        Me.divPlatos.Panel2.ResumeLayout(False)
        CType(Me.divPlatos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divPlatos.ResumeLayout(False)
        CType(Me.gridPlatos, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridFijos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraPlatos.ResumeLayout(False)
        Me.barraPlatos.PerformLayout()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents divPrincipal As System.Windows.Forms.SplitContainer
    Friend WithEvents panelArriba As System.Windows.Forms.Panel
    Friend WithEvents gridMinutas As System.Windows.Forms.DataGridView
    Friend WithEvents barraMinutas As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblDesde As System.Windows.Forms.Label
    Friend WithEvents dtDesde As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblHasta As System.Windows.Forms.Label
    Friend WithEvents dtHasta As System.Windows.Forms.DateTimePicker
    Friend WithEvents panelInferior As System.Windows.Forms.Panel
    Friend WithEvents divPlatos As System.Windows.Forms.SplitContainer
    Friend WithEvents gridPlatos As System.Windows.Forms.DataGridView
    Friend WithEvents lblPlatos As System.Windows.Forms.Label
    Friend WithEvents gridFijos As System.Windows.Forms.DataGridView
    Friend WithEvents lblFijos As System.Windows.Forms.Label
    Friend WithEvents barraPlatos As System.Windows.Forms.FlowLayoutPanel
End Class
