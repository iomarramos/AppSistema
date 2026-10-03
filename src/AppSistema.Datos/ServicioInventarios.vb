Imports AppSistema.Dominio
Imports AppSistema.Dominio.Importacion
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Dominio.Seguridad
Imports AppSistema.Dominio.Stock

Public NotInheritable Class InventarioDto
    Public Property Id As Long
    Public Property Numero As String
    Public Property Tipo As String
    Public Property FechaCorte As Date
    Public Property Estado As String
End Class

Public NotInheritable Class LineaConteoDto
    Public Property Id As Long
    Public Property VarianteId As Long
    Public Property VarianteCodigo As String
    Public Property Descripcion As String
    Public Property Presentacion As String
    Public Property Unidad As String
    Public Property ContenidoEnvaseU6 As Long
    ''' <summary>Stock al corte (Nothing en conteo ciego).</summary>
    Public Property SistemaU6 As Long?
    ''' <summary>Nothing = sin contar (no es cero, T34).</summary>
    Public Property FisicoU6 As Long?
    Public Property DiferenciaU6 As Long?
    Public Property ValorDiferenciaU6 As Long?
    Public Property Resultado As String
End Class

Public NotInheritable Class ResumenInventario
    Public Property Lineas As Integer
    Public Property SinContar As Integer
    Public Property ConDiferencia As Integer
    Public Property FaltanteValorU6 As Long
    Public Property SobranteValorU6 As Long
End Class

