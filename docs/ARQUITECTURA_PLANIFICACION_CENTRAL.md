# Planificación y Abastecimiento Central: análisis y diseño (primera entrega)

Fecha: 2026-10-03. Estado: **propuesta para aprobar antes de programar**. Este documento no cambia código.

Lo pedido es la cadena **TEÓRICO CENTRAL → LIBERACIÓN → PLAN OPERATIVO → EJECUCIÓN REAL → COMPARATIVOS → APRENDIZAJE → NUEVA PLANIFICACIÓN**. Debe quedar versionada y auditable, con seguridad por módulo, pantalla, acción y alcance.

---

## 1. Arquitectura actual (lo que ya existe)

### 1.1 Capas

| Capa | Contenido | Tamaño |
|---|---|---|
| `AppSistema.Dominio` | Cálculos puros: `EscalaU6` (enteros ×1e6), costeo, previsión, Food Cost y venta por estructura (`VentaEstructura`), producción, valoración, seguridad (permisos, módulos, niveles, roles base) | 16 archivos, ~1 700 líneas |
| `AppSistema.Datos` | Servicios con sesión (`ServicioConSesion.EnTransaccion(permiso, …)`), migrador, reportes y continuidad | 30 archivos, ~7 000 líneas |
| `AppSistema.Escritorio` | WinForms: tema común (`Tema.vb`), ayudas `Ui`, `DialogoCampos`, `SalidaReporte`; 23 pantallas | 28 archivos, ~3 900 líneas |
| `AppSistema.Instalador` | Consola: migrar, crear empresa o dueño, cargas, sincronización, respaldo | 1 archivo |
| Pruebas | 69 aserciones SQL, 91 de dominio, 94 de integración, instalador de punta a punta (casos T01–T48) | 38 archivos |
| Base | PostgreSQL 16, **V001–V020**, 71 tablas, RLS por empresa, auditoría por trigger, índices en FK | |

### 1.2 Seguridad actual

* **Permiso:** son 27 códigos (por ejemplo `MINUTAS_APROBAR`), cada uno con su módulo y nivel en `Seguridad.Modulos`.
* **Asignación:** `rol` → `rol_permiso` → `usuario_operacion_rol`. El alcance es **siempre una operación**.
* **Validación:**
  * en los servicios, porque cada operación exige su permiso dentro de la transacción;
  * en la pantalla, que muestra el botón solo si el usuario tiene el permiso (`BotonSi`);
  * en la base, mediante RLS por empresa y algunos triggers (`fn_proteger_dueno`, cierres, minutas aprobadas).
* **Dueño del sistema (V020):** `usuario.es_dueno` le da todos los permisos en todas las operaciones de **su empresa**.
* **Sin escalada:** nadie puede dar permisos que él mismo no tiene.

### 1.3 Ciclo de menú actual

```
receta_version (aprobada, inmutable)
estructura_servicio (factor teórico)  ──►  minuta (por operación y fecha: comensales, estado borrador → aprobada → cerrada)
factor_consumo_operacion (factor de la operación)      └─ minuta_detalle (receta, raciones, factor y reparto usados, costo de la ración guardado al aprobar)
                                                        └─ minuta_estructura_fija (fijos)
minuta aprobada ──► requerimiento (calculado / adicional) ──► documento_stock (entrega) ──► produccion, merma_produccion
                ──► consumo_plato (preparadas y consumidas por componente)  ──►  venta_servicio (venta real)
ServicioComparativo: minuta (planificado) vs real (raciones, venta, costo, productos, no planificados) y factor real a 30 días
ServicioConsolidadoCompras: demanda de minutas de varias operaciones − stock − pendiente + reserva (nuevo, V020)
```

### 1.4 Continuidad

* Hay un servidor por sede.
* Los envíos van en **un solo sentido, de la sede a la central**: la cola `sincronizacion_evento` envía documentos de stock y cierres.
* **No existe un canal de la central a la sede.**

---

## 2. Componentes que se reutilizan (no se duplican)

