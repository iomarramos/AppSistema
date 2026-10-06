-- V023: ajustes de perfiles (pedido del usuario, 2026-10-05).
--
-- 1. ALMACEN y JEFE_ALMACEN ya no preparan pedidos (COMPRAS_EDITAR). La recepción contra la orden sigue con COMPRAS_VER.
-- 2. ADICIONAL_APROBAR: un requerimiento adicional pasa de borrador a aprobado (jefe de operación) antes de entregarse.
--    El requerimiento calculado se entrega directamente, como antes. La aprobación la valida la base (fn_tiene_permiso),
--    no solo la pantalla, y registra quién aprobó y cuándo.
-- 3. INVENTARIO_VER: consultar inventarios y diferencias sin poder contar ni aprobar.
-- Con la conexión del propietario o del instalador (sin app.usuario_id) las reglas de permiso no aplican.

-- ---------- Permisos nuevos en las empresas ya instaladas ----------
INSERT INTO permiso(empresa_id, codigo, descripcion)
SELECT e.id, 'ADICIONAL_APROBAR', 'Aprobar los requerimientos adicionales antes de entregarlos'
FROM empresa e WHERE NOT EXISTS (SELECT 1 FROM permiso p WHERE p.empresa_id = e.id AND p.codigo = 'ADICIONAL_APROBAR');

INSERT INTO permiso(empresa_id, codigo, descripcion)
SELECT e.id, 'INVENTARIO_VER', 'Consultar los inventarios fisicos y sus diferencias'
FROM empresa e WHERE NOT EXISTS (SELECT 1 FROM permiso p WHERE p.empresa_id = e.id AND p.codigo = 'INVENTARIO_VER');

INSERT INTO rol_permiso(empresa_id, rol_id, permiso_id)
SELECT r.empresa_id, r.id, p.id
FROM rol r
JOIN permiso p ON p.empresa_id = r.empresa_id
WHERE (p.codigo = 'ADICIONAL_APROBAR' AND r.codigo IN ('ADMIN', 'SUPERVISOR', 'OPERACIONES'))
   OR (p.codigo = 'INVENTARIO_VER' AND r.codigo IN ('ADMIN', 'SUPERVISOR', 'OPERACIONES', 'ALMACEN', 'JEFE_ALMACEN'))
ON CONFLICT (empresa_id, rol_id, permiso_id) DO NOTHING;

-- ---------- Almacén sin pedidos ----------
DELETE FROM rol_permiso rp
USING rol r, permiso p
WHERE r.empresa_id = rp.empresa_id AND r.id = rp.rol_id
  AND p.empresa_id = rp.empresa_id AND p.id = rp.permiso_id
  AND r.codigo IN ('ALMACEN', 'JEFE_ALMACEN') AND p.codigo = 'COMPRAS_EDITAR';

-- ---------- Aprobación del requerimiento adicional ----------
ALTER TABLE requerimiento ADD COLUMN aprobado_por BIGINT, ADD COLUMN aprobado_en TIMESTAMPTZ;
ALTER TABLE requerimiento ADD FOREIGN KEY (empresa_id, aprobado_por) REFERENCES usuario(empresa_id, id);

CREATE OR REPLACE FUNCTION fn_proteger_requerimiento() RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE v_operacion bigint;
BEGIN
  IF TG_OP = 'INSERT' THEN
    IF NEW.estado <> 'borrador' THEN
      RAISE EXCEPTION 'REQUERIMIENTO_NACE_BORRADOR: todo requerimiento se crea en borrador';
    END IF;
    RETURN NEW;
  END IF;
  IF TG_OP = 'DELETE' THEN
    IF OLD.estado <> 'borrador' THEN
      RAISE EXCEPTION 'REQUERIMIENTO_ATENDIDO: el requerimiento % esta % y no se elimina', OLD.numero, OLD.estado;
    END IF;
    RETURN OLD;
  END IF;

  IF (NEW.almacen_id, NEW.minuta_id, NEW.operacion_servicio_id, NEW.fecha, NEW.numero, NEW.usuario_id)
     IS DISTINCT FROM (OLD.almacen_id, OLD.minuta_id, OLD.operacion_servicio_id, OLD.fecha, OLD.numero, OLD.usuario_id) THEN
    RAISE EXCEPTION 'DATO_INVALIDO: los datos del requerimiento no se cambian; cree otro';
  END IF;

  IF OLD.estado = NEW.estado THEN
    IF OLD.estado <> 'borrador' THEN
      RAISE EXCEPTION 'REQUERIMIENTO_ATENDIDO: el requerimiento % esta % y no se modifica', OLD.numero, OLD.estado;
    END IF;
    RETURN NEW;
  END IF;

  -- Borrador -> aprobado: solo el adicional, y con el permiso de la operación.
  IF OLD.estado = 'borrador' AND NEW.estado = 'aprobado' THEN
    IF NEW.tipo <> 'adicional' THEN
      RAISE EXCEPTION 'SOLO_ADICIONAL: solo el requerimiento adicional se aprueba; el calculado se entrega directamente';
    END IF;
    SELECT s.operacion_id INTO v_operacion FROM operacion_servicio s WHERE s.empresa_id = NEW.empresa_id AND s.id = NEW.operacion_servicio_id;
    IF NOT fn_tiene_permiso('ADICIONAL_APROBAR', v_operacion) THEN
      RAISE EXCEPTION 'SIN_PERMISO: no tiene permiso para aprobar requerimientos adicionales en esta operacion';
    END IF;
    NEW.aprobado_por := fn_usuario_actual();
    NEW.aprobado_en := now();
    RETURN NEW;
  END IF;

  -- Entrega: el calculado sale de borrador; el adicional solo desde aprobado.
  IF NEW.estado = 'atendido' AND OLD.estado IN ('borrador', 'aprobado') THEN
    IF OLD.estado = 'borrador' AND NEW.tipo = 'adicional' THEN
      RAISE EXCEPTION 'ADICIONAL_NO_APROBADO: el requerimiento adicional % debe aprobarse antes de entregarse', OLD.numero;
    END IF;
    RETURN NEW;
  END IF;

  IF NEW.estado = 'anulado' AND OLD.estado IN ('borrador', 'aprobado') THEN
    RETURN NEW;
  END IF;

  RAISE EXCEPTION 'REQUERIMIENTO_ATENDIDO: el requerimiento % esta % y no pasa a %', OLD.numero, OLD.estado, NEW.estado;
END $$;

-- ---------- Nombres visibles ----------
UPDATE rol SET nombre = 'Almacenero (recepcion, despacho, conteo; sin pedidos ni aprobaciones)' WHERE codigo = 'ALMACEN';
UPDATE rol SET nombre = 'Jefe de almacen (aprueba inventarios y ajustes; reportes)' WHERE codigo = 'JEFE_ALMACEN';
UPDATE rol SET nombre = 'Jefe de operacion (aprueba planificacion, adicionales, cierres, Food Cost y resultados; sin stock)' WHERE codigo = 'OPERACIONES';
