# AppSistema

Sistema de menús, compras e inventarios para empresas de servicios de alimentación.
VB.NET + WinForms (.NET 8) en **Windows 10 o superior**, con **PostgreSQL** como servidor de cada sede.

## Estructura

| Carpeta | Contenido |
|---|---|
| `src/AppSistema.Dominio` | Reglas puras: cantidades escaladas (u6), conversiones, empaques, previsión, permisos, lectura del CSV de catálogo |
| `src/AppSistema.Datos` | Acceso a PostgreSQL (Npgsql): sesión, acceso, catálogo, proveedores, importación, stock, migrador |
| `src/AppSistema.Escritorio` | Aplicación WinForms (acceso, catálogo, proveedores, importación, usuarios) |
| `src/AppSistema.Instalador` | Consola para instalar una sede: migraciones, primera empresa, usuario de sede |
| `database/postgresql` | Migraciones SQL y pruebas de la base (incluida concurrencia) |
| `tests/` | Pruebas xUnit de dominio e integración |
| `datos/sgp/` | Listado de productos del SGP (factor de conversión y unidad mínima de pedido) y su conversión al catálogo |
| `datos/recetas/` | Recetas del SGP (fichas revisadas y Recetón), normalizadas para importar, con ingredientes y observaciones |
| `datos/enlace/` | Enlace producto SGP → ingrediente: catálogo por ingrediente (productos SGP como variantes) y recetas enlazadas. **Flujo de carga recomendado** |
| `datos/inventario/` | Inventario inicial valorizado (379 productos, S/ 307 498,45) para el documento de apertura |
| `herramientas/` | Conversores de recetas y del enlace (Python, uso puntual) |
| `docs/` | Requerimientos, guía de construcción, seguimiento y flujo de ramas |

## Instalar una sede (servidor)

1. Instalar PostgreSQL 16 o superior en la PC servidor (Windows 10+) y crear una base vacía, p. ej. `appsistema`.
2. Con la cuenta propietaria de la base:
   ```
   set APPSISTEMA_CONEXION_PROPIETARIO=Host=localhost;Database=appsistema;Username=postgres;Password=...
   AppSistema.Instalador migrar
   AppSistema.Instalador crear-empresa
   AppSistema.Instalador crear-usuario-sede app_sede
   ```
   No existen usuarios ni claves por defecto: la clave del administrador se define en `crear-empresa`.
3. (Opcional) Cargar catálogo y recetas del SGP: `set APPSISTEMA_CONEXION=...app_sede...`, luego `AppSistema.Instalador importar-catalogo catalogo_por_ingrediente.csv` `AppSistema.Instalador importar-recetas recetas_enlazadas.csv --aprobar` y `AppSistema.Instalador importar-inventario inventario_inicial.csv` (detalle en [`datos/enlace/LEEME.md`](datos/enlace/LEEME.md)). También desde la aplicación: Catálogo > Importar y Menús > Importar recetas.
4. En cada computadora, abrir **AppSistema**, indicar servidor, base, `app_sede` y su clave (se guarda cifrada con DPAPI en `%PROGRAMDATA%\AppSistema\conexion.json`) e iniciar sesión con empresa, usuario y clave.

### Varias sedes y central (opcional)

Cada sede trabaja con su propio servidor: si se corta internet, las PC de la sede siguen operando. Lo confirmado (documentos de stock, cierres) queda en una cola dentro de la misma transacción y se envía a la central cuando hay conexión.

1. En la **central** (otra base con las mismas migraciones y la misma empresa): `AppSistema.Instalador registrar-sede EMPRESA SEDE "Nombre"` muestra la credencial de la sede **una sola vez**; `AppSistema.Instalador crear-usuario-sincronizacion app_sync` crea el usuario que solo puede entregar eventos.
2. En la **sede**: `AppSistema.Instalador configurar-sede EMPRESA SEDE` (anota también la historia ya confirmada).
3. Programar en la sede (Programador de tareas de Windows, p. ej. cada 15 min) `AppSistema.Instalador sincronizar EMPRESA` con `APPSISTEMA_CONEXION_CENTRAL` (usuario `app_sync`) y `APPSISTEMA_CREDENCIAL_SEDE`. Reenviar es seguro: la central no duplica.
4. Control: `estado-sincronizacion EMPRESA` (sede) y `reporte-central EMPRESA` (central: última sincronización y stock por sede).

**Respaldo:** `AppSistema.Instalador respaldar D:\respaldos\sede.dump` (deja además `sede.dump.conciliacion`). **Restauración** en una base nueva y vacía: `AppSistema.Instalador restaurar D:\respaldos\sede.dump`, que compara recuentos, saldos y referencias con el respaldo. Si `pg_dump`/`pg_restore` no están en el PATH, indique su carpeta en `APPSISTEMA_PG_BIN` (p. ej. `C:\Program Files\PostgreSQL\16\bin`).

### Contratos, gastos y resultado (opcional)

En **Cierres > Contratos y clientes** se registran clientes y contratos con el importe mensual de cada servicio; un cambio de tarifa se registra como *ajuste* desde una fecha. *Generar ingresos del mes* calcula el ingreso de cada servicio (prorrateado por días) sin reemplazar un ingreso registrado a mano. En **Cierres > Gastos y resultado mensual** se registran gastos (personal, operación, administración, otros) y se ve el margen por servicio; *Exportar CSV* entrega el resultado con el formato de [`docs/INTEGRACION_RESULTADOS.md`](docs/INTEGRACION_RESULTADOS.md). El administrador puede crear roles propios en **Administración > Usuarios y roles**.

Para actualizar a una versión nueva: `AppSistema.Instalador actualizar D:\respaldos\antes_de_actualizar.dump` (respalda, migra y comprueba que saldos e historia no cambiaron). En el servidor de sede, `herramientas\windows\programar_sede.ps1` programa el envío a la central y el respaldo diario.

## Pruebas

```bash
./ejecutar_pruebas.sh
```
Ejecuta: aserciones SQL y concurrencia real, pruebas de dominio, integración contra PostgreSQL, el instalador de punta a punta y la compilación de la aplicación de escritorio. Lo mismo corre en GitHub Actions (`.github/workflows/ci.yml`) en cada push y PR.

## Documentación

- Estado real y pendientes: [`docs/SEGUIMIENTO.md`](docs/SEGUIMIENTO.md)
- Requerimientos: [`docs/REQUERIMIENTOS.md`](docs/REQUERIMIENTOS.md)
- Plan y reglas de construcción: [`docs/guia_construccion/`](docs/guia_construccion/)
- Ramas: [`docs/FLUJO_DE_RAMAS.md`](docs/FLUJO_DE_RAMAS.md)
