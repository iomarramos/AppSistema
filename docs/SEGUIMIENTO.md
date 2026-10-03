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
| 0 Diagnóstico y línea base | **Hecha** | Esquema portado a PostgreSQL; migraciones V001–V007 con migrador versionado; brechas H01–H03 cerradas |
| 1 Fundamentos y catálogo | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Acceso con bloqueo, permisos por operación, auditoría automática, aislamiento RLS, operaciones/almacenes/usuarios, unidades, categorías, marcas, productos, variantes, empaques, proveedores, precios con vigencia, importador CSV con vista previa, **carga del listado de productos del SGP** (4 158 productos con factor y unidad mínima de pedido; `datos/sgp/`). Pantallas WinForms compiladas **pero no ejecutadas** (no hay Windows en este entorno) |
| 2 Menús y recetas | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Servicios, regímenes y estructuras; recetas versionadas (borrador → aprobada inmutable → retirada) con rendimiento, ingredientes por producto base y variantes permitidas; minutas por día y servicio con platos y fijos; aprobación con snapshot de costo (fuente y fecha por ingrediente); costo simulado; necesidades consolidadas por producto. **Recetas del SGP cargadas**: 946 recetas (417 fichas revisadas + 529 del Recetón) con 412 ingredientes (`datos/recetas/`). Pantallas: Recetas, Minutas y necesidades, Servicios y estructuras, Importar recetas. **Regla de precio provisional** (D02 pendiente): menor costo vigente entre las variantes permitidas; sin precio → "pendiente". El precio se usa tal como se registró (D03 impuestos pendiente) |
| 3 Previsión y compras | **Hecha en código y pruebas; falta validar la interfaz en Windows** | Previsión por almacén desde minutas aprobadas: demanda del horizonte, consumo puente (una sola vez), stock actual, reserva por producto y almacén (D08 como parámetro, 0 por defecto), pendientes de pedidos aprobados menos lo recibido, **recorrido por fechas** con fecha de quiebre (un tránsito tardío no oculta la falta). Validación y obsolescencia (recalcula y compara; los pedidos de la propia previsión no la vuelven obsoleta). Pedido generado por proveedor con el empaque de menor costo vigente y redondeo por mínimo/múltiplo (D04 propuesto); pedidos manuales; aprobación (exige previsión vigente) y anulación; inmutables tras aprobar. Pantalla Compras > Previsión y pedidos |
| 4 Almacén y kárdex | Parcial avanzado | Servicio de contabilización atómico con sesión y permisos. **Inventario inicial valorizado** (documento de apertura, 379 productos, S/ 307 498,45; solo en almacén sin movimientos) y consulta de stock con valor y costo promedio (Almacén > Stock e inventario inicial). Falta: idempotencia, recepciones parciales, devoluciones, reversiones, valoración real (D01) |
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
| T12–T15 previsión y empaques | Dominio, SQL (T14) y `ComprasTests` (T12, T13 con la base) | Pasa |
| T16 tránsito tardío | Dominio y `ComprasTests` | Pasa |
| T17 dos variantes, asignación única | Dominio (`Prevision.Asignar`) | Pasa |
| T18 previsión obsoleta tras cambiar minuta o stock | `ComprasTests` | Pasa |
| T19, T20, T21, T23, T24, T25, T26, T39 | SQL, concurrencia real y servicio | Pasa |
| T33, T34 conteo sin ajuste | SQL (vista) | Pasa a nivel de vista |
| T07 variante de otro producto en receta | `MenusTests` (trigger de BD) | Pasa |
| T10 precio nuevo no cambia minuta aprobada | `MenusTests` (snapshot y bloqueo en BD) | Pasa |
| T11 ingrediente sin costo → costo pendiente | Dominio y `MenusTests` | Pasa |
| Aceptación etapa 2: 10 raciones/1 L → 150 = 15 L, rendimiento cero, consolidado sin duplicar | `MenusTests` | Pasa |
| T47 importación repetida y filas inválidas | `ImportacionTests`, `ImportacionSgpTests` (listado real del SGP) | Pasa |

**No ejecutados:** T22, T27–T32, T35–T38, T40–T46, T48.

## Evidencia

```bash
./ejecutar_pruebas.sh
```
Última corrida: 67 aserciones SQL + concurrencia (T24 y carrera de 10 sesiones), 76 pruebas de dominio, 56 de integración, instalador de punta a punta (migrar dos veces + crear empresa + cargar catálogo por ingrediente, las 946 recetas enlazadas y el inventario inicial dos veces) y compilación WinForms sin advertencias. Entorno: Ubuntu 24.04, PostgreSQL 16.14, SDK .NET 8.0.425 oficial de Microsoft. El mismo script corre en GitHub Actions.

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

**Enlace ingrediente → productos SGP**: cargado desde `PRODUCTO_INGREDIENTE.csv` (3 133 ingredientes con los 4 158 productos SGP como variantes; 303 de 412 ingredientes de receta enlazados). Pendientes para revisar en `datos/enlace/`: 102 ingredientes de receta sin enlace, 72 productos con unidad distinta a su ingrediente, 221 productos SGP sin ingrediente.

**Preguntas al usuario:** ¿las ≈20 PC son de una sola sede? · ¿entran CD/ADS/tránsitos y raciones por cliente en la primera etapa? · ¿los precios del SGP están en soles? · revisar los 47 productos de `datos/sgp/observaciones_sgp.csv`.

**Brechas de esquema aún abiertas:** estado "en tránsito" para traspasos entre bodegas; atributos de receta por régimen; raciones diarias por cliente; fórmula exacta de `necesidad_neta` con `reserva` y `stock_utilizable`.

## Siguiente tarea exacta

1. Probar la aplicación WinForms en una PC Windows 10+ con un PostgreSQL local (pasos en `README.md`) y registrar observaciones.
2. Etapa 4 (`feature/etapa-4-almacen-kardex`): recepción de pedidos (parcial, con conversión y costo), devoluciones, bajas, traspasos, kárdex y consulta de stock. **Necesita decidir D01 (valoración: promedio ponderado propuesto)** para operar con costos reales.
3. Decidir D02 (precio de ingrediente genérico): hoy se usa la regla provisional "menor costo vigente".

## Continuidad

Rama de trabajo: `claude/busy-mayer-9fxop6` (PR #1 hacia `main`). `develop` se actualiza con cada entrega verificada.
No hay datos reales; nada en producción.
