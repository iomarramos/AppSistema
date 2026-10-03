' DTOs del módulo 1 (menús y recetas). Cantidades y montos en u6.

Public NotInheritable Class ServicioDto
    Public Property Id As Long
    Public Property Codigo As String
    Public Property Nombre As String
End Class

Public NotInheritable Class EstructuraDto
    Public Property Id As Long
    Public Property ServicioId As Long
    Public Property Codigo As String
    Public Property Nombre As String
    Public Property Orden As Long
    ''' <summary>Parte de los comensales que consume el componente (10000 = 100 %).</summary>
    Public Property FactorConsumoBp As Long = 10000
    Public ReadOnly Property FactorConsumoTexto As String
        Get
            Return (FactorConsumoBp / 100D).ToString("0.##") & " %"
        End Get
    End Property
End Class

''' <summary>Servicio + régimen que presta la operación de la sesión (a lo que se asigna una minuta).</summary>
Public NotInheritable Class OperacionServicioDto
    Public Property Id As Long
    Public Property ServicioId As Long
    Public Property ServicioNombre As String
    Public Property RegimenId As Long
    Public Property RegimenNombre As String
    Public Property CostoObjetivoRacionU6 As Long?
    ''' <summary>Food Cost objetivo (48 % = 4800); Nothing = 48 % por defecto.</summary>
    Public Property FoodCostObjetivoBp As Long?
    Public ReadOnly Property FoodCostObjetivoTexto As String
        Get
            Return If(FoodCostObjetivoBp.HasValue, (FoodCostObjetivoBp.Value / 100D).ToString("0.##") & " %", "48 % (por defecto)")
        End Get
    End Property
End Class

Public NotInheritable Class RecetaDto
    Public Property Id As Long
    Public Property Codigo As String
    Public Property Nombre As String
    Public Property Categoria As String
    Public Property Activo As Boolean
    ''' <summary>Versión aprobada vigente (Nothing si aún no hay).</summary>
    Public Property VersionAprobadaId As Long?
    Public Property VersionAprobada As Long?
End Class

Public NotInheritable Class RecetaVersionDto
    Public Property Id As Long
    Public Property RecetaId As Long
    Public Property Version As Long
    Public Property RendimientoRacionesU6 As Long
    Public Property Instrucciones As String
    Public Property Estado As String
End Class

Public NotInheritable Class IngredienteDto
    Public Property Id As Long
    Public Property ProductoBaseId As Long
    Public Property ProductoCodigo As String
    Public Property ProductoDescripcion As String
    Public Property Unidad As String
    Public Property CantidadBrutaU6 As Long
    Public Property CantidadNetaU6 As Long?
    Public Property Orden As Long
    Public Property Tecnica As String
    ''' <summary>Códigos de variantes permitidas separados por coma; vacío = cualquiera del producto.</summary>
    Public Property VariantesPermitidas As String
End Class

''' <summary>Costo de un ingrediente con su fuente. CostoUnitarioBaseU6 = Nothing → costo pendiente.</summary>
Public NotInheritable Class CostoIngredienteDto
    Public Property IngredienteId As Long
    Public Property ProductoDescripcion As String
    Public Property Unidad As String
    Public Property CantidadBrutaU6 As Long
    Public Property CostoUnitarioBaseU6 As Long?
    Public Property CostoLineaU6 As Long?
    Public Property Fuente As String
    Public Property FechaPrecio As Date?
End Class

Public NotInheritable Class CostoRecetaDto
    Public Property RecetaVersionId As Long
    Public Property RendimientoRacionesU6 As Long
    Public ReadOnly Property Ingredientes As New List(Of CostoIngredienteDto)
    ''' <summary>Nothing si algún ingrediente no tiene costo (T11).</summary>
    Public Property CostoRacionU6 As Long?
    Public ReadOnly Property Pendientes As Integer
        Get
            Return Ingredientes.Where(Function(i) Not i.CostoUnitarioBaseU6.HasValue).Count()
        End Get
    End Property
End Class

Public NotInheritable Class MinutaDto
    Public Property Id As Long
    Public Property OperacionServicioId As Long
    Public Property ServicioNombre As String
    Public Property RegimenNombre As String
    Public Property Fecha As Date
    Public Property Comensales As Long
    Public Property Estado As String
    Public Property MonedaCosteo As String
    ''' <summary>Costo previsto de la estructura (al aprobar).</summary>
    Public Property CostoPrevistoU6 As Long?
    ''' <summary>Venta = costo previsto / Food Cost objetivo (al aprobar).</summary>
    Public Property VentaPrevistaU6 As Long?
    Public Property FoodCostObjetivoBp As Long?
    Public ReadOnly Property PrecioVentaComensalU6 As Long?
        Get
            If Not VentaPrevistaU6.HasValue OrElse Comensales <= 0 Then Return Nothing
            Return AppSistema.Dominio.Numerico.EscalaU6.MultiplicarDividir(VentaPrevistaU6.Value, 1, Comensales)
        End Get
    End Property
    Public ReadOnly Property CostoComensalU6 As Long?
        Get
            If Not CostoPrevistoU6.HasValue OrElse Comensales <= 0 Then Return Nothing
            Return AppSistema.Dominio.Numerico.EscalaU6.MultiplicarDividir(CostoPrevistoU6.Value, 1, Comensales)
        End Get
    End Property
End Class

Public NotInheritable Class PlatoDto
    Public Property Id As Long
    Public Property EstructuraId As Long
    Public Property EstructuraNombre As String
    Public Property EstructuraOrden As Long
    Public Property RecetaVersionId As Long
    Public Property RecetaCodigo As String
    Public Property RecetaNombre As String
    Public Property Version As Long
    Public Property Raciones As Long
    ''' <summary>Snapshot al aprobar; Nothing = sin aprobar o costo pendiente.</summary>
    Public Property CostoPrevistoRacionU6 As Long?
    Public Property FechaCosteo As Date?
    ''' <summary>Ingredientes sin costo en el snapshot (solo minutas aprobadas).</summary>
    Public Property IngredientesSinCosto As Long
End Class

Public NotInheritable Class FijoMinutaDto
    Public Property Id As Long
    Public Property ProductoBaseId As Long
    Public Property ProductoDescripcion As String
    Public Property Unidad As String
    Public Property CantidadBaseU6 As Long
    Public Property CostoPrevistoUnitarioU6 As Long?
End Class

''' <summary>Necesidad consolidada de un producto base (suma de todas las recetas y fijos).</summary>
Public NotInheritable Class NecesidadDto
    Public Property ProductoBaseId As Long
    Public Property ProductoCodigo As String
    Public Property ProductoDescripcion As String
    Public Property Unidad As String
    Public Property CantidadU6 As Long
    ''' <summary>Cuántos platos o fijos aportan a esta necesidad.</summary>
    Public Property Origenes As Integer
End Class
