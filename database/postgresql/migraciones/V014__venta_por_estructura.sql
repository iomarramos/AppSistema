-- V014: venta del servicio desde la estructura del menú (D13, definido por el usuario).
--
-- Cada componente de la estructura (bebida caliente, jugo, panes, fondo o sopa, complemento 1 y 2, huevo,
-- mantequilla, mermelada, yogurt, ensalada, fruta, cereales…) tiene un FACTOR DE CONSUMO: qué parte de las raciones
-- lo consume (plato caliente ≈ 100 %, complementos 30–70 %). Las alternativas de un componente se reparten
-- (jugo A 50 % + jugo B 50 %). Con esas raciones se obtiene el costo previsto de la estructura y la VENTA:
--   venta = costo previsto / Food Cost objetivo (48 % por defecto → 52 % de margen).
-- El Food Cost real del mes = costo real consumido / venta.

ALTER TABLE estructura_servicio
  ADD COLUMN factor_consumo_bp BIGINT NOT NULL DEFAULT 10000 CHECK (factor_consumo_bp BETWEEN 0 AND 10000);

-- Se fijan al aprobar la minuta (con el costo previsto) y no cambian después.
ALTER TABLE minuta
  ADD COLUMN costo_previsto_u6 BIGINT CHECK (costo_previsto_u6 >= 0),
  ADD COLUMN venta_prevista_u6 BIGINT CHECK (venta_prevista_u6 >= 0),
  ADD COLUMN food_cost_objetivo_bp BIGINT CHECK (food_cost_objetivo_bp BETWEEN 1 AND 10000);

CREATE FUNCTION fn_proteger_venta_minuta() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF OLD.estado <> 'borrador' AND (NEW.costo_previsto_u6, NEW.venta_prevista_u6, NEW.food_cost_objetivo_bp)
       IS DISTINCT FROM (OLD.costo_previsto_u6, OLD.venta_prevista_u6, OLD.food_cost_objetivo_bp) THEN
    RAISE EXCEPTION 'MINUTA_APROBADA: el costo y la venta previstos de una minuta aprobada no cambian';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER proteger_venta_minuta BEFORE UPDATE ON minuta FOR EACH ROW EXECUTE FUNCTION fn_proteger_venta_minuta();

-- Origen del ingreso: manual, contrato o estructura (venta calculada desde las minutas).
ALTER TABLE ingreso_servicio DROP CONSTRAINT ingreso_servicio_origen_check;
ALTER TABLE ingreso_servicio ADD CONSTRAINT ingreso_servicio_origen_check CHECK (origen IN ('manual', 'contrato', 'estructura'));
