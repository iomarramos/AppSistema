-- V008: reglas del módulo 3 (almacén).
-- - Un comprobante del proveedor (tipo + número) se recibe una sola vez: repetir la confirmación no duplica (T21).
-- - La línea de recepción de un pedido es del mismo empaque del pedido.
-- - Devoluciones de producción y entradas de traspaso referencian el documento de salida que las origina.

CREATE UNIQUE INDEX recepcion_comprobante_unico ON recepcion (empresa_id, proveedor_id, tipo_documento, numero_documento)
  WHERE estado <> 'anulada';

CREATE FUNCTION fn_validar_recepcion_pedido() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF NEW.pedido_detalle_id IS NOT NULL AND NOT EXISTS (
       SELECT 1 FROM pedido_detalle d
        WHERE d.empresa_id = NEW.empresa_id AND d.id = NEW.pedido_detalle_id AND d.empaque_id = NEW.empaque_id) THEN
    RAISE EXCEPTION 'PRESENTACION_INCOMPATIBLE: la linea recibida no es del empaque de la linea del pedido';
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER validar_recepcion_pedido BEFORE INSERT OR UPDATE ON recepcion_detalle
  FOR EACH ROW EXECUTE FUNCTION fn_validar_recepcion_pedido();

CREATE FUNCTION fn_validar_origen_documento() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v_tipo_origen text;
BEGIN
  IF NEW.tipo IN ('devolucion_produccion','traspaso_entrada') THEN
    SELECT tipo INTO v_tipo_origen FROM documento_stock WHERE empresa_id = NEW.empresa_id AND id = NEW.documento_origen_id;
    IF v_tipo_origen IS NULL OR (NEW.tipo = 'devolucion_produccion' AND v_tipo_origen <> 'salida_produccion')
       OR (NEW.tipo = 'traspaso_entrada' AND v_tipo_origen <> 'traspaso_salida') THEN
      RAISE EXCEPTION 'ORIGEN_REQUERIDO: % debe referenciar su documento de salida', NEW.tipo;
    END IF;
  END IF;
  IF NEW.tipo = 'baja' AND COALESCE(btrim(NEW.motivo), '') = '' THEN
    RAISE EXCEPTION 'DATO_OBLIGATORIO: una baja exige motivo';
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER validar_origen_documento BEFORE INSERT ON documento_stock
  FOR EACH ROW EXECUTE FUNCTION fn_validar_origen_documento();

DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY['recepcion','documento_stock'] LOOP
    EXECUTE format('CREATE TRIGGER auditar AFTER INSERT OR UPDATE OR DELETE ON %I
                    FOR EACH ROW EXECUTE FUNCTION fn_auditar()', t);
  END LOOP;
END $$;
