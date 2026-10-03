Imports AppSistema.Dominio.Numerico

Namespace Calculos

    ''' <summary>Presentación disponible en el almacén para cubrir una necesidad.</summary>
    Public Structure PresentacionDisponible
        Public ReadOnly Property VarianteId As Long
        Public ReadOnly Property ContenidoEnvaseU6 As Long
        Public ReadOnly Property StockU6 As Long
        ''' <summary>Costo vigente por unidad base (desempate: primero la más barata).</summary>
        Public ReadOnly Property CostoUnitarioU6 As Long

        Public Sub New(varianteId As Long, contenidoEnvaseU6 As Long, stockU6 As Long, costoUnitarioU6 As Long)
            If contenidoEnvaseU6 <= 0 Then Throw New ReglaNegocioException("CONVERSION_INVALIDA", "El contenido del envase debe ser positivo.")
            Me.VarianteId = varianteId
            Me.ContenidoEnvaseU6 = contenidoEnvaseU6
            Me.StockU6 = Math.Max(0L, stockU6)
            Me.CostoUnitarioU6 = costoUnitarioU6
        End Sub

        ''' <summary>Envases completos en stock.</summary>
        Public ReadOnly Property EnvasesEnStock As Long
            Get
                Return StockU6 \ ContenidoEnvaseU6
            End Get
        End Property
    End Structure

    Public NotInheritable Class EntregaPlanificada
        Public ReadOnly Property Lineas As New List(Of (VarianteId As Long, Envases As Long, CantidadU6 As Long))
        Public Property NecesidadU6 As Long
        Public ReadOnly Property EntregadoU6 As Long
            Get
                Return Lineas.Sum(Function(l) l.CantidadU6)
            End Get
        End Property
        ''' <summary>Lo que se entrega de más por descargar la presentación completa (D12: se da por consumido).</summary>
        Public ReadOnly Property ExcedentePresentacionU6 As Long
            Get
                Return Math.Max(0L, EntregadoU6 - NecesidadU6)
            End Get
        End Property
        ''' <summary>Lo que no se pudo cubrir con el stock en presentaciones completas.</summary>
        Public ReadOnly Property FaltanteU6 As Long
            Get
                Return Math.Max(0L, NecesidadU6 - EntregadoU6)
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Entrega a cocina (D12, usuario 03/10/2026: "se descarga toda la presentación"): el almacén entrega
    ''' presentaciones completas y lo entregado se da por consumido; no hay stock crudo en cocina.
    ''' </summary>
    Public Module Produccion

        ''' <summary>
        ''' Cubre la necesidad con envases completos. Primero busca UNA presentación que alcance con el menor
        ''' excedente (empate: menor costo); si ninguna alcanza sola, combina de la más grande a la más chica y,
        ''' al final, usa la más chica que cubra el resto. Lo que no alcance queda como faltante.
        ''' Ejemplo: 3,2 kg con bolsas de 5 kg y de 1 kg en stock → 4 bolsas de 1 kg (4 kg, excedente 0,8).
        ''' </summary>
        Public Function PlanificarEntrega(necesidadU6 As Long, disponibles As IEnumerable(Of PresentacionDisponible)) As EntregaPlanificada
            If necesidadU6 < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "La necesidad no puede ser negativa.")
            Dim r As New EntregaPlanificada With {.NecesidadU6 = necesidadU6}
            If necesidadU6 = 0 Then Return r
            Dim lista = disponibles.Where(Function(d) d.EnvasesEnStock > 0).ToList()

            Dim unica = lista.Select(Function(d) (D:=d, Envases:=TechoDiv(necesidadU6, d.ContenidoEnvaseU6))) _
                             .Where(Function(x) x.Envases <= x.D.EnvasesEnStock) _
                             .OrderBy(Function(x) x.Envases * x.D.ContenidoEnvaseU6 - necesidadU6).ThenBy(Function(x) x.D.CostoUnitarioU6) _
                             .ThenBy(Function(x) x.D.VarianteId).FirstOrDefault()
            If unica.Envases > 0 Then
                r.Lineas.Add((unica.D.VarianteId, unica.Envases, unica.Envases * unica.D.ContenidoEnvaseU6))
                Return r
            End If

            Dim resto = necesidadU6
            Dim usados As New Dictionary(Of Long, Long)
            For Each d In lista.OrderByDescending(Function(x) x.ContenidoEnvaseU6).ThenBy(Function(x) x.CostoUnitarioU6)
                If resto <= 0 Then Exit For
                Dim n = Math.Min(d.EnvasesEnStock, resto \ d.ContenidoEnvaseU6)
                If n > 0 Then usados(d.VarianteId) = n : resto -= n * d.ContenidoEnvaseU6
            Next
            If resto > 0 Then
                ' Un envase más de la presentación más chica que aún tenga stock y cubra el resto (si no, la más grande que quede).
                Dim quedan = lista.Where(Function(d) d.EnvasesEnStock > If(usados.ContainsKey(d.VarianteId), usados(d.VarianteId), 0L)).ToList()
                Dim extra = quedan.Where(Function(d) d.ContenidoEnvaseU6 >= resto).OrderBy(Function(d) d.ContenidoEnvaseU6).Concat(
                            quedan.OrderByDescending(Function(d) d.ContenidoEnvaseU6)).FirstOrDefault()
                If extra.ContenidoEnvaseU6 > 0 Then
                    usados(extra.VarianteId) = If(usados.ContainsKey(extra.VarianteId), usados(extra.VarianteId), 0L) + 1
                End If
            End If
            For Each d In lista.Where(Function(x) usados.ContainsKey(x.VarianteId))
                r.Lineas.Add((d.VarianteId, usados(d.VarianteId), usados(d.VarianteId) * d.ContenidoEnvaseU6))
            Next
            ' Si el último envase no cubrió todo, se repite el ciclo con lo que queda.
            If r.FaltanteU6 > 0 AndAlso lista.Any(Function(d) d.EnvasesEnStock > If(usados.ContainsKey(d.VarianteId), usados(d.VarianteId), 0L)) Then
                Dim resto2 = PlanificarEntrega(r.FaltanteU6, lista.Select(Function(d) New PresentacionDisponible(d.VarianteId, d.ContenidoEnvaseU6,
                    d.StockU6 - If(usados.ContainsKey(d.VarianteId), usados(d.VarianteId), 0L) * d.ContenidoEnvaseU6, d.CostoUnitarioU6)))
                For Each l In resto2.Lineas
                    Dim i = r.Lineas.FindIndex(Function(x) x.VarianteId = l.VarianteId)
                    If i >= 0 Then
                        r.Lineas(i) = (l.VarianteId, r.Lineas(i).Envases + l.Envases, r.Lineas(i).CantidadU6 + l.CantidadU6)
                    Else
                        r.Lineas.Add(l)
                    End If
                Next
            End If
            Return r
        End Function

        Private Function TechoDiv(a As Long, b As Long) As Long
            Return a \ b + If(a Mod b <> 0L, 1L, 0L)
        End Function

    End Module

End Namespace
