# Checklist de avance

Actualizado: 2026-10-03, 12:30 hora de Lima. Se actualiza en cada entrega.

**Leyenda**

| Marca | Significado |
|---|---|
| ✅ | Hecho y probado (pruebas automáticas o carga de punta a punta) |
| 🟡 | Hecho en código, pero falta validarlo en Windows o con el usuario |
| ⬜ | Pendiente |

## 1. Pantallas (front) por menú

### Mejoras compartidas de interfaz — 2026-10-04

- ✅ Tema común: superficies, botones de 34 px, menús y pestañas con mayor espacio, encabezado de pantalla y operación sin solapamiento.
- ✅ Tablas: encabezados multilínea, columnas descriptivas más anchas, filas de 32 px, estado con texto y foco de teclado, aviso cuando no hay registros.
- ✅ Ayudas y totales ajustan su altura al ancho de la ventana; los diálogos conservan Enter/Escape y orden explícito de campos.
- ✅ Ventanas: lista de pantallas abiertas, cascada, mosaico y cierre con Ctrl+F4. Ctrl+K abre un selector limitado a las opciones de menú permitidas.
- ✅ Compilación Release sin advertencias; 98 pruebas de dominio y 6 pruebas de controles WinForms en Windows aprobadas. Capturas locales de acceso, diálogo, encabezado y tabla revisadas.
- 🟡 Pendiente repetir el recorrido completo autenticado y comprobar DPI 125/150 %: la conexión PostgreSQL local requiere credenciales no disponibles en la sesión. No se modificaron servicios, permisos, datos ni migraciones.

Todas las pantallas **abren sin errores en Windows** con el superusuario (recorrido E2E con FlaUI en el CI; capturas en el artefacto `e2e-capturas`). Falta la revisión visual y de uso por el usuario (🟡). Réplica visual: https://claude.ai/artifact/LqViLBPDyFwUqMktDxNqaz

| Menú > opción | Permiso para abrir | Acciones con permiso propio | Estado |
|---|---|---|---|
| Catálogo > Productos, variantes y empaques | CATALOGO_VER | Editar, sin costo de compra, corregir contenido y **producto activo en la operación**: CATALOGO_EDITAR | 🟡 |
| Catálogo > Proveedores y precios | CATALOGO_VER | Proveedores: PROVEEDORES_EDITAR · Precios: PRECIOS_EDITAR | 🟡 |
| Catálogo > Importar catálogo | CATALOGO_IMPORTAR | Solo acepta archivos de catálogo o del listado SGP | 🟡 |
| Menús > Recetas | MENUS_VER | Editar: RECETAS_EDITAR · Aprobar y retirar: RECETAS_APROBAR | 🟡 |
| Menús > Minutas y necesidades | MENUS_VER | Planificar y cambiar comensales: MINUTAS_EDITAR · Aprobar: MINUTAS_APROBAR · Factores de la operación: **FACTORES_EDITAR** (V021; la base también lo exige) · Imprimir minuta: todos | 🟡 |
| Menús > Servicios y estructuras | MENUS_CONFIGURAR | Estructura teórica, factor teórico y Food Cost objetivo | 🟡 |
| Menús > Importar recetas | RECETAS_EDITAR | Solo acepta archivos de recetas | 🟡 |
| Menús > Producción | MENUS_VER | Requerimientos, producción, merma, venta real y consumo: PRODUCCION_EDITAR · Entregar: STOCK_CONTABILIZAR · Teórico vs real e imprimir requerimiento: todos | 🟡 |
| Compras > Consolidado de compras (todas las operaciones) | COMPRAS_CONSOLIDAR | **Nueva** (Abastecimiento). Periodo (por defecto, el mes siguiente) y minutas en borrador opcionales; imprimir o exportar | 🟡 |
| Compras > Previsión y pedidos | COMPRAS_VER | Calcular, pedidos y reservas: COMPRAS_EDITAR · Aprobar y anular: COMPRAS_APROBAR | 🟡 |
| Almacén > Stock e inventario inicial | CATALOGO_VER | Movimientos: STOCK_CONTABILIZAR · Imprimir kárdex, stock valorizado y **registro de inventario permanente valorizado (SUNAT 13.1)**: todos | 🟡 |
| Almacén > Inventario físico | INVENTARIO_CONTAR | Revisar y autorizar ajuste: INVENTARIO_APROBAR · Imprimir hoja de conteo y resultado: todos | 🟡 |
| Cierres y control > Pendientes, cierres y Food Cost | REPORTES_VER | Cerrar, ingresos, venta por estructura y objetivo: CIERRE_EJECUTAR | 🟡 |
| Cierres y control > Contratos y clientes | CONTRATOS_VER | Editar: CONTRATOS_EDITAR | 🟡 |
| Cierres y control > Gastos y resultado mensual | RESULTADOS_VER | Gastos: GASTOS_EDITAR | 🟡 |
| Administración > Usuarios y roles | USUARIOS_ADMINISTRAR | Roles propios por módulo y nivel; asignar en cualquier operación propia; **Accesos por módulo**; **Asignaciones y alcance**; **Alcance…** (OPERACION, ZONA o TODAS: solo el superusuario); dar o quitar el rango de dueño (solo el dueño) | 🟡 |
| Administración > **Matriz de acceso** | USUARIOS_ADMINISTRAR | **Nueva (fase 1).** Persona y operación → módulo, pantalla, acción, por rol, excepción y efectivo. Conceder o negar aquí o en todas, y quitar la excepción: solo el superusuario | 🟡 |
| Administración > Operaciones y almacenes | USUARIOS_ADMINISTRAR | Nueva · **Zona**: Costa, Sierra o Selva (fase 1) | 🟡 |
| Administración > Auditoría | AUDITORIA_VER | **Nueva**, solo lectura | 🟡 |
| Administración > Sincronización y respaldo (TI) | USUARIOS_ADMINISTRAR | **Nueva**. Además pide la conexión del propietario de la base (no se guarda). Sede: estado de la cola, configurar, enviar ahora, respaldar y conciliación. Central: sedes, registrar (la credencial se muestra una vez) y desactivar | 🟡 |
| Administración > Carga de datos reales | CATALOGO_IMPORTAR | **Nueva**. Precios: PRECIOS_EDITAR · Sin costo: CATALOGO_EDITAR · Estructuras: MENUS_CONFIGURAR · Ciclo: MINUTAS_EDITAR (aprobar: MINUTAS_APROBAR). Catálogo, recetas e inventario inicial abren su pantalla propia | 🟡 |
| Barra de estado | REPORTES_VER | Estado de envío a la central | 🟡 |

Los menús sin ninguna opción para el usuario ya no se muestran.

### Qué ve cada rol base

El **dueño del sistema** (administrador general, V020) tiene todo, en todas las operaciones. Se crea con `AppSistema.Instalador crear-dueno` y su clave personal.

| Menú | DUEÑO | ADMIN | SUPERVISOR | PLANIFICACIÓN | ABASTECIMIENTO | ALMACEN | COCINA | FINANZAS |
|---|---|---|---|---|---|---|---|---|
| Catálogo (ver) | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | — |
| Catálogo (editar), proveedores y precios | ✔ | ✔ | ✔ | — | ✔ | — | — | — |
| Recetas, minutas, costos y necesidades (ver) | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | ✔ | — |
| Recetas y minutas (editar y aprobar), factores, pax | ✔ | ✔ | ✔ | ✔ | — | — | editar | — |
| Servicios y estructuras | ✔ | ✔ | ✔ | ✔ | — | — | — | — |
| Producción: registrar, venta real, consumo | ✔ | ✔ | ✔ | — | — | — | ✔ | — |
| Producción: entregar (almacén) | ✔ | ✔ | ✔ | — | — | ✔ | — | — |
| Compras: previsión y pedidos (ver y preparar) | ✔ | ✔ | ✔ | — | ✔ | ✔ | — | — |
| Compras (aprobar) | ✔ | ✔ | ✔ | — | ✔ | — | — | — |
| **Consolidado de compras (todas las operaciones)** | ✔ | ✔ | ✔ | — | ✔ | — | — | — |
| Almacén: stock y movimientos | ✔ | ✔ | ✔ | — | — | ✔ | ver | — |
| Inventario físico: contar / autorizar | ✔ | ✔ | ✔ | — | — | contar | — | — |
| Cierres y Food Cost (ver) | ✔ | ✔ | ✔ | ✔ | — | — | — | ✔ |
| Cierres (ejecutar) | ✔ | ✔ | ✔ | — | — | — | — | — |
| Contratos, gastos y resultado | ✔ | ✔ | ✔ | — | — | — | — | ✔ |
| Usuarios, operaciones y almacenes; accesos por módulo | ✔ | ✔ | — | — | — | — | — | — |
| Dar o quitar el rango de dueño | ✔ | — | — | — | — | — | — | — |
| Carga de datos reales | ✔ | ✔ | ✔ | — | — | — | — | — |
| Sincronización y respaldo (TI) | ✔ | ✔ | — | — | — | — | — | — |
| Auditoría | ✔ | ✔ | ✔ | — | — | — | — | — |

