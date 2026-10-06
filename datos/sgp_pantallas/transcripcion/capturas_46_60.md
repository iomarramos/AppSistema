# Transcripcion de capturas SGP Peru 46 a 60

Contrato de ejemplo en todas: PE017401 - ORCOPAMPA FOALI. Bodega: PE017401 - FOALI. Fechas de captura: 2026-10-04 (13:20 aprox.).

---
## 46 (2026-10-04 132043) Vista previa del reporte "Salida de Bodega a Produccion (Resumido)"
1. Ventana "Preview" (visor de reporte). Ruta probable: Almacen/Bodega > Salida de Bodega a Produccion (S) / Devolucion de Produccion (E) > tipo de informe "Resumen de Salida a Bodega" (ver captura 55).
2. Cabecera del reporte:
   - Empresa -> Sodexo Peru S.A.
   - Centro de Costo -> PE017401 - ORCOPAMPA FOALI
   - Centro de Costo OPTIMUM -> PE017401
   - Titulo -> Salida de Bodega a Produccion (Resumido)
   - Estado (texto naranja/marron) -> PENDIENTE
   - N° Documento -> 63169
   - F. Emision -> 04/10/2026
   - F. Produccion -> 04/10/2026
   - Contrato -> PE017401 - ORCOPAMPA FOALI
   - Bodega -> PE017401 - FOALI
   - Servicios -> REGIMEN 3 - ALMUERZO NORMAL 1
3. Grilla (columnas en orden): Codigo (codigo texto, ej. 01020404082 1; el codigo se parte en dos lineas por ancho), Descripcion (texto), Und. Bulto (codigo texto 3 letras), Cantidad Planif. (decimal 6), Cantidad Realizada (decimal 6), P.M.P. (decimal 6; precio medio ponderado), Total Planif. (decimal 6), Total Realizada (decimal 6), Und. Desp. (codigo unidad), Cantidad Despacho (decimal 6).
   Filas:
   - 010204040821 | ATUN TROZOS COMPASS 140 GR | LAT | 0.000000 | 1.000000 | 3.860000 | 0.000000 | 3.860000 | KG | 0.140000
   - 050607037455 | CREMA TIPO CHANTILLY SNEL 1LT | CAJ | 0.000000 | 1.000000 | 10.710000 | 0.000000 | 10.710000 | LT | 1.000000
   - 010107039479 | LECHE EVAPORADA (CONCENTRADA) LAIVE BOLSITARRO 800 ML | BOL | 0.000000 | 3.000000 | 5.690000 | 0.000000 | 17.070000 | LT | 2.400000
   - 010211039170 | MAYONESA SCALA EMIC 3.8 KG | BOL | 0 | 5.000000 | 34.580000 | 0 | 172.900000 | KG | 19.000000
   - 010101036994 | ACEITE VEGETAL CIELO 5 LT | BID | 0 | 0.500000 | 34.120000 | 0 | 17.060000 | LT | 2.500000
   - 01020234896 | CALDO CONCENTRADO GALLINA MACRO FOOD 1 KG | BOL | 0 | 0.300000 | 17.560000 | 0 | 5.268000 | KG | 0.300000
   - 01010434914 | AZUCAR RUBIA SACO 50 KG | SCO | 0 | 0.500000 | 135.816741 | 0 | 67.908371 | KG | 25.000000
   - 0102010018 | PAPA BLANCA | KG | 0 | 40.000000 | 2.500000 | 0 | 100.000000 | KG | 40.000000
   - 0102010006 | CEBOLLA ROJA | KG | 0 | 3.000000 | 3.330000 | 0 | 9.990000 | KG | 3.000000
   - 01010830134 | QUINUA CANTA CLARO 5 KG | BOL | 0 | 1.000000 | 60.984632 | 0 | 60.984632 | KG | 5.000000
   - 010106037527 | HARINA ESPECIAL DEL CIELO GRANEL SACO 50 KG | SCO | 0 | 0.200000 | 136.780000 | 0 | 27.356000 | KG | 10.000000
   (los codigos con digito cortado en la linea siguiente: se copian como se ven; ultimo digito en segunda linea.) Observacion: Total Realizada = Cantidad Realizada x P.M.P. (ej. 3 x 5.69 = 17.07). Cantidad Despacho esta en Und. Desp. (conversion de bulto a unidad base, ej. 1 LAT = 0.14 KG; 5 BOL = 19 KG).
