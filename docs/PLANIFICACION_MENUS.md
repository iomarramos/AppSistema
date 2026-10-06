# Planificación de menús (matriz mensual)

Estado: **primera entrega** (2026-10-05). Pantalla en Menús > Planificación de menús (matriz mensual), con cálculos,
ediciones y resúmenes del plan teórico. Compilada y probada con pruebas automáticas; **la pantalla no se ha ejecutado
todavía en una PC Windows con datos**. Las pruebas de base de datos corrieron contra PostgreSQL 17 en un clúster aislado.

## 1. Diagnóstico (etapa 1)

### 1.1 Lo que ya existe y se reutiliza (no se duplicó)

| Necesidad del encargo | Componente existente | Decisión |
|---|---|---|
| Servicio, régimen y estructura del servicio | `servicio`, `regimen`, `operacion_servicio`, `estructura_servicio` | Se usan tal cual. Las filas de la matriz salen de las estructuras, no del código |
| Jornada por fecha: comensales y estado | `minuta` (`fecha`, `comensales`, `estado` borrador/aprobada/cerrada, `tipo` teorica/real) | Es la jornada. Se usa `tipo = 'teorica'` |
| Preparaciones por componente y raciones | `minuta_detalle` (receta, raciones, `factor_consumo_bp`, `reparto_bp`) | Es la celda. Raciones = comensales × factor del componente × factor de participación |
| Productos fuera de recetas (estructura fija) | `minuta_estructura_fija` | Se suma como "Estructura fija" del día |
| Costo previsto por ración | `minuta_detalle.costo_previsto_racion_u6` (se fija al aprobar, con fuente y fecha) | Se muestra. Sin aprobar, el costo queda **pendiente** |
| Validación de operación y estado | `ServicioMinutas.ExigirMinutaDeOperacion` | Se reutiliza (pasó a `Friend`) |
| Cálculo de raciones por factor y reparto | `VentaEstructura.Raciones` (Dominio) | Se reutiliza: 520 × 60 % = 312 |
| Aprobar y fijar costo | `ServicioMinutas.Aprobar` | Se llama desde la pantalla |
| Versiones inmutables de planificación central | `planificacion`, `planificacion_version` (V022: BORRADOR / APROBADO / REEMPLAZADO) | **No conectado todavía** (ver pendientes) |
| Consumo y venta real | `consumo_plato`, `venta_servicio` (V015) y `ServicioComparativo` | **No conectado todavía** (ver pendientes) |

**Migraciones:** no se requiere ninguna en esta entrega. `minuta_detalle` ya guarda factor y reparto (V015) y `minuta`
ya tiene comensales, estado y tipo. Si después se decide liberar y versionar la matriz, se hará con la tabla de
versiones existente (V022), no con tablas nuevas.

### 1.2 Lo que muestran las capturas (`Pant/`, "Planificación Real" del SGP)

Confirmado en la imagen:

* **Estructura Servicio**: columna fija con los componentes (entrada fría, sopa, plato de fondo, guarnición, salad bar, isla, fruta, postre, bebida).
* **Comensales**: última fila, un valor por día.
* **Costo Minuta Día**: sobre cada día aparece el costo **por bandeja** del día. Verificado: el 1/10 muestra 8,63 y el acumulado de ese día da 4 485,92 ÷ 520 = 8,63.
* **Total Mes**: "Rac." es la suma de comensales del mes (15 218) y "Cto. Bandeja" = costo total ÷ esa suma (127 684,72 ÷ 15 218 = 8,39). Confirma la regla del encargo: el mensual por bandeja se calcula con totales, no con promedio de días.
* **Estructura fija**: aparece en 0,00 en todo el mes de esa operación. Costo total = materia prima + estructura fija.
* **Leyenda**: verde = estructura del servicio; amarillo = celda habilitada; rojo = celda bloqueada (los días pasados aparecen en rojo).
* **Acumulado hasta**: planificado y realizado, con costo por bandeja.

**No confirmado:** la letra **"R"** delante de cada preparación. No hay leyenda que la explique. No se le asigna ningún comportamiento. Pregunta al usuario: ¿significa "receta", "renglón" u otra cosa?

