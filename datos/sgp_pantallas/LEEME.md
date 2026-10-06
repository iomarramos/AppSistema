# Pantallas, manual y reportes del SGP (recibido el 2026-10-06)

Material enviado por el usuario con la instrucción de actualizar la base con "estas tablas" y revisar los reportes.
Contrato de las capturas: **PE017401 - ORCOPAMPA FOALI** (SGP Perú v4.08.0059, SQL Server), octubre de 2026.

## Contenido

| Ruta | Qué es |
|---|---|
| `origen/Captura de pantalla *.png` (75) | Pantallas del SGP, tal como llegaron. Fechas del 2026-08-18 al 2026-10-05. |
| `origen/manual.pdf` | Manual SGP Local para Operaciones, V006 (92 páginas, A5). |
| `origen/Reportes Cierre Pre Cierre/` | Reportes del pre-cierre (27/09/2026) en Excel y PDF: Inventario Físico Valorizado, Diferencias Físico vs Sistema, Consumo Alternativo, Listado para toma de inventario, TOMA_INVENTARIO.xls, Boleta de Ajustes R-AL-15-2 y Formato de Explicación de Ajustes R-AI-15-2. |
| `indice_capturas.csv` | Número de la captura (orden cronológico) → archivo original → pantalla. |
| `transcripcion/capturas_*.md` | Transcripción de cada captura (5 archivos de 15): campos, columnas con ejemplos, botones, leyendas, totales y las tablas que cada pantalla necesita. |

Los nombres de archivo con tilde (`Físico`, `Explicación`) llegaron deformados dentro del ZIP (`F#U00edsico`); aquí están corregidos.

## Cómo se transcribió

Las transcripciones las hizo un modelo mirando cada imagen. Lo que no se lee dice `(ilegible)` y lo que se deduce dice
`(inferido)`. Se contrastaron a mano contra la imagen las capturas 04, 09, 19 y 75 (valores de la grilla, totales y barra de
estado) y coinciden. **Antes de basar una regla en un valor concreto, mirar la imagen.**

## Qué se hizo con esto

* `docs/TABLAS_SGP_VS_BASE.md`: cada tabla que muestran las pantallas contra lo que ya tiene la base, qué se agregó y qué queda por confirmar.
* `database/postgresql/migraciones/V026__tablas_sgp_pantallas.sql`: 14 tablas y varias columnas nuevas (receta por régimen, nutrientes, control de raciones, venta del día, cafetería, traspasos).
* Reporte **Consumo alternativo** (Inventarios > Consumo alternativo...): ver `docs/FORMATOS_SGP.md`.

## Hallazgo sobre "Consumo Alternativo"

El menú Stock del SGP lo rotula "Consumo Alternativo (ex ajuste de inventario)". Comparado con el reporte de diferencias de la
misma toma (27/09/2026), son **exactamente los productos con diferencia distinta de cero** (43 filas), en orden alfabético,
con la misma diferencia, el mismo precio y el mismo total (−26,388276 en ambos).