**Roles centrales (fase 1, V021).** Se suman a los anteriores, que no cambian:

| Rol | Tiene | No tiene |
|---|---|---|
| PLANIFICADOR_CENTRAL | Catálogo (ver), recetas (editar y aprobar), servicios, estructuras y factores teóricos, minutas (editar y aprobar), compras (ver), reportes | Stock (T54), factores de la operación |
| COMPRAS_CENTRAL | Catálogo, proveedores y precios, compras (ver, preparar, aprobar), consolidado, reportes. Se le da alcance TODAS | Recetas, minutas, stock |
| OPERACIONES | Minutas (editar), **factores de la operación**, producción, compras (ver), reportes. Se le puede dar alcance ZONA | Planificación central, stock |
| CHEF | Minutas (editar), **factores de la operación**, producción | Stock, recetas centrales |
| ALMACEN (existente) | Sin cambios | **Factores** (T53), recetas, planificación |

**Siete perfiles de la operación (V022, pedido del usuario 2026-10-05).** Los códigos no cambian; se alinean los nombres visibles y se agrega JEFE_ALMACEN. Prueba `Perfiles_de_la_operacion_tienen_solo_lo_que_les_corresponde`.

| Perfil (código) | Nivel | Tiene | No tiene |
|---|---|---|---|
| SUPERUSUARIO (dueño) | Sistema completo | Todo, en todas las operaciones; define la matriz de acceso | — |
| PLANIFICADOR_CENTRAL | Planificación maestra | Recetas, menús, factores teóricos, minutas (editar y aprobar), costos, compras (ver) | Stock, inventario, compras (aprobar) |
| COMPRAS_CENTRAL | Abastecimiento | Consolidado, proveedores, precios, pedidos (preparar y aprobar), reportes | Recetas, minutas, stock |
| OPERACIONES (Jefe de operación) | Control y aprobación | Aprueba minutas y cambios de planificación, **requerimientos adicionales** (ADICIONAL_APROBAR), factores, producción, **cierres diario y mensual**, Food Cost, resultados, **inventario (ver)** | Stock (movimientos), contar o aprobar inventario, precios |
| CHEF (Chef operativo) | Producción diaria | Minutas (editar), factores de la operación, producción y requerimientos, mermas | Stock, precios, catálogo, cierres, aprobar compras |
| JEFE_ALMACEN (nuevo) | Control de almacén | Todo lo del almacenero, **aprueba inventarios y ajustes**, reportes | Cierres, minutas, pedidos (preparar) |
| ALMACEN (Almacenero) | Ejecución física | Recepción contra la orden, despacho (movimientos de stock), **atender devoluciones de cocina**, conteo de inventario, inventario (ver), compras (ver) | Pedidos (preparar, V023), aprobar inventarios, recetas, factores, planificación |

**Ajustes de V023 y V024 (2026-10-05):**
* El almacenero y el jefe de almacén **no preparan pedidos** (se quitó `COMPRAS_EDITAR`). Prueba `Almacen_no_prepara_ni_aprueba_pedidos_y_otra_empresa_no_ve_los_pedidos`.
* **Requerimiento adicional** (ADICIONAL_APROBAR): borrador → aprobado por el jefe de operación → entregado. La base valida el permiso y guarda quién aprobó y cuándo; el calculado se entrega directamente.
* **Devolución de cocina:** la cocina pide (`Pedir devolucion a almacen...`, sin mover stock); el almacén la atiende (`Atender devoluciones pedidas por cocina...`) y ahí sale el stock al costo de la entrega. La cocina puede anular la solicitud mientras esté pendiente. Prueba `Adicional_lo_aprueba_el_jefe_y_la_devolucion_de_cocina_la_atiende_el_almacen`.
* **INVENTARIO_VER**: consultar inventarios sin poder contar. Los botones de conteo solo aparecen con INVENTARIO_CONTAR.

**Superusuario (= dueño del sistema).** Respuesta del usuario: "el superusuario soy yo". Es el dueño de V020 en su empresa.

**Alcance (cuarto nivel).** Cada asignación de rol vale en su operación (OPERACION), en todas las de su zona o región (ZONA) o en todas (TODAS). Solo el superusuario lo amplía.

**Matriz de acceso.** El superusuario concede o niega un permiso a una persona fuera de su rol, en una operación o en todas. Lo negado gana. Los permisos efectivos se calculan en la base (`fn_permisos_usuario`): la sesión y los triggers usan el mismo cálculo.

**Validado en la base, no solo en botones:** mover stock (STOCK_CONTABILIZAR en la operación del almacén) y cambiar factores (FACTORES_EDITAR en la operación; MENUS_CONFIGURAR para el teórico). Un usuario sin permiso tampoco puede hacerlo por SQL con la sesión de la aplicación.

**Privilegios:**
* quien administra usuarios solo puede dar permisos que él mismo tiene en esa operación (`ESCALADA_NO_PERMITIDA`);
* el dueño no tiene límite;
* solo el dueño otorga el rango de dueño, y la base también lo exige.

Además, el administrador puede crear **roles propios** con los permisos que elija.

## 2. Corregido en la revisión de pantallas y accesos (2026-10-03)

- ✅ **Botones por posición.** En Minutas, "Aprobar" se veía para todos y "Cambiar comensales" exigía el permiso de aprobar. Ahora todos los botones con permiso se definen con `Ui.BotonSi`, en 9 pantallas.
- ✅ **Importación duplicada.** Las dos opciones abrían la misma ventana. Ahora cada una tiene su modo (catálogo o recetas) y rechaza el archivo del otro tipo, indicando a qué menú ir.
- ✅ **Factores de la operación.** Pasaron de Servicios y estructuras a **Minutas**. Allí aparecen con el permiso que los exige: desde V021, **FACTORES_EDITAR**, que tienen todos los roles que antes tenían MINUTAS_APROBAR.
- ✅ **Producción.** "Previsto vs real" estaba duplicado por "Teórico vs real", así que se quitó. El comparativo suma lo entregado, lo devuelto y las mermas. Se agregó "Anular requerimiento", que antes no tenía botón.
- ✅ **Gastos.** La lista de servicios usa el permiso de resultados; antes exigía el de contratos.
- ✅ **Pantallas que faltaban.** Operaciones y almacenes, y Auditoría. En Catálogo, "Sin costo de compra" y "Corregir contenido".
- ✅ **Menús vacíos.** Se ocultan.

### Reportes (pendiente 10, entregado el 2026-10-03)

Cada reporte sale con el botón **Imprimir…** de su pantalla. Se puede imprimir desde el navegador (o guardar en PDF) o exportar a CSV para Excel. Exige el mismo permiso que la pantalla de origen y solo muestra datos de la operación elegida. Si un importe no tiene precio, la celda queda vacía (nunca 0).

| Reporte | Dónde | Contenido |
|---|---|---|
| Minuta del día | Minutas > Imprimir minuta… | Platos por componente con raciones y costo, fijos, necesidad de insumos para cocina, costo y venta prevista, Food Cost objetivo y firmas |
| Requerimiento | Producción > Imprimir requerimiento… | Previsto y solicitado por producto, columna para lo entregado y firmas de cocina y almacén |
| Kárdex | Stock > Kardex… > marcar "Imprimir o exportar" | Saldo inicial, movimientos, saldo corrido, costo promedio y totales |
| Stock valorizado | Stock > Imprimir stock… | Presentaciones con saldo, costo promedio y valor total |
| Hoja de conteo | Inventario físico > Imprimir hoja de conteo… | Sin el stock del sistema (conteo ciego), con columnas en blanco para envases y parcial |
| Resultado de inventario | Inventario físico > Imprimir resultado… | Sistema, físico, diferencia y su valor; faltante, sobrante y firmas |

### Diseño de pantallas y base de datos (2026-10-03)

