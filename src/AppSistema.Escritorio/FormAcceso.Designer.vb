<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormAcceso
    Inherits System.Windows.Forms.Form

    'Form reemplaza a Dispose para limpiar la lista de componentes.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Requerido por el Diseñador de Windows Forms
    Private components As System.ComponentModel.IContainer

    'NOTA: el Diseñador de Windows Forms necesita el siguiente procedimiento. Se puede modificar con el Diseñador.
    'No lo modifique con el editor de código: la lógica de acceso va en FormAcceso.vb.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.tabla = New System.Windows.Forms.TableLayoutPanel()
        Me.lblTitulo = New System.Windows.Forms.Label()
        Me.lblEmpresa = New System.Windows.Forms.Label()
        Me._empresa = New System.Windows.Forms.TextBox()
        Me.lblUsuario = New System.Windows.Forms.Label()
        Me._usuario = New System.Windows.Forms.TextBox()
        Me.lblClave = New System.Windows.Forms.Label()
        Me._clave = New System.Windows.Forms.TextBox()
        Me.botones = New System.Windows.Forms.FlowLayoutPanel()
        Me.btnSalir = New System.Windows.Forms.Button()
        Me.btnEntrar = New System.Windows.Forms.Button()
        Me.btnConexion = New System.Windows.Forms.Button()
        Me.tabla.SuspendLayout()
        Me.botones.SuspendLayout()
        Me.SuspendLayout()
        '
        'tabla
        '
        Me.tabla.AutoSize = True
        Me.tabla.ColumnCount = 2
        Me.tabla.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
        Me.tabla.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle())
        Me.tabla.Controls.Add(Me.lblTitulo, 0, 0)
        Me.tabla.Controls.Add(Me.lblEmpresa, 0, 1)
        Me.tabla.Controls.Add(Me._empresa, 1, 1)
        Me.tabla.Controls.Add(Me.lblUsuario, 0, 2)
        Me.tabla.Controls.Add(Me._usuario, 1, 2)
        Me.tabla.Controls.Add(Me.lblClave, 0, 3)
        Me.tabla.Controls.Add(Me._clave, 1, 3)
        Me.tabla.Dock = System.Windows.Forms.DockStyle.Fill
        Me.tabla.Location = New System.Drawing.Point(0, 0)
        Me.tabla.Name = "tabla"
        Me.tabla.Padding = New System.Windows.Forms.Padding(16)
        Me.tabla.RowCount = 4
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tabla.RowStyles.Add(New System.Windows.Forms.RowStyle())
        Me.tabla.Size = New System.Drawing.Size(380, 150)
        Me.tabla.TabIndex = 0
        '
        'lblTitulo
        '
        Me.lblTitulo.AutoSize = True
        Me.tabla.SetColumnSpan(Me.lblTitulo, 2)
        Me.lblTitulo.Margin = New System.Windows.Forms.Padding(3, 3, 3, 12)
        Me.lblTitulo.Name = "lblTitulo"
        Me.lblTitulo.Text = "Sistema de menus, compras e inventarios"
        '
        'lblEmpresa
        '
        Me.lblEmpresa.Anchor = System.Windows.Forms.AnchorStyles.Left
        Me.lblEmpresa.AutoSize = True
        Me.lblEmpresa.Margin = New System.Windows.Forms.Padding(3, 7, 12, 3)
        Me.lblEmpresa.Name = "lblEmpresa"
        Me.lblEmpresa.Text = "Empresa"
        '
        '_empresa
        '
        Me._empresa.AccessibleName = "Empresa"
        Me._empresa.Name = "txtEmpresa"
        Me._empresa.Size = New System.Drawing.Size(260, 23)
        Me._empresa.TabIndex = 1
        '
        'lblUsuario
        '
        Me.lblUsuario.Anchor = System.Windows.Forms.AnchorStyles.Left
        Me.lblUsuario.AutoSize = True
        Me.lblUsuario.Margin = New System.Windows.Forms.Padding(3, 7, 12, 3)
        Me.lblUsuario.Name = "lblUsuario"
        Me.lblUsuario.Text = "Usuario"
        '
        '_usuario
        '
        Me._usuario.AccessibleName = "Usuario"
        Me._usuario.Name = "txtUsuario"
        Me._usuario.Size = New System.Drawing.Size(260, 23)
        Me._usuario.TabIndex = 2
        '
        'lblClave
        '
        Me.lblClave.Anchor = System.Windows.Forms.AnchorStyles.Left
        Me.lblClave.AutoSize = True
        Me.lblClave.Margin = New System.Windows.Forms.Padding(3, 7, 12, 3)
        Me.lblClave.Name = "lblClave"
        Me.lblClave.Text = "Clave"
        '
        '_clave
        '
        Me._clave.AccessibleName = "Clave"
        Me._clave.Name = "txtClave"
        Me._clave.Size = New System.Drawing.Size(260, 23)
        Me._clave.TabIndex = 3
        Me._clave.UseSystemPasswordChar = True
        '
        'botones
        '
        Me.botones.AutoSize = True
        Me.botones.Controls.Add(Me.btnSalir)
        Me.botones.Controls.Add(Me.btnEntrar)
        Me.botones.Controls.Add(Me.btnConexion)
        Me.botones.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.botones.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft
        Me.botones.Name = "botones"
        Me.botones.Padding = New System.Windows.Forms.Padding(12)
        Me.botones.TabIndex = 1
        '
        'btnSalir
        '
        Me.btnSalir.AccessibleName = "Salir"
        Me.btnSalir.AutoSize = True
        Me.btnSalir.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.btnSalir.Name = "btnSalir"
        Me.btnSalir.TabIndex = 6
        Me.btnSalir.Text = "Salir"
        '
        'btnEntrar
        '
        Me.btnEntrar.AccessibleName = "Iniciar sesion"
        Me.btnEntrar.AutoSize = True
        Me.btnEntrar.Name = "btnEntrar"
        Me.btnEntrar.TabIndex = 4
        Me.btnEntrar.Text = "Entrar"
        '
        'btnConexion
        '
        Me.btnConexion.AccessibleName = "Conexion con el servidor"
        Me.btnConexion.AutoSize = True
        Me.btnConexion.Name = "btnConexion"
        Me.btnConexion.TabIndex = 5
        Me.btnConexion.Text = "Conexion..."
        '
        'FormAcceso
        '
        Me.AcceptButton = Me.btnEntrar
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.AutoSize = True
        Me.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink
        Me.CancelButton = Me.btnSalir
        Me.ClientSize = New System.Drawing.Size(380, 210)
        Me.Controls.Add(Me.tabla)
        Me.Controls.Add(Me.botones)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormAcceso"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "AppSistema - Acceso"
        Me.tabla.ResumeLayout(False)
        Me.tabla.PerformLayout()
        Me.botones.ResumeLayout(False)
        Me.botones.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents tabla As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents lblTitulo As System.Windows.Forms.Label
    Friend WithEvents lblEmpresa As System.Windows.Forms.Label
    Friend WithEvents _empresa As System.Windows.Forms.TextBox
    Friend WithEvents lblUsuario As System.Windows.Forms.Label
    Friend WithEvents _usuario As System.Windows.Forms.TextBox
    Friend WithEvents lblClave As System.Windows.Forms.Label
    Friend WithEvents _clave As System.Windows.Forms.TextBox
    Friend WithEvents botones As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents btnSalir As System.Windows.Forms.Button
    Friend WithEvents btnEntrar As System.Windows.Forms.Button
    Friend WithEvents btnConexion As System.Windows.Forms.Button
End Class
