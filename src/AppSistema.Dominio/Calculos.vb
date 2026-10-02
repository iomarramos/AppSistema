Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Catalogo

Namespace Calculos

    ''' <summary>Escalado de recetas: la cantidad del ingrediente corresponde al rendimiento completo de la versión.</summary>
    Public Module Recetas

        ''' <summary>
        ''' Necesidad = cantidad bruta × raciones planificadas / rendimiento de la receta (todo en u6).
        ''' Receta de 10 raciones con 1 L de aceite, para 150 raciones: 15 L.
        ''' </summary>
        Public Function NecesidadIngredienteU6(cantidadBrutaU6 As Long, rendimientoRacionesU6 As Long, racionesU6 As Long) As Long
            If rendimientoRacionesU6 <= 0 Then
                Throw New ReglaNegocioException("RENDIMIENTO_INVALIDO", "El rendimiento de la receta debe ser mayor que cero.")
            End If
            If racionesU6 < 0 OrElse cantidadBrutaU6 < 0 Then
                Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Raciones y cantidad bruta no pueden ser negativas.")
            End If
            Return EscalaU6.MultiplicarDividir(cantidadBrutaU6, racionesU6, rendimientoRacionesU6)
        End Function

    End Module

    Public Structure ResultadoCompra
        Public ReadOnly Property Empaques As Long
        Public ReadOnly Property TotalBaseU6 As Long
        ''' <summary>Excedente causado por el redondeo del empaque (total comprado − necesidad).</summary>
        Public ReadOnly Property ExcesoU6 As Long

        Public Sub New(empaques As Long, totalBaseU6 As Long, excesoU6 As Long)
            Me.Empaques = empaques
            Me.TotalBaseU6 = totalBaseU6
            Me.ExcesoU6 = excesoU6
        End Sub
    End Structure

    Public Module Compras

        ''' <summary>
        ''' Necesidad neta = máx(0, demanda del horizonte + reserva al final − stock utilizable − recepciones elegibles).
        ''' Ejemplo: 50 + 10 − 27 − 8 = 25 L.
        ''' </summary>
        Public Function NecesidadNetaU6(demandaU6 As Long, reservaU6 As Long, stockUtilizableU6 As Long, recepcionesElegiblesU6 As Long) As Long
            If demandaU6 < 0 OrElse reservaU6 < 0 OrElse stockUtilizableU6 < 0 OrElse recepcionesElegiblesU6 < 0 Then
                Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "Las cantidades de la prevision no pueden ser negativas.")
            End If
            Dim neta As Decimal = CDec(demandaU6) + reservaU6 - stockUtilizableU6 - recepcionesElegiblesU6
            If neta <= 0D Then Return 0
            Return CLng(neta)
        End Function

        ''' <summary>
        ''' Empaques = k × techo( máx( techo(N / C), m ) / k ), con N = necesidad, C = contenido del empaque,
        ''' m = mínimo y k = múltiplo. Para N = 0 no se compra nada (no se fuerza el mínimo).
        ''' </summary>
        Public Function EmpaquesAComprar(necesidadU6 As Long, empaque As EmpaqueCompra) As ResultadoCompra
            If empaque Is Nothing Then Throw New ArgumentNullException(NameOf(empaque))
            If necesidadU6 < 0 Then Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "La necesidad no puede ser negativa.")
            If necesidadU6 = 0 Then Return New ResultadoCompra(0, 0, 0)

            Dim contenido As Long = empaque.ContenidoBaseU6
            Dim paraCubrir As Long = TechoDiv(necesidadU6, contenido)
            Dim conMinimo As Long = Math.Max(paraCubrir, empaque.MinimoEmpaques)
            Dim empaques As Long = TechoDiv(conMinimo, empaque.MultiploEmpaques) * empaque.MultiploEmpaques
            Dim total As Long = empaque.CantidadBaseU6(empaques)
            Return New ResultadoCompra(empaques, total, total - necesidadU6)
        End Function

        Private Function TechoDiv(a As Long, b As Long) As Long
            Return a \ b + If(a Mod b <> 0L, 1L, 0L)
        End Function

    End Module

End Namespace
