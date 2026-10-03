# Listado de productos del SGP

| Archivo | Contenido |
|---|---|
| `productos_sgp_original.tsv` | Listado tal como vino del SGP: `pro_nombre`, `pro_coduni` (presentación), `pro_facing` (factor de conversión). 4 174 líneas. |
| `catalogo_sgp.csv` | El mismo listado en el formato del importador de catálogo (generado; no editar a mano). |
| `origen/` | Archivos tal como se recibieron del SGP (Latin-1, separador `;`). No se modifican. |
| `productos_precios.csv` | Último precio de compra por producto (1 520), en UTF-8 y listo para usar: familia/subfamilia/grupo, unidad de envase, último precio y fecha (ISO). Miles sin separador, decimal con punto. **Aún no se carga**: se usará con proveedores y precios. **Regla del usuario (03/10/2026):** un producto sin precio en este archivo se deja sin precio; no se le asigna 0 ni un precio estimado. |
| `observaciones_sgp.csv` | Productos cuyo nombre no confirma el factor, o que tienen el mismo nombre que otro producto. **Para revisión.** |

Regenerar los dos últimos: `dotnet run --project src/AppSistema.Instalador -- convertir-sgp datos/sgp/productos_sgp_original.tsv datos/sgp`

## Cómo se guarda cada producto

Cada línea del SGP pasa a ser:

* **producto base** `SGP00001…`, numerado en el orden del archivo, con su **unidad base** (KG, L o UND);
* **variante** con el mismo código. `tipo_envase` = la presentación del SGP. `contenido_por_envase` = `pro_facing` **sin cambios**;
* **empaque de compra** = la presentación del SGP. Es la **unidad mínima de pedido del almacén**: 1 envase, mínimo 1, múltiplo 1.

Ejemplo: `ARVEJA VERDE PARTIDA CANTA CLARO BOLSA 500 GR  8  0.5` queda así:

* unidad base KG;
* presentación BOLSA con 0.5 KG;
* el almacén pide de a 1 BOLSA.

### Cómo se elige la unidad base

1. Se compara el factor con el tamaño escrito en el nombre, con una tolerancia de 3 %. Se reconocen:
   * GR, KG, ML, LT, CC, GALON, OZ;
   * "6X4 LITROS";
   * "100 UND", "X 50", "MILLAR".
2. Si el nombre no indica tamaño:
   * `pro_coduni` 23 con factor 1 → KG;
   * `pro_coduni` 26 con factor 1 → L;
   * factor 1 o entero → UND (unidades).
3. Si el nombre contradice el factor, el producto **se carga igual con el factor del SGP** y queda en `observaciones_sgp.csv`. Son 47 productos.
   * Ejemplo: `PAPA SECA LA SERRANITA 3 KG`, factor 5.
   * Se corrige desde Catálogo, con "Corregir contenido", mientras la presentación no tenga movimientos.
4. Los productos con nombre repetido y otra presentación llevan la presentación en la descripción, por ejemplo `CAJA CHICA - TOMATE CHERRY (KILOGRAMO x 1 KG)`.
5. Las 16 líneas idénticas a otra anterior se cargan una sola vez. Cuentan como idénticas dos códigos con la misma presentación: por ejemplo 1 y 23 son KILOGRAMO.

Resultado: 4 158 productos (KG 1 482, L 375, UND 2 301).

## Significado de `pro_coduni`

| Código | Presentación | Código | Presentación | Código | Presentación |
|---|---|---|---|---|---|
| 1 | KILOGRAMO ✔ | | | | |
| 3 | GRANO ✔ | 19 | GALON | 34 | POTE |
| 4 | BIDON | 20 | GRAMO | 36 | ROLLO |
| 5 | BALDE | 22 | KIT | 37 | PAQUETE ✔ (resma) |
| 6 | GRAMO ✔ (blister) | 23 | KILOGRAMO | 38 | SACHET |
| 8 | BOLSA | 24 | LATA | 39 | SACO |
| 9 | BOTELLA | 26 | LITRO | 41 | SIXPACK |
| 10 | CAJA | 28 | MILLAR | 42 | SOBRE |
| 14 | CAJETILLA | 31 | PAQUETE | 44 | TUBO |
| 18 | FRASCO | 32 | PAR | 45 | UNIDAD |
| | | 33 | PAQUETE ✔ (PCH) | 46 | VASO |

✔ = confirmado por el usuario el 03/10/2026. Los demás nombres salen del cruce con `Uni.Env` del archivo de precios.

Todos los códigos del listado tienen nombre. Un código nuevo que no esté en la tabla se carga como `PRES-SGP-n`.

El nombre de la presentación se fija en `ConversorSgp.Presentaciones` **antes** de la primera carga real.

## Cargar en una sede

Se puede cargar de dos formas:

* **Aplicación:** Catálogo > Importar > elegir `productos_sgp_original.tsv`. El sistema reconoce el formato SGP, muestra la vista previa y las observaciones, y luego importa.
* **Consola:**

  ```
  set APPSISTEMA_CONEXION=Host=SERVIDOR;Database=appsistema;Username=app_sede;Password=...
  AppSistema.Instalador importar-sgp productos_sgp_original.tsv
  ```

Requisitos y garantías:

* El usuario necesita el permiso CATALOGO_IMPORTAR.
* Crea las unidades KG, L y UND si faltan.
* Es todo o nada.
* Repetirlo no duplica nada.

## Archivos recibidos el 03/10/2026

* `origen/PRODUCTOS_PRESENTACION_DE_COMPRA.csv` es **el mismo listado** que `productos_sgp_original.tsv`:
  * las 4 174 líneas coinciden;
  * solo cambian espacios duros y comillas en 24 nombres, que el conversor ya normaliza.
* `origen/PRODUCTOS_PRECIOS.csv`:
  * todos sus nombres existen en el listado de presentaciones;
  * 3 nombres aparecen dos veces, por ejemplo `CAJA CHICA - CULANTRO`;
  * el SGP no indica proveedor ni moneda; son soles (confirmado por el usuario el 03/10/2026).

### Evidencia usada para los `pro_coduni` (ya resueltos arriba)

Se cruzó `Uni.Env` de precios con `pro_coduni`. Cada código tiene una sola abreviatura.

| Código | Uni.Env | Probable |
|---|---|---|
| 3 | BAR | BARRA |
| 6 | BLI | BLISTER |
| 20 | GR | GRAMO |
| 28 | MIL | MILLAR |
| 33 | PCH | ¿? |
| 37 | RSM | RESMA |
| 1 | (sin precios) | ¿? |

El cruce también confirma los nombres ya usados: 8 BOL, 10 CAJ, 23 KG, 31 PQT, 45 UND, etc.

Antes de la primera carga real conviene fijar estos nombres en `ConversorSgp.Presentaciones`.
