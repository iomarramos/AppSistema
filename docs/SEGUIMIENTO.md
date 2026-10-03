# Seguimiento del proyecto

Actualizado: 2026-10-03. Registra **solo lo ejecutado y comprobado**; una tabla o pantalla no es "terminado".
Plan: `docs/guia_construccion/04_PLAN_POR_ETAPAS.md`. Decisiones de negocio: `docs/guia_construccion/09_SEGUIMIENTO_Y_DECISIONES.md`.

## Decisiones técnicas tomadas

| ID | Decisión | Fecha | Origen |
|---|---|---|---|
| RNF-12 | Lenguaje **VB.NET** | 02/10/2026 | Usuario |
| RNF-12.1 | **Windows 10 o superior** | 02/10/2026 | Usuario |
| RNF-13 | **PostgreSQL**, servidor por sede | 02/10/2026 | Propuesta aceptada |
| RNF-14 | ≈20 computadoras | 02/10/2026 | Usuario (por confirmar si por sede o total) |
| RNF-15 | Interfaz **WinForms** (.NET 8) | 02/10/2026 | Propuesta aceptada |
| RNF-16 | Ramas `main` ← `develop` ← `feature/etapa-N-*` | 02/10/2026 | Usuario |
| D12 Stock en cocina | **Se descarga toda la presentación**: el almacén entrega presentaciones completas y lo entregado se da por consumido (sin stock crudo en cocina) | 03/10/2026 | Usuario |
| D01 Valoración | **Cantidad × precio**: entradas a su costo; salidas a cantidad × costo vigente del saldo (promedio ponderado móvil por almacén y variante) | 03/10/2026 | Usuario |
| Moneda | **Soles (PEN)** para precios del SGP, inventario y costos; es el valor por defecto en el sistema | 03/10/2026 | Usuario |
| D13 Venta y Food Cost | **Food Cost = costo / venta; la venta sale de la estructura del menú**: cada componente (bebida caliente, jugo, panes, fondo o sopa, complementos, huevo, mantequilla, mermelada, yogurt, ensalada, fruta, cereales…) tiene un factor de consumo (plato caliente ≈ 100 %, complementos 30–70 %) y sus alternativas se reparten (jugo 50/50); venta = costo previsto de la estructura / Food Cost objetivo **48 %** (margen 52 %), ajustable por servicio | 03/10/2026 | Usuario |
| Teórico vs real | **Los factores son teóricos y la operación los actualiza** según su consumo real; la minuta lleva el total de comensales y toda la estructura se calcula con los factores (cambiar comensales recalcula); se **carga la venta real** del servicio; se compara planificado vs realizado en raciones (planificadas, preparadas, consumidas, vendidas/no vendidas), venta, costo y **por producto**, incluidos los que salieron del almacén para el servicio sin estar planificados | 03/10/2026 | Usuario |

## Estado por etapa

