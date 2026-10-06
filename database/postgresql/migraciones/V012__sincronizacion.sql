-- V012: continuidad y sincronización (etapa 8, D10 propuesta: autoridad local por sede).
--
-- Cada sede opera con su propio PostgreSQL en la red local: las ≈20 PC trabajan aunque no haya internet.
-- Lo confirmado en la sede se anota en la cola de salida (sincronizacion_evento) EN LA MISMA TRANSACCIÓN
-- que el documento o el cierre: un corte de energía no deja ni el documento sin evento ni el evento sin documento.
-- Un agente envía la cola a la base central cuando hay conexión. La central:
--   * autentica a la sede por su credencial (solo guarda el hash) y registra los rechazos sin guardar el contenido (T44);
--   * ignora los reenvíos idénticos (misma clave = mismo contenido) y marca conflicto si el contenido difiere (T22/T42);
--   * aplica en el orden de la sede (secuencia sin huecos) y retiene lo que llega antes que su antecesor (T43);
--   * guarda cada dato bajo la sede que lo envió: ninguna sede escribe sobre otra.
-- La misma migración se aplica en sedes y central; cada base usa la parte que le toca.

-- ---------- Identificador global de documentos ----------
ALTER TABLE documento_stock ADD COLUMN uuid uuid NOT NULL DEFAULT gen_random_uuid();
ALTER TABLE documento_stock ADD CONSTRAINT documento_stock_uuid_unico UNIQUE (uuid);

