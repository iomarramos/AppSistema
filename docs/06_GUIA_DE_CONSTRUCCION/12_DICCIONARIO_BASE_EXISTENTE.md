# Diccionario extraído del esquema existente

Fuente: referencia/Esquema_Sistema.sql. Extracción automática mediante SQLite; documenta la estructura actual, no todas las reglas funcionales propuestas.

## `almacen`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| operacion_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| activo | INTEGER | Sí | — | 1 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | operacion | empresa_id |
| 0 | operacion_id | operacion | id |
| 1 | empresa_id | empresa | id |

## `auditoria`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| usuario_id | INTEGER | No | — | — |
| tabla | TEXT | Sí | — | — |
| registro_id | INTEGER | Sí | — | — |
| accion | TEXT | Sí | — | — |
| antes_json | TEXT | No | — | — |
| despues_json | TEXT | No | — | — |
| fecha | TEXT | Sí | — | CURRENT_TIMESTAMP |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | usuario_id | usuario | id |
| 1 | empresa_id | empresa | id |

## `categoria_producto`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `cierre_diario`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| operacion_id | INTEGER | Sí | — | — |
| fecha | TEXT | Sí | — | — |
| estado | TEXT | Sí | — | 'abierto' |
| usuario_cierre_id | INTEGER | No | — | — |
| fecha_cierre | TEXT | No | — | — |
| estado_envio | TEXT | Sí | — | 'pendiente' |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | usuario_cierre_id | usuario | id |
| 1 | empresa_id | operacion | empresa_id |
| 1 | operacion_id | operacion | id |
| 2 | empresa_id | empresa | id |

## `cierre_validacion`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| periodo_id | INTEGER | No | — | — |
| cierre_diario_id | INTEGER | No | — | — |
| codigo | TEXT | Sí | — | — |
| resultado | TEXT | Sí | — | — |
| detalle | TEXT | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | cierre_diario | empresa_id |
| 0 | cierre_diario_id | cierre_diario | id |
| 1 | empresa_id | periodo_mensual | empresa_id |
| 1 | periodo_id | periodo_mensual | id |
| 2 | empresa_id | empresa | id |

## `cliente`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| identificacion_fiscal | TEXT | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `contrato`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| cliente_id | INTEGER | Sí | — | — |
| operacion_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| fecha_desde | TEXT | Sí | — | — |
| fecha_hasta | TEXT | No | — | — |
| moneda | TEXT | Sí | — | — |
| condiciones | TEXT | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | operacion | empresa_id |
| 0 | operacion_id | operacion | id |
| 1 | empresa_id | cliente | empresa_id |
| 1 | cliente_id | cliente | id |
| 2 | empresa_id | empresa | id |

## `contrato_servicio`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| contrato_id | INTEGER | Sí | — | — |
| operacion_servicio_id | INTEGER | Sí | — | — |
| importe_mensual_u6 | INTEGER | Sí | — | — |
| fecha_desde | TEXT | Sí | — | — |
| fecha_hasta | TEXT | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | operacion_servicio | empresa_id |
| 0 | operacion_servicio_id | operacion_servicio | id |
| 1 | empresa_id | contrato | empresa_id |
| 1 | contrato_id | contrato | id |
| 2 | empresa_id | empresa | id |

## `costeo_ingrediente`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| minuta_detalle_id | INTEGER | Sí | — | — |
| ingrediente_id | INTEGER | Sí | — | — |
| costo_unitario_base_u6 | INTEGER | Sí | — | — |
| fuente_precio | TEXT | Sí | — | — |
| fecha_precio | TEXT | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | receta_ingrediente | empresa_id |
| 0 | ingrediente_id | receta_ingrediente | id |
| 1 | empresa_id | minuta_detalle | empresa_id |
| 1 | minuta_detalle_id | minuta_detalle | id |
| 2 | empresa_id | empresa | id |

