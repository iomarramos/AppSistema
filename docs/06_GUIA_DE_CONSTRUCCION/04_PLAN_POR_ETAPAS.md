# Plan de implementación y puertas de aceptación

El orden técnico incorpora fundamentos antes de los seis módulos visibles. Compras se desarrolla con consultas de stock simuladas al principio; su aceptación integrada ocurre después de almacén. No presentar esa simulación como integración terminada. Estimar esfuerzo solo después de inspeccionar el repositorio; no asignar fechas sin conocer equipo, infraestructura y datos.

## Etapa 0 — Diagnóstico y línea base
**Entrada:** paquete anterior, repositorio si existe y esta guía.
**Tareas:** identificar instrucciones locales, stack, dependencias, estado Git, código ajeno, SQL y migraciones; contar tablas; ejecutar pruebas base en memoria; documentar autoridad de datos y diferencias entre alcance y código; registrar decisiones pendientes; elaborar backlog priorizado.
**Entregables:** diagnóstico, mapa de tablas, registro de brechas, comandos de desarrollo y decisión inicial de arquitectura.
**Aceptación:** todos los hallazgos tienen evidencia y tarea; no se modifican datos reales; el entorno puede reproducir las comprobaciones. Esta guía ya aporta un diagnóstico del SQL, pero no sustituye inspeccionar el repositorio donde se construirá la aplicación.

## Etapa 1 — Fundamentos y catálogo
**Dependencia:** etapa 0 y elección técnica del proyecto.
**Tareas:** proyecto ejecutable; configuración por entorno; migraciones; pruebas automatizadas; usuarios/permisos básicos; empresas, operaciones y almacenes; unidades y dimensiones; categorías, producto base, marca, variante y empaque; proveedores y precios con vigencia; auditoría; importador con vista previa, validación y rechazo de duplicados.
**Pantallas:** acceso, selector de operación, catálogo, ficha de variante, empaques, proveedores e importación.
**Aceptación:** empresa A no consulta ni modifica registros de B; 2 cajas de 4 × 4 L equivalen a 32 L; 5 L es variante distinta; cambio de empaque no altera documento histórico; importación repetida no duplica registros; no existen credenciales de ejemplo activas fuera de demostración.
**Demostración:** crear aceite vegetal, dos marcas/presentaciones y sus empaques; mostrar la conversión y códigos únicos.

## Etapa 2 — Módulo 1: menús y recetas
**Dependencia:** catálogo estable.
**Tareas:** servicios y estructuras; recetas con rendimiento; ingredientes por producto base y unidades; variantes permitidas; versión aprobada inmutable; minuta por día, servicio y raciones; estructura fija; costeo explícito por fuente/fecha; snapshot de menú aprobado; reporte de necesidad por producto.
**Pantallas:** ficha de receta, ingredientes, calendario de minuta, costo por preparación y consolidado de necesidades.
**Aceptación:** receta para 10 raciones con 1 L, planificada para 150, requiere 15 L; restricción de variante funciona; rendimiento cero se rechaza; producto sin precio muestra costo pendiente; actualizar precio no modifica minuta aprobada.
**Demostración:** minuta de 150 raciones con sopa, fondo y bebida; consolidar productos usados por varias recetas sin duplicar.

## Etapa 3 — Módulo 2: previsión y compras
**Dependencia:** menú aprobado y contrato de consulta de disponibilidad. Stock real puede integrarse al terminar etapa 4.
**Tareas:** horizonte de consumo y fechas de entrega; reserva configurable; consumo puente sin solapamiento; pedidos pendientes netos de recepción/cancelación; asignación a variantes; redondeo por mínimo y múltiplo; excedente por empaque; previsión con snapshot; invalidación por cambios; revisión y aprobación; pedidos normales y adicionales; seguimiento parcial.
**Pantallas:** desglose de previsión, comparación de presentaciones, edición de pedido y pendientes.
**Aceptación:** demanda 50 L, reserva 10, stock 27 y tránsito elegible 8 → necesidad 25 L; caja 16 L → 2 cajas, 32 L, exceso 7 L; mínimo/múltiplo se cumplen; tránsito tardío no oculta faltante; demanda genérica no se pide completa en dos variantes.
**Demostración:** pedido redondeado, explicación del cálculo y advertencia de obsolescencia tras cambio de menú. La integración con stock real permanece pendiente hasta probar etapa 4.

