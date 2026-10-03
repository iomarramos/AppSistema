<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormStock
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
        Me.lblTituloDiseno = New System.Windows.Forms.Label()
        Me.grp1 = New System.Windows.Forms.GroupBox()
        Me.grp1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.grp1.Text = "Filtros y almacén"
        Me.grp1.Name = "grp1"
        Me.grp2 = New System.Windows.Forms.GroupBox()
        Me.grp2.Dock = System.Windows.Forms.DockStyle.Fill
        Me.grp2.Text = "Stock valorizado"
        Me.grp2.Name = "grp2"
        Me.grp3 = New System.Windows.Forms.GroupBox()
        Me.grp3.Dock = System.Windows.Forms.DockStyle.Fill
        Me.grp3.Text = "Kárdex / movimientos"
        Me.grp3.Name = "grp3"
        Me.grp4 = New System.Windows.Forms.GroupBox()
        Me.grp4.Dock = System.Windows.Forms.DockStyle.Fill
        Me.grp4.Text = "Totales / documentos"
        Me.grp4.Name = "grp4"
        Me.SuspendLayout()
        Me.tabla.ColumnCount = 1
        Me.tabla.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.tabla.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tabla.Padding = New System.Windows.Forms.Padding(12)
        Me.tabla.RowCount = 5
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48.0!))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.00!))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.00!))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.00!))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25.00!))
        Me.lblTituloDiseno.AutoSize = True
        Me.lblTituloDiseno.Dock = System.Windows.Forms.DockStyle.Fill
        Me.lblTituloDiseno.Font = New System.Drawing.Font("Segoe UI", 14.0!, System.Drawing.FontStyle.Bold)
        Me.lblTituloDiseno.Text = "Stock y kárdex"
        Me.lblTituloDiseno.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.tabla.Controls.Add(Me.lblTituloDiseno, 0, 0)
        Me.tabla.Controls.Add(Me.grp1, 0, 1)
        Me.tabla.Controls.Add(Me.grp2, 0, 2)
        Me.tabla.Controls.Add(Me.grp3, 0, 3)
        Me.tabla.Controls.Add(Me.grp4, 0, 4)
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1100, 720)
        Me.Controls.Add(Me.tabla)
        Me.Name = "FormStock"
        Me.Text = "Stock y kárdex"
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents tabla As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents lblTituloDiseno As System.Windows.Forms.Label
    Friend WithEvents grp1 As System.Windows.Forms.GroupBox
    Friend WithEvents grp2 As System.Windows.Forms.GroupBox
    Friend WithEvents grp3 As System.Windows.Forms.GroupBox
    Friend WithEvents grp4 As System.Windows.Forms.GroupBox
End Class
