# Skills de agente

Los skills se instalan en `.agents/skills/`, que es donde la herramienta de agente los busca. No se mueven: este índice dice qué hay y para qué sirve cada uno. Son de terceros: no se editan en este repositorio.

| Skill | Para qué sirve | Relevante para AppSistema |
|---|---|---|
| `ui-ux-pro-max` | Inteligencia de diseño UI/UX para web, móvil y escritorio: accesibilidad, sistemas de diseño, revisión de interfaces | Sí: revisión de pantallas WinForms y criterios de usabilidad (RNF-06) |
| `ui-styling` | Interfaces accesibles con componentes shadcn/ui y Tailwind | Baja: el proyecto es WinForms; útil solo para la presentación web futura |
| `design-system` | Tokens de diseño en tres capas y especificaciones de componentes | Media: referencia para `Tema.vb` |
| `design` | Identidad de marca, tokens, logos, presentaciones | Baja: uso comercial |
| `brand` | Voz, identidad visual y guías de marca | Baja |
| `banner-design` | Banners para redes sociales y publicidad | No aplica |
| `slides` | Presentaciones HTML con Chart.js | Baja: demostraciones al cliente |

Regla de uso: antes de cambiar la apariencia, `Tema.vb` manda (`CLAUDE.md`, §3). Un skill sugiere; el tema decide.
