# Transcripcion de capturas SGP Peru (Sodexo) 61 a 75

Fuente: img/61.png ... img/75.png. Fechas del indice.csv: 61-71 = 2026-10-04 (13:24 a 13:26 en el nombre del archivo; el reloj de Windows dentro de las capturas marca 01:24-01:26 del 04/10/2026); 72-75 = 2026-10-05 (09:26-09:27).
Contrato comun a todas: PE017401 - ORCOPAMPA FOALI (Centro de Costo OPTIMUM: PE017401). Empresa: Sodexo Peru S.A.
Convenciones: "dec(N)" = decimales visibles. Los textos entre comillas son copia literal.

---------------------------------------------------------------------
## Captura 61 - Preview "Food Cost (Alimento)"
1. Ventana: "Preview" (visor de reportes, 1/10 paginas). Titulo del reporte: "Food Cost  (Alimento)". Ruta probable: Cierres/Costos > Informes > Food Cost (Alimento) (el menu no se ve; ver 62 para el selector de informes de costos).
2. Cabecera:
   - Sodexo Perú S.A.
   - Centro de Costo -> PE017401 - ORCOPAMPA FOALI
   - Centro de Costo OPTIMUM -> PE017401
   - Contrato -> 2010007950 - ORCOPAMPA FOALI (numero de contrato 2010007950, distinto del codigo de centro de costo PE017401)
   - Rango Fecha -> 01/09/2026 - 30/09/2026
3. Grilla (agrupada: Regimen > Servicio > dias). Cabecera de columnas en dos lineas:
   - Linea 1: Fecha | Servicio
   - Linea 2: Rac. Vendidas | Venta Día | Valor Bandeja | Rac. | Costo Día | Costo Bandeja | Food Cost
   Columnas en orden: Fecha (fecha dd/mm/aaaa), Rac. Vendidas (entero), Venta Día (dec 2, con miles), Valor Bandeja (dec 2), Rac. (entero = raciones producidas/planificadas), Costo Día (dec 2), Costo Bandeja (dec 2), Food Cost (porcentaje dec 2 con " %").
   Agrupadores: "1 - REGIMEN 1" (codigo 1, nombre REGIMEN 1) y debajo "31 - DESAYUNO NORMAL 1" (codigo servicio 31).
   Filas ejemplo:
   - 01/09/2026 | 494 | 2,119.26 | 4.29 | 524 | 1,709.29 | 3.26 | 80.66 %
   - 02/09/2026 | 487 | 2,089.23 | 4.29 | 517 | 2,440.21 | 4.72 | 116.80 %
   - 14/09/2026 | 409 | 1,754.61 | 4.29 | 439 | 2,194.59 | 5.00 | 125.08 %
   Relacion observada: Venta Día = Rac. Vendidas x Valor Bandeja (494 x 4.29 = 2,119.26). Costo Bandeja = Costo Día / Rac. (1,709.29/524=3.26). Food Cost = Costo Día / Venta Día (1709.29/2119.26=80.66 %).
   Valor Bandeja constante 4.29 en todos los dias vistos (precio de venta por racion/bandeja del servicio).
4. Barra: navegacion de paginas (primera/anterior, "1/10", siguiente/ultima), zoom (lupa + desplegable), imprimir, exportar a Excel, Word y PDF (iconos), salir (icono de puerta).
5. Leyendas de color: fondo amarillo claro en la cabecera de columnas; sin leyenda de colores.
6. Totales: no visibles en esta pagina (cortada en 24/09/2026).
7. Entidades implicitas:
   - contrato (codigo PE017401, numero_contrato 2010007950, nombre, centro_costo_optimum).
   - regimen (codigo 1, nombre) por contrato.
   - servicio (codigo 31, nombre DESAYUNO NORMAL 1) por regimen.
   - venta_diaria_servicio (contrato, fecha, servicio, raciones_vendidas, valor_bandeja, venta_dia).
   - costo_diario_servicio (contrato, fecha, servicio, raciones, costo_dia) -> Food Cost derivado.
8. Dudas: "Rac. Vendidas" vs "Rac." (producidas/realizadas) difieren (494 vs 524) - origen de cada una no visible (venta/facturacion vs produccion). Valor Bandeja probablemente viene de la tarifa del contrato/servicio. El agrupador "Regimen > Servicio" implica que hay mas regimenes/servicios en paginas siguientes (10 paginas).

---------------------------------------------------------------------
## Captura 62 - Ventana "Plan. Teórico & Realizado" (filtros de informes de costos)
1. Titulo: "Plan. Teórico & Realizado". Ruta probable: Costos/Cierres > Informes > Plan. Teórico & Realizado. Es un formulario de parametros que lanza reportes (ver 63, 64, 61, 68, 70).
2. Campos de cabecera:
   - Informes (lista desplegable) -> "Plan. Teórico & Realizado" (opciones en captura 63)
   - Contrato (codigo, gris, solo lectura) -> PE017401 ; icono mano para buscar/seleccionar contrato ; nombre -> ORCOPAMPA FOALI (cuadro gris)
   - Fecha Inicial (selector de fecha) -> 04/10/2026 (seleccionada en azul)
   - Fecha Final (selector de fecha) -> 04/10/2026
   - Grupo de opciones (radio): "Costo Alimentación" (marcado), "Costo Desechable", "Total Costo"
   - Casilla "Solamente Costo Totales" (fondo rojo, casilla sin marcar)
   - Marco "Regimen": radio "Todos" (marcado) / "Lista" (con icono mano para elegir lista de regimenes)
   - Marco "Servicio": radio "Todos" (marcado) / "Lista" (icono mano)
3. Grillas: ninguna.
4. Botones (barra superior de 3 iconos): vista previa/ejecutar informe (lupa sobre hoja), imprimir, salir (puerta). Iconos mano = selector de lista.
5. Colores: rojo en la etiqueta "Solamente Costo Totales" (llama la atencion de opcion que resume). Gris = solo lectura.
6. Totales: no aplica.
7. Entidades implicitas:
   - tipo_informe_costo (catalogo de los 6 informes de la lista 63).
   - tipo_costo (ALIMENTACION, DESECHABLE, TOTAL) - sugiere que existen costos de desechables ademas de alimentos (categoria de producto o de receta).
   - contrato, regimen, servicio (seleccion por lista o todos).
8. Dudas: la lista de regimenes/servicios que abre "Lista" no se ve. "Costo Desechable" implica productos clasificados como desechables.

