-- Plantilla de una salida de stock en UNA transacción. Variables: doc, cant (u6), espera (segundos).
BEGIN;
INSERT INTO documento_stock(id, empresa_id, almacen_id, numero, fecha, tipo, estado, usuario_id)
VALUES (:doc, 1, 1, :'doc', '2026-10-03', 'salida_produccion', 'borrador', 1);
INSERT INTO documento_stock_detalle(id, empresa_id, documento_id, variante_id, cantidad_base_u6, costo_unitario_base_u6, valor_u6)
VALUES (:doc, 1, :doc, 1, :cant, 8000000, :cant * 8);
UPDATE documento_stock SET estado = 'confirmado' WHERE id = :doc;
INSERT INTO movimiento_stock(empresa_id, documento_detalle_id, almacen_id, variante_id, fecha, secuencia, signo,
                             cantidad_base_u6, costo_unitario_base_u6, valor_u6, usuario_id)
VALUES (1, :doc, 1, 1, '2026-10-03', :doc, -1, :cant, 8000000, :cant * 8, 1);
SELECT pg_sleep(:espera);
COMMIT;
