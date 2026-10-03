# Estado de la base de datos con los datos reales

Actualizado: 2026-10-03. Es el resultado de cargar todo `datos/real/` en una base nueva con los pasos de `datos/real/LEEME.md` (migraciones V001–V019, 71 tablas).

Los productos usan códigos `PRDnnnnn` y los ingredientes `INGnnnnn`; ya no hay códigos con "SGP". El proveedor de los precios de referencia es `REF`.

## Qué hay

| Área | Contenido |
|---|---|
| Empresa y acceso | 1 empresa, 1 operación, 1 almacén; 5 roles base, 26 permisos |
| Unidades | KG, L, UND |
| Categorías | 14 familias, 26 subfamilias y 107 grupos (familia › subfamilia › grupo del SGP) |
| Ingredientes (producto base) | 3 203: 939 en KG, 253 en L y 2 011 en UND. 197 no tienen categoría |
| Productos (presentaciones `PRD`) | 4 158, cada uno con su contenido en la unidad del ingrediente; 1 516 con familia › subfamilia › grupo |
| Bulto de compra | 4 158 empaques de 1 unidad (sacos individuales; sin múltiplo se pide de a uno) |
| Precios (sin IGV) | 1 497 productos con precio, que cubren 975 ingredientes |
| Producto activo en la operación | 975 ingredientes, con el producto que se costea |
| Insumos sin costo | 1 (agua para receta) |
| Recetas | 946 aprobadas con 8 873 líneas de ingredientes; 643 con todos sus ingredientes con precio |
| Recetas por componente | Fondo 201, guarnición 180, entrada 105, bebida caliente 86, sopa 85, postre 76, sándwich 51, jugo 41, refresco 26, pan 22, complemento 19, salsa y ají 19, huevo 12, otros 10, fruta 9, cereal 2, untable 2 |
| Servicios y régimen | Desayuno, Almuerzo y Cena; régimen General |
| Estructura del servicio | 22 componentes con su factor de consumo: Desayuno 10, Almuerzo 7, Cena 5 |
| Minutas (ciclo propuesto) | 84 (28 días × 3 servicios, del 05/10 al 01/11/2026) con 700 platos, todas aprobadas con costo y venta |
| Inventario inicial | 379 productos, S/ 307 498,45 |
| Clientes, contratos y pedidos | 0 (se crean al operar) |

## Qué falta

| Falta | Detalle |
|---|---|
| **Menú del mes real** | Las 84 minutas son un ciclo propuesto a partir de las recetas. Falta el menú que la operación sirve de verdad. |
| **Estructura de servicios completa** | Hoy hay Desayuno, Almuerzo y Cena, con estructuras propuestas. Faltan **loncheras, refrigerios y coffee break**, y confirmar los componentes y factores de los tres actuales. El usuario los va a presentar. |
| Ingredientes de receta sin producto seguro | 85 (`datos/real/ingredientes_por_revisar.csv`). Por eso 303 de las 946 recetas no tienen costo completo. |
| Precios atípicos | 20 (`datos/real/precios_atipicos.csv`), entre ellos la margarina de 190 g. |
| Ingredientes sin categoría | 197. |
| Cereales y yogurt del desayuno | Ninguna receta con precio. |
| Clientes, contratos, gastos y presupuesto | Se cargan al operar o cuando el usuario los entregue. |
