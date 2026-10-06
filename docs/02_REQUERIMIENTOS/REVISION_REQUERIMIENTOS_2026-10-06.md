# Revisión de formularios y base de datos contra el documento de requerimientos

Fecha: 2026-10-06. Fuente de requisitos: `docs/02_REQUERIMIENTOS/REQUERIMIENTOS.md` (v0.1.2, RF, RNF, RN y preguntas P). Evidencia: código en `src/`, migraciones en `database/postgresql/migraciones/`, la base real `appsistema` y una base limpia construida solo con las migraciones de esta rama (`appsistema_limpia`, 75 tablas).

Estados: **Cumple** (funciona y hay evidencia), **Parcial** (existe una parte), **Falta** (no existe), **No verificado** (no se pudo comprobar aquí), **Decisión** (depende de una respuesta del usuario o de datos que no llegaron).

## 1. Resumen

| Grupo | Cumple | Parcial | Falta | No verificado | Decisión | Total |
|---|---:|---:|---:|---:|---:|---:|
| Acceso y acciones (RF-ACC) | 1 | 4 | 0 | 0 | 0 | 5 |
| Planificación teórica (RF-PT) | 3 | 7 | 3 | 0 | 1 | 14 |
| Pedido mensual y extra (RF-PM, RF-PE) | 5 | 7 | 3 | 1 | 0 | 16 |
| Planificación real (RF-PR) | 1 | 4 | 4 | 0 | 0 | 9 |
| Requerimientos a almacén (RF-RD) | 1 | 5 | 0 | 0 | 0 | 6 |
| Entradas (RF-EN) | 2 | 2 | 0 | 0 | 1 | 5 |
| Salidas (RF-SA) | 2 | 1 | 5 | 0 | 0 | 8 |
| Ventas (RF-VE) | 1 | 3 | 1 | 0 | 0 | 5 |
| Cierre (RF-CD, RN-CM) | 4 | 4 | 1 | 2 | 0 | 11 |
| Traspasos ADS (RF-TR) | 0 | 1 | 5 | 0 | 0 | 6 |
| Inventario (RF-IR, RF-IN) | 0 | 3 | 2 | 1 | 0 | 6 |
| No funcionales (RNF) | 11 | 8 | 1 | 1 | 1 | 22 |
| **Total** | **31** | **49** | **25** | **5** | **3** | **113** |

**Lectura:** el núcleo operativo (planificación, requerimiento, entrega a producción, almacén, traspaso entre almacenes en dos pasos, cierre y costos) existe y está probado. Lo que falta se concentra en **integraciones** (ADS, SAP, central), en **contratos** como ámbito de stock, en **venta directa y cafetería** (sin datos ni especificación completa), y en reglas de "planificación real" y tolerancia de cierre. Varios puntos dependen de respuestas del usuario (sección 4).

## 2. Requisitos funcionales

### 2.1 Acceso (RF-ACC)

| ID | Requisito | Estado | Evidencia | Acción sugerida |
|---|---|---|---|---|
| ACC-01 | Login; el contrato se autocompleta | Parcial | `ServicioAcceso.IniciarSesion` pide empresa, usuario y clave; no hay contrato como ámbito | Decidir si "contrato" = operación (ver P-13) |
| ACC-02 | Menú por módulos con rutas Planificación, Pedidos, Transacciones Ent/Sal, Ventas, Gastos, Cierre, Control de documentos, Informes | Parcial | `FormPrincipal`: Catálogo, Menús, Compras, Almacén, Cierres y control, Administración | Reagrupar el menú según el manual o acordar la taxonomía propia |
| ACC-03 | Acciones estándar (incluir, alterar, actualizar, borrar, cancelar, confirmar, buscar, vista previa, imprimir, guardar, filtrar) | Parcial | `Ui.Boton` con Nuevo, Editar, Actualizar, Quitar, Anular, Aprobar, Buscar, Imprimir; la vista previa es la del sistema, no propia | Estandarizar nombres de botones; decidir si la vista previa se hace en pantalla |
| ACC-04 | Offline con cola de sincronización e indicador por día | Parcial | Cola `sincronizacion_evento`; estado en barra de estado; `EstadoColaDto` | Mostrar en el calendario de cierres el estado "pendiente sync" por día |
| ACC-05 | Auditoría con valores previos | Cumple | Trigger `auditar` en tablas principales; `ConsultarAuditoria` con antes/después | Ver §3 (tablas sin auditoría) |

