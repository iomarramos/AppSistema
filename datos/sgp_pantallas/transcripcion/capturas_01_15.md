# Transcripción de capturas SGP Perú (Sodexo) 01 a 15

Fechas originales (indice.csv): 01=2026-08-18; 02 a 09=2026-10-02; 10 a 15=2026-10-03.
Convención: "(ilegible)" = no se lee; "(inferido)" = deducción, no visible.

---------------------------------------------------------------------
## 01 — Traspaso Entre Contratos (reporte impreso/vista previa)
1. Título: "Traspaso Entre Contratos". Ruta probable: Informes Transacciones Ent/Sal > Traspasos entre Cont (E/S) / CD (E/S) / Dev. CD (S) (ver captura 15). Es un reporte/documento, no una pantalla de captura.
2. Cabecera del reporte:
   - Empresa: Sodexo Perú S.A.
   - Centro de Costo : PE017401 - ORCOPAMPA FOALI
   - Centro de Costo OPTIMUM : PE017401
   - N° de Documento : 8031
   - Folio -> 98
   - F. Emisión -> 17/08/2026
   - Contrato -> PE017401 - ORCOPAMPA FOALI
   - Nro. Doc. Origen -> 13606
   - Guía de Remisión -> (vacío)
   - Orden de Pedido -> (vacío)
   - Bodega -> PE017401 - FOALI
   - Tipo Traspaso -> Entrada - Traspaso Entre Contratos
   - Contrato Origen -> PE016801 - TAMBOMAYO FOALI
   - Fecha Origen -> 16/08/2026
   - Placa Camión -> (vacío)
3. Grilla (detalle), columnas en orden: Código (código texto 12 dígitos) | Descripción (texto) | Unidad (texto: SACO) | Cantidad (decimal 6) | Cant.Recibida (decimal 6) | Precio (decimal 6) | Total (decimal 6).
   - Fila: 010106037527 | HARINA ESPECIAL DEL CIELO GRANEL SACO 50 KG | SACO | 1.000000 | 1.000000 | 136.780000 | 136.780000
4. Botones: ninguno (reporte).
5. Leyendas de colores: fila de encabezado de grilla en amarillo claro (solo estilo).
6. Totales: Total 136.780000 (suma de la columna Total).
7. Entidades implícitas:
   - `traspaso_contrato` (cabecera): id, numero_documento(8031), folio(98), fecha_emision, contrato_destino_id, contrato_origen_id, bodega_id, tipo_traspaso (Entrada/Salida), nro_doc_origen (13606), fecha_origen, guia_remision, orden_pedido, placa_camion.
   - `traspaso_contrato_detalle`: traspaso_id, producto_id/codigo, unidad, cantidad, cantidad_recibida, precio, total.
   - `contrato` (código PE017401 + nombre; además código OPTIMUM), `bodega` (código = contrato + nombre FOALI), `producto` (código 12 dígitos, descripción, unidad), `unidad_medida`.
8. Dudas: "N° de Documento 8031" y "Folio 98" son numeraciones distintas (documento global vs folio por bodega/tipo?). Cantidad vs Cant.Recibida sugiere recepción con diferencias. El documento origen 13606 es el número de la salida en el contrato origen (PE016801). "Centro de Costo" = contrato; "Centro de Costo OPTIMUM" es un código externo igual al contrato aquí.

---------------------------------------------------------------------
## 02 — Planificación Teórica (diálogo de selección de periodo)
1. Título: "Planificación Teórica". Ruta: Planificación > Planificación Teórica (diálogo previo a la grilla de la captura 04).
2. Cabecera:
   - Contrato -> PE017401 (campo gris) ; descripción: ORCOPAMPA FOALI
   - Regimen -> 3 ; descripción: REGIMEN 3
   - Servicio -> 141 ; descripción: ALMUERZO NORMAL 1
   - Fecha desde -> 10/2026 (selector mes/año con flechas arriba/abajo)
   Los campos Contrato/Regimen/Servicio tienen icono de "mano" (lupa/selector de lista de valores).
3. Calendario (cuadrícula Lunes..Domingo) del mes 10/2026: días 1 a 31 con colores. Cada celda es un día del mes. Ejemplo: Jueves 1, Viernes 2, Sábado 3, Domingo 4; semana 5-11, 12-18, 19-25, 26-31. Todos los días en rojo/salmón; celdas vacías de Lunes-Miércoles de la primera semana (sin color, la celda Lunes tiene foco), y vacías tras el 31.
4. Barra lateral derecha con 4 iconos (sin texto): (1) tabla/calendario (abrir/consultar planificación), (2) exportar/enviar (flecha), (3) archivo/histórico (icono de pila de carpetas), (4) salir (puerta). Funciones inferidas.
5. Colores: rojo/salmón en todos los días (por analogía con captura 04 = "Celda Bloqueada", o sea periodo cerrado). Sin leyenda en esta ventana.
6. Totales: ninguno.
7. Entidades implícitas: `contrato`, `regimen` (código 3, nombre REGIMEN 3), `servicio` (código 141 ALMUERZO NORMAL 1), `contrato_regimen_servicio` (qué servicios/regímenes tiene un contrato), `planificacion_teorica_dia` (contrato, regimen, servicio, fecha, estado abierto/cerrado).
8. Dudas: significado exacto del color rojo (cerrado vs. bloqueado); captura 03 muestra estado "Cerrado" para 10/2026, consistente.

