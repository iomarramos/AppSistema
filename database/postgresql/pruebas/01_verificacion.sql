-- Verificación del esquema PostgreSQL (V001–V003). Datos ficticios.
-- Ejecutar con: psql -v ON_ERROR_STOP=1 -f 01_verificacion.sql  (sobre una base recién migrada)
-- Cualquier aserción fallida aborta con error y código de salida distinto de cero.

\set ON_ERROR_STOP on
\set QUIET on
\o /dev/null

CREATE TEMP TABLE resultado(id serial, prueba text);
GRANT ALL ON resultado TO app_stock;
GRANT ALL ON SEQUENCE pg_temp.resultado_id_seq TO app_stock;

CREATE FUNCTION pg_temp.ok(p text) RETURNS void LANGUAGE sql AS
$$ INSERT INTO resultado(prueba) VALUES (p) $$;

CREATE FUNCTION pg_temp.afirmar(p_cond boolean, p_msg text) RETURNS void LANGUAGE plpgsql AS $$
BEGIN
  IF p_cond IS NOT TRUE THEN RAISE EXCEPTION 'AFIRMACION_FALLIDA: %', p_msg; END IF;
  PERFORM pg_temp.ok(p_msg);
END $$;

-- Ejecuta p_sql y exige que falle con un mensaje que contenga p_esperado.
CREATE FUNCTION pg_temp.debe_fallar(p_msg text, p_sql text, p_esperado text) RETURNS void LANGUAGE plpgsql AS $$
DECLARE v_fallo boolean := false; v_err text;
BEGIN
  BEGIN
    EXECUTE p_sql;
  EXCEPTION WHEN OTHERS THEN
    v_fallo := true; v_err := SQLERRM;
  END;
  IF NOT v_fallo THEN
    RAISE EXCEPTION 'NO_FALLO (%): %', p_msg, p_sql;
  END IF;
  IF position(p_esperado IN v_err) = 0 THEN
    RAISE EXCEPTION 'MENSAJE_INESPERADO (%): obtuve [%], esperaba [%]', p_msg, v_err, p_esperado;
  END IF;
  PERFORM pg_temp.ok(p_msg);
END $$;

-- Flujo correcto: borrador -> línea -> confirmar -> movimiento.
CREATE FUNCTION pg_temp.contabilizar(p_doc bigint, p_tipo text, p_fecha date, p_cant bigint,
                                     p_signo int, p_costo bigint DEFAULT 8000000, p_variante bigint DEFAULT 1)
RETURNS void LANGUAGE plpgsql AS $$
DECLARE v_valor bigint := p_cant * p_costo / 1000000;
BEGIN
  INSERT INTO documento_stock(id, empresa_id, almacen_id, numero, fecha, tipo, estado, usuario_id)
  VALUES (p_doc, 1, 1, p_doc::text, p_fecha, p_tipo, 'borrador', 1);
  INSERT INTO documento_stock_detalle(id, empresa_id, documento_id, variante_id, cantidad_base_u6, costo_unitario_base_u6, valor_u6)
  VALUES (p_doc, 1, p_doc, p_variante, p_cant, p_costo, v_valor);
  UPDATE documento_stock SET estado = 'confirmado' WHERE id = p_doc;
  INSERT INTO movimiento_stock(empresa_id, documento_detalle_id, almacen_id, variante_id, fecha, secuencia, signo,
                               cantidad_base_u6, costo_unitario_base_u6, valor_u6, usuario_id)
  VALUES (1, p_doc, 1, p_variante, p_fecha, p_doc, p_signo, p_cant, p_costo, v_valor, 1);
END $$;

