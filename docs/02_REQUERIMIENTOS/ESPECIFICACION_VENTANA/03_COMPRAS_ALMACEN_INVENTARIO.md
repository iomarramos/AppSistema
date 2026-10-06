# Compras, Almacén e Inventario

# 1. FormCompras - Previsión y Pedido

**Estado:** [EXISTE]  
**Fuente:** Pedido Mensual / Pedido Extra del Manual.

## Objetivo

Transformar demanda planificada en propuesta de compra.

La fórmula conceptual del Manual:

```text
Pedido propuesto
= Necesidad teórica
- Stock actual
+ Stock de seguridad
- Órdenes por recibir
+ Consumo pendiente hasta llegada
```

AppSistema debe ampliar la fórmula con:

- múltiplo de compra;
- unidad mínima;
- cobertura;
- lead time;
- reserva;
- stock comprometido;
- proveedor;
- precio vigente.

## Diseño

### Cabecera

- Operación.
- Período.
- Fecha cálculo.
- Fecha objetivo cobertura.
- Estado.

### Grilla

| Producto | UM base | Necesidad | Stock | Reservado | OC por recibir | Seguridad | Propuesta base | Múltiplo | Pedido final | Proveedor | Precio | Total |
|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---:|---:|

## Acciones

- Calcular.
- Recalcular.
- Ver fórmula.
- Ajustar pedido.
- Justificar ajuste.
- Validar cobertura.
- Aprobar.
- Generar pedidos por proveedor.
- Pedido extra.
- Anular.

## Reglas

- guardar snapshot del cálculo;
- ajuste manual requiere motivo;
- no perder fórmula que originó la cantidad;
- el pedido enviado no se modifica: se anula/reemplaza;
- control de presentación/múltiplo.

---

# 2. FormConsolidado - Compras Globales

**Estado:** [EXISTE]

## Objetivo

Consolidar necesidades de múltiples operaciones.

## Grilla

| Producto | Operación A | Operación B | Operación C | Total | Presentación compra | Múltiplo | Cantidad comprar | Proveedor | Precio | Total S/ |
|---|---:|---:|---:|---:|---|---:|---:|---|---:|---:|

Drill-down:

- doble clic en una cantidad → origen por servicio/fecha/receta;
- doble clic proveedor → histórico precios;
- doble clic producto → stock y tránsito.

## Acciones

- consolidar;
- bloquear lote de compra;
- seleccionar proveedor;
- distribuir por operación;
- emitir OC;
- exportar.

---

# 3. FormProveedores

**Estado:** [EXISTE]

Debe administrar:

- proveedor;
- RUC/documento;
- contacto;
- sede;
- lead time;
- días de entrega;
- condiciones;
- productos;
- presentaciones;
- historial precios;
- vigencia.

No eliminar proveedor con historia.

---

# 4. FormRecepciones [PROPUESTA]

No se identificó un Form dedicado en la lista actual. Debe existir como proceso explícito.

## Tipos de recepción

- Proveedor / OC.
- CD / cesión.
- Caja chica / FOFI.
- Traspaso entre operaciones.
- Traspaso entre almacenes.
- Ajuste autorizado.

## Cabecera

| Campo | Descripción |
|---|---|
| Documento | Número interno |
| Tipo | OC/CD/FOFI/TRASPASO |
| Documento origen | OC, cesión, factura, boleta |
| Proveedor/origen | Entidad |
| Fecha emisión | Documento |
| Fecha recepción | Física |
| Almacén | Destino |
| Usuario receptor | Responsable |
| Estado | Borrador/Confirmado |

## Detalle

| Producto | Presentación | Solicitado | Recibido | Diferencia | UM | Lote | Vencimiento | Precio | Valor | Observación |
|---|---|---:|---:|---:|---|---|---|---:|---:|---|

## Validaciones

- registrar cantidad física real;
- diferencia contra documento;
- lote/vencimiento según producto;
- precio obligatorio cuando corresponda;
- confirmación genera kardex;
- confirmado es inmutable;
- diferencia genera incidencia/NC/ND según integración.

---

# 5. FormStock

**Estado:** [EXISTE]

## Vista principal

Filtros:

- operación;
- almacén;
- familia;
- producto;
- stock cero;
- stock negativo (debe ser cero casos);
- próximos a vencer.

Grilla:

| Código | Producto | Presentación activa | Stock base | Stock compra | Costo promedio | Valor | Reservado | Disponible | Próx. venc. |
|---|---|---|---:|---:|---:|---:|---:|---:|---|

