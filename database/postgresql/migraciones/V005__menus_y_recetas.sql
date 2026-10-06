-- V005: reglas del módulo 1 (menús y recetas).
-- - Una versión de receta nace en borrador; aprobada es inmutable (solo puede retirarse).
-- - Ingredientes y variantes permitidas solo cambian mientras la versión está en borrador.
-- - Una minuta nace en borrador; al aprobarla quedan fijos sus platos, el costeo y la estructura fija.
-- - Un plato solo usa versiones aprobadas y estructuras del mismo servicio de la minuta.
-- - El costeo de un ingrediente debe pertenecer a la versión del plato.

-- ---------- Recetas ----------
CREATE FUNCTION fn_proteger_receta_version() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
  v_ingredientes bigint;
BEGIN
  IF TG_OP = 'INSERT' THEN
    IF NEW.estado <> 'borrador' THEN
      RAISE EXCEPTION 'RECETA_NACE_BORRADOR: toda version de receta se crea en borrador';
    END IF;
    RETURN NEW;
  END IF;

  IF TG_OP = 'DELETE' THEN
    IF OLD.estado <> 'borrador' THEN
      RAISE EXCEPTION 'RECETA_APROBADA: la version % esta % y no se elimina', OLD.version, OLD.estado;
    END IF;
    RETURN OLD;
  END IF;

  IF OLD.estado = 'retirada' THEN
    RAISE EXCEPTION 'RECETA_APROBADA: la version % esta retirada y no se modifica', OLD.version;
  END IF;

  IF OLD.estado = 'aprobada' THEN
    IF NEW.estado <> 'retirada'
       OR (NEW.receta_id, NEW.version, NEW.rendimiento_raciones_u6, NEW.instrucciones)
          IS DISTINCT FROM (OLD.receta_id, OLD.version, OLD.rendimiento_raciones_u6, OLD.instrucciones) THEN
      RAISE EXCEPTION 'RECETA_APROBADA: la version % esta aprobada; cree una nueva version', OLD.version;
    END IF;
    RETURN NEW;
  END IF;

  -- Desde borrador.
  IF NEW.estado = 'retirada' THEN
    RAISE EXCEPTION 'TRANSICION_INVALIDA: un borrador no se retira; eliminelo';
  END IF;
  IF NEW.estado = 'aprobada' THEN
    SELECT count(*) INTO v_ingredientes FROM receta_ingrediente
     WHERE empresa_id = NEW.empresa_id AND receta_version_id = NEW.id;
    IF v_ingredientes = 0 THEN
      RAISE EXCEPTION 'RECETA_SIN_INGREDIENTES: no se aprueba una receta sin ingredientes';
    END IF;
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_receta_version BEFORE INSERT OR UPDATE OR DELETE ON receta_version
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_receta_version();

CREATE FUNCTION fn_estado_version_de_ingrediente(p_empresa bigint, p_ingrediente bigint) RETURNS text
LANGUAGE sql STABLE AS $$
  SELECT rv.estado FROM receta_ingrediente i
    JOIN receta_version rv ON rv.empresa_id = i.empresa_id AND rv.id = i.receta_version_id
   WHERE i.empresa_id = p_empresa AND i.id = p_ingrediente
$$;

CREATE FUNCTION fn_proteger_ingrediente() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
  v_estado text;
BEGIN
  IF TG_TABLE_NAME = 'receta_ingrediente' THEN
    IF TG_OP <> 'INSERT' THEN
      SELECT estado INTO v_estado FROM receta_version WHERE empresa_id = OLD.empresa_id AND id = OLD.receta_version_id;
      IF v_estado <> 'borrador' THEN
        RAISE EXCEPTION 'RECETA_APROBADA: los ingredientes de una version % no se modifican', v_estado;
      END IF;
    END IF;
    IF TG_OP <> 'DELETE' THEN
      SELECT estado INTO v_estado FROM receta_version WHERE empresa_id = NEW.empresa_id AND id = NEW.receta_version_id;
      IF v_estado <> 'borrador' THEN
        RAISE EXCEPTION 'RECETA_APROBADA: los ingredientes de una version % no se modifican', v_estado;
      END IF;
    END IF;
  ELSE -- ingrediente_variante_permitida
    IF TG_OP <> 'INSERT' AND fn_estado_version_de_ingrediente(OLD.empresa_id, OLD.ingrediente_id) <> 'borrador' THEN
      RAISE EXCEPTION 'RECETA_APROBADA: las variantes permitidas de una version aprobada no se modifican';
    END IF;
    IF TG_OP <> 'DELETE' AND fn_estado_version_de_ingrediente(NEW.empresa_id, NEW.ingrediente_id) <> 'borrador' THEN
      RAISE EXCEPTION 'RECETA_APROBADA: las variantes permitidas de una version aprobada no se modifican';
    END IF;
  END IF;
  RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