---------------------------------------------------------------------
## 03 — Histórico Planificación Teórica
1. Título: "Histórico Planificación Teórica". Ruta: Planificación > Planificación Teórica > botón Histórico (inferido).
2. Cabecera: ninguna.
3. Grilla, columnas en orden: C.Regimen (entero/código) | Descripción (texto, régimen) | C.Servicio (entero/código) | Descripción (texto, servicio) | Fecha (mes/año MM/AAAA) | Estado (texto).
   Filas:
   - 1 | REGIMEN 1 | 31 | DESAYUNO NORMAL 1 | 10/2026 | Cerrado (fila seleccionada)
   - 3 | REGIMEN 3 | 141 | ALMUERZO NORMAL 1 | 10/2026 | Cerrado
   - 3 | REGIMEN 3 | 281 | CENA NORMAL 1 | 10/2026 | Cerrado
   - 2 | REGIMEN 2 | 441 | LONCHERA SIMPLE 1 | 10/2026 | Cerrado
   - 2 | REGIMEN 2 | 639 | SERVICIO VENTA DIRE(ctos?) (truncado) | 10/2026 | Cerrado
   - 5 | REGIMEN GENERAL | 725 | CONSUMO FIJO FOOD(truncado) | 10/2026 | Cerrado
   - 2 | REGIMEN 2 | 727 | RANCHO 1 | 10/2026 | Cerrado
   - 2 | REGIMEN 2 | 728 | RANCHO 2 | 10/2026 | Cerrado
   - 2 | REGIMEN 2 | 784 | LONCHERA BAJADA | 10/2026 | Cerrado
   - 5 | REGIMEN GENERAL | 921 | CONSUMO FIJO TGM | 10/2026 | Cerrado
   - 1 | REGIMEN 1 | 31 | DESAYUNO NORMAL 1 | 09/2026 | (Estado ilegible, probable Cerrado)
4. Botones: check verde (aceptar/seleccionar), puerta (salir). Barra de desplazamiento vertical.
5. Colores: fila seleccionada en negro con texto blanco.
6. Totales: no.
7. Entidades: `planificacion_teorica_mes` (contrato, regimen_id, servicio_id, periodo AAAA-MM, estado Abierto/Cerrado). `regimen` (códigos 1, 2, 3, 5 con nombres REGIMEN 1, REGIMEN 2, REGIMEN 3, REGIMEN GENERAL). `servicio` (códigos 31, 141, 281, 441, 639, 725, 727, 728, 784, 921).
8. Dudas: la lista es del contrato seleccionado (ORCOPAMPA FOALI, inferido). Servicio 639 y 725 truncados. Falta ver qué otros estados existen (Abierto, etc.).

---------------------------------------------------------------------
## 04 — Planificación Teórica (grilla semanal/mensual, almuerzo)
1. Título ventana: "Planificación Teórica". Menú de ventana: "Menú", "Plato Menú". Ruta: Planificación > Planificación Teórica.
2. Cabecera (banda amarilla): "ORCOPAMPA FOALI(PE017401) - REGIMEN 3 - ALMUERZO NORMAL 1".
   Nota: "Las raciones debe incluir las raciones del personal".
3. Grilla: filas = estructura de servicio; columnas = por cada día un bloque de 3 columnas.
   - Fila "Costo Minuta Día": valores por día: 8.63 (Jue 1/10), 6.62 (Vie 2/10), 8.95 (Sáb 3/10), 7.49 (Dom 4/10), 8.46 (Lun 5/10), 7.16 (Mar 6/10) ...
   - Primera columna fija "Estructura Servicio" con número de fila (1 a 33). Filas: 1 ENTRADA FRIA 1; 2 ENTRADA FRIA 2; 3 ENTRADA CALIENTE(1?) (truncado); 4 ENTRADA CALIENTE (2?) (truncado); 5 COMPLEMENTO 1; 6 SOPA NORMAL 1; 7 SOPA DIETA; 8 PLATO DE FONDO N(ormal 1?); 9 PLATO DE FONDO N(ormal 2?); 10 PLATO DE FONDO N(...); 11 ENSALADA CON CAL(ientes?)(truncado); 12 PLATO DE FONDO D(ieta?); 13 GUARNICION ARRO(Z); 14 GUARNICION 1; 15 GUARNICION 2; 16 GUARNICION DIETA; 17 GUARNICION 4; 18 GUARNICION 5; 19 GUARNICION 6; 20 COM SALAD BAR 1; 21 ISLA 1; 22 ISLA 2; 23 ISLA 3; 24 ISLA 4; 25 ISLA 5; 26 ISLA 6; 27 ISLA 7; 28 FRUTA 1; 29 POSTRE NORMAL 1; 30 POSTRE NORMAL 2; 31 (vacía); 32 BEBIDA FRIA 1; 33 Comensales.
   - Por cada día: columna con marca "R" (indicador de receta/estado, celda pequeña) + nombre del plato/receta (texto) | N.Rac. (entero, raciones) | Cto.Plat (decimal 6, costo por plato).
   - Encabezados de día: "Jue 1/10/2026", "Vie 2/10/2026", "Sáb 3/10/2026", "Dom 4/10/2026", "Lun 5/10/2026", "Mar 6/10/2026", luego "M..." (siguiente).
   - Ejemplos reales: Fila 1 ENTRADA FRIA 1, Jue 1/10: R ENSALADA RUSA | 239 | 0.460000; Vie 2/10: HUEVO RELLENO DE VERDURA | 159 | 0.780000; Sáb 3/10: ENSALADA DE LEGUMBRES | 414 | 0.420000.
     Fila 6 SOPA NORMAL 1, Jue 1/10: SOPA DE CHUÑO NEGRO | 482 | 2.700000; Vie: SOPA DE MORON CON POLLO | 481 | 0.930000; Sáb: SOPA CALDO BLANCO CON RES | 472 | 0.000000.
     Fila 8 PLATO DE FONDO N: Jue RES - OLLUQUITO CON RES | 209 | 5.210000; Vie POLL - POLLADA | 330 | 1.750000; Sáb CMIX - PARRILLA MIXTA | 500 | 3.610000.
     Fila 9: Jue GALL - AJI DE GALLINA | 300 | 2.210000; Vie PES - TRUCHA FRITA | 182 | 5.810000.
     Fila 13 GUARNICION ARRO: ARROZ CON ZANAHORIA | 477 | 0.590000.
     Fila 21 ISLA 1: LIMON | 380 | 0.070000. Fila 32 BEBIDA FRIA 1: REFRESCO EMOLIENTE/MARAC... | 901 | 0.220000.
     Fila 33 Comensales: 530 (Jue), 530 (Vie), 518 (Sáb), 252 (Dom), 530 (Lun), 530 (Mar) en la columna N.Rac.
   - Códigos de receta: los nombres tienen prefijo de tipo de proteína (RES -, POLL -, GALL -, PES -, CMIX -, CORD -) (convención de nombre).
