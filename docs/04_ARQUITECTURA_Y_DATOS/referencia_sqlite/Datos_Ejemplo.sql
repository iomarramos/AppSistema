BEGIN TRANSACTION;
CREATE TABLE almacen (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  operacion_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  nombre TEXT NOT NULL,
  activo INTEGER NOT NULL DEFAULT 1,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,operacion_id) REFERENCES operacion(empresa_id,id),
  UNIQUE(empresa_id,operacion_id,codigo)
);
INSERT INTO "almacen" VALUES(1,1,1,'P','Principal',1,'2026-10-02 22:28:17');
CREATE TABLE auditoria (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  usuario_id INTEGER,
  tabla TEXT NOT NULL,
  registro_id INTEGER NOT NULL,
  accion TEXT NOT NULL,
  antes_json TEXT,
  despues_json TEXT,
  fecha TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,usuario_id) REFERENCES usuario(empresa_id,id)
);
CREATE TABLE categoria_producto (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  nombre TEXT NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,codigo)
);
CREATE TABLE cierre_diario (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  operacion_id INTEGER NOT NULL,
  fecha TEXT NOT NULL,
  estado TEXT NOT NULL DEFAULT 'abierto' CHECK(estado IN ('abierto','cerrado')),
  usuario_cierre_id INTEGER,
  fecha_cierre TEXT,
  estado_envio TEXT NOT NULL DEFAULT 'pendiente' CHECK(estado_envio IN ('pendiente','enviado','error')),
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,operacion_id) REFERENCES operacion(empresa_id,id),
  FOREIGN KEY(empresa_id,usuario_cierre_id) REFERENCES usuario(empresa_id,id),
  UNIQUE(empresa_id,operacion_id,fecha)
);
INSERT INTO "cierre_diario" VALUES(1,1,1,'2026-10-02','cerrado',1,NULL,'pendiente','2026-10-02 22:28:17');
CREATE TABLE cierre_validacion (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  periodo_id INTEGER,
  cierre_diario_id INTEGER,
  codigo TEXT NOT NULL,
  resultado TEXT NOT NULL,
  detalle TEXT,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,periodo_id) REFERENCES periodo_mensual(empresa_id,id),
  FOREIGN KEY(empresa_id,cierre_diario_id) REFERENCES cierre_diario(empresa_id,id),
  CHECK((periodo_id IS NOT NULL)+(cierre_diario_id IS NOT NULL)=1)
);
CREATE TABLE cliente (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  nombre TEXT NOT NULL,
  identificacion_fiscal TEXT,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,codigo)
);
CREATE TABLE contrato (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  cliente_id INTEGER NOT NULL,
  operacion_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  fecha_desde TEXT NOT NULL,
  fecha_hasta TEXT,
  moneda TEXT NOT NULL,
  condiciones TEXT,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,cliente_id) REFERENCES cliente(empresa_id,id),
  FOREIGN KEY(empresa_id,operacion_id) REFERENCES operacion(empresa_id,id),
  CHECK(fecha_hasta IS NULL OR fecha_hasta>=fecha_desde),
  UNIQUE(empresa_id,codigo)
);
CREATE TABLE contrato_servicio (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  contrato_id INTEGER NOT NULL,
  operacion_servicio_id INTEGER NOT NULL,
  importe_mensual_u6 INTEGER NOT NULL CHECK(importe_mensual_u6>=0),
  fecha_desde TEXT NOT NULL,
  fecha_hasta TEXT,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,contrato_id) REFERENCES contrato(empresa_id,id),
  FOREIGN KEY(empresa_id,operacion_servicio_id) REFERENCES operacion_servicio(empresa_id,id),
  CHECK(fecha_hasta IS NULL OR fecha_hasta>=fecha_desde)
);
CREATE TABLE costeo_ingrediente (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  minuta_detalle_id INTEGER NOT NULL,
  ingrediente_id INTEGER NOT NULL,
  costo_unitario_base_u6 INTEGER NOT NULL CHECK(costo_unitario_base_u6>=0),
  fuente_precio TEXT NOT NULL,
  fecha_precio TEXT NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,minuta_detalle_id) REFERENCES minuta_detalle(empresa_id,id),
  FOREIGN KEY(empresa_id,ingrediente_id) REFERENCES receta_ingrediente(empresa_id,id),
  UNIQUE(empresa_id,minuta_detalle_id,ingrediente_id)
);
INSERT INTO "costeo_ingrediente" VALUES(1,1,1,1,8000000,'Precio ficticio del ejemplo','2026-10-02','2026-10-02 22:28:17');
CREATE TABLE documento_stock (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  almacen_id INTEGER NOT NULL,
  almacen_destino_id INTEGER,
  requerimiento_id INTEGER,
  recepcion_id INTEGER,
  operacion_servicio_id INTEGER,
  numero TEXT NOT NULL,
  fecha TEXT NOT NULL,
  tipo TEXT NOT NULL CHECK(tipo IN ('recepcion','salida_produccion','devolucion_produccion','baja','traspaso_salida','traspaso_entrada','ajuste_positivo','ajuste_negativo','apertura','reversion')),
  estado TEXT NOT NULL DEFAULT 'borrador' CHECK(estado IN ('borrador','confirmado','anulado')),
  motivo TEXT,
  usuario_id INTEGER NOT NULL,
  aprobador_id INTEGER,
  documento_origen_id INTEGER,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,almacen_id) REFERENCES almacen(empresa_id,id),
  FOREIGN KEY(empresa_id,almacen_destino_id) REFERENCES almacen(empresa_id,id),
  FOREIGN KEY(empresa_id,requerimiento_id) REFERENCES requerimiento(empresa_id,id),
  FOREIGN KEY(empresa_id,recepcion_id) REFERENCES recepcion(empresa_id,id),
  FOREIGN KEY(empresa_id,operacion_servicio_id) REFERENCES operacion_servicio(empresa_id,id),
  FOREIGN KEY(empresa_id,usuario_id) REFERENCES usuario(empresa_id,id),
  FOREIGN KEY(empresa_id,aprobador_id) REFERENCES usuario(empresa_id,id),
  FOREIGN KEY(empresa_id,documento_origen_id) REFERENCES documento_stock(empresa_id,id),
  CHECK(almacen_destino_id IS NULL OR almacen_destino_id<>almacen_id),
  UNIQUE(empresa_id,numero)
);
INSERT INTO "documento_stock" VALUES(1,1,1,NULL,NULL,NULL,NULL,'1','2026-10-02','apertura','confirmado',NULL,1,NULL,NULL,'2026-10-02 22:28:17');
INSERT INTO "documento_stock" VALUES(2,1,1,NULL,NULL,NULL,NULL,'2','2026-10-02','salida_produccion','confirmado',NULL,1,NULL,NULL,'2026-10-02 22:28:17');
CREATE TABLE documento_stock_detalle (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  documento_id INTEGER NOT NULL,
  variante_id INTEGER NOT NULL,
  cantidad_base_u6 INTEGER NOT NULL CHECK(cantidad_base_u6>0),
  costo_unitario_base_u6 INTEGER NOT NULL CHECK(costo_unitario_base_u6>=0),
  valor_u6 INTEGER NOT NULL CHECK(valor_u6>=0),
  recepcion_detalle_id INTEGER,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,documento_id) REFERENCES documento_stock(empresa_id,id),
  FOREIGN KEY(empresa_id,variante_id) REFERENCES variante_producto(empresa_id,id),
  FOREIGN KEY(empresa_id,recepcion_detalle_id) REFERENCES recepcion_detalle(empresa_id,id)
);
INSERT INTO "documento_stock_detalle" VALUES(1,1,1,1,32000000,8000000,256000000,NULL,'2026-10-02 22:28:17');
INSERT INTO "documento_stock_detalle" VALUES(2,1,2,1,5000000,8000000,40000000,NULL,'2026-10-02 22:28:17');
CREATE TABLE empaque_compra (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  variante_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  descripcion TEXT NOT NULL,
  envases_por_empaque INTEGER NOT NULL CHECK(envases_por_empaque>0),
  minimo_empaques INTEGER NOT NULL DEFAULT 1 CHECK(minimo_empaques>0),
  multiplo_empaques INTEGER NOT NULL DEFAULT 1 CHECK(multiplo_empaques>0),
  activo INTEGER NOT NULL DEFAULT 1,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,variante_id) REFERENCES variante_producto(empresa_id,id),
  UNIQUE(empresa_id,variante_id,codigo)
);
INSERT INTO "empaque_compra" VALUES(1,1,1,'CAJA','Caja 4x4L',4,1,1,1,'2026-10-02 22:28:17');
CREATE TABLE empresa (id INTEGER PRIMARY KEY, codigo TEXT NOT NULL UNIQUE, nombre TEXT NOT NULL, identificacion_fiscal TEXT, moneda TEXT NOT NULL DEFAULT 'PEN', activo INTEGER NOT NULL DEFAULT 1 CHECK(activo IN (0,1)));
INSERT INTO "empresa" VALUES(1,'A','Ejemplo',NULL,'PEN',1);
INSERT INTO "empresa" VALUES(2,'B','Otra empresa',NULL,'PEN',1);
CREATE TABLE estructura_servicio (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  servicio_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  nombre TEXT NOT NULL,
  orden INTEGER NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,servicio_id) REFERENCES servicio(empresa_id,id),
  UNIQUE(empresa_id,servicio_id,codigo)
);
INSERT INTO "estructura_servicio" VALUES(1,1,1,'FON','Fondo',1,'2026-10-02 22:28:17');
CREATE TABLE gasto (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  periodo_id INTEGER NOT NULL,
  operacion_servicio_id INTEGER,
  concepto TEXT NOT NULL,
  categoria TEXT NOT NULL,
  cuenta_contable TEXT,
  importe_u6 INTEGER NOT NULL CHECK(importe_u6>=0),
  moneda TEXT NOT NULL,
  es_proyectado INTEGER NOT NULL DEFAULT 0,
  usuario_id INTEGER NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,periodo_id) REFERENCES periodo_mensual(empresa_id,id),
  FOREIGN KEY(empresa_id,operacion_servicio_id) REFERENCES operacion_servicio(empresa_id,id),
  FOREIGN KEY(empresa_id,usuario_id) REFERENCES usuario(empresa_id,id)
);
CREATE TABLE ingrediente_variante_permitida (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  ingrediente_id INTEGER NOT NULL,
  variante_id INTEGER NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,ingrediente_id) REFERENCES receta_ingrediente(empresa_id,id),
  FOREIGN KEY(empresa_id,variante_id) REFERENCES variante_producto(empresa_id,id),
  UNIQUE(empresa_id,ingrediente_id,variante_id)
);
CREATE TABLE ingreso_servicio (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  operacion_servicio_id INTEGER NOT NULL,
  periodo_id INTEGER NOT NULL,
  importe_neto_u6 INTEGER NOT NULL CHECK(importe_neto_u6>=0),
  ajustes_u6 INTEGER NOT NULL DEFAULT 0,
  moneda TEXT NOT NULL,
  fuente TEXT NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,operacion_servicio_id) REFERENCES operacion_servicio(empresa_id,id),
  FOREIGN KEY(empresa_id,periodo_id) REFERENCES periodo_mensual(empresa_id,id),
  UNIQUE(empresa_id,operacion_servicio_id,periodo_id)
);
CREATE TABLE integracion_envio (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  sistema_destino TEXT NOT NULL,
  clave_idempotencia TEXT NOT NULL,
  tipo TEXT NOT NULL,
  payload_json TEXT NOT NULL,
  estado TEXT NOT NULL DEFAULT 'pendiente',
  respuesta_json TEXT,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,sistema_destino,clave_idempotencia)
);
CREATE TABLE inventario (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  almacen_id INTEGER NOT NULL,
  numero TEXT NOT NULL,
  fecha_corte TEXT NOT NULL,
  tipo TEXT NOT NULL CHECK(tipo IN ('general','rotativo')),
  estado TEXT NOT NULL DEFAULT 'borrador' CHECK(estado IN ('borrador','contado','revisado','cerrado')),
  usuario_id INTEGER NOT NULL,
  revisor_id INTEGER,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,almacen_id) REFERENCES almacen(empresa_id,id),
  FOREIGN KEY(empresa_id,usuario_id) REFERENCES usuario(empresa_id,id),
  FOREIGN KEY(empresa_id,revisor_id) REFERENCES usuario(empresa_id,id),
  UNIQUE(empresa_id,numero)
);
INSERT INTO "inventario" VALUES(1,1,1,'INV1','2026-10-02','general','borrador',1,NULL,'2026-10-02 22:28:17');
CREATE TABLE inventario_ajuste (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  inventario_detalle_id INTEGER NOT NULL,
  documento_stock_id INTEGER NOT NULL,
  autorizador_id INTEGER NOT NULL,
  motivo TEXT NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,inventario_detalle_id) REFERENCES inventario_detalle(empresa_id,id),
  FOREIGN KEY(empresa_id,documento_stock_id) REFERENCES documento_stock(empresa_id,id),
  FOREIGN KEY(empresa_id,autorizador_id) REFERENCES usuario(empresa_id,id),
  UNIQUE(empresa_id,inventario_detalle_id)
);
CREATE TABLE inventario_detalle (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  inventario_id INTEGER NOT NULL,
  variante_id INTEGER NOT NULL,
  stock_sistema_u6 INTEGER NOT NULL CHECK(stock_sistema_u6>=0),
  fisico_u6 INTEGER CHECK(fisico_u6>=0),
  costo_corte_u6 INTEGER NOT NULL CHECK(costo_corte_u6>=0),
  observacion TEXT,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,inventario_id) REFERENCES inventario(empresa_id,id),
  FOREIGN KEY(empresa_id,variante_id) REFERENCES variante_producto(empresa_id,id),
  UNIQUE(empresa_id,inventario_id,variante_id)
);
INSERT INTO "inventario_detalle" VALUES(1,1,1,1,27000000,26000000,8000000,NULL,'2026-10-02 22:28:17');
CREATE TABLE marca (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  nombre TEXT NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,nombre)
);
CREATE TABLE merma_produccion (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  produccion_id INTEGER NOT NULL,
  variante_id INTEGER,
  receta_version_id INTEGER,
  etapa TEXT NOT NULL,
  cantidad_u6 INTEGER NOT NULL CHECK(cantidad_u6>0),
  unidad_id INTEGER NOT NULL,
  motivo TEXT NOT NULL,
  ya_incluida_consumo INTEGER NOT NULL DEFAULT 1 CHECK(ya_incluida_consumo IN (0,1)),
  documento_baja_id INTEGER,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,produccion_id) REFERENCES produccion(empresa_id,id),
  FOREIGN KEY(empresa_id,variante_id) REFERENCES variante_producto(empresa_id,id),
  FOREIGN KEY(empresa_id,receta_version_id) REFERENCES receta_version(empresa_id,id),
  FOREIGN KEY(empresa_id,unidad_id) REFERENCES unidad_medida(empresa_id,id),
  FOREIGN KEY(empresa_id,documento_baja_id) REFERENCES documento_stock(empresa_id,id),
  CHECK(variante_id IS NOT NULL OR receta_version_id IS NOT NULL)
);
CREATE TABLE minuta (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  operacion_servicio_id INTEGER NOT NULL,
  fecha TEXT NOT NULL,
  tipo TEXT NOT NULL CHECK(tipo IN ('teorica','real')),
  minuta_origen_id INTEGER,
  comensales INTEGER NOT NULL CHECK(comensales>=0),
  estado TEXT NOT NULL DEFAULT 'borrador' CHECK(estado IN ('borrador','aprobada','cerrada')),
  usuario_id INTEGER NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,operacion_servicio_id) REFERENCES operacion_servicio(empresa_id,id),
  FOREIGN KEY(empresa_id,minuta_origen_id) REFERENCES minuta(empresa_id,id),
  FOREIGN KEY(empresa_id,usuario_id) REFERENCES usuario(empresa_id,id),
  UNIQUE(empresa_id,operacion_servicio_id,fecha,tipo)
);
INSERT INTO "minuta" VALUES(1,1,1,'2026-10-03','teorica',NULL,150,'borrador',1,'2026-10-02 22:28:17');
CREATE TABLE minuta_detalle (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  minuta_id INTEGER NOT NULL,
  estructura_id INTEGER NOT NULL,
  receta_version_id INTEGER NOT NULL,
  raciones INTEGER NOT NULL CHECK(raciones>=0),
  costo_previsto_racion_u6 INTEGER CHECK(costo_previsto_racion_u6>=0),
  fecha_costeo TEXT,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,minuta_id) REFERENCES minuta(empresa_id,id),
  FOREIGN KEY(empresa_id,estructura_id) REFERENCES estructura_servicio(empresa_id,id),
  FOREIGN KEY(empresa_id,receta_version_id) REFERENCES receta_version(empresa_id,id)
);
INSERT INTO "minuta_detalle" VALUES(1,1,1,1,1,150,160000,'2026-10-02','2026-10-02 22:28:17');
CREATE TABLE minuta_estructura_fija (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  minuta_id INTEGER NOT NULL,
  producto_base_id INTEGER NOT NULL,
  cantidad_base_u6 INTEGER NOT NULL CHECK(cantidad_base_u6>=0),
  costo_previsto_unitario_u6 INTEGER CHECK(costo_previsto_unitario_u6>=0),
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,minuta_id) REFERENCES minuta(empresa_id,id),
  FOREIGN KEY(empresa_id,producto_base_id) REFERENCES producto_base(empresa_id,id)
);
CREATE TABLE movimiento_stock (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  documento_detalle_id INTEGER NOT NULL,
  almacen_id INTEGER NOT NULL,
  variante_id INTEGER NOT NULL,
  fecha TEXT NOT NULL,
  secuencia INTEGER NOT NULL CHECK(secuencia>0),
  signo INTEGER NOT NULL CHECK(signo IN (-1,1)),
  cantidad_base_u6 INTEGER NOT NULL CHECK(cantidad_base_u6>0),
  costo_unitario_base_u6 INTEGER NOT NULL CHECK(costo_unitario_base_u6>=0),
  valor_u6 INTEGER NOT NULL CHECK(valor_u6>=0),
  usuario_id INTEGER NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,documento_detalle_id) REFERENCES documento_stock_detalle(empresa_id,id),
  FOREIGN KEY(empresa_id,almacen_id) REFERENCES almacen(empresa_id,id),
  FOREIGN KEY(empresa_id,variante_id) REFERENCES variante_producto(empresa_id,id),
  FOREIGN KEY(empresa_id,usuario_id) REFERENCES usuario(empresa_id,id),
  UNIQUE(empresa_id,documento_detalle_id),
  UNIQUE(empresa_id,almacen_id,secuencia)
);
INSERT INTO "movimiento_stock" VALUES(1,1,1,1,1,'2026-10-02',1,1,32000000,8000000,256000000,1,'2026-10-02 22:28:17');
INSERT INTO "movimiento_stock" VALUES(2,1,2,1,1,'2026-10-02',2,-1,5000000,8000000,40000000,1,'2026-10-02 22:28:17');
CREATE TABLE operacion (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  nombre TEXT NOT NULL,
  ubicacion TEXT,
  zona TEXT,
  activo INTEGER NOT NULL DEFAULT 1,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,codigo)
);
INSERT INTO "operacion" VALUES(1,1,'ORC','Orcopampa',NULL,NULL,1,'2026-10-02 22:28:17');
INSERT INTO "operacion" VALUES(2,2,'OTR','Otra',NULL,NULL,1,'2026-10-02 22:28:17');
CREATE TABLE operacion_servicio (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  operacion_id INTEGER NOT NULL,
  servicio_id INTEGER NOT NULL,
  regimen_id INTEGER NOT NULL,
  costo_objetivo_racion_u6 INTEGER CHECK(costo_objetivo_racion_u6>=0),
  food_cost_objetivo_bp INTEGER CHECK(food_cost_objetivo_bp BETWEEN 0 AND 10000),
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,operacion_id) REFERENCES operacion(empresa_id,id),
  FOREIGN KEY(empresa_id,servicio_id) REFERENCES servicio(empresa_id,id),
  FOREIGN KEY(empresa_id,regimen_id) REFERENCES regimen(empresa_id,id),
  UNIQUE(empresa_id,operacion_id,servicio_id,regimen_id)
);
INSERT INTO "operacion_servicio" VALUES(1,1,1,1,1,5000000,NULL,'2026-10-02 22:28:17');
CREATE TABLE pedido_compra (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  almacen_id INTEGER NOT NULL,
  proveedor_id INTEGER NOT NULL,
  prevision_id INTEGER,
  numero TEXT NOT NULL,
  tipo TEXT NOT NULL CHECK(tipo IN ('normal','extra','caja_chica')),
  fecha TEXT NOT NULL,
  moneda TEXT NOT NULL,
  estado TEXT NOT NULL DEFAULT 'borrador' CHECK(estado IN ('borrador','aprobado','enviado','parcial','recibido','anulado')),
  usuario_id INTEGER NOT NULL,
  aprobador_id INTEGER,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,almacen_id) REFERENCES almacen(empresa_id,id),
  FOREIGN KEY(empresa_id,proveedor_id) REFERENCES proveedor(empresa_id,id),
  FOREIGN KEY(empresa_id,prevision_id) REFERENCES prevision(empresa_id,id),
  FOREIGN KEY(empresa_id,usuario_id) REFERENCES usuario(empresa_id,id),
  FOREIGN KEY(empresa_id,aprobador_id) REFERENCES usuario(empresa_id,id),
  UNIQUE(empresa_id,numero)
);
CREATE TABLE pedido_detalle (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  pedido_id INTEGER NOT NULL,
  empaque_id INTEGER NOT NULL,
  cantidad_empaques INTEGER NOT NULL CHECK(cantidad_empaques>0),
  factor_base_por_empaque_u6 INTEGER NOT NULL CHECK(factor_base_por_empaque_u6>0),
  cantidad_base_u6 INTEGER NOT NULL CHECK(cantidad_base_u6>0),
  precio_empaque_u6 INTEGER NOT NULL CHECK(precio_empaque_u6>=0),
  fecha_entrega TEXT NOT NULL,
  prevision_detalle_id INTEGER,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,pedido_id) REFERENCES pedido_compra(empresa_id,id),
  FOREIGN KEY(empresa_id,empaque_id) REFERENCES empaque_compra(empresa_id,id),
  FOREIGN KEY(empresa_id,prevision_detalle_id) REFERENCES prevision_detalle(empresa_id,id),
  CHECK(cantidad_base_u6=cantidad_empaques*factor_base_por_empaque_u6)
);
CREATE TABLE periodo_mensual (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  operacion_id INTEGER NOT NULL,
  anio INTEGER NOT NULL CHECK(anio BETWEEN 2000 AND 2200),
  mes INTEGER NOT NULL CHECK(mes BETWEEN 1 AND 12),
  estado TEXT NOT NULL DEFAULT 'abierto' CHECK(estado IN ('abierto','en_revision','cerrado')),
  metodo_valoracion TEXT,
  usuario_cierre_id INTEGER,
  fecha_cierre TEXT,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,operacion_id) REFERENCES operacion(empresa_id,id),
  FOREIGN KEY(empresa_id,usuario_cierre_id) REFERENCES usuario(empresa_id,id),
  UNIQUE(empresa_id,operacion_id,anio,mes)
);
CREATE TABLE permiso (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  descripcion TEXT,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,codigo)
);
CREATE TABLE politica_abastecimiento (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  almacen_id INTEGER NOT NULL,
  producto_base_id INTEGER NOT NULL,
  reserva_base_u6 INTEGER NOT NULL DEFAULT 0 CHECK(reserva_base_u6>=0),
  cobertura_dias INTEGER NOT NULL DEFAULT 0 CHECK(cobertura_dias>=0),
  plazo_entrega_dias INTEGER NOT NULL DEFAULT 0 CHECK(plazo_entrega_dias>=0),
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,almacen_id) REFERENCES almacen(empresa_id,id),
  FOREIGN KEY(empresa_id,producto_base_id) REFERENCES producto_base(empresa_id,id),
  UNIQUE(empresa_id,almacen_id,producto_base_id)
);
CREATE TABLE precio_compra (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  proveedor_empaque_id INTEGER NOT NULL,
  fecha_desde TEXT NOT NULL,
  fecha_hasta TEXT,
  moneda TEXT NOT NULL,
  precio_empaque_u6 INTEGER NOT NULL CHECK(precio_empaque_u6>=0),
  incluye_impuesto INTEGER NOT NULL DEFAULT 0,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,proveedor_empaque_id) REFERENCES proveedor_empaque(empresa_id,id),
  CHECK(fecha_hasta IS NULL OR fecha_hasta>=fecha_desde)
);
CREATE TABLE prevision (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  almacen_id INTEGER NOT NULL,
  fecha_desde TEXT NOT NULL,
  fecha_hasta TEXT NOT NULL,
  fecha_calculo TEXT NOT NULL,
  estado TEXT NOT NULL DEFAULT 'borrador' CHECK(estado IN ('borrador','validada','reemplazada')),
  usuario_id INTEGER NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,almacen_id) REFERENCES almacen(empresa_id,id),
  FOREIGN KEY(empresa_id,usuario_id) REFERENCES usuario(empresa_id,id),
  CHECK(fecha_hasta>=fecha_desde)
);
CREATE TABLE prevision_detalle (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  prevision_id INTEGER NOT NULL,
  producto_base_id INTEGER NOT NULL,
  necesidad_menu_u6 INTEGER NOT NULL CHECK(necesidad_menu_u6>=0),
  consumo_puente_u6 INTEGER NOT NULL DEFAULT 0 CHECK(consumo_puente_u6>=0),
  stock_utilizable_u6 INTEGER NOT NULL CHECK(stock_utilizable_u6>=0),
  reserva_u6 INTEGER NOT NULL DEFAULT 0 CHECK(reserva_u6>=0),
  pendiente_recibir_u6 INTEGER NOT NULL DEFAULT 0 CHECK(pendiente_recibir_u6>=0),
  necesidad_neta_u6 INTEGER NOT NULL CHECK(necesidad_neta_u6>=0),
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,prevision_id) REFERENCES prevision(empresa_id,id),
  FOREIGN KEY(empresa_id,producto_base_id) REFERENCES producto_base(empresa_id,id),
  UNIQUE(empresa_id,prevision_id,producto_base_id)
);
CREATE TABLE produccion (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  minuta_id INTEGER,
  operacion_servicio_id INTEGER NOT NULL,
  fecha TEXT NOT NULL,
  raciones_producidas INTEGER NOT NULL CHECK(raciones_producidas>=0),
  raciones_servidas INTEGER NOT NULL CHECK(raciones_servidas>=0),
  raciones_excedentes INTEGER NOT NULL CHECK(raciones_excedentes>=0),
  usuario_id INTEGER NOT NULL,
  observacion TEXT,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,minuta_id) REFERENCES minuta(empresa_id,id),
  FOREIGN KEY(empresa_id,operacion_servicio_id) REFERENCES operacion_servicio(empresa_id,id),
  FOREIGN KEY(empresa_id,usuario_id) REFERENCES usuario(empresa_id,id)
);
CREATE TABLE produccion_documento (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  produccion_id INTEGER NOT NULL,
  documento_stock_id INTEGER NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,produccion_id) REFERENCES produccion(empresa_id,id),
  FOREIGN KEY(empresa_id,documento_stock_id) REFERENCES documento_stock(empresa_id,id),
  UNIQUE(empresa_id,documento_stock_id)
);
CREATE TABLE producto_base (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  descripcion TEXT NOT NULL,
  especificacion TEXT,
  unidad_base_id INTEGER NOT NULL,
  categoria_id INTEGER,
  activo INTEGER NOT NULL DEFAULT 1,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,unidad_base_id) REFERENCES unidad_medida(empresa_id,id),
  FOREIGN KEY(empresa_id,categoria_id) REFERENCES categoria_producto(empresa_id,id),
  UNIQUE(empresa_id,codigo)
);
INSERT INTO "producto_base" VALUES(1,1,'ACE','Aceite',NULL,1,NULL,1,'2026-10-02 22:28:17');
CREATE TABLE proveedor (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  nombre TEXT NOT NULL,
  identificacion_fiscal TEXT,
  contacto TEXT,
  correo TEXT,
  telefono TEXT,
  es_caja_chica INTEGER NOT NULL DEFAULT 0,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,codigo)
);
CREATE TABLE proveedor_empaque (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  proveedor_id INTEGER NOT NULL,
  empaque_id INTEGER NOT NULL,
  plazo_entrega_dias INTEGER NOT NULL DEFAULT 0 CHECK(plazo_entrega_dias>=0),
  activo INTEGER NOT NULL DEFAULT 1,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,proveedor_id) REFERENCES proveedor(empresa_id,id),
  FOREIGN KEY(empresa_id,empaque_id) REFERENCES empaque_compra(empresa_id,id),
  UNIQUE(empresa_id,proveedor_id,empaque_id)
);
CREATE TABLE recepcion (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  almacen_id INTEGER NOT NULL,
  proveedor_id INTEGER NOT NULL,
  pedido_id INTEGER,
  numero TEXT NOT NULL,
  tipo_documento TEXT NOT NULL,
  numero_documento TEXT NOT NULL,
  fecha_documento TEXT NOT NULL,
  fecha_recepcion TEXT NOT NULL,
  moneda TEXT NOT NULL,
  tipo_ingreso TEXT NOT NULL CHECK(tipo_ingreso IN ('compra','caja_chica','traspaso','apertura')),
  estado TEXT NOT NULL DEFAULT 'borrador' CHECK(estado IN ('borrador','confirmada','anulada')),
  usuario_id INTEGER NOT NULL,
  observacion TEXT,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,almacen_id) REFERENCES almacen(empresa_id,id),
  FOREIGN KEY(empresa_id,proveedor_id) REFERENCES proveedor(empresa_id,id),
  FOREIGN KEY(empresa_id,pedido_id) REFERENCES pedido_compra(empresa_id,id),
  FOREIGN KEY(empresa_id,usuario_id) REFERENCES usuario(empresa_id,id),
  UNIQUE(empresa_id,numero)
);
CREATE TABLE recepcion_detalle (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  recepcion_id INTEGER NOT NULL,
  variante_id INTEGER NOT NULL,
  pedido_detalle_id INTEGER,
  empaque_id INTEGER,
  unidad_recibida TEXT NOT NULL,
  cantidad_recibida_u6 INTEGER NOT NULL CHECK(cantidad_recibida_u6>0),
  factor_conversion_u6 INTEGER NOT NULL CHECK(factor_conversion_u6>0),
  cantidad_base_u6 INTEGER NOT NULL CHECK(cantidad_base_u6>0),
  precio_unidad_recibida_u6 INTEGER NOT NULL CHECK(precio_unidad_recibida_u6>=0),
  descuento_u6 INTEGER NOT NULL DEFAULT 0 CHECK(descuento_u6>=0),
  impuesto_u6 INTEGER NOT NULL DEFAULT 0 CHECK(impuesto_u6>=0),
  cargos_u6 INTEGER NOT NULL DEFAULT 0 CHECK(cargos_u6>=0),
  costo_adquisicion_u6 INTEGER NOT NULL CHECK(costo_adquisicion_u6>=0),
  costo_unitario_base_u6 INTEGER NOT NULL CHECK(costo_unitario_base_u6>=0),
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,recepcion_id) REFERENCES recepcion(empresa_id,id),
  FOREIGN KEY(empresa_id,variante_id) REFERENCES variante_producto(empresa_id,id),
  FOREIGN KEY(empresa_id,pedido_detalle_id) REFERENCES pedido_detalle(empresa_id,id),
  FOREIGN KEY(empresa_id,empaque_id) REFERENCES empaque_compra(empresa_id,id)
);
CREATE TABLE receta (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  nombre TEXT NOT NULL,
  categoria TEXT,
  activo INTEGER NOT NULL DEFAULT 1,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,codigo)
);
INSERT INTO "receta" VALUES(1,1,'DEMO','Preparación demostrativa: componente aceite',NULL,1,'2026-10-02 22:28:17');
CREATE TABLE receta_ingrediente (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  receta_version_id INTEGER NOT NULL,
  producto_base_id INTEGER NOT NULL,
  cantidad_base_bruta_u6 INTEGER NOT NULL CHECK(cantidad_base_bruta_u6>0),
  cantidad_base_neta_u6 INTEGER CHECK(cantidad_base_neta_u6>=0),
  orden INTEGER NOT NULL DEFAULT 1,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,receta_version_id) REFERENCES receta_version(empresa_id,id),
  FOREIGN KEY(empresa_id,producto_base_id) REFERENCES producto_base(empresa_id,id),
  CHECK(cantidad_base_neta_u6 IS NULL OR cantidad_base_neta_u6<=cantidad_base_bruta_u6)
);
INSERT INTO "receta_ingrediente" VALUES(1,1,1,1,2000000,NULL,1,'2026-10-02 22:28:17');
CREATE TABLE receta_version (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  receta_id INTEGER NOT NULL,
  version INTEGER NOT NULL CHECK(version>0),
  rendimiento_raciones_u6 INTEGER NOT NULL CHECK(rendimiento_raciones_u6>0),
  instrucciones TEXT,
  estado TEXT NOT NULL DEFAULT 'borrador' CHECK(estado IN ('borrador','aprobada','retirada')),
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,receta_id) REFERENCES receta(empresa_id,id),
  UNIQUE(empresa_id,receta_id,version)
);
INSERT INTO "receta_version" VALUES(1,1,1,1,100000000,NULL,'aprobada','2026-10-02 22:28:17');
CREATE TABLE regimen (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  nombre TEXT NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,codigo)
);
INSERT INTO "regimen" VALUES(1,1,'GEN','General','2026-10-02 22:28:17');
CREATE TABLE requerimiento (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  almacen_id INTEGER NOT NULL,
  minuta_id INTEGER,
  operacion_servicio_id INTEGER NOT NULL,
  fecha TEXT NOT NULL,
  numero TEXT NOT NULL,
  estado TEXT NOT NULL DEFAULT 'borrador' CHECK(estado IN ('borrador','aprobado','atendido','anulado')),
  usuario_id INTEGER NOT NULL,
  aprobador_id INTEGER,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,almacen_id) REFERENCES almacen(empresa_id,id),
  FOREIGN KEY(empresa_id,minuta_id) REFERENCES minuta(empresa_id,id),
  FOREIGN KEY(empresa_id,operacion_servicio_id) REFERENCES operacion_servicio(empresa_id,id),
  FOREIGN KEY(empresa_id,usuario_id) REFERENCES usuario(empresa_id,id),
  FOREIGN KEY(empresa_id,aprobador_id) REFERENCES usuario(empresa_id,id),
  UNIQUE(empresa_id,numero)
);
CREATE TABLE requerimiento_detalle (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  requerimiento_id INTEGER NOT NULL,
  producto_base_id INTEGER NOT NULL,
  cantidad_prevista_u6 INTEGER NOT NULL CHECK(cantidad_prevista_u6>=0),
  cantidad_solicitada_u6 INTEGER NOT NULL CHECK(cantidad_solicitada_u6>=0),
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,requerimiento_id) REFERENCES requerimiento(empresa_id,id),
  FOREIGN KEY(empresa_id,producto_base_id) REFERENCES producto_base(empresa_id,id)
);
CREATE TABLE rol (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  nombre TEXT NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,codigo)
);
CREATE TABLE rol_permiso (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  rol_id INTEGER NOT NULL,
  permiso_id INTEGER NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,rol_id) REFERENCES rol(empresa_id,id),
  FOREIGN KEY(empresa_id,permiso_id) REFERENCES permiso(empresa_id,id),
  UNIQUE(empresa_id,rol_id,permiso_id)
);
CREATE TABLE saldo_stock (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  almacen_id INTEGER NOT NULL,
  variante_id INTEGER NOT NULL,
  cantidad_base_u6 INTEGER NOT NULL DEFAULT 0 CHECK(cantidad_base_u6>=0),
  valor_u6 INTEGER NOT NULL DEFAULT 0 CHECK(valor_u6>=0),
  version INTEGER NOT NULL DEFAULT 0,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,almacen_id) REFERENCES almacen(empresa_id,id),
  FOREIGN KEY(empresa_id,variante_id) REFERENCES variante_producto(empresa_id,id),
  UNIQUE(empresa_id,almacen_id,variante_id)
);
INSERT INTO "saldo_stock" VALUES(1,1,1,1,27000000,216000000,2,'2026-10-02 22:28:17');
CREATE TABLE servicio (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  nombre TEXT NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,codigo)
);
INSERT INTO "servicio" VALUES(1,1,'ALM','Almuerzo','2026-10-02 22:28:17');
CREATE TABLE sincronizacion_evento (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  uuid TEXT NOT NULL,
  operacion_id INTEGER NOT NULL,
  tipo TEXT NOT NULL,
  payload_json TEXT NOT NULL,
  version_origen INTEGER NOT NULL,
  estado TEXT NOT NULL DEFAULT 'pendiente' CHECK(estado IN ('pendiente','enviado','error','conflicto')),
  intentos INTEGER NOT NULL DEFAULT 0,
  ultimo_error TEXT,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,operacion_id) REFERENCES operacion(empresa_id,id),
  UNIQUE(empresa_id,uuid)
);
CREATE TABLE unidad_medida (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  codigo TEXT NOT NULL,
  nombre TEXT NOT NULL,
  dimension TEXT NOT NULL CHECK(dimension IN ('masa','volumen','conteo')),
  decimales INTEGER NOT NULL DEFAULT 3 CHECK(decimales BETWEEN 0 AND 6),
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,codigo)
);
INSERT INTO "unidad_medida" VALUES(1,1,'L','Litro','volumen',3,'2026-10-02 22:28:17');
CREATE TABLE usuario (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  nombre TEXT NOT NULL,
  login TEXT NOT NULL,
  password_hash TEXT NOT NULL,
  activo INTEGER NOT NULL DEFAULT 1 CHECK(activo IN (0,1)),
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  UNIQUE(empresa_id,login)
);
INSERT INTO "usuario" VALUES(1,1,'Ejemplo','ejemplo','NO_ES_CREDENCIAL',1,'2026-10-02 22:28:17');
CREATE TABLE usuario_operacion_rol (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  usuario_id INTEGER NOT NULL,
  operacion_id INTEGER NOT NULL,
  rol_id INTEGER NOT NULL,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,usuario_id) REFERENCES usuario(empresa_id,id),
  FOREIGN KEY(empresa_id,operacion_id) REFERENCES operacion(empresa_id,id),
  FOREIGN KEY(empresa_id,rol_id) REFERENCES rol(empresa_id,id),
  UNIQUE(empresa_id,usuario_id,operacion_id,rol_id)
);
CREATE TABLE variante_producto (
  id INTEGER PRIMARY KEY,
  empresa_id INTEGER NOT NULL,
  producto_base_id INTEGER NOT NULL,
  marca_id INTEGER,
  codigo TEXT NOT NULL,
  descripcion_comercial TEXT NOT NULL,
  tipo_envase TEXT NOT NULL,
  contenido_base_por_envase_u6 INTEGER NOT NULL CHECK(contenido_base_por_envase_u6>0),
  activo INTEGER NOT NULL DEFAULT 1,
  creado_en TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
  UNIQUE(empresa_id,id),
  FOREIGN KEY(empresa_id) REFERENCES empresa(id),
  FOREIGN KEY(empresa_id,producto_base_id) REFERENCES producto_base(empresa_id,id),
  FOREIGN KEY(empresa_id,marca_id) REFERENCES marca(empresa_id,id),
  UNIQUE(empresa_id,codigo)
);
INSERT INTO "variante_producto" VALUES(1,1,1,NULL,'ACE4','Aceite 4L','envase',4000000,1,'2026-10-02 22:28:17');
CREATE INDEX ix_usuario_empresa ON usuario(empresa_id);
CREATE INDEX ix_rol_empresa ON rol(empresa_id);
CREATE INDEX ix_permiso_empresa ON permiso(empresa_id);
CREATE INDEX ix_rol_permiso_empresa ON rol_permiso(empresa_id);
CREATE INDEX ix_operacion_empresa ON operacion(empresa_id);
CREATE INDEX ix_usuario_operacion_rol_empresa ON usuario_operacion_rol(empresa_id);
CREATE INDEX ix_almacen_empresa ON almacen(empresa_id);
CREATE INDEX ix_unidad_medida_empresa ON unidad_medida(empresa_id);
CREATE INDEX ix_categoria_producto_empresa ON categoria_producto(empresa_id);
CREATE INDEX ix_producto_base_empresa ON producto_base(empresa_id);
CREATE INDEX ix_marca_empresa ON marca(empresa_id);
CREATE INDEX ix_variante_producto_empresa ON variante_producto(empresa_id);
CREATE INDEX ix_empaque_compra_empresa ON empaque_compra(empresa_id);
CREATE INDEX ix_proveedor_empresa ON proveedor(empresa_id);
CREATE INDEX ix_proveedor_empaque_empresa ON proveedor_empaque(empresa_id);
CREATE INDEX ix_precio_compra_empresa ON precio_compra(empresa_id);
CREATE INDEX ix_politica_abastecimiento_empresa ON politica_abastecimiento(empresa_id);
CREATE INDEX ix_servicio_empresa ON servicio(empresa_id);
CREATE INDEX ix_regimen_empresa ON regimen(empresa_id);
CREATE INDEX ix_estructura_servicio_empresa ON estructura_servicio(empresa_id);
CREATE INDEX ix_operacion_servicio_empresa ON operacion_servicio(empresa_id);
CREATE INDEX ix_receta_empresa ON receta(empresa_id);
CREATE INDEX ix_receta_version_empresa ON receta_version(empresa_id);
CREATE INDEX ix_receta_ingrediente_empresa ON receta_ingrediente(empresa_id);
CREATE INDEX ix_ingrediente_variante_permitida_empresa ON ingrediente_variante_permitida(empresa_id);
CREATE INDEX ix_minuta_empresa ON minuta(empresa_id);
CREATE INDEX ix_minuta_detalle_empresa ON minuta_detalle(empresa_id);
CREATE INDEX ix_minuta_estructura_fija_empresa ON minuta_estructura_fija(empresa_id);
CREATE INDEX ix_costeo_ingrediente_empresa ON costeo_ingrediente(empresa_id);
CREATE INDEX ix_prevision_empresa ON prevision(empresa_id);
CREATE INDEX ix_prevision_detalle_empresa ON prevision_detalle(empresa_id);
CREATE INDEX ix_pedido_compra_empresa ON pedido_compra(empresa_id);
CREATE INDEX ix_pedido_detalle_empresa ON pedido_detalle(empresa_id);
CREATE INDEX ix_recepcion_empresa ON recepcion(empresa_id);
CREATE INDEX ix_recepcion_detalle_empresa ON recepcion_detalle(empresa_id);
CREATE INDEX ix_requerimiento_empresa ON requerimiento(empresa_id);
CREATE INDEX ix_requerimiento_detalle_empresa ON requerimiento_detalle(empresa_id);
CREATE INDEX ix_documento_stock_empresa ON documento_stock(empresa_id);
CREATE INDEX ix_documento_stock_detalle_empresa ON documento_stock_detalle(empresa_id);
CREATE INDEX ix_movimiento_stock_empresa ON movimiento_stock(empresa_id);
CREATE INDEX ix_saldo_stock_empresa ON saldo_stock(empresa_id);
CREATE INDEX ix_produccion_empresa ON produccion(empresa_id);
CREATE INDEX ix_produccion_documento_empresa ON produccion_documento(empresa_id);
CREATE INDEX ix_merma_produccion_empresa ON merma_produccion(empresa_id);
CREATE INDEX ix_inventario_empresa ON inventario(empresa_id);
CREATE INDEX ix_inventario_detalle_empresa ON inventario_detalle(empresa_id);
CREATE INDEX ix_inventario_ajuste_empresa ON inventario_ajuste(empresa_id);
CREATE INDEX ix_periodo_mensual_empresa ON periodo_mensual(empresa_id);
CREATE INDEX ix_cierre_diario_empresa ON cierre_diario(empresa_id);
CREATE INDEX ix_cierre_validacion_empresa ON cierre_validacion(empresa_id);
CREATE INDEX ix_ingreso_servicio_empresa ON ingreso_servicio(empresa_id);
CREATE INDEX ix_gasto_empresa ON gasto(empresa_id);
CREATE INDEX ix_cliente_empresa ON cliente(empresa_id);
CREATE INDEX ix_contrato_empresa ON contrato(empresa_id);
CREATE INDEX ix_contrato_servicio_empresa ON contrato_servicio(empresa_id);
CREATE INDEX ix_auditoria_empresa ON auditoria(empresa_id);
CREATE INDEX ix_sincronizacion_evento_empresa ON sincronizacion_evento(empresa_id);
CREATE INDEX ix_integracion_envio_empresa ON integracion_envio(empresa_id);
CREATE TRIGGER validar_recepcion_detalle_insert BEFORE INSERT ON recepcion_detalle WHEN NEW.empaque_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM empaque_compra e WHERE e.empresa_id=NEW.empresa_id AND e.id=NEW.empaque_id AND e.variante_id=NEW.variante_id) BEGIN SELECT RAISE(ABORT,'Presentacion o multiplo incompatible'); END;
CREATE TRIGGER validar_recepcion_detalle_update BEFORE UPDATE ON recepcion_detalle WHEN NEW.empaque_id IS NOT NULL AND NOT EXISTS (SELECT 1 FROM empaque_compra e WHERE e.empresa_id=NEW.empresa_id AND e.id=NEW.empaque_id AND e.variante_id=NEW.variante_id) BEGIN SELECT RAISE(ABORT,'Presentacion o multiplo incompatible'); END;
CREATE TRIGGER validar_pedido_detalle_insert BEFORE INSERT ON pedido_detalle WHEN NEW.cantidad_empaques % (SELECT multiplo_empaques FROM empaque_compra WHERE empresa_id=NEW.empresa_id AND id=NEW.empaque_id) <> 0 OR NEW.cantidad_empaques < (SELECT minimo_empaques FROM empaque_compra WHERE empresa_id=NEW.empresa_id AND id=NEW.empaque_id) BEGIN SELECT RAISE(ABORT,'Presentacion o multiplo incompatible'); END;
CREATE TRIGGER validar_pedido_detalle_update BEFORE UPDATE ON pedido_detalle WHEN NEW.cantidad_empaques % (SELECT multiplo_empaques FROM empaque_compra WHERE empresa_id=NEW.empresa_id AND id=NEW.empaque_id) <> 0 OR NEW.cantidad_empaques < (SELECT minimo_empaques FROM empaque_compra WHERE empresa_id=NEW.empresa_id AND id=NEW.empaque_id) BEGIN SELECT RAISE(ABORT,'Presentacion o multiplo incompatible'); END;
CREATE TRIGGER validar_variante_receta_insert BEFORE INSERT ON ingrediente_variante_permitida WHEN NOT EXISTS (SELECT 1 FROM receta_ingrediente i JOIN variante_producto v ON v.empresa_id=i.empresa_id AND v.producto_base_id=i.producto_base_id WHERE i.empresa_id=NEW.empresa_id AND i.id=NEW.ingrediente_id AND v.id=NEW.variante_id) BEGIN SELECT RAISE(ABORT,'Variante incompatible con ingrediente'); END;
CREATE TRIGGER validar_variante_receta_update BEFORE UPDATE ON ingrediente_variante_permitida WHEN NOT EXISTS (SELECT 1 FROM receta_ingrediente i JOIN variante_producto v ON v.empresa_id=i.empresa_id AND v.producto_base_id=i.producto_base_id WHERE i.empresa_id=NEW.empresa_id AND i.id=NEW.ingrediente_id AND v.id=NEW.variante_id) BEGIN SELECT RAISE(ABORT,'Variante incompatible con ingrediente'); END;
CREATE TRIGGER movimiento_inmutable_update BEFORE UPDATE ON movimiento_stock BEGIN SELECT RAISE(ABORT,'Corregir mediante movimiento de reversion'); END;
CREATE TRIGGER auditoria_inmutable_update BEFORE UPDATE ON auditoria BEGIN SELECT RAISE(ABORT,'Auditoria inmutable'); END;
CREATE TRIGGER movimiento_inmutable_delete BEFORE DELETE ON movimiento_stock BEGIN SELECT RAISE(ABORT,'Corregir mediante movimiento de reversion'); END;
CREATE TRIGGER auditoria_inmutable_delete BEFORE DELETE ON auditoria BEGIN SELECT RAISE(ABORT,'Auditoria inmutable'); END;
CREATE TRIGGER validar_movimiento BEFORE INSERT ON movimiento_stock BEGIN
 SELECT CASE WHEN NOT EXISTS (
 SELECT 1 FROM documento_stock_detalle l JOIN documento_stock d ON d.empresa_id=l.empresa_id AND d.id=l.documento_id
 WHERE l.empresa_id=NEW.empresa_id AND l.id=NEW.documento_detalle_id AND l.variante_id=NEW.variante_id
 AND d.almacen_id=NEW.almacen_id AND d.fecha=NEW.fecha AND d.estado='confirmado'
 AND l.cantidad_base_u6=NEW.cantidad_base_u6 AND l.valor_u6=NEW.valor_u6 AND l.costo_unitario_base_u6=NEW.costo_unitario_base_u6
 ) THEN RAISE(ABORT,'Movimiento no coincide con documento confirmado') END;
 SELECT CASE WHEN EXISTS (SELECT 1 FROM cierre_diario c JOIN almacen a ON a.empresa_id=c.empresa_id AND a.operacion_id=c.operacion_id WHERE a.empresa_id=NEW.empresa_id AND a.id=NEW.almacen_id AND c.fecha=NEW.fecha AND c.estado='cerrado') THEN RAISE(ABORT,'Dia cerrado') END;
 SELECT CASE WHEN EXISTS (SELECT 1 FROM periodo_mensual p JOIN almacen a ON a.empresa_id=p.empresa_id AND a.operacion_id=p.operacion_id WHERE a.empresa_id=NEW.empresa_id AND a.id=NEW.almacen_id AND p.anio=CAST(substr(NEW.fecha,1,4) AS INTEGER) AND p.mes=CAST(substr(NEW.fecha,6,2) AS INTEGER) AND p.estado='cerrado') THEN RAISE(ABORT,'Mes cerrado') END;
 SELECT CASE WHEN NEW.signo=-1 AND COALESCE((SELECT cantidad_base_u6 FROM saldo_stock WHERE empresa_id=NEW.empresa_id AND almacen_id=NEW.almacen_id AND variante_id=NEW.variante_id),0)<NEW.cantidad_base_u6 THEN RAISE(ABORT,'Stock insuficiente') END;
 SELECT CASE WHEN NEW.signo=-1 AND COALESCE((SELECT valor_u6 FROM saldo_stock WHERE empresa_id=NEW.empresa_id AND almacen_id=NEW.almacen_id AND variante_id=NEW.variante_id),0)<NEW.valor_u6 THEN RAISE(ABORT,'Valor de stock insuficiente') END;
 END;
