# Checklist de avance

Actualizado: 2026-10-03. Se actualiza en cada entrega.

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
| Catálogo > Productos, variantes y empaques | CATALOGO_VER | Editar, sin costo de compra y corregir contenido: CATALOGO_EDITAR | 🟡 |
| Catálogo > Proveedores y precios | CATALOGO_VER | Proveedores: PROVEEDORES_EDITAR · Precios: PRECIOS_EDITAR | 🟡 |
| Catálogo > Importar catálogo | CATALOGO_IMPORTAR | Solo acepta archivos de catálogo o del listado SGP | 🟡 |
| Menús > Recetas | MENUS_VER | Editar: RECETAS_EDITAR · Aprobar y retirar: RECETAS_APROBAR | 🟡 |
| Menús > Minutas y necesidades | MENUS_VER | Planificar y cambiar comensales: MINUTAS_EDITAR · Aprobar y factores de la operación: MINUTAS_APROBAR · Imprimir minuta: todos | 🟡 |
| Menús > Servicios y estructuras | MENUS_CONFIGURAR | Estructura teórica, factor teórico y Food Cost objetivo | 🟡 |
| Menús > Importar recetas | RECETAS_EDITAR | Solo acepta archivos de recetas | 🟡 |
| Menús > Producción | MENUS_VER | Requerimientos, producción, merma, venta real y consumo: PRODUCCION_EDITAR · Entregar: STOCK_CONTABILIZAR · Teórico vs real e imprimir requerimiento: todos | 🟡 |
| Compras > Previsión y pedidos | COMPRAS_VER | Calcular, pedidos y reservas: COMPRAS_EDITAR · Aprobar y anular: COMPRAS_APROBAR | 🟡 |
| Almacén > Stock e inventario inicial | CATALOGO_VER | Movimientos: STOCK_CONTABILIZAR · Imprimir kárdex y stock valorizado: todos | 🟡 |
| Almacén > Inventario físico | INVENTARIO_CONTAR | Revisar y autorizar ajuste: INVENTARIO_APROBAR · Imprimir hoja de conteo y resultado: todos | 🟡 |
| Cierres y control > Pendientes, cierres y Food Cost | REPORTES_VER | Cerrar, ingresos, venta por estructura y objetivo: CIERRE_EJECUTAR | 🟡 |
| Cierres y control > Contratos y clientes | CONTRATOS_VER | Editar: CONTRATOS_EDITAR | 🟡 |
| Cierres y control > Gastos y resultado mensual | RESULTADOS_VER | Gastos: GASTOS_EDITAR | 🟡 |
| Administración > Usuarios y roles | USUARIOS_ADMINISTRAR | Incluye roles propios | 🟡 |
| Administración > Operaciones y almacenes | USUARIOS_ADMINISTRAR | **Nueva** | 🟡 |
| Administración > Auditoría | AUDITORIA_VER | **Nueva**, solo lectura | 🟡 |
| Administración > Sincronización y respaldo (TI) | USUARIOS_ADMINISTRAR | **Nueva**. Además pide la conexión del propietario de la base (no se guarda). Sede: estado de la cola, configurar, enviar ahora, respaldar y conciliación. Central: sedes, registrar (la credencial se muestra una vez) y desactivar | 🟡 |
| Administración > Carga de datos reales | CATALOGO_IMPORTAR | **Nueva**. Precios: PRECIOS_EDITAR · Sin costo: CATALOGO_EDITAR · Estructuras: MENUS_CONFIGURAR · Ciclo: MINUTAS_EDITAR (aprobar: MINUTAS_APROBAR). Catálogo, recetas e inventario inicial abren su pantalla propia | 🟡 |
| Barra de estado | REPORTES_VER | Estado de envío a la central | 🟡 |

Los menús sin ninguna opción para el usuario ya no se muestran.

### Qué ve cada rol base

