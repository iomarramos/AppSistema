# Guía de construcción por módulos
Versión 1.0 · 02/10/2026 · Documentación de desarrollo, no aplicación terminada.

## Objetivo
Construir un sistema comercial para empresas pequeñas y medianas de servicios de alimentación. Piloto de una operación y un almacén; datos aislados por empresa desde el inicio. El cliente típico recibe un ingreso mensual por servicio. Las raciones sirven para planificar y medir consumo, no para inferir automáticamente ingresos.

## Cómo comenzar
1. Descomprimir todo el paquete en una carpeta de trabajo del proyecto.
2. Leer `01_PROMPT_MAESTRO.md` y copiar su contenido completo al asistente programador junto con el acceso a estos archivos.
3. Ejecutar primero el encargo de etapa 0 de `10_PROMPTS_POR_ETAPA.md`. Revisar el repositorio real; no crear un sistema paralelo si ya existe código.
4. Continuar con fundamentos y catálogo antes de menús. Los seis módulos visibles siguen siendo menús/recetas, compras, almacén, producción, inventarios y cierres.
5. Usar `09_SEGUIMIENTO_Y_DECISIONES.md` para registrar avances reales. No marcar algo terminado porque exista una tabla o una pantalla.

## Documentos
| Archivo | Para qué sirve |
|---|---|
| 01_PROMPT_MAESTRO.md | Instrucciones completas para el programador senior o agente |
| 02_ALCANCE_Y_REGLAS.md | Fuente funcional consolidada y prioridades |
| 03_ARQUITECTURA_Y_DATOS.md | Límites de módulos, integridad, esquema existente y migraciones |
| 04_PLAN_POR_ETAPAS.md | Tareas, dependencias y criterios para avanzar |
| 05_CONTRATOS_API_Y_PANTALLAS.md | Acciones, datos, errores y pantallas propuestas |
| 06_CALCULOS_Y_EJEMPLOS.md | Cantidades, empaques, costos, pedidos y conciliación |
| 07_PRUEBAS_Y_ACEPTACION.md | Casos normales, errores, concurrencia y piloto |
| 08_GIT_COMUNICACION_Y_OPERACION.md | Control de cambios, conflictos, entrega y recuperación |
| 09_SEGUIMIENTO_Y_DECISIONES.md | Estado inicial, pendientes y plantillas de seguimiento |
| 10_PROMPTS_POR_ETAPA.md | Encargos listos para comenzar cada etapa |
| 11_AUDITORIA_BASE_EXISTENTE.md | Hallazgos comprobados y límites de las pruebas |
| 12_DICCIONARIO_BASE_EXISTENTE.md | Campos extraídos de las 59 tablas del SQL real |
| referencia/ | Copia del esquema SQL y datos ficticios de ejemplo |
| verificacion/ | Script reproducible y resultados de la revisión actual |

## Qué está confirmado
Se conservan productos base, marcas, variantes, empaques y múltiplos. Recetas usan productos base; recepciones y salidas identifican variantes. El kárdex conserva unidad, cantidad y valor. Sin lotes ni vencimientos en la primera versión. Contar inventario no ajusta stock ni genera consumo alternativo.

## Qué sigue pendiente
Método de valoración, fuente de costo previsto, impuestos/cargos, formato único de bajas y reglas finales de ajustes. Se pueden desarrollar catálogo, estructura de recetas y simulaciones mientras se resuelven. No usar simulaciones como contabilidad real.

## Evidencia y limitaciones
Se revisó el esquema SQLite existente de 59 tablas. Se repitieron nueve comprobaciones en memoria y tres diagnósticos adicionales detectaron brechas de protección. Se incluyen los resultados exactos. No hay evidencia de pruebas de aplicación, sincronización o piloto completo. Ningún prompt garantiza ausencia total de errores: este paquete exige controles y pruebas para detectarlos antes de operar.

## Orden de autoridad
Instrucción reciente del usuario > decisiones de negocio confirmadas > alcance consolidado de este paquete > documentos anteriores > ejemplos y propuestas técnicas. El esquema existente es evidencia de implementación, no autoridad para cambiar una regla de negocio. Las páginas citadas del manual original no se verificaron contra ese manual.
