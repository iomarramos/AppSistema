-- V004: fundamentos de la etapa 1.
--   1. Factor de conversión de unidades (brecha detectada: kg↔g, L↔mL).
--   2. Control de intentos de acceso y funciones de autenticación.
--   3. Auditoría automática de maestros y seguridad (sin guardar secretos).
--   4. Presentaciones usadas en documentos: su contenido no cambia (T06).
--   5. Precios de compra sin vigencias superpuestas.
--   6. Aislamiento por empresa con Row Level Security para el rol de la aplicación (T01/T02).
--
-- Contexto de sesión: la aplicación fija, en cada transacción,
--   SELECT set_config('app.empresa_id', '<id>', true), set_config('app.usuario_id', '<id>', true);
-- con valores obtenidos del inicio de sesión, nunca de datos enviados por el usuario.

CREATE EXTENSION IF NOT EXISTS btree_gist;

-- ---------- 1. Unidades ----------
ALTER TABLE unidad_medida
  ADD COLUMN factor_a_base_u6 BIGINT NOT NULL DEFAULT 1000000 CHECK (factor_a_base_u6 > 0);
COMMENT ON COLUMN unidad_medida.factor_a_base_u6 IS
  'Cuantas unidades base de su dimension equivale 1 unidad (escala u6). Ej. base kg: kg=1000000, g=1000.';

-- ---------- 2. Acceso ----------
ALTER TABLE usuario
  ADD COLUMN intentos_fallidos BIGINT NOT NULL DEFAULT 0 CHECK (intentos_fallidos >= 0),
  ADD COLUMN bloqueado_hasta   TIMESTAMPTZ;

CREATE FUNCTION fn_empresa_actual() RETURNS bigint LANGUAGE sql STABLE AS
$$ SELECT NULLIF(current_setting('app.empresa_id', true), '')::bigint $$;

CREATE FUNCTION fn_usuario_actual() RETURNS bigint LANGUAGE sql STABLE AS
$$ SELECT NULLIF(current_setting('app.usuario_id', true), '')::bigint $$;

-- Datos necesarios para verificar credenciales ANTES de conocer la empresa de la sesión.
CREATE FUNCTION fn_datos_acceso(p_empresa_codigo text, p_login text)
RETURNS TABLE(usuario_id bigint, empresa_id bigint, nombre text, password_hash text,
              activo boolean, bloqueado_hasta timestamptz)
LANGUAGE sql STABLE SECURITY DEFINER SET search_path = public, pg_temp AS $$
  SELECT u.id, u.empresa_id, u.nombre, u.password_hash, (u.activo = 1 AND e.activo = 1), u.bloqueado_hasta
    FROM usuario u JOIN empresa e ON e.id = u.empresa_id
   WHERE e.codigo = p_empresa_codigo AND u.login = p_login
$$;

-- Registra el resultado de un intento. Tras p_maximo fallos seguidos bloquea p_minutos.
CREATE FUNCTION fn_registrar_intento_acceso(p_usuario_id bigint, p_exito boolean,
                                            p_maximo int DEFAULT 5, p_minutos int DEFAULT 15)
RETURNS void LANGUAGE plpgsql SECURITY DEFINER SET search_path = public, pg_temp AS $$
BEGIN
  IF p_exito THEN
    UPDATE usuario SET intentos_fallidos = 0, bloqueado_hasta = NULL WHERE id = p_usuario_id;
  ELSE
    UPDATE usuario
       SET intentos_fallidos = CASE WHEN intentos_fallidos + 1 >= p_maximo THEN 0 ELSE intentos_fallidos + 1 END,
           bloqueado_hasta   = CASE WHEN intentos_fallidos + 1 >= p_maximo
                                    THEN now() + make_interval(mins => p_minutos) ELSE bloqueado_hasta END
     WHERE id = p_usuario_id;
  END IF;
END $$;

REVOKE ALL ON FUNCTION fn_datos_acceso(text, text) FROM PUBLIC;
REVOKE ALL ON FUNCTION fn_registrar_intento_acceso(bigint, boolean, int, int) FROM PUBLIC;
GRANT EXECUTE ON FUNCTION fn_datos_acceso(text, text) TO app_stock;
GRANT EXECUTE ON FUNCTION fn_registrar_intento_acceso(bigint, boolean, int, int) TO app_stock;

