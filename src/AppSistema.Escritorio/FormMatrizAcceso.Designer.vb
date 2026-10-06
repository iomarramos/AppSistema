<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormMatrizAcceso
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
        Me.gridMatriz = New System.Windows.Forms.DataGridView()
        Me.lblDescripcion = New System.Windows.Forms.Label()
        Me.barraFiltros = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblUsuario = New System.Windows.Forms.Label()
        Me.cmbUsuario = New System.Windows.Forms.ComboBox()
        Me.lblOperacion = New System.Windows.Forms.Label()
        Me.cmbOperacion = New System.Windows.Forms.ComboBox()
        CType(Me.gridMatriz, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.barraFiltros.SuspendLayout()
        Me.SuspendLayout()
        '
        'gridMatriz
        '
        Me.gridMatriz.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridMatriz.Name = "gridMatriz"
        Me.gridMatriz.AccessibleName = "Matriz"
        '
        'lblDescripcion
        '
        Me.lblDescripcion.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblDescripcion.Height = 34
        Me.lblDescripcion.Name = "lblDescripcion"
        Me.lblDescripcion.Padding = New System.Windows.Forms.Padding(4)
        Me.lblDescripcion.Text = "Por rol = lo da su rol segun el alcance (operacion, zona o todas). Excepcion = lo que el superusuario concede o niega a la persona. Lo negado gana."
        '
        'barraFiltros
        '
        Me.barraFiltros.AutoSize = True
        Me.barraFiltros.Dock = System.Windows.Forms.DockStyle.Top
        Me.barraFiltros.Controls.Add(Me.lblUsuario)
        Me.barraFiltros.Controls.Add(Me.cmbUsuario)
        Me.barraFiltros.Controls.Add(Me.lblOperacion)
        Me.barraFiltros.Controls.Add(Me.cmbOperacion)
        Me.barraFiltros.Name = "barraFiltros"
        '
        'lblUsuario
        '
        Me.lblUsuario.AutoSize = True
        Me.lblUsuario.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblUsuario.Name = "lblUsuario"
        Me.lblUsuario.Text = "Usuario"
        '
        'cmbUsuario
        '
        Me.cmbUsuario.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbUsuario.Name = "cmbUsuario"
        Me.cmbUsuario.AccessibleName = "Usuario"
        Me.cmbUsuario.Width = 220
        '
        'lblOperacion
        '
        Me.lblOperacion.AutoSize = True
        Me.lblOperacion.Margin = New System.Windows.Forms.Padding(3, 9, 3, 3)
        Me.lblOperacion.Name = "lblOperacion"
        Me.lblOperacion.Text = "Operacion"
        '
        'cmbOperacion
        '
        Me.cmbOperacion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.cmbOperacion.Name = "cmbOperacion"
        Me.cmbOperacion.AccessibleName = "Operacion"
        Me.cmbOperacion.Width = 240
        '
        'FormMatrizAcceso
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.gridMatriz)
        Me.Controls.Add(Me.lblDescripcion)
        Me.Controls.Add(Me.barraFiltros)
        Me.Name = "FormMatrizAcceso"
        Me.Text = "Matriz de acceso"
        CType(Me.gridMatriz, System.ComponentModel.ISupportInitialize).EndInit()
        Me.barraFiltros.ResumeLayout(False)
        Me.barraFiltros.PerformLayout()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents gridMatriz As System.Windows.Forms.DataGridView
    Friend WithEvents lblDescripcion As System.Windows.Forms.Label
    Friend WithEvents barraFiltros As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblUsuario As System.Windows.Forms.Label
    Friend WithEvents cmbUsuario As System.Windows.Forms.ComboBox
    Friend WithEvents lblOperacion As System.Windows.Forms.Label
    Friend WithEvents cmbOperacion As System.Windows.Forms.ComboBox
End Class
