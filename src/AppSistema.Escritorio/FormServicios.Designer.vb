<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormServicios
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
        Me.gridServicios = New System.Windows.Forms.DataGridView()
        Me.barraServicios = New System.Windows.Forms.FlowLayoutPanel()
        Me.divideDerecha = New System.Windows.Forms.SplitContainer()
        Me.gridEstructuras = New System.Windows.Forms.DataGridView()
        Me.barraEstructuras = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblEstructuras = New System.Windows.Forms.Label()
        Me.gridOperacion = New System.Windows.Forms.DataGridView()
        Me.barraOperacion = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblOperacion = New System.Windows.Forms.Label()
        CType(Me.divide, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divide.Panel1.SuspendLayout()
        Me.divide.Panel2.SuspendLayout()
        Me.divide.SuspendLayout()
        CType(Me.divideDerecha, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.divideDerecha.Panel1.SuspendLayout()
        Me.divideDerecha.Panel2.SuspendLayout()
        Me.divideDerecha.SuspendLayout()
        CType(Me.gridServicios, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridEstructuras, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridOperacion, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'divide
        '
        Me.divide.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divide.Name = "divide"
        Me.divide.Panel1.Controls.Add(Me.gridServicios)
        Me.divide.Panel1.Controls.Add(Me.barraServicios)
        Me.divide.Panel2.Controls.Add(Me.divideDerecha)
        '
        'gridServicios
        '
        Me.gridServicios.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridServicios.Name = "gridServicios"
        Me.gridServicios.AccessibleName = "Servicios"
        '
        'barraServicios
        '
        Me.barraServicios.AutoSize = True
        Me.barraServicios.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraServicios.Name = "barraServicios"
        '
        'divideDerecha
        '
        Me.divideDerecha.Dock = System.Windows.Forms.DockStyle.Fill
        Me.divideDerecha.Name = "divideDerecha"
        Me.divideDerecha.Orientation = System.Windows.Forms.Orientation.Horizontal
        Me.divideDerecha.Panel1.Controls.Add(Me.gridEstructuras)
        Me.divideDerecha.Panel1.Controls.Add(Me.barraEstructuras)
        Me.divideDerecha.Panel1.Controls.Add(Me.lblEstructuras)
        Me.divideDerecha.Panel2.Controls.Add(Me.gridOperacion)
        Me.divideDerecha.Panel2.Controls.Add(Me.barraOperacion)
        Me.divideDerecha.Panel2.Controls.Add(Me.lblOperacion)
        '
        'gridEstructuras
        '
        Me.gridEstructuras.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridEstructuras.Name = "gridEstructuras"
        Me.gridEstructuras.AccessibleName = "Estructuras"
        '
        'barraEstructuras
        '
        Me.barraEstructuras.AutoSize = True
        Me.barraEstructuras.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraEstructuras.Name = "barraEstructuras"
        '
        'lblEstructuras
        '
        Me.lblEstructuras.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblEstructuras.Name = "lblEstructuras"
        Me.lblEstructuras.Padding = New System.Windows.Forms.Padding(4)
        Me.lblEstructuras.Text = "Estructura del servicio (orden en que se sirve). Factor de consumo = parte de los comensales que toma el componente."
        '
        'gridOperacion
        '
        Me.gridOperacion.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridOperacion.Name = "gridOperacion"
        Me.gridOperacion.AccessibleName = "Operacion"
        '
        'barraOperacion
        '
        Me.barraOperacion.AutoSize = True
        Me.barraOperacion.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraOperacion.Name = "barraOperacion"
        '
        'lblOperacion
        '
        Me.lblOperacion.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblOperacion.Name = "lblOperacion"
        Me.lblOperacion.Padding = New System.Windows.Forms.Padding(4)
        '
        'FormServicios
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.divide)
        Me.Name = "FormServicios"
        Me.Text = "Servicios y estructuras"
        Me.divide.Panel1.ResumeLayout(False)
        Me.divide.Panel1.PerformLayout()
        Me.divide.Panel2.ResumeLayout(False)
        CType(Me.divide, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divide.ResumeLayout(False)
        Me.divideDerecha.Panel1.ResumeLayout(False)
        Me.divideDerecha.Panel1.PerformLayout()
        Me.divideDerecha.Panel2.ResumeLayout(False)
        Me.divideDerecha.Panel2.PerformLayout()
        CType(Me.divideDerecha, System.ComponentModel.ISupportInitialize).EndInit()
        Me.divideDerecha.ResumeLayout(False)
        CType(Me.gridServicios, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridEstructuras, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridOperacion, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents divide As System.Windows.Forms.SplitContainer
    Friend WithEvents gridServicios As System.Windows.Forms.DataGridView
    Friend WithEvents barraServicios As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents divideDerecha As System.Windows.Forms.SplitContainer
    Friend WithEvents gridEstructuras As System.Windows.Forms.DataGridView
    Friend WithEvents barraEstructuras As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblEstructuras As System.Windows.Forms.Label
    Friend WithEvents gridOperacion As System.Windows.Forms.DataGridView
    Friend WithEvents barraOperacion As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblOperacion As System.Windows.Forms.Label
End Class