### 2.2 Planificación teórica (RF-PT)

| ID | Requisito | Estado | Evidencia | Acción sugerida |
|---|---|---|---|---|
| PT-01 | (a) Planificación con recetas; (b) estructura fija diaria solo con productos | Parcial | (a) minutas y platos; (b) `minuta_estructura_fija` (`AgregarFijo`) | Modalidad (b) sin pantalla propia de venta directa/retail |
| PT-02 | Selección por contrato, régimen, servicio y fecha | Parcial | Servicio × régimen (`operacion_servicio`); calendario por rango | Ver P-13 |
| PT-03 | Histórico con estado Abierta / Cerrada (pedido hecho) | Falta | No existe estado "cerrada por pedido" en minutas | Agregar estado y bloqueo al generar el pedido (RF-PM-10) |
| PT-04 | Grilla estructura × día: receta, N.Rac., Cto.Plat., fila Comensales | Parcial | `FormPlanillaMenu` (entrega 11): receta, raciones, % y comensales; el costo por persona está en la matriz, no en la celda | Mostrar Cto.Plat en la celda o en tooltip |
| PT-05 | Costo minuta día = Σ(N.Rac × Cto.Plat) / Comensales | Cumple | `MinutaDto.CostoPrevistoU6`, `CostoComensalU6`; resumen por día en la matriz | — |
| PT-06 | Costo patrón techo y alerta si lo supera | Cumple | `CeldaMatrizDto.TechoU6` y `DesviacionU6`; objetivo en `operacion_servicio.CostoObjetivoRacionU6` | — |
| PT-07 | Acción "Actualizar costos receta" como primer paso | Falta | No existe | Agregar acción que recalcula costos de recetas antes de planificar |
| PT-08 | Buscar receta dentro de la planificación | Falta | Buscador solo en `FormRecetas` | Agregar buscador en `FormPlanillaMenu` |
| PT-09 | Aportes nutricionales por receta y día | Decisión | Sin tabla de composición (decisión del usuario: diferido) | Ver P-15 / pregunta de nutrientes |
| PT-10 | Panel de costos: total mes, día, acumulado a la fecha | Parcial | `ComparativoMes`, `ResumenPorDia`; falta acumulado a la fecha en pantalla | Agregar panel acumulado |
| PT-11 | Frecuencia de recetas | Cumple | Reporte `FrecuenciaRecetas` | — |
| PT-12 | Celdas con estado (estructura / bloqueada / habilitada) | Parcial | `EstadosOperativos`; minuta aprobada bloquea la edición | Mostrar el estado de celda en la planilla |
| PT-13 | Estructura fija: copiar el primer lunes a los demás lunes | Parcial | Estructura fija por minuta; sin copia semanal | Agregar "copiar semana" |
| PT-14 | Costo detallado y resumido teórico; previsión de consumo con filtros | Parcial | Reportes `CostoDetalladoTeorico`, `CostoResumidoTeorico`, `PrevisionConsumo`; filtros de régimen y servicio no verificados | Verificar filtros en `FormReportes` |

### 2.3 Pedido mensual y pedido extra (RF-PM, RF-PE)