-- ---------- Datos de arranque ----------
INSERT INTO empresa(id, codigo, nombre) VALUES (1,'A','Empresa ejemplo'),(2,'B','Otra empresa');
INSERT INTO usuario(id, empresa_id, nombre, login, password_hash) VALUES (1,1,'Ejemplo','ejemplo','NO_ES_CREDENCIAL');
INSERT INTO operacion(id, empresa_id, codigo, nombre) VALUES (1,1,'ORC','Orcopampa'),(2,2,'OTR','Otra');
INSERT INTO almacen(id, empresa_id, operacion_id, codigo, nombre) VALUES (1,1,1,'P','Principal');
INSERT INTO unidad_medida(id, empresa_id, codigo, nombre, dimension) VALUES (1,1,'L','Litro','volumen');
INSERT INTO producto_base(id, empresa_id, codigo, descripcion, unidad_base_id) VALUES (1,1,'ACE','Aceite vegetal',1);
INSERT INTO variante_producto(id, empresa_id, producto_base_id, codigo, descripcion_comercial, tipo_envase, contenido_base_por_envase_u6)
VALUES (1,1,1,'ACE4','Aceite A 4 L','envase',4000000), (2,1,1,'ACE5','Aceite B 5 L','envase',5000000);
INSERT INTO empaque_compra(id, empresa_id, variante_id, codigo, descripcion, envases_por_empaque)
VALUES (1,1,1,'CAJA','Caja 4 x 4 L',4);

-- Los ids explícitos no avanzan las secuencias: se reinician para que los INSERT sin id no choquen.
DO $$
DECLARE r record;
BEGIN
  FOR r IN SELECT c.table_name FROM information_schema.columns c
            WHERE c.table_schema='public' AND c.column_name='id' AND c.is_identity='YES' LOOP
    EXECUTE format('SELECT setval(pg_get_serial_sequence(%L,''id''), 1000)', r.table_name);
  END LOOP;
END $$;

-- ================= Pruebas de la guía (9) =================

-- 1. Conversión: caja de 4 envases de 4 L = 16 L; 2 cajas = 32 L; 5 L es otra variante.
SELECT pg_temp.afirmar((SELECT contenido_base_total_u6 FROM v_empaque_conversion WHERE codigo='CAJA') = 16000000,
       'T04 caja 4x4 L = 16 L');
SELECT pg_temp.afirmar(2 * (SELECT contenido_base_total_u6 FROM v_empaque_conversion WHERE codigo='CAJA') = 32000000,
       'T04 2 cajas = 32 L');
SELECT pg_temp.afirmar((SELECT count(DISTINCT id) FROM variante_producto WHERE producto_base_id=1) = 2,
       'T04 presentacion de 5 L es una variante distinta');

-- 2. Aislamiento de referencias entre empresas.
SELECT pg_temp.debe_fallar('T01 almacen de empresa 1 no enlaza operacion de empresa 2',
  $q$INSERT INTO almacen(empresa_id, operacion_id, codigo, nombre) VALUES (1,2,'X','Invalido')$q$, 'violates foreign key');

-- 3. Entrada y salida concilian stock, kárdex y libro.
SELECT pg_temp.contabilizar(1, 'apertura',          '2026-10-01', 32000000, 1);
SELECT pg_temp.contabilizar(2, 'salida_produccion', '2026-10-02',  5000000, -1);
SELECT pg_temp.afirmar((SELECT cantidad_base_u6 FROM saldo_stock) = 27000000
                   AND (SELECT valor_u6 FROM saldo_stock) = 216000000,
       'T19/T20 recibir 32 L a S/8 y sacar 5 L deja 27 L / S/216');
SELECT pg_temp.afirmar((SELECT saldo_cantidad_u6 FROM v_kardex ORDER BY secuencia DESC LIMIT 1) = 27000000
                   AND (SELECT saldo_valor_u6 FROM v_kardex ORDER BY secuencia DESC LIMIT 1) = 216000000,
       'kardex acumula 27 L / S/216');
SELECT pg_temp.afirmar((SELECT count(*) FROM v_conciliacion_saldo) = 0, 'saldo concilia con el libro');

-- 4. Un movimiento por línea.
SELECT pg_temp.debe_fallar('T21 movimiento duplicado bloqueado',
  $q$INSERT INTO movimiento_stock(empresa_id, documento_detalle_id, almacen_id, variante_id, fecha, secuencia, signo,
        cantidad_base_u6, costo_unitario_base_u6, valor_u6, usuario_id)
     SELECT empresa_id, documento_detalle_id, almacen_id, variante_id, fecha, 99, signo,
        cantidad_base_u6, costo_unitario_base_u6, valor_u6, usuario_id FROM movimiento_stock ORDER BY id LIMIT 1$q$,
  'duplicate key');

