#!/usr/bin/env bash
# Concurrencia real: conexiones independientes contra una base ya migrada y vacía.
# Uso: 02_concurrencia.sh <base>
set -u
DB="${1:?indique la base}"
DIR="$(cd "$(dirname "$0")" && pwd)"
P="psql -d $DB -X -q -v ON_ERROR_STOP=1"
fallos=0
chequear() { if [ "$2" = "$3" ]; then echo "  OK   $1"; else echo "  FALLA $1 (obtuve $2, esperaba $3)"; fallos=$((fallos+1)); fi; }

# Datos de arranque + apertura de 27 L a S/8 (documento 1)
arranque() {
  $P -o /dev/null <<'SQL'
INSERT INTO empresa(id,codigo,nombre) VALUES (1,'A','Empresa ejemplo');
INSERT INTO usuario(id,empresa_id,nombre,login,password_hash) VALUES (1,1,'Ejemplo','ejemplo','NO_ES_CREDENCIAL');
INSERT INTO operacion(id,empresa_id,codigo,nombre) VALUES (1,1,'ORC','Orcopampa');
INSERT INTO almacen(id,empresa_id,operacion_id,codigo,nombre) VALUES (1,1,1,'P','Principal');
INSERT INTO unidad_medida(id,empresa_id,codigo,nombre,dimension) VALUES (1,1,'L','Litro','volumen');
INSERT INTO producto_base(id,empresa_id,codigo,descripcion,unidad_base_id) VALUES (1,1,'ACE','Aceite',1);
INSERT INTO variante_producto(id,empresa_id,producto_base_id,codigo,descripcion_comercial,tipo_envase,contenido_base_por_envase_u6)
VALUES (1,1,1,'ACE4','Aceite 4 L','envase',4000000);
INSERT INTO documento_stock(id,empresa_id,almacen_id,numero,fecha,tipo,estado,usuario_id) VALUES (1,1,1,'1','2026-10-01','apertura','borrador',1);
INSERT INTO documento_stock_detalle(id,empresa_id,documento_id,variante_id,cantidad_base_u6,costo_unitario_base_u6,valor_u6)
VALUES (1,1,1,1,27000000,8000000,216000000);
UPDATE documento_stock SET estado='confirmado' WHERE id=1;
INSERT INTO movimiento_stock(empresa_id,documento_detalle_id,almacen_id,variante_id,fecha,secuencia,signo,cantidad_base_u6,costo_unitario_base_u6,valor_u6,usuario_id)
VALUES (1,1,1,1,'2026-10-01',1,1,27000000,8000000,216000000,1);
SQL
}
saldo() { $P -Atc "SELECT cantidad_base_u6 FROM saldo_stock WHERE variante_id=1"; }
conciliado() { $P -Atc "SELECT count(*) FROM v_conciliacion_saldo"; }

echo "T24  dos conexiones sacan 20 L cada una con 27 L disponibles"
arranque
( $P -v doc=100 -v cant=20000000 -v espera=2 -f "$DIR/_salida.sql" >/tmp/conc_A.out 2>&1; echo $? >/tmp/conc_A.rc ) &
sleep 0.7
( $P -v doc=101 -v cant=20000000 -v espera=0 -f "$DIR/_salida.sql" >/tmp/conc_B.out 2>&1; echo $? >/tmp/conc_B.rc ) &
wait
chequear "la primera salida confirma" "$(cat /tmp/conc_A.rc)" "0"
chequear "la segunda es rechazada" "$([ "$(cat /tmp/conc_B.rc)" != 0 ] && echo si)" "si"
chequear "motivo STOCK_INSUFICIENTE" "$(grep -c STOCK_INSUFICIENTE /tmp/conc_B.out)" "1"
chequear "saldo final 7 L" "$(saldo)" "7000000"
chequear "saldo concilia con el libro" "$(conciliado)" "0"

echo "Carrera: 10 sesiones simultaneas sacan 5 L cada una de 27 L (caben 5)"
# Base nueva para esta prueba (el libro es inmutable y no se limpia).
DB2="${DB}_carrera"
dropdb --if-exists "$DB2" && createdb "$DB2"
for f in "$DIR"/../migraciones/*.sql; do psql -d "$DB2" -X -q -v ON_ERROR_STOP=1 -f "$f" || exit 2; done
P="psql -d $DB2 -X -q -v ON_ERROR_STOP=1"
arranque
for i in $(seq 1 10); do
  ( $P -v doc=$((200+i)) -v cant=5000000 -v espera=0 -f "$DIR/_salida.sql" >/tmp/car_$i.out 2>&1; echo $? >/tmp/car_$i.rc ) &
done
wait
ok=0; for i in $(seq 1 10); do [ "$(cat /tmp/car_$i.rc)" = 0 ] && ok=$((ok+1)); done
chequear "exactamente 5 salidas confirmadas" "$ok" "5"
chequear "saldo final 2 L" "$(saldo)" "2000000"
chequear "saldo concilia con el libro" "$(conciliado)" "0"
dropdb "$DB2"

if [ "$fallos" -eq 0 ]; then echo "CONCURRENCIA: TODAS LAS PRUEBAS PASARON"; else echo "CONCURRENCIA: $fallos fallo(s)"; exit 1; fi
