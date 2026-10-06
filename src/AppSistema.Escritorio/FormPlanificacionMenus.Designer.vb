<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormPlanificacionMenus
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
        Me.gridMatriz = New System.Windows.Forms.DataGridView()
        Me.lblResumen = New System.Windows.Forms.Label()
        Me.gridResumen = New System.Windows.Forms.DataGridView()
        Me.lblEstado = New System.Windows.Forms.Label()
        Me.tabla.SuspendLayout()
        Me.barraFiltros.SuspendLayout()
        CType(Me.gridMatriz, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridResumen, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'tabla
        '
        Me.tabla.ColumnCount = 1
        Me.tabla.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.tabla.Controls.Add(Me.barraFiltros, 0, 0)
        Me.tabla.Controls.Add(Me.barraAcciones, 0, 1)
        Me.tabla.Controls.Add(Me.gridMatriz, 0, 2)
        Me.tabla.Controls.Add(Me.lblResumen, 0, 3)
        Me.tabla.Controls.Add(Me.gridResumen, 0, 4)
        Me.tabla.Controls.Add(Me.lblEstado, 0, 5)
        Me.tabla.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tabla.Name = "tabla"
        Me.tabla.RowCount = 6
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60.0!))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40.0!))
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
        'gridMatriz
        '
        Me.gridMatriz.AllowUserToAddRows = False
        Me.gridMatriz.AllowUserToDeleteRows = False
        Me.gridMatriz.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridMatriz.Name = "gridMatriz"
        Me.gridMatriz.AccessibleName = "Matriz"
        Me.gridMatriz.ReadOnly = True
        Me.gridMatriz.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect
        '
        'lblResumen
        '
        Me.lblResumen.AutoSize = True
        Me.lblResumen.Name = "lblResumen"
        Me.lblResumen.Text = "Resumen por dia"
        '
        'gridResumen
        '
        Me.gridResumen.AllowUserToAddRows = False
        Me.gridResumen.AllowUserToDeleteRows = False
        Me.gridResumen.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridResumen.Name = "gridResumen"
        Me.gridResumen.AccessibleName = "Resumen"
        Me.gridResumen.ReadOnly = True
        Me.gridResumen.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        '
        'lblEstado
        '
        Me.lblEstado.AutoSize = True
        Me.lblEstado.Name = "lblEstado"
        Me.lblEstado.Text = "Costo y techo salen del snapshot aprobado. Una minuta sin aprobar no tiene costo: muestra 'sin costo', nunca cero."
        '
        'FormPlanificacionMenus
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1280, 720)
        Me.Controls.Add(Me.tabla)
        Me.Name = "FormPlanificacionMenus"
        Me.Text = "Planificacion de menus (matriz)"
        Me.tabla.ResumeLayout(False)
        Me.tabla.PerformLayout()
        Me.barraFiltros.ResumeLayout(False)
        Me.barraFiltros.PerformLayout()
        CType(Me.gridMatriz, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridResumen, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents tabla As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents barraFiltros As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblDesde As System.Windows.Forms.Label
    Friend WithEvents dtDesde As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblHasta As System.Windows.Forms.Label
    Friend WithEvents dtHasta As System.Windows.Forms.DateTimePicker
    Friend WithEvents barraAcciones As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents gridMatriz As System.Windows.Forms.DataGridView
    Friend WithEvents lblResumen As System.Windows.Forms.Label
    Friend WithEvents gridResumen As System.Windows.Forms.DataGridView
    Friend WithEvents lblEstado As System.Windows.Forms.Label
End Class
