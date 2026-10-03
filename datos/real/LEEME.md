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
| `recetas_clasificadas.csv` | Cada receta con:<br>• su componente de menú (bebida caliente, jugo, pan, sopa, fondo, guarnición, entrada, postre, refresco, complemento, huevo…);<br>• la proteína del fondo;<br>• el **gramaje por ración** (g, ml, und);<br>• el costo estimado por ración y si todos sus ingredientes tienen precio. |
| `estructuras_menu.csv` | Estructura teórica de Desayuno, Almuerzo y Cena: componentes, **factor de consumo** y reparto de alternativas (jugo 50/50, huevo frito 50 / a la orden 50, fondo 70/30). Es una propuesta: cada operación la ajusta en la aplicación. |
| `ciclo_menu.csv` | Ciclo de 28 días: recetas por día, servicio y componente. Reglas:<br>• solo recetas con costo completo;<br>• una receta no se repite en 7 días en el mismo servicio;<br>• almuerzo y cena no repiten receta el mismo día;<br>• los fondos rotan la proteína (pollo, res, pescado, cerdo, pavo, gallina…). |
| `resumen.txt` | Conteos. |

## Orden de carga en una sede

Se usan la conexión de sede y un usuario administrador. Todos los pasos se pueden repetir sin duplicar.

```
AppSistema.Instalador importar-catalogo  datos/enlace/catalogo_por_ingrediente.csv
AppSistema.Instalador importar-precios   datos/real/precios_sgp.csv
AppSistema.Instalador importar-recetas   datos/real/recetas_reales.csv --aprobar
AppSistema.Instalador marcar-sin-costo   datos/real/insumos_sin_costo.csv
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
| Desayuno | 28 | S/ 3,13 (2,56–4,05) | S/ 6,53 |
| Almuerzo | 28 | S/ 6,57 (4,72–10,53) | S/ 13,69 |
| Cena | 28 | S/ 5,01 (3,41–11,53) | S/ 10,43 |

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
