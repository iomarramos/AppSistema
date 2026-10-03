# Relaciones de la base de datos (llaves primarias y foráneas)

Actualizado: 2026-10-03 (migraciones V001–V019).

La base tiene 71 tablas: las 70 del esquema y `esquema_migracion`, que crea el migrador. Hay 203 llaves foráneas. Este documento se generó leyendo las restricciones directamente de PostgreSQL.

## Cómo leerlo

* **PK** es la llave primaria. Todas las tablas tienen `id` autonumérico.
* **Toda tabla con datos de una empresa** tiene `empresa_id → empresa.id`. Además, sus llaves foráneas incluyen `empresa_id`: por ejemplo, `(empresa_id, producto_base_id) → producto_base(empresa_id, id)`. Esto impide enlazar un registro con otro de otra empresa. En las tablas de abajo se escribe abreviado: `producto_base_id → producto_base.id (misma empresa)`.
* Las relaciones con `usuario` (quién creó, aprobó o contó) se listan, pero no se dibujan en los diagramas para no cargarlos.

## Resumen

| Área | Tablas | Relaciones principales |
|---|---|---|
| Acceso y seguridad | 9 | empresa → operación → almacén; usuario ↔ rol por operación; rol ↔ permiso |
| Catálogo | 7 | ingrediente (producto_base) → productos (variante_producto) → bulto (empaque_compra); categoría jerárquica; producto activo por operación |
| Proveedores y compras | 10 | proveedor ↔ bulto (proveedor_empaque) → precio; previsión → pedido → recepción |
| Recetas y menús | 13 | receta → versión → ingredientes; servicio + régimen → servicio de la operación → minuta → platos → costo de ingredientes |
| Almacén y kárdex | 4 | documento → líneas → movimiento (kárdex) y saldo por almacén y producto |
| Producción y real del servicio | 7 | minuta → requerimiento → entrega (documento); producción → mermas; consumo y venta real |
| Inventario físico | 3 | inventario por almacén → líneas por producto → ajuste (documento) |
| Cierres, contratos y resultados | 8 | cierre diario y periodo mensual; cliente → contrato → servicios; ingresos y gastos por periodo |
| Sincronización e integraciones | 9 | cola de salida (sincronizacion_evento) → central: sede → eventos → documentos y movimientos |

## Acceso y seguridad

```mermaid
erDiagram
    operacion ||--o{ almacen : "operacion_id"
    operacion ||--o{ usuario_operacion_rol : "operacion_id"
    permiso ||--o{ rol_permiso : "permiso_id"
    rol ||--o{ rol_permiso : "rol_id"
    rol ||--o{ usuario_operacion_rol : "rol_id"
```

