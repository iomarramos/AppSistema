#!/usr/bin/env bash
# Ejecuta TODAS las pruebas: base PostgreSQL, dominio, capa de datos, instalador y compilación del escritorio.
# Requisitos: PostgreSQL 16+ (usuario con CREATEDB/CREATEROLE; variables PG* estándar) y .NET SDK 8.
# La aplicación WinForms se compila solo si el SDK incluye el soporte de escritorio (SDK oficial de Microsoft).
set -euo pipefail
cd "$(dirname "$0")"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
# Para la capa de datos se usan las mismas credenciales que psql.
export APPSISTEMA_PG_PRUEBAS=1
export APPSISTEMA_PG_HOST="${APPSISTEMA_PG_HOST:-${PGHOST:-/var/run/postgresql}}"
export APPSISTEMA_PG_USER="${APPSISTEMA_PG_USER:-${PGUSER:-$(whoami)}}"
export APPSISTEMA_PG_PASSWORD="${APPSISTEMA_PG_PASSWORD:-${PGPASSWORD:-}}"

echo "######## 1/5  Base de datos PostgreSQL"
database/postgresql/ejecutar_pruebas.sh

echo
echo "######## 2/5  Dominio VB.NET (sin base de datos)"
dotnet test tests/AppSistema.Dominio.Tests --nologo -v q

echo
echo "######## 3/5  Capa de datos VB.NET contra PostgreSQL"
dotnet test tests/AppSistema.Datos.Tests --nologo -v q

echo
echo "######## 4/5  Instalador de consola (migrar dos veces + crear empresa + catalogo, precios, recetas e inventario inicial dos veces + estructuras y ciclo de menu + sincronizacion con la central + respaldo, restauracion y actualizacion + exportacion de resultados)"
DB=appsistema_instalador
dropdb --if-exists "$DB" >/dev/null 2>&1 || true
createdb "$DB"
CLAVE=""; [ -n "$APPSISTEMA_PG_PASSWORD" ] && CLAVE=";Password=$APPSISTEMA_PG_PASSWORD"
export APPSISTEMA_CONEXION_PROPIETARIO="Host=$APPSISTEMA_PG_HOST;Username=$APPSISTEMA_PG_USER;Database=$DB$CLAVE"
dotnet run --project src/AppSistema.Instalador -v q -- migrar | tail -2
dotnet run --project src/AppSistema.Instalador -v q -- migrar | grep -c "ya estaba" | xargs -I{} echo "  segunda corrida: {} migraciones ya estaban"
printf 'DEMO\nEmpresa demo\nOP1\nOperacion demo\nALM\nAlmacen demo\nadmin\nAdministrador\nDemo-Clave-2026\nDemo-Clave-2026\n' \
  | dotnet run --project src/AppSistema.Instalador -v q -- crear-empresa | tail -1
export APPSISTEMA_CONEXION="Host=$APPSISTEMA_PG_HOST;Username=$APPSISTEMA_PG_USER;Database=$DB;Options=-c role=app_stock$CLAVE"
# Flujo recomendado: catálogo por ingrediente (productos SGP como variantes) y luego recetas enlazadas; dos veces.
for archivo in datos/enlace/catalogo_por_ingrediente.csv; do
  printf 'DEMO\nadmin\nDemo-Clave-2026\nSI\n' | dotnet run --project src/AppSistema.Instalador -v q -- importar-catalogo "$archivo" | tail -1
  printf 'DEMO\nadmin\nDemo-Clave-2026\n' | dotnet run --project src/AppSistema.Instalador -v q -- importar-catalogo "$archivo" | tail -1
done
# Datos reales ordenados (datos/real/): precios por presentacion, recetas con el ingrediente del catalogo, agua sin costo.
printf 'DEMO\nadmin\nDemo-Clave-2026\n' | dotnet run --project src/AppSistema.Instalador -v q -- importar-precios datos/real/precios_sgp.csv | tail -1
printf 'DEMO\nadmin\nDemo-Clave-2026\n' | dotnet run --project src/AppSistema.Instalador -v q -- importar-precios datos/real/precios_sgp.csv | tail -1
printf 'DEMO\nadmin\nDemo-Clave-2026\nSI\n' \
  | dotnet run --project src/AppSistema.Instalador -v q -- importar-recetas datos/real/recetas_reales.csv --aprobar | tail -1
printf 'DEMO\nadmin\nDemo-Clave-2026\n' \
  | dotnet run --project src/AppSistema.Instalador -v q -- importar-recetas datos/real/recetas_reales.csv --aprobar | tail -1
printf 'DEMO\nadmin\nDemo-Clave-2026\n' | dotnet run --project src/AppSistema.Instalador -v q -- marcar-sin-costo datos/real/insumos_sin_costo.csv | tail -1
# Familias del SGP (familia, subfamilia y grupo); la segunda corrida no cambia nada.
for vez in 1 2; do
  printf 'DEMO\nadmin\nDemo-Clave-2026\n' | dotnet run --project src/AppSistema.Instalador -v q -- cargar-familias datos/real/familias_sgp.csv 2>&1 | tail -1