---------------------------------------------------------------------
## Captura 63 - Lista desplegable "Informes"
1. Es el desplegable abierto de la ventana 62 (campo Informes). Detras se ve el fondo gris con parte del campo contrato.
2. Opciones (en orden exacto):
   1. Plan. Teórico & Realizado (seleccionada, resaltada en azul)
   2. Plan. Real & Realizado
   3. Plan. Teórico & Plan. Real & Realizado
   4. Plan. Teórico & Realizado Acumulado
   5. Plan. Real & Realizado Acumulado
   6. Plan. Teórico & Plan. Real & Realizado Acumulado
3. Grillas: ninguna. 4. Botones: flecha del combo. 5. Colores: seleccion azul. 6. Totales: no.
7. Entidades implicitas: catalogo fijo de tipos de informe de costo; refleja que existen TRES versiones de costo: Plan Teorico (menu planificado, costo por receta/standard), Plan Real (planificacion real, ver 72-75) y Realizado (consumo/produccion real, ver 70-71). Acumulado = suma acumulada del mes.
8. Dudas: ninguna adicional.

---------------------------------------------------------------------
## Captura 64 - Preview "Costo Plan. Teórico & Realizado (Alimentación) 09/2026"
1. Ventana "Preview" (1/14). Titulo: "Costo Plan. Teórico & Realizado (Alimentación) 09/2026". Ruta: resultado del informe de la 62.
2. Cabecera:
   - Centro de Costo : PE017401 - ORCOPAMPA FOALI
   - Centro de Costo OPTIMUM : PE017401
   - Contrato -> ORCOPAMPA FOALI
   - Regimen -> REGIMEN 1
   - Servicio -> DESAYUNO NORMAL 1
   - A la derecha (etiquetas sin valor visible): "Costo Piso", "Costo Techo"  (limites de costo por bandeja; valores no visibles)
3. Grilla (cabecera en dos niveles, grupos "Planif. Teórico", "Realizado", "Desviación"):
   Fecha (fecha) | Costo Bandeja (Planif. Teorico, dec 2) | Nro. Rac. (entero) | Costo Total (dec 2) | Costo bandeja (Realizado, dec 2) | Nro. Rac. (entero) | Costo Total (dec 2) | C.Band.Reali (Desviacion, dec 2, puede ser negativo)
   Filas:
   - 01/09/2026 | 2.59 | 550 | 1,426.54 | 3.03 | 564 | 1,709.29 | 0.44
   - 04/09/2026 | 3.75 | 550 | 2,064.99 | 3.12 | 564 | 1,761.43 | -0.63
   - 06/09/2026 | 3.13 | 288 | 902.52 | 3.55 | 577 | 2,047.84 | 0.42
   Relaciones: Costo Total teorico = Costo Bandeja x Nro. Rac. (2.59x550=1,424.5 ~1,426.54, redondeo de bandeja). Desviacion = Costo bandeja realizado - Costo bandeja teorico (3.03-2.59=0.44).
   Nota: el Costo Total Realizado del 01/09 (1,709.29) coincide con "Costo Día" de la captura 61 (mismo dato).
4. Barra: igual que 61 mas iconos de grafico de barras y grafico de lineas (dos iconos adicionales).
5. Colores: cabecera amarilla clara.
6. Totales: no visibles (pagina 1 de 14, cortada en 18/09).
7. Entidades implicitas: costo_teorico_diario (contrato, fecha, regimen, servicio, raciones_plan, costo_total, costo_bandeja), costo_realizado_diario (idem con raciones reales), parametros de costo piso/techo por servicio o contrato (tabla de limites).
8. Dudas: "Costo Piso"/"Costo Techo" sin valor en la captura. Nro. Rac. teorico = 550 en la mayoria de dias, 288 en domingos/sabados (06/09, 13/09 son domingos) -> planificacion por dia de semana. Esta pantalla no muestra "Plan Real".

---------------------------------------------------------------------
## Captura 65 - Preview "Previsión de Consumo (Total del Periodo)" - un dia
1. Ventana "Preview" (1/3). Titulo: "Previsión de Consumo (Total del Periodo)". Ruta probable: Planificacion/Compras > Prevision de Consumo.
2. Cabecera (franja gris):
   - Centro de Costo -> : PE017401 - ORCOPAMPA FOALI
   - Período -> : 04/10/2026 a 04/10/2026
3. Grilla: (codigo sin encabezado) | Producto | Cantidad | UN
   - codigo de producto (texto/numerico de 10-12 digitos, alineado a la derecha) ; Producto (texto, descripcion comercial con marca y presentacion) ; Cantidad (decimal 6 decimales, miles con coma) ; UN (codigo de unidad de 3 letras: LAT, BID, GLN, CAJ, KG, BOL, PQT, SCO, BLD).
   Filas:
   - 0101010194 | ATUN TROZOS GLORIA 170 GR | 334.588235 | LAT
   - 010101036994 | ACEITE VEGETAL CIELO 5 LT | 1.774800 | BID
   - 01010124225 | ACEITE DE OLIVA VIRGEN KUSASA 4 LT | 0.134000 | GLN
   - 010102041614 | ARROZ COSTEÑO EXTRA HOJA REDONDA AÑEJO 50 KG | 0.912000 | SCO
   - 01010425038 | AZUCAR RUBIA | 29.648472 | KG
   Orden: por codigo ascendente (texto), no por descripcion.
   Observacion: la cantidad esta expresada en la unidad de compra/presentacion del producto (ej. 0.912 sacos de 50 KG), con fracciones; 6 decimales.
4. Barra: navegacion, zoom, imprimir, Excel, Word, PDF, salir.
5. Colores: encabezado gris claro.
6. Totales: no visibles.
7. Entidades implicitas: producto (codigo, descripcion, unidad_codigo), unidad_medida (LAT, BID, GLN, CAJ, KG, BOL, PQT, SCO, BLD, UND, BLI...), prevision_consumo (contrato, periodo, producto, cantidad) - resultado calculado a partir de menu planificado x raciones x recetas/ingredientes (ver 69).
8. Dudas: no se ve si agrupa por categoria. Los codigos tienen longitud variable (10 a 12 digitos) y parecen jerarquicos (prefijos 0101..., 0102..., 0505...). Dependen de la tabla de productos (probablemente codigos de familia 01=abarrotes, 02=frescos/verduras, 03=carnes, 05=otros).

