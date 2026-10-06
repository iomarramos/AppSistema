<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormProduccionChef
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
        Me.tabla = New System.Windows.Forms.TableLayoutPanel()
        Me.barraFiltros = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblDesde = New System.Windows.Forms.Label()
        Me.dtDesde = New System.Windows.Forms.DateTimePicker()
        Me.lblHasta = New System.Windows.Forms.Label()
        Me.dtHasta = New System.Windows.Forms.DateTimePicker()
        Me.barraAcciones = New System.Windows.Forms.FlowLayoutPanel()
        Me.gridPlan = New System.Windows.Forms.DataGridView()
        Me.lblEstado = New System.Windows.Forms.Label()
        Me.tabla.SuspendLayout()
        Me.barraFiltros.SuspendLayout()
        CType(Me.gridPlan, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'tabla
        '
        Me.tabla.ColumnCount = 1
        Me.tabla.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.tabla.Controls.Add(Me.barraFiltros, 0, 0)
        Me.tabla.Controls.Add(Me.barraAcciones, 0, 1)
        Me.tabla.Controls.Add(Me.gridPlan, 0, 2)
        Me.tabla.Controls.Add(Me.lblEstado, 0, 3)
        Me.tabla.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tabla.Name = "tabla"
        Me.tabla.RowCount = 4
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize))
        '
        'barraFiltros
        '
        Me.barraFiltros.AutoSize = True
        Me.barraFiltros.Controls.Add(Me.lblDesde)
        Me.barraFiltros.Controls.Add(Me.dtDesde)
        Me.barraFiltros.Controls.Add(Me.lblHasta)
        Me.barraFiltros.Controls.Add(Me.dtHasta)
        Me.barraFiltros.Name = "barraFiltros"
        '
        'lblDesde
        '
        Me.lblDesde.AutoSize = True
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
        'barraAcciones
        '
        Me.barraAcciones.AutoSize = True
        Me.barraAcciones.Name = "barraAcciones"
        '
        'gridPlan
        '
        Me.gridPlan.AllowUserToAddRows = False
        Me.gridPlan.AllowUserToDeleteRows = False
        Me.gridPlan.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridPlan.Name = "gridPlan"
        Me.gridPlan.AccessibleName = "Plan"
        Me.gridPlan.ReadOnly = True
        Me.gridPlan.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        '
        'lblEstado
        '
        Me.lblEstado.AutoSize = True
        Me.lblEstado.Name = "lblEstado"
        Me.lblEstado.Text = "Raciones teoricas frente a comensales. + = mas raciones que comensales; - = menos."
        '
        'FormProduccionChef
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1280, 720)
        Me.Controls.Add(Me.tabla)
        Me.Name = "FormProduccionChef"
        Me.Text = "Plan operativo del chef"
        Me.tabla.ResumeLayout(False)
        Me.tabla.PerformLayout()
        Me.barraFiltros.ResumeLayout(False)
        Me.barraFiltros.PerformLayout()
        CType(Me.gridPlan, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents tabla As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents barraFiltros As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblDesde As System.Windows.Forms.Label
    Friend WithEvents dtDesde As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblHasta As System.Windows.Forms.Label
    Friend WithEvents dtHasta As System.Windows.Forms.DateTimePicker
    Friend WithEvents barraAcciones As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents gridPlan As System.Windows.Forms.DataGridView
    Friend WithEvents lblEstado As System.Windows.Forms.Label
End Class
