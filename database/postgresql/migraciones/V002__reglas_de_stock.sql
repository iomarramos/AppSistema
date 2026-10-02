-- V002: reglas de integridad del esquema v0.3, portadas de SQLite a PL/pgSQL.
-- Equivalen a los 12 triggers del SQL original, más:
--   * bloqueo de la fila de saldo antes de validar una salida (concurrencia),
--   * validación del signo permitido por tipo de documento.
-- Los mensajes comienzan con un código funcional estable (p. ej. STOCK_INSUFICIENTE).

-- ---------- Presentación / múltiplo ----------

CREATE FUNCTION fn_validar_recepcion_detalle() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF NEW.empaque_id IS NOT NULL AND NOT EXISTS (
       SELECT 1 FROM empaque_compra e
        WHERE e.empresa_id = NEW.empresa_id AND e.id = NEW.empaque_id AND e.variante_id = NEW.variante_id) THEN
    RAISE EXCEPTION 'PRESENTACION_INCOMPATIBLE: el empaque no corresponde a la variante';
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER validar_recepcion_detalle BEFORE INSERT OR UPDATE ON recepcion_detalle
  FOR EACH ROW EXECUTE FUNCTION fn_validar_recepcion_detalle();

CREATE FUNCTION fn_validar_pedido_detalle() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v_multiplo bigint; v_minimo bigint;
BEGIN
  SELECT multiplo_empaques, minimo_empaques INTO v_multiplo, v_minimo
    FROM empaque_compra WHERE empresa_id = NEW.empresa_id AND id = NEW.empaque_id;
  IF FOUND AND (NEW.cantidad_empaques % v_multiplo <> 0 OR NEW.cantidad_empaques < v_minimo) THEN
    RAISE EXCEPTION 'MULTIPLO_INCOMPATIBLE: la cantidad incumple el minimo (%) o el multiplo (%)', v_minimo, v_multiplo;
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER validar_pedido_detalle BEFORE INSERT OR UPDATE ON pedido_detalle
  FOR EACH ROW EXECUTE FUNCTION fn_validar_pedido_detalle();

CREATE FUNCTION fn_validar_variante_receta() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF NOT EXISTS (
       SELECT 1 FROM receta_ingrediente i
         JOIN variante_producto v ON v.empresa_id = i.empresa_id AND v.producto_base_id = i.producto_base_id
        WHERE i.empresa_id = NEW.empresa_id AND i.id = NEW.ingrediente_id AND v.id = NEW.variante_id) THEN
    RAISE EXCEPTION 'VARIANTE_INCOMPATIBLE: la variante no pertenece al producto base del ingrediente';
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER validar_variante_receta BEFORE INSERT OR UPDATE ON ingrediente_variante_permitida
  FOR EACH ROW EXECUTE FUNCTION fn_validar_variante_receta();

-- ---------- Libro inmutable ----------

CREATE FUNCTION fn_movimiento_inmutable() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  RAISE EXCEPTION 'MOVIMIENTO_INMUTABLE: corregir mediante un documento de reversion';
END $$;

CREATE TRIGGER movimiento_inmutable BEFORE UPDATE OR DELETE ON movimiento_stock
  FOR EACH ROW EXECUTE FUNCTION fn_movimiento_inmutable();
CREATE TRIGGER movimiento_inmutable_truncate BEFORE TRUNCATE ON movimiento_stock
  FOR EACH STATEMENT EXECUTE FUNCTION fn_movimiento_inmutable();

CREATE FUNCTION fn_auditoria_inmutable() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  RAISE EXCEPTION 'AUDITORIA_INMUTABLE: la auditoria no se modifica';
END $$;

CREATE TRIGGER auditoria_inmutable BEFORE UPDATE OR DELETE ON auditoria
  FOR EACH ROW EXECUTE FUNCTION fn_auditoria_inmutable();
CREATE TRIGGER auditoria_inmutable_truncate BEFORE TRUNCATE ON auditoria
  FOR EACH STATEMENT EXECUTE FUNCTION fn_auditoria_inmutable();

-- ---------- Contabilización ----------
-- SECURITY DEFINER: el rol de la aplicación no escribe en saldo_stock; solo lo hace
-- esta función, ejecutada con los privilegios de su propietario.

CREATE FUNCTION fn_validar_movimiento() RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path = public, pg_temp AS $$
DECLARE
  v_tipo text;
  v_cant bigint;
  v_valor bigint;