- 🟡 **Tema común** (`Tema.vb`), con base en las pantallas del SGP y las pautas de Windows 11 / Fluent:
  - franja con la pantalla y la operación;
  - botones con texto (azul para confirmar, rojo para anular);
  - grillas con filas alternas, números a la derecha y estados en color;
  - escalado por DPI.

  Falta verlo en Windows. Detalle en `docs/04_ARQUITECTURA_Y_DATOS/DISENO_PANTALLAS.md`.
- ✅ **Base de datos:** V017 agrega índices a las claves foráneas que se recorren. Una prueba fija las que quedan sin índice.

## 3. Funcionalidad

| Módulo | Estado |
|---|---|
| Acceso, permisos por operación, RLS y auditoría | ✅ |
| Catálogo, proveedores, precios e importadores | ✅ |
| Recetas versionadas, minutas, costeo con snapshot y necesidades | ✅ |
| Venta por estructura (factor y reparto, costo ÷ 48 %) | ✅ |
| Teórico vs real (factores por operación, venta real, consumo, productos) | ✅ |
| Previsión y compras | ✅ |
| Almacén y kárdex valorizado (promedio móvil) | ✅ |
| Producción (entrega por presentación completa, mermas) | ✅ |
| Inventario físico | ✅ |
| Cierres diario y mensual, Food Cost | ✅ |
| Continuidad (cola, sincronización, respaldo, restauración, actualización), con pantalla para TI | ✅ (falta el piloto) |
| Contratos, gastos, resultado y roles propios | ✅ |
| Datos reales del SGP ordenados y cargables (`datos/real/`), desde la consola o la pantalla de carga; archivo equivocado rechazado | ✅ |
| Dueño del sistema, roles por área (Planificación y Abastecimiento), accesos por módulo y nivel, sin escalada de privilegios | ✅ |
| Consolidado de compras por periodo entre todas las operaciones | ✅ |
| D02 producto activo por operación (Catálogo, carga masiva, costeo y pedidos) y D03 precios sin IGV | ✅ |
| Reportes imprimibles y exportables (HTML para imprimir o PDF, CSV para Excel), con el permiso de la pantalla de origen | ✅ |
| Comparativo teórico → plan real → realizado (fórmula del SGP, probada con sus cifras: `PlanVsRealTests`) y costo piso y techo por factores | ✅ |
| Registro de inventario permanente valorizado, formato 13.1 de SUNAT (tablas 5, 6, 10 y 12) | ✅ |
| Requerimiento con bulto en decimales (presentación activa) | ✅ |
| **Planificación y Abastecimiento Central, fase 1 (seguridad):** superusuario, módulo → pantalla → acción → alcance (operación, zona o todas), matriz de acceso, roles centrales, stock y factores validados en la base (T53, T54, T61, T62, T63) | ✅ |
| **Siete perfiles de la operación (V022 a V024):** Jefe de almacén nuevo; almacenero, jefe de operación y chef operativo con sus permisos; aprobación de adicionales; devolución de cocina atendida por el almacén; inventario (ver). Pruebas `Perfiles_de_la_operacion_tienen_solo_lo_que_les_corresponde`, `Adicional_lo_aprueba_el_jefe…` y `Almacen_no_prepara…` (compiladas; falta correrlas con PostgreSQL) | 🟡 |

## 4. Pendiente por crear o confirmar

| # | Pendiente | Depende de |
|---|---|---|
| 1 | ⬜ **Probar la aplicación en Windows 10+**: abrir cada pantalla del punto 1 y anotar los ajustes | PC con Windows |
| 2 | ⬜ Enlazar los 85 ingredientes de `datos/real/ingredientes_por_revisar.csv` (ajo molido, mayonesa, ajíes molidos, panes de marca…) | Usuario |
| 3 | ⬜ Confirmar los 20 precios de `datos/real/precios_atipicos.csv` | Usuario |
| 4 | ⬜ Factores de consumo reales por operación y servicio, y confirmar las estructuras propuestas | Usuario |
| 5 | ⬜ Recetas con precio para cereales y yogurt del desayuno | Usuario |
| 6 | ✅ Decisiones tomadas. D02: precio del producto activo en la operación. D03: precios sin IGV. D10: una sede por ahora, ampliable. Ya aplicadas (V018) | Usuario (respondido el 2026-10-03) |
| 7 | 🟡 Pantalla para la carga de datos reales: **hecha** (Administración > Carga de datos reales, 7 pasos con el estado de lo cargado). Falta probarla en Windows | Programación (hecho) · PC con Windows |
| 8 | 🟡 Pantalla de sincronización y respaldo para TI: **hecha** (Administración > Sincronización y respaldo). Pide la conexión del propietario y no la guarda. Restaurar y actualizar siguen solo en el Instalador, a propósito. Falta probarla en el piloto | Programación (hecho) · Piloto |
| 9 | ⏸ Resultado consolidado de varias sedes: no hace falta mientras sea una sede (D10). La central y la sincronización ya están listas para cuando se amplíe | Cuando se agregue otra sede |
| 10 | 🟡 Reportes imprimibles o exportables: **hecho** (minuta del día, requerimiento, kárdex, hoja de conteo, resultado de inventario y stock valorizado). Falta verlos impresos desde Windows | Programación (hecho) · PC con Windows |
| 13 | ✅ Contenidos revisados por el usuario: 30 corregidos a la medida del nombre (con dos pesos, vale el segundo); las carnes van por kilo; "3.785 ML" son litros (`datos/enlace/correcciones_contenido.csv`). Por la corrección, la margarina de 190 g pasó a precio atípico | Usuario (respondido) |
| 14 | ✅ Bulto de pedido: los sacos son individuales. Si no hay un múltiplo, se pide de a uno (decisión del usuario, 2026-10-03). Si más adelante un producto se compra por caja, se le agrega un empaque en Catálogo | Usuario (respondido) |
| 15 | ✅ Familias del SGP cargadas: familia › subfamilia › grupo (V019, `familias_sgp.csv`, 1 516 presentaciones en 112 grupos). El ingrediente sin categoría toma la familia de sus presentaciones | Programación (hecho) |
| 16 | ⬜ **Menú del mes real** y **estructuras de servicio** que faltan (loncheras, refrigerios, coffee break), además de confirmar las de desayuno, almuerzo y cena. Estado actual en `docs/04_ARQUITECTURA_Y_DATOS/ESTADO_BASE_DATOS.md` | Usuario (los va a presentar) |
| 17 | ⬜ **Planificación y Abastecimiento Central, fases 2–10** (`docs/04_ARQUITECTURA_Y_DATOS/ARQUITECTURA_PLANIFICACION_CENTRAL.md`): planificación teórica con versiones, liberación, plan operativo, programación del día del chef, requerimiento y adicional, ejecución real, 3 comparativos, aprendizaje de factores, compras globales con almacén central y tránsito, controles reutilizables | Programación (fase 1 hecha) |
| 18 | 🟡 **Corte del requerimiento interno con retrasos permitidos** (respuesta del usuario): hora configurable por operación; lo tardío se acepta marcado TARDÍO; tiempo de llegada por zona desde el central. Falta la hora por defecto y los días por zona; se programa en las fases 4–5 y 8–9 | Usuario (dato) · Programación |
| 19 | ✅ **Zonas:** Costa, Sierra y Selva (solo esos valores). Se elige en Operaciones y almacenes | Usuario (respondido) |
| 21 | ⬜ **Nutrientes** (aporte por receta, % aprovechamiento y cocción): ningún archivo recibido trae valores; falta la tabla de composición por ingrediente | Usuario (tabla de nutrientes) |
| 20 | 🟡 **Modelo objetivo del SGP** (`datos/plan_real/`): leído y verificado (11 de 11 reglas cuadran, con la cena real de octubre). Hecho además: costo piso y techo por factores, bulto en decimales en el requerimiento y registro SUNAT 13.1. Por construir, dentro de las fases: cuadrícula mensual de planificación (2), reporte de frecuencia, requisición por rango con preparación (4–5), comparativo de tres niveles (6; fórmula ya hecha), productos sin movimiento y registro permanente valorizado, servicios lonchera, rancho, venta directa y consumo fijo | Programación · Usuario (nutrientes, cena de octubre, piso y techo) |
| 22 | ✅ **Pruebas E2E de la aplicación Windows** (FlaUI, job `e2e-windows` en verde): acceso (superusuario y clave incorrecta), permisos por rol en pantalla (chef, almacén), excepción de la matriz de acceso que oculta el botón y **recorrido de todas las pantallas sin errores, con una captura PNG de cada una** (artefacto `e2e-capturas`). Siguientes suites: planificador → liberación → chef → almacén → comparativos, cuando existan esas pantallas | Programación (hecho) |
| 23 | 🟡 **Pantallas editables en el Diseñador de Visual Studio** (pedido del usuario: el diseñador salía en blanco porque todo se armaba por código). Hechas: Acceso y Principal (menú y barra de estado). Siguen por etapas: Catálogo, Proveedores y Stock; Minutas, Recetas, Producción y Servicios; Compras, Consolidado, Inventarios y Cierres; Administración y resto. Las pruebas E2E verifican cada etapa en Windows | Programación |
| 11 | ⬜ Piloto de un mes en una sede (`docs/05_OPERACION/PILOTO_ETAPA_8.md`) | Sede real |
| 12 | ⬜ Integraciones (SAP, ADS, SGO) | Especificación del cliente |

