<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormReportes
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
        Me.divide = New System.Windows.Forms.SplitContainer()
        Me.lstReportes = New System.Windows.Forms.ListBox()
        Me.lblDescripcion = New System.Windows.Forms.Label()
        Me.barraAcciones = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblEstado = New System.Windows.Forms.Label()
        Me.tabla.SuspendLayout()
        Me.barraFiltros.SuspendLayout()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divide.Panel1.SuspendLayout()
        Me.divide.Panel2.SuspendLayout()
        Me.divide.SuspendLayout()
        Me.SuspendLayout()
        '
        'tabla
        '
        Me.tabla.ColumnCount = 1
        Me.tabla.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.tabla.Controls.Add(Me.barraFiltros, 0, 0)
        Me.tabla.Controls.Add(Me.divide, 0, 1)
        Me.tabla.Controls.Add(Me.lblEstado, 0, 2)
        Me.tabla.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tabla.Name = "tabla"
        Me.tabla.RowCount = 3
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
        'divide
        '
        Me.divide.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divide.Name = "divide"
        Me.divide.SplitterDistance = 380
        '
        'divide.Panel1
        '
        Me.divide.Panel1.Controls.Add(Me.lstReportes)
        '
        'divide.Panel2
        '
        Me.divide.Panel2.Controls.Add(Me.barraAcciones)
        Me.divide.Panel2.Controls.Add(Me.lblDescripcion)
        '
        'lstReportes
        '
        Me.lstReportes.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lstReportes.IntegralHeight = False
        Me.lstReportes.Name = "lstReportes"
        Me.lstReportes.AccessibleName = "Reportes"
        '
        'lblDescripcion
        '
        Me.lblDescripcion.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblDescripcion.Name = "lblDescripcion"
        Me.lblDescripcion.Padding = New System.Windows.Forms.Padding(8)
        Me.lblDescripcion.Text = "Elija un reporte. Luego elija el formato: imprimir, Excel, PDF o CSV."
        '
        'barraAcciones
        '
        Me.barraAcciones.AutoSize = True
        Me.barraAcciones.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraAcciones.Name = "barraAcciones"
        '
        'lblEstado
        '
        Me.lblEstado.AutoSize = True
        Me.lblEstado.Name = "lblEstado"
        Me.lblEstado.Text = "Los reportes se generan desde la base; Excel y PDF son salidas, no fuente de datos."
        '
        'FormReportes
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1200, 680)
        Me.Controls.Add(Me.tabla)
        Me.Name = "FormReportes"
        Me.Text = "Reportes"
        Me.tabla.ResumeLayout(False)
        Me.tabla.PerformLayout()
        Me.barraFiltros.ResumeLayout(False)
        Me.barraFiltros.PerformLayout()
        Me.divide.Panel1.ResumeLayout(False)
        Me.divide.Panel2.ResumeLayout(False)
        Me.divide.Panel2.PerformLayout()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divide.ResumeLayout(False)
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents tabla As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents barraFiltros As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblDesde As System.Windows.Forms.Label
    Friend WithEvents dtDesde As System.Windows.Forms.DateTimePicker
    Friend WithEvents lblHasta As System.Windows.Forms.Label
    Friend WithEvents dtHasta As System.Windows.Forms.DateTimePicker
    Friend WithEvents divide As System.Windows.Forms.SplitContainer
    Friend WithEvents lstReportes As System.Windows.Forms.ListBox
    Friend WithEvents lblDescripcion As System.Windows.Forms.Label
    Friend WithEvents barraAcciones As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblEstado As System.Windows.Forms.Label
End Class