4. Botones/barra del visor: navegacion de paginas (primera/anterior, "1/1", siguiente/ultima), zoom (lupa con desplegable), imprimir, exportar a Excel, Word, PDF, boton de salir. Controles de ventana minimizar/restaurar/cerrar. Hay barra de desplazamiento vertical (el reporte continua, filas cortadas).
5. Leyendas de colores: encabezado de grilla amarillo claro (solo estilo). "PENDIENTE" en color marron/naranja = estado del documento.
6. Totales: no visibles (reporte cortado en la ultima fila visible).
7. Entidades implicitas:
   - documento_salida_bodega (nro_documento 63169, fecha_emision, fecha_produccion, contrato_id, bodega_id, estado PENDIENTE/otros, servicio/regimen).
   - detalle_salida_bodega (documento_id, producto_id, unidad_bulto, cantidad_planificada, cantidad_realizada, pmp, total_planificado, total_realizado, unidad_despacho, cantidad_despacho).
   - producto (codigo, descripcion), unidad_medida (KG, LT, LAT, CAJ, BOL, BID, SCO), factor de conversion bulto a unidad despacho.
   - contrato / centro_costo (codigo PE017401 y codigo OPTIMUM), bodega, regimen, servicio.
8. Dudas: significado exacto de "Planif." vs "Realizada" (planificado en minuta vs entregado). Los codigos de producto tienen largo variable (10 a 12 digitos). "Servicios" muestra REGIMEN 3 - ALMUERZO NORMAL 1 (concatena regimen y servicio). Estados posibles distintos de PENDIENTE (desconocido).

---
## 47 (2026-10-04 132106) Control de Raciones, octubre 2026
1. Ventana "Control de Raciones". Ruta probable: Produccion/Servicios > Control de Raciones.
2. Cabecera "Datos Generales":
   - Contrato -> PE017401 (gris, solo lectura) | ORCOPAMPA FOALI (descripcion; icono mano de busqueda)
   - Regimen -> 1 | REGIMEN 1
   - Servicio -> 31 | DESAYUNO NORMAL 1
   - Fecha Minuta -> 10/2026 (selector mes/ano con flechas)
3. Grilla "Datos Raciones" (matriz clientes x dias): columnas: Rut (texto), Clientes (texto), luego una columna por dia con encabezado "dia semana + dd/mm/aaaa": Jueves 01/10/2026, Viernes 02/10/2026, Sabado 03/10/2026, Domingo 04/10/2026, Lunes 05/10/2026, Martes 06/10/2026, Miercoles 07/10/2026, Jueves 08/10/2026 (y mas a la derecha con scroll). Bajo cada fecha hay una fila de casilla "Facturable" (checkbox por dia).
   Filas:
   - (fila verde vacia de cabecera con Facturable)
   - 2010007950 | COMPAÑIA DE MINAS BUENAVE(ntura) | 470 | 470 | 470 | (vacio) ...
   - (fila) Total Cliente | 470 | 470 | 470 (negrita, gris)
   - PERSONAL | PERSONAL | 20 | 20 | 20 | 20 | (vacio)...
   - PRODUCIDAS | PRODUCIDAS | 500 | 500 | 500 | 500 | 500 | 500 | 520 | 520
   - (fila gris) Mermas
   - MER_DESCON | MER_DESCON | 22 | 20 | 23 | 22 | 0
   - MER_PRODUC | MER_PRODUC | 18 | 27 | 25 (celda seleccionada) | 27 | 0
   Enteros (raciones), sin decimales. Dias futuros sin dato quedan vacios.
