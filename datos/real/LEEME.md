# Datos reales ordenados

Juego de datos listo para cargar, armado con **todo lo recibido del SGP**:

* inventario con cantidades y precios (`datos/inventario/`);
* listado de productos con precios y categorías (`datos/sgp/`);
* enlace producto–ingrediente (`datos/enlace/`);
* presentaciones con su contenido en KG, L o UND (`datos/sgp/`);
* recetas con gramaje por ración (`datos/recetas/`).

Se regenera con:

```
python3 herramientas/ordenar_datos_reales.py
```

## Archivos

| Archivo | Qué es |
|---|---|
| `precios_sgp.csv` | Precio de cada presentación (variante SGP). Si la presentación está en el **inventario**, se usa ese precio, que es el vigente al 01/10/2026. Si no, el **último precio de compra** del SGP con su fecha. Sin precio no se inventa (regla del usuario). |
| `precios_atipicos.csv` | Precios apartados para revisar porque son 4 veces mayores o menores que el resto de presentaciones del mismo ingrediente. Ejemplo: guantes con el precio de la caja como si fuera una unidad. No se cargan. |
| `enlace_manual.csv` | **Se edita a mano.** Enlaces confirmados entre un ingrediente de receta y un ingrediente del catálogo. Mandan sobre el enlace automático. |
| `enlace_complementario.csv` | Ingredientes de receta enlazados automáticamente por nombre a un ingrediente del catálogo con precio. Ejemplos: ARROZ EXTRA NIR → ARROZ EXTRA, POLLO SIN MENUDENCIA 1.68 KG → POLLO SIN MENUDENCIA CONGELADO, BASE CRIOLLA → PRE ELABORADO BASE CRIOLLA. Solo se enlaza si todas las palabras significativas coinciden. Las compras de caja chica no cuentan. |
| `ingredientes_por_revisar.csv` | Los que no se enlazaron solos, con el mejor candidato y su coincidencia. Para confirmarlos, se copian a `enlace_manual.csv`. |
| `recetas_reales.csv` | Las 946 recetas con el ingrediente del catálogo. **Es lo que se importa.** |
| `insumos_sin_costo.csv` | AGUA PARA RECETA: no se compra y se costea en S/ 0, en vez de dejar la receta con costo "pendiente". |
| `contenido_por_revisar.csv` | Presentaciones cuyo contenido no coincide con la medida del nombre. **Hoy está vacío**: el usuario revisó las 60 el 2026-10-03 y quedaron así (ver `datos/enlace/correcciones_contenido.csv`): 30 corregidas a la medida del nombre (con dos pesos, vale el segundo); las carnes se compran por kilo (contenido 1 kg; el peso del nombre solo describe la presentación); y "3.785 ML" es un error del nombre, son litros. |
| `familias_sgp.csv` | Familia › subfamilia › grupo del SGP por presentación (1 516 presentaciones, 112 grupos). La caja chica solo trae familia. |
| `productos_activos.csv` | D02: por ingrediente, el producto activo en la operación, es decir el que tiene stock en el inventario o el de compra más reciente en el SGP. Su precio es el que se costea. Se corrige en Catálogo > Producto activo en la operación. |
| `recetas_clasificadas.csv` | Cada receta con:<br>• su componente de menú (bebida caliente, jugo, pan, sopa, fondo, guarnición, entrada, postre, refresco, complemento, huevo…);<br>• la proteína del fondo;<br>• el **gramaje por ración** (g, ml, und);<br>• el costo estimado por ración y si todos sus ingredientes tienen precio. |
| `estructuras_menu.csv` | Estructura teórica de Desayuno, Almuerzo y Cena: componentes, **factor de consumo** y reparto de alternativas (jugo 50/50, huevo frito 50 / a la orden 50, fondo 70/30). Es una propuesta: cada operación la ajusta en la aplicación. |
| `ciclo_menu.csv` | Ciclo de 28 días: recetas por día, servicio y componente. Reglas:<br>• solo recetas con costo completo;<br>• una receta no se repite en 7 días en el mismo servicio;<br>• almuerzo y cena no repiten receta el mismo día;<br>• los fondos rotan la proteína (pollo, res, pescado, cerdo, pavo, gallina…). |
| `resumen.txt` | Conteos. |

