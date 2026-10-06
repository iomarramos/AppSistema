# Transcripcion de capturas SGP Peru (Sodexo) 16 a 30

Fechas segun indice.csv: 16-18 = 2026-10-03 17:17-17:17; 19-30 = 2026-10-04 13:15-13:17. Version visible: "SGP Peru v4.08.0059". Contrato PE017401 ORCOPAMPA FOALI.

---
## Captura 16 (2026-10-03 171718) - Recorte de cabecera de Orden de Compra / Pedido impreso (PDF)
1. Ventana: no hay ventana, es un recorte de un documento impreso (orden de compra/pedido a proveedor). Ruta probable: Pedidos > Orden de Compra (impresion/vista previa). En el borde superior se corta el texto "FECHA: 25/09/2026" (parcialmente visible, el dia/mes se lee "25/09/2026" apenas; (parcialmente ilegible)).
2. Cabecera (bloque "Para"):
- Para -> GRUPO MAKBEN S.A.C. (SMITH & GREENE)
- Ruc -> 20525141188
- Direc -> AV. LA FLORESTA 367 DPTO 402 - SANTIAGO DE SURCO
- Atte -> JOHN MARTIN #947474994
- Fono -> 4356581 / ; Fax -> (vacio)
- PEDIDO:// -> LCL-06695-092026-ORCOPA_1
3. Grillas: ninguna visible.
4. Botones: ninguno.
5. Leyendas: ninguna.
6. Totales: ninguno.
7. Entidades implicitas:
- proveedor (ruc, razon_social, direccion, distrito, contacto_atte, telefono, fax)
- pedido_compra (codigo_pedido 'LCL-06695-092026-ORCOPA_1', fecha, proveedor_id, contrato_id)
- Estructura del codigo: LCL (tipo/origen?) - 06695 (correlativo o codigo de unidad?) - 092026 (mes-anio MMAAAA) - ORCOPA (abreviatura del contrato) _ 1 (secuencia).
8. Dudas: el significado de LCL y 06695; si "Atte" incluye telefono en el mismo campo (JOHN MARTIN #947474994). El pedido es "por proveedor": una orden por proveedor y periodo.

---
## Captura 17 (2026-10-03 171737) - Recorte de cabecera de Orden de Compra / Pedido impreso
1. Igual que 16, otro proveedor. Ruta probable: Pedidos > Orden de Compra (impresion).
2. Cabecera:
- Para -> FRIONOX SAC
- Ruc -> 20460436771
- Direc -> AV. LOS PLATINOS NRO. 299 URB. INDUSTRIA - LOS OLIVOS
- Atte -> CECILIA SANCHEZ
- Fono -> 522-1181 / 5227081 ; Fax -> 5227081
- PEDIDO:// -> LCL-06695-092026-ORCOPA_1
3-6. Sin grillas, botones, leyendas ni totales.
7. Entidades: proveedor (fono y fax con varios numeros: guardar como texto o tabla proveedor_telefono), pedido_compra.
8. Dudas: el mismo codigo de pedido LCL-06695-092026-ORCOPA_1 aparece en proveedores distintos (16, 17, 18), por lo que el codigo identifica la tanda/lote de pedidos (contrato+periodo), no a una orden individual; la orden individual es (pedido, proveedor).

---
## Captura 18 (2026-10-03 171753) - Recorte de cabecera de Orden de Compra / Pedido impreso
1. Igual que 16/17. Ruta: Pedidos > Orden de Compra (impresion).
2. Cabecera:
- FECHA: -> 25/09/2026
- Para -> UNION YCHICAWA S.A.C.
- Ruc -> 20100047137
- Direc -> JR. JUNIN 758-774 - LIMA
- Atte -> CESAR MOGOLLON 99-8289570 / ANEXO 140
- Fono -> 426-5669 / ; Fax -> (vacio)
- PEDIDO:// -> LCL-06695-092026-ORCOPA_1
3-6. Sin grillas, botones, leyendas ni totales.
7. Entidades: proveedor, pedido_compra con fecha (25/09/2026).
8. Dudas: "FECHA" es de emision del pedido (25/09/2026) para el periodo 09/2026 (codigo 092026); el periodo del codigo y la fecha podrian no ser el mismo mes (pedido emitido el 25/09 para consumo posterior?). Las lineas (items) no se ven en estas capturas.

---
## Captura 19 (2026-10-04 131508) - Ventana principal SGP Peru v4.08.0059 (menu y calendario del mes)
1. Titulo: "SGP Peru v4.08.0059". Menu superior: Planificacion, Pedidos, Transacciones Ent/Sal, Control de Documentos, Venta, Gastos, Informes Transacciones Ent/Sal, Otros Informes, Cierre, Base de Datos, General, Salir. Los submenus no se ven.
2. Cabecera:
- Fecha(mm/aa) -> 10/2026 (control numerico con flechas)
3. Grilla calendario (7 columnas): Lunes, Martes, Miercoles, Jueves, Viernes, Sabado, Domingo. Filas de semanas. Octubre 2026: dia 1 (jueves) a 31 (sabado... en la grilla el 31 cae en jueves de la ultima fila; se lee 26 27 28 29 30 31). Celdas: 1, 2, 3 en verde; 4 a 31 en amarillo claro.
 Fila 1: (vacio) (vacio) (vacio) 1 2 3 4; fila 2: 5 6 7 8 9 10 11; fila 3: 12..18; fila 4: 19..25; fila 5: 26 27 28 29 30 31 (vacio).
 Nota: la grilla muestra 1 = Jueves, pero 31/10/2026 queda bajo Sabado en otras capturas; en esta captura la ultima fila va 26..31 con domingo vacio (consistente con el 1 en Jueves y 31 en Sabado).
 Panel inferior "Mensaje" (grilla de log): columnas Fecha (fecha-hora, formato dd/mm/aaaa hh:mm:ss a. m./p. m.), Mensaje (texto). Filas ejemplo:
 1 | 24/09/2026 07:12:07 p. m. | PE017401 - ORCOPAMPA FOALI - 1 - Traspaso Existoso.
 2 | 24/09/2026 06:20:42 p. m. | PE017401 - ORCOPAMPA FOALI - 1 - Traspaso Existoso.
 3 | 20/08/2026 06:43:28 p. m. | idem
 4 | 20/08/2026 05:39:22 p. m. | idem
 5 | 22/07/2026 04:02:03 p. m. | idem
 6 | 22/07/2026 02:58:10 p. m. | idem
4. Botones: ninguno; logo SGP SODEXO al centro. Controles de ventana (minimizar, maximizar, cerrar).
5. Leyenda del calendario: amarillo claro = "Dia Habilitado"; verde = "Dia Cerrado y enviado"; rojo/rosa = "Dia Cerrado y no enviado".
6. Barra de estado inferior: 04/10/2026 | 01:15 | Contrato : PE017401 ORCOPAMPA FOALI | Usuario : ALM274 | Disponible | Periodo : 10/2026 | Ultimo Cierre : 03/10/2026 | SQL Server | Pais : Peru.
7. Entidades: contrato (codigo PE017401, nombre ORCOPAMPA FOALI, pais Peru); usuario (ALM274, rol almacen?); periodo (mm/aaaa); cierre_diario (contrato, fecha, estado: habilitado / cerrado_enviado / cerrado_no_enviado, ultimo cierre); bitacora_traspaso (fecha_hora, contrato, mensaje "Traspaso Existoso", nro 1) - traspaso = envio de datos a sede central.
8. Dudas: "Traspaso Existoso" (sic, con error ortografico en origen); el "- 1 -" puede ser un nro de contrato/sub-unidad o tipo de traspaso; "Disponible" es un estado de la sesion o conexion. Ultimo cierre 03/10/2026 coincide con dias 1-3 verdes.

---
## Captura 20 (2026-10-04 131521) - Planificacion Teorica (dialogo de seleccion)
1. Titulo: "Planificacion Teorica". Ruta: Planificacion > Planificacion Teorica.
2. Cabecera:
- Contrato -> PE017401 (deshabilitado/gris) ; descripcion: ORCOPAMPA FOALI
- Regimen -> 1 ; descripcion: REGIMEN 1
- Servicio -> 31 ; descripcion: DESAYUNO NORMAL 1
- Fecha desde -> 10/2026 (mm/aaaa con flechas)
 Cada campo codigo tiene un icono de "mano" (busqueda/lista de valores) y una caja gris con la descripcion.
3. Grilla: calendario del mes (Lunes..Domingo), dias 1 a 31 todos en rosa/rojo.
4. Botones (iconos verticales a la derecha, sin texto, de arriba a abajo): grilla/calendario (abrir planificacion o historico), exportar/importar flecha (?), impresora/carpeta (historico?), puerta de salida (cerrar). (iconos sin rotulo; funcion inferida)
5. Leyenda: rojo/rosa = dias de planificacion cerrados (concuerda con Estado "Cerrado" en captura 21). Amarillo (en la Real, ver 27) = habilitado.
6. Totales: ninguno.
7. Entidades: contrato, regimen (codigo, descripcion), servicio (codigo, descripcion, regimen), planificacion_teorica (contrato, regimen, servicio, anio_mes, estado), planificacion_teorica_dia (planificacion, fecha, estado_dia).
8. Dudas: el servicio 31 aparece en Regimen 1. En el historico (21) un servicio parece pertenecer a un unico regimen.

---
## Captura 21 (2026-10-04 131531) - Planificacion Teorica + "Historico Planificacion Teorica"
1. Dos ventanas: "Planificacion Teorica" (igual a 20) y "Historico Planificacion Teorica" (lista de planificaciones existentes).
2. Cabecera de la primera: Contrato PE017401 ORCOPAMPA FOALI; Regimen 1 REGIMEN 1; Servicio 31 DESAYUNO NORMAL 1; Fecha desde 10/2026; calendario todo rosa.
3. Grilla historico, columnas en orden: C.Regimen (entero), Descripcion (texto), C.Servicio (entero), Descripcion (texto), Fecha (mm/aaaa), Estado (texto).
 Filas ejemplo:
 1 | REGIMEN 1 | 31 | DESAYUNO NORMAL 1 | 10/2026 | Cerrado
 3 | REGIMEN 3 | 141 | ALMUERZO NORMAL 1 | 10/2026 | Cerrado
 3 | REGIMEN 3 | 281 | CENA NORMAL 1 | 10/2026 | Cerrado
 2 | REGIMEN 2 | 441 | LONCHERA SIMPLE 1 | 10/2026 | Cerrado
 2 | REGIMEN 2 | 639 | SERVICIO VENTA DIRE(cortado: probablemente DIRECTA) | 10/2026 | Cerrado
 5 | REGIMEN GENERAL | 725 | CONSUMO FIJO FOOD(cortado) | 10/2026 | Cerrado
 2 | REGIMEN 2 | 727 | RANCHO 1 | 10/2026 | Cerrado
 2 | REGIMEN 2 | 728 | RANCHO 2 | 10/2026 | Cerrado
 2 | REGIMEN 2 | 784 | LONCHERA BAJADA | 10/2026 | Cerrado
 5 | REGIMEN GENERAL | 921 | CONSUMO FIJO TGM | 10/2026 | Cerrado
 1 | REGIMEN 1 | 31 | DESAYUNO NORMAL 1 | 09/2026 | Cerrado (hay mas filas; scroll)
4. Botones del historico: check verde (seleccionar/aceptar), puerta (salir). Scrollbars.
5. Leyenda: fila seleccionada en negro.
6. Totales: ninguno.
7. Entidades: regimen (1 REGIMEN 1, 2 REGIMEN 2, 3 REGIMEN 3, 5 REGIMEN GENERAL; el 4 no aparece); servicio (31, 141, 281, 441, 639, 725, 727, 728, 784, 921 con regimen); planificacion_teorica (contrato, servicio, mes, estado Cerrado/Abierto).
8. Dudas: los codigos de servicio son numericos no consecutivos (31, 141, 281...), posible codificacion antigua; regimen 5 "General" agrupa "consumo fijo" (servicios no consumidos por menu).

---
## Captura 22 (2026-10-04 131545) - Planificacion Teorica (grilla de menu, vista superior)
1. Titulo: "Planificacion Teorica". Menus: Menu, Plato Menu. Ruta: Planificacion > Planificacion Teorica > (servicio seleccionado).
2. Banda amarilla: "ORCOPAMPA FOALI(PE017401) - REGIMEN 1 - DESAYUNO NORMAL 1". Nota: "Las raciones debe incluir las raciones del personal".
3. Grilla: filas = Estructura Servicio; columnas por dia en bloques de 3: [Plato (texto "Jue 1/10/2026" con prefijo R), N.Rac. (entero), Cto.Plat (decimal 6 dec)]. Encima de cada dia, fila "Costo Minuta Dia" con valor decimal 2 dec: Jue 1/10 = 2.99; Vie 2/10 = 3.53; Sab 3/10 = 3.34; Dom 4/10 = 3.64; Lun 5/10 = 3.78; Mar 6/10 = 3.11.
 Filas (numero, estructura): 1 SOPA NORMAL; 2 PLATO DE FONDO N(cortado); 3 GUARNICION ARRO(cortado, ARROZ); 4 GUARNICION 1; 5 BEBIDA FRIA 1; 6 BEBIDA FRIA 2; 7 BEBIDA CALIENTE 1; 8 BEBIDA CALIENTE 2; 9 BEBIDA CALIENTE 3; 10 BEBIDA CALIENTE 4; 11 COMPLEMENTO 1; 12 COMPLEMENTO 2; 13 COMPLEMENTO 3; 14 COMPLEMENTO 4; 15 COMPLEMENTO 5; 16 PAN 1; 17 PAN 2; 18 PAN 3; 19 PAN 4; 20 PAN 5; 21 ISLA 1; 22 ISLA 2; 23 Comensales.
 Ejemplos Jue 1/10: fila2 "R PUL - CHANFAINITA" 312 raciones 1.140000; fila3 "R ARROZ BLANCO" 312 0.520000; fila4 "R MOTE" 312 0.410000; fila5 "R JUGO DE PAPAYA CON NARANJA" 176 1.030000; fila7 "R PUNQUI (7 SEMILLAS)" 514 0.060000; fila8 "R CAFE PASADO" 126 0.330000; fila9 "R INFUSIONES" 103 0.090000; fila11 "R QUESO FRESCO" 182 0.680000; fila12 "R HUEVO FRITO" 100 1.000000; fila13 "R MANTEQUILLA CON SAL" 250 0.380000; fila14 "R MERMELADA" 158 0.130000; fila15 "R HUEVOS A LA ORDEN" 158 0.860000; fila16 "R PAN FRANCES" 572 0.050000; fila17 "R PAN DE YEMA" 572 0.050000. Fila 23 Comensales: 520 (Jue), 520 (Vie), 514 (Sab), 480 (Dom), 520 (Lun), 520 (Mar).
 Vie 2/10: SOPA vacio; "POLL - ESCABECHE DE POLLO" 338 1.340000; ARROZ BLANCO 312 0.520000; "CAMOTE AL HORNO" 338 0.200000; "JUGO DE PAPAYA" 176 1.190000; "JUGO DE FRESA Y ARANDANO" 102 1.090000; "AVENA CON CHOCOLATE" 514 0.270000; "LECHE CALIENTE I" 78 0.750000; "QUESO EDAM" 130 1.090000; "PALTA" 132 1.010000.
 Sab 3/10: "CER - PATITA CON MANI" 257 1.730000; "MACAAVENA CON LECHE" 510 0.300000; "ACEITUNA NEGRA" 133 1.080000; "HOT DOG" 100 0.320000; "CINNAMON ROLL" 226 0.510000; "TOSTADAS INTEGRALES" 226 0.060000; "PAN - PAN DE MANTECA" 226 0.160000; "PAN ARABE" 226 0.050000.
 Dom 4/10: sopa "CALDO DE GALLINA" 336 1.710000; "MAIZ CANCHA" 288 0.350000; "JUGO SURTIDO I" 170 1.340000; "KIWICHA AVENA CON PINA" 473 0.330000; "SALSA GUACAMOLE" 146 0.590000; "OMELETTE DE PIMIENTO" 130(?) ; ISLA 1 "LIMON" 96 0.070000; ISLA 2 "AJI - ROCOTO PICADO" 70 0.100000.
 Lun 5/10: "POLL - MILANESA DE POLLO" 314 1.290000; "LEGUMBRES SALTEADAS" 314 0.600000; "JUGO DE FRESA" 130 1.040000; "QUINUA CON MANZANA" 514 0.330000.
 Mar 6/10: "HUEVO FRITO" 318 1.000000; "ENCEBOLLADO" 318 0.430000; "JUGO DE NARANJA" 102 1.390000; "API" 182(?); "MACA" 514 0.220000; "PALTA" 158 1.010000; "JAMONADA" 130 0.350000.
 Ultima columna visible cortada (Mie 7/10): CALDO DE..., MAIZ CAN..., LIMON, AJI - ROCO...
 Todas las celdas de plato en rojo (bloqueado). Columna izquierda "Estructura Servicio" en verde.
 Prefijo "R" rojo en cada plato (posible indicador de "Receta" asociada).
4. Barra de iconos (sin rotulos): guardar, cortar, copiar, pegar (2), buscar (binoculares), insertar/eliminar linea (2), subir, bajar, ver receta, exportar, varios iconos (copos, signo $ = costos, calendario/reporte, etc.), salir. Menu "Menu" y "Plato Menu" (detalle en 25 y 26).
5. Leyendas (barra): verde claro = "Estructura de Servicio"; rojo/salmon = "Celda Bloqueada"; amarillo claro = "Celda Habilitada".
6. Totales: fila "Costo Minuta Dia" por dia; fila "Comensales" por dia.
7. Entidades: estructura_servicio (servicio_id, orden, nombre: SOPA NORMAL, PLATO DE FONDO, GUARNICION..., BEBIDA FRIA, BEBIDA CALIENTE, COMPLEMENTO, PAN, ISLA); plan_menu_dia (planificacion, fecha, estructura_linea, plato_id/receta_id, raciones, costo_plato); comensales_dia (planificacion, fecha, n); costo_minuta_dia (derivado = suma cto.plat / raciones? no determinable); plato (nombre, receta). Prefijos de plato: PUL, POLL, CER, PES (tipo de proteina: pulpa? pollo, cerdo, pescado) = codigo de categoria en el nombre.
8. Dudas: formula del Costo Minuta Dia (ej. 2.99 en Jue 1/10 coincide con Cto.Band. 2.99 de la captura 23). N.Rac. del plato puede ser menor que Comensales (porcentaje de aceptacion). La "R" puede significar Receta existente. Raciones de la Real (28) son distintas (planificado vs realizado).

---
## Captura 23 (2026-10-04 131557) - Planificacion Teorica con panel de totales (mismo dia 1/10)
1. Misma ventana que 22, con barra inferior y la barra de tareas de Windows. Mismos datos y filas que 22.
2. Cabecera igual.
3. Grilla igual a 22 (ver). Panel inferior con 3 bloques:
 a) "Total Mes": columnas Cto. Bandeja, Costo Total. Filas: Mat.Prima 3.43 | 54,679.51; Est.Fija 0.00 | 0.00; Total 3.43 | 54,679.51; Rac. (solo costo total col) 15,930.00.
 b) "Jue 1/10/2026": columnas Planificado, Realizado. Mat.Prima 1,556.03 | (vacio); Est.Fija 0.00; Cto.Total 1,556.03 | 1,710.85; Rac. 520.00 | 500.00; Cto.Band. 2.99 | 3.42.
 c) "Acumulado hasta Jue 1/10/2026": Planificado, Realizado. Mat.Prima 1,556.03; Est.Fija 0.00; Cto.Total 1,556.03 | 1,710.85; Rac. 520.00 | 500.00; Cto.Band. 2.99 | 3.42.