4. Botones barra: nuevo, guardar/documento con marca roja, eliminar (papelera), buscar/verificar, cancelar (X), aceptar/grabar (check), imprimir, salir. Scroll horizontal y vertical.
5. Leyenda: verde = Clientes (filas de clientes/etiquetas); rojo = Dias Bloqueados; amarillo claro = Dias Habilitados (celdas editables).
6. Totales: fila "Total Cliente" por dia (suma de clientes). PRODUCIDAS = raciones producidas; PERSONAL = personal propio; Mermas = MER_DESCON (merma descongelado?) y MER_PRODUC (merma de produccion?). Sin total general.
7. Entidades implicitas:
   - control_raciones (contrato_id, regimen_id, servicio_id, fecha, cliente_id/tipo_fila, cantidad, facturable bool).
   - cliente (rut 2010007950, razon social COMPAÑIA DE MINAS BUENAVENTURA S.A. (ver captura 51)).
   - tipo_fila_racion (CLIENTE, PERSONAL, PRODUCIDAS, MER_DESCON, MER_PRODUC) con codigo y descripcion.
   - dia_bloqueado/dia_habilitado por contrato-servicio-fecha (bloqueo de edicion).
   - contrato, regimen, servicio.
8. Dudas: significado de MER_DESCON y MER_PRODUC (nombres truncados "MER_DESCON", "MER_PRODUC"); PRODUCIDAS parece independiente del total cliente (500 vs 470 + 20 personal). Facturable: por dia, afecta la facturacion al cliente. El 05/10 en adelante clientes vacios (dias futuros).

---
## 48 (2026-10-04 132112) Control de Raciones, septiembre 2026 (primera semana)
1. Misma ventana "Control de Raciones" que 47.
2. Cabecera: Contrato PE017401 | ORCOPAMPA FOALI; Regimen 1 | REGIMEN 1; Servicio 31 | DESAYUNO NORMAL 1; Fecha Minuta -> 09/2026 (campo mes en edicion, "09" resaltado).
3. Grilla: columnas Rut, Clientes, Martes 01/09/2026, Miercoles 02/09/2026, Jueves 03/09/2026, Viernes 04/09/2026, Sabado 05/09/2026, Domingo 06/09/2026, Lunes 07/09/2026, Martes 08/09/2026 (continua). Fila Facturable bajo cada fecha (sin marcar).
   Filas:
   - 2010007950 | COMPAÑIA DE MINAS BUENAVE.. | 494 | 487 | 463 | 485 | 485 | 500 | 430 | 495
   - Total Cliente | 494 | 487 | 463 | 485 | 485 | 500 | 430 | 495
   - PERSONAL | PERSONAL | 30 | 30 | 30 | 30 | 30 | 30 | 30 | 30
   - PRODUCIDAS | PRODUCIDAS | 480 | 480 | 480 | 480 | 480 | 480 | 480 | 460
   - Mermas (cabecera)
   - MER_DESCON | MER_DESCON | 22 | 20 | 23 | 22 | 20 | 22 | 24 | 25
   - MER_PRODUC | MER_PRODUC | 18 | 27 | 25 | 27 | 22 | 25 | 27 | 27 (la ultima visible 27; transcrito de lo visible: 18,27,25,27,25?,27,27) -> los valores de MER_PRODUC visibles: 18, 27, 25, 27, 12?, (ilegible parcial) ; ver imagen.
4-5. Igual que 47 (barra, leyenda Clientes verde / Dias Bloqueados rojo / Dias Habilitados amarillo).
6. Total Cliente por dia.
7. Entidades: igual que 47. Muestra que cada mes se consulta con selector mes/ano.
8. Dudas: la fila MER_PRODUC mostrada en esta captura: 18, 27, 25, 27, (ilegible), 25, 27, 27. Las filas MER_DESCON: 22,20,23,22,20,22,24,25.

