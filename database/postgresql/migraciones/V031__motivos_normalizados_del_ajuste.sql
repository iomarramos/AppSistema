-- V027: motivos normalizados del ajuste de inventario (especificación 03, FormAjustesInventario y reportes R25 y R26).
--
-- * motivo_codigo: uno de la lista de la especificación; las filas anteriores quedan como OTRO y conservan su texto.
-- * explicacion y documento_soporte: texto libre que acompaña al motivo (la boleta de ajuste lo imprime).
-- * El ajuste sigue siendo una fila inmutable por línea (ya lo garantiza el flujo de autorización).
-- Con la conexión del propietario o del instalador las reglas de permiso no aplican.

ALTER TABLE inventario_ajuste
  ADD COLUMN motivo_codigo TEXT NOT NULL DEFAULT 'OTRO'
    CHECK (motivo_codigo IN ('ERROR_CONTEO', 'INGRESO_OMITIDO', 'SALIDA_OMITIDA', 'ERROR_UNIDAD', 'MERMA_NO_REGISTRADA',
                             'DIGITACION', 'VENCIMIENTO', 'PERDIDA', 'SOBRANTE', 'OTRO')),
  ADD COLUMN explicacion TEXT,
  ADD COLUMN documento_soporte TEXT;

CREATE INDEX ix_inventario_ajuste_motivo ON inventario_ajuste(empresa_id, motivo_codigo);
