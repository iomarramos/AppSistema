<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormCierres
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
        Me.lblDescripcion = New System.Windows.Forms.Label()
        Me.divide = New System.Windows.Forms.SplitContainer()
        Me.barraDia = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblDia = New System.Windows.Forms.Label()
        Me.dtFecha = New System.Windows.Forms.DateTimePicker()
        Me.gridPendientes = New System.Windows.Forms.DataGridView()
        Me.lblEstadoDia = New System.Windows.Forms.Label()
        Me.lblEnvio = New System.Windows.Forms.Label()
        Me.barraMes = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblMes = New System.Windows.Forms.Label()
        Me.dtMes = New System.Windows.Forms.DateTimePicker()
        Me.gridServicios = New System.Windows.Forms.DataGridView()
        Me.lblTotales = New System.Windows.Forms.Label()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divide.Panel1.SuspendLayout()
        Me.divide.Panel2.SuspendLayout()
        Me.divide.SuspendLayout()
        Me.barraDia.SuspendLayout()
        Me.barraMes.SuspendLayout()
        CType(Me.gridPendientes, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridServicios, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'lblDescripcion
        '
        Me.lblDescripcion.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblDescripcion.Height = 34
        Me.lblDescripcion.Name = "lblDescripcion"
        Me.lblDescripcion.Padding = New System.Windows.Forms.Padding(4)
        Me.lblDescripcion.Text = "Food Cost = costo de alimentos consumidos / venta del servicio. La venta sale de la estructura: costo previsto de las minutas / Food Cost objetivo (48 % por defecto). Un dia o mes cerrado ya no admite cambios."
        '
        'divide
        '
        Me.divide.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divide.Name = "divide"
        Me.divide.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divide.SplitterDistance = 220
        '
        'divide.Panel1
        '
        Me.divide.Panel1.Controls.Add(Me.gridPendientes)
        Me.divide.Panel1.Controls.Add(Me.lblEstadoDia)
        Me.divide.Panel1.Controls.Add(Me.lblEnvio)
        Me.divide.Panel1.Controls.Add(Me.barraDia)
        '
        'divide.Panel2
        '
        Me.divide.Panel2.Controls.Add(Me.gridServicios)
        Me.divide.Panel2.Controls.Add(Me.lblTotales)
        Me.divide.Panel2.Controls.Add(Me.barraMes)
        '
        'barraDia
        '
        Me.barraDia.AutoSize = True
        Me.barraDia.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraDia.Controls.Add(Me.lblDia)
        Me.barraDia.Controls.Add(Me.dtFecha)
        Me.barraDia.Name = "barraDia"
        '
        'lblDia
        '
        Me.lblDia.AutoSize = True
        Me.lblDia.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblDia.Name = "lblDia"
        Me.lblDia.Text = "Dia"
        '
        'dtFecha
        '
        Me.dtFecha.Format = System.Windows.Forms.DateTimePickerFormat.Short
        Me.dtFecha.Name = "dtFecha"
        Me.dtFecha.AccessibleName = "Fecha"
        Me.dtFecha.Width = 110
        '
        'gridPendientes
        '
        Me.gridPendientes.AllowUserToAddRows = False
        Me.gridPendientes.AllowUserToDeleteRows = False
        Me.gridPendientes.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridPendientes.Name = "gridPendientes"
        Me.gridPendientes.AccessibleName = "Pendientes"
        Me.gridPendientes.ReadOnly = True
        Me.gridPendientes.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        '
        'lblEstadoDia
        '
        Me.lblEstadoDia.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lblEstadoDia.Height = 28
        Me.lblEstadoDia.Name = "lblEstadoDia"
        Me.lblEstadoDia.Padding = New System.Windows.Forms.Padding(6)
        '
        'lblEnvio
        '
        Me.lblEnvio.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lblEnvio.Height = 28
        Me.lblEnvio.Name = "lblEnvio"
        Me.lblEnvio.Padding = New System.Windows.Forms.Padding(6)
        '
        'barraMes
        '
        Me.barraMes.AutoSize = True
        Me.barraMes.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraMes.Controls.Add(Me.lblMes)
        Me.barraMes.Controls.Add(Me.dtMes)
        Me.barraMes.Name = "barraMes"
        '
        'lblMes
        '
        Me.lblMes.AutoSize = True
        Me.lblMes.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblMes.Name = "lblMes"
        Me.lblMes.Text = "Mes"
        '
        'dtMes
        '
        Me.dtMes.CustomFormat = "MM/yyyy"
        Me.dtMes.Format = System.Windows.Forms.DateTimePickerFormat.Custom
        Me.dtMes.Name = "dtMes"
        Me.dtMes.AccessibleName = "Mes"
        Me.dtMes.ShowUpDown = True
        Me.dtMes.Width = 90
        '
        'gridServicios
        '
        Me.gridServicios.AllowUserToAddRows = False
        Me.gridServicios.AllowUserToDeleteRows = False
        Me.gridServicios.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridServicios.Name = "gridServicios"
        Me.gridServicios.AccessibleName = "Servicios"
        Me.gridServicios.ReadOnly = True
        Me.gridServicios.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        '
        'lblTotales
        '
        Me.lblTotales.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.lblTotales.Height = 28
        Me.lblTotales.Name = "lblTotales"
        Me.lblTotales.Padding = New System.Windows.Forms.Padding(6)
        '
        'FormCierres
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.divide)
        Me.Controls.Add(Me.lblDescripcion)
        Me.Name = "FormCierres"
        Me.Text = "Cierres y Food Cost"
        Me.divide.Panel1.ResumeLayout(False)
        Me.divide.Panel1.PerformLayout()
        Me.divide.Panel2.ResumeLayout(False)
        Me.divide.Panel2.PerformLayout()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divide.ResumeLayout(False)
        Me.barraDia.ResumeLayout(False)
        Me.barraDia.PerformLayout()
        Me.barraMes.ResumeLayout(False)
        Me.barraMes.PerformLayout()
        CType(Me.gridPendientes, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridServicios, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents lblDescripcion As System.Windows.Forms.Label
    Friend WithEvents divide As System.Windows.Forms.SplitContainer
    Friend WithEvents barraDia As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblDia As System.Windows.Forms.Label
    Friend WithEvents dtFecha As System.Windows.Forms.DateTimePicker
    Friend WithEvents gridPendientes As System.Windows.Forms.DataGridView
    Friend WithEvents lblEstadoDia As System.Windows.Forms.Label
    Friend WithEvents lblEnvio As System.Windows.Forms.Label
    Friend WithEvents barraMes As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblMes As System.Windows.Forms.Label
    Friend WithEvents dtMes As System.Windows.Forms.DateTimePicker
    Friend WithEvents gridServicios As System.Windows.Forms.DataGridView
    Friend WithEvents lblTotales As System.Windows.Forms.Label
End Class