### 1.3 Reglas pendientes de definir (el usuario debe confirmar)

1. **Costo en borrador.** Hoy el costo solo se fija al aprobar (snapshot). Falta decidir si la matriz debe recalcular costos con precios vigentes mientras la jornada está en borrador.
2. **Estructura fija en la fórmula de costo total.** Se implementó: costo total = materia prima + estructura fija. Confirmar.
3. **Realizado.** Hoy no hay registro de lo realizado en la minuta. La comparación con lo ejecutado está en `consumo_plato` y `venta_servicio`. Falta decidir cómo se relaciona con la matriz.
4. **Liberado y versiones.** Confirmar si el "liberado" es la aprobación de la minuta (estado `aprobada`) o la versión de la planificación central (V022).
5. **Factor de participación.** Se tomó como el reparto de la preparación dentro de su componente. Con componente al 100 %, el factor es el porcentaje de comensales. Confirmar.
6. **Comensales cero.** El costo por bandeja muestra "No calculable". Pero al cambiar comensales a cero, las preparaciones con factor quedan con **1 ración** (regla existente en `ServicioMinutas.ActualizarComensales`). Confirmar si debe ser cero.
7. **Redondeo de raciones.** Se usa el redondeo a la unidad (mitad hacia arriba) de `VentaEstructura`. Para alternativas que deben cubrir el total exacto existe `PlanificacionMenu.RepartirResiduo` (mayor fracción primero; a igual fracción, la de menor posición), pero **todavía no se usa en la pantalla**.

## 2. Qué hace la pantalla

Menús > **Planificación de menús (matriz mensual)**. Permiso de entrada: `MENUS_VER`.

* **Cabecera:** operación, servicio y régimen, mes (con flechas), botones **Consultar**, **Ir a hoy**, **Ir a fecha...** y **Aprobar jornada...** (solo con `MINUTAS_APROBAR`).
* **Franja:** la nota "Las raciones deben incluir las raciones del personal" y la leyenda de colores con el estado escrito (no solo color).
* **Matriz:** filas = componentes y sus alternativas. Columnas congeladas: **Orden** y **Estructura del servicio**. Por cada día: Receta, Factor %, Raciones, Costo unitario y Costo total. Debajo: Comensales, Costo del día, Costo por bandeja y Estado de la jornada. Barras de desplazamiento horizontal y vertical.
* **Ediciones** (solo en jornadas en borrador y con permiso `MINUTAS_EDITAR`):
  * **Factor %**: cambia el factor de participación; las raciones se recalculan y los comensales **no** cambian.
  * **Raciones**: raciones escritas a mano (quedan sin factor).
  * **Comensales** (fila del día): total de referencia de la jornada.
  * Cada cambio se valida en la base. Si falla, no queda nada a medias y la matriz se vuelve a leer.
* **Resúmenes:** Total del mes; Día seleccionado (planificado y realizado); Acumulado hasta la fecha seleccionada. La fecha seleccionada es la última celda o día que se eligió.

## 3. Reglas implementadas

