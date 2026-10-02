# Plan de pruebas y aceptación

## Alcance de la evidencia actual
Las nueve pruebas repetidas del SQL figuran en `verificacion/Resultado_Auditoria.json`. Los casos siguientes son especificaciones por implementar; no se declaran ejecutados. Probar fórmulas con valores esperados independientes, no comparando la función contra una copia de sí misma.

## Matriz de casos
| ID | Requisito | Preparación/acción | Resultado esperado |
|---|---|---|---|
| T01 | R01 | Usuario A consulta ID de empresa B | Sin datos; rechazo seguro |
| T02 | R01 | Exportar datos o buscar por texto de empresa B | Ninguna filtración |
| T03 | R02 | Crear código de variante repetido en empresa | Rechazo; otra empresa puede usar su código |
| T04 | R03 | Convertir 2 cajas 4×4 L | 32 L exactos |
| T05 | R03 | Convertir kg a L sin regla | Rechazo |
| T06 | R02 | Cambiar contenido de empaque tras recepción | Documento histórico conserva 32 L |
| T07 | R04 | Añadir variante de otro producto a receta | Rechazo |
| T08 | R05 | Rendimiento 10, ingrediente 1 L, 150 raciones | 15 L |
| T09 | R05 | Rendimiento cero o raciones negativas | Rechazo |
| T10 | R06 | Modificar precio después de aprobar minuta | Costo previsto aprobado permanece |
| T11 | R06 | Ingrediente sin costo de referencia | Costo incompleto explícito |
| T12 | R07 | 50 demanda+10 reserva−27 stock−8 tránsito | 25 L netos |
| T13 | R07 | Necesidad 25 L, caja 16, múltiplo 1 | 2 cajas/32 L/exceso 7 L |
| T14 | R07 | Igual, múltiplo 3 | 3 cajas/48 L/exceso 23 L |
| T15 | R07 | Necesidad cero con mínimo 3 | Pedido cero |
| T16 | R07 | Tránsito llega después de fecha de falta | Alerta; no cubre demanda anterior |
| T17 | R08 | Dos variantes para una necesidad de 25 L | Asignación única; excedente explicado |
| T18 | R07 | Cambiar stock o minuta tras calcular | Previsión obsoleta detectada |
| T19 | R09 | Confirmar recepción 32 L, S/256 | Saldo 32 L/S/256 |
| T20 | R10 | Salida 5 L a S/8 | Saldo 27 L/S/216 |
| T21 | R10 | Reintentar misma confirmación | Un documento/un movimiento por línea |
| T22 | R10 | Misma clave idempotente, distinto payload | Conflicto, sin movimiento |
| T23 | R10 | Segunda línea de recepción inválida | Rollback de documento, movimientos y saldo |
| T24 | R10 | Dos conexiones sacan 20 L cada una de 27 | Una confirma, otra rechaza; saldo 7 L |
| T25 | R18 | Editar cabecera/detalle confirmado | Rechazo en API y persistencia controlada |
| T26 | R10 | Signo positivo para salida a producción | Rechazo |
| T27 | R09 | Pedido 32 L, recibe 16 y luego 16 | Pendiente 16 y luego cero; sin duplicado |
| T28 | R09 | Recibir más de saldo de pedido | Aplicar tolerancia definida o rechazar |
| T29 | R10 | Devolver más que salida neta histórica | Rechazo |
| T30 | R10 | Agotar stock con costo periódico decimal | Cantidad y valor finales cero según política |
| T31 | R13 | Entrega 5+adicional 1−devolución 0,5 | Neto 5,5 L |
| T32 | R13 | Merma 0,2 ya incluida | Sin segunda baja |
| T33 | R14 | Conteo 26 vs sistema 27 | Diferencia −1, saldo sin cambio |
| T34 | R14 | Celda vacía en conteo | Pendiente, no cero |
| T35 | R15 | Doble aplicación de ajuste | Una sola contabilización |
| T36 | R14 | Movimiento durante conteo congelado | Bloqueado o reconciliado según política |
| T37 | R16 | Cerrar con documentos pendientes | Lista de pendientes; no cierra |
| T38 | R16 | Salida simultánea con cierre | Serialización: incluida antes o rechazada después |
| T39 | R16 | Fecha/período ya cerrado | No nueva contabilización |
| T40 | R17 | 4200 costo/10000 ingreso, objetivo 40 % | 42 %, +2 pp, +S/200 |
| T41 | R17 | Ingreso cero | No calculable |
| T42 | R19 | Reenviar evento tras pérdida del acuse | Sin duplicados |
| T43 | R19 | Evento hijo llega antes que documento padre | Se retiene; no se pierde ni aplica mal |
| T44 | R19 | Evento de sede no autorizada | Rechazo y registro seguro |
| T45 | R18 | Restaurar copia de prueba | Mismos recuentos, saldos y referencias |
| T46 | R18 | Migrar base con documentos históricos | Historia y conciliación conservadas |
| T47 | R09 | Importación repetida y archivo con filas inválidas | Sin duplicados; reporte por fila |
| T48 | R16 | Repetir reporte de período cerrado | Resultado idéntico con mismos filtros |

## Niveles de prueba
- Unitarias: conversiones, redondeo, costo receta, previsión, valoración y diferencias. Incluir ceros, máximos, fracciones y dimensiones incompatibles.
- Integración: transacciones, triggers, claves, permisos, migraciones, outbox y consultas. Usar el motor real seleccionado; SQLite en memoria no valida bloqueos de PostgreSQL.
- Contrato: API, esquema decimal, errores e idempotencia.
- Concurrencia: conexiones/procesos independientes y barreras para forzar solapamiento; repetir de forma controlada. No basta una secuencia de llamadas en una sola conexión.
- Extremo a extremo: menú → pedido → recepción → entrega → producción → conteo → cierre.
- Recuperación: corte durante escritura, reintento tras commit, restauración y sincronización repetida.

## Datos de prueba
Empresa A y empresa B; operación Orcopampa ficticia; almacén principal; aceite A 4 L/caja 4, aceite B 5 L; arroz kg; usuario almacén, cocina y supervisor. Mantener fixtures separados por escenario para que un cierre no invalide el siguiente caso. Cada caso declara saldo inicial, acciones, estado final y efecto monetario. No incluir datos personales ni credenciales reales.

## Puerta de publicación del piloto
Todas las pruebas críticas de stock, autorización, cierre y restauración deben pasar. No publicar con pérdida de datos, mezcla de empresas, doble movimiento o saldos sin explicación. Para rendimiento proponer una carga inicial medible (por ejemplo, 500 variantes y 5 usuarios) y registrar equipo, volumen de movimientos y percentiles de respuesta. El umbral y la carga se validan con el cliente; no afirmar “menos de dos segundos” sin medir.

## Piloto de un mes
Registrar apertura aprobada; completar al menos un recorrido diario de cada servicio; incluir compra parcial, cambio de receta, devolución, baja, diferencia física y corrección; realizar cierre mensual; conciliar cantidades y valores con documentos; probar un respaldo restaurado. Registrar incidencias y aceptación del responsable. El mes puede simularse primero; solo se declara piloto operativo tras evidencia real de uso autorizado.

## Ficha de evidencia
Caso, versión de código, migración, entorno, fecha, comando, entrada, resultado esperado, resultado observado, log/captura y estado. Si no se ejecutó, indicar “No ejecutado” y motivo. Una captura de pantalla sola no prueba integridad de la base.
