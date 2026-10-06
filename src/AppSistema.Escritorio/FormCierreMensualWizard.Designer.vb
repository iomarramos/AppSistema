<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormCierreMensualWizard
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
        Me.barraMes = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblMes = New System.Windows.Forms.Label()
        Me.dtMes = New System.Windows.Forms.DateTimePicker()
        Me.barraAcciones = New System.Windows.Forms.FlowLayoutPanel()
        Me.lblPasos = New System.Windows.Forms.Label()
        Me.gridPasos = New System.Windows.Forms.DataGridView()
        Me.lblCalendario = New System.Windows.Forms.Label()
        Me.gridCalendario = New System.Windows.Forms.DataGridView()
        Me.lblControles = New System.Windows.Forms.Label()
        Me.gridControles = New System.Windows.Forms.DataGridView()
        Me.lblEstado = New System.Windows.Forms.Label()
        Me.tabla.SuspendLayout()
        Me.barraMes.SuspendLayout()
        CType(Me.gridPasos, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridCalendario, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.gridControles, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'tabla
        '
        Me.tabla.ColumnCount = 1
        Me.tabla.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.tabla.Controls.Add(Me.barraMes, 0, 0)
        Me.tabla.Controls.Add(Me.barraAcciones, 0, 1)
        Me.tabla.Controls.Add(Me.lblPasos, 0, 2)
        Me.tabla.Controls.Add(Me.gridPasos, 0, 3)
        Me.tabla.Controls.Add(Me.lblCalendario, 0, 4)
        Me.tabla.Controls.Add(Me.gridCalendario, 0, 5)
        Me.tabla.Controls.Add(Me.lblControles, 0, 6)
        Me.tabla.Controls.Add(Me.gridControles, 0, 7)
        Me.tabla.Controls.Add(Me.lblEstado, 0, 8)
        Me.tabla.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tabla.Name = "tabla"
        Me.tabla.RowCount = 9
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35.0!))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35.0!))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30.0!))
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize))
        '
        'barraMes
        '
        Me.barraMes.AutoSize = True
        Me.barraMes.Controls.Add(Me.lblMes)
        Me.barraMes.Controls.Add(Me.dtMes)
        Me.barraMes.Name = "barraMes"
        '
        'lblMes
        '
        Me.lblMes.AutoSize = True
        Me.lblMes.Name = "lblMes"
        Me.lblMes.Text = "Mes"
        '
        'dtMes
        '
        Me.dtMes.Format = System.Windows.Forms.DateTimePickerFormat.Custom
        Me.dtMes.CustomFormat = "MMMM yyyy"
        Me.dtMes.Name = "dtMes"
        Me.dtMes.AccessibleName = "Mes"
        Me.dtMes.Width = 150
        '
        'barraAcciones
        '
        Me.barraAcciones.AutoSize = True
        Me.barraAcciones.Name = "barraAcciones"
        '
        'lblPasos
        '
        Me.lblPasos.AutoSize = True
        Me.lblPasos.Name = "lblPasos"
        Me.lblPasos.Text = "Cierre del mes: 8 pasos (OK, PENDIENTE o INFO)"
        '
        'gridPasos
        '
        Me.gridPasos.AllowUserToAddRows = False
        Me.gridPasos.AllowUserToDeleteRows = False
        Me.gridPasos.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridPasos.Name = "gridPasos"
        Me.gridPasos.AccessibleName = "Pasos"
        Me.gridPasos.ReadOnly = True
        Me.gridPasos.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        '
        'lblCalendario
        '
        Me.lblCalendario.AutoSize = True
        Me.lblCalendario.Name = "lblCalendario"
        Me.lblCalendario.Text = "Calendario del mes: Cerrado, Con pendientes, Listo o Abierto (dia futuro)"
        '
        'gridCalendario
        '
        Me.gridCalendario.AllowUserToAddRows = False
        Me.gridCalendario.AllowUserToDeleteRows = False
        Me.gridCalendario.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridCalendario.Name = "gridCalendario"
        Me.gridCalendario.AccessibleName = "Calendario"
        Me.gridCalendario.ReadOnly = True
        Me.gridCalendario.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        '
        'lblControles
        '
        Me.lblControles.AutoSize = True
        Me.lblControles.Name = "lblControles"
        Me.lblControles.Text = "Checklist del dia seleccionado en el calendario (columna Ir a)"
        '
        'gridControles
        '
        Me.gridControles.AllowUserToAddRows = False
        Me.gridControles.AllowUserToDeleteRows = False
        Me.gridControles.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gridControles.Name = "gridControles"
        Me.gridControles.AccessibleName = "Controles"
        Me.gridControles.ReadOnly = True
        Me.gridControles.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        '
        'lblEstado
        '
        Me.lblEstado.AutoSize = True
        Me.lblEstado.Name = "lblEstado"
        Me.lblEstado.Text = "Cerrar el mes solo cuando los pasos 1, 2, 4 y 5 esten en OK. El cierre queda registrado."
        '
        'FormCierreMensualWizard
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1200, 680)
        Me.Controls.Add(Me.tabla)
        Me.Name = "FormCierreMensualWizard"
        Me.Text = "Cierre mensual"
        Me.tabla.ResumeLayout(False)
        Me.tabla.PerformLayout()
        Me.barraMes.ResumeLayout(False)
        Me.barraMes.PerformLayout()
        CType(Me.gridPasos, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridCalendario, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.gridControles, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
    End Sub
    Friend WithEvents tabla As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents barraMes As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblMes As System.Windows.Forms.Label
    Friend WithEvents dtMes As System.Windows.Forms.DateTimePicker
    Friend WithEvents barraAcciones As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents lblPasos As System.Windows.Forms.Label
    Friend WithEvents gridPasos As System.Windows.Forms.DataGridView
    Friend WithEvents lblCalendario As System.Windows.Forms.Label
    Friend WithEvents gridCalendario As System.Windows.Forms.DataGridView
    Friend WithEvents lblControles As System.Windows.Forms.Label
    Friend WithEvents gridControles As System.Windows.Forms.DataGridView
    Friend WithEvents lblEstado As System.Windows.Forms.Label
End Class