| Tabla | PK | Llaves foráneas (columna → tabla.columna) | La referencian |
|---|---|---|---|
| `empresa` | `id` | — | todas las tablas de datos |
| `operacion` | `id` | `empresa_id` → `empresa.id` | `almacen`, `cierre_diario`, `contrato`, `operacion_servicio`, `periodo_mensual`, `producto_operacion`, `sincronizacion_evento`, `usuario_operacion_rol` |
| `almacen` | `id` | `empresa_id` → `empresa.id`<br>`operacion_id` → `operacion.id` (misma empresa) | `documento_stock`, `inventario`, `movimiento_stock`, `pedido_compra`, `politica_abastecimiento`, `prevision`, `recepcion`, `requerimiento`, `saldo_stock` |
| `usuario` | `id` | `empresa_id` → `empresa.id` | `auditoria`, `cierre_diario`, `consumo_plato`, `documento_stock`, `factor_consumo_operacion`, `gasto`, `inventario`, `inventario_ajuste`, `inventario_detalle`, `minuta`, `movimiento_stock`, `pedido_compra`, `periodo_mensual`, `prevision`, `produccion`, `producto_operacion`, `recepcion`, `requerimiento`, `usuario_operacion_rol`, `venta_servicio` |
| `rol` | `id` | `empresa_id` → `empresa.id` | `rol_permiso`, `usuario_operacion_rol` |
| `permiso` | `id` | `empresa_id` → `empresa.id` | `rol_permiso` |
| `rol_permiso` | `id` | `empresa_id` → `empresa.id`<br>`permiso_id` → `permiso.id` (misma empresa)<br>`rol_id` → `rol.id` (misma empresa) |  |
| `usuario_operacion_rol` | `id` | `empresa_id` → `empresa.id`<br>`operacion_id` → `operacion.id` (misma empresa)<br>`rol_id` → `rol.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) |  |
| `auditoria` | `id` | `empresa_id` → `empresa.id`<br>`usuario_id` → `usuario.id` (misma empresa) |  |

## Catálogo

```mermaid
erDiagram
    categoria_producto ||--o{ categoria_producto : "padre_id"
    categoria_producto ||--o{ producto_base : "categoria_id"
    categoria_producto ||--o{ variante_producto : "categoria_id"
    marca ||--o{ variante_producto : "marca_id"
    operacion ||--o{ producto_operacion : "operacion_id"
    producto_base ||--o{ producto_operacion : "producto_base_id"
    producto_base ||--o{ variante_producto : "producto_base_id"
    unidad_medida ||--o{ producto_base : "unidad_base_id"
    variante_producto ||--o{ empaque_compra : "variante_id"
    variante_producto ||--o{ producto_operacion : "variante_id_producto_base_id"
```

| Tabla | PK | Llaves foráneas (columna → tabla.columna) | La referencian |
|---|---|---|---|
| `unidad_medida` | `id` | `empresa_id` → `empresa.id` | `merma_produccion`, `producto_base` |
| `categoria_producto` | `id` | `empresa_id` → `empresa.id`<br>`padre_id` → `categoria_producto.id` (misma empresa) | `producto_base`, `variante_producto` |
| `marca` | `id` | `empresa_id` → `empresa.id` | `variante_producto` |
| `producto_base` | `id` | `empresa_id` → `empresa.id`<br>`categoria_id` → `categoria_producto.id` (misma empresa)<br>`unidad_base_id` → `unidad_medida.id` (misma empresa) | `minuta_estructura_fija`, `politica_abastecimiento`, `prevision_detalle`, `producto_operacion`, `receta_ingrediente`, `requerimiento_detalle`, `variante_producto` |
| `variante_producto` | `id` | `empresa_id` → `empresa.id`<br>`categoria_id` → `categoria_producto.id` (misma empresa)<br>`marca_id` → `marca.id` (misma empresa)<br>`producto_base_id` → `producto_base.id` (misma empresa) | `documento_stock_detalle`, `empaque_compra`, `ingrediente_variante_permitida`, `inventario_detalle`, `merma_produccion`, `movimiento_stock`, `producto_operacion`, `recepcion_detalle`, `saldo_stock` |
| `empaque_compra` | `id` | `empresa_id` → `empresa.id`<br>`variante_id` → `variante_producto.id` (misma empresa) | `pedido_detalle`, `proveedor_empaque`, `recepcion_detalle` |
| `producto_operacion` | `id` | `empresa_id` → `empresa.id`<br>`operacion_id` → `operacion.id` (misma empresa)<br>`producto_base_id` → `producto_base.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa)<br>`variante_id, producto_base_id` → `variante_producto.id, producto_base_id` (misma empresa) |  |

## Proveedores y compras

```mermaid
erDiagram
    almacen ||--o{ pedido_compra : "almacen_id"
    almacen ||--o{ politica_abastecimiento : "almacen_id"
    almacen ||--o{ prevision : "almacen_id"
    almacen ||--o{ recepcion : "almacen_id"
    empaque_compra ||--o{ pedido_detalle : "empaque_id"
    empaque_compra ||--o{ proveedor_empaque : "empaque_id"
    empaque_compra ||--o{ recepcion_detalle : "empaque_id"
    pedido_compra ||--o{ pedido_detalle : "pedido_id"
    pedido_compra ||--o{ recepcion : "pedido_id"
    pedido_detalle ||--o{ recepcion_detalle : "pedido_detalle_id"
    prevision ||--o{ pedido_compra : "prevision_id"
    prevision ||--o{ prevision_detalle : "prevision_id"
    prevision_detalle ||--o{ pedido_detalle : "prevision_detalle_id"
    producto_base ||--o{ politica_abastecimiento : "producto_base_id"
    producto_base ||--o{ prevision_detalle : "producto_base_id"
    proveedor ||--o{ pedido_compra : "proveedor_id"
    proveedor ||--o{ proveedor_empaque : "proveedor_id"
    proveedor ||--o{ recepcion : "proveedor_id"
    proveedor_empaque ||--o{ precio_compra : "proveedor_empaque_id"
    recepcion ||--o{ recepcion_detalle : "recepcion_id"
    variante_producto ||--o{ recepcion_detalle : "variante_id"
```

| Tabla | PK | Llaves foráneas (columna → tabla.columna) | La referencian |
|---|---|---|---|
| `proveedor` | `id` | `empresa_id` → `empresa.id` | `pedido_compra`, `proveedor_empaque`, `recepcion` |
| `proveedor_empaque` | `id` | `empresa_id` → `empresa.id`<br>`empaque_id` → `empaque_compra.id` (misma empresa)<br>`proveedor_id` → `proveedor.id` (misma empresa) | `precio_compra` |
| `precio_compra` | `id` | `empresa_id` → `empresa.id`<br>`proveedor_empaque_id` → `proveedor_empaque.id` (misma empresa) |  |
| `politica_abastecimiento` | `id` | `empresa_id` → `empresa.id`<br>`almacen_id` → `almacen.id` (misma empresa)<br>`producto_base_id` → `producto_base.id` (misma empresa) |  |
| `prevision` | `id` | `empresa_id` → `empresa.id`<br>`almacen_id` → `almacen.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) | `pedido_compra`, `prevision_detalle` |
| `prevision_detalle` | `id` | `empresa_id` → `empresa.id`<br>`prevision_id` → `prevision.id` (misma empresa)<br>`producto_base_id` → `producto_base.id` (misma empresa) | `pedido_detalle` |
| `pedido_compra` | `id` | `empresa_id` → `empresa.id`<br>`almacen_id` → `almacen.id` (misma empresa)<br>`prevision_id` → `prevision.id` (misma empresa)<br>`proveedor_id` → `proveedor.id` (misma empresa)<br>`aprobador_id` → `usuario.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) | `pedido_detalle`, `recepcion` |
| `pedido_detalle` | `id` | `empresa_id` → `empresa.id`<br>`empaque_id` → `empaque_compra.id` (misma empresa)<br>`pedido_id` → `pedido_compra.id` (misma empresa)<br>`prevision_detalle_id` → `prevision_detalle.id` (misma empresa) | `recepcion_detalle` |
| `recepcion` | `id` | `empresa_id` → `empresa.id`<br>`almacen_id` → `almacen.id` (misma empresa)<br>`pedido_id` → `pedido_compra.id` (misma empresa)<br>`proveedor_id` → `proveedor.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) | `documento_stock`, `recepcion_detalle` |
| `recepcion_detalle` | `id` | `empresa_id` → `empresa.id`<br>`empaque_id` → `empaque_compra.id` (misma empresa)<br>`pedido_detalle_id` → `pedido_detalle.id` (misma empresa)<br>`recepcion_id` → `recepcion.id` (misma empresa)<br>`variante_id` → `variante_producto.id` (misma empresa) | `documento_stock_detalle` |

## Recetas y menús

```mermaid
erDiagram
    estructura_servicio ||--o{ factor_consumo_operacion : "estructura_id"
    estructura_servicio ||--o{ minuta_detalle : "estructura_id"
    minuta ||--o{ minuta : "minuta_origen_id"
    minuta ||--o{ minuta_detalle : "minuta_id"
    minuta ||--o{ minuta_estructura_fija : "minuta_id"
    minuta_detalle ||--o{ costeo_ingrediente : "minuta_detalle_id"
    operacion ||--o{ operacion_servicio : "operacion_id"
    operacion_servicio ||--o{ factor_consumo_operacion : "operacion_servicio_id"
    operacion_servicio ||--o{ minuta : "operacion_servicio_id"
    producto_base ||--o{ minuta_estructura_fija : "producto_base_id"
    producto_base ||--o{ receta_ingrediente : "producto_base_id"
    receta ||--o{ receta_version : "receta_id"
    receta_ingrediente ||--o{ costeo_ingrediente : "ingrediente_id"
    receta_ingrediente ||--o{ ingrediente_variante_permitida : "ingrediente_id"
    receta_version ||--o{ minuta_detalle : "receta_version_id"
    receta_version ||--o{ receta_ingrediente : "receta_version_id"
    regimen ||--o{ operacion_servicio : "regimen_id"
    servicio ||--o{ estructura_servicio : "servicio_id"
    servicio ||--o{ operacion_servicio : "servicio_id"
    variante_producto ||--o{ ingrediente_variante_permitida : "variante_id"
```

| Tabla | PK | Llaves foráneas (columna → tabla.columna) | La referencian |
|---|---|---|---|
| `receta` | `id` | `empresa_id` → `empresa.id` | `receta_version` |
| `receta_version` | `id` | `empresa_id` → `empresa.id`<br>`receta_id` → `receta.id` (misma empresa) | `merma_produccion`, `minuta_detalle`, `receta_ingrediente` |
| `receta_ingrediente` | `id` | `empresa_id` → `empresa.id`<br>`producto_base_id` → `producto_base.id` (misma empresa)<br>`receta_version_id` → `receta_version.id` (misma empresa) | `costeo_ingrediente`, `ingrediente_variante_permitida` |
| `ingrediente_variante_permitida` | `id` | `empresa_id` → `empresa.id`<br>`ingrediente_id` → `receta_ingrediente.id` (misma empresa)<br>`variante_id` → `variante_producto.id` (misma empresa) |  |
| `servicio` | `id` | `empresa_id` → `empresa.id` | `estructura_servicio`, `operacion_servicio` |
| `regimen` | `id` | `empresa_id` → `empresa.id` | `operacion_servicio` |
| `operacion_servicio` | `id` | `empresa_id` → `empresa.id`<br>`operacion_id` → `operacion.id` (misma empresa)<br>`regimen_id` → `regimen.id` (misma empresa)<br>`servicio_id` → `servicio.id` (misma empresa) | `contrato_servicio`, `documento_stock`, `factor_consumo_operacion`, `gasto`, `ingreso_servicio`, `minuta`, `produccion`, `requerimiento` |
| `estructura_servicio` | `id` | `empresa_id` → `empresa.id`<br>`servicio_id` → `servicio.id` (misma empresa) | `factor_consumo_operacion`, `minuta_detalle` |
| `factor_consumo_operacion` | `id` | `empresa_id` → `empresa.id`<br>`estructura_id` → `estructura_servicio.id` (misma empresa)<br>`operacion_servicio_id` → `operacion_servicio.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) |  |
| `minuta` | `id` | `empresa_id` → `empresa.id`<br>`minuta_origen_id` → `minuta.id` (misma empresa)<br>`operacion_servicio_id` → `operacion_servicio.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) | `minuta_detalle`, `minuta_estructura_fija`, `produccion`, `requerimiento`, `venta_servicio` |
| `minuta_detalle` | `id` | `empresa_id` → `empresa.id`<br>`estructura_id` → `estructura_servicio.id` (misma empresa)<br>`minuta_id` → `minuta.id` (misma empresa)<br>`receta_version_id` → `receta_version.id` (misma empresa) | `consumo_plato`, `costeo_ingrediente` |
| `minuta_estructura_fija` | `id` | `empresa_id` → `empresa.id`<br>`minuta_id` → `minuta.id` (misma empresa)<br>`producto_base_id` → `producto_base.id` (misma empresa) |  |
| `costeo_ingrediente` | `id` | `empresa_id` → `empresa.id`<br>`minuta_detalle_id` → `minuta_detalle.id` (misma empresa)<br>`ingrediente_id` → `receta_ingrediente.id` (misma empresa) |  |

## Almacén y kárdex

```mermaid
erDiagram
    almacen ||--o{ documento_stock : "almacen_destino_id"
    almacen ||--o{ documento_stock : "almacen_id"
    almacen ||--o{ movimiento_stock : "almacen_id"
    almacen ||--o{ saldo_stock : "almacen_id"
    documento_stock ||--o{ documento_stock : "documento_origen_id"
    documento_stock ||--o{ documento_stock_detalle : "documento_id"
    documento_stock_detalle ||--o{ movimiento_stock : "documento_detalle_id"
    operacion_servicio ||--o{ documento_stock : "operacion_servicio_id"
    recepcion ||--o{ documento_stock : "recepcion_id"
    recepcion_detalle ||--o{ documento_stock_detalle : "recepcion_detalle_id"
    requerimiento ||--o{ documento_stock : "requerimiento_id"
    variante_producto ||--o{ documento_stock_detalle : "variante_id"
    variante_producto ||--o{ movimiento_stock : "variante_id"
    variante_producto ||--o{ saldo_stock : "variante_id"
```

| Tabla | PK | Llaves foráneas (columna → tabla.columna) | La referencian |
|---|---|---|---|
| `documento_stock` | `id` | `empresa_id` → `empresa.id`<br>`almacen_destino_id` → `almacen.id` (misma empresa)<br>`almacen_id` → `almacen.id` (misma empresa)<br>`documento_origen_id` → `documento_stock.id` (misma empresa)<br>`operacion_servicio_id` → `operacion_servicio.id` (misma empresa)<br>`recepcion_id` → `recepcion.id` (misma empresa)<br>`requerimiento_id` → `requerimiento.id` (misma empresa)<br>`aprobador_id` → `usuario.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) | `documento_stock_detalle`, `inventario_ajuste`, `merma_produccion`, `produccion_documento` |
| `documento_stock_detalle` | `id` | `empresa_id` → `empresa.id`<br>`documento_id` → `documento_stock.id` (misma empresa)<br>`recepcion_detalle_id` → `recepcion_detalle.id` (misma empresa)<br>`variante_id` → `variante_producto.id` (misma empresa) | `movimiento_stock` |
| `movimiento_stock` | `id` | `empresa_id` → `empresa.id`<br>`almacen_id` → `almacen.id` (misma empresa)<br>`documento_detalle_id` → `documento_stock_detalle.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa)<br>`variante_id` → `variante_producto.id` (misma empresa) |  |
| `saldo_stock` | `id` | `empresa_id` → `empresa.id`<br>`almacen_id` → `almacen.id` (misma empresa)<br>`variante_id` → `variante_producto.id` (misma empresa) |  |

## Producción y real del servicio

```mermaid
erDiagram
    almacen ||--o{ requerimiento : "almacen_id"
    documento_stock ||--o{ merma_produccion : "documento_baja_id"
    documento_stock ||--o{ produccion_documento : "documento_stock_id"
    minuta ||--o{ produccion : "minuta_id"
    minuta ||--o{ requerimiento : "minuta_id"
    minuta ||--o{ venta_servicio : "minuta_id"
    minuta_detalle ||--o{ consumo_plato : "minuta_detalle_id"
    operacion_servicio ||--o{ produccion : "operacion_servicio_id"
    operacion_servicio ||--o{ requerimiento : "operacion_servicio_id"
    produccion ||--o{ merma_produccion : "produccion_id"
    produccion ||--o{ produccion_documento : "produccion_id"
    producto_base ||--o{ requerimiento_detalle : "producto_base_id"
    receta_version ||--o{ merma_produccion : "receta_version_id"
    requerimiento ||--o{ requerimiento_detalle : "requerimiento_id"
    unidad_medida ||--o{ merma_produccion : "unidad_id"
    variante_producto ||--o{ merma_produccion : "variante_id"
```

| Tabla | PK | Llaves foráneas (columna → tabla.columna) | La referencian |
|---|---|---|---|
| `requerimiento` | `id` | `empresa_id` → `empresa.id`<br>`almacen_id` → `almacen.id` (misma empresa)<br>`minuta_id` → `minuta.id` (misma empresa)<br>`operacion_servicio_id` → `operacion_servicio.id` (misma empresa)<br>`aprobador_id` → `usuario.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) | `documento_stock`, `requerimiento_detalle` |
| `requerimiento_detalle` | `id` | `empresa_id` → `empresa.id`<br>`producto_base_id` → `producto_base.id` (misma empresa)<br>`requerimiento_id` → `requerimiento.id` (misma empresa) |  |
| `produccion` | `id` | `empresa_id` → `empresa.id`<br>`minuta_id` → `minuta.id` (misma empresa)<br>`operacion_servicio_id` → `operacion_servicio.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) | `merma_produccion`, `produccion_documento` |
| `produccion_documento` | `id` | `empresa_id` → `empresa.id`<br>`documento_stock_id` → `documento_stock.id` (misma empresa)<br>`produccion_id` → `produccion.id` (misma empresa) |  |
| `merma_produccion` | `id` | `empresa_id` → `empresa.id`<br>`documento_baja_id` → `documento_stock.id` (misma empresa)<br>`produccion_id` → `produccion.id` (misma empresa)<br>`receta_version_id` → `receta_version.id` (misma empresa)<br>`unidad_id` → `unidad_medida.id` (misma empresa)<br>`variante_id` → `variante_producto.id` (misma empresa) |  |
| `consumo_plato` | `id` | `empresa_id` → `empresa.id`<br>`minuta_detalle_id` → `minuta_detalle.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) |  |
| `venta_servicio` | `id` | `empresa_id` → `empresa.id`<br>`minuta_id` → `minuta.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) |  |

## Inventario físico

```mermaid
erDiagram
    almacen ||--o{ inventario : "almacen_id"
    documento_stock ||--o{ inventario_ajuste : "documento_stock_id"
    inventario ||--o{ inventario_detalle : "inventario_id"
    inventario_detalle ||--o{ inventario_ajuste : "inventario_detalle_id"
    variante_producto ||--o{ inventario_detalle : "variante_id"
```

| Tabla | PK | Llaves foráneas (columna → tabla.columna) | La referencian |
|---|---|---|---|
| `inventario` | `id` | `empresa_id` → `empresa.id`<br>`almacen_id` → `almacen.id` (misma empresa)<br>`autorizador_id` → `usuario.id` (misma empresa)<br>`revisor_id` → `usuario.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) | `inventario_detalle` |
| `inventario_detalle` | `id` | `empresa_id` → `empresa.id`<br>`inventario_id` → `inventario.id` (misma empresa)<br>`contado_por` → `usuario.id` (misma empresa)<br>`variante_id` → `variante_producto.id` (misma empresa) | `inventario_ajuste` |
| `inventario_ajuste` | `id` | `empresa_id` → `empresa.id`<br>`documento_stock_id` → `documento_stock.id` (misma empresa)<br>`inventario_detalle_id` → `inventario_detalle.id` (misma empresa)<br>`autorizador_id` → `usuario.id` (misma empresa) |  |

## Cierres, contratos y resultados

```mermaid
erDiagram
    cierre_diario ||--o{ cierre_validacion : "cierre_diario_id"
    cliente ||--o{ contrato : "cliente_id"
    contrato ||--o{ contrato_servicio : "contrato_id"
    operacion ||--o{ cierre_diario : "operacion_id"
    operacion ||--o{ contrato : "operacion_id"
    operacion ||--o{ periodo_mensual : "operacion_id"
    operacion_servicio ||--o{ contrato_servicio : "operacion_servicio_id"
    operacion_servicio ||--o{ gasto : "operacion_servicio_id"
    operacion_servicio ||--o{ ingreso_servicio : "operacion_servicio_id"
    periodo_mensual ||--o{ cierre_validacion : "periodo_id"
    periodo_mensual ||--o{ gasto : "periodo_id"
    periodo_mensual ||--o{ ingreso_servicio : "periodo_id"
```

| Tabla | PK | Llaves foráneas (columna → tabla.columna) | La referencian |
|---|---|---|---|
| `cierre_diario` | `id` | `empresa_id` → `empresa.id`<br>`operacion_id` → `operacion.id` (misma empresa)<br>`usuario_cierre_id` → `usuario.id` (misma empresa) | `cierre_validacion` |
| `cierre_validacion` | `id` | `empresa_id` → `empresa.id`<br>`cierre_diario_id` → `cierre_diario.id` (misma empresa)<br>`periodo_id` → `periodo_mensual.id` (misma empresa) |  |
| `periodo_mensual` | `id` | `empresa_id` → `empresa.id`<br>`operacion_id` → `operacion.id` (misma empresa)<br>`usuario_cierre_id` → `usuario.id` (misma empresa) | `cierre_validacion`, `gasto`, `ingreso_servicio` |
| `cliente` | `id` | `empresa_id` → `empresa.id` | `contrato` |
| `contrato` | `id` | `empresa_id` → `empresa.id`<br>`cliente_id` → `cliente.id` (misma empresa)<br>`operacion_id` → `operacion.id` (misma empresa) | `contrato_servicio` |
| `contrato_servicio` | `id` | `empresa_id` → `empresa.id`<br>`contrato_id` → `contrato.id` (misma empresa)<br>`operacion_servicio_id` → `operacion_servicio.id` (misma empresa) |  |
| `ingreso_servicio` | `id` | `empresa_id` → `empresa.id`<br>`operacion_servicio_id` → `operacion_servicio.id` (misma empresa)<br>`periodo_id` → `periodo_mensual.id` (misma empresa) |  |
| `gasto` | `id` | `empresa_id` → `empresa.id`<br>`operacion_servicio_id` → `operacion_servicio.id` (misma empresa)<br>`periodo_id` → `periodo_mensual.id` (misma empresa)<br>`usuario_id` → `usuario.id` (misma empresa) |  |

## Sincronización e integraciones

```mermaid
erDiagram
    central_documento ||--o{ central_movimiento : "documento_id"
    evento_recibido ||--o{ central_documento : "evento_id"
    operacion ||--o{ sincronizacion_evento : "operacion_id"
    sede_central ||--o{ central_cierre : "sede_id"
    sede_central ||--o{ central_documento : "sede_id"
    sede_central ||--o{ central_movimiento : "sede_id"
    sede_central ||--o{ evento_recibido : "sede_id"
```

| Tabla | PK | Llaves foráneas (columna → tabla.columna) | La referencian |
|---|---|---|---|
| `origen_sincronizacion` | `empresa_id` | `empresa_id` → `empresa.id` |  |
| `sincronizacion_evento` | `id` | `empresa_id` → `empresa.id`<br>`operacion_id` → `operacion.id` (misma empresa) |  |
| `sede_central` | `id` | `empresa_id` → `empresa.id` | `central_cierre`, `central_documento`, `central_movimiento`, `evento_recibido` |
| `evento_recibido` | `id` | `empresa_id` → `empresa.id`<br>`sede_id` → `sede_central.id` | `central_documento` |
| `rechazo_sincronizacion` | `id` | — |  |
| `central_documento` | `id` | `empresa_id` → `empresa.id`<br>`evento_id` → `evento_recibido.id`<br>`sede_id` → `sede_central.id` | `central_movimiento` |
| `central_movimiento` | `id` | `empresa_id` → `empresa.id`<br>`documento_id` → `central_documento.id`<br>`sede_id` → `sede_central.id` |  |
| `central_cierre` | `id` | `empresa_id` → `empresa.id`<br>`sede_id` → `sede_central.id` |  |
| `integracion_envio` | `id` | `empresa_id` → `empresa.id` |  |