## Sprint 1 de la especificación docs/02_REQUERIMIENTOS/ESPECIFICACION_VENTANA/ (2026-10-06)

| Pantalla / tarea | Estado | Detalle |
|---|---|---|
| Librerías: QuestPDF (PDF) y ClosedXML (Excel) | 🟡 instaladas | En `AppSistema.Escritorio`. Aún no usadas: se conectan en el Sprint 3 (reportes). QuestPDF usa licencia Community: revisar si la empresa supera su umbral de ingresos |
| FormPlanificacionMenus (matriz de planificación, menú Menus > Matriz de planificacion) | 🟡 solo lectura | Minutas del rango por día y servicio: comensales, raciones teóricas, costo previsto, costo por comensal, venta, platos, producción y requerimiento. Pendiente: días en columnas, resumen diario, edición de borradores desde la matriz, costo patrón/techo |
| FormProduccionChef (plan operativo del chef, menú Menus > Plan operativo del chef) | 🟡 | Raciones teóricas frente a comensales (+/-), producción y requerimiento. Cambio de comensales solo en borrador. Pendiente: raciones operativas, sustituciones, adicional desde el plan |
| Servicio ServicioMatrizMenu (Datos) | ✅ | Celdas por rango, costo por comensal con aritmética entera |
| Bloqueo de comensales en minutas aprobadas (ServicioMinutas) | ✅ | Código MINUTA_APROBADA. Antes se podía cambiar después de aprobar |
| Pruebas | ✅ | Datos 101/101 (incluida `Matriz_muestra_costo_solo_si_esta_aprobada…`), dominio 98/98, SQL 69 + concurrencia. E2E Windows: lo corre el CI |

### Sprint 1, continuación (2026-10-06)

| Pendiente anterior | Estado | Detalle |
|---|---|---|
| Matriz con días en columnas | ✅ | Filas = servicio y régimen; columnas = días del rango; celda = comensales y costo por comensal (o "sin costo") |
| Resumen por día | ✅ | Minutas, comensales, minutas sin costo, costo del día, costo por bandeja, techo y desviación (+/-) |
| Costo patrón/techo en pantalla | ✅ | Techo = costo objetivo por comensal del servicio × comensales. Desviación = costo previsto − techo |
| Edición de borradores desde la matriz | ✅ | "Actualizar comensales..." sobre la celda; solo en borrador (el servicio también lo exige) |
| Adicional desde el plan del chef | ✅ | "Pedir adicional..." crea el requerimiento en borrador; el jefe de operación lo aprueba (V023) |
| Raciones operativas separadas de las teóricas | ⏳ pendiente | Requiere el modelo de plan operativo versionado (arquitectura, fases 2 y 3). Hoy solo hay raciones teóricas |
| Sustitución de receta en el plan del chef | ⏳ pendiente | Falta definir la política de sustituciones permitidas (qué recetas puede cambiar el chef) |
| Motivo obligatorio del adicional | ⏳ pendiente | `RequerimientoAdicional` no recibe motivo; hace falta columna y campo en pantalla |
| Cambio de comensales después del despacho | ⏳ pendiente | Debe generar un adicional y no alterar el despacho (regla de la especificación 07). Hoy el servicio solo bloquea minutas aprobadas |
| Verificar la zona horaria del día operativo | ⏳ pendiente | Ver `DIAGNOSTICO_VENTANA.md` |
| Pruebas | ✅ | Datos 101/101, dominio 98/98, aplicación sin advertencias |

## Plan de la especificación docs/02_REQUERIMIENTOS/ESPECIFICACION_VENTANA/: estado por sprint (2026-10-06)

| Sprint | Contenido | Estado |
|---|---|---|
| 1 | Matriz de planificación, plan operativo del chef, costo patrón/techo, adicional desde el plan | 🟡 parcial: falta raciones operativas separadas, sustitución de receta, motivo del adicional, adicional tras despacho |
| 2 | Recepciones (tipos, lote, vencimiento, cantidad física), despacho con FEFO, devolución con condición apto/no apto | ⏳ no empezado (la recepción y la devolución básicas existen en FormStock y en el flujo de cocina) |
| 3 | Motor de reportes con vista previa, PDF, Excel y CSV; catálogo por categorías | 🟡 parcial: PDF (QuestPDF) y Excel (ClosedXML) funcionando y probados; catálogo `FormReportes` con 7 reportes. Faltan ~20 de los 27 |
| 4 | Inventario y ajustes: motivos normalizados, boleta de ajuste, política ABC | ⏳ no empezado |
| 5 | Cierres: checklist con "Ir a", calendario con estados, asistente de cierre mensual | ⏳ no empezado |
| 6 | Resultados: comparación presupuesto, plan y real | ⏳ no empezado |
| 7 | Administración: usuarios en lista y detalle, auditoría Antes/Después | ⏳ no empezado |
| 8 | Tránsitos y monitor de traslados | ⏳ no empezado |
| 9 | Endurecimiento: Designer en 20 formularios (`Controls.Clear`), `AccessibleName`, prueba 1366x768 | 🟡 Designer y `AccessibleName` hechos (entrega 4); prueba 1366x768 en el CI |

**Bloqueado por datos o decisiones:** tabla de composición nutricional (aporte nutricional), zona horaria del día operativo, y revisión de licencias de QuestPDF para la empresa.

### Sprint 1, cierre (2026-10-06)

| Pendiente | Estado | Detalle |
|---|---|---|
| Motivo obligatorio del adicional | ✅ | V025: columna `requerimiento.motivo` y restricción NOT VALID (las filas antiguas no se revisan). El servicio y las dos pantallas lo piden |
| Sustitución de receta en el plan del chef | ✅ | Política por defecto: solo en minutas en borrador y solo por receta aprobada; no repite la receta en la misma estructura (`PLATO_REPETIDO`). Botón "Sustituir plato..." |
| Raciones operativas separadas | ⏳ decisión | `minuta_detalle` está protegida (snapshot aprobado). Hay que decidir dónde vive el ajuste del chef sin alterar el plan liberado |
| Adicional tras el despacho | ⏳ decisión | Mismo motivo: el cambio de comensales de una minuta ya despachada debe generar un adicional; falta definir la regla de cuándo hay despacho |

## Sprint 2: pendiente de decisión

No existen lotes ni fechas de vencimiento en el esquema (ninguna migración los menciona). El despacho con FEFO y la recepción con lote exigen un modelo nuevo que cambia la valoración del almacén (costo promedio móvil). Hay que decidir antes de construirlo: si el lote es obligatorio para todos los productos o solo para perecederos, y si el FEFO reemplaza la salida por presentación completa.

## Cierre de Sprint 1 y Sprint 2 (2026-10-06)

**Sprint 1: cerrado.** Reglas por defecto que quedan escritas en el código y en la base:
* Motivo obligatorio del adicional (V025).
* Sustitución de receta: solo en borrador y solo por receta aprobada; no repite receta en la misma estructura.
* Raciones operativas (V026): tabla aparte `minuta_ajuste_operativo`; el ajuste del chef se permite en plan aprobado y **sin despacho**. Después del despacho el servicio responde `DESPACHADA` y el cambio se pide como adicional. El plan teórico no cambia.
* Cambio de comensales: solo en borrador (el servicio ya bloquea las aprobadas).

**Sprint 2: cerrado como no realizado (pospuesto).** Motivo: no hay lotes ni vencimientos en el esquema, y su diseño cambia la valoración del almacén. Queda pendiente de decisión del usuario; no se construyó nada de lotes, FEFO ni devolución con condición.

