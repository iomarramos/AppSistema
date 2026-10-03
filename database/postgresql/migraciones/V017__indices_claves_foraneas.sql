-- V017: índices para las claves foráneas que se recorren (detalle → documento, documento → almacén/minuta, catálogo).
-- Referencia: convenciones de PostgreSQL (toda FK que se usa para unir o para validar el borrado del padre debe tener un
-- índice cuyo prefijo sean sus columnas). PostgreSQL no los crea solo. Sin ellos, las consultas por documento y la
-- validación de cada borrado o cambio del padre recorren la tabla hija completa, y eso crece con los meses de
-- movimientos. Las FK a usuarios (quién creó, aprobó o contó) se dejan sin índice: no se consultan por ese camino y un
-- usuario no se borra (se desactiva). La lista de las que quedan sin índice está fijada en MigradorTests.
-- Nombres: ix_<tabla>_<columnas>, como el resto de los índices del esquema.

-- Almacén y kárdex
CREATE INDEX ix_movimiento_stock_kardex ON movimiento_stock(empresa_id, almacen_id, variante_id, fecha, secuencia);
CREATE INDEX ix_movimiento_stock_variante ON movimiento_stock(empresa_id, variante_id);
CREATE INDEX ix_saldo_stock_variante ON saldo_stock(empresa_id, variante_id);
CREATE INDEX ix_documento_stock_almacen_fecha ON documento_stock(empresa_id, almacen_id, fecha);
CREATE INDEX ix_documento_stock_almacen_destino ON documento_stock(empresa_id, almacen_destino_id) WHERE almacen_destino_id IS NOT NULL;
CREATE INDEX ix_documento_stock_requerimiento ON documento_stock(empresa_id, requerimiento_id) WHERE requerimiento_id IS NOT NULL;
CREATE INDEX ix_documento_stock_recepcion ON documento_stock(empresa_id, recepcion_id) WHERE recepcion_id IS NOT NULL;
CREATE INDEX ix_documento_stock_origen ON documento_stock(empresa_id, documento_origen_id) WHERE documento_origen_id IS NOT NULL;
CREATE INDEX ix_documento_stock_operacion_servicio ON documento_stock(empresa_id, operacion_servicio_id) WHERE operacion_servicio_id IS NOT NULL;
CREATE INDEX ix_documento_stock_detalle_documento ON documento_stock_detalle(empresa_id, documento_id);
CREATE INDEX ix_documento_stock_detalle_variante ON documento_stock_detalle(empresa_id, variante_id);
CREATE INDEX ix_documento_stock_detalle_recepcion ON documento_stock_detalle(empresa_id, recepcion_detalle_id) WHERE recepcion_detalle_id IS NOT NULL;

-- Inventario físico (el bloqueo de movimientos mientras se cuenta busca por variante)
CREATE INDEX ix_inventario_detalle_variante ON inventario_detalle(empresa_id, variante_id);
CREATE INDEX ix_inventario_ajuste_documento ON inventario_ajuste(empresa_id, documento_stock_id);

-- Compras y recepción
CREATE INDEX ix_pedido_compra_proveedor ON pedido_compra(empresa_id, proveedor_id);
CREATE INDEX ix_pedido_compra_prevision ON pedido_compra(empresa_id, prevision_id) WHERE prevision_id IS NOT NULL;
CREATE INDEX ix_pedido_compra_almacen ON pedido_compra(empresa_id, almacen_id);
CREATE INDEX ix_pedido_detalle_pedido ON pedido_detalle(empresa_id, pedido_id);
CREATE INDEX ix_pedido_detalle_prevision_detalle ON pedido_detalle(empresa_id, prevision_detalle_id) WHERE prevision_detalle_id IS NOT NULL;
CREATE INDEX ix_pedido_detalle_empaque ON pedido_detalle(empresa_id, empaque_id);
CREATE INDEX ix_prevision_detalle_producto ON prevision_detalle(empresa_id, producto_base_id);
CREATE INDEX ix_recepcion_pedido ON recepcion(empresa_id, pedido_id);
CREATE INDEX ix_recepcion_almacen ON recepcion(empresa_id, almacen_id);
CREATE INDEX ix_recepcion_detalle_recepcion ON recepcion_detalle(empresa_id, recepcion_id);
CREATE INDEX ix_recepcion_detalle_pedido_detalle ON recepcion_detalle(empresa_id, pedido_detalle_id);
CREATE INDEX ix_recepcion_detalle_variante ON recepcion_detalle(empresa_id, variante_id);
CREATE INDEX ix_proveedor_empaque_empaque ON proveedor_empaque(empresa_id, empaque_id);
CREATE INDEX ix_politica_abastecimiento_producto ON politica_abastecimiento(empresa_id, producto_base_id);

