# Tablas del SGP contra la base de AppSistema

Fuente: 75 capturas del SGP (`datos/sgp_pantallas/`, transcritas en `datos/sgp_pantallas/transcripcion/`), el manual V006 y los
reportes del pre-cierre. Fecha: 2026-10-06. Migración resultante: **V026** (14 tablas nuevas; 89 → 103 con `esquema_migracion`).

Leyenda: ✅ ya existía · 🆕 se agregó en V026 · ⏳ pendiente (se sabe qué falta) · ❓ no se puede decidir sin confirmar.
Regla del proyecto: **no se inventa**. Donde la captura no muestra una estructura, un valor o el significado de una sigla,
queda en ❓ y se pregunta (sección final).

## 1. Maestros

| Lo que muestra el SGP (capturas) | En AppSistema | Estado |
|---|---|---|
| Contrato / centro de costo `PE017401 - ORCOPAMPA FOALI` y "Centro de Costo OPTIMUM" (01, 19, 46, 61) | `operacion` (codigo, nombre) | ✅ · 🆕 `operacion.codigo_optimum` |
| "Contrato 2010007950" en el Food Cost (61) | — | ❓ coincide con el RUT del cliente (cap. 47); no se sabe si es el número de contrato comercial |
| Bodega `PE017401 - FOALI` (39-45) y "Bodega 433" (70) | `almacen` | ✅ · ❓ qué es el 433 |
| Régimen (1, 2, 3, 5 "General"), servicio (31, 141, 281, 441, 639, 725, 727, 728, 784, 921) (03, 21, 45) | `regimen`, `servicio`, `operacion_servicio` | ✅ |
| Estructura del servicio: 33 filas (ENTRADA FRIA 1 … BEBIDA FRIA 1) y fila "Comensales" (04, 09, 72) | `estructura_servicio` (orden); "Comensales" = `minuta.comensales` | ✅ |
| Cliente (RUT, razón social) (47-52) | `cliente` | ✅ |
| Proveedor: RUC, dirección, atte., fonos, fax (16-18) | `proveedor` | ✅ · 🆕 `direccion`, `fax` (varios fonos van en `telefono` como texto) |
| Ingrediente genérico ↔ productos comerciales equivalentes (69) | `producto_base` ↔ `variante_producto` vía `ingrediente_variante_permitida` | ✅ |
| Unidades: abreviatura y nombre (LAT/LATA, BOL/BOLSA, BID/BIDON, SCO/SACO, CAJ/CAJA, PQT/PAQUETE, GLN/GALON, BLD/BALDE, BLI/BLISTER, UND, KG, LT) (39, 44, 65, 70) | `unidad_medida` (base) y `tipo_envase` (texto) | ⏳ falta un catálogo abreviatura→nombre; hoy la abreviatura vive como texto en `sgp_codigo_producto.unidad_bulto` |
| Forma de pago (CONTADO) (50, 51) | — | 🆕 `forma_pago` (catálogo; se carga con CONTADO) |
| Sector de cocina (código, descripción) en salida y devolución a producción (42, 43, 14) | — | ❓ ver pregunta 3 |
| Tipos de movimiento E/S: recepción proveedor, FOFI, traspasos entre contratos / CD / bodegas, salida a producción, devolución, merma, venta cafetería, toma de inventario, requerimiento diario, merma ADS, traspaso ALM remoto (15, 38, 53) | `documento_stock.tipo` (10 tipos) + `recepcion.tipo_ingreso` | ✅ para lo que existe · ❓ CD, ADS, ALM remoto (pregunta 4) |

## 2. Receta (pantalla "Receta", capturas 05-08, 10, 33)

