# Prompts de implementación por formulario

Estos prompts se pueden entregar a Codex, Claude Code u otro agente de programación. Cada prompt supone que el agente debe inspeccionar el repositorio antes de modificarlo.

---

# Prompt 1 - FormPrincipal

> **Estado al 2026-10-06:** Designer en `FormPrincipal.Designer.vb`; menú con permiso por opción (`ConstruirMenu`); `Ctrl+K` para ir a pantalla; barra de estado con empresa, operación, usuario, versión, último cierre y estado de la central. **Pendiente:** agrupar el menú en las áreas de este prompt (hoy: Catálogo, Menús, Compras, Almacén, Cierres y control, Administración); la prueba de 1366x768 mide la ventana maximizada y debe medir el diseño (`CLAUDE.md`, §5).

Actúa como arquitecto senior de WinForms .NET 8 y VB.NET. Trabaja sobre `iomarramos/AppSistema`.

Objetivo: rediseñar `FormPrincipal` como shell operativo de AppSistema, sin romper permisos, pruebas E2E ni comportamiento MDI existente.

Requisitos:

1. Conserva autenticación, `SesionUsuario`, `Configuracion` y evaluación de permisos.
2. Organiza la navegación en:
   - Catálogo
   - Planificación
   - Producción
   - Abastecimiento
   - Almacén
   - Inventario
   - Cierres y Food Cost
   - Resultados
   - Administración
3. Mostrar empresa, operación, usuario, rol efectivo, estado de sincronización y versión.
4. Mantener `Ctrl+K` para ir a pantalla.
5. No crear controles permanentes fuera de `FormPrincipal.Designer.vb`.
6. No hacer SQL desde la UI.
7. Todas las opciones deben ocultarse según permiso.
8. Debe ser usable a 1366x768.
9. Agrega pruebas E2E:
   - superusuario ve todo;
   - Chef solo ve sus módulos;
   - almacenero no ve Administración;
   - opción sin permiso no existe visualmente.
10. Compilación con 0 warnings.

Entrega:
- diseño;
- código;
- pruebas;
- breve documento de cambios.

---

# Prompt 2 - FormRecetas

> **Estado al 2026-10-06:** Designer; versiones (borrador, aprobada, retirada); una versión aprobada no se edita (se crea otra); ingredientes con variantes permitidas; costo simulado. **Pendiente:** pestañas de método, nutrición e historial; los nutrientes esperan la tabla de composición (decisión del usuario: diferido).

Implementa/moderniza `FormRecetas`.

Debe soportar receta versionada, ingredientes, método, nutrición e historial.

No permitir editar una versión aprobada. Para cambiar una aprobada se crea nueva versión.

Cabecera:
Código, nombre, régimen, tipo plato, categoría dietética, versión, estado, raciones base, rendimiento.

Grilla:
Código ingrediente, ingrediente, variante activa, cantidad, UM, merma, neto, precio, costo.

Tabs:
Ingredientes, Método, Nutrición, Versiones, Uso en menú.

Acciones:
Nueva, Nueva versión, Guardar, Aprobar, Retirar, Clonar, Ver costo.

Permisos:
MENUS_VER, RECETAS_EDITAR, RECETAS_APROBAR.

Pruebas:
- aprobar bloquea edición;
- usuario solo lectura no edita;
- ingrediente inválido se rechaza;
- costo recalculado por servicio de dominio.

---

# Prompt 3 - FormServicios

> **Estado al 2026-10-06:** estructuras y factores de consumo; el factor teórico se cambia aquí y el de la operación en Minutas. **Pendiente:** validar alternativas al 100 % y versionar los cambios de factor futuros.

Diseña `FormServicios` para administrar Regímenes, Servicios y Estructuras versionadas.

Debe permitir componentes, orden, factor, alternativas y distribución.

Validar:
- factores no negativos;
- distribución de alternativas consistente;
- estructura usada históricamente no se borra;
- cambios futuros crean versión.

Permiso principal: MENUS_CONFIGURAR.

---

# Prompt 4 - FormPlanificacionMenus

> **Estado al 2026-10-06:** matriz servicio × día con comensales y costo por comensal (`FormPlanificacionMenus`) y resumen por día. La planilla en formato SGP (estructura × día) está en `FormPlanillaMenu` (entrega 11). **Pendiente:** costo piso y techo en pantalla; planificación central por fases (`docs/04_ARQUITECTURA_Y_DATOS/ARQUITECTURA_PLANIFICACION_CENTRAL.md`).

