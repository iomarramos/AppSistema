# Base de datos PostgreSQL

Portado del esquema SQLite v0.3 (59 tablas, 4 vistas) a PostgreSQL 16 (RNF-13).

| Archivo | Contenido |
|---|---|
| `migraciones/V001__esquema_base.sql` | Tablas, índices y vistas (generado desde el SQL original y revisado) |
| `migraciones/V002__reglas_de_stock.sql` | Los 12 triggers originales en PL/pgSQL + bloqueo de saldo y signo por tipo de documento |
| `migraciones/V003__proteccion_y_roles.sql` | Cierra las brechas H01–H03, vista de conciliación y rol `app_stock` |
| `migraciones/V004__acceso_auditoria_aislamiento.sql` | Factor de unidades, bloqueo por intentos, funciones de acceso, auditoría automática sin secretos, presentaciones en uso protegidas, precios sin superposición y aislamiento por empresa con RLS |
| `migraciones/V010__inventarios.sql` | Inventario nace en borrador con transiciones controladas; fotografía inmutable; conteo solo en borrador; cerrar conteo exige todas las líneas; movimientos bloqueados mientras se cuenta; autorizador distinto de quien contó; un inventario abierto por almacén |
| `migraciones/V011__cierres.sql` | Día o mes cerrado no se modifica (solo el estado de envío); ingresos y gastos de un período cerrado quedan fijos (PERIODO_CERRADO); auditoría de cierres |
| `migraciones/V012__sincronizacion.sql` | UUID global de documentos; cola de salida de la sede escrita en la misma transacción (secuencia sin huecos, eventos inmutables); central con credencial por sede (solo hash), recepción idempotente, orden por origen, retención de lo que llega antes de su antecesor, rechazos registrados sin contenido, datos consolidados inmutables y rol `app_sincronizacion` que solo entrega eventos |
| `migraciones/V013__contratos_y_resultados.sql` | Líneas de contrato sin superposición por servicio; importe e inicio inmutables (los ajustes abren una línea nueva); ningún cambio de contrato toca un mes cerrado; servicio de la operación del contrato y dentro de su vigencia; origen del ingreso (manual o contrato); categorías de gasto y gasto de la operación del período; auditoría de clientes y contratos |
| `migraciones/V014__venta_por_estructura.sql` | Factor de consumo por componente de la estructura; costo previsto, venta prevista y Food Cost objetivo de la minuta fijados al aprobar e inmutables; ingreso de origen `estructura` (D13) |
| `migraciones/V015__teorico_vs_real.sql` | Factor de consumo por operación; la minuta guarda factor y reparto de cada plato; consumo real por componente (preparadas ≥ consumidas) y venta real del servicio, solo sobre minuta aprobada y nunca en un día cerrado; aislamiento y auditoría |
| `migraciones/V016__insumo_sin_costo.sql` | Producto sin costo de compra (agua para receta): se costea en S/ 0 con fuente explícita en vez de dejar la receta pendiente |
| `migraciones/V017__indices_claves_foraneas.sql` | Índices para las claves foráneas que se recorren (detalle → documento, kárdex, compras, minutas, cierres). Las que quedan sin índice están fijadas en `MigradorTests` |
| `migraciones/V018__producto_activo_y_precio_sin_igv.sql` | D02: producto activo por ingrediente y operación (`producto_operacion`), cuyo precio se costea. D03: `precio_sin_igv`, los precios no incluyen IGV |
| `migraciones/V019__familias_sgp.sql` | Categorías jerárquicas (`padre_id`): familia › subfamilia › grupo del SGP; cada presentación guarda su grupo (`variante_producto.categoria_id`) |
| `migraciones/V020__dueno_del_sistema.sql` | Dueño del sistema (`usuario.es_dueno`): todos los permisos en todas las operaciones. Solo otro dueño o el instalador lo otorgan (`fn_proteger_dueno`) |
| `migraciones/V021__seguridad_por_alcance.sql` | Fase 1 de Planificación central: alcance de cada asignación (OPERACION, ZONA, TODAS), `usuario_permiso` (concedido o negado por el superusuario), `fn_permisos_usuario`, `fn_operaciones_usuario`, `fn_tiene_permiso`; mover stock y cambiar factores validados en la base; permiso FACTORES_EDITAR |
| `migraciones/V009__produccion.sql` | Requerimiento nace en borrador y atendido no cambia, un producto por línea; una producción por minuta con raciones coherentes; solo entregas/devoluciones del mismo servicio se vinculan; merma no incluida exige baja |
| `migraciones/V008__almacen.sql` | Comprobante de proveedor único por recepción, línea recibida del empaque del pedido, devolución y entrada de traspaso con documento origen, baja con motivo, auditoría |
| `migraciones/V007__prevision_y_pedidos.sql` | Previsión y pedido nacen en borrador; detalle y líneas solo en borrador; estados que solo avanzan; aprobar exige líneas y aprobador; empaque ofrecido por el proveedor y del producto de la previsión; una previsión vigente por almacén y horizonte |
| `migraciones/V006__tecnica_y_correccion_auditoria.sql` | Técnica por ingrediente; corrige `fn_auditar` (un UPDATE sin cambios fallaba fuera de la tabla usuario) |
| `migraciones/V005__menus_y_recetas.sql` | Versión de receta aprobada inmutable, ingredientes solo en borrador, minuta aprobada inmutable (platos, fijos y costeo), solo recetas aprobadas y estructuras del mismo servicio, coherencia del costeo, auditoría del módulo |
| `pruebas/01_verificacion.sql` | 67 aserciones (9 de la guía + brechas, signo, rollback, múltiplo, permisos, aislamiento, auditoría, T06, precios, acceso) |
| `pruebas/02_concurrencia.sh` | Conexiones independientes: T24 y carrera de 10 sesiones |
| `ejecutar_pruebas.sh` | Crea bases nuevas, migra y ejecuta todo |