| Lo que muestra el SGP | En AppSistema | Estado |
|---|---|---|
| Nombre, nombre de fantasía | `receta.nombre` | ✅ · 🆕 `receta.nombre_fantasia` |
| Categoría dietética (NORMAL) | — | 🆕 `categoria_dietetica` + `receta.categoria_dietetica_id` |
| Tipo de plato (ENSALADAS; `SALSAS / ALCUZAS / ISLAS\ALCUZAS`; `GUARNICIONES\OTROS`) con jerarquía por `\` | — | 🆕 `tipo_plato` (con `padre_id`) + `receta.tipo_plato_id` |
| Pestaña **Detalle Receta Patrón** (ingredientes con C.Bruta, U.M., %Aprov., %A.Coc.) | `receta_ingrediente` | ✅ · 🆕 `pct_aprovechamiento_bp`, `pct_ajuste_coccion_bp` (100 % = 10000) |
| Pestaña **Detalle Receta Local** (vacía en la captura 07) | — | 🆕 `receta_ingrediente_ambito` con `ambito='local'` y `operacion_id` |
| Pestaña **Detalle Receta x Regimen** (selector de régimen; otros ingredientes y cantidades que el patrón) | — | 🆕 `receta_ingrediente_ambito` con `ambito='regimen'` y `regimen_id` |
| Panel "Aporte Nutricionales": Agua, Energía kcal y kJ, Proteínas, Grasa total, Cenizas, Carbohidratos totales y disponibles, Fibra cruda y dietaria, Calcio, Fósforo, y más con scroll | — | 🆕 `nutriente` (catálogo) y `producto_nutriente` (composición por 100 g). **Sin valores**: los archivos recibidos no traen nutrientes |
| Métodos de preparación | `sgp_preparacion` (pasos importados) | ✅ |
| Grupo vulnerable (pestaña sin contenido a la vista) | — | ⏳ no se ve su estructura |
| Cabecera C.Bruta, C.Servida, G.Neto; pie Gr.Net.Verd., P.A.V.B., P.A.V.B. %, Costo | `cantidad_base_neta_u6` y el costo de la minuta | ❓ fórmulas sin confirmar (pregunta 5) |

**Lo que sí se ve en los datos del SGP** (sirve para comprobar después):

* La receta ENSALADA RUSA II tiene 8 ingredientes en el patrón (C.Bruta 0,0362) y otros, con más cantidad, en el régimen 3 (C.Bruta 1,1362). El costo de la planificación (0,484232, captura 09) es el del detalle **por régimen**, no el del patrón. Por eso el costeo debe poder usar el detalle por régimen. **Esta migración guarda el dato; el cambio del costeo es otra entrega.**
* QUESO EDAM tiene un solo ingrediente (30 g, 100 % de aprovechamiento) y aporta 106,80 kcal, 8,10 g de proteínas, 7,83 g de grasa total, 12,00 g de agua, 1,23 g de cenizas y 0,84 g de carbohidratos totales (captura 33). Eso equivale a 356 kcal, 27 g de proteínas, 26,1 g de grasa, 40 g de agua, 4,1 g de cenizas y 2,8 g de carbohidratos por 100 g: sirve como primer dato de composición y como prueba del cálculo.
* El SGP muestra la energía en kJ **igual** a la de kcal (106,80 en ambas): es un error del origen; no se replica.

## 3. Planificación (capturas 02-04, 09, 11-13, 20-32, 72-75)

| Lo que muestra el SGP | En AppSistema | Estado |
|---|---|---|
| Plan teórico y plan real por servicio, régimen y día | `minuta` (`tipo` teorica/real, `comensales`) y `minuta_detalle` (receta, raciones, `reparto_bp`) | ✅ |
| "Por.[%]" del plan real (cap. 31-32): porcentaje de los comensales que toma el plato | `minuta_detalle.reparto_bp` | ✅ |
| Costo de la minuta del día, costo por bandeja, resumen del mes / día / acumulado (Mat.Prima, Est.Fija, Total, Rac., Cto.Bandeja; planificado y realizado) | derivado; pantalla *Planificación de menús* | ✅ |
| Estructura fija (Est.Fija) | `minuta_estructura_fija` | ✅ |
| Celdas bloqueadas (rojo) / habilitadas (amarillo) | `cierre_diario` | ✅ |
| Calendario del mes: día habilitado / cerrado y enviado / cerrado y no enviado (19) | `cierre_diario.estado` (abierto/cerrado) y `estado_envio` (pendiente/enviado/error) | ✅ |
| **Histórico Planificación Teórica / Real** (por servicio y mes: Abierto o Cerrado) (03, 21, 28) | — | 🆕 `planificacion_mes_estado` (en octubre la teórica está *Cerrada* y la real *Abierta*) |
| Plan del SGP importado (teórico, real, requisición, comparativos, piso y techo) | `sgp_*` (V023) | ✅ |
| Frecuencia de recetas (13) | reporte *Frecuencia teórica* | ✅ |
| Aporte nutricional por días (12) | — | ⏳ necesita composición (arriba) |
| Menú: Grabar semana, Copiar minutas, Actualizar costo recetas, Exportar recetas (25) | — | ⏳ copiar minutas y actualizar costo no existen |
| **Generar Minuta Real** (34): copia la teórica al periodo real | — | ⏳ no existe |
| Columnas "Estado GH" y "Estado GI" (31) | — | ❓ |
| Marca "R" en cada celda | — | ❓ (¿receta asignada?) |

## 4. Raciones y ventas (capturas 47-52)

| Lo que muestra el SGP | En AppSistema | Estado |
|---|---|---|
| **Control de Raciones**: matriz clientes × días con filas por cliente, PERSONAL, PRODUCIDAS, MER_DESCON, MER_PRODUC, y la casilla "Facturable" por día; días bloqueados | `produccion` (producidas/servidas) cubría solo el total | 🆕 `control_racion` (`concepto`: cliente, personal, producidas, mer_descon, mer_produc) y `control_racion_dia` (facturable) |
| **Venta Servicio Contado**: importe por día, régimen, servicio, cliente (opcional) y forma de pago; total del mes (septiembre: 35 651,42) | `venta_servicio` (por minuta) e `ingreso_servicio` (por periodo) | 🆕 `venta_servicio_dia` |
| **Registro de Venta Cafetería**: cabecera (fecha, bodega, cliente, centro de costo) y artículos (código, cantidad, precio de venta, tipo de pago) | — | 🆕 `venta_cafeteria` y `venta_cafeteria_detalle` (con `documento_stock_id` para la salida de stock) |

Reglas en la base: control de raciones, facturable y venta del día **no se cambian con el día cerrado** (`DIA_CERRADO`) y exigen
`PRODUCCION_EDITAR` (raciones) o `CIERRE_EJECUTAR` (venta) en la operación.

## 5. Compras (capturas 16-18, 35-37, 57-59)

| Lo que muestra el SGP | En AppSistema | Estado |
|---|---|---|
| Orden de compra impresa: proveedor, RUC, dirección, atte., fono, fax, "PEDIDO: LCL-06695-092026-ORCOPA_1", fecha | `pedido_compra`, `proveedor` | ✅ · 🆕 `pedido_compra.codigo_sgp` (el mismo código para todos los proveedores de la tanda: no es único) |
| Pedido manual: periodo inicial y final, persona de contacto, cuenta de correo, "Enviar correo" | `pedido_compra` | 🆕 `periodo_desde`, `periodo_hasta`, `persona_contacto`, `correo_destino` · ⏳ el envío del correo |
| Pedidos extra, pedido sugerido (necesidad según minuta, pedido propuesto, formato de compra) | `pedido_compra.tipo='extra'`, `prevision` | ✅ |
| Guía de despacho y factura de venta directa (36) | — | ⏳ |
| **Documento Proveedor**: RUT, tipo y número de documento, fechas, bodega, orden de compra, "Tipo de Operación" CFC o FOFI con folio, glosa, Exento / Neto / IVA / Otros impuestos / Total, fletes | `recepcion` | ✅ · 🆕 `modalidad_sgp` (CFC/FOFI), `folio`, `glosa`, `fletes_u6`, `exento_u6` · neto, IVA y total salen de las líneas |
| Grilla "Impuesto del producto" (descripción, impuesto, valor) | `recepcion_detalle.impuesto_u6` | ⏳ no se ve cómo se calcula (precios sin IGV, decisión D03) |
| Compras de caja chica (proveedor "PROVEEDOR CAJA CHICA", código 26003-6143) | `proveedor.es_caja_chica` | ✅ |

## 6. Almacén (capturas 01, 14-15, 38-45, 53-56)

| Lo que muestra el SGP | En AppSistema | Estado |
|---|---|---|
| **Traspaso entre contratos**: N.º de documento, folio, documento y contrato de origen, fecha de origen, guía de remisión, orden de pedido, placa del camión; cantidad enviada y **recibida** | `documento_stock` (`traspaso_entrada` / `traspaso_salida`) | 🆕 `traspaso_documento` (modalidad contrato / cd / bodega / alm_remoto) y `traspaso_linea` (cantidad recibida) |
| Salida a producción: fecha de emisión y **de producción**, "Cant. Planif." contra "Cant. Realizada", P.M.P., estado PENDIENTE | `documento_stock` (`salida_produccion`), `estado` borrador | ✅ · 🆕 `documento_stock.fecha_produccion`, `documento_stock_detalle.cantidad_planificada_u6` |
| Un documento por servicio y día (63167, 63169, 63171…) (45) | `documento_stock.numero` | ✅ |
| Vistas "Resumido / Sector" y "Oculta ingrediente" | — | ⏳ depende del sector (pregunta 3) |
| Devolución de producción (cantidad de salida, cantidad a devolver) | `devolucion_produccion` | ✅ |
| Toma de inventario general y rotativa; lista de tomas anteriores (30/09, 27/09, 31/08, 23/08…) | `inventario` (`tipo` general/rotativo) | ✅ |
| Impresión de la toma: 5 listados y opciones "solo con diferencias", "incluir stock cero físico", "incluir stock cero sistema" (41) | `ServicioReportes.Inventario` | ✅ parcial · ⏳ falta "Listado de inventario Sistema valorizado" y las tres opciones |
| Stock > Posición, Movimiento, **Consumo alternativo**, Cartola, Detalle cartola, Producto sin movimiento, Registro permanente (54) | `StockValorizado`, `MovimientoStockSintetico`, `RegistroInventarioPermanente` | ✅ · 🆕 **Consumo alternativo** (esta entrega) · ⏳ Cartola, Detalle cartola, Producto sin movimiento |
| Reporte Registro de Mermas; Reporte Traslados ADS | `merma_produccion` | ⏳ / ❓ |

## 7. Cierre y costos (capturas 19, 61-64, 68, 70-71)

| Lo que muestra el SGP | En AppSistema | Estado |
|---|---|---|
| Food Cost (alimento): raciones vendidas, venta del día, valor bandeja, raciones, costo del día, costo bandeja, food cost | reporte *Food cost* | ✅ |
| Costo Plan. Teórico & Realizado; los **6 informes** (teórico & realizado, real & realizado, los tres, y los tres acumulados) | *Comparativo de tres niveles* | ✅ parcial · ⏳ los acumulados y el cruce con el plan real del SGP |
| Costo piso y techo en la cabecera (sin valores en la captura) | `sgp_costo_piso_techo` | ✅ |
| Tipo de costo: Alimentación / **Desechable** / Total | — | ❓ no hay clasificación de desechables (pregunta 6) |
| Costo detallado (teórico), costos detalle período realizado (por documento de salida, con total del servicio) | reporte *Menú teórico*, `documento_stock_detalle` | ⏳ |
| Previsión de consumo (total del periodo) | `prevision` | ✅ |
| Bitácora "Traspaso Existoso" (19) | `sincronizacion_evento` | ✅ |

## 8. Hallazgos que cambian cosas

1. **Una entrada de traspaso exige la salida en la misma base** (`ORIGEN_REQUERIDO`). En el SGP el traspaso entre contratos (Tambomayo → Orcopampa, captura 01) llega de **otro** contrato, cuya salida está en otra base. Hoy AppSistema no puede registrar esa recepción sin inventar una salida local. Hace falta aceptar un documento de origen externo (`traspaso_documento.contrato_origen` y `numero_documento_origen` ya guardan el dato; falta relajar la regla para `modalidad <> 'bodega'`). Se deja para la siguiente entrega a propósito: toca el kárdex.
2. **El costeo debe poder usar el detalle por régimen** (sección 2).
3. **Datos inconsistentes del propio SGP**: el servicio 639 aparece bajo el régimen 2 (capturas 03 y 21) y bajo el régimen General (captura 45); "Contrato" en un reporte es `PE017401` y en otro `2010007950`; hay platos con costo 0,000000 (SOPA CALDO BLANCO CON RES, NARANJA DE MESA, TUNA ROJA) = recetas con ingrediente sin precio, que el SGP no avisa.
4. **"Consumo Alternativo" no es un reporte nuevo de datos**: son las diferencias de la toma (verificado contra el reporte del 27/09/2026).

## 9. Preguntas para el usuario

1. **Traspasos desde otro contrato o CD:** ¿se registra la entrada aunque la salida esté en otra base? (hallazgo 1)
2. **Detalle por régimen y local:** ¿cuál manda para el costo cuando hay patrón, local y régimen? Con las capturas, el régimen costea; del local solo se ve que está vacío.
3. **Sector de cocina:** ¿de dónde sale el sector de un producto en la salida a producción: de la estructura del servicio (p. ej. "Plato de fondo" → cocina caliente) o de la receta?
4. **Siglas:** CFC, FOFI (caja chica, ya se sabe que va con folio), CD (¿centro de distribución?), ADS y "ALM remoto".
5. **Cabecera de receta:** fórmulas de C.Servida, G.Neto, Gr.Net.Verd. y P.A.V.B.
6. **Desechables:** ¿qué productos son "costo desechable"? (hoy no hay clasificación).
7. **MER_DESCON y MER_PRODUC** en el control de raciones: ¿qué mide cada una?
8. **Nutrientes:** ¿hay una tabla de composición (por ejemplo la peruana del CENAN) para cargar, o se parte de las recetas que el SGP ya calculó?
9. **"Estado GH / Estado GI"** y la marca **"R"** de las celdas del plan.
