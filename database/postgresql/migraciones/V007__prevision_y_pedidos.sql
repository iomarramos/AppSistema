-- V007: reglas del módulo 2 (previsión y compras).
-- - Una previsión nace en borrador; su detalle solo cambia en borrador; validada solo pasa a reemplazada.
-- - Un pedido nace en borrador; cabecera y líneas solo cambian en borrador; los estados avanzan, no retroceden.
-- - Aprobar exige líneas. Cada línea usa un empaque que el proveedor del pedido ofrece, y si viene de la
--   previsión, del mismo producto base.
-- - El detalle de previsión guarda la fecha de quiebre (primera fecha sin stock antes de comprar).

ALTER TABLE prevision_detalle ADD COLUMN fecha_quiebre DATE;
ALTER TABLE prevision ADD COLUMN fecha_corte DATE;

-- ---------- Previsión ----------
CREATE FUNCTION fn_proteger_prevision() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN
    IF NEW.estado <> 'borrador' THEN
      RAISE EXCEPTION 'PREVISION_NACE_BORRADOR: toda prevision se calcula en borrador';
    END IF;
    RETURN NEW;
  END IF;
  IF TG_OP = 'DELETE' THEN
    IF OLD.estado <> 'borrador' THEN
      RAISE EXCEPTION 'PREVISION_VALIDADA: la prevision esta % y no se elimina', OLD.estado;
    END IF;
    RETURN OLD;
  END IF;
  IF (NEW.almacen_id, NEW.fecha_desde, NEW.fecha_hasta, NEW.fecha_calculo, NEW.usuario_id, NEW.fecha_corte)
     IS DISTINCT FROM (OLD.almacen_id, OLD.fecha_desde, OLD.fecha_hasta, OLD.fecha_calculo, OLD.usuario_id, OLD.fecha_corte) THEN
    RAISE EXCEPTION 'PREVISION_VALIDADA: los datos de una prevision calculada no se modifican; calcule otra';
  END IF;
  IF OLD.estado = 'reemplazada' OR (OLD.estado = 'validada' AND NEW.estado <> 'reemplazada') THEN
    RAISE EXCEPTION 'TRANSICION_INVALIDA: una prevision % no pasa a %', OLD.estado, NEW.estado;
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_prevision BEFORE INSERT OR UPDATE OR DELETE ON prevision
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_prevision();

CREATE FUNCTION fn_proteger_prevision_detalle() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v_estado text;
BEGIN
  SELECT estado INTO v_estado FROM prevision
   WHERE empresa_id = COALESCE(NEW.empresa_id, OLD.empresa_id) AND id = COALESCE(NEW.prevision_id, OLD.prevision_id);
  IF v_estado <> 'borrador' THEN
    RAISE EXCEPTION 'PREVISION_VALIDADA: el detalle de una prevision % no se modifica', v_estado;
  END IF;
  RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
END $$;

CREATE TRIGGER proteger_prevision_detalle BEFORE INSERT OR UPDATE OR DELETE ON prevision_detalle
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_prevision_detalle();

