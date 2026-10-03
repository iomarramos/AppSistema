-- V009: reglas del módulo 4 (producción).
-- - Un requerimiento nace en borrador; su detalle solo cambia en borrador; atendido o anulado no cambia.
-- - Un registro de producción por minuta; raciones servidas + excedentes no superan las producidas.
-- - Los documentos vinculados a una producción son entregas (salida a producción) o devoluciones del mismo servicio.
-- - Una merma no incluida en el consumo debe tener su baja (D12: la merma de cocina ya está en la entrega).

CREATE FUNCTION fn_proteger_requerimiento() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    IF NEW.estado <> 'borrador' THEN
      RAISE EXCEPTION 'REQUERIMIENTO_NACE_BORRADOR: todo requerimiento se crea en borrador';
    END IF;
    RETURN NEW;
  END IF;
  IF TG_OP = 'DELETE' THEN
    IF OLD.estado <> 'borrador' THEN
      RAISE EXCEPTION 'REQUERIMIENTO_ATENDIDO: el requerimiento % esta % y no se elimina', OLD.numero, OLD.estado;
    END IF;
    RETURN OLD;
  END IF;
  IF OLD.estado <> 'borrador' THEN
    RAISE EXCEPTION 'REQUERIMIENTO_ATENDIDO: el requerimiento % esta % y no se modifica', OLD.numero, OLD.estado;
  END IF;
  IF (NEW.almacen_id, NEW.minuta_id, NEW.operacion_servicio_id, NEW.fecha, NEW.numero, NEW.usuario_id)
     IS DISTINCT FROM (OLD.almacen_id, OLD.minuta_id, OLD.operacion_servicio_id, OLD.fecha, OLD.numero, OLD.usuario_id) THEN
    RAISE EXCEPTION 'DATO_INVALIDO: los datos del requerimiento no se cambian; cree otro';
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_requerimiento BEFORE INSERT OR UPDATE OR DELETE ON requerimiento
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_requerimiento();

CREATE FUNCTION fn_proteger_requerimiento_detalle() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v_estado text;
BEGIN
  SELECT estado INTO v_estado FROM requerimiento
   WHERE empresa_id = COALESCE(NEW.empresa_id, OLD.empresa_id) AND id = COALESCE(NEW.requerimiento_id, OLD.requerimiento_id);
  IF v_estado <> 'borrador' THEN
    RAISE EXCEPTION 'REQUERIMIENTO_ATENDIDO: el detalle de un requerimiento % no se modifica', v_estado;
  END IF;
  RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
END $$;

CREATE TRIGGER proteger_requerimiento_detalle BEFORE INSERT OR UPDATE OR DELETE ON requerimiento_detalle
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_requerimiento_detalle();

ALTER TABLE requerimiento_detalle ADD CONSTRAINT requerimiento_producto_unico UNIQUE (empresa_id, requerimiento_id, producto_base_id);
ALTER TABLE requerimiento ADD COLUMN tipo TEXT NOT NULL DEFAULT 'calculado' CHECK (tipo IN ('calculado','adicional'));

-- Producción: uno por minuta; cifras coherentes.
CREATE UNIQUE INDEX produccion_por_minuta ON produccion (empresa_id, minuta_id) WHERE minuta_id IS NOT NULL;
ALTER TABLE produccion ADD CONSTRAINT produccion_raciones_coherentes CHECK (raciones_servidas + raciones_excedentes <= raciones_producidas);

CREATE FUNCTION fn_validar_produccion_documento() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (
       SELECT 1 FROM documento_stock d JOIN produccion p ON p.empresa_id = d.empresa_id AND p.id = NEW.produccion_id
        WHERE d.empresa_id = NEW.empresa_id AND d.id = NEW.documento_stock_id AND d.estado = 'confirmado'
          AND d.tipo IN ('salida_produccion','devolucion_produccion') AND d.operacion_servicio_id = p.operacion_servicio_id) THEN
    RAISE EXCEPTION 'DOCUMENTO_INCOMPATIBLE: solo se vinculan entregas o devoluciones confirmadas del mismo servicio';
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER validar_produccion_documento BEFORE INSERT OR UPDATE ON produccion_documento
  FOR EACH ROW EXECUTE FUNCTION fn_validar_produccion_documento();

ALTER TABLE merma_produccion ADD CONSTRAINT merma_con_baja CHECK (ya_incluida_consumo = 1 OR documento_baja_id IS NOT NULL);

DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY['requerimiento','produccion','merma_produccion'] LOOP
    EXECUTE format('CREATE TRIGGER auditar AFTER INSERT OR UPDATE OR DELETE ON %I
                    FOR EACH ROW EXECUTE FUNCTION fn_auditar()', t);
  END LOOP;
END $$;
