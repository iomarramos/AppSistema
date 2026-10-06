# Planificación teórica, plan real, realizado y requisición (SGP)

Recibido del usuario el 2026-10-03 ("a este modelo tengo que llegar"). Es la operación **ORCOPAMPA FOALI (PE017401)**, de agosto a octubre de 2026. Los archivos originales están en `origen/` tal como llegaron.

| Archivo de origen | Contenido |
|---|---|
| `MENU_REAL_Y__TEORICO.xlsx` | Menú por mes en cuadrícula: componente de la estructura × día, con receta, raciones y costo. Tiene la **planificación teórica** y el **plan real** de desayuno, almuerzo y cena, en bloques de agosto, septiembre y octubre. La hoja **PANTALLAS** trae 6 capturas de ventanas del SGP |
| `Costo_Plan._Teorico_-_Plan._Real_-_Realizado_Alimentacion.xlsx` | Reporte de agosto con **los tres niveles por día**: teórico, plan real y realizado. Por cada uno da costo bandeja, raciones y costo total, más las desviaciones. Cubre 10 servicios en 4 regímenes |
| `MENU_REAL_CENA.xlsx` | **Plan real de la cena de octubre** (llegó aparte el 2026-10-03, porque `Hoja7` repetía agosto) |
| `REQUISICION.xlsx` | "Formato de Requisición Detallado" del 01 al 07/10/2026. Por régimen, servicio, día, componente y receta, con sus raciones, lista cada producto: cantidad bruta por ración, cantidad en bulto, cantidad a despachar y la preparación. Las columnas de cantidad real, extra y devolución quedan en blanco para llenarlas a mano |

Se convierte con:

```
python3 herramientas/convertir_plan_real.py
```

## Archivos generados

| Archivo | Qué es |
|---|---|
| `menu_planificado.csv` | Una fila por plato con estos campos: nivel (TEORICO o REAL), servicio, fecha, orden y componente, código SGP de la receta, receta, raciones, porcentaje (plan real), costo por ración y marca "R". 11 046 platos |
| `menu_dias.csv` | Por nivel, servicio y día: costo minuta día y comensales. 552 días-servicio: 92 por servicio y nivel, de agosto a octubre completos |
| `costo_piso_techo.csv` | **Costo piso y techo por factores**, por nivel, servicio y mes (ver abajo) |
| `comparativo_costos.csv` | Los tres niveles por día y servicio, con sus desviaciones. 465 filas |
| `comparativo_totales.csv` | Total de cada servicio y total general del mes |
| `requisicion.csv` | Una fila por producto, receta, servicio y día. 2 778 líneas, 230 recetas y 10 servicios |
| `preparacion_recetas.csv` | Pasos de preparación de cada receta, tal como los imprime la requisición |
| `codigos_sgp_productos.csv` | Código SGP del producto (por ejemplo, 10101036994 = ACEITE VEGETAL CIELO 5 LT) enlazado por descripción al código `PRD` del catálogo cargado. **256 de 256** |
| `codigos_sgp_recetas.csv` | Código SGP de la receta (por ejemplo, 276 = PUL - CHANFAINITA) enlazado por nombre a las recetas cargadas. **553 de 628**; las demás, con la columna vacía, no están entre las recetas recibidas |
| `validacion.txt` | Resumen y verificaciones (abajo) |
| `pantallas_sgp/` | Las 6 capturas de la hoja PANTALLAS |

## Verificaciones: todas cuadran

| Regla del SGP | Resultado |
|---|---|
| Teórico: costo minuta día = Σ(raciones × costo por ración) ÷ comensales | 276 de 276 días |
| Plan real: Por.(%) = raciones ÷ comensales. Es el **factor de consumo** del componente | 5 526 de 5 526 platos |
| Plan real: costo minuta día con la misma fórmula | 276 de 276 días |
| Comparativo: costo bandeja = costo total ÷ raciones | 465 de 465 |
| Comparativo: desviación del plan = costo bandeja real − teórico | 465 de 465 |
| Comparativo: desviación del realizado = costo bandeja realizado − real (si el día no tiene realizado, el SGP muestra 0) | 465 de 465 |
| El comparativo coincide con el costo minuta día de los menús, tanto el teórico como el real | 192 de 192 y 192 de 192 |
| Requisición: despacho = cantidad bruta por ración × raciones (la cantidad por ración se imprime con 4 decimales) | 2 778 de 2 778 |
| Requisición: raciones = las del plan real del mismo servicio, día y receta | 380 de 380 |
| Cada día queda dentro de su costo piso y techo por factores | 552 de 552 |

Ejemplo, desayuno del 1/08/2026: los 18 platos teóricos suman **S/ 1 077,47** para 250 comensales, o sea **S/ 4,31** por bandeja. El plan real da S/ 4,90 y el realizado S/ 3,38. Las desviaciones son +0,59 y −1,52. Las pruebas `PlanVsRealTests` del dominio usan estas cifras.

## Observaciones