4. Iconos pequenos sobre el panel: grafico de barras y grafico de lineas.
5. Leyendas igual a 22.
6. Totales: ver arriba. Cto.Band. = Cto.Total / Rac. (1,556.03/520 = 2.99).
7. Entidades: resumen_costo_planificacion (mes, dia, acumulado) derivable: materia prima, estructura fija, costo total, raciones, costo bandeja; comparativo planificado vs realizado (relaciona con Planificacion Real).
8. Dudas: "Est.Fija" (estructura fija) = costos fijos por racion o de estructura, aqui 0.00.

---
## Captura 24 (2026-10-04 131603) - Planificacion Teorica, ultima semana del mes (26 a 31/10/2026)
1. Misma ventana, mes desplazado al final.
2. Cabecera igual.
3. Grilla: aqui el primer bloque de dias muestra columnas Cto.Plat y (en dias) Plato, N.Rac., Cto.Plat. Dias: Lun 26/10, Mar 27/10, Mie 28/10, Jue 29/10, Vie 30/10, Sab 31/10/2026. Costo Minuta Dia: 3.13 (dia previo parcialmente), 3.41 (Lun 26), 2.91 (Mar 27), 4.27 (Mie 28), 3.40 (Jue 29), 3.60 (Vie 30), 3.44 (Sab 31).
 Filas ejemplo: Lun 26: fila2 "R CER - PATITA CON MANI" 260 1.730000; ARROZ BLANCO 312 0.520000; "JUGO DE PAPAYA CON NARANJA" 103 1.030000; "JUGO SURTIDO I" 186 1.340000; "SOYA" 514 0.230000; "LECHE CALIENTE I" 78 0.750000; "ACEITUNA NEGRA" 103 1.080000.
 Mar 27: "POLL - CEVICHE DE POLLO" 338 0.800000; "ARROZ CON PEREJIL Y ZANAHORIA" 312 0.510000; "CAMOTE SANCOCHADO" 338 0.200000; "JUGO MIXTO" 188 1.040000; "KIWICHA CON MANZANA" 514 0.300000; "QUESO FRESCO" 176 0.680000; "JAMON" 112 0.390000.
 Mie 28: sopa "R SOPA CALDO BLANCO CON RES" 312 3.750000; "JUGO DE PAPAYA" 176 1.190000; "AVENA CON MEMBRILLO" 514 0.230000; "HOT DOG" 110 0.320000; "HUEVO FRITO" 103 1.000000; ISLA 1 "LIMON" 103 0.070000; ISLA 2 "SALSA ROCOTO" 84 0.130000.
 Jue 29: "PES - PESCADO A LA CHORRILLANA" 260 1.900000; "CAMOTE SANCOCHADO" 260 0.200000; "CREMA DE PALTA" 157 0.610000; "QUESO EDAM" 132 1.090000.
 Vie 30: "HUEVO FRITO" 260 1.000000; "LOCRO DE ZAPALLO" 260 0.960000; "HUEVOS REVUELTOS" 132 1.530000.
 Sab 31: "POLL - ESCABECHE DE POLLO" 334 1.340000; Comensales Sab 31 = 514 (celda activa). Comensales Lun-Vie = 520.
 En la columna izquierda aparece "Cto.Plat" repetido como columna fija de estructura (valores 2.150000 en sopa, 0.080000 guarnicion 1, 1.040000, 0.870000, 0.170000, 0.330000, 0.090000, 0.510000 ... = costo de dia anterior cortado).
 Panel inferior: "Total Mes" igual (3.43 / 54,679.51 / 15,930.00); bloque "Sab 31/10/2026": Mat.Prima 1,765.89; Est.Fija 0.00; Cto.Total 1,765.89 | Realizado 0.00; Rac. 514.00 | 514.00; Cto.Band. 3.44 | 0.00. Bloque "Acumulado hasta Sab 31/10/2026": Mat.Prima 54,679.51; Est.Fija 0.00; Cto.Total 54,679.51 | 5,798.93; Rac. 15,930.00 | 15,856.00; Cto.Band. 3.43 | 0.37.