END $$;

CREATE TRIGGER proteger_ingrediente BEFORE INSERT OR UPDATE OR DELETE ON receta_ingrediente
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_ingrediente();
CREATE TRIGGER proteger_variante_permitida BEFORE INSERT OR UPDATE OR DELETE ON ingrediente_variante_permitida
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_ingrediente();

-- ---------- Minutas ----------
CREATE FUNCTION fn_proteger_minuta() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
  v_platos bigint;
  v_no_aprobadas bigint;
BEGIN
  IF TG_OP = 'INSERT' THEN
    IF NEW.estado <> 'borrador' THEN
      RAISE EXCEPTION 'MINUTA_NACE_BORRADOR: toda minuta se crea en borrador';
    END IF;
    RETURN NEW;
  END IF;

  IF TG_OP = 'DELETE' THEN
    IF OLD.estado <> 'borrador' THEN
      RAISE EXCEPTION 'MINUTA_APROBADA: la minuta del % esta % y no se elimina', OLD.fecha, OLD.estado;
    END IF;
    RETURN OLD;
  END IF;

  IF OLD.estado <> 'borrador' THEN
    IF NOT (OLD.estado = 'aprobada' AND NEW.estado = 'cerrada')
       OR (NEW.operacion_servicio_id, NEW.fecha, NEW.tipo, NEW.minuta_origen_id, NEW.comensales, NEW.usuario_id, NEW.moneda_costeo)
          IS DISTINCT FROM (OLD.operacion_servicio_id, OLD.fecha, OLD.tipo, OLD.minuta_origen_id, OLD.comensales, OLD.usuario_id, OLD.moneda_costeo) THEN
      RAISE EXCEPTION 'MINUTA_APROBADA: la minuta del % esta % y no se modifica', OLD.fecha, OLD.estado;
    END IF;
    RETURN NEW;
  END IF;

  IF NEW.estado = 'cerrada' THEN
    RAISE EXCEPTION 'TRANSICION_INVALIDA: una minuta en borrador se aprueba antes de cerrarse';
  END IF;
  IF NEW.estado = 'aprobada' THEN
    SELECT count(*), count(*) FILTER (WHERE rv.estado <> 'aprobada')
      INTO v_platos, v_no_aprobadas
      FROM minuta_detalle d
      JOIN receta_version rv ON rv.empresa_id = d.empresa_id AND rv.id = d.receta_version_id
     WHERE d.empresa_id = NEW.empresa_id AND d.minuta_id = NEW.id;
    IF v_platos = 0 THEN
      RAISE EXCEPTION 'MINUTA_VACIA: no se aprueba una minuta sin platos';
    END IF;
    IF v_no_aprobadas > 0 THEN
      RAISE EXCEPTION 'RECETA_NO_APROBADA: la minuta usa versiones de receta que ya no estan aprobadas';
    END IF;
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_minuta BEFORE INSERT OR UPDATE OR DELETE ON minuta
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_minuta();

-- Detalle, estructura fija y costeo: solo con la minuta en borrador.
CREATE FUNCTION fn_estado_minuta(p_empresa bigint, p_minuta bigint) RETURNS text
LANGUAGE sql STABLE AS $$
  SELECT estado FROM minuta WHERE empresa_id = p_empresa AND id = p_minuta
$$;

CREATE FUNCTION fn_proteger_detalle_minuta() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
  v_minuta_old bigint;
  v_minuta_new bigint;
  v_servicio_minuta bigint;
  v_servicio_estructura bigint;
  v_estado_receta text;
