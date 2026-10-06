# Módulo Planificación, Cocina y Producción

## 1. Flujo funcional

```text
Recetas / Catálogo
        ↓
Servicios y Estructuras
        ↓
PLAN TEÓRICO CENTRAL
        ↓
Aprobación / Liberación
        ↓
PLAN OPERATIVO
        ↓
Chef ajusta comensales/raciones/factor
        ↓
REQUERIMIENTO DIARIO
        ↓
Aprobación si es adicional
        ↓
ALMACÉN DESPACHA
        ↓
PRODUCCIÓN REAL
        ↓
Devolución / Merma
        ↓
REAL EJECUTADO
```

El Manual SGP establece que la Planificación Teórica se usa anticipadamente para determinar las compras. La Planificación Real nace luego del pedido y es la base para los requerimientos diarios.

---

# 2. FormServicios - Servicios, Regímenes y Estructuras

**Estado:** [EXISTE]

## Objetivo

Configurar la composición de cada servicio:

- régimen;
- servicio;
- componente;
- orden;
- factor;
- alternativas;
- distribución porcentual.

Ejemplo de Almuerzo:

1. Entrada.
2. Sopa.
3. Fondo.
4. Guarnición.
5. Ensalada.
6. Postre.
7. Refresco.

## Diseño

Cabecera:

- Operación.
- Régimen.
- Servicio.
- Vigencia desde/hasta.
- Estado.

Grilla de estructura:

| Orden | Componente | Obligatorio | Factor | Alternativas | Distribución | Activo |
|---:|---|---|---:|---:|---:|---|

Panel inferior:

- suma de factores;
- validación 100% de alternativas;
- observaciones.

## Reglas

- no editar estructura de jornadas ya liberadas;
- versionar cambios;
- validar suma de alternativas;
- no borrar componente con historia;
- permitir desactivar.

## Permisos

- `MENUS_VER`
- `MENUS_CONFIGURAR`

---

# 3. FormRecetas - Maestro y Versiones de Receta

**Estado:** [EXISTE]  
**Fuente visual:** capturas de ficha de receta por patrón/local/régimen.

## Objetivo

Administrar receta técnica versionada.

## Diseño

### Encabezado

- Código.
- Nombre.
- Tipo plato.
- Categoría dietética.
- Régimen.
- Estado.
- Versión.
- Raciones base.
- Peso/rendimiento.
- Vigencia.

### Tabs

1. Ingredientes.
2. Método de preparación.
3. Información nutricional.
4. Historial/versiones.
5. Operaciones donde está habilitada.

### Grilla Ingredientes

| Código | Ingrediente | Variante activa | Cantidad | UM | Merma % | Cantidad neta | Costo unitario | Costo línea |
|---|---|---|---:|---|---:|---:|---:|---:|

## Acciones

- Nueva receta.
- Nueva versión.
- Agregar ingrediente.
- Sustituir ingrediente.
- Aprobar versión.
- Retirar versión.
- Clonar receta.
- Ver costo.
- Ver nutrición.

## Reglas

- versión aprobada inmutable;
- cambios crean nueva versión;
- ingredientes activos y con equivalencia válida;
- una receta puede tener costo pendiente si algún insumo carece de precio, pero debe mostrarlo explícitamente;
- aprobación exige consistencia de unidades.

## Permisos

- `MENUS_VER`
- `RECETAS_EDITAR`
- `RECETAS_APROBAR`

---

# 4. FormPlanificacionMenus - Planificación Teórica Central

**Estado:** [EXISTE]  
**Fuente:** Manual Planificación Teórica + capturas.

## Objetivo

Construir y revisar la matriz mensual del menú por servicio/régimen.

## Diseño recomendado

### Filtros

- Empresa/Operación.
- Régimen.
- Servicio.
- Mes.
- Versión del plan.
- Estado.

### Matriz

Dos columnas congeladas:

- orden;
- estructura.

Por cada día:

- receta;
- factor;
- raciones;
- costo unitario;
- costo total.

### Filas de resumen

- comensales;
- costo del día;
- costo por bandeja;
- costo patrón techo;
- desviación vs techo;
- estado jornada.

### Panel lateral opcional

Al seleccionar una receta:

- ingredientes;
- costo;
- nutrición;
- frecuencia en mes;
- última utilización;
- alertas de repetición.

## Acciones

- Consultar.
- Ir a hoy.
- Ir a fecha.
- Buscar receta.
- Cambiar receta.
- Copiar día/semana.
- Frecuencia de recetas.
- Aporte nutricional.
- Actualizar costos.
- Ver previsión consumo.
- Comparar con techo.
- Aprobar jornada.
- Aprobar mes.
- Crear nueva versión.
- Liberar.

## Reglas

- recetas no repetidas según política configurable;
- costos calculados desde productos activos/precios vigentes;
- no editar jornadas aprobadas;
- liberar crea snapshot;
- nunca modificar snapshot liberado.