-- ---------- 3. Auditoría automática ----------
-- Campos que nunca se guardan (secretos) o que no son cambios de negocio.
CREATE FUNCTION fn_auditar() RETURNS trigger
LANGUAGE plpgsql SECURITY DEFINER SET search_path = public, pg_temp AS $$
DECLARE
  v_excluir text[] := ARRAY['password_hash','intentos_fallidos','bloqueado_hasta'];
  v_antes jsonb; v_despues jsonb; v_fila jsonb; v_accion text := TG_OP;
  v_empresa bigint;
BEGIN
  IF TG_OP <> 'INSERT' THEN v_antes := to_jsonb(OLD) - v_excluir; END IF;
  IF TG_OP <> 'DELETE' THEN v_despues := to_jsonb(NEW) - v_excluir; END IF;
  v_fila := COALESCE(to_jsonb(NEW), to_jsonb(OLD));

  IF TG_OP = 'UPDATE' AND v_antes = v_despues THEN
    IF TG_TABLE_NAME = 'usuario' AND OLD.password_hash IS DISTINCT FROM NEW.password_hash THEN
      v_accion := 'CAMBIO_CLAVE'; v_antes := NULL; v_despues := NULL;
    ELSE
      RETURN NULL;   -- solo cambiaron contadores de acceso: no es un cambio de negocio
    END IF;
  END IF;

  IF TG_TABLE_NAME = 'empresa' THEN
    v_empresa := (v_fila->>'id')::bigint;
  ELSE
    v_empresa := (v_fila->>'empresa_id')::bigint;
  END IF;

  INSERT INTO auditoria(empresa_id, usuario_id, tabla, registro_id, accion, antes_json, despues_json)
  VALUES (v_empresa, fn_usuario_actual(), TG_TABLE_NAME, (v_fila->>'id')::bigint, v_accion,
          v_antes::text, v_despues::text);
  RETURN NULL;
END $$;

DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY['empresa','usuario','rol','permiso','rol_permiso','usuario_operacion_rol',
                           'operacion','almacen','unidad_medida','categoria_producto','producto_base','marca',
                           'variante_producto','empaque_compra','proveedor','proveedor_empaque','precio_compra',
                           'politica_abastecimiento'] LOOP
    EXECUTE format('CREATE TRIGGER auditar AFTER INSERT OR UPDATE OR DELETE ON %I
                    FOR EACH ROW EXECUTE FUNCTION fn_auditar()', t);
  END LOOP;
END $$;

-- La aplicación no escribe la auditoría: solo el trigger.
REVOKE INSERT ON auditoria FROM app_stock;

-- ---------- 4. Presentaciones en uso (T06) ----------
CREATE FUNCTION fn_proteger_presentacion_en_uso() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v_usada boolean := false;
BEGIN
  IF TG_TABLE_NAME = 'variante_producto' THEN
    IF NEW.contenido_base_por_envase_u6 IS DISTINCT FROM OLD.contenido_base_por_envase_u6
       OR NEW.producto_base_id IS DISTINCT FROM OLD.producto_base_id THEN
      v_usada := EXISTS (SELECT 1 FROM documento_stock_detalle WHERE empresa_id = OLD.empresa_id AND variante_id = OLD.id)
              OR EXISTS (SELECT 1 FROM recepcion_detalle       WHERE empresa_id = OLD.empresa_id AND variante_id = OLD.id)
              OR EXISTS (SELECT 1 FROM inventario_detalle      WHERE empresa_id = OLD.empresa_id AND variante_id = OLD.id)
              OR EXISTS (SELECT 1 FROM empaque_compra e JOIN pedido_detalle p ON p.empresa_id = e.empresa_id AND p.empaque_id = e.id
                          WHERE e.empresa_id = OLD.empresa_id AND e.variante_id = OLD.id);
    END IF;
  ELSIF TG_TABLE_NAME = 'empaque_compra' THEN
    IF NEW.envases_por_empaque IS DISTINCT FROM OLD.envases_por_empaque
       OR NEW.variante_id IS DISTINCT FROM OLD.variante_id THEN
      v_usada := EXISTS (SELECT 1 FROM pedido_detalle    WHERE empresa_id = OLD.empresa_id AND empaque_id = OLD.id)
              OR EXISTS (SELECT 1 FROM recepcion_detalle WHERE empresa_id = OLD.empresa_id AND empaque_id = OLD.id)
              OR EXISTS (SELECT 1 FROM proveedor_empaque WHERE empresa_id = OLD.empresa_id AND empaque_id = OLD.id);
    END IF;
  ELSIF TG_TABLE_NAME = 'unidad_medida' THEN
    IF NEW.factor_a_base_u6 IS DISTINCT FROM OLD.factor_a_base_u6 OR NEW.dimension IS DISTINCT FROM OLD.dimension THEN
      v_usada := EXISTS (SELECT 1 FROM producto_base WHERE empresa_id = OLD.empresa_id AND unidad_base_id = OLD.id);
    END IF;
  ELSIF TG_TABLE_NAME = 'producto_base' THEN
    IF NEW.unidad_base_id IS DISTINCT FROM OLD.unidad_base_id THEN
      v_usada := EXISTS (SELECT 1 FROM variante_producto  WHERE empresa_id = OLD.empresa_id AND producto_base_id = OLD.id)
              OR EXISTS (SELECT 1 FROM receta_ingrediente WHERE empresa_id = OLD.empresa_id AND producto_base_id = OLD.id);
    END IF;
  END IF;

  IF v_usada THEN
    RAISE EXCEPTION 'PRESENTACION_EN_USO: % % ya se uso en documentos; cree una nueva presentacion en lugar de cambiar su contenido',
      TG_TABLE_NAME, OLD.id;
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_presentacion BEFORE UPDATE ON variante_producto FOR EACH ROW EXECUTE FUNCTION fn_proteger_presentacion_en_uso();
CREATE TRIGGER proteger_presentacion BEFORE UPDATE ON empaque_compra    FOR EACH ROW EXECUTE FUNCTION fn_proteger_presentacion_en_uso();
CREATE TRIGGER proteger_presentacion BEFORE UPDATE ON unidad_medida     FOR EACH ROW EXECUTE FUNCTION fn_proteger_presentacion_en_uso();
CREATE TRIGGER proteger_presentacion BEFORE UPDATE ON producto_base     FOR EACH ROW EXECUTE FUNCTION fn_proteger_presentacion_en_uso();

-- ---------- 5. Precios sin superposición ----------
ALTER TABLE precio_compra ADD CONSTRAINT precio_sin_superposicion
  EXCLUDE USING gist (empresa_id WITH =, proveedor_empaque_id WITH =, moneda WITH =,
                      daterange(fecha_desde, fecha_hasta, '[]') WITH &&);

-- ---------- 6. Aislamiento por empresa (RLS) ----------
-- Se aplica al rol de la aplicación. El propietario (migraciones, funciones SECURITY DEFINER) no se ve afectado.
DO $$
DECLARE r record;
BEGIN
  FOR r IN SELECT c.table_name FROM information_schema.columns c
             JOIN information_schema.tables t ON t.table_schema = c.table_schema AND t.table_name = c.table_name
            WHERE c.table_schema = 'public' AND c.column_name = 'empresa_id' AND t.table_type = 'BASE TABLE' LOOP
    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', r.table_name);
    EXECUTE format('CREATE POLICY aislamiento_empresa ON %I FOR ALL TO app_stock
                    USING (empresa_id = fn_empresa_actual()) WITH CHECK (empresa_id = fn_empresa_actual())', r.table_name);
  END LOOP;
END $$;

ALTER TABLE empresa ENABLE ROW LEVEL SECURITY;
CREATE POLICY aislamiento_empresa ON empresa FOR ALL TO app_stock
  USING (id = fn_empresa_actual()) WITH CHECK (id = fn_empresa_actual());

-- Las vistas se evalúan con los permisos de quien consulta (si no, saltarían el RLS).
ALTER VIEW v_kardex                 SET (security_invoker = true);
ALTER VIEW v_stock_producto         SET (security_invoker = true);
ALTER VIEW v_diferencias_inventario SET (security_invoker = true);
ALTER VIEW v_empaque_conversion     SET (security_invoker = true);
ALTER VIEW v_conciliacion_saldo     SET (security_invoker = true);
