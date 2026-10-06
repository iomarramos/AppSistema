# Seguimiento del proyecto

Actualizado: 2026-10-03. Registra **solo lo ejecutado y comprobado**; una tabla o pantalla no es "terminado".
Plan: `docs/06_GUIA_DE_CONSTRUCCION/04_PLAN_POR_ETAPAS.md`. Decisiones de negocio: `docs/06_GUIA_DE_CONSTRUCCION/09_SEGUIMIENTO_Y_DECISIONES.md`.

## 2026-10-04 — Mejora compartida de diseño y usabilidad

Se mejoraron `Tema`, `Ui`, `DialogoCampos` y la navegación de `FormPrincipal`, siguiendo las pautas de referencia locales de `ui-ux-pro-max` (su buscador no se ejecutó porque Python no está disponible). Cambios: superficies consistentes, botones más cómodos, encabezados y columnas legibles, orientación en tablas vacías, foco visible en estados, ayudas que se ajustan al ancho y orden de teclado en diálogos. Ventanas incorpora selector de pantalla con Ctrl+K (solo opciones permitidas), lista de abiertas, cascada, mosaico y cierre de la activa. Se mantienen los identificadores existentes y la lógica de servicios, permisos, cálculos y base de datos.

Validación local en Windows: compilación Release sin errores ni advertencias; 98 pruebas de dominio y 6 pruebas nuevas de controles aprobadas. Se revisaron capturas de acceso, diálogo, contexto y tabla (`artifacts/screenshots/ui-*.png`, datos de prueba). Las pruebas cubren solapamiento/idempotencia del encabezado, recuperación de botones tras error y repetición, pintura de estados en columnas estrechas, orden de campos y clave literal, ajuste de ayuda, formato monetario y costo pendiente.