| Componente actual | Papel en la nueva arquitectura |
|---|---|
| `receta`, `receta_version`, `ServicioRecetas`, `CosteoBD` | Recetas, gramajes y costeo de la Planificación Central, sin cambios. El costeo con el producto activo (D02) sirve para el teórico y el operativo |
| `estructura_servicio`, `servicio`, `regimen`, `operacion_servicio` | Estructura del servicio y factor teórico central |
| `VentaEstructura` (Dominio) | Raciones = comensales × factor × reparto; venta = costo ÷ Food Cost objetivo; aplica a los tres niveles |
| **`minuta` + `minuta_detalle`** | **Pasan a ser el PLAN OPERATIVO.** Ya pertenecen a la operación y guardan comensales, factor, reparto y costo. Requerimiento, producción, consumo y venta ya cuelgan de ellas. Se les agrega la referencia a la versión teórica de origen |
| `requerimiento` (calculado y adicional), `ServicioProduccion` | El requerimiento ya sale de la minuta (plan operativo). La regla "si ya se entregó, va como adicional" ya existe y se completa (T64–T66) |
| `produccion`, `consumo_plato`, `merma_produccion`, `venta_servicio`, `documento_stock` | Capa REAL: ya separada del plan |
| `factor_consumo_operacion`, `ServicioComparativo.FactoresReales` | Base del histórico y de las sugerencias de factores |
| `ServicioComparativo` | Se divide en tres comparativos con un núcleo común de diferencias, absolutas y porcentuales, en el Dominio |
| `ServicioConsolidadoCompras` (V020) | Base de Compras Globales: se amplía con la columna por operación, el tránsito, el proveedor y el precio sugeridos, y la distribución |
| `pedido_compra`, `recepcion`, `proveedor`, `precio_compra` (vigencias) | Pedidos de la distribución por operación y centro de precios |
| `usuario.es_dueno` (V020), `Modulos`/niveles, `ExigirSinEscalada` | Base del SUPERUSUARIO y de la matriz de permisos |
| `fn_auditar` en todas las tablas, `auditoria`, `FormAuditoria` | Auditoría de las tablas nuevas (T63) |
| `Tema.vb`, `Ui`, `DialogoCampos`, `SalidaReporte`, `Reporte` | Base de los controles reutilizables de la fase 10 |
| Cola `sincronizacion_evento` y `fn_recibir_evento` | Modelo para el canal nuevo de la central a la sede (liberaciones) |

---

## 3. Conflictos y brechas detectados

| # | Tema | Situación actual | Propuesta |
|---|---|---|---|
| C1 | **Numeración de migraciones** | El pedido dice "V018 en adelante", pero **V018, V019 y V020 ya existen** (producto activo y precio sin IGV, familias, dueño) | Las nuevas empiezan en **V021**. No se toca ninguna aplicada |
| C2 | **Superusuario en todas las empresas** | RLS aísla por empresa y el dueño (V020) vale en **una** empresa | Usar la **opción A**: superusuario por empresa (lo actual, con el nombre visible SUPERUSUARIO) y un comando del instalador que lo da en cada empresa. La opción B sería un acceso de plataforma que sale del aislamiento por empresa: más riesgo, no se recomienda |
| C3 | **Liberación a sedes remotas** | No hay canal de la central a la sede | **Fase 3a:** la liberación funciona dentro del mismo servidor (las operaciones ya conviven en una base; D10: una sede por ahora). **Fase 3b:** canal de bajada con la misma garantía de orden e idempotencia que el de subida |
| C4 | **Factores sin vigencia** | `estructura_servicio.factor_consumo_bp` es un único valor y `factor_consumo_operacion` no tiene fecha | Tabla nueva de factores con vigencia `desde/hasta`, para aplicar una sugerencia desde una fecha, a una operación o a todas, sin perder historia |
| C5 | **Estados de la minuta** | Solo `borrador`, `aprobada` y `cerrada` | Se agregan los estados de ejecución sin romper los actuales (ver 5.3) |
| C6 | **Almacén central y tránsito** | No existe almacén central y el traspaso es inmediato, sin estado "en tránsito" | Agregar `almacen.tipo` (`operacion` o `central`) y un traspaso en dos pasos: despacho, tránsito y recepción |
| C7 | **Permisos de 27 códigos** | Son por módulo y acción, sin pantalla ni alcance | Catálogo **pantalla × acción** y alcance por asignación. Cada código actual se vuelve una pantalla y acción equivalente, así que `EnTransaccion(permiso)` sigue funcionando |
| C8 | **Roles actuales** | SUPERVISOR casi lo tiene todo; COCINA no puede cambiar factores; PLANIFICACION y ABASTECIMIENTO (V020) | SUPERVISOR se mantiene igual. Se agregan PLANIFICADOR_CENTRAL, COMPRAS_CENTRAL, OPERACIONES y CHEF. COCINA se mantiene. Los de V020 se vuelven equivalentes de los centrales |
| C9 | **Comparativo actual** | Compara la minuta contra lo real. Con la minuta como plan operativo, eso es **Operativo vs Real** | Se renombra y se agregan Teórico vs Operativo y Teórico vs Real |