---
## 49 (2026-10-04 132119) Control de Raciones, septiembre 2026 (final de mes)
1. Misma ventana. Cabecera igual: Contrato PE017401, Regimen 1, Servicio 31 DESAYUNO NORMAL 1, Fecha Minuta 09/2026 (desplazada con scroll horizontal al final del mes).
3. Columnas: Rut, Clientes, Miercoles 23/09/2026, Jueves 24/09/2026, Viernes 25/09/2026, Sabado 26/09/2026, Domingo 27/09/2026, Lunes 28/09/2026, Martes 29/09/2026, Miercoles 30/09/2026.
   Filas:
   - 2010007950 | COMPAÑIA DE MINAS BUENAVE.. | 475 | 514 | 509 | 517 | 486 | 442 | 501 | 501
   - Total Cliente | 475 | 514 | 509 | 517 | 486 | 442 | 501 | 501
   - PERSONAL | 30 | 30 | 30 | 30 | 20 | 30 | 30 | 30
   - PRODUCIDAS | 500 | 500 | 520 | 515 | 500 | 500 | 510 | 520
   - MER_DESCON | 25 | 16 | 23 | 24 | 12 | 16 | 23 | 18
   - MER_PRODUC | 21 | 21 | 27 | 26 | 12 | 21 | 27 | 22 (ultima celda seleccionada)
4-5. Igual. 6. Total Cliente por dia. El ultimo dia del mes es 30/09 (confirma que la matriz cubre el mes completo).
7. Entidades: igual que 47.
8. Dudas: ninguna nueva.

---
## 50 (2026-10-04 132132) Venta Servicio Contado (vacia, octubre 2026)
1. Ventana "Venta Servicio Contado". Ruta probable: Ventas/Facturacion > Venta Servicio Contado.
2. Cabecera:
   - Contrato -> PE017401 | ORCOPAMPA FOALI
   - Regimen -> (vacio) con buscador y descripcion vacia
   - Servicio -> (vacio) con buscador y descripcion vacia
   - Cliente -> (vacio) con buscador y descripcion; leyenda "Opcional"
   - Forma Pago -> combo (vacio)
   - Fecha -> 10/2026 (mes/ano)
   - Titulo del calendario: "Octubre 2026"
3. Grilla: calendario mensual en pestana "Venta Servicio". Columnas Lun., Mar., Mie., Jue., Vie., Sab., Dom. Cada semana tiene una fila con el numero de dia (azul) y debajo una fila amarilla para el monto del dia. Dias 1 a 31 visibles (1 es jueves). Montos vacios.
4. Botones barra: nuevo, guardar, eliminar, copiar (iconos algunos deshabilitados), cancelar (X), aceptar (check), imprimir, salir.
5. Leyenda: rojo = Dias Bloqueados; amarillo claro = Dias Habilitados.
6. Total Mes : 0.
7. Entidades: venta_servicio_contado (contrato_id, regimen_id, servicio_id, cliente_id opcional, forma_pago, fecha, monto); forma_pago (CONTADO, etc.); cliente; dia_bloqueado.
8. Dudas: valores del combo Forma Pago (solo se ve CONTADO en 51). El monto por dia parece importe en soles (decimal 2).