4-5. Igual a 22.
6. Totales: ver 3 bloques.
7. Entidades: igual a 22/23.
8. Dudas: el Realizado acumulado (5,798.93) muestra que solo los dias 1-3 estan realizados/cerrados; Cto.Band. realizado 0.37 es artefacto (total realizado / raciones del mes completo). Raciones realizadas 15,856 vs planificadas 15,930 en el acumulado (raciones realizadas del dia 31 = 514?). (cifra exacta ambigua).

---
## Captura 25 (2026-10-04 131614) - Menu desplegable "Menu" de Planificacion Teorica
1. Menu "Menu" de la ventana Planificacion Teorica.
2-3. Sin campos. Opciones en orden: Grabar Semana (deshabilitada); Ver Receta; Copiar Minutas; Aporte Nutricionales x Dias; Frecuencia Recetas; Actualizar Costo Recetas; Exportar Recetas; Cerrar.
4. Esas son las opciones.
7. Entidades: receta (con ingredientes), aporte_nutricional por dia (calorias, proteinas.. ilegible/no visible), frecuencia_receta (conteo de uso en el periodo), costo_receta (actualizable), copia de minutas (plantilla de menu).
8. Dudas: "Copiar Minutas" implica minuta = plan del menu de un dia/semana. "Grabar Semana" indica que la edicion y el guardado son por semana.

