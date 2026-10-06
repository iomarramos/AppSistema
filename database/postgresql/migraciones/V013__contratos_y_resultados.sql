-- V013: extensiones (etapa 9) — clientes, contratos mensuales con vigencia y ajustes, ingreso desde contrato,
-- gastos por categoría y resultado mensual trazable.
--
-- Reglas:
-- * Una línea de contrato (servicio + importe mensual + vigencia) no se superpone con otra del mismo servicio.
-- * El importe de una línea no se edita: un ajuste cierra la línea vigente y abre otra desde la fecha del ajuste.
--   Así la historia de lo facturado queda intacta y cada mes se explica con las líneas vigentes.
-- * Ninguna línea puede cambiar lo que ya pertenece a un mes cerrado.
-- * El servicio de la línea pertenece a la operación del contrato; la línea cae dentro de la vigencia del contrato.
-- * El ingreso generado desde contrato se distingue del ingresado a mano (origen).

-- ---------- Contratos ----------
ALTER TABLE contrato_servicio ADD CONSTRAINT contrato_servicio_sin_superposicion
  EXCLUDE USING gist (empresa_id WITH =, operacion_servicio_id WITH =,
                      daterange(fecha_desde, fecha_hasta, '[]') WITH &&);

-- ¿Algún mes cerrado de la operación toca el rango [desde, hasta]? (hasta NULL = sin fin)
CREATE FUNCTION fn_mes_cerrado_en_rango(p_empresa bigint, p_operacion bigint, p_desde date, p_hasta date) RETURNS boolean
LANGUAGE sql STABLE AS $$
  SELECT EXISTS (
    SELECT 1 FROM periodo_mensual pm
     WHERE pm.empresa_id = p_empresa AND pm.operacion_id = p_operacion AND pm.estado = 'cerrado'
       AND daterange(make_date(pm.anio::int, pm.mes::int, 1), (make_date(pm.anio::int, pm.mes::int, 1) + interval '1 month')::date, '[)')
           && daterange(p_desde, p_hasta, '[]'))
$$;

CREATE FUNCTION fn_proteger_contrato_servicio() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE c record; v_op_servicio bigint;
BEGIN
  IF TG_OP = 'DELETE' THEN
    SELECT operacion_id INTO c FROM contrato WHERE empresa_id = OLD.empresa_id AND id = OLD.contrato_id;
    IF fn_mes_cerrado_en_rango(OLD.empresa_id, c.operacion_id, OLD.fecha_desde, OLD.fecha_hasta) THEN
      RAISE EXCEPTION 'PERIODO_CERRADO: la linea ya explica meses cerrados; registre un ajuste en lugar de borrarla';
    END IF;
    RETURN OLD;
  END IF;

  SELECT operacion_id, fecha_desde, fecha_hasta INTO c FROM contrato WHERE empresa_id = NEW.empresa_id AND id = NEW.contrato_id;
  SELECT operacion_id INTO v_op_servicio FROM operacion_servicio WHERE empresa_id = NEW.empresa_id AND id = NEW.operacion_servicio_id;
  IF v_op_servicio IS DISTINCT FROM c.operacion_id THEN
    RAISE EXCEPTION 'OPERACION_AJENA: el servicio no pertenece a la operacion del contrato';
  END IF;
  IF NEW.fecha_desde < c.fecha_desde OR (c.fecha_hasta IS NOT NULL AND (NEW.fecha_hasta IS NULL OR NEW.fecha_hasta > c.fecha_hasta)) THEN
    RAISE EXCEPTION 'FUERA_DE_VIGENCIA: la linea debe caer dentro de la vigencia del contrato';
  END IF;

  IF TG_OP = 'INSERT' THEN
    IF fn_mes_cerrado_en_rango(NEW.empresa_id, c.operacion_id, NEW.fecha_desde, NEW.fecha_hasta) THEN
      RAISE EXCEPTION 'PERIODO_CERRADO: la linea empezaria en un mes cerrado';
    END IF;
    RETURN NEW;
  END IF;

  -- UPDATE: solo se puede cerrar o acortar la vigencia (fecha_hasta), sin tocar meses cerrados.
  IF NEW.contrato_id <> OLD.contrato_id OR NEW.operacion_servicio_id <> OLD.operacion_servicio_id
     OR NEW.importe_mensual_u6 <> OLD.importe_mensual_u6 OR NEW.fecha_desde <> OLD.fecha_desde THEN
    RAISE EXCEPTION 'CONTRATO_INMUTABLE: el importe y el inicio no se editan; registre un ajuste desde la fecha que corresponda';
  END IF;
  IF NEW.fecha_hasta IS DISTINCT FROM OLD.fecha_hasta THEN
    IF NEW.fecha_hasta IS NULL OR (OLD.fecha_hasta IS NOT NULL AND NEW.fecha_hasta > OLD.fecha_hasta) THEN
      RAISE EXCEPTION 'CONTRATO_INMUTABLE: una linea no se extiende; registre una linea nueva';
    END IF;
    IF fn_mes_cerrado_en_rango(NEW.empresa_id, c.operacion_id, NEW.fecha_hasta + 1, OLD.fecha_hasta) THEN
      RAISE EXCEPTION 'PERIODO_CERRADO: el cambio alteraria un mes cerrado';
    END IF;
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER proteger_contrato_servicio BEFORE INSERT OR UPDATE OR DELETE ON contrato_servicio
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_contrato_servicio();

