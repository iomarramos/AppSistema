-- V028: foto de los movimientos del día en el momento del cierre (T38).
-- El cierre toma el mismo bloqueo de almacén que cada contabilización, así que los movimientos del día son exactamente
-- los que la foto cuenta: ninguno puede confirmarse después. Un día cerrado antes de esta versión queda sin foto (NULL).
-- Con la conexión del propietario o del instalador las reglas de permiso no aplican.

ALTER TABLE cierre_diario
  ADD COLUMN movimientos_al_cerrar BIGINT,
  ADD COLUMN valor_al_cerrar_u6 BIGINT;
