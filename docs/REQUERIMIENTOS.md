# Documento de Requerimientos – Sistema de Gestión Previsional (propio)

| Campo | Valor |
|---|---|
| Versión | 0.1.1 – Borrador para revisión (se registra RNF-12: VB.NET) |
| Fecha | 2026-10-02 |
| Fuente | *Manual SGP Local – Para Operaciones* (Sodexo Perú, V006, 01/07/2013, 92 pp.) |
| Estado | Borrador: requiere validación del negocio (ver §12 Preguntas abiertas) |

> Las referencias `[p.N]` apuntan a la página del manual. Los requisitos nuevos que **no** salen del manual están marcados como **(propuesto)**.

---

## 1. Introducción

### 1.1 Propósito
Definir los requerimientos de un sistema propio que reemplace al SGP actual (Windows, cliente de escritorio con base local) y cubra el ciclo completo de una operación de alimentación: **planificación de menú → pedidos de compra → recepción → stock → salidas a producción → ventas/raciones → control de costos → cierres diario y mensual**.

### 1.2 Alcance
El sistema gestiona, por **contrato** (operación/sitio):

1. Planificación de menú (teórica y real) y costeo de recetas.
2. Pedidos de compra (mensual y extra).
3. Requerimientos diarios de cocina a almacén.
4. Entradas y salidas de mercadería (7 tipos de entrada/salida de stock + mermas).
5. Registro de raciones y ventas (facturación oficial queda fuera: se hace en SGO).
6. Cierre diario, cierre de mes y resultado operacional (A13).
7. Control de tránsitos con el Centro de Distribución (CD).
8. Inventarios (completo y rotativo) y ajustes.
9. Integraciones con ADS, SGO y SAP.

### 1.3 Fuera de alcance (según el manual)
- Facturación oficial (SGO) [p.66].
- Contabilidad / cierre contable (SAP): solo se le envía información [p.82].
- Gestión de compras/logística del CD (ADS): solo se integra.
- Capítulo 1 "Check list de rutinas" del manual está vacío ("adjuntar rutinas operacionales") [p.4] → **pendiente de recibir**.

### 1.4 Glosario

| Término | Significado |
|---|---|
| SGP | Sistema de Gestión Previsional (sistema actual) |
| Contrato / Site / CECO | Operación (cliente) donde se presta el servicio. Cada uno tiene código de centro de costo (ej. `GBABPA0721`) |
| Régimen | Tipo de menú/población (Ejecutivo, Económico, General, Normal…) |
| Servicio | Desayuno, Almuerzo, Cena, Lonchera, Retail, Eventos, etc. |
| Estructura de servicio | Componentes de un servicio (Sopa normal, Plato de fondo, Guarnición, Bebida fría…) |
| Minuta | Menú planificado |
| NT / SA / SS / OC / NR | Necesidad teórica / Stock actual / Stock de seguridad / Órdenes de compra por recibir / Necesidad por consumir según minuta real |
| CD | Centro de Distribución (Lima Fríos, Lima Secos, Arequipa, Trujillo, Cuzco, Chiclayo…) |
| Cesión | Documento de despacho del CD hacia el contrato |
| OC | Orden de compra a proveedor |
| FOFI | Fondo fijo / caja chica |
| NC / ND | Nota de crédito / Nota de débito |
| ADS | Sistema de compras y logística (externo) |
| SGO | Sistema de facturación (externo) |
| SAP | Sistema contable (externo) |
| PMP | Precio medio ponderado |
| A13 | Resultado Operacional Mensual |
| Kardex | Libro de movimientos de stock por producto |
| Consumo Alternativo | Servicio especial donde se imputan ajustes de inventario y diferencias no aceptadas |

---

## 2. Visión general del producto

### 2.1 Modelo operativo actual (as-is) [p.7]
- Aplicación de escritorio, instalada en 1..N PCs por contrato; **una** aloja la BD local.
- Funciona **offline**; necesita internet solo para sincronizar con la **BD Central** (envío diario de movimientos) y para descargar cesiones/OC desde ADS.
- Intercambios entre contratos por archivos `.zip` enviados por correo (Outlook) [p.46–59].
- Reportes impresos, exportaciones a Excel para inventario.

### 2.2 Dirección propuesta (to-be)
- **Restricción tecnológica confirmada (RNF-12):** el sistema se desarrolla en **VB.NET** y debe ejecutarse en **todas las computadoras** de la operación (ver §8). **Reemplaza** la propuesta anterior de PWA/web.
- Se mantiene la tolerancia a conectividad mala de los sites: la operación diaria no depende de internet y se sincroniza cuando hay conexión **(propuesto)**.
- Eliminar el intercambio de `.zip` por correo: traspasos entre contratos como documentos electrónicos, con modo diferido si no hay conexión **(propuesto)**.
- Arquitectura detallada, motor de base de datos y mecanismo de sincronización: por definir (§12, P-01 y P-17).

### 2.3 Principios de diseño derivados del manual
1. **Los documentos cerrados son inmutables** (no hay "marcha atrás"); se corrigen anulando y rehaciendo, conservando histórico de anulados [p.38, 53, 64].
2. **El día cerrado no se reabre** y el mes cerrado no admite documentos [p.69, 72].
3. **Orden estricto del flujo**: teórica → pedido → real → requerimiento → salidas → ventas → cierre.
4. Todo movimiento deja rastro auditable en Kárdex.