done
# D02: producto activo por ingrediente en la operacion (su precio es el que se costea); la segunda corrida no cambia nada.
for vez in 1 2; do
  printf 'DEMO\nadmin\nDemo-Clave-2026\n' | dotnet run --project src/AppSistema.Instalador -v q -- liberar-productos datos/real/productos_activos.csv 2>&1 | tail -1
done
psql -d "$DB" -tAc "SELECT 'ingredientes: ' || count(DISTINCT p.id) || ', productos como variantes: ' || count(v.id) FROM producto_base p LEFT JOIN variante_producto v ON v.producto_base_id = p.id AND v.codigo LIKE 'PRD%'"
psql -d "$DB" -tAc "SELECT 'recetas aprobadas: ' || count(*) FROM receta_version WHERE estado = 'aprobada'"
printf 'DEMO\nadmin\nDemo-Clave-2026\n2026-10-01\nSI\n' \
  | dotnet run --project src/AppSistema.Instalador -v q -- importar-inventario datos/inventario/inventario_inicial.csv | tail -1
printf 'DEMO\nadmin\nDemo-Clave-2026\n2026-10-01\n' \
  | dotnet run --project src/AppSistema.Instalador -v q -- importar-inventario datos/inventario/inventario_inicial.csv 2>&1 | tail -1 || true
psql -d "$DB" -tAc "SELECT 'stock inicial: ' || count(*) || ' variantes, valor ' || round(sum(valor_u6) / 1000000.0, 2) FROM saldo_stock"
# Estructuras de menu y una semana del ciclo (aprobada: costo y venta previstos); la segunda corrida no duplica.
printf 'DEMO\nadmin\nDemo-Clave-2026\n' | dotnet run --project src/AppSistema.Instalador -v q -- cargar-estructuras datos/real/estructuras_menu.csv | tail -1
for vez in 1 2; do
  printf 'DEMO\nadmin\nDemo-Clave-2026\n' \
    | dotnet run --project src/AppSistema.Instalador -v q -- cargar-ciclo datos/real/ciclo_menu.csv 2026-10-05 500 500 300 --aprobar --dias 7 2>&1 | tail -1
done
psql -d "$DB" -tAc "SELECT 'minutas: ' || count(*) || ', con venta calculada: ' || count(venta_prevista_u6) || ', costo medio por comensal ' || round(avg(costo_previsto_u6 / 1e6 / comensales), 2) FROM minuta"
test "$(psql -d "$DB" -tAc "SELECT count(*) FROM minuta WHERE venta_prevista_u6 IS NULL")" = "0" && echo "  todas las minutas del ciclo tienen costo y venta"

echo "  -- continuidad: central, sede, sincronizacion repetida, respaldo y restauracion conciliada"
CENTRAL=appsistema_central; RESTAURADA=appsistema_restaurada; RESPALDO="$(mktemp -d)/sede.dump"
dropdb --if-exists "$CENTRAL" >/dev/null 2>&1 || true; dropdb --if-exists "$RESTAURADA" >/dev/null 2>&1 || true
createdb "$CENTRAL"; createdb "$RESTAURADA"
PROP_CENTRAL="Host=$APPSISTEMA_PG_HOST;Username=$APPSISTEMA_PG_USER;Database=$CENTRAL$CLAVE"
APPSISTEMA_CONEXION_PROPIETARIO="$PROP_CENTRAL" dotnet run --project src/AppSistema.Instalador -v q -- migrar | tail -1
printf 'DEMO\nEmpresa demo\nCEN\nCentral\nCEN\nAlmacen central\nadmin\nAdministrador\nDemo-Clave-2026\nDemo-Clave-2026\n' \
  | APPSISTEMA_CONEXION_PROPIETARIO="$PROP_CENTRAL" dotnet run --project src/AppSistema.Instalador -v q -- crear-empresa | tail -1
export APPSISTEMA_CREDENCIAL_SEDE="$(APPSISTEMA_CONEXION_PROPIETARIO="$PROP_CENTRAL" dotnet run --project src/AppSistema.Instalador -v q -- registrar-sede DEMO OP1 Sede demo | tail -1)"
dotnet run --project src/AppSistema.Instalador -v q -- configurar-sede DEMO OP1
export APPSISTEMA_CONEXION_CENTRAL="Host=$APPSISTEMA_PG_HOST;Username=$APPSISTEMA_PG_USER;Database=$CENTRAL;Options=-c role=app_sincronizacion$CLAVE"
dotnet run --project src/AppSistema.Instalador -v q -- sincronizar DEMO
dotnet run --project src/AppSistema.Instalador -v q -- sincronizar DEMO
APPSISTEMA_CONEXION_PROPIETARIO="$PROP_CENTRAL" dotnet run --project src/AppSistema.Instalador -v q -- reporte-central DEMO
test "$(psql -d "$DB" -tAc "SELECT sum(valor_u6) FROM saldo_stock")" = "$(psql -d "$CENTRAL" -tAc "SELECT sum(valor_u6) FROM v_central_saldo")" \
  && echo "  stock de la central = stock de la sede"
