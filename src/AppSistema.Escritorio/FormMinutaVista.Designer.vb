<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormMinutaVista
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
        Me.gridVista = New System.Windows.Forms.DataGridView()
        Me.lblEstado = New System.Windows.Forms.Label()
        Me.barraAcciones = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblServicio = New System.Windows.Forms.Label()
        Me.cmbServicio = New System.Windows.Forms.ComboBox()
        Me.lblNivel = New System.Windows.Forms.Label()
        Me.cmbNivel = New System.Windows.Forms.ComboBox()
        Me.lblDesde = New System.Windows.Forms.Label()
        Me.dtDesde = New System.Windows.Forms.DateTimePicker()
        Me.lblHasta = New System.Windows.Forms.Label()
        Me.dtHasta = New System.Windows.Forms.DateTimePicker()
        CType(Me.gridVista, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraAcciones.SuspendLayout()
        Me.SuspendLayout()
        '
        'gridVista
        '
        Me.gridVista.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridVista.Name = "gridVista"
        '
        'lblEstado
        '
        Me.lblEstado.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lblEstado.Height = 34
        Me.lblEstado.Name = "lblEstado"
        Me.lblEstado.Padding = New System.Windows.Forms.Padding(6)
        '
        'barraAcciones
        '
        Me.barraAcciones.AutoSize = True
        Me.barraAcciones.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraAcciones.Name = "barraAcciones"
        Me.barraAcciones.Padding = New System.Windows.Forms.Padding(4)
        Me.barraAcciones.WrapContents = True
        Me.barraAcciones.Controls.Add(Me.lblServicio)
        Me.barraAcciones.Controls.Add(Me.cmbServicio)
        Me.barraAcciones.Controls.Add(Me.lblNivel)
        Me.barraAcciones.Controls.Add(Me.cmbNivel)
        Me.barraAcciones.Controls.Add(Me.lblDesde)
        Me.barraAcciones.Controls.Add(Me.dtDesde)
        Me.barraAcciones.Controls.Add(Me.lblHasta)
        Me.barraAcciones.Controls.Add(Me.dtHasta)
        '
        'lblServicio
        '
        Me.lblServicio.AutoSize = True
        Me.lblServicio.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblServicio.Name = "lblServicio"
        Me.lblServicio.Text = "Servicio"
        '
        'cmbServicio
        '
        Me.cmbServicio.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbServicio.Name = "cmbServicio"
        Me.cmbServicio.Width = 180
        '
        'lblNivel
        '
        Me.lblNivel.AutoSize = True
        Me.lblNivel.Margin = New System.Windows.Forms.Padding(12, 9, 3, 3)
        Me.lblNivel.Name = "lblNivel"
        Me.lblNivel.Text = "Minuta"
        '
        'cmbNivel
        '
        Me.cmbNivel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbNivel.Name = "cmbNivel"
        Me.cmbNivel.Width = 160
        '
        'lblDesde
        '
        Me.lblDesde.AutoSize = True
        Me.lblDesde.Margin = New System.Windows.Forms.Padding(12, 9, 3, 3)
        Me.lblDesde.Name = "lblDesde"
        Me.lblDesde.Text = "Desde"
        '
        'dtDesde
        '
        Me.dtDesde.Format = System.Windows.Forms.DateTimePickerFormat.Short
        Me.dtDesde.Name = "dtDesde"
        Me.dtDesde.Width = 110
        '
        'lblHasta
        '
        Me.lblHasta.AutoSize = True
        Me.lblHasta.Margin = New System.Windows.Forms.Padding(12, 9, 3, 3)
        Me.lblHasta.Name = "lblHasta"
        Me.lblHasta.Text = "hasta"
        '
        'dtHasta
        '
        Me.dtHasta.Format = System.Windows.Forms.DateTimePickerFormat.Short
        Me.dtHasta.Name = "dtHasta"
        Me.dtHasta.Width = 110
        '
        'FormMinutaVista
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.gridVista)
        Me.Controls.Add(Me.lblEstado)
        Me.Controls.Add(Me.barraAcciones)
        Me.Name = "FormMinutaVista"
        Me.Text = "Minuta teorica y real"
        CType(Me.gridVista, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraAcciones.ResumeLayout(False)
        Me.barraAcciones.PerformLayout()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents gridVista As System.Windows.Forms.DataGridView
    Friend WithEvents lblEstado As System.Windows.Forms.Label
    Friend WithEvents barraAcciones As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblServicio As System.Windows.Forms.Label
    Friend WithEvents cmbServicio As System.Windows.Forms.ComboBox
    Friend WithEvents lblNivel As System.Windows.Forms.Label
    Friend WithEvents cmbNivel As System.Windows.Forms.ComboBox
    Friend WithEvents lblDesde As System.Windows.Forms.Label
    Friend WithEvents dtDesde As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblHasta As System.Windows.Forms.Label
    Friend WithEvents dtHasta As System.Windows.Forms.DateTimePicker
End Class