---

## 3. Actores y roles

| Rol | Responsabilidades principales |
|---|---|
| Operador de bodega / almacén | Entradas, salidas, picking, despacho, inventarios |
| Cocina / Chef | Revisión de planificación real, requerimientos, mermas |
| Jefe de Contrato / Jefe de Catering | Planificación, pedidos, aprobación de requerimientos manuales, cierres |
| Administrador del contrato | Ventas/raciones, gastos A13, cierre de mes |
| Gastronomía y Nutrición (central) | Planifica menú centralizado, recetas, regímenes |
| Gerencia de Operaciones (central) | Define costo patrón techo |
| Compras / Logística (ADS) | Acepta/rechaza NC, ND y devoluciones |
| Soporte | Ingreso/cambio de precios de productos (login especial) [p.78] |
| Administrador del sistema | Usuarios, roles, contratos, parámetros |

Requisito transversal **RF-SEG**: autenticación por usuario/contraseña, asociación usuario↔contratos habilitados, selección de contrato al ingresar [p.5], permisos por rol y por pantalla, y **doble autorización** (usuario de soporte) para crear/cambiar precios [p.78].

---

## 4. Modelo de dominio (entidades principales)

**Maestros**: Contrato, Régimen, Servicio, Régimen-Servicio por contrato (tabla que define dónde se hace la salida de bodega [p.64]), Estructura de servicio, Receta (con ingredientes, rendimiento, aportes nutricionales, categoría dietética, tipo de plato, tipo de receta *Patrón / x Régimen / Local*), Producto (código, unidad, factor de conversión, familia, clase ABC, unidad de despacho), Bodega, Proveedor (incl. "Proveedor Caja Chica"), Cliente, CD, Cuenta contable, Impuestos, Calendario de cierre.

**Transaccionales**:
- Planificación: PlanificaciónTeórica, PlanificaciónEstructuraFija, PlanificaciónReal (por contrato-régimen-servicio-mes-día).
- Compras: PedidoMensual, PedidoExtra (con detalle y fechas de entrega).
- Requerimiento diario.
- Stock: Documento de movimiento (Entrada/Salida + tipo + documento origen), Kárdex, StockPorBodega, PMP.
- Ventas: ControlRaciones (cliente×día), VentaServicioContado (monto×día).
- Cierres: CierreDiario, CierreMes, Folio.
- Gastos A13, Inventario (toma), ConsumoAlternativo, NC/ND/SolicitudDevolución.

---

## 5. Requisitos funcionales

Prioridad: **M** = Must, **S** = Should, **C** = Could.

### 5.1 Acceso y generalidades
| ID | Requisito | Pri | Ref |
|---|---|---|---|
| RF-ACC-01 | Login con usuario/contraseña; el contrato se autocompleta según el usuario y puede cambiarse si tiene varios | M | p.5 |
| RF-ACC-02 | Menú por módulos con las mismas rutas funcionales (Planificación, Pedidos, Transacciones Ent/Sal, Ventas, Gastos, Cierre, Control de documentos, Informes) | M | p.2, 6 |
| RF-ACC-03 | Acciones estándar en toda pantalla: incluir, alterar, actualizar lista, borrar, cancelar, confirmar, buscar, vista previa, imprimir, salir, guardar, filtrar | M | p.6 |
| RF-ACC-04 | Funcionamiento **offline** con cola de sincronización; indicador visible de estado de sincronización por día | M | p.7, 69 |
| RF-ACC-05 | Auditoría: usuario, fecha/hora y valores previos de cada cambio | S | propuesto |

### 5.2 Planificación teórica [Cap. 3, p.8–17]
Se planifica con anticipación; su objetivo es generar el pedido de compras.

| ID | Requisito | Pri |
|---|---|---|
| RF-PT-01 | Dos modalidades: **(a) Planificación teórica** con recetas (almuerzo, cena…) y **(b) Estructura fija día teórica** solo con productos (venta directa, retail, lavandería) | M |
| RF-PT-02 | Selección por contrato, régimen, servicio, fecha (calendario mensual) | M |
| RF-PT-03 | Histórico de planificaciones con estado **Abierta** (sin pedido mensual) / **Cerrada** (pedido ya hecho) | M |
| RF-PT-04 | Grilla por estructura de servicio × día con: receta, **N.Rac.** (cantidad a preparar) y **Cto.Plat.** (costo por persona); fila de **Comensales** | M |
| RF-PT-05 | **Costo minuta día** = Σ(N.Rac × Cto.Plat) / Comensales | M |
| RF-PT-06 | Mostrar **Costo patrón techo** (objetivo definido por gerencia) y alertar si el costo minuta lo supera | M |
| RF-PT-07 | Acción "Actualizar costos receta" como primer paso antes de trabajar | M |
| RF-PT-08 | Buscar receta dentro de la planificación | S |
| RF-PT-09 | **Aportes nutricionales** por receta y total del día (bruto, servido, neto, agua, energía, proteínas, etc.) | S |
| RF-PT-10 | **Verificar costos**: panel Total mes (materia prima, estructura fija, total, raciones, costo bandeja), Día (planificado vs realizado), Acumulado a la fecha | M |
| RF-PT-11 | **Frecuencia de recetas**: veces que se repite cada receta en el mes, total de recetas listadas y costo promedio diario | S |
| RF-PT-12 | Celdas con estado: estructura de servicio / bloqueada / habilitada | M |
| RF-PT-13 | **Estructura fija**: ingresar productos y cantidades por día; permitir copiar el primer lunes a los demás lunes (consumo semanal) o cargar día por día; repetir para todos los servicios no planificados centralmente | M |
| RF-PT-14 | Reportes: Costo detallado (teórico), Costo resumido (teórico), **Previsión de consumo** (total por producto en rango, con opción detallada, filtro por régimen/servicio) | M |