| Etapa | Estado | Qué hay / qué falta |
|---|---|---|
| 0 Diagnóstico y línea base | **Hecha** | Esquema portado a PostgreSQL; migraciones V001–V015 con migrador versionado; brechas H01–H03 cerradas |
| 1 Fundamentos y catálogo | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Acceso con bloqueo, permisos por operación, auditoría automática, aislamiento RLS, operaciones/almacenes/usuarios, unidades, categorías, marcas, productos, variantes, empaques, proveedores, precios con vigencia, importador CSV con vista previa, **carga del listado de productos del SGP** (4 158 productos con factor y unidad mínima de pedido; `datos/sgp/`). Pantallas WinForms compiladas **pero no ejecutadas** (no hay Windows en este entorno) |
| 2 Menús y recetas | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Servicios, regímenes y estructuras; recetas versionadas (borrador → aprobada inmutable → retirada) con rendimiento, ingredientes por producto base y variantes permitidas; minutas por día y servicio con platos y fijos; aprobación con snapshot de costo (fuente y fecha por ingrediente); costo simulado; necesidades consolidadas por producto. **Recetas del SGP cargadas**: 946 recetas (417 fichas revisadas + 529 del Recetón) con 412 ingredientes (`datos/recetas/`). Pantallas: Recetas, Minutas y necesidades, Servicios y estructuras, Importar recetas. **Regla de precio provisional** (D02 pendiente): menor costo vigente entre las variantes permitidas; sin precio → "pendiente". El precio se usa tal como se registró (D03 impuestos pendiente) |
| 3 Previsión y compras | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Previsión por almacén desde minutas aprobadas: demanda del horizonte, consumo puente (una sola vez), stock actual, reserva por producto y almacén (D08 como parámetro, 0 por defecto), pendientes de pedidos aprobados menos lo recibido, **recorrido por fechas** con fecha de quiebre (un tránsito tardío no oculta la falta). Validación y obsolescencia (recalcula y compara; los pedidos de la propia previsión no la vuelven obsoleta). Pedido generado por proveedor con el empaque de menor costo vigente y redondeo por mínimo/múltiplo (D04 propuesto); pedidos manuales; aprobación (exige previsión vigente) y anulación; inmutables tras aprobar. Pantalla Compras > Previsión y pedidos |
| 4 Almacén y kárdex | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Inventario inicial valorizado (apertura). Recepción de pedidos total o parcial (conversión histórica del pedido, costo = empaques × precio del comprobante, sin exceder lo pendiente, comprobante único, pedido pasa a parcial/recibido). Salidas a cocina, bajas con motivo, devoluciones al costo de la entrega sin exceder lo entregado, traspasos entre almacenes de la operación. **Valoración D01 (usuario: "cantidad × precio")**: entradas a su costo, salidas a cantidad × costo vigente del saldo (promedio móvil), la última salida se lleva el valor restante. Kárdex valorizado con saldo corrido. Pantalla Almacén > Stock |
| 5 Producción | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Requerimiento calculado desde la minuta aprobada (uno por minuta) y adicionales; entrega del almacén en **presentaciones completas que se dan por consumidas** (D12, usuario), eligiendo la presentación con menor excedente e informando faltantes; devoluciones al costo de la entrega; raciones producidas/servidas/excedentes coherentes; mermas analíticas (incluidas en lo entregado, sin segunda baja) o con baja de almacén; previsto vs real por producto y **costo real del servicio** (D14: sin reparto por receta). Pantalla Menús > Producción |
| 6 Inventarios | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Inventario general o rotativo con fotografía de stock y costo al corte (inmutable); hoja de conteo por envases + parcial, ciega opcional; importación del conteo; celda vacía = pendiente; reconteo; diferencias físico − sistema sin tocar el saldo; **mientras se cuenta, esas variantes no admiten movimientos**; revisión y autorización por quien no contó; ajuste en documentos separados (positivo al costo del corte, negativo al costo vigente), una sola vez. Pantalla Almacén > Inventario físico. D06/D07 aplicados según la propuesta de la guía (usuario: "procede") |
| 7 Cierres y control | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Tablero de pendientes por día (minuta sin aprobar, producción sin registrar, requerimiento pendiente, documentos y recepciones en borrador, inventario abierto, conciliación saldo/libro: bloquean; pedido vencido: aviso). Cierre diario serializado con las contabilizaciones (bloquea los almacenes: una salida simultánea queda antes o es rechazada) y validaciones guardadas. Ingreso mensual neto por servicio y objetivo de Food Cost; reporte mensual por servicio (raciones, costo de alimentos consumidos, costo por ración, ingreso, Food Cost, desviación y diferencia contra presupuesto) más bajas y ajustes de inventario. Cierre de mes: exige todos los días con actividad cerrados; después movimientos, ingresos y gastos del mes no cambian (BD) y el reporte se repite igual. **D13 (usuario)**: la venta sale de la estructura del menú — factor de consumo por componente y reparto de alternativas; venta = costo previsto / Food Cost objetivo (48 % por defecto, margen 52 %); Food Cost real = consumo real / venta. *Generar venta (estructura)* la carga como ingreso del mes; un ingreso manual o de contrato tiene prioridad. Pantalla Cierres > Pendientes, cierres y Food Cost |
| 8 Continuidad y piloto | **Parcial: hecho en código y pruebas; falta el piloto real** | **D10 aplicado según la propuesta de la guía (autoridad local por sede)**: cada sede tiene su servidor PostgreSQL y opera sin internet. Cola de salida escrita en la misma transacción que el documento o cierre (un corte no deja media operación), UUID global y secuencia por sede sin huecos, eventos inmutables, versión de contenido. Agente `sincronizar` con reintentos de espera creciente que no adelanta eventos. Central: credencial por sede (solo hash), rechazo registrado sin contenido, reenvíos sin duplicar, misma clave con otro contenido = conflicto, orden por sede y dependencias (lo que llega antes de su antecesor se retiene), datos por sede sin sobrescritura, reporte con última sincronización y stock por sede. Respaldo consistente (pg_dump con la misma fotografía que la conciliación) y restauración en base vacía conciliada. Migración de una base con historia (V011 → V012) conservando saldos, y alta de la historia en la cola. Comando `actualizar` (respaldo + migraciones + conciliación de saldos e historia). Estado de envío visible en la aplicación (barra de estado y Cierres: "por enviar / último envío"). Script `herramientas/windows/programar_sede.ps1` para programar envío y respaldo diario sin guardar claves. **Falta:** piloto de un mes en sede real (plan en `docs/PILOTO_ETAPA_8.md`), ensayo de corte de energía físico y prueba del script en Windows |
| 9 Extensiones | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Clientes y contratos mensuales de la operación con vigencia; líneas por servicio con importe mensual que no se superponen; **ajuste** desde una fecha (cierra la línea anterior y abre otra; el importe nunca se edita); nada cambia un mes cerrado. **Ingreso desde contrato** prorrateado por días (alternativa a la venta por estructura), sin pisar un ingreso manual, repetible. Gastos de personal, operación, administración y otros, reales o presupuestados, por servicio o comunes. **Resultado mensual** por servicio: ingreso − alimentos − gastos = margen y margen %, con "No asignado" para gastos comunes, bajas y ajustes; cada cifra rastreable (fuente del ingreso, documentos, registro del gasto). **Roles propios** (accesos avanzados): el administrador crea roles con los permisos que elija, quita roles, nunca deja la empresa sin administrador; rol base nuevo FINANZAS (se agrega a empresas existentes al actualizar). Exportación CSV con contrato de datos v1 (`docs/INTEGRACION_RESULTADOS.md`). Pantallas: Cierres > Contratos y clientes, Gastos y resultado mensual; Administración > Usuarios (roles). **No incluido** (la guía exige especificación y entorno de prueba): integraciones con SAP/ADS/SGO; resultado consolidado multi-sede en la central |

