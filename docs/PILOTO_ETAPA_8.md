# Plan de piloto — etapa 8 (continuidad)

Objetivo: demostrar en una sede real que se opera sin internet, que lo enviado llega una sola vez a la central y que un respaldo se restaura conciliado. Sin esta evidencia no se ofrece operación sin conexión como terminada.

## Preparación (día 0)

| Paso | Responsable | Comando / acción | Evidencia |
|---|---|---|---|
| Servidor de sede (PC fija, UPS) con PostgreSQL 16 | TI | instalación + `migrar`, `crear-empresa`, `crear-usuario-sede` | salida del instalador |
| Base central | TI | `migrar`, `crear-empresa` (mismo código), `crear-usuario-sincronizacion app_sync` | salida del instalador |
| Registrar la sede | TI | central: `registrar-sede EMPRESA SEDE "Nombre"`; guardar la credencial fuera del repositorio | captura sin la credencial |
| Activar la sede | TI | sede: `configurar-sede EMPRESA SEDE` | número de eventos históricos |
| Envío programado | TI | Programador de tareas: `sincronizar EMPRESA` cada 15 min | historial de la tarea |
| Respaldo diario | TI | Programador de tareas: `respaldar D:\respaldos\sede_%date%.dump` | archivos `.dump` y `.conciliacion` |

## Ensayos obligatorios (semana 1)

1. **Sin internet:** desconectar el enlace de la sede medio día; operar recepciones, salidas y cierre del día. Al reconectar, `sincronizar` debe dejar 0 pendientes y `reporte-central` debe mostrar el mismo stock valorizado que la sede.
2. **Acuse perdido:** cortar la red durante un `sincronizar`; repetirlo. La central no debe duplicar (respuesta "ya estaban").
3. **Corte de energía:** apagar el servidor de sede (sin UPS) durante una carga de documentos. Al encender: `conciliar` debe mostrar `conciliacion.filas_sin_conciliar=0` y ningún documento a medias.
4. **Restauración:** restaurar el respaldo del día en una base nueva (`restaurar`). Debe decir "Restauracion conciliada".
5. **Credencial inválida:** `sincronizar` con una credencial errada debe ser rechazado y quedar registrado en la central.

## Operación (semanas 2–4)

- Revisión diaria: `estado-sincronizacion` (sede) y `reporte-central` (central): pendientes, errores, retenidos, conflictos y fecha de última sincronización.
- Toda incidencia se anota con fecha, paso, mensaje y solución en `docs/SEGUIMIENTO.md`.

## Criterio de aceptación

Ensayos 1–5 superados; un mes sin documentos perdidos ni duplicados en la central; al menos cuatro restauraciones conciliadas; stock de la central igual al de la sede al cierre de cada mes.
