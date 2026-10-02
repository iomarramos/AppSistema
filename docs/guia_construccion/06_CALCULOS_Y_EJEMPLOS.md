# Cálculos, ejemplos de referencia y conciliación
Todos los datos son ficticios. Las políticas propuestas requieren decisión antes de uso operativo.

## 1. Unidades y empaques
Contenido por empaque = envases por empaque × contenido base por envase.
Cantidad recibida base = empaques recibidos × contenido por empaque.
Costo base = costo atribuible a la línea / cantidad recibida base.

| Caso | Resultado |
|---|---:|
| Caja A: 4 envases × 4 L | 16 L/caja |
| Recepción 2 cajas A | 32 L |
| Precio caja A S/128 | S/8 por L |
| Variante B: envase 5 L a S/45 | S/9 por L |
| 2 envases B | 10 L/S/90 |
| Stock A 32 L y B 10 L | Total 42 L y S/346, conservando los dos saldos |

El cociente 346/42 puede mostrarse como indicador agregado; no autoriza valorar una salida de A a ese costo combinado. Nunca inferir contenido desde la palabra galón. Recibir media caja requiere unidad física clara: dos envases de 4 L son 8 L.

## 2. Receta, minuta y costo previsto
La cantidad de la receta corresponde a su rendimiento completo. Necesidad de ingrediente = cantidad bruta de la versión × raciones a preparar / rendimiento de la receta. Convertir a unidad base antes de sumar entre recetas.

Receta con rendimiento 10 raciones: 1 L de aceite. Para 150 raciones requiere 15 L. A precio de referencia S/8/L: contribución del aceite S/120 y S/0,80 por ración. Esto no es el costo de toda la receta si tiene más ingredientes.

Costo receta completa = suma de cantidades base × precios de referencia. Costo por ración = costo receta / rendimiento. Costo servicio = suma de costos de preparaciones por sus raciones + estructura fija. Costo por comensal = costo servicio / comensales. No sumar raciones de sopa y fondo como si fueran comensales distintos. Rendimiento y comensales cero requieren tratamiento explícito.

La fuente del precio genérico puede ser variante preferida, presupuesto aprobado u otra política; queda pendiente. Si falta precio, marcar costo incompleto. Guardar fuente, fecha, moneda y valor usado. No elegir automáticamente la variante más barata si no está permitida.

## 3. Previsión de compras
Para un horizonte sin solapamientos:
Necesidad neta = máximo(0, demanda del horizonte + reserva al final − stock utilizable inicial − recepciones elegibles).
Si el cálculo comienza antes del horizonte, descontar el consumo puente del stock proyectado, o agregarlo como demanda previa; nunca ambas cosas. El término NR del documento original no debe sumarse si ya está incluido en NT.

Descontar de un pedido solo su saldo realmente pendiente, aprobado, no cancelado y con llegada útil. Considerar pedidos adicionales cuando estén comprometidos: ignorarlos por herencia del sistema antiguo puede duplicar compras. Esta adaptación comercial debe quedar registrada.

| Variable | Ejemplo |
|---|---:|
| Demanda del horizonte | 50 L |
| Reserva objetivo | 10 L |
| Stock inicial utilizable | 27 L |
| Recepción pendiente que llega a tiempo | 8 L |
| Necesidad neta | 25 L |
| Caja elegida | 16 L |
| Pedido con múltiplo 1 y mínimo 1 | 2 cajas = 32 L |
| Excedente por redondeo | 7 L |
| Stock final proyectado | 27 + 8 + 32 − 50 = 17 L |

Demanda debe analizarse además por fecha: una recepción al final del mes no cubre una falta al inicio. Recorrer eventos cronológicamente, proyectar saldo y señalar fechas de quiebre. El total mensual positivo no prueba disponibilidad diaria.

## 4. Mínimo y múltiplo
Para necesidad N>0, contenido C, mínimo de empaques m y múltiplo k:
Empaques = k × techo(máximo(N/C, m) / k).
Para N=0, empaques=0: no forzar mínimo si no se compra.

Ejemplo anterior con múltiplo 3: 3 cajas = 48 L; exceso 23 L. Si mínimo 4 y múltiplo 3, comprar 6 cajas. Validar m y k positivos. Si se combinan variantes, asignar primero porciones de necesidad y redondear cada asignación; recalcular el total para no abastecer dos veces los mismos 25 L.

