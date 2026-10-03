# AppSistema — guía para retomar el trabajo

Sistema de menús, compras, almacén, producción, inventarios, cierres y Food Cost que reemplaza al SGP de Sodexo.
**El usuario escribe en español; respóndale en español.**
Antes de cualquier tarea, lea:

* `docs/CONTINUAR.md`: estado, decisiones, pendientes y cómo seguir;
* `docs/SEGUIMIENTO.md`: registro detallado;
* `docs/CHECKLIST.md`: checklist de avance, pantallas, accesos y pendientes. **Actualícelo en cada entrega.**

## Pila y estructura

* VB.NET .NET 8: dominio, datos e instalador de consola. La aplicación de escritorio es **WinForms** (Windows 10+).
* PostgreSQL 16 con un servidor por sede.
* `src/AppSistema.Dominio`: cálculos puros. Cantidades y dinero son enteros ×1 000 000 (`_u6`, `EscalaU6`); nunca Double.
* `src/AppSistema.Datos`: servicios con sesión (`ServicioConSesion.EnTransaccion(permiso, …)`), con RLS por `app.empresa_id`. Los errores de la base llegan como `CODIGO: mensaje` y se convierten en `ReglaNegocioException`.
* `src/AppSistema.Escritorio`: formularios WinForms. Se usan `Ui.Mostrar(grid, lista, "Prop|Encabezado"…)` y `DialogoCampos`. Los botones que dependen de un permiso se crean con `Ui.BotonSi(permiso, …)`; nunca se ocultan por posición en la barra. Cada opción de menú lleva el permiso mínimo de la pantalla (`FormPrincipal.Agregar`).
* `src/AppSistema.Instalador`: comandos de consola (migrar, crear-empresa, importar-*, cargar-*, sincronizar, respaldar…).
* `database/postgresql/migraciones/V*.sql`:
  * van embebidas y el migrador verifica su hash, así que **nunca se edita una migración aplicada**: se crea `V0NN` nueva;
  * las tablas nuevas necesitan GRANT a `app_stock`, una política RLS y el trigger `auditar`.
* `datos/`: archivos del SGP recibidos (cada carpeta tiene `origen/` y `LEEME.md`). `datos/real/` es el juego de datos ordenado y cargable (`herramientas/ordenar_datos_reales.py`).

## Pruebas (obligatorias antes de cada push)

```bash
pg_ctlcluster 16 main start   # si PostgreSQL está detenido
export DOTNET_ROOT=/opt/dotnet-ms/usr/share/dotnet PATH=/opt/dotnet-ms/usr/share/dotnet:$PATH
./ejecutar_pruebas.sh         # SQL + concurrencia, dominio, integración, instalador de punta a punta, compilación WinForms
```

## Reglas de trabajo acordadas con el usuario

* **Git:**
  * rama de trabajo `claude/busy-mayer-9fxop6`, con el PR #1 (borrador) hacia `main`;
  * tras cada entrega con el CI verde, `develop` avanza por fast-forward al commit probado;
  * cada etapa tiene además su rama `feature/etapa-N-*`.
* **Mantener el repositorio actualizado hasta nuevo aviso:** cada cambio terminado se prueba, se hace commit (con las líneas de atribución de la sesión) y se hace push. Después se revisa el CI y se avanza `develop`.
* Los archivos que el usuario sube con "SUBIR AL REPOSITORIO Y TRABAJAR SOBRE ESTA INFORMACIÓN" se guardan en `datos/<tema>/origen/` tal como llegan. Luego se convierten con una herramienta en `herramientas/` y se documentan en un `LEEME.md`.
* "Continúa", "procede" o "continua con la programación" significan avanzar con lo siguiente del plan o de los pendientes de `docs/CONTINUAR.md`.
* No se inventan datos: sin precio no hay precio; lo dudoso va a una lista de revisión.
* No se incluyen secretos ni claves por defecto en el repositorio.