4. Barra de iconos (sin texto, funciones inferidas): guardar; cortar; copiar; pegar; pegar especial; buscar (binoculares); insertar/quitar columna (2 iconos de sangría); flechas arriba/abajo (mover plato); calendario/reloj; salir con flecha; dos iconos rojos (bloquear/desbloquear o copiar día); copo de nieve (congelar?); "$" (costos); iconos de aportes/frecuencia (ver capturas 13 y 14: aporte nutricional y frecuencia); resto (ilegible). Menús: Menú, Plato Menú.
5. Leyendas (nota superior): verde claro = "Estructura de Servicio"; rojo/salmón = "Celda Bloqueada"; amarillo claro = "Celda Habilitada". En esta captura todas las celdas de plato están rojas (bloqueadas, mes cerrado).
6. Totales: fila Costo Minuta Día (costo total de la minuta por día) y fila Comensales (raciones de personas por día). Sin pie.
7. Entidades: `planificacion_teorica_dia_servicio`... mejor: `planificacion_teorica` (contrato, regimen, servicio, fecha, estado); `planificacion_teorica_linea` (planificacion_id, estructura_servicio_linea_id, receta_id, n_raciones, costo_plato, flag bloqueada); `estructura_servicio` (servicio_id, orden/número, nombre de tipo de plato: ENTRADA FRIA 1, SOPA NORMAL 1, ISLA 1 ...); `receta` (código, nombre); `comensales_planificados` (contrato, regimen, servicio, fecha, cantidad).
8. Dudas: significado de la "R" (¿receta asignada / receta con detalle por régimen?). Costo Minuta Día = suma de Cto.Plat de las filas (hay que verificar). Cto.Plat parece costo por ración (receta) vs N.Rac. Columnas posteriores (Mié 7 en adelante) fuera de pantalla. No se sabe si N.Rac. de cada plato puede ser distinto de Comensales (sí: 239 vs 530).

---------------------------------------------------------------------
## 05 — Receta, pestaña "Detalle Receta x Regimen" (ENSALADA RUSA II)
1. Título: "Receta". Pestañas: Receta | Detalle Receta Patrón | Detalle Receta Local | Detalle Receta x Regimen (activa) | Metodos Preparación | Grupo Vulnerable. Ruta: Planificación > Recetas (inferido) o desde Plato Menú.
2. Cabecera:
   - Regimen -> 3 ; REGIMEN 3 (selector con mano)
   - Nombre Receta -> ENSALADA RUSA II
   - Nombre Fantasia -> ENSALADA RUSA II
   - Categoria Dietetica -> NORMAL (selector)
   - Tipo Plato -> ENSALADAS (selector)
   - Raciones -> 1
   - C. Bruta -> 1.136200
   - C. Servida -> 0.236200
   - G. Neto -> 1.136200
3. Grilla de ingredientes, columnas: Cod. Ing. (código entero) | Descripción (texto) | C.Bruta (decimal 6) | U. M. (texto KG/LT) | %Aprov. (decimal 6) | %A.Coc. (decimal 6; probable % ajuste de cocción; etiqueta truncada "%A.Coc.").
   - 7901 | HUEVO ROSADO REFRIGERADO CAJA | 0.015000 | KG | 100.000000 | 100.000000
   - 2302 | PIMIENTA NEGRA MOLIDA | 0.001200 | KG | 100.000000 | 100.000000
   - 2710 | SAL DE COCINA | 0.002000 | KG | 100.000000 | 100.000000
   - 4023 | AGUA PARA RECETA | 0.005000 | LT | 100 | 100
   - 112 | ARVEJA PELADA CONGELADO | 0.010000 | KG | 100 | 100
   - 147 | BETERRAGA ENTERA REFRIGERADO | 0.025000 | KG | 100 | 100
   - (fila vacía)
   - 2198 | PEREJIL LIZO REFRIGERADO | 0.003000 | KG
   - 2034 | PAPA BLANCA REFRIGERADO | 0.030000 | KG
   - 3209 | ZANAHORIA REFRIGERADO | 0.015000 | KG
   (la grilla continúa con scroll; las C.Bruta suman ~1.1362 con otros ítems no visibles).
   Panel "Aporte Nutricionales": columnas Nutrientes | Aporte. Filas: Agua g 53.89; Energía kcal 810.12; Energía kJ 810.12; Proteínas g 17.17; Grasa total g 1.93; Cenizas g 0.64; Carbohidratos totales 16.02; Carbohidratos disponibles 0.00; Fibra cruda g 0.00; Fibra dietaria g 0.99; Calcio mg 33.10; Fósforo mg 30.31 (más con scroll).
4. Botones: Agrega Ing.; Agrega Línea; Borra Línea; Mueve Up. Iconos de barra superior: nuevo, documento (copiar/ver), papelera (eliminar), copiar; X (cancelar), check (aceptar), flecha-salir/exportar, filtro, binoculares (buscar), impresora, salir.
5. Leyendas: valores azules = datos editables/numéricos; campos grises = solo lectura.
6. Totales al pie: Gr.Net.Verd.: 0.07 | P.A.V.B.: 25.22 | P.A.V.B. %: 0.00 | Costo: 0.484232. (P.A.V.B. = probable "Precio de Venta/Peso ... " (ilegible el significado).)
7. Entidades: `receta` (id, nombre, nombre_fantasia, categoria_dietetica_id, tipo_plato_id); `categoria_dietetica` (NORMAL); `tipo_plato` (ENSALADAS; en cap. 10 "SALSAS / ALCUZAS / ISLAS\ALCUZAS"); `receta_ingrediente` (receta_id, regimen_id (nullable: patrón/local/por régimen), producto_id, cantidad_bruta, unidad, pct_aprovechamiento, pct_ajuste_coccion, orden); `receta_nivel` ámbitos Patrón/Local/Régimen; `producto` (cod 112, 147, 2034... ); `nutriente` y `producto_nutriente` / `receta_aporte_nutricional` (calculado); `metodo_preparacion`; `grupo_vulnerable`.
8. Dudas: distinción entre receta Patrón (global), Local (contrato) y por Régimen (esta pestaña, con selector de Régimen); la pestaña x Régimen aquí tiene 1.136200 de C.Bruta vs Patrón 0.036200 (cap. 06): el detalle por régimen sobreescribe completamente al patrón y trae otros ingredientes (huevo, arveja, perejil...). Raciones=1 (bases por ración). Fórmula de C.Servida y G.Neto no visible. Pestañas Metodos Preparación y Grupo Vulnerable no mostradas.

