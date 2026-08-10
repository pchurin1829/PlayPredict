# INFORME DE AUDITORÍA VISUAL Y UX — PlayPredict

Fecha: 7 de agosto de 2026
Metodología: Análisis de código fuente (lectura completa de todos los archivos frontend)
Nota: No se tuvo acceso visual a la aplicación corriendo en navegador. Todo lo identificado se basa en inspección del código fuente.

---

## A. Arquitectura encontrada

**COMPROBADO EN CÓDIGO**

| Aspecto | Detalle |
|---|---|
| Framework | React 19 + Vite 8 + TypeScript 6 |
| Routing | react-router-dom v7 (client-side SPA) |
| Estado global | Context API (AuthContext) — sin Redux/Zustand |
| Estilos | CSS puro plano (3 archivos CSS, sin preprocesador, sin design system, sin librería de UI) |
| Dependencias UI | **NINGUNA** — 0 librerías de componentes. Solo React, React DOM y React Router |
| HTTP client | Fetch nativo wrapper (api/client.ts) |
| Layout | 1 solo Layout compartido ADMIN+PLAYER (Layout.tsx) — header horizontal con nav inline |
| Componentes compartidos | 3: Layout, StatusMessage, MatchResultModal |
| Páginas | 32 archivos .tsx en pages/ |
| Icons | 1 sprite SVG (icons.svg) con íconos de redes sociales — **NO se usa en la aplicación** |
| Favicon | SVG con rayo estilizado púrpura/azul |
| Auth | JWT con localStorage, redirección por rol al login |

