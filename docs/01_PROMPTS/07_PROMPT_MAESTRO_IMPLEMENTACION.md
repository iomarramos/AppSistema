# Prompt Maestro - Construcción completa del Frontend AppSistema

Copia este prompt al agente de programación junto con los documentos de este paquete.

---

## ROL

Actúa como **arquitecto de software y desarrollador senior especializado en VB.NET, WinForms .NET 8, PostgreSQL, sistemas ERP/logísticos, seguridad, pruebas y diseño UX de aplicaciones de escritorio**.

Vas a trabajar sobre el repositorio:

`iomarramos/AppSistema`

No debes reescribir el sistema desde cero. Debes evolucionar la arquitectura existente sin romper funcionalidades, migraciones, datos ni pruebas.

## FUENTES OBLIGATORIAS

Antes de modificar código:

1. Lee la solución completa.
2. Lee los documentos MD de diseño entregados.
3. Identifica servicios, dominio, migraciones y permisos existentes.
4. Revisa Forms existentes.
5. Revisa tests.
6. Compara cada requisito con lo que ya está implementado.
7. No implementes duplicados si ya existe una solución funcional.

## ARQUITECTURA

Mantener separación:

```text
AppSistema.Dominio
    reglas, estados, cálculos, modelos

AppSistema.Datos
    PostgreSQL, transacciones, repositorios/servicios

AppSistema.Escritorio
    WinForms, presentación e interacción

AppSistema.Instalador
    migraciones/carga

tests
    dominio, datos, E2E
```

## REGLAS DE FRONTEND

Todo Form visual debe:

- ser compatible con Visual Studio Designer;
- usar `.Designer.vb` como fuente real de controles permanentes;
- no reconstruir toda la UI con `Controls.Clear()` en runtime;
- separar diseño de lógica;
- no contener SQL;
- no duplicar reglas del dominio;
- respetar `Tema`;
- respetar permisos;
- incluir `AccessibleName`;
- escalar a 1366x768;
- utilizar nombres de controles estables para E2E.

## SEGURIDAD

Modelo:

```text
Módulo → Pantalla → Acción → Alcance
```

Roles base (nombres de negocio; los códigos reales están en el bloque de estado):

- SUPERUSUARIO
- PLANIFICADOR_CENTRAL
- COMPRAS_CENTRAL
- JEFE_OPERACION
- CHEF_OPERATIVO
- JEFE_ALMACEN
- ALMACENERO

No confiar únicamente en ocultamiento de botones.
Backend/base debe volver a autorizar.

## MODELO OPERACIONAL

Conservar tres niveles:

1. Teórico Central.
2. Operativo/Liberado.
3. Real Ejecutado.

Comparaciones:

- Teórico vs Operativo.
- Operativo vs Real.
- Teórico vs Real.

Nunca sobrescribir historia.

## DOCUMENTOS

Estados generales:

```text
BORRADOR
CONFIRMADO
REVERTIDO/ANULADO
```

Documento confirmado no se edita.

## PLANIFICACIÓN

Plan liberado es snapshot inmutable.

Cambios:
nueva versión.

## PRODUCCIÓN

Chef puede modificar antes del despacho según permiso.

Después del despacho:
generar adicional, no alterar documento confirmado.

## STOCK

Saldo es proyección del libro.
No permitir edición directa.

Todo movimiento confirmado debe ser trazable.

## INVENTARIO

Conteo no modifica stock.

Flujo:

```text
Abrir
→ Contar
→ Revisar
→ Aprobar
→ Ajustar
→ Cerrar
```

Separar contar/aprobar.

## CIERRES

Día cerrado inmutable.
Mes cerrado inmutable.

Validar pendientes antes del cierre.

## REPORTES

Crear motor común de reportes.

Todos:
vista previa + exportación.

No duplicar formateo y paginación por reporte.

## ESTADO AL 2026-10-06 Y DECISIONES VIGENTES

Este bloque se lee antes del plan. Lo que dice aquí manda sobre las fases de abajo cuando hay diferencia. La fuente de verdad del avance es `docs/03_ESTADO/CHECKLIST.md` (entregas 1 a 11).

### Estado por fase

