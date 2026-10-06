# Transcripcion de capturas SGP Peru 31 a 45

Fechas (indice.csv): 31 = 2026-10-04 13:17:16; 32 = 13:17:21; 33 = 13:17:31; 34 = 13:17:51; 35 = 13:17:58; 36 = 13:18:28; 37 = 13:18:45; 38 = 13:18:52; 39 = 13:19:05; 40 = 13:19:15; 41 = 13:19:22; 42 = 13:20:07; 43 = 13:20:17; 44 = 13:20:24; 45 = 13:20:36 (todas 2026-10-04).
Contrato en todas: PE017401 = ORCOPAMPA FOALI. Bodega: PE017401 - FOALI.

---------------------------------------------------------------
## 31 - Planificacion Real (semana 27/10 a 31/10/2026)

1. Ventana "Planificacion Real". Menu de la ventana: "Menu", "Plato Menu". Ruta probable: Planificacion > Planificacion Real.
2. Cabecera: banda amarilla con titulo "ORCOPAMPA FOALI(PE017401) - REGIMEN 1 - DESAYUNO NORMAL 1" (formato: Contrato(codigo) - Regimen - Servicio). Nota: "Las raciones debe incluir las raciones del personal".
3. Grilla (matriz): filas = estructura del servicio; por cada dia 3 columnas (plato, Por.[%], Costo) precedidas de una columna con marca "R" (verde = celda de estructura/plato asignado).
   - Fila encabezado 1: "Costo Minuta Dia" con un valor por dia: 3.47 (Mar 27/10), 2.90 (Mie 28/10), 4.27 (Jue 29/10), 3.40 (Vie 30/10), 3.61 (Sab 31/10), 3.43 (segunda cifra al lado de Sab, columna despues). Dos columnas finales: "Estado GH" y "Estado GI" (otra "Es..." cortada).
   - Fila encabezado 2: "Estructura Servicio" | por dia: "<Dia> dd/mm/aaaa" (ej. "Mar 27/10/2026") | "Por.[%]" | "Costo". Primera columna "Costo" (costo del item de la estructura, decimal 6) antes de los dias.
   - Filas numeradas 1 a 23. Estructura Servicio (col. verde): 1 SOPA NORMAL; 2 PLATO DE FONDO N(ORMAL, cortado); 3 GUARNICION ARRO(Z, cortado); 4 GUARNICION 1; 5 BEBIDA FRIA 1; 6 BEBIDA FRIA 2; 7 BEBIDA CALIENTE 1; 8 BEBIDA CALIENTE 2; 9 BEBIDA CALIENTE 3; 10 BEBIDA CALIENTE 4; 11 COMPLEMENTO 1; 12 COMPLEMENTO 2; 13 COMPLEMENTO 3; 14 COMPLEMENTO 4; 15 COMPLEMENTO 5; 16 PAN 1; 17 PAN 2; 18 PAN 3; 19 PAN 4; 20 PAN 5; 21 ISLA 1; 22 ISLA 2; 23 Comensales (fila de totales de raciones).
   - Tipos: plato = texto (nombre de receta); Por.[%] = decimal 2 con signo %; Costo = decimal 6; Comensales = entero.
   - Ejemplos (fila 2 PLATO DE FONDO, costo base 1.847341): Mar 27/10 "POLL - CEVICHE DE POLLO" 65.00 % 0.803373; Jue 29/10 "PES - PESCADO A LA CHORRILLA" 50.00 % 1.901277; Sab 31/10 "POLL - ESCABECHE DE POLLO" 64.98 % 1.339312.
   - Fila 5 BEBIDA FRIA 1 (costo base 1.033742): Mar "JUGO DE NARANJA" 19.81 % 1.385000; Mie "JUGO DE PAPAYA" 33.85 % 1.191817.
   - Fila 16 PAN 1 (0.046984): "PAN FRANCES" 110.00 % 0.046984. Fila 17 PAN 2 (0.051189): "PAN DE YEMA" 110.00 % 0.051189. Sab: "PAN ARABE" 44.36 % 0.047408; PAN 3 "TOSTADAS INTEGRALES" 0.064073; PAN 4 "PAN DE MAIZ" 0.076046; PAN 5 "PAN CACHITO" 0.071612.
   - Fila 21 ISLA 1 Mie: "LIMON" 19.81 % 0.067500; fila 22 ISLA 2 Mie: "SALSA ROCOTO" 16.15 % 0.134030.
   - Mas ejemplos: COMPLEMENTO 1 Mie "HOT DOG" 21.15 % 0.320128; COMPLEMENTO 3 "MANTEQUILLA CON SAL" 48.08 % 0.380000; COMPLEMENTO 5 "HUEVOS A LA ORDEN" 30.19 % 0.861320.
   - Comensales: 520, 520, 520, 520, 514 (Sab); Estado GH 0, Estado GI 0.
   - Celda seleccionada: Sab 31/10 fila 12 "PALTA" 30.35 % 1.008691.
   - Algunas celdas sin plato (vacias), ej. GUARNICION 1 sin plato el Mie; BEBIDA CALIENTE 4 solo con leche en algunos dias ("LECHE CALIENTE I" 15.00 % 0.751995).