BEGIN
  -- 1. El movimiento debe coincidir con una línea de un documento confirmado.
  SELECT d.tipo INTO v_tipo
    FROM documento_stock_detalle l
    JOIN documento_stock d ON d.empresa_id = l.empresa_id AND d.id = l.documento_id
   WHERE l.empresa_id = NEW.empresa_id AND l.id = NEW.documento_detalle_id
     AND l.variante_id = NEW.variante_id AND d.almacen_id = NEW.almacen_id
     AND d.fecha = NEW.fecha AND d.estado = 'confirmado'
     AND l.cantidad_base_u6 = NEW.cantidad_base_u6 AND l.valor_u6 = NEW.valor_u6
     AND l.costo_unitario_base_u6 = NEW.costo_unitario_base_u6;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'MOVIMIENTO_NO_COINCIDE: el movimiento no coincide con un documento confirmado';
  END IF;

  -- 2. Signo permitido por tipo de documento.
  IF v_tipo IN ('recepcion','devolucion_produccion','traspaso_entrada','ajuste_positivo','apertura')
     AND NEW.signo <> 1 THEN
    RAISE EXCEPTION 'SIGNO_NO_PERMITIDO: el tipo % solo admite entradas', v_tipo;
  END IF;
  IF v_tipo IN ('salida_produccion','baja','traspaso_salida','ajuste_negativo')
     AND NEW.signo <> -1 THEN
    RAISE EXCEPTION 'SIGNO_NO_PERMITIDO: el tipo % solo admite salidas', v_tipo;
  END IF;

  -- 3. Día y mes cerrados.
  IF EXISTS (SELECT 1 FROM cierre_diario c
               JOIN almacen a ON a.empresa_id = c.empresa_id AND a.operacion_id = c.operacion_id
              WHERE a.empresa_id = NEW.empresa_id AND a.id = NEW.almacen_id
                AND c.fecha = NEW.fecha AND c.estado = 'cerrado') THEN
    RAISE EXCEPTION 'DIA_CERRADO: no se contabiliza en un dia cerrado';
  END IF;
  IF EXISTS (SELECT 1 FROM periodo_mensual p
               JOIN almacen a ON a.empresa_id = p.empresa_id AND a.operacion_id = p.operacion_id
              WHERE a.empresa_id = NEW.empresa_id AND a.id = NEW.almacen_id
                AND p.anio = EXTRACT(YEAR FROM NEW.fecha) AND p.mes = EXTRACT(MONTH FROM NEW.fecha)
                AND p.estado = 'cerrado') THEN
    RAISE EXCEPTION 'PERIODO_CERRADO: no se contabiliza en un mes cerrado';
  END IF;

  -- 4. Bloqueo de la fila de saldo (serializa salidas simultáneas) y verificación.
  INSERT INTO saldo_stock(empresa_id, almacen_id, variante_id, cantidad_base_u6, valor_u6, version)
  VALUES (NEW.empresa_id, NEW.almacen_id, NEW.variante_id, 0, 0, 0)
  ON CONFLICT (empresa_id, almacen_id, variante_id) DO NOTHING;

  SELECT cantidad_base_u6, valor_u6 INTO v_cant, v_valor
    FROM saldo_stock
   WHERE empresa_id = NEW.empresa_id AND almacen_id = NEW.almacen_id AND variante_id = NEW.variante_id
     FOR UPDATE;

  IF NEW.signo = -1 AND v_cant < NEW.cantidad_base_u6 THEN
    RAISE EXCEPTION 'STOCK_INSUFICIENTE: disponible %, solicitado %', v_cant, NEW.cantidad_base_u6;
  END IF;
  IF NEW.signo = -1 AND v_valor < NEW.valor_u6 THEN
    RAISE EXCEPTION 'VALOR_STOCK_INSUFICIENTE: valor disponible %, solicitado %', v_valor, NEW.valor_u6;
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER validar_movimiento BEFORE INSERT ON movimiento_stock
  FOR EACH ROW EXECUTE FUNCTION fn_validar_movimiento();

-- Único propietario del saldo: la aplicación NO debe actualizarlo por su cuenta.
CREATE FUNCTION fn_actualizar_saldo() RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path = public, pg_temp AS $$
BEGIN
  UPDATE saldo_stock
     SET cantidad_base_u6 = cantidad_base_u6 + NEW.signo * NEW.cantidad_base_u6,
         valor_u6         = valor_u6         + NEW.signo * NEW.valor_u6,
         version          = version + 1
   WHERE empresa_id = NEW.empresa_id AND almacen_id = NEW.almacen_id AND variante_id = NEW.variante_id;
  RETURN NULL;
END $$;

CREATE TRIGGER actualizar_saldo AFTER INSERT ON movimiento_stock
  FOR EACH ROW EXECUTE FUNCTION fn_actualizar_saldo();