## Orden de carga en una sede

Se usan la conexión de sede y un usuario administrador. Todos los pasos se pueden repetir sin duplicar. Si un archivo no es el que espera el paso (le faltan columnas), se rechaza sin cargar nada.

**Desde la aplicación:** Administración > Carga de datos reales. Muestra los 9 pasos en este orden, con lo que ya está cargado. Cada paso exige su permiso. Los de catálogo, recetas e inventario inicial abren su pantalla de siempre.

**Desde la consola** (Instalador):

```
AppSistema.Instalador importar-catalogo  datos/enlace/catalogo_por_ingrediente.csv
AppSistema.Instalador cargar-familias    datos/real/familias_sgp.csv
AppSistema.Instalador importar-precios   datos/real/precios_sgp.csv
AppSistema.Instalador importar-recetas   datos/real/recetas_reales.csv --aprobar
AppSistema.Instalador marcar-sin-costo   datos/real/insumos_sin_costo.csv
AppSistema.Instalador liberar-productos  datos/real/productos_activos.csv
AppSistema.Instalador importar-inventario datos/inventario/inventario_inicial.csv
AppSistema.Instalador cargar-estructuras datos/real/estructuras_menu.csv
AppSistema.Instalador cargar-ciclo       datos/real/ciclo_menu.csv 2026-10-05 500 500 300 --aprobar
```

`cargar-ciclo` recibe:

* la fecha del día 1 del ciclo;
* los comensales de desayuno, almuerzo y cena.

Las raciones de cada plato salen de comensales × factor × reparto. Con `--aprobar`, cada minuta guarda su costo previsto y su **venta = costo / 48 %**.

## Resultado con estos datos (ciclo completo, 500 / 500 / 300 comensales)

| Servicio | Minutas | Costo por comensal (promedio, mín–máx) | Precio de venta por comensal (48 %) |
|---|---|---|---|
| Desayuno | 28 | S/ 3,54 (2,67–4,58) | S/ 7,38 |
| Almuerzo | 28 | S/ 7,87 (5,28–12,00) | S/ 16,40 |
| Cena | 28 | S/ 6,15 (3,64–12,59) | S/ 12,82 |

Con la regla D02 se usa el precio del **producto activo** de cada ingrediente (`productos_activos.csv`): el que tiene stock en el inventario o, si no, el de compra más reciente en el SGP. Antes se tomaba el más barato y el costo salía entre 12 % y 23 % menor. Los precios no incluyen IGV (D03).

## Lo que falta revisar

1. **`ingredientes_por_revisar.csv` (85 ingredientes, 448 líneas de receta).** Los más usados son:
   * AJO MOLIDO ENVASADO (106 líneas);
   * MAYONESA;
   * AJÍ PANCA y AJÍ AMARILLO MOLIDO ENVASADO;
   * los panes de marca (Molinos, Pastipan);
   * MEZCLA LÁCTEA;
   * KETCHUP.

   Indique a qué producto corresponde cada uno, o copie la línea a `enlace_manual.csv`, y vuelva a ejecutar la herramienta.
2. **Cereales y yogurt del desayuno:** ninguna receta tiene precio, así que el componente queda en la estructura pero fuera del ciclo.
3. **`precios_atipicos.csv` (20):** confirme si el precio es de la caja o de la unidad.
4. **Errores del enlace de origen.** Por ejemplo, en `PRODUCTO_INGREDIENTE.csv` hay mostazas asignadas a CHOCOLATE PARA TAZA. Están en `datos/enlace/observaciones_enlace.csv`.