4. Barra de iconos (sin texto, funcion probable): guardar (disquete), cortar, copiar, pegar, pegar especial, buscar (binoculares), insertar/quitar linea (2 iconos de sangria), flecha arriba, flecha abajo, (reloj/historial), salir/eliminar (flecha roja), iconos de estrella/copos, "$" (costos), imagen/grafico, calculo, borrador, Excel, salir. Los nombres exactos son (ilegible); solo "$" y Excel son identificables.
5. Leyenda: verde claro = "Estructura de Servicio"; rojo/rosa = "Celda Bloqueada" (dia ya cerrado/pasado, no editable); amarillo claro = "Celda Habilitada" (editable). Marca "R" verde a la izquierda de cada plato.
6. Totales al pie: fila "Comensales" (raciones por dia). Fila superior "Costo Minuta Dia" (suma de costos del dia).
7. Entidades implicitas:
   - contrato (codigo PE017401, nombre), regimen, servicio (Desayuno Normal 1), contrato_servicio.
   - estructura_servicio / linea_estructura (numero fila 1-23, nombre: SOPA NORMAL, PLATO DE FONDO..., tipo plato).
   - planificacion_real_dia (contrato, regimen, servicio, fecha, comensales, costo_minuta_dia, estado_GH, estado_GI).
   - planificacion_real_detalle (planificacion_dia_id, linea_estructura_id, receta_id, porcentaje (por.%), costo_receta, bloqueada, marca_R).
   - receta (nombre, costo calculado).
8. Dudas: significado de "R" (receta? real?), de "Por.[%]" (porcentaje de comensales que eligen el plato; puede pasar de 100 en pan), de "Estado GH / GI" (grupos? ilegible). Costo de columna izquierda = costo del item en estructura (parece costo ponderado/promedio). Dependiente de recetas (img 32/33) y de Generar Minuta Real (img 34).

---------------------------------------------------------------
## 32 - Planificacion Real (semana 01/10 a 06/10/2026, con dias bloqueados)

1. Misma ventana "Planificacion Real", mismo titulo: ORCOPAMPA FOALI(PE017401) - REGIMEN 1 - DESAYUNO NORMAL 1.
2. Cabecera igual (Nota + leyenda). Esta captura muestra la estructura "Estructura Servicio" a la izquierda (SOPA NORMAL ... Comensales) pero con la columna Costo de la estructura oculta; dias: Jue 1/10/2026, Vie 2/10/2026, Sab 3/10/2026 (rojo = bloqueados), Dom 4/10/2026, Lun 5/10/2026, Mar 6/10/2026 (amarillos = habilitados).
3. Columnas por dia: marca "R" | plato | "Por.[%]" | "Costo". Costo Minuta Dia: 3.22 (Jue 1), 4.13 (Vie 2), 3.82 (Sab 3), 3.98 (Dom 4), 4.45 (Lun 5).
   - Ejemplos Jue 1/10 (bloqueado): PLATO DE FONDO "PUL - CHANFAINITA" 70.00 % 1.140334; GUARNICION 1 "MOTE" 40.00 % 0.411136; BEBIDA CALIENTE 1 "PUNQUI (7 SEMILLAS)" 98.80 % 0.057284.
   - Vie 2/10: "POLL - ESCABECHE DE POLLO" 70.00 % 1.339312; "CAMOTE AL HORNO" 65.00 % 0.200000; COMPLEMENTO 1 "QUESO EDAM" 40.00 % 1.086600.
   - Sab 3/10: "CER - PATITA CON MANI" 64.00 % 1.847341; "TOSTADAS INTEGRALES" 44.00 % 0.064073; PAN 4 "PAN - PAN DE MANTECA" 44.00 % 0.161902; COMPLEMENTO 1 "ACEITUNA NEGRA" 40.00 % 1.080000.
   - Dom 4/10 (habilitado): SOPA NORMAL "CALDO DE GALLINA" 80.00 % 1.706095; COMPLEMENTO 1 "SALSA GUACAMOLE" 40.00 % 0.586687; ISLA 1 "LIMON" 20.00 % 0.067500; ISLA 2 "AJI - ROCOTO PICADO" 14.60 % 0.103701.
   - Lun 5/10: PLATO "POLL - MILANESA DE POLLO" 76.00 % 1.219287; COMPLEMENTO 2 "OMELETTE DE PIMIENTO" 40.00 % 1.022501.
   - Mar 6/10: "ARROZ A LA CUBANA" 80.00 %; "JAMONADA" 40.00 %; "ENCEBOLLADO" 0.00 %.
   - Comensales: 500 para cada dia.