## `documento_stock`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| almacen_id | INTEGER | Sí | — | — |
| almacen_destino_id | INTEGER | No | — | — |
| requerimiento_id | INTEGER | No | — | — |
| recepcion_id | INTEGER | No | — | — |
| operacion_servicio_id | INTEGER | No | — | — |
| numero | TEXT | Sí | — | — |
| fecha | TEXT | Sí | — | — |
| tipo | TEXT | Sí | — | — |
| estado | TEXT | Sí | — | 'borrador' |
| motivo | TEXT | No | — | — |
| usuario_id | INTEGER | Sí | — | — |
| aprobador_id | INTEGER | No | — | — |
| documento_origen_id | INTEGER | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | documento_stock | empresa_id |
| 0 | documento_origen_id | documento_stock | id |
| 1 | empresa_id | usuario | empresa_id |
| 1 | aprobador_id | usuario | id |
| 2 | empresa_id | usuario | empresa_id |
| 2 | usuario_id | usuario | id |
| 3 | empresa_id | operacion_servicio | empresa_id |
| 3 | operacion_servicio_id | operacion_servicio | id |
| 4 | empresa_id | recepcion | empresa_id |
| 4 | recepcion_id | recepcion | id |
| 5 | empresa_id | requerimiento | empresa_id |
| 5 | requerimiento_id | requerimiento | id |
| 6 | empresa_id | almacen | empresa_id |
| 6 | almacen_destino_id | almacen | id |
| 7 | empresa_id | almacen | empresa_id |
| 7 | almacen_id | almacen | id |
| 8 | empresa_id | empresa | id |

## `documento_stock_detalle`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| documento_id | INTEGER | Sí | — | — |
| variante_id | INTEGER | Sí | — | — |
| cantidad_base_u6 | INTEGER | Sí | — | — |
| costo_unitario_base_u6 | INTEGER | Sí | — | — |
| valor_u6 | INTEGER | Sí | — | — |
| recepcion_detalle_id | INTEGER | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | recepcion_detalle | empresa_id |
| 0 | recepcion_detalle_id | recepcion_detalle | id |
| 1 | empresa_id | variante_producto | empresa_id |
| 1 | variante_id | variante_producto | id |
| 2 | empresa_id | documento_stock | empresa_id |
| 2 | documento_id | documento_stock | id |
| 3 | empresa_id | empresa | id |

## `empaque_compra`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| variante_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| descripcion | TEXT | Sí | — | — |
| envases_por_empaque | INTEGER | Sí | — | — |
| minimo_empaques | INTEGER | Sí | — | 1 |
| multiplo_empaques | INTEGER | Sí | — | 1 |
| activo | INTEGER | Sí | — | 1 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | variante_producto | empresa_id |
| 0 | variante_id | variante_producto | id |
| 1 | empresa_id | empresa | id |

## `empresa`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| codigo | TEXT | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| identificacion_fiscal | TEXT | No | — | — |
| moneda | TEXT | Sí | — | 'PEN' |
| activo | INTEGER | Sí | — | 1 |

## `estructura_servicio`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| servicio_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| orden | INTEGER | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | servicio | empresa_id |
| 0 | servicio_id | servicio | id |
| 1 | empresa_id | empresa | id |

## `gasto`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| periodo_id | INTEGER | Sí | — | — |
| operacion_servicio_id | INTEGER | No | — | — |
| concepto | TEXT | Sí | — | — |
| categoria | TEXT | Sí | — | — |
| cuenta_contable | TEXT | No | — | — |
| importe_u6 | INTEGER | Sí | — | — |
| moneda | TEXT | Sí | — | — |
| es_proyectado | INTEGER | Sí | — | 0 |
| usuario_id | INTEGER | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | usuario_id | usuario | id |
| 1 | empresa_id | operacion_servicio | empresa_id |
| 1 | operacion_servicio_id | operacion_servicio | id |
| 2 | empresa_id | periodo_mensual | empresa_id |
| 2 | periodo_id | periodo_mensual | id |
| 3 | empresa_id | empresa | id |

## `ingrediente_variante_permitida`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| ingrediente_id | INTEGER | Sí | — | — |
| variante_id | INTEGER | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | variante_producto | empresa_id |
| 0 | variante_id | variante_producto | id |
| 1 | empresa_id | receta_ingrediente | empresa_id |
| 1 | ingrediente_id | receta_ingrediente | id |
| 2 | empresa_id | empresa | id |