| ID | Requisito | Estado | Evidencia | Acción sugerida |
|---|---|---|---|---|
| PM-01 | Pedido sugerido de lo planificado (teórica + estructura fija) por periodo | Cumple | `ServicioCompras.CalcularPrevision` | — |
| PM-02 | Fórmula Pedido = NT − SA + SS − OC + NR | Parcial | Desglose con demanda, stock, reserva, pendiente y necesidad; **stock de seguridad (SS) no está** (pregunta P-04) | Decidir SS (P-04) |
| PM-03 | Columnas: código, descripción, unidad, necesidad, pedido propuesto, unidad de despacho, fechas, cantidad | Parcial | `gridDesglose` y `gridLineas`; unidad de despacho = empaque | Revisar columnas contra la lista |
| PM-04 | Desglose de la fórmula al posicionarse | Cumple | Al seleccionar un producto se muestra el desglose | — |
| PM-05 | Fechas sugeridas editables; cantidad editable | Parcial | Fecha editable en `AgregarLinea`; sugerencia automática no verificada | Sugerir fecha desde la previsión |
| PM-06 | Agregar o cambiar producto a mano | Parcial | `AgregarLinea`, `PedidoManual`; cambiar = quitar y agregar | Acción "cambiar producto" |
| PM-07 | Fecha máxima para generar el pedido; después se bloquea | Falta | No existe | Agregar fecha límite por operación |
| PM-08 | Precondiciones: movimientos del día, regenerar si hubo cambios | Parcial | `Diferencias` marca la previsión como OBSOLETA | Bloquear generación si hay movimientos del día sin cerrar |
| PM-09 | Anulaciones y pedidos extra no cuentan en el cálculo | No verificado | No se revisó la consulta de necesidades | Verificar con prueba |
| PM-10 | Al enviar: se bloquea la planificación teórica y se crea la Planificación Real | Falta | No existe el concepto de planificación real ni bloqueo al enviar | Ver sección 2.4 |
| PM-11 | El pedido sube a ADS solo tras el cierre diario | Falta | Sin integración ADS | Ver RF-TR y P-02 |
| PM-12 | Reporte "Mapa de solicitud de compras" | Cumple | `MapaSolicitudCompras` | — |
| PE-01 | Pedido extra en cualquier momento; varios por periodo; número y estado | Cumple | `pedido_compra.tipo = 'extra'`; `PedidoManual` | — |
| PE-02 | Cantidad, fecha de entrega editable, agregar productos, grabar y enviar | Parcial | Captura y aprobación; no existe el estado "enviado" como acción separada | Confirmar si "enviar" = aprobar |
| PE-03 | No requiere cierre diario para subir a la central | Parcial | Sin central conectada | Ver P-01 |
| PE-04 | Reporte "Mapa de solicitud de compras" | Cumple | Mismo reporte | — |

### 2.4 Planificación real (RF-PR)

| ID | Requisito | Estado | Evidencia | Acción sugerida |
|---|---|---|---|---|
| PR-01 | Se crea al enviar el pedido mensual (copia de la teórica) | Falta | — | Ver PM-10 |
| PR-02 | Misma grilla que la teórica, con histórico por servicio y mes | Parcial | `FormPlanillaMenu` por rango; sin copia de "real" | — |
| PR-03 | Cierre diario por fecha calendario con tolerancia de 3 días; después se bloquea | Falta | Cierre sin tolerancia (`CerrarDia`) | Agregar tolerancia configurable |
| PR-04 | Modificar comensales, raciones y platos; cambiar plato por otro; no cambiar ni agregar estructuras | Cumple | `ActualizarComensales`, `FijarRaciones`, `SustituirReceta`; `AgregarPlato` solo en estructuras existentes | — |
| PR-05 | Selector de plato filtrable por categoría dietética, tipo de plato y texto; solo recetas de régimen o patrón | Falta | Recetas sin categoría dietética ni tipo de plato | Agregar campos y filtro |
| PR-06 | Ver receta según régimen, aportes, frecuencia, actualizar costos | Parcial | Ver receta en `FormRecetas`; aportes y actualizar costos faltan | Ver PT-07 y PT-09 |
| PR-07 | Exportar recetas del día a Excel o Word | Falta | — | Exportar desde el catálogo de reportes |
| PR-08 | Alerta de costo minuta vs techo | Parcial | `DesviacionU6` en la matriz; sin alerta visible en la planilla | Mostrar alerta en la planilla |
| PR-09 | Reporte "Planificación Real" | Parcial | `MinutaDelDia` cubre parte | Crear el reporte con su nombre |