BEGIN
  IF TG_TABLE_NAME = 'costeo_ingrediente' THEN
    IF TG_OP <> 'INSERT' THEN
      SELECT minuta_id INTO v_minuta_old FROM minuta_detalle WHERE empresa_id = OLD.empresa_id AND id = OLD.minuta_detalle_id;
    END IF;
    IF TG_OP <> 'DELETE' THEN
      SELECT minuta_id INTO v_minuta_new FROM minuta_detalle WHERE empresa_id = NEW.empresa_id AND id = NEW.minuta_detalle_id;
    END IF;
  ELSE
    IF TG_OP <> 'INSERT' THEN v_minuta_old := OLD.minuta_id; END IF;
    IF TG_OP <> 'DELETE' THEN v_minuta_new := NEW.minuta_id; END IF;
  END IF;

  IF v_minuta_old IS NOT NULL AND fn_estado_minuta(OLD.empresa_id, v_minuta_old) <> 'borrador' THEN
    RAISE EXCEPTION 'MINUTA_APROBADA: la minuta ya esta aprobada y su contenido no se modifica';
  END IF;
  IF v_minuta_new IS NOT NULL AND fn_estado_minuta(NEW.empresa_id, v_minuta_new) <> 'borrador' THEN
    RAISE EXCEPTION 'MINUTA_APROBADA: la minuta ya esta aprobada y su contenido no se modifica';
  END IF;
  IF TG_OP = 'DELETE' THEN
    RETURN OLD;
  END IF;

  IF TG_TABLE_NAME = 'minuta_detalle' THEN
    SELECT estado INTO v_estado_receta FROM receta_version WHERE empresa_id = NEW.empresa_id AND id = NEW.receta_version_id;
    IF v_estado_receta <> 'aprobada' THEN
      RAISE EXCEPTION 'RECETA_NO_APROBADA: solo se planifican versiones de receta aprobadas (esta: %)', v_estado_receta;
    END IF;
    SELECT os.servicio_id INTO v_servicio_minuta FROM minuta m
      JOIN operacion_servicio os ON os.empresa_id = m.empresa_id AND os.id = m.operacion_servicio_id
     WHERE m.empresa_id = NEW.empresa_id AND m.id = NEW.minuta_id;
    SELECT servicio_id INTO v_servicio_estructura FROM estructura_servicio WHERE empresa_id = NEW.empresa_id AND id = NEW.estructura_id;
    IF v_servicio_minuta IS DISTINCT FROM v_servicio_estructura THEN
      RAISE EXCEPTION 'ESTRUCTURA_DE_OTRO_SERVICIO: la estructura no pertenece al servicio de la minuta';
    END IF;
  ELSIF TG_TABLE_NAME = 'costeo_ingrediente' THEN
    IF NOT EXISTS (SELECT 1 FROM minuta_detalle d
                     JOIN receta_ingrediente i ON i.empresa_id = d.empresa_id AND i.receta_version_id = d.receta_version_id
                    WHERE d.empresa_id = NEW.empresa_id AND d.id = NEW.minuta_detalle_id AND i.id = NEW.ingrediente_id) THEN
      RAISE EXCEPTION 'COSTEO_INCOHERENTE: el ingrediente no pertenece a la receta del plato';
    END IF;
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_detalle_minuta BEFORE INSERT OR UPDATE OR DELETE ON minuta_detalle
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_detalle_minuta();
CREATE TRIGGER proteger_estructura_fija BEFORE INSERT OR UPDATE OR DELETE ON minuta_estructura_fija
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_detalle_minuta();
CREATE TRIGGER proteger_costeo BEFORE INSERT OR UPDATE OR DELETE ON costeo_ingrediente
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_detalle_minuta();

-- Un mismo ingrediente no se repite en la versión; un plato por estructura en la minuta.
ALTER TABLE receta_ingrediente ADD CONSTRAINT ingrediente_unico UNIQUE (empresa_id, receta_version_id, producto_base_id);
ALTER TABLE minuta_detalle ADD CONSTRAINT plato_unico UNIQUE (empresa_id, minuta_id, estructura_id, receta_version_id);

-- Moneda del snapshot de costo (el precio se toma en la moneda indicada al aprobar).
ALTER TABLE minuta ADD COLUMN moneda_costeo TEXT;

-- Auditoría de los datos maestros y documentos del módulo.
DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY['servicio','regimen','estructura_servicio','operacion_servicio','receta','receta_version',
                           'receta_ingrediente','ingrediente_variante_permitida','minuta','minuta_detalle',
                           'minuta_estructura_fija'] LOOP
    EXECUTE format('CREATE TRIGGER auditar AFTER INSERT OR UPDATE OR DELETE ON %I
                    FOR EACH ROW EXECUTE FUNCTION fn_auditar()', t);
  END LOOP;
END $$;