## `ingreso_servicio`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| operacion_servicio_id | INTEGER | Sí | — | — |
| periodo_id | INTEGER | Sí | — | — |
| importe_neto_u6 | INTEGER | Sí | — | — |
| ajustes_u6 | INTEGER | Sí | — | 0 |
| moneda | TEXT | Sí | — | — |
| fuente | TEXT | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | periodo_mensual | empresa_id |
| 0 | periodo_id | periodo_mensual | id |
| 1 | empresa_id | operacion_servicio | empresa_id |
| 1 | operacion_servicio_id | operacion_servicio | id |
| 2 | empresa_id | empresa | id |

## `integracion_envio`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| sistema_destino | TEXT | Sí | — | — |
| clave_idempotencia | TEXT | Sí | — | — |
| tipo | TEXT | Sí | — | — |
| payload_json | TEXT | Sí | — | — |
| estado | TEXT | Sí | — | 'pendiente' |
| respuesta_json | TEXT | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `inventario`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| almacen_id | INTEGER | Sí | — | — |
| numero | TEXT | Sí | — | — |
| fecha_corte | TEXT | Sí | — | — |
| tipo | TEXT | Sí | — | — |
| estado | TEXT | Sí | — | 'borrador' |
| usuario_id | INTEGER | Sí | — | — |
| revisor_id | INTEGER | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | revisor_id | usuario | id |
| 1 | empresa_id | usuario | empresa_id |
| 1 | usuario_id | usuario | id |
| 2 | empresa_id | almacen | empresa_id |
| 2 | almacen_id | almacen | id |
| 3 | empresa_id | empresa | id |

## `inventario_ajuste`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| inventario_detalle_id | INTEGER | Sí | — | — |
| documento_stock_id | INTEGER | Sí | — | — |
| autorizador_id | INTEGER | Sí | — | — |
| motivo | TEXT | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | autorizador_id | usuario | id |
| 1 | empresa_id | documento_stock | empresa_id |
| 1 | documento_stock_id | documento_stock | id |
| 2 | empresa_id | inventario_detalle | empresa_id |
| 2 | inventario_detalle_id | inventario_detalle | id |
| 3 | empresa_id | empresa | id |

## `inventario_detalle`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| inventario_id | INTEGER | Sí | — | — |
| variante_id | INTEGER | Sí | — | — |
| stock_sistema_u6 | INTEGER | Sí | — | — |
| fisico_u6 | INTEGER | No | — | — |
| costo_corte_u6 | INTEGER | Sí | — | — |
| observacion | TEXT | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | variante_producto | empresa_id |
| 0 | variante_id | variante_producto | id |
| 1 | empresa_id | inventario | empresa_id |
| 1 | inventario_id | inventario | id |
| 2 | empresa_id | empresa | id |

## `marca`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `merma_produccion`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| produccion_id | INTEGER | Sí | — | — |
| variante_id | INTEGER | No | — | — |
| receta_version_id | INTEGER | No | — | — |
| etapa | TEXT | Sí | — | — |
| cantidad_u6 | INTEGER | Sí | — | — |
| unidad_id | INTEGER | Sí | — | — |
| motivo | TEXT | Sí | — | — |
| ya_incluida_consumo | INTEGER | Sí | — | 1 |
| documento_baja_id | INTEGER | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | documento_stock | empresa_id |
| 0 | documento_baja_id | documento_stock | id |
| 1 | empresa_id | unidad_medida | empresa_id |
| 1 | unidad_id | unidad_medida | id |
| 2 | empresa_id | receta_version | empresa_id |
| 2 | receta_version_id | receta_version | id |
| 3 | empresa_id | variante_producto | empresa_id |
| 3 | variante_id | variante_producto | id |
| 4 | empresa_id | produccion | empresa_id |
| 4 | produccion_id | produccion | id |
| 5 | empresa_id | empresa | id |