---
## 51 (2026-10-04 132147) Venta Servicio Contado (septiembre 2026 con datos)
1. Misma ventana.
2. Cabecera: Contrato PE017401 | ORCOPAMPA FOALI; Regimen -> 5 | REGIMEN GENERAL; Servicio -> 639 | SERVICIO VENTA DIRECTA 1; Cliente -> 2010007950 | COMPAÑIA DE MINAS BUENAVENTURA S.A. (Opcional); Forma Pago -> CONTADO; Fecha -> 09/2026; calendario "Septiembre 2026".
3. Calendario con importes (decimal 2, con separador de miles):
   - 1: 727.35 | 2: 1,464.27 | 3: 515.68 | 4: 521.87 | 5: 509.49 | 6: 4,101.15
   - 7: 719.15 | 8: 671.37 | 9: 515.68 | 10: 340.29 | 11: 340.29 | 12: 340.29 | 13: 3,729.40
   - 14: 459.51 | 15: 521.41 | 16: 453.32 | 17: 390.27 | 18: 757.35 | 19: 546.17 | 20: 3,734.02
   - 21: 259.82 | 22: 293.21 | 23: 867.93 | 24: 583.67 | 25: 294.80 | 26: 3,396.37 | 27: 7,438.72
   - 28: 299.57 | 29: 566.00 | 30: 293.00
   (el 1 de septiembre es martes; celda del dia 1 seleccionada.)
4-5. Igual a 50.
6. Total Mes : 35,651.42
7. Entidades: igual a 50; datos reales muestran un monto por dia por combinacion contrato-regimen-servicio-cliente-forma de pago. Cliente 2010007950 = rut/RUC de COMPAÑIA DE MINAS BUENAVENTURA S.A.
8. Dudas: no se ve si el monto incluye IGV; no se ve cantidad ni precio unitario (solo importe).

---
## 52 (2026-10-04 132200) Registro de Venta Cafeteria
1. Ventana "Registro de Venta Cafeteria". Ruta probable: Ventas > Venta Cafeteria.
2. Pestanas: "Venta Cafeteria" (activa), "Inventario producto".
   Cabecera: Fecha -> 04/10/2026 (gris, deshabilitada con desplegable); Contrato -> PE017401 | ORCOPAMPA FOALI; Bodega -> PE017401 - FOALI (combo).
   Grupo "Datos del Cliente" (deshabilitado): Cliente -> (vacio, buscador y descripcion); Centro Costo -> (vacio).
3. Grilla (vacia): Cod. Articulo (codigo), Articulo (texto), Cantidad (numerico), Precio Venta (decimal), Tipo Pago (codigo/texto). Sin filas.
4. Botones barra: nuevo (activo), guardar/ver, eliminar, copiar (deshab.), cancelar, aceptar, imprimir, (otro icono mano deshab.), salir.
5. Leyendas: area gris = grilla vacia; sin leyenda de colores.
6. Totales: ninguno visible.
7. Entidades: venta_cafeteria (fecha, contrato_id, bodega_id, cliente_id opcional, centro_costo) y venta_cafeteria_detalle (venta_id, producto_id, cantidad, precio_venta, tipo_pago); stock de bodega (Salida de Bodega por Venta Cafeteria (S)).
8. Dudas: contenido de la pestana "Inventario producto"; valores de Tipo Pago; Centro Costo del cliente (texto o catalogo).

---
## 53 (2026-10-04 132219) Menu desplegable de Almacen/Bodega
1. Menu emergente (ruta probable: Bodega/Inventario). Opciones visibles:
   - Recep. Proveedor (E)/ FOFI (E) [submenu]
   - Traspasos entre Cont (E/S) / CD (E/S) / Dev. CD (S) [submenu]
   - Traspasos entre Bodega (E/S) [submenu]
   - Salida de Bodega a Produccion (S) / Devolucion de Produccion (E) [submenu]
   - Salida de Bodega por Mermas (S) [submenu]
   - Salida de Bodega por Venta Cafeteria (S) [submenu]
   - Stock [submenu]
   - Reporte Registro de Mermas
   - Reporte Traslados ADS
2-3. No hay campos ni grillas.
7. Entidades implicitas: tipos de movimiento de inventario (E=entrada, S=salida): recepcion proveedor, FOFI (¿recepcion sin orden?), traspaso entre contratos/CD, traspaso entre bodegas, salida a produccion, devolucion de produccion, merma, venta cafeteria; movimiento_inventario con tipo_operacion. "ADS" sin explicacion (ilegible su significado).
8. Dudas: significado de FOFI, CD, Cont, ADS.

