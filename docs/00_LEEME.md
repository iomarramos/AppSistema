# Documentación de AppSistema: índice

Toda la documentación interna del proyecto está en `docs/`. Lo que el sistema necesita en la raíz del repositorio es solo lo que las herramientas exigen: `CLAUDE.md` (prompt interno), `README.md`, los scripts de prueba y de instalación.

## Dónde está cada cosa

| Carpeta | Qué contiene | Cuándo se consulta |
|---|---|---|
| `01_PROMPTS/` | Prompts de creación: maestro (07) y por formulario (06). El prompt interno vive en `CLAUDE.md`, en la raíz | Antes de construir o cambiar una pantalla |
| `02_REQUERIMIENTOS/` | Requerimientos del manual SGP (`REQUERIMIENTOS.md`), la revisión contra el código y la base (`REVISION_REQUERIMIENTOS_2026-10-06.md`), el diagnóstico de la especificación (`DIAGNOSTICO_VENTANA.md`) y la especificación de pantallas en `ESPECIFICACION_VENTANA/` (00 a 05 y 08) | Antes de decidir qué falta |
| `03_ESTADO/` | `CHECKLIST.md` (avance y entregas, lo más reciente al final), `CONTINUAR.md` (cómo retomar y decisiones del usuario que no se reabren), `SEGUIMIENTO.md` (registro por etapa) | Al empezar una sesión y al cerrar una entrega |
| `04_ARQUITECTURA_Y_DATOS/` | Arquitectura de planificación central, base de datos (estado y relaciones), diseño de pantallas, integración de resultados, referencia SQLite | Al tocar la base o el diseño |
| `05_OPERACION/` | Piloto de la etapa 8 y flujo de ramas Git | Al desplegar o al confirmar cambios |
| `06_GUIA_DE_CONSTRUCCION/` | Guía modular de construcción, con su manifiesto SHA-256 (`MANIFIESTO_SHA256.json`) y su verificador | Al verificar integridad de la guía |
| `07_SKILLS/` | Índice de los skills de agente instalados en `.agents/skills/` | Al elegir una herramienta de diseño |
| `08_MEMORIA_DEL_ASISTENTE/` | Copia de la memoria del asistente (estado del proyecto y decisiones recordadas) | Para entender el contexto de sesiones anteriores |

Fuera de `docs/`, por la misma razón de uso:

* `datos/`: archivos del SGP y de pantallas que entrega el usuario (cada carpeta tiene `origen/` y `LEEME.md`).
* `herramientas/`: utilidades de conversión, carga y verificación.
* `artifacts/`: salidas generadas (reportes de demostración, capturas, respaldos). No se versiona.

## Reglas para agregar documentación

1. Un documento nuevo va en la carpeta que corresponde a su uso, con número si es de una serie.
2. Un documento que cambia el estado del proyecto se registra en `03_ESTADO/CHECKLIST.md` (nueva entrega al final).
3. No duplicar archivos. La versión vigente es la que está en esta carpeta; si hay una copia vieja, se borra.
4. No poner claves ni datos personales en documentos. Las claves de prueba se guardan fuera del repositorio.
5. Al mover un documento, actualizar sus referencias en `CLAUDE.md`, `README.md` y en los documentos de `03_ESTADO/`.
6. No editar migraciones de `database/postgresql/migraciones/` aunque mencionen una ruta: las migraciones aplicadas tienen hash.