---------------------------------------------------------------------
## Captura 66 - Preview "Previsión de Consumo (Total del Periodo)" - varios dias
1. Igual que 65, pagina 1/5. Ruta igual.
2. Cabecera:
   - Sodexo Perú S.A.
   - Centro de Costo : PE017401 - ORCOPAMPA FOALI ; Centro de Costo OPTIMUM : PE017401
   - Centro de Costo -> : PE017401 - ORCOPAMPA FOALI
   - Período -> : 04/10/2026 a 27/10/2026
3. Grilla: (codigo) | Producto | Cantidad (dec 6) | UN
   - 0101010036 | LECHE CONDENSADA NESTLE 100 GR | 45.758800 | LAT
   - 0101010194 | ATUN TROZOS GLORIA 170 GR | 1,620.058824 | LAT
   - 010101036994 | ACEITE VEGETAL CIELO 5 LT | 64.207848 | BID
   - 0101020092 | ARVEJA VERDE PARTIDA CANTA CLARO BOLSA 500 GR | 34.720000 | BOL
   - 0101020113 | CHUÑO NEGRO SAN JUAN MASIAS (EX BELEN)5 KG | 2.405000 | BOL
   - 0101020264 | FIDEO SPAGUETTI DON VITTORIO 500 GR | 365.010000 | PQT
   Comparado con 65: el atun de un dia (334.588235 LAT) vs periodo (1,620.058824 LAT) - mismo producto, rango mayor.
4. Barra, colores, totales: igual que 65 (no hay totales).
5. Entidades: las mismas que 65. Esta version tiene mas paginas (5 vs 3).
6. Dudas: el periodo llega hasta 27/10/2026 (no fin de mes 31), coherente con que la planificacion real de 72-75 llega hasta 31/10. Los periodos son parametrizables.

---------------------------------------------------------------------
## Captura 67 - Preview menu impreso "ALMUERZO NORMAL 1" (estructura del servicio y platos de un dia)
1. Ventana "Preview" (pagina 5/8). Sin titulo de empresa en la parte visible; titulo de pagina: "ALMUERZO NORMAL 1". Ruta probable: Planificacion > Menus > Impresion de Menu (listado por dia, una pagina por dia: 5/8). Dia/fecha no visible en esta parte.
2. Cabecera: titulo "ALMUERZO NORMAL 1" (nombre del servicio).
3. Lista de dos columnas: Estructura de servicio (componente del menu) -> Plato/receta (nombre). Orden exacto:
   - ENTRADA CALIENTE 1 -> EMPANADA DE QUESO
   - SOPA NORMAL 1 -> SOPA DE QUINUA CON POLLO
   - SOPA DIETA -> SOPA DIETA DE POLLO
   - PLATO DE FONDO NORMAL 1 -> CORD - PACHAMANCA DE CORDERO
   - PLATO DE FONDO NORMAL 2 -> POLL - POLLADA ANDINA
   - ENSALADA CON CARNICO -> ENSALADA DE ATUN FEST
   - PLATO DE FONDO DIETA -> POLL - GUISO DE CALABAZA CON POLLO
   - GUARNICION ARROZ -> ARROZ BLANCO
   - GUARNICION 1 -> HABAS, CAMOTE Y CHOCLO
   - GUARNICION 2 -> PAPA COCKTAIL AL HORNO
   - GUARNICION DIETA 1 -> VERDURAS COCIDAS
   - COM SALAD BAR 1 -> SALAD BAR VII
   - ISLA 1 -> LIMON
   - ISLA 2 -> AJI - ROCOTO PICADO
   - ISLA 3 -> ALCUZA
   (la pagina continua mas abajo)
   Coincide con la columna "Dom 4/10/2026" de la captura 72 (EMPANADA DE QUESO, SOPA DE QUINUA CON POLLO, SOPA DIETA DE POLLO, CORD - PACHAMANCA DE CORDERO, POLL - POLLADA ANDINA, ENSALADA DE ATUN FEST, ...), por lo tanto este es el menu del 04/10/2026.
   Los nombres de platos llevan prefijos de proteina: "CORD - " (cordero), "POLL - " (pollo), "PES - " (pescado), "RES - " (res), "CER - " (cerdo), "PAV - " (pavo), "MON - " (?), "CMIX - " (?).
4. Barra: igual que 65. Fondo gris laterales (vista de pagina).
5. Colores: no hay leyenda.
6. Totales: no.
7. Entidades implicitas: servicio (ALMUERZO NORMAL 1), estructura_servicio / componente_servicio (ENTRADA CALIENTE 1, SOPA NORMAL 1, ... con orden), receta/plato (nombre), menu_dia_servicio_componente (contrato, fecha, servicio, componente, receta).
8. Dudas: no se ven raciones ni costos. Prefijos de proteina podrian ser un atributo (tipo de proteina) de la receta.

---------------------------------------------------------------------
## Captura 68 - Preview "Costo Detallado (Teórico) 10/2026"
1. Ventana "Preview" (1/8). Titulo: "Costo Detallado (Teórico)  10/2026" (mes/año). Ruta: Costos > Informes > Costo Detallado (Teorico).
2. Cabecera:
   - Contrato -> ORCOPAMPA FOALI
   - Regimen -> REGIMEN 1
   - Servicio -> DESAYUNO NORMAL 1
3. Grilla: Día | Nombre Receta | Costo Unit. | Nro. Rac. | Costo
   Tipos: Día (fecha), Nombre Receta (texto), Costo Unit. (dec 2, costo por racion de la receta), Nro. Rac. (entero), Costo (dec 2 = Costo Unit. x Nro. Rac.).
   Filas del 04/10/2026:
   - CALDO DE GALLINA | 1.71 | 336 | 574.56
   - MAIZ CANCHA | 0.35 | 288 | 100.80
   - JUGO SURTIDO I | 1.34 | 170 | 227.80
   - JUGO DE PIÑA CON MANZANA | 0.96 | 126 | 120.96
   - KIWICHA AVENA CON PIÑA | 0.33 | 473 | 156.09
   - CAFE PASADO | 0.33 | 114 | 37.62
   - INFUSIONES | 0.09 | 96 | 8.64
   - SALSA GUACAMOLE | 0.59 | 146 | 86.14
   - QUESO EDAM | 1.09 | 126 | 137.34
   - MANTEQUILLA CON SAL | 0.38 | 230 | 87.40
   - MERMELADA | 0.13 | 146 | 18.98
   - HUEVOS A LA ORDEN | 0.86 | 146 | 125.56
   - PAN FRANCES | 0.05 | 528 | 26.40
   - PAN DE YEMA | 0.05 | 528 | 26.40
   - LIMON | 0.07 | 96 | 6.72
   - AJI - ROCOTO PICADO | 0.10 | 70 | 7.00