---
## 54 (2026-10-04 132228) Submenu Stock
1. Menu emergente: Stock > opciones:
   - Posicion Stock
   - Movimiento Stock
   - Consumo Alternativo (ex ajuste de inventario)
   - Cartola Inventario
   - Detalle Cartola de Inventario (resaltada)
   - Producto Sin Movimiento
   - Registro De Inventario Permanente Valorizado
   Tambien visibles arriba: Reporte Registro de Mermas, Reporte Traslados ADS.
7. Entidades: stock_producto_bodega (posicion), movimiento_stock (kardex), consumo_alternativo (ajuste), cartola_inventario (cabecera) y detalle_cartola_inventario (toma de inventario), inventario permanente valorizado (vista con PMP).
8. Dudas: estructura de cada pantalla no visible.

---
## 55 (2026-10-04 132239) Salida y Devolucion de Produccion (filtro de reporte)
1. Ventana "Salida y Devolucion de Produccion". Ruta: Bodega > Salida de Bodega a Produccion (S) / Devolucion de Produccion (E).
2. Campos: Contrato -> PE017401 | ORCOPAMPA FOALI; Tipo de Informe -> combo abierto con opciones:
   - Formato de Requisicion x Servicio (seleccionada)
   - Formato de Requisicion x Sector
   - Formato de Requisicion x Estructura Servicio Detallado
   - Formato de Requisicion x Estructura Servicio Resumido
   - Resumen de Salida a Bodega
   - Devolucion de Salida a Bodega
   - Salida Menos Devoluciones a Bodega
   Fecha Inicio -> (tapada por la lista); grupo Regimen: opcion "Todos" marcada, otra opcion con buscador (parcialmente tapada, ilegible); casillas rojas: "Consolidado x fecha" y "Salto Pagina".
3. Sin grilla.
4. Botones: vista previa (lupa), salir.
5. Leyendas: casillas con fondo rojo = opciones de formato de reporte.
6. Sin totales.
7. Entidades: reporte parametrizable sobre documento_salida_bodega, devolucion_produccion, sector, estructura de servicio.
8. Dudas: campos tapados por la lista (Fecha Final, Servicio) ilegibles.

---
## 56 (2026-10-04 132250) Informe Traspasos
1. Ventana "Informe Traspasos". Ruta probable: Bodega > Traspasos > Informe.
2. Campos: Tipo de Informe -> "Resumen Traspasos por Periodo" (combo); Fecha Desde -> 04/10/2026; Fecha Hasta -> 04/10/2026; Bodega -> TODOS (combo); Site -> opcion Todos (marcada) / Lista (con buscador); Orden del Informe -> Site (marcada) / Fecha; Tipo de Traspasos -> TODOS (combo, resaltado azul); Tipo de Operacion -> (vacio combo); Productos -> Uno / Todos (Todos marcada, deshabilitado), campo codigo y descripcion (deshab.).
3. Sin grilla.
4. Botones: vista previa, salir.
5. Sin leyenda de color.
6. Sin totales.
7. Entidades: traspaso (origen, destino, site, tipo_traspaso, tipo_operacion, fecha); site (sede/ubicacion); tipo_traspaso; tipo_operacion; producto.
8. Dudas: que es "Site" (ubicacion/contrato destino); valores de Tipo de Traspasos y Tipo de Operacion no visibles.

---
## 57 (2026-10-04 132259) Informe de Compras por Periodo
1. Ventana "Informe de Compras por Periodo". Ruta probable: Compras > Informes.
2. Filtro de Seleccion (casillas): Fecha, Bodega, Proveedor, Tipo de Documento, Tipo de Operacion (todas sin marcar).
   Formato Reporte -> "X Bodega - Proveedor" (combo).
   Fecha Desde / Fecha (deshab.); Bodega: casilla Todas + combo Bodega; Proveedor: casilla Todos + Rut (buscador y descripcion); Tipo de Operacion: Todas + Operacion; Tipo de Documento: Todos + Documento.