### 2.5 Requerimientos a almacén (RF-RD)

| ID | Requisito | Estado | Evidencia | Acción sugerida |
|---|---|---|---|---|
| RD-01 | Emitir requerimientos solo para servicios con planificación real | Parcial | Requerimiento desde minuta aprobada; no existe "planificación real" | Ver PR-01 |
| RD-02 | Filtros: contrato, tipo de informe, fechas, régimen, servicio, consolidado por fecha | Parcial | `RequisicionPorServicio` y `RequisicionPorEstructura` con fechas | Agregar régimen y consolidado |
| RD-03 | Formatos: requisición por servicio y por estructura detallada | Cumple | `RequisicionPorServicio`, `RequisicionPorEstructura` | — |
| RD-04 | Columnas: cantidad minuta, real, extra, devolución, unidad | Parcial | Previsto, solicitado, entregado, excedente, faltante; devolución por solicitud (V028) | Alinear nombres de columnas |
| RD-05 | Servicios no planificados: solicitud manual, aprobación del jefe, envío a almacén | Parcial | Adicional con motivo y aprobación (`ADICIONAL_APROBAR`) | Tipo de servicio no planificado (eventos, retail) |
| RD-06 | Flujo recepción en contrato → requerimiento → entrega → despacho | Parcial | Requerimiento → entrega → salida; sin recepción "en contrato" | Ver P-13 |

### 2.6 Entradas (RF-EN)

| ID | Requisito | Estado | Evidencia | Acción sugerida |
|---|---|---|---|---|
| EN-01 | Traspaso CD (entrada) | Decisión | No hay almacén central en esta rama | Ver P-01 y la fase 3b |
| EN-02 | Recepción de proveedor con OC y guía de remisión | Cumple | `RecibirPedido` con tipo y número de documento | — |
| EN-03 | FOFI y caja chica (factura o boleta) | Parcial | Caja chica como tipo de pedido; comprobante único por tipo y número (V008) | Revisar flujo de FOFI |
| EN-04 | Traspaso entre contratos (entrada) | Parcial | Traspaso entre almacenes de la operación (V033); contrato no es ámbito de stock | Ver P-13 |
| EN-05 | Traspaso entre bodegas (entrada) | Cumple | `Recibir` (V033) | — |

### 2.7 Salidas (RF-SA)

| ID | Requisito | Estado | Evidencia | Acción sugerida |
|---|---|---|---|---|
| SA-01 | Devolución de traspaso CD (referencia obligatoria; cantidad ≤ recibida) | Falta | Depende del CD | Ver P-01 |
| SA-02 | Traspaso CD (salida) | Falta | Depende del CD | Ver P-01 |
| SA-03 | Traspaso entre contratos (salida) con exportación al destino (archivo `SGPtras…`) | Falta | No hay exportación de traspasos | Ver P-09 |
| SA-04 | Traspaso entre bodegas (salida) queda PENDIENTE; el stock no cambia hasta la entrada; bloquea el cierre | Cumple | V033: estado `enviado`; `TRANSITO_PENDIENTE` bloquea el cierre | — |
| SA-05 | Salida de bodega a producción | Cumple | `SalidaProduccion`, entrega por requerimiento | — |
| SA-06 | Venta directa (documento propio) | Falta | Sin módulo de venta directa | Ver P-07 |
| SA-07 | Venta cafetería | Falta | Sin módulo; sin datos de origen (R14) | Ver P-07 |
| SA-08 | Mermas descargadas en un servicio "Servicio 1" | Parcial | Mermas con etapa (`RegistrarMerma`); sin servicio "Servicio 1" | Crear el servicio de mermas |

### 2.8 Ventas (RF-VE)