## Casos de la guía ejecutados

| Caso | Dónde | Estado |
|---|---|---|
| T01 aislamiento de referencias / almacén ajeno | SQL, servicio de stock, RLS | Pasa |
| T02 búsqueda por texto y vistas sin fuga | SQL (RLS + `security_invoker`) y `CatalogoTests` | Pasa (y falla al quitar el RLS: verificado) |
| T03 código de variante repetido | `CatalogoTests` | Pasa |
| T04 conversiones 4×4 L, 5 L distinta | Dominio, SQL, servicio | Pasa |
| T05 kg→L sin regla | Dominio | Pasa |
| T06 cambio de presentación usada no altera historia | SQL y servicio | Pasa |
| T08, T09 escalado de receta | Dominio | Pasa |
| T12–T15 previsión y empaques | Dominio, SQL (T14) y `ComprasTests` (T12, T13 con la base) | Pasa |
| T16 tránsito tardío | Dominio y `ComprasTests` | Pasa |
| T17 dos variantes, asignación única | Dominio (`Prevision.Asignar`) | Pasa |
| T18 previsión obsoleta tras cambiar minuta o stock | `ComprasTests` | Pasa |
| T19, T20, T21, T23, T24, T25, T26, T39 | SQL, concurrencia real y servicio | Pasa |
| T33, T34 conteo sin ajuste, celda vacía | SQL y `InventariosTests` | Pasa |
| T35 ajuste aplicado una sola vez | `InventariosTests` | Pasa |
| T36 movimiento durante el conteo | SQL y `InventariosTests` (bloqueado) | Pasa |
| T07 variante de otro producto en receta | `MenusTests` (trigger de BD) | Pasa |
| T10 precio nuevo no cambia minuta aprobada | `MenusTests` (snapshot y bloqueo en BD) | Pasa |
| T11 ingrediente sin costo → costo pendiente | Dominio y `MenusTests` | Pasa |
| Aceptación etapa 2: 10 raciones/1 L → 150 = 15 L, rendimiento cero, consolidado sin duplicar | `MenusTests` | Pasa |
| T27 recepción 16 + 16 de un pedido de 32 L | `AlmacenTests` | Pasa |
| T28 recibir más que el saldo del pedido | `AlmacenTests` (se rechaza, D11 por defecto) | Pasa |
| T29 devolver más que lo entregado | `AlmacenTests` | Pasa |
| T30 agotar stock con costo decimal | Dominio (`ValoracionTests`) y `ProduccionDatosTests` | Pasa |
| T31 entrega 5 + adicional 1 − devolución 0,5 = 5,5 L | `ProduccionDatosTests` | Pasa |
| T32 merma 0,2 ya incluida, sin segunda baja | `ProduccionDatosTests` | Pasa |
| T37 cierre con pendientes | `CierresTests` (no cierra, deja validaciones) | Pasa |
| T38 salidas simultáneas con el cierre | `CierresTests` (8 salidas + cierre en paralelo: incluidas antes o rechazadas después) | Pasa |
| T40 Food Cost 4 200 / 10 000 = 42 %, +2 puntos, +S/200 vs presupuesto | Dominio (`FoodCostTests`) y `CierresTests` | Pasa |
| T41 servicio sin ingreso → no calculable | Dominio y `CierresTests` | Pasa |
| T48 mes cerrado: no cambia y el reporte se repite | `CierresTests` y trigger de BD | Pasa |
| T22 misma clave idempotente, distinto contenido | `ContinuidadTests` (conflicto, sin movimiento) | Pasa |
| T42 reenvío tras perder el acuse | `ContinuidadTests` (sin duplicados; stock central = sede) | Pasa |
| T43 evento hijo antes que el padre | `ContinuidadTests` (se retiene y se aplica en orden) | Pasa |
| T44 sede no autorizada | `ContinuidadTests` (rechazo registrado sin contenido; el usuario de sincronización no lee tablas) | Pasa |
| T45 restaurar copia | `ContinuidadTests` e instalador de punta a punta (mismos recuentos, saldos y referencias) | Pasa |
| T46 migrar base con documentos históricos | `ContinuidadTests` (V011 con documentos → V012: saldos y libro iguales; historia a la cola en orden) | Pasa |
| Etapa 9: ajuste a mitad de mes, ingreso prorrateado, mes cerrado intocable, resultado trazable, roles propios, aislamiento | Dominio (`ContratosTests`) y `ExtensionesTests` | Pasa |
| D13 venta por estructura: desayuno de 500 con factores (100 %, 50/50, 70 %, 30 %) → costo S/ 1 510, venta S/ 3 145,83 al 48 %; consumo real S/ 1 600 → Food Cost 50,86 % | Dominio (`VentaEstructuraTests`) y `VentaEstructuraDatosTests` | Pasa |
| Teórico vs real: comensales recalculan raciones; factor por operación; venta real 470 de 500; consumo por componente (factor real 63,83 %); costo teórico S/ 1 510 vs real S/ 1 630; producto no planificado detectado; día cerrado protege lo real | `TeoricoRealTests` | Pasa |
| T47 importación repetida y filas inválidas | `ImportacionTests`, `ImportacionSgpTests` (listado real del SGP) | Pasa |