---------------------------------------------------------------------
## 06 — Receta, pestaña "Detalle Receta Patrón" (ENSALADA RUSA II)
1. Título: "Receta"; pestaña activa: Detalle Receta Patrón.
2. Cabecera: Nombre Receta ENSALADA RUSA II; Nombre Fantasia ENSALADA RUSA II; Categoria Dietetica NORMAL; Tipo Plato ENSALADAS; Raciones 1; C. Bruta 0.036200; C. Servida 0.036200; G. Neto 0.036200. (No hay selector de régimen en esta pestaña.)
3. Grilla ingredientes (mismas columnas que 05):
   - 2302 | PIMIENTA NEGRA MOLIDA | 0.001200 | KG | 100.000000 | 100.000000
   - 2710 | SAL DE COCINA | 0.002000 | KG
   - 5656 | MAYONESA HELLMANS SUAVE | 0.002000 | LT
   - 147 | BETERRAGA ENTERA REFRIGERADO | 0.007000 | KG
   - 3110 | VAINITA ENTERO REFRIGERADO | 0.007000 | KG
   - 2034 | PAPA BLANCA REFRIGERADO | 0.007000 | KG
   - 3209 | ZANAHORIA REFRIGERADO | 0.007000 | KG
   - 4023 | AGUA PARA RECETA | 0.003000 | LT
   Aporte nutricional: Agua g 14.47; Energía kcal 33.91; Energía kJ 33.91; Proteínas g 0.83; Grasa total g 0.16; Cenizas g 0.18; Carbohidratos totales 4.22; Carbohidratos disponibles 0.00; Fibra cruda 0.00; Fibra dietaria 0.32; Calcio mg 10.79; Fósforo mg 8.23.
4. Botones: igual que 05 (Agrega Ing., Agrega Línea, Borra Línea, Mueve Up y barra de iconos).
5. Colores: igual (azul = numérico editable; gris = solo lectura).
6. Pie: Gr.Net.Verd.: 0.02 | P.A.V.B.: 80.40 | P.A.V.B. %: 0.00 | Costo: 0.050206.
7. Entidades: las mismas de 05; confirma `receta_ingrediente` con ámbito PATRON (sin régimen ni contrato).
8. Dudas: ver 05. La receta patrón tiene 8 ingredientes; la de régimen 3 tiene más y mayor cantidad.

---------------------------------------------------------------------
## 07 — Receta, pestaña "Detalle Receta Local" (ENSALADA RUSA II)
1. Título: "Receta"; pestaña activa: Detalle Receta Local.
2. Cabecera: Nombre Receta ENSALADA RUSA II; Nombre Fantasia ENSALADA RUSA II; Categoria Dietetica NORMAL; Tipo Plato ENSALADAS; Raciones 1; C. Bruta 0.000000; C. Servida 0.000000; G. Neto 0.000000.
3. Grilla de ingredientes vacía (mismas columnas: Cod. Ing., Descripción, C.Bruta, U. M., %Aprov., %A.Coc.). Panel de aporte con todos los nutrientes en 0.00 (Agua g, Energía kcal, Energía kJ, Proteínas g, Grasa total g, Cenizas g, Carbohidratos totales, Carbohidratos disponibles, Fibra cruda g, Fibra dietaria g, Calcio mg, Fósforo mg).
4. Botones: Agrega Ing., Agrega Línea, Borra Línea, Mueve Up; barra de iconos igual.
5. Colores: igual.
6. Pie: Gr.Net.Verd.: 0.00 | P.A.V.B.: 0.00 | P.A.V.B. %: 0.00 | Costo: 0.000000.
7. Entidades: `receta_ingrediente` ámbito LOCAL (por contrato) — aquí no hay registros, es decir la receta local es opcional y vacía = no personalizada.
8. Dudas: no se ve a qué contrato aplica la receta local (falta selector; se asume el contrato activo de la sesión). No se sabe si Local reemplaza a Patrón o se suma.

---------------------------------------------------------------------
## 08 — Receta, pestaña "Detalle Receta x Regimen" (repetición de 05)
1. Título: "Receta"; pestaña "Detalle Receta x Regimen". Es la misma pantalla y datos que la captura 05 (misma receta ENSALADA RUSA II, Regimen 3), pero con la barra de iconos con el primer icono ("nuevo") y el de documento deshabilitados/ausentes: iconos visibles: nuevo(?), papelera, copiar (grises), X, check, salir/exportar, filtro, binoculares, impresora, salir.
2. Cabecera idéntica: Regimen 3 / REGIMEN 3; Raciones 1; C. Bruta 1.136200; C. Servida 0.236200; G. Neto 1.136200.
3. Grilla idéntica a 05 (7901 HUEVO ROSADO REFRIGERADO CAJA 0.015000 KG 100.000000 100.000000; 2302 PIMIENTA NEGRA MOLIDA 0.001200 ...). Aporte: Agua g 53.89; Energía kcal 810.12; Proteínas g 17.17; Calcio mg 33.10; Fósforo mg 30.31.
4. Botones: igual que 05.
6. Pie: Gr.Net.Verd. 0.07 | P.A.V.B. 25.22 | P.A.V.B. % 0.00 | Costo 0.484232.
7. Entidades: las de 05.
8. Duda: diferencia de iconos habilitados entre 05 y 08 = modo consulta vs. edición (¿permiso de usuario?).

