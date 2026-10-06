<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormProduccion
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
        Me.gridMinutas = New System.Windows.Forms.DataGridView()
        Me.divRequerimientos = New System.Windows.Forms.SplitContainer()
        Me.gridRequerimientos = New System.Windows.Forms.DataGridView()
        Me.gridLineas = New System.Windows.Forms.DataGridView()
        Me.lblNota = New System.Windows.Forms.Label()
        Me.barraReal = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblReal = New System.Windows.Forms.Label()
        Me.barraFecha = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblFecha = New System.Windows.Forms.Label()
        Me.dtFecha = New System.Windows.Forms.DateTimePicker()
        CType(Me.divPrincipal, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divPrincipal.Panel1.SuspendLayout()
        Me.divPrincipal.Panel2.SuspendLayout()
        Me.divPrincipal.SuspendLayout()
        CType(Me.gridMinutas, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.divRequerimientos, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divRequerimientos.Panel1.SuspendLayout()
        Me.divRequerimientos.Panel2.SuspendLayout()
        Me.divRequerimientos.SuspendLayout()
        CType(Me.gridRequerimientos, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridLineas, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraReal.SuspendLayout()
        Me.barraFecha.SuspendLayout()
        Me.SuspendLayout()
        '
        'divPrincipal
        '
        Me.divPrincipal.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divPrincipal.Name = "divPrincipal"
        Me.divPrincipal.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divPrincipal.Panel1.Controls.Add(Me.gridMinutas)
        Me.divPrincipal.Panel2.Controls.Add(Me.divRequerimientos)
        '
        'gridMinutas
        '
        Me.gridMinutas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridMinutas.Name = "gridMinutas"
        Me.gridMinutas.AccessibleName = "Minutas"
        '
        'divRequerimientos
        '
        Me.divRequerimientos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divRequerimientos.Name = "divRequerimientos"
        Me.divRequerimientos.Panel1.Controls.Add(Me.gridRequerimientos)
        Me.divRequerimientos.Panel2.Controls.Add(Me.gridLineas)
        '
        'gridRequerimientos
        '
        Me.gridRequerimientos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridRequerimientos.Name = "gridRequerimientos"
        Me.gridRequerimientos.AccessibleName = "Requerimientos"
        '
        'gridLineas
        '
        Me.gridLineas.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridLineas.Name = "gridLineas"
        Me.gridLineas.AccessibleName = "Lineas"
        '
        'lblNota
        '
        Me.lblNota.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblNota.Height = 34
        Me.lblNota.Name = "lblNota"
        Me.lblNota.Padding = New System.Windows.Forms.Padding(4)
        Me.lblNota.Text = "El almacen entrega presentaciones completas y lo entregado se da por consumido (D12). El costo real es del servicio: entregas menos devoluciones."
        '
        'barraReal
        '
        Me.barraReal.AutoSize = True
        Me.barraReal.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraReal.Name = "barraReal"
        Me.barraReal.Padding = New System.Windows.Forms.Padding(4)
        Me.barraReal.WrapContents = True
        Me.barraReal.Controls.Add(Me.lblReal)
        '
        'lblReal
        '
        Me.lblReal.AutoSize = True
        Me.lblReal.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblReal.Name = "lblReal"
        Me.lblReal.Text = "Real del servicio:"
        '
        'barraFecha
        '
        Me.barraFecha.AutoSize = True
        Me.barraFecha.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraFecha.Name = "barraFecha"
        Me.barraFecha.Padding = New System.Windows.Forms.Padding(4)
        Me.barraFecha.WrapContents = True
        Me.barraFecha.Controls.Add(Me.lblFecha)
        Me.barraFecha.Controls.Add(Me.dtFecha)
        '
        'lblFecha
        '
        Me.lblFecha.AutoSize = True
        Me.lblFecha.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblFecha.Name = "lblFecha"
        Me.lblFecha.Text = "Fecha"
        '
        'dtFecha
        '
        Me.dtFecha.Format = System.Windows.Forms.DateTimePickerFormat.Short
        Me.dtFecha.Name = "dtFecha"
        Me.dtFecha.AccessibleName = "Fecha"
        Me.dtFecha.Width = 110
        '
        'FormProduccion
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.divPrincipal)
        Me.Controls.Add(Me.lblNota)
        Me.Controls.Add(Me.barraReal)
        Me.Controls.Add(Me.barraFecha)
        Me.Name = "FormProduccion"
        Me.Text = "Produccion"
        Me.divPrincipal.Panel1.ResumeLayout(False)
        Me.divPrincipal.Panel2.ResumeLayout(False)
        CType(Me.divPrincipal, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divPrincipal.ResumeLayout(False)
        CType(Me.gridMinutas, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divRequerimientos.Panel1.ResumeLayout(False)
        Me.divRequerimientos.Panel2.ResumeLayout(False)
        CType(Me.divRequerimientos, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divRequerimientos.ResumeLayout(False)
        CType(Me.gridRequerimientos, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridLineas, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraReal.ResumeLayout(False)
        Me.barraReal.PerformLayout()
        Me.barraFecha.ResumeLayout(False)
        Me.barraFecha.PerformLayout()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents divPrincipal As System.Windows.Forms.SplitContainer
    Friend WithEvents gridMinutas As System.Windows.Forms.DataGridView
    Friend WithEvents divRequerimientos As System.Windows.Forms.SplitContainer
    Friend WithEvents gridRequerimientos As System.Windows.Forms.DataGridView
    Friend WithEvents gridLineas As System.Windows.Forms.DataGridView
    Friend WithEvents lblNota As System.Windows.Forms.Label
    Friend WithEvents barraReal As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblReal As System.Windows.Forms.Label
    Friend WithEvents barraFecha As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblFecha As System.Windows.Forms.Label
    Friend WithEvents dtFecha As System.Windows.Forms.DateTimePicker
End Class