| Fase | Estado | Hecho | Pendiente |
|---|---|---|---|
| 0 Diagnóstico | Hecha | `docs/02_REQUERIMIENTOS/DIAGNOSTICO_VENTANA.md` (2026-10-05) | Actualizar con los cambios de entregas 4 a 11 |
| 1 Infraestructura UI | Parcial | Tema común; `Ui.Configurar` para grillas; motor de reportes (`Reporte`, `ExportadorReporte`) con impresión, Excel, PDF y CSV; catálogo de reportes por categorías | Vista previa propia en pantalla (hoy: impresión del sistema) |
| 2 Planificación | Parcial | Recetas con versiones aprobadas; minutas por día y servicio; matriz servicio × día; planilla estructura × día con edición de receta, raciones y comensales (`FormPlanillaMenu`); plan operativo del chef | Costo piso y techo en pantalla; pestañas de método y nutrición; planificación central por fases 2 a 10 (`docs/04_ARQUITECTURA_Y_DATOS/ARQUITECTURA_PLANIFICACION_CENTRAL.md`) |
| 3 Producción y requerimientos | Parcial | Requerimiento en borrador, adicional con aprobación, entrega del almacén, devolución por solicitud, producción real, mermas, raciones operativas del chef | Estados ENVIADO, PREPARADO y CERRADO del requerimiento |
| 4 Compras | Parcial | Previsión con desglose, pedidos, consolidado de todas las operaciones | Componentes de la fórmula por producto, snapshot del cálculo, drill-down |
| 5 Almacén | Parcial | Recepción de pedidos, kárdex valorizado, traspaso en dos pasos con tránsito que bloquea el cierre (V033), monitor de tránsitos | Lotes y vencimientos, reservas, recepción parcial, tránsito entre operaciones por la central (fase 3b) |
| 6 Inventario | Parcial | Conteo separado de la aprobación; ajustes con motivo normalizado (V031); clasificación ABC | Flujo completo en pantalla: abrir, contar, revisar, aprobar, ajustar, cerrar |
| 7 Cierres y resultados | Parcial | Checklist de cierre con "Ir a"; calendario por días y por semanas; cierre diario y mensual; comparado con presupuesto por rubro, mes anterior y acumulado; Food Cost | Revisar la especificación 04 frente a lo hecho |
| 8 Administración | Parcial | Usuarios (crear, roles, reiniciar clave, editar datos, reactivar); auditoría exportable; matriz de acceso; operaciones y almacenes; continuidad (TI) | Piloto real en una sede (`docs/05_OPERACION/PILOTO_ETAPA_8.md`) |
| 9 Reportes completos | Parcial | Catálogo con categorías y cuatro formatos | Reportes que faltan en `docs/02_REQUERIMIENTOS/DIAGNOSTICO_VENTANA.md` |

### Roles: nombres reales en el código

La lista de roles de la seguridad se usa con estos códigos. Los nombres de este documento son los de negocio.

| Nombre de negocio | Código en el sistema | Nota |
|---|---|---|
| SUPERUSUARIO | Dueño del sistema (`es_dueno`) | Todos los permisos en todas las operaciones |
| PLANIFICADOR_CENTRAL | `PLANIFICADOR_CENTRAL` | |
| COMPRAS_CENTRAL | `COMPRAS_CENTRAL` | |
| JEFE_OPERACION | `OPERACIONES` | |
| CHEF_OPERATIVO | `CHEF` | |
| JEFE_ALMACEN | `JEFE_ALMACEN` | Aprueba adicionales (`ADICIONAL_APROBAR`), ve inventario |
| ALMACENERO | `ALMACEN` | No prepara ni aprueba compras |

### Decisiones vigentes (no reabrir sin el usuario)

* **Migraciones:** nunca se edita una migración aplicada. En esta rama, V001 a V021 están confirmadas; las pendientes van de V026 a V033. La base local ya tiene V022 a V025 de otras ramas, y eso está pendiente de decidir (ver `CLAUDE.md`, §4).
* **Traspaso entre almacenes en dos pasos:** el envío descuenta el origen y queda en tránsito; la recepción suma en el destino por el mismo valor. Un traspaso enviado sin recibir bloquea el cierre del día de envío.
* **Planilla del menú:** el formato de referencia es el del SGP (estructura × día, receta, raciones, % sobre comensales, costo del día). Se edita por celda; solo en minutas en borrador.
* **Consultas en ventana no modal:** las pantallas de consulta (comparativo, reportes) no bloquean la aplicación. Los diálogos modales solo para pedir datos o confirmar.
* **Grillas:** toda grilla se configura con `Ui.Configurar` en el constructor. Las columnas se ajustan con el manejador de ventana creado.
* **Pruebas de interfaz:** el criterio de 1366x768 se mide sobre el diseño del formulario, no sobre la ventana maximizada. Hasta corregir la prueba, la revisión de tamaño es estática.