---------------------------------------------------------------------
## 09 — Planificación Real (grilla)
1. Título: "Planificación Real". Menú: "Menú", "Plato Menú". Ruta: Planificación > Planificación Real.
2. Cabecera: banda amarilla "ORCOPAMPA FOALI(PE017401) - REGIMEN 3 - ALMUERZO NORMAL 1". Nota: "Las raciones debe incluir las raciones del personal".
3. Grilla: igual estructura que 04 (filas 1 a 33 de estructura de servicio idénticas), pero cada día tiene bloque: marca R + plato | N.Rac. | Costo (decimal 6) — la columna se llama "Costo" (no "Cto.Plat") y la fila de totales se llama igual "Costo Minuta Día".
   - Costo Minuta Día: 8.63 (Jue 1/10), 6.93 (Vie 2/10), 10.71 (Sáb 3/10), 7.68 (Dom 4/10), 8.15 (Lun 5/10), (Mar 6/10 ilegible/no visible).
   - Ejemplos (Jue 1/10): ENTRADA FRIA 1: ENSALADA RUSA | 442 | 0.457382; ENTRADA CALIENTE (3): ENSALADA RUSA II | 0 | 0.484232; COMPLEMENTO 1: MAYONESA | 343 | 0.127000; SOPA NORMAL 1: SOPA DE CHUÑO NEGRO | 473 | 2.704629; SOPA DIETA: CALDILLO DE HUEVO | 10 | 0.668200; PLATO DE FONDO N: RES - OLLUQUITO CON RES | 196 | 5.208646; GALL - AJI DE GALLINA | 275 | 2.206666; GUARNICION ARRO: ARROZ CON ZANAHORIA | 480 | 0.593380; ISLA 1 LIMON | 520 | 0.067500; BEBIDA FRIA 1: REFRESCO EMOLIENTE/MARAC... | 884 | 0.217802; Comensales: 520.
   - Vie 2/10: HUEVO RELLENO DE VERDURA | 143 | 0.781459; ENSALADA DE LEGUMBRES | 357 | 0.424187; Comensales 520. Sáb 3/10: Comensales 520. Dom 4/10: Comensales 240. Lun 5/10: Comensales 520. Mar 6/10: Comensales 530 (cantidad planificada teórica).
   - En real, los N.Rac. difieren de la teórica (ej. ENSALADA RUSA 442 real vs 239 teórica; Comensales 520 vs 530) y el costo tiene decimales calculados (6 dígitos), probablemente costo real por receta.
4. Barra de iconos como 04 pero algunos distintos (el último icono muestra "X" de Excel verde = exportar a Excel (inferido); iconos para estado/copiar planificación). Ventana sin botón de maximizar visible.
5. Leyendas: verde claro = "Estructura de Servicio"; rojo/salmón = "Celda Bloqueada"; amarillo claro = "Celda Habilitada". Aquí casi todas las celdas están amarillas (habilitadas = mes abierto/editable); las marcas "R" aparecen en color verde sobre la celda en la columna de marca; algunas celdas con "R" rojo/verde (celda bloqueada puntual: ej. Vie 2/10 y Sáb 3/10 ya pasados?). (Interpretación de la R no confirmada.)
6. Totales: fila Costo Minuta Día; fila Comensales. No hay pie con totales mensuales visibles.
7. Entidades: `planificacion_real` / `planificacion_real_linea` (planificacion_id, estructura_linea_id, receta_id, n_raciones, costo real, bloqueada) — análoga a planificación teórica; `comensales_reales`. Probable relación: Real se copia desde Teórica (distintos N.Rac.).
8. Dudas: ¿Real puede cambiar la receta respecto a la teórica? (en 09 los platos coinciden con 04). ¿La R indica "receta"? Fila 3 ENTRADA CALIENTE tiene "ENSALADA RUSA II" con 0 raciones y costo 0.484232 (igual al costo de receta x régimen en cap. 05/08) → el costo del plato = costo de receta por régimen para 1 ración.

---------------------------------------------------------------------
## 10 — Receta, pestaña "Detalle Receta x Regimen" (AJI - CREMA DE ROCOTO)
1. Título: "Receta"; pestaña activa: Detalle Receta x Regimen. Ventana abierta sobre la Planificación Teórica.
2. Cabecera: Regimen 3 / REGIMEN 3; Nombre Receta AJI - CREMA DE ROCOTO; Nombre Fantasia AJI - CREMA DE ROCOTO; Categoria Dietetica NORMAL; Tipo Plato "SALSAS / ALCUZAS / ISLAS\ALCUZAS"; Raciones 1; C. Bruta 0.031100; C. Servida 0.031100; G. Neto 0.031100.
3. Grilla (Cod. Ing. | Descripción | C.Bruta | U. M. | %Aprov. | %A.Coc.):
   - 2302 | PIMIENTA NEGRA MOLIDA | 0.000100 | KG | 100.000000 | 100.000000
   - 2710 | SAL DE COCINA | 0.002000 | KG
   - 2697 | ROCOTO REFRIGERADO | 0.005000 | KG
   - 48 | AJI LIMO REFRIGERADO | 0.010000 | KG
   - 4023 | AGUA PARA RECETA | 0.005000 | LT
   - 428 | CEBOLLA ROJA REFRIGERADO | 0.005000 | KG
   - 6566 | PASTA DE AJO | 0.001000 | KG
   - 5 | ACEITE VEGETAL | 0.003000 | LT
   Aporte nutricional: Agua g 18.72; Energía kcal 35.06; Energía kJ 35.06; Proteínas g 0.19; Grasa total g 3.08; Cenizas g 0.08; Carbohidratos totales 1.98; Carbohidratos disponibles 0.00; Fibra cruda 0.00; Fibra dietaria 0.22; Calcio mg 1.63; Fósforo mg 5.53.
4. Botones: igual que 05.
6. Pie: Gr.Net.Verd.: 0.02 | P.A.V.B.: 1.29 | P.A.V.B. %: 0.00 | Costo: 0.163071.
7. Entidades: igual que 05. Confirma que `tipo_plato` tiene valores con jerarquía/ruta ("SALSAS / ALCUZAS / ISLAS\ALCUZAS").
8. Dudas: el costo de esta receta (0.163071) no coincide con el Cto.Plat de 0.080000 en la planificación teórica (cap. 04 fila ISLA 7 "AJI - CREMA DE ROCOTO" 0.080000/0.090000...). Probable diferencia por precios vigentes o por la fecha. No se determina.