-- 5. Libro inmutable.
SELECT pg_temp.debe_fallar('movimiento no se actualiza', 'UPDATE movimiento_stock SET valor_u6=1', 'MOVIMIENTO_INMUTABLE');
SELECT pg_temp.debe_fallar('movimiento no se borra',     'DELETE FROM movimiento_stock',            'MOVIMIENTO_INMUTABLE');
SELECT pg_temp.debe_fallar('movimiento no se trunca',    'TRUNCATE movimiento_stock',               'MOVIMIENTO_INMUTABLE');

-- 6. Salida mayor que existencias.
SELECT pg_temp.debe_fallar('T24 salida de 50 L con 27 L disponibles',
  $q$SELECT pg_temp.contabilizar(3, 'salida_produccion', '2026-10-02', 50000000, -1)$q$, 'STOCK_INSUFICIENTE');
SELECT pg_temp.afirmar((SELECT cantidad_base_u6 FROM saldo_stock) = 27000000, 'saldo intacto tras salida rechazada');

-- 7. Día cerrado.
INSERT INTO cierre_diario(empresa_id, operacion_id, fecha, estado, usuario_cierre_id)
VALUES (1,1,'2026-10-02','cerrado',1);
SELECT pg_temp.debe_fallar('T39 dia cerrado bloquea contabilizacion',
  $q$SELECT pg_temp.contabilizar(4, 'apertura', '2026-10-02', 1000000, 1)$q$, 'DIA_CERRADO');

-- 7b. Mes cerrado.
INSERT INTO periodo_mensual(empresa_id, operacion_id, anio, mes, estado) VALUES (1,1,2026,9,'cerrado');
SELECT pg_temp.debe_fallar('T39 mes cerrado bloquea contabilizacion',
  $q$SELECT pg_temp.contabilizar(5, 'apertura', '2026-09-15', 1000000, 1)$q$, 'PERIODO_CERRADO');

-- 8. Conteo genera diferencia sin ajustar stock; celda vacía no es cero.
INSERT INTO inventario(id, empresa_id, almacen_id, numero, fecha_corte, tipo, usuario_id)
VALUES (1,1,1,'INV1','2026-10-02','general',1);
INSERT INTO inventario_detalle(id, empresa_id, inventario_id, variante_id, stock_sistema_u6, fisico_u6, costo_corte_u6)
VALUES (1,1,1,1,27000000,26000000,8000000), (2,1,1,2,0,NULL,0);
SELECT pg_temp.afirmar((SELECT diferencia_u6 FROM v_diferencias_inventario WHERE id=1) = -1000000
                   AND (SELECT resultado FROM v_diferencias_inventario WHERE id=1) = 'faltante',
       'T33 conteo 26 vs sistema 27 = -1 L (faltante)');
SELECT pg_temp.afirmar((SELECT resultado FROM v_diferencias_inventario WHERE id=2) = 'sin contar',
       'T34 celda vacia = sin contar, no cero');
SELECT pg_temp.afirmar((SELECT cantidad_base_u6 FROM saldo_stock) = 27000000, 'T33 el conteo no modifica el saldo');

-- 9. Integridad.
SELECT pg_temp.afirmar((SELECT count(*) FROM pg_constraint WHERE contype='f' AND NOT convalidated) = 0,
       'todas las claves foraneas estan validadas');

-- ================= Pruebas nuevas (brechas H01–H03 y reglas del plan) =================

-- H01: detalle de documento confirmado.
SELECT pg_temp.debe_fallar('H01 detalle confirmado no se edita',
  'UPDATE documento_stock_detalle SET cantidad_base_u6 = 1 WHERE id = 1', 'DOCUMENTO_CONFIRMADO');
SELECT pg_temp.debe_fallar('T25 detalle confirmado no se borra',
  'DELETE FROM documento_stock_detalle WHERE id = 1', 'DOCUMENTO_CONFIRMADO');
SELECT pg_temp.debe_fallar('T25 no se agregan lineas a un documento confirmado',
  $q$INSERT INTO documento_stock_detalle(id, empresa_id, documento_id, variante_id, cantidad_base_u6, costo_unitario_base_u6, valor_u6)
     VALUES (900,1,1,1,1000000,8000000,8000000)$q$, 'DOCUMENTO_CONFIRMADO');