4. Mismos iconos y menu que 31.
5. Leyenda: verde = Estructura de Servicio; rojo = Celda Bloqueada (dias ya pasados Jue 1 a Sab 3: toda la columna en rojo); amarillo = Celda Habilitada (desde Dom 4/10, que es la fecha de las capturas).
6. Totales: "Comensales" 500 por dia; Costo Minuta Dia en fila superior.
7. Entidades: las mismas de 31. Ademas confirma regla: bloqueo por fecha (dias previos a hoy) -> campo calculado o columna bloqueado.
8. Dudas: porcentajes en dia bloqueado tienen valores como 33.80/25.40 (porcentaje de aceptacion/consumo). Un dia puede tener 0.00 % (ENCEBOLLADO 0.00 %).

---------------------------------------------------------------
## 33 - Receta (pestana "Detalle Receta x Regimen", abierta desde Planificacion Real)

1. Ventana "Receta" (modal sobre Planificacion Real). Ruta probable: Recetas > Receta, o doble clic en plato de Planificacion.
2. Pestanas: Receta | Detalle Receta Patron | Detalle Receta Local | **Detalle Receta x Regimen** (activa) | Metodos Preparacion | Grupo Vulnerable.
   Cabecera: Regimen -> "1" con boton selector y texto "REGIMEN 1".
   Nombre Receta -> QUESO EDAM; Nombre Fantasia -> QUESO EDAM; Categoria Dietetica -> NORMAL (selector); Tipo Plato -> GUARNICIONES\OTROS (selector).
   Cuadro derecho: Raciones -> 1; C. Bruta -> 0.030000; C. Servida -> 0.030000; G. Neto -> 0.030000.
3. Grilla ingredientes: Cod. Ing. (codigo) | Descripcion (texto) | C.Bruta (decimal 6) | U. M. (texto) | %Aprov. (decimal 6) | %A.Coc. (decimal 6).
   Fila: 2509 | QUESO EDAM CONGELADO | 0.030000 | KG | 100.000000 | 100.000000.
   Grilla "Aporte Nutricional": Nutrientes | Aporte. Filas: Agua g 12.00; Energia kcal 106.80; Energia kJ 106.80; Proteinas g 8.10; Grasa total g 7.83; Cenizas g 1.23; Carbohidratos totale(s) 0.84; Carbohidratos dispon(ibles) 0.00; Fibra cruda g 0.00; Fibra dietaria g 0.00; Calcio mg 0.00; Fosforo mg 0.00 (lista continua con scroll).
4. Botones: Agrega Ing., Agrega Linea, Borra Linea, Mueve Up. Iconos de barra: nuevo, guardar/copiar, eliminar, copiar, cancelar (X), aceptar (check), imprimir/salir rojo, filtro, binoculares, impresora, salir.
5. Sin leyenda de colores.
6. Pie: Gr.Net.Verd.: 0.00; P.A.V.B.: 244.50; P.A.V.B. %: 0.00; Costo: 1.086600. (Costo coincide con el costo del plato en Planificacion Real.)
7. Entidades: receta (nombre, nombre_fantasia, categoria_dietetica_id, tipo_plato_id), categoria_dietetica, tipo_plato (ej. "GUARNICIONES\OTROS" jerarquia con barra), receta_regimen (receta_id, regimen_id, raciones, c_bruta, c_servida, g_neto), receta_ingrediente (receta_id, regimen_id, producto_codigo, c_bruta, unidad, pct_aprovechamiento, pct_a_coccion), aporte_nutricional (receta, nutriente, aporte), nutriente (catalogo), receta_metodo_preparacion, receta_grupo_vulnerable, receta_patron / receta_local (herencia de receta patron a local).
8. Dudas: P.A.V.B. (precio ... venta bruto?), significado %A.Coc. (aprovechamiento en coccion). Pestanas no visibles (Patron, Local, Metodos, Grupo Vulnerable) dependen de otras capturas. Tabla de producto (codigo 2509) esta en maestro de productos.

---------------------------------------------------------------
## 34 - Generar Minuta Real

1. Ventana "Generar Minuta Real". Ruta probable: Planificacion > Generar Minuta Real.
2. Campos: Contrato -> PE017401 (con selector) y nombre "ORCOPAMPA FOALI"; Periodo -> "10/2026" (selector numerico mes/anio con flechas).
3. Sin grillas.
4. Botones: check verde (aceptar/generar), papelera (eliminar/cancelar), salir.
5. Sin leyenda. 6. Sin totales.
7. Entidades: minuta_real (contrato_id, periodo mes/anio, estado, generada), copia de minuta teorica/planificacion al periodo real.
8. Dudas: no se ve de donde copia (probablemente de la planificacion teorica o patron). Alimenta img 31/32.

---------------------------------------------------------------
## 35 - Pedido Manual

