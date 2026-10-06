-- V035: redondeo de las vistas de la minuta teórica frente a la real (corrige V034 sin modificarla).
--
-- Las divisiones entre 1 000 000 y 1 000 000 000 000 dejaban importes con hasta 53 dígitos y escala 32, que no caben en
-- decimal de .NET (28 dígitos). Se redondea a 6 decimales (una milésima de céntimo). Mismas columnas y tipos que V034.
-- Como V034, solo actúa si existen las tablas del plan del SGP.

DO $$
BEGIN
  IF to_regclass('public.sgp_plan_plato') IS NULL OR to_regclass('public.sgp_plan_dia') IS NULL THEN
    RAISE NOTICE 'V035: no hay plan del SGP en esta base; no se cambian las vistas.';
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
           ROUND(COALESCE(t.raciones_u6, 0) / 1000000.0, 6)  AS raciones_teorico,
           ROUND(COALESCE(r.raciones_u6, 0) / 1000000.0, 6)  AS raciones_real,
           ROUND((COALESCE(r.raciones_u6, 0) - COALESCE(t.raciones_u6, 0)) / 1000000.0, 6) AS diferencia_raciones,
           ROUND(t.costo_racion_u6 / 1000000.0, 6)         AS costo_racion_teorico,
           ROUND(r.costo_racion_u6 / 1000000.0, 6)         AS costo_racion_real,
           ROUND(COALESCE(t.raciones_u6::numeric * t.costo_racion_u6::numeric, 0) / 1000000000000.0, 6) AS costo_teorico,
           ROUND(COALESCE(r.raciones_u6::numeric * r.costo_racion_u6::numeric, 0) / 1000000000000.0, 6) AS costo_real,
           ROUND((COALESCE(r.raciones_u6::numeric * r.costo_racion_u6::numeric, 0)
            - COALESCE(t.raciones_u6::numeric * t.costo_racion_u6::numeric, 0)) / 1000000000000.0, 6)   AS diferencia_costo,
           ROUND(r.porcentaje_bp / 100.0, 4)               AS porcentaje_real,
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
           ROUND(MAX(d.costo_minuta_dia_u6) FILTER (WHERE d.nivel = 'TEORICO') / 1000000.0, 6) AS costo_bandeja_teorico,
           ROUND(MAX(d.costo_minuta_dia_u6) FILTER (WHERE d.nivel = 'REAL') / 1000000.0, 6)    AS costo_bandeja_real,
           (SELECT ROUND(COALESCE(SUM(p.raciones_teorico * p.costo_racion_teorico), 0), 6) FROM v_minuta_teorico_real_plato p
             WHERE p.empresa_id = d.empresa_id AND p.operacion_id = d.operacion_id AND p.fecha = d.fecha AND p.servicio = d.servicio) AS costo_total_teorico,
           (SELECT ROUND(COALESCE(SUM(p.raciones_real * p.costo_racion_real), 0), 6) FROM v_minuta_teorico_real_plato p
             WHERE p.empresa_id = d.empresa_id AND p.operacion_id = d.operacion_id AND p.fecha = d.fecha AND p.servicio = d.servicio) AS costo_total_real
      FROM sgp_plan_dia d
     GROUP BY d.empresa_id, d.operacion_id, d.fecha, d.servicio
  $v$;
END
$$;
