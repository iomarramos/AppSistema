# Estado de la base de datos con los datos reales

Las llaves primarias y foráneas de cada tabla, con un diagrama por área, están en `docs/04_ARQUITECTURA_Y_DATOS/RELACIONES_BASE_DATOS.md`.

Actualizado: 2026-10-03. Es el resultado de cargar todo `datos/real/` en una base nueva con los pasos de `datos/real/LEEME.md` (migraciones V001–V019, 71 tablas).

Los productos usan códigos `PRDnnnnn` y los ingredientes `INGnnnnn`; ya no hay códigos con "SGP". El proveedor de los precios de referencia es `REF`.

## Qué hay

| Área | Contenido |
|---|---|
| Empresa y acceso | 1 empresa, 1 operación, 1 almacén; 5 roles base, 26 permisos |
| Unidades | KG, L, UND |
| Categorías | 14 familias, 26 subfamilias y 107 grupos (familia › subfamilia › grupo del SGP) |
| Ingredientes (producto base) | 3 203: 939 en KG, 253 en L y 2 011 en UND. 197 no tienen categoría |
| Productos (presentaciones `PRD`) | 4 158, cada uno con su contenido en la unidad del ingrediente; 1 516 con familia › subfamilia › grupo |
| Bulto de compra | 4 158 empaques de 1 unidad (sacos individuales; sin múltiplo se pide de a uno) |
| Precios (sin IGV) | 1 497 productos con precio, que cubren 975 ingredientes |
| Producto activo en la operación | 975 ingredientes, con el producto que se costea |
| Insumos sin costo | 1 (agua para receta) |
| Recetas | 946 aprobadas con 8 873 líneas de ingredientes; 643 con todos sus ingredientes con precio |
| Recetas por componente | Fondo 201, guarnición 180, entrada 105, bebida caliente 86, sopa 85, postre 76, sándwich 51, jugo 41, refresco 26, pan 22, complemento 19, salsa y ají 19, huevo 12, otros 10, fruta 9, cereal 2, untable 2 |
| Servicios y régimen | Desayuno, Almuerzo y Cena; régimen General |
| Estructura del servicio | 22 componentes con su factor de consumo: Desayuno 10, Almuerzo 7, Cena 5 |
| Minutas (ciclo propuesto) | 84 (28 días × 3 servicios, del 05/10 al 01/11/2026) con 700 platos, todas aprobadas con costo y venta |
| Inventario inicial | 379 productos, S/ 307 498,45 |
| Clientes, contratos y pedidos | 0 (se crean al operar) |

## Qué falta

| Falta | Detalle |
|---|---|
| **Menú del mes real** | Las 84 minutas son un ciclo propuesto a partir de las recetas. Falta el menú que la operación sirve de verdad. |
| **Estructura de servicios completa** | Hoy hay Desayuno, Almuerzo y Cena, con estructuras propuestas. Faltan **loncheras, refrigerios y coffee break**, y confirmar los componentes y factores de los tres actuales. El usuario los va a presentar. |
| Ingredientes de receta sin producto seguro | 85 (`datos/real/ingredientes_por_revisar.csv`). Por eso 303 de las 946 recetas no tienen costo completo. |
| Precios atípicos | 20 (`datos/real/precios_atipicos.csv`), entre ellos la margarina de 190 g. |
| Ingredientes sin categoría | 197. |
| Cereales y yogurt del desayuno | Ninguna receta con precio. |
| Clientes, contratos, gastos y presupuesto | Se cargan al operar o cuando el usuario los entregue. |

## Registros por tabla (carga completa de `datos/real/`)

