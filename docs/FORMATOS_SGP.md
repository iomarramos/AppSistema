# Formatos de salida: reportes del SGP y de AppSistema

Referencia: `Pantalla\Reportes Cierre` y `Pantalla\Reportes Cierre Pre Cierre` (cierre de Orcopampa del 1/10/2026 y pre-cierre del 28/09/2026).

## Adaptados en esta entrega

| Reporte del SGP | Reporte de AppSistema | Pantalla | Qué cambió |
|---|---|---|---|
| Inventario Físico Valorizado | `StockValorizado` ("Inventario fisico valorizado") | Stock > Imprimir stock | Cabecera Bodega / Toma de inventario / Familia; una tabla por familia con código, descripción, unidad, cantidad, precio (costo promedio) y total; subtotal por familia y total general. |
| Diferencias Físico vs Sistema - Valorizado | `Inventario` (hoja de diferencias) | Inventarios > Imprimir | Columnas del SGP: P.M.P., stock físico, total físico, stock sistema, total sistema, diferencia y total de la diferencia. |
| Movimiento de Stock - Sintético | `MovimientoStockSintetico` (nuevo) | Stock > Movimiento de stock... | Por familia: saldo anterior, entradas, implantación, retiradas, ajuste, salida, devolución y saldo actual, en valor. No tiene "Dif. evol. costo": cada salida ya se valoriza al promedio móvil. |
| Resumen de Salidas para Producción Consolidado | `SalidasConsolidadas` (nuevo, `devoluciones: false`) | Stock > Salidas a produccion... | Por producto: código, descripción, cantidad, unidad y total. |
| Resumen de Devolución de Producción a Bodega Consolidado | `SalidasConsolidadas` (nuevo, `devoluciones: true`) | Stock > Devoluciones a bodega... | Igual que el anterior, con las devoluciones. |
| Resultados Operacionales Mensual o A13 | `ResultadoA13` (nuevo) | Resultados > Imprimir A13... | Ventas; consumo realizado (inventario inicial + recepciones + implantación + traspasos recibidos − traspasos enviados − inventario final); porcentaje de consumo, días de stock, costo diario de servicios, diferencia (a − b), total de gastos y utilidad operacional. |

## Sin cambios (válidos o de otro uso)

* Registro de inventario permanente valorizado (formato 13.1 SUNAT): ya sigue la tabla 13.1; el `.xls` del SGP no se comparó columna por columna.
* Kárdex: se conserva como detalle por producto; el resumen del SGP es el movimiento sintético.

## Pendientes (no están en esta entrega)

| Reporte del SGP | Motivo |
|---|---|
| Food Cost (Alimento) | Revisar `ServicioCierres` contra el formato diario por servicio (ración vendida, valor bandeja, costo bandeja, food cost). |
| Traspasos (entrada CD, entrada y salida entre contratos) | AppSistema tiene los movimientos (`traspaso_entrada` / `traspaso_salida`), pero no un listado con número de documento, origen y folio. |
| Consumo Alternativo | Pendiente de definir el origen de la diferencia. |
| Boleta de ajustes R-AL-15-2 y formato de explicación de ajustes R-AI-15-2 | Requieren un flujo de ajustes con justificación; no existe todavía. |
| Listado para toma de inventario / TOMA_INVENTARIO.xls | La hoja de conteo de AppSistema tiene otras columnas; falta igualarla. |
| Requisición (Formato de Requisición Detallado) | Se compara con el requerimiento por minuta en la siguiente revisión. |
| Comparativo de tres niveles | Los datos ya están en `sgp_comparativo_*` (V023); falta mostrarlos en la pantalla de comparativos. |

## Diferencias conocidas

* Días de stock del A13: el SGP muestra 21 para el inventario final de 205 645 y consumo de 304 651. Con el mes de 30 días sale 20; con 31 días sale 21. Falta confirmar con el usuario qué base usa el SGP.

## Días de stock por contrato (V024)

* Cada operación (contrato) tiene sus **días base** para medir el stock: 20, 21, 31… (por defecto 30). Se fija en Administración > Operaciones y almacenes > **Días base de stock...**.
* Días de stock = inventario final ÷ (consumo del periodo ÷ días base). El A13 los muestra con el criterio del contrato y la nota indica el valor usado.
* Con 31 días el A13 del SGP da 21 (205 645 ÷ (304 651 ÷ 31) = 20,9 → 21), así que el SGP de Orcopampa parece usar 31. Se confirma en el contrato, no en el código.

## Vista previa de los reportes (estilo SGP)

* Cada reporte abre una **vista previa** con barra de impresión, exportar a Excel (CSV), páginas (anterior/siguiente, "Pagina n de N") y zoom (ajustar, acercar, alejar).
* Hoja: título, datos de cabecera en pares etiqueta-valor, tablas con encabezado amarillo, cantidades con tres decimales y dinero con dos, totales en negrita con línea superior, notas y firmas. Pie con número de página y fecha de generación.
* Las tablas que continúan en otra hoja repiten su encabezado.

## Para aplicar en la base de la sede

1. Respaldar: `AppSistema.Instalador.exe respaldar D:\respaldos\antes_V024.dump`.
2. Aplicar la migración V024: `AppSistema.Instalador.exe actualizar D:\respaldos\antes_V024.dump` (migra y comprueba que saldos e historia no cambian). Alternativa sin comprobación: `AppSistema.Instalador.exe migrar`.
3. Fijar los días base de cada contrato en Administración > Operaciones y almacenes.