---------------------------------------------------------------------
## 11 — Planificación Teórica, servicio Cena Normal 1 (Agosto 2026) con resumen al pie
1. Título: "Planificación Teórica". Menú: Menú, Plato Menú.
2. Cabecera: "ORCOPAMPA FOALI(PE017401) - REGIMEN 3 - CENA NORMAL 1". Nota: "Las raciones debe incluir las raciones del personal".
3. Grilla, columnas por día: marca R + plato | N.Rac. | Cto.Plat. Encabezado de día: Sáb 1/08/2026, Dom 2/08/2026, Lun 3/08/2026, Mar 4/08/2026, Mié 5/08/2026, Jue 6/08/2026, luego "V..." (viernes).
   - Costo Minuta Día: 4.98 (Sáb 1/08), 4.48 (Dom 2/08), 5.72 (Lun 3/08), 4.82 (Mar 4/08), 4.88 (Mié 5/08), 6.94 (Jue 6/08).
   - Estructura Servicio (filas 1 a 26): 1 SOPA NORMAL 1; 2 SOPA DIETA 1; 3 PLATO DE FONDO N (truncado); 4 PLATO DE FONDO N (truncado); 5 ROMPE RUTINA 1; 6 PLATO DE FONDO D (truncado); 7 GUARNICION ARRO(Z); 8 GUARNICION 1; 9 GUARNICION 2; 10 GUARNICION DIETA; 11 COM SALAD BAR 1; 12 ISLA 1; 13 ISLA 2; 14 ISLA 3; 15 ISLA 4; 16 ISLA 5; 17 ISLA 6; 18 ISLA 7; 19 FRUTA 1; 20 POSTRE NORMAL 1; 21 POSTRE NORMAL 2; 22 POSTRE NORMAL 3; 23 POSTRE NORMAL 4; 24 POSTRE NORMAL 5; 25 BEBIDA CALIENTE 1; 26 Comensales.
   - Ejemplos: Fila 1 SOPA NORMAL 1: Sáb 1/08 CALDO DE GALLINA | 186 | 1.070000; Dom 2/08 SOPA DE SEMOLA CON POLLO I | 102 | 0.940000; Lun 3/08 SOPA CRIOLLA | 184 | 1.370000.
     Fila 3 PLATO DE FONDO: RES - HAMBURGUESA MONTAD(A) | 95 | 2.770000; MON - MONDONGUITO A LA ITAL(IANA) | 44 | 2.280000; RES - ESTOFADO DE RES | 69 | 4.820000.
     Fila 5 ROMPE RUTINA 1: solo Mié 5/08: SANDWICH CON HAMBURGUES(A) | 45 | 3.380000.
     Fila 25 BEBIDA CALIENTE 1: REFRESCO INFUSION FRIA HIEF(...) | 440 | 0.060000.
     Fila 26 Comensales: 220 (Sáb), 120 (Dom), 230 (Lun), 230 (Mar), 230 (Mié), 230 (Jue).
   - Celda con foco (borde verde): Dom 2/08/2026 fila 17 ISLA 6 "AJI - CREMA DE ROCOTO" | 13 | 0.160000.
4. Barra de iconos como 04. Dos iconos pequeños sobre el panel inferior (gráfico de barras y gráfico de líneas = ver gráficos de costo).
5. Leyendas: verde = Estructura de Servicio; rojo = Celda Bloqueada; amarillo = Celda Habilitada. Todo en rojo (mes cerrado).
6. Totales / panel inferior en 3 cuadros:
   - "Total Mes": columnas Cto. Bandeja | Costo Total. Mat.Prima 5.43 | 35,441.27; Est.Fija 0.00 | 0.00; Total 5.43 | 35,441.27; Rac. 6,530.00.
   - "Dom 2/08/2026" (día seleccionado): columnas Planificado | Realizado. Mat.Prima 537.81 | (vacío); Est.Fija 0.00 | (vacío); Cto.Total 537.81 | 710.83; Rac. 120.00 | 310.00; Cto.Band. 4.48 | 2.29.
   - "Acumulado hasta Dom 2/08/2026": Planificado | Realizado. Mat.Prima 1,633.20; Est.Fija 0.00; Cto.Total 1,633.20 | 1,988.69; Rac. 340.00 | 310.00; Cto.Band. 4.80 | 6.42.
7. Entidades: igual que 04; además `costo_diario` (planificado vs realizado: materia_prima, estructura_fija, costo_total, raciones, costo_bandeja) por contrato/regimen/servicio/fecha; `estructura_fija` (costo de estructura, 0.00 aquí). El "Realizado" proviene de Planificación Real/consumos.
8. Dudas: Cto.Bandeja = Costo total / Raciones (537.81/120 = 4.48; consistente). Realizado Cto.Band. 2.29 con 310 raciones (710.83/310 = 2.29). Acumulado Rac.: planificado 340 (220+120) vs realizado 310 (solo domingo?), inconsistencia aparente → depende de qué días tienen real. Origen de "Est.Fija" desconocido.

---------------------------------------------------------------------
## 12 — Aporte Planificación Teórica
1. Título: "Aporte Planificación Teórica". Ruta: Planificación Teórica > botón de aportes nutricionales del día seleccionado (Dom 2/08/2026, inferido).
2. Cabecera: Contrato ORCOPAMPA FOALI; Regimen REGIMEN 3; Servicio CENA NORMAL 1.
3. Grilla, columnas: (número de fila) | Cód. (entero código de receta) | Nombre Recetas (texto) | Bruto (decimal 6) | Servida (decimal 6) | Neto (decimal 6) | Agua g | Energía kcal | Energía kJ | (más nutrientes a la derecha, con scroll horizontal) (decimal 2). Columnas Cód..Neto en verde; nutrientes en amarillo.
   - 1 | 355 | SOPA DE SEMOLA CON POLLO I | 0.492620 | 0.492620 | 0.492620 | 324.98 | 279.63 | 279.63
   - 2 | 112 | CONSOME DE POLLO | 0.474620 | 0.474620 | 0.474620 | 324.98 | 262.03 | 262.03
   - 3 | 297 | MON - MONDONGUITO A LA ITALI(ANA) | 0.294820 | 0.294820 | 0.294820 | 155.40 | 265.34 | 265.34
   - 4 | 465 | POLL - POLLO AL HORNO | 0.310360 | 0.310360 | 0.310360 | 12.01 | 378.96 | 299.79
   - 5 | 1387 | POLL - POLLO GUISADO C/ LIMON(...) | 0.347060 | 0.347060 | 0.347060 | 30.43 | 387.31 | 308.11
   - 6 | 270 | ARROZ BLANCO | 0.252000 | 0.252000 | 0.252000 | 120.00 | 487.83 | 57.03
   - 7 | 414 | PAPAS FRITAS | 0.084000 | ... | 57.37 | 118.89 | 118.89
   - 8 | 699 | ENSALADA RUSA GUAR I | 0.098120 | ... | 43.30 | 99.65 | 99.65
   - 9 | 937 | VERDURAS COCIDAS | 0.190120 | ... | 92.90 | 183.22 | 183.22
   - 10 | 1067 | SALAD BAR II | 0.120000 | ... | 86.40 | 23.70 | 23.70
