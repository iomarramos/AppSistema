# Contratos de aplicación, API y pantallas
Propuesta de diseño, no endpoints ya implementados. Prefijo sugerido `/api/v1`. Adaptar al stack elegido conservando los comportamientos.

## Convenciones
IDs expuestos opacos o UUID cuando exista sincronización; no confundirlos con los enteros del esquema actual. Cantidades y dinero en JSON como cadenas decimales ("32.000000"), convertidas a `_u6` en el límite de persistencia. Fechas de negocio `YYYY-MM-DD`, instantes con zona horaria. Respuestas incluyen versión y ID de correlación. Paginación con límite máximo, filtros validados y orden estable. La empresa se deriva del contexto autorizado.

Errores: 400 solicitud mal formada, 401 sin sesión, 403 sin permiso, 404 recurso no visible, 409 conflicto de versión/estado/idempotencia, 422 regla de negocio. No revelar existencia de recursos de otra empresa. Códigos funcionales: STOCK_INSUFICIENTE, PERIODO_CERRADO, CONVERSION_INVALIDA, COSTO_PENDIENTE, PREVISION_OBSOLETA, VERSION_CONFLICTIVA. Mensaje humano y campo afectado, sin SQL interno.

## Matriz de acciones
| Recurso/acción | Entrada esencial | Validaciones | Resultado/efecto |
|---|---|---|---|
| POST /productos | código, descripción, unidad base, especificación | código único y dimensión válida | Producto genérico |
| POST /variantes | producto, marca, envase, contenido | contenido positivo, empresa consistente | Artículo comercial |
| POST /empaques | variante, envases, mínimo, múltiplo | enteros positivos | Conversión de compra |
| POST /recetas/{id}/versiones | rendimiento e ingredientes | productos y unidades compatibles | Borrador versionado |
| POST /recetas/versiones/{id}/aprobar | versión esperada | completa, sin cantidades inválidas | Inmutabilidad |
| POST /minutas | fecha, servicio, estructuras, recetas, raciones | versión aprobada y estructura compatible | Planificación |
| POST /minutas/{id}/costear | fuente de precio y fecha | precios completos | Snapshot de costos |
| POST /previsiones/calcular | operación, almacén, horizonte, fecha corte | sin doble demanda | Desglose por producto |
| POST /pedidos | previsión, variantes/empaques, entrega | mínimo, múltiplo, vigencia | Pedido borrador |
| POST /pedidos/{id}/aprobar | versión, motivo de cambios | previsión vigente | Pedido aprobado |
| POST /recepciones | proveedor, documento, almacén, líneas | conversión, cantidades físicas, vínculo OC | Recepción borrador |
| POST /recepciones/{id}/confirmar | versión; Idempotency-Key | permisos, período, costos y duplicados | Documento y movimientos atómicos |
| POST /documentos-stock/{id}/confirmar | versión; clave idempotente | saldo, signo/tipo, período | Movimiento único |
| POST /documentos-stock/{id}/revertir | motivo, fecha abierta | saldo y reversión acumulada | Compensación trazable |
| GET /stock y /kardex | almacén, variante, rango | acceso y corte coherentes | Cantidad/valor, sin edición |
| POST /requerimientos | minuta o motivo manual | servicio y raciones | Necesidad de cocina |
| POST /producciones/{id}/cerrar | documentos, raciones, merma | conciliación y no doble costo | Costo real |
| POST /inventarios | almacén, corte, tipo | corte controlado | Fotografía del sistema |
| PUT /inventarios/{id}/conteos | versión, variantes, cantidades | unidad, filas y cero explícito | Diferencias, sin movimientos |
| POST /inventarios/{id}/ajustes | diferencias, motivo, autorización | política aprobada y no duplicado | Documento separado |
| POST /cierres/validar | operación, fecha/período | todos los pendientes | Lista accionable |
| POST /cierres/confirmar | versión; clave idempotente | repetir validación bajo bloqueo | Cierre definitivo |

## Ejemplo de recepción
```json
{
  "almacen_id": "ALM-DEMO",
  "proveedor_id": "PROV-DEMO",
  "fecha_recepcion": "2026-10-02",
  "tipo_documento": "factura",
  "numero_documento": "F001-DEMO",
  "moneda": "PEN",
  "lineas": [{
    "variante_id": "ACE-A-4L",
    "empaque_id": "CAJA-4X4L",
    "cantidad_empaques": "2",
    "precio_por_empaque": "128.000000"
  }]
}
```
Ejemplo sin impuestos, descuentos ni cargos para aislar la conversión. El servidor resuelve y conserva factor 16 L/caja, cantidad 32 L, valor S/256 y costo S/8/L. No acepta que el cliente imponga 40 L para esas mismas dos cajas. Las recepciones parciales pueden capturarse en envases o unidad base con factor explícito; el múltiplo del pedido no impide registrar lo realmente recibido.

## Contratos internos
- `calcular_necesidad(minuta_version)` devuelve producto base, cantidad base, contribuciones por receta y costo previsto.
- `consultar_disponibilidad(almacen, corte, horizonte)` devuelve stock, reservas y tránsitos elegibles con referencias; no modifica nada.
- `valorar_salida(variante, almacen, cantidad, politica)` devuelve costo y valor; no mueve existencias.
- `contabilizar(documento, clave, version)` es el único que confirma movimientos y saldo.
- `comparar_inventario(corte, conteos)` devuelve diferencias; no llama a contabilizar.
- `validar_cierre(operacion, periodo)` devuelve lista de causas; confirmar vuelve a verificar dentro de la transacción.

## Pantallas y errores recuperables
Cada pantalla debe mostrar operación activa, estado del documento, unidad y fuente del costo. Borradores guardables, búsqueda por código/nombre/marca, navegación por teclado, mensajes junto al campo y total visible. No usar solo colores para comunicar estado. Evitar pérdida de captura al fallar la red; distinguir guardado local, confirmado y sincronizado.

El kárdex muestra fecha, documento, producto, marca/presentación, unidad base, entrada, salida, costo unitario aplicado, valor y saldo. La recepción diferencia unidad de compra y unidad de stock. El conteo permite blanco y cero como valores distintos. Reportes e importaciones tienen los mismos permisos que las pantallas.

## Pruebas de contrato
Validar esquema de request/response, decimales, errores, filtros, paginación y permisos. Un cambio incompatible exige versionado o migración de consumidores. El frontend no calcula una segunda versión independiente de las reglas; puede mostrar anticipos, pero confirma los valores del servidor.
