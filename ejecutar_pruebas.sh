#!/usr/bin/env bash
# Ejecuta TODAS las pruebas del repositorio: base PostgreSQL, dominio VB.NET y capa de datos.
# Requisitos: PostgreSQL 16+ (usuario con CREATEDB, p. ej. via socket local) y .NET SDK 8.
set -euo pipefail
cd "$(dirname "$0")"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

echo "######## 1/3  Base de datos PostgreSQL"
database/postgresql/ejecutar_pruebas.sh

echo
echo "######## 2/3  Dominio VB.NET (sin base de datos)"
dotnet test tests/AppSistema.Dominio.Tests --nologo -v q

echo
echo "######## 3/3  Capa de datos VB.NET contra PostgreSQL"
APPSISTEMA_PG_PRUEBAS=1 dotnet test tests/AppSistema.Datos.Tests --nologo -v q

echo
echo "TODO OK"