---

## 4. Nuevos módulos

| Módulo | Área | Responsable |
|---|---|---|
| **Planificación Central** | Recetas, estructuras, ciclos, planificación teórica, revisión, versiones y liberación | PLANIFICADOR_CENTRAL |
| **Aprendizaje de factores** | Histórico (teórico, operativo y real), estadísticas a 7, 30 y 90 días, desviación, tendencia y sugerencias pendientes de revisión | PLANIFICADOR_CENTRAL |
| **Operación** | Recepción de liberaciones, plan operativo, programación del día del chef, factores del día, cierre del servicio | OPERACIONES, CHEF |
| **Comparativos** | Teórico vs Operativo, Operativo vs Real, Teórico vs Real | Todos, según su alcance |
| **Compras Globales** | Necesidades consolidadas, comparación de proveedores, centro de precios, compra global, distribución por operación, pedidos | COMPRAS_CENTRAL |
| **Seguridad avanzada** | Superusuario, matriz de acceso pantalla × acción × alcance | SUPERUSUARIO |

---

## 5. Nuevas tablas (propuesta; nombres en la convención actual: singular, `snake_case`, `empresa_id` y FK compuestas)

Todas las tablas llevan:
* `empresa_id` y FK `(empresa_id, x_id)`;
* RLS `aislamiento_empresa`;
* el trigger `auditar`;
* índices en sus FK (lo controla la prueba de V017);
* CHECK y UNIQUE;
* cantidades y dinero como `_u6` y porcentajes como `_bp`.

### 5.1 Teórico (Planificación Central): inmutable desde que se aprueba

| Tabla | Columnas principales |
|---|---|
| `planificacion` | código `PLAN-2026-11-ORC`, operación destino, periodo desde/hasta, régimen, creador. Una por destino y periodo |
| `planificacion_version` | planificación, número de versión (V001…), estado, Food Cost objetivo, totales (costo, venta, costo por comensal), versión anterior, motivo del cambio |
| `planificacion_estado` | versión, estado anterior y nuevo, usuario, fecha, observación. Es el historial completo del flujo |
| `planificacion_servicio` | versión, fecha, servicio, estructura, comensales previstos |
| `planificacion_plato` | servicio planificado, componente, receta, versión de receta, factor teórico, reparto, raciones, costo por ración, fecha del precio |
| `planificacion_producto` | versión, fecha, producto, cantidad requerida, precio usado, fecha del precio, fuente del precio |

**Inmutabilidad:** un trigger impide modificar las filas de una versión `APROBADO` o posterior (T49). Para cambiar algo se crea una versión nueva.

### 5.2 Liberación y recepción

| Tabla | Columnas principales |
|---|---|
| `liberacion_planificacion` | versión, operación, quién liberó y cuándo, estado (`LIBERADO`, `RECIBIDO_OPERACION`, `REEMPLAZADA`, `RECHAZADA`), quién recibió y cuándo, decisión ante una nueva versión (aceptar, mantener, revisar), advertencia si ya está en ejecución |

### 5.3 Plan operativo (reutiliza `minuta`)

* Columnas nuevas en `minuta`:
  * `planificacion_version_id` (origen, NULL para las minutas locales);
  * `liberacion_id`;
  * `comensales_centrales` (copia de lo liberado);
  * `estado_ejecucion` (`PLAN_OPERATIVO`, `EN_EJECUCION`, `EJECUTADO`, `CERRADO`), que se suma al `estado` actual sin cambiarlo.
* Columnas nuevas en `minuta_detalle`:
  * `planificacion_plato_id` (origen);
  * `factor_central_bp`, `reparto_central_bp` y `receta_version_central_id` (copia de lo liberado);
  * `sustituye_a` (sustitución registrada).
