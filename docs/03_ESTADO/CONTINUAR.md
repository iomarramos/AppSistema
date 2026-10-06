# Cómo continuar (traspaso a una nueva conversación)

Actualizado: 2026-10-06 (corte de la sesión de entregas 4 a 17). Reemplaza el corte del 2026-10-03. La entrega 17 (menú mensual en JSON, V036 `codigo_contrato`) está en `CHECKLIST.md`.

| | |
|---|---|
| Rama | `claude/busy-mayer-9fxop6` |
| PR | #1 (borrador). Los cambios de las entregas 4 a 11 **no están confirmados** (commit pendiente de decisión del usuario) |
| Estado vigente | `docs/03_ESTADO/CHECKLIST.md`, entregas 1 a 11 (al final). Este archivo resume; el checklist manda |
| Prompt interno | `CLAUDE.md` (reglas, estado, pendientes ordenados). Los prompts de creación están en `docs/02_REQUERIMIENTOS/ESPECIFICACION_VENTANA/07` y `docs/02_REQUERIMIENTOS/ESPECIFICACION_VENTANA/06` |

El detalle de cada etapa está en `docs/03_ESTADO/SEGUIMIENTO.md`.

## Qué es

Reemplazo del SGP de Sodexo para empresas de alimentación. Abarca:

* catálogo y precios;
* recetas y minutas;
* planilla del menú por estructura y día (formato SGP);
* previsión y compras;
* almacén con kárdex valorizado y traspasos en dos pasos;
* producción, requerimientos, adicionales y devoluciones;
* inventario físico;
* cierres diario y mensual, con checklist;
* Food Cost, resultado comparado y presupuesto por rubro;
* sincronización entre sedes;
* contratos y usuarios con roles y alcance.

Plataforma: VB.NET + WinForms en Windows 10+ y PostgreSQL. Unas 20 PC; por ahora **una sola sede** (decisión D10).

## Estado

| Parte | Estado al 2026-10-06 |
|---|---|
| Etapas 1 a 7 (catálogo a cierres) | Hechas en código y pruebas |
| Etapa 8 (continuidad) | Parcial: falta el piloto real en una sede (`docs/05_OPERACION/PILOTO_ETAPA_8.md`) |
| Etapa 9 (contratos, gastos, resultado, roles propios) | Hecha; comparado con presupuesto por rubro, mes anterior y acumulado (entrega 6) |
| Siete perfiles de la operación | Hechos en código y pruebas (migraciones renumeradas V026 a V029, ver "Cambios"). Falta confirmar la línea de migraciones con las otras ramas |
| Planilla del menú en formato SGP | Hecha (`FormPlanillaMenu`, entrega 11): edición de receta, raciones y comensales |
| Traspaso en dos pasos con tránsito | Hecho (entrega 7, V033): bloquea el cierre hasta recibirlo |
| Cierre, calendario y checklist | Hechos: calendario por días y por semanas, checklist con "Ir a" |
| Usuarios | Hecho: crear, roles, reiniciar clave, editar nombre y estado (reactivar) |
| Datos reales del SGP ordenados y cargables (`datos/real/`) | Hecho: 4 158 productos, 3 203 ingredientes, 1 497 precios sin IGV, 975 productos activos, familias y 84 minutas de un ciclo propuesto. Conteo en `docs/04_ARQUITECTURA_Y_DATOS/ESTADO_BASE_DATOS.md` |
| Planificación y Abastecimiento Central | Fase 1 hecha (seguridad, matriz de acceso, roles centrales). Fases 2 a 10 según `docs/04_ARQUITECTURA_Y_DATOS/ARQUITECTURA_PLANIFICACION_CENTRAL.md` |
| Modelo objetivo del SGP (`datos/plan_real/`) | Recibido y leído; fórmulas verificadas; comparativo de tres niveles en el dominio (`Calculos.PlanVsReal`) |
| Falta del usuario | Menú del mes real, estructuras de loncheras, refrigerios y coffee break; hora de corte por defecto; días de llegada por zona |
| Interfaz WinForms | Recorrido de pantallas y pruebas de permisos ejecutados en esta máquina; 24 de 25 pantallas abren sin error de la aplicación. Limitaciones del entorno: entrada de mouse bloqueada, diálogos modales no legibles (`CLAUDE.md`, §5) |
| Demostración del menú teórico y real | `DemoTeoricoRealTests` (E2E): cifras verificadas a mano; reportes en `artifacts/demo` |

