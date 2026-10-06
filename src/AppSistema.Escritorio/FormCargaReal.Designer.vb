<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormCargaReal
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
        Me.txtResultado = New System.Windows.Forms.TextBox()
        Me.lblResultado = New System.Windows.Forms.Label()
        Me.lblEstado = New System.Windows.Forms.Label()
        Me.tablaPasos = New System.Windows.Forms.TableLayoutPanel()
        Me.lblDescripcion = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'txtResultado
        '
        Me.txtResultado.Dock = System.Windows.Forms.DockStyle.Fill
        Me.txtResultado.Font = New System.Drawing.Font("Consolas", 9.0!)
        Me.txtResultado.Multiline = True
        Me.txtResultado.Name = "txtResultado"
        Me.txtResultado.AccessibleName = "Resultado"
        Me.txtResultado.ReadOnly = True
        Me.txtResultado.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.txtResultado.WordWrap = False
        '
        'lblResultado
        '
        Me.lblResultado.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblResultado.Height = 22
        Me.lblResultado.Name = "lblResultado"
        Me.lblResultado.Padding = New System.Windows.Forms.Padding(6, 4, 4, 0)
        Me.lblResultado.Text = "Resultado de la ultima carga:"
        '
        'lblEstado
        '
        Me.lblEstado.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblEstado.Height = 84
        Me.lblEstado.Name = "lblEstado"
        Me.lblEstado.Padding = New System.Windows.Forms.Padding(6)
        '
        'tablaPasos
        '
        Me.tablaPasos.AutoSize = True
        Me.tablaPasos.ColumnCount = 3
        Me.tablaPasos.Dock = System.Windows.Forms.DockStyle.Top
        Me.tablaPasos.Name = "tablaPasos"
        Me.tablaPasos.Padding = New System.Windows.Forms.Padding(6)
        '
        'lblDescripcion
        '
        Me.lblDescripcion.Dock = System.Windows.Forms.DockStyle.Top
        Me.lblDescripcion.Height = 34
        Me.lblDescripcion.Name = "lblDescripcion"
        Me.lblDescripcion.Padding = New System.Windows.Forms.Padding(4)
        Me.lblDescripcion.Text = "Siga los pasos en orden. Cada paso se puede repetir: lo que ya existe no se duplica ni se pisa. Los archivos los genera herramientas/ordenar_datos_reales.py (ver datos/real/LEEME.md)."
        '
        'FormCargaReal
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.txtResultado)
        Me.Controls.Add(Me.lblResultado)
        Me.Controls.Add(Me.lblEstado)
        Me.Controls.Add(Me.tablaPasos)
        Me.Controls.Add(Me.lblDescripcion)
        Me.Name = "FormCargaReal"
        Me.Text = "Carga de datos reales"
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents txtResultado As System.Windows.Forms.TextBox
    Friend WithEvents lblResultado As System.Windows.Forms.Label
    Friend WithEvents lblEstado As System.Windows.Forms.Label
    Friend WithEvents tablaPasos As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents lblDescripcion As System.Windows.Forms.Label
End Class
