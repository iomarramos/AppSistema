-- V034: vistas de la minuta teórica frente a la real (plan del SGP cargado en sgp_plan_dia y sgp_plan_plato).
--
-- * Solo se crean si existen las tablas del plan del SGP (otra línea de desarrollo las cargó en la base local).
--   En una base limpia de esta rama no hace nada.
-- * security_invoker = true: la vista se ejecuta con los permisos de quien la consulta, así que el aislamiento por
--   empresa (RLS de las tablas base) se aplica igual que a las tablas. Sin esto, la vista saltaría el RLS.
-- * Importes en soles (las tablas guardan U6 = ×1 000 000). Las raciones también vienen en U6.
--
-- v_minuta_teorico_real_plato: una fila por día, servicio, estructura y receta, con lo teórico y lo real lado a lado.
--   Una receta real que no estaba en lo teórico aparece con teórico en cero (sustitución).
-- v_minuta_teorico_real_dia: una fila por día y servicio, con comensales, costo por bandeja y costo total de platos.

DO $$
BEGIN
  IF to_regclass('public.sgp_plan_plato') IS NULL OR to_regclass('public.sgp_plan_dia') IS NULL THEN
    RAISE NOTICE 'V034: no hay plan del SGP en esta base; no se crean las vistas.';
    RETURN;
  END IF;

  EXECUTE $v$
    CREATE OR REPLACE VIEW v_minuta_teorico_real_plato WITH (security_invoker = true) AS
    SELECT COALESCE(t.empresa_id, r.empresa_id)            AS empresa_id,
           COALESCE(t.operacion_id, r.operacion_id)        AS operacion_id,
           COALESCE(t.fecha, r.fecha)                      AS fecha,
           COALESCE(t.servicio, r.servicio)                AS servicio,
           COALESCE(t.estructura, r.estructura)            AS estructura,
           COALESCE(t.receta_codigo_sgp, r.receta_codigo_sgp) AS receta_codigo_sgp,
           COALESCE(t.raciones_u6, 0) / 1000000.0          AS raciones_teorico,
           COALESCE(r.raciones_u6, 0) / 1000000.0          AS raciones_real,
           (COALESCE(r.raciones_u6, 0) - COALESCE(t.raciones_u6, 0)) / 1000000.0 AS diferencia_raciones,
           t.costo_racion_u6 / 1000000.0                   AS costo_racion_teorico,
           r.costo_racion_u6 / 1000000.0                   AS costo_racion_real,
           COALESCE(t.raciones_u6::numeric * t.costo_racion_u6::numeric, 0) / 1000000000000.0 AS costo_teorico,
           COALESCE(r.raciones_u6::numeric * r.costo_racion_u6::numeric, 0) / 1000000000000.0 AS costo_real,
           (COALESCE(r.raciones_u6::numeric * r.costo_racion_u6::numeric, 0)
            - COALESCE(t.raciones_u6::numeric * t.costo_racion_u6::numeric, 0)) / 1000000000000.0 AS diferencia_costo,
           r.porcentaje_bp / 100.0                         AS porcentaje_real,
           (t.id IS NOT NULL)                              AS en_teorico,
           (r.id IS NOT NULL)                              AS en_real
      FROM (SELECT * FROM sgp_plan_plato WHERE nivel = 'TEORICO') t
      FULL OUTER JOIN (SELECT * FROM sgp_plan_plato WHERE nivel = 'REAL') r
        ON r.empresa_id = t.empresa_id AND r.operacion_id = t.operacion_id AND r.fecha = t.fecha
       AND r.servicio = t.servicio AND r.estructura = t.estructura AND r.receta_codigo_sgp = t.receta_codigo_sgp
  $v$;

  EXECUTE $v$
    CREATE OR REPLACE VIEW v_minuta_teorico_real_dia WITH (security_invoker = true) AS
    SELECT d.empresa_id,
           d.operacion_id,
           d.fecha,
           d.servicio,
           MAX(d.comensales) FILTER (WHERE d.nivel = 'TEORICO')          AS comensales_teorico,
           MAX(d.comensales) FILTER (WHERE d.nivel = 'REAL')             AS comensales_real,
           MAX(d.costo_minuta_dia_u6) FILTER (WHERE d.nivel = 'TEORICO') / 1000000.0 AS costo_bandeja_teorico,
           MAX(d.costo_minuta_dia_u6) FILTER (WHERE d.nivel = 'REAL') / 1000000.0    AS costo_bandeja_real,
           (SELECT COALESCE(SUM(p.raciones_teorico * p.costo_racion_teorico), 0) FROM v_minuta_teorico_real_plato p
             WHERE p.empresa_id = d.empresa_id AND p.operacion_id = d.operacion_id AND p.fecha = d.fecha AND p.servicio = d.servicio) AS costo_total_teorico,
           (SELECT COALESCE(SUM(p.raciones_real * p.costo_racion_real), 0) FROM v_minuta_teorico_real_plato p
             WHERE p.empresa_id = d.empresa_id AND p.operacion_id = d.operacion_id AND p.fecha = d.fecha AND p.servicio = d.servicio) AS costo_total_real
      FROM sgp_plan_dia d
     GROUP BY d.empresa_id, d.operacion_id, d.fecha, d.servicio
  $v$;

  GRANT SELECT ON v_minuta_teorico_real_plato, v_minuta_teorico_real_dia TO app_stock;
END
$$;