Pendiente: recorrido completo E2E con autenticación, pruebas de integración PostgreSQL y comprobación a DPI 125/150 %. PostgreSQL local responde, pero pide credenciales que no están configuradas para estas pruebas. Los archivos locales previos sin seguimiento se conservaron.

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
| 0 Diagnóstico y línea base | **Hecha** | Esquema portado a PostgreSQL; migraciones V001–V021 con migrador versionado; brechas H01–H03 cerradas |
| 1 Fundamentos y catálogo | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Acceso con bloqueo, permisos por operación, auditoría automática, aislamiento RLS, operaciones/almacenes/usuarios, unidades, categorías, marcas, productos, variantes, empaques, proveedores, precios con vigencia, importador CSV con vista previa, **carga del listado de productos del SGP** (4 158 productos con factor y unidad mínima de pedido; `datos/sgp/`). Pantallas WinForms compiladas **pero no ejecutadas** (no hay Windows en este entorno) |
| 2 Menús y recetas | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Servicios, regímenes y estructuras; recetas versionadas (borrador → aprobada inmutable → retirada) con rendimiento, ingredientes por producto base y variantes permitidas; minutas por día y servicio con platos y fijos; aprobación con snapshot de costo (fuente y fecha por ingrediente); costo simulado; necesidades consolidadas por producto. **Recetas del SGP cargadas**: 946 recetas (417 fichas revisadas + 529 del Recetón) con 412 ingredientes (`datos/recetas/`). Pantallas: Recetas, Minutas y necesidades, Servicios y estructuras, Importar recetas. **Regla de precio provisional** (D02 pendiente): menor costo vigente entre las variantes permitidas; sin precio → "pendiente". El precio se usa tal como se registró (D03 impuestos pendiente) |
| 3 Previsión y compras | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Previsión por almacén desde minutas aprobadas: demanda del horizonte, consumo puente (una sola vez), stock actual, reserva por producto y almacén (D08 como parámetro, 0 por defecto), pendientes de pedidos aprobados menos lo recibido, **recorrido por fechas** con fecha de quiebre (un tránsito tardío no oculta la falta). Validación y obsolescencia (recalcula y compara; los pedidos de la propia previsión no la vuelven obsoleta). Pedido generado por proveedor con el empaque de menor costo vigente y redondeo por mínimo/múltiplo (D04 propuesto); pedidos manuales; aprobación (exige previsión vigente) y anulación; inmutables tras aprobar. Pantalla Compras > Previsión y pedidos |
| 4 Almacén y kárdex | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Inventario inicial valorizado (apertura). Recepción de pedidos total o parcial (conversión histórica del pedido, costo = empaques × precio del comprobante, sin exceder lo pendiente, comprobante único, pedido pasa a parcial/recibido). Salidas a cocina, bajas con motivo, devoluciones al costo de la entrega sin exceder lo entregado, traspasos entre almacenes de la operación. **Valoración D01 (usuario: "cantidad × precio")**: entradas a su costo, salidas a cantidad × costo vigente del saldo (promedio móvil), la última salida se lleva el valor restante. Kárdex valorizado con saldo corrido. Pantalla Almacén > Stock |
| 5 Producción | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Requerimiento calculado desde la minuta aprobada (uno por minuta) y adicionales; entrega del almacén en **presentaciones completas que se dan por consumidas** (D12, usuario), eligiendo la presentación con menor excedente e informando faltantes; devoluciones al costo de la entrega; raciones producidas/servidas/excedentes coherentes; mermas analíticas (incluidas en lo entregado, sin segunda baja) o con baja de almacén; previsto vs real por producto y **costo real del servicio** (D14: sin reparto por receta). Pantalla Menús > Producción |
| 6 Inventarios | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Inventario general o rotativo con fotografía de stock y costo al corte (inmutable); hoja de conteo por envases + parcial, ciega opcional; importación del conteo; celda vacía = pendiente; reconteo; diferencias físico − sistema sin tocar el saldo; **mientras se cuenta, esas variantes no admiten movimientos**; revisión y autorización por quien no contó; ajuste en documentos separados (positivo al costo del corte, negativo al costo vigente), una sola vez. Pantalla Almacén > Inventario físico. D06/D07 aplicados según la propuesta de la guía (usuario: "procede") |
| 7 Cierres y control | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Tablero de pendientes por día (minuta sin aprobar, producción sin registrar, requerimiento pendiente, documentos y recepciones en borrador, inventario abierto, conciliación saldo/libro: bloquean; pedido vencido: aviso). Cierre diario serializado con las contabilizaciones (bloquea los almacenes: una salida simultánea queda antes o es rechazada) y validaciones guardadas. Ingreso mensual neto por servicio y objetivo de Food Cost; reporte mensual por servicio (raciones, costo de alimentos consumidos, costo por ración, ingreso, Food Cost, desviación y diferencia contra presupuesto) más bajas y ajustes de inventario. Cierre de mes: exige todos los días con actividad cerrados; después movimientos, ingresos y gastos del mes no cambian (BD) y el reporte se repite igual. **D13 (usuario)**: la venta sale de la estructura del menú — factor de consumo por componente y reparto de alternativas; venta = costo previsto / Food Cost objetivo (48 % por defecto, margen 52 %); Food Cost real = consumo real / venta. *Generar venta (estructura)* la carga como ingreso del mes; un ingreso manual o de contrato tiene prioridad. Pantalla Cierres > Pendientes, cierres y Food Cost |
| 8 Continuidad y piloto | **Parcial: hecho en código y pruebas; falta el piloto real** | **D10 aplicado según la propuesta de la guía (autoridad local por sede)**: cada sede tiene su servidor PostgreSQL y opera sin internet. Cola de salida escrita en la misma transacción que el documento o cierre (un corte no deja media operación), UUID global y secuencia por sede sin huecos, eventos inmutables, versión de contenido. Agente `sincronizar` con reintentos de espera creciente que no adelanta eventos. Central: credencial por sede (solo hash), rechazo registrado sin contenido, reenvíos sin duplicar, misma clave con otro contenido = conflicto, orden por sede y dependencias (lo que llega antes de su antecesor se retiene), datos por sede sin sobrescritura, reporte con última sincronización y stock por sede. Respaldo consistente (pg_dump con la misma fotografía que la conciliación) y restauración en base vacía conciliada. Migración de una base con historia (V011 → V012) conservando saldos, y alta de la historia en la cola. Comando `actualizar` (respaldo + migraciones + conciliación de saldos e historia). Estado de envío visible en la aplicación (barra de estado y Cierres: "por enviar / último envío"). Script `herramientas/windows/programar_sede.ps1` para programar envío y respaldo diario sin guardar claves. **Falta:** piloto de un mes en sede real (plan en `docs/05_OPERACION/PILOTO_ETAPA_8.md`), ensayo de corte de energía físico y prueba del script en Windows |
| Planificación y Abastecimiento Central, fase 1 (seguridad) | **Hecha en código y pruebas; falta validar la interfaz en Windows** | V021. Superusuario = dueño del sistema (respuesta del usuario). Cuatro niveles: módulo → pantalla → acción (`Seguridad.Pantallas`) → alcance de la asignación (OPERACION, ZONA por `operacion.zona`, TODAS); solo el superusuario amplía alcances. Matriz de acceso con excepciones por persona (concedido o negado, aquí o en todas; lo negado gana). Permisos efectivos calculados en la base (`fn_permisos_usuario`) y usados por la sesión, la escalada, el consolidado y los triggers: mover stock y cambiar factores se validan también en la base. Permiso FACTORES_EDITAR (lo reciben los roles que tenían MINUTAS_APROBAR). Roles PLANIFICADOR_CENTRAL, COMPRAS_CENTRAL, OPERACIONES y CHEF. Pantallas: Administración > Matriz de acceso; zona en Operaciones; alcance en Usuarios. Diseño completo y fases 2–10 en `docs/04_ARQUITECTURA_Y_DATOS/ARQUITECTURA_PLANIFICACION_CENTRAL.md` |
| Siete perfiles de la operación (V022 a V024) | **Hecha en código; pruebas de base pendientes de correr** | Se revisó la propuesta de perfiles contra lo existente: la seguridad por módulo, pantalla, acción y alcance ya estaba (V021). Faltaba el jefe de almacén (aprueba inventarios y ajustes; el almacenero solo ejecuta). Se agregó JEFE_ALMACEN y se alinearon los nombres visibles de ALMACEN, OPERACIONES y CHEF; los códigos no cambian. El jefe de operación gana MINUTAS_APROBAR, CIERRE_EJECUTAR y RESULTADOS_VER. `RolesBase` y V022 (empresas ya instaladas). V023: el almacén no prepara pedidos; ADICIONAL_APROBAR (el adicional pasa por el jefe antes de entregarse; la base valida el permiso y guarda quién aprobó); INVENTARIO_VER. V024: solicitud de devolución desde cocina (`devolucion_solicitud`), atendida por el almacén con su documento de devolución. Pruebas nuevas: `Perfiles_de_la_operacion_tienen_solo_lo_que_les_corresponde`, `Adicional_lo_aprueba_el_jefe_y_la_devolucion_de_cocina_la_atiende_el_almacen`, `Almacen_no_prepara_ni_aprueba_pedidos…` |
| 9 Extensiones | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Clientes y contratos mensuales de la operación con vigencia; líneas por servicio con importe mensual que no se superponen; **ajuste** desde una fecha (cierra la línea anterior y abre otra; el importe nunca se edita); nada cambia un mes cerrado. **Ingreso desde contrato** prorrateado por días (alternativa a la venta por estructura), sin pisar un ingreso manual, repetible. Gastos de personal, operación, administración y otros, reales o presupuestados, por servicio o comunes. **Resultado mensual** por servicio: ingreso − alimentos − gastos = margen y margen %, con "No asignado" para gastos comunes, bajas y ajustes; cada cifra rastreable (fuente del ingreso, documentos, registro del gasto). **Roles propios** (accesos avanzados): el administrador crea roles con los permisos que elija, quita roles, nunca deja la empresa sin administrador; rol base nuevo FINANZAS (se agrega a empresas existentes al actualizar). Exportación CSV con contrato de datos v1 (`docs/04_ARQUITECTURA_Y_DATOS/INTEGRACION_RESULTADOS.md`). Pantallas: Cierres > Contratos y clientes, Gastos y resultado mensual; Administración > Usuarios (roles). **No incluido** (la guía exige especificación y entorno de prueba): integraciones con SAP/ADS/SGO; resultado consolidado multi-sede en la central |

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
| Datos reales: precios por presentación, agua sin costo, estructuras y ciclo repetibles | `CargaRealTests` e instalador de punta a punta (una semana del ciclo aprobada, todas con venta) | Pasa |
| Reportes imprimibles y exportables: minuta del día (S/ 40 de costo, S/ 83,33 de venta, necesidad 5 L), requerimiento, kárdex (10 − 5 = 5 L a S/ 8), hoja de conteo sin stock del sistema, resultado con faltante S/ 8 y stock valorizado S/ 104; CSV y HTML escapados; otra empresa y sin permiso rechazados | `ReportesTests` | Pasa |
| T53 almacén no cambia factores · T54 planificador no mueve stock · T61 superusuario en todos los módulos y operaciones · T62 sin permiso no entra aunque llame al servicio · T63 cambios de acceso auditados; alcance por zona y todas; matriz con excepciones (el servicio y el SQL directo con la sesión de la aplicación) | `SeguridadCentralTests` | Pasa |
| T47 importación repetida y filas inválidas | `ImportacionTests`, `ImportacionSgpTests` (listado real del SGP) | Pasa |