## 5. Valoración propuesta: promedio ponderado móvil
Propuesta exclusiva de demostración hasta confirmación. Ámbito sugerido: empresa + almacén + variante. Costo medio nuevo = (valor anterior + costo de entrada) / (cantidad anterior + cantidad de entrada).

| Movimiento de la misma variante | Cantidad saldo | Valor saldo | Costo medio |
|---|---:|---:|---:|
| Entrada 32 L a S/8 | 32 L | S/256 | S/8 |
| Entrada 16 L a S/10 | 48 L | S/416 | S/8,666666… |
| Salida 6 L | 42 L | S/364 | S/8,666666… |

La salida exacta vale S/52 antes de redondeos internos. No multiplicar a ciegas el costo unitario ya redondeado si introduce residuales. Conservar numerador/denominador o aritmética decimal suficiente; redondear el valor de movimiento a la escala acordada y derivar el saldo. Al agotar todo el stock, asignar el valor residual restante a la última salida según política documentada. Nunca dejar cantidad cero con dinero residual inadvertido.

Devolución desde cocina: referenciar la salida y su costo histórico; no exceder lo entregado menos devoluciones previas. Reversar una recepción después de consumos exige analizar disponibilidad y efecto de valoración; no borrar historia. La decisión final de valoración define estos casos.

## 6. Enteros escalados
Para cantidades y costo `_u6`, producto monetario escalado = redondear(cantidad_u6 × costo_u6 / 1 000 000). Para factores, aplicar igual control de escala. Utilizar enteros de precisión suficiente o decimal; verificar límites de persistencia. Propuesta: seis decimales internos y dos en visualización monetaria, con modo de redondeo único documentado. Totales oficiales deben conciliar por líneas; no sumar exclusivamente números de pantalla.

## 7. Producción y mermas
Consumo neto de materia prima asignado al servicio = entregas + adicionales − devoluciones de ingredientes utilizables. Si se mantendrá stock crudo en cocina entre días, debe modelarse como ubicación/almacén y descontarse al consumir; no tratar simultáneamente la entrega como consumo y como transferencia.

Ejemplo sin stock remanente en cocina: 5 + 1 − 0,5 = 5,5 L. A S/8/L el costo es S/44. Una merma de 0,2 L ya incluida en esa entrega se identifica como parte de los 5,5 L, no eleva el consumo a 5,7 L. Una merma en almacén anterior al despacho sí exige baja específica.

Excedente de plato preparado no se convierte de nuevo en arroz o aceite. Registrar cantidades producidas, servidas, excedentes y pérdidas en unidades coherentes. Si no se mide consumo por receta, informar el costo real del servicio; un reparto teórico se etiqueta como asignación estimada.

## 8. Inventario
Diferencia = físico − sistema al corte. Diferencia valorizada = diferencia × costo al corte. Sistema 27 L, físico 26 y costo S/8: −1 L/−S/8. El reporte no mueve stock. Si posteriormente se autoriza ajuste negativo: saldo 26 L/S/208; movimiento de ajuste único con documento e historial.

No aplicar un faltante antiguo sobre un saldo actual sin reconciliar movimientos posteriores al corte. Propuesta inicial: ventana corta de conteo con movimientos bloqueados en el almacén; otras modalidades requieren reconciliación por secuencia.

## 9. Conciliación y Food Cost
Saldo final = saldo inicial + entradas − salidas, por variante y almacén, tanto en cantidad como en valor. Para calcular consumo operacional separar salidas a cocina, bajas, transferencias externas e internas y ajustes. Las transferencias internas se eliminan al consolidar la sede. No presentar toda salida como consumo de alimentos.

Food Cost real = costo de alimentos reconocido / ingreso neto del mismo servicio y período × 100. Ejemplo: S/4 200 / S/10 000 = 42 %. Objetivo 40 %: desviación +2 puntos porcentuales y S/200 sobre presupuesto de alimentos. No decir “subió 2 %” como equivalente de dos puntos porcentuales. Presupuesto = ingreso × objetivo. Ingreso cero o negativo: no calculable y revisar causa. Sin gastos de personal y operación completos, no llamar utilidad neta al margen tras alimentos.

Si se distribuye ingreso mensual por días para un indicador diario, documentar criterio (días calendario o días de servicio) y mostrar “ingreso asignado”; no inventar facturación diaria por raciones.