dotnet run --project src/AppSistema.Instalador -v q -- respaldar "$RESPALDO"
APPSISTEMA_CONEXION_PROPIETARIO="Host=$APPSISTEMA_PG_HOST;Username=$APPSISTEMA_PG_USER;Database=$RESTAURADA$CLAVE" \
  dotnet run --project src/AppSistema.Instalador -v q -- restaurar "$RESPALDO"
dotnet run --project src/AppSistema.Instalador -v q -- actualizar "$(dirname "$RESPALDO")/antes_de_actualizar.dump"
printf 'DEMO\nadmin\nDemo-Clave-2026\n' | dotnet run --project src/AppSistema.Instalador -v q -- exportar-resultados 2026-10 "$(dirname "$RESPALDO")/resultado.csv" | tail -1
head -1 "$(dirname "$RESPALDO")/resultado.csv" | grep -q '^.*version;empresa;operacion;periodo' && echo "  CSV de resultados con el encabezado del contrato v1"
unset APPSISTEMA_CREDENCIAL_SEDE APPSISTEMA_CONEXION_CENTRAL
rm -rf "$(dirname "$RESPALDO")"
dropdb "$DB"; dropdb "$CENTRAL"; dropdb "$RESTAURADA"

echo
echo "######## 4b/5  Asistente de instalacion (sin argumentos): base nueva, todos los datos y segunda corrida sin cambios"
ASIS=appsistema_asistente_pruebas
dropdb --if-exists "$ASIS" >/dev/null 2>&1 || true
# Respuestas del asistente en orden: servidor, puerto, administrador, clave, base, usuario de la base (y clave x2),
# empresa, operacion y almacen (Enter = valor por defecto), administrador (clave x2), dueno (clave x2), carpeta datos,
# cargar, clave del dueno, fecha del inventario y ciclo (Enter = valor por defecto).
LOG_ASISTENTE=$(mktemp)
# Va por tuberia directa: una variable con $(...) perderia las respuestas vacias del final (Enter del ciclo).
printf '%s\n' "${PGHOST:-localhost}" "${PGPORT:-5432}" "$APPSISTEMA_PG_USER" "$APPSISTEMA_PG_PASSWORD" "$ASIS" \
  "" "Clave-App-2026" "Clave-App-2026" "" "" "" "" "" "" "" "" "Clave-Admin-2026" "Clave-Admin-2026" \
  "dueno" "Dueno Prueba" "Clave-Dueno-2026" "Clave-Dueno-2026" "datos" "" "Clave-Dueno-2026" "2026-10-01" "" "" "" "" \
  | dotnet run --project src/AppSistema.Instalador -v q > "$LOG_ASISTENTE" 2>&1 \
  || { echo "El asistente fallo en la primera corrida:"; cat "$LOG_ASISTENTE"; exit 1; }
for vez in 1 2; do
  printf '%s\n' "${PGHOST:-localhost}" "${PGPORT:-5432}" "$APPSISTEMA_PG_USER" "$APPSISTEMA_PG_PASSWORD" "$ASIS" \
    "" "" "Clave-App-2026" "Clave-App-2026" "" "dueno" "Dueno Prueba" "" "" "Clave-Dueno-2026" "" "" "" "" \
    | dotnet run --project src/AppSistema.Instalador -v q > "$LOG_ASISTENTE" 2>&1 \
    || { echo "El asistente fallo en la corrida $vez:"; cat "$LOG_ASISTENTE"; exit 1; }
done
# Conteos esperados del juego real (ver docs/ESTADO_BASE_DATOS.md): la segunda corrida no duplica nada.
[ "$(psql -d "$ASIS" -tAc 'SELECT count(*) FROM variante_producto')" = "4158" ]
[ "$(psql -d "$ASIS" -tAc 'SELECT count(*) FROM receta')" = "946" ]
[ "$(psql -d "$ASIS" -tAc 'SELECT count(*) FROM movimiento_stock')" = "379" ]
[ "$(psql -d "$ASIS" -tAc 'SELECT count(*) FROM minuta')" = "84" ]
echo "Asistente: 4158 variantes, 946 recetas, 379 movimientos de apertura y 84 minutas; segunda corrida sin cambios."
dropdb "$ASIS"

echo
echo "######## 5/5  Aplicacion de escritorio WinForms y pruebas E2E (compilacion)"
SDK_DIR="$(dotnet --list-sdks | tail -1 | sed -E 's/^([^ ]+) \[(.*)\]$/\2\/\1/')"
if [ -d "$SDK_DIR/Sdks/Microsoft.NET.Sdk.WindowsDesktop" ]; then
  dotnet build src/AppSistema.Escritorio --nologo -v q -warnaserror | tail -3
  # Pruebas E2E con FlaUI: aquí solo se compilan; se ejecutan en Windows (job e2e-windows del CI).
  dotnet build tests/AppSistema.E2E.Tests --nologo -v q -warnaserror | tail -3
else
  echo "  OMITIDA: este SDK de .NET no incluye Microsoft.NET.Sdk.WindowsDesktop (use el SDK oficial de Microsoft)."
fi

echo
echo "TODO OK"