**Todos los casos T01–T48 de la guía tienen prueba**; lo que falta es el piloto en una sede real.

## Evidencia

```bash
./ejecutar_pruebas.sh
```
Última corrida: 69 aserciones SQL + concurrencia (T24 y carrera de 10 sesiones), 91 pruebas de dominio, 92 de integración, instalador de punta a punta (migrar dos veces + crear empresa + cargar catálogo por ingrediente, las 946 recetas enlazadas y el inventario inicial dos veces + central, sincronización repetida sin duplicar, respaldo, restauración y actualización conciliadas, exportación de resultados) y compilación WinForms sin advertencias. Entorno: Ubuntu 24.04, PostgreSQL 16.14, SDK .NET 8.0.425 oficial de Microsoft. El mismo script corre en GitHub Actions.

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

**Decisiones tomadas por el usuario (2026-10-03):**
* **D02:** precio del producto activo en la operación (liberado, o el último ingresado al almacén; si no hay, pendiente). También se aplica en los pedidos.
* **D03:** precios sin IGV; la restricción `precio_sin_igv` está en V018.
* **D10:** una sede por ahora, ampliable.

**Decisiones de negocio pendientes** (guía doc. 09): D04 redondeo de compra · D05 formato de bajas · D08 reserva · D09 sustituciones · D10 hardware y red de la sede · D11 excesos de recepción · D14 costo por receta.

