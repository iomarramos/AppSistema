-- V011: reglas del módulo 6 (cierres y control).
-- - Un día o un mes cerrado no se reabre en esta versión ni cambia sus datos de cierre.
-- - Ingresos y gastos de un período cerrado no se modifican (T48: el reporte del período cerrado es repetible).
-- - Las contabilizaciones en día/mes cerrado ya las rechaza fn_validar_movimiento (V002, T39).

CREATE FUNCTION fn_proteger_cierre() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP = 'DELETE' THEN
    IF OLD.estado = 'cerrado' THEN
      RAISE EXCEPTION 'PERIODO_CERRADO: un cierre no se elimina';
    END IF;
    RETURN OLD;
  END IF;
  IF TG_OP = 'UPDATE' AND OLD.estado = 'cerrado' AND
     (to_jsonb(NEW) - 'estado_envio') IS DISTINCT FROM (to_jsonb(OLD) - 'estado_envio') THEN
    RAISE EXCEPTION 'PERIODO_CERRADO: un cierre no se reabre ni se modifica';
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_cierre BEFORE UPDATE OR DELETE ON cierre_diario
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_cierre();
CREATE TRIGGER proteger_cierre BEFORE UPDATE OR DELETE ON periodo_mensual
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_cierre();

CREATE FUNCTION fn_proteger_dato_de_periodo() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v_estado text;
BEGIN
  SELECT estado INTO v_estado FROM periodo_mensual
   WHERE empresa_id = COALESCE(NEW.empresa_id, OLD.empresa_id) AND id = COALESCE(NEW.periodo_id, OLD.periodo_id);
  IF v_estado = 'cerrado' THEN
    RAISE EXCEPTION 'PERIODO_CERRADO: los datos de un mes cerrado no se modifican';
  END IF;
  IF TG_OP = 'UPDATE' AND OLD.periodo_id IS DISTINCT FROM NEW.periodo_id THEN
    SELECT estado INTO v_estado FROM periodo_mensual WHERE empresa_id = OLD.empresa_id AND id = OLD.periodo_id;
    IF v_estado = 'cerrado' THEN
      RAISE EXCEPTION 'PERIODO_CERRADO: los datos de un mes cerrado no se modifican';
    END IF;
  END IF;
  RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
END $$;

CREATE TRIGGER proteger_ingreso BEFORE INSERT OR UPDATE OR DELETE ON ingreso_servicio
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_dato_de_periodo();
CREATE TRIGGER proteger_gasto BEFORE INSERT OR UPDATE OR DELETE ON gasto
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_dato_de_periodo();

DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY['cierre_diario','periodo_mensual','ingreso_servicio','gasto'] LOOP
    EXECUTE format('CREATE TRIGGER auditar AFTER INSERT OR UPDATE OR DELETE ON %I
                    FOR EACH ROW EXECUTE FUNCTION fn_auditar()', t);
  END LOOP;
END $$;