Última batería de pruebas (2026-10-06): datos 123 de 123 contra PostgreSQL; dominio 102 de 102; consultas SQL: 469 completas sin error (`herramientas/verificar_consultas_sql.py`). Migraciones en esta rama: V001 a V021 confirmadas; V026 a V033 pendientes de confirmar.

## Cambios desde 2026-10-03

* **Numeración de migraciones:** las pendientes de esta rama pasaron de V022 a V029 a **V026 a V033**, porque la base local `appsistema` ya tenía V022 a V025 de otras ramas (`feature/formatos-sgp`, `feature/planificacion-menus-matriz`). Respaldo previo en `artifacts/respaldo/`.
* **Errores reales corregidos** en la aplicación: la matriz de planificación se caía al abrirse (columnas inmovilizadas y anchos antes de tener ventana); el teórico vs real era modal y ahora es una ventana no modal; las grillas ajustaban columnas sin manejador de ventana.
* **Nuevas reglas de negocio:** traspaso en dos pasos; devolución de cocina por solicitud atendida por el almacén; adicional con motivo y aprobación; raciones operativas del chef; presupuesto por rubro en el resultado.

## Decisiones del usuario (no reabrir)

* **Moneda:** soles (PEN).
* **D01:** "cantidad × precio". Las entradas van a su costo y las salidas al promedio móvil.
* **D12:** se descarga toda la presentación; lo entregado a cocina se da por consumido.
* **Precios:** si no hay precio, no se considera (nunca 0 ni estimado).
* **Unidades (`coduni`):** 1 = KILOGRAMO, 3 = GRANO, 6 = GRAMO, 33 y 37 = PAQUETE.
* **D13, Food Cost = costo / venta:**
  * la venta sale de la estructura del menú: comensales × factor de consumo × reparto de alternativas → costo previsto;
  * venta = costo ÷ Food Cost objetivo de **48 %** (margen de 52 %).
* **Factores de consumo:**
  * son teóricos y cada operación los actualiza con su consumo real;
  * el plato caliente va casi al 100 % y los complementos entre 30 y 70 %;
  * el jugo se reparte 50/50;
  * el huevo tiene dos presentaciones: frito y a la orden.
* **Real del servicio:**
  * se carga la venta real (raciones vendidas e importe);
  * se compara contra lo teórico en raciones (preparadas, consumidas, vendidas y no vendidas), venta, costo y productos, incluidos los que salieron del almacén sin estar planificados.

* **D02 (2026-10-03):** el costo del ingrediente es el precio del producto **activo en la operación**, no el más barato. El orden para elegirlo es:
  1. el producto liberado en Catálogo, o con `liberar-productos`;
  2. si no hay ninguno, la presentación con el último ingreso al almacén de la operación;
  3. si tampoco hay, el costo queda pendiente.

  Los pedidos de compra usan ese mismo producto.
* **D03 (2026-10-03):** los precios **no incluyen IGV**; la base lo exige (V018).
* **Bulto de pedido (2026-10-03):** los sacos son individuales; si no hay múltiplo, se pide de a uno.
* **Familias (2026-10-03):** se usan las del SGP en tres niveles: familia › subfamilia › grupo.
* **Dueño y áreas (2026-10-03):**
  * el dueño del sistema es el administrador general: tiene clave personal y todos los permisos en todas las operaciones, y decide qué módulos y hasta qué nivel tiene cada persona;
  * hay dos áreas separadas: **Planificación** arma el menú con los factores teóricos, los costos del día y los pax a vender; **Abastecimiento** consolida las compras de todas las operaciones por periodo, por ejemplo un mes;
  * el sistema es para las sedes locales y los sitios remotos propios.
* **Planificación y Abastecimiento Central (2026-10-03, respuestas del usuario):**
  * el **superusuario es el usuario** (el dueño del sistema de V020, por empresa);
  * la **liberación** la hace el área de Planificación o el superusuario directamente; no se exige un revisor distinto;
  * **existe un almacén central** y las sedes se agrupan en **Costa, Sierra y Selva** (`operacion.zona`; un rol puede valer en toda su zona);
  * **"el sistema debe permitir retrasos"**: el corte del requerimiento interno es configurable por operación y no bloquea; lo tardío se acepta marcado como TARDÍO (hora, persona, motivo), y la distribución desde el central usa un tiempo de llegada por zona;
  * la liberación funciona primero en el mismo servidor; el canal remoto a otras sedes queda para la fase 3b.
