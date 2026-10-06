<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormAuditoria
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
        Me.pnlFiltros = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblDesde = New System.Windows.Forms.Label()
        Me._desde = New System.Windows.Forms.DateTimePicker()
        Me.lblHasta = New System.Windows.Forms.Label()
        Me._hasta = New System.Windows.Forms.DateTimePicker()
        Me.lblTabla = New System.Windows.Forms.Label()
        Me._tabla = New System.Windows.Forms.ComboBox()
        Me.lblUsuario = New System.Windows.Forms.Label()
        Me._usuario = New System.Windows.Forms.TextBox()
        Me.btnBuscar = New System.Windows.Forms.Button()
        Me._filas = New System.Windows.Forms.DataGridView()
        Me._detalle = New System.Windows.Forms.TextBox()
        Me.pnlFiltros.SuspendLayout()
        CType(Me._filas, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        Me.pnlFiltros.AutoSize = True
        Me.pnlFiltros.Dock = System.Windows.Forms.DockStyle.Top
        Me.pnlFiltros.Padding = New System.Windows.Forms.Padding(4)
        Me.pnlFiltros.Controls.AddRange(New System.Windows.Forms.Control() {Me.lblDesde, Me._desde, Me.lblHasta, Me._hasta, Me.lblTabla, Me._tabla, Me.lblUsuario, Me._usuario, Me.btnBuscar})
        Me.lblDesde.AutoSize = True
        Me.lblDesde.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblDesde.Text = "Desde"
        Me._desde.Format = System.Windows.Forms.DateTimePickerFormat.Short
        Me._desde.Name = "dtpDesde"
        Me._desde.AccessibleName = "Desde"
        Me._desde.Width = 110
        Me.lblHasta.AutoSize = True
        Me.lblHasta.Margin = New System.Windows.Forms.Padding(8, 9, 3, 3)
        Me.lblHasta.Text = "Hasta"
        Me._hasta.Format = System.Windows.Forms.DateTimePickerFormat.Short
        Me._hasta.Name = "dtpHasta"
        Me._hasta.AccessibleName = "Hasta"
        Me._hasta.Width = 110
        Me.lblTabla.AutoSize = True
        Me.lblTabla.Margin = New System.Windows.Forms.Padding(8, 9, 3, 3)
        Me.lblTabla.Text = "Tabla"
        Me._tabla.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me._tabla.Name = "cboTabla"
        Me._tabla.AccessibleName = "Tabla"
        Me._tabla.Width = 200
        Me.lblUsuario.AutoSize = True
        Me.lblUsuario.Margin = New System.Windows.Forms.Padding(8, 9, 3, 3)
        Me.lblUsuario.Text = "Usuario"
        Me._usuario.Name = "txtUsuario"
        Me._usuario.AccessibleName = "Usuario"
        Me._usuario.Width = 120
        Me.btnBuscar.AutoSize = True
        Me.btnBuscar.Name = "btnBuscar"
        Me.btnBuscar.Text = "Buscar"
        Me._filas.AllowUserToAddRows = False
        Me._filas.AllowUserToDeleteRows = False
        Me._filas.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells
        Me._filas.Dock = System.Windows.Forms.DockStyle.Fill
        Me._filas.MultiSelect = False
        Me._filas.Name = "gridAuditoria"
        Me._filas.AccessibleName = "Auditoria"
        Me._filas.ReadOnly = True
        Me._filas.RowHeadersVisible = False
        Me._filas.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me._detalle.Dock = System.Windows.Forms.DockStyle.Bottom
        Me._detalle.Height = 120
        Me._detalle.Multiline = True
        Me._detalle.Name = "txtDetalle"
        Me._detalle.AccessibleName = "Detalle"
        Me._detalle.ReadOnly = True
        Me._detalle.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me._filas)
        Me.Controls.Add(Me._detalle)
        Me.Controls.Add(Me.pnlFiltros)
        Me.Name = "FormAuditoria"
        Me.Text = "Auditoria"
        Me.pnlFiltros.ResumeLayout(False)
        Me.pnlFiltros.PerformLayout()
        CType(Me._filas, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents pnlFiltros As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblDesde As System.Windows.Forms.Label
    Friend WithEvents _desde As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblHasta As System.Windows.Forms.Label
    Friend WithEvents _hasta As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblTabla As System.Windows.Forms.Label
    Friend WithEvents _tabla As System.Windows.Forms.ComboBox
    Friend WithEvents lblUsuario As System.Windows.Forms.Label
    Friend WithEvents _usuario As System.Windows.Forms.TextBox
    Friend WithEvents btnBuscar As System.Windows.Forms.Button
    Friend WithEvents _filas As System.Windows.Forms.DataGridView
    Friend WithEvents _detalle As System.Windows.Forms.TextBox
End Class
