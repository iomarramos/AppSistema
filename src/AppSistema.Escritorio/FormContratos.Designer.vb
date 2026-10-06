<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormContratos
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
        Me.divide = New System.Windows.Forms.SplitContainer()
        Me.gridContratos = New System.Windows.Forms.DataGridView()
        Me.gridLineas = New System.Windows.Forms.DataGridView()
        Me.lblDescripcion = New System.Windows.Forms.Label()
        Me.barraAcciones = New System.Windows.Forms.FlowLayoutPanel()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divide.Panel1.SuspendLayout()
        Me.divide.Panel2.SuspendLayout()
        Me.divide.SuspendLayout()
        CType(Me.gridContratos, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridLineas, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraAcciones.SuspendLayout()
        Me.SuspendLayout()
        '
        'divide
        '
        Me.divide.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divide.Name = "divide"
        Me.divide.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divide.SplitterDistance = 200
        Me.divide.Panel1.Controls.Add(Me.gridContratos)
        Me.divide.Panel2.Controls.Add(Me.gridLineas)
        '
        'gridContratos
        '
        Me.gridContratos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridContratos.Name = "gridContratos"
        Me.gridContratos.AccessibleName = "Contratos"
        '
        'gridLineas
        '
        Me.gridLineas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridLineas.Name = "gridLineas"
        Me.gridLineas.AccessibleName = "Lineas"
        '
        'lblDescripcion
        '
        Me.lblDescripcion.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblDescripcion.Height = 34
        Me.lblDescripcion.Name = "lblDescripcion"
        Me.lblDescripcion.Padding = New System.Windows.Forms.Padding(4)
        Me.lblDescripcion.Text = "Un ajuste cierra la linea vigente el dia anterior y abre otra con el importe nuevo. El ingreso del mes se prorratea por dias y no reemplaza un ingreso registrado a mano."
        '
        'barraAcciones
        '
        Me.barraAcciones.AutoSize = True
        Me.barraAcciones.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraAcciones.Name = "barraAcciones"
        '
        'FormContratos
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.divide)
        Me.Controls.Add(Me.lblDescripcion)
        Me.Controls.Add(Me.barraAcciones)
        Me.Name = "FormContratos"
        Me.Text = "Contratos"
        Me.divide.Panel1.ResumeLayout(False)
        Me.divide.Panel2.ResumeLayout(False)
        CType(Me.divide, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divide.ResumeLayout(False)
        CType(Me.gridContratos, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridLineas, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraAcciones.ResumeLayout(False)
        Me.barraAcciones.PerformLayout()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents divide As System.Windows.Forms.SplitContainer
    Friend WithEvents gridContratos As System.Windows.Forms.DataGridView
    Friend WithEvents gridLineas As System.Windows.Forms.DataGridView
    Friend WithEvents lblDescripcion As System.Windows.Forms.Label
    Friend WithEvents barraAcciones As System.Windows.Forms.FlowLayoutPanel
End Class