-- El contrato no puede quedar más corto que sus líneas ni cambiar de cliente u operación.
CREATE FUNCTION fn_proteger_contrato() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP = 'DELETE' THEN
    IF EXISTS (SELECT 1 FROM contrato_servicio WHERE empresa_id = OLD.empresa_id AND contrato_id = OLD.id) THEN
      RAISE EXCEPTION 'CONTRATO_CON_LINEAS: el contrato tiene servicios; cierre su vigencia en lugar de borrarlo';
    END IF;
    RETURN OLD;
  END IF;
  IF NEW.cliente_id <> OLD.cliente_id OR NEW.operacion_id <> OLD.operacion_id OR NEW.moneda <> OLD.moneda THEN
    RAISE EXCEPTION 'CONTRATO_INMUTABLE: cliente, operacion y moneda no cambian';
  END IF;
  IF EXISTS (SELECT 1 FROM contrato_servicio s WHERE s.empresa_id = NEW.empresa_id AND s.contrato_id = NEW.id
              AND (s.fecha_desde < NEW.fecha_desde
                   OR (NEW.fecha_hasta IS NOT NULL AND (s.fecha_hasta IS NULL OR s.fecha_hasta > NEW.fecha_hasta)))) THEN
    RAISE EXCEPTION 'FUERA_DE_VIGENCIA: hay servicios del contrato fuera de la nueva vigencia; cierrelos primero';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER proteger_contrato BEFORE UPDATE OR DELETE ON contrato
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_contrato();

-- ---------- Ingresos y gastos ----------
ALTER TABLE ingreso_servicio ADD COLUMN origen TEXT NOT NULL DEFAULT 'manual' CHECK (origen IN ('manual', 'contrato'));

ALTER TABLE gasto ADD CONSTRAINT gasto_categoria_valida CHECK (categoria IN ('personal', 'operacion', 'administracion', 'otros'));

-- El servicio del gasto pertenece a la operación del período.
CREATE FUNCTION fn_validar_gasto() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF NEW.operacion_servicio_id IS NOT NULL AND NOT EXISTS (
       SELECT 1 FROM operacion_servicio os JOIN periodo_mensual pm ON pm.empresa_id = os.empresa_id AND pm.operacion_id = os.operacion_id
        WHERE os.empresa_id = NEW.empresa_id AND os.id = NEW.operacion_servicio_id AND pm.id = NEW.periodo_id) THEN
    RAISE EXCEPTION 'OPERACION_AJENA: el servicio del gasto no pertenece a la operacion del periodo';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER validar_gasto BEFORE INSERT OR UPDATE ON gasto FOR EACH ROW EXECUTE FUNCTION fn_validar_gasto();

-- ---------- Auditoría ----------
DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY['cliente','contrato','contrato_servicio'] LOOP
    EXECUTE format('CREATE TRIGGER auditar AFTER INSERT OR UPDATE OR DELETE ON %I
                    FOR EACH ROW EXECUTE FUNCTION fn_auditar()', t);
  END LOOP;
END $$;
