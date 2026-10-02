# Alcance funcional consolidado
Estado: requisitos confirmados más propuestas expresamente identificadas. Fecha: 02/10/2026.

## Matriz principal
| ID | Regla | Estado | Módulo responsable |
|---|---|---|---|
| R01 | Empresa → operación → almacén; aislamiento de datos | Confirmado | Fundamentos |
| R02 | Producto base → variante por marca/presentación → empaque | Confirmado | Catálogo |
| R03 | Unidad base y conversiones por dimensión | Confirmado | Catálogo |
| R04 | Ingrediente vinculado al mismo producto del almacén | Confirmado | Menús |
| R05 | Recetas por rendimiento y minuta por fecha/servicio/estructura | Confirmado | Menús |
| R06 | Costos previstos conservados históricamente | Requisito técnico derivado | Menús |
| R07 | Necesidad, stock objetivo y pedido sugerido explicables | Confirmado | Compras |
| R08 | Asignación a variantes sin duplicar demanda | Requisito técnico derivado | Compras |
| R09 | Recepción física con conversión y costo | Confirmado | Almacén |
| R10 | Kárdex valorizado por variante y almacén | Confirmado | Almacén |
| R11 | Sin lotes ni vencimientos | Confirmado para versión inicial | Todos |
| R12 | Salidas/bajas en unidad base y formato único | Confirmado; diseño del formato pendiente | Almacén |
| R13 | Entregas, devoluciones, raciones, excedentes y mermas | Confirmado | Producción |
| R14 | Diferencia de inventario sin movimiento automático | Confirmado | Inventarios |
| R15 | Ajustes separados del conteo | Propuesta; flujo de aprobación pendiente | Inventarios |
| R16 | Cierres diario/mensual y costos frente al objetivo | Confirmado | Cierres |
| R17 | Food Cost con ingreso mensual registrado | Confirmado conceptualmente | Cierres |
| R18 | Auditoría y documentos compensatorios | Requisito técnico derivado | Fundamentos |
| R19 | Operación sin internet y sincronización posterior | Alcance base; topología pendiente | Continuidad |
| R20 | Contratos, accesos avanzados y resultados gerenciales | Etapa posterior | Ampliaciones |

## Responsabilidad y límites
Menús describe lo que se pretende preparar. Compras determina necesidad de abastecimiento. Almacén es el único responsable de contabilizar movimientos. Producción relaciona esos documentos con preparaciones y raciones, sin duplicar descargas. Inventarios compara conteos y solicita ajustes. Cierres congela una situación conciliada y produce reportes.

No confundir consumo con compra: recibir 100 kg aumenta inventario; no implica consumir 100 kg. No confundir entrega a cocina con venta. Una devolución utilizable a almacén reduce el consumo neto del servicio según la política acordada y restaura cantidades y valor de referencia.

## Alcance heredado que debe adaptarse
| Referencia anterior | Tratamiento comercial |
|---|---|
| ADS, SGO, SAP obligatorios | Adaptadores posteriores, nunca dependencia para el piloto |
| Servicio “Consumo Alternativo” automático | Excluido del conteo; diferencias visibles y ajuste separado |
| Login especial de Soporte para costos | Permisos y aprobación trazable; no credenciales compartidas |
| Bloqueos a tres días y calendario de pedido fijo | Parámetros de negocio pendientes, no constantes impuestas |
| Inventario rotativo obligatorio cada mañana | Modalidad configurable a validar; no bloquear pequeñas operaciones por defecto |
| Importe por ración como ingreso | Ingreso mensual por servicio como caso principal |
| Traspasos ZIP por correo | Documentos internos; sincronización posterior; no enviar correos automáticamente |
| A13 corporativo | Reporte operacional adaptable; paridad exacta exige definición y datos comparables |

## Estados y transiciones propuestas
| Entidad | Transiciones | Efecto |
|---|---|---|
| Receta | Borrador → aprobada → reemplazada por nueva versión | Aprobada no se reescribe |
| Minuta | Borrador → aprobada → ejecución real vinculada | Congelar versión y costo previsto |
| Previsión | Calculada → vigente u obsoleta → aplicada | Guarda fecha y versiones de insumos |
| Pedido | Borrador → aprobado → parcial → recibido/cancelado | No cambia stock; recepción sí |
| Documento stock | Borrador → confirmado → compensado/anulado | Movimiento único y corrección vinculada |
| Producción | Abierta → en revisión → cerrada | Concilia entregas, devolución y raciones |
| Inventario | Borrador → contado → revisado → cerrado | Conteo no mueve stock |
| Ajuste | Propuesto → autorizado → aplicado/rechazado | Propuesta de flujo independiente |
| Día/mes | Abierto → en revisión → cerrado | Cierre definitivo inmutable |

Los nombres propuestos no necesariamente existen en los CHECK actuales. Toda diferencia se resuelve con migración y pruebas; no enviar un estado nuevo a una base que lo rechaza.

## Cierre e inventario
Propuesta para eliminar la contradicción del borrador original: cerrar servicios y revisar el último día, tomar inventario a un corte controlado, revisar diferencias, aplicar ajustes autorizados mientras el día y mes permanecen abiertos, cerrar definitivamente el último día y después el mes. Correcciones descubiertas posteriormente se registran en un período abierto con referencia al original. El usuario debe validar este procedimiento antes del cierre real.

## Exclusiones actuales
Facturación electrónica, contabilidad fiscal, pasarelas, publicación automática en redes, nutrición avanzada, predicción con IA, selección EOQ automática e integraciones corporativas no forman parte del primer recorrido mínimo. Pueden añadirse tras estabilizar los seis módulos sin cambiar su trazabilidad.
