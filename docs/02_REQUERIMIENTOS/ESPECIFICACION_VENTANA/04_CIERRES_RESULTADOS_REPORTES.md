# Cierres, Resultados y Reportes

# 1. FormCierres - Centro de control

**Estado:** [EXISTE]

El Manual indica que el cierre diario consolida:

- planificación;
- pedidos;
- entradas/salidas de stock;
- inventarios;
- costos.

Una vez cerrado un día no debe modificarse.

## Diseño recomendado

### Calendario mensual

Cada día muestra:

- abierto;
- con pendientes;
- listo;
- cerrado local;
- pendiente sync;
- sincronizado;
- error.

### Panel "Checklist de cierre"

Antes de habilitar `Cerrar día`:

| Control | Estado | Detalle | Ir a |
|---|---|---|---|
| Recepciones pendientes | OK/ERROR | 2 pendientes | Recepciones |
| Salidas a producción | OK/ERROR | SP-63169 pendiente | Producción |
| Traspasos | OK/ERROR | 1 tránsito | Tránsitos |
| Raciones | OK/ERROR | Cena sin registrar | Raciones |
| Ventas | OK/ERROR | ... | Ventas |
| Inventario requerido | OK/ERROR | ... | Inventario |
| Food Cost | INFO | 48.7% | Reporte |
| Conciliación stock | OK/ERROR | 0 diferencias técnicas | Stock |

## Acción Cerrar día

Debe:

1. volver a validar backend;
2. mostrar resumen;
3. pedir confirmación;
4. generar snapshot;
5. bloquear fecha;
6. encolar sincronización.

Nunca reabrir silenciosamente.

---

# 2. Cierre mensual

Secuencia recomendada basada en el Manual:

1. Verificación documental.
2. Cerrar último día.
3. Registrar gastos.
4. Inventario físico.
5. Revisar diferencias.
6. Aprobar ajustes.
7. Generar resultado A13.
8. Verificar integraciones contables.
9. Cerrar folios/documentos si aplica.
10. Cerrar mes.
11. Generar paquete documental.

## Wizard recomendado

`FormCierreMensualWizard`

Pasos visuales con estado:

```text
[1 Documentos ✓]
[2 Día final ✓]
[3 Gastos ✓]
[4 Inventario ✓]
[5 Ajustes !]
[6 Resultado]
[7 Integración]
[8 Cierre]
```

No permitir saltar dependencias críticas.

---

# 3. FormResultados

**Estado:** [EXISTE]

## Objetivo

Resultado operacional mensual.

### Bloques

Ingresos:
- contrato;
- ventas;
- ajustes.

Costos:
- alimentos;
- descartables;
- otros consumos.

Gastos:
- personal;
- operación;
- otros.

Indicadores:
- Food Cost;
- margen bruto;
- resultado;
- costo por comensal;
- venta por comensal.

## Comparación

- presupuesto;
- plan;
- real;
- mes anterior;
- acumulado.

---

# 4. FormReportes

**Estado:** [EXISTE]

Debe evolucionar a un catálogo por categorías, no una lista plana.

## Categorías

### Planificación
- Menú teórico.
- Costo detallado teórico.
- Costo resumido teórico.
- Previsión de consumo.
- Frecuencia de recetas.
- Aporte nutricional.
- Costo piso/techo.
- Planificación real.
- Comparativo tres niveles.

### Compras
- Mapa solicitud de compras.
- Pedido mensual.
- Pedido extra.
- Compras por período.
- Compras por proveedor.
- OC pendientes de recibir.

### Producción
- Requisición x servicio.
- Requisición x estructura.
- Requisición detallada.
- Requisición resumida.
- Salida a producción.
- Devolución producción.
- Raciones teóricas/operativas/reales.

### Almacén
- Posición stock.
- Movimiento/Kardex.
- Valorización.
- Lotes/vencimientos.
- Tránsitos.
- Mermas.

### Inventario
- Listado para toma.
- Inventario físico valorizado.
- Diferencias físico vs sistema.
- Diferencias valorizadas.
- Boleta de ajuste.
- Explicación de ajustes.
- Inventario rotativo.

### Costos/Cierre
- Costo detalle período realizado.
- Food Cost.
- Plan teórico vs plan real vs realizado.
- Comparativo de raciones.
- Resultado A13.
- Paquete cierre mensual.

---

# 5. Especificación de reportes

## R01 - Costo Detallado Teórico

Filtros:

- mes;
- operación;
- régimen;
- servicio;
- fecha opcional.

Columnas:

| Código | Ingrediente | Costo unitario | Raciones/consumo | Costo |
|---|---|---:|---:|---:|

Totales:

- total día;
- costo promedio diario;
- costo/comensal.

---

## R02 - Costo Resumido Teórico

| Fecha | Servicio | Costo teórico |
|---|---|---:|

Incluir total período y promedio.

---

## R03 - Previsión de Consumo

| Código | Producto | Cantidad | UM | Origen necesidad |
|---|---|---:|---|---|

Drill-down opcional:

producto → fecha → servicio → receta.

---

## R04 - Frecuencia de Recetas

| Código | Receta | Frecuencia | Costo | Días utilizados |
|---|---|---:|---:|---|

Alertar repetición por encima de política.

---

## R05 - Aporte Nutricional

Por receta y día:

- calorías;
- proteínas;
- grasas;
- carbohidratos;
- fibra;
- sodio, si existe dato.

