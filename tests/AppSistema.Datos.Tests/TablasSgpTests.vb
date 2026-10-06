Imports System.Text.RegularExpressions
Imports Npgsql
Imports Xunit
Imports AppSistema.Dominio
Imports AppSistema.Dominio.Numerico
Imports AppSistema.Datos

''' <summary>
''' V026: tablas que muestran las pantallas del SGP (receta por régimen, nutrientes, control de raciones, venta del día,
''' cafetería y traspasos). Las reglas están en la base: se prueban con SQL directo, como las rechaza el servidor.
''' </summary>
Public Class TablasSgpTests

    Private Shared Function U(v As Decimal) As Long
        Return EscalaU6.DesdeDecimal(v)
    End Function

    ''' <summary>Ejecuta SQL como administrador y devuelve el código del error ("DIA_CERRADO", "23514", …) o Nothing si pasó.</summary>
    Private Shared Function Error_(bd As BaseDatosPrueba, sql As String) As String
        Try
            bd.EjecutarAdmin(sql)
            Return Nothing
        Catch ex As PostgresException
            Dim m = Regex.Match(ex.MessageText, "^([A-Z_]+): ")
            Return If(m.Success, m.Groups(1).Value, ex.SqlState)
        End Try
    End Function

    Private Const Unico As String = "23505"
    Private Const Restriccion As String = "23514"

    Private Shared Function EmpresaA(bd As BaseDatosPrueba) As Long
        Return bd.A.EmpresaId
    End Function

    Private Shared Function UsuarioAdmin(bd As BaseDatosPrueba) As Long
        Return Convert.ToInt64(bd.Escalar($"SELECT id FROM usuario WHERE empresa_id = {EmpresaA(bd)} AND login = 'admin'"))
    End Function

    ''' <summary>Servicio, régimen y su asignación a la operación; devuelve operacion_servicio.id.</summary>
    Private Shared Function ServicioDeLaOperacion(bd As BaseDatosPrueba) As Long
        Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, bd.Sesion("A"))
        Dim servicio = minutas.CrearServicio("ALM", "Almuerzo")
        Return minutas.AsignarServicio(servicio, minutas.CrearRegimen("GEN", "General"), Nothing)
    End Function

    Private Shared Function Cliente(bd As BaseDatosPrueba, codigo As String) As Long
        bd.EjecutarAdmin($"INSERT INTO cliente(empresa_id, codigo, nombre, identificacion_fiscal) VALUES ({EmpresaA(bd)}, '{codigo}', 'Cliente {codigo}', '{codigo}')")
        Return Convert.ToInt64(bd.Escalar($"SELECT id FROM cliente WHERE empresa_id = {EmpresaA(bd)} AND codigo = '{codigo}'"))
    End Function

    Private Shared Function VersionDeReceta(bd As BaseDatosPrueba, Optional aprobar As Boolean = False) As Long
        Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, bd.Sesion("A"))
        Dim version = recetas.CrearReceta("RUS", "Ensalada rusa", Nothing, U(1D), Nothing)
        recetas.AgregarIngrediente(version, bd.ProductoAceiteId, U(0.1D), Nothing, 1)
        If aprobar Then recetas.Aprobar(version)
        Return version
    End Function

    <FactPostgres>
    Public Sub La_receta_guarda_fantasia_categoria_dietetica_y_tipo_de_plato_con_jerarquia()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = EmpresaA(bd)
            VersionDeReceta(bd)
            bd.EjecutarAdmin($"INSERT INTO categoria_dietetica(empresa_id, codigo, nombre) VALUES ({e}, 'NORMAL', 'NORMAL')")
            bd.EjecutarAdmin($"INSERT INTO tipo_plato(empresa_id, codigo, nombre) VALUES ({e}, 'SAL', 'SALSAS / ALCUZAS / ISLAS')")
            bd.EjecutarAdmin($"INSERT INTO tipo_plato(empresa_id, codigo, nombre, padre_id) " &
                             $"SELECT {e}, 'ALC', 'ALCUZAS', id FROM tipo_plato WHERE empresa_id = {e} AND codigo = 'SAL'")
            bd.EjecutarAdmin($"UPDATE receta SET nombre_fantasia = 'ENSALADA RUSA II', " &
                             $"categoria_dietetica_id = (SELECT id FROM categoria_dietetica WHERE empresa_id = {e} AND codigo = 'NORMAL'), " &
                             $"tipo_plato_id = (SELECT id FROM tipo_plato WHERE empresa_id = {e} AND codigo = 'ALC') WHERE empresa_id = {e} AND codigo = 'RUS'")
            Assert.Equal("SALSAS / ALCUZAS / ISLAS", Convert.ToString(bd.Escalar(
                $"SELECT p.nombre FROM receta r JOIN tipo_plato t ON t.empresa_id = r.empresa_id AND t.id = r.tipo_plato_id " &
                $"JOIN tipo_plato p ON p.empresa_id = t.empresa_id AND p.id = t.padre_id WHERE r.empresa_id = {e} AND r.codigo = 'RUS'")))
            ' Un tipo de plato no es su propio padre, y los códigos no se repiten.
            Assert.Equal(Restriccion, Error_(bd, $"UPDATE tipo_plato SET padre_id = id WHERE empresa_id = {e} AND codigo = 'SAL'"))
            Assert.Equal(Unico, Error_(bd, $"INSERT INTO tipo_plato(empresa_id, codigo, nombre) VALUES ({e}, 'SAL', 'otra')"))
            ' El nombre de fantasía no puede quedar en blanco.
            Assert.Equal(Restriccion, Error_(bd, $"UPDATE receta SET nombre_fantasia = '  ' WHERE empresa_id = {e} AND codigo = 'RUS'"))
        End Using
    End Sub

    <FactPostgres>
    Public Sub El_ingrediente_guarda_aprovechamiento_y_ajuste_de_coccion_en_puntos_base()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = EmpresaA(bd)
            Dim version = VersionDeReceta(bd)
            bd.EjecutarAdmin($"UPDATE receta_ingrediente SET pct_aprovechamiento_bp = 10000, pct_ajuste_coccion_bp = 10000 WHERE empresa_id = {e} AND receta_version_id = {version}")
            Assert.Equal(10000L, Convert.ToInt64(bd.Escalar($"SELECT pct_aprovechamiento_bp FROM receta_ingrediente WHERE empresa_id = {e} AND receta_version_id = {version}")))
            Assert.Equal(Restriccion, Error_(bd, $"UPDATE receta_ingrediente SET pct_aprovechamiento_bp = -1 WHERE empresa_id = {e} AND receta_version_id = {version}"))
            Assert.Equal(Restriccion, Error_(bd, $"UPDATE receta_ingrediente SET pct_ajuste_coccion_bp = 100001 WHERE empresa_id = {e} AND receta_version_id = {version}"))
        End Using
    End Sub

    <FactPostgres>
    Public Sub El_detalle_por_regimen_y_local_sigue_las_reglas_de_la_version()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = EmpresaA(bd)
            Dim version = VersionDeReceta(bd)
            Dim minutas As New ServicioMinutas(bd.CadenaAplicacion, bd.Sesion("A"))
            Dim regimen = minutas.CrearRegimen("R3", "Regimen 3")
            Dim op = bd.A.OperacionId
            Dim filaRegimen = $"INSERT INTO receta_ingrediente_ambito(empresa_id, receta_version_id, ambito, regimen_id, producto_base_id, cantidad_base_bruta_u6, pct_aprovechamiento_bp, pct_ajuste_coccion_bp) " &
                              $"VALUES ({e}, {version}, 'regimen', {regimen}, {bd.ProductoAceiteId}, {U(0.015D)}, 10000, 10000)"
            bd.EjecutarAdmin(filaRegimen)
            ' La misma fila no se repite por régimen, y el ámbito exige su régimen u operación.
            Assert.Equal(Unico, Error_(bd, filaRegimen))
            Assert.Equal(Restriccion, Error_(bd, $"INSERT INTO receta_ingrediente_ambito(empresa_id, receta_version_id, ambito, producto_base_id, cantidad_base_bruta_u6) " &
                                                 $"VALUES ({e}, {version}, 'regimen', {bd.ProductoAceiteId}, {U(0.01D)})"))
            Assert.Equal(Restriccion, Error_(bd, $"INSERT INTO receta_ingrediente_ambito(empresa_id, receta_version_id, ambito, producto_base_id, cantidad_base_bruta_u6) " &
                                                 $"VALUES ({e}, {version}, 'local', {bd.ProductoAceiteId}, {U(0.01D)})"))
            bd.EjecutarAdmin($"INSERT INTO receta_ingrediente_ambito(empresa_id, receta_version_id, ambito, operacion_id, producto_base_id, cantidad_base_bruta_u6) " &
                             $"VALUES ({e}, {version}, 'local', {op}, {bd.ProductoAceiteId}, {U(0.02D)})")
            Assert.Equal(Restriccion, Error_(bd, $"UPDATE receta_ingrediente_ambito SET cantidad_base_bruta_u6 = 0 WHERE empresa_id = {e} AND ambito = 'local'"))
            Assert.Equal(2L, Convert.ToInt64(bd.Escalar($"SELECT count(*) FROM receta_ingrediente_ambito WHERE empresa_id = {e}")))

            ' Con la versión aprobada, el detalle por régimen y el local ya no cambian (igual que el patrón).
            Dim recetas As New ServicioRecetas(bd.CadenaAplicacion, bd.Sesion("A"))
            recetas.Aprobar(version)
            Assert.Equal("RECETA_APROBADA", Error_(bd, $"UPDATE receta_ingrediente_ambito SET orden = 5 WHERE empresa_id = {e}"))
            Assert.Equal("RECETA_APROBADA", Error_(bd, $"DELETE FROM receta_ingrediente_ambito WHERE empresa_id = {e}"))
            Assert.Equal("RECETA_APROBADA", Error_(bd, filaRegimen))
        End Using
    End Sub

    <FactPostgres>
    Public Sub La_composicion_nutricional_se_guarda_por_ingrediente_y_nutriente_sin_repetir()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = EmpresaA(bd)
            bd.EjecutarAdmin($"INSERT INTO nutriente(empresa_id, codigo, nombre, unidad, orden) VALUES ({e}, 'KCAL', 'Energia', 'kcal', 2)")
            Dim fila = $"INSERT INTO producto_nutriente(empresa_id, producto_base_id, nutriente_id, valor_100g_u6, fuente) " &
                       $"SELECT {e}, {bd.ProductoAceiteId}, id, {U(884D)}, 'tabla de composicion' FROM nutriente WHERE empresa_id = {e} AND codigo = 'KCAL'"
            bd.EjecutarAdmin(fila)
            Assert.Equal(Unico, Error_(bd, fila))
            Assert.Equal(Restriccion, Error_(bd, fila.Replace(U(884D).ToString(), "-1")))
            Assert.Equal(Restriccion, Error_(bd, $"UPDATE producto_nutriente SET fuente = ' ' WHERE empresa_id = {e}"))
            Assert.Equal(Unico, Error_(bd, $"INSERT INTO nutriente(empresa_id, codigo, nombre, unidad) VALUES ({e}, 'KCAL', 'otra', 'kcal')"))
        End Using
    End Sub

    <FactPostgres>
    Public Sub El_estado_mensual_de_la_planificacion_es_unico_por_servicio_mes_y_tipo()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = EmpresaA(bd)
            Dim os = ServicioDeLaOperacion(bd)
            bd.EjecutarAdmin($"INSERT INTO planificacion_mes_estado(empresa_id, operacion_servicio_id, anio, mes, tipo) VALUES ({e}, {os}, 2026, 10, 'teorica')")
            bd.EjecutarAdmin($"INSERT INTO planificacion_mes_estado(empresa_id, operacion_servicio_id, anio, mes, tipo) VALUES ({e}, {os}, 2026, 10, 'real')")
            Assert.Equal(Unico, Error_(bd, $"INSERT INTO planificacion_mes_estado(empresa_id, operacion_servicio_id, anio, mes, tipo) VALUES ({e}, {os}, 2026, 10, 'real')"))
            Assert.Equal(Restriccion, Error_(bd, $"INSERT INTO planificacion_mes_estado(empresa_id, operacion_servicio_id, anio, mes, tipo) VALUES ({e}, {os}, 2026, 13, 'real')"))
            ' Cerrado exige la fecha de cierre y abierto no la lleva.
            Assert.Equal(Restriccion, Error_(bd, $"UPDATE planificacion_mes_estado SET estado = 'cerrado' WHERE empresa_id = {e} AND tipo = 'teorica'"))
            bd.EjecutarAdmin($"UPDATE planificacion_mes_estado SET estado = 'cerrado', fecha_cierre = now() WHERE empresa_id = {e} AND tipo = 'teorica'")
            Assert.Equal("cerrado", Convert.ToString(bd.Escalar($"SELECT estado FROM planificacion_mes_estado WHERE empresa_id = {e} AND tipo = 'teorica'")))
        End Using
    End Sub

    <FactPostgres>
    Public Sub El_control_de_raciones_valida_el_concepto_el_cliente_y_el_dia_cerrado()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = EmpresaA(bd)
            Dim usr = UsuarioAdmin(bd)
            Dim os = ServicioDeLaOperacion(bd)
            Dim cli = Cliente(bd, "CLI1")
            Dim fila = Function(concepto As String, cliente As String, raciones As Integer, fecha As String) _
                $"INSERT INTO control_racion(empresa_id, operacion_servicio_id, fecha, concepto, cliente_id, raciones, usuario_id) VALUES ({e}, {os}, '{fecha}', '{concepto}', {cliente}, {raciones}, {usr})"
            bd.EjecutarAdmin(fila("cliente", cli.ToString(), 470, "2026-10-01"))
            bd.EjecutarAdmin(fila("personal", "NULL", 20, "2026-10-01"))
            bd.EjecutarAdmin(fila("producidas", "NULL", 500, "2026-10-01"))
            bd.EjecutarAdmin(fila("mer_descon", "NULL", 22, "2026-10-01"))
            bd.EjecutarAdmin(fila("mer_produc", "NULL", 18, "2026-10-01"))
            ' Una fila por cliente y concepto y día (aunque el cliente sea NULL).
            Assert.Equal(Unico, Error_(bd, fila("cliente", cli.ToString(), 1, "2026-10-01")))
            Assert.Equal(Unico, Error_(bd, fila("personal", "NULL", 1, "2026-10-01")))
            ' Solo las filas de cliente llevan cliente.
            Assert.Equal(Restriccion, Error_(bd, fila("cliente", "NULL", 1, "2026-10-02")))
            Assert.Equal(Restriccion, Error_(bd, fila("personal", cli.ToString(), 1, "2026-10-02")))
            Assert.Equal(Restriccion, Error_(bd, fila("otro", "NULL", 1, "2026-10-02")))
            Assert.Equal(Restriccion, Error_(bd, fila("producidas", "NULL", -1, "2026-10-02")))

            ' Casilla facturable por día.
            bd.EjecutarAdmin($"INSERT INTO control_racion_dia(empresa_id, operacion_servicio_id, fecha, facturable, usuario_id) VALUES ({e}, {os}, '2026-10-01', true, {usr})")
            Assert.Equal(Unico, Error_(bd, $"INSERT INTO control_racion_dia(empresa_id, operacion_servicio_id, fecha, usuario_id) VALUES ({e}, {os}, '2026-10-01', {usr})"))

            ' Día cerrado: ni se agrega, ni se cambia, ni se borra.
            bd.EjecutarAdmin($"INSERT INTO cierre_diario(empresa_id, operacion_id, fecha, estado) VALUES ({e}, {bd.A.OperacionId}, '2026-10-01', 'cerrado')")
            Assert.Equal("DIA_CERRADO", Error_(bd, fila("personal", "NULL", 1, "2026-10-01")))
            Assert.Equal("DIA_CERRADO", Error_(bd, $"UPDATE control_racion SET raciones = 480 WHERE empresa_id = {e} AND concepto = 'cliente'"))
            Assert.Equal("DIA_CERRADO", Error_(bd, $"DELETE FROM control_racion WHERE empresa_id = {e} AND concepto = 'mer_produc'"))
            Assert.Equal("DIA_CERRADO", Error_(bd, $"UPDATE control_racion_dia SET facturable = false WHERE empresa_id = {e}"))
            ' Otro día sigue abierto.
            bd.EjecutarAdmin(fila("personal", "NULL", 20, "2026-10-02"))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Quien_no_tiene_permiso_no_cambia_raciones_ni_ventas_del_dia()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = EmpresaA(bd)
            Dim os = ServicioDeLaOperacion(bd)
            Dim admin = New ServicioAdministracion(bd.CadenaAplicacion, bd.Sesion("A"))
            admin.CrearUsuario("almacen", "Almacenero", "Clave-Persona-2026", bd.A.OperacionId, "ALMACEN")
            Dim almacenero = Convert.ToInt64(bd.Escalar($"SELECT id FROM usuario WHERE empresa_id = {e} AND login = 'almacen'"))
            bd.EjecutarAdmin($"INSERT INTO forma_pago(empresa_id, codigo, nombre) VALUES ({e}, 'CONTADO', 'Contado')")
            Dim fp = Convert.ToInt64(bd.Escalar($"SELECT id FROM forma_pago WHERE empresa_id = {e} AND codigo = 'CONTADO'"))
            ' set_config(…, true) vale solo en esta transacción: es la sesión del almacenero.
            Dim comoAlmacenero = $"SELECT set_config('app.usuario_id', '{almacenero}', true); "
            Assert.Equal("SIN_PERMISO", Error_(bd, comoAlmacenero &
                $"INSERT INTO control_racion(empresa_id, operacion_servicio_id, fecha, concepto, raciones, usuario_id) VALUES ({e}, {os}, '2026-10-03', 'personal', 20, {almacenero})"))
            Assert.Equal("SIN_PERMISO", Error_(bd, comoAlmacenero &
                $"INSERT INTO venta_servicio_dia(empresa_id, operacion_servicio_id, fecha, forma_pago_id, importe_u6, usuario_id) VALUES ({e}, {os}, '2026-10-03', {fp}, {U(100D)}, {almacenero})"))
            ' El administrador (todos los permisos) sí puede, y la base deja constancia de su usuario.
            Dim adminId = UsuarioAdmin(bd)
            bd.EjecutarAdmin($"SELECT set_config('app.usuario_id', '{adminId}', true); " &
                $"INSERT INTO control_racion(empresa_id, operacion_servicio_id, fecha, concepto, raciones, usuario_id) VALUES ({e}, {os}, '2026-10-03', 'personal', 20, {almacenero})")
            Assert.Equal(adminId, Convert.ToInt64(bd.Escalar($"SELECT usuario_id FROM control_racion WHERE empresa_id = {e} AND fecha = '2026-10-03'")))
        End Using
    End Sub

    <FactPostgres>
    Public Sub La_venta_del_servicio_por_dia_acepta_cliente_opcional_y_no_se_repite()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = EmpresaA(bd)
            Dim usr = UsuarioAdmin(bd)
            Dim os = ServicioDeLaOperacion(bd)
            Dim cli = Cliente(bd, "CLI1")
            bd.EjecutarAdmin($"INSERT INTO forma_pago(empresa_id, codigo, nombre) VALUES ({e}, 'CONTADO', 'Contado')")
            Dim fp = Convert.ToInt64(bd.Escalar($"SELECT id FROM forma_pago WHERE empresa_id = {e} AND codigo = 'CONTADO'"))
            Dim fila = Function(cliente As String, importe As Long, fecha As String) _
                $"INSERT INTO venta_servicio_dia(empresa_id, operacion_servicio_id, fecha, cliente_id, forma_pago_id, importe_u6, usuario_id) VALUES ({e}, {os}, '{fecha}', {cliente}, {fp}, {importe}, {usr})"
            bd.EjecutarAdmin(fila(cli.ToString(), U(727.35D), "2026-09-01"))
            bd.EjecutarAdmin(fila("NULL", U(100D), "2026-09-01"))
            Assert.Equal(Unico, Error_(bd, fila(cli.ToString(), U(1D), "2026-09-01")))
            Assert.Equal(Unico, Error_(bd, fila("NULL", U(1D), "2026-09-01")))
            Assert.Equal(Restriccion, Error_(bd, fila("NULL", -1, "2026-09-02")))
            Assert.Equal(U(827.35D), Convert.ToInt64(bd.Escalar($"SELECT sum(importe_u6) FROM venta_servicio_dia WHERE empresa_id = {e}")))
            bd.EjecutarAdmin($"INSERT INTO cierre_diario(empresa_id, operacion_id, fecha, estado) VALUES ({e}, {bd.A.OperacionId}, '2026-09-01', 'cerrado')")
            Assert.Equal("DIA_CERRADO", Error_(bd, $"UPDATE venta_servicio_dia SET importe_u6 = {U(5D)} WHERE empresa_id = {e}"))
        End Using
    End Sub

    <FactPostgres>
    Public Sub La_venta_de_cafeteria_lleva_articulos_con_cantidad_y_precio()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = EmpresaA(bd)
            Dim usr = UsuarioAdmin(bd)
            bd.EjecutarAdmin($"INSERT INTO venta_cafeteria(empresa_id, almacen_id, fecha, centro_costo, usuario_id) VALUES ({e}, {bd.A.AlmacenId}, '2026-10-04', 'CC-1', {usr})")
            Dim venta = Convert.ToInt64(bd.Escalar($"SELECT id FROM venta_cafeteria WHERE empresa_id = {e}"))
            bd.EjecutarAdmin($"INSERT INTO venta_cafeteria_detalle(empresa_id, venta_id, variante_id, cantidad_base_u6, precio_venta_u6, tipo_pago) VALUES ({e}, {venta}, {bd.VarianteAceiteId}, {U(2D)}, {U(3.5D)}, 'EFECTIVO')")
            Assert.Equal(Restriccion, Error_(bd, $"INSERT INTO venta_cafeteria_detalle(empresa_id, venta_id, variante_id, cantidad_base_u6, precio_venta_u6) VALUES ({e}, {venta}, {bd.VarianteAceiteId}, 0, {U(1D)})"))
            Assert.Equal(Restriccion, Error_(bd, $"INSERT INTO venta_cafeteria_detalle(empresa_id, venta_id, variante_id, cantidad_base_u6, precio_venta_u6) VALUES ({e}, {venta}, {bd.VarianteAceiteId}, {U(1D)}, -1)"))
            ' La venta de una empresa no se une al almacén de otra.
            Assert.Equal("23503", Error_(bd, $"INSERT INTO venta_cafeteria(empresa_id, almacen_id, fecha, usuario_id) VALUES ({e}, {bd.B.AlmacenId}, '2026-10-04', {usr})"))
        End Using
    End Sub

    <FactPostgres>
    Public Sub Proveedor_pedido_y_recepcion_guardan_los_datos_que_imprime_el_SGP()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = EmpresaA(bd)
            bd.EjecutarAdmin($"INSERT INTO proveedor(empresa_id, codigo, nombre, identificacion_fiscal) VALUES ({e}, 'P1', 'Proveedor uno', '20525141188')")
            Dim prov = Convert.ToInt64(bd.Escalar($"SELECT id FROM proveedor WHERE empresa_id = {e} AND codigo = 'P1'"))
            bd.EjecutarAdmin($"UPDATE proveedor SET direccion = 'AV. LA FLORESTA 367 DPTO 402 - SANTIAGO DE SURCO', fax = '5227081', telefono = '522-1181 / 5227081' WHERE empresa_id = {e} AND id = {prov}")
            Assert.Equal("5227081", Convert.ToString(bd.Escalar($"SELECT fax FROM proveedor WHERE empresa_id = {e} AND id = {prov}")))
            Assert.Equal(Restriccion, Error_(bd, $"UPDATE proveedor SET direccion = '' WHERE empresa_id = {e} AND id = {prov}"))

            Dim usr = UsuarioAdmin(bd)
            Dim pedido = $"INSERT INTO pedido_compra(empresa_id, almacen_id, proveedor_id, numero, tipo, fecha, moneda, estado, usuario_id, codigo_sgp, persona_contacto, correo_destino, periodo_desde, periodo_hasta) " &
                         $"VALUES ({e}, {bd.A.AlmacenId}, {prov}, 'PC-1', 'normal', '2026-09-25', 'PEN', 'borrador', {usr}, 'LCL-06695-092026-ORCOPA_1', 'JOHN MARTIN', 'compras@proveedor.test', '2026-10-01', '2026-10-31')"
            bd.EjecutarAdmin(pedido)
            ' El código de la tanda se repite entre proveedores: no es único. El periodo no va al revés.
            Assert.Equal(Restriccion, Error_(bd, pedido.Replace("'PC-1'", "'PC-2'").Replace("'2026-10-01', '2026-10-31'", "'2026-10-31', '2026-10-01'")))

            Dim recepcion = $"INSERT INTO recepcion(empresa_id, almacen_id, proveedor_id, numero, tipo_documento, numero_documento, fecha_documento, fecha_recepcion, moneda, tipo_ingreso, estado, usuario_id, modalidad_sgp, folio, glosa, fletes_u6, exento_u6) " &
                            $"VALUES ({e}, {bd.A.AlmacenId}, {prov}, 'R-1', 'factura', '1251', '2026-07-19', '2026-07-20', 'PEN', 'caja_chica', 'borrador', {usr}, 'FOFI', 81, 'Caja chica', 0, {U(19D)})"
            bd.EjecutarAdmin(recepcion)
            Assert.Equal(81L, Convert.ToInt64(bd.Escalar($"SELECT folio FROM recepcion WHERE empresa_id = {e} AND numero = 'R-1'")))
            Assert.Equal(Restriccion, Error_(bd, recepcion.Replace("'R-1'", "'R-2'").Replace("'FOFI'", "'XYZ'")))
            Assert.Equal(Restriccion, Error_(bd, recepcion.Replace("'R-1'", "'R-3'").Replace("81,", "-1,")))
        End Using
    End Sub

    <FactPostgres>
    Public Sub El_traspaso_guarda_guia_placa_documento_de_origen_y_cantidad_recibida()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = EmpresaA(bd)
            Dim usr = UsuarioAdmin(bd)
            ' La base exige que una entrada de traspaso cite su salida (ORIGEN_REQUERIDO). Cuando el origen es OTRO contrato
            ' (como en el SGP: Tambomayo -> Orcopampa) esa salida vive en otra base: ver docs/TABLAS_SGP_VS_BASE.md, pendiente 1.
            bd.EjecutarAdmin($"INSERT INTO documento_stock(empresa_id, almacen_id, numero, fecha, tipo, estado, usuario_id) VALUES ({e}, {bd.A.AlmacenId}, 'TS-1', '2026-08-16', 'traspaso_salida', 'borrador', {usr})")
            Dim salida = Convert.ToInt64(bd.Escalar($"SELECT id FROM documento_stock WHERE empresa_id = {e} AND numero = 'TS-1'"))
            bd.EjecutarAdmin($"INSERT INTO documento_stock(empresa_id, almacen_id, numero, fecha, tipo, estado, usuario_id, documento_origen_id) VALUES ({e}, {bd.A.AlmacenId}, 'TE-1', '2026-08-17', 'traspaso_entrada', 'borrador', {usr}, {salida})")
            Dim doc = Convert.ToInt64(bd.Escalar($"SELECT id FROM documento_stock WHERE empresa_id = {e} AND numero = 'TE-1'"))
            Dim traspaso = $"INSERT INTO traspaso_documento(empresa_id, documento_stock_id, modalidad, folio, contrato_origen, numero_documento_origen, fecha_origen, guia_remision, placa_camion) " &
                           $"VALUES ({e}, {doc}, 'contrato', 98, 'PE016801', '13606', '2026-08-16', 'T001-100', 'ABC-123')"
            bd.EjecutarAdmin(traspaso)
            Assert.Equal(Unico, Error_(bd, traspaso))   ' un documento de stock tiene un solo registro de traspaso
            Assert.Equal(Restriccion, Error_(bd, traspaso.Replace($"{doc},", $"{doc + 1000},").Replace("'contrato'", "'camion'")))
            Assert.Equal("13606", Convert.ToString(bd.Escalar($"SELECT numero_documento_origen FROM traspaso_documento WHERE empresa_id = {e}")))

            bd.EjecutarAdmin($"INSERT INTO documento_stock_detalle(empresa_id, documento_id, variante_id, cantidad_base_u6, costo_unitario_base_u6, valor_u6, cantidad_planificada_u6) " &
                             $"VALUES ({e}, {doc}, {bd.VarianteAceiteId}, {U(4D)}, {U(10D)}, {U(40D)}, {U(5D)})")
            Dim linea = Convert.ToInt64(bd.Escalar($"SELECT id FROM documento_stock_detalle WHERE empresa_id = {e} AND documento_id = {doc}"))
            bd.EjecutarAdmin($"INSERT INTO traspaso_linea(empresa_id, documento_detalle_id, cantidad_recibida_base_u6) VALUES ({e}, {linea}, {U(3.5D)})")
            Assert.Equal(U(5D), Convert.ToInt64(bd.Escalar($"SELECT cantidad_planificada_u6 FROM documento_stock_detalle WHERE id = {linea}")))
            Assert.Equal(Restriccion, Error_(bd, $"INSERT INTO traspaso_linea(empresa_id, documento_detalle_id, cantidad_recibida_base_u6) VALUES ({e}, {linea + 1}, -1)"))
            Assert.Equal(Unico, Error_(bd, $"INSERT INTO traspaso_linea(empresa_id, documento_detalle_id, cantidad_recibida_base_u6) VALUES ({e}, {linea}, 1)"))
            bd.EjecutarAdmin($"UPDATE documento_stock SET fecha_produccion = '2026-08-17' WHERE empresa_id = {e} AND id = {doc}")
            bd.EjecutarAdmin($"UPDATE operacion SET codigo_optimum = 'PE017401' WHERE empresa_id = {e}")
        End Using
    End Sub

    <FactPostgres>
    Public Sub Las_tablas_nuevas_estan_aisladas_por_empresa()
        Using bd = BaseDatosPrueba.Crear()
            Dim e = EmpresaA(bd)
            bd.EjecutarAdmin($"INSERT INTO nutriente(empresa_id, codigo, nombre, unidad) VALUES ({e}, 'AGUA', 'Agua', 'g')")
            bd.EjecutarAdmin($"INSERT INTO forma_pago(empresa_id, codigo, nombre) VALUES ({e}, 'CONTADO', 'Contado')")
            ' La aplicación de la empresa B (rol app_stock con su empresa) no ve lo de A.
            Dim sesionB = bd.Sesion("B")
            Using cn As New NpgsqlConnection(bd.CadenaAplicacion)
                cn.Open()
                Using tx = cn.BeginTransaction()
                    Using cmd As New NpgsqlCommand($"SELECT set_config('app.empresa_id', '{sesionB.EmpresaId}', true)", cn, tx)
                        cmd.ExecuteNonQuery()
                    End Using
                    For Each tabla In {"nutriente", "forma_pago"}
                        Using cmd As New NpgsqlCommand($"SELECT count(*) FROM {tabla}", cn, tx)
                            Assert.Equal(0L, Convert.ToInt64(cmd.ExecuteScalar()))
                        End Using
                    Next
                    tx.Rollback()
                End Using
            End Using
        End Using
    End Sub

End Class
