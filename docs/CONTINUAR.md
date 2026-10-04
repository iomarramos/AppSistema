# Cómo continuar (traspaso a una nueva conversación)

Actualizado: 2026-10-03, 10:40 hora de Lima (15:40 UTC).

| | |
|---|---|
| Rama | `claude/busy-mayer-9fxop6` |
| PR | #1 (borrador, CI verde, sin conflictos) |
| `develop` | en el mismo commit probado |
| Último corte | 2026-10-04: **fase 2 de Planificación y Abastecimiento Central** (planificación con versiones inmutables, V022; solo base y pruebas, sin pantallas). Antes: fase 1 (seguridad de cuatro niveles, V021, 2026-10-03) y análisis y diseño en `docs/ARQUITECTURA_PLANIFICACION_CENTRAL.md` |

El detalle de cada etapa está en `docs/SEGUIMIENTO.md`; el checklist de avance (pantallas, accesos por rol y pendientes) en `docs/CHECKLIST.md`.

## Qué es

Reemplazo del SGP de Sodexo para empresas de alimentación. Abarca:

* catálogo y precios;
* recetas y minutas;
* previsión y compras;
* almacén con kárdex valorizado;
* producción;
* inventario físico;
* cierres;
* Food Cost;
* sincronización entre sedes;
* contratos y resultado mensual.

Plataforma: VB.NET + WinForms en Windows 10+ y PostgreSQL. Unas 20 PC; falta confirmar si son una sola sede.

## Estado

| Parte | Estado |
|---|---|
| Etapas 1–7 (catálogo → cierres) | Hechas en código y pruebas |
| Etapa 8 (continuidad) | Parcial: falta el piloto real en una sede (`docs/PILOTO_ETAPA_8.md`) |
| Etapa 9 (contratos, gastos, resultado, roles propios) | Hecha |
| Venta por estructura (D13) y teórico vs real | Hecho |
| Datos reales del SGP ordenados y cargables (`datos/real/`) | Hecho: 4 158 productos (`PRD`), 3 203 ingredientes (`ING`), 1 497 precios sin IGV, 975 productos activos, familias del SGP y 84 minutas de un ciclo propuesto con costo y venta. Conteo por tabla en `docs/ESTADO_BASE_DATOS.md` |
| Planificación y Abastecimiento Central | **Fase 1 hecha** (seguridad: superusuario, módulo → pantalla → acción → alcance, matriz de acceso, roles centrales, stock y factores validados en la base). Fases 2–10 según `docs/ARQUITECTURA_PLANIFICACION_CENTRAL.md` |
| Modelo objetivo del SGP (`datos/plan_real/`) | Recibido y leído: menú teórico y plan real de ago–oct 2026, comparativo de tres niveles, requisición del 1 al 7/10 y 6 capturas de ventanas. Todas sus fórmulas verificadas; comparativo de tres niveles en el dominio (`Calculos.PlanVsReal`). Ventanas que faltan y preguntas en su `LEEME.md` |
| Falta del usuario | Menú del mes real y estructuras de loncheras, refrigerios y coffee break; hora de corte por defecto y días de llegada por zona |
| Reportes imprimibles y exportables (minuta del día, requerimiento, kárdex, inventario, stock valorizado) | Hecho: se imprimen desde el navegador o se exportan a CSV para Excel |
| Interfaz WinForms | **Se ejecuta en Windows en el CI** (job `e2e-windows`, FlaUI): acceso, permisos por rol y recorrido de todas las pantallas sin errores, con una captura de cada una (artefacto `e2e-capturas`). Falta la revisión del usuario en su PC |

Última batería de pruebas: 69 aserciones SQL, 98 de dominio, 98 de integración e instalador de punta a punta, todo verde. Migraciones V001–V021.

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

0. **Nutrientes:** ningún Excel recibido trae valores (solo la captura de la ventana "Aporte"). Hace falta la tabla de composición por ingrediente (energía, agua, proteínas, grasa, carbohidratos…, por 100 g) y los % de aprovechamiento y cocción; con eso se calcula el aporte de cada receta.
0. **Hora de corte por defecto del requerimiento interno** (se permiten retrasos: lo tardío se acepta marcado) y **días de llegada desde el almacén central a Costa, Sierra y Selva**.

1. Revisar `datos/real/ingredientes_por_revisar.csv`: 85 ingredientes de receta sin producto seguro. Los principales:
   * AJO MOLIDO ENVASADO;
   * MAYONESA;
   * ají panca y ají amarillo molidos envasados;
   * panes de marca;
   * MEZCLA LÁCTEA;
   * KETCHUP.
2. Revisar `datos/real/precios_atipicos.csv` (20): ¿el precio es de la caja o de la unidad?
3. Factores de consumo reales por operación y servicio.
4. Cereales y yogurt del desayuno: no hay recetas con precio.
5. Revisar `datos/real/productos_activos.csv`: el producto activo propuesto por ingrediente (el que tiene stock o la compra más reciente del SGP).

## Próximos pasos sugeridos

La lista completa y numerada está en `docs/CHECKLIST.md` (sección 4). En resumen:


1. Con las respuestas, actualizar:
   * `enlace_manual.csv`;
   * `python3 herramientas/ordenar_datos_reales.py`;
   * volver a cargar y a probar.
2. Probar la aplicación en una PC con Windows 10+ (pasos en `README.md`) y corregir lo que aparezca.
3. Piloto de la etapa 8 en una sede.
4. Integraciones (SAP, ADS, SGO): solo con especificación y entorno de prueba del cliente.

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

> Continuamos con AppSistema (repositorio iomarramos/AppSistema, rama claude/busy-mayer-9fxop6). Lee CLAUDE.md y docs/CONTINUAR.md y sigue con los pendientes. Mantén el repositorio actualizado con commit y push después de cada cambio.