### 5.3 Pedido mensual [Cap. 4, p.18–21]
| ID | Requisito | Pri |
|---|---|---|
| RF-PM-01 | Calcular el **pedido sugerido** de todo lo planificado (teórica + estructura fija) para el período elegido (mm/aaaa) | M |
| RF-PM-02 | Fórmula por producto: **Pedido propuesto = NT − SA + SS − OC + NR** (ver §6.1) | M |
| RF-PM-03 | Mostrar columnas: código, descripción, unidad, necesidad según minuta teórica, pedido propuesto, unidad de despacho (formato de compra / mínimo a pedir), fecha(s) de entrega, cantidad a solicitar | M |
| RF-PM-04 | Al posicionarse en el pedido propuesto, mostrar el **desglose de la fórmula** | S |
| RF-PM-05 | Fechas de entrega sugeridas por el sistema y **modificables**; cantidad a solicitar editable | M |
| RF-PM-06 | Agregar producto / cambiar producto a mano | M |
| RF-PM-07 | Existe una **fecha máxima** (calendario) para generar el pedido; después se bloquea la generación | M |
| RF-PM-08 | Precondiciones: movimientos de stock del día al día; estructura fija revisada; si se generó el cálculo hay que **enviar el mismo día**, y si hubo cambios de inventario posteriores, regenerar | M |
| RF-PM-09 | Anulaciones y pedidos extra **no** se consideran en el cálculo | M |
| RF-PM-10 | Al **enviar** el pedido: la planificación teórica se **bloquea** y se crea copia automática como **Planificación Real** | M |
| RF-PM-11 | El pedido llega al sistema de compras (ADS) **solo tras el cierre diario** | M |
| RF-PM-12 | Reporte "Mapa de solicitud de compras" | M |

### 5.4 Pedido extra [Cap. 5, p.22–23]
| ID | Requisito | Pri |
|---|---|---|
| RF-PE-01 | Pedido fuera del mensual, en cualquier momento; **varios pedidos extra** por período; N.º de pedido y estado (Enviado…) | M |
| RF-PE-02 | Capturar cantidad solicitada y fecha de entrega (sugerida, editable); agregar productos; grabar y enviar | M |
| RF-PE-03 | **No requiere cierre diario** para subir a la BD central (envío inmediato) | M |
| RF-PE-04 | Reporte "Mapa de solicitud de compras" | M |