* **Modelo del SGP (2026-10-03):**
  * costo **piso y techo** por factores: piso = Σ factor × ración más barata del componente; techo = Σ factor × la más cara (alerta, no bloquea);
  * el **bulto** de la requisición va en **decimales**;
  * el **registro de inventario permanente valorizado** sale en el **formato 13.1 de SUNAT**;
  * los **nutrientes** van desde el inicio; se calculan por receta cuando llegue la tabla de composición.
* **D10 (2026-10-03):** por ahora es **una sola sede**, pero puede ampliarse a varias. Se mantiene un servidor por sede con la central opcional, y el resultado consolidado de varias sedes queda para cuando se amplíe.

## Propuestas aplicadas que falta confirmar

* **Estructuras de menú y factores** en `datos/real/estructuras_menu.csv`: los propuse yo, salvo los que dio el usuario.
* **Enlace manual** en `datos/real/enlace_manual.csv`: solo la mantequilla 8 g, propuesta.

## Preguntas abiertas al usuario

1. **Commit:** ¿se confirman los cambios de las entregas 4 a 11 en un solo commit, o por etapas? Antes, separar lo que no es de esta rama (incluidos `instalar_appsistema_local*.ps1`, que no se suben con claves por defecto).
2. **Línea oficial de migraciones:** ¿V022 a V025 de `feature/*` o las de esta rama (ahora V026 a V033)? Hay que decidirlo antes de fusionar ramas.
3. **Menú del SGP:** enviar el archivo original del menú (Excel o reporte del SGP) para cargarlo a la base. No sirve el texto copiado.
4. **Cuentas de trabajo:** ¿crear chef, almacén y planificador ahora desde Usuarios y roles? Las claves las define el usuario.
5. **Nutrientes:** ningún Excel recibido trae valores. Hace falta la tabla de composición por ingrediente (energía, agua, proteínas, grasa, carbohidratos… por 100 g) y los % de aprovechamiento y cocción. Diferido por decisión del usuario.
6. **Hora de corte por defecto** del requerimiento interno y **días de llegada** desde el almacén central a Costa, Sierra y Selva.
7. **Revisión de datos reales:** `datos/real/ingredientes_por_revisar.csv` (85 ingredientes sin producto seguro: ajo molido envasado, mayonesa, ajíes molidos, panes de marca, mezcla láctea, ketchup); `datos/real/precios_atipicos.csv` (20: ¿precio de caja o de unidad?); `datos/real/productos_activos.csv` (producto activo propuesto por ingrediente).

## Próximos pasos sugeridos

La lista ordenada por prioridad está en `CLAUDE.md`, §7. En resumen:

1. Confirmar el commit y decidir la línea de migraciones (preguntas 1 y 2).
2. Cargar el menú del SGP cuando llegue el archivo (pregunta 3) y revisar la planilla con datos reales.
3. Corregir la presentación detectada en la demostración: montos sin formato en "Venta teórica", food cost con todos los decimales, plurales en los avisos.
4. Corregir la prueba de 1366x768 para que mida el diseño, no la ventana maximizada.
5. Piloto de la etapa 8 en una sede.
6. Canal de tránsito entre operaciones por la central (fase 3b), con especificación antes de implementar.
7. Integraciones (SAP, ADS, SGO): solo con especificación y entorno de prueba del cliente.

## Cargar una base con datos reales

Desde la aplicación: Administración > Carga de datos reales. Desde la consola: ver `datos/real/LEEME.md`. El orden es:

1. `importar-catalogo`
2. `importar-precios`
3. `importar-recetas --aprobar`
4. `marcar-sin-costo`
5. `importar-inventario`
6. `cargar-estructuras`
7. `cargar-ciclo AAAA-MM-DD DES ALM CEN --aprobar`

## Mensaje sugerido para abrir la nueva conversación

> Continuamos con AppSistema (repositorio iomarramos/AppSistema, rama claude/busy-mayer-9fxop6). Lee CLAUDE.md, docs/03_ESTADO/CONTINUAR.md y docs/03_ESTADO/CHECKLIST.md (entregas 1 a 11) y sigue con los pendientes de CLAUDE.md, §7. Responde en español. Antes de confirmar cambios, pregunta por el commit: hay entregas sin confirmar. No edites migraciones aplicadas; las nuevas van a continuación de V033.