4. Barra: igual a 61.
5. Colores: cabecera amarilla.
6. Totales:
   - Total Día -> 1,748.41 (columna Costo)
   - Total Servicio -> 8.43 (en Costo Unit.) y 1,748.41 (en Costo)
   - Costo Promedio Diario -> 8.43 (cortado al pie)
   El 8.43 es la suma de los Costo Unit. (costo por bandeja del dia). Ademas, el total 1,748.41 coincide con el Costo Total Teorico del 04/10; (ver 72: "Costo Minuta Día" 8.63 para 4/10 - otra cifra del Plan Real).
7. Entidades implicitas: menu_dia_servicio_receta (con nro_raciones por receta), receta (nombre, costo_unitario calculado), costo_teorico_receta_dia.
8. Dudas: el numero de raciones varia por receta dentro del mismo servicio (336, 288, 170...): son raciones por receta (porcentaje de comensales que la eligen o la cantidad planificada por plato), no un solo total por servicio. El "Costo Unit." no es costo de bandeja sino costo por racion de la receta; el costo de bandeja = suma = 8.43.

---------------------------------------------------------------------
## Captura 69 - Preview de equivalencias "Ingredientes / Productos"
1. Ventana "Preview" (1/12). Sin titulo visible (parte superior cortada). Ruta probable: Recetas > Listado Ingredientes por Producto (relacion ingrediente - productos comerciales equivalentes).
2. Cabecera: no visible.
3. Grilla: Ingredientes | Descripción | Productos | Descripción
   - Ingredientes: codigo numerico (entero, 1 a 4 digitos: 5, 4023, 48, 118, 124, 5003, 342, 362, 1007)
   - Descripción: nombre del ingrediente generico (texto)
   - Productos: codigo de producto (10-12 digitos, 5 digitos como 33598 tambien)
   - Descripción: nombre comercial del producto
   Filas:
   - 5 | ACEITE VEGETAL | 010101042263 | ACEITE VEGETAL CIELO 18 LT
   - 5 | ACEITE VEGETAL | 0101030051 | ACEITE VEGETAL CIL 5 LT
   - 5 | ACEITE VEGETAL | 0101030042 | ACEITE VEGETAL COCINERO 5 LT
   - 4023 | AGUA PARA RECETA | 33598 | CAJA CHICA - AGUA PARA RECETA - CH
   - 48 | AJI LIMO REFRIGERADO | 0102010001 | AJI LIMO
   - 118 | AVENA | 010103039237 | AVENA 3 OSITOS 100 GR
   - 118 | AVENA | 0101040458 | AVENA SANTA CATALINA SACO 10 KG
   - 124 | AZUCAR RUBIA | 01010425038 | AZUCAR RUBIA
   - 5003 | CAFE MOLIDO PARA PASAR KG | 01020136185 | CAFE MOLIDO PARA PASAR CAFETAL 454 GR
   - 342 | CALDO DE GALLINA | 0101070178 | CALDO DE GALLINA KNORR SACHET 1.5 KG
   - 1007 | FIDEO SPAGUETTI | 01010533623 | FIDEO SPAGUETTI BENOTI 10 KG
   Orden: alfabetico por descripcion del ingrediente.
4. Barra: igual a 61.
5. Colores: cabecera amarilla.
6. Totales: no.
7. Entidades implicitas: ingrediente (id entero, descripcion) - concepto generico usado en recetas; producto (codigo, descripcion); ingrediente_producto (relacion N a N: un ingrediente tiene varios productos comerciales sustituibles; un producto pertenece a un ingrediente). Es clave: las recetas usan INGREDIENTE y el consumo/compra se resuelve a PRODUCTO.
8. Dudas: no se ve si un producto puede estar en varios ingredientes, ni factor de conversion de unidades (ej. receta en KG, producto en SACO 50 KG) ni producto preferente. Existe "AGUA PARA RECETA" como ingrediente (producto 33598, codigo corto).

---------------------------------------------------------------------
## Captura 70 - Preview "Costos Detalle Período Realizado" (documento de salida de almacen, pagina 1)
1. Ventana "Preview" (1/3). Titulo: "Costos Detalle Período Realizado". Ruta: Costos > Informes > Costos Detalle Periodo Realizado (detalle de documentos de consumo/produccion).
2. Cabecera:
   - Sodexo Perú S.A.
   - Centro de Costo : PE017401 - ORCOPAMPA FOALI ; Centro de Costo OPTIMUM : PE017401
   - Nº Documento -> 63167
   - F. Emisión -> 04/10/2026
   - F. Producción -> 04/10/2026
   - Contrato -> PE017401 - ORCOPAMPA FOALI
   - Bodega -> 433
   - Servicios -> REGIMEN 1 - DESAYUNO NORMAL 1
3. Grilla: Código | Descripción | Unid. | Costo Unitario | Cantidad | Costo Total
   Tipos: Código (texto), Descripción (texto), Unid. (codigo unidad), Costo Unitario (dec 2), Cantidad (dec 3), Costo Total (dec 2 = Costo Unitario x Cantidad)
   Filas:
   - 0103020010 | GALLINA ENTERA SIN MENUDENCIA CONGELADO - IMPORTADO | KG | 6.49 | 40.800 | 264.71
   - 0102020091 | PULPA DE FRESA  CONGELADO | KG | 8.13 | 3.000 | 24.40
   - 0102030081 | QUESO EDAM EN TAJADAS X 500 GR CONGELADO | PQT | 18.11 | 8.000 | 144.88
   - 050402042247 | HUEVO ROJO (CAJA 11.5 KG) | CAJ | 80.50 | 3.170 | 255.19
   - 0101040511 | CAFE MOLIDO NESCAFE TRADICION NESTLE 500 GR | BOL | 140.36 | 1.000 | 140.36
4. Barra: igual a 61.
5. Colores: cabecera amarilla.
6. Totales: ver captura 71.
7. Entidades implicitas: documento_consumo (numero 63167, fecha_emision, fecha_produccion, contrato, bodega 433, regimen, servicio) y detalle_documento_consumo (producto, unidad, costo_unitario, cantidad, costo_total). Parece una salida/consumo de almacen por servicio y dia (vale de consumo/requisicion a produccion).
8. Dudas: "Bodega 433" es un codigo numerico (nombre no visible; podria ser id de almacen). F. Emision = F. Produccion en este caso. El documento 63167 es DESAYUNO NORMAL 1 del 04/10/2026 (costo total 1,939.18 en 71) y el 63169 el siguiente (otro servicio). La numeracion salta (63167 -> 63169), por lo que habra un 63168 (probablemente otro documento/servicio o anulado).

