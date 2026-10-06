# Especificación común del Frontend WinForms

## 1. Plataforma

- .NET 8.
- Visual Basic .NET.
- Windows Forms.
- PostgreSQL.
- Windows 10/11.
- Arquitectura existente: Dominio / Datos / Escritorio / Instalador.

## 2. Regla crítica del Diseñador

Cada formulario visual debe ser un WinForm real compatible con Visual Studio Designer.

### Obligatorio

```text
FormX.vb
FormX.Designer.vb
```

- `Partial Public Class FormX`.
- `InitializeComponent()` real.
- Los controles permanentes deben estar definidos en `.Designer.vb`.
- No crear toda la interfaz programáticamente en el constructor.
- El constructor recibe servicios/contexto y configura eventos/datos.
- La UI dinámica solo se permite para elementos cuyo número depende realmente de los datos, por ejemplo columnas de días del mes.

### Prohibido

- `Controls.Clear()` para reemplazar el diseño del Designer durante runtime.
- duplicar una versión visual de diseño y otra completamente distinta en ejecución;
- SQL embebido en eventos del formulario;
- reglas de costos, stock o permisos implementadas solo en la UI.

## 3. Shell principal

### FormPrincipal

Diseño recomendado:

```text
┌────────────────────────────────────────────────────────────┐
│ AppSistema | Empresa | Operación | Usuario | Estado Sync   │
├───────────────┬────────────────────────────────────────────┤
│ Navegación    │                                            │
│               │             MDI / Contenido                │
│ Inicio        │                                            │
│ Catálogo      │                                            │
│ Planificación │                                            │
│ Producción    │                                            │
│ Compras       │                                            │
│ Almacén       │                                            │
│ Inventario    │                                            │
│ Cierres       │                                            │
│ Resultados    │                                            │
│ Administración│                                            │
├───────────────┴────────────────────────────────────────────┤
│ Estado BD | Último cierre | Sync central | Versión        │
└────────────────────────────────────────────────────────────┘
```

El menú actual puede seguir siendo `MenuStrip`, pero debe organizarse con la misma taxonomía funcional.

## 4. Anatomía estándar de cada formulario

### Zona A - Encabezado
Debe incluir:

- nombre funcional;
- operación actual;
- estado;
- permiso efectivo;
- fecha/período si corresponde.

### Zona B - Filtros
Filtros alineados en una sola barra:

- empresa (solo perfiles globales);
- operación;
- almacén/bodega;
- régimen;
- servicio;
- rango de fechas o mes;
- estado.

### Zona C - Acciones
Botones con texto, no solo iconos:

- Nuevo
- Editar
- Guardar
- Aprobar
- Confirmar
- Anular/Revertir
- Actualizar
- Vista previa
- Exportar
- Cerrar

Acciones irreversibles deben estar separadas visualmente.

### Zona D - Contenido
Usar:

- `DataGridView` para tablas;
- `TabControl` cuando una entidad tenga cabecera/detalle/historial;
- `SplitContainer` para maestro-detalle;
- panel lateral para detalle contextual cuando evite abrir ventanas innecesarias.

### Zona E - Resumen
Para procesos con costo/stock:

- cantidad;
- valor;
- desviación;
- costo por comensal;
- Food Cost;
- estado.

### Zona F - Estado
Pie persistente:

```text
128 filas | Última actualización 23:10 | Borrador | Sin cambios pendientes
```

## 5. Sistema visual

### Tipografía
- Segoe UI.
- 9.5 a 10 pt contenido.
- 14-18 pt títulos.
- Evitar texto menor de 9 pt.

### Colores semánticos

No depender únicamente del color; siempre acompañar con texto/icono.

- Borrador: amarillo suave.
- Aprobado: verde suave.
- Cerrado/bloqueado: gris.
- Error: rojo.
- Advertencia: ámbar.
- Información: azul.
- Diferencia positiva: indicador `+`.
- Diferencia negativa: indicador `-`.

### Inspiración SGP conservada

Las capturas usan colores para indicar:

- celdas bloqueadas;
- celdas habilitadas;
- estado de día;
- stock disponible/no disponible.

AppSistema debe mantener la semántica, pero con una paleta accesible.

## 6. DataGridView

Requisitos:

- columnas con nombres funcionales;
- encabezados multilínea cuando sea necesario;
- columnas de identificación congeladas;
- números alineados a la derecha;
- unidades visibles;
- dinero con `S/`;
- cantidades con precisión por unidad;
- filtros externos, no filtros ocultos;
- selección de fila completa en maestro;
- selección de celda en planificación mensual;
- no permitir edición si estado/permiso no lo permite.

Para grillas grandes:

- paginar o virtualizar;
- evitar `AutoSizeColumnsMode=AllCells` en miles de registros;
- permitir búsqueda incremental;
- filtros rápidos.

## 7. Fechas y períodos

- Día: `dd/MM/yyyy`.
- Mes: `MMMM yyyy`.
- Base de datos: fecha sin hora para períodos.
- La lógica del día operativo debe usar zona horaria definida por negocio, no la hora local arbitraria del PC.

## 8. Estados estándar

### Documento
- BORRADOR
- CONFIRMADO
- ANULADO / REVERTIDO

### Plan
- BORRADOR
- EN_REVISION
- APROBADO
- LIBERADO
- REEMPLAZADO
- CERRADO

### Pedido
- BORRADOR
- CALCULADO
- APROBADO
- ENVIADO
- PARCIALMENTE_RECIBIDO
- RECIBIDO
- ANULADO

### Inventario
- ABIERTO
- EN_CONTEO
- CONTADO
- EN_REVISION
- APROBADO
- AJUSTADO
- CERRADO

## 9. Confirmaciones

Ejemplo:

```text
CONFIRMAR SALIDA A PRODUCCIÓN

Documento: SP-2026-00063169
Servicio: Almuerzo Normal 1
Fecha: 04/10/2026
Productos: 63
Valor: S/ 1,438.27

Después de confirmar, el movimiento no podrá editarse.
Las correcciones deberán hacerse mediante reversión.

[Cancelar] [Confirmar salida]
```

## 10. Accesibilidad y teclado

- `AccessibleName` en controles relevantes.
- Orden de tabulación coherente.
- `Alt` + letra para acciones comunes.
- `Ctrl+F`: buscar.
- `Ctrl+R`: actualizar.
- `Ctrl+P`: vista previa.
- `Ctrl+K`: ir a pantalla.
- `Esc`: cancelar/cerrar diálogo no guardado.
- Enter no debe confirmar acciones destructivas por accidente.

## 11. Mensajes de error

No mostrar excepciones técnicas directamente.

Formato:

```text
No se pudo confirmar el despacho.

Motivo:
Stock insuficiente para ACEITE VEGETAL CIELO 5 LT.

Disponible: 2 BID
Solicitado: 4 BID

Acción recomendada:
Revise el requerimiento o registre la recepción pendiente.
```

Registrar detalle técnico en log/auditoría.

## 12. Seguridad en UI

Un botón oculto no sustituye autorización.

Toda operación debe validarse otra vez en servicio/base.

La pantalla debe:

1. ocultar opciones no permitidas;
2. deshabilitar acciones condicionadas;
3. mostrar por qué una acción está bloqueada;
4. dejar al backend la decisión definitiva.

## 13. Reportes

Toda vista previa debe compartir un componente común:

- título;
- empresa/operación;
- filtros;
- fecha de generación;
- usuario;
- paginación;
- imprimir;
- exportar Excel;
- exportar PDF;
- CSV cuando sea tabular;
- ajuste de columnas;
- orientación.

## 14. Auditoría visual

Para entidad sensible debe existir opción:

`Ver historial`

Mostrar:

- fecha/hora;
- usuario;
- operación;
- acción;
- valor anterior;
- valor nuevo;
- motivo;
- documento relacionado.

## 15. Criterio mínimo de aceptación de cualquier Form

Un formulario no está terminado hasta cumplir:

- abre sin excepción;
- abre desde `FormPrincipal`;
- respeta permisos;
- visualiza correctamente a 1366x768;
- tab order correcto;
- Designer abre en Visual Studio;
- carga datos reales;
- no duplica registros al refrescar;
- valida estado de negocio;
- muestra errores comprensibles;
- registra auditoría cuando corresponde;
- cuenta con prueba E2E del flujo principal;
- cuenta con prueba negativa de permiso/estado;
- no introduce warnings en compilación.