## Sprint 3: motor de reportes y catálogo (2026-10-06)

| Reporte | Estado | Fuente |
|---|---|---|
| Motor: PDF (QuestPDF), Excel (ClosedXML), HTML para imprimir y CSV | ✅ | `ExportadorReporte`, `Reporte`, `SalidaReporte` (cuatro formatos). Pruebas `ExportadorReporteTests` |
| Matriz de planificación del periodo | ✅ | `ServicioReportes.MatrizDelPeriodo` |
| Minuta del día, requerimiento, stock valorizado, kárdex, registro 13.1, inventario y hoja de conteo | ✅ | Ya existían; ahora en el catálogo |
| Salida a producción (R11) | ✅ | `SalidasAProduccion`; el total cuadra con la entrega |
| Comparativo mensual teórico frente a realizado (R20) | ✅ | `ComparativoMensual`; raciones, venta, costo y food cost por secciones |
| Resultado mensual de alimentos (R27, parcial) | 🟡 | `ResultadoMensual`; falta la sección de gastos y el resultado operacional completo |
| Costo detallado, resumido y previsión de consumo (R01 a R03), frecuencia (R04), aporte nutricional (R05, bloqueado por datos), mapa de solicitud (R06), requisiciones (R07 y R08), resúmenes de compras y traspasos (R09, R10), devolución, raciones (R12 a R14), costo realizado (R15), food cost (R16), posición (R17), comparativo de raciones (R21), listados de toma (R22 a R24), boleta y explicación de ajustes (R25, R26) | ⏳ pendiente | Cada uno necesita su consulta y su prueba |

**Pendiente del Sprint 3:** unos 15 reportes de los 27, más la vista previa dentro de la aplicación (hoy se abre el HTML en el navegador).

## Sprint 4: inventario y ajustes (2026-10-06)

| Pendiente de la especificación | Estado | Detalle |
|---|---|---|
| Motivos normalizados del ajuste | ✅ | V027: `motivo_codigo` con la lista de la especificación (diez códigos), más explicación y documento de soporte. Las filas antiguas quedan como OTRO con su texto. Pantalla: lista de motivos, explicación obligatoria |
| Boleta de ajuste (R25) | ✅ | `ServicioReportes.BoletaAjustes`: línea por línea, con motivo, explicación, soporte, responsable y valor |
| Explicación de ajustes (R26) | ✅ | `ExplicacionAjustes`: agrupado por motivo y responsable |
| Política ABC del rotativo | ✅ | `ClasificacionAbc` (dominio, con pruebas): A mientras lo acumulado antes del producto sea menos del 80 %, B menos del 95 %, C el resto y los que no tienen consumo. El rotativo puede abrirse por clase |
| Conteo ciego y separación de funciones | ✅ | Ya existían: conteo ciego, quien autoriza no puede haber contado |
| Bloqueo configurable si falta un conteo crítico | ⏳ pendiente | Requiere definir qué productos son críticos |
| Motivo por línea (hoy es un motivo por autorización) | ⏳ pendiente | El diseño actual guarda un motivo para el ajuste completo; por línea exige cambiar la pantalla de revisión |

Pruebas: `Sprint4_ABC_motivo_normalizado_boleta_y_explicacion_de_ajustes`, `InventarioAbcTests` (dominio), `MotivosAjuste`.

## Sprint 3, continuación (2026-10-06)

Catálogo de reportes: **18 reportes** (la especificación pide 27; algunos son variantes de otros).

Nuevos en esta tanda, con prueba:
* Costo resumido teórico por día y servicio (R02): `CostoResumidoTeorico`.
* Previsión de consumo, necesidades consolidadas (R03): `PrevisionConsumo`.
* Frecuencia de recetas (R04): `FrecuenciaRecetas`. Sin la alerta de política de repetición (falta definir la política).
* Requisición por servicio, solo lo atendido (R07): `RequisicionPorServicio`.
* Comparativo de raciones: teóricas, operativas y servidas (R21): `ComparativoRaciones`.

**Pendientes del Sprint 3 (9 de 27):**
* Costo detallado teórico (R01) y costo realizado (R15), por ingrediente.
* Resumen de compras (R09) y de traspasos (R10).
* Mapa de solicitud de compras (R06).
* Requisición detallada y por estructura (R08).
* Control de raciones (R12) y ventas (R13, R14, esta última de cafetería).
* Aporte nutricional (R05): bloqueado por la tabla de composición.
* Vista previa dentro de la aplicación (hoy se abre el HTML en el navegador).

## Sprint 3, pendientes cerrados (2026-10-06)

Catálogo de reportes: **26 reportes**. Cerrados en esta tanda, cada uno con prueba:
* Costo detallado teórico por producto (R01): consumo de receta × raciones ÷ rendimiento, con costo; el total queda vacío si falta un precio.
* Requisición detallada por estructura (R08).
* Mapa de solicitud de compras (R06): la fórmula de la última previsión, línea por línea.
* Resumen de compras (R09): recepciones confirmadas por proveedor y producto, sin IGV.
* Resumen de traspasos (R10): salidas y entradas entre almacenes de la operación.
* Control de raciones (R12): teóricas, operativas, servidas y vendidas por día y servicio.
* Venta de servicio por fuente (R13): importe y raciones por fuente y servicio.
* Costo realizado por periodo (R15): previsto, entregado, devuelto, neto y costo real por minuta.

**Bloqueados, no se implementan sin datos:**
* Aporte nutricional (R05): falta la tabla de composición de ingredientes.
* Venta de cafetería (R14): no existe en el esquema ninguna fuente de datos de cafetería.

**Pendiente transversal:** la vista previa dentro de la aplicación (hoy el reporte se abre como HTML en el navegador).

## Sprints 5 a 9 (2026-10-06)

| Sprint | Estado | Qué quedó hecho | Pendiente |
|---|---|---|---|
| 5 Cierres | 🟡 | Pendientes con "Ir a" (`ServicioCierres.ChecklistDia`); calendario del mes con estado por día (`CalendarioMes`: Cerrado, Con pendientes, Listo, Abierto); cierre mensual en 8 pasos (`ChecklistMes`); pantalla `FormCierreMensualWizard` en Cierres y control. Prueba `Sprint5_checklist_calendario_y_cierre_mensual_en_8_pasos` | Calendario por días y por semanas (botón "Calendario por semanas..."). "Ir a" es texto; la navegación directa queda para Ctrl+K |
| 6 Resultados | 🟡 | Comparado por servicio: presupuesto, costo teórico, costo real, ingreso y alerta de Food Cost ("Sobre objetivo" / "En objetivo") en `ComparativoResultado`. Mes cerrado deshabilita registrar y eliminar gasto | Presupuesto por rubro, mes anterior y acumulado del año en "Presupuesto, mes anterior y acumulado..." (entrega 6). Exportación del comparado en entrega 5 |
| 7 Administración | 🟡 | Reinicio de clave con la política de contraseñas (`ReiniciarClave`, auditado); detalle del usuario elegido (login, estado, roles); auditoría exportable de solo lectura (`Auditoria`) | Ninguno. El bloqueo por intentos fallidos ya existía (`fn_registrar_intento_acceso`, `bloqueado_hasta`); nombre y estado se editan desde la entrega 5 |
| 8 Tránsitos | 🟡 dos pasos en la operación | Monitor con estados "En transito" (recepción de traspaso en borrador) y "Recibido" (confirmada) (`Transitos`) | Tránsito real entre operaciones: requiere la tabla de tránsitos y el canal con la central (fase 3b). Traspaso en dos pasos desde la entrega 7 (V029); el canal con la central sigue pendiente |
| 9 Endurecimiento | 🟡 | Diseñador en los 20 formularios que usaban `Controls.Clear()` (ninguno queda); grillas configuradas con `Ui.Configurar` (ver entrega 4); prueba E2E a 1366x768 en el CI | `AccessibleName` en campos, grillas y listas de los formularios con Diseñador (79 controles, derivado del nombre del control; los botones usan su texto). Falta FormAuditoria (nombres de campo distintos); la apariencia de los formularios migrados no se ha visto en pantalla |