---
## Captura 26 (2026-10-04 131622) - Menu desplegable "Plato Menu"
1. Menu "Plato Menu" de la misma ventana.
2-3. Opciones: Cambiar Plato Menu; Insertar Linea; Eliminar Linea; Subir Linea; Bajar Linea; Cortar (Ctrl+X); Copiar (Ctrl+C); Pegar (Ctrl+V, deshabilitada); Pegado Especial (deshabilitada); Buscar Receta; Agrega Estructura (submenu con flecha).
7. Entidades: estructura_servicio (se pueden agregar lineas de estructura), plato_menu (linea por dia con orden), receta.
8. Dudas: submenu "Agrega Estructura" no visible; Insertar/Eliminar/Subir/Bajar Linea operan sobre las lineas de estructura (columna fija verde) o sobre platos.

---
## Captura 27 (2026-10-04 131638) - Planificacion Real (dialogo de seleccion, vacio)
1. Titulo: "Planificacion Real". Ruta: Planificacion > Planificacion Real.
2. Cabecera: Contrato PE017401 (gris) ORCOPAMPA FOALI; Regimen (vacio); Servicio (vacio); Fecha desde 10/2026.
3. Calendario Lunes..Domingo con todos los dias 1 a 31 en amarillo claro (Dia Habilitado).
4. Botones: igual que 20 (iconos verticales).
5. Leyenda: amarillo = habilitado (en Teorica todos rojo, en Real amarillo).
7. Entidades: planificacion_real (contrato, regimen, servicio, mes, estado) con dias abiertos/cerrados.
8. Dudas: contrato igual; sin regimen/servicio seleccionado el calendario muestra solo el estado generico.

