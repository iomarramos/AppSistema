# Inventario inicial

| Archivo | Contenido |
|---|---|
| `origen/INVENTARIO_PRODUCTOS.xlsx` | Archivo tal como se recibió: 379 productos con `Descripción, Unidad, Stock, Precio`. El stock está en la presentación del SGP (latas, bidones, kg…) y el precio es el de una presentación. |
| `inventario_inicial.csv` | Lo que importa el sistema (generado con `python3 herramientas/convertir_inventario.py`). Las columnas son `variante_codigo;descripcion;presentacion;stock_envases;precio_envase` más columnas informativas. |
| `observaciones_inventario.csv` | Problemas encontrados. Está vacío: los 379 productos existen en el catálogo y su unidad coincide con la presentación. |

## Resultado

| | |
|---|---|
| Líneas | 379 |
| Valor total | **S/ 307 498,45** (exacto: 307 498,452459) |

Ejemplo: `ACEITE VEGETAL CIELO 5 LT`, BID, stock 42, precio 34,12 se registra así:

* cantidad: 42 × 5 L = **210 L** del ingrediente ACEITE VEGETAL (variante SGP00010);
* valor: 42 × 34,12 = **S/ 1 433,04**.

## Cómo se registra

* Es un **documento de apertura** confirmado en el almacén: todo o nada.
* **Cantidad:** envases × contenido del envase según el catálogo. El sistema no usa la cantidad del archivo.
* **Valor:** envases × precio del envase, exacto, sin recalcular desde un costo redondeado.
* **Solo una vez:** se admite únicamente en un almacén **sin movimientos**. Repetirlo no duplica el stock.
* **Moneda:** soles (confirmado por el usuario el 03/10/2026).

## Cargar

Requisito: el catálogo cargado (`datos/enlace/catalogo_por_ingrediente.csv`).

Se puede cargar desde:

* **la aplicación:** Almacén > Stock e inventario inicial > Inventario inicial…
* **la consola:** `AppSistema.Instalador importar-inventario datos/inventario/inventario_inicial.csv` (pide almacén y fecha).