4. Botones: icono Excel (exportar), puerta (salir). 
5. Colores: verde = datos de receta; amarillo = aportes nutricionales.
6. Totales: ninguno visible.
7. Entidades: `receta_aporte_nutricional` (receta_id, regimen_id, nutriente_id, valor) y vista agregada por planificación; `nutriente` (Agua g, Energía kcal, Energía kJ, Proteínas, Grasa, ... ver 05).
8. Dudas: Bruto/Servida/Neto en kg por ración (se ve que Servida y Neto = Bruto). Código 112 aquí es receta CONSOME DE POLLO (mientras en recetas 112 es código de producto ARVEJA PELADA CONGELADO: son maestros distintos con códigos que se solapan). Lista = platos del día para la CENA NORMAL 1 de ese día (10 filas visibles de más).

---------------------------------------------------------------------
## 13 — Frecuencia Planificación Teórica
1. Título: "Frecuencia Planificación Teórica". Ruta: Planificación Teórica > botón Frecuencia (inferido). Tooltip visible "Salir" sobre el icono de la puerta.
2. Cabecera: Contrato ORCOPAMPA FOALI; Regimen REGIMEN 3; Servicio CENA NORMAL 1.
3. Grilla, columnas: (fila) | Cód. (entero, código receta) | Nombre Recetas (texto) | Frecuencia (entero, cuántas veces se planifica en el mes) | Costo (decimal 2) | Lun | Mar | Mié | Jue | Vie | Sáb | Dom | Lun (celdas de calendario: la semana, con raciones en el día planificado).
   - 1 | 23 | CER - CERDO AL HORNO | 1 | 2.29
   - 2 | 54 | YUCA DORADA | 2 | 0.68
   - 3 | 56 | RES - ESPAGUETTI A LA BOLOG(NESA) | 1 | 3.11
   - 4 | 57 | RES - ESTOFADO DE RES | 2 | 4.82 ; Lun (2ª): 69
   - 5 | 61 | RES - GUISO DE RES A LA CRIO(LLA) | 1 | 4.59
   - 6 | 68 | RES - PICANTE DE RES | 1 | 4.80
   - 7 | 108 | CALDILLO DE HUEVO | 2 | 0.67
   - 8 | 109 | CALDO DE GALLINA | 1 | 1.07 ; Sáb: 186
   - 9 | 110 | CALDO DE POLLO | 4 | 0.74
   - 10 | 111 | CHUPE DE VERDURAS | 2 | 1.50
   - 11 | 112 | CONSOME DE POLLO | 3 | 0.86 ; Dom: 5
   - 12 | 113 | CONSOME DE RES | 1 | 2.03
   (la tabla es un calendario por semanas; las celdas muestran n_raciones en el día en que aparece la receta, columnas grises = fuera del mes).
4. Botones: icono Excel (exportar), puerta Salir.
5. Colores: verde = datos de receta; celdas amarillas = días del mes; celdas grises = días fuera del mes/ no aplicables.
6. Totales al pie: "Total Recetas Listadas" 211.00; "Costo Promedio Diario" 15.70.
7. Entidades: consulta/agregado sobre `planificacion_teorica_linea` (receta_id, fecha, n_raciones, costo) agrupada por receta: frecuencia = count(fechas). Sin tabla nueva.
8. Dudas: Total Recetas Listadas 211.00 con 2 decimales: ¿suma de frecuencias o número de recetas distintas? No se sabe. Costo Promedio Diario 15.70 = suma de costo de minuta por día / días (distinto a Costo Minuta Día 4.98 de cap. 11 por ser todo el servicio vs. estructura?). No resuelto.

---------------------------------------------------------------------
## 14 — Salida y Devolución de Producción (parámetros de informe)
1. Título: "Salida y Devolución de Producción". Ruta: Informes Transacciones Ent/Sal > Salida de Bodega a Producción (S) / Devolución de Producción (E) (ver cap. 15).
2. Cabecera:
   - Contrato -> PE017401 ; ORCOPAMPA FOALI
   - Tipo de Informe -> (lista desplegable abierta) opciones:
     1. Formato de Requisición x Servicio
     2. Formato de Requisición x Sector
     3. Formato de Requisición x Estructura Servicio Detallado (seleccionada, azul)
     4. Formato de Requisición x Estructura Servicio Resumido
     5. Resumen de Salida a Bodega
     6. Devolución de Salida a Bodega
     7. Salida Menos Devoluciones a Bodega
   - Fecha Inicio -> (oculta por la lista; también se intuye Fecha Fin, ilegible)
   - Grupo "Regimen": opción de radio "Todos" (marcada) y otra opción (con selector de mano a la derecha, texto parcial "...a" ilegible) para un régimen específico.
   - Casilla "Salto Página" (sobre fondo rojo, sin marcar).