| ID | Requisito | Estado | Evidencia | Acción sugerida |
|---|---|---|---|---|
| VE-01 | Servicios de precio fijo: raciones por día y cliente, personal Sodexo, producidas | Parcial | Venta real por raciones (`RegistrarVenta`); cliente por contrato; "personal" no | Agregar tipo de comensal |
| VE-02 | Servicios de precio variable: monto diario sin IGV, forma de pago contado | Parcial | `VentaServicioContado` con importe; forma de pago no | Agregar forma de pago |
| VE-03 | Calendario mensual con días bloqueados por cierre | Cumple | `CalendarioMes` con estados (Cerrado bloquea) | — |
| VE-04 | Venta directa y venta cafetería | Falta | Sin desarrollo en el manual (P-07) | Ver P-07 |
| VE-05 | Reportes: venta de servicios, facturación de clientes, comparativo de raciones | Parcial | `VentaServicioContado`, `ComparativoRaciones`; facturación de clientes no existe | Crear reporte de facturación |

### 2.9 Cierre diario y mensual (RF-CD, RN-CM)

| ID | Requisito | Estado | Evidencia | Acción sugerida |
|---|---|---|---|---|
| CD-01 | Recoge modificaciones, pedidos, ingresos, salidas, inventarios y costos | Cumple | Pendientes y datos del día en `LeerPendientes` y en la foto de cierre (V032) | — |
| CD-02 | Pre-chequeo: costo realizado, food cost, posición de stock, movimiento, consumo alternativo, costo teórico-real, comparativo | Parcial | `CostoRealizado`, food cost, `StockValorizado`, `Kardex`, comparativo; consumo alternativo no es un reporte | Agregar consumo alternativo al pre-chequeo |
| CD-03 | Validaciones bloqueantes: traspasos pendientes, salidas a producción sin cerrar, inventario rotativo pendiente | Parcial | Traspasos (V033) e inventario abierto (`INVENTARIO_ABIERTO`); "salidas sin cerrar" no tiene validación | Agregar validación de salidas |
| CD-04 | Calendario: habilitado, cerrado no enviado, cerrado enviado | Parcial | Estados Cerrado, Con pendientes, Listo, Abierto; sin "enviado" | Ver P-01 (envío a central) |
| CD-05 | Cierre con confirmación; sin modificaciones ni reaperturas | Cumple | `CerrarDia` con confirmación; día cerrado inmutable (prueba `DIA_CERRADO`) | — |
| CD-06 | Envío a la central automático nocturno; cierres acumulados al reconectar | Parcial | `sincronizar` e instalador; `programar_sede.ps1`; central no configurada | Ver P-01 |
| CD-07 | Cerrar días sin conexión | Cumple | Cierre local | — |
| RN-CM-01 | No cerrar el mes con días sin cerrar o documentos pendientes | Cumple | `CerrarMes` usa `LeerPendientes` | — |
| RN-CM-02 | Cerrar el mes es prerrequisito para las rutinas del mes siguiente | No verificado | Sin prueba de bloqueo del mes siguiente | Probar |
| RN-CM-03 | Envío a SAP irreversible, con confirmación | Falta | Sin integración SAP | Ver P-02 |
| RN-CM-04 | Cambios de precio posteriores al ajuste obligan a rehacer inventario | No verificado | No se revisó la regla | Revisar con prueba |

### 2.10 Traspasos ADS (RF-TR)

| ID | Requisito | Estado | Evidencia | Acción sugerida |
|---|---|---|---|---|
| TR-01 | Filtros: fecha, tipo de operación (traspaso CD / devolución), tipo (entrada / salida), estado SGP (abiertas / cerradas) | Parcial | `Transitos` filtra por fecha; sin tipo ni estado SGP | Agregar filtros |
| TR-02 | Cabecera con documentos ADS y SGP | Falta | Sin integración ADS | Ver P-02 |
| TR-03 | Detalle con NC y ND aceptadas o no aceptadas | Falta | No existen notas de crédito ni de débito | Ver P-02 |
| TR-04 | Regla NC: si Logística acepta, no cambia stock; si no acepta, reingresa y descarga como consumo alternativo | Falta | — | Ver P-02 |
| TR-05 | Regla ND: solo entradas con diferencia positiva | Falta | — | Ver P-02 |
| TR-06 | Respuestas de ADS que actualizan el monitor | Falta | — | Ver P-02 |