-- ---------- Pedido de compra ----------
CREATE FUNCTION fn_proteger_pedido() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v_lineas bigint;
BEGIN
  IF TG_OP = 'INSERT' THEN
    IF NEW.estado <> 'borrador' THEN
      RAISE EXCEPTION 'PEDIDO_NACE_BORRADOR: todo pedido se crea en borrador';
    END IF;
    RETURN NEW;
  END IF;
  IF TG_OP = 'DELETE' THEN
    IF OLD.estado <> 'borrador' THEN
      RAISE EXCEPTION 'PEDIDO_APROBADO: el pedido % esta % y no se elimina; anulelo', OLD.numero, OLD.estado;
    END IF;
    RETURN OLD;
  END IF;

  IF OLD.estado <> 'borrador' AND
     (NEW.almacen_id, NEW.proveedor_id, NEW.prevision_id, NEW.numero, NEW.tipo, NEW.fecha, NEW.moneda, NEW.usuario_id, NEW.aprobador_id)
     IS DISTINCT FROM (OLD.almacen_id, OLD.proveedor_id, OLD.prevision_id, OLD.numero, OLD.tipo, OLD.fecha, OLD.moneda, OLD.usuario_id, OLD.aprobador_id) THEN
    RAISE EXCEPTION 'PEDIDO_APROBADO: el pedido % esta % y no se modifica', OLD.numero, OLD.estado;
  END IF;

  IF NEW.estado IS DISTINCT FROM OLD.estado AND NOT (
       (OLD.estado = 'borrador' AND NEW.estado IN ('aprobado','anulado')) OR
       (OLD.estado = 'aprobado' AND NEW.estado IN ('enviado','parcial','recibido','anulado')) OR
       (OLD.estado = 'enviado'  AND NEW.estado IN ('parcial','recibido','anulado')) OR
       (OLD.estado = 'parcial'  AND NEW.estado IN ('recibido','anulado'))) THEN
    RAISE EXCEPTION 'TRANSICION_INVALIDA: un pedido % no pasa a %', OLD.estado, NEW.estado;
  END IF;

  IF OLD.estado = 'borrador' AND NEW.estado = 'aprobado' THEN
    SELECT count(*) INTO v_lineas FROM pedido_detalle WHERE empresa_id = NEW.empresa_id AND pedido_id = NEW.id;
    IF v_lineas = 0 THEN
      RAISE EXCEPTION 'PEDIDO_VACIO: no se aprueba un pedido sin lineas';
    END IF;
    IF NEW.aprobador_id IS NULL THEN
      RAISE EXCEPTION 'DATO_OBLIGATORIO: el pedido aprobado debe registrar quien lo aprobo';
    END IF;
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_pedido BEFORE INSERT OR UPDATE OR DELETE ON pedido_compra
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_pedido();

CREATE FUNCTION fn_proteger_pedido_detalle() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
  v_estado text;
  v_proveedor bigint;
BEGIN
  IF TG_OP <> 'INSERT' THEN
    SELECT estado INTO v_estado FROM pedido_compra WHERE empresa_id = OLD.empresa_id AND id = OLD.pedido_id;
    IF v_estado <> 'borrador' THEN
      RAISE EXCEPTION 'PEDIDO_APROBADO: las lineas de un pedido % no se modifican', v_estado;
    END IF;
  END IF;
  IF TG_OP = 'DELETE' THEN
    RETURN OLD;
  END IF;

  SELECT estado, proveedor_id INTO v_estado, v_proveedor FROM pedido_compra WHERE empresa_id = NEW.empresa_id AND id = NEW.pedido_id;
  IF v_estado <> 'borrador' THEN
    RAISE EXCEPTION 'PEDIDO_APROBADO: las lineas de un pedido % no se modifican', v_estado;
  END IF;
  IF NOT EXISTS (SELECT 1 FROM proveedor_empaque WHERE empresa_id = NEW.empresa_id AND proveedor_id = v_proveedor
                    AND empaque_id = NEW.empaque_id AND activo = 1) THEN
    RAISE EXCEPTION 'EMPAQUE_NO_OFRECIDO: el proveedor del pedido no ofrece ese empaque';
  END IF;
  IF NEW.prevision_detalle_id IS NOT NULL AND NOT EXISTS (
       SELECT 1 FROM prevision_detalle pd
         JOIN empaque_compra e ON e.empresa_id = pd.empresa_id AND e.id = NEW.empaque_id
         JOIN variante_producto v ON v.empresa_id = e.empresa_id AND v.id = e.variante_id
        WHERE pd.empresa_id = NEW.empresa_id AND pd.id = NEW.prevision_detalle_id AND v.producto_base_id = pd.producto_base_id) THEN
    RAISE EXCEPTION 'VARIANTE_INCOMPATIBLE: el empaque no es del producto de la linea de prevision';
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_pedido_detalle BEFORE INSERT OR UPDATE OR DELETE ON pedido_detalle
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_pedido_detalle();

-- Una sola previsión vigente (borrador o validada) por almacén y horizonte.
CREATE UNIQUE INDEX prevision_vigente ON prevision (empresa_id, almacen_id, fecha_desde, fecha_hasta)
  WHERE estado IN ('borrador','validada');

DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY['prevision','pedido_compra','pedido_detalle'] LOOP
    EXECUTE format('CREATE TRIGGER auditar AFTER INSERT OR UPDATE OR DELETE ON %I
                    FOR EACH ROW EXECUTE FUNCTION fn_auditar()', t);
  END LOOP;
END $$;
