# Prompts de trabajo por etapa

Usar después del prompt maestro. Leer siempre dependencias y aceptación del documento 04. Cada encargo autoriza trabajo de desarrollo, no publicación ni uso de datos reales.

## Etapa 0 — Inspección y diagnóstico

```text
Actúa con el prompt maestro y esta guía.
Inspecciona el repositorio y las instrucciones locales. Reproduce la verificación adjunta en un entorno aislado. Identifica stack, migraciones, tests y cambios ajenos. Registra las tres brechas del SQL y verifica si el código actual ya las corrige. Entrega diagnóstico y backlog, conserva originales y empieza los fundamentos independientes.
Criterio mínimo de salida: Diagnóstico con evidencia, decisión técnica y primera tarea ejecutable.
Comunica avances y hallazgos; corrige fallos antes de declarar terminado.
Entrega archivos modificados, migraciones, pruebas ejecutadas, demostración y pendientes.
Actualiza el seguimiento y deja la siguiente tarea concreta.
```

## Etapa 1 — Fundamentos y catálogo

```text
Actúa con el prompt maestro y esta guía.
Implementa permisos básicos y aislamiento por empresa, operaciones, almacenes, unidades, producto base, marca, variante y empaque. Añade migraciones, API y pantallas de mantenimiento e importación validada. Conserva factores históricos. Usa fixture aceite A 4 L/caja de 4 y aceite B 5 L.
Criterio mínimo de salida: Pruebas T01–T06; demostración 2 cajas = 32 L; otra empresa no puede acceder.
Comunica avances y hallazgos; corrige fallos antes de declarar terminado.
Entrega archivos modificados, migraciones, pruebas ejecutadas, demostración y pendientes.
Actualiza el seguimiento y deja la siguiente tarea concreta.
```

## Etapa 2 — Menús y recetas

```text
Actúa con el prompt maestro y esta guía.
Implementa estructuras de servicio, recetas por rendimiento, ingredientes del catálogo, variantes permitidas, versiones y minuta. Costea con fuente explícita y conserva snapshot al aprobar. Marca precios faltantes. No descontar stock.
Criterio mínimo de salida: T07–T11; receta de 10 raciones con 1 L escalada a 150 exige 15 L.
Comunica avances y hallazgos; corrige fallos antes de declarar terminado.
Entrega archivos modificados, migraciones, pruebas ejecutadas, demostración y pendientes.
Actualiza el seguimiento y deja la siguiente tarea concreta.
```

## Etapa 3 — Previsión y compras

```text
Actúa con el prompt maestro y esta guía.
Implementa necesidad por fechas, reserva, stock/tránsito elegible, consumo puente sin solapamiento, asignación a variantes y redondeo. Separa stock objetivo de cantidad a comprar. Guarda fuentes de la previsión y detecta obsolescencia. Implementa pedido editable/aprobado y recepciones pendientes. Si almacén no existe, usa interfaz simulada identificada y deja aceptación integrada pendiente.
Criterio mínimo de salida: T12–T18; caso 25 L netos → 2 cajas de 16 L; exceso 7 L.
Comunica avances y hallazgos; corrige fallos antes de declarar terminado.
Entrega archivos modificados, migraciones, pruebas ejecutadas, demostración y pendientes.
Actualiza el seguimiento y deja la siguiente tarea concreta.
```

## Etapa 4 — Almacén y kárdex

```text
Actúa con el prompt maestro y esta guía.
Corrige protección de confirmados y saldos. Implementa apertura, recepción, salidas, devoluciones, bajas y libro. Confirma atómica e idempotentemente, valida signos y cierres, resuelve concurrencia. Respeta el trigger dueño del saldo. No activar valoración real con D01/D03 abiertas. Integra compras con recepción parcial.
Criterio mínimo de salida: T19–T30; saldo 27 L/S216 tras 32 L a S8 y salida 5 L. Sin doble movimiento.
Comunica avances y hallazgos; corrige fallos antes de declarar terminado.
Entrega archivos modificados, migraciones, pruebas ejecutadas, demostración y pendientes.
Actualiza el seguimiento y deja la siguiente tarea concreta.
```

## Etapa 5 — Producción

```text
Actúa con el prompt maestro y esta guía.
Implementa requerimientos desde minuta, entrega genérica asignada a variantes, adicionales, devoluciones, raciones, excedentes y mermas. Usa documentos de stock existentes, sin segunda descarga. Declara si costo por receta es observado o asignado.
Criterio mínimo de salida: T31/T32; consumo 5,5 L y merma incluida no aumenta la salida.
Comunica avances y hallazgos; corrige fallos antes de declarar terminado.
Entrega archivos modificados, migraciones, pruebas ejecutadas, demostración y pendientes.
Actualiza el seguimiento y deja la siguiente tarea concreta.
```

## Etapa 6 — Inventarios

```text
Actúa con el prompt maestro y esta guía.
Implementa corte controlado, conteo por envases/parciales, importación, diferencias y reconteo. Mantén conteo sin impacto en stock. Implementa ajuste separado solo conforme a política definida, con autorización y trazabilidad.
Criterio mínimo de salida: T33–T36; sistema 27, físico 26 → −1 sin modificar saldo; ajuste único si autorizado.
Comunica avances y hallazgos; corrige fallos antes de declarar terminado.
Entrega archivos modificados, migraciones, pruebas ejecutadas, demostración y pendientes.
Actualiza el seguimiento y deja la siguiente tarea concreta.
```

## Etapa 7 — Cierres y reportes

```text
Actúa con el prompt maestro y esta guía.
Implementa validaciones de pendientes y conciliación, revisión antes de cierre definitivo, ingreso mensual y Food Cost. Serializa cierre frente a contabilización. Conserva reportes del período. No llamar utilidad neta al margen parcial.
Criterio mínimo de salida: T37–T41/T48; 42 % frente a objetivo 40 %, diferencia 2 pp y S200.
Comunica avances y hallazgos; corrige fallos antes de declarar terminado.
Entrega archivos modificados, migraciones, pruebas ejecutadas, demostración y pendientes.
Actualiza el seguimiento y deja la siguiente tarea concreta.
```

## Etapa 8 — Continuidad y piloto

```text
Actúa con el prompt maestro y esta guía.
Implementa topología offline elegida, outbox/inbox, IDs globales, reintentos, orden, conflictos y reconciliación. Ensaya respaldo/restauración y actualización. Ejecuta recorrido completo y plan de piloto mensual. No declarar cobertura offline solo por existir una cola.
Criterio mínimo de salida: T42–T46; repetición de eventos sin duplicados, recuperación conciliada y evidencia del piloto.
Comunica avances y hallazgos; corrige fallos antes de declarar terminado.
Entrega archivos modificados, migraciones, pruebas ejecutadas, demostración y pendientes.
Actualiza el seguimiento y deja la siguiente tarea concreta.
```

## Etapa 9 — Extensiones

```text
Actúa con el prompt maestro y esta guía.
Con el núcleo aceptado, amplía contratos mensuales, roles avanzados y resultados con gastos. Cada integración externa requiere especificación, sandbox y conciliación; no inventar endpoints ni credenciales. Mantén compatibilidad de API e historia.
Criterio mínimo de salida: Pruebas propias del contrato, aislamiento y resultado trazable; sin regresiones del núcleo.
Comunica avances y hallazgos; corrige fallos antes de declarar terminado.
Entrega archivos modificados, migraciones, pruebas ejecutadas, demostración y pendientes.
Actualiza el seguimiento y deja la siguiente tarea concreta.
```
