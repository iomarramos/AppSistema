# Checklist de avance

Actualizado: 2026-10-03, 09:55 hora de Lima. Se actualiza en cada entrega.

**Leyenda**

| Marca | Significado |
|---|---|
| ✅ | Hecho y probado (pruebas automáticas o carga de punta a punta) |
| 🟡 | Hecho en código, pero falta validarlo en Windows o con el usuario |
| ⬜ | Pendiente |

## 1. Pantallas (front) por menú

Todas las pantallas **compilan, pero ninguna se ejecutó en Windows todavía** (🟡). Réplica visual: https://claude.ai/artifact/LqViLBPDyFwUqMktDxNqaz

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
| Almacén > Stock e inventario inicial | CATALOGO_VER | Movimientos: STOCK_CONTABILIZAR · Imprimir kárdex y stock valorizado: todos | 🟡 |
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

  Falta verlo en Windows. Detalle en `docs/DISENO_PANTALLAS.md`.
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
| Comparativo teórico → plan real → realizado (fórmula del SGP, probada con sus cifras: `PlanVsRealTests`) | ✅ |
| **Planificación y Abastecimiento Central, fase 1 (seguridad):** superusuario, módulo → pantalla → acción → alcance (operación, zona o todas), matriz de acceso, roles centrales, stock y factores validados en la base (T53, T54, T61, T62, T63) | ✅ |

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
| 16 | ⬜ **Menú del mes real** y **estructuras de servicio** que faltan (loncheras, refrigerios, coffee break), además de confirmar las de desayuno, almuerzo y cena. Estado actual en `docs/ESTADO_BASE_DATOS.md` | Usuario (los va a presentar) |
| 17 | ⬜ **Planificación y Abastecimiento Central, fases 2–10** (`docs/ARQUITECTURA_PLANIFICACION_CENTRAL.md`): planificación teórica con versiones, liberación, plan operativo, programación del día del chef, requerimiento y adicional, ejecución real, 3 comparativos, aprendizaje de factores, compras globales con almacén central y tránsito, controles reutilizables | Programación (fase 1 hecha) |
| 18 | 🟡 **Corte del requerimiento interno con retrasos permitidos** (respuesta del usuario): hora configurable por operación; lo tardío se acepta marcado TARDÍO; tiempo de llegada por zona desde el central. Falta la hora por defecto y los días por zona; se programa en las fases 4–5 y 8–9 | Usuario (dato) · Programación |
| 19 | ✅ **Zonas:** Costa, Sierra y Selva (solo esos valores). Se elige en Operaciones y almacenes | Usuario (respondido) |
| 20 | 🟡 **Modelo objetivo del SGP** (`datos/plan_real/`): leído y verificado (10 de 10 reglas cuadran). Por construir, dentro de las fases: cuadrícula mensual de planificación (2), reporte de frecuencia, requisición por rango con preparación (4–5), comparativo de tres niveles (6; fórmula ya hecha), productos sin movimiento y registro permanente valorizado, servicios lonchera, rancho, venta directa y consumo fijo | Programación · Usuario (nutrientes, cena de octubre, piso y techo) |
| 11 | ⬜ Piloto de un mes en una sede (`docs/PILOTO_ETAPA_8.md`) | Sede real |
| 12 | ⬜ Integraciones (SAP, ADS, SGO) | Especificación del cliente |