**Datos reales ordenados** (`datos/real/`, `herramientas/ordenar_datos_reales.py`): precios por presentación (inventario + último precio SGP; 20 atípicos apartados), enlace complementario por nombre (49 ingredientes; 85 por revisar), recetas con el ingrediente del catálogo, agua para receta sin costo, recetas clasificadas por componente con gramaje y costo, estructuras de Desayuno/Almuerzo/Cena y ciclo de 28 días con todas las recetas costeadas. Cargado de punta a punta con la regla D02 (producto activo, 975 ingredientes liberados): 84 minutas aprobadas con costo y venta. Costo por comensal: desayuno S/ 3,54, almuerzo S/ 7,87 y cena S/ 6,15. Con la regla provisional anterior, que tomaba el más barato, eran S/ 3,13, S/ 6,57 y S/ 5,01.

**Enlace ingrediente → productos SGP**: cargado desde `PRODUCTO_INGREDIENTE.csv` (3 133 ingredientes con los 4 158 productos SGP como variantes; 303 de 412 ingredientes de receta enlazados). Pendientes para revisar en `datos/enlace/`: 102 ingredientes de receta sin enlace, 72 productos con unidad distinta a su ingrediente, 221 productos SGP sin ingrediente.

**Preguntas al usuario:** ¿las ≈20 PC son de una sola sede? · ¿entran CD/ADS/tránsitos y raciones por cliente en la primera etapa? · revisar los 47 productos de `datos/sgp/observaciones_sgp.csv`.