Moderniza `FormPlanificacionMenus` tomando como referencia funcional la matriz mensual SGP.

Mantén:
- servicio/régimen;
- mes;
- matriz por días;
- receta;
- factor;
- raciones;
- costo unitario;
- costo total;
- comensales;
- resumen mensual;
- resumen del día;
- acumulado.

Agregar:
- costo patrón/techo;
- desviación;
- versión plan;
- estado;
- frecuencia receta;
- búsqueda/sustitución;
- copia día/semana;
- comparación con plan operativo;
- vista previa de previsión de consumo.

Reglas:
- solo editar jornadas en borrador;
- plan liberado inmutable;
- toda liberación crea snapshot;
- cambios posteriores requieren nueva versión;
- no calcular lógica financiera dentro del Form.

Pruebas:
- edición permitida/bloqueada;
- aprobación;
- persistencia;
- rendimiento en 31 días;
- navegación por teclado.

---

# Prompt 5 - FormProduccionChef

> **Estado al 2026-10-06:** plan operativo con raciones teóricas y operativas, diferencia, costo previsto y producción registrada; sustituir plato y ajustar raciones operativas (V030). **Pendiente:** diferencia teórica contra operativa por componente en pantalla.

Rediseña `FormProduccionChef` como “Plan operativo del Chef”.

Mostrar:
fecha, servicio, estructura, receta, raciones teóricas, raciones operativas, diferencia, costo.

Permitir:
- cambiar comensales;
- cambiar raciones;
- sustituir receta autorizada;
- actualizar factor si tiene FACTORES_EDITAR;
- recalcular requerimiento.

Si ya hubo despacho:
- no modificar el despacho existente;
- crear requerimiento adicional vinculado;
- requerir motivo;
- aplicar aprobación según política.

Pruebas de día cerrado, usuario sin permiso y despacho previo.

---

# Prompt 6 - FormRequerimientoProduccion (nuevo o integrado)

> **Estado al 2026-10-06:** integrado en `FormProduccion`. Estados existentes: borrador, aprobado (solo adicional), atendido y anulado. Adicional con motivo obligatorio y aprobación por `ADICIONAL_APROBAR` (V027, V029). **Pendiente:** estados ENVIADO, PREPARADO y CERRADO.

Crea una pantalla para requerimiento Cocina → Almacén.

Tipos:
- planificado;
- adicional;
- manual autorizado.

Estados:
BORRADOR → ENVIADO → APROBADO → PREPARADO → DESPACHADO → CERRADO.

Detalle:
producto, UM, necesidad, reservado, despachado previo, solicitud, disponible, diferencia.

No descontar stock al crear requerimiento. Descontar solo al confirmar despacho.

Agregar historial y trazabilidad.

---

# Prompt 7 - FormCompras

> **Estado al 2026-10-06:** previsión con desglose, validación y obsolescencia; pedidos (generados o manuales), aprobación y anulación; el almacenero no prepara ni aprueba compras. **Pendiente:** mostrar cada componente de la fórmula por producto, estados CALCULADO y PARCIAL, y el snapshot del cálculo.

Rediseña `FormCompras` para previsión y pedido.

Fórmula explicable:
Necesidad - Stock + Seguridad - OC por recibir + Consumo antes de llegada, ajustado por reservas, múltiplos y unidades mínimas.

Mostrar cada componente de la fórmula por producto.

No permitir ajuste manual sin motivo.

Estados:
CALCULADO, BORRADOR, APROBADO, ENVIADO, PARCIAL, RECIBIDO, ANULADO.

Separar pedido mensual y extra mediante tipo, no duplicar motor de cálculo.

---

# Prompt 8 - FormConsolidado

> **Estado al 2026-10-06:** consolidado de compras de todas las operaciones con su impresión. **Pendiente:** drill-down por origen y distribución por operación en pantalla.

Moderniza `FormConsolidado`.

Consolidar por producto todas las operaciones.

Permitir drill-down por operación y origen de necesidad.

Seleccionar presentación/proveedor y distribuir compra.

Conservar cantidades originales por operación; nunca perder trazabilidad por el consolidado.

---

# Prompt 9 - FormRecepciones

> **Estado al 2026-10-06:** no existe como pantalla propia: la recepción de pedidos está en `FormStock` ("Recibir pedido"). **Pendiente:** lote, vencimiento y recepción parcial en pantalla; decidir si se separa en una pantalla propia.

