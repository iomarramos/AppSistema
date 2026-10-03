-- V020: dueño del sistema (pedido del usuario, 2026-10-03).
-- El dueño es el administrador general. Entra con su clave personal, tiene todos los permisos en todas las
-- operaciones de la empresa (también en las que se creen después) y decide qué módulos y hasta qué nivel tiene
-- cada persona. Solo otro dueño, o el instalador con la conexión del propietario, puede marcar o desmarcar a un
-- dueño. Desde la aplicación nadie se otorga ese rango.

ALTER TABLE usuario ADD COLUMN es_dueno boolean NOT NULL DEFAULT false;

CREATE FUNCTION fn_proteger_dueno() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF (TG_OP = 'INSERT' AND NEW.es_dueno) OR (TG_OP = 'UPDATE' AND NEW.es_dueno IS DISTINCT FROM OLD.es_dueno) THEN
    -- Con sesión de la aplicación (app.usuario_id fijado), solo un dueño activo de la misma empresa puede hacerlo.
    IF fn_usuario_actual() IS NOT NULL AND NOT EXISTS (
         SELECT 1 FROM usuario d WHERE d.id = fn_usuario_actual() AND d.empresa_id = NEW.empresa_id AND d.es_dueno AND d.activo = 1) THEN
      RAISE EXCEPTION 'SOLO_DUENO: solo el dueno del sistema puede otorgar o quitar ese rango';
    END IF;
  END IF;
  RETURN NEW;
END $$;

CREATE TRIGGER proteger_dueno BEFORE INSERT OR UPDATE ON usuario FOR EACH ROW EXECUTE FUNCTION fn_proteger_dueno();
