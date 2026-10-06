-- V016: insumos sin costo de compra (p. ej. AGUA PARA RECETA, agua de red).
-- Un insumo así se costea en S/ 0 con fuente explícita; no deja la receta "pendiente" ni se le inventa un precio.
ALTER TABLE producto_base ADD COLUMN sin_costo_compra BOOLEAN NOT NULL DEFAULT false;
