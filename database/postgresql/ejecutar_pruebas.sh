#!/usr/bin/env bash
# Crea bases nuevas, aplica las migraciones y ejecuta todas las pruebas.
# Requiere un PostgreSQL 16+ accesible con el usuario actual (variables PG* estándar).
# Uso: ./ejecutar_pruebas.sh
set -euo pipefail
cd "$(dirname "$0")"
DB1=appsistema_verificacion
DB2=appsistema_concurrencia

preparar() {
  dropdb --if-exists "$1" >/dev/null 2>&1
  createdb "$1"
  for f in migraciones/V*.sql; do
    psql -d "$1" -X -q -v ON_ERROR_STOP=1 -f "$f"
  done
}

echo "== Migraciones y verificación (reglas, protección, roles) =="
preparar "$DB1"
psql -d "$DB1" -X -v ON_ERROR_STOP=1 -f pruebas/01_verificacion.sql

echo
echo "== Concurrencia real =="
preparar "$DB2"
pruebas/02_concurrencia.sh "$DB2"

dropdb --if-exists "$DB1" "$DB2" >/dev/null 2>&1 || true
echo
echo "RESULTADO GLOBAL: OK"
