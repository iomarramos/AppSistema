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
        Public Const ComprasConsolidar As String = "COMPRAS_CONSOLIDAR"
        Public Const FactoresEditar As String = "FACTORES_EDITAR"
        Public Const AdicionalAprobar As String = "ADICIONAL_APROBAR"
        Public Const InventarioVer As String = "INVENTARIO_VER"

        Public ReadOnly Property Todos As IReadOnlyList(Of String) = New String() {
            CatalogoVer, CatalogoEditar, CatalogoImportar, ProveedoresEditar, PreciosEditar,
            StockContabilizar, UsuariosAdministrar, AuditoriaVer,
            MenusVer, MenusConfigurar, RecetasEditar, RecetasAprobar, MinutasEditar, MinutasAprobar,
            ComprasVer, ComprasEditar, ComprasAprobar, ProduccionEditar,
            InventarioContar, InventarioAprobar, ReportesVer, CierreEjecutar,
            ContratosVer, ContratosEditar, GastosEditar, ResultadosVer, ComprasConsolidar, FactoresEditar, AdicionalAprobar, InventarioVer}

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
                Case ComprasConsolidar : Return "Consolidar las compras de todas las operaciones por periodo"
                Case FactoresEditar : Return "Cambiar el factor de consumo de la operacion"
                Case AdicionalAprobar : Return "Aprobar los requerimientos adicionales antes de entregarlos"
                Case InventarioVer : Return "Consultar los inventarios fisicos y sus diferencias"
                Case Else : Return codigo
            End Select
        End Function
    End Module

    ''' <summary>
    ''' Módulos del sistema y nivel de cada permiso, para asignar accesos por módulo (qué ve y hasta dónde llega cada
    ''' persona). Las áreas de Planificación (menús) y Abastecimiento (compras) son módulos distintos.
    ''' </summary>
    Public Module Modulos
        Public Const Catalogo As String = "Catalogo"
        Public Const Planificacion As String = "Planificacion (menus y recetas)"
        Public Const Produccion As String = "Produccion"
        Public Const Abastecimiento As String = "Abastecimiento (compras)"
        Public Const Almacen As String = "Almacen"
        Public Const Inventario As String = "Inventario fisico"
        Public Const Cierres As String = "Cierres y Food Cost"
        Public Const Resultados As String = "Contratos y resultados"
        Public Const Administracion As String = "Administracion"

        Public ReadOnly Property Todos As IReadOnlyList(Of String) = New String() {
            Catalogo, Planificacion, Produccion, Abastecimiento, Almacen, Inventario, Cierres, Resultados, Administracion}

        ''' <summary>Niveles de acceso, de menor a mayor.</summary>
        Public ReadOnly Property Niveles As IReadOnlyList(Of String) = New String() {"Ver", "Editar", "Aprobar", "Administrar"}

        Public Function ModuloDe(permiso As String) As String
            Select Case permiso
                Case Permisos.CatalogoVer, Permisos.CatalogoEditar, Permisos.CatalogoImportar : Return Catalogo
                Case Permisos.MenusVer, Permisos.MenusConfigurar, Permisos.RecetasEditar, Permisos.RecetasAprobar,
                     Permisos.MinutasEditar, Permisos.MinutasAprobar : Return Planificacion
                Case Permisos.ProduccionEditar, Permisos.FactoresEditar, Permisos.AdicionalAprobar : Return Produccion
                Case Permisos.ProveedoresEditar, Permisos.PreciosEditar, Permisos.ComprasVer, Permisos.ComprasEditar,
                     Permisos.ComprasAprobar, Permisos.ComprasConsolidar : Return Abastecimiento
                Case Permisos.StockContabilizar : Return Almacen
                Case Permisos.InventarioContar, Permisos.InventarioAprobar, Permisos.InventarioVer : Return Inventario
                Case Permisos.ReportesVer, Permisos.CierreEjecutar : Return Cierres
                Case Permisos.ContratosVer, Permisos.ContratosEditar, Permisos.GastosEditar, Permisos.ResultadosVer : Return Resultados
                Case Else : Return Administracion
            End Select
        End Function

        Public Function NivelDe(permiso As String) As String
            Select Case permiso
                Case Permisos.CatalogoVer, Permisos.MenusVer, Permisos.ComprasVer, Permisos.ReportesVer, Permisos.ContratosVer,
                     Permisos.ResultadosVer, Permisos.AuditoriaVer, Permisos.InventarioVer : Return "Ver"
                Case Permisos.RecetasAprobar, Permisos.MinutasAprobar, Permisos.ComprasAprobar, Permisos.InventarioAprobar,
                     Permisos.CierreEjecutar, Permisos.ComprasConsolidar, Permisos.AdicionalAprobar : Return "Aprobar"
                Case Permisos.UsuariosAdministrar, Permisos.MenusConfigurar, Permisos.CatalogoImportar : Return "Administrar"
                Case Else : Return "Editar"
            End Select
        End Function

        ''' <summary>Nivel más alto que dan los permisos en el módulo ("" si ninguno).</summary>
        Public Function NivelEnModulo(permisos As IEnumerable(Of String), modulo As String) As String
            Dim niveles = permisos.Where(Function(p) ModuloDe(p) = modulo).Select(Function(p) Array.IndexOf(Modulos.Niveles.ToArray(), NivelDe(p))).ToList()
            Return If(niveles.Count = 0, "", Modulos.Niveles(niveles.Max()))
        End Function
    End Module

    ''' <summary>
    ''' Pantalla y acción de cada permiso (niveles 2 y 3 de la matriz de acceso: módulo → pantalla → acción). El cuarto nivel,
    ''' el alcance, va en cada asignación de rol (Alcances).
    ''' </summary>
    Public Module Pantallas
        Public ReadOnly Property Acciones As IReadOnlyList(Of String) = New String() {
            "VER", "CREAR", "EDITAR", "APROBAR", "ANULAR", "LIBERAR", "EXPORTAR", "IMPRIMIR", "CONFIGURAR"}

        Public Function PantallaDe(permiso As String) As String
            Select Case permiso
                Case Permisos.CatalogoVer, Permisos.CatalogoEditar : Return "Catalogo de productos"
                Case Permisos.CatalogoImportar : Return "Importacion de catalogo"
                Case Permisos.ProveedoresEditar : Return "Proveedores"
                Case Permisos.PreciosEditar : Return "Precios de compra"
                Case Permisos.StockContabilizar : Return "Movimientos de stock"
                Case Permisos.UsuariosAdministrar : Return "Usuarios y roles"
                Case Permisos.AuditoriaVer : Return "Auditoria"
                Case Permisos.MenusVer : Return "Menus, costos y necesidades"
                Case Permisos.MenusConfigurar : Return "Servicios, estructuras y factores teoricos"
                Case Permisos.RecetasEditar, Permisos.RecetasAprobar : Return "Recetas"
                Case Permisos.MinutasEditar, Permisos.MinutasAprobar : Return "Minutas (plan operativo)"
                Case Permisos.ComprasVer, Permisos.ComprasEditar, Permisos.ComprasAprobar : Return "Prevision y pedidos de compra"
                Case Permisos.ComprasConsolidar : Return "Consolidado de compras"
                Case Permisos.ProduccionEditar : Return "Produccion y requerimientos"
                Case Permisos.AdicionalAprobar : Return "Requerimientos adicionales"
                Case Permisos.FactoresEditar : Return "Factores de la operacion"
                Case Permisos.InventarioContar, Permisos.InventarioAprobar, Permisos.InventarioVer : Return "Inventario fisico"
                Case Permisos.ReportesVer : Return "Reportes y Food Cost"
                Case Permisos.CierreEjecutar : Return "Cierres de dia y mes"
                Case Permisos.ContratosVer, Permisos.ContratosEditar : Return "Clientes y contratos"
                Case Permisos.GastosEditar : Return "Gastos"
                Case Permisos.ResultadosVer : Return "Resultado mensual"
                Case Else : Return permiso
            End Select
        End Function

        Public Function AccionDe(permiso As String) As String
            Select Case permiso
                Case Permisos.CatalogoImportar, Permisos.InventarioContar : Return "CREAR"
                Case Permisos.RecetasAprobar, Permisos.MinutasAprobar, Permisos.ComprasAprobar, Permisos.InventarioAprobar,
                     Permisos.AdicionalAprobar, Permisos.CierreEjecutar, Permisos.StockContabilizar : Return "APROBAR"
                Case Permisos.UsuariosAdministrar, Permisos.MenusConfigurar : Return "CONFIGURAR"
                Case Permisos.ComprasConsolidar : Return "EXPORTAR"
                Case Else
                    Return If(Modulos.NivelDe(permiso) = "Ver", "VER", "EDITAR")
            End Select
        End Function
    End Module

    ''' <summary>Alcance de una asignación de rol: su operación, las de su zona o región, o todas las de la empresa.</summary>
    Public Module Alcances
        Public Const Operacion As String = "OPERACION"
        Public Const Zona As String = "ZONA"
        Public Const Todas As String = "TODAS"
        Public ReadOnly Property Todos As IReadOnlyList(Of String) = New String() {Operacion, Zona, Todas}
    End Module

    ''' <summary>
    ''' Zonas o regiones de las operaciones (decisión del usuario, 2026-10-03): Costa, Sierra y Selva. Agrupan sedes para el
    ''' acceso por zona y, más adelante, para la distribución desde el almacén central, donde la zona pesa en los tiempos
    ''' de llegada.
    ''' </summary>
    Public Module Zonas
        Public Const Costa As String = "COSTA"
        Public Const Sierra As String = "SIERRA"
        Public Const Selva As String = "SELVA"
        Public ReadOnly Property Todas As IReadOnlyList(Of String) = New String() {Costa, Sierra, Selva}

        ''' <summary>Normaliza la zona (mayúsculas, sin espacios); vacío = sin zona. Lanza DATO_INVALIDO si no es Costa, Sierra o Selva.</summary>
        Public Function Normalizar(zona As String) As String
            If String.IsNullOrWhiteSpace(zona) Then Return Nothing
            Dim z = zona.Trim().ToUpperInvariant()
            If Not Todas.Contains(z) Then Throw New ReglaNegocioException("DATO_INVALIDO", $"Zona desconocida: {zona.Trim()}. Use COSTA, SIERRA o SELVA.")
            Return z
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
        Public Const Planificacion As String = "PLANIFICACION"
        Public Const Abastecimiento As String = "ABASTECIMIENTO"
        Public Const PlanificadorCentral As String = "PLANIFICADOR_CENTRAL"
        Public Const ComprasCentral As String = "COMPRAS_CENTRAL"
        Public Const Operaciones As String = "OPERACIONES"
        Public Const Chef As String = "CHEF"
        Public Const Almacenero As String = "ALMACEN"
        Public Const JefeAlmacen As String = "JEFE_ALMACEN"

        ''' <summary>
        ''' Perfiles de la operación (pedido del usuario, 2026-10-05). Los códigos se conservan porque las asignaciones y las
        ''' pruebas los usan; lo que cambia es el nombre que se muestra. ALMACEN es el almacenero que ejecuta; JEFE_ALMACEN
        ''' hace lo mismo y además aprueba inventarios y ajustes; OPERACIONES es el jefe de operación que aprueba y cierra.
        ''' </summary>
        Public ReadOnly Property Todos As IReadOnlyList(Of RolBase) = New RolBase() {
            New RolBase(Administrador, "Administrador", Permisos.Todos),
            New RolBase("SUPERVISOR", "Supervisor", Permisos.Todos.Where(Function(p) p <> Permisos.UsuariosAdministrar)),
            New RolBase(Almacenero, "Almacenero (recepcion, despacho, conteo; sin pedidos ni aprobaciones)",
                        {Permisos.CatalogoVer, Permisos.StockContabilizar, Permisos.MenusVer, Permisos.ComprasVer, Permisos.InventarioContar, Permisos.InventarioVer}),
            New RolBase(JefeAlmacen, "Jefe de almacen (aprueba inventarios y ajustes; reportes)",
                        {Permisos.CatalogoVer, Permisos.StockContabilizar, Permisos.MenusVer, Permisos.ComprasVer,
                         Permisos.InventarioContar, Permisos.InventarioVer, Permisos.InventarioAprobar, Permisos.ReportesVer}),
            New RolBase("COCINA", "Cocina", {Permisos.CatalogoVer, Permisos.MenusVer, Permisos.RecetasEditar, Permisos.MinutasEditar, Permisos.ProduccionEditar}),
            New RolBase("FINANZAS", "Finanzas y contratos", {Permisos.ReportesVer, Permisos.ContratosVer, Permisos.ContratosEditar, Permisos.GastosEditar, Permisos.ResultadosVer}),
            New RolBase(Planificacion, "Planificacion (menu, factores, costo y pax)",
                        {Permisos.CatalogoVer, Permisos.MenusVer, Permisos.MenusConfigurar, Permisos.RecetasEditar, Permisos.RecetasAprobar,
                         Permisos.MinutasEditar, Permisos.MinutasAprobar, Permisos.FactoresEditar, Permisos.ReportesVer}),
            New RolBase(Abastecimiento, "Abastecimiento (compras y consolidado)",
                        {Permisos.CatalogoVer, Permisos.CatalogoEditar, Permisos.ProveedoresEditar, Permisos.PreciosEditar, Permisos.MenusVer,
                         Permisos.ComprasVer, Permisos.ComprasEditar, Permisos.ComprasAprobar, Permisos.ComprasConsolidar}),
            New RolBase(PlanificadorCentral, "Planificador central (recetas, menus, factores teoricos, costos; sin stock)",
                        {Permisos.CatalogoVer, Permisos.MenusVer, Permisos.MenusConfigurar, Permisos.RecetasEditar, Permisos.RecetasAprobar,
                         Permisos.MinutasEditar, Permisos.MinutasAprobar, Permisos.ComprasVer, Permisos.ReportesVer}),
            New RolBase(ComprasCentral, "Compras central (demanda consolidada, proveedores, precios y pedidos)",
                        {Permisos.CatalogoVer, Permisos.CatalogoEditar, Permisos.ProveedoresEditar, Permisos.PreciosEditar, Permisos.MenusVer,
                         Permisos.ComprasVer, Permisos.ComprasEditar, Permisos.ComprasAprobar, Permisos.ComprasConsolidar, Permisos.ReportesVer}),
            New RolBase(Operaciones, "Jefe de operacion (aprueba planificacion, adicionales, cierres, Food Cost y resultados; sin stock)",
                        {Permisos.CatalogoVer, Permisos.MenusVer, Permisos.MinutasEditar, Permisos.MinutasAprobar, Permisos.FactoresEditar,
                         Permisos.ProduccionEditar, Permisos.AdicionalAprobar, Permisos.ComprasVer, Permisos.InventarioVer, Permisos.ReportesVer,
                         Permisos.ResultadosVer, Permisos.CierreEjecutar}),
            New RolBase(Chef, "Chef operativo (programacion del dia, factores del dia, produccion; sin precios ni stock)",
                        {Permisos.CatalogoVer, Permisos.MenusVer, Permisos.MinutasEditar, Permisos.FactoresEditar, Permisos.ProduccionEditar})}

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