''' <summary>
''' Inventario físico (módulo 5, D06/D07 según la guía): corte con fotografía de stock y costo; conteo por envases y
''' parciales (celda vacía = pendiente); importación; reconteo; diferencia físico − sistema sin tocar el saldo (T33);
''' revisión y autorización independientes; ajuste en documentos separados y una sola vez (T35). Mientras se cuenta,
''' las variantes del inventario no admiten movimientos (T36).
''' </summary>
Public NotInheritable Class ServicioInventarios
    Inherits ServicioConSesion

    Public Sub New(cadenaConexion As String, sesion As SesionUsuario)
        MyBase.New(cadenaConexion, sesion)
    End Sub

    ''' <summary>Abre la toma. General: todas las variantes con saldo en el almacén. Rotativo: las indicadas.</summary>
    Public Function Abrir(almacenId As Long, fechaCorte As Date, tipo As String, Optional variantes As IEnumerable(Of Long) = Nothing) As Long
        If tipo <> "general" AndAlso tipo <> "rotativo" Then Throw New ReglaNegocioException("DATO_INVALIDO", "El tipo es general o rotativo.")
        Dim lista = If(variantes, Enumerable.Empty(Of Long)()).Distinct().ToArray()
        If tipo = "rotativo" AndAlso lista.Length = 0 Then Throw New ReglaNegocioException("DATO_OBLIGATORIO", "Un inventario rotativo necesita productos.")
        Return EnTransaccion(Permisos.InventarioContar,
            Function(u)
                If u.Escalar("SELECT 1 FROM almacen WHERE id = @a AND operacion_id = @o FOR UPDATE", "a", almacenId, "o", Sesion.Operacion.Id) Is Nothing Then
                    Throw New ReglaNegocioException("OPERACION_AJENA", "El almacen no pertenece a la operacion seleccionada.")
                End If
                u.Ejecutar("SELECT pg_advisory_xact_lock(hashtext('inventario'), @e::int)", "e", Sesion.EmpresaId)
                Dim base = $"INV-{Date.Today.Year}-"
                Dim n = u.EscalarLong("SELECT COALESCE(max(substring(numero from 10 for 5)::bigint), 0) + 1 FROM inventario WHERE numero LIKE @b", "b", base & "%")
                Dim id = u.EscalarLong("INSERT INTO inventario(empresa_id, almacen_id, numero, fecha_corte, tipo, usuario_id) VALUES (@e, @a, @n, @f, @t, @u) RETURNING id",
                                       "e", Sesion.EmpresaId, "a", almacenId, "n", $"{base}{n:00000}", "f", fechaCorte.Date, "t", tipo, "u", Sesion.UsuarioId)
                ' Fotografía: saldo y costo vigente al momento del corte (el almacén está bloqueado: nadie mueve en medio).
                Dim filas = If(tipo = "general",
                    u.Ejecutar("INSERT INTO inventario_detalle(empresa_id, inventario_id, variante_id, stock_sistema_u6, costo_corte_u6) " &
                               "SELECT s.empresa_id, @i, s.variante_id, s.cantidad_base_u6, " &
                               "       CASE WHEN s.cantidad_base_u6 > 0 THEN round(s.valor_u6::numeric * 1000000 / s.cantidad_base_u6) ELSE 0 END " &
                               "FROM saldo_stock s WHERE s.almacen_id = @a", "i", id, "a", almacenId),
                    u.Ejecutar("INSERT INTO inventario_detalle(empresa_id, inventario_id, variante_id, stock_sistema_u6, costo_corte_u6) " &
                               "SELECT v.empresa_id, @i, v.id, COALESCE(s.cantidad_base_u6, 0), " &
                               "       CASE WHEN s.cantidad_base_u6 > 0 THEN round(s.valor_u6::numeric * 1000000 / s.cantidad_base_u6) ELSE 0 END " &
                               "FROM variante_producto v LEFT JOIN saldo_stock s ON s.variante_id = v.id AND s.almacen_id = @a WHERE v.id = ANY(@v)",
                               "i", id, "a", almacenId, "v", lista))
                If filas = 0 Then Throw New ReglaNegocioException("DOCUMENTO_VACIO", "No hay productos para contar.")
                Return id
            End Function)
    End Function

    ''' <summary>Hoja de conteo. En conteo ciego no se muestra el stock del sistema ni la diferencia.</summary>
    Public Function Hoja(inventarioId As Long, Optional ciego As Boolean = False) As List(Of LineaConteoDto)
        Return EnTransaccion(Permisos.InventarioContar,
            Function(u)
                ExigirInventario(u, inventarioId, Nothing)
                Return u.Consultar(
                    "SELECT d.id, v.id, v.codigo, v.descripcion_comercial, v.tipo_envase, um.codigo, v.contenido_base_por_envase_u6, d.stock_sistema_u6, d.fisico_u6, d.costo_corte_u6 " &
                    "FROM inventario_detalle d JOIN variante_producto v ON v.id = d.variante_id JOIN producto_base p ON p.id = v.producto_base_id " &
                    "JOIN unidad_medida um ON um.id = p.unidad_base_id WHERE d.inventario_id = @i ORDER BY p.descripcion, v.codigo",
                    Function(rd)
                        Dim l As New LineaConteoDto With {
                            .Id = rd.GetInt64(0), .VarianteId = rd.GetInt64(1), .VarianteCodigo = rd.GetString(2), .Descripcion = rd.GetString(3),
                            .Presentacion = rd.GetString(4), .Unidad = rd.GetString(5), .ContenidoEnvaseU6 = rd.GetInt64(6),
                            .FisicoU6 = If(rd.IsDBNull(8), CType(Nothing, Long?), rd.GetInt64(8))}
                        Dim sistema = rd.GetInt64(7), costo = rd.GetInt64(9)
                        If l.FisicoU6.HasValue Then
                            Dim dif = l.FisicoU6.Value - sistema
                            l.Resultado = If(dif > 0, "sobrante", If(dif < 0, "faltante", "sin diferencia"))
                            If Not ciego Then l.DiferenciaU6 = dif : l.ValorDiferenciaU6 = EscalaU6.Multiplicar(dif, costo)
                        Else
                            l.Resultado = "sin contar"
                        End If
                        If ciego Then
                            If l.FisicoU6.HasValue Then l.Resultado = "contado"
                        Else
                            l.SistemaU6 = sistema
                        End If
                        Return l
                    End Function, "i", inventarioId)
            End Function)
    End Function

    ''' <summary>Registra el físico = envases × contenido + parcial (unidad base). Ambos Nothing = vuelve a pendiente.</summary>
    Public Sub RegistrarConteo(detalleId As Long, envasesU6 As Long?, parcialU6 As Long?)
        If (envasesU6.HasValue AndAlso envasesU6.Value < 0) OrElse (parcialU6.HasValue AndAlso parcialU6.Value < 0) Then
            Throw New ReglaNegocioException("CANTIDAD_INVALIDA", "El conteo no puede ser negativo.")
        End If
        EnTransaccion(Permisos.InventarioContar,
            Function(u)
                Dim inv = u.EscalarLong("SELECT inventario_id FROM inventario_detalle WHERE id = @d", "d", detalleId)
                ExigirInventario(u, inv, "borrador")
                Return Contar(u, detalleId, envasesU6, parcialU6)
            End Function)
    End Sub

    ''' <summary>Importa conteos (variante_codigo;envases;parcial). Todo o nada; filas vacías quedan pendientes.</summary>
    Public Function ImportarConteo(inventarioId As Long, textoCsv As String) As Integer
        Return EnTransaccion(Permisos.InventarioContar,
            Function(u)
                ExigirInventario(u, inventarioId, "borrador")
                Dim detalles = u.Consultar("SELECT v.codigo, d.id FROM inventario_detalle d JOIN variante_producto v ON v.id = d.variante_id WHERE d.inventario_id = @i",
                                           Function(rd) (rd.GetString(0), rd.GetInt64(1)), "i", inventarioId).ToDictionary(Function(x) x.Item1, Function(x) x.Item2, StringComparer.Ordinal)
                Dim lectura = LectorConteo.Leer(textoCsv)
                Dim errores = lectura.Errores.Select(Function(e) e.ToString()).ToList()
                Dim contados = 0
                For Each l In lectura.Lineas
                    Dim id As Long
                    If Not detalles.TryGetValue(l.VarianteCodigo, id) Then errores.Add($"Fila {l.Linea}: '{l.VarianteCodigo}' no esta en el inventario") : Continue For
                    If Not l.EnvasesU6.HasValue AndAlso Not l.ParcialU6.HasValue Then Continue For   ' pendiente, no cero
                    Contar(u, id, l.EnvasesU6, l.ParcialU6)
                    contados += 1
                Next
                If errores.Count > 0 Then Throw New ReglaNegocioException("IMPORTACION_CON_ERRORES", "No se importo nada. " & String.Join("; ", errores.Take(10)))
                Return contados
            End Function)
    End Function

    ''' <summary>Cierra el conteo (todas las líneas contadas). Desde aquí la variante vuelve a admitir movimientos.</summary>
    Public Sub CerrarConteo(inventarioId As Long)
        Cambiar(inventarioId, "borrador", "contado", Permisos.InventarioContar)
    End Sub

    ''' <summary>Vuelve a abrir el conteo para recontar (antes de autorizar).</summary>
    Public Sub Recontar(inventarioId As Long)
        Cambiar(inventarioId, "contado", "borrador", Permisos.InventarioContar)
    End Sub

    Public Sub Revisar(inventarioId As Long)
        EnTransaccion(Permisos.InventarioAprobar,
            Function(u)
                ExigirInventario(u, inventarioId, "contado")
                Return u.Ejecutar("UPDATE inventario SET estado = 'revisado', revisor_id = @u WHERE id = @i", "u", Sesion.UsuarioId, "i", inventarioId)
            End Function)
    End Sub

    ''' <summary>
    ''' Autoriza y aplica el ajuste: sobrantes como ajuste positivo al costo del corte; faltantes como ajuste negativo
    ''' al costo vigente. Un documento por signo; cada línea se ajusta una sola vez. Quien autoriza no puede haber contado.
    ''' </summary>
    Public Function AutorizarAjuste(inventarioId As Long, fecha As Date, motivo As String) As ResumenInventario
        Dim m = ServicioAdministracion.Requerido(motivo, "motivo del ajuste")
        Return EnTransaccion(Permisos.InventarioAprobar,
            Function(u)
                Dim inv = ExigirInventario(u, inventarioId, "revisado")
                If u.Escalar("SELECT 1 FROM inventario_detalle WHERE inventario_id = @i AND contado_por = @u", "i", inventarioId, "u", Sesion.UsuarioId) IsNot Nothing Then
                    Throw New ReglaNegocioException("APROBACION_NO_INDEPENDIENTE", "Quien autoriza el ajuste no puede haber contado.")
                End If
                Dim lineas = u.Consultar("SELECT id, variante_id, fisico_u6 - stock_sistema_u6, costo_corte_u6 FROM inventario_detalle WHERE inventario_id = @i AND fisico_u6 <> stock_sistema_u6 ORDER BY id",
                                         Function(rd) (Id:=rd.GetInt64(0), Variante:=rd.GetInt64(1), Dif:=rd.GetInt64(2), Costo:=rd.GetInt64(3)), "i", inventarioId)
                Dim stock As New ServicioStock(CadenaConexion, Sesion)
                For Each grupo In {(Signo:=1, Tipo:=TipoDocumentoStock.AjustePositivo, Sufijo:="P"), (Signo:=-1, Tipo:=TipoDocumentoStock.AjusteNegativo, Sufijo:="N")}
                    Dim g = grupo
                    Dim delGrupo = lineas.Where(Function(l) Math.Sign(l.Dif) = g.Signo).ToList()
                    If delGrupo.Count = 0 Then Continue For
                    Dim doc = stock.ContabilizarEn(u, New DocumentoStockNuevo(inv.Almacen, g.Tipo, fecha, $"{inv.Numero}-AJ{g.Sufijo}",
                        delGrupo.Select(Function(l) New LineaDocumentoStock(l.Variante, Math.Abs(l.Dif), l.Costo))) With {.Motivo = m})
                    For Each l In delGrupo
                        u.Ejecutar("INSERT INTO inventario_ajuste(empresa_id, inventario_detalle_id, documento_stock_id, autorizador_id, motivo) VALUES (@e, @d, @doc, @u, @m)",
                                   "e", Sesion.EmpresaId, "d", l.Id, "doc", doc, "u", Sesion.UsuarioId, "m", m)
                    Next
                Next
                u.Ejecutar("UPDATE inventario SET estado = 'cerrado', autorizador_id = @u WHERE id = @i", "u", Sesion.UsuarioId, "i", inventarioId)
                Return LeerResumen(u, inventarioId)
            End Function)
    End Function

    Public Function Resumen(inventarioId As Long) As ResumenInventario
        Return EnTransaccion(Permisos.InventarioContar,
            Function(u)
                ExigirInventario(u, inventarioId, Nothing)
                Return LeerResumen(u, inventarioId)
            End Function)
    End Function

    Public Function Listar(almacenId As Long) As List(Of InventarioDto)
        Return EnTransaccion(Permisos.InventarioContar,
            Function(u) u.Consultar("SELECT i.id, i.numero, i.tipo, i.fecha_corte, i.estado FROM inventario i JOIN almacen a ON a.id = i.almacen_id " &
                                    "WHERE i.almacen_id = @a AND a.operacion_id = @o ORDER BY i.id DESC",
                Function(rd) New InventarioDto With {.Id = rd.GetInt64(0), .Numero = rd.GetString(1), .Tipo = rd.GetString(2), .FechaCorte = rd.GetDateTime(3), .Estado = rd.GetString(4)},
                "a", almacenId, "o", Sesion.Operacion.Id))
    End Function

    ' ---------- Auxiliares ----------

    Private Function Contar(u As UnidadDeTrabajo, detalleId As Long, envasesU6 As Long?, parcialU6 As Long?) As Integer
        If Not envasesU6.HasValue AndAlso Not parcialU6.HasValue Then
            Return u.Ejecutar("UPDATE inventario_detalle SET fisico_u6 = NULL, contado_por = NULL WHERE id = @d", "d", detalleId)
        End If
        Dim contenido = u.EscalarLong("SELECT v.contenido_base_por_envase_u6 FROM inventario_detalle d JOIN variante_producto v ON v.id = d.variante_id WHERE d.id = @d", "d", detalleId)
        Dim fisico = EscalaU6.Multiplicar(If(envasesU6, 0L), contenido) + If(parcialU6, 0L)
        Return u.Ejecutar("UPDATE inventario_detalle SET fisico_u6 = @f, contado_por = @u WHERE id = @d", "f", fisico, "u", Sesion.UsuarioId, "d", detalleId)
    End Function

    Private Sub Cambiar(inventarioId As Long, desde As String, hacia As String, permiso As String)
        EnTransaccion(permiso,
            Function(u)
                ExigirInventario(u, inventarioId, desde)
                Return u.Ejecutar("UPDATE inventario SET estado = @h WHERE id = @i", "h", hacia, "i", inventarioId)
            End Function)
    End Sub

    Private Function ExigirInventario(u As UnidadDeTrabajo, inventarioId As Long, estado As String) As (Almacen As Long, Numero As String)
        Dim i = u.Consultar("SELECT i.almacen_id, i.numero, i.estado FROM inventario i JOIN almacen a ON a.id = i.almacen_id WHERE i.id = @i AND a.operacion_id = @o FOR UPDATE OF i",
                            Function(rd) (Almacen:=rd.GetInt64(0), Numero:=rd.GetString(1), Estado:=rd.GetString(2)), "i", inventarioId, "o", Sesion.Operacion.Id).SingleOrDefault()
        If i.Almacen = 0 Then Throw New ReglaNegocioException("OPERACION_AJENA", "El inventario no pertenece a la operacion seleccionada.")
        If estado IsNot Nothing AndAlso i.Estado <> estado Then
            Throw New ReglaNegocioException(If(i.Estado = "cerrado", "INVENTARIO_CERRADO", "TRANSICION_INVALIDA"), $"El inventario {i.Numero} esta {i.Estado}; se esperaba {estado}.")
        End If
        Return (i.Almacen, i.Numero)
    End Function

    Private Shared Function LeerResumen(u As UnidadDeTrabajo, inventarioId As Long) As ResumenInventario
        Return u.Consultar(
            "SELECT count(*), count(*) FILTER (WHERE fisico_u6 IS NULL), count(*) FILTER (WHERE fisico_u6 <> stock_sistema_u6), " &
            "       COALESCE(sum(round((stock_sistema_u6 - fisico_u6)::numeric * costo_corte_u6 / 1000000)) FILTER (WHERE fisico_u6 < stock_sistema_u6), 0)::bigint, " &
            "       COALESCE(sum(round((fisico_u6 - stock_sistema_u6)::numeric * costo_corte_u6 / 1000000)) FILTER (WHERE fisico_u6 > stock_sistema_u6), 0)::bigint " &
            "FROM inventario_detalle WHERE inventario_id = @i",
            Function(rd) New ResumenInventario With {.Lineas = CInt(rd.GetInt64(0)), .SinContar = CInt(rd.GetInt64(1)), .ConDiferencia = CInt(rd.GetInt64(2)),
                                                     .FaltanteValorU6 = rd.GetInt64(3), .SobranteValorU6 = rd.GetInt64(4)}, "i", inventarioId).Single()
    End Function

End Class
