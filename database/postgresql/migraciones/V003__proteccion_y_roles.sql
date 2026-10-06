-- V003: cierra las brechas H01–H03 de la auditoría de la guía (documentos confirmados y
-- saldo editables por SQL directo) y define los privilegios del rol de la aplicación.
--
-- Reglas:
--   * Un documento de stock o una recepción solo se edita mientras está en 'borrador'.
--     Una vez confirmado (o anulado) ni su cabecera ni su detalle cambian. Las correcciones
--     se hacen con documentos de reversión.
--   * Todo documento nace en 'borrador'; no se puede confirmar sin líneas.
--   * saldo_stock solo lo escribe fn_actualizar_saldo / fn_validar_movimiento (nivel de
--     trigger >= 2) y el rol de la aplicación no tiene permiso de escritura sobre él.

-- ---------- Cabecera de documento_stock y recepcion ----------

CREATE FUNCTION fn_proteger_cabecera_documento() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
  v_lineas bigint;
BEGIN
  IF TG_OP = 'INSERT' THEN
    IF NEW.estado <> 'borrador' THEN
      RAISE EXCEPTION 'DOCUMENTO_NACE_BORRADOR: todo documento se crea en borrador y luego se confirma';
    END IF;
    RETURN NEW;
  END IF;

  IF OLD.estado <> 'borrador' THEN
    RAISE EXCEPTION 'DOCUMENTO_CONFIRMADO: el documento % esta en estado % y no se modifica; use una reversion',
      OLD.id, OLD.estado;
  END IF;

  IF TG_OP = 'DELETE' THEN
    RETURN OLD;
  END IF;

  -- UPDATE desde borrador: solo se permite confirmar si tiene líneas.
  IF NEW.estado IN ('confirmado','confirmada') THEN
    IF TG_TABLE_NAME = 'documento_stock' THEN
      SELECT count(*) INTO v_lineas FROM documento_stock_detalle
       WHERE empresa_id = NEW.empresa_id AND documento_id = NEW.id;
    ELSE
      SELECT count(*) INTO v_lineas FROM recepcion_detalle
       WHERE empresa_id = NEW.empresa_id AND recepcion_id = NEW.id;
    END IF;
    IF v_lineas = 0 THEN
      RAISE EXCEPTION 'DOCUMENTO_VACIO: no se puede confirmar un documento sin lineas';
    END IF;
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_cabecera BEFORE INSERT OR UPDATE OR DELETE ON documento_stock
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_cabecera_documento();
CREATE TRIGGER proteger_cabecera BEFORE INSERT OR UPDATE OR DELETE ON recepcion
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_cabecera_documento();

-- ---------- Detalle ----------

CREATE FUNCTION fn_proteger_detalle_documento() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
  v_empresa bigint; v_doc bigint; v_estado text;
BEGIN
  IF TG_OP = 'DELETE' THEN v_empresa := OLD.empresa_id; ELSE v_empresa := NEW.empresa_id; END IF;

  IF TG_TABLE_NAME = 'documento_stock_detalle' THEN
    IF TG_OP = 'DELETE' THEN v_doc := OLD.documento_id; ELSE v_doc := NEW.documento_id; END IF;
    SELECT estado INTO v_estado FROM documento_stock WHERE empresa_id = v_empresa AND id = v_doc;
    IF TG_OP = 'UPDATE' AND OLD.documento_id <> NEW.documento_id THEN
      RAISE EXCEPTION 'DOCUMENTO_CONFIRMADO: una linea no cambia de documento';
    END IF;
  ELSE
    IF TG_OP = 'DELETE' THEN v_doc := OLD.recepcion_id; ELSE v_doc := NEW.recepcion_id; END IF;
    SELECT estado INTO v_estado FROM recepcion WHERE empresa_id = v_empresa AND id = v_doc;
    IF TG_OP = 'UPDATE' AND OLD.recepcion_id <> NEW.recepcion_id THEN
      RAISE EXCEPTION 'DOCUMENTO_CONFIRMADO: una linea no cambia de documento';
    END IF;
  END IF;

  IF v_estado IS NOT NULL AND v_estado <> 'borrador' THEN
    RAISE EXCEPTION 'DOCUMENTO_CONFIRMADO: el documento % esta en estado % y su detalle no se modifica', v_doc, v_estado;
  END IF;
  IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_detalle BEFORE INSERT OR UPDATE OR DELETE ON documento_stock_detalle
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_detalle_documento();
CREATE TRIGGER proteger_detalle BEFORE INSERT OR UPDATE OR DELETE ON recepcion_detalle
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_detalle_documento();

-- ---------- Saldo: proyección controlada ----------

CREATE FUNCTION fn_proteger_saldo() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  -- pg_trigger_depth() >= 2: la operación proviene de un trigger de movimiento_stock.
  IF pg_trigger_depth() < 2 THEN
    RAISE EXCEPTION 'SALDO_PROTEGIDO: saldo_stock solo cambia al contabilizar movimientos';
  END IF;
  IF TG_OP = 'DELETE' THEN RETURN OLD; END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_saldo BEFORE INSERT OR UPDATE OR DELETE ON saldo_stock
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_saldo();

CREATE FUNCTION fn_proteger_saldo_truncate() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  RAISE EXCEPTION 'SALDO_PROTEGIDO: saldo_stock no se vacia';
END $$;
CREATE TRIGGER proteger_saldo_truncate BEFORE TRUNCATE ON saldo_stock
  FOR EACH STATEMENT EXECUTE FUNCTION fn_proteger_saldo_truncate();

-- ---------- Conciliación saldo vs libro ----------
-- Debe devolver CERO filas. Cualquier fila es una inconsistencia a investigar.

CREATE VIEW v_conciliacion_saldo AS
SELECT COALESCE(s.empresa_id, m.empresa_id)   AS empresa_id,
       COALESCE(s.almacen_id, m.almacen_id)   AS almacen_id,
       COALESCE(s.variante_id, m.variante_id) AS variante_id,
       COALESCE(s.cantidad_base_u6, 0)        AS saldo_cantidad_u6,
       COALESCE(m.cantidad_u6, 0)             AS libro_cantidad_u6,
       COALESCE(s.valor_u6, 0)                AS saldo_valor_u6,
       COALESCE(m.valor_u6, 0)                AS libro_valor_u6
  FROM saldo_stock s
  FULL JOIN (SELECT empresa_id, almacen_id, variante_id,
                    SUM(signo * cantidad_base_u6) AS cantidad_u6,
                    SUM(signo * valor_u6)         AS valor_u6
               FROM movimiento_stock GROUP BY empresa_id, almacen_id, variante_id) m
    ON m.empresa_id = s.empresa_id AND m.almacen_id = s.almacen_id AND m.variante_id = s.variante_id
 WHERE COALESCE(s.cantidad_base_u6, 0) <> COALESCE(m.cantidad_u6, 0)
    OR COALESCE(s.valor_u6, 0)         <> COALESCE(m.valor_u6, 0);

-- ---------- Rol de la aplicación ----------
-- Rol de grupo sin inicio de sesión. Cada usuario de servicio de la aplicación se hace miembro.

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_stock') THEN
    CREATE ROLE app_stock NOLOGIN;
  END IF;
END $$;

GRANT USAGE ON SCHEMA public TO app_stock;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO app_stock;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO app_stock;
REVOKE UPDATE, DELETE, TRUNCATE ON movimiento_stock FROM app_stock;
REVOKE UPDATE, DELETE, TRUNCATE ON auditoria FROM app_stock;
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON saldo_stock FROM app_stock;
