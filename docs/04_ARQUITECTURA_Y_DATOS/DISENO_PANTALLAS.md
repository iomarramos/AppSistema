# Diseño de pantallas y de la base de datos

Actualizado: 2026-10-03. Réplica visual de todas las pantallas con este estilo, incluida una página con las capturas del SGP usadas como base: https://claude.ai/artifact/LqViLBPDyFwUqMktDxNqaz

## Base: las pantallas del SGP

Del manual *SGP Local – Para Operaciones* (Sodexo Perú, V006) se toma la estructura que los usuarios ya conocen:

| Elemento del SGP | En AppSistema |
|---|---|
| Franja amarilla con contrato, régimen y servicio ("ANTAMINA FOALI – RÉGIMEN GENERAL – SERVICIO…") | Franja superior de cada ventana con el nombre de la pantalla y la operación, subrayada en el color de acento |
| Barra de iconos (incluir, grabar, anular, imprimir, salir) | Barra de botones **con texto**: las acciones que confirman van en azul y las que anulan en rojo |
| Cabecera del documento (contrato, n.º, fecha, bodega, tipo) y grilla de líneas | Igual: filtros y datos del documento arriba, líneas abajo, divisiones con `SplitContainer` |
| Total del documento al pie | Etiqueta de totales al pie, en gris y seminegrita |
| Leyenda de colores ("Celda bloqueada", "Cantidad sobrepasa stock") | Estados como etiquetas de color dentro de la grilla y franja de ayuda en amarillo suave |
| Listados para imprimir (toma de inventario, diferencias, valorizado) | Botón **Imprimir…** en cada pantalla: HTML para imprimir o PDF, CSV para Excel |

## Pautas aplicadas (referencias)

* **Windows 11 / Fluent 2:**
  * letra Segoe UI;
  * texto de 14 px (9,75 pt) y títulos en seminegrita;
  * un color de acento (#0F6CBD);
  * superficies claras y bordes suaves.
* **Tablas de datos:**
  * números a la derecha para comparar por la coma decimal;
  * filas alternas con poco contraste;
  * filas de 32 px, con más espacio de lectura;
  * encabezados multilínea y un ancho mínimo por columna para evitar texto comprimido;
  * estados como etiquetas.
* **ERP, documento con cabecera y líneas:** la cabecera lleva quién, cuándo y por qué; las líneas, qué, cuánto y a qué precio. Las acciones del documento van en una barra arriba y los totales al pie.
* **.NET 8 WinForms:** las ventanas se arman en código a 96 ppp y se escalan con `AutoScaleMode.Dpi`. Así se ven bien en pantallas al 125 % o al 150 %.

## Dónde está y cómo se cambia

* `src/AppSistema.Escritorio/Tema.vb` concentra la paleta, las fuentes y el estilo de grillas, botones, barras, franjas de ayuda y totales. **Los formularios no fijan colores ni fuentes.**
* Se aplica solo:
  * a toda ventana abierta desde el menú (`FormPrincipal`, también a las que abre otra pantalla);
  * a los diálogos (`DialogoCampos`, `Ui.MostrarLista`, imprimir o exportar, acceso y comparativo);
  * a toda grilla creada con `Ui.NuevaGrilla`.
* Las reglas que dependen del texto se definen en `Tema.vb`:
  * los botones que empiezan por Aprobar, Autorizar, Entregar, Cerrar, Guardar, Aceptar, Importar o Ingresar van en azul;
  * los que empiezan por Anular, Eliminar, Quitar, Desactivar o Retirar van en rojo;
  * los estados que se colorean (aprobada, borrador, faltante, etc.) están en `Tonos`.

### Usabilidad compartida (2026-10-04)

El tema conserva Segoe UI y el acento azul, con superficies de gris azulado. Los botones tienen un mínimo de 34 px; Entrar recibe el mismo estilo principal que Aceptar. Las franjas de ayuda y los totales crecen cuando el texto necesita más líneas. El encabezado divide su ancho entre pantalla y operación, con puntos suspensivos si el espacio se agota.

Las tablas reservan más ancho a descripciones, productos y conceptos. Una lista vacía muestra orientación; los estados conservan su texto y el foco de teclado. Los diálogos asignan un orden de tabulación y enfocan el primer campo, manteniendo Enter/Escape y el contenido literal de las claves. Los botones creados con `Ui.Boton` se deshabilitan durante su acción para impedir que se repita mientras sigue abierto un diálogo.

En **Ventanas**, Ctrl+K permite elegir una pantalla disponible según los permisos existentes; usa la misma acción del menú para reutilizar una ventana ya abierta. El menú también lista las ventanas abiertas, organiza en cascada o lado a lado y cierra la activa con Ctrl+F4.

Las pruebas de controles se ejecutan en Windows sin base de datos:

```powershell
dotnet test tests/AppSistema.E2E.Tests -c Release --filter FullyQualifiedName~UsabilidadTests
```

Para guardar capturas de los controles de prueba, definir `APPSISTEMA_UI_CAPTURAS=1`; se guardan en `artifacts/screenshots/ui-*.png`. Estas capturas no sustituyen el recorrido E2E autenticado ni la comprobación en distintas escalas DPI.

### Editar una pantalla en Visual Studio (Diseñador)

Las pantallas convertidas tienen dos archivos:

* `FormX.Designer.vb`, que se edita **con el Diseñador**: controles, posición, tamaño, textos y nombres (`Name` = identificador para las pruebas E2E, por ejemplo `btnEntrar`). No se escribe a mano.
* `FormX.vb`, que se edita **con el código**: permisos (qué se ve según el rol), tema, carga de datos y eventos (`Handles btnX.Click`).

Para abrir el Diseñador, doble clic en `FormX.vb` en el Explorador de soluciones (o clic derecho > Ver diseñador, Mayús+F7). Si se cambia el `Name` de un control, hay que actualizar la prueba E2E que lo usa. Los colores y fuentes que se pongan en el Diseñador se sobrescriben al abrir la ventana: el estilo sale de `Tema.vb`.

Ya convertidas: Acceso y Principal. Las demás se convierten por etapas; mientras tanto, su Diseñador sale vacío y se editan por código (clic derecho > Ver código, F7).

## Base de datos (PostgreSQL)

Revisión contra convenciones públicas de PostgreSQL (guía de revisión SQL de Bytebase y convenciones de desarrollo de PostgreSQL):

| Pauta | Estado |
|---|---|
| Clave primaria en toda tabla | ✅ |
| Restricciones de integridad (CHECK, UNIQUE, FK, EXCLUDE para vigencias) en la base y no solo en la aplicación | ✅ |
| Nombres en minúsculas con guion bajo; vistas con `v_`; índices `ix_<tabla>_<columnas>` | ✅ |
| **Índice para cada FK que se usa al unir tablas o al borrar el padre** | ✅ desde **V017**: 59 índices nuevos (detalle → documento, kárdex por almacén, variante y fecha, compras, recepción, minutas, cierres). Las 35 FK que quedan sin índice (quién creó, aprobó o contó, y catálogos que no se borran) están fijadas en una prueba: una FK nueva debe llevar índice o justificarse |
| Dinero y cantidades exactos (enteros ×1 000 000, nunca coma flotante) | ✅ |
| Aislamiento por empresa (RLS), auditoría por trigger, migraciones versionadas e inmutables | ✅ |