---------------------------------------------------------------------
## Captura 71 - Preview "Costos Detalle Período Realizado" (documento, desplazado)
1. Misma pantalla que 70 desplazada hacia abajo (pagina 1/3). Muestra el bloque completo del documento 63167 hasta el total y el inicio del documento 63169.
2. Cabecera (parcial): F. Producción 04/10/2026 ; Servicios REGIMEN 1 - DESAYUNO NORMAL 1. Al pie aparece el siguiente documento: "Nº Documento 63169" y "Contrato PE017401 - ORCOPAMPA FOALI".
3. Filas adicionales del detalle (mismas columnas):
   - 0102020017 | PLATANO DE SEDA | KG | 5.00 | 11.000 | 55.00
   - 0102010045 | PAPA AMARILLA | KG | 6.50 | 15.000 | 97.50
   - 0102010006 | CEBOLLA ROJA | KG | 3.33 | 2.000 | 6.66
   - 01020137022 | MERMELADA FRESA TRES ESPADAS 14 GR | BLI | 0.26 | 200.000 | 52.00
   - 0505040005 | MANTEQUILLA GLORIA PERSONAL 10 GR | UND | 0.38 | 324.000 | 123.12
   - 010106037527 | HARINA ESPECIAL DEL CIELO GRANEL SACO 50 KG | SCO | 136.78 | 0.300 | 41.03
   - 01010623177 | HARINA DE KIWICHA 5 KG | BOL | 83.33 | 1.000 | 83.33
   - 010105042270 | FIDEO SPAGUETTI MOLITALIA 950 GR | PQT | 4.55 | 12.000 | 54.64
   - 01020234896 | CALDO CONCENTRADO GALLINA MACRO FOOD 1 KG | BOL | 17.56 | 0.500 | 8.78
   - 01010434914 | AZUCAR RUBIA SACO 50 KG | SCO | 135.82 | 0.400 | 54.33
   - 01020125401 | INFUSION TE PURO DEL VALLE 100 SOBRES | CAJ | 5.15 | 1.000 | 5.15
   - 01020125403 | INFUSION TE BOLDO DEL VALLE 100 SOBRES | CAJ | 6.23 | 1.000 | 6.23
   Ultima linea del detalle: 01020125403 INFUSION TE BOLDO...
4. Totales: "Total Servicio" -> 1,939.18 (suma del Costo Total del documento 63167).
   Comparacion: Costo Total Realizado del 04/10 en 68 (teorico) es 1,748.41; el realizado de este documento es 1,939.18, es decir hay diferencia teorico vs realizado.
5. Entidades: iguales a 70. Un reporte contiene varios documentos seguidos (uno por servicio/dia), cada uno con encabezado y "Total Servicio".
6. Dudas: no se ve el total periodo ni la separacion entre documentos de distinto servicio (63169 ya cabecera nueva). Unidades observadas: KG, PQT, CAJ, BOL, UND, BLI (blister), SCO (saco).

---------------------------------------------------------------------
## Captura 72 - Ventana "Planificación Real" (1 a 6 oct 2026)
1. Titulo de ventana: "Planificación Real". Menu de ventana: "Menú", "Plato Menú". Ruta probable: Planificacion > Planificacion Real (el nombre del titulo del contenido: "ORCOPAMPA FOALI(PE017401) - REGIMEN 3 - ALMUERZO NORMAL 1").
2. Cabecera:
   - Linea de titulo (fondo amarillo): "ORCOPAMPA FOALI(PE017401) - REGIMEN 3 - ALMUERZO NORMAL 1"  -> contrato(codigo) - regimen - servicio. Aqui es REGIMEN 3 (distinto del REGIMEN 1 de los reportes) y el servicio ALMUERZO NORMAL 1.
   - Nota: "Nota : Las raciones debe incluir las raciones del personal"
   - Fila "Costo Minuta Día": valores encima de cada columna de dia: 8.63 (sobre Jue 1/10), 6.93 (Vie 2/10), 10.71 (Sáb 3/10), 7.68 (Dom 4/10), 8.12 (Lun 5/10) (Mar 6/10 parcial).