| Menú | ADMIN | SUPERVISOR | ALMACEN | COCINA | FINANZAS |
|---|---|---|---|---|---|
| Catálogo (ver) | ✔ | ✔ | ✔ | ✔ | — |
| Catálogo (editar, importar, precios) | ✔ | ✔ | — | — | — |
| Recetas / Minutas / Producción (ver) | ✔ | ✔ | ✔ | ✔ | — |
| Recetas y minutas (editar) | ✔ | ✔ | — | ✔ | — |
| Recetas y minutas (aprobar), factores de la operación | ✔ | ✔ | — | — | — |
| Servicios y estructuras | ✔ | ✔ | — | — | — |
| Producción: registrar, venta real, consumo | ✔ | ✔ | — | ✔ | — |
| Producción: entregar (almacén) | ✔ | ✔ | ✔ | — | — |
| Compras (ver y preparar) | ✔ | ✔ | ✔ | — | — |
| Compras (aprobar) | ✔ | ✔ | — | — | — |
| Almacén: stock y movimientos | ✔ | ✔ | ✔ | ver | — |
| Inventario físico: contar | ✔ | ✔ | ✔ | — | — |
| Inventario físico: autorizar | ✔ | ✔ | — | — | — |
| Cierres (ver) | ✔ | ✔ | — | — | ✔ |
| Cierres (ejecutar) | ✔ | ✔ | — | — | — |
| Contratos, gastos y resultado | ✔ | ✔ | — | — | ✔ |
| Usuarios, operaciones y almacenes | ✔ | — | — | — | — |
| Carga de datos reales | ✔ | ✔ | — | — | — |
| Sincronización y respaldo (TI) | ✔ | — | — | — | — |
| Auditoría | ✔ | ✔ | — | — | — |

Además, el administrador puede crear **roles propios** con los permisos que elija.

## 2. Corregido en la revisión de pantallas y accesos (2026-10-03)

- ✅ **Botones por posición.** En Minutas, "Aprobar" se veía para todos y "Cambiar comensales" exigía el permiso de aprobar. Ahora todos los botones con permiso se definen con `Ui.BotonSi`, en 9 pantallas.
- ✅ **Importación duplicada.** Las dos opciones abrían la misma ventana. Ahora cada una tiene su modo (catálogo o recetas) y rechaza el archivo del otro tipo, indicando a qué menú ir.
- ✅ **Factores de la operación.** Pasaron de Servicios y estructuras a **Minutas**. Allí aparecen con el permiso que los exige (MINUTAS_APROBAR); antes estaban en una ventana con otro permiso.
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
| Reportes imprimibles y exportables (HTML para imprimir o PDF, CSV para Excel), con el permiso de la pantalla de origen | ✅ |

## 4. Pendiente por crear o confirmar

| # | Pendiente | Depende de |
|---|---|---|
| 1 | ⬜ **Probar la aplicación en Windows 10+**: abrir cada pantalla del punto 1 y anotar los ajustes | PC con Windows |
| 2 | ⬜ Enlazar los 85 ingredientes de `datos/real/ingredientes_por_revisar.csv` (ajo molido, mayonesa, ajíes molidos, panes de marca…) | Usuario |
| 3 | ⬜ Confirmar los 20 precios de `datos/real/precios_atipicos.csv` | Usuario |
| 4 | ⬜ Factores de consumo reales por operación y servicio, y confirmar las estructuras propuestas | Usuario |
| 5 | ⬜ Recetas con precio para cereales y yogurt del desayuno | Usuario |
| 6 | ⬜ D02 (regla de precio del ingrediente), D03 (impuestos) y D10 (¿una sede o varias?) | Usuario |
| 7 | 🟡 Pantalla para la carga de datos reales: **hecha** (Administración > Carga de datos reales, 7 pasos con el estado de lo cargado). Falta probarla en Windows | Programación (hecho) · PC con Windows |
| 8 | 🟡 Pantalla de sincronización y respaldo para TI: **hecha** (Administración > Sincronización y respaldo). Pide la conexión del propietario y no la guarda. Restaurar y actualizar siguen solo en el Instalador, a propósito. Falta probarla en el piloto | Programación (hecho) · Piloto |
| 9 | ⬜ Resultado consolidado de varias sedes en la central | Programación, con D10 |
| 10 | 🟡 Reportes imprimibles o exportables: **hecho** (minuta del día, requerimiento, kárdex, hoja de conteo, resultado de inventario y stock valorizado). Falta verlos impresos desde Windows | Programación (hecho) · PC con Windows |
| 11 | ⬜ Piloto de un mes en una sede (`docs/PILOTO_ETAPA_8.md`) | Sede real |
| 12 | ⬜ Integraciones (SAP, ADS, SGO) | Especificación del cliente |