1. Ventana "Pedido Manual". Ruta probable: Compras > Pedido Manual (o Pedidos > Pedido Manual).
2. Cabecera: Contrato -> PE017401 / ORCOPAMPA FOALI; Periodo Inicial -> (vacio, selector "..."); Periodo Final -> (vacio, selector "..."); boton de proceso (icono engranaje) para cargar productos.
   Seccion "Detalle": Nº Orden Compras -> 1; Proveedor -> (vacio, selector con nombre al lado); Persona Contacto -> (vacio); Cuenta Correo -> (vacio); boton "Enviar Correo" (deshabilitado).
3. Grilla "Productos": (selector fila) | Activo (casilla) | Codigo Productos (codigo) | Descripcion (texto) | Unidad (texto) | Cantidad Consumir (decimal, cortado; otras columnas a la derecha ilegibles). Vacia. Debajo hay dos cajas de texto (codigo y descripcion) para agregar producto.
   Grilla "Detalle": (selector) | Codigo Producto | Descripcion | Unidad | Cantidad | Precio | Fecha Despacho. Vacia.
4. Iconos de barra: nuevo, eliminar, cancelar, aceptar, guardar/ver, imprimir, salir.
5. Sin leyenda. 6. Sin totales.
7. Entidades: pedido_manual / orden_compra (numero, contrato_id, proveedor_id, periodo_inicial, periodo_final, persona_contacto, correo), orden_compra_detalle (orden_id, producto_id, unidad, cantidad, precio, fecha_despacho), proveedor (nombre, contacto, correo), consumo_proyectado por producto (cantidad a consumir en periodo).
8. Dudas: un pedido puede dividirse en varias ordenes por proveedor (Nº Orden Compras = 1). Alimentado por necesidades de minuta.

---------------------------------------------------------------
## 36 - Pedidos Extras (con dialogo "Guia despacho y Factura - Venta directa")

1. Ventana "Pedidos Extras"; encima dialogo "Guia despacho y Factura - Venta directa". Ruta probable: Compras/Pedidos > Pedidos Extras.
2. Cabecera: Contrato -> PE017401 / ORCOPAMPA FOALI; Fecha(mm/aa) -> 10/2026; Nº Pedido -> (vacio).
   Dialogo: Inicio -> 04/09/2026 (fecha desplegable); Termino -> 04/10/2026.
3. Grilla principal: Codigo | Descripcion | Unidad | Necesidad Segun Minuta | Pedido Propuesto | Unidad Despacho | Fecha Entrega | Cantidad Solicitud (cortado). Vacia.
   Grilla del dialogo: Nº Pedido | Fecha Pedido | Tipo Pedido | Estado. Vacia.
4. Botones: "Agregar Producto"; dialogo: check verde (aceptar), salir. Iconos de barra: nuevo, eliminar, cancelar, aceptar, ver/imprimir, impresora, salir.
5. Sin leyenda. 6. Sin totales.
7. Entidades: pedido (numero, contrato, fecha, tipo_pedido [extra, normal, venta directa], estado), pedido_detalle (producto, necesidad_minuta, pedido_propuesto, unidad_despacho, fecha_entrega, cantidad_solicitada), guia_despacho / factura venta directa.
8. Dudas: el dialogo parece buscar pedidos por rango de fechas para generar guia/factura de venta directa. Tipos y estados de pedido no visibles.

---------------------------------------------------------------
## 37 - Pedido (carga de pedido por archivo)

1. Ventana "Pedido". Ruta probable: Compras > Pedido (carga de archivo).
2. Cabecera: Archivo Carga -> (vacio, selector); Contrato -> (vacio) + nombre (gris); Fecha Desde -> 01/10/2026; Fecha Hasta -> 04/10/2026; Codigo Pedido -> (vacio); Costo Pedido -> (vacio).
3. Grilla: (selector) | Categoria | Familia | Sub-Familia | Codigo | Producto | U.M. | Precio | Cant. Plan. | Cant sitio | Formato Compra | Pedido Sugerido | Co(sto) S(...) (cortado). Vacia.
   Debajo: dos cajas (codigo / descripcion) de agregar producto.
4. Iconos de barra: imprimir, nuevo/documento, aceptar (check, gris), cancelar (X, gris), Excel, vista previa, eliminar, salir.
5. Sin leyenda. 6. Total: "Costo Pedido".
7. Entidades: pedido_sugerido (contrato, periodo desde/hasta, codigo, costo_total), pedido_sugerido_detalle (producto, precio, cant_plan, cant_sitio, formato_compra, pedido_sugerido, costo), jerarquia producto: categoria > familia > subfamilia; formato_compra (presentacion y factor).
8. Dudas: "Cant sitio" = stock en sitio. Archivo Carga sugiere importacion Excel.

---------------------------------------------------------------
## 38 - Menu "Transacciones Ent/Sal" (desplegable)

