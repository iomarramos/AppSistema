# Auditoría inicial del esquema existente
Fecha: 02/10/2026. Revisión de estructura y pruebas dirigidas; no auditoría exhaustiva de seguridad ni certificación para producción.

## Archivos revisados
Esquema_Sistema.sql, Base_Datos_Sistema.md, secciones de actualización del Requerimiento_Sistema_Actualizado.md, Validacion.json y script anterior verify_database.py. El SQL y datos de ejemplo se incluyen como referencias reproducibles. Las bases originales no fueron modificadas. El manual corporativo original no se recibió para cotejo de páginas.

## Resultado reproducido
59 tablas. Nueve comprobaciones en SQLite en memoria: conversión 4×4 L; aislamiento de referencias entre empresas; conciliación entrada/salida; duplicado bloqueado; movimientos inmutables; saldo insuficiente bloqueado; día cerrado bloqueado; conteo sin ajuste; integridad y claves foráneas. Todas pasaron en esta ejecución.

## Hallazgos adicionales comprobados
| ID | Prueba realizada en memoria | Resultado | Acción antes del piloto |
|---|---|---|---|
| H01 | UPDATE cantidad de detalle de documento confirmado | Permitido | Proteger detalles, validar estado y probar API/persistencia |
| H02 | UPDATE cantidad en saldo_stock | Permitido | Quitar edición ordinaria, controlar escritor único y conciliación |
| H03 | UPDATE fecha de cabecera confirmada | Permitido | Proteger cabecera y correcciones compensatorias |

Estos casos se ejecutaron en savepoints y se revirtieron. Son brechas de la estructura si se accede directamente a ella; no demuestran una vulnerabilidad remota porque todavía no se evaluó una aplicación ni su autorización. SQLite no dispone del mismo modelo de privilegios por tabla de un servidor relacional; proteger el archivo y canalizar escrituras por el servicio es parte del diseño. La aplicación futura también debe cerrar estos caminos.

## Observaciones de diseño
- `actualizar_saldo` modifica saldos después de insertar movimientos. Añadir una segunda actualización desde backend duplica cantidades y valores.
- `_u6` exige control de escalas; no equivale a un campo decimal sin conversión.
- `unidad_recibida` es texto en el SQL; la guía funcional hablaba de unidad identificada. Resolver normalización/validación y conservar factor aplicado antes de implementar captura libre.
- Estados nuevos de la guía requieren adaptar restricciones CHECK por migración.
- IDs enteros locales y secuencia de almacén no resuelven por sí solos unicidad global al sincronizar sedes.
- La base protege referencias entre empresas, pero una consulta sin filtro aún podría leer datos de otra empresa. Se necesita autorización real.
- El método de valoración sigue abierto: costos fijos de la prueba son datos de prueba, no un motor de PMP implementado.
- La confirmación multiparte, las reversiones, las recepciones parciales y el cierre completo necesitan lógica y pruebas de aplicación.

## Cómo reproducir
Desde la raíz del paquete:
```bash
python3 verificacion/verificar_base.py
```
Solo utiliza la biblioteca estándar de Python y una base en memoria. Lee el SQL de `referencia/`. No modifica los archivos originales ni las bases del usuario. El JSON incluido registra la ejecución realizada al preparar esta entrega.

## Lo que no prueba
Interfaz, API, inicio de sesión, autorización de lectura, concurrencia real, rollback ante fallos del proceso, políticas de impuestos, valoración, previsión, sincronización, restauración ni mes operativo completo. Esas verificaciones se especifican en el plan. Nueve pruebas satisfactorias no significan que las 59 tablas estén completamente validadas.