**Estructura de archivos CSS:**
- `index.css`: 12 líneas (reset mínimo, font system-ui, fondo #f4f5f7)
- `Layout.css`: 57 líneas (header, nav, content)
- `admin.css`: ~350 líneas (TODO el sistema visual: botones, tablas, badges, formularios, cards, modales, tabs, pronósticos, auth, premios)

**Sin biblioteca de componentes externa.** Todo es CSS custom escrito a mano, clase por clase.

---

## B. Estado visual actual

**COMPROBADO EN CÓDIGO (no visualmente en navegador — no se tuvo acceso al browser)**

### Header / Navegación
- Header oscuro (#1a1a2e) con texto blanco
- Layout horizontal: brand "PlayPredict" + subtítulo "Panel administrativo" + nav links + botón Salir
- Todos los links de navegación en una sola fila, sin jerarquía visual
- El subtítulo "Panel administrativo" es **visible para TODOS los usuarios**, incluidos PLAYERs
- No hay sidebar, no hay menú hamburguesa, no hay iconos
- La nav crece horizontalmente — con 9+ links para ADMIN se desborda en móvil

### Contenido principal
- Max-width 960px centrado
- Padding 1.5rem
- Fondo general #f4f5f7 (gris muy claro)

### Tablas (admin-table)
- Único patrón de presentación de datos para TODO: competencias, ediciones, fechas, partidos, usuarios, ligas, rankings, experiencias, premios
- Estilo genérico: cabecera gris clara, bordes sutiles, filas hover
- No hay cards, no hay grids visuales, no hay dashboards

### Formularios (form-card)
- Card blanca con sombra mínima, max-width 520px
- Labels en negrita, inputs con borde gris estándar
- Sin validación visual avanzada (solo texto de error en rojo debajo)

### Botones
- 2 variantes: btn-primary (#4a4ad1 púrpura) y btn-secondary (blanco con borde gris)
- Sin variantes de tamaño, sin iconos, sin estados de hover avanzados

### Badges
- Colores por estado: activo=verde, inactivo=gris, draft=gris, cancelado=rojo, en curso=amarillo
- Funcionales pero básicos

### Pronósticos
- Inputs de 3.5rem para goles local/visitante con guión separador
- Inline en la celda de la tabla — presentación muy compacta y técnica
- El botón "Guardar pronóstico" / "Actualizar pronóstico" es texto completo dentro de la fila

### Ranking
- Tabla plana: posición, nombre, puntos, exactos, correctos, incorrectos, pronósticos
- Sin medallas, sin colores de posición, sin destacados para top 3

### Premios (vista Player)
- Única pantalla con cards (prize-cards): grid responsive, la presentación más trabajada
- Tiene sponsor, tipo, ganador provisional

---

## C. Problemas principales

### CRÍTICOS

1. **El subtítulo "Panel administrativo" es visible para TODOS los usuarios (PLAYER incluidos)**
   - INFERIDO (comprobado en Layout.tsx línea 19: `<span className="layout__subtitle">Panel administrativo</span>` — sin condicional de rol)
   - Un jugador que entra a jugar un prode ve "Panel administrativo" en el header
   - **Impacto**: Transmite que el producto es una herramienta interna, no un producto de consumo

2. **No existe dashboard PLAYER**
   - COMPROBADO EN CÓDIGO: el PLAYER aterriza en `/leagues` (LeaguesMinePage) que es una tabla genérica
   - No hay resumen de: próximos partidos, pronósticos pendientes, posición en rankings, puntos, actividad reciente
   - **Impacto**: El jugador no tiene una razón emocional para volver; no hay "enganche"

3. **Experiencia PLAYER y ADMIN comparten el mismo layout visual sin diferenciación**
   - COMPROBADO EN CÓDIGO: Layout.tsx es único, sin variantes por rol
   - Un jugador ve la misma estética "panel de gestión" que un administrador
   - **Impacto**: No transmite diversión, competencia ni expectativa deportiva

### ALTOS

4. **La pantalla de pronósticos es una tabla administrativa, no una experiencia de juego**
   - COMPROBADO EN CÓDIGO: PredictionsMatchesPage usa `admin-table` con inputs inline
   - "Boca vs River" aparece como texto plano en una celda, sin escudos, sin colores, sin emoción
   - El botón dice "Guardar pronóstico" / "Actualizar pronóstico" — lenguaje de formulario, no de juego
   - **Impacto**: La pantalla más importante del producto (donde el jugador pronostica) parece un CRUD interno

5. **Ranking sin personalidad competitiva**
   - COMPROBADO EN CÓDIGO: RankingGeneralPage y RankingRoundPage son tablas idénticas sin destacados
   - No hay podio visual, no hay colores para posiciones, no hay animación, no hay diferenciación entre 1° y último
   - **Impacto**: No genera deseo de competir ni de subir posiciones

6. **Navegación no escala — links inline sin agrupación ni iconos**
   - COMPROBADO EN CÓDIGO: Layout.tsx renderiza 9+ links en una sola fila `<nav>`
   - Sin iconos, sin agrupación, sin sección PLAYER vs ADMIN
   - ADMIN ve: Mis Ligas, Explorar, Unirse, Rankings, Fixture, Premios, Usuarios, Admin Premios, Experiencias, Perfil, Salir
   - **Impacto**: Confusión de roles, difícil de escanear, se rompe en móvil

7. **Login sin branding deportivo**
   - COMPROBADO EN CÓDIGO: LoginPage es una form-card blanca genérica sobre fondo gris
   - No hay logo grande, no hay imagen deportiva, no hay mensaje emocional
   - **Impacto**: Primera impresión plana, no genera confianza de producto profesional

### MEDIOS

8. **Los estados vacíos son solo texto**
   - COMPROBADO EN CÓDIGO: todos usan `<div className="empty-state">` con texto plano
   - No hay ilustraciones, no hay CTAs visuales destacados (excepto en LeaguesMinePage que tiene botones)
   - **Impacto**: Percepción de producto incompleto

9. **Los colores del favicon (púrpura/azul #863bff) no coinciden con el color primario de la app (#4a4ad1)**
   - COMPROBADO EN CÓDIGO: favicon.svg usa #863bff/#47bfff; admin.css usa #4a4ad1
   - **Impacto**: Inconsistencia de marca

10. **No hay footer**
    - COMPROBADO EN CÓDIGO: Layout.tsx no tiene footer
    - **Impacto**: Sin espacio para copyright, links legales, ni futuros sponsors

11. **La pantalla "Explorar Competencias" hace N+1 requests al backend**
    - COMPROBADO EN CÓDIGO: ExploreCompetitionsPage hace 1 GET de competencias + N GETs de ediciones + M GETs de rounds
    - **Impacto**: Performance; puede ser lento con muchas competencias

12. **No hay favicon.png para navegadores que no soportan SVG**
    - COMPROBADO EN CÓDIGO: solo existe favicon.svg en public/
    - **Impacto**: Favicon roto en algunos navegadores

### BAJOS

13. **`<html lang="en">` debería ser `"es"`**
    - COMPROBADO EN CÓDIGO: index.html línea 2
    - **Impacto**: Accesibilidad y SEO para hispanohablantes

14. **`color-scheme: light dark` en index.css pero la app no tiene tema dark**
    - COMPROBADO EN CÓDIGO: index.css línea 2
    - **Impacto**: Scrollbars y controles nativos pueden verse inconsistentes en sistemas con dark mode

15. **icons.svg con íconos de Bluesky/Discord/GitHub/X que no se usan en la app**
    - COMPROBADO EN CÓDIGO: icons.svg tiene símbolos que no se referencian en ningún .tsx
    - **Impacto**: Peso innecesario

---

## D. Experiencia ADMIN actual

**COMPROBADO EN CÓDIGO**

### Pantallas que ve el ADMIN:
- `/competitions` — Lista de competencias (tabla)
- `/competitions/new` y `/competitions/:id/edit` — Alta/editar competencia (form)
- `/competitions/:id/editions` — Ediciones (tabla)
- `/editions/:id/edit` — Editar edición (form)
- `/editions/:id/scoring-configuration` — Configurar puntuación (form con tabs)
- `/editions/:id/rounds` — Fechas (tabla)
- `/rounds/:id/edit` — Editar fecha (form)
- `/rounds/:id/matches` — Partidos (tabla + modal resultado)
- `/matches/:id/edit` — Editar partido (form)
- `/admin/users` — Usuarios (tabla con activar/desactivar)
- `/admin/prizes` — Premios admin (tabla)
- `/admin/prizes/new` y `/admin/prizes/:id/edit` — Alta/editar premio (form)
- `/admin/experiences` — Experiencias (tabla)
- `/admin/experiences/new` y `/admin/experiences/:id/edit` — Alta/editar experiencia (form con tabs)
- `/rankings` — Rankings (navegación por Competencia → Edición → Fecha/General)
- `/prizes` — Premios player (cards)
- `/leagues` — Mis Ligas (tabla)
- `/profile` — Perfil (form)
- Todo lo de PLAYER también

### Evaluación ADMIN:
- **Funcional**: el ADMIN puede hacer todo lo que necesita (CRUD completo de competencias, ediciones, fechas, partidos, resultados, premios, experiencias, usuarios)
- **Claridad**: la navegación por breadcrumbs es consistente, el flujo Competencia → Edición → Fecha → Partido es lógico
- **Problema principal**: parece un **panel de gestión genérico**, no un producto deportivo. Un ADMIN que es cliente (empresa que contrata PlayPredict) vería una herramienta interna, no un producto que puede vender
- **No hay dashboard**: el ADMIN aterriza en la lista de competencias, sin resumen del estado general

---

## E. Experiencia PLAYER actual

**COMPROBADO EN CÓDIGO**

### Pantallas que ve el PLAYER:
- `/leagues` — Mis Ligas (tabla con "Explorar Competencias" y "Unirse por código")
- `/competitions/explore` — Explorar Competencias (tabla de competencias activas)
- `/competitions/:id` — Detalle de Competencia (info + Mis Ligas + Crear Liga)
- `/leagues/new` — Crear Liga (form con cascada)
- `/leagues/join` — Unirse por código (form simple)
- `/leagues/:id` — Detalle de Liga (info + participantes)
- `/leagues/:leagueId/matches` — Pronósticos (tabla con inputs inline)
- `/rankings` — Rankings (navegación Competencia → Edición → Ranking)
- `/profile` — Perfil (form)
- `/login` y `/register` — Auth

### Lo que un PLAYER NO tiene:
- Dashboard con resumen personal
- Indicador de próximos partidos a pronosticar
- Notificación de pronósticos pendientes
- Vista de su posición en la liga sin entrar al ranking general
- Historial de pronósticos propios
- Estadísticas personales
- Sensación de progreso o logro

### Evaluación PLAYER:
- **Funcional**: puede crear ligas, unirse, pronosticar, ver rankings — el core funciona
- **Problema principal**: **no transmite emoción deportiva**. La experiencia es la de completar formularios, no la de jugar un prode con amigos. Las pantallas clave (pronósticos, rankings) usan el mismo estilo que el panel de administración de usuarios
- **Prioridad al entrar**: debería ver inmediatamente "¿Qué tengo que pronosticar hoy?" y "¿Cómo voy en mis ligas?". Hoy ve una tabla genérica de ligas

---

## F. Propuesta visual recomendada

### Principio rector
**No rehacer la aplicación. Cambiar la "piel" y la jerarquía sin tocar la lógica.**

### Estilo general
- **Deportivo, no corporativo**: mantener el púrpura como color de marca pero con presencia de acentos vivos (verde para éxito, dorado para rankings, azul para links)
- **Card-based para PLAYER, table-based para ADMIN**: el patrón prize-cards ya existe y funciona — replicar ese enfoque para ligas, partidos y rankings del jugador
- **Tipografía con personalidad**: agregar una font-display para títulos (ej: Inter, Poppins o Manrope vía Google Fonts — sin instalación, solo @import)

### Navegación
- **PLAYER**: sidebar lateral colapsable o menú hamburguesa en móvil, con iconos + texto
- **ADMIN**: mantener header pero con agrupación visual (separador o sección "Admin")
- Eliminar "Panel administrativo" para PLAYER
- Agregar avatar/nombre de usuario con menú desplegable (Perfil + Salir)

### Dashboard PLAYER (nueva página `/` para PLAYER)
- **Tarjetas resumen**: Ligas activas, Pronósticos pendientes, Mejor posición en ranking
- **Próximos partidos a pronosticar**: cards de partidos con inputs inline
- **Tu ranking**: mini-tabla con top 5 de cada liga + posición del usuario destacada
- **Actividad reciente**: "Juan se unió a tu Liga X", "Se cargó el resultado de Boca vs River"
- **CTA principal**: botón prominente "Pronosticar ahora" si hay partidos pendientes

### Presentación de partidos/pronósticos
- **Cards en vez de tabla**: cada partido como una card con equipo local [input] vs [input] equipo visitante
- **Escudos/placeholders**: aunque no haya escudos reales, usar círculos con iniciales (B vs R)
- **Estados visuales**: pendiente (borde púrpura), pronosticado (borde verde), cerrado (gris), resultado (con puntos)
- **Lenguaje de juego**: "Tu pronóstico" en vez de "Mi pronóstico", "¡Guardado!" en vez de "Pronóstico guardado correctamente."

### Ranking
- **Podio para top 3**: visual con colores oro/plata/bronce
- **Destacar posición del usuario**: fila con fondo púrpura claro
- **Barra de puntos**: mini-barra proporcional para dar sentido visual a la distancia entre posiciones

### Dashboard ADMIN
- Tarjetas resumen: Competencias activas, Ediciones en curso, Partidos sin resultado, Usuarios registrados
- Acciones rápidas: "Cargar resultado" directo desde el dashboard

### Colores propuestos
- Primario: #4a4ad1 (existente, consistente con la marca)
- Acento éxito: #22c55e (verde)
- Acento ranking: #f59e0b (dorado)
- Fondo PLAYER: gradiente sutil púrpura oscuro → gris
- Fondo ADMIN: mantener #f4f5f7 actual
- Header PLAYER: gradiente con logo más prominente

### Quick wins (sin cambiar arquitectura)
1. Eliminar "Panel administrativo" para PLAYER (1 línea en Layout.tsx)
2. Cambiar `<html lang="en">` a `"es"` (1 línea en index.html)
3. Agregar font-display para títulos (1 @import en index.css)
4. Agregar avatar con menú desplegable en vez de link "Perfil" + botón "Salir" separados
5. Página de login: agregar logo grande y subtítulo deportivo
6. Ranking: colores para top 3 posiciones (solo CSS)
7. Pronósticos: cambiar labels a lenguaje de juego ("¡Pronosticá!" en vez de "Guardar pronóstico")
8. Mis Ligas: reemplazar tabla por cards (reutilizar prize-cards como base)
9. Agregar footer mínimo
10. Favicon: alinear color con primario de la app

---

## G. Publicidad / sponsors

### Ubicaciones propuestas

| Ubicación | Visibilidad | Intrusión | Utilidad comercial | Riesgo UX | Tipo recomendado |
|---|---|---|---|---|---|
| Header (derecha del brand) | Alta | Baja | Media | Bajo | Sponsor principal de competencia |
| Dashboard PLAYER (banner entre secciones) | Alta | Media | Alta | Medio | Sponsor principal |
| Encima del ranking | Alta | Media | Alta | Medio | Sponsor de liga |
| Card de partido (borde/footer) | Alta | Baja | Alta | Bajo | Sponsor de fecha |
| Card de premio | Alta | Baja | Alta | Bajo | Sponsor de premio (ya existe campo `sponsorName`) |
| Sidebar derecho (si se agrega) | Media | Media | Media | Medio | Publicidad general |
| Footer | Baja | Baja | Baja | Bajo | Publicidad general |

### Tipos diferenciados

**A. Sponsor principal de competencia**
- Banner en header cuando hay competencia activa
- Logo + nombre en dashboard PLAYER
- Ya existe `Experience.primaryColor`/`secondaryColor` — extender con `sponsorLogoUrl`/`sponsorName`
- Riesgo bajo: es branding esperado en productos deportivos

**B. Sponsors secundarios**
- En cards de premios (ya existe `sponsorName`/`imageUrl` en el modelo)
- En footer de cards de partidos
- Riesgo bajo: ya hay soporte parcial en el modelo

**C. Publicidad general de plataforma**
- Banner en footer
- Espacio lateral derecho en desktop
- Riesgo medio: puede molestar si es intrusivo. Mantener siempre contextual y no animado

### Preparación técnica actual
- COMPROBADO EN CÓDIGO: el modelo `Prize` ya tiene `sponsorName` e `imageUrl`
- COMPROBADO EN CÓDIGO: el modelo `Experience` ya tiene `primaryColor`, `secondaryColor`, `logoUrl`
- El backend ya está parcialmente preparado para sponsors. Faltan campos en Competición y en Liga

---

## H. Archivos que sería necesario modificar

### Layout / navegación (RIESGO BAJO)
- `frontend/src/components/Layout.tsx` — reestructurar header, agregar sidebar/variantes por rol, eliminar "Panel administrativo" para PLAYER
- `frontend/src/components/Layout.css` — estilos del nuevo layout
- `frontend/src/App.tsx` — nueva ruta dashboard PLAYER, reorganización de rutas si hace falta

### Componentes compartidos (RIESGO BAJO-MEDIO)
- `frontend/src/components/admin.css` — refactor principal de estilos (es el archivo más tocado)
- Nuevo: `frontend/src/components/DashboardCard.tsx` — card resumen reutilizable
- Nuevo: `frontend/src/components/MatchCard.tsx` — card de partido para PLAYER
- Nuevo: `frontend/src/components/RankingTable.tsx` — ranking con podio visual
- Nuevo: `frontend/src/components/SponsorBanner.tsx` — banner de sponsor

### Estilos (RIESGO BAJO)
- `frontend/src/index.css` — font-display, variables CSS, reset mejorado
- `frontend/index.html` — lang="es", meta tags, preload font

### Páginas PLAYER (RIESGO MEDIO)
- `frontend/src/pages/LeaguesMinePage.tsx` — cards en vez de tabla
- `frontend/src/pages/PredictionsMatchesPage.tsx` — **rediseño principal**: cards de partido con inputs
- `frontend/src/pages/RankingGeneralPage.tsx` — podio + destacado usuario
- `frontend/src/pages/RankingRoundPage.tsx` — idem
- `frontend/src/pages/ExploreCompetitionsPage.tsx` — cards en vez de tabla
- `frontend/src/pages/CompetitionDetailPage.tsx` — mejorar presentación
- `frontend/src/pages/LeagueDetailPage.tsx` — agregar mini-ranking
- `frontend/src/pages/LoginPage.tsx` — branding deportivo
- `frontend/src/pages/RegisterPage.tsx` — idem
- Nuevo: `frontend/src/pages/PlayerDashboardPage.tsx` — dashboard del jugador

### Páginas ADMIN (RIESGO BAJO — cambios menores)
- La mayoría de páginas ADMIN pueden quedar casi igual (tablas administrativas son apropiadas para ADMIN)
- `frontend/src/pages/CompetitionsListPage.tsx` — posible dashboard ADMIN
- Mejoras menores: breadcrumbs consistentes, estados vacíos con ilustraciones

### Otros (RIESGO BAJO)
- `frontend/public/favicon.svg` — alinear colores con marca
- Eliminar `frontend/public/icons.svg` si no se usa
- Agregar `frontend/public/favicon.png` como fallback

---

## I. Riesgos técnicos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Romper flujos existentes al cambiar Layout | Media | Alto | Hacer cambios incrementales, no reescribir Layout de una vez |
| PredictionsMatchesPage es la página más compleja — rediseño puede introducir bugs | Media | Alto | Rediseñar solo la presentación, NO tocar la lógica de state/efectos |
| Agregar componentes nuevos puede crear inconsistencia con estilos existentes | Baja | Medio | Usar variables CSS compartidas, eliminar estilos duplicados |
| Cambiar admin.css masivamente puede romper pantallas que no se prueban | Media | Medio | Cambios incrementales por sección, probar cada pantalla después de cada cambio |
| Sin tests visuales/E2E — regresiones no detectadas automáticamente | Alta | Medio | Testing manual exhaustivo por cada cambio; considerar agregar Playwright después de la demo |
| No tener la app corriendo en navegador para validar visualmente | Alta | Alto (para la demo) | Levantar Docker antes de la demo y hacer un recorrido completo |

---

## J. Orden de implementación recomendado

### Etapa 0: Fundamentos (1-2 horas) — RIESGO BAJO
1. Cambiar `lang="en"` a `"es"` en index.html
2. Eliminar `color-scheme: light dark` de index.css (o implementar dark mode real)
3. Agregar font-display (Inter o Poppins) vía @import en index.css
4. Unificar color primario (elegir entre #4a4ad1 y #863bff, aplicar consistentemente)
5. Actualizar favicon para coincidir con el color elegido
6. Agregar footer mínimo a Layout.tsx

### Etapa 1: Header y navegación (2-3 horas) — RIESGO BAJO-MEDIO
1. Condicional: ocultar "Panel administrativo" para PLAYER
2. Reorganizar nav: agrupar items PLAYER vs ADMIN con separador visual
3. Mover "Perfil" y "Salir" a un dropdown de avatar
4. Agregar iconos a los links de navegación (emoji o SVG inline — sin librería)
5. Mobile: hacer el nav responsive (hamburguesa o scroll horizontal)

### Etapa 2: Login / Registro (1-2 horas) — RIESGO BAJO
1. Agregar logo grande y subtítulo deportivo al login
2. Fondo con gradiente o imagen deportiva sutil
3. Mejorar tipografía del título
4. Agregar mensaje emocional ("Pronosticá. Competí. Ganá.")

### Etapa 3: Dashboard PLAYER (3-4 horas) — RIESGO MEDIO
1. Crear PlayerDashboardPage.tsx
2. Redirigir `/` a dashboard en vez de a `/leagues`
3. Tarjetas resumen: Ligas activas, Pronósticos pendientes, Mejor posición
4. CTA "Pronosticá ahora" si hay partidos pendientes
5. Reutilizar endpoints existentes — NO crear endpoints nuevos

### Etapa 4: Mis Ligas como cards (1-2 horas) — RIESGO BAJO
1. Reemplazar tabla en LeaguesMinePage por grid de cards (copiar patrón prize-cards)
2. Cada card: nombre de liga, competencia, participantes, estado, botón "Abrir"

### Etapa 5: Pronósticos rediseñados (3-4 horas) — RIESGO MEDIO-ALTO
1. Crear MatchCard.tsx — componente de card de partido
2. Rediseñar PredictionsMatchesPage usando cards en vez de tabla
3. Cada card: equipos con iniciales en círculos, inputs de pronóstico prominentes
4. Cambiar labels a lenguaje de juego
5. **NO tocar lógica de state, efectos ni API** — solo JSX + CSS

### Etapa 6: Ranking con personalidad (1-2 horas) — RIESGO BAJO
1. Podio visual para top 3 (oro/plata/bronce)
2. Fila destacada para posición del usuario actual
3. Mini-barras de puntos
4. Aplicar a RankingGeneralPage y RankingRoundPage

### Etapa 7: Detalles finales (1-2 horas) — RIESGO BAJO
1. Estados vacíos con iconos/ilustraciones (pueden ser SVGs inline simples)
2. Mejorar CompetitionDetailPage y LeagueDetailPage
3. Agregar espacios preparados para sponsors (slots vacíos con comments)
4. Recorrido completo de regresión manual

---

## K. Quick wins (5-10 cambios de muy bajo riesgo, mejora inmediata)

1. **Eliminar "Panel administrativo" para PLAYER** — 1 línea en Layout.tsx: `{isAdmin && <span className="layout__subtitle">Panel administrativo</span>}`

2. **Cambiar `<html lang="en">` a `"es"`** — 1 línea en index.html

3. **Agregar @import de Google Fonts (Inter)** — 1 línea en index.css, usar `font-family: 'Inter', system-ui, ...` en `:root`

4. **Ranking: agregar colores a top 3** — ~10 líneas CSS en admin.css: `.admin-table tr:nth-child(1) td:first-child { color: #d4a017; font-weight: 800; }` etc.

5. **Pronósticos: cambiar labels** — en PredictionsMatchesPage.tsx: "¡Pronosticá!" en vez de "Guardar pronóstico", "¡Actualizado!" en vez de "Pronóstico guardado correctamente."

6. **Login: agregar logo y subtítulo** — en LoginPage.tsx: agregar `<img src="/favicon.svg" alt="PlayPredict" style={{width:64,margin:'0 auto 1rem',display:'block'}} />` y subtítulo "Pronosticá. Competí. Ganá."

7. **Unificar color primario** — en Layout.css cambiar `#1a1a2e` del header para que use el mismo púrpura (#4a4ad1) que los botones, o viceversa. Unificar marca.

8. **Mis Ligas: agregar badge de participantes** — en LeaguesMinePage.tsx: mostrar número de participantes con un badge visual estilo "👥 5" en vez de texto plano

9. **Agregar footer mínimo** — en Layout.tsx: `<footer style={{textAlign:'center',padding:'1rem',color:'#999',fontSize:'0.8rem'}}>© 2026 PlayPredict</footer>`

10. **Estados vacíos: agregar emoji** — en todas las páginas con empty-state: "No participás en ninguna Liga todavía 🏟️" en vez de texto plano

---

## L. Veredicto

| Aspecto | Puntuación (1-10) | Justificación |
|---|---|---|
| **Calidad visual actual** | **3/10** | Funcional pero sin personalidad. Parece un CRUD interno, no un producto deportivo. No hay jerarquía visual diferenciada, todo es tablas genéricas con el mismo estilo. La única excepción es la vista de premios con cards. |
| **Claridad UX ADMIN** | **6/10** | Funcional y consistente. Los breadcrumbs ayudan, los formularios son claros, el flujo Competencia→Edición→Fecha→Partido es lógico. Le falta un dashboard de resumen y la estética es demasiado genérica para ser un producto vendible a clientes. |
| **Claridad UX PLAYER** | **3/10** | El jugador puede hacer lo esencial, pero la experiencia no lo engancha. No hay dashboard, no hay indicadores de acción pendiente, la pantalla de pronósticos es una tabla, el ranking no genera competencia. La navegación mezcla items de admin. |
| **Preparación comercial** | **2/10** | No está listo para mostrarse como producto comercial. Un demo a clientes mostraría una herramienta interna, no un producto deportivo atractivo. Faltan: branding, emoción, dashboard, diferenciación de roles, espacios de sponsor. |
| **Potencial después del rediseño propuesto** | **8/10** | La base funcional es sólida (8 sprints de desarrollo limpio, arquitectura 9/10 según el hardening). Con los cambios propuestos (que son mayormente CSS + reorganización de JSX sin tocar lógica), PlayPredict puede pasar de "CRUD interno" a "producto deportivo atractivo" en ~15-20 horas de trabajo. El backend está bien preparado para sponsors y el modelo de Experience ya soporta customización por color/logo. |

**Resumen**: La aplicación tiene una base técnica excelente pero una presentación visual que no hace justicia a la funcionalidad. Los cambios más urgentes son: (1) eliminar "Panel administrativo" para jugadores, (2) crear un dashboard PLAYER mínimo, (3) rediseñar la pantalla de pronósticos de tabla a cards, y (4) dar personalidad competitiva al ranking. Todo esto se puede lograr sin tocar la lógica de negocio ni el backend.
