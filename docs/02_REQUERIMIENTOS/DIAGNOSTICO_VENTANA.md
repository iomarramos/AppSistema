# Diagnóstico de la especificación `docs/02_REQUERIMIENTOS/ESPECIFICACION_VENTANA/` (Fase 0)

Fecha: 2026-10-05. Fuente: carpeta `docs/02_REQUERIMIENTOS/ESPECIFICACION_VENTANA/` (00 a 08). Método: el prompt maestro (07) pide diagnosticar antes de tocar código. Este documento compara cada requisito con el repositorio. No cambia código.

Clasificación: **HECHO** (cumple lo que pide la especificación), **PARCIAL** (existe una parte), **FALTA** (no existe), **BLOQUEADO** (depende de datos o decisiones del usuario).

## 1. Resumen

- De los 26 formularios que lista el índice (00), **23 existen con ese nombre**. Faltan `FormReportes`, `FormPlanificacionMenus` (la matriz mensual) y `FormProduccionChef`. Su función vive hoy en otros formularios o no existe.
- Las **reglas de negocio críticas ya están en la base** (documento confirmado inmutable, plan con versiones, día y mes cerrados, conteo que no mueve stock, permisos por módulo, pantalla, acción y alcance). La especificación pide exactamente eso.
- Los **grandes faltantes** son la pantalla mensual de planificación, el catálogo de 27 reportes con vista previa común y exportación real (hoy solo CSV e impresión por navegador), la pantalla de recepciones con lote y vencimiento, el monitor de tránsitos y el cierre mensual como asistente.
- **Deuda de interfaz:** la regla "el Designer es la fuente real" (01 y 07) se cumple solo en `FormAcceso` y `FormPrincipal`. Veinte formularios todavía llaman `Controls.Clear()`; hay que auditar si lo usan para controles permanentes o solo para contenido dinámico. `AccessibleName` aparece en pocos archivos.
- **Bloqueado por datos:** la información nutricional (no hay tabla de composición) y el menú del mes real del usuario.

## 2. Formularios: existencia y estado

