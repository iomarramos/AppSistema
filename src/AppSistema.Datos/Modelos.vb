' Datos que devuelven los servicios a la interfaz. "Version" es el token de concurrencia optimista
' (xmin de PostgreSQL): una edición con versión antigua recibe VERSION_CONFLICTIVA.

Public NotInheritable Class UsuarioResumen
    Public Property Id As Long
    Public Property Login As String
    Public Property Nombre As String
    Public Property Activo As Boolean
    Public Property Roles As String
    ''' <summary>Dueño del sistema (administrador general).</summary>
    Public Property EsDueno As Boolean
End Class

''' <summary>Hasta qué nivel llega una persona en cada módulo, en una operación ("" = sin acceso).</summary>
Public NotInheritable Class AccesoModuloDto
    Public Property Login As String
    Public Property Nombre As String
    Public Property Operacion As String
    Public Property Roles As String
    Public Property Catalogo As String
    Public Property Planificacion As String
    Public Property Produccion As String
    Public Property Abastecimiento As String
    Public Property Almacen As String
    Public Property Inventario As String
    Public Property Cierres As String
    Public Property Resultados As String
    Public Property Administracion As String
End Class

Public NotInheritable Class RolDto
    Public Property Codigo As String
    Public Property Nombre As String
    Public Property Permisos As List(Of String)
    ''' <summary>Rol creado por el sistema (no se edita desde la aplicación).</summary>
    Public Property EsBase As Boolean
    Public ReadOnly Property PermisosTexto As String
        Get
            Return String.Join(", ", Permisos)
        End Get
    End Property
End Class

Public NotInheritable Class OperacionDto
    Public Property Id As Long
    Public Property Codigo As String
    Public Property Nombre As String
    Public Property Ubicacion As String
    Public Property Almacenes As Long
    Public Property Usuarios As Long
End Class

Public NotInheritable Class AuditoriaDto
    Public Property Fecha As DateTime
    Public Property Usuario As String
    Public Property Tabla As String
    Public Property RegistroId As Long
    Public Property Accion As String
    Public Property Antes As String
    Public Property Despues As String
End Class

Public NotInheritable Class AlmacenResumen
    Public Property Id As Long
    Public Property OperacionId As Long
    Public Property Codigo As String
    Public Property Nombre As String
End Class

Public NotInheritable Class UnidadMedidaDto
    Public Property Id As Long
    Public Property Codigo As String
    Public Property Nombre As String
    Public Property Dimension As String
    Public Property FactorABaseU6 As Long
End Class

Public NotInheritable Class CategoriaDto
    Public Property Id As Long
    Public Property Codigo As String
    Public Property Nombre As String
End Class

Public NotInheritable Class MarcaDto
    Public Property Id As Long
    Public Property Nombre As String
End Class

Public NotInheritable Class ProductoBaseDto
    Public Property Id As Long
    Public Property Codigo As String
    Public Property Descripcion As String
    Public Property Especificacion As String
    Public Property UnidadBaseId As Long
    Public Property UnidadCodigo As String
    Public Property CategoriaId As Long?
    Public Property CategoriaCodigo As String
    Public Property Activo As Boolean
    ''' <summary>Insumo que no se compra (agua de red): se costea en S/ 0.</summary>
    Public Property SinCostoCompra As Boolean
    Public Property Version As String
End Class

Public NotInheritable Class VarianteDto
    Public Property Id As Long
    Public Property ProductoBaseId As Long
    Public Property MarcaId As Long?
    Public Property MarcaNombre As String
    Public Property Codigo As String
    Public Property DescripcionComercial As String
    Public Property TipoEnvase As String
    Public Property ContenidoBasePorEnvaseU6 As Long
    Public Property Activo As Boolean
    Public Property Version As String
    ''' <summary>Es el producto activo (liberado) de su ingrediente en la operación de la sesión: su precio es el que se costea (D02).</summary>
    Public Property ActivoEnOperacion As Boolean
    ''' <summary>Familia › subfamilia › grupo del SGP (ruta de la categoría de la presentación).</summary>
    Public Property Familia As String
End Class

Public NotInheritable Class EmpaqueDto
    Public Property Id As Long
    Public Property VarianteId As Long
    Public Property VarianteCodigo As String
    Public Property VarianteDescripcion As String
    Public Property Codigo As String
    Public Property Descripcion As String
    Public Property EnvasesPorEmpaque As Long
    Public Property MinimoEmpaques As Long
    Public Property MultiploEmpaques As Long
    Public Property ContenidoBaseU6 As Long
    Public Property Activo As Boolean

    Public Overrides Function ToString() As String
        Return $"{VarianteCodigo} / {Codigo} - {Descripcion}"
    End Function
End Class

Public NotInheritable Class ProveedorDto
    Public Property Id As Long
    Public Property Codigo As String
    Public Property Nombre As String
    Public Property IdentificacionFiscal As String
    Public Property Contacto As String
    Public Property Correo As String
    Public Property Telefono As String
    Public Property EsCajaChica As Boolean
End Class

Public NotInheritable Class ProveedorEmpaqueDto
    Public Property Id As Long
    Public Property ProveedorId As Long
    Public Property EmpaqueId As Long
    Public Property EmpaqueDescripcion As String
    Public Property VarianteCodigo As String
    Public Property PlazoEntregaDias As Long
End Class

Public NotInheritable Class PrecioDto
    Public Property Id As Long
    Public Property ProveedorEmpaqueId As Long
    Public Property FechaDesde As Date
    Public Property FechaHasta As Date?
    Public Property Moneda As String
    Public Property PrecioEmpaqueU6 As Long
    Public Property IncluyeImpuesto As Boolean
End Class