## Reportes vinculados

- Costo detallado teórico.
- Costo resumido teórico.
- Previsión de consumo.
- Frecuencia de recetas.
- Aporte nutricional.
- Menú teórico mensual.
- Costo piso/techo.

---

# 5. FormMinutas - Minuta / Necesidades

**Estado:** [EXISTE]

## Objetivo

Ver una jornada como documento operativo, no solo como matriz mensual.

## Diseño

Cabecera:

- fecha;
- servicio;
- régimen;
- comensales;
- estado;
- versión teórica;
- versión operativa.

Detalle:

| Estructura | Receta | Factor | Raciones | Costo/ración | Costo total |
|---|---|---:|---:|---:|---:|

Tabs:

1. Platos.
2. Necesidad consolidada de ingredientes.
3. Costo.
4. Cambios.
5. Requerimientos.

## Acciones

- editar borrador;
- calcular necesidades;
- aprobar;
- comparar teórico vs operativo;
- generar requerimiento.

---

# 6. FormProduccionChef - Plan Operativo del Chef

**Estado:** [EXISTE]

## Objetivo

Permitir al Chef revisar y ajustar las raciones a producir en un horizonte corto.

El Manual SGP permite en Planificación Real ajustar:

- comensales;
- raciones;
- sustitución de platos dentro de reglas.

## Rediseño recomendado

Filtros:

- fecha;
- turno/servicio;
- estado.

Grilla:

| Fecha | Servicio | Estructura | Código | Receta | Raciones teóricas | Raciones operativas | Diferencia | Estado |
|---|---|---|---|---|---:|---:|---:|---|

Panel de indicadores:

- comensales teóricos;
- comensales esperados;
- costo teórico;
- costo operativo;
- variación;
- requerimiento ya despachado.

## Acciones

- Actualizar comensales.
- Ajustar raciones.
- Sustituir receta permitida.
- Guardar cambios.
- Recalcular requerimiento.
- Solicitar adicional.

## Regla crítica

Si almacén ya confirmó el despacho original:

- no modificar el despacho anterior;
- generar `Requerimiento Adicional`;
- conservar vínculo con el requerimiento original.

## Permisos

- `MENUS_VER`
- `PRODUCCION_EDITAR`
- `FACTORES_EDITAR` cuando proceda.

---

# 7. FormRequerimientoProduccion [PROPUESTA / puede integrarse en FormProduccion]

## Objetivo

Convertir el plan operativo en solicitud de productos Cocina → Almacén.

## Tipos

- Requisición por servicio.
- Requisición por estructura.
- Requisición detallada.
- Requisición resumida.
- Adicional.

## Cabecera

- número;
- fecha;
- operación;
- servicio;
- régimen;
- tipo;
- origen;
- estado;
- solicitante;
- aprobador.

## Detalle

| Producto | UM | Necesidad | Reservado | Ya despachado | Solicitud actual | Stock disponible | Alerta |
|---|---|---:|---:|---:|---:|---:|---|

## Flujo

```text
BORRADOR
→ ENVIADO
→ APROBADO (si adicional/no planificado)
→ PREPARADO
→ DESPACHADO
→ CERRADO
```

## Reglas

- requerimiento planificado puede seguir flujo automático;
- adicional exige motivo;
- adicional puede requerir aprobación de Jefe de Operación;
- stock insuficiente no debe ocultarse;
- almacén no cambia unilateralmente la necesidad: propone sustitución/diferencia.

---

# 8. FormProduccion - Real Ejecutado

**Estado:** [EXISTE]

## Objetivo

Registrar ejecución real:

- raciones reales;
- consumos;
- mermas;
- devoluciones;
- observaciones.

## Diseño

Cabecera:

- fecha;
- servicio;
- régimen;
- estado;
- jefe/chef.

Detalle platos:

| Plato | Raciones planificadas | Operativas | Reales | Diferencia | Motivo |
|---|---:|---:|---:|---:|---|

Detalle insumos:

| Producto | Despachado | Devuelto | Merma | Consumo neto | Costo |
|---|---:|---:|---:|---:|---:|

## Validaciones

`Consumo neto = Despachado - Devuelto - Reversión aplicable`

No permitir:

- raciones negativas;
- devolución superior a despacho sin proceso excepcional;
- registrar producción en día cerrado.

---

# 9. Comparativo de tres niveles

Debe estar disponible desde Planificación y Reportes.

## Vista

| Fecha | Servicio | Indicador | Teórico | Operativo | Real | Desv. T-O | Desv. O-R | Desv. T-R |
|---|---|---|---:|---:|---:|---:|---:|---:|

Indicadores:

- comensales;
- raciones;
- kg/l/und;
- costo;
- costo/comensal;
- venta;
- Food Cost.

## Uso

- Planificador: calidad del plan.
- Chef: precisión operativa.
- Operación: control de desviación.
- Compras: precisión del abastecimiento.