-- ---------- Sede: origen y cola de salida ----------
CREATE TABLE origen_sincronizacion (
  empresa_id BIGINT PRIMARY KEY REFERENCES empresa(id),
  codigo TEXT NOT NULL CHECK (codigo ~ '^[A-Za-z0-9_-]{1,30}$'),
  ultima_secuencia BIGINT NOT NULL DEFAULT 0 CHECK (ultima_secuencia >= 0),
  configurado_en TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE sincronizacion_evento
  ADD COLUMN secuencia BIGINT,
  ADD COLUMN version_payload INTEGER NOT NULL DEFAULT 1,
  ADD COLUMN referencia TEXT,
  ADD COLUMN depende_de TEXT,
  ADD COLUMN proximo_intento TIMESTAMPTZ NOT NULL DEFAULT now(),
  ADD COLUMN enviado_en TIMESTAMPTZ;
ALTER TABLE sincronizacion_evento ADD CONSTRAINT sincronizacion_secuencia_unica UNIQUE (empresa_id, secuencia);
ALTER TABLE sincronizacion_evento ADD CONSTRAINT sincronizacion_referencia_unica UNIQUE (empresa_id, tipo, referencia);
CREATE INDEX ix_sincronizacion_evento_pendiente ON sincronizacion_evento(empresa_id, secuencia) WHERE estado <> 'enviado';

-- El contenido de un evento ya anotado no cambia; solo su estado de envío.
CREATE FUNCTION fn_proteger_evento() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP = 'DELETE' THEN
    RAISE EXCEPTION 'EVENTO_INMUTABLE: los eventos de sincronizacion no se eliminan';
  END IF;
  IF NEW.uuid IS DISTINCT FROM OLD.uuid OR NEW.payload_json IS DISTINCT FROM OLD.payload_json
     OR NEW.secuencia IS DISTINCT FROM OLD.secuencia OR NEW.tipo IS DISTINCT FROM OLD.tipo
     OR NEW.referencia IS DISTINCT FROM OLD.referencia OR NEW.version_payload IS DISTINCT FROM OLD.version_payload THEN
    RAISE EXCEPTION 'EVENTO_INMUTABLE: el contenido de un evento no cambia';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER proteger_evento BEFORE UPDATE OR DELETE ON sincronizacion_evento
  FOR EACH ROW EXECUTE FUNCTION fn_proteger_evento();

-- La aplicación solo puede leer la cola; la escriben los triggers y el agente (propietario).
REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON sincronizacion_evento FROM app_stock;
REVOKE ALL ON origen_sincronizacion FROM app_stock;

-- Anota un evento con la siguiente secuencia de la sede. Sin origen configurado (sede aislada) no hace nada.
-- El bloqueo de la fila de origen dura hasta el commit: las secuencias quedan en orden de confirmación y sin huecos.
CREATE FUNCTION fn_anotar_evento(p_empresa bigint, p_operacion bigint, p_tipo text, p_referencia text,
                                 p_depende_de text, p_payload jsonb) RETURNS void
LANGUAGE plpgsql SECURITY DEFINER SET search_path = public AS $$
DECLARE v_seq bigint;
BEGIN
  IF NOT EXISTS (SELECT 1 FROM origen_sincronizacion WHERE empresa_id = p_empresa) THEN RETURN; END IF;
  IF EXISTS (SELECT 1 FROM sincronizacion_evento WHERE empresa_id = p_empresa AND tipo = p_tipo AND referencia = p_referencia) THEN RETURN; END IF;
  UPDATE origen_sincronizacion SET ultima_secuencia = ultima_secuencia + 1 WHERE empresa_id = p_empresa RETURNING ultima_secuencia INTO v_seq;
  INSERT INTO sincronizacion_evento(empresa_id, uuid, operacion_id, tipo, payload_json, version_origen, secuencia, version_payload, referencia, depende_de)
  VALUES (p_empresa, gen_random_uuid()::text, p_operacion, p_tipo, p_payload::text, v_seq, v_seq, 1, p_referencia, p_depende_de);
END $$;
REVOKE ALL ON FUNCTION fn_anotar_evento(bigint, bigint, text, text, text, jsonb) FROM PUBLIC;

-- Documento confirmado → evento con cabecera y movimientos (códigos, no IDs locales).
CREATE FUNCTION fn_payload_documento(p_empresa bigint, p_documento bigint) RETURNS jsonb
LANGUAGE sql STABLE SECURITY DEFINER SET search_path = public AS $$
  SELECT jsonb_build_object(
    'documento', d.uuid, 'numero', d.numero, 'tipo', d.tipo, 'fecha', d.fecha, 'motivo', d.motivo,
    'operacion', o.codigo, 'almacen', a.codigo, 'almacen_destino', ad.codigo, 'documento_origen', dor.uuid,
    'lineas', COALESCE((
      SELECT jsonb_agg(jsonb_build_object('variante', v.codigo, 'producto', p.codigo, 'almacen', am.codigo, 'signo', m.signo,
                                          'cantidad_u6', m.cantidad_base_u6, 'valor_u6', m.valor_u6) ORDER BY det.id)
        FROM documento_stock_detalle det
        JOIN movimiento_stock m ON m.empresa_id = det.empresa_id AND m.documento_detalle_id = det.id
        JOIN almacen am ON am.empresa_id = m.empresa_id AND am.id = m.almacen_id
        JOIN variante_producto v ON v.empresa_id = det.empresa_id AND v.id = det.variante_id
        JOIN producto_base p ON p.empresa_id = v.empresa_id AND p.id = v.producto_base_id
       WHERE det.empresa_id = d.empresa_id AND det.documento_id = d.id), '[]'::jsonb))
  FROM documento_stock d
  JOIN almacen a ON a.empresa_id = d.empresa_id AND a.id = d.almacen_id
  JOIN operacion o ON o.empresa_id = a.empresa_id AND o.id = a.operacion_id
  LEFT JOIN almacen ad ON ad.empresa_id = d.empresa_id AND ad.id = d.almacen_destino_id
  LEFT JOIN documento_stock dor ON dor.empresa_id = d.empresa_id AND dor.id = d.documento_origen_id
  WHERE d.empresa_id = p_empresa AND d.id = p_documento
$$;
REVOKE ALL ON FUNCTION fn_payload_documento(bigint, bigint) FROM PUBLIC;

CREATE FUNCTION fn_encolar_documento(p_empresa bigint, p_documento bigint) RETURNS void
LANGUAGE plpgsql SECURITY DEFINER SET search_path = public AS $$
DECLARE d record;
BEGIN
  SELECT doc.uuid, doc.estado, a.operacion_id, dor.uuid AS origen_uuid INTO d
    FROM documento_stock doc JOIN almacen a ON a.empresa_id = doc.empresa_id AND a.id = doc.almacen_id
    LEFT JOIN documento_stock dor ON dor.empresa_id = doc.empresa_id AND dor.id = doc.documento_origen_id
   WHERE doc.empresa_id = p_empresa AND doc.id = p_documento;
  IF d.estado IS DISTINCT FROM 'confirmado' THEN RETURN; END IF;
  PERFORM fn_anotar_evento(p_empresa, d.operacion_id, 'documento_stock', d.uuid::text, d.origen_uuid::text,
                           fn_payload_documento(p_empresa, p_documento));
END $$;
REVOKE ALL ON FUNCTION fn_encolar_documento(bigint, bigint) FROM PUBLIC;

-- Diferido al commit: para entonces el documento ya tiene todas sus líneas y movimientos.
CREATE FUNCTION fn_trg_encolar_documento() RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path = public AS $$
BEGIN
  PERFORM fn_encolar_documento(NEW.empresa_id, NEW.id);
  RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER encolar_documento AFTER INSERT OR UPDATE ON documento_stock
  DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_trg_encolar_documento();

-- Cierres: día y mes cerrados viajan a la central.
CREATE FUNCTION fn_encolar_cierre(p_tabla text, p_empresa bigint, p_id bigint) RETURNS void
LANGUAGE plpgsql SECURITY DEFINER SET search_path = public AS $$
DECLARE r record;
BEGIN
  IF p_tabla = 'cierre_diario' THEN
    SELECT c.estado, c.operacion_id, o.codigo, to_char(c.fecha, 'YYYY-MM-DD') AS periodo, c.fecha_cierre INTO r
      FROM cierre_diario c JOIN operacion o ON o.empresa_id = c.empresa_id AND o.id = c.operacion_id
     WHERE c.empresa_id = p_empresa AND c.id = p_id;
  ELSE
    SELECT c.estado, c.operacion_id, o.codigo, c.anio || '-' || lpad(c.mes::text, 2, '0') AS periodo, c.fecha_cierre INTO r
      FROM periodo_mensual c JOIN operacion o ON o.empresa_id = c.empresa_id AND o.id = c.operacion_id
     WHERE c.empresa_id = p_empresa AND c.id = p_id;
  END IF;
  IF r.estado IS DISTINCT FROM 'cerrado' THEN RETURN; END IF;
  PERFORM fn_anotar_evento(p_empresa, r.operacion_id, p_tabla, r.codigo || '/' || r.periodo, NULL,
                           jsonb_build_object('operacion', r.codigo, 'periodo', r.periodo, 'cerrado_en', r.fecha_cierre));
END $$;
REVOKE ALL ON FUNCTION fn_encolar_cierre(text, bigint, bigint) FROM PUBLIC;

CREATE FUNCTION fn_trg_encolar_cierre() RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path = public AS $$
BEGIN
  PERFORM fn_encolar_cierre(TG_TABLE_NAME, NEW.empresa_id, NEW.id);
  RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER encolar_cierre AFTER INSERT OR UPDATE ON cierre_diario
  DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_trg_encolar_cierre();
CREATE CONSTRAINT TRIGGER encolar_cierre AFTER INSERT OR UPDATE ON periodo_mensual
  DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION fn_trg_encolar_cierre();

-- Configura el origen de la sede y anota la historia ya confirmada, en orden (migración con documentos históricos, T46).
CREATE FUNCTION fn_configurar_origen(p_empresa bigint, p_codigo text) RETURNS bigint
LANGUAGE plpgsql SECURITY DEFINER SET search_path = public AS $$
DECLARE r record; v_antes bigint;
BEGIN
  INSERT INTO origen_sincronizacion(empresa_id, codigo) VALUES (p_empresa, p_codigo)
  ON CONFLICT (empresa_id) DO NOTHING;
  IF (SELECT codigo FROM origen_sincronizacion WHERE empresa_id = p_empresa) <> p_codigo THEN
    RAISE EXCEPTION 'ORIGEN_CONFIGURADO: esta base ya sincroniza como otra sede';
  END IF;
  SELECT ultima_secuencia INTO v_antes FROM origen_sincronizacion WHERE empresa_id = p_empresa FOR UPDATE;
  FOR r IN SELECT 'documento_stock' AS t, d.id, d.fecha, d.creado_en FROM documento_stock d WHERE d.empresa_id = p_empresa AND d.estado = 'confirmado'
           UNION ALL SELECT 'cierre_diario', c.id, c.fecha, c.fecha_cierre FROM cierre_diario c WHERE c.empresa_id = p_empresa AND c.estado = 'cerrado'
           UNION ALL SELECT 'periodo_mensual', c.id, make_date(c.anio::int, c.mes::int, 1), c.fecha_cierre FROM periodo_mensual c WHERE c.empresa_id = p_empresa AND c.estado = 'cerrado'
           ORDER BY 4, 1, 2 LOOP
    IF r.t = 'documento_stock' THEN PERFORM fn_encolar_documento(p_empresa, r.id);
    ELSE PERFORM fn_encolar_cierre(r.t, p_empresa, r.id);
    END IF;
  END LOOP;
  RETURN (SELECT ultima_secuencia FROM origen_sincronizacion WHERE empresa_id = p_empresa) - v_antes;
END $$;
REVOKE ALL ON FUNCTION fn_configurar_origen(bigint, text) FROM PUBLIC;

-- ---------- Central: sedes, bandeja de entrada y datos consolidados ----------
CREATE TABLE sede_central (
  id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  empresa_id BIGINT NOT NULL REFERENCES empresa(id),
  codigo TEXT NOT NULL CHECK (codigo ~ '^[A-Za-z0-9_-]{1,30}$'),
  nombre TEXT NOT NULL,
  credencial_sha256 TEXT NOT NULL,
  activa BOOLEAN NOT NULL DEFAULT true,
  ultima_secuencia_aplicada BIGINT NOT NULL DEFAULT 0,
  ultima_sincronizacion TIMESTAMPTZ,
  creado_en TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (empresa_id, codigo)
);

CREATE TABLE evento_recibido (
  id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  empresa_id BIGINT NOT NULL REFERENCES empresa(id),
  sede_id BIGINT NOT NULL REFERENCES sede_central(id),
  uuid TEXT NOT NULL,
  secuencia BIGINT NOT NULL CHECK (secuencia > 0),
  tipo TEXT NOT NULL,
  version_payload INTEGER NOT NULL,
  referencia TEXT,
  depende_de TEXT,
  payload jsonb NOT NULL,
  contenido_sha256 TEXT NOT NULL,
  estado TEXT NOT NULL CHECK (estado IN ('retenido','aplicado','conflicto')),
  motivo TEXT,
  recibido_en TIMESTAMPTZ NOT NULL DEFAULT now(),
  aplicado_en TIMESTAMPTZ,
  UNIQUE (empresa_id, uuid),
  UNIQUE (sede_id, secuencia)
);

-- Rechazos: solo datos de identificación, nunca el contenido enviado.
CREATE TABLE rechazo_sincronizacion (
  id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  empresa_codigo TEXT,
  sede_codigo TEXT,
  uuid TEXT,
  motivo TEXT NOT NULL,
  recibido_en TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE central_documento (
  id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  empresa_id BIGINT NOT NULL REFERENCES empresa(id),
  sede_id BIGINT NOT NULL REFERENCES sede_central(id),
  evento_id BIGINT NOT NULL REFERENCES evento_recibido(id),
  uuid uuid NOT NULL,
  numero TEXT NOT NULL,
  tipo TEXT NOT NULL,
  fecha DATE NOT NULL,
  operacion_codigo TEXT NOT NULL,
  almacen_codigo TEXT NOT NULL,
  valor_u6 BIGINT NOT NULL,
  UNIQUE (empresa_id, uuid),
  UNIQUE (sede_id, numero)
);

CREATE TABLE central_movimiento (
  id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  empresa_id BIGINT NOT NULL REFERENCES empresa(id),
  sede_id BIGINT NOT NULL REFERENCES sede_central(id),
  documento_id BIGINT NOT NULL REFERENCES central_documento(id),
  fecha DATE NOT NULL,
  almacen_codigo TEXT NOT NULL,
  variante_codigo TEXT NOT NULL,
  producto_codigo TEXT NOT NULL,
  signo SMALLINT NOT NULL CHECK (signo IN (-1, 1)),
  cantidad_u6 BIGINT NOT NULL CHECK (cantidad_u6 > 0),
  valor_u6 BIGINT NOT NULL CHECK (valor_u6 >= 0)
);
CREATE INDEX ix_central_movimiento_sede ON central_movimiento(sede_id, almacen_codigo, variante_codigo);

CREATE TABLE central_cierre (
  id BIGINT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
  empresa_id BIGINT NOT NULL REFERENCES empresa(id),
  sede_id BIGINT NOT NULL REFERENCES sede_central(id),
  tipo TEXT NOT NULL CHECK (tipo IN ('cierre_diario','cierre_mensual')),
  operacion_codigo TEXT NOT NULL,
  periodo TEXT NOT NULL,
  cerrado_en TIMESTAMPTZ,
  UNIQUE (sede_id, tipo, operacion_codigo, periodo)
);

-- Lo consolidado no se corrige a mano: solo lo escribe la recepción de eventos.
CREATE FUNCTION fn_consolidado_inmutable() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  RAISE EXCEPTION 'CONSOLIDADO_INMUTABLE: los datos recibidos de una sede no se modifican';
END $$;
CREATE TRIGGER consolidado_inmutable BEFORE UPDATE OR DELETE ON central_documento  FOR EACH ROW EXECUTE FUNCTION fn_consolidado_inmutable();
CREATE TRIGGER consolidado_inmutable BEFORE UPDATE OR DELETE ON central_movimiento FOR EACH ROW EXECUTE FUNCTION fn_consolidado_inmutable();
CREATE TRIGGER consolidado_inmutable BEFORE UPDATE OR DELETE ON central_cierre     FOR EACH ROW EXECUTE FUNCTION fn_consolidado_inmutable();

CREATE VIEW v_central_saldo AS
SELECT m.empresa_id, s.codigo AS sede, m.almacen_codigo, m.variante_codigo, m.producto_codigo,
       SUM(m.signo * m.cantidad_u6) AS cantidad_u6, SUM(m.signo * m.valor_u6) AS valor_u6
  FROM central_movimiento m JOIN sede_central s ON s.id = m.sede_id
 GROUP BY m.empresa_id, s.codigo, m.almacen_codigo, m.variante_codigo, m.producto_codigo;

-- Aplica un evento ya validado. Devuelve NULL si se aplicó o el motivo por el que se retiene.
CREATE FUNCTION fn_aplicar_evento(p_evento bigint) RETURNS text
LANGUAGE plpgsql SECURITY DEFINER SET search_path = public AS $$
DECLARE e record; v_doc bigint; l jsonb; v_valor bigint;
BEGIN
  SELECT * INTO e FROM evento_recibido WHERE id = p_evento;
  IF e.version_payload <> 1 THEN RETURN 'VERSION_NO_SOPORTADA: version ' || e.version_payload; END IF;
  IF e.depende_de IS NOT NULL AND NOT EXISTS (
       SELECT 1 FROM evento_recibido x WHERE x.empresa_id = e.empresa_id AND x.referencia = e.depende_de AND x.estado = 'aplicado') THEN
    RETURN 'ESPERA_DEPENDENCIA: ' || e.depende_de;
  END IF;
  IF e.tipo = 'documento_stock' THEN
    SELECT COALESCE(SUM((x->>'signo')::bigint * (x->>'valor_u6')::bigint), 0) INTO v_valor FROM jsonb_array_elements(e.payload->'lineas') x;
    INSERT INTO central_documento(empresa_id, sede_id, evento_id, uuid, numero, tipo, fecha, operacion_codigo, almacen_codigo, valor_u6)
    VALUES (e.empresa_id, e.sede_id, e.id, (e.payload->>'documento')::uuid, e.payload->>'numero', e.payload->>'tipo',
            (e.payload->>'fecha')::date, e.payload->>'operacion', e.payload->>'almacen', v_valor)
    RETURNING id INTO v_doc;
    FOR l IN SELECT * FROM jsonb_array_elements(e.payload->'lineas') LOOP
      INSERT INTO central_movimiento(empresa_id, sede_id, documento_id, fecha, almacen_codigo, variante_codigo, producto_codigo, signo, cantidad_u6, valor_u6)
      VALUES (e.empresa_id, e.sede_id, v_doc, (e.payload->>'fecha')::date, l->>'almacen', l->>'variante', l->>'producto',
              (l->>'signo')::smallint, (l->>'cantidad_u6')::bigint, (l->>'valor_u6')::bigint);
    END LOOP;
  ELSIF e.tipo IN ('cierre_diario', 'periodo_mensual') THEN
    INSERT INTO central_cierre(empresa_id, sede_id, tipo, operacion_codigo, periodo, cerrado_en)
    VALUES (e.empresa_id, e.sede_id, CASE e.tipo WHEN 'cierre_diario' THEN 'cierre_diario' ELSE 'cierre_mensual' END,
            e.payload->>'operacion', e.payload->>'periodo', (e.payload->>'cerrado_en')::timestamptz);
  ELSE
    RETURN 'TIPO_NO_SOPORTADO: ' || e.tipo;
  END IF;
  UPDATE evento_recibido SET estado = 'aplicado', motivo = NULL, aplicado_en = now() WHERE id = p_evento;
  RETURN NULL;
END $$;
REVOKE ALL ON FUNCTION fn_aplicar_evento(bigint) FROM PUBLIC;

-- Aplica en orden todo lo retenido de la sede que ya tenga su antecesor. Requiere la fila de la sede bloqueada.
CREATE FUNCTION fn_aplicar_retenidos(p_sede bigint) RETURNS void
LANGUAGE plpgsql SECURITY DEFINER SET search_path = public AS $$
DECLARE e record; v_motivo text;
BEGIN
  LOOP
    SELECT r.* INTO e FROM evento_recibido r JOIN sede_central s ON s.id = r.sede_id
     WHERE r.sede_id = p_sede AND r.secuencia = s.ultima_secuencia_aplicada + 1 AND r.estado = 'retenido';
    EXIT WHEN NOT FOUND;
    BEGIN
      v_motivo := fn_aplicar_evento(e.id);
    EXCEPTION WHEN unique_violation THEN
      -- Mismo documento ya recibido de otra sede (o número repetido): no se sobrescribe nada.
      UPDATE evento_recibido SET estado = 'conflicto', motivo = 'DATO_YA_EXISTE: ' || SQLERRM WHERE id = e.id;
      EXIT;
    END;
    IF v_motivo IS NOT NULL THEN
      UPDATE evento_recibido SET motivo = v_motivo WHERE id = e.id;
      EXIT;
    END IF;
    UPDATE sede_central SET ultima_secuencia_aplicada = e.secuencia WHERE id = p_sede;
  END LOOP;
END $$;
REVOKE ALL ON FUNCTION fn_aplicar_retenidos(bigint) FROM PUBLIC;

-- Punto de entrada único del agente de sincronización.
-- Respuestas: APLICADO, RETENIDO (espera su antecesor o una actualización), DUPLICADO (ya estaba: acuse repetido),
-- CONFLICTO (misma clave con otro contenido o dato ya existente), RECHAZADO (credencial o datos inválidos).
CREATE FUNCTION fn_recibir_evento(p_empresa text, p_sede text, p_credencial text, p_uuid text, p_secuencia bigint,
                                  p_tipo text, p_version integer, p_referencia text, p_depende_de text, p_payload text)
RETURNS text LANGUAGE plpgsql SECURITY DEFINER SET search_path = public AS $$
DECLARE s record; v_hash text; v_previo record; v_payload jsonb; v_id bigint; v_estado text;
BEGIN
  SELECT sc.* INTO s FROM sede_central sc JOIN empresa em ON em.id = sc.empresa_id
   WHERE em.codigo = p_empresa AND sc.codigo = p_sede FOR UPDATE OF sc;
  IF NOT FOUND OR NOT s.activa OR p_credencial IS NULL
     OR s.credencial_sha256 <> encode(sha256(convert_to(p_credencial, 'UTF8')), 'hex') THEN
    INSERT INTO rechazo_sincronizacion(empresa_codigo, sede_codigo, uuid, motivo)
    VALUES (left(p_empresa, 30), left(p_sede, 30), left(p_uuid, 40), 'SEDE_NO_AUTORIZADA');
    RETURN 'RECHAZADO';
  END IF;
  IF p_uuid IS NULL OR p_secuencia IS NULL OR p_secuencia < 1 OR p_tipo IS NULL OR p_version IS NULL THEN
    INSERT INTO rechazo_sincronizacion(empresa_codigo, sede_codigo, uuid, motivo) VALUES (p_empresa, p_sede, left(p_uuid, 40), 'EVENTO_INCOMPLETO');
    RETURN 'RECHAZADO';
  END IF;
  BEGIN
    v_payload := p_payload::jsonb;
  EXCEPTION WHEN others THEN
    INSERT INTO rechazo_sincronizacion(empresa_codigo, sede_codigo, uuid, motivo) VALUES (p_empresa, p_sede, left(p_uuid, 40), 'CONTENIDO_INVALIDO');
    RETURN 'RECHAZADO';
  END;
  v_hash := encode(sha256(convert_to(p_payload || '|' || p_tipo || '|' || p_secuencia || '|' || p_version, 'UTF8')), 'hex');

  SELECT id, contenido_sha256, sede_id INTO v_previo FROM evento_recibido WHERE empresa_id = s.empresa_id AND uuid = p_uuid;
  IF FOUND THEN
    IF v_previo.contenido_sha256 = v_hash AND v_previo.sede_id = s.id THEN
      UPDATE sede_central SET ultima_sincronizacion = now() WHERE id = s.id;
      RETURN 'DUPLICADO';
    END IF;
    INSERT INTO rechazo_sincronizacion(empresa_codigo, sede_codigo, uuid, motivo) VALUES (p_empresa, p_sede, p_uuid, 'CLAVE_CON_OTRO_CONTENIDO');
    RETURN 'CONFLICTO';
  END IF;
  IF EXISTS (SELECT 1 FROM evento_recibido WHERE sede_id = s.id AND secuencia = p_secuencia) THEN
    INSERT INTO rechazo_sincronizacion(empresa_codigo, sede_codigo, uuid, motivo) VALUES (p_empresa, p_sede, p_uuid, 'SECUENCIA_REPETIDA');
    RETURN 'CONFLICTO';
  END IF;

  INSERT INTO evento_recibido(empresa_id, sede_id, uuid, secuencia, tipo, version_payload, referencia, depende_de, payload, contenido_sha256, estado)
  VALUES (s.empresa_id, s.id, p_uuid, p_secuencia, p_tipo, p_version, p_referencia, p_depende_de, v_payload, v_hash, 'retenido')
  RETURNING id INTO v_id;
  PERFORM fn_aplicar_retenidos(s.id);
  UPDATE sede_central SET ultima_sincronizacion = now() WHERE id = s.id;
  SELECT estado INTO v_estado FROM evento_recibido WHERE id = v_id;
  RETURN CASE v_estado WHEN 'aplicado' THEN 'APLICADO' WHEN 'conflicto' THEN 'CONFLICTO' ELSE 'RETENIDO' END;
END $$;
REVOKE ALL ON FUNCTION fn_recibir_evento(text, text, text, text, bigint, text, integer, text, text, text) FROM PUBLIC;

-- Rol del agente en la central: solo puede llamar a la recepción; no lee ni escribe tablas.
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'app_sincronizacion') THEN
    CREATE ROLE app_sincronizacion NOLOGIN;
  END IF;
END $$;
GRANT USAGE ON SCHEMA public TO app_sincronizacion;
GRANT EXECUTE ON FUNCTION fn_recibir_evento(text, text, text, text, bigint, text, integer, text, text, text) TO app_sincronizacion;

-- La aplicación de sede no ve las tablas de la central.
REVOKE ALL ON sede_central, evento_recibido, rechazo_sincronizacion, central_documento, central_movimiento, central_cierre, v_central_saldo FROM app_stock;