3. Grilla (matriz estructura x dias). Fila 1 de cabecera: "Costo Minuta Día". Fila 2: "Estructura Servicio" | por cada dia un bloque de 3 sub-columnas: [marca R] | nombre del dia con fecha (Jue 1/10/2026, Vie 2/10/2026, Sáb 3/10/2026, Dom 4/10/2026, Lun 5/10/2026, Mar 6/10/2026) con el nombre de la receta | N.Rac. (entero, azul) | Costo (dec 6).
   Numeracion de filas 1 a 33 (columna izquierda, numero de orden).
   Filas de "Estructura Servicio" (fondo verde claro, orden exacto):
   1 ENTRADA FRIA 1; 2 ENTRADA FRIA 2; 3 ENTRADA CALIENTE 1 (cortado: "ENTRADA CALIENTI"); 4 ENTRADA CALIENTE 2 (cortado); 5 COMPLEMENTO 1; 6 SOPA NORMAL 1; 7 SOPA DIETA; 8 PLATO DE FONDO NORMAL 1 (cortado); 9 PLATO DE FONDO NORMAL 2 (cortado); 10 PLATO DE FONDO N... (cortado, vacio); 11 ENSALADA CON CARNICO (cortado "ENSALADA CON CAI"); 12 PLATO DE FONDO DIETA (cortado); 13 GUARNICION ARROZ (cortado); 14 GUARNICION 1; 15 GUARNICION 2; 16 GUARNICION DIETA 1 (cortado); 17 GUARNICION 4; 18 GUARNICION 5; 19 GUARNICION 6; 20 COM SALAD BAR 1; 21 ISLA 1; 22 ISLA 2; 23 ISLA 3; 24 ISLA 4; 25 ISLA 5; 26 ISLA 6; 27 ISLA 7; 28 FRUTA 1; 29 POSTRE NORMAL 1; 30 POSTRE NORMAL 2; 31 (vacia); 32 BEBIDA FRIA 1; 33 Comensales (fila gris, solo N.Rac.).
   Los nombres exactos truncados no se leen completos: se escribe "(cortado)". Los de 3, 4, 8, 9, 10, 11, 12, 13, 16 estan cortados en la grilla.
   Filas ejemplo de celdas (dia, receta, N.Rac., Costo):
   - Fila 1 ENTRADA FRIA 1: Jue 1/10: ENSALADA RUSA | 442 | 0.457382 ; Vie 2/10: HUEVO RELLENO DE VERDURA | 143 | 0.781459 ; Sáb 3/10: ENSALADA DE LEGUMBRES | 460 | 0.424187 ; Lun 5/10: SOLTERITO | 120 | 1.767386
   - Fila 6 SOPA NORMAL 1: Jue 1/10: SOPA DE CHUÑO NEGRO | 473 | 2.704629 ; Vie 2/10: SOPA DE MORON CON POLLO | 489 | 0.932364 ; Sáb 3/10: SOPA CHAIRO CON RES I | 490 | 2.503035 ; Dom 4/10: SOPA DE QUINUA CON POLLO | 210 | 1.167797 ; Lun 5/10: CALDO DE RES | 480 | 1.889460
   - Fila 8 PLATO DE FONDO NORMAL 1: Jue 1/10: RES - OLLUQUITO CON RES | 196 | 5.208646 ; Vie 2/10: POLL - POLLADA | 326 | 1.751965 ; Sáb 3/10: CMIX - PARRILLA MIXTA | 510 | 3.601503 ; Dom 4/10: CORD - PACHAMANCA DE CORD(ERO) | 80 | 5.583332 ; Lun 5/10: PES - PESCADO ENCEBOLLADO | 180 | 3.139242
   - Fila 20 COM SALAD BAR 1: Jue 1/10: SALAD BAR IV | 350 | 0.716572 ; Vie 2/10: SALAD BAR V | 357 | 0.667330 ; Sáb: SALAD BAR VI | 400 | 0.432500 ; Dom: SALAD BAR VII | 119 | 0.501900
   - Fila 21 ISLA 1: LIMON | 520 | 0.067500 (todos los dias)
   - Fila 22 ISLA 2: AJI - ROCOTO PICADO | 197 | 0.103701
   - Fila 33 Comensales: 520 (Jue), 520 (Vie), 520 (Sáb), 240 (Dom), 520 (Lun), 520 (Mar).
   Cada celda de receta tiene un indicador "R" (letra roja sobre fondo, al lado izquierdo) en el inicio de la celda cuando la receta esta asignada.
4. Botones / barra de iconos (de izquierda a derecha, sin rotulos): guardar (disquete), cortar, copiar, pegar, pegar especial, buscar (binoculares), insertar columna/fila, quitar/mover (iconos de lineas con flecha), flecha arriba, flecha abajo, icono de recalcular/cronometro, salir con flecha roja, estrella/copo (preferencias?), signo "$" (costos), iconos de graficos y calendario (reporte/costos), Excel, salir. Menus: "Menú" y "Plato Menú".
5. Leyendas de colores (en pantalla):
   - Verde claro = "Estructura de Servicio" (columna izquierda de componentes)
   - Rojo/rosado = "Celda Bloqueada" (dias ya cerrados/pasados: Jue 1, Vie 2, Sáb 3 estan rosados; no editables)
   - Amarillo claro = "Celda Habilitada" (dias editables: Dom 4/10 en adelante)
   Marca "R" en verde claro (celdas habilitadas) o en rojo/rosa (celdas bloqueadas): indicador de receta.
6. Totales: fila "Costo Minuta Día" arriba (costo de bandeja del dia: 8.63, 6.93, 10.71, 7.68, 8.12); fila "Comensales" al pie (raciones del dia).
7. Entidades implicitas:
   - planificacion_real (contrato, regimen, servicio, fecha) con estado bloqueado/habilitado (por fecha/cierre).
   - planificacion_real_detalle (planificacion, componente/estructura_servicio, receta, nro_raciones, costo_receta).
   - estructura_servicio (servicio, orden 1-32, nombre del componente).
   - comensales_dia (contrato, regimen, servicio, fecha, raciones_totales).
   - Costo Minuta Día = suma de Costo por componentes (derivado).
8. Dudas: la pantalla alterna con "Plan Teorico" (menu base) y se ve el REGIMEN 3 (los reportes anteriores mostraban REGIMEN 1) - hay varios regimenes por contrato. El texto de nota indica que raciones deben incluir raciones del personal. Las celdas bloqueadas son los dias pasados (hasta el 3/10); hoy (4/10) ya esta habilitado. Costo = costo por racion de la receta (dec 6) en moneda local (PEN presumiblemente, no se ve simbolo).

---------------------------------------------------------------------
## Captura 73 - Planificación Real (7 a 12 oct 2026)
1. Misma ventana y titulo que 72: "ORCOPAMPA FOALI(PE017401) - REGIMEN 3 - ALMUERZO NORMAL 1". Mismas leyendas.
2. Fila "Costo Minuta Día": 10.81 (Mié 7/10), 5.56 (Jue 8/10), 7.70 (Vie 9/10), 7.65 (Sáb 10/10), 12.36 (Dom 11/10), (Lun 12/10 cortado).
3. Grilla: mismas columnas. Dias: Mié 7/10/2026, Jue 8/10/2026, Vie 9/10/2026, Sáb 10/10/2026, Dom 11/10/2026, Lun 12/10/2026. Todas las celdas amarillas (habilitadas).
   Filas ejemplo:
   - Fila 1 ENTRADA FRIA 1: Mié: PES - CEVICHE DE PESCADO | 540 | 4.014691 ; Jue: ENSALADA DE PALTA | 159 | 1.147864 ; Vie: HUEVOS AL NIDO | 133 | 0.441053 ; Sáb: PAPA A LA CREMA DE OCOPA | 181 | 1.198868 ; Dom: SALPICON DE JAMON | 126 | 0.800721 ; Lun: ENROLLADO DE JAMON PRIMAV(ERA)
   - Fila 6 SOPA NORMAL 1: SOPA PATASCA | 500 | 1.868241 ; SOPA CAMPESINA DE POLLO | 481 | 0.975449 ; SOPA PUCHERO DE POLLO | 481 | 1.024948 ; SOPA CALDO BLANCO CON RES | 473 | 0.000000 ; CHUPE DE PESCADO | 230 | 3.441029 ; SOPA PEBRE CON POLLO | 481
   - Fila 8 PLATO DE FONDO NORMAL 1: RES - MALAYA DORADA | 230 | 4.079545 ; MON - PICANTE A LA TACNEÑA | 111 | 3.136725 ; PES - TRUCHA FRITA | 182 | 5.805070 ; ALMUERZO TEMATICO (CHICH...) | 500 | 4.596302 ; RES - BISTECK A LA PARRILLA | 147 | 5.068418
   - Fila 28 FRUTA 1: NARANJA DE MESA | 220 | 0.000000 ; TUNA ROJA | 265 | 0.000000 ; UVA NEGRA | 265 | 1.161000 ; MANDARINA | 259 | 0.654750 ; SANDIA | 126 | 0.909000 ; PLATANO DE SEDA (celda seleccionada con borde negro, Lun 12/10)
   - Fila 32 BEBIDA FRIA 1: REFRESCO DE MANZANA | 918 | 0.164262 ; REFRESCO DE MARACUYA | 901 | 0.226405 ; CHICHA MORADA | 881 | 0.193976
   - Fila 33 Comensales: 540, 530, 530, 518, 252, 530.
   Observaciones de calidad de datos: hay recetas con costo 0.000000 (SOPA CALDO BLANCO CON RES 473 raciones; NARANJA DE MESA; TUNA ROJA) = receta sin costo (precio/ingrediente faltante).
   Hay filas con el costo en rojo dentro de la columna N.Rac. (valores en azul).