### 2.11 Inventario (RF-IR, RF-IN)

| ID | Requisito | Estado | Evidencia | Acción sugerida |
|---|---|---|---|---|
| IR-01 | Inventario diario al azar basado en el consumo del mes anterior y la curva ABC | Parcial | Toma rotativa con lista; clasificación ABC (`ClasificarAbc`); selección al azar y por consumo no | Agregar selección automática |
| IR-02 | A primera hora; el sistema no permite transacciones del día sin inventario | Falta | Solo aparece como pendiente del cierre | Bloquear movimientos del día sin toma |
| IR-03 | Toma rotativa: stock físico y diferencias a consumo alternativo | Parcial | `FormInventarios` con toma rotativa; diferencias van a ajuste con motivo (no "consumo alternativo") | Renombrar o agregar el motivo |
| IN-01 | Listados: toma, diferencias, valorizados, filtros por familia, solo diferencias, incluir stock cero | Parcial | `Inventario`, `RegistroInventarioPermanente`; filtros no verificados | Verificar filtros |
| IN-02 | Exportar plantilla e importar cantidades | No verificado | Hoja de conteo; importación no verificada | Probar |
| IN-03 | Envío del inventario a SAP | Falta | Sin integración SAP | Ver P-02 |

## 3. Requisitos no funcionales (RNF)

| ID | Requisito | Estado | Evidencia | Acción sugerida |
|---|---|---|---|---|
| RNF-01 | Operación diaria sin internet | Cumple | Servidor local por sede | — |
| RNF-02 | Sincronización idempotente con resolución de conflictos | Parcial | Cola de eventos y estado "en conflicto" (`EstadoColaDto`) | Probar la resolución de conflictos |
| RNF-03 | Integridad de stock y kárdex | Cumple | Triggers, `FOR UPDATE`, conciliación (`v_conciliacion_saldo`) | — |
| RNF-04 | Roles, auditoría, cifrado, contraseñas con hash | Parcial | Roles y auditoría; contraseñas con hash (`ClaveSegura`); cifrado en tránsito y en reposo no verificado | Verificar SSL de la conexión |
| RNF-05 | Grillas de 31 días × 10 estructuras con respuesta < 2 s | No verificado | No se midió | Medir con datos del SGP |
| RNF-06 | Atajos de teclado y captura rápida | Parcial | Ctrl+K, Ctrl+F4; faltan atajos de captura | Definir atajos de almacén |
| RNF-07 | Impresión con firma "Entregado conforme" y cabecera de contrato | Falta | Sin esa firma en los reportes | Agregar al formato de requerimiento |
| RNF-08 | Carga inicial de maestros y saldos | Cumple | Instalador `importar-*`, `cargar-*`, `importar-inventario` | — |
| RNF-09 | Aislamiento de datos por contrato | Parcial | Aislamiento por empresa (RLS); contrato no es ámbito | Ver P-13 |
| RNF-10 | Trazabilidad de ajustes y anulaciones | Cumple | Auditoría; motivos normalizados (V031) | — |
| RNF-11 | Backup y recuperación | Cumple | Comandos `respaldar` y `restaurar` | — |
| RNF-12 | VB.NET | Cumple | Proyectos .NET 8 | — |
| RNF-12.1 | Windows 10 o superior | Cumple | `net8.0-windows` | — |
| RNF-12.2 | .NET 8 y verificación del runtime en el instalador | Parcial | Runtime no incluido | Verificar runtime en el instalador |
| RNF-12.3 | Instalación simple con versión visible | Parcial | Versión en la barra de estado; instalador de escritorio no empaquetado | Empaquetar |
| RNF-13 | PostgreSQL con un servidor por sede | Cumple | Migraciones y RLS | — |
| RNF-13.1 | Esquema portado de la referencia SQLite | Cumple | Migraciones V001 a V033 | — |
| RNF-13.2 | Transacciones con bloqueo por fila; rol de aplicación | Cumple | `app_stock`; `FOR UPDATE` | — |
| RNF-13.3 | Servidor de sede con respaldo programado | Parcial | `respaldar` existe; programación no verificada | Programar el respaldo |
| RNF-14 | Volumen ≈ 20 computadoras | Decisión | Pendiente de confirmar (P-13) | Ver P-13 |
| RNF-15 | WinForms | Cumple | Formularios con Designer | — |
| RNF-16 | Ramas `main` ← `develop` ← `feature/etapa-N-*` | Parcial | Rama de trabajo `claude/…`; ramas `feature/*` con otra numeración | Ver CLAUDE.md §4 |