* **`minuta_cambio`** (nueva): minuta o plato, campo (comensales, factor, reparto, receta, cantidad), valor anterior, valor nuevo, motivo, usuario, fecha. Guarda la historia del plan operativo para los comparativos y la auditoría funcional.

### 5.4 Real ejecutado

* **`ejecucion_servicio`** (nueva): una por minuta al finalizar el servicio. Guarda:
  * comensales reales;
  * raciones preparadas, servidas, no servidas y sobrantes;
  * costo real (entregas − devoluciones + mermas);
  * venta real y Food Cost real;
  * quién cerró y cuándo.

  Es una foto **inmutable** de lo real. No modifica el plan.
* **`ejecucion_producto`** (nueva): por producto, cantidad planificada (operativa), entregada, devuelta, consumida, adicional y no planificada, con su costo real.
* Se mantienen y alimentan esta foto: `produccion`, `consumo_plato`, `venta_servicio`, `merma_produccion` y `documento_stock`.

### 5.5 Factores

| Tabla | Columnas principales |
|---|---|
| `factor_vigencia` | estructura, operación (NULL = central), receta (opcional), factor, desde y hasta, origen (`central`, `operacion`, `sugerencia`), usuario. Sin superposiciones (EXCLUDE) |
| `factor_historico` | operación, servicio, régimen, componente, receta, fecha, factor teórico, operativo y real, comensales, cantidad usada. Se llena al cerrar el servicio |
| `factor_sugerencia` | componente, operación o central, factor central vigente, promedios a 7, 30 y 90 días, desviación, tendencia, último valor, sugerencia, estado (`PENDIENTE`, `ACEPTADA`, `RECHAZADA`, `MODIFICADA`), factor aplicado, alcance, desde cuándo, quién decidió. **Aceptarla crea una `factor_vigencia` y nunca cambia el factor en uso de forma automática (T60)** |

### 5.6 Compras Globales

| Tabla | Columnas principales |
|---|---|
| `compra_global` | número, periodo, estado (`borrador`, `aprobada`, `distribuida`, `anulada`), filtros usados, quién y cuándo |
| `compra_global_linea` | producto, demanda total, stock central, stock en operaciones, tránsito, reserva, necesidad neta, compra sugerida, proveedor y empaque sugeridos, precio vigente, último precio, variación |
| `distribucion_global` | línea, operación, cantidad, pedido generado (`pedido_compra`), fecha prevista, fecha recibida |
| `acuerdo_precio` | proveedor, vigencia, documento o referencia, observación. Agrupa los `precio_compra` negociados (centro de precios) |
| `almacen.tipo` (columna) y `traspaso_transito` | almacén central y mercadería en tránsito |

### 5.7 Seguridad de cuatro niveles

| Tabla | Columnas principales |
|---|---|
| `pantalla` | código, módulo, nombre, orden (catálogo del árbol de la matriz) |
| `permiso` (existente) + columnas `pantalla_id` y `accion` | acción: VER, CREAR, EDITAR, APROBAR, ANULAR, LIBERAR, EXPORTAR, IMPRIMIR, CONFIGURAR. Los 27 códigos actuales se asignan a su pantalla y acción |
| `usuario_operacion_rol` + columna `alcance` | `OPERACION_ASIGNADA` (lo actual) o `TODAS_OPERACIONES` |
| `usuario_permiso` (nueva) | permiso concedido o negado a una persona fuera de su rol, con alcance. El superusuario lo ajusta en la matriz |
| `fn_tiene_permiso(codigo, operacion)` | función en la base para los triggers de las acciones críticas: liberar, cambiar factor y mover stock (T53, T54, T62) |

---

## 6. Nuevas pantallas y menú

