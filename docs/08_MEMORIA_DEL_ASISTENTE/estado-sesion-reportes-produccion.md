---
name: estado-sesion-reportes-produccion
description: "Estado de AppSistema al 2026-10-06 (rama claude/busy-mayer-9fxop6, sin commit): migraciones V026-V033 aplicadas a la base local, perfiles de prueba creados, documentación reorganizada en docs/"
metadata:
  node_type: memory
  type: project
  originSessionId: 38a75a09-6121-4c49-9d55-f3a1930580f4
  modified: 2026-10-06T14:02:40.401Z
---

Al 2026-10-06 los cambios de las entregas 4 a 11 están en el árbol de trabajo de `claude/busy-mayer-9fxop6` **sin commit**. El usuario todavía no decidió el commit.

**Why:** el usuario quiere probar la aplicación real con su base local y no tener documentación dispersa.

**How to apply:**
- Antes de decir que algo "está en el sistema", revisar `docs/03_ESTADO/CONTINUAR.md` y `docs/03_ESTADO/CHECKLIST.md` (entregas 1 a 13). `docs/FORMATOS_SGP.md` mencionado antes ya no existe en esta rama.
- Base local `appsistema`: migraciones V001 a V035 de esta rama (V034 y V035 son las vistas de la minuta teórica frente a la real), más 17 tablas de otra línea (`planificacion_*`, `produccion_plan*`, `sgp_*`, con datos en las `sgp_*`). No borrar esas tablas sin decisión del usuario.
- Perfiles de prueba en `appsistema` (empresa DEMO, operación ORC): `chef_prueba`, `almacen_prueba`, `jefe_almacen_prueba`, `operaciones_prueba`, `planificacion_prueba`, `compras_prueba`. Claves en `%LOCALAPPDATA%\AppSistema\perfiles_prueba.txt`, fuera del repositorio. Hay una cuenta de administración local que no creó el sistema: preguntar antes de tocarla.
- Choque de migraciones: V022 a V025 de `feature/formatos-sgp` y `feature/planificacion-menus-matriz` usan esos números para otro contenido. Esta rama usa V026 a V033. Decisión pendiente antes de fusionar.
- Decisiones del usuario que siguen vigentes: R-AL y R-AI se imprimen y se llenan a mano; ventana del chef de 3 días atrás en adelante (fecha de Lima); días de stock 20/21/30/31 por contrato.
- Pendientes: carga del menú del SGP (necesita el archivo original, no texto copiado); decisiones P-01, P-02 y P-13 de `docs/02_REQUERIMIENTOS/REVISION_REQUERIMIENTOS_2026-10-06.md`.