-- Catálogo y recetas
CREATE INDEX ix_variante_producto_producto ON variante_producto(empresa_id, producto_base_id);
CREATE INDEX ix_producto_base_categoria ON producto_base(empresa_id, categoria_id);
CREATE INDEX ix_receta_ingrediente_producto ON receta_ingrediente(empresa_id, producto_base_id);
CREATE INDEX ix_ingrediente_variante_permitida_variante ON ingrediente_variante_permitida(empresa_id, variante_id);

-- Minutas, requerimientos y producción
CREATE INDEX ix_minuta_detalle_receta_version ON minuta_detalle(empresa_id, receta_version_id);
CREATE INDEX ix_minuta_detalle_estructura ON minuta_detalle(empresa_id, estructura_id);
CREATE INDEX ix_minuta_estructura_fija_minuta ON minuta_estructura_fija(empresa_id, minuta_id);
CREATE INDEX ix_minuta_estructura_fija_producto ON minuta_estructura_fija(empresa_id, producto_base_id);
CREATE INDEX ix_costeo_ingrediente_ingrediente ON costeo_ingrediente(empresa_id, ingrediente_id);
CREATE INDEX ix_requerimiento_minuta ON requerimiento(empresa_id, minuta_id) WHERE minuta_id IS NOT NULL;
CREATE INDEX ix_requerimiento_almacen ON requerimiento(empresa_id, almacen_id);
CREATE INDEX ix_requerimiento_operacion_servicio ON requerimiento(empresa_id, operacion_servicio_id);
CREATE INDEX ix_requerimiento_detalle_producto ON requerimiento_detalle(empresa_id, producto_base_id);
CREATE INDEX ix_produccion_operacion_servicio ON produccion(empresa_id, operacion_servicio_id);
CREATE INDEX ix_produccion_documento_produccion ON produccion_documento(empresa_id, produccion_id);
CREATE INDEX ix_merma_produccion_produccion ON merma_produccion(empresa_id, produccion_id);
CREATE INDEX ix_merma_produccion_variante ON merma_produccion(empresa_id, variante_id) WHERE variante_id IS NOT NULL;
CREATE INDEX ix_factor_consumo_operacion_estructura ON factor_consumo_operacion(empresa_id, estructura_id);
CREATE INDEX ix_operacion_servicio_servicio ON operacion_servicio(empresa_id, servicio_id);

-- Cierres, contratos y resultados
CREATE INDEX ix_cierre_validacion_cierre_diario ON cierre_validacion(empresa_id, cierre_diario_id) WHERE cierre_diario_id IS NOT NULL;
CREATE INDEX ix_cierre_validacion_periodo ON cierre_validacion(empresa_id, periodo_id) WHERE periodo_id IS NOT NULL;
CREATE INDEX ix_gasto_periodo ON gasto(empresa_id, periodo_id);
CREATE INDEX ix_ingreso_servicio_periodo ON ingreso_servicio(empresa_id, periodo_id);
CREATE INDEX ix_contrato_cliente ON contrato(empresa_id, cliente_id);
CREATE INDEX ix_contrato_operacion ON contrato(empresa_id, operacion_id);
CREATE INDEX ix_contrato_servicio_contrato ON contrato_servicio(empresa_id, contrato_id);

-- Acceso (el ingreso busca los roles del usuario por operación)
CREATE INDEX ix_usuario_operacion_rol_operacion ON usuario_operacion_rol(empresa_id, operacion_id);
CREATE INDEX ix_usuario_operacion_rol_rol ON usuario_operacion_rol(empresa_id, rol_id);
CREATE INDEX ix_rol_permiso_permiso ON rol_permiso(empresa_id, permiso_id);

-- Central (sincronización)
CREATE INDEX ix_central_documento_evento ON central_documento(evento_id);
CREATE INDEX ix_central_movimiento_documento ON central_movimiento(documento_id);