-- H03: cabecera confirmada.
SELECT pg_temp.debe_fallar('H03 cabecera confirmada no cambia de fecha',
  $q$UPDATE documento_stock SET fecha = '2026-01-01' WHERE id = 1$q$, 'DOCUMENTO_CONFIRMADO');
SELECT pg_temp.debe_fallar('cabecera confirmada no se anula en sitio',
  $q$UPDATE documento_stock SET estado = 'anulado' WHERE id = 1$q$, 'DOCUMENTO_CONFIRMADO');
SELECT pg_temp.debe_fallar('cabecera confirmada no se borra',
  'DELETE FROM documento_stock WHERE id = 1', 'DOCUMENTO_CONFIRMADO');

-- H02: saldo.
SELECT pg_temp.debe_fallar('H02 saldo no se edita',
  'UPDATE saldo_stock SET cantidad_base_u6 = 999000000', 'SALDO_PROTEGIDO');
SELECT pg_temp.debe_fallar('saldo no se borra', 'DELETE FROM saldo_stock', 'SALDO_PROTEGIDO');
SELECT pg_temp.debe_fallar('saldo no se inserta a mano',
  'INSERT INTO saldo_stock(empresa_id, almacen_id, variante_id, cantidad_base_u6, valor_u6) VALUES (1,1,2,5000000,40000000)',
  'SALDO_PROTEGIDO');
SELECT pg_temp.debe_fallar('saldo no se trunca', 'TRUNCATE saldo_stock', 'SALDO_PROTEGIDO');
SELECT pg_temp.afirmar((SELECT cantidad_base_u6 FROM saldo_stock WHERE variante_id=1) = 27000000
                   AND (SELECT count(*) FROM v_conciliacion_saldo) = 0,
       'saldo sigue conciliado tras los intentos de edicion');

-- Documentos: nacen en borrador y no se confirman vacíos.
SELECT pg_temp.debe_fallar('documento no nace confirmado',
  $q$INSERT INTO documento_stock(id, empresa_id, almacen_id, numero, fecha, tipo, estado, usuario_id)
     VALUES (901,1,1,'901','2026-10-03','apertura','confirmado',1)$q$, 'DOCUMENTO_NACE_BORRADOR');
INSERT INTO documento_stock(id, empresa_id, almacen_id, numero, fecha, tipo, estado, usuario_id)
VALUES (902,1,1,'902','2026-10-03','apertura','borrador',1);
SELECT pg_temp.debe_fallar('documento sin lineas no se confirma',
  $q$UPDATE documento_stock SET estado='confirmado' WHERE id=902$q$, 'DOCUMENTO_VACIO');
DELETE FROM documento_stock WHERE id = 902;
SELECT pg_temp.afirmar(NOT EXISTS (SELECT 1 FROM documento_stock WHERE id = 902), 'un borrador si se puede borrar');

-- T26: signo permitido por tipo.
SELECT pg_temp.debe_fallar('T26 salida a produccion con signo positivo',
  $q$SELECT pg_temp.contabilizar(910, 'salida_produccion', '2026-10-03', 1000000, 1)$q$, 'SIGNO_NO_PERMITIDO');
SELECT pg_temp.debe_fallar('apertura con signo negativo',
  $q$SELECT pg_temp.contabilizar(911, 'apertura', '2026-10-03', 1000000, -1)$q$, 'SIGNO_NO_PERMITIDO');

