-- V025: el requerimiento adicional exige motivo (especificación 02 y 07: "adicional exige motivo").
--
-- * Las borradores de adicional que existan sin motivo reciben una leyenda que dice que son anteriores a esta regla
--   (el trigger de requerimientos deja modificar borradores, no atendidos ni aprobados).
-- * La restricción va NOT VALID: se revisa en cada alta y cada cambio, pero no obliga a revisar filas antiguas que ya
--   no se pueden modificar (atendidas o anuladas).
-- Con la conexión del propietario o del instalador las reglas de permiso no aplican.

ALTER TABLE requerimiento ADD COLUMN motivo TEXT;

UPDATE requerimiento
SET motivo = 'Sin motivo registrado (anterior a V025)'
WHERE tipo = 'adicional' AND estado = 'borrador' AND (motivo IS NULL OR btrim(motivo) = '');

ALTER TABLE requerimiento ADD CONSTRAINT requerimiento_adicional_con_motivo
  CHECK (tipo <> 'adicional' OR (motivo IS NOT NULL AND btrim(motivo) <> '')) NOT VALID;