Crea `FormRecepciones`.

Tipos:
Proveedor, CD, FOFI, operación, almacén.

Cabecera:
documento, origen, proveedor, fecha, almacén.

Detalle:
producto, solicitado, recibido, diferencia, lote, vencimiento, precio, total.

Confirmar genera movimientos stock inmutables.

Cantidad recibida = físico real, no cantidad del documento.

Diferencias deben quedar registradas.

---

# Prompt 10 - FormStock

> **Estado al 2026-10-06:** saldos, kárdex valorizado, inventario inicial, traspaso en dos pasos ("Traspaso..." y "Recibir traspaso...", V033), bajas y devoluciones. **Pendiente:** pestañas de lotes, reservas y tránsitos; filtros de stock cero y de próximos a vencer.

Moderniza `FormStock` como consulta, no editor de saldo.

Tabs:
Posición, Kardex, Lotes, Reservas, Tránsitos, Pendientes.

Agregar búsquedas y alertas de vencimiento/stock crítico.

Nunca permitir UPDATE manual de saldo.

---

# Prompt 11 - FormDespachoProduccion

> **Estado al 2026-10-06:** la entrega del almacén a producción se hace desde `FormProduccion` ("Entregar (almacen)"), por requerimiento (día y servicio). La devolución se pide con solicitud y la atiende el almacén (V028). **Pendiente:** pantalla de despacho propia si el área lo pide.

Crea pantalla de preparación y despacho.

Tomar requerimiento aprobado.

Sugerir FEFO por lote.

Mostrar solicitado, disponible, preparado, despachado, pendiente.

Confirmación:
- transacción;
- valida stock;
- genera kardex;
- documento inmutable.

---

# Prompt 12 - FormInventarios

> **Estado al 2026-10-06:** conteo separado de la aprobación; hoja de conteo ciega; ajustes con motivo normalizado y explicación (V031); clasificación ABC. **Pendiente:** flujo completo en pantalla (abrir, contar, revisar, aprobar, ajustar, cerrar).

Moderniza `FormInventarios`.

Tipos:
inicial, rotativo, total, selectivo.

Separar:
- apertura;
- conteo;
- revisión;
- aprobación;
- ajuste.

Primer conteo puede ser ciego.

No ajustar stock al guardar conteo.
Ajustar solo tras aprobación.

Generar:
- listado toma;
- físico valorizado;
- diferencias;
- boleta ajuste.

---

# Prompt 13 - FormCierres

> **Estado al 2026-10-06:** tablero de pendientes por día con "Ir a"; calendario por días y vista por semanas (botón "Calendario por semanas..." en el asistente, entrega 6); cierre diario y mensual; Food Cost. **Pendiente:** revisar la especificación 04 frente a lo hecho.

Rediseña `FormCierres` como checklist operacional.

Calendario con estados.

Antes de cerrar verificar:
recepciones, salidas, devoluciones, traspasos, raciones, ventas, inventario, conciliación y pendientes.

Cerrar día debe ser transaccional, idempotente y bloquear fecha.

No permitir reapertura simple.

---

# Prompt 14 - FormResultados

> **Estado al 2026-10-06:** gastos del mes (reales y presupuestados por rubro); resultado mensual por servicio; comparado para imprimir o exportar (entrega 5); "Presupuesto, mes anterior y acumulado..." (entrega 6); exportación CSV para contabilidad. **Pendiente:** comparación contra el plan.

Moderniza `FormResultados` para A13 / resultado mensual.

Bloques:
Ingresos, alimento, otros costos, gastos, margen.

Comparar presupuesto/plan/real.

Mostrar Food Cost y variaciones.

Mes cerrado = solo lectura.

---

# Prompt 15 - FormReportes

> **Estado al 2026-10-06:** catálogo por categorías con impresión, Excel, PDF y CSV desde un motor común. **Pendiente:** los reportes que faltan en `docs/02_REQUERIMIENTOS/DIAGNOSTICO_VENTANA.md` (costo detallado y resumido teórico ya existen; faltan los demás de la lista).

Rediseña `FormReportes` como catálogo de reportes por categorías.

No usar ComboBox plano como única navegación.

Categorías:
Planificación, Compras, Producción, Almacén, Inventario, Costos/Cierre.

Componente de filtros dinámicos por reporte.

Vista previa común.

Exportar:
PDF, Excel, CSV según reporte.

