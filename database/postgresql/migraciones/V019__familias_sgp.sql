-- V019: familias del SGP (pedido del usuario, 2026-10-03).
-- El listado del SGP clasifica cada producto en tres niveles: familia › subfamilia › grupo (p. ej. ABARROTES ›
-- ABARROTES 2DA NECESIDAD › REPOSTERIA). Las categorías pasan a ser jerárquicas (padre_id) y cada presentación
-- (producto SGP) guarda su grupo. El ingrediente conserva su categoría, que es la familia.

ALTER TABLE categoria_producto ADD COLUMN padre_id BIGINT;
ALTER TABLE categoria_producto ADD CONSTRAINT categoria_padre_fk FOREIGN KEY (empresa_id, padre_id) REFERENCES categoria_producto(empresa_id, id);
ALTER TABLE categoria_producto ADD CONSTRAINT categoria_no_es_su_padre CHECK (padre_id IS NULL OR padre_id <> id);
CREATE INDEX ix_categoria_producto_padre ON categoria_producto(empresa_id, padre_id) WHERE padre_id IS NOT NULL;

ALTER TABLE variante_producto ADD COLUMN categoria_id BIGINT;
ALTER TABLE variante_producto ADD CONSTRAINT variante_categoria_fk FOREIGN KEY (empresa_id, categoria_id) REFERENCES categoria_producto(empresa_id, id);
CREATE INDEX ix_variante_producto_categoria ON variante_producto(empresa_id, categoria_id) WHERE categoria_id IS NOT NULL;