## `minuta`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| operacion_servicio_id | INTEGER | Sí | — | — |
| fecha | TEXT | Sí | — | — |
| tipo | TEXT | Sí | — | — |
| minuta_origen_id | INTEGER | No | — | — |
| comensales | INTEGER | Sí | — | — |
| estado | TEXT | Sí | — | 'borrador' |
| usuario_id | INTEGER | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | usuario_id | usuario | id |
| 1 | empresa_id | minuta | empresa_id |
| 1 | minuta_origen_id | minuta | id |
| 2 | empresa_id | operacion_servicio | empresa_id |
| 2 | operacion_servicio_id | operacion_servicio | id |
| 3 | empresa_id | empresa | id |

## `minuta_detalle`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| minuta_id | INTEGER | Sí | — | — |
| estructura_id | INTEGER | Sí | — | — |
| receta_version_id | INTEGER | Sí | — | — |
| raciones | INTEGER | Sí | — | — |
| costo_previsto_racion_u6 | INTEGER | No | — | — |
| fecha_costeo | TEXT | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | receta_version | empresa_id |
| 0 | receta_version_id | receta_version | id |
| 1 | empresa_id | estructura_servicio | empresa_id |
| 1 | estructura_id | estructura_servicio | id |
| 2 | empresa_id | minuta | empresa_id |
| 2 | minuta_id | minuta | id |
| 3 | empresa_id | empresa | id |

## `minuta_estructura_fija`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| minuta_id | INTEGER | Sí | — | — |
| producto_base_id | INTEGER | Sí | — | — |
| cantidad_base_u6 | INTEGER | Sí | — | — |
| costo_previsto_unitario_u6 | INTEGER | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | producto_base | empresa_id |
| 0 | producto_base_id | producto_base | id |
| 1 | empresa_id | minuta | empresa_id |
| 1 | minuta_id | minuta | id |
| 2 | empresa_id | empresa | id |

## `movimiento_stock`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| documento_detalle_id | INTEGER | Sí | — | — |
| almacen_id | INTEGER | Sí | — | — |
| variante_id | INTEGER | Sí | — | — |
| fecha | TEXT | Sí | — | — |
| secuencia | INTEGER | Sí | — | — |
| signo | INTEGER | Sí | — | — |
| cantidad_base_u6 | INTEGER | Sí | — | — |
| costo_unitario_base_u6 | INTEGER | Sí | — | — |
| valor_u6 | INTEGER | Sí | — | — |
| usuario_id | INTEGER | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | usuario_id | usuario | id |
| 1 | empresa_id | variante_producto | empresa_id |
| 1 | variante_id | variante_producto | id |
| 2 | empresa_id | almacen | empresa_id |
| 2 | almacen_id | almacen | id |
| 3 | empresa_id | documento_stock_detalle | empresa_id |
| 3 | documento_detalle_id | documento_stock_detalle | id |
| 4 | empresa_id | empresa | id |

## `operacion`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| ubicacion | TEXT | No | — | — |
| zona | TEXT | No | — | — |
| activo | INTEGER | Sí | — | 1 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `operacion_servicio`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| operacion_id | INTEGER | Sí | — | — |
| servicio_id | INTEGER | Sí | — | — |
| regimen_id | INTEGER | Sí | — | — |
| costo_objetivo_racion_u6 | INTEGER | No | — | — |
| food_cost_objetivo_bp | INTEGER | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | regimen | empresa_id |
| 0 | regimen_id | regimen | id |
| 1 | empresa_id | servicio | empresa_id |
| 1 | servicio_id | servicio | id |
| 2 | empresa_id | operacion | empresa_id |
| 2 | operacion_id | operacion | id |
| 3 | empresa_id | empresa | id |