### 5.5 Planificación real [Cap. 6, p.24–31]
| ID | Requisito | Pri |
|---|---|---|
| RF-PR-01 | Se crea automáticamente al enviar el pedido mensual (copia de la teórica) | M |
| RF-PR-02 | Misma grilla que la teórica, con histórico por servicio/mes | M |
| RF-PR-03 | Se **cierra a diario** por fecha calendario; tolerancia de **3 días** de atraso; más allá, esos días se **bloquean** | M |
| RF-PR-04 | El usuario puede modificar: **comensales, raciones y platos**. Los platos se respetan según planificación central, pero se permite **cambiar un plato por otro** (no cambiar ni agregar estructuras) | M |
| RF-PR-05 | Cambio de plato: selector filtrable por categoría dietética, tipo de plato y texto; solo recetas de tipo *Régimen*/*Patrón* aplicables | M |
| RF-PR-06 | Ver receta (según régimen planificado: pestaña "Receta x Régimen"), aportes nutricionales, verificar costos, frecuencia de recetas, actualizar costos | S |
| RF-PR-07 | **Exportar recetas del día** a Excel/Word | S |
| RF-PR-08 | Alerta de costo minuta día vs costo patrón techo (no debe sobrepasarlo) | M |
| RF-PR-09 | Reporte "Planificación Real" | M |

### 5.6 Requerimiento diario [Cap. 7, p.32–35]
| ID | Requisito | Pri |
|---|---|---|
| RF-RD-01 | Emitir requerimientos de cocina a almacén **solo para servicios con planificación real** | M |
| RF-RD-02 | Filtros: contrato, tipo de informe, fechas, régimen (todos/lista), servicio (todos/lista), consolidado por fecha, salto de página | M |
| RF-RD-03 | Formatos: **Requisición por servicio** (agrupado por servicio) y **Requisición por estructura de servicio detallado** (por preparación, con receta, N.º raciones, cantidad bruta). *(El manual dice "4 tipos" pero describe 2: ver P-06)* | M |
| RF-RD-04 | Columnas: cantidad (minuta real), cantidad real, cantidad extra, devolución, unidad | M |
| RF-RD-05 | Servicios **no planificados** (eventos, venta directa, retail, limpieza, lavandería, hotelería, productos de limpieza, descartables): **solicitud manual** → aprobación de Jefe de Catering/Jefe de Contrato → envío a almacén | M |
| RF-RD-06 | Flujo: recepción en contrato → requerimiento → entrega → despacho y registro de salidas | M |

### 5.7 Entradas de mercadería [Cap. 6 (2.º), p.36–49]

Reglas comunes (**RN-ENT**): se registra solo la **cantidad física recibida** (igual, mayor o menor al documento) · documento cerrado = sin marcha atrás · la fecha de recepción de mercadería se registra aparte de la emisión.

| ID | Tipo | Documento | Requisitos clave |
|---|---|---|---|
| RF-EN-01 | **Traspaso CD (entrada)** | Cesión | Seleccionar bodega destino, CD origen y la **cesión pendiente** (descargada de ADS); digitar cantidad recibida; marcar **recepción cerrada**; grabar. ADS calcula diferencias → NC (recibido < documento) o ND (recibido > documento) |
| RF-EN-02 | **Recepción proveedor** | OC + Guía de remisión | Elegir proveedor, tipo doc, N.º, fechas, bodega y **OC** (de ADS); se cargan productos/cantidades/precios de la OC; digitar cantidades recibidas y **monto total** del documento; grabar. Informar a Compras (conformidad de recepción) |
| RF-EN-03 | **FOFI / Caja chica** | Factura o boleta | Proveedor "Caja Chica"; tipo FOFI; **solo productos de consumo** (no materiales ni servicios); cantidad y precio manuales; total del documento. Consumo → inventario; gastos → registro a fin de mes |
| RF-EN-04 | **Traspaso entre contratos (entrada)** | Traspaso | Carga del traspaso emitido por la operación origen (hoy vía `.zip`); verificar cantidades; solo ingreso manual si el origen **no tiene SGP**, y limitado a productos de la zona del contrato |
| RF-EN-05 | **Traspaso entre bodegas (entrada)** | Traspaso | Visualiza la salida (réplica); confirmar y **cerrar** la entrada (candado) |

Notas de negocio de entradas CD [p.38–39]:
- **RN-ENT-01** ADS acepta o no NC/ND. Las **ND (diferencias positivas) se aceptan siempre**.
- **RN-ENT-02** Al **no aceptarse** una NC, las cantidades **reingresan al stock** del contrato y se **descuentan automáticamente** vía el servicio **Consumo Alternativo** (Kárdex: ingreso tipo **DP** devolución de producción + salida tipo **SP** salida a producción).
- **RN-ENT-03** Visibilidad de estados en el monitor de tránsitos (§5.12).

### 5.8 Salidas de mercadería [Cap. 7 (2.º), p.50–65]

Tipos: Devolución traspaso CD · Traspaso CD (salida) · Traspaso entre contratos · Traspaso entre bodegas · Salida de bodega a producción · Venta directa · Venta cafetería · Mermas.

| ID | Tipo | Requisitos clave |
|---|---|---|
| RF-SA-01 | **Devolución traspaso CD** | Referencia **obligatoria** a un documento origen (cesión recibida); cantidad **≤ recibida** y **≤ stock disponible**; se cierra automáticamente; ADS genera "Solicitud de devolución" que acepta o no (misma regla de Consumo Alternativo) |
| RF-SA-02 | **Traspaso CD (salida)** | Para enviar mercadería al CD (ej. cierre de contrato); se cierra automáticamente; ADS genera solicitud de traspaso CD salida |
| RF-SA-03 | **Traspaso entre contratos (salida)** | Elegir contrato destino y productos; cantidades; al grabar, **exportar** el traspaso al destino (nombre de archivo `SGPtras<CECO origen><aaaammdd hhmm>-<N° traspaso>.zip`); sin retroceso ni modificación posterior |
| RF-SA-04 | **Traspaso entre bodegas (salida)** | Bodega origen/destino del mismo contrato; la salida queda **PENDIENTE** y el **stock no se actualiza** hasta confirmar la entrada; **bloquea el cierre diario** mientras haya pendientes |
| RF-SA-05 | **Salida de bodega a producción** | Ver §5.8.1 |
| RF-SA-06 | **Venta directa** | Salida de stock por venta directa (documento propio) — *el manual solo lo menciona en el flujo [p.50]* (P-07) |
| RF-SA-07 | **Venta cafetería** | Ídem, se registra en el servicio correspondiente (P-07) |
| RF-SA-08 | **Mermas** | Se descargan en la salida de bodega a producción, en un servicio llamado **"Servicio 1"** [p.64] |

#### 5.8.1 Salida de bodega a producción [p.63–65]
- **RF-SA-05.1** Elegir fecha de emisión, fecha de producción (iguales), bodega, régimen y servicio; vista **Resumido** o por **Sector**; opción "ocultar ingrediente".
- **RF-SA-05.2** El sistema precarga ingredientes con **cantidad planificada** (desde planificación real) y permite digitar **cantidad realizada** (0 en no usados).
- **RF-SA-05.3** Colores: **rojo** = sin stock; **amarillo** = con stock; verde = ingrediente; sobrepasa stock actual.
- **RF-SA-05.4** Agregar producto no listado.
- **RF-SA-05.5** Mientras no se **cierre la salida**, se puede modificar y grabar, pero la mercadería **no se descarga** (estado PENDIENTE). **El cierre diario se bloquea** si hay salidas sin cerrar.
- **RF-SA-05.6** Cerrada la salida, una corrección exige **anular todo el documento y rehacerlo**; el histórico guarda los anulados (solo consulta).
- **RF-SA-05.7** La relación régimen-servicio para la salida se valida con la tabla Régimen-Servicio del contrato (cargada en la implementación).
- **RF-SA-05.8** Reporte "Resumen de salida a bodega" con firma "Entregado conforme".

### 5.9 Ingreso de raciones y ventas [Cap. 8, p.66–68]
El sistema registra raciones/ventas para costos y cierre; **la facturación oficial se hace en SGO**.

| ID | Requisito | Pri |
|---|---|---|
| RF-VE-01 | **Servicios con precio fijo**: ingresar **raciones por día** y por cliente, más "personal Sodexo" y "producidas"; los precios por cliente y régimen-servicio están configurados | M |
| RF-VE-02 | **Servicios con precio variable** (eventos, hotelería, lavandería y limpieza, gastos refacturables, venta directa, retail y otros sin precio fijo): ingresar **monto diario en soles sin IGV** por servicio/cliente, forma de pago *Contado* | M |
| RF-VE-03 | Calendario mensual con días bloqueados (por cierre) y habilitados | M |
| RF-VE-04 | **Venta directa** (8.3) y **Venta cafetería** (8.4): listadas en el índice pero sin desarrollo en el manual | S |
| RF-VE-05 | Reportes: Venta servicios, Facturación clientes, Comparativo de raciones | M |

### 5.10 Cierre diario [Cap. 9, p.69–71]
| ID | Requisito | Pri |
|---|---|---|
| RF-CD-01 | Recoge: modificaciones de planificación teórica/real, pedido mensual, ingresos y salidas de stock, inventarios y costos | M |
| RF-CD-02 | **Pre-chequeo**: Costo detalle período realizado, Food Cost, Posición stock, Movimiento stock, Consumo alternativo, Costo plan teórico–real–realizado, Comparativo de raciones | M |
| RF-CD-03 | Validaciones bloqueantes: sin traspasos entre bodegas pendientes, sin salidas a producción sin cerrar, sin inventario rotativo pendiente | M |
| RF-CD-04 | Calendario con estados: **Habilitado** (blanco) / **Cerrado y no enviado** (rojo) / **Cerrado y enviado** (verde) | M |
| RF-CD-05 | Cierre con confirmación; desde ahí **no hay modificaciones ni reaperturas** del día | M |
| RF-CD-06 | Envío a BD central: **automático nocturno** si hay conexión; cierres acumulados suben al reconectar | M |
| RF-CD-07 | Permitir cerrar días sin conexión | M |

### 5.11 Cierre de mes y resultado operacional A13 [Cap. 10, p.72–85]
Flujo de 10 pasos que el sistema debe guiar (checklist con estado por paso) **(propuesto)**:

1. **Verificación documentaria** de todos los documentos del mes (OC, cesiones, facturas/boletas caja chica, traspasos, salidas, mermas).
2. **Cierre del último día** del mes (luego no se agregan documentos).
3. **Gastos A13**: lista de conceptos (registros del sistema en rojo: depreciación, gestión personal, cuota negociación, bono vacacional, cuota dirigente sindical, N.º horas extra/trabajadas/ausencia; gastos generales: uniformes, teléfono, etc.) con valor, valor proyectado y cuenta contable; permitir agregar gastos nuevos con descripción y cuenta.
4. **Carga de inventario**: reporte "Listado para toma de inventario"; ingreso manual, o **exportar/importar plantilla Excel** (no se pueden agregar productos en el archivo; los fuera de lista se agregan en pantalla).
5. **Ajuste de inventario** vía **Consumo Alternativo**: faltante → salida a servicio; sobrante → devolución de producción al servicio "Consumo alternativo". Todo producto debe tener **precio** (si no, pedir a Soporte con login especial; lo mismo para cambiar precio). Permite **anular consumo alternativo** y rehacer el inventario. Revisar **reporte de diferencias** (físico vs sistema, valorizado).
6. **Reporte A13** (resultado operacional mensual): ventas, consumo (inventario inicial, FOFI, recepción proveedor, traspasos entre operaciones/bodegas/CD, devolución, inventario final), consumo según A13 vs según costo (diferencia), días de stock, total gastos, **utilidad operacional**, gastos generales y de personal.
7. **Envío a SAP** (obligatorio; solo si el cierre es correcto).
8. **Cierre de folios**: control de facturas compras, traspasos entre contratos y fondo fijo; **generar folio** nuevo para el mes siguiente.
9. **Calendario de cierre de mes**: período Abierto → **Cerrado**, habilitando el siguiente (Inhabilitado → Abierto).
10. **Documentar el cierre**: generar y guardar los reportes: A13, compras por caja chica, resumen OC recibidas, resumen traspasos entre operaciones (entrada/salida), traspasos CD (entrada/salida/devoluciones), traspasos entre bodegas, inventario de stock, movimiento sintético de stock, Food Cost.

| ID | Regla |
|---|---|
| RN-CM-01 | No se puede cerrar el mes con días sin cerrar o documentos pendientes |
| RN-CM-02 | Cerrar el mes es **prerrequisito** para iniciar las rutinas del mes siguiente |
| RN-CM-03 | Envío a SAP es irreversible a nivel operación; confirmar explícitamente |
| RN-CM-04 | Cambios de precio posteriores al ajuste requieren eliminar el inventario, rehacer y regrabar |

### 5.12 Control de tránsitos [Cap. 11, p.86–90]
Monitor para contratos abastecidos por un CD que muestra todas las transacciones CD ↔ operación.

| ID | Requisito | Pri |
|---|---|---|
| RF-TR-01 | Filtros: fecha de emisión (desde/hasta), tipo de operación (Traspaso CD / Devolución traspaso CD), tipo traspaso (Entrada/Salida), **status SGP** (Abiertas/Cerradas) | M |
| RF-TR-02 | Cabecera: fecha origen, proveedor (CD), N.º documento origen ADS, N.º documento SGP, fecha de emisión SGP | M |
| RF-TR-03 | Detalle por producto: cantidad documento, cantidad recibida, cantidad devuelta, diferencia, precio; **NC aceptada / NC no aceptada / ND aceptada / ND no aceptada** (cantidad y valor) y fecha de aceptación | M |
| RF-TR-04 | **Regla NC**: si Logística **acepta** → no modifica el stock del contrato (aparece en "NC aceptada"). Si **no acepta** → modifica el stock, reingresa la mercadería y se descarga como Consumo Alternativo | M |
| RF-TR-05 | **Regla ND**: solo aplica a **entradas** con diferencia positiva; siempre aceptada por Logística. No aplica en salidas ni devoluciones | M |
| RF-TR-06 | Las respuestas de ADS retornan al sistema y actualizan el monitor | M |

### 5.13 Inventario rotativo [Cap. 12, p.91]
| ID | Requisito | Pri |
|---|---|---|
| RF-IR-01 | Inventario **diario** sobre una selección **al azar** de productos, basada en el consumo del mes anterior y la **curva ABC** | M |
| RF-IR-02 | Se hace a **primera hora**; el sistema **no permite ninguna transacción** del día si no se ingresó | M |
| RF-IR-03 | Pantalla de Toma de Inventario con opción "Toma inventario rotativo": lista de productos del día; ingreso de stock físico; diferencias → Consumo Alternativo | M |

### 5.14 Toma de inventario general [p.74–80]
| ID | Requisito | Pri |
|---|---|---|
| RF-IN-01 | Tipos de listado: para toma de inventario, diferencias físico vs sistema, inventario físico valorizado, inventario sistema valorizado, diferencias valorizado; filtros por familia, solo con diferencias, incluir con stock cero | M |
| RF-IN-02 | Exportar plantilla (código, descripción, unidad, columna de cantidad) e **importar** cantidades | M |
| RF-IN-03 | Envío del inventario a SAP | M |

### 5.15 Reportes consolidados
Costo detallado/resumido (teórico) · Previsión de consumo · Mapa solicitud de compras · Planificación real · Costo detalle período realizado · Food Cost · Posición/Movimiento de stock · Consumo alternativo · Costo plan teórico–real–realizado · Comparativo de raciones · Resumen compras por período · Traspasos (contratos, CD, bodegas) · Salida de bodega a producción · Venta servicios / facturación clientes · Diferencias de inventario · A13.
**RF-REP-01**: todo reporte con vista previa, impresión, exportación PDF/Excel **(propuesto)**.

---

## 6. Reglas de negocio y cálculos clave

### 6.1 Pedido propuesto [p.19–20]
```
Pedido propuesto = (+) Necesidad según minuta teórica (NT)
                   (–) Stock actual (SA)
                   (+) Stock de seguridad (SS)
                   (–) Órdenes de compra por recibir (OC)
                   (+) Por consumir según minuta real (NR)
```
- **NT**: cantidades a utilizar en el período (planificación teórica + estructura fija).
- **SA**: stock de todos los productos al hacer el pedido.
- **SS**: stock mínimo de contingencia, actualizado por compras.
- **OC**: cantidad pedida en el pedido anterior aún por llegar.
- **NR**: consumo desde la fecha de pedido hasta la llegada del primer despacho del período.
- Redondeo a **unidad de despacho** (mínimo a pedir). *(Regla de redondeo: P-03)*

### 6.2 Costos
- Costo minuta día = Σ(N.Rac × Cto.Plat) / Comensales.
- Costo patrón techo definido por gerencia de operaciones; sirve de tope.
- Total mes = materia prima + estructura fija; costo bandeja = costo total / raciones.
- Valorización de stock por **PMP**.

### 6.3 Estados clave
| Entidad | Estados |
|---|---|
| Planificación teórica | Abierta → Cerrada (al enviar pedido mensual) |
| Planificación real | Abierta por día → bloqueada (> 3 días de atraso) |
| Documento de entrada/salida | Pendiente → Cerrado → (Anulado) |
| Traspaso entre bodegas | Pendiente (salida) → Confirmado (entrada) |
| Día | Habilitado → Cerrado no enviado → Cerrado enviado |
| Mes | Inhabilitado → Abierto → Cerrado |
| Pedido | Generado → Enviado → Recibido en ADS (tras cierre diario; extra: inmediato) |

### 6.4 Reglas transversales
- **RN-01** Un documento enviado/cerrado no se edita; se anula y rehace.
- **RN-02** Día cerrado: sin modificaciones ni reapertura.
- **RN-03** Mes cerrado: sin nuevos documentos; cierre de mes requiere cierre del último día.
- **RN-04** Productos sin precio no permiten ajuste de inventario.
- **RN-05** No se permite stock negativo en salidas; se señala en rojo y se exige corrección o ajuste.
- **RN-06** Intercambio entre contratos limitado a productos de la zona del contrato.

---

## 7. Integraciones

| Sistema | Dirección | Contenido | Frecuencia |
|---|---|---|---|
| **ADS** (compras/logística) | Sistema → ADS | Pedidos (mensual tras cierre diario; extra inmediato), recepciones, devoluciones, traspasos CD salida | Cierre diario / inmediato |
| | ADS → Sistema | Cesiones, OC, aceptación/rechazo de NC/ND/devoluciones, stock de seguridad | Bajo demanda con conexión |
| **SGO** (facturación) | Sistema → SGO | Raciones y montos de venta | Cierre diario |
| **SAP** (contabilidad) | Sistema → SAP | Inventario y cierre de mes | Una vez al cierre |
| **BD Central** | Bidireccional | Cierres diarios, maestros, recetas, planificaciones | Nocturno |
| **Correo** | — | *Se reemplaza* por traspasos electrónicos **(propuesto)** | — |

> La forma técnica de cada integración (API, archivos, colas) está **por definir** (P-02).

---

## 8. Requisitos no funcionales

| ID | Requisito |
|---|---|
| RNF-01 | **Disponibilidad offline**: todas las operaciones diarias deben funcionar sin internet |
| RNF-02 | **Sincronización** idempotente con resolución de conflictos (el contrato local es la fuente de verdad de sus movimientos) |
| RNF-03 | **Integridad**: stock y kárdex consistentes, transacciones atómicas por documento |
| RNF-04 | **Seguridad**: roles, auditoría, cifrado en tránsito y en reposo, contraseñas con hash |
| RNF-05 | **Rendimiento**: grillas de planificación (≈ 31 días × 10 estructuras) y pedidos de cientos de productos con respuesta < 2 s |
| RNF-06 | **Usabilidad**: atajos de teclado y flujo de captura rápido (usuarios de almacén), interfaz en español |
| RNF-07 | **Impresión**: reportes imprimibles con firma "Entregado conforme" y cabecera de contrato |
| RNF-08 | **Migración**: carga inicial de maestros (productos, recetas, regímenes, servicios, clientes, precios) y saldos de stock |
| RNF-09 | **Multiempresa/contrato**: aislamiento de datos por contrato |
| RNF-10 | **Trazabilidad** de cada ajuste y anulación |
| RNF-11 | **Backup** y recuperación de la BD local/central |
| RNF-12 | **Plataforma y lenguaje (confirmado, 02/10/2026):** el sistema se construye en **VB.NET** y debe funcionar en **todas las computadoras** donde se use. Esta decisión **reemplaza** el stack/PWA propuesto en versiones anteriores y la sugerencia Python/TypeScript de la *Guía de construcción modular* |
| RNF-12.1 | Alcance de "todas las computadoras" **(por precisar, P-17)**: versiones de Windows soportadas, equipos antiguos (el SGP actual corre en Windows XP, p.5), si se requiere Mac/Linux, y requisitos mínimos de hardware |
| RNF-12.2 | La versión de .NET se fija según los equipos objetivo y se bloquea en el proyecto; el instalador debe incluir o verificar el runtime necesario **(propuesto)** |
| RNF-12.3 | Instalación y actualización simples en cada PC, con número de versión visible **(propuesto)** |

---

## 9. Flujo de proceso extremo a extremo

```
Planificación teórica ─► Pedido mensual ─► (envío) ─► Planificación real
        │                                                   │
 Estructura fija (productos)                         Requerimiento diario
                                                            │
 Entradas: CD · Proveedor · FOFI · Contratos · Bodegas      ▼
        │                                         Salidas / Producción
        └───────────► Stock / Kárdex ◄────────────────────┘
                              │
                 Ventas y raciones (FMS-10)
                              │
                       Cierre diario (FMS-11) ──► BD Central ──► ADS / SGO
                              │
              Cierre de mes y A13 (FMS-12) ──► SAP
```
Procesos del manual: FMS-04 (requerimiento de compras), FMS-05 (recepción y entradas), FMS-06 (requerimiento diario), FMS-07 (despacho y salidas), FMS-09 (producción/servicio), FMS-10 (ventas y medición), FMS-11 (cierre diario), FMS-12 (cierre periódico/mensual).

---

## 10. Fases sugeridas **(propuesto)**

| Fase | Contenido | Objetivo |
|---|---|---|
| 0 | Arquitectura, modelo de datos, seguridad, maestros, sincronización base | Cimientos |
| 1 | Maestros + planificación teórica + costos + estructura fija | Planificar y costear |
| 2 | Pedido mensual/extra + planificación real + requerimiento diario | Comprar y pedir a cocina |
| 3 | Entradas/salidas + stock/kárdex + salida a producción + mermas | Operar almacén |
| 4 | Ventas/raciones + cierre diario + tránsitos | Control diario |
| 5 | Inventarios (general y rotativo) + cierre de mes + A13 + integración SAP | Cierre contable |
| 6 | Integraciones ADS/SGO, migración, endurecimiento y pilotos por contrato | Salida a producción |

---

## 11. Criterios de aceptación generales
1. Un contrato piloto completa **un mes** de operación (planificar → pedir → recibir → producir → vender → cerrar días → cerrar mes) sin recurrir al SGP.
2. El **pedido propuesto** coincide con el calculado por el SGP para el mismo conjunto de datos (prueba de paridad).
3. Los **reportes A13 y Food Cost** coinciden con los del SGP en el mes de prueba.
4. Se puede operar y cerrar días **sin conexión** y sincronizar después sin pérdida ni duplicados.
5. Ninguna operación permite modificar un día cerrado o un documento cerrado.

---

## 12. Preguntas abiertas y supuestos

| ID | Pregunta / Supuesto |
|---|---|
| P-01 | **Stack tecnológico y despliegue.** *Parcialmente resuelta:* lenguaje **VB.NET** para todas las computadoras (RNF-12). Siguen abiertos: tecnología de interfaz (WinForms/WPF), motor de base de datos (SQLite, SQL Server Express…) y nube vs on-premise |
| P-17 | **Alcance de "todas las computadoras":** ¿solo Windows (qué versiones mínimas) o también Mac/Linux? Las apps de escritorio en VB.NET (WinForms/WPF) solo corren en Windows; para otros sistemas habría que usar otra interfaz |
| P-02 | **Interfaces con ADS, SGO y SAP**: ¿existen APIs, o se mantienen archivos? Formato y contratos |
| P-03 | Regla de **redondeo** del pedido a unidad de despacho, y de **fechas de entrega** sugeridas (¿por qué calendario de proveedor?) |
| P-04 | Cálculo de **Stock de seguridad** y quién lo mantiene ("actualizado por compras") |
| P-05 | Cálculo exacto de **PMP** y valorización de Consumo Alternativo |
| P-06 | El manual indica "4 tipos de requerimiento" pero describe 2; ¿cuáles son los otros 2? |
| P-07 | **Venta directa (7.6, 8.3) y Venta cafetería (7.7, 8.4)** figuran en el índice pero no están desarrolladas |
| P-08 | **Capítulo 1 – Check list de rutinas** está vacío; solicitar las rutinas operacionales |
| P-09 | Nomenclatura exacta del archivo de traspaso (`aaaammdd` + `hhmm` + N.º); ¿se reemplaza por electrónico? |
| P-10 | ¿Se mantiene el flujo "login de Soporte" para precios o se pasa a roles con aprobación? |
| P-11 | ¿Se debe migrar la **historia** (kárdex, planificaciones) del SGP o solo saldos iniciales? |
| P-12 | Alcance real de los **reportes de gerencia** (más allá de los del manual) |
| P-13 | Volumen: n.º de contratos, usuarios concurrentes y productos/recetas |
| P-14 | Gestión de **impuestos** (IGV) en compras: el manual muestra campos Exento/Neto/IGV/Otros imp./Total |
| P-15 | El manual es de **2013 (V006)**: confirmar qué procesos cambiaron desde entonces |
| P-16 | Moneda y localización: Soles/IGV 18 % (Perú); ¿otros países? |

**Supuestos**: (a) un contrato = un CECO = una BD lógica; (b) el cálculo de costos usa el último costo conocido de ingredientes; (c) la fecha/hora del servidor central es la referencia para sincronización; (d) los idiomas/terminología se mantienen en español.

---

## 13. Trazabilidad con el manual

| Sección del requerimiento | Capítulo / páginas del manual |
|---|---|
| 5.1 Acceso y generalidades | Cómo ingresar, Íconos, Generalidades – pp.5–7 |
| 5.2 Planificación teórica | Cap. 3 – pp.8–17 |
| 5.3 Pedido mensual | Cap. 4 – pp.18–21 |
| 5.4 Pedido extra | Cap. 5 – pp.22–23 |
| 5.5 Planificación real | Cap. 6 – pp.24–31 |
| 5.6 Requerimiento diario | Cap. 7 – pp.32–35 |
| 5.7 Entradas | Cap. 6 (2.º) – pp.36–49 |
| 5.8 Salidas | Cap. 7 (2.º) – pp.50–65 |
| 5.9 Raciones y ventas | Cap. 8 – pp.66–68 |
| 5.10 Cierre diario | Cap. 9 – pp.69–71 |
| 5.11 Cierre de mes y A13 | Cap. 10 – pp.72–85 |
| 5.12 Control de tránsitos | Cap. 11 – pp.86–90 |
| 5.13 Inventario rotativo | Cap. 12 – p.91 |

> El manual tiene numeración de capítulos inconsistente (dos "Capítulo 6" y dos "Capítulo 7", y el índice no coincide con el cuerpo). Este documento sigue el contenido, no la numeración.