## Etapa 4 — Módulo 3: almacén y kárdex
**Dependencia:** fundamentos; decisión de valoración para operación real.
**Tareas:** apertura valorizada; recepción compra/caja chica; cantidades parciales; conversiones históricas; confirmación atómica e idempotente; entradas, salidas, devoluciones, bajas y traspasos; libro inmutable; protecciones de documentos; saldo por variante; consulta consolidada; exportación con filtros; pruebas concurrentes; integración de pendientes con compras.
**Pantallas:** recepción cabecera/detalle, documentos de almacén, stock, kárdex, bajas y devoluciones.
**Aceptación:** recibir 32 L a S/8 genera S/256; salir 5 L genera S/40 y saldo 27 L/S/216; repetición de confirmación no duplica; error en segunda línea revierte toda la operación; dos salidas de 20 L con 27 disponibles permiten solo una; cabecera y detalle confirmado no se editan.
**Demostración:** compra → recepción parcial → saldo → entrega → devolución vinculada. El formato único de bajas queda rotulado como borrador hasta recibir el modelo del usuario.

## Etapa 5 — Módulo 4: producción
**Dependencia:** menús y almacén aceptados.
**Tareas:** requerimiento calculado; solicitudes manuales con permisos; asignación de variantes; entrega inicial/adicional; devolución utilizable; raciones producidas/servidas; excedentes y mermas por etapa; vincular documentos ya contabilizados; conciliación y costo real; resolver asignación de consumo compartido entre recetas.
**Pantallas:** requerimiento por servicio, entrega, registro de producción y comparación previsto/real.
**Aceptación:** entrega 5 L, adicional 1 y devolución 0,5 → neto 5,5 L; merma de 0,2 L incluida en la entrega es detalle analítico, no nueva salida; no atribuir costo real por receta sin una regla de reparto; excedentes preparados no se devuelven como ingredientes crudos.
**Demostración:** un día con 150 raciones previstas y cifras reales distintas, explicando cantidad, costo y desperdicio.

## Etapa 6 — Módulo 5: inventarios
**Dependencia:** stock y política de corte. Flujo de ajuste confirmado para aplicarlo en operación.
**Tareas:** inventario general/rotativo; snapshot de cantidades/valor; conteo por envases y parciales; importación con validación de filas; reconteo; detalle físico menos sistema; aprobación independiente y ajuste separado; historial de revisión; conciliación posterior.
**Pantallas:** crear corte, hoja de conteo, importar, diferencias, revisión y ajuste.
**Aceptación:** sistema 27 L y físico 26 → −1 L; saldo sigue 27 hasta ajuste; conteo nulo no equivale a cero; cambio concurrente tras corte no se mezcla con fotografía anterior; un mismo ajuste no se aplica dos veces.
**Demostración:** faltante explicado sin ajuste y segundo caso autorizado. Mantener documentación de ambos.

## Etapa 7 — Módulo 6: cierres y control
**Dependencia:** módulos 1 a 5, valoración definida, reglas de ajustes y cierre acordadas.
**Tareas:** lista de pendientes; reconciliación movimientos/saldos; cierre de producción e inventario; secuencia de último día y mes; snapshot de reportes; ingreso mensual básico; presupuesto y Food Cost objetivo; comparativos; correcciones en período abierto.
**Pantallas:** tablero de cierre, pendientes accionables, validaciones, reporte diario y mensual.
**Aceptación:** no cerrar con documentos pendientes o diferencia sin tratamiento; día cerrado rechaza nuevas contabilizaciones; carrera cierre/salida resuelta transaccionalmente; Food Cost no se calcula con ingreso cero; repetir reporte cerrado produce los mismos resultados.
**Demostración:** cierre de mes de ejemplo con costo, diferencia, merma, ingreso y Food Cost; las cifras se rastrean al documento.

## Etapa 8 — Continuidad, sincronización y piloto
**Dependencia:** recorrido local completo estable.
**Tareas:** servidor local operativo; outbox e inbox idempotentes; identificadores globales; reintentos; orden por origen; dependencia entre eventos; estado pendiente/error/conflicto; versionado de payload; credenciales por sede; maestro central vs movimientos locales; respaldos; restauración; instalación y actualización; métricas y soporte; piloto de un mes.
**Aceptación:** operar sin internet, recuperar conexión y repetir entrega de eventos sin duplicados; corte de energía no deja media transacción; restauración concilia saldos; reportes centrales muestran fecha de última sincronización; ninguna sede sobrescribe a otra.
**Demostración:** caída simulada, operaciones locales, reintento y recuperación, seguida de informe de conciliación. Sin esta prueba no ofrecer operación offline completa.

## Etapa 9 — Ampliación comercial
Contratos, clientes, vigencia y ajustes; accesos avanzados; resultados con gastos de personal/operación; multi-sede; integraciones específicas. Cada integración exige contrato de datos, entorno de prueba, reconciliación y tratamiento de errores. Nutrición y pronóstico estadístico se tratan como proyectos posteriores. No asumir que SAP/ADS/SGO están disponibles para cualquier cliente.

## Puerta común de salida
Código revisado, pruebas relevantes pasando, fallos bloqueantes resueltos, migración ensayada, demostración reproducible, documentación y decisiones actualizadas. Si falta una condición, marcar “parcial” y señalar la tarea pendiente; no renombrar una simulación como terminado.