---
## Captura 28 (2026-10-04 131647) - Planificacion Real + "Historico Planificacion Real"
1. Dos ventanas: Planificacion Real (vacia) e "Historico Planificacion Real".
2. Cabecera: igual a 27.
3. Grilla historico, mismas columnas que 21: C.Regimen, Descripcion, C.Servicio, Descripcion, Fecha, Estado. Filas:
 1 | REGIMEN 1 | 31 | DESAYUNO NORMAL 1 | 10/2026 | Abierto
 3 | REGIMEN 3 | 141 | ALMUERZO NORMAL 1 | 10/2026 | Abierto
 3 | REGIMEN 3 | 281 | CENA NORMAL 1 | 10/2026 | Abierto
 2 | REGIMEN 2 | 441 | LONCHERA SIMPLE 1 | 10/2026 | Abierto
 2 | REGIMEN 2 | 639 | SERVICIO VENTA DIRE | 10/2026 | Abierto
 5 | REGIMEN GENERAL | 725 | CONSUMO FIJO FOOD | 10/2026 | Abierto
 2 | REGIMEN 2 | 727 | RANCHO 1 | 10/2026 | Abierto
 2 | REGIMEN 2 | 728 | RANCHO 2 | 10/2026 | Abierto
 2 | REGIMEN 2 | 784 | LONCHERA BAJADA | 10/2026 | Abierto
 5 | REGIMEN GENERAL | 921 | CONSUMO FIJO TGM | 10/2026 | Abierto
 1 | REGIMEN 1 | 31 | DESAYUNO NORMAL 1 | 09/2026 | Cerrado