Tabs:

1. Posición.
2. Kardex.
3. Lotes.
4. Reservas.
5. Tránsitos.
6. Documentos pendientes.

No permitir editar saldo directamente.

---

# 6. FormMovimientosAlmacen [PROPUESTA]

Unifica entrada/salida/traspaso en una experiencia común.

## Tipos

### Entradas
- recepción proveedor;
- FOFI;
- CD;
- devolución producción;
- traspaso entrada;
- ajuste positivo.

### Salidas
- producción;
- merma;
- traspaso salida;
- venta directa;
- cafetería;
- ajuste negativo.

## Diseño

Maestro de documentos + detalle.

Estados:

- borrador;
- confirmado;
- revertido.

Acciones:

- nuevo;
- guardar;
- confirmar;
- revertir;
- imprimir;
- historial.

---

# 7. FormDespachoProduccion [PROPUESTA]

Puede ser submódulo de Producción/Almacén.

## Objetivo

Preparar y confirmar el despacho solicitado por Cocina.

## Grilla

| Producto | Solicitado | Disponible | Preparado | Despachado previo | Pendiente | Lote sugerido | Vencimiento |
|---|---:|---:|---:|---:|---:|---|---|

Aplicar FEFO donde proceda.

## Flujo

```text
REQUERIMIENTO APROBADO
→ PREPARACIÓN
→ VALIDACIÓN STOCK
→ CONFIRMAR DESPACHO
→ MOVIMIENTO KARDEX
```

---

# 8. FormDevolucionProduccion [PROPUESTA]

## Objetivo

Registrar retorno Cocina → Almacén.

Campos:

- requerimiento/despacho origen;
- fecha;
- servicio;
- producto;
- cantidad;
- lote;
- condición;
- apto/no apto;
- motivo.

Reglas:

- no mezclar devolución apta con merma;
- rastrear documento origen;
- retorno apto incrementa stock;
- producto no apto sigue flujo de merma/calidad.

---

# 9. FormInventarios

**Estado:** [EXISTE]  
**Fuente:** cierre mensual + inventario rotativo + reportes adjuntos.

## Tipos

- inventario inicial;
- inventario total de cierre;
- inventario rotativo;
- inventario selectivo;
- re-conteo.

## Flujo recomendado

```text
ABIERTO
→ GENERAR LISTA
→ EN CONTEO
→ CONTADO
→ REVISIÓN
→ APROBADO
→ GENERAR AJUSTE
→ CERRADO
```

Separación de funciones:

- contador registra;
- revisor valida;
- aprobador autoriza ajuste.

## Grilla conteo

| Código | Producto | UM | Sistema | Conteo 1 | Conteo 2 | Físico aprobado | Diferencia | PMP | Valor diferencia |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|

El stock sistema puede ocultarse durante primer conteo para evitar sesgo.

## Inventario rotativo

El manual selecciona productos por consumo/ABC.

AppSistema debería permitir:

- política ABC;
- frecuencia A/B/C;
- selección automática;
- bloqueo configurable si no se completa conteo crítico.

---

# 10. FormAjustesInventario [PROPUESTA]

Reemplaza el concepto opaco de "consumo alternativo" por un documento auditable de ajuste, manteniendo compatibilidad contable si se requiere.

## Campos

- inventario origen;
- producto;
- diferencia;
- signo;
- motivo normalizado;
- explicación;
- documento soporte;
- aprobador.

Motivos:

- error de conteo previo;
- ingreso omitido;
- salida omitida;
- error de unidad;
- merma no registrada;
- digitación;
- vencimiento;
- pérdida;
- sobrante;
- otro.

## Reportes

- Boleta de Ajuste.
- Explicación de Ajustes.
- Consumo Alternativo/Equivalente contable.

---

# 11. FormTransitos [PROPUESTA]

## Objetivo

Monitor de operaciones pendientes entre origen y destino.

Tipos:

- CD entrada;
- CD salida;
- devolución CD;
- operación a operación;
- almacén a almacén.

## Grilla

| Tipo | Documento origen | Documento local | Emisión | Recepción | Origen | Destino | Estado | Diferencia | NC/ND |
|---|---|---|---|---|---|---|---|---:|---|

Estados:

- enviado;
- en tránsito;
- recibido parcial;
- recibido;
- con diferencia;
- conciliado.