| Especificación | Formulario en el repositorio | Estado | Observación |
|---|---|---|---|
| 01 Shell `FormPrincipal` | `FormPrincipal` | PARCIAL | Menú por áreas antiguas (Catálogo, Menús, Producción, Compras, Almacén, Inventario, Cierres y control, Administración). Falta la taxonomía de la especificación: Planificación, Abastecimiento, Resultados separados. `Ctrl+K` existe (`mnuIrPantalla`). Barra de estado: empresa, operación, usuario, sincronización con la central, versión y último cierre (agregados el 2026-10-05; falta estado de BD) |
| 02 `FormServicios` | `FormServicios` | PARCIAL | Estructuras y factores existen. Falta validar alternativas al 100 % y versionar cambios futuros (la base versiona el factor; revisar la pantalla) |
| 02 `FormRecetas` | `FormRecetas` | PARCIAL | Versiones y aprobación (V005) existen. Faltan pestañas Método, Nutrición y Uso en menú |
| 02 `FormPlanificacionMenus` (matriz mensual por día) | **No existe** | **FALTA** | Es el P0 que la especificación recomienda como primera pantalla completa. Hoy el plan se trabaja por minuta (`FormMinutas`) |
| 02 `FormMinutas` | `FormMinutas` | PARCIAL | Cabecera y platos existen. Faltan costo piso/techo en pantalla, pestañas de necesidades, cambios y requerimientos |
| 02 `FormProduccionChef` | **No existe** | **FALTA** | Las funciones del chef están en `FormProduccion` (Calcular, Adicional, Cambiar cantidad, Entregar, Registrar, Merma). Falta el plan operativo con diferencia teórica vs operativa |
| 02 `FormProduccion` | `FormProduccion` | PARCIAL | Requerimiento calculado y adicional, entrega, producción, mermas, venta real y comparativo existen. Ahora también aprobación de adicionales y pedido de devolución (V023 y V024) |
| 02 Requerimiento (estados BORRADOR→ENVIADO→APROBADO→PREPARADO→DESPACHADO→CERRADO) | En `FormProduccion` | PARCIAL | Existen BORRADOR, APROBADO (solo adicional), ATENDIDO, ANULADO. Faltan ENVIADO, PREPARADO y CERRADO |
| 02 Comparativo de tres niveles | `FormComparativo` | PARCIAL | Cálculo teórico, plan real y realizado existe (`PlanVsReal`). Revisar la vista contra la tabla de la especificación |
| 03 `FormCompras` | `FormCompras` | PARCIAL | Previsión y pedido existen, con pedido manual (adicional o caja chica). Falta mostrar cada componente de la fórmula por producto, estados CALCULADO/PARCIAL, y el snapshot del cálculo |
| 03 `FormConsolidado` | `FormConsolidado` | PARCIAL | Consolidado por operación existe. Falta drill-down por origen y distribución por operación en pantalla |
| 03 `FormProveedores` | `FormProveedores` | PARCIAL | Proveedores, precios y empaques existen. Faltan lead time, días de entrega e historial en pantalla |
| 03 `FormRecepciones` | **No existe** | **FALTA** | La recepción existe como "Recibir pedido..." en `FormStock`. Falta tipo (proveedor, CD, FOFI, traspaso), lote, vencimiento, diferencia visible y confirmación con cantidad física real |
| 03 `FormStock` | `FormStock` | PARCIAL | Saldos, kárdex, traspasos, bajas, devoluciones, inventario inicial y registro SUNAT existen. Faltan pestañas Lotes, Reservas y Tránsitos, filtros de stock cero y próximos a vencer |
| 03 `FormMovimientosAlmacen` [propuesta] | **No existe** | FALTA | P1 |
| 03 `FormDespachoProduccion` [propuesta] | En `FormProduccion` ("Entregar (almacen)") | PARCIAL | Falta la pantalla de preparación con solicitado, disponible, preparado y pendiente, y FEFO por lote |
| 03 `FormDevolucionProduccion` [propuesta] | En `FormStock` y, desde V024, flujo cocina → almacén | PARCIAL | Falta condición apto / no apto y lote |
| 03 `FormInventarios` | `FormInventarios` | PARCIAL | Apertura, conteo, recuento, revisión y autorización del ajuste separados; conteo ciego; costo promedio. Falta la política ABC del rotativo |
| 03 `FormAjustesInventario` [propuesta] | En `FormInventarios` ("Autorizar ajuste") | PARCIAL | Faltan motivo normalizado (lista de la especificación), explicación y documento soporte |
| 03 `FormTransitos` [propuesta] | **No existe** | FALTA | P1 |
| 04 `FormCierres` | `FormCierres` | PARCIAL | Tablero de pendientes por día y Food Cost existen. Falta el checklist con "Ir a" y el calendario con estados de la especificación |
| 04 Cierre mensual (asistente de 8 pasos) | En `FormCierres` / `FormResultados` | PARCIAL | No hay asistente. P1 |
| 04 `FormResultados` | `FormResultados` | PARCIAL | Gastos y resultado mensual existen. Faltan comparación presupuesto / plan / real y acumulado |
| 04 `FormReportes` (catálogo por categorías) | **No existe** | **FALTA** | Los reportes están dispersos: minuta del día, requerimiento, kárdex, inventario, stock valorizado, registro SUNAT, Food Cost. Ver sección 4 |
| 05 `FormUsuarios` | `FormUsuarios` | PARCIAL | Roles, operaciones, alcance, excepciones. Falta lista izquierda y detalle derecho, y bloqueo y reset de clave con el diseño pedido |
| 05 `FormMatrizAcceso` | `FormMatrizAcceso` | HECHO (base) | Módulo → pantalla → acción → alcance y excepciones (V021). Falta el árbol jerárquico y la columna "efectivo" en la misma grilla, si se quiere lo exacto de la especificación |
| 05 Perfiles base | Roles en `RolesBase` y V022 a V024 | HECHO | Los siete perfiles existen con otros códigos: OPERACIONES (jefe de operación), CHEF (chef operativo), ALMACEN (almacenero). Documentado en `docs/03_ESTADO/CHECKLIST.md` |
| 05 `FormOperaciones` | `FormOperaciones` | PARCIAL | Operaciones, almacenes, zona. Faltan centros de costo y servicios habilitados por operación |
| 05 `FormAuditoria` | `FormAuditoria` | PARCIAL | Filtros y lista existen. Falta comparar Antes / Después lado a lado y exportar |
| 05 `FormCargaReal` | `FormCargaReal` | PARCIAL | Carga por pasos existe. Falta el asistente de 9 pasos con archivo, filas, insertadas, actualizadas, rechazadas y log por paso |
| 05 `FormContinuidad` | `FormContinuidad` | PARCIAL | Se revisa en el piloto (Etapa 8, pendiente) |
| 05 `FormAcceso` | `FormAcceso` | HECHO (base) | Falta recordar la última empresa y el bloqueo temporal por intentos fallidos |

## 3. Reglas transversales

