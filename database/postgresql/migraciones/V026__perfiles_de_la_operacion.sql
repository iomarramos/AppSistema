-- V022: perfiles de la operación (pedido del usuario, 2026-10-05). Las empresas nuevas reciben estos roles desde
-- RolesBase; aquí se alinean las ya instaladas.
--
-- * JEFE_ALMACEN: nuevo. Hace lo del almacenero (ALMACEN) y además aprueba inventarios y ajustes (INVENTARIO_APROBAR)
--   y ve reportes. Es un conjunto de permisos que ya existían: no se crea ningún permiso nuevo.
-- * OPERACIONES pasa a ser el jefe de operación: aprueba cambios de planificación (MINUTAS_APROBAR), ejecuta el cierre
--   diario y mensual (CIERRE_EJECUTAR) y ve resultados (RESULTADOS_VER). No recibe ni mueve stock.
-- * Los nombres visibles de ALMACEN (Almacenero), OPERACIONES (Jefe de operación) y CHEF (Chef operativo) se alinean
--   con el código. Los códigos no cambian: las asignaciones existentes siguen valiendo.
-- Con la conexión del propietario o del instalador (sin app.usuario_id) las reglas de permiso no aplican.

INSERT INTO rol(empresa_id, codigo, nombre)
SELECT e.id, 'JEFE_ALMACEN', 'Jefe de almacen (aprueba inventarios y ajustes; reportes)'
FROM empresa e
WHERE NOT EXISTS (SELECT 1 FROM rol r WHERE r.empresa_id = e.id AND r.codigo = 'JEFE_ALMACEN');

INSERT INTO rol_permiso(empresa_id, rol_id, permiso_id)
SELECT r.empresa_id, r.id, p.id
FROM rol r
JOIN permiso p ON p.empresa_id = r.empresa_id
WHERE r.codigo = 'JEFE_ALMACEN'
  AND p.codigo IN ('CATALOGO_VER', 'STOCK_CONTABILIZAR', 'MENUS_VER', 'COMPRAS_VER', 'COMPRAS_EDITAR',
                   'INVENTARIO_CONTAR', 'INVENTARIO_APROBAR', 'REPORTES_VER')
ON CONFLICT (empresa_id, rol_id, permiso_id) DO NOTHING;

INSERT INTO rol_permiso(empresa_id, rol_id, permiso_id)
SELECT r.empresa_id, r.id, p.id
FROM rol r
JOIN permiso p ON p.empresa_id = r.empresa_id
WHERE r.codigo = 'OPERACIONES'
  AND p.codigo IN ('MINUTAS_APROBAR', 'CIERRE_EJECUTAR', 'RESULTADOS_VER')
ON CONFLICT (empresa_id, rol_id, permiso_id) DO NOTHING;

UPDATE rol SET nombre = 'Almacenero (recepcion, despacho, conteo; prepara pedidos extra)' WHERE codigo = 'ALMACEN';
UPDATE rol SET nombre = 'Jefe de operacion (aprueba planificacion, cierres, Food Cost y resultados; sin stock)' WHERE codigo = 'OPERACIONES';
UPDATE rol SET nombre = 'Chef operativo (programacion del dia, factores del dia, produccion; sin precios ni stock)' WHERE codigo = 'CHEF';