## `pedido_compra`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| almacen_id | INTEGER | Sí | — | — |
| proveedor_id | INTEGER | Sí | — | — |
| prevision_id | INTEGER | No | — | — |
| numero | TEXT | Sí | — | — |
| tipo | TEXT | Sí | — | — |
| fecha | TEXT | Sí | — | — |
| moneda | TEXT | Sí | — | — |
| estado | TEXT | Sí | — | 'borrador' |
| usuario_id | INTEGER | Sí | — | — |
| aprobador_id | INTEGER | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | aprobador_id | usuario | id |
| 1 | empresa_id | usuario | empresa_id |
| 1 | usuario_id | usuario | id |
| 2 | empresa_id | prevision | empresa_id |
| 2 | prevision_id | prevision | id |
| 3 | empresa_id | proveedor | empresa_id |
| 3 | proveedor_id | proveedor | id |
| 4 | empresa_id | almacen | empresa_id |
| 4 | almacen_id | almacen | id |
| 5 | empresa_id | empresa | id |

## `pedido_detalle`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| pedido_id | INTEGER | Sí | — | — |
| empaque_id | INTEGER | Sí | — | — |
| cantidad_empaques | INTEGER | Sí | — | — |
| factor_base_por_empaque_u6 | INTEGER | Sí | — | — |
| cantidad_base_u6 | INTEGER | Sí | — | — |
| precio_empaque_u6 | INTEGER | Sí | — | — |
| fecha_entrega | TEXT | Sí | — | — |
| prevision_detalle_id | INTEGER | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | prevision_detalle | empresa_id |
| 0 | prevision_detalle_id | prevision_detalle | id |
| 1 | empresa_id | empaque_compra | empresa_id |
| 1 | empaque_id | empaque_compra | id |
| 2 | empresa_id | pedido_compra | empresa_id |
| 2 | pedido_id | pedido_compra | id |
| 3 | empresa_id | empresa | id |

## `periodo_mensual`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| operacion_id | INTEGER | Sí | — | — |
| anio | INTEGER | Sí | — | — |
| mes | INTEGER | Sí | — | — |
| estado | TEXT | Sí | — | 'abierto' |
| metodo_valoracion | TEXT | No | — | — |
| usuario_cierre_id | INTEGER | No | — | — |
| fecha_cierre | TEXT | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | usuario_cierre_id | usuario | id |
| 1 | empresa_id | operacion | empresa_id |
| 1 | operacion_id | operacion | id |
| 2 | empresa_id | empresa | id |

## `permiso`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| descripcion | TEXT | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `politica_abastecimiento`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| almacen_id | INTEGER | Sí | — | — |
| producto_base_id | INTEGER | Sí | — | — |
| reserva_base_u6 | INTEGER | Sí | — | 0 |
| cobertura_dias | INTEGER | Sí | — | 0 |
| plazo_entrega_dias | INTEGER | Sí | — | 0 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | producto_base | empresa_id |
| 0 | producto_base_id | producto_base | id |
| 1 | empresa_id | almacen | empresa_id |
| 1 | almacen_id | almacen | id |
| 2 | empresa_id | empresa | id |

## `precio_compra`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| proveedor_empaque_id | INTEGER | Sí | — | — |
| fecha_desde | TEXT | Sí | — | — |
| fecha_hasta | TEXT | No | — | — |
| moneda | TEXT | Sí | — | — |
| precio_empaque_u6 | INTEGER | Sí | — | — |
| incluye_impuesto | INTEGER | Sí | — | 0 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | proveedor_empaque | empresa_id |
| 0 | proveedor_empaque_id | proveedor_empaque | id |
| 1 | empresa_id | empresa | id |

## `prevision`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| almacen_id | INTEGER | Sí | — | — |
| fecha_desde | TEXT | Sí | — | — |
| fecha_hasta | TEXT | Sí | — | — |
| fecha_calculo | TEXT | Sí | — | — |
| estado | TEXT | Sí | — | 'borrador' |
| usuario_id | INTEGER | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | usuario_id | usuario | id |
| 1 | empresa_id | almacen | empresa_id |
| 1 | almacen_id | almacen | id |
| 2 | empresa_id | empresa | id |

## `prevision_detalle`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| prevision_id | INTEGER | Sí | — | — |
| producto_base_id | INTEGER | Sí | — | — |
| necesidad_menu_u6 | INTEGER | Sí | — | — |
| consumo_puente_u6 | INTEGER | Sí | — | 0 |
| stock_utilizable_u6 | INTEGER | Sí | — | — |
| reserva_u6 | INTEGER | Sí | — | 0 |
| pendiente_recibir_u6 | INTEGER | Sí | — | 0 |
| necesidad_neta_u6 | INTEGER | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | producto_base | empresa_id |
| 0 | producto_base_id | producto_base | id |
| 1 | empresa_id | prevision | empresa_id |
| 1 | prevision_id | prevision | id |
| 2 | empresa_id | empresa | id |