| Regla (01 y 07) | Estado | Dónde se cumple o qué falta |
|---|---|---|
| Documento confirmado = inmutable; corregir por reversión | HECHO | Triggers en stock, requerimientos (V009 y V023), devoluciones (V024) |
| Plan liberado = snapshot, cambios = nueva versión | PARCIAL | Recetas aprobadas son inmutables. El plan operativo versionado es fase 2 a 3 de la arquitectura |
| Día y mes cerrados no editables | HECHO | Cierres (V011) |
| Conteo no modifica stock; contar y aprobar separados | HECHO | `InventarioContar` e `InventarioAprobar`; ajuste solo al autorizar |
| Saldo sin edición directa | HECHO | Saldo como proyección del libro; no hay UPDATE manual |
| Permisos validados en la base, no solo en botones | HECHO | `fn_tiene_permiso`, V021 en adelante |
| Auditoría de cambios sensibles | HECHO | `fn_auditar` en tablas sensibles |
| Confirmación explícita y motivo en acciones destructivas | PARCIAL | Revisar cada acción. Hay motivo en bajas, devoluciones y anulaciones nuevas |
| La UI no ejecuta SQL | HECHO (revisar) | Hay que confirmar que ningún formulario tenga SQL. Ver "Designer" |
| Todo reporte con vista previa antes de exportar | **FALTA** | Hoy se imprime en navegador o se exporta CSV directo. Ver sección 4 |
| Excel y PDF son salidas, no fuentes | HECHO | Las fuentes son la base |
| 1366x768 y escalado a Full HD | **NO VERIFICADO** | Falta una prueba E2E a esa resolución |
| `AccessibleName` en controles relevantes | PARCIAL | Los botones de `Ui.Boton` ya lo reciben desde `Identificadores`. Faltan campos de filtro y grillas (revisar) |
| Designer como fuente real de controles | PARCIAL | Todos los formularios tienen `.Designer.vb`; 20 usan `Controls.Clear()`. Auditar (ver sección 5) |
| Estados estándar (documento, plan, pedido, inventario) | PARCIAL | Existen en la base con otros nombres. Ver sección 6 |

## 4. Reportes (`04`, catálogo de 27)

- Motor común: **parcial**. Existen la clase `Reporte` y `SalidaReporte` (imprimir en navegador y CSV para Excel con BOM). Falta vista previa dentro de la aplicación, zoom, paginación, PDF propio, Excel real, filtros dinámicos por reporte y totales/agrupaciones como metadatos.
- Reportes existentes: `MinutaDelDia`, `Requerimiento`, `Kardex`, `Inventario` (hoja de conteo o resultado), `StockValorizado`, `RegistroInventarioPermanente` (formato 13.1), además de Food Cost, pendientes de cierre, comparativo y resultado mensual.
- Reportes de la especificación que **faltan**: costo detallado y resumido teórico, previsión de consumo, frecuencia de recetas, aporte nutricional (bloqueado por datos), mapa de solicitud de compras, pedido mensual y extra como documento, compras por período o proveedor, OC pendientes, requisición por servicio, estructura, detallada y resumida, salida a producción (formato de la captura), devolución, raciones, traspasos, posición de stock, lotes, tránsitos, mermas, toma de inventario ciega, diferencias físico vs sistema, boleta de ajuste, explicación de ajustes, A13 reproducible, paquete de cierre.
- **Conclusión:** hay alrededor de 6 a 10 de los 27. El catálogo por categorías (`FormReportes`) no existe.

## 5. Designer y estructura de formularios

- Todos los formularios tienen `.Designer.vb`, pero **20 usan `Controls.Clear()`**: `FormCargaReal`, `FormCatalogo`, `FormCierres`, `FormComparativo`, `FormCompras`, `FormConsolidado`, `FormContinuidad`, `FormContratos`, `FormImportacion`, `FormInventarios`, `FormMatrizAcceso`, `FormMinutas`, `FormOperaciones`, `FormProduccion`, `FormProveedores`, `FormRecetas`, `FormResultados`, `FormServicios`, `FormStock`, `FormUsuarios`.
- La especificación lo prohíbe **para controles permanentes**; está permitido solo para lo que depende de los datos (columnas de días, por ejemplo). Hay que revisar cada caso antes de cambiarlo. Es el mismo pendiente que el usuario pidió el 2026-10-03: pasar las pantallas al Diseñador.
- `FormAcceso` y `FormPrincipal` ya siguen la regla (son las dos que el usuario pidió).

## 6. Vocabulario de estados

La especificación usa nombres de estado que no coinciden con la base. Se conservan los de la base y se muestran con el nombre de la especificación en pantalla.

| Especificación | Base |
|---|---|
| Documento BORRADOR / CONFIRMADO / ANULADO | `documento_stock.estado` ('borrador', 'confirmado'); anulación por reversión |
| Plan BORRADOR / EN_REVISION / APROBADO / LIBERADO / REEMPLAZADO / CERRADO | Minuta: borrador, aprobada; liberación y versiones están en la fase 2 a 3 |
| Pedido BORRADOR / CALCULADO / APROBADO / ENVIADO / PARCIAL / RECIBIDO / ANULADO | `pedido_compra` (V007): revisar el conjunto |
| Inventario ABIERTO / EN_CONTEO / CONTADO / EN_REVISION / APROBADO / AJUSTADO / CERRADO | `inventario.estado` (V010): revisar el conjunto |
| Requerimiento BORRADOR / ENVIADO / APROBADO / PREPARADO / DESPACHADO / CERRADO | `requerimiento.estado`: borrador, aprobado, atendido, anulado |

