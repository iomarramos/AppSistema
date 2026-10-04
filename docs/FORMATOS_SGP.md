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