| Área | Tabla | Registros | Qué guarda |
|---|---|---|---|
| Acceso | `empresa` | 1 | Empresa |
| Acceso | `operacion` | 1 | Operación (sede) |
| Acceso | `almacen` | 1 | Almacén |
| Acceso | `usuario` | 1 | Usuarios |
| Acceso | `rol` | 5 | Roles (ADMIN, SUPERVISOR, ALMACEN, COCINA, FINANZAS) |
| Acceso | `permiso` | 26 | Permisos |
| Acceso | `rol_permiso` | 67 | Permisos de cada rol |
| Acceso | `usuario_operacion_rol` | 1 | Rol de cada usuario en cada operación |
| Acceso | `auditoria` | 30 593 | Quién cambió qué y cuándo |
| Catálogo | `unidad_medida` | 3 | KG, L, UND |
| Catálogo | `categoria_producto` | 147 | Familias, subfamilias y grupos |
| Catálogo | `producto_base` | 3 203 | Ingredientes |
| Catálogo | `variante_producto` | 4 158 | Productos (presentaciones PRD) |
| Catálogo | `empaque_compra` | 4 158 | Bulto de compra de cada producto |
| Catálogo | `marca` | 0 | Marcas |
| Catálogo | `producto_operacion` | 975 | Producto activo de cada ingrediente en la operación |
| Compras | `proveedor` | 1 | Proveedores (REF = precios de referencia) |
| Compras | `proveedor_empaque` | 1 497 | Qué producto ofrece cada proveedor |
| Compras | `precio_compra` | 1 497 | Precios sin IGV con vigencia |
| Compras | `politica_abastecimiento` | 0 | Stock mínimo y reservas |
| Compras | `prevision`, `prevision_detalle` | 0 | Previsión de compras |
| Compras | `pedido_compra`, `pedido_detalle` | 0 | Pedidos |
| Compras | `recepcion`, `recepcion_detalle` | 0 | Recepciones |
| Menús | `receta` | 946 | Recetas |
| Menús | `receta_version` | 946 | Versiones aprobadas |
| Menús | `receta_ingrediente` | 8 873 | Ingredientes con gramaje |
| Menús | `ingrediente_variante_permitida` | 0 | Producto obligado en una receta |
| Menús | `servicio` | 3 | Desayuno, Almuerzo, Cena |
| Menús | `regimen` | 1 | General |
| Menús | `operacion_servicio` | 3 | Servicios que presta la operación |
| Menús | `estructura_servicio` | 22 | Componentes de cada servicio con su factor |
| Menús | `factor_consumo_operacion` | 0 | Factor ajustado por la operación |
| Menús | `minuta` | 84 | Minutas (ciclo propuesto de 28 días) |
| Menús | `minuta_detalle` | 700 | Platos de las minutas |
| Menús | `minuta_estructura_fija` | 0 | Productos fijos de la minuta |
| Menús | `costeo_ingrediente` | 4 829 | Costo de cada ingrediente al aprobar la minuta |
| Almacén | `documento_stock` | 1 | Documentos (apertura) |
| Almacén | `documento_stock_detalle` | 379 | Líneas del inventario inicial |
| Almacén | `movimiento_stock` | 379 | Kárdex |
| Almacén | `saldo_stock` | 379 | Stock por producto |
| Inventario | `inventario`, `inventario_detalle`, `inventario_ajuste` | 0 | Inventario físico |
| Producción | `requerimiento`, `requerimiento_detalle` | 0 | Requerimientos a almacén |
| Producción | `produccion`, `produccion_documento`, `merma_produccion` | 0 | Producción y mermas |
| Producción | `consumo_plato`, `venta_servicio` | 0 | Consumo y venta real |
| Cierres | `cierre_diario`, `cierre_validacion`, `periodo_mensual` | 0 | Cierres |
| Resultados | `cliente`, `contrato`, `contrato_servicio`, `ingreso_servicio`, `gasto` | 0 | Contratos, ingresos y gastos |
| Sincronización | `origen_sincronizacion`, `sincronizacion_evento`, `sede_central`, `evento_recibido`, `rechazo_sincronizacion`, `central_documento`, `central_movimiento`, `central_cierre`, `integracion_envio` | 0 | Envío a la central e integraciones |
| Sistema | `esquema_migracion` | 19 | Versiones de la base aplicadas |

Las tablas en 0 se llenan al operar: compras, recepción, producción, inventario físico, cierres y contratos.
