# Mapeo de AppSistema vs Manual SGP y prioridades

## 1. Mapeo principal

| Proceso | Form actual | Estado | Recomendación |
|---|---|---|---|
| Acceso | FormAcceso | Existe | Mejorar selección empresa/UX |
| Shell | FormPrincipal | Existe | Reorganizar navegación |
| Catálogo | FormCatalogo | Existe | Mantener |
| Proveedores | FormProveedores | Existe | Ampliar lead time/historial |
| Recetas | FormRecetas | Existe | Completar versión/nutrición |
| Servicios | FormServicios | Existe | Versionar estructuras |
| Plan Teórico | FormPlanificacionMenus | Existe | Prioridad P0 |
| Minuta | FormMinutas | Existe | Integrar 3 niveles |
| Plan Chef | FormProduccionChef | Existe | Prioridad P0 |
| Producción | FormProduccion | Existe | Completar real/devolución |
| Pedido | FormCompras | Existe | Prioridad P0 |
| Consolidado | FormConsolidado | Existe | Ampliar compras globales |
| Recepción | No Form dedicado identificado | Gap | Crear P0 |
| Traspasos | No Form dedicado identificado | Gap | Crear P1 |
| Despacho producción | No Form dedicado identificado | Gap | Crear P0 |
| Devolución producción | No Form dedicado identificado | Gap | Crear P0/P1 |
| Stock/Kardex | FormStock | Existe | Completar tabs |
| Inventario | FormInventarios | Existe | Prioridad P0 |
| Ajustes inventario | Parcial en inventario/stock | Gap | Crear flujo P0 |
| Raciones | No Form dedicado identificado | Gap | Crear P1 |
| Venta contado | No Form dedicado identificado | Gap | Crear P1 |
| Venta cafetería | No Form dedicado identificado | Gap | Crear P2 si aplica |
| Cierre diario | FormCierres | Existe | Prioridad P0 |
| Cierre mensual | FormCierres/FormResultados | Parcial | Wizard P0/P1 |
| Contratos | FormContratos | Existe | Mantener |
| Resultado A13 | FormResultados | Existe | Completar |
| Reportes | FormReportes | Existe | Ampliar catálogo |
| Comparativo | FormComparativo | Existe | Integrar tres niveles |
| Tránsitos | No Form dedicado identificado | Gap | Crear P1 |
| Usuarios | FormUsuarios | Existe | Mantener |
| Matriz permisos | FormMatrizAcceso | Existe | Ampliar roles base |
| Operaciones | FormOperaciones | Existe | Mantener |
| Auditoría | FormAuditoria | Existe | Mantener |
| Carga real | FormCargaReal | Existe | Wizard |
| Continuidad | FormContinuidad | Existe | Mantener |

## 2. Prioridad P0 - indispensable para piloto

1. FormPlanificacionMenus final.
2. FormProduccionChef.
3. Requerimiento diario.
4. FormCompras.
5. Recepción proveedor.
6. Despacho producción.
7. Stock/Kardex.
8. Inventario físico.
9. Ajuste inventario.
10. Cierre diario.
11. Food Cost.
12. Reportes básicos.
13. Matriz de permisos.

## 3. Prioridad P1 - operación completa

1. Pedido extra.
2. Consolidado global.
3. Traspasos.
4. Devolución producción.
5. Inventario rotativo.
6. Control de raciones.
7. Cierre mensual.
8. A13.
9. Tránsitos.
10. Comparativos de tres niveles.

## 4. Prioridad P2 - ampliaciones

1. Venta cafetería.
2. Nutrición avanzada.
3. Integraciones contables externas.
4. Dashboards avanzados.
5. Alertas automáticas.
6. pronóstico estadístico avanzado.

## 5. Deuda visual detectada

Las capturas del SGP antiguo muestran:

- exceso de columnas simultáneas;
- tipografía pequeña;
- dependencias fuertes de colores;
- iconografía sin texto;
- menús profundos;
- muchas ventanas modales.

AppSistema debe conservar la potencia funcional sin copiar esas limitaciones.

## 6. Riesgos técnicos

### R1 - Doble UI Designer/runtime
Evitar formularios donde el Designer muestre un layout y runtime lo destruya/reconstruya.

### R2 - Grillas mensuales gigantes
31 días × varias columnas por día puede exceder ancho.

Mitigación:
- columnas congeladas;
- scroll horizontal;
- navegación fecha;
- alternativa de vista semanal/diaria.

### R3 - Estado distribuido
No inferir estado únicamente en UI.

### R4 - Ajustes directos
No permitir edición directa de saldo para reconciliar.

### R5 - Reportes duplicados
Crear motor común antes de añadir decenas de reportes.

### R6 - Permisos muy gruesos
Ampliar a acción/alcance sin eliminar compatibilidad con permisos actuales.

## 7. Orden recomendado de desarrollo

```text
Sprint 1
Shell + Planificación + Chef

Sprint 2
Requerimiento + Despacho + Stock

Sprint 3
Compras + Recepción

Sprint 4
Inventario + Ajustes

Sprint 5
Cierre diario + Food Cost

Sprint 6
Cierre mensual + A13

Sprint 7
Reportes completos

Sprint 8
Tránsitos + consolidado multioperación + endurecimiento
```

## 8. Primera pantalla que conviene cerrar de punta a punta

Recomendación:

`FormPlanificacionMenus`

porque conecta:

- receta;
- estructura;
- comensales;
- factor;
- costo;
- previsión;
- compra;
- Chef;
- requerimiento.

Una vez esa pantalla y sus servicios estén estables, el resto del flujo puede construirse sobre datos consistentes.