4. Botones: check verde, puerta.
7. Entidades: planificacion_real (estado Abierto/Cerrado por mes).
8. Dudas: en 10/2026 la Teorica esta Cerrada y la Real Abierta; estado a nivel de mes-servicio, y el calendario muestra dias cerrados individuales (ver 29).

---
## Captura 29 (2026-10-04 131654) - Planificacion Real con servicio seleccionado
1. Titulo: "Planificacion Real".
2. Cabecera: Contrato PE017401 ORCOPAMPA FOALI; Regimen 1 REGIMEN 1; Servicio 31 DESAYUNO NORMAL 1; Fecha desde 10/2026.
3. Calendario: dias 1, 2, 3 en rosa/rojo (cerrados); 4 a 31 amarillo (habilitados). Tooltip parcial "Planif..." cerca del boton (texto cortado, ilegible).
4. Botones laterales como en 20.
5. Leyenda: rojo = cerrado (los dias con cierre 01-03/10 coinciden con "Ultimo Cierre 03/10/2026").
7. Entidades: planificacion_real_dia (fecha, estado cerrado/abierto).
8. Dudas: el cierre de dia es global del contrato (captura 19 pinta dia 1-3 verdes = cerrado y enviado) y aqui rojo por el servicio.

---
## Captura 30 (2026-10-04 131700) - Planificacion Real (grilla de menu)
1. Titulo: "Planificacion Real". Menus: Menu, Plato Menu. Banda: "ORCOPAMPA FOALI(PE017401) - REGIMEN 1 - DESAYUNO NORMAL 1". Nota: "Las raciones debe incluir las raciones del personal".
2. Cabecera igual.
3. Grilla: filas iguales a 22 (Estructura Servicio, 23 filas). Columnas por dia: [Plato con "R" a la izquierda, N.Rac. (entero), Costo (decimal 6 dec)]. Fila superior "Costo Minuta Dia": Jue 1/10 = 3.22; Vie 2/10 = 4.13; Sab 3/10 = 3.82; Dom 4/10 = 3.98; Lun 5/10 = 4.45.
 Ejemplos Jue 1/10 (rojo, cerrado): "PUL - CHANFAINITA" 350 1.140334; "ARROZ BLANCO" 300 0.523183; "MOTE" 200 0.411136; "JUGO DE PAPAYA CON NARANJA" 169 1.033742; "JUGO DE PINA" 127 0.873245; "PUNQUI (7 SEMILLAS)" 494 0.057284; "CAFE PASADO" 121 0.325093; "INFUSIONES" 0 0.091928; "QUESO FRESCO" 200 0.684600; "HUEVO FRITO" 200 1.002168; "MANTEQUILLA CON SAL" 240 0.380000; "MERMELADA" 250 0.130000; "HUEVOS A LA ORDEN" 120 0.861320; "PAN FRANCES" 550 0.046984; "PAN DE YEMA" 550 0.051189; Comensales 500.
 Vie 2/10: "POLL - ESCABECHE DE POLLO" 350 1.339312; "ARROZ BLANCO" 300 0.523183; "CAMOTE AL HORNO" 325 0.200000; "JUGO DE PAPAYA" 169 1.191817; "AVENA CON CHOCOLATE" 494 0.265718; "LECHE CALIENTE I" 75 0.751995; "QUESO EDAM" 200 1.086600; "PALTA" 200 1.008691; Comensales 500.
 Sab 3/10: "CER - PATITA CON MANI" 320 1.847341; "ARROZ BLANCO" 301 0.523183; "JUGO DE PINA" 178 0.873245; "JUGO DE NARANJA" 97 1.385000; "MACAAVENA CON LECHE" 496 0.302037; "ACEITUNA NEGRA" 200 1.080000; "HOT DOG" 200 0.320128; "CINNAMON ROLL" 220 0.505850; "TOSTADAS INTEGRALES" 220 0.064073; "PAN DE MANTECA" 220 0.161902; "PAN ARABE" 220 0.047408; Comensales 500.
 Dom 4/10 (amarillo, habilitado): "CALDO DE GALLINA" 400 1.706095; "MAIZ CANCHA" 300 0.353392; "JUGO SURTIDO I" 177 1.330577; "JUGO DE PINA CON MANZANA" 150 0.957245; "KIWICHA AVENA CON PINA" 493 0.332628; "INFUSIONES" 200 0.091928; "SALSA GUACAMOLE" 200 0.586687; "QUESO EDAM" 200 1.086600; ISLA 1 "LIMON" 100 0.067500; ISLA 2 "AJI - ROCOTO PICADO" 73 0.103701; Comensales 500.
 Lun 5/10: "POLL - MILANESA DE POLLO" 380 1.219287; "LEGUMBRES SALTEADAS" 380 0.595350; "JUGO DE PAPAYA" 175 1.191817; "JUGO DE FRESA" 170 1.036564; "QUINUA CON MANZANA" 500 0.334883; "OMELETTE DE PIMIENTO" 200 1.022501; Comensales 500.
 Mar 6/10: "ARROZ A LA CUBANA" 400 ...; "JUGO DE NARANJA" 80; "JUGO DE PAPAYA CON PINA" 170; "API" 500; "PALTA" 200; "JAMONADA" 200 (cortado a la derecha).
 Diferencias con Teorica: el valor "Costo" (en vez de Cto.Plat) es el costo REAL con 6 decimales no redondeados (costo ponderado de recetas), y N.Rac. son raciones realmente servidas/ajustadas. En los dias abiertos (Dom 4 en adelante) el plato lleva "R" verde y celda amarilla; en dias cerrados "R" roja y celda rosa.