Agregar al menos los 27 reportes especificados en `04_CIERRES_RESULTADOS_REPORTES.md`.

---

# Prompt 16 - FormUsuarios

> **Estado al 2026-10-06:** crear usuario, asignar y quitar roles, roles propios, reiniciar clave, editar nombre y estado (reactivar), detalle del usuario elegido, alcance por operación. **Pendiente:** ninguno propio; la matriz de acceso tiene su prompt (17).

Moderniza `FormUsuarios`.

Lista + detalle.

Administrar:
usuario, roles, operaciones, vigencia, bloqueo.

No exponer hash.

Auditar toda modificación.

---

# Prompt 17 - FormMatrizAcceso

> **Estado al 2026-10-06:** existe; excepciones por permiso y operación con motivo. **Pendiente:** revisar con el usuario la matriz de roles frente a la tabla de la sección de roles de este documento.

Visualizar permisos en cuatro niveles:

Módulo → Pantalla → Acción → Alcance.

Mostrar:
rol, excepción usuario, resultado efectivo.

Agregar perfiles base:
SUPERUSUARIO, PLANIFICADOR_CENTRAL, COMPRAS_CENTRAL, JEFE_OPERACION, CHEF_OPERATIVO, JEFE_ALMACEN, ALMACENERO.

---

# Prompt 18 - FormAuditoria

> **Estado al 2026-10-06:** consulta de auditoría con filtros, exportable; `AccessibleName` agregado (entrega 9). **Pendiente:** ninguno propio.

Crear experiencia de auditoría de solo lectura.

Filtros por fecha, usuario, entidad, documento, acción.

Permitir comparar Antes/Después.

Exportar.

Nunca permitir modificar auditoría.

---

# Prompt 19 - FormContinuidad

> **Estado al 2026-10-06:** sincronización y respaldo para TI con la conexión del propietario (no se guarda); estado de la cola; conciliación. **Pendiente:** piloto en una sede; el diálogo de conexión es modal y no se puede automatizar en la prueba de pantallas.

Mostrar:
estado sede, cola, último sync, conflictos, último backup.

Acciones:
sincronizar, respaldar, restaurar, conciliar.

Operaciones críticas requieren confirmación y permiso.

No imprimir secretos.

---

# Prompt 20 - FormCargaReal

> **Estado al 2026-10-06:** carga de datos reales por pasos, con resultado y pasos repetibles. **Pendiente:** cargar el menú del SGP que el usuario entregue (ver `CLAUDE.md`, §7).

Convertir la carga de datos en wizard con nueve pasos oficiales.

Mostrar por paso:
archivo, filas, insertadas, actualizadas, rechazadas, log.

Cada carga debe ser repetible sin duplicar.

# Prompt 21 - FormPlanillaMenu (nuevo, 2026-10-06)

Actúa como desarrollador WinForms .NET 8 y VB.NET senior. Trabaja sobre `iomarramos/AppSistema`.

Objetivo: una planilla del menú en el formato del SGP: estructuras en filas, días en columnas, y en cada celda el plato del día con su receta, sus raciones y su porcentaje sobre los comensales.

Requisitos:

1. El Designer tiene los controles fijos (barra de servicio y rango, grilla, estado). Los permisos van en el código (`MinutasEditar`).
2. Cada celda: receta (código y raciones) y % sobre comensales. Filas finales: comensales del día y costo de la minuta del día (o "sin costo").
3. Doble clic o "Cambiar receta o raciones...": cambia la receta (`SustituirReceta`), las raciones (`FijarRaciones`) o crea el plato si la celda está vacía (`AgregarPlato`). Si el día no tiene minuta, la crea con los comensales que indique el usuario (`CrearMinuta`).
4. "Comensales del dia...": cambia los comensales del día (`ActualizarComensales`).
5. Solo en minutas en borrador. Una minuta aprobada se rechaza en el servicio (`MINUTA_APROBADA`) y la pantalla lo dice.
6. Sin SQL en la pantalla. Reglas en `ServicioMinutas`.
7. Estados y mensajes: sin minuta, sin recetas aprobadas, minuta aprobada.
8. Pruebas: servicio (`Planilla_cambia_raciones_solo_en_minuta_en_borrador`); E2E: la opción aparece en el recorrido de pantallas y abre sin error.

Pendiente: cargar la planilla del menú del SGP que entregue el usuario (archivo original, no texto copiado).
