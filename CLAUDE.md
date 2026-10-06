# AppSistema — guía para retomar el trabajo

Sistema de menús, compras, almacén, producción, inventarios, cierres y Food Cost que reemplaza al SGP de Sodexo.
**El usuario escribe en español; respóndale en español.**

## 1. Lectura obligatoria antes de cualquier tarea

Toda la documentación interna está en `docs/` y su índice es `docs/00_LEEME.md` (prompts, requerimientos, estado, arquitectura, operación, guía, skills y memoria del asistente). Las reglas para agregar documentos están al final de ese índice.

1. `docs/03_ESTADO/CONTINUAR.md`: estado, decisiones y cómo seguir (su fecha de corte puede estar atrasada; manda el checklist).
2. `docs/03_ESTADO/CHECKLIST.md`: avance por sprint, pantallas, accesos y pendientes. Las **entregas 1 a 11** de 2026-10-06 están al final y son el estado vigente. **Actualícelo en cada entrega.**
3. `docs/03_ESTADO/SEGUIMIENTO.md`: registro detallado por etapa.
4. Especificación de la pantalla de trabajo: `docs/02_REQUERIMIENTOS/ESPECIFICACION_VENTANA/` (00 a 08) y el diagnóstico en `docs/02_REQUERIMIENTOS/DIAGNOSTICO_VENTANA.md`.
5. Según el tema: `docs/02_REQUERIMIENTOS/REQUERIMIENTOS.md`, `docs/04_ARQUITECTURA_Y_DATOS/INTEGRACION_RESULTADOS.md`, `docs/04_ARQUITECTURA_Y_DATOS/ARQUITECTURA_PLANIFICACION_CENTRAL.md`, `docs/04_ARQUITECTURA_Y_DATOS/DISENO_PANTALLAS.md`, `docs/04_ARQUITECTURA_Y_DATOS/ESTADO_BASE_DATOS.md`, `docs/04_ARQUITECTURA_Y_DATOS/RELACIONES_BASE_DATOS.md`.

## 2. Estado actual (corte 2026-10-06)

* **Operativo:** menú por áreas con permisos por pantalla; minutas por día y servicio; planilla del menú en formato SGP (estructura × día, edición de receta, raciones y comensales); requerimiento y entrega a producción; adicionales con aprobación; devoluciones por solicitud; raciones operativas del chef; cierre diario y mensual con checklist; resultado comparado (presupuesto por rubro, mes anterior y acumulado); traspaso entre almacenes **en dos pasos** (envío y recepción, con tránsito que bloquea el cierre); monitor de tránsitos; reportes con exportación a impresora, Excel, PDF y CSV.
* **Ramas:** trabajo en `claude/busy-mayer-9fxop6`. Las ramas `feature/formatos-sgp` y `feature/planificacion-menus-matriz` tienen otras migraciones con los mismos números (ver §4).
* **Sin commit:** los cambios de las entregas 4 a 11 todavía no están confirmados. Antes de confirmar, separar lo que no corresponde a esta rama (incluidos `instalar_appsistema_local*.ps1`, que nunca se suben con claves por defecto).

## 3. Pila, estructura y reglas técnicas

* VB.NET .NET 8: dominio, datos e instalador de consola. La aplicación de escritorio es **WinForms** (Windows 10+). PostgreSQL 16/17 con un servidor por sede.
* `src/AppSistema.Dominio`: cálculos puros. Cantidades y dinero son enteros ×1 000 000 (`_u6`, `EscalaU6`); nunca `Double`.
* `src/AppSistema.Datos`: servicios con sesión (`ServicioConSesion.EnTransaccion(permiso, …)`), RLS por `app.empresa_id`. Los errores de la base llegan como `CODIGO: mensaje` y se convierten en `ReglaNegocioException`. Los servicios que el usuario ve en pantalla reportan el código en el mensaje (`SIN_PERMISO`, `MINUTA_APROBADA`, `TRASPASO_RECIBIDO`…).
* `src/AppSistema.Escritorio`:
  * Formularios con **Diseñador** (`FormX.Designer.vb` con `InitializeComponent`): solo controles y propiedades. Permisos (`Visible`/`Available`), datos y eventos van en `FormX.vb`. Los botones con permiso se crean con `Ui.BotonSi(permiso, …)`; nunca se ocultan por posición en la barra.
  * Toda grilla se configura en el constructor con **`Ui.Configurar(grilla)`** (solo lectura, montos, tema). `Ui.Mostrar(grid, lista, "Prop|Encabezado"…)` aplica las columnas **solo con el manejador de ventana creado**; sin él, WinForms lanza `NullReferenceException` en `DataGridViewBand`.
  * Columnas inmovilizadas: deben estar al inicio y todas, también las ocultas (si no: `InvalidOperationException`). Con anchos fijos, `AutoSizeColumnsMode = None` antes de fijarlos.
  * `DialogoCampos` queda por código (campos dinámicos). `Ui.MostrarLista` sirve para listas de solo lectura.
  * La apariencia (fuente, colores, grillas, botones) está solo en `Tema.vb`.
  * Cada opción de menú lleva el permiso mínimo de la pantalla (`FormPrincipal.ConstruirMenu`). Toda ventana de consulta se abre **sin modal** (`Show(Me)`); los diálogos modales bloquean la automatización de interfaz.
  * Identificadores estables (`Name` = AutomationId) según `Identificadores.vb`: botones `btnX`, menús `mnuX` (el de menú sale del texto visible), campos `gridX`, `txtX`, `cmbX`, `dtX`.
  * `AccessibleName` en campos y grillas (texto derivado del nombre; los botones usan su texto).
