<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormPrincipal
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
    'No lo modifique con el editor de código: los permisos, el tema y las acciones van en FormPrincipal.vb.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me._menu = New System.Windows.Forms.MenuStrip()
        Me._estado = New System.Windows.Forms.StatusStrip()
        Me._etiquetaEstado = New System.Windows.Forms.ToolStripStatusLabel()
        Me.lblVersion = New System.Windows.Forms.ToolStripStatusLabel()
        Me.mnuCatalogo = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuProductosVariantesYEmpaques = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuProveedoresYPrecios = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuImportarCatalogo = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuMenus = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuRecetas = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuMinutasYNecesidades = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuServiciosYEstructuras = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuImportarRecetas = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuProduccion = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuCompras = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuPrevisionYPedidos = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuConsolidadoDeComprasTodasLasOperaciones = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuAlmacen = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuStockEInventarioInicial = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuInventarioFisico = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuCierresYControl = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuPendientesCierresYFoodCost = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuReportesDelSgp = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuPlanProduccionChef = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuContratosYClientes = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuGastosYResultadoMensual = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuAdministracion = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuUsuariosYRoles = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuMatrizDeAcceso = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuOperacionesYAlmacenes = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuAuditoria = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuCargaDeDatosReales = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuSincronizacionYRespaldoTI = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuSesion = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuCambiarClave = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuConexionConElServidor = New System.Windows.Forms.ToolStripMenuItem()
        Me.sepSesion2 = New System.Windows.Forms.ToolStripSeparator()
        Me.mnuSalir = New System.Windows.Forms.ToolStripMenuItem()
        Me.mnuVentanas = New System.Windows.Forms.ToolStripMenuItem()
        Me._menu.SuspendLayout()
        Me._estado.SuspendLayout()
        Me.SuspendLayout()
        '
        '_menu
        '
        Me._menu.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuCatalogo, Me.mnuMenus, Me.mnuCompras, Me.mnuAlmacen, Me.mnuCierresYControl, Me.mnuAdministracion, Me.mnuSesion, Me.mnuVentanas})
        Me._menu.Location = New System.Drawing.Point(0, 0)
        Me._menu.MdiWindowListItem = Me.mnuVentanas
        Me._menu.Name = "menuPrincipal"
        Me._menu.Padding = New System.Windows.Forms.Padding(6, 3, 0, 3)
        Me._menu.Size = New System.Drawing.Size(1024, 25)
        Me._menu.TabIndex = 0
        '
        'mnuCatalogo
        '
        Me.mnuCatalogo.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuProductosVariantesYEmpaques, Me.mnuProveedoresYPrecios, Me.mnuImportarCatalogo})
        Me.mnuCatalogo.Name = "mnuCatalogo"
        Me.mnuCatalogo.Text = "&Catalogo"
        '
        'mnuProductosVariantesYEmpaques
        '
        Me.mnuProductosVariantesYEmpaques.Name = "mnuProductosVariantesYEmpaques"
        Me.mnuProductosVariantesYEmpaques.Text = "&Productos, variantes y empaques"
        '
        'mnuProveedoresYPrecios
        '
        Me.mnuProveedoresYPrecios.Name = "mnuProveedoresYPrecios"
        Me.mnuProveedoresYPrecios.Text = "Pro&veedores y precios"
        '
        'mnuImportarCatalogo
        '
        Me.mnuImportarCatalogo.Name = "mnuImportarCatalogo"
        Me.mnuImportarCatalogo.Text = "&Importar catalogo..."
        '
        'mnuMenus
        '
        Me.mnuMenus.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuRecetas, Me.mnuMinutasYNecesidades, Me.mnuServiciosYEstructuras, Me.mnuImportarRecetas, Me.mnuProduccion, Me.mnuPlanProduccionChef})
        Me.mnuMenus.Name = "mnuMenus"
        Me.mnuMenus.Text = "&Menus"
        '
        'mnuRecetas
        '
        Me.mnuRecetas.Name = "mnuRecetas"
        Me.mnuRecetas.Text = "&Recetas"
        '
        'mnuMinutasYNecesidades
        '
        Me.mnuMinutasYNecesidades.Name = "mnuMinutasYNecesidades"
        Me.mnuMinutasYNecesidades.Text = "&Minutas y necesidades"
        '
        'mnuServiciosYEstructuras
        '
        Me.mnuServiciosYEstructuras.Name = "mnuServiciosYEstructuras"
        Me.mnuServiciosYEstructuras.Text = "&Servicios y estructuras"
        '
        'mnuImportarRecetas
        '
        Me.mnuImportarRecetas.Name = "mnuImportarRecetas"
        Me.mnuImportarRecetas.Text = "&Importar recetas..."
        '
        'mnuProduccion
        '
        Me.mnuProduccion.Name = "mnuProduccion"
        Me.mnuProduccion.Text = "&Produccion"
        '
        'mnuCompras
        '
        Me.mnuCompras.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuPrevisionYPedidos, Me.mnuConsolidadoDeComprasTodasLasOperaciones})
        Me.mnuCompras.Name = "mnuCompras"
        Me.mnuCompras.Text = "C&ompras"
        '
        'mnuPrevisionYPedidos
        '
        Me.mnuPrevisionYPedidos.Name = "mnuPrevisionYPedidos"
        Me.mnuPrevisionYPedidos.Text = "&Prevision y pedidos"
        '
        'mnuConsolidadoDeComprasTodasLasOperaciones
        '
        Me.mnuConsolidadoDeComprasTodasLasOperaciones.Name = "mnuConsolidadoDeComprasTodasLasOperaciones"
        Me.mnuConsolidadoDeComprasTodasLasOperaciones.Text = "&Consolidado de compras (todas las operaciones)"
        '
        'mnuAlmacen
        '
        Me.mnuAlmacen.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuStockEInventarioInicial, Me.mnuInventarioFisico})
        Me.mnuAlmacen.Name = "mnuAlmacen"
        Me.mnuAlmacen.Text = "A&lmacen"
        '
        'mnuStockEInventarioInicial
        '
        Me.mnuStockEInventarioInicial.Name = "mnuStockEInventarioInicial"
        Me.mnuStockEInventarioInicial.Text = "&Stock e inventario inicial"
        '
        'mnuInventarioFisico
        '
        Me.mnuInventarioFisico.Name = "mnuInventarioFisico"
        Me.mnuInventarioFisico.Text = "&Inventario fisico"
        '
        'mnuCierresYControl
        '
        Me.mnuCierresYControl.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuPendientesCierresYFoodCost, Me.mnuContratosYClientes, Me.mnuGastosYResultadoMensual, Me.mnuReportesDelSgp})
        Me.mnuCierresYControl.Name = "mnuCierresYControl"
        Me.mnuCierresYControl.Text = "Cie&rres y control"
        '
        'mnuPendientesCierresYFoodCost
        '
        Me.mnuPendientesCierresYFoodCost.Name = "mnuPendientesCierresYFoodCost"
        Me.mnuPendientesCierresYFoodCost.Text = "&Pendientes, cierres y Food Cost"
        'mnuReportesDelSgp
        Me.mnuReportesDelSgp.Name = "mnuReportesDelSgp"
        Me.mnuReportesDelSgp.Text = "&Reportes (SGP y plan)..."
        'mnuPlanProduccionChef
        Me.mnuPlanProduccionChef.Name = "mnuPlanProduccionChef"
        Me.mnuPlanProduccionChef.Text = "Produccion del &chef (raciones a producir)..."
        '
        'mnuContratosYClientes
        '
        Me.mnuContratosYClientes.Name = "mnuContratosYClientes"
        Me.mnuContratosYClientes.Text = "&Contratos y clientes"
        '
        'mnuGastosYResultadoMensual
        '
        Me.mnuGastosYResultadoMensual.Name = "mnuGastosYResultadoMensual"
        Me.mnuGastosYResultadoMensual.Text = "&Gastos y resultado mensual"
        '
        'mnuAdministracion
        '
        Me.mnuAdministracion.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuUsuariosYRoles, Me.mnuMatrizDeAcceso, Me.mnuOperacionesYAlmacenes, Me.mnuAuditoria, Me.mnuCargaDeDatosReales, Me.mnuSincronizacionYRespaldoTI})
        Me.mnuAdministracion.Name = "mnuAdministracion"
        Me.mnuAdministracion.Text = "&Administracion"
        '
        'mnuUsuariosYRoles
        '
        Me.mnuUsuariosYRoles.Name = "mnuUsuariosYRoles"
        Me.mnuUsuariosYRoles.Text = "&Usuarios y roles"
        '
        'mnuMatrizDeAcceso
        '
        Me.mnuMatrizDeAcceso.Name = "mnuMatrizDeAcceso"
        Me.mnuMatrizDeAcceso.Text = "&Matriz de acceso"
        '
        'mnuOperacionesYAlmacenes
        '
        Me.mnuOperacionesYAlmacenes.Name = "mnuOperacionesYAlmacenes"
        Me.mnuOperacionesYAlmacenes.Text = "&Operaciones y almacenes"
        '
        'mnuAuditoria
        '
        Me.mnuAuditoria.Name = "mnuAuditoria"
        Me.mnuAuditoria.Text = "&Auditoria"
        '
        'mnuCargaDeDatosReales
        '
        Me.mnuCargaDeDatosReales.Name = "mnuCargaDeDatosReales"
        Me.mnuCargaDeDatosReales.Text = "&Carga de datos reales"
        '
        'mnuSincronizacionYRespaldoTI
        '
        Me.mnuSincronizacionYRespaldoTI.Name = "mnuSincronizacionYRespaldoTI"
        Me.mnuSincronizacionYRespaldoTI.Text = "&Sincronizacion y respaldo (TI)"
        '
        'mnuSesion
        '
        Me.mnuSesion.DropDownItems.AddRange(New System.Windows.Forms.ToolStripItem() {Me.mnuCambiarClave, Me.mnuConexionConElServidor, Me.sepSesion2, Me.mnuSalir})
        Me.mnuSesion.Name = "mnuSesion"
        Me.mnuSesion.Text = "&Sesion"
        '
        'mnuCambiarClave
        '
        Me.mnuCambiarClave.Name = "mnuCambiarClave"
        Me.mnuCambiarClave.Text = "Cambiar &clave..."
        '
        'mnuConexionConElServidor
        '
        Me.mnuConexionConElServidor.Name = "mnuConexionConElServidor"
        Me.mnuConexionConElServidor.Text = "&Conexion con el servidor..."
        '
        'sepSesion2
        '
        Me.sepSesion2.Name = "sepSesion2"
        '
        'mnuSalir
        '
        Me.mnuSalir.Name = "mnuSalir"
        Me.mnuSalir.Text = "&Salir"
        '
        'mnuVentanas
        '
        Me.mnuVentanas.Name = "mnuVentanas"
        Me.mnuVentanas.Text = "&Ventanas"
        '
        '_estado
        '
        Me._estado.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me._etiquetaEstado, Me.lblVersion})
        Me._estado.Location = New System.Drawing.Point(0, 746)
        Me._estado.Name = "barraEstado"
        Me._estado.Size = New System.Drawing.Size(1024, 22)
        Me._estado.TabIndex = 1
        '
        '_etiquetaEstado
        '
        Me._etiquetaEstado.Name = "lblEstado"
        Me._etiquetaEstado.Spring = True
        Me._etiquetaEstado.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'lblVersion
        '
        Me.lblVersion.Name = "lblVersion"
        Me.lblVersion.Text = "Version"
        '
        'FormPrincipal
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.ClientSize = New System.Drawing.Size(1024, 768)
        Me.Controls.Add(Me._menu)
        Me.Controls.Add(Me._estado)
        Me.IsMdiContainer = True
        Me.MainMenuStrip = Me._menu
        Me.Name = "FormPrincipal"
        Me.Text = "AppSistema"
        Me.WindowState = System.Windows.Forms.FormWindowState.Maximized
        Me._menu.ResumeLayout(False)
        Me._menu.PerformLayout()
        Me._estado.ResumeLayout(False)
        Me._estado.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()
    End Sub

    Friend WithEvents _menu As System.Windows.Forms.MenuStrip
    Friend WithEvents _estado As System.Windows.Forms.StatusStrip
    Friend WithEvents _etiquetaEstado As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents lblVersion As System.Windows.Forms.ToolStripStatusLabel
    Friend WithEvents mnuCatalogo As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuProductosVariantesYEmpaques As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuProveedoresYPrecios As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuImportarCatalogo As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuMenus As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuReportesDelSgp As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuPlanProduccionChef As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuRecetas As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuMinutasYNecesidades As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuServiciosYEstructuras As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuImportarRecetas As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuProduccion As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuCompras As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuPrevisionYPedidos As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuConsolidadoDeComprasTodasLasOperaciones As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuAlmacen As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuStockEInventarioInicial As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuInventarioFisico As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuCierresYControl As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuPendientesCierresYFoodCost As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuContratosYClientes As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuGastosYResultadoMensual As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuAdministracion As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuUsuariosYRoles As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuMatrizDeAcceso As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuOperacionesYAlmacenes As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuAuditoria As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuCargaDeDatosReales As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuSincronizacionYRespaldoTI As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuSesion As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuCambiarClave As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuConexionConElServidor As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents sepSesion2 As System.Windows.Forms.ToolStripSeparator
    Friend WithEvents mnuSalir As System.Windows.Forms.ToolStripMenuItem
    Friend WithEvents mnuVentanas As System.Windows.Forms.ToolStripMenuItem
End Class
