# Enlace producto SGP → ingrediente

`origen/PRODUCTO_INGREDIENTE.csv` es el archivo tal como se recibió (Windows-1252). Tiene 4 560 filas con las columnas `Producto;Ingrediente;Categoria`, que relacionan 4 510 productos con 2 935 ingredientes.

## Modelo resultante

| En el SGP | En el sistema | Ejemplo |
|---|---|---|
| Ingrediente | **Producto base** (código `INGnnnnn`, unidad KG/L/UND) | ACEITE VEGETAL (L) |
| Producto comercial | **Variante** del ingrediente (código `SGPnnnnn`) con contenido = `pro_facing` | ACEITE VEGETAL CIELO 5 LT = 5 L |
| Presentación (`pro_coduni`) | **Empaque de compra**, unidad mínima de pedido (1 envase, mínimo 1, múltiplo 1) | BIDON |

Las recetas usan el ingrediente. Al costear se toma el precio de cualquiera de sus productos comerciales (regla provisional D02). Al comprar (etapa 3) se elegirá el producto.

## Archivos generados (`python3 herramientas/enlazar_sgp.py`)

| Archivo | Contenido |
|---|---|
| `catalogo_por_ingrediente.csv` | 3 133 productos base con los 4 158 productos SGP como variantes. Formato del importador de catálogo. |
| `recetas_enlazadas.csv` | Las 946 recetas con el ingrediente del enlace. |
| `ingredientes_sin_enlace.csv` | 102 ingredientes de receta (1 270 líneas) que no se pudieron enlazar. La mitad de esas líneas son AGUA PARA RECETA. |
| `observaciones_enlace.csv` | Decisiones automáticas y conflictos, para revisar. |

### Cómo se enlaza un ingrediente de receta

Se prueban estas opciones, en orden:

1. Es un ingrediente del archivo. Caso: 235.
2. Es un **producto** del archivo, y se usa su ingrediente. Caso: 68; por ejemplo, PEREJIL LIZO REFRIGERADO → PEREJIL.
3. Coincide con la otra terminación: REFRIGERADA/REFRIGERADO, CONGELADA/CONGELADO o ENTERA/ENTERO. Caso: 7.

### Observaciones

| Tipo | Casos | Qué se hizo |
|---|---|---|
| PRODUCTO_SGP_SIN_INGREDIENTE | 221 | El producto no está en el archivo. Queda como su propio ingrediente. |
| UNIDAD_DISTINTA_AL_INGREDIENTE | 72 | Por ejemplo, `CAJA CHICA - ACEITE VEGETAL CIL X 5 LT` está en UND con factor 1, pero ACEITE VEGETAL es L. Queda bajo `ACEITE VEGETAL (UND)` hasta definir su factor en litros. |
| PRODUCTO_CON_DOS_INGREDIENTES | 14 | El mismo producto estaba en dos filas con distinto ingrediente. Se usa el primero. |
| INGREDIENTES_UNIDOS | 1 | Dos ingredientes de una receta enlazan al mismo. Se suman sus cantidades. |

## Orden de carga en una sede

1. `AppSistema.Instalador importar-catalogo datos/enlace/catalogo_por_ingrediente.csv` (crea KG, L y UND si faltan).
2. `AppSistema.Instalador importar-recetas datos/enlace/recetas_enlazadas.csv --aprobar`.

El paso 2 reutiliza los ingredientes del paso 1, buscándolos por descripción y unidad: son 287. Además crea 113 ingredientes que no tienen producto comprable, como AGUA PARA RECETA, BASE CRIOLLA o AJO MOLIDO ENVASADO.

Este flujo **reemplaza** a `importar-sgp`, que creaba un producto base por cada producto SGP. En una base nueva use solo este flujo.