| Regla | Dónde | Prueba |
|---|---|---|
| Raciones = comensales × factor × reparto (redondeo a la unidad) | `VentaEstructura.Raciones` | 520 → 312 y 208; 70/30 → 364 y 156; sopa 100 % → 520 |
| Costo total de preparación = raciones × costo unitario | `PlanificacionMenu.CostoTotalU6` | A 100 × 2,50 = 250; B 80 × 1,25 = 100 |
| Materia prima = suma de preparaciones; pendiente si una falta | `PlanificacionMenu.SumaCostosU6` | Un costo pendiente deja el total pendiente |
| Costo por bandeja = costo ÷ comensales; "No calculable" con cero comensales | `PlanificacionMenu.CostoBandejaU6` | 350 ÷ 100 = 3,50 |
| Mensual por bandeja = costo mensual ÷ comensales del mes (no promedio de días) | `ResumenPlanificacion.Sumar` | (350 + 300) ÷ 400 = 1,625 (promedio daría 2,25) |
| Diferencia monetaria = realizado − planificado | `PlanificacionMenu.DiferenciaMonetariaU6` | 385 − 350 = 35 |
| Diferencia % = (realizado − planificado) ÷ planificado; "No calculable" si el plan es cero | `PlanificacionMenu.DiferenciaPorcentualBp` | 10 % con 385 y 350; nada con plan cero |
| Grupo de alternativas que debe sumar 100 %; sin normalizar | `PlanificacionMenu.ValidarCobertura` | 60 + 40 bien; 50 + 40 advierte |
| Reparto exacto de residuos, determinista | `PlanificacionMenu.RepartirResiduo` | 100 → 33, 33, 34; 10 → 3, 3, 4 |
| Jornada aprobada o cerrada no se edita (aunque se llame al servicio) | `ServicioPlanificacionMenu` + `ExigirMinutaDeOperacion` | `MINUTA_APROBADA` al editar factor, raciones o comensales |
| Operación ajena no se edita | `ServicioPlanificacionMenu.PlatoEditable` | `OPERACION_AJENA` |
| Lo guardado se recupera al volver a abrir | Lectura desde la base | Factores 70/30 persisten en una instancia nueva |
| Editar un factor no cambia los comensales | `FijarFactor` | Total 520 después de 70/30 |

Pruebas: `tests/AppSistema.Dominio.Tests/PlanificacionMenuTests.vb` (12, en verde) y
`tests/AppSistema.Datos.Tests/PlanificacionMenuDatosTests.vb` (5, en verde contra PostgreSQL 17).

## 4. Pendientes (no están en esta entrega)

Se dejan explícitos para que nadie crea que existen:

* **Vista Realizado y Comparativo** en la matriz (pendiente de la decisión 3). Hoy el realizado aparece como "pendiente".
* **Liberar y versiones** (Teórico, Liberado, Real con revisiones y motivo): pendiente de la decisión 4.
* **Cabecera completa:** falta contrato, código y régimen como campo aparte (hoy va en el selector de servicio y régimen).
* **Asignar o cambiar receta con selector** (código, nombre, categoría, costo, búsqueda), **copiar día y semana**, **eliminar una asignación** con confirmación, **recalcular costos** (decisión 1), **exportar a Excel**, **imprimir o PDF**, **ver receta e ingredientes**.
* **Agregar o quitar estructura fija** desde la matriz (hoy se ve, pero se edita en Menús > Minutas).
* **Concurrencia:** no hay control de versión por fila. Dos usuarios pueden pisarse. Pendiente.
* **Recálculo de cantidades de ingredientes** (requerimiento): fuera de esta pantalla; se mantiene en Compras y Producción.
* **Pruebas de interfaz** (E2E) de esta pantalla: no se agregaron. Se ejecutó compilación y pruebas de servicio y dominio.
* **Ejecución en Windows con datos reales:** no se ha hecho. Hay que abrir la pantalla en la PC para validar la altura de filas, el ancho de columnas y los colores en 1366 × 768 y 1920 × 1080, con escalas de 100 %, 125 % y 150 %.

## 5. Archivos de esta entrega

| Archivo | Cambio |
|---|---|
| `src/AppSistema.Dominio/PlanificacionMenu.vb` | Nuevo: cálculos de costo, bandeja, diferencias, cobertura, reparto y resumen |
| `src/AppSistema.Datos/ServicioPlanificacionMenu.vb` | Nuevo: lectura del mes y ediciones de factor, raciones y comensales |
| `src/AppSistema.Datos/ServicioMinutas.vb` | `ExigirMinutaDeOperacion` pasa de `Private` a `Friend` |
| `src/AppSistema.Escritorio/FormPlanificacionMenus.vb` | Nuevo: pantalla |
| `src/AppSistema.Escritorio/FormPrincipal.vb` / `.Designer.vb` | Opción de menú "Planificacion de menus (matriz mensual)" |
| `src/AppSistema.Escritorio/Tema.vb` | Tres colores de estado de la matriz |
| `tests/AppSistema.Dominio.Tests/PlanificacionMenuTests.vb` | Nuevo: 12 pruebas |
| `tests/AppSistema.Datos.Tests/PlanificacionMenuDatosTests.vb` | Nuevo: 5 pruebas |