3. Sin grilla. 4. Botones: vista previa, salir. 5-6. Sin leyendas ni totales.
7. Entidades: documento_proveedor (compra) con proveedor, bodega, tipo_documento, tipo_operacion, fecha; catalogos proveedor (rut), tipo_documento, tipo_operacion.
8. Dudas: valores de los combos.

---
## 58 (2026-10-04 132309) Documento Proveedor (vacio)
1. Ventana "Documento Proveedor". Ruta probable: Bodega > Recep. Proveedor (E) / FOFI (E).
2. Datos Generales: Rut (vacio, buscador, nombre); Tipo Documento (combo vacio); N° Documento -> 0; Fecha Emision (vacio, boton "..."); Recep. Merc. -> 04/10/2026 (gris, "..."); Bodega -> PE017401 - FOALI; Orden de Compra (vacio); Recepcion: casilla "Cerrar"; Tipo de Operacion: radio CFC / FOFI, Folio N° -> 0 (gris).
   Totales: Exento 0.000000; Neto 0.0; I.V.A 0.0; Otr. Imp. 0.0; Total 0.0 (icono mano). Mensaje rojo: "No Tiene Permisos Para Realizar Un Ingreso Manual." Fletes -> 0.0.
3. Grilla Detalle: columnas (primera columna numero de linea), Codigo, Descripcion, Unidad, Cantidad Documento, Precio Documento, Total Documento, Cantidad Recibida, Precio C.. Con.. (cortado, probable "Precio Costo Contable/Conversion", ilegible) y mas a la derecha con scroll. Sin filas.
   Grillas pequenas: "Impuesto del Producto" (Descripcion, Impuesto, Valor); campo "Glosa" (texto libre).
4. Botones barra: nuevo, eliminar, cancelar, aceptar, (otros iconos: buscar, imprimir), salir. Botones "Agr. Prod." y "Elim. Prod.".
6. Totales: Exento, Neto, IVA, Otros Impuestos, Total, Fletes.
7. Entidades: documento_proveedor (cabecera), documento_proveedor_detalle, documento_proveedor_impuesto, proveedor, tipo_documento, bodega, orden_compra, impuesto.
8. Dudas: CFC y FOFI (tipo de operacion con folio); campos Exento con 6 decimales y demas con 1 decimal (formato de pantalla).

---
## 59 (2026-10-04 132323) Documento Proveedor (con datos)
1. Misma ventana que 58.
2. Valores: Rut -> 004891 | PROVEEDOR CAJA CHICA; Tipo Documento -> FACTURA ELECTONICA (sic); N° Documento -> 1251; Fecha Emision -> 19/07/2026; Recep. Merc. -> 20/07/2026; Bodega -> PE017401 - FOALI; Orden de Compra -> (vacio); Recepcion Cerrar -> sin marcar; Tipo de Operacion -> FOFI (marcada), Folio N° -> 81; Totales: Exento 19.000000, Neto 0.0, I.V.A 0.0, Otr. Imp. 0.0, Total 19.0; Fletes 0.0; mensaje rojo igual.
3. Detalle fila 1: (linea) 1 | Codigo 26003-6143 | Descripcion CAJA CHICA - POP CORN A GRANEL - MAIZ PERLITA - 1.00000 X KG | Unidad KG | Cantidad Documento 4.000000 | Precio Documento 4.661000 | Total Documento 18.644040 | Cantidad Recibida 4.000000 | Precio C.. (cortado, 4...).
   Debajo de la grilla, fila de edicion: 26003 | CAJA CHICA - POP CORN A GRANEL - MAIZ PERLITA | 1.00. (codigo de producto 26003 y formato 1.00 de contenido/factor.)