## Adaptados en la última entrega (orientación y reportes pendientes)

| Reporte | Cómo quedó | Dónde |
|---|---|---|
| Minuta del día (menú) | Hoja horizontal | Menús > Minutas > Imprimir |
| Food cost (alimento) | Nuevo, horizontal: por minuta con raciones preparadas y vendidas, venta del día, valor bandeja, costo del día, costo bandeja y food cost | Cierres > Imprimir food cost... |
| Comparativo de tres niveles | Nuevo, horizontal: teórico (raciones, costo total, bandeja), realizado y desviación | Cierres > Imprimir tres niveles... |
| Resumen de traspasos | Nuevo: entradas y salidas del periodo, con número de documento, fecha, bodega y total | Stock > Resumen de traspasos... |
| Listado para toma de inventario | Título del formato del SGP y columna de observación en blanco | Inventarios > Imprimir hoja |

Todas las vistas se pueden girar entre vertical y horizontal con el botón "Girar hoja" de la vista previa.

## Pendiente

* **Boleta de ajustes R-AL-15-2 y formato de explicación R-AI-15-2.** Necesitan un campo de explicación por cada ajuste de inventario (motivo, justificación y responsable). AppSistema no lo guarda todavía: se agrega primero en la base y después se imprimen los dos formatos.
* **Comparativo de tres niveles contra el plan real del SGP.** El plan real importado está en `sgp_plan_*` (V023), pero el comparativo de AppSistema todavía no lo cruza por día.

## Formatos de ajuste y menú (última entrega)

* **Boleta de ajuste R-AL-15-2** (Stock > Boleta de ajuste R-AL...): los ajustes de entrada y de salida del periodo, con código, descripción, unidad, cantidad, costo y valor. La columna **Explicación del ajuste** queda en blanco con espacio para escribir a mano. No se guarda en el sistema: es un formato que se imprime y se llena.
* **Explicación de ajustes R-AI-15-2** (Inventarios > Explicación de ajustes R-AI...): las diferencias físico vs sistema del inventario, de mayor a menor valor, con ajuste (+ o −), porcentaje y acumulado, y la columna **Motivo del ajuste** para llenar a mano. Hoja horizontal.
* **Menú teórico (planificación)** (Cierres > Menú teórico (mes)...): lo que carga planificación en el sistema, por servicio. Una columna por día; por estructura, código de la receta, raciones y costo por ración; al pie, el costo de la minuta del día y el costo total del servicio en el mes. Hoja horizontal.
* **Menú real (chef)** (Cierres > Menú real (mes)...): las raciones preparadas que registra el chef en el cierre diario, con el mismo formato. Si el día no tiene registro, la celda queda vacía (no se rellena con el teórico).
* Los códigos de receta (no los nombres) van en las celdas para que quepan los 31 días; el nombre completo de cada receta está en la minuta del día.
* No se necesitó migración: los formatos se generan con los datos existentes.

## Plan de producción del chef (V025)

* El chef cambia **las raciones a producir** de cada plato de una minuta: cualquier día desde **3 días atrás** en adelante, mientras el día **no esté cerrado**. La regla la aplica la base (fecha de Lima, día cerrado, minuta aprobada, permiso PRODUCCION_EDITAR).
* Cada cambio queda en `produccion_plan_cambio` (anterior, nuevo, usuario, fecha de producción), de solo lectura.
* Si no hay cambio, las raciones a producir son las de la minuta.

## Registro de inventario permanente valorizado (formato 13.1)

* Cada entrada y salida lleva su número de documento: la recepción con su comprobante del proveedor (tipo, serie y número); los ajustes, traspasos, salidas y devoluciones con el número del documento de stock.

## Cambios de tabla para aplicar en la base de la sede

| Migración | Cambio | Tipo |
|---|---|---|
| V024 | `operacion.dias_stock_base` (1 a 31, por defecto 30) | columna nueva |
| V025 | `produccion_plan` (raciones a producir por plato de minuta) | tabla nueva |
| V025 | `produccion_plan_cambio` (historial de solo lectura) | tabla nueva |

No se modifica ninguna migración ya aplicada. Para aplicar: respaldar y luego `AppSistema.Instalador.exe actualizar <respaldo>` (o `migrar`). No aplicar el SQL a mano: el migrador guarda la huella de cada archivo y un cambio manual la desincroniza.

## Menú de reportes y producción del chef (última entrega)

* **Reportes (SGP y plan)** — menú *Cierres y control > Reportes (SGP y plan)*: elegir el reporte y el periodo (rango o mes) y abrir la vista previa. Incluye: requisición detallada por rango (SGP), salidas y devoluciones de producción por servicio, resumen de traspasos, boleta R-AL, frecuencia de la planificación teórica, costo piso y techo, menú teórico, menú real, food cost, comparativo de tres niveles y A13.
* **Producción del chef** — menú *Menús > Produccion del chef (raciones a producir)*: lista los platos desde 3 días atrás (fecha de Lima); el chef cambia las raciones a producir y guarda. Los días cerrados no se editan y la base registra cada cambio.
* Pendiente sin datos: aporte nutricional (los archivos no traen nutrientes) y consumo alternativo (falta definir su origen). La salida y devolución por estructura (no solo por servicio) queda pendiente.