## 7. Roles de la especificación frente al repositorio

| Especificación | Código en AppSistema | Nota |
|---|---|---|
| SUPERUSUARIO | dueño (`es_dueno`) | Sin fila de rol |
| PLANIFICADOR_CENTRAL | PLANIFICADOR_CENTRAL | Igual |
| COMPRAS_CENTRAL | COMPRAS_CENTRAL | Igual |
| JEFE_OPERACION | OPERACIONES | Nombre visible actualizado |
| CHEF_OPERATIVO | CHEF | Nombre visible actualizado |
| JEFE_ALMACEN | JEFE_ALMACEN | Nuevo (V022) |
| ALMACENERO | ALMACEN | Nombre visible actualizado |

La matriz 05 (sección 3) se compara con la de `RolesBase`. Hay diferencias de detalle que conviene revisar con el usuario: por ejemplo, la especificación da al jefe de operación "inventario aprobar: opcional", y hoy no lo tiene.

## 8. Riesgos

1. **Migración de la pantalla de planificación:** es la pieza más grande y toca recetas, minutas, costos y factores. Sin base de datos corriendo en esta máquina no se puede verificar.
2. **`Controls.Clear()` en 20 formularios:** cambiarlo sin auditoría puede romper pantallas que hoy funcionan. Hay que hacerlo formulario por formulario con prueba E2E.
3. **Reportes con PDF y Excel reales:** requiere decidir la librería. No hay ninguna en el proyecto; hoy todo sale por el navegador o CSV.
4. **Nutrición:** sin tabla de composición no se puede calcular el aporte. No inventar valores (regla de la especificación 04).
5. **Cobertura E2E:** la especificación exige prueba positiva y negativa por formulario y resolución 1366x768. Hoy hay un recorrido de todas las pantallas, sin verificar resolución ni casos negativos por pantalla.

## 9. Pruebas existentes

- Dominio: 98 pruebas (verdes en esta sesión).
- Datos: 69 aserciones SQL más las pruebas de integración de `tests/AppSistema.Datos.Tests` (compilan; **no corridas aquí**, ver CONTINUAR).
- E2E Windows: `AccesoTests`, `PermisosTests`, `RecorridoPantallasTests`, `UsabilidadTests`. Corren en el CI, no en esta máquina.

## 10. Orden propuesto (sprints de la especificación 08, ajustados)

La especificación recomienda empezar por `FormPlanificacionMenus`. Propuesta de orden, con lo que ya está hecho primero:

1. **Cerrar lo ya empezado:** verificar en base los cambios V022 a V024 y la prueba de perfiles; auditar `Controls.Clear()` en formularios P0 (`FormProduccion`, `FormStock`, `FormInventarios`, `FormCompras`, `FormCierres`).
2. **Shell y taxonomía** (`FormPrincipal`): menú por Planificación, Producción, Abastecimiento, Almacén, Inventario, Cierres y Food Cost, Resultados, Administración; barra de estado con BD, último cierre, sincronización y versión.
3. **Planificación** (`FormPlanificacionMenus` nuevo): matriz mensual por día con receta, factor, raciones, costo; resumen de comensales, costo del día, costo por bandeja y techo; solo editar borradores.
4. **Chef** (`FormProduccionChef` nuevo, o pestaña del plan operativo): raciones teóricas vs operativas, sustituciones permitidas y adicional vinculado.
5. **Recepciones y despacho:** `FormRecepciones` con tipos, lote, vencimiento y cantidad física; despacho con FEFO.
6. **Inventario y ajustes:** motivos normalizados, boleta de ajuste, política ABC.
7. **Cierres:** checklist con "Ir a", calendario con estados y asistente de cierre mensual.
8. **Reportes:** `FormReportes` por categorías, motor común con vista previa, PDF, Excel real y los 27 reportes por tandas.
9. **Administración y auditoría:** lista y detalle en usuarios, comparación Antes / Después.

## 11. Decisiones que necesito del usuario

1. **¿Cuál es el primer paso?** Propongo el paso 1 (verificar la base y auditar `Controls.Clear()`), porque corre con la clave de PostgreSQL que ya pedí.
2. **¿Exportación a PDF y Excel real?** Requiere una librería (por ejemplo, para PDF y para `.xlsx`). ¿Se puede agregar dependencias NuGet o se quiere seguir con CSV e impresión?
3. **¿La matriz mensual nueva reemplaza la pestaña de minutas o convive con ella?**
4. **Nutrición:** ¿tiene la tabla de composición de los ingredientes? Sin ella, ese reporte queda pendiente.