**Todos los casos T01–T48 de la guía tienen prueba**; lo que falta es el piloto en una sede real.

## Evidencia

```bash
./ejecutar_pruebas.sh
```
Última corrida: 69 aserciones SQL + concurrencia (T24 y carrera de 10 sesiones), 91 pruebas de dominio, 85 de integración, instalador de punta a punta (migrar dos veces + crear empresa + cargar catálogo por ingrediente, las 946 recetas enlazadas y el inventario inicial dos veces + central, sincronización repetida sin duplicar, respaldo, restauración y actualización conciliadas, exportación de resultados) y compilación WinForms sin advertencias. Entorno: Ubuntu 24.04, PostgreSQL 16.14, SDK .NET 8.0.425 oficial de Microsoft. El mismo script corre en GitHub Actions.

Se comprobó que las pruebas detectan fallos: mutación del redondeo de empaques (6 pruebas fallan), quitar el bloqueo de saldo (concurrencia falla), desactivar el RLS (T02 falla) quitar el control de duplicados y de orden en la central (T42, T43 y T44 fallan) y quitar la protección de contratos (importe editable o mes cerrado alterado: fallan las pruebas de etapa 9).

## Límites de lo comprobado

- **La interfaz WinForms no se ha ejecutado**: solo compila. Hay que probarla en una PC con Windows 10+.
- Valoración: decidida (D01) y aplicada en salidas; las pruebas antiguas de stock usan costos fijos a propósito.
- No se probó: carga con 20 usuarios; la sincronización y la restauración se probaron entre bases del mismo servidor, no entre sedes reales.
- La clave del usuario de sede se guarda cifrada con DPAPI en cada PC; cualquier administrador local de esa PC puede descifrarla (aceptable para una red de sede, revisar si el riesgo cambia).
- El rol de aplicación puede modificar usuarios de su empresa (necesario para administración); el control es por permiso en la aplicación.