**Brechas de esquema aún abiertas:** estado "en tránsito" para traspasos entre bodegas; atributos de receta por régimen; raciones diarias por cliente; fórmula exacta de `necesidad_neta` con `reserva` y `stock_utilizable`.

## Siguiente tarea exacta

1. Probar la aplicación WinForms en una PC Windows 10+ con un PostgreSQL local (pasos en `README.md`) y registrar observaciones.
2. Piloto de etapa 8 en una sede: servidor PostgreSQL de sede, central, sincronización programada, respaldo diario y una restauración de prueba; registrar incidencias un mes (plan en `docs/05_OPERACION/PILOTO_ETAPA_8.md`).
3. Revisar `datos/real/ingredientes_por_revisar.csv` (ajo molido envasado, mayonesa, ajíes molidos, panes de marca…) y `precios_atipicos.csv`; confirmar la estructura y los factores propuestos en `estructuras_menu.csv`.
4. Integraciones (SAP/ADS/SGO u otras): solo con especificación, entorno de prueba y conciliación entregados por el cliente.
5. Revisar `datos/real/productos_activos.csv` (producto activo propuesto por ingrediente) y corregir en Catálogo los que no correspondan.

## Continuidad

Rama de trabajo: `claude/busy-mayer-9fxop6` (PR #1 hacia `main`). `develop` se actualiza con cada entrega verificada. Para retomar en otra conversación: `CLAUDE.md` y `docs/03_ESTADO/CONTINUAR.md`.
Datos reales del SGP ordenados en `datos/real/` y cargados en bases de prueba; nada en producción todavía.

## Sprint 1 de la especificación docs/02_REQUERIMIENTOS/ESPECIFICACION_VENTANA/ (2026-10-06)

* **Hecho:** librerías QuestPDF y ClosedXML instaladas; matriz de planificación (solo lectura) y plan operativo del chef, con su menú; bloqueo de comensales en minutas aprobadas en el servicio; pruebas de datos 101/101.
* **Pendiente del Sprint 1:**
  * matriz con días en columnas y resumen diario (comensales, costo del día, costo por bandeja, desviación frente al techo);
  * edición de borradores desde la matriz;
  * costo patrón/techo en pantalla;
  * raciones operativas y sustituciones en el plan del chef, y adicional desde el plan (la regla 07: el cambio después del despacho genera un adicional, no altera el despacho);
  * la tabla de composición nutricional sigue sin datos: el aporte nutricional queda bloqueado.
* **Decisiones tomadas por defecto:** la matriz convive con Minutas y necesidades; PDF y Excel con librerías instaladas, sin cambiar aún la salida de los reportes.
* **Riesgo abierto:** la zona horaria del día operativo (ver `DIAGNOSTICO_VENTANA.md`).

* **Continuación Sprint 1 (2026-10-06):** matriz con días en columnas y resumen por día (techo y desviación), edición de comensales desde la matriz, y adicional desde el plan del chef. Quedan pendientes: raciones operativas separadas, sustituciones de receta, motivo del adicional, adicional tras el despacho, y zona horaria. Ver `CHECKLIST.md`.
