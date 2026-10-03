# Seguimiento del proyecto

Actualizado: 2026-10-03. Registra **solo lo ejecutado y comprobado**; una tabla o pantalla no es "terminado".
Plan: `docs/guia_construccion/04_PLAN_POR_ETAPAS.md`. Decisiones de negocio: `docs/guia_construccion/09_SEGUIMIENTO_Y_DECISIONES.md`.

## Decisiones técnicas tomadas

| ID | Decisión | Fecha | Origen |
|---|---|---|---|
| RNF-12 | Lenguaje **VB.NET** | 02/10/2026 | Usuario |
| RNF-12.1 | **Windows 10 o superior** | 02/10/2026 | Usuario |
| RNF-13 | **PostgreSQL**, servidor por sede | 02/10/2026 | Propuesta aceptada |
| RNF-14 | ≈20 computadoras | 02/10/2026 | Usuario (por confirmar si por sede o total) |
| RNF-15 | Interfaz **WinForms** (.NET 8) | 02/10/2026 | Propuesta aceptada |
| RNF-16 | Ramas `main` ← `develop` ← `feature/etapa-N-*` | 02/10/2026 | Usuario |

## Estado por etapa

| Etapa | Estado | Qué hay / qué falta |
|---|---|---|
| 0 Diagnóstico y línea base | **Hecha** | Esquema portado a PostgreSQL; migraciones V001–V004 con migrador versionado; brechas H01–H03 cerradas |
| 1 Fundamentos y catálogo | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Acceso con bloqueo, permisos por operación, auditoría automática, aislamiento RLS, operaciones/almacenes/usuarios, unidades, categorías, marcas, productos, variantes, empaques, proveedores, precios con vigencia, importador CSV con vista previa. Pantallas WinForms compiladas **pero no ejecutadas** (no hay Windows en este entorno) |
| 2 Menús y recetas | Parcial mínimo | Solo la fórmula de escalado (T08, T09) |
| 3 Previsión y compras | Parcial mínimo | Solo necesidad neta y redondeo (T12–T15) |
| 4 Almacén y kárdex | Parcial avanzado | Servicio de contabilización atómico con sesión y permisos. Falta: idempotencia, recepciones parciales, devoluciones, reversiones, valoración real (D01) |
| 5–9 | No iniciadas | |

## Casos de la guía ejecutados

| Caso | Dónde | Estado |
|---|---|---|
| T01 aislamiento de referencias / almacén ajeno | SQL, servicio de stock, RLS | Pasa |
| T02 búsqueda por texto y vistas sin fuga | SQL (RLS + `security_invoker`) y `CatalogoTests` | Pasa (y falla al quitar el RLS: verificado) |
| T03 código de variante repetido | `CatalogoTests` | Pasa |
| T04 conversiones 4×4 L, 5 L distinta | Dominio, SQL, servicio | Pasa |
| T05 kg→L sin regla | Dominio | Pasa |
| T06 cambio de presentación usada no altera historia | SQL y servicio | Pasa |
| T08, T09 escalado de receta | Dominio | Pasa |
| T12–T15 previsión y empaques | Dominio (T14 también en SQL) | Pasa |
| T19, T20, T21, T23, T24, T25, T26, T39 | SQL, concurrencia real y servicio | Pasa |
| T33, T34 conteo sin ajuste | SQL (vista) | Pasa a nivel de vista |
| T47 importación repetida y filas inválidas | `ImportacionTests` | Pasa |

**No ejecutados:** T07, T10, T11, T16–T18, T22, T27–T32, T35–T38, T40–T46, T48.

## Evidencia

```bash
./ejecutar_pruebas.sh
```
Última corrida: 67 aserciones SQL + concurrencia (T24 y carrera de 10 sesiones), 35 pruebas de dominio, 34 de integración, instalador de punta a punta (migrar dos veces + crear empresa) y compilación WinForms sin advertencias. Entorno: Ubuntu 24.04, PostgreSQL 16.14, SDK .NET 8.0.425 oficial de Microsoft. El mismo script corre en GitHub Actions.

Se comprobó que las pruebas detectan fallos: mutación del redondeo de empaques (6 pruebas fallan), quitar el bloqueo de saldo (concurrencia falla) y desactivar el RLS (T02 falla).

## Límites de lo comprobado

- **La interfaz WinForms no se ha ejecutado**: solo compila. Hay que probarla en una PC con Windows 10+.
- Valoración con **costo fijo de S/8/L solo en pruebas**; el método real (D01) no está decidido.
- No se probó: carga con 20 usuarios, respaldo/restauración, sincronización entre sedes.
- La clave del usuario de sede se guarda cifrada con DPAPI en cada PC; cualquier administrador local de esa PC puede descifrarla (aceptable para una red de sede, revisar si el riesgo cambia).
- El rol de aplicación puede modificar usuarios de su empresa (necesario para administración); el control es por permiso en la aplicación.

## Brechas de la auditoría de la guía

H01, H02 y H03: **cerradas** (V003) y probadas también con el rol de la aplicación.

## Pendiente

**Decisiones de negocio** (guía doc. 09): D01 valoración · D02 precio de ingrediente genérico · D03 impuestos/cargos · D04 redondeo de compra · D05 formato de bajas · D06/D07 ajustes de inventario y corte · D08 reserva · D09 sustituciones · D10 offline · D11 excesos de recepción · D12 stock crudo en cocina · D13/D14 Food Cost y costo por receta.

**Preguntas al usuario:** ¿las ≈20 PC son de una sola sede? · ¿entran CD/ADS/tránsitos y raciones por cliente en la primera etapa?

**Brechas de esquema aún abiertas:** estado "en tránsito" para traspasos entre bodegas; atributos de receta por régimen; raciones diarias por cliente; fórmula exacta de `necesidad_neta` con `reserva` y `stock_utilizable`.

## Siguiente tarea exacta

1. Probar la aplicación WinForms en una PC Windows 10+ con un PostgreSQL local (pasos en `README.md`) y registrar observaciones.
2. Etapa 2 (`feature/etapa-2-menus-recetas`): servicios y estructuras, recetas versionadas con rendimiento e ingredientes del catálogo, minutas por día/servicio, costeo con snapshot (T07, T10, T11). No depende de D01.

## Continuidad

Rama de trabajo: `claude/busy-mayer-9fxop6` (PR #1 hacia `main`). `develop` se actualiza con cada entrega verificada.
No hay datos reales; nada en producción.
