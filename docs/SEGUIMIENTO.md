# Seguimiento del proyecto

Actualizado: 2026-10-02. Registra **solo lo ejecutado y comprobado**; una tabla o función no es "terminado".
Plan de referencia: `docs/guia_construccion/04_PLAN_POR_ETAPAS.md`. Decisiones: `docs/guia_construccion/09_SEGUIMIENTO_Y_DECISIONES.md`.

## Decisiones técnicas tomadas

| ID | Decisión | Fecha | Origen |
|---|---|---|---|
| RNF-12 | Lenguaje **VB.NET**, para todas las computadoras | 02/10/2026 | Usuario |
| RNF-13 | Base de datos **PostgreSQL** (servidor por sede). Se descarta SQLite | 02/10/2026 | Propuesta aceptada |
| RNF-14 | ≈20 computadoras | 02/10/2026 | Usuario (por confirmar si por sede o total) |

## Estado por etapa

| Etapa | Estado | Qué hay |
|---|---|---|
| 0 Diagnóstico y línea base | **Hecha en lo técnico** | Stack fijado; esquema portado a PostgreSQL con migraciones V001–V003; verificación reproducida; brechas H01–H03 cerradas |
| 1 Fundamentos y catálogo | **Parcial** | Dominio de conversión (escala u6, empaques, unidades) con pruebas. **Falta:** permisos/sesión, auditoría automática, CRUD de catálogo, importador, pantallas |
| 2 Menús y recetas | Parcial mínimo | Solo la fórmula de escalado de receta (T08, T09). Sin tablas de aplicación ni pantallas |
| 3 Previsión y compras | Parcial mínimo | Solo necesidad neta y redondeo por mínimo/múltiplo (T12–T15). Falta asignación a variantes, obsolescencia, pedidos |
| 4 Almacén y kárdex | **Parcial avanzado** | Servicio de contabilización atómico en VB.NET + reglas en BD. **Falta:** idempotencia, versiones, recepción parcial, devoluciones, reversiones, valoración real |
| 5–9 | No iniciadas | |

## Casos de la guía ejecutados (de T01–T48)

| Caso | Dónde se prueba | Estado |
|---|---|---|
| T01 aislamiento entre empresas | `01_verificacion.sql` (FK) y `ServicioStockTests` (almacén ajeno) | Pasa |
| T04 conversión 4×4 L = 16 L; 2 cajas = 32 L; 5 L distinta | Dominio (`CatalogoTests`), SQL | Pasa |
| T05 kg→L sin regla | Dominio | Pasa |
| T08, T09 escalado de receta | Dominio | Pasa |
| T12–T15 previsión y empaques | Dominio | Pasa |
| T19, T20 recibir 32 L a S/8, sacar 5 L → 27 L / S/216 | SQL y servicio VB.NET | Pasa |
| T21 un movimiento por línea | SQL (único por detalle) | Pasa. Falta idempotencia de reintento (T22) |
| T23 rollback si falla la 2.ª línea | SQL y servicio VB.NET | Pasa |
| T24 dos salidas de 20 L con 27 L | Bash con conexiones independientes y servicio VB.NET con hilos | Pasa; más carrera de 10 sesiones |
| T25 documento confirmado no se edita | SQL y servicio (con rol de aplicación) | Pasa |
| T26 signo por tipo | SQL y servicio | Pasa |
| T33, T34 conteo sin ajuste; vacío ≠ cero | SQL (vista) | Pasa a nivel de vista; falta el flujo completo |
| T39 día y mes cerrado | SQL y servicio | Pasa |
| T14 mínimo y múltiplo en pedidos | SQL (trigger) | Pasa |

**No ejecutados:** T02, T03, T06, T07, T10, T11, T16–T18, T22, T27–T32, T35–T38, T40–T48 (incluye sincronización, restauración, migración con datos, importación y Food Cost).

## Evidencia (cómo reproducir)

```bash
./ejecutar_pruebas.sh     # 44 aserciones SQL + concurrencia + 25 pruebas de dominio + 9 de capa de datos
```
Resultado de la última corrida: todo OK. Entorno: Ubuntu 24.04, PostgreSQL 16.14, .NET SDK 8.0.131. Se comprobó que las pruebas detectan fallos: una mutación del cálculo de empaques rompe 6 pruebas de dominio, y quitar el bloqueo de saldo hace fallar la prueba de concurrencia.

## Límites de lo comprobado

- Todo corre en **Linux**. No se ha probado en Windows ni con equipos reales.
- No hay interfaz ni proyecto de escritorio (WinForms/WPF) todavía.
- La valoración usa un **costo fijo de S/8/L solo en pruebas**; el método real (D01) sigue sin decidir.
- No se probó servidor de sede, respaldo/restauración, sincronización, ni carga con 20 usuarios.
- `Npgsql 8` requiere .NET 6+; si hay equipos con Windows anterior a 10 hay que revisar (P-17).

## Brechas de la auditoría de la guía

| ID | Estado |
|---|---|
| H01 detalle confirmado editable | **Cerrada** (V003, probada con rol de la aplicación) |
| H02 saldo editable | **Cerrada** (V003 + sin permiso de escritura para `app_stock`) |
| H03 cabecera confirmada editable | **Cerrada** (V003) |

## Pendiente (resumen)

**Decisiones de negocio** (sin cambios, ver guía doc. 09): D01 valoración · D02 precio de ingrediente genérico · D03 impuestos/cargos · D04 redondeo de compra · D05 formato de bajas · D06/D07 ajustes de inventario y corte · D08 reserva · D09 sustituciones · D10 offline · D11 excesos de recepción · D12 stock crudo en cocina · D13/D14 Food Cost y costo por receta.

**Preguntas nuevas para el usuario:** P-17 (¿solo Windows? versiones mínimas) · tecnología de interfaz (WinForms/WPF) · ¿las ≈20 PC son una sola sede? · ¿entran CD/ADS/tránsitos y raciones por cliente en la primera etapa? (el manual SGP los trae; la guía los difiere).

**Brechas del esquema detectadas al validar** (no resueltas): factores de conversión entre unidades de la misma dimensión (kg↔g, L↔ml); estado "en tránsito" para traspasos entre bodegas; atributos de receta por régimen; raciones diarias por cliente; fórmula exacta de `necesidad_neta` con `reserva` y `stock_utilizable`.

## Siguiente tarea exacta

Etapa 1, B003: sesión autorizada y permisos básicos (empresa y operación derivadas de la sesión, nunca del cliente) + auditoría automática, con pruebas T01/T02 a nivel de aplicación. En paralelo, cuando se confirme P-17, elegir WinForms o WPF y crear el proyecto de escritorio con la pantalla de acceso.

## Continuidad

Última tarea completada: servicio de contabilización de stock con pruebas de concurrencia.
Rama: `claude/busy-mayer-9fxop6`. Sin PR: el repositorio remoto no tiene rama base.
No tocar: nada en producción; no hay datos reales.