**Riesgo abierto (T38):** la prueba de concurrencia `T38_salidas_simultaneas_con_el_cierre...` falla de forma intermitente solo con la suite completa. Pasa al correrla sola. La afirmación compara `creado_en` y `fecha_cierre`, que son `now()`, es decir, la hora de inicio de cada transacción. Una salida que empezó después del cierre y se confirmó antes queda incluida legítimamente, así que la comprobación puede marcarla por error. Revisar la aserción con la hora de confirmación antes de tocar el bloqueo.

## Entrega 1-2-3 (2026-10-06)

1. **Diseñador en tres formularios** (Cierres, Inventarios y Stock): controles fijos (grillas, barras, selectores de fecha y mes, etiquetas) en `.Designer.vb` con nombres estables; los botones que dependen de permiso siguen en código con `Ui.BotonSi`. Sin `Controls.Clear()`. Se corrigió además el orden de acoplamiento (en WinForms el último control agregado se acopla primero); la etiqueta de resumen de la hoja de conteo quedó visible. **Pendientes:** los 17 formularios restantes con `Controls.Clear()`.
2. **Resolución 1366x768 en el CI:** `RecorridoPantallasTests` mide cada ventana y falla si pasa de 1366x768. El asistente de cierre bajó a 1200x680. Se verifica en la próxima corrida del CI E2E (no se corre localmente).
3. **T38 corregido de raíz:** el cierre guarda una foto de los movimientos del día (V028: `movimientos_al_cerrar`, `valor_al_cerrar_u6`) en la misma actualización que lo marca cerrado. La prueba compara esa foto con los movimientos finales, en lugar de comparar horas de inicio de transacción. Pasa 3 de 3 corridas aisladas y la suite completa (119/119).

**Pendientes de sprint 9:** `AccessibleName` en campos y grillas; migrar los 17 formularios restantes.

## Entrega 4: Diseñador en los 20 formularios restantes (2026-10-06)

1. **Diseñador completo:** los 10 formularios de la entrega anterior (Comparativo, Consolidado, Operaciones, Matriz de acceso, Resultados, Proveedores, Servicios, Contratos, Carga real, Importación) y los 7 que faltaban (Continuidad, Usuarios, Recetas, Compras, Minutas, Catálogo, Producción) tienen sus controles fijos en `.Designer.vb`. Ningún formulario usa `Controls.Clear()`. Los botones con permiso siguen en código con `Ui.BotonSi`.
2. **Corrección de grillas:** `Ui.NuevaGrilla()` era la única que aplicaba la configuración de grilla (solo lectura, montos y cantidades, columnas, tema). Al pasar las grillas al Diseñador esa configuración se había perdido. Ahora `Ui.Configurar(grilla)` la aplica en el constructor de cada formulario (28 grillas).
3. **Usuarios:** la suscripción a la selección y el panel de detalle ya no se agregan cada vez que se recarga la lista (antes se duplicaban).
4. **Compilación:** `dotnet build` de `AppSistema.Escritorio` sin errores. Pruebas de dominio 102/102.

5. **`AccessibleName`:** 79 campos de entrada y grillas en los formularios con Diseñador (texto derivado del nombre: `gridSedes` → "Sedes"). Los botones ya usan su texto. FormAuditoria queda sin el atributo porque sus campos tienen nombres distintos.

**Pendientes:** la apariencia de los formularios migrados no se ha verificado en pantalla (las pruebas E2E corren solo en el CI); la rama no está confirmada (commit y push pendientes de instrucción).

## Entrega 5: pendientes de sprints 6, 7 y 9 (2026-10-06)

1. **Sprint 6, comparado exportable:** el botón "Comparado con el presupuesto..." de Resultados usa `ServicioReportes.ComparativoResultado` (mismo formato que el catálogo de reportes). Requiere `REPORTES_VER`.
2. **Sprint 7, datos del usuario:** `ServicioAdministracion.EditarUsuario(id, nombre, activo)` y el botón "Editar datos..." en Usuarios. Permite corregir el nombre y **reactivar** un usuario (antes solo se podía desactivar). Protecciones: no desactivarse a sí mismo, solo el dueño modifica a otro dueño, y debe quedar un administrador. Prueba `Editar_usuario_desactiva_reactiva_y_protege_al_propio_usuario`.
3. **Sprint 7, bloqueo por intentos fallidos:** ya estaba implementado (`ServicioAcceso.IniciarSesion` con `fn_registrar_intento_acceso`); el pendiente anterior era una entrada desactualizada.
4. **Sprint 9, `AccessibleName`:** completado también en FormAuditoria (6 controles).
5. **Sprint 9, 1366x768:** revisión estática: ningún formulario del Diseñador supera 1280x720 de área de cliente (la ventana principal está maximizada). La prueba E2E sigue corriendo solo en el CI.
6. **Menú:** las 25 opciones que abren formulario están enlazadas a su pantalla y a su permiso (`FormPrincipal.ConstruirMenu`).

**Verificación:** `dotnet build` sin errores (escritorio, E2E y datos). Pruebas de datos contra PostgreSQL local: 120/120. Dominio: 102/102.

**Pendientes abiertos (de la entrega 4, superados en la entrega 6):** ver la entrega 6 para lo cerrado. Queda abierto el tránsito real (Sprint 8) y R05 y R14 (sin datos de origen).

## Entrega 6: sprints 5 y 6 (2026-10-06)

1. **Sprint 5, calendario por semanas:** `ServicioCierres.CalendarioSemanal` arma el mismo calendario del mes de lunes a domingo (una celda por día con su estado). Botón "Calendario por semanas..." en el asistente de cierre. Prueba `Sprint5_calendario_por_semanas_muestra_cada_dia_del_mes_una_vez_en_su_dia`.
2. **Sprint 6, presupuesto por rubro:** cada línea de resultado y el total traen el presupuesto de personal, operación y otros (administración incluida), sumado de los gastos proyectados. `ServicioResultados.Comparacion` compara el mes con su presupuesto, con el mes anterior y con el acumulado de enero al mes. Botón "Presupuesto, mes anterior y acumulado..." en Resultados. Prueba `Sprint6_comparacion_con_presupuesto_por_rubro_mes_anterior_y_acumulado`.

**Verificación:** datos 122/122 contra PostgreSQL local; dominio 102/102; compilan escritorio y E2E.

**Pendiente abierto:** tránsito real entre operaciones (Sprint 8). Cambia cómo se descuenta el stock en un traspaso (hoy es inmediato) y necesita el canal con la central (fase 3b). Requiere decidir antes la regla de negocio; no se implementó. R05 y R14 siguen sin datos de origen.

## Entrega 7: tránsito real entre almacenes de la operación (Sprint 8, opción A, 2026-10-06)

1. **Migración V029 (`traspaso_transito`):** cada traspaso guarda su envío (origen, destino, documento de salida, fecha y valor) y pasa a 'recibido' con su documento de entrada. Un trigger impide cambiar el envío y borrar; la recepción ocurre una sola vez. Los traspasos anteriores se copian como recibidos. Tiene RLS, grants a `app_stock` y auditoría.
2. **Enviar:** `ServicioAlmacen.Traspasar` saca el stock del origen y devuelve el id del tránsito. El destino no recibe nada todavía: el stock queda fuera de los almacenes.
3. **Recibir:** `ServicioAlmacen.Recibir` crea la entrada en el destino por el mismo valor de la salida. Es todo o nada (la recepción parcial queda para después). Un traspaso ya recibido responde `TRASPASO_RECIBIDO`.
4. **Cierre del día:** un traspaso enviado sin recibir bloquea el día en que se envió (`TRANSITO_PENDIENTE`). Aparece en el checklist con "Ir a: Almacen > Stock e inventario inicial > Recibir traspaso". Por eso el checklist de cierre pasa de 8 a 9 controles.
5. **Pantalla Stock:** botón "Recibir traspaso..." (elige un traspaso enviado hacia este almacen). Al enviar, el aviso dice que queda en tránsito.
6. **Monitor de tránsitos (reportes):** ahora lee `traspaso_transito`: "En transito" hasta recibirse, después "Recibido".

**Verificación:** datos 123/123 contra PostgreSQL local (incluye la prueba de que un traspaso recibido no se borra y la del bloqueo del cierre); dominio 102/102; compilan escritorio y E2E.

**Pendiente:** el canal con la central (tránsito entre operaciones, fase 3b). La tabla nueva no se envía todavía a la central por la sincronización; hay que sumarla cuando se diseñe ese canal. La recepción parcial y la anulación de un envío sin recibir tampoco existen aún.

## Entrega 8: revisión de todas las consultas contra la base (2026-10-06)

