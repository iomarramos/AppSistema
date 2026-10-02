# Flujo de ramas

| Rama | Propósito | Quién escribe |
|---|---|---|
| `main` | Versión estable, solo lo aceptado y probado | Merge desde `develop` mediante PR revisado |
| `develop` | Integración: estado verificado más reciente | Merge de ramas de etapa/tarea tras pasar `./ejecutar_pruebas.sh` |
| `feature/etapa-1-fundamentos-catalogo` | Usuarios, permisos, auditoría, catálogo, proveedores, importación | Etapa 1 |
| `feature/etapa-2-menus-recetas` | Servicios, estructuras, recetas versionadas, minutas, costeo | Etapa 2 |
| `feature/etapa-3-prevision-compras` | Previsión, asignación a variantes, pedidos | Etapa 3 |
| `feature/etapa-4-almacen-kardex` | Recepciones, salidas, devoluciones, bajas, traspasos, kárdex | Etapa 4 |
| `feature/etapa-5-produccion` | Requerimientos, entregas, raciones, mermas | Etapa 5 |
| `feature/etapa-6-inventarios` | Corte, conteo, diferencias, ajustes autorizados | Etapa 6 |
| `feature/etapa-7-cierres-reportes` | Cierre diario/mensual, Food Cost, reportes | Etapa 7 |
| `feature/etapa-8-continuidad-piloto` | Servidor de sede, sincronización, respaldo, piloto | Etapa 8 |
| `feature/etapa-9-extensiones` | Contratos, accesos avanzados, integraciones | Etapa 9 |
| `claude/*` | Ramas de trabajo de sesiones del asistente | El asistente; se integran por PR |

## Reglas

1. No se trabaja directamente sobre `main` ni `develop`.
2. Cada rama de etapa nace de `develop` actualizado; las tareas pequeñas pueden crear sub-ramas (`feature/etapa-1-importador`).
3. Antes de integrar: `./ejecutar_pruebas.sh` sin fallos, migraciones nuevas numeradas (`V005__...`) y `docs/SEGUIMIENTO.md` actualizado.
4. Nunca editar una migración ya integrada en `develop`; se corrige con una nueva.
5. Conflictos: leer ambos cambios y su intención; no elegir "ours/theirs" a ciegas; repetir pruebas (guía, doc. 08).
6. Sin secretos ni bases reales en Git.

## Rama por defecto

Se recomienda configurar `main` como rama por defecto en GitHub (*Settings → General → Default branch*). La primera rama publicada fue la de la sesión del asistente, por lo que GitHub pudo haberla tomado como predeterminada.