## `produccion`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| minuta_id | INTEGER | No | — | — |
| operacion_servicio_id | INTEGER | Sí | — | — |
| fecha | TEXT | Sí | — | — |
| raciones_producidas | INTEGER | Sí | — | — |
| raciones_servidas | INTEGER | Sí | — | — |
| raciones_excedentes | INTEGER | Sí | — | — |
| usuario_id | INTEGER | Sí | — | — |
| observacion | TEXT | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | usuario_id | usuario | id |
| 1 | empresa_id | operacion_servicio | empresa_id |
| 1 | operacion_servicio_id | operacion_servicio | id |
| 2 | empresa_id | minuta | empresa_id |
| 2 | minuta_id | minuta | id |
| 3 | empresa_id | empresa | id |

## `produccion_documento`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| produccion_id | INTEGER | Sí | — | — |
| documento_stock_id | INTEGER | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | documento_stock | empresa_id |
| 0 | documento_stock_id | documento_stock | id |
| 1 | empresa_id | produccion | empresa_id |
| 1 | produccion_id | produccion | id |
| 2 | empresa_id | empresa | id |

## `producto_base`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| descripcion | TEXT | Sí | — | — |
| especificacion | TEXT | No | — | — |
| unidad_base_id | INTEGER | Sí | — | — |
| categoria_id | INTEGER | No | — | — |
| activo | INTEGER | Sí | — | 1 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | categoria_producto | empresa_id |
| 0 | categoria_id | categoria_producto | id |
| 1 | empresa_id | unidad_medida | empresa_id |
| 1 | unidad_base_id | unidad_medida | id |
| 2 | empresa_id | empresa | id |

## `proveedor`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| identificacion_fiscal | TEXT | No | — | — |
| contacto | TEXT | No | — | — |
| correo | TEXT | No | — | — |
| telefono | TEXT | No | — | — |
| es_caja_chica | INTEGER | Sí | — | 0 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `proveedor_empaque`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| proveedor_id | INTEGER | Sí | — | — |
| empaque_id | INTEGER | Sí | — | — |
| plazo_entrega_dias | INTEGER | Sí | — | 0 |
| activo | INTEGER | Sí | — | 1 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empaque_compra | empresa_id |
| 0 | empaque_id | empaque_compra | id |
| 1 | empresa_id | proveedor | empresa_id |
| 1 | proveedor_id | proveedor | id |
| 2 | empresa_id | empresa | id |

## `recepcion`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| almacen_id | INTEGER | Sí | — | — |
| proveedor_id | INTEGER | Sí | — | — |
| pedido_id | INTEGER | No | — | — |
| numero | TEXT | Sí | — | — |
| tipo_documento | TEXT | Sí | — | — |
| numero_documento | TEXT | Sí | — | — |
| fecha_documento | TEXT | Sí | — | — |
| fecha_recepcion | TEXT | Sí | — | — |
| moneda | TEXT | Sí | — | — |
| tipo_ingreso | TEXT | Sí | — | — |
| estado | TEXT | Sí | — | 'borrador' |
| usuario_id | INTEGER | Sí | — | — |
| observacion | TEXT | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | usuario_id | usuario | id |
| 1 | empresa_id | pedido_compra | empresa_id |
| 1 | pedido_id | pedido_compra | id |
| 2 | empresa_id | proveedor | empresa_id |
| 2 | proveedor_id | proveedor | id |
| 3 | empresa_id | almacen | empresa_id |
| 3 | almacen_id | almacen | id |
| 4 | empresa_id | empresa | id |