No inventar nutrientes faltantes.

---

## R06 - Mapa Solicitud de Compras

| Producto | Necesidad | Stock | Seguridad | OC | Consumo pendiente | Pedido |
|---|---:|---:|---:|---:|---:|---:|

Debe mostrar fórmula.

---

## R07 - Requisición x Servicio

Cabecera:

- operación;
- fecha;
- servicio;
- régimen.

Detalle:

| Producto | UM | Cantidad requerida |
|---|---|---:|

---

## R08 - Requisición x Estructura Detallada

| Estructura | Receta | Producto | Cantidad |
|---|---|---|---:|

---

## R09 - Resumen de Compras

| Documento | Fecha | Proveedor | Producto | Cantidad | Precio | Total |
|---|---|---|---|---:|---:|---:|

---

## R10 - Resumen de Traspasos

| Tipo | Documento | Fecha | Origen | Destino | Producto | Cantidad | Estado |
|---|---|---|---|---|---|---:|---|

---

## R11 - Salida a Producción

Debe reproducir conceptualmente el reporte observado en capturas:

- número documento;
- fecha;
- bodega;
- régimen;
- servicio;
- producto;
- unidad;
- cantidad planificada;
- cantidad realizada;
- precio promedio;
- total.

---

## R12 - Control de Raciones

Matriz por fecha:

- cliente;
- tipo de ración;
- día;
- cantidad;
- precio;
- venta.

---

## R13 - Venta Servicio Contado

Calendario/tabla mensual:

- fecha;
- cliente;
- servicio;
- raciones/venta;
- total.

---

## R14 - Venta Cafetería

| Fecha | Artículo | Cantidad | Precio | Tipo pago | Total |
|---|---|---:|---:|---|---:|

---

## R15 - Costo Detalle Período Realizado

| Código | Producto | UM | Costo unitario | Cantidad real | Costo total |
|---|---|---|---:|---:|---:|

---

## R16 - Food Cost

### Resumen

| Fecha | Raciones | Venta | Venta/bandeja | Costo | Costo/bandeja | Food Cost |
|---|---:|---:|---:|---:|---:|---:|

Fórmula:

```text
Food Cost % = Costo de alimentos / Venta de alimentos × 100
```

Debe mostrar alerta contra objetivo.

---

## R17 - Posición Stock

| Código | Producto | UM | Stock | PMP | Valor | Reservado | Disponible |
|---|---|---|---:|---:|---:|---:|---:|

---

## R18 - Movimiento Stock / Kardex

| Fecha | Documento | Tipo | Entrada | Salida | Saldo | Costo | Valor saldo |
|---|---|---|---:|---:|---:|---:|---:|

---

## R19 - Consumo Alternativo / Ajustes

Mantener nombre compatible si negocio lo requiere, pero incluir:

- inventario/documento origen;
- motivo;
- usuario;
- aprobador.

---

## R20 - Plan Teórico vs Plan Real vs Realizado

Debe ofrecer:

- diario;
- acumulado;
- por servicio;
- costo alimentación;
- costo desechable;
- total costo.

| Fecha | Teórico | Operativo | Real | Desv. T-O | Desv. O-R | Desv. T-R |
|---|---:|---:|---:|---:|---:|---:|

---

## R21 - Comparativo de Raciones

| Fecha | Servicio | Teóricas | Operativas | Reales | Dif. |
|---|---|---:|---:|---:|---:|

---

## R22 - Listado para Toma de Inventario

Versión ciega:

| Código | Descripción | UM | Cantidad física |
|---|---|---|---|

No mostrar stock sistema por defecto.

---

## R23 - Inventario Físico Valorizado

| Código | Producto | UM | Físico | PMP | Valor |
|---|---|---|---:|---:|---:|

---

## R24 - Diferencias Físico vs Sistema - Valorizado

| Código | Producto | UM | Sistema | Físico | Diferencia | PMP | Valor diferencia |
|---|---|---|---:|---:|---:|---:|---:|

---

## R25 - Boleta de Ajustes

Campos observados en archivo aportado:

- contrato;
- fecha;
- código;
- descripción;
- explicación;
- unidad;
- cantidad;
- costo;
- valor.

Debe incluir:

- usuario;
- aprobador;
- inventario origen;
- firma/validación digital si la organización lo requiere.

---

## R26 - Formato de Explicación de Ajustes

Agrupar por motivo y responsable.

Debe poder adjuntarse al cierre mensual.

---

## R27 - Resultado Operacional A13

Secciones:

- ingresos;
- costos;
- gastos;
- margen;
- desviaciones;
- indicadores.

Debe ser reproducible después del cierre.

---

# 6. Motor común de reportes

Cada `Reporte` debe modelar:

```text
Titulo
Subtitulo
Filtros aplicados
Columnas
Filas
Totales
Metadatos
Orientacion
Formato de números
Agrupaciones
Saltos de página
```

No construir cada reporte directamente con `PrintDocument` y lógica duplicada.

Crear:

```text
Reporte
ReporteColumna
ReporteGrupo
ReporteTotal
ServicioReportes
VistaPreviaReporte
ExportadorExcel
ExportadorPdf
```

## Vista previa

Acciones:

- imprimir;
- PDF;
- Excel;
- CSV;
- zoom;
- ajustar página;
- encabezado/pie;
- volver a filtros.