- **Herramienta:** `herramientas/verificar_consultas_sql.py BASE [USUARIO] [ROL]`. Extrae las 472 consultas literales de `AppSistema.Datos` y `AppSistema.Escritorio` (uniendo los literales concatenados) y ejecuta `EXPLAIN` de cada una contra una base migrada. No modifica datos.
- **Resultado como propietario sobre base migrada (V001 a V029):** 469 consultas completas, 0 errores. Las 3 restantes se arman con variables y se revisaron a mano: `filtro` toma solo dos valores fijos; el nombre de tabla de `VerificarVersion` sale de tres constantes; y en `ServicioContinuidad` los nombres vienen de la base y se escapan.
- **Permisos por rol:** `app_stock` y `app_sede` pasan todas las consultas de negocio. Las 9 denegaciones con `app_stock` son de `ServicioContinuidad`, que va con la conexión del propietario a propósito (`FormContinuidad`). `app_sincronizacion` no ejecuta consultas de negocio, así que sus denegaciones son esperadas.
- **Límite:** `EXPLAIN` valida nombres, tipos de columna y funciones, pero no los tipos de los valores que se pasan (eso lo cubren las pruebas de integración, que pasan 123/123) ni el resultado de cada consulta.

## Entrega 9: demostración del menú teórico y real en la aplicación (2026-10-06)

**Lo que se ejecutó:** `DemoTeoricoRealTests` (E2E, `APPSISTEMA_E2E=1`). Siembra un desayuno de 500 comensales con su producción (480 preparadas, 470 servidas), consumo por componente, 200 L de aceite y 10 kg de azúcar no planificados y venta de 470 raciones. Después abre `AppSistema.exe` con el usuario dueño y recorre la matriz de planificación, el plan operativo del chef y el teórico vs real de la minuta. Deja los archivos en `artifacts/demo` (Excel y PDF de 7 reportes, `teorico_vs_real.txt`) y las capturas en `artifacts/screenshots`.

**Cifras verificadas a mano (todas cuadran):** venta teórica S/ 3 145,83 (1 510 / 48 %); venta real S/ 2 957,08 (3 145,83 × 470/500); costo teórico S/ 1 510 (aceite a S/ 8/L); costo teórico de lo consumido S/ 1 287; costo real S/ 1 630 (1 600 aceite + 30 azúcar); food cost teórico 48 % y real 55,12 %; factor real de jamón 63,83 % (300/470).

**Errores reales corregidos en la aplicación:**
- La **matriz de planificación se caía al abrirse**: la columna oculta "Clave" no estaba inmovilizada y "Servicio" sí (`InvalidOperationException`); después, con "Ajustar a la grilla" el ancho de los días no aplicaba (`NullReferenceException`). Ahora la matriz usa anchos fijos con desplazamiento.
- **Grillas**: `Ui.Mostrar` ajustaba anchos antes de que la grilla tuviera manejador de ventana (`NullReferenceException` en `DataGridViewBand`). Ahora el formato se aplica con el manejador creado.
- **Teórico vs real** se abría como diálogo modal; ahora es una ventana no modal junto a Producción.

**Arnés E2E (solo pruebas):** el clic de mouse no hace nada en esta sesión sin error, así que el arnés pulsa primero por UI Automation (`Invoke`); el menú desplegado se contrae antes de volver a abrirlo; el identificador del ítem del chef sale del texto visible (`mnuPlanOperativoDelChef`).

**Resultado del recorrido de pantallas:** 24 de 25 abren sin error de la aplicación. Dos observaciones para revisar:
1. La prueba de 1366x768 mide la ventana **maximizada** (1932x975 en un monitor de 1920x1080), no el diseño. El diseño de cada formulario sí cabe (ver Diseñador); conviene que la prueba compare el área de cliente diseñada o fije el monitor.
2. Sincronización y respaldo (TI) abre un diálogo de conexión modal: UI Automation no puede leerlo y vence.

**Pruebas que siguen fallando en esta sesión por el arnés (no por la aplicación):** "Una clave incorrecta muestra el aviso": el aviso es un `MessageBox` modal y UI Automation no lo lee.

**Pendientes de presentación detectados en pantalla (no corregidos):** la columna "Venta teorica" de Producción muestra `3145.833333` sin formato de moneda (el prefijo "Venta" no está en la lista de montos de `Ui`); el food cost muestra `55,121882 %` con todos los decimales; el resumen de la matriz dice "1 minutas" y "1 servicios".

## Entrega 10: numeración de migraciones y base local (2026-10-06)

- **Problema:** la base local `appsistema` tenía V001 a V025 de otras ramas (`feature/formatos-sgp`, `feature/planificacion-menus-matriz`), donde V022 a V025 son otras migraciones. Las de esta rama (V022 a V029, sin confirmar) usaban los mismos números. Al abrir "Plan operativo del chef" fallaba con `no existe la relación «minuta_ajuste_operativo»`.
- **Decisión:** las migraciones pendientes de esta rama pasan a V026 a V033, después de V025. No se editó ninguna migración aplicada.
  - V026 perfiles de la operación; V027 aprobación de adicionales; V028 solicitud de devolución; V029 motivo del adicional; V030 raciones operativas; V031 motivos normalizados del ajuste; V032 foto de movimientos al cerrar; V033 traspaso en tránsito.
- **Verificación:** prueba en una copia de `appsistema` (aplicó limpio); suite de datos 123/123 con bases nuevas; consultas SQL 469/469 sin error.
- **Respaldo previo:** `artifacts/respaldo/appsistema_antes_de_migrar.backup` (restaurable con `pg_restore`).
- **Pendiente:** unificar el historial con las ramas `feature/*`, que siguen usando V022 a V025 para otro contenido. Hay que decidir cuál línea es la oficial antes de fusionarlas.

## Entrega 11: planilla del menú en formato SGP (2026-10-06)

- **Pantalla nueva:** Menús → **Planilla del menu (estructura x dia)** (`FormPlanillaMenu`). Estructuras en filas, días en columnas. Cada celda muestra receta, raciones y % sobre comensales. Filas finales: comensales y costo de la minuta del día.
- **Edición:** doble clic en una celda (o "Cambiar receta o raciones..."): cambia la receta (`SustituirReceta`), las raciones (`FijarRaciones`, nuevo) o crea el plato si la celda está vacía (crea la minuta del día si no existe). "Comensales del dia..." cambia los comensales (`ActualizarComensales`). Solo en minutas en borrador; las aprobadas se rechazan en el servicio.
- **Factores de consumo:** se cambian en Menús → Servicios y estructuras (factor teórico) y en Minutas → "Factores de la operacion..." (factor de la operación).
- **Prueba:** `Planilla_cambia_raciones_solo_en_minuta_en_borrador` (raciones 0 rechazadas, cambio en borrador, bloqueo al aprobar). Datos 123 + 1 pasando en `TeoricoRealTests`.
- **E2E:** la opción aparece en el recorrido de pantallas y abre sin error. El arnés ya no usa teclado para cerrar menús.
- **Pendiente:** cargar la planilla del SGP que se pegó (Excel/PDF del menú) a la base; hoy la base real no tiene minutas.

## Entrega 12: perfiles de prueba en la base local (2026-10-06)

- **Comando nuevo:** `AppSistema.Instalador perfiles-prueba` (empresa y operación). Crea una cuenta por rol de la operación con clave generada que cumple la política. Repetible: las cuentas existentes no cambian.
- **Perfiles:** `chef_prueba` (CHEF), `almacen_prueba` (ALMACEN), `jefe_almacen_prueba` (JEFE_ALMACEN), `operaciones_prueba` (OPERACIONES), `planificacion_prueba` (PLANIFICADOR_CENTRAL), `compras_prueba` (COMPRAS_CENTRAL). Todos en la operación ORC de la empresa DEMO.
- **Claves:** se muestran una vez y se guardan en `%LOCALAPPDATA%\AppSistema\perfiles_prueba.txt`, fuera del repositorio. No están en Git.
- **Base local:** creados los seis en `appsistema`; verificado en la base que cada uno tiene su rol. `admin` e `iomar` no se tocaron.
- **Pendiente de revisión:** existe la cuenta de administración local (rol CHEF) que no creó el sistema. Confirmar con el usuario si es suya y si debe conservarse.
- **Verificación:** prueba `PerfilesPruebaTests` (2 pruebas): crea una vez, no repite, cada perfil entra con su clave y el rol correcto; rechaza empresa u operación inexistentes. Pasa en base nueva.

