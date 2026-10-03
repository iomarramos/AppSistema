-- V010: reglas del módulo 5 (inventarios físicos).
-- - El inventario nace en borrador; estados: borrador (contando) → contado → revisado → cerrado (ajuste aplicado).
-- - La fotografía (stock y costo al corte) no se modifica; el físico solo se registra mientras se cuenta.
-- - Mientras se cuenta (borrador) no hay movimientos de las variantes incluidas en ese almacén (T36: bloqueado).
-- - Un ajuste por línea (UNIQUE ya existente en inventario_ajuste): no se aplica dos veces (T35).
-- - Quien autoriza el ajuste no puede haber contado (aprobación independiente, D06).

ALTER TABLE inventario_detalle ADD COLUMN contado_por BIGINT;
ALTER TABLE inventario_detalle ADD CONSTRAINT inventario_detalle_contador_fk FOREIGN KEY (empresa_id, contado_por) REFERENCES usuario(empresa_id, id);
ALTER TABLE inventario ADD COLUMN autorizador_id BIGINT;
ALTER TABLE inventario ADD CONSTRAINT inventario_autorizador_fk FOREIGN KEY (empresa_id, autorizador_id) REFERENCES usuario(empresa_id, id);

CREATE FUNCTION fn_proteger_inventario() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v_pendientes bigint;
BEGIN
  IF TG_OP = 'INSERT' THEN
    IF NEW.estado <> 'borrador' THEN
      RAISE EXCEPTION 'INVENTARIO_NACE_BORRADOR: todo inventario empieza en borrador';
    END IF;
    RETURN NEW;
  END IF;
  IF TG_OP = 'DELETE' THEN
    IF OLD.estado <> 'borrador' THEN
      RAISE EXCEPTION 'INVENTARIO_CERRADO: el inventario % esta % y no se elimina', OLD.numero, OLD.estado;
    END IF;
    RETURN OLD;
  END IF;
  IF (NEW.almacen_id, NEW.numero, NEW.fecha_corte, NEW.tipo, NEW.usuario_id) IS DISTINCT FROM
     (OLD.almacen_id, OLD.numero, OLD.fecha_corte, OLD.tipo, OLD.usuario_id) THEN
    RAISE EXCEPTION 'INVENTARIO_CERRADO: los datos de corte no se modifican';
  END IF;
  IF NEW.estado IS DISTINCT FROM OLD.estado AND NOT (
       (OLD.estado = 'borrador' AND NEW.estado = 'contado') OR
       (OLD.estado = 'contado'  AND NEW.estado IN ('borrador','revisado')) OR
       (OLD.estado = 'revisado' AND NEW.estado IN ('contado','cerrado'))) THEN
    RAISE EXCEPTION 'TRANSICION_INVALIDA: un inventario % no pasa a %', OLD.estado, NEW.estado;
  END IF;
  IF OLD.estado = 'cerrado' THEN
    RAISE EXCEPTION 'INVENTARIO_CERRADO: el inventario % ya esta cerrado', OLD.numero;
  END IF;
  IF OLD.estado = 'borrador' AND NEW.estado = 'contado' THEN
    SELECT count(*) INTO v_pendientes FROM inventario_detalle WHERE empresa_id = NEW.empresa_id AND inventario_id = NEW.id AND fisico_u6 IS NULL;
    IF v_pendientes > 0 THEN
      RAISE EXCEPTION 'CONTEO_INCOMPLETO: hay % lineas sin contar (una celda vacia no es cero)', v_pendientes;
    END IF;
  END IF;
  IF NEW.estado = 'cerrado' AND (NEW.autorizador_id IS NULL OR EXISTS (
       SELECT 1 FROM inventario_detalle WHERE empresa_id = NEW.empresa_id AND inventario_id = NEW.id AND contado_por = NEW.autorizador_id)) THEN
    RAISE EXCEPTION 'APROBACION_NO_INDEPENDIENTE: quien autoriza el ajuste no puede haber contado';
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_inventario BEFORE INSERT OR UPDATE OR DELETE ON inventario
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_inventario();

CREATE FUNCTION fn_proteger_inventario_detalle() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v_estado text;
BEGIN
  SELECT estado INTO v_estado FROM inventario
   WHERE empresa_id = COALESCE(NEW.empresa_id, OLD.empresa_id) AND id = COALESCE(NEW.inventario_id, OLD.inventario_id);
  IF TG_OP = 'UPDATE' AND (NEW.variante_id, NEW.stock_sistema_u6, NEW.costo_corte_u6) IS DISTINCT FROM (OLD.variante_id, OLD.stock_sistema_u6, OLD.costo_corte_u6) THEN
    RAISE EXCEPTION 'FOTOGRAFIA_INMUTABLE: el stock y costo al corte no se modifican';
  END IF;
  IF v_estado <> 'borrador' THEN
    RAISE EXCEPTION 'INVENTARIO_CERRADO: el conteo de un inventario % no se modifica (vuelva a borrador para recontar)', v_estado;
  END IF;
  RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
END $$;

CREATE TRIGGER proteger_inventario_detalle BEFORE INSERT OR UPDATE OR DELETE ON inventario_detalle
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_inventario_detalle();

-- T36: mientras se cuenta, la variante está congelada en ese almacén.
CREATE FUNCTION fn_bloquear_por_inventario() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v_numero text;
BEGIN
  SELECT i.numero INTO v_numero FROM inventario i JOIN inventario_detalle d ON d.empresa_id = i.empresa_id AND d.inventario_id = i.id
   WHERE i.empresa_id = NEW.empresa_id AND i.almacen_id = NEW.almacen_id AND i.estado = 'borrador' AND d.variante_id = NEW.variante_id
   LIMIT 1;
  IF v_numero IS NOT NULL THEN
    RAISE EXCEPTION 'INVENTARIO_EN_CURSO: la variante se esta contando en el inventario %; cierre el conteo antes de moverla', v_numero;
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER bloquear_por_inventario BEFORE INSERT ON movimiento_stock
  FOR EACH ROW EXECUTE FUNCTION fn_bloquear_por_inventario();

-- Una sola toma abierta por almacén.
CREATE UNIQUE INDEX inventario_abierto ON inventario (empresa_id, almacen_id) WHERE estado IN ('borrador','contado','revisado');

DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY['inventario','inventario_detalle','inventario_ajuste'] LOOP
    EXECUTE format('CREATE TRIGGER auditar AFTER INSERT OR UPDATE OR DELETE ON %I
                    FOR EACH ROW EXECUTE FUNCTION fn_auditar()', t);
  END LOOP;
END $$;