* `src/AppSistema.Instalador`: comandos de consola (`migrar`, `crear-empresa`, `importar-*`, `cargar-*`, `sincronizar`, `respaldar`…).
* `herramientas/`: utilidades Python para datos del SGP y verificación (`verificar_consultas_sql.py`: ejecuta `EXPLAIN` de todas las consultas literales contra una base migrada).
* `datos/`: archivos del SGP recibidos (cada carpeta tiene `origen/` y `LEEME.md`). `datos/real/` es el juego ordenado y cargable (`herramientas/ordenar_datos_reales.py`).

## 4. Migraciones y base de datos

* Van embebidas y el migrador verifica su hash. **Nunca se edita una migración aplicada**: se crea una nueva con el siguiente número libre.
* Numeración vigente en esta rama: **V001 a V021 confirmadas; V022 a V025 de esta rama quedaron sin usar** (ver siguiente punto); V026 a V033 pendientes de confirmar, en este orden: perfiles de la operación, aprobación de adicionales, solicitud de devolución, motivo del adicional, raciones operativas, motivos normalizados del ajuste, foto de movimientos al cerrar y traspaso en tránsito.
* **Choque de numeración:** la base local `appsistema` tiene V022 a V025 de otras ramas (planificación central, plan real SGP, días base de stock, plan del chef). Decisión tomada (2026-10-06): las pendientes de esta rama se numeran después de V025. Antes de fusionar ramas hay que decidir cuál línea es la oficial.
* Antes de migrar una base real: **respaldo** (`pg_dump -Fc`), prueba en una copia restaurada y solo después la base real. Ejemplo de respaldo guardado: `artifacts/respaldo/`.
* Las tablas nuevas necesitan `GRANT` a `app_stock`, una política RLS (`fn_empresa_actual()`) y el trigger `auditar`. Toda FK necesita un índice cuyo prefijo sean sus columnas (la prueba `Las_claves_foraneas_tienen_indice…` lo verifica), salvo la lista fija de esa prueba.
* Traspasos: `traspaso_transito` (V033). El envío descuenta el origen; la recepción crea la entrada en el destino por el mismo valor. Un traspaso enviado sin recibir bloquea el cierre del día de envío (`TRANSITO_PENDIENTE`).

## 5. Pruebas (obligatorias antes de cada entrega)

```bash
pg_ctlcluster 16 main start   # si PostgreSQL está detenido
export DOTNET_ROOT=/opt/dotnet-ms/usr/share/dotnet PATH=/opt/dotnet-ms/usr/share/dotnet:$PATH
./ejecutar_pruebas.sh         # SQL + concurrencia, dominio, integración, instalador, compilación WinForms y E2E
```

* **Datos e integración** (`tests/AppSistema.Datos.Tests`): `APPSISTEMA_PG_PRUEBAS=1`, `APPSISTEMA_PG_HOST`, `APPSISTEMA_PG_USER`, `APPSISTEMA_PG_PASSWORD` (solo en el entorno del comando, nunca en archivos) y `PGOPTIONS="-c lc_messages=C"` para que los mensajes de error lleguen en inglés. **No** usar `PGOPTIONS` en E2E: el rol de sede no puede cambiar `lc_messages`.
* **E2E** (`tests/AppSistema.E2E.Tests`, solo Windows con escritorio): `APPSISTEMA_E2E=1` y `APPSISTEMA_E2E_PG` con la conexión del propietario. Limitaciones conocidas de esta máquina:
  * la entrada de mouse y teclado puede estar bloqueada (`SendInput` denegado o sin efecto): el arnés pulsa por **patrón Invoke** y contrae los menús por UI Automation;
  * los diálogos modales (avisos de error, conexión de TI) no se pueden leer: la prueba de clave incorrecta y la de Sincronización fallan por eso;
  * la prueba de 1366x768 mide la ventana **maximizada** (1932x975 en un monitor de 1920x1080), no el diseño: está pendiente cambiarla para que compare el área de cliente diseñada.