```
INICIO
PLANIFICACIÓN CENTRAL   Recetas* · Estructuras* · Ciclos · Planificación teórica · Revisión · Liberaciones · Versiones y cambios ·
                        Histórico de factores · Sugerencias de factores · Comparativos
COMPRAS GLOBAL          Necesidades consolidadas (amplía la actual) · Proveedores* · Centro de precios · Compras globales · Distribución por operación
OPERACIÓN               Recepción de liberaciones · Plan operativo (Minutas*) · Programación del día · Factores operativos · Producción* ·
                        Teórico vs Operativo · Operativo vs Real (actual) · Teórico vs Real
ALMACÉN                 Requerimientos · Stock* · Kárdex* · Inventario*
CIERRES Y CONTROL*
ADMINISTRACIÓN          Usuarios* · Roles · Matriz de acceso · Operaciones* · Auditoría* · Sincronización*
(* = pantalla existente que se reubica o amplía)
```

**Programación del día (chef):**
* **Muestra:** servicio, comensales centrales y operativos, y los componentes con su factor central, el operativo, el reparto, las cantidades y el costo.
* **Botones:** Actualizar comensales, Actualizar factores, Generar requerimiento, Solicitar adicional, Registrar producción, Finalizar servicio.
* **Recálculo:** cada cambio recalcula al instante las cantidades y el costo operativo.
* **Corte del requerimiento:**
  * antes de la entrega, el requerimiento se recalcula;
  * después de la entrega, el cambio genera un requerimiento **adicional** y la entrega anterior no se toca.

**Matriz de acceso (superusuario):**
* Se elige usuario, rol y operación.
* Muestra el árbol módulo → pantalla con las acciones VER, CREAR, EDITAR, APROBAR, ANULAR, LIBERAR, EXPORTAR, IMPRIMIR y CONFIGURAR, y el alcance.

**Controles reutilizables (fase 10):** ControlCabecera, ControlBarraAcciones, ControlFiltros, ControlEstado, ControlTotales, ControlBusqueda, ControlGrid, ControlDocumento, ControlVersion, ControlComparativo y ControlProceso, sobre el `Tema.vb` actual.

---

## 7. Roles base propuestos

| Rol | Resumen | Excluido |
|---|---|---|
| SUPERUSUARIO | `es_dueno`: todo, en todas las operaciones de la empresa; define la matriz | — |
| PLANIFICADOR_CENTRAL | Recetas, costeo, menús, ciclos, factores teóricos, históricos, sugerencias, planificación teórica, enviar a revisión; aprobar y liberar solo con permiso aparte; comparativos | Almacén físico, movimientos de stock (T54) |
| COMPRAS_CENTRAL | Demanda consolidada, proveedores, precios (editar según permiso), pedidos, distribución, stock consolidado, tránsito | Planificación central, salvo precios y disponibilidad |
| OPERACIONES | Recibir liberaciones, plan operativo, comensales, factores autorizados, reparto, sustituciones, necesidades, Food Cost, comparativos | Planificación central |
| CHEF | Programación del día, recetas, factor operativo del día, comensales, reparto, producción, raciones, sobrantes, adicional | Stock, recetas centrales |
| ALMACEN (existente) | Requerimientos, entrega, devoluciones, mermas autorizadas, inventario, stock, kárdex | **Factores (T53)**, recetas, planificación |

Se mantienen ADMIN, SUPERVISOR, COCINA, FINANZAS, PLANIFICACION y ABASTECIMIENTO para no romper las asignaciones actuales.

---

## 8. Plan de migraciones (a partir de V021)

| Migración | Fase | Contenido |
|---|---|---|
| V021 | 1 | `pantalla`, permisos con pantalla y acción, alcance, `usuario_permiso`, `fn_tiene_permiso`, roles nuevos |
| V022 | 2 | `planificacion`, `planificacion_version`, `planificacion_estado`, `planificacion_servicio`, `planificacion_plato`, `planificacion_producto`, trigger de inmutabilidad |
| V023 | 3 | `liberacion_planificacion`; referencias de origen y estado de ejecución en `minuta` y `minuta_detalle` |
| V024 | 4–5 | `minuta_cambio`; triggers de factor y comensales por permiso y antes o después de la entrega |
| V025 | 6 | `ejecucion_servicio`, `ejecucion_producto` (foto inmutable de lo real) |
| V026 | 7 | `factor_vigencia` (con EXCLUDE), `factor_historico`, `factor_sugerencia` |
| V027 | 8–9 | `almacen.tipo`, `traspaso_transito`, `compra_global`, `compra_global_linea`, `distribucion_global`, `acuerdo_precio` |
| V028 | 3b | Canal de la central a la sede (cola de bajada y aplicación idempotente) |

