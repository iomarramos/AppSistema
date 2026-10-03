-- V006
-- 1. Técnica de preparación del ingrediente (corte, cocción…) tal como viene en las fichas técnicas del SGP.
-- 2. Corrección de fn_auditar (V004): un UPDATE sin cambios en una tabla distinta de "usuario" fallaba con
--    'record "old" has no field "password_hash"' (p. ej. guardar un producto sin modificarlo).

ALTER TABLE receta_ingrediente ADD COLUMN tecnica TEXT;

CREATE OR REPLACE FUNCTION fn_auditar() RETURNS trigger
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
    -- PL/pgSQL resuelve OLD.password_hash aunque la primera condición sea falsa: se consulta por JSON.
    IF TG_TABLE_NAME = 'usuario' AND (to_jsonb(OLD)->>'password_hash') IS DISTINCT FROM (to_jsonb(NEW)->>'password_hash') THEN
      v_accion := 'CAMBIO_CLAVE'; v_antes := NULL; v_despues := NULL;
    ELSE
      RETURN NULL;   -- nada cambió, o solo contadores de acceso: no es un cambio de negocio
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
