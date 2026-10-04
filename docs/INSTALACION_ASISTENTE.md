# Instalación con el asistente (AppSistema.Instalador.exe)

El instalador es un solo ejecutable. Al abrirlo sin argumentos (doble clic) o con `AppSistema.Instalador.exe instalar`, un asistente en consola hace todo lo necesario para dejar una sede lista:

1. Se conecta al PostgreSQL del servidor (versión 16 o superior).
2. Crea la base de datos (o la conserva; con `RECREAR` la empieza de cero).
3. Aplica las migraciones V001 a V022 (la estructura y las reglas de la base).
4. Crea el usuario de la base con el que trabajan las computadoras y le da acceso a la base.
5. Crea la empresa, la operación, el almacén y el administrador.
6. Crea el superusuario (dueño del sistema).
7. Carga los datos de la carpeta `datos\` que el sistema puede cargar hoy: catálogo, familias, precios, recetas, insumos sin costo, productos activos, inventario inicial, estructuras de servicio y ciclo de menú (minutas). Lo que aún no tiene importador se indica más abajo.
8. Muestra la conciliación de la base y los datos con que se conecta cada computadora.

No necesita `psql`, PowerShell ni Python en la PC de destino. Las claves se piden en la consola y **no se guardan** ni se vuelven a mostrar.

## Requisitos

* PostgreSQL 16 o superior instalado y en ejecución en el servidor (en esta PC: `postgresql-x64-17`). El asistente no instala el motor de PostgreSQL; verifica que esté disponible y lo informa si no.
* La clave del usuario administrador de PostgreSQL (`postgres`), que el asistente pide y no guarda. Si el servidor no pide clave (autenticación de confianza), basta con Enter.
* La carpeta `datos\` junto al ejecutable (o indicada en el asistente).

## Cómo se repite

El asistente se puede volver a ejecutar sobre la misma base: lo que ya existe se conserva, cada carga reporta "ya estaba" y el inventario inicial se omite si el almacén ya tiene movimientos. La segunda corrida no duplica ningún dato (verificado en `ejecutar_pruebas.sh`, paso 4b).

## Datos de origen

Los Excel de `datos\*\origen\` se convierten con las herramientas de `herramientas\` (Python con `openpyxl`). El asistente no lee `.xlsx`: carga los CSV que resultan de esa conversión.

| Excel | Cadena de conversión | CSV que carga el asistente |
|---|---|---|
| `inventario\origen\INVENTARIO_PRODUCTOS.xlsx` | `convertir_inventario.py` | `inventario\inventario_inicial.csv` |
| `recetas\origen\RECETAS_SGP_REVISADAS_DEL_TOTAL_3.xlsx` | `convertir_recetas_sgp.py` → `enlazar_sgp.py` → `ordenar_datos_reales.py` | `real\recetas_reales.csv` (y el catálogo `enlace\catalogo_por_ingrediente.csv`) |

Verificación: volver a ejecutar `convertir_inventario.py` y `convertir_recetas_sgp.py` reproduce el contenido de los CSV del repositorio (solo cambian los fines de línea).

| `plan_real\origen\MENU_REAL_Y__TEORICO.xlsx`, `MENU_REAL_CENA.xlsx` (plan real y teórico por día y plato), `REQUISICION.xlsx`, `Costo_Plan…xlsx` (comparativos) | `convertir_plan_real.py` | `plan_real\*.csv` → paso 7.9, tablas `sgp_*` (V023) |

El paso 7.9 guarda el plan del SGP **completo** en el servidor, tal como llegó: 552 días-servicio, 11 046 platos (teórico y real), 2 778 líneas de requisición, 465 comparativos por día, 11 totales, piso y techo, y 254 pasos de preparación. Los códigos del SGP se enlazan a las recetas y a las variantes del sistema: 256 de 256 productos y 553 de 628 recetas.

**Pendiente, con datos a la vista:**

* 75 recetas del SGP no están entre las recetas recibidas. Afectan a 222 platos del plan y a 385 líneas de requisición. Quedan guardadas, sin enlace a una receta del sistema (lista en `sgp_codigo_receta` con la columna de receta vacía).
* El plan real es referencia importada. Todavía no se convierte en minutas operativas: el plan del SGP tiene raciones distintas cada día, y eso necesita el modelo de factores por día de la fase 3 de Planificación central.
* El ciclo de menú (`real\ciclo_menu.csv`) y las estructuras (`real\estructuras_menu.csv`) son **propuestas generadas a partir de las recetas con costo**, no el menú real; el asistente las sigue cargando como minutas de prueba.

## Usar el asistente desde otra computadora

En cada computadora de la sede, en la ventana **Conexión** de AppSistema:

* Servidor, puerto y base: los que indicó el asistente.
* Usuario de la base: el usuario de la base que eligió (por defecto `app_sede`) y su clave.
* Empresa: el código de la empresa (por defecto `DEMO`); usuario y clave de un usuario de la empresa.

## Comandos avanzados

`AppSistema.Instalador.exe` sigue aceptando los comandos de consola de siempre (`migrar`, `importar-catalogo`, `respaldar`, `sincronizar`, etc.); aparecen con `--ayuda`.
