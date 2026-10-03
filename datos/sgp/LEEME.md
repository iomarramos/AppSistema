# Listado de productos del SGP

| Archivo | Contenido |
|---|---|
| `productos_sgp_original.tsv` | Listado tal como vino del SGP: `pro_nombre`, `pro_coduni` (presentación), `pro_facing` (factor de conversión). 4 174 líneas. |
| `catalogo_sgp.csv` | El mismo listado en el formato del importador de catálogo (generado; no editar a mano). |
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
3. Si el nombre contradice el factor, el producto **se carga igual con el factor del SGP** y queda en `observaciones_sgp.csv`. Son 51 productos.
   * Ejemplo: `PAPA SECA LA SERRANITA 3 KG`, factor 5.
   * Se corrige desde Catálogo, con "Corregir contenido", mientras la presentación no tenga movimientos.
4. Los productos con nombre repetido y otra presentación llevan la presentación en la descripción, por ejemplo `CAJA CHICA - TOMATE CHERRY (KILOGRAMO x 1 KG)`.
5. Las 14 líneas idénticas a otra anterior se cargan una sola vez.

Resultado: 4 160 productos (KG 1 476, L 375, UND 2 309).

## Significado de `pro_coduni` (POR CONFIRMAR)

El significado se dedujo de los nombres de los productos:

| Código | Presentación | Código | Presentación | Código | Presentación |
|---|---|---|---|---|---|
| 4 | BIDON | 19 | GALON | 34 | POTE |
| 5 | BALDE | 22 | KIT | 36 | ROLLO |
| 8 | BOLSA | 23 | KILOGRAMO | 38 | SACHET |
| 9 | BOTELLA | 24 | LATA | 39 | SACO |
| 10 | CAJA | 26 | LITRO | 41 | SIXPACK |
| 14 | CAJETILLA | 31 | PAQUETE | 42 | SOBRE |
| 18 | FRASCO | 32 | PAR | 44 | TUBO |
| | | | | 45 | UNIDAD |
| | | | | 46 | VASO |

**Sin nombre confirmado.** Por ahora se cargan como `PRES-SGP-n`:

| Código | Productos | Ejemplos |
|---|---|---|
| 1 | 8 | caja chica, hierbas frescas |
| 3 | 60 | barras energéticas, cobertura |
| 6 | 10 | mermeladas en porción |
| 20 | 69 | caja chica, golosinas |
| 28 | 9 | colorante, cerveza, bolsas al vacío |
| 33 | 1 | tamal |
| 37 | 1 | bolsas de papel |

Para corregir un nombre: se cambia en `ConversorSgp.Presentaciones` **antes** de la primera carga.

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