4. Botones/iconos: igual que Teorica pero se ve un icono extra (excel verde) y otro (borrador?). Sin barra de totales inferior en esta captura.
5. Leyendas: igual: verde = Estructura de Servicio; salmon = Celda Bloqueada; amarillo claro = Celda Habilitada.
6. Totales: Costo Minuta Dia, Comensales (500 por dia).
7. Entidades: plan_real_dia_linea (planificacion_real, fecha, estructura_linea, plato_id, raciones, costo_unitario decimal 6), comensales_real_dia, costo_minuta_real. Relacion con planificacion_teorica (la Real parece copiarse de la Teorica: mismos platos pero raciones y costos distintos).
8. Dudas: costos con 6 decimales (ej. 1.140334 vs 1.140000 en teorica: la teorica usa costo de receta redondeado/vigente al planificar, la real recalcula con precio de inventario real). "INFUSIONES" con 0 raciones. Mar 6/10 y columnas posteriores cortadas.

---
# Resumen: entidades candidatas (capturas 16 a 30)

| Entidad candidata | Columnas clave | Imagenes |
|---|---|---|
| proveedor | ruc, razon_social, direccion, atte (contacto), fono(s), fax | 16, 17, 18 |
| pedido_compra (orden) | codigo_pedido LCL-06695-092026-ORCOPA_1, fecha, proveedor_id, contrato_id | 16, 17, 18 |
| contrato (unidad/sede) | codigo PE017401, nombre ORCOPAMPA FOALI, pais Peru | 19, 20-30 |
| usuario / sesion | usuario ALM274, estado Disponible | 19 |
| periodo y cierre_diario | contrato, fecha, estado (habilitado, cerrado y enviado, cerrado y no enviado), ultimo_cierre | 19, 20, 27, 29 |
| bitacora_traspaso | fecha_hora, contrato, mensaje "Traspaso Existoso" | 19 |
| regimen | codigo (1,2,3,5), descripcion | 20, 21, 27, 28, 29 |
| servicio | codigo (31,141,281,441,639,725,727,728,784,921), descripcion, regimen | 20, 21, 22-30 |
| planificacion_teorica (cabecera mes-servicio-contrato, estado Abierto/Cerrado) | contrato, regimen, servicio, fecha mm/aaaa, estado | 20, 21, 22-24 |
| planificacion_real (cabecera) | idem, estado | 27, 28, 29, 30 |
| planificacion_dia (estado por dia) | planificacion, fecha, estado | 20, 27, 29 |
| estructura_servicio (lineas: SOPA, PLATO DE FONDO, GUARNICION, BEBIDA FRIA/CALIENTE, COMPLEMENTO, PAN, ISLA, Comensales) | servicio, orden, nombre | 22, 23, 24, 30 |
| plan_menu_linea (plato por dia y linea con raciones y costo) | planificacion, fecha, linea, plato, n_raciones, costo_plato | 22, 23, 24, 30 |
| comensales_dia | planificacion, fecha, nro_comensales | 22, 23, 24, 30 |
| plato / receta | nombre, prefijo tipo (PUL, POLL, CER, PES), costo | 22-26, 30 |
| resumen_costos (Mat.Prima, Est.Fija, Cto.Total, Rac., Cto.Bandeja; planificado vs realizado; dia, acumulado, mes) | derivado | 23, 24 |
| frecuencia_receta / aporte_nutricional / copia_minuta (funciones de menu) | derivados | 25 |
