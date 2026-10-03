# Trabajo seguro, comunicación y operación

## Control de cambios
Antes de editar, revisar estado Git y diferencias. No descartar cambios ajenos. Crear rama de tarea si el proyecto lo usa y mantener commits pequeños: catálogo, regla, prueba y documentación correspondiente. No guardar secretos ni bases reales en Git. Usar datos sintéticos.

Una tarea registra alcance, archivos, dependencia y aceptación. Un cambio de esquema debe coordinarse con API e interfaz. Un único responsable ordena migraciones. Si participan varios programadores, repartir módulos y acordar contratos antes de editar archivos compartidos; no es necesario usar varios agentes para este plan.

## Conflictos
- Git: leer ambos cambios y su intención, resolver sin eliminar trabajo válido, repetir pruebas afectadas. No elegir automáticamente “ours” o “theirs”.
- Datos: versión esperada en edición; informar conflicto y permitir recargar/comparar borrador.
- Stock: transacción y bloqueos; no reintentar indiscriminadamente una salida como si nada hubiera ocurrido.
- Idempotencia: tras timeout consultar el resultado por clave; no generar clave nueva para el mismo intento.
- Sincronización: conservar evento original, causa y estado; evitar mezcla silenciosa de saldos.
- Requisitos: registrar decisión y efecto sobre módulos, pruebas y migraciones; no mantener dos reglas contradictorias activas.

## Ritmo de comunicación
Inicio: “Trabajaré catálogo. Terminará cuando podamos registrar marcas y empaques y convertir 2 cajas en 32 L. La valoración sigue pendiente y no bloquea esta parte”.
Avance: “La conversión funciona. Detecté que cambiar el empaque alteraría una recepción antigua; estoy incorporando el factor histórico”.
Bloqueo: “Falta definir si el impuesto forma parte del costo. Puedo continuar cantidades y permisos; la valorización real de recepción depende de esa respuesta”.
Cierre: “Implementado X; probado con Y; pendientes Z; siguiente tarea W”.

Informar aproximadamente cada minuto durante trabajo prolongado cuando la plataforma lo permita, sin saturar con cada comando. Si el usuario interrumpe para completar contexto, pausar. No pedir autorización para cada cambio reversible ya incluido en la tarea. No ocultar decisiones que alteran dinero, existencias o reglas de cierre.

## Plantilla de cambio revisable
Problema observable → comportamiento esperado → alcance de la solución → migración → pruebas → impacto y recuperación. Ejemplo: “La segunda confirmación de una recepción duplicaba stock; ahora reutiliza resultado idempotente. Prueba: doble solicitud deja una recepción y 32 L”. No afirmar ese resultado hasta ejecutarlo.

## Entornos
Desarrollo con fixtures; prueba integrada con configuración representativa; piloto aislado; producción cuando exista autorización y aceptación. Configuración por entorno y secretos fuera del repositorio. La misma migración probada se aplica al entorno destino. No conectar automáticamente una integración real desde pruebas.

## Copia y recuperación
Definir RPO (pérdida máxima tolerable) y RTO (tiempo máximo de recuperación) con el negocio. Propuesta de discusión: RPO 24 horas/RTO 4 horas; no son compromisos hasta validarlos. SQLite necesita mecanismo de copia consistente, no copiar un archivo activo sin tratar WAL. Otros motores requieren procedimiento propio. Incluir configuración necesaria y versión de esquema, cifrado, retención y destino separado.

Ensayo: generar actividad conocida, hacer respaldo, restaurar en entorno aislado, verificar usuarios, documentos, cantidades, valores y eventos pendientes. No reenviar integraciones de producción desde la copia restaurada. Registrar duración y pérdida real. Una copia no ensayada no acredita recuperación.

## Sincronización
Autoridad por sede; IDs globales; secuencia de origen; outbox en la misma transacción; inbox con unicidad; acuse tras persistencia durable; reintentos con espera creciente y límite/alerta; eventos con versión de esquema. Incluir operación, empresa, origen, ID, secuencia, versión y referencia de documento. Verificar autorización del origen.

Las aplicaciones no reconstruyen valoración usando precios actuales al replicar; replican los importes ya contabilizados. No sumar simultáneamente eventos y saldos de snapshot. Una reconciliación compara recuentos, secuencias y saldos derivados, y lleva diferencias a revisión. Cierre local puede estar cerrado y pendiente de sincronizar; la central debe mostrarlo claramente.

## Observabilidad y soporte
Logs con correlación, usuario autorizado, sede, acción, duración y resultado sin secretos. Métricas: errores de confirmación, pendientes de sincronizar, antigüedad de último respaldo, conflictos, discrepancias de conciliación y tiempos de respuesta. Documentar diagnóstico y responsable por incidencia.

## Actualizaciones
Respaldar, probar migración y compatibilidad, aplicar en ventana acordada y ejecutar pruebas de humo. Para revertir, distinguir fallo de aplicación de transformación de datos; no ejecutar migraciones destructivas inversas sin verificar. Mantener número de versión visible y notas de cambio comprensibles para operadores.
