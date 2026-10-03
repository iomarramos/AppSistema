Namespace Seguridad

    ''' <summary>Códigos de permiso. Se crean por empresa al instalarla (tabla permiso).</summary>
    Public Module Permisos
        Public Const CatalogoVer As String = "CATALOGO_VER"
        Public Const CatalogoEditar As String = "CATALOGO_EDITAR"
        Public Const CatalogoImportar As String = "CATALOGO_IMPORTAR"
        Public Const ProveedoresEditar As String = "PROVEEDORES_EDITAR"
        Public Const PreciosEditar As String = "PRECIOS_EDITAR"
        Public Const StockContabilizar As String = "STOCK_CONTABILIZAR"
        Public Const UsuariosAdministrar As String = "USUARIOS_ADMINISTRAR"
        Public Const AuditoriaVer As String = "AUDITORIA_VER"
        Public Const MenusVer As String = "MENUS_VER"
        Public Const MenusConfigurar As String = "MENUS_CONFIGURAR"
        Public Const RecetasEditar As String = "RECETAS_EDITAR"
        Public Const RecetasAprobar As String = "RECETAS_APROBAR"
        Public Const MinutasEditar As String = "MINUTAS_EDITAR"
        Public Const MinutasAprobar As String = "MINUTAS_APROBAR"
        Public Const ComprasVer As String = "COMPRAS_VER"
        Public Const ComprasEditar As String = "COMPRAS_EDITAR"
        Public Const ComprasAprobar As String = "COMPRAS_APROBAR"
        Public Const ProduccionEditar As String = "PRODUCCION_EDITAR"
        Public Const InventarioContar As String = "INVENTARIO_CONTAR"
        Public Const InventarioAprobar As String = "INVENTARIO_APROBAR"
        Public Const ReportesVer As String = "REPORTES_VER"
        Public Const CierreEjecutar As String = "CIERRE_EJECUTAR"
        Public Const ContratosVer As String = "CONTRATOS_VER"
        Public Const ContratosEditar As String = "CONTRATOS_EDITAR"
        Public Const GastosEditar As String = "GASTOS_EDITAR"
        Public Const ResultadosVer As String = "RESULTADOS_VER"

        Public ReadOnly Property Todos As IReadOnlyList(Of String) = New String() {
            CatalogoVer, CatalogoEditar, CatalogoImportar, ProveedoresEditar, PreciosEditar,
            StockContabilizar, UsuariosAdministrar, AuditoriaVer,
            MenusVer, MenusConfigurar, RecetasEditar, RecetasAprobar, MinutasEditar, MinutasAprobar,
            ComprasVer, ComprasEditar, ComprasAprobar, ProduccionEditar,
            InventarioContar, InventarioAprobar, ReportesVer, CierreEjecutar,
            ContratosVer, ContratosEditar, GastosEditar, ResultadosVer}

        Public Function Descripcion(codigo As String) As String
            Select Case codigo
                Case CatalogoVer : Return "Consultar el catalogo de productos"
                Case CatalogoEditar : Return "Crear y modificar productos, variantes y empaques"
                Case CatalogoImportar : Return "Importar el catalogo desde un archivo"
                Case ProveedoresEditar : Return "Crear y modificar proveedores"
                Case PreciosEditar : Return "Registrar precios de compra"
                Case StockContabilizar : Return "Confirmar documentos de stock"
                Case UsuariosAdministrar : Return "Administrar usuarios y roles"
                Case AuditoriaVer : Return "Consultar la auditoria"
                Case MenusVer : Return "Consultar recetas, minutas, costos y necesidades"
                Case MenusConfigurar : Return "Configurar servicios, regimenes y estructuras"
                Case RecetasEditar : Return "Crear y modificar recetas en borrador"
                Case RecetasAprobar : Return "Aprobar y retirar versiones de receta"
                Case MinutasEditar : Return "Planificar minutas en borrador"
                Case MinutasAprobar : Return "Aprobar minutas (fija el costo previsto)"
                Case ComprasVer : Return "Consultar previsiones y pedidos de compra"
                Case ComprasEditar : Return "Calcular previsiones, fijar reservas y preparar pedidos"
                Case ComprasAprobar : Return "Aprobar y anular pedidos de compra"
                Case ProduccionEditar : Return "Calcular requerimientos y registrar produccion y mermas"
                Case InventarioContar : Return "Abrir inventarios fisicos y registrar conteos"
                Case InventarioAprobar : Return "Revisar inventarios y autorizar ajustes (independiente del conteo)"
                Case ReportesVer : Return "Consultar pendientes, reportes diarios y mensuales y Food Cost"
                Case CierreEjecutar : Return "Registrar ingresos y objetivos, cerrar dias y meses"
                Case ContratosVer : Return "Consultar clientes y contratos"
                Case ContratosEditar : Return "Registrar clientes, contratos, ajustes e ingresos desde contrato"
                Case GastosEditar : Return "Registrar gastos de personal y operacion"
                Case ResultadosVer : Return "Consultar el resultado mensual (ingresos, alimentos, gastos y margen)"
                Case Else : Return codigo
            End Select
        End Function
    End Module

    Public NotInheritable Class RolBase
        Public ReadOnly Property Codigo As String
        Public ReadOnly Property Nombre As String
        Public ReadOnly Property Permisos As IReadOnlyList(Of String)

        Public Sub New(codigo As String, nombre As String, permisos As IEnumerable(Of String))
            Me.Codigo = codigo
            Me.Nombre = nombre
            Me.Permisos = permisos.ToList()
        End Sub
    End Class

    ''' <summary>Roles que se crean con cada empresa. El administrador puede crear roles propios con los permisos que elija (etapa 9).</summary>
    Public Module RolesBase
        Public Const Administrador As String = "ADMIN"

        Public ReadOnly Property Todos As IReadOnlyList(Of RolBase) = New RolBase() {
            New RolBase(Administrador, "Administrador", Permisos.Todos),
            New RolBase("SUPERVISOR", "Supervisor", Permisos.Todos.Where(Function(p) p <> Permisos.UsuariosAdministrar)),
            New RolBase("ALMACEN", "Almacen", {Permisos.CatalogoVer, Permisos.StockContabilizar, Permisos.MenusVer, Permisos.ComprasVer, Permisos.ComprasEditar, Permisos.InventarioContar}),
            New RolBase("COCINA", "Cocina", {Permisos.CatalogoVer, Permisos.MenusVer, Permisos.RecetasEditar, Permisos.MinutasEditar, Permisos.ProduccionEditar}),
            New RolBase("FINANZAS", "Finanzas y contratos", {Permisos.ReportesVer, Permisos.ContratosVer, Permisos.ContratosEditar, Permisos.GastosEditar, Permisos.ResultadosVer})}

        ''' <summary>True si el código es de un rol que crea el sistema (no se modifica desde la aplicación).</summary>
        Public Function EsRolBase(codigo As String) As Boolean
            Return Todos.Any(Function(r) String.Equals(r.Codigo, codigo, StringComparison.OrdinalIgnoreCase))
        End Function
    End Module

    Public Module PoliticaClave
        Public Const LongitudMinima As Integer = 10

        ''' <summary>Lanza CLAVE_DEBIL si la clave no cumple la política mínima.</summary>
        Public Sub Validar(clave As String)
            If clave Is Nothing OrElse clave.Length < LongitudMinima Then
                Throw New ReglaNegocioException("CLAVE_DEBIL", $"La clave debe tener al menos {LongitudMinima} caracteres.")
            End If
            If Not clave.Any(AddressOf Char.IsLetter) OrElse Not clave.Any(AddressOf Char.IsDigit) Then
                Throw New ReglaNegocioException("CLAVE_DEBIL", "La clave debe combinar letras y numeros.")
            End If
        End Sub
    End Module

End Namespace