* **La hoja `Hoja7` no tiene nombre.** Por su estructura de 25 componentes es el **plan real de la cena**. Su tercer bloque repite agosto y se omite; **octubre llegó en `MENU_REAL_CENA.xlsx`**.
* Los **comensales** del plan real traen dos valores iguales (por ejemplo, 480 y 480).
* En el plan real, el costo por ración tiene 6 decimales y difiere un poco del teórico; por ejemplo, el arroz cuesta 0,525618 contra 0,53. El plan real se costea con el precio del día.
* La **requisición** da el bulto **en decimales**: 0,14 bidones de 5 L para despachar 0,7 L. **Decisión del usuario: el bulto se imprime en decimales**, como en el SGP. El requerimiento de AppSistema ya lo muestra con la presentación activa.
* **Servicios** que aparecen además de desayuno, almuerzo y cena: Lonchera simple, Lonchera bajada, Rancho 1 y 2, Servicio venta directa, Consumo fijo FOOD (insumos fijos por comensal) y Consumo fijo TGM.
* **Regímenes:** 1 (desayuno), 2 (loncheras, ranchos y venta directa), 3 (almuerzo y cena) y General (consumos fijos).

## Decisiones del usuario (2026-10-03)

| Tema | Decisión | Aplicado |
|---|---|---|
| Cena real de octubre | Enviada en `MENU_REAL_CENA.xlsx` | Convertida y verificada |
| Costo piso y techo | "Va de acuerdo a los factores; revisar en las hojas internas". El reporte trae las etiquetas vacías, así que se calcula: por servicio y mes, factor del componente = Σ raciones ÷ Σ comensales; **piso = Σ factor × la ración más barata** del componente; **techo = Σ factor × la más cara**. Todos los días quedan dentro | `costo_piso_techo.csv` y `Calculos.PlanVsReal.BandaCosto` (alerta, no bloquea) |
| Bulto en la requisición | En decimales | Requerimiento imprimible: columnas presentación activa, bulto y envase |
| Registro de inventario permanente valorizado | Generarlo en el **formato 13.1 de SUNAT** | Almacén > Stock > "Registro SUNAT 13.1..." |
| Nutrientes | Desde el inicio, con la data de los Excel; si falta en las recetas, calcularlo | **Ningún archivo recibido trae valores de nutrientes**; solo aparecen en la captura de la ventana "Aporte". Falta la tabla de composición por ingrediente (ver pendientes) |

Ejemplo de banda (plan real): desayuno de octubre, piso S/ 2,24, medio S/ 3,51 y techo S/ 5,88. Los días van de S/ 2,76 a S/ 4,53.

## Ventanas del SGP comparadas con AppSistema

| Ventana del SGP (`pantallas_sgp/`) | Qué hace | En AppSistema hoy | Falta |
|---|---|---|---|
| 1 Planificación Teórica | Mes en cuadrícula: componente × día, con receta, raciones y costo por plato. Arriba, el costo minuta día. Colores: estructura (verde), celda bloqueada (rojo), habilitada (amarillo). Abajo, el total del mes (materia prima, estructura fija, total, raciones, costo bandeja), el día y el acumulado, planificado contra realizado. Nota: "las raciones deben incluir las del personal" | Minutas por día, en lista | **Cuadrícula mensual** (fase 2), celdas bloqueadas o habilitadas, estructura fija y acumulado del mes |
| 2 Aporte Planificación Teórica | Por receta: bruto, servida, neta, agua, energía en kcal y kJ, y otros nutrientes | No hay nutrientes | Tabla de composición de alimentos (dato que falta) |
| 3 Receta | Detalle patrón, local y **por régimen**; métodos de preparación; grupo vulnerable. Por ingrediente: C. bruta, U.M., **% aprovechamiento** y **% cocción**. Totales: C. bruta, C. servida, G. neto y costo | Receta versionada con cantidad bruta e instrucciones | Receta por régimen, % de aprovechamiento y de cocción, peso servido y neto, nutrientes |
| 4 Frecuencia Planificación Teórica | Cuántas veces sale cada receta en el mes, en qué días, con su costo; total de recetas y costo promedio diario | No | **Reporte de frecuencia** (sale de las minutas; se puede hacer ya) |
| 5 Informes > Stock | Posición de stock, movimiento, consumo alternativo, cartola de inventario y su detalle, productos sin movimiento y **registro de inventario permanente valorizado** | Stock, kárdex, inventario físico y **registro SUNAT 13.1** | Productos sin movimiento y cartola |
| 6 Salida y Devolución de Producción | Formatos de requisición por servicio, por sector y por estructura (detallado o resumido); resumen de salidas, de devoluciones y salidas menos devoluciones | Requerimiento imprimible por minuta | **Requisición por rango de fechas** agrupada por servicio, componente y receta, con preparación y columnas para lo real, lo extra y la devolución |

## Cómo encaja con Planificación y Abastecimiento Central

* **Planificación teórica** = nivel Teórico (fase 2).
* **Plan real** = plan operativo (minuta con factores). El "Por.(%)" del SGP es el factor de consumo de AppSistema.
* **Realizado** = producción y consumo real (fase 6).
* El reporte de tres niveles es el comparativo de la fase 6. La fórmula ya está en `Calculos.PlanVsReal`, con pruebas sobre estas cifras.