3. Grillas: ninguna.
4. Botones/iconos: vista previa/imprimir (icono de lupa con página) y salir (puerta).
5. Colores: etiqueta "Salto Página" con fondo rojo (resaltado, significado no leyenda).
6. Totales: no.
7. Entidades: `salida_produccion` / `devolucion_produccion` (cabecera: contrato, fecha, número; detalle por estructura de servicio, servicio, sector, producto, cantidad). `sector` (zona de producción). `tipo_informe` (parámetro de reporte, no tabla).
8. Dudas: la salida a producción se agrupa por Servicio, Sector o Estructura de Servicio → el detalle de la salida debe guardar servicio_id, sector_id y estructura_servicio_linea_id. Falta ver el rango de fechas completo y régimen.

---------------------------------------------------------------------
## 15 — Menú "Informes Transacciones Ent/Sal" (barra de menú principal)
1. Título: no hay ventana; es el menú desplegable de la barra principal del SGP.
2. Barra superior visible: "...Informes Transacciones Ent/Sal | Otros Informes | Cierre | Base de Datos | General | Salir" (las opciones anteriores quedan fuera de imagen).
3. Opciones del menú Informes Transacciones Ent/Sal (cada una con submenú ">", salvo las dos últimas):
   - Recep. Proveedor (E)/ FOFI (E)
   - Traspasos entre Cont (E/S) / CD (E/S) / Dev. CD (S)
   - Traspasos entre Bodega (E/S)
   - Salida de Bodega a Producción (S) / Devolución de Producción (E)
   - Salida de Bodega por Mermas (S)
   - Salida de Bodega por Venta Cafetería (S)
   - Stock (submenú abierto)
   - Reporte Registro de Mermas
   - Reporte Traslados ADS
   Submenú Stock: Posicion Stock; Movimiento Stock; Consumo Alternativo (ex ajuste de inventario); Cartola Inventario; Detalle Cartola de Inventario; Producto Sin Movimiento; Registro De Inventario Permanente Valorizado.
4. Botones: n/a (es menú). Fondo: logotipo SGP.
5. Leyendas: n/a.
6. Totales: n/a.
7. Entidades inferidas de los tipos de movimiento (E=entrada, S=salida): `movimiento_bodega` con tipos: RECEPCION_PROVEEDOR (E), FOFI (E) (FOFI desconocido), TRASPASO_CONTRATO (E/S), CD = Centro de Distribución (E/S), DEVOLUCION_CD (S), TRASPASO_BODEGA (E/S), SALIDA_PRODUCCION (S), DEVOLUCION_PRODUCCION (E), SALIDA_MERMA (S), SALIDA_VENTA_CAFETERIA (S); `merma` (registro de mermas); `traslado_ads` (ADS desconocido); `stock` (posición, movimiento, cartola de inventario, inventario permanente valorizado = kardex valorizado); `ajuste_inventario` (consumo alternativo es "ex ajuste de inventario").
8. Dudas: significado de FOFI, ADS y CD (Centro de Distribución, inferido). Otros menús (Cierre, Base de Datos, General, Otros Informes) no se ven aquí. Estos son informes; los registros de captura de las transacciones están en otras pantallas.

---------------------------------------------------------------------
# RESUMEN: entidades candidatas y capturas que las muestran

| Entidad candidata (español) | Columnas clave / relaciones | Capturas |
|---|---|---|
| contrato | codigo (PE017401), nombre, codigo_optimum | 01, 02, 04, 09, 11, 12, 13, 14 |
| bodega | codigo (PE017401 - FOALI), contrato_id | 01 |
| traspaso_contrato (cabecera) | numero_documento, folio, fecha_emision, contrato_destino, contrato_origen, bodega, tipo, nro_doc_origen, fecha_origen, guia, orden_pedido, placa | 01, 15 |
| traspaso_contrato_detalle | producto, unidad, cantidad, cantidad_recibida, precio, total | 01 |
| producto / unidad_medida | codigo (12 dígitos o código corto), descripcion, unidad (SACO, KG, LT) | 01, 05, 06, 07, 10 |
| regimen | codigo (1, 2, 3, 5), nombre (REGIMEN 1/2/3/GENERAL) | 02, 03, 04, 05, 10, 11, 14 |
| servicio | codigo (31, 141, 281, 441, 639, 725, 727, 728, 784, 921), nombre | 02, 03, 04, 11 |
| contrato_regimen_servicio | contrato, regimen, servicio | 02, 03 |
| estructura_servicio (líneas) | servicio, orden, nombre de tipo de plato (ENTRADA FRIA 1, ISLA 1, ...) | 04, 09, 11 |
| planificacion_teorica (mes/estado) | contrato, regimen, servicio, periodo, estado (Cerrado) | 02, 03 |
| planificacion_teorica_linea | fecha, estructura_linea, receta, n_raciones, costo_plato, bloqueada | 04, 11, 13 |
| planificacion_real / linea | igual con costo real y habilitada | 09 |
| comensales_dia | contrato, regimen, servicio, fecha, cantidad (fila Comensales) | 04, 09, 11 |
| costo_diario_planificado_realizado | mat. prima, est. fija, costo total, raciones, costo bandeja | 11 |
| receta | codigo, nombre, nombre_fantasia, categoria_dietetica, tipo_plato | 05, 06, 07, 08, 10, 12, 13 |
| categoria_dietetica | NORMAL | 05, 06, 07, 10 |
| tipo_plato | ENSALADAS; SALSAS / ALCUZAS / ISLAS\ALCUZAS | 05, 06, 07, 10 |
| receta_ingrediente (ámbito patrón / local / régimen) | receta, regimen?, contrato?, producto, cantidad_bruta, unidad, %aprov, %ajuste cocción | 05, 06, 07, 08, 10 |
| nutriente / aporte nutricional (producto y receta) | nombre, unidad, valor | 05, 06, 07, 10, 12 |
| metodo_preparacion, grupo_vulnerable (pestañas, sin ver) | receta | 05 a 08, 10 (solo pestañas) |
| salida_produccion / devolucion_produccion | contrato, fecha, servicio, sector, estructura servicio | 14, 15 |
| sector | zona de producción | 14 |
| movimientos de bodega (recepción, traspaso bodega, mermas, venta cafetería, CD, FOFI, ADS) | tipo, E/S | 15 |
| stock / inventario (posición, cartola, permanente valorizado) | producto, bodega, cantidad, costo | 15 |

Total capturas transcritas: 15.