* `DemoTeoricoRealTests` (E2E) siembra el desayuno de 500 comensales y deja los reportes en `artifacts/demo` (Excel, PDF y `teorico_vs_real.txt`). Las cifras esperadas están en esa prueba y en `TeoricoRealTests`.
* Los errores inesperados de la aplicación quedan en `%LOCALAPPDATA%\AppSistema\errores.log`: revisarlos antes de dar una entrega por buena.

## 6. Reglas de trabajo acordadas con el usuario

* **Git:** rama de trabajo `claude/busy-mayer-9fxop6`, con el PR #1 (borrador) hacia `main`; tras cada entrega con el CI verde, `develop` avanza por fast-forward al commit probado; cada etapa tiene su rama `feature/etapa-N-*`.
* **Commit y push:** cada cambio terminado se prueba y se confirma con las líneas de atribución de la sesión. Hasta el 2026-10-06 el usuario no confirmó el commit del conjunto mezclado: preguntar antes de confirmar cambios de varias entregas.
* Los archivos que el usuario sube con "SUBIR AL REPOSITORIO Y TRABAJAR SOBRE ESTA INFORMACIÓN" se guardan en `datos/<tema>/origen/` tal como llegan; luego se convierten con una herramienta en `herramientas/` y se documentan en un `LEEME.md`.
* "Continúa", "procede" o "continua con la programación" significan avanzar con lo siguiente de los pendientes del checklist, sin preguntar lo que ya tiene una respuesta razonable.
* **Decisiones de negocio** (regla de tránsito, numeración de migraciones, carga de un menú) se consultan con el usuario; lo técnico reversible se decide y se informa.
* **No se inventan datos:** sin precio no hay precio; lo dudoso va a una lista de revisión.
* **Secretos:** no se incluyen claves en el repositorio ni en documentos. Las claves de prueba viven en el código de prueba y solo sirven para bases temporales. Las claves reales del usuario no se guardan ni se repiten en las respuestas.
* **Verificación antes de afirmar:** una corrida de prueba o un recorrido real vale más que la lectura del código. Si algo no se pudo verificar en esta máquina, decirlo.
* **Operaciones destructivas** (borrar bases, reescribir historial, renumerar migraciones aplicadas) se confirman antes; se respalda primero.

## 7. Pendientes, por prioridad

1. **Confirmar el commit** del conjunto de entregas 4 a 11 (separando lo que no corresponde a esta rama).
2. **Decidir la línea oficial de migraciones** (V022 a V025 de `feature/*` frente a las de esta rama) antes de fusionar.
3. **Cargar el menú del SGP que el usuario pegó** (planilla por estructura y día) a la base; hoy la base real no tiene minutas. Pedir el archivo original (Excel o reporte del SGP), no el texto pegado.
4. **Cuentas de trabajo:** hechas como perfiles de prueba (`chef_prueba`, `almacen_prueba`, `jefe_almacen_prueba`, `operaciones_prueba`, `planificacion_prueba`, `compras_prueba`) con `herramientas`: `AppSistema.Instalador perfiles-prueba`. Las claves están en `%LOCALAPPDATA%\AppSistema\perfiles_prueba.txt`, fuera del repositorio. Revisar la cuenta de administración local (rol CHEF), que no creó el sistema.
5. **Presentación en pantalla** (detectada en la demostración): montos sin formato en "Venta teórica" de Producción (`VentaPrevistaU6` no está en la lista de montos de `Ui`), food cost con todos los decimales, plurales ("1 minutas", "1 servicios").
6. **Calendario de cierres por semanas:** existe como vista de lista (botón "Calendario por semanas..."); decidir si pasa a cuadrícula en pantalla.
7. **Tránsito entre operaciones por la central** (fase 3b): la tabla `traspaso_transito` todavía no viaja en la sincronización. Diseñar el canal antes de implementarlo. Recepción parcial y anulación de envíos sin recibir, pendientes.
8. **Pruebas E2E:** cambiar la de 1366x768 para que mida el diseño, no la ventana maximizada; resolver la lectura de diálogos modales o aceptar esa limitación en el CI.
9. **Datos sin fuente:** R05 (aporte nutricional, requiere tabla de composición validada) y R14 (cafetería, sin origen de datos). Nutrición queda diferida por decisión del usuario.
10. **Dato de referencia** de la especificación en `docs/01_PROMPTS/07_PROMPT_MAESTRO_IMPLEMENTACION.md` y `docs/01_PROMPTS/06_PROMPTS_POR_FORMULARIO.md`: revisar contra la implementación al cerrar cada sprint.