## Ejecutar

```bash
cd database/postgresql
./ejecutar_pruebas.sh        # requiere PostgreSQL 16+ y un usuario con CREATEDB
```

## Cambios respecto a SQLite

- `INTEGER` → `BIGINT`; `id` → `GENERATED BY DEFAULT AS IDENTITY`. Tras importar datos con ids explícitos hay que reiniciar las secuencias (`setval`).
- Fechas `TEXT` → `DATE`; `creado_en` → `TIMESTAMPTZ DEFAULT now()`.
- Los banderas 0/1 se mantienen como enteros para no divergir de la guía.
- Los mensajes de error empiezan con un código estable: `STOCK_INSUFICIENTE`, `DIA_CERRADO`, `PERIODO_CERRADO`, `DOCUMENTO_CONFIRMADO`, `SALDO_PROTEGIDO`, `SIGNO_NO_PERMITIDO`, `MULTIPLO_INCOMPATIBLE`…

## Aplicar migraciones en una sede

Con el instalador (`AppSistema.Instalador migrar`), que lleva las migraciones incluidas, las aplica en orden,
registra versión y hash en `esquema_migracion` y se detiene si una migración ya aplicada fue modificada.

## Aislamiento por empresa (V004)

El rol `app_stock` (y los usuarios de sede, miembros de él) solo ven filas de la empresa fijada en la
transacción con `set_config('app.empresa_id', ...)`. Sin ese contexto no ven nada. Las vistas usan
`security_invoker`. El propietario y las funciones `SECURITY DEFINER` no están sujetos al RLS.

## Reglas que la aplicación debe respetar

- Todo documento nace en `borrador`; se confirma con `UPDATE ... SET estado='confirmado'` y luego se insertan sus movimientos, **todo en una transacción**.
- **No** actualizar `saldo_stock` desde la aplicación: lo hace `fn_actualizar_saldo` (el rol `app_stock` ni siquiera tiene permiso).
- `secuencia` del movimiento la asigna la aplicación dentro de la transacción, serializando por almacén.
- Documentos confirmados no se editan; se corrigen con un documento de `reversion`.
- Las funciones de trigger son `SECURITY DEFINER`: ejecutar las migraciones con el rol propietario, no con el rol de la aplicación.

## Limitaciones conocidas

- Valoración (D01), impuestos (D03) y ajustes de inventario (D06) siguen abiertos; las pruebas usan un costo fijo de S/8/L.
- No hay aún: idempotencia de comandos, UUID/secuencia de sincronización, política de valoración versionada (ver `docs/guia_construccion/03`).
- Los `CHECK` de estados nuevos de la guía requieren migraciones futuras.
