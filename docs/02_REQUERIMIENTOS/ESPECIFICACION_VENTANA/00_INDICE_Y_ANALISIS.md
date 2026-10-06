# AppSistema - Diseño funcional, Frontend WinForms y Reportes

## 1. Objetivo

Este paquete convierte el **Manual SGP Local para Operaciones**, las capturas de `Pantalla.zip`, los formatos de pre-cierre y el estado actual del repositorio `iomarramos/AppSistema` en una especificación ejecutable para diseñar y programar el Frontend WinForms de AppSistema.

No se busca copiar visualmente el SGP antiguo. Se conserva su lógica operacional útil y se rediseña la experiencia para una aplicación .NET 8 / WinForms moderna, auditable, segura, multioperación y preparada para trabajo local con sincronización.

## 2. Fuentes revisadas

### 2.1 Manual SGP

Manual de 92 páginas. El manual indica que SGP gestiona:

- planificación de menú;
- pedidos de compras;
- control de stock;
- control de costos.

Procesos cubiertos:

1. Planificación teórica.
2. Pedido mensual.
3. Pedido extra.
4. Planificación real.
5. Requerimiento diario.
6. Recepción de mercadería.
7. Salida de mercadería.
8. Ingreso de raciones y facturación.
9. Cierre diario.
10. Cierre de mes y resultado operacional.
11. Control de tránsitos.
12. Inventario rotativo.

### 2.2 Pantalla.zip

Se revisaron 75 capturas. Entre las pantallas observadas se encuentran:

- Planificación Teórica.
- Histórico de Planificación Teórica.
- Matriz mensual de menú.
- Recetas por patrón/local/régimen.
- Aporte nutricional.
- Frecuencia de recetas.
- Planificación Real.
- Generación de minuta real.
- Pedido mensual.
- Pedido extra.
- Requerimiento diario.
- Salida y devolución de producción.
- Toma de inventario.
- Impresión de inventario.
- Control de raciones.
- Venta de servicio contado.
- Venta cafetería.
- Recepción proveedor / FOFI.
- Traspasos.
- Food Cost.
- Comparativo Plan Teórico / Plan Real / Realizado.
- Previsión de consumo.
- Costo detallado teórico.
- Costo realizado.
- Menú por período.
- Reportes de compras y traspasos.

### 2.3 Reportes de cierre / pre-cierre revisados

El ZIP contiene, entre otros:

- Boleta de Ajuste de Inventarios.
- Formato de Explicación de Ajustes.
- Consumo Alternativo.
- Diferencias Físico vs Sistema - Valorizado.
- Inventario Físico Valorizado.
- Listado para Toma de Inventario.
- TOMA_INVENTARIO.

### 2.4 Formularios actuales identificados en AppSistema

Actualmente existen o están iniciados:

- `FormAcceso`
- `FormPrincipal`
- `FormCatalogo`
- `FormProveedores`
- `FormImportacion`
- `FormRecetas`
- `FormMinutas`
- `FormServicios`
- `FormProduccion`
- `FormProduccionChef`
- `FormPlanificacionMenus`
- `FormCompras`
- `FormConsolidado`
- `FormStock`
- `FormInventarios`
- `FormCierres`
- `FormReportes`
- `FormContratos`
- `FormResultados`
- `FormUsuarios`
- `FormMatrizAcceso`
- `FormOperaciones`
- `FormAuditoria`
- `FormCargaReal`
- `FormContinuidad`
- `FormComparativo`

## 3. Regla de interpretación

En estos documentos se usa esta clasificación:

- **[MANUAL]**: comportamiento exigido o descrito por el Manual SGP.
- **[CAPTURA]**: elemento visible en las capturas aportadas.
- **[EXISTE]**: formulario/funcionalidad identificado en AppSistema.
- **[PROPUESTA]**: ampliación recomendada para completar el producto.

Cuando el manual antiguo impone una limitación técnica propia de SGP (por ejemplo, procesos ligados a ADS/SAP), AppSistema debe conservar el **concepto de negocio**, pero implementarlo mediante integraciones/configuración desacopladas.

## 4. Estructura documental

| Archivo | Contenido |
|---|---|
| `01_ESPECIFICACION_FRONTEND_WINFORMS.md` | Sistema de diseño y arquitectura UI común |
| `02_PLANIFICACION_Y_PRODUCCION.md` | Planificación, recetas, Chef, producción y requerimientos |
| `03_COMPRAS_ALMACEN_INVENTARIO.md` | Abastecimiento, recepción, stock, movimientos e inventarios |
| `04_CIERRES_RESULTADOS_REPORTES.md` | Cierres, Food Cost, A13 y catálogo completo de reportes |
| `05_ADMINISTRACION_ROLES_SEGURIDAD.md` | Usuarios, roles, permisos, auditoría y continuidad |
| `06_PROMPTS_POR_FORMULARIO.md` | Prompts listos para implementar cada ventana |
| `07_PROMPT_MAESTRO_IMPLEMENTACION.md` | Prompt general para un agente senior de programación |
| `08_MAPEO_GAPS_PRIORIDADES.md` | Qué existe, qué falta y orden recomendado |

## 5. Objetivo de producto recomendado

AppSistema debe convertirse en un sistema con tres capas operativas claramente separadas:

### A. Teórico Central
Planeamiento anticipado:

- recetas;
- gramajes;
- estructuras;
- factores;
- comensales;
- costo objetivo;
- menú mensual;
- previsión de consumo.

### B. Plan Operativo / Liberado
Adaptación local controlada:

- cambios autorizados de comensales;
- raciones;
- sustituciones;
- requerimientos;
- reservas;
- pedidos;
- despachos.

### C. Real Ejecutado
Registro inmutable de lo ocurrido:

- recepción;
- consumo;
- devolución;
- merma;
- raciones reales;
- ventas;
- stock;
- inventario;
- costos;
- cierre.

Comparaciones obligatorias:

1. Teórico vs Operativo.
2. Operativo vs Real.
3. Teórico vs Real.

## 6. Principios no negociables

1. Nunca sobrescribir historia confirmada.
2. Documento confirmado = inmutable; corregir mediante reversión/ajuste.
3. Plan liberado = versionado.
4. Día cerrado = no editable.
5. Mes cerrado = no editable.
6. Inventario contado y aprobado por personas distintas cuando sea posible.
7. Permisos por módulo, pantalla, acción y alcance.
8. Todo cambio sensible debe quedar auditado.
9. La UI no ejecuta SQL directamente.
10. Las reglas de negocio pertenecen a Dominio/Datos/Servicios.
11. El Frontend solo orquesta interacción y presentación.
12. Toda acción destructiva exige confirmación explícita y motivo.
13. Todo reporte debe poder visualizarse antes de exportarse.
14. Excel/PDF son salidas, no fuentes de verdad de negocio.
15. Las pantallas deben funcionar correctamente a 1366x768 y escalar a Full HD.