4-6. Igual a 58. Nota: Total 19.0 contra Total Documento 18.644040 (diferencia por redondeo/Exento=19).
7. Entidades: igual a 58; el codigo del detalle "26003-6143" parece producto-lote/presentacion; el producto es "PROVEEDOR CAJA CHICA" (compras de caja chica con proveedor ficticio).
8. Dudas: relacion entre 26003 y 6143; que significa el factor 1.00000 X KG.

---
## 60 (2026-10-04 132345) Food Cost (filtro)
1. Ventana "Food Cost". Ruta probable: Food Cost / Costos > Food Cost.
2. Campos: Contrato -> PE017401 | ORCOPAMPA FOALI; Fecha Inicial -> 04/10/2026; Fecha Final -> 04/10/2026; opciones radio: Costo Alimentacion (marcada), Costo Desechable, Total Costo; Regimen: Todos (marcado) / Lista (buscador); Servicio: Todos (marcado) / Lista (buscador).
3. Sin grilla. 4. Botones: vista previa, exportar/guardar, salir.
5-6. Sin leyendas ni totales.
7. Entidades: reporte food_cost sobre consumo de alimentos vs raciones; tipo de costo (ALIMENTACION, DESECHABLE, TOTAL); regimen; servicio; contrato.
8. Dudas: formula del food cost; origen del costo (salidas de bodega a produccion valorizadas con PMP / raciones).

---
# Resumen: entidades candidatas

| Entidad candidata | Columnas clave | Imagenes |
|---|---|---|
| contrato / centro_costo | codigo (PE017401), nombre, codigo OPTIMUM | 46-52, 55, 58-60 |
| bodega | codigo (PE017401 - FOALI), contrato_id | 46, 52, 56, 58, 59 |
| regimen | codigo (1, 3, 5), nombre | 46, 47-51, 55, 60 |
| servicio | codigo (31, 639), nombre | 46-51, 60 |
| cliente | rut (2010007950), razon_social | 47-52 |
| control_raciones (diario) | contrato, regimen, servicio, fecha, cliente/tipo_fila, cantidad, facturable | 47, 48, 49 |
| tipo_fila_racion (CLIENTE, PERSONAL, PRODUCIDAS, MER_DESCON, MER_PRODUC) | codigo, descripcion | 47-49 |
| dia_bloqueado / habilitado | contrato, servicio, fecha | 47-51 |
| venta_servicio_contado | contrato, regimen, servicio, cliente, forma_pago, fecha, monto | 50, 51 |
| forma_pago | codigo (CONTADO) | 50, 51 |
| venta_cafeteria + detalle | fecha, contrato, bodega, cliente, centro_costo, articulo, cantidad, precio_venta, tipo_pago | 52, 53 |
| documento_salida_bodega (produccion) + detalle | nro_documento, fechas, estado, producto, cant planif/realizada, PMP, unidad despacho | 46, 53, 55 |
| devolucion_produccion | idem salida | 53, 55 |
| movimiento_inventario / tipos E-S | recepcion, traspasos, mermas, venta cafeteria | 53, 54, 56 |
| traspaso (cont/CD/bodega, site) | origen, destino, tipo_traspaso, tipo_operacion | 53, 56 |
| stock / cartola / consumo alternativo | producto, bodega, posicion, movimientos | 54 |
| documento_proveedor + detalle + impuesto | rut, tipo_doc, nro, fechas, bodega, OC, tipo_operacion (CFC/FOFI), folio, totales, fletes, detalle (codigo, unidad, cant, precio, recibida) | 57, 58, 59 |
| proveedor | rut (004891), nombre | 57-59 |
| tipo_documento, tipo_operacion | codigo, nombre | 56-59 |
| producto + unidad_medida + conversion | codigo, descripcion, unidad bulto, unidad despacho | 46, 58, 59 |
| food_cost (reporte) | contrato, fechas, tipo de costo, regimen, servicio | 60 |