## `recepcion_detalle`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| recepcion_id | INTEGER | Sí | — | — |
| variante_id | INTEGER | Sí | — | — |
| pedido_detalle_id | INTEGER | No | — | — |
| empaque_id | INTEGER | No | — | — |
| unidad_recibida | TEXT | Sí | — | — |
| cantidad_recibida_u6 | INTEGER | Sí | — | — |
| factor_conversion_u6 | INTEGER | Sí | — | — |
| cantidad_base_u6 | INTEGER | Sí | — | — |
| precio_unidad_recibida_u6 | INTEGER | Sí | — | — |
| descuento_u6 | INTEGER | Sí | — | 0 |
| impuesto_u6 | INTEGER | Sí | — | 0 |
| cargos_u6 | INTEGER | Sí | — | 0 |
| costo_adquisicion_u6 | INTEGER | Sí | — | — |
| costo_unitario_base_u6 | INTEGER | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empaque_compra | empresa_id |
| 0 | empaque_id | empaque_compra | id |
| 1 | empresa_id | pedido_detalle | empresa_id |
| 1 | pedido_detalle_id | pedido_detalle | id |
| 2 | empresa_id | variante_producto | empresa_id |
| 2 | variante_id | variante_producto | id |
| 3 | empresa_id | recepcion | empresa_id |
| 3 | recepcion_id | recepcion | id |
| 4 | empresa_id | empresa | id |

## `receta`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| categoria | TEXT | No | — | — |
| activo | INTEGER | Sí | — | 1 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `receta_ingrediente`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| receta_version_id | INTEGER | Sí | — | — |
| producto_base_id | INTEGER | Sí | — | — |
| cantidad_base_bruta_u6 | INTEGER | Sí | — | — |
| cantidad_base_neta_u6 | INTEGER | No | — | — |
| orden | INTEGER | Sí | — | 1 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | producto_base | empresa_id |
| 0 | producto_base_id | producto_base | id |
| 1 | empresa_id | receta_version | empresa_id |
| 1 | receta_version_id | receta_version | id |
| 2 | empresa_id | empresa | id |

## `receta_version`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| receta_id | INTEGER | Sí | — | — |
| version | INTEGER | Sí | — | — |
| rendimiento_raciones_u6 | INTEGER | Sí | — | — |
| instrucciones | TEXT | No | — | — |
| estado | TEXT | Sí | — | 'borrador' |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | receta | empresa_id |
| 0 | receta_id | receta | id |
| 1 | empresa_id | empresa | id |

## `regimen`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `requerimiento`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| almacen_id | INTEGER | Sí | — | — |
| minuta_id | INTEGER | No | — | — |
| operacion_servicio_id | INTEGER | Sí | — | — |
| fecha | TEXT | Sí | — | — |
| numero | TEXT | Sí | — | — |
| estado | TEXT | Sí | — | 'borrador' |
| usuario_id | INTEGER | Sí | — | — |
| aprobador_id | INTEGER | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | usuario | empresa_id |
| 0 | aprobador_id | usuario | id |
| 1 | empresa_id | usuario | empresa_id |
| 1 | usuario_id | usuario | id |
| 2 | empresa_id | operacion_servicio | empresa_id |
| 2 | operacion_servicio_id | operacion_servicio | id |
| 3 | empresa_id | minuta | empresa_id |
| 3 | minuta_id | minuta | id |
| 4 | empresa_id | almacen | empresa_id |
| 4 | almacen_id | almacen | id |
| 5 | empresa_id | empresa | id |

## `requerimiento_detalle`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| requerimiento_id | INTEGER | Sí | — | — |
| producto_base_id | INTEGER | Sí | — | — |
| cantidad_prevista_u6 | INTEGER | Sí | — | — |
| cantidad_solicitada_u6 | INTEGER | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | producto_base | empresa_id |
| 0 | producto_base_id | producto_base | id |
| 1 | empresa_id | requerimiento | empresa_id |
| 1 | requerimiento_id | requerimiento | id |
| 2 | empresa_id | empresa | id |

## `rol`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `rol_permiso`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| rol_id | INTEGER | Sí | — | — |
| permiso_id | INTEGER | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | permiso | empresa_id |
| 0 | permiso_id | permiso | id |
| 1 | empresa_id | rol | empresa_id |
| 1 | rol_id | rol | id |
| 2 | empresa_id | empresa | id |