1. Barra de menu principal: "Transacciones Ent/Sal", "Control de Documentos", "Venta", "Gastos", "Informes Transacciones Ent(/Sal)". Submenu "Ent/Sal" con opciones. Al fondo, calendario de dias (Miercoles... Jueves, Viernes: 1,2 en verde) y leyenda con "Dia C..." (cortada, verde y rojo).
2-3. Sin campos. Opciones del submenu (copiadas):
   1. Recep. Proveedor (E) / FOFI (E)
   2. Traspasos entre Cont (E/S) / CD (E/S) / Dev. CD (S)
   3. Traspaso Entre Bodega (E/S)
   4. Salida de Bodega a Produccion (S)
   5. Devolucion de Produccion (E)
   6. Salida de Bodega por Mermas (S)
   7. Toma de Inventario (E/S)
   8. Requerimiento Diario (E/S)
   9. Registro Merma ADS
   10. Traspaso ALM Remoto-SGP
   (E = Entrada, S = Salida.)
5. Calendario: dia 1 y 2 resaltados en verde; leyenda cortada "Dia C..." (verde) y otra en rojo (ilegible).
7. Entidades: tipo_movimiento (catalogo con signo E/S): recepcion_proveedor, recepcion_FOFI, traspaso_contrato, traspaso_CD, devolucion_CD, traspaso_bodega, salida_produccion, devolucion_produccion, salida_merma, toma_inventario, requerimiento_diario, registro_merma_ADS, traspaso_ALM_remoto.
8. Dudas: FOFI (ilegible/sigla no explicada), CD (centro de distribucion), ADS, ALM. Calendario de dias abiertos/cerrados, depende de cierre diario.

---------------------------------------------------------------
## 39 - Toma de Inventario (principal)

1. Ventana "Toma de Inventario". Ruta: Transacciones Ent/Sal > Ent/Sal > Toma de Inventario (E/S).
2. Cabecera: Contrato -> PE017401 / ORCOPAMPA FOALI; Fecha -> 30/09/2026 (fecha con selector); Bodega -> "PE017401 - FOALI" (combo); casilla deshabilitada "Toma Inventario Rotativo".
3. Grilla: (nº fila) | Codigo | Descripcion | Unidad | Stock Sistema | Stock Fisico | P.M.P | Total.
   Ejemplos:
   - 01010132199 | ACEITE AJONJOLI OLIVOS DEL SUR 200 ML | FRASCO | 0.000000 | 0.000000 | 12.502000 | 0.000000
   - 01010104223 (visto "010101042223") | ACEITE AJONJOLI OLIVOS DEL SUR 220 ML PET | BOTELLA | 12.000000 | 12.000000 | 14.109000 | 169.308000
   - 0101036994 (visto "010101036994") | ACEITE VEGETAL CIELO 5 LT | BIDON | 42.000000 | 42.000000 | 34.120000 | 1,433.040000
   - 05060130729 | ACEITUNA NEGRA RODAJA | LOGRAM(O?) | 13.000000 | 13.000000 | 27.000000 | 351.000000
   - 030201037901 | AGUA SAN LUIS SIN GAS 750 ML | BOTELLA | 1,169.000000 | 1,169.000000 | 0.950000 | 1,110.550000
   Otros: 070040 ACELGA (LOGRAM) P.M.P 4.280000; 0101040028 ACHOCOLATADO MILO NESTLE 400 GR LATA 12.860000; 02020019 AGUA SAN LUIS DESCARTABLE BIB 20 LT CAJA 14.711417. Unidades vistas: FRASCO, BOTELLA, GALON, BALDE, BIDON, LOGRAM(O), LATA, CAJA.
   Codigos de producto con largo variable (6 a 12 digitos), texto numerico con ceros a la izquierda. Cantidades con 6 decimales; Total = Stock Fisico x P.M.P. (P.M.P = precio medio ponderado).
4. Botones: "Agregar Producto", "Eliminar Producto"; dos cajas para buscar producto (codigo/descripcion). Iconos de barra: nuevo, guardar/doc, eliminar, ver/editar, cancelar, aceptar, imprimir, filtro/lista de fechas, y varios iconos (algunos grises) de acciones de inventario (cargar, ajustar etc.; nombres ilegibles), salir.
5. Sin leyenda. Celda de Stock Fisico editable (resaltada).
6. Totales: columna Total por fila; total general no visible.
7. Entidades: toma_inventario (contrato, bodega, fecha, rotativo, estado), toma_inventario_detalle (toma_id, producto_id, stock_sistema, stock_fisico, pmp, total), bodega, producto (codigo, descripcion, unidad), stock_bodega, precio_medio_ponderado.
8. Dudas: columna Stock Sistema se congela a la fecha de la toma. "Toma Inventario Rotativo" (parcial). Fecha 30/09/2026 = ultimo dia del mes anterior.

---------------------------------------------------------------
## 40 - Toma de Inventario (lista desplegable de fechas)

