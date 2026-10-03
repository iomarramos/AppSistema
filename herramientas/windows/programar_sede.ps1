<#
.SYNOPSIS
  Programa en el servidor de una sede las tareas de continuidad de AppSistema (etapa 8):
  envío a la central cada 15 minutos y respaldo diario con conciliación, conservando N días.

.DESCRIPTION
  Ejecutar como administrador en el servidor de la sede. NO guarda claves ni credenciales:
  antes, el administrador define como variables de entorno del SISTEMA (setx /M ...):
    APPSISTEMA_CONEXION_PROPIETARIO  conexión del propietario de la base de la sede
    APPSISTEMA_CONEXION_CENTRAL      conexión a la central con el usuario de sincronización
    APPSISTEMA_CREDENCIAL_SEDE       credencial que entregó "registrar-sede" en la central
    APPSISTEMA_PG_BIN                (opcional) carpeta de pg_dump/pg_restore
  Las tareas corren con la cuenta SYSTEM, que lee esas variables.

.EXAMPLE
  .\programar_sede.ps1 -Instalador "C:\AppSistema\AppSistema.Instalador.exe" -Empresa SODEXO -CarpetaRespaldos "D:\respaldos"
#>
param(
  [Parameter(Mandatory = $true)] [string] $Instalador,
  [Parameter(Mandatory = $true)] [string] $Empresa,
  [Parameter(Mandatory = $true)] [string] $CarpetaRespaldos,
  [int] $MinutosEntreEnvios = 15,
  [string] $HoraRespaldo = "23:30",
  [int] $DiasConservados = 30
)
$ErrorActionPreference = "Stop"
if (-not (Test-Path $Instalador)) { throw "No existe $Instalador" }
foreach ($v in "APPSISTEMA_CONEXION_PROPIETARIO", "APPSISTEMA_CONEXION_CENTRAL", "APPSISTEMA_CREDENCIAL_SEDE") {
  if (-not [Environment]::GetEnvironmentVariable($v, "Machine")) { throw "Falta la variable de sistema $v (setx /M $v ...)" }
}
New-Item -ItemType Directory -Force -Path $CarpetaRespaldos | Out-Null

# Envío a la central: reenviar es seguro (la central no duplica); sin conexión, reintenta en la próxima corrida.
$envio = New-ScheduledTaskAction -Execute $Instalador -Argument "sincronizar $Empresa"
$cada = New-ScheduledTaskTrigger -Once -At (Get-Date) -RepetitionInterval (New-TimeSpan -Minutes $MinutosEntreEnvios)
Register-ScheduledTask -TaskName "AppSistema - Envio a la central" -Action $envio -Trigger $cada -User "SYSTEM" -RunLevel Highest -Force | Out-Null

# Respaldo diario (deja .dump y .dump.conciliacion) y limpieza de respaldos antiguos.
$comando = "`$f = Join-Path '$CarpetaRespaldos' ('sede_' + (Get-Date -Format 'yyyyMMdd') + '.dump'); " +
           "& '$Instalador' respaldar `$f; " +
           "Get-ChildItem '$CarpetaRespaldos' -Filter 'sede_*.dump*' | Where-Object { `$_.LastWriteTime -lt (Get-Date).AddDays(-$DiasConservados) } | Remove-Item"
$respaldo = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-NoProfile -ExecutionPolicy Bypass -Command `"$comando`""
$diario = New-ScheduledTaskTrigger -Daily -At $HoraRespaldo
Register-ScheduledTask -TaskName "AppSistema - Respaldo diario" -Action $respaldo -Trigger $diario -User "SYSTEM" -RunLevel Highest -Force | Out-Null

Write-Host "Tareas programadas: envio cada $MinutosEntreEnvios min y respaldo diario a las $HoraRespaldo en $CarpetaRespaldos ($DiasConservados dias)."
Write-Host "Revise el resultado con: $Instalador estado-sincronizacion $Empresa"