-- T23: rollback total si una línea falla (dos movimientos en una transacción; el segundo no tiene stock).
DO $$
DECLARE v_antes bigint; v_despues bigint; v_docs_antes bigint; v_docs_despues bigint;
BEGIN
  SELECT cantidad_base_u6 INTO v_antes FROM saldo_stock WHERE variante_id = 1;
  SELECT count(*) INTO v_docs_antes FROM documento_stock;
  BEGIN
    PERFORM pg_temp.contabilizar(920, 'salida_produccion', '2026-10-03', 10000000, -1);  -- valida
    PERFORM pg_temp.contabilizar(921, 'salida_produccion', '2026-10-03', 40000000, -1);  -- falla
  EXCEPTION WHEN OTHERS THEN
    NULL;  -- el bloque BEGIN revierte ambas operaciones
  END;
  SELECT cantidad_base_u6 INTO v_despues FROM saldo_stock WHERE variante_id = 1;
  SELECT count(*) INTO v_docs_despues FROM documento_stock;
  PERFORM pg_temp.afirmar(v_antes = v_despues AND v_docs_antes = v_docs_despues,
          'T23 error en la 2.a linea revierte documento, movimiento y saldo');
END $$;

-- Múltiplo y mínimo de pedido.
INSERT INTO empaque_compra(id, empresa_id, variante_id, codigo, descripcion, envases_por_empaque, minimo_empaques, multiplo_empaques)
VALUES (2,1,2,'CAJA5','Caja 5 L',1,4,3);
INSERT INTO proveedor(id, empresa_id, codigo, nombre) VALUES (1,1,'P1','Proveedor ejemplo');
INSERT INTO pedido_compra(id, empresa_id, almacen_id, proveedor_id, numero, tipo, fecha, moneda, usuario_id)
VALUES (1,1,1,1,'PED1','normal','2026-10-02','PEN',1);
SELECT pg_temp.debe_fallar('pedido por debajo del minimo (3 < 4)',
  $q$INSERT INTO pedido_detalle(empresa_id, pedido_id, empaque_id, cantidad_empaques, factor_base_por_empaque_u6, cantidad_base_u6, precio_empaque_u6, fecha_entrega)
     VALUES (1,1,2,3,5000000,15000000,45000000,'2026-10-10')$q$, 'MULTIPLO_INCOMPATIBLE');
SELECT pg_temp.debe_fallar('pedido que no respeta el multiplo (5 no es multiplo de 3)',
  $q$INSERT INTO pedido_detalle(empresa_id, pedido_id, empaque_id, cantidad_empaques, factor_base_por_empaque_u6, cantidad_base_u6, precio_empaque_u6, fecha_entrega)
     VALUES (1,1,2,5,5000000,25000000,45000000,'2026-10-10')$q$, 'MULTIPLO_INCOMPATIBLE');
INSERT INTO pedido_detalle(empresa_id, pedido_id, empaque_id, cantidad_empaques, factor_base_por_empaque_u6, cantidad_base_u6, precio_empaque_u6, fecha_entrega)
VALUES (1,1,2,6,5000000,30000000,45000000,'2026-10-10');
SELECT pg_temp.ok('T14 pedido de 6 cajas (minimo 4, multiplo 3) aceptado');

-- Rol de la aplicación: puede contabilizar, no puede escribir el saldo ni alterar el libro.
SET ROLE app_stock;
SELECT pg_temp.contabilizar(930, 'salida_produccion', '2026-10-03', 2000000, -1);
SELECT pg_temp.debe_fallar('rol app no actualiza saldo_stock', 'UPDATE saldo_stock SET cantidad_base_u6 = 0', 'permission denied');
SELECT pg_temp.debe_fallar('rol app no actualiza movimiento_stock', 'UPDATE movimiento_stock SET valor_u6 = 0', 'permission denied');
SELECT pg_temp.debe_fallar('rol app no borra movimiento_stock', 'DELETE FROM movimiento_stock', 'permission denied');
SELECT pg_temp.debe_fallar('rol app no modifica auditoria',
  $q$INSERT INTO auditoria(empresa_id, tabla, registro_id, accion) VALUES (1,'x',1,'a'); UPDATE auditoria SET accion='b'$q$,
  'permission denied');
RESET ROLE;
SELECT pg_temp.afirmar((SELECT cantidad_base_u6 FROM saldo_stock WHERE variante_id=1) = 25000000
                   AND (SELECT count(*) FROM v_conciliacion_saldo) = 0,
       'el rol app contabiliza y el saldo se actualiza solo (27 - 2 = 25 L), conciliado');

\o
\unset QUIET
SELECT count(*) AS pruebas_ok FROM resultado;
\echo 'TODAS LAS PRUEBAS PASARON'