CREATE TRIGGER actualizar_saldo AFTER INSERT ON movimiento_stock BEGIN
 INSERT INTO saldo_stock(empresa_id,almacen_id,variante_id,cantidad_base_u6,valor_u6,version)
 VALUES(NEW.empresa_id,NEW.almacen_id,NEW.variante_id,0,0,0)
 ON CONFLICT(empresa_id,almacen_id,variante_id) DO NOTHING;
 UPDATE saldo_stock SET cantidad_base_u6=cantidad_base_u6+NEW.signo*NEW.cantidad_base_u6,valor_u6=valor_u6+NEW.signo*NEW.valor_u6,version=version+1 WHERE empresa_id=NEW.empresa_id AND almacen_id=NEW.almacen_id AND variante_id=NEW.variante_id;
 END;
CREATE VIEW v_kardex AS SELECT m.*,p.codigo AS codigo_producto,p.descripcion AS producto,v.codigo AS codigo_variante,v.descripcion_comercial,u.codigo AS unidad,
 SUM(m.signo*m.cantidad_base_u6) OVER(PARTITION BY m.empresa_id,m.almacen_id,m.variante_id ORDER BY m.secuencia) AS saldo_cantidad_u6,
 SUM(m.signo*m.valor_u6) OVER(PARTITION BY m.empresa_id,m.almacen_id,m.variante_id ORDER BY m.secuencia) AS saldo_valor_u6
 FROM movimiento_stock m JOIN variante_producto v ON v.empresa_id=m.empresa_id AND v.id=m.variante_id JOIN producto_base p ON p.empresa_id=v.empresa_id AND p.id=v.producto_base_id JOIN unidad_medida u ON u.empresa_id=p.empresa_id AND u.id=p.unidad_base_id;
CREATE VIEW v_stock_producto AS SELECT s.empresa_id,s.almacen_id,v.producto_base_id,SUM(s.cantidad_base_u6) AS cantidad_base_u6,SUM(s.valor_u6) AS valor_u6 FROM saldo_stock s JOIN variante_producto v ON v.empresa_id=s.empresa_id AND v.id=s.variante_id GROUP BY s.empresa_id,s.almacen_id,v.producto_base_id;
CREATE VIEW v_diferencias_inventario AS SELECT i.*,i.fisico_u6-i.stock_sistema_u6 AS diferencia_u6,CASE WHEN i.fisico_u6 IS NULL THEN 'sin contar' WHEN i.fisico_u6>i.stock_sistema_u6 THEN 'sobrante' WHEN i.fisico_u6<i.stock_sistema_u6 THEN 'faltante' ELSE 'sin diferencia' END AS resultado FROM inventario_detalle i;
CREATE VIEW v_empaque_conversion AS SELECT e.*,v.contenido_base_por_envase_u6,e.envases_por_empaque*v.contenido_base_por_envase_u6 AS contenido_base_total_u6 FROM empaque_compra e JOIN variante_producto v ON v.empresa_id=e.empresa_id AND v.id=e.variante_id;
COMMIT;
