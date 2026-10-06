# Seguimiento inicial y decisiones
Fecha: 02/10/2026. Este archivo no afirma que exista aplicación implementada.

## Estado al entregar este paquete
| Trabajo | Estado | Evidencia |
|---|---|---|
| Alcance y prompts | Preparados | Documentos 01 a 10 |
| Esquema anterior de 59 tablas | Existente, revisado parcialmente | SQL y diccionario extraído |
| Nueve controles base | Reejecutados correctamente | Resultado_Auditoria.json |
| Protección de cabeceras/detalles/saldo | Brechas detectadas | Tres diagnósticos en memoria |
| Aplicación/API/interfaz | No implementadas por esta entrega | Pendiente |
| Motor pedidos/valoración real | Pendiente | Decisiones y desarrollo |
| Sincronización/integraciones | Pendiente | Diseño no equivale a ejecución |
| Piloto mensual | No ejecutado | Pendiente |

## Decisiones de negocio
| ID | Tema | Propuesta de trabajo | Estado/bloqueo |
|---|---|---|---|
| D01 | Valoración | Promedio móvil por variante y almacén | No aprobado; bloquea salidas valorizadas reales |
| D02 | Precio de ingrediente genérico | Fuente explícita, fecha y variante de referencia | Pendiente; simulación con costo etiquetado |
| D03 | Impuestos, descuentos y cargos | Separarlos del valor de adquisición calculado | Pendiente por cliente/país; no asumir tratamiento fiscal |
| D04 | Redondeo compra | Hacia arriba al mínimo/múltiplo, excedente visible | Propuesto; confirmar antes de pedidos reales |
| D05 | Formato único de bajas | Diseñar campos básicos y mapear modelo del usuario | Modelo pendiente |
| D06 | Ajustes de inventario | Conteo → revisión → autorización → documento | Propuesto; no activar ajuste automático |
| D07 | Corte y cierre final | Ajustes antes del cierre definitivo del último día | Propuesto; validar con negocio |
| D08 | Reserva | Parámetro por producto/sede | Pendiente; no imponer 15 días a todos |
| D09 | Sustituciones | Solo variantes compatibles y permitidas | Confirmar restricciones de cada receta |
| D10 | Offline | Autoridad local compartida por sede | Propuesta; validar hardware/red |
| D11 | Excesos de recepción | Tolerancia configurable con motivo | Pendiente; por defecto advertir/bloquear confirmación excedida |
| D12 | Stock crudo en cocina | Definir si entrega equivale a consumo o transferencia | Pendiente antes de producción real |
| D13 | Ingreso para Food Cost | Importe mensual neto del servicio | Confirmado conceptualmente; política diaria pendiente |
| D14 | Costo real por receta | Trazabilidad directa o reparto explícito | Pendiente; servicio sí se puede conciliar |

## Backlog inicial ordenado
| ID | Acción | Dependencia | Aceptación |
|---|---|---|---|
| B001 | Inspeccionar repositorio y fijar stack | Acceso al código | Decisión documentada |
| B002 | Crear línea base migratoria | B001 | Instala y actualiza copia |
| B003 | Permisos/aislamiento | B001 | T01 y T02 |
| B004 | Catálogo y conversiones | B002/B003 | T03–T06 |
| B005 | Proteger documentos confirmados | B002 | T25 |
| B006 | Restringir escritura de saldos | B002 | No edición ordinaria; conciliación |
| B007 | Signo/tipo y confirmación atómica | B005/B006 | T19–T26 |
| B008 | Recetas/minuta versionadas | B004 | T07–T11 |
| B009 | Previsión y empaques | B008 | T12–T18 |
| B010 | Recepción y kárdex integrados | B007/D01/D03 | T19–T30 |
| B011 | Producción | B008/B010/D12 | T31/T32 |
| B012 | Conteo y ajuste | B010/D06 | T33–T36 |
| B013 | Cierres y reportes | B011/B012/D07 | T37–T41/T48 |
| B014 | Offline, restauración y piloto | B013/D10 | T42–T46 |

## Plantilla de tarea
ID; módulo; objetivo; contexto; reglas; dependencias; archivos previstos; migración; aceptación; casos de prueba; responsable; estado; evidencia; siguiente paso. Estados: pendiente, en curso, bloqueada, en revisión, terminada. “Bloqueada” debe mencionar decisión o dependencia concreta.

## Plantilla de decisión
ID; pregunta; opciones; ejemplo numérico; propuesta; impacto; quién confirma; fecha; estado; archivos y pruebas que cambian. Registrar reversibilidad y modo demostración cuando corresponda. No marcar “confirmado” por silencio del usuario.

## Plantilla de continuidad
Última tarea completada; commit/versión si existe; archivos modificados; pruebas ejecutadas; fallos abiertos; decisiones pendientes; siguiente comando/tarea; datos que no deben tocarse. Al retomar leer este registro y el estado real del repositorio, evitando recrear el proyecto.

## Primer bloque que puede programarse
Fundamentos y catálogo: empresa, operación, almacén, unidades, producto base, marca, variante y empaque. Puede avanzar sin definir todavía PMP. Su demostración obligatoria es 2 cajas de cuatro envases de 4 L = 32 L, con otra presentación de 5 L diferenciada. Después se conectan esos mismos productos a recetas.