1. Misma ventana "Toma de Inventario", con una lista desplegable de fechas abierta desde el icono de filtro de la barra.
2. Mismos campos: Contrato PE017401 ORCOPAMPA FOALI; Fecha 30/09/2026; Bodega "...- FOALI"; Toma Inventario Rotativo (deshabilitada).
3. Lista de fechas (tomas previas): 30/09/2026, 27/09/2026, 31/08/2026, 23/08/2026, 31/07/2026, 25/07/2026 (seleccionada), 30/06/2026, 26/06/2026, 31/05/2026, 26/05/2026 ...
   La grilla es la misma de la 39 (mismas filas).
4. Mismos botones.
5-6. Sin leyenda ni totales.
7. Entidades: igual que 39; confirma que existen varias tomas por contrato/bodega por mes: fin de mes (30/09, 31/08, 31/07, 30/06, 31/05) y tomas rotativas intermedias (27/09, 23/08, 25/07, 26/06, 26/05).
8. Dudas: la lista permite recuperar una toma anterior (historico).

---------------------------------------------------------------
## 41 - Imprimir Toma de Inventario

1. Dialogo "Imprimir Toma de Inventario" (desde Toma de Inventario).
2. Opciones:
   - Grupo "Tipo Listado" (radio): Listado para la toma de inventario (seleccionado); Listado de diferencias Fisico v/s Sistema; Listado de inventario Fisico Valorizado; Listado de inventario Sistema Valorizado; Diferencias Fisico v/s Sistema - Valorizado.
   - Grupo "Familia Producto": radio "Una Familia" / "Todas" (seleccionado); combo de familia (deshabilitado).
   - Casillas: Solo productos con diferencias; Incluir productos con Stock Fisico cero; Incluir productos con Stock Sistema cero.
4. Iconos: vista previa, salir.
7. Entidades: no nuevas; usa toma_inventario_detalle y familia_producto.
8. Dudas: reportes derivados.

---------------------------------------------------------------
## 42 - Devolucion de Produccion para Bodega

1. Ventana "Devolucion de Produccion para Bodega". Ruta: Transacciones Ent/Sal > Ent/Sal > Devolucion de Produccion (E).
2. Cabecera: Contrato -> PE017401 / ORCOPAMPA FOALI; Nº Documento -> 137; Fecha Emision -> 04/10/2026; Fecha Prod. -> (vacio, combo); Bodega -> (vacio, combo deshabilitado); Reg. - Serv. -> (combo vacio, regimen-servicio); radio Resumido / Sector (Sector seleccionado); casilla "Oculta Ingrediente" (marcada).
3. Grilla superior (sectores): Codigo | Descripcion Sector | Totales. Vacia.
   Grilla inferior: Codigo | Descripcion | Unidad | Cant.Salida | Cant.Devolver | P.M.P. | Total. Vacia.
4. Iconos: nuevo, eliminar, cancelar, aceptar, guardar/ver, impresora, salir.
5. Leyenda: verde = Ingrediente; amarillo = Producto.
7. Entidades: devolucion_produccion (numero, contrato, fecha_emision, fecha_produccion, bodega, regimen_servicio), devolucion_produccion_detalle (producto, cant_salida, cant_devolver, pmp, total), sector (catalogo de cocina: Codigo/Descripcion Sector), relacion con salida_bodega_produccion.
8. Dudas: "Sector" agrupa por sectores de cocina; "Resumido" suma por producto; ingrediente vs producto = nivel de explosion de receta. Numeracion de documentos por tipo (137).

---------------------------------------------------------------
## 43 - Salida de Bodega a Produccion (nueva, vacia)

1. Ventana "Salida de Bodega a Produccion". Ruta: Transacciones Ent/Sal > Ent/Sal > Salida de Bodega a Produccion (S).
2. Cabecera: Fecha Emision -> 04/10/2026; Fecha Prod. -> 04/10/2026 (deshabilitada); Bodega -> (combo vacio); Nº Doc. -> 63181; Regimen -> (codigo + selector + nombre); Servicio -> (codigo + selector + nombre); radio Resumido / Sector (Sector seleccionado); casilla "Oculta Ingrediente" (marcada).
3. Grilla superior: Codigo | Descripcion Sector | Totales. Grilla inferior: Codigo | Descripcion | Unidad | Cant.Planif. | Cant.Realizada | P.M.P. | Total. Vacias.
4. Botones: "Agr. Prod.", "Elim. Prod."; iconos: nuevo, eliminar, cancelar, aceptar, guardar, impresora, candado (cerrar/bloquear documento), salir.
5. Leyenda: verde = Ingrediente; rojo = Sobrepasa Stock actual; amarillo = Producto.
7. Entidades: salida_bodega_produccion (numero doc, fecha emision, fecha produccion, bodega, regimen, servicio, estado), salida_produccion_detalle (producto, cant_planificada, cant_realizada, pmp, total).
8. Dudas: estado se muestra en img 44. Cant.Planif. viene de planificacion de la fecha.