## 4. Preguntas abiertas (sección P) y decisiones

| ID | Pregunta | Estado en el sistema | Qué necesito del usuario |
|---|---|---|---|
| P-01 | Nube o on-premise para el servidor central | Abierta | Decisión de arquitectura |
| P-02 | Interfaces con ADS, SGO y SAP: APIs o archivos | Abierta | Contratos o archivos de ejemplo |
| P-03 | Redondeo del pedido y fechas de entrega sugeridas | Abierta | Regla del proveedor |
| P-04 | Stock de seguridad y quién lo mantiene | Abierta | Valores y responsable |
| P-05 | PMP y valorización de consumo alternativo | Abierta | Fórmula |
| P-06 | "4 tipos de requerimiento" con solo 2 descritos | Abierta | Los otros dos tipos |
| P-07 | Venta directa y venta cafetería sin desarrollo | Abierta | Especificación y datos (R14) |
| P-08 | Checklist de rutinas vacío en el manual | Abierta | Rutinas operacionales |
| P-09 | Nombre del archivo de traspaso y si se reemplaza por electrónico | Abierta | Formato y decisión |
| P-10 | Login de soporte para precios o roles con aprobación | Abierta | Decisión (hoy: roles con aprobación) |
| P-11 | Migrar la historia del SGP o solo saldos | Abierta | Decisión |
| P-12 | Reportes de gerencia | Abierta | Lista |
| P-13 | Volumen, contratos, sedes y usuarios simultáneos | Abierta | Números |
| P-14 | IGV en compras (exento, neto, IGV, otros impuestos) | Parcial: precios sin IGV (D03) | Confirmar el tratamiento de compras |
| P-15 | Manual de 2013: qué procesos cambiaron | Abierta | Revisión con el negocio |
| P-16 | Otros países | Abierta | Confirmar que es solo Perú |

## 5. Base de datos

### 5.1 Comparación con el modelo del documento

La base limpia de esta rama tiene **75 tablas**. El documento pide las entidades de la sección 4 (contratos, regímenes, servicios, recetas, planificaciones, pedidos, traspasos ADS, notas de crédito y débito, inventarios, cierres). Lo que existe:

* **Existe:** productos, variantes, recetas, minutas, servicios y regímenes, pedidos y recepciones, movimientos y saldos, documentos de stock, requerimientos, producción, inventarios y conteos, cierres y validaciones, auditoría, usuarios, roles y permisos, traspasos entre almacenes (V033).
* **Falta en el modelo:** contratos como ámbito de stock, almacén central, traspasos ADS (cabecera y detalle), notas de crédito y débito, planificación real, ventas directas, cafetería, tabla de composición nutricional, categoría dietética y tipo de plato.

### 5.2 Diferencias entre la base real y esta rama

La base real `appsistema` tiene 92 tablas y objetos que no vienen de esta rama:

* **17 tablas de otras ramas:** `planificacion`, `planificacion_estado`, `planificacion_plato`, `planificacion_producto`, `planificacion_servicio`, `planificacion_version`, `produccion_plan`, `produccion_plan_cambio`, `sgp_codigo_producto`, `sgp_codigo_receta`, `sgp_comparativo_dia`, `sgp_comparativo_total`, `sgp_costo_piso_techo`, `sgp_plan_dia`, `sgp_plan_plato`, `sgp_preparacion`, `sgp_requisicion`.
  * Ninguna se usa en el código de esta rama.
  * Las `sgp_*` tienen **datos cargados** (≈ 15 000 filas en total: plan de 552 días, 11 046 platos, 2 778 requisiciones). Vienen del plan real del SGP de la otra rama.
  * **No se borran sin decisión del usuario.**
* **Columna `operacion.dias_stock_base`:** viene de la otra rama; una operación tiene valor. El código de esta rama no la usa.

Las migraciones V022 a V025 de esas ramas están registradas en `esquema_migracion` de la base real. Es el choque de numeración descrito en `CLAUDE.md` §4.

### 5.3 Comprobaciones estructurales (aplican a la base limpia y a la real)

| Comprobación | Resultado | Lectura |
|---|---|---|
| Tablas sin RLS | `central_cierre`, `central_documento`, `central_movimiento`, `evento_recibido`, `origen_sincronizacion`, `rechazo_sincronizacion`, `sede_central` (y `esquema_migracion`) | Tablas de la central y de sincronización. Confirmar que deben ser globales; si no, agregar RLS |
| Tablas sin trigger `auditar` | 19 tablas: `movimiento_stock`, `saldo_stock`, `documento_stock_detalle`, `recepcion_detalle`, `requerimiento_detalle`, `prevision_detalle`, `produccion_documento`, `costeo_ingrediente`, `cierre_validacion`, `integracion_envio`, `sincronizacion_evento`, y las de central y sincronización | Las de detalle se auditan por su cabecera. **`movimiento_stock` (el libro) y `saldo_stock` no tienen auditoría propia**; RNF-10 y CLAUDE.md piden trazabilidad. Recomiendo auditar el libro |
| Tablas sin grant a `app_stock` | Tablas de central y sincronización | Coherente con su rol; confirmar |
| Claves foráneas sin índice de prefijo | 37 | Son las de la lista fija de la prueba de índices (`Las_claves_foraneas_tienen_indice…`). Intencional |

### 5.4 Lo que debe verificarse con pruebas antes de cerrar

* RN-CM-02, RN-CM-04, PM-09 e IN-02 (ver tabla de la sección 2).
* Que las tablas de central no se usen desde la operación local por error.

## 6. Plan de cierre recomendado (orden)

1. **Decisiones del usuario** (sección 4): sobre todo P-01, P-02 y P-13. Son las que destraban las integraciones y el alcance de contratos.
2. **Base de datos:**
   1. decidir qué hacer con las 17 tablas de otras ramas (conservar en un esquema aparte, o eliminar después de respaldar);
   2. auditar `movimiento_stock` y `saldo_stock`;
   3. confirmar que las tablas de central deben ser globales.
3. **Formularios de mayor valor operativo:**
   1. planilla del menú: buscador de receta, Cto.Plat. en celda, alerta de techo, copia semanal (PT-04, PT-08, PT-13, PR-08);
   2. pedido: fecha máxima y precondiciones (PM-07, PM-08);
   3. planificación real y tolerancia de cierre de 3 días (PM-10, PR-03).
4. **Reportes y exportaciones:** recetas del día (PR-07), facturación de clientes (VE-05), mapa y planificación real (PR-09).
5. **Integraciones:** ADS, SAP y central, solo con especificación y entorno del cliente (sección 2.10, RN-CM-03).
6. **Pruebas:** RN-CM-02, RN-CM-04, PM-09, IN-02, y la medición de RNF-05.

## 7. Limitaciones de esta revisión

* La clasificación es por evidencia en código y en la base; no incluye pruebas manuales de cada flujo.
* Los "No verificado" no son fallas: faltó tiempo o un entorno para comprobarlos.
* No se modificó la base real: se usó una copia limpia construida desde las migraciones (`appsistema_limpia`), que se eliminó al terminar.