## `saldo_stock`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| almacen_id | INTEGER | Sí | — | — |
| variante_id | INTEGER | Sí | — | — |
| cantidad_base_u6 | INTEGER | Sí | — | 0 |
| valor_u6 | INTEGER | Sí | — | 0 |
| version | INTEGER | Sí | — | 0 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | variante_producto | empresa_id |
| 0 | variante_id | variante_producto | id |
| 1 | empresa_id | almacen | empresa_id |
| 1 | almacen_id | almacen | id |
| 2 | empresa_id | empresa | id |

## `servicio`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `sincronizacion_evento`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| uuid | TEXT | Sí | — | — |
| operacion_id | INTEGER | Sí | — | — |
| tipo | TEXT | Sí | — | — |
| payload_json | TEXT | Sí | — | — |
| version_origen | INTEGER | Sí | — | — |
| estado | TEXT | Sí | — | 'pendiente' |
| intentos | INTEGER | Sí | — | 0 |
| ultimo_error | TEXT | No | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | operacion | empresa_id |
| 0 | operacion_id | operacion | id |
| 1 | empresa_id | empresa | id |

## `unidad_medida`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| codigo | TEXT | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| dimension | TEXT | Sí | — | — |
| decimales | INTEGER | Sí | — | 3 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `usuario`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| nombre | TEXT | Sí | — | — |
| login | TEXT | Sí | — | — |
| password_hash | TEXT | Sí | — | — |
| activo | INTEGER | Sí | — | 1 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | empresa | id |

## `usuario_operacion_rol`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| usuario_id | INTEGER | Sí | — | — |
| operacion_id | INTEGER | Sí | — | — |
| rol_id | INTEGER | Sí | — | — |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | rol | empresa_id |
| 0 | rol_id | rol | id |
| 1 | empresa_id | operacion | empresa_id |
| 1 | operacion_id | operacion | id |
| 2 | empresa_id | usuario | empresa_id |
| 2 | usuario_id | usuario | id |
| 3 | empresa_id | empresa | id |

## `variante_producto`

| Campo | Tipo | Obligatorio declarado | PK | Valor por defecto |
|---|---|---|---|---|
| id | INTEGER | No | 1 | — |
| empresa_id | INTEGER | Sí | — | — |
| producto_base_id | INTEGER | Sí | — | — |
| marca_id | INTEGER | No | — | — |
| codigo | TEXT | Sí | — | — |
| descripcion_comercial | TEXT | Sí | — | — |
| tipo_envase | TEXT | Sí | — | — |
| contenido_base_por_envase_u6 | INTEGER | Sí | — | — |
| activo | INTEGER | Sí | — | 1 |
| creado_en | TEXT | Sí | — | CURRENT_TIMESTAMP |

Referencias declaradas (las filas con igual ID forman la misma FK compuesta):

| ID | Campo | Tabla destino | Campo destino |
|---|---|---|---|
| 0 | empresa_id | marca | empresa_id |
| 0 | marca_id | marca | id |
| 1 | empresa_id | producto_base | empresa_id |
| 1 | producto_base_id | producto_base | id |
| 2 | empresa_id | empresa | id |

## Vistas y triggers

Consultar su SQL exacto para restricciones, índices UNIQUE y fórmulas; table_info no enumera todos los CHECK.

- trigger: `actualizar_saldo`
- trigger: `auditoria_inmutable_delete`
- trigger: `auditoria_inmutable_update`
- trigger: `movimiento_inmutable_delete`
- trigger: `movimiento_inmutable_update`
- trigger: `validar_movimiento`
- trigger: `validar_pedido_detalle_insert`
- trigger: `validar_pedido_detalle_update`
- trigger: `validar_recepcion_detalle_insert`
- trigger: `validar_recepcion_detalle_update`
- trigger: `validar_variante_receta_insert`
- trigger: `validar_variante_receta_update`
- view: `v_diferencias_inventario`
- view: `v_empaque_conversion`
- view: `v_kardex`
- view: `v_stock_producto`