---------------------------------------------------------------
## 44 - Salida de Bodega a Produccion (documento con datos)

1. Misma ventana, documento Nº 63169, estado "PENDIENTE" (texto marron junto al Nº Doc.).
2. Cabecera: Fecha Emision 04/10/2026; Fecha Prod. 04/10/2026; Bodega PE017401 - FOALI; Nº Doc. 63169; Regimen -> 3 "REGIMEN 3"; Servicio -> 141 "ALMUERZO NORMAL 1"; radio Resumido (seleccionado) / Sector; Oculta Ingrediente marcada.
3. Grilla (todo amarillo = Producto): Codigo | Descripcion | Unidad | Cant.Planif. | Cant.Realizada | P.M.P. | Total. Filas:
   - 010204040821 | ATUN TROZOS COMPASS 140 GR | LAT | 0.000000 | 1.000000 | 3.860000 | 3.860000
   - 050607037455 | CREMA TIPO CHANTILLY SNEL 1LT | CAJ | 0.000000 | 1.000000 | 10.710000 | 10.710000
   - 010107039479 | LECHE EVAPORADA (CONCENTRADA) LAIVE BOLSITARRO 800 ML | BOL | 0.000000 | 3.000000 | 5.690000 | 17.070000
   - 010211039170 | MAYONESA SCALA EMIC 3.8 KG | BOL | 0 | 5.000000 | 34.580000 | 172.900000
   - 010101036994 | ACEITE VEGETAL CIELO 5 LT | BID | 0 | 0.500000 | 34.120000 | 17.060000
   - 010202034896 | CALDO CONCENTRADO GALLINA MACRO FOOD 1 KG | KG? | 0 | 0.300000 | 17.560000 | 5.268000
   - 010104034914 | AZUCAR RUBIA SACO 50 KG | SCO | 0 | 0.500000 | 135.816741 | 67.908371
   - 0102010018 | PAPA BLANCA | KG | 0 | 40.000000 | 2.500000 | 100.000000
   - 0102010006 | CEBOLLA ROJA | KG | 0 | 3.000000 | 3.330000 | 9.990000
   - 0104030232902 | MIX DE SOPA COMPLETO CONGELADO - KGM | KG | 0 | 7.000000 | 5.950415 | 41.652905
   - 04010127698 | CORDERO DESHUESADO CONGELADO - KGM | KG | 0 | 13.000000 | 34.045989 | 442.597857
   - 04010731134 | POLLO SIN MENUDENCIA CONGELADO RANGO 1.68 KG | KG | 0 | 11.000000 | 9.198430 | 101.182730
   Otros: QUINUA CANTA CLARO 5 KG BOL; HARINA ESPECIAL DEL CIELO GRANEL SACO 50 KG SCO; GELATINA SABOR NARANJA MACROFOOD 1KG BOL; ARROZ COSTENO EXTRA HOJA REDONDA ANEJO 50 KG SCO; PIERNA DE POLLO CON ENCUENTRO CONGELADO (BLOQUE) KG 50.000000.
   Observacion: Cant.Planif. = 0 en todas las filas (no hay planificacion para ese dia) y Cant.Realizada con valores digitados; numeros de color azul = editables. Codigos con largo variable.
4. Mismos botones. 5. Misma leyenda. 6. Total general no visible.
7. Entidades: igual a 43. Confirma catalogo regimen (1,2,3, General) y servicio (141 = ALMUERZO NORMAL 1).
8. Dudas: unidades abreviadas aqui (LAT, CAJ, BOL, BID, SCO, KG) vs completas en toma de inventario (LATA, CAJA, BIDON...): posible catalogo con abreviatura y nombre.

---------------------------------------------------------------
## 45 - Salida de Bodega a Produccion - dialogo de busqueda de documentos

1. Dialogo (sin titulo visible; "Salida de Bodega a Produccion") sobre el documento 63169 PENDIENTE, abierto con el icono de ver/buscar.
2. Campos: Inicio -> 04/10/2026; Termino -> 04/10/2026.
3. Grilla: Nº Doc. | Fecha | Regimen - Servicio | Estado. Filas:
   - 63167 | 04/10/2026 | REGIMEN 1 - DESAYUNO NORMAL 1 | PENDIENTE (fila seleccionada)
   - 63169 | 04/10/2026 | REGIMEN 3 - ALMUERZO NORMAL 1 | PENDIENTE
   - 63171 | 04/10/2026 | REGIMEN 3 - CENA NORMAL 1 | PENDIENTE
   - 63173 | 04/10/2026 | REGIMEN GENERAL - CONSUMO FIJO FOOD | PENDIENTE
   - 63174 | 04/10/2026 | REGIMEN GENERAL - CONSUMO FIJO TGM | PENDIENTE
   - 63175 | 04/10/2026 | REGIMEN GENERAL - SERVICIO VENTA DIRECTA 1 | PENDIENTE
   - 63178 | 04/10/2026 | REGIMEN 2 - RANCHO 1 | PENDIENTE
   - 63180 | 04/10/2026 | REGIMEN 2 - RANCHO 2 | PENDIENTE
