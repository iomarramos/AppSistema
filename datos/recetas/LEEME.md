# Recetas del SGP

| Archivo | Contenido |
|---|---|
| `origen/RECETAS_SGP_REVISADAS_DEL_TOTAL_3.xlsx` | Fichas técnicas **revisadas**: 418 fichas en 19 hojas (una por categoría). Cada ficha tiene ingredientes con cantidad por ración, unidad, técnica y preparación. Archivo tal como se recibió. |
| `origen/Receta_-_Receton.csv` | Exportación del SGP con el formato `receta;ingrediente;unidad;cantidad por ración` (Latin-1). Son 27 096 líneas y 624 recetas; cada receta aparece en varios bloques repetidos. Archivo tal como se recibió. |
| `recetas_normalizadas.csv` | **Lo que importa el sistema:** 946 recetas y 8 874 líneas (generado). |
| `ingredientes.csv` | Los 412 ingredientes, con su unidad, en cuántas recetas aparecen y si coinciden con un producto del listado SGP (exacto o candidatos). |
| `observaciones_recetas.csv` | Todo lo que se corrigió o conviene revisar: 162 observaciones. |

Regenerar los archivos derivados: `python3 herramientas/convertir_recetas_sgp.py` (requiere `pip install openpyxl`).

## Cómo se unieron las dos fuentes

1. **Las fichas revisadas tienen prioridad.**
   * Son 417 recetas; una ficha estaba duplicada.
   * Código `Fnnnnn`, con el número de la ficha.
   * La categoría es el nombre de la hoja.
   * La preparación pasa a las instrucciones de la receta y la técnica a cada ingrediente.
2. **Del Recetón** se agregan las 529 recetas que no tienen ficha.
   * Código `RTnnnn`, en orden alfabético.
   * Categoría "RECETON SGP".
3. **Recetas que están en ambas fuentes:** son 95. En 93 de ellas el Recetón difiere de la ficha y se usa la ficha. Las diferencias quedan anotadas con el tipo `FICHA_DIFIERE_DE_RECETON`.
4. **Rendimiento:** 1 ración. Las cantidades del SGP son por ración, y la minuta las multiplica por las raciones planificadas.

## Correcciones automáticas (todas en `observaciones_recetas.csv`)

| Tipo | Casos | Qué se hizo |
|---|---|---|
| RECETON_VARIAS_VERSIONES | 36 | La receta tenía dos versiones distintas en sus bloques. Se toma la más frecuente; en empate, la última del archivo. |
| COLUMNAS_INVERTIDAS | 3 | En LIMONADA, cantidad y unidad estaban intercambiadas. |
| UNIDAD_DEDUCIDA / UNIDAD_INVALIDA | 8 | La unidad estaba vacía o era PQT. Se usa la unidad habitual del ingrediente; si no había ninguna, UND. |
| INGREDIENTE_REPETIDO | 3 | El mismo ingrediente aparecía dos veces en una ficha. Se suman las cantidades. |
| INGREDIENTE_VARIAS_UNIDADES | 5 | Por ejemplo, ANIS FILTRANTE aparece en UND y en KG. Un producto tiene una sola unidad, así que el uso minoritario pasa a `ANIS FILTRANTE (KG)`, que es otro ingrediente. |
| CODIGO_EN_NOMBRE | 4 | El nombre traía el código SGP (p. ej. `1.11.4.56.0010 JUGO DE YACON`). Se quitó del nombre. |

## Cargar en el sistema

* **Aplicación:** Menús > Importar recetas > elegir `recetas_normalizadas.csv`.
* **Consola:**

  ```
  AppSistema.Instalador importar-recetas recetas_normalizadas.csv --aprobar
  ```

Qué hace la carga:

* Cada ingrediente es un **producto base** en su unidad (KG, L o UND):
  * si ya existe un producto con la misma descripción y unidad, se reutiliza; con el listado SGP cargado se reutilizan 15;
  * si no, se crea con código `INGnnnnn` y categoría INGREDIENTE.
* Sin `--aprobar`, las recetas quedan en borrador para revisarlas.
* Es todo o nada y repetirla no duplica nada.
* Si una receta ya existe con otros ingredientes, es un error: para cambiarla se crea una nueva versión desde Recetas.

## Unir ingredientes con productos comprables (resuelto con `datos/enlace/`)

**Actualización:** el enlace llegó en `PRODUCTO_INGREDIENTE.csv`. Ver `datos/enlace/LEEME.md`; para cargar, use `datos/enlace/recetas_enlazadas.csv`. Lo que sigue describe el problema original.

Las recetas usan **ingredientes genéricos** como ACEITE VEGETAL o SAL DE COCINA. El listado del SGP tiene **productos comerciales** como ACEITE VEGETAL CIELO 5 LT o ACEITE VEGETAL PRIMOR 5 LT. Solo 15 ingredientes coinciden exactamente con un producto.

Mientras no se unan:

* **Necesidades:** ya funcionan por ingrediente. Por ejemplo, 150 raciones de ARROZ BLANCO → 18 kg de ARROZ EXTRA.
* **Costo de la receta:** sale "pendiente", porque el ingrediente no tiene presentaciones con precio.
* **Compras (etapa 3):** no puede pasar del ingrediente al producto que se pide.

**Propuesta.** En el modelo del sistema, el ingrediente es el producto base y los productos del SGP son sus variantes (marcas y presentaciones). `ingredientes.csv` trae en `candidatos_sgp` hasta 5 productos del SGP que empiezan con las mismas palabras. Se necesita una tabla revisada `ingrediente → productos SGP` para enlazarlos.
