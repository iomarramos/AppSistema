# Contrato de datos — exportación del resultado mensual (versión 1)

Integración por **archivo** (no hay endpoints de terceros definidos; la guía prohíbe inventarlos). El archivo lo produce la pantalla *Cierres > Gastos y resultado mensual > Exportar CSV* o el comando `AppSistema.Instalador exportar-resultados AAAA-MM ARCHIVO` (programable).

## Formato

- CSV separado por `;`, UTF-8 con BOM, fin de línea `LF`, primera fila de encabezados.
- Importes en soles con punto decimal y 2 decimales (`10000.00`), sin separador de miles. Porcentaje con 2 decimales (`15.00` = 15 %).
- Una fila por servicio de la operación, una fila `No asignado (...)` (gastos comunes, bajas y ajustes de inventario) y una fila `TOTAL`.
- Los textos no contienen `;` ni saltos de línea (se reemplazan).

| Columna | Tipo | Descripción |
|---|---|---|
| version | entero | Versión del contrato (1). Un cambio incompatible sube la versión. |
| empresa | texto | Código de empresa |
| operacion | texto | Código de la operación (sede) |
| periodo | AAAA-MM | Mes del resultado |
| estado_periodo | texto | `abierto` o `cerrado`. Solo un período **cerrado** es definitivo (repetir la exportación da el mismo archivo). |
| servicio | texto | `Servicio - Régimen`, `No asignado (...)` o `TOTAL` |
| ingreso | decimal | Ingreso neto + ajustes del mes |
| fuente_ingreso | texto | `Contrato <código> (<días> dias)` o el texto del ingreso manual; `sin ingreso` si falta |
| alimentos | decimal | Consumo neto de alimentos (entregas − devoluciones) a costo histórico |
| personal | decimal | Gastos reales de personal |
| operacion_gastos | decimal | Gastos reales de operación |
| otros | decimal | Administración + otros; en "No asignado" también bajas − ajustes de inventario |
| total_gastos | decimal | alimentos + personal + operacion_gastos + otros |
| margen | decimal | ingreso − total_gastos |
| margen_pct | decimal o vacío | margen / ingreso × 100; vacío si el ingreso es 0 |
| presupuesto_gastos | decimal | Gastos marcados como presupuesto (no entran al margen) |

## Conciliación

`TOTAL.ingreso` = suma de `ingreso` de las filas de servicio; lo mismo para cada columna de importe. El costo de alimentos se rastrea a los documentos de salida y devolución del mes; cada gasto, a su registro en la pantalla de gastos. El sistema receptor debe rechazar un archivo cuya versión no conozca y no debe cargar dos veces el mismo `empresa + operacion + periodo` cerrado.