4. Resto: igual a 72. Fila 10 "PLATO DE FONDO N..." y filas 17-19 (GUARNICION 4-6) sin recetas esta semana; fila 31 vacia sin nombre.
5. Entidades: iguales a 72. Detalle adicional: "ALMUERZO TEMATICO (CHICH..." (receta tematica con 500 raciones, costo 4.596302). Celda con foco (borde negro) en Lun 12/10 fila 28.
6. Dudas: las columnas "R" son un indicador por celda (tal vez "Receta asignada/Real"). El domingo (11/10) tiene 252 comensales; sabado 10/10 518.

---------------------------------------------------------------------
## Captura 74 - Planificación Real (27 a 31 oct 2026)
1. Mismas ventana y titulo. Dias: Mar 27/10/2026, Mié 28/10/2026, Jue 29/10/2026, Vie 30/10/2026, Sáb 31/10/2026 (la primera columna de dia a la izquierda es un dia anterior parcialmente visible solo con N.Rac. y Costo, sin nombre de receta). Todas las celdas amarillas.
2. Fila "Costo Minuta Día": 7.68 (columna anterior), 6.69 (Mar 27), 11.30 (Mié 28), 7.47 (Jue 29), 5.97 (Vie 30), 5.91 (Sáb 31).
3. Filas ejemplo:
   - Fila 1 ENTRADA FRIA 1: Mar 27: ENSALADA RUSA | 186 | 0.457382 ; Mié 28: PES - CEVICHE DE PESCADO | 530 | 4.014691 ; Jue 29: PAPA A LA HUANCAINA | 133 | 1.106118 ; Vie 30: HUEVO RELLENO DE VERDURA | 450 | 0.781459 ; Sáb 31: CAUSA DE POLLO | 259 | 0.938424
   - Fila 3 ENTRADA CALIENTE 1: columna anterior: 159 | 1.551030 ; Jue 29: ENTRA - CHICHARRON DE POLL(O) | 318 | 1.123689
   - Fila 8 PLATO DE FONDO NORMAL 1: Mar 27: RES - MALAYA DORADA | 214 | 1.493953 ; Mié 28: POLL - POLLO A LA CHICLAYANA | 322 | 4.079545 ; Jue 29: CER - CARAPULCRA CON SOPA S(ECA) | 347 | 3.461681 ; Vie 30: RES - CAIGUA RELLENA | 164 | 2.955349 ; Sáb 31: CER - CERDO AL HORNO | 237 | 2.279231
   - Fila 9 PLATO DE FONDO NORMAL 2: Mar 27: MON - PICANTE A LA TACNEÑA | 296 | 3.136725 ; Mié 28: RES - PICANTE DE RES | 189 | 4.797743 ; Jue 29: PES - TRUCHA APANADA | 163 | 5.978339 ; Vie 30: POLL - ESTOFADO DE POLLO | 346 | 1.800734 ; Sáb 31: POLL - POLLADA | 259 | 1.751965
   - Fila 28 FRUTA 1: Mar 27: SANDIA | 265 | 0.825000 ; Mié 28: MANDARINA | 265 | 0.909000 ; Jue 29: TUNA ROJA | 265 | 0.000000 ; Vie 30: PLATANO DE SEDA | 265 | 0.675000 ; Sáb 31: SANDIA | 259 | 0.909000
   - Fila 33 Comensales: 530, 530, 530, 530, 518 (celda final seleccionada con borde negro, 518 y vacia a la derecha).
4. Leyendas, barra: igual a 72.
5. Totales de pie de pantalla: no visibles en esta captura (ver 75).
6. Dudas: la pantalla muestra todo el mes hasta el 31/10; los dias 1-3 estan bloqueados (rosado) y el resto habilitados.

---------------------------------------------------------------------
## Captura 75 - Planificación Real con panel de totales al pie (27 a 31 oct 2026)
1. Misma pantalla que 74 con ventana mas grande: se ve la barra de desplazamiento y debajo el panel de totales. Mismos dias (Mar 27/10 ... Sáb 31/10) y mismos valores de grilla que 74.
2. Panel de resumen inferior (tres cuadros):
   a) Marco "Total Mes" (cabeceras: Cto. Bandeja | Costo Total)
      - Mat.Prima: 8.39 | 127,684.72
      - Est.Fija: 0.00 | 0.00
      - Total: 8.39 | 127,684.72
      - Rac.: 15,218.00 (solo en la columna Costo Total)
   b) Segundo cuadro (sin marco rotulado; columnas Planificado | Realizado):
      - Mat.Prima: 4,485.92 | (vacio)
      - Est.Fija: 0.00 | (vacio)
      - Cto.Total: 4,485.92 | 4,649.83
      - Rac.: 520.00 | 520.00
      - Cto.Band.: 8.63 | 8.94
   c) Marco "Acumulado hasta" (columnas Planificado | Realizado):
      - Mat.Prima: 4,485.92 | (vacio)
      - Est.Fija: 0.00 | (vacio)
      - Cto.Total: 4,485.92 | 4,649.83
      - Rac.: 520.00 | 520.00
      - Cto.Band.: 8.63 | 8.94
   Iconos de grafico de barras y de lineas encima del panel (para ver graficos).
   Interpretacion del cuadro b: corresponde al dia seleccionado/actual (4/10 con Costo Minuta 8.63 de la captura 72 pasa a ser el costo bandeja planificado; el acumulado es igual porque aun solo hay un dia con realizado).