## Brechas de la auditoría de la guía

H01, H02 y H03: **cerradas** (V003) y probadas también con el rol de la aplicación.

## Pendiente

**Decisiones de negocio** (guía doc. 09): D02 precio de ingrediente genérico · D03 impuestos/cargos · D04 redondeo de compra · D05 formato de bajas · D08 reserva · D09 sustituciones · D10 offline (aplicada la propuesta: servidor por sede; confirmar hardware y red) · D11 excesos de recepción · D14 costo por receta.

**Enlace ingrediente → productos SGP**: cargado desde `PRODUCTO_INGREDIENTE.csv` (3 133 ingredientes con los 4 158 productos SGP como variantes; 303 de 412 ingredientes de receta enlazados). Pendientes para revisar en `datos/enlace/`: 102 ingredientes de receta sin enlace, 72 productos con unidad distinta a su ingrediente, 221 productos SGP sin ingrediente.

**Preguntas al usuario:** ¿las ≈20 PC son de una sola sede? · ¿entran CD/ADS/tránsitos y raciones por cliente en la primera etapa? · revisar los 47 productos de `datos/sgp/observaciones_sgp.csv`.

**Brechas de esquema aún abiertas:** estado "en tránsito" para traspasos entre bodegas; atributos de receta por régimen; raciones diarias por cliente; fórmula exacta de `necesidad_neta` con `reserva` y `stock_utilizable`.

## Siguiente tarea exacta

1. Probar la aplicación WinForms en una PC Windows 10+ con un PostgreSQL local (pasos en `README.md`) y registrar observaciones.
2. Piloto de etapa 8 en una sede: servidor PostgreSQL de sede, central, sincronización programada, respaldo diario y una restauración de prueba; registrar incidencias un mes (plan en `docs/PILOTO_ETAPA_8.md`).
3. Confirmar D10 (¿las ≈20 PC son una sola sede?) y cargar los factores teóricos de cada estructura (desayuno, almuerzo…); luego cada operación los ajusta con su factor real.
4. Integraciones (SAP/ADS/SGO u otras): solo con especificación, entorno de prueba y conciliación entregados por el cliente.
5. Decidir D02 (precio de ingrediente genérico): hoy se usa la regla provisional "menor costo vigente".

## Continuidad

Rama de trabajo: `claude/busy-mayer-9fxop6` (PR #1 hacia `main`). `develop` se actualiza con cada entrega verificada.
No hay datos reales; nada en producción.