## Entrega 13: revisión de formularios y base de datos contra el requerimiento (2026-10-06)

- **Informe:** `docs/02_REQUERIMIENTOS/REVISION_REQUERIMIENTOS_2026-10-06.md`. Compara 113 requisitos (RF, RNF y RN) con el código, las migraciones y la base.
- **Resultado:** 31 cumplen, 49 parciales, 25 faltan, 5 no verificados, 3 dependen de decisiones del usuario.
- **Base de datos:** la base real tiene 17 tablas y una columna de otras ramas (`planificacion_*`, `produccion_plan*`, `sgp_*`, `operacion.dias_stock_base`). Las `sgp_*` tienen unas 15 000 filas y no se usan en esta rama: no se borran sin decisión.
- **Hallazgos de auditoría:** `movimiento_stock` y `saldo_stock` no tienen trigger de auditoría; hay tablas de central y sincronización sin RLS (confirmar si deben ser globales).
- **Pendiente:** decisiones P-01, P-02 y P-13; planificación real y tolerancia de cierre de tres días; buscador y Cto.Plat. en la planilla; fecha máxima de pedido; reportes de recetas del día y facturación.

## Entrega 14: documentación ordenada en docs/ (2026-10-06)

- **Índice:** `docs/00_LEEME.md` (dónde está cada cosa y cómo agregar documentos).
- **Carpetas:** `01_PROMPTS` (06 y 07, versión vigente de Ventana), `02_REQUERIMIENTOS` (requerimientos, revisión, diagnóstico y especificación de Ventana), `03_ESTADO` (checklist, continuación, seguimiento), `04_ARQUITECTURA_Y_DATOS`, `05_OPERACION`, `06_GUIA_DE_CONSTRUCCION` (con su manifiesto), `07_SKILLS` (índice de `.agents/skills`), `08_MEMORIA_DEL_ASISTENTE` (copia).
- **Duplicados eliminados:** las copias viejas de la especificación que estaban en `docs/`. La versión vigente es la de `Ventana/`, ahora en `02_REQUERIMIENTOS/ESPECIFICACION_VENTANA/`.
- **Archivos de usuario:** `Pantalla/` y `Pantalla.zip` pasaron a `datos/pantallas/origen/`, según la regla de archivos subidos.
- **Referencias:** actualizadas en CLAUDE.md, README.md, documentos, y comentarios de tres archivos de código. No se tocaron migraciones (tienen hash).
- **Verificación:** compilación del escritorio sin errores.
- **Pendiente:** `.agents/skills` se queda donde la herramienta lo busca; el índice está en `07_SKILLS`.

## Entrega 15: vista de la minuta teórica y real del SGP (2026-10-06)

- **Migraciones V034 y V035** (condicionales: solo actúan si existe el plan del SGP en la base): vistas `v_minuta_teorico_real_plato` (estructura y receta, teórico y real lado a lado) y `v_minuta_teorico_real_dia` (comensales, costo por bandeja y costo total por día y servicio). `security_invoker`: respetan el aislamiento por empresa. V035 redondea a 6 decimales: V034 daba importes de hasta 53 dígitos, que no caben en decimal de .NET. V034 no se editó (ya aplicada).
- **Reporte nuevo** en Reportes → Planificación: "Minuta teorico vs real (plan SGP)" con impresión, Excel, PDF y CSV.
- **Verificación con datos reales** (`MinutaRealTests`, requiere APPSISTEMA_BD_LOCAL y el usuario de prueba): 276 días y servicios (92 días × 3) y platos del 01/08/2026 al 31/10/2026. Control de la vista contra las tablas base: diferencia 0.
- **Resultado:** costo total teórico frente a real: agosto S/ 189 278 frente a S/ 199 677; septiembre S/ 202 106 frente a S/ 226 681; octubre S/ 224 317 frente a S/ 224 985. Platos: 5 484 planificados, 42 no planificados y 36 sin salida.
- **Pendiente:** el PDF se generó pero no se pudo revisar visualmente en esta máquina (falta pdftoppm); revisar el archivo.

## Entrega 16: vista de la minuta teórica y real en el formato del SGP (2026-10-06)

- **Pantalla nueva:** Menús → **Minuta teorica y real (SGP)** (`FormMinutaVista`, solo lectura). Sigue el modelo de la planilla del SGP que entregó el usuario: estructuras con sus platos, y por cada día cuatro columnas (N.Rac., Por.(%), Costo y Cod. Receta). Filas finales: comensales, costo de la minuta del día, estado, costo del otro nivel y diferencia.
- **Mejoras frente al modelo:** Por.(%) calculado en teórico y en real (raciones / comensales del día); fila de diferencia real − teórico por día; estado "Sin real" cuando el día no tiene plan real; filtros de servicio, nivel (teórico o real) y rango de fechas.
- **Servicio:** `ServicioPlanSgp` (Datos), lee `sgp_plan_dia` y `sgp_plan_plato` con la operación de la sesión y el permiso de menús. Si la base no tiene el plan del SGP (base limpia de esta rama), la vista queda vacía y no da error.
- **Verificación con datos reales** (`MinutaRealTests`): para cada día y servicio, en teórico y en real, Σ(raciones × costo por ración) / comensales coincide con el costo de la minuta (tolerancia de céntimos). Octubre de 2026 completo.
- **Texto de muestra:** `artifacts/reportes/planilla_sgp_almuerzo_oct.txt`, con el mismo diseño de la planilla para revisar.
- **Recorrido E2E:** la pantalla abre sin error. Antes fallaba por un error inesperado cuando la base no tenía el plan; ahora se controla.
- **Migraciones:** V034 y V035 (vistas de la minuta teórica frente a la real) siguen vigentes; esta entrega no agrega migraciones.
- **Pendiente:** revisar la pantalla con el usuario (la captura de esta máquina sale en negro, no se pudo ver el resultado visual); resaltar en color las celdas no planificadas.


### Entrega 17 (2026-10-06): menú mensual en JSON (planilla SGP)

- **Esquema acordado con el programador:** `operacion` (codigo = código del contrato, nombre, regimen, servicio), `resumen_mes` (planificado: materia_prima, raciones = comensales del mes, costo_bandeja; realizado: null), `programacion_diaria` (fecha, dia_semana, costo_minuta_dia, comensales, items).
- **Decisiones del usuario:** categoria_id = número de estructura (orden) del plan SGP; estado = R (plan real); realizado = null (no hay datos); resumen mensual; código del contrato desde la operación.
- **Estado de los ítems:** `null` en todos. El diccionario del usuario define R = Realizado/Bloqueado y H = Habilitado; no hay dato que decida cuál aplica, así que no se inventa. (Antes se exportaba "R" como plan real: era un error de interpretación y se corrigió.)
- **Campos sin dato:** no se incluyen en el JSON.
- **Ajustes de presentación (mismo corte):** "Venta teórica" (`VentaPrevistaU6`) se muestra como dinero; Food Cost, objetivo y desviación con dos decimales (`Ui.Porcentaje`); plural correcto en el aviso de servicios del chef.
- **Verificación del corte:** compilación del escritorio sin errores; datos 129 de 129; dominio 102 de 102.
- **Migración V036** (`codigo_contrato` en `operacion`, nullable). Aplicada en la base local. La exportación no inventa el código: si falta, da CONTRATO_PENDIENTE.
- **Servicio:** `ServicioPlanSgp.MenuMensualJson(servicioPlan, desde, hasta)`. El nombre del servicio y el régimen salen de `sgp_comparativo_dia`; si no hay una sola coincidencia, quedan el nombre del plan y régimen vacío.
- **Pantalla:** botón "Exportar JSON..." en la planilla SGP (`FormMinutaVista`).
- **Verificación:** `MinutaRealTests.Menu_mensual_json_sigue_el_esquema_acordado_con_el_plan_real_de_octubre` (los seis platos del ejemplo, realizado null, materia del día ≈ 4 485,89). Suite de datos: 129 de 129 superadas. Muestra: `artifacts/reportes/menu_mensual_almuerzo_oct.json`.
- **Sin verificar en pantalla:** el botón no se probó en la interfaz; la aplicación estaba abierta y bloqueó la compilación del escritorio, así que solo se verificó que el código compila a nivel de Datos.
- **Costo por bandeja:** `costo_minuta_dia` es el costo por bandeja del día (sgp_plan_dia.costo_minuta_dia_u6), no el total. Confirmar con el usuario el significado del campo en el modelo.
