-- V036: código del contrato de la operación (p. ej. PE017401 en ORCOPAMPA FOALI).
-- Lo usa la exportación del menú mensual en JSON (ServicioPlanSgp.MenuMensualJson). Es un dato de configuración:
-- queda vacío hasta que la operación lo registre; la exportación no inventa un código.

ALTER TABLE operacion ADD COLUMN IF NOT EXISTS codigo_contrato text;

COMMENT ON COLUMN operacion.codigo_contrato IS 'Código del contrato con el cliente (SGP). Se usa en la exportación del menú mensual.';