---

## 9. Pruebas por fase

| Fase | Pruebas |
|---|---|
| 1 Seguridad | T61 superusuario en todos los módulos · T62 sin permiso no accede al llamar el servicio · T53 almacén no cambia factores · T54 planificador no mueve stock · T63 auditoría |
| 2 Planificación central | T49 versión liberada inmutable · T56 nueva versión no reemplaza un plan ejecutado |
| 3 Liberación | T50 el plan operativo guarda la versión teórica exacta |
| 4–5 Plan operativo y chef | T51 el factor operativo no cambia el central · T52 el chef con permiso cambia el factor · T64 el requerimiento usa el plan operativo vigente · T65 un cambio antes del requerimiento recalcula · T66 un cambio después de la entrega crea un adicional |
| 6 Comparativos | T57 Teórico vs Operativo · T58 Operativo vs Real · T59 Teórico vs Real (diferencias absolutas y %) |
| 7 Factores | T60 la sugerencia no cambia el factor aprobado |
| 8–9 Compras globales | T55 consolidado de varias operaciones (amplía `ConsolidadoComprasTests`) + distribución |
| Final | Caso completo del punto 28, de punta a punta, en el instalador y en las pruebas de integración |

---

## 10. Forma de entrega

* Una fase por entrega.
* Cada entrega sigue los pasos: análisis puntual, implementación, compilación, pruebas, documentación y commit por unidad funcional.
* Al terminar la fase se sube y se avanza `develop` con el CI en verde.
* No se elimina nada probado.
* Teórico, operativo y real nunca comparten registro.
* Ningún cambio central altera en silencio lo que ya está en ejecución.

## 11. Decisiones del usuario (2026-10-03) y fase 1

**Respuestas:**
1. **C2:** "el superusuario soy yo": es el dueño del sistema, por empresa (opción A).
2. y 3. **Liberación:** la hace el área de Planificación o el superusuario directamente. No se exige un revisor distinto. Empieza en el mismo servidor (fase 3a).
4. **Corte del requerimiento:** el usuario preguntó si es el interno o el pedido global. Es el **interno** (cocina → almacén de la sede, y sede → almacén central); la compra global a proveedores no tiene corte diario. Falta la hora.
5. **Almacén central:** existe.
6. **Regiones:** las sedes se agrupan por regiones o zonas. Se usa `operacion.zona`.

**Fase 1 hecha (V021):**
* La tabla `pantalla` propuesta se reemplazó por el catálogo `Seguridad.Pantallas` del dominio: cada permiso tiene su pantalla y su acción. Los permisos también se crean desde el código, así hay una sola fuente.
* Alcance OPERACION, ZONA o TODAS en `usuario_operacion_rol`; solo el superusuario lo amplía.
* `usuario_permiso`: excepciones concedidas o negadas por el superusuario.
* `fn_permisos_usuario` da los permisos efectivos; la sesión, el consolidado de compras, el control de escalada y los triggers usan ese mismo cálculo.
* Triggers: mover stock exige STOCK_CONTABILIZAR, el factor de la operación exige FACTORES_EDITAR y el factor teórico exige MENUS_CONFIGURAR.
* Roles PLANIFICADOR_CENTRAL, COMPRAS_CENTRAL, OPERACIONES y CHEF.
* Pantalla Administración > Matriz de acceso; zona en Operaciones; alcance en Usuarios.
* Pruebas T53, T54, T61, T62 y T63, más alcance y matriz (`SeguridadCentralTests`).

**Siguiente:** fase 2, planificación central con versiones (V022).

### Preguntas originales



1. **C2:** ¿superusuario por empresa (recomendado) o un acceso global a todas las empresas?
2. **C3:** ¿empezamos con la liberación en el mismo servidor y el canal remoto después (recomendado)?
3. **¿Quién aprueba la planificación?** ¿Un revisor distinto del planificador, como en inventarios, donde quien cuenta no autoriza?
4. **Corte del requerimiento:** ¿a qué hora del día anterior, o al aprobar el plan operativo?
5. **Almacén central:** ¿hay hoy un almacén central físico que abastece a las operaciones, o cada operación compra para sí?
6. **Regiones:** ¿qué región tiene cada operación (para el filtro de compras globales)?
