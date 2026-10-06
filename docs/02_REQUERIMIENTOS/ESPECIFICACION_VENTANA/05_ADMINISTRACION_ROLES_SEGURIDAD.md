# Administración, Roles, Permisos y Seguridad

# 1. Roles operativos recomendados

Se recomiendan siete perfiles base:

1. `SUPERUSUARIO`
2. `PLANIFICADOR_CENTRAL`
3. `COMPRAS_CENTRAL`
4. `JEFE_OPERACION`
5. `CHEF_OPERATIVO`
6. `JEFE_ALMACEN`
7. `ALMACENERO`

Los roles son plantillas. Los permisos efectivos deben seguir siendo configurables.

# 2. Permisos actuales de AppSistema

El Dominio ya contempla:

- `CATALOGO_VER`
- `CATALOGO_EDITAR`
- `CATALOGO_IMPORTAR`
- `PROVEEDORES_EDITAR`
- `PRECIOS_EDITAR`
- `STOCK_CONTABILIZAR`
- `USUARIOS_ADMINISTRAR`
- `AUDITORIA_VER`
- `MENUS_VER`
- `MENUS_CONFIGURAR`
- `RECETAS_EDITAR`
- `RECETAS_APROBAR`
- `MINUTAS_EDITAR`
- `MINUTAS_APROBAR`
- `COMPRAS_VER`
- `COMPRAS_EDITAR`
- `COMPRAS_APROBAR`
- `PRODUCCION_EDITAR`
- `INVENTARIO_CONTAR`
- `INVENTARIO_APROBAR`
- `REPORTES_VER`
- `CIERRE_EJECUTAR`
- `CONTRATOS_VER`
- `CONTRATOS_EDITAR`
- `GASTOS_EDITAR`
- `RESULTADOS_VER`
- `COMPRAS_CONSOLIDAR`
- `FACTORES_EDITAR`

# 3. Matriz recomendada

Leyenda:

- V = ver
- E = editar
- A = aprobar
- ADM = administrar

| Área | Planificador | Compras | Jefe Operación | Chef | Jefe Almacén | Almacenero |
|---|---|---|---|---|---|---|
| Catálogo | V | V/E precios | V | V | V | V |
| Recetas | E/A | V | V | V | V | V |
| Estructuras | E/ADM | V | V | V | - | - |
| Plan teórico | E/A | V | V | V | V limitado | - |
| Plan operativo | V | V | A | E | V | - |
| Producción | V | - | A/V | E | V | V limitado |
| Requerimiento | V | - | A | E | A/V | V |
| Compras | V | E/A | V | - | V | V |
| Recepción | - | V | V | - | A/E | E |
| Stock | V | V | V | V consulta | A/E | E |
| Inventario contar | - | - | V | - | V/A | E |
| Inventario aprobar | - | - | A opcional | - | A | - |
| Cierre diario | V | V | A/E | V | V | V |
| Cierre mensual | V | V | A/E | - | V | - |
| Resultados | V | V | V | V limitado | V | - |

# 4. Segregación de funciones

Recomendaciones:

- quien cuenta inventario no debería aprobar su propio ajuste;
- quien crea pedido puede requerir aprobación distinta;
- quien recibe mercadería no modifica precios maestros sin permiso;
- Chef no modifica stock directamente;
- Almacén no modifica raciones/recetas;
- Jefe de Operación puede aprobar, pero no debe usar cuenta Superusuario para operación diaria.

# 5. FormUsuarios

**Estado:** [EXISTE]

Diseño:

Lista izquierda:
- usuario;
- login;
- activo;
- rol base;
- última conexión.

Detalle derecha:

- nombre;
- login;
- estado;
- roles;
- operaciones;
- vigencia;
- cambio de clave;
- MFA futuro si aplica.

Acciones:

- crear;
- bloquear;
- desbloquear;
- reset clave;
- asignar alcance;
- ver auditoría.

No mostrar hashes ni secretos.

# 6. FormMatrizAcceso

**Estado:** [EXISTE]

Debe representar:

```text
Módulo
  → Pantalla
      → Acción
          → Alcance
```

Alcance:

- operación;
- zona;
- todas.

UI recomendada:

árbol o grilla jerárquica:

| Módulo | Pantalla | Acción | Rol | Excepción usuario | Efectivo | Alcance |
|---|---|---|---|---|---|---|

Siempre mostrar el permiso efectivo resultante.

# 7. FormOperaciones

**Estado:** [EXISTE]

Administrar:

- empresas;
- operaciones;
- zonas;
- almacenes;
- servicios habilitados;
- centros de costo;
- parámetros.

No eliminar operación con historia.

# 8. FormAuditoria

**Estado:** [EXISTE]

Filtros:

- fecha;
- usuario;
- operación;
- entidad;
- documento;
- acción;
- severidad.

Grilla:

| Fecha/hora | Usuario | Operación | Entidad | ID | Acción | Antes | Después | Motivo |
|---|---|---|---|---|---|---|---|---|

Permitir exportar, no editar.

# 9. FormCargaReal

**Estado:** [EXISTE]

Convertir el proceso de carga en wizard/checklist:

1. Catálogo.
2. Familias.
3. Precios.
4. Recetas.
5. Sin costo.
6. Productos activos.
7. Inventario.
8. Estructuras.
9. Ciclo.

Por paso:

- archivo;
- estado;
- cantidad;
- insertados;
- actualizados;
- rechazados;
- log;
- evidencia.

# 10. FormContinuidad

**Estado:** [EXISTE]

Debe centralizar:

- identidad de sede;
- estado conexión;
- cola pendiente;
- último envío;
- errores;
- último backup;
- probar conexión;
- sincronizar;
- respaldar;
- restaurar;
- conciliar.

No mostrar claves/credenciales en claro.

# 11. FormAcceso

**Estado:** [EXISTE]

Campos:

- empresa;
- usuario;
- clave.

La conexión BD debe configurarse en diálogo aparte.

Mejoras:

- empresa seleccionable cuando exista más de una;
- recordar empresa, no contraseña;
- bloquear temporalmente después de intentos fallidos según política;
- mensajes de acceso genéricos;
- nombre/versión visible.

# 12. Auditoría mínima obligatoria

Registrar:

- login exitoso/fallido;
- cambio de clave;
- alta/baja usuario;
- cambios permisos;
- aprobación plan;
- cambio receta;
- pedido aprobado/anulado;
- recepción confirmada;
- movimiento revertido;
- inventario aprobado;
- ajuste;
- cierre diario;
- cierre mensual;
- restauración/backup;
- sincronización manual.