4. Botones: check verde (aceptar/abrir), salir.
7. Entidades: servicios existentes por regimen: REGIMEN 1 (DESAYUNO NORMAL 1), REGIMEN 2 (RANCHO 1, RANCHO 2), REGIMEN 3 (ALMUERZO NORMAL 1, CENA NORMAL 1), REGIMEN GENERAL (CONSUMO FIJO FOOD, CONSUMO FIJO TGM, SERVICIO VENTA DIRECTA 1). Estado documento: PENDIENTE (otros estados ilegibles). Los Nº Doc. impares/consecutivos (63167, 63169, 63171, 63173...) sugieren que el sistema genera documentos automaticamente por servicio y dia (con saltos de 2).
8. Dudas: otros estados (p. ej. procesado/cerrado) no visibles; saltos de numeracion (63172, 63176, 63177, 63179) corresponderian a otros tipos de documento.

---------------------------------------------------------------
# RESUMEN: entidades candidatas

| Entidad candidata | Columnas clave / notas | Capturas |
|---|---|---|
| contrato | codigo (PE017401), nombre (ORCOPAMPA FOALI) | 31-37, 39-45 |
| regimen | codigo (1, 2, 3, GENERAL), nombre | 31-33, 43-45 |
| servicio | codigo (141), nombre (ALMUERZO NORMAL 1, DESAYUNO NORMAL 1, CENA NORMAL 1, RANCHO 1/2, CONSUMO FIJO FOOD/TGM, SERVICIO VENTA DIRECTA 1), regimen | 31, 32, 43-45 |
| estructura_servicio (lineas) | nº fila, nombre (SOPA NORMAL, PLATO DE FONDO, GUARNICION, BEBIDA FRIA/CALIENTE, COMPLEMENTO, PAN, ISLA), costo | 31, 32 |
| planificacion_real_dia | contrato, regimen, servicio, fecha, comensales, costo_minuta_dia, estado GH/GI | 31, 32 |
| planificacion_real_detalle | dia, linea, receta, por_%, costo, bloqueada | 31, 32 |
| minuta_real (generacion) | contrato, periodo mm/aaaa | 34 |
| receta | nombre, nombre_fantasia, categoria_dietetica, tipo_plato | 33 |
| receta_regimen | receta, regimen, raciones, c_bruta, c_servida, g_neto, P.A.V.B., costo | 33 |
| receta_ingrediente | producto, c_bruta, unidad, %aprov, %a.coc | 33 |
| categoria_dietetica / tipo_plato | catalogos (NORMAL; GUARNICIONES\OTROS) | 33 |
| aporte_nutricional / nutriente | nutriente, aporte por receta | 33 |
| receta_patron, receta_local, metodo_preparacion, grupo_vulnerable | pestanas (contenido no visible) | 33 |
| producto | codigo, descripcion, unidad, categoria > familia > subfamilia | 33, 35-37, 39-44 |
| unidad_medida | abreviatura (KG, LAT, CAJ, BOL, BID, SCO) y nombre (LATA, CAJA, BIDON...) | 33, 39, 44 |
| familia_producto / categoria / subfamilia | jerarquia de producto | 37, 41 |
| pedido / orden_compra | numero, contrato, proveedor, periodo, fecha, tipo, estado | 35, 36, 37 |
| pedido_detalle | producto, cantidad, precio, fecha_despacho, necesidad minuta, pedido propuesto, formato compra | 35, 36, 37 |
| proveedor | codigo, nombre, contacto, correo | 35 |
| guia_despacho / factura venta directa | rango de fechas, pedidos | 36 |
| tipo_movimiento (E/S) | recepcion proveedor, traspasos, salidas, merma, toma inventario... | 38 |
| bodega | codigo (PE017401 - FOALI), contrato | 39-45 |
| stock_bodega / pmp | stock sistema, precio medio ponderado | 39, 40, 42-44 |
| toma_inventario | contrato, bodega, fecha, rotativo | 39, 40, 41 |
| toma_inventario_detalle | producto, stock_sistema, stock_fisico, pmp, total | 39, 40 |
| salida_bodega_produccion | nº doc, fechas, bodega, regimen, servicio, estado | 43, 44, 45 |
| salida_produccion_detalle | producto, cant_planif, cant_realizada, pmp, total | 43, 44 |
| devolucion_produccion | nº doc, fechas, bodega, reg-servicio | 42 |
| devolucion_produccion_detalle | producto, cant_salida, cant_devolver, pmp, total | 42 |
| sector (cocina) | codigo, descripcion | 42, 43 |
| calendario_dia (abierto/cerrado) | fecha, estado | 38 |