### Verificación obligatoria además de las pruebas

* `herramientas/verificar_consultas_sql.py`: ejecuta `EXPLAIN` de todas las consultas literales del código contra una base migrada (469 completas, 0 errores al 2026-10-06).
* Revisar `%LOCALAPPDATA%\AppSistema\errores.log` después de cada recorrido de pantallas.
* Un cambio no se da por terminado sin prueba real en la aplicación cuando el entorno lo permite. Si no, decirlo explícitamente.

## PLAN DE IMPLEMENTACIÓN

### Fase 0 - Diagnóstico

Entregar:

- Forms existentes;
- servicios disponibles;
- tablas/migraciones relevantes;
- permisos;
- gaps;
- riesgos;
- pruebas existentes.

No modificar código hasta terminar diagnóstico.

### Fase 1 - Infraestructura UI

- shell;
- controles reutilizables;
- report preview;
- estilos;
- helpers;
- navegación;
- errores.

### Fase 2 - Planificación

- Servicios.
- Recetas.
- Planificación.
- Minutas.
- Chef.

### Fase 3 - Producción / requerimientos

- requerimiento;
- despacho;
- devolución;
- producción real.

### Fase 4 - Compras

- previsión;
- pedido;
- consolidado;
- proveedor.

### Fase 5 - Almacén

- recepción;
- movimientos;
- stock;
- tránsitos.

### Fase 6 - Inventario

- conteo;
- aprobación;
- ajustes;
- reportes.

### Fase 7 - Cierres/resultados

- pre-cierre;
- cierre diario;
- cierre mensual;
- A13;
- Food Cost.

### Fase 8 - Administración

- usuarios;
- roles;
- auditoría;
- operaciones;
- continuidad.

### Fase 9 - Reportes completos

Implementar catálogo y exportaciones.

## METODO DE TRABAJO POR FORM

Para cada Form:

1. Identificar caso de uso.
2. Identificar permisos.
3. Identificar servicio backend.
4. Identificar tablas/DTO necesarios.
5. Diseñar wireframe textual.
6. Definir controles y nombres.
7. Definir estados.
8. Definir validaciones.
9. Implementar Designer.
10. Implementar lógica.
11. Tests de servicio.
12. Tests E2E.
13. Compilar.
14. Ejecutar prueba real.
15. Documentar.

## REGLA ANTI-REGRESIÓN

Antes y después de cada módulo ejecutar:

```text
dotnet restore
dotnet build
dotnet test
```

y E2E Windows cuando corresponda.

Además de lo anterior, después de cada cambio en consultas SQL ejecutar `herramientas/verificar_consultas_sql.py` contra una base migrada, y revisar `%LOCALAPPDATA%\AppSistema\errores.log` tras el recorrido de pantallas.

Migraciones: una migración aplicada no se edita; si el número ya está usado en otra rama, se usa el siguiente libre y se registra el choque en `CLAUDE.md`, §4. Antes de migrar una base real: respaldo y prueba en copia.

No aceptar:

- warnings nuevos;
- tests omitidos para "hacer verde";
- reglas comentadas;
- excepciones tragadas;
- SQL peligroso;
- bypass de permisos.

## CRITERIO DE DONE

Una pantalla solo está terminada si:

- el Designer abre;
- funciona con datos reales;
- permisos correctos;
- estados bloqueados;
- auditoría;
- errores amigables;
- E2E positivo;
- E2E negativo;
- 0 errores;
- 0 warnings nuevos;
- documentación actualizada;
- prueba real en la aplicación cuando el entorno lo permite (si no, decirlo en la entrega).

## ENTREGA POR ITERACIÓN

Cada iteración debe reportar:

```text
Módulo:
Forms modificados:
Servicios modificados:
Migraciones nuevas:
Pruebas agregadas:
Pruebas ejecutadas:
Resultado:
Riesgos:
Pendientes:
Captura/flujo validado:
```

No avanzar a otro módulo si el anterior deja compilación o pruebas en rojo.