3. Botones: iconos de grafico (barras y lineas) sobre el panel.
4. Colores: campos de totales en gris (solo lectura).
5. Entidades implicitas: resumen_costo_mes (contrato, regimen, servicio, mes: costo_materia_prima, costo_estructura_fija, total, raciones, costo_bandeja), resumen_planificado_vs_realizado (acumulado hasta fecha de corte). "Est.Fija" (Estructura Fija) = costo de componentes fijos (p. ej. desechables, gas, mano de obra u otros). "Mat.Prima" = suma de costo de ingredientes.
6. Dudas: Total Mes tiene 15,218 raciones y 127,684.72 de costo total -> 8.39 por bandeja. "Est.Fija" siempre 0.00 en la captura (puede ser parametro no usado). "Realizado" para Mat.Prima y Est.Fija en blanco - solo se muestra Realizado en Cto.Total, Rac., Cto.Band.

---------------------------------------------------------------------
# RESUMEN: Entidades candidatas (capturas 61-75)

| Entidad candidata (nombre sugerido) | Columnas clave / atributos | Relaciones | Capturas |
|---|---|---|---|
| contrato | codigo (PE017401), numero_contrato (2010007950), nombre (ORCOPAMPA FOALI), centro_costo_optimum (PE017401) | 1 contrato -> N regimenes | 61, 62, 64, 65, 66, 68, 70, 72-75 |
| regimen | codigo (1, 3), nombre (REGIMEN 1, REGIMEN 3) | pertenece a contrato; tiene N servicios | 61, 64, 68, 70, 72-75 |
| servicio | codigo (31), nombre (DESAYUNO NORMAL 1, ALMUERZO NORMAL 1) | pertenece a regimen | 61, 64, 67, 68, 70, 72-75 |
| estructura_servicio (componente del servicio) | servicio_id, orden (1-33), nombre (ENTRADA FRIA 1 ... BEBIDA FRIA 1, Comensales) | N componentes por servicio; las celdas de plan referencian el componente | 67, 72-75 |
| receta / plato | id, nombre (con prefijo de proteina POLL/RES/PES/CER/CORD/MON/CMIX/PAV), costo_unitario | se asigna a un componente por dia; contiene ingredientes | 67, 68, 72-75 |
| plan_teorico (menu base) | contrato, regimen, servicio, fecha, receta, nro_raciones, costo_unit, costo | derivado de la receta | 64, 68 |
| planificacion_real | contrato, regimen, servicio, fecha, estado_bloqueo (bloqueada/habilitada) | cabecera del dia | 72-75 |
| planificacion_real_detalle | planificacion_id, componente, receta_id, nro_raciones, costo | N por dia; 1 receta por celda | 72-75 |
| comensales_dia | contrato, regimen, servicio, fecha, raciones (fila "Comensales") | por servicio/dia | 72-75 |
| costo_minuta_dia (derivado) | fecha, costo_bandeja = suma costos de componentes | resumen de planificacion_real_detalle | 72-74 |
| resumen_costo_mes (derivado) | mes, mat_prima, est_fija, total, raciones, costo_bandeja, planificado vs realizado, acumulado | contrato/servicio | 75 |
| venta_diaria_servicio | contrato, fecha, servicio, raciones_vendidas, valor_bandeja, venta_dia | calcula Food Cost | 61 |
| costo_diario_realizado | contrato, fecha, servicio, raciones, costo_dia, costo_bandeja, food_cost_pct | se alimenta de documentos de consumo | 61, 64, 71 |
| costo_diario_teorico / plan | fecha, costo_bandeja, nro_rac, costo_total | desviacion = realizado - teorico | 64 |
| limite_costo_servicio (costo piso / costo techo) | contrato/servicio, costo_piso, costo_techo | parametrizacion | 64 (etiquetas sin valor) |
| tipo_informe_costo (catalogo) | 6 informes: Plan Teorico & Realizado, Plan Real & Realizado, Teorico & Real & Realizado, y 3 acumulados | catalogo fijo | 62, 63 |
| tipo_costo | ALIMENTACION, DESECHABLE, TOTAL | clasificacion de producto/costo | 62 |
| ingrediente | id entero (5, 48, 118, 124, 342, 362, 1007, 4023, 5003), descripcion | N a N con producto | 69 |
| producto | codigo (10-12 digitos; codigos cortos como 33598), descripcion, unidad | referenciado por consumo, prevision, almacen | 65, 66, 69, 70, 71 |
| ingrediente_producto (equivalencia) | ingrediente_id, producto_codigo | N a N | 69 |
| unidad_medida | codigo (KG, LAT, BID, GLN, CAJ, BOL, PQT, SCO, BLD, UND, BLI, ...) | usada por producto | 65, 66, 70, 71 |
| prevision_consumo (resultado calculado) | contrato, periodo_desde, periodo_hasta, producto, cantidad dec 6 | calculada desde plan x receta x ingrediente | 65, 66 |
| documento_consumo (salida de almacen por servicio) | numero (63167, 63169), f_emision, f_produccion, contrato, bodega (433), regimen+servicio | cabecera de salida de almacen | 70, 71 |
| documento_consumo_detalle | documento, producto, unidad, costo_unitario, cantidad (dec 3), costo_total | N por documento | 70, 71 |
| bodega / almacen | codigo 433 (nombre no visible) | por contrato | 70 |
| menu_dia_servicio (menu impreso) | fecha, servicio, componente, receta | listado por dia | 67 |

Dependencias entre pantallas / dudas generales:
- El costo de una receta (Costo en 72-75, Costo Unit. en 68) debe venir de ingredientes x precio del producto elegido (69, 71); las recetas con costo 0.000000 (73-75) indican productos/ingredientes sin precio.
- Existen tres pantallas de costo: Teorico (64, 68), Plan Real (72-75) y Realizado (61, 70, 71). Los acumulados se calculan sobre cualquiera.
- Los regimenes cambian entre pantallas (REGIMEN 1 en reportes, REGIMEN 3 en Planificacion Real): hay varios regimenes por contrato y cada uno con sus servicios.
- No se ven valores de "Costo Piso" y "Costo Techo" (64).
