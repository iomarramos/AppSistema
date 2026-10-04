-- V024: días base del stock por contrato (operación). Los días de stock = inventario final ÷ (consumo del periodo ÷ días base).
-- El contrato decide el criterio: 20, 21 o 31 días según cómo lo mide el cliente; por defecto 30 (días del mes comercial).
-- Solo cambia la configuración de la operación: no hay tablas nuevas, así que no hacen falta GRANT ni RLS adicionales.
ALTER TABLE operacion
  ADD COLUMN dias_stock_base BIGINT NOT NULL DEFAULT 30 CHECK (dias_stock_base BETWEEN 1 AND 31);
