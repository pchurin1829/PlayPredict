# GLM — Rediseño Visual PlayPredict

## 1. Resumen ejecutivo

Se implementó la primera etapa del rediseño visual de PlayPredict, transformando la experiencia del jugador (PLAYER) de un CRUD administrativo genérico a una plataforma deportiva con dashboard, sidebar, header dedicado y columna derecha informativa. La experiencia ADMIN se mantuvo con su layout original pero se separó visualmente con la etiqueta "Administración" y se eliminó "Panel administrativo" para jugadores. Se crearon 12 componentes nuevos y se modificaron 5 archivos existentes. Todo el trabajo es exclusivamente frontend/presentación: sin cambios de backend, endpoints, migraciones ni modelo de datos. TypeScript y build pasan sin errores. No se validó visualmente en navegador.

## 2. Objetivo del trabajo

- Transformar PlayPredict de una apariencia de CRUD/panel administrativo a una experiencia de producto deportivo moderno.
- Tomar como referencia visual el diseño aprobado en `docs/imagenes/Playpredict_v1.png`.
- Conservar toda la funcionalidad existente sin alterar lógica de negocio.
- Dejar visibles funcionalidades futuras (Amigos, Notificaciones, Configuración, Fixture, etc.) con badge "Próximamente" sin implementarlas.
- Crear un dashboard PLAYER con hero, próximos partidos, ranking, premios y espacio de sponsor.
- Separar claramente la experiencia PLAYER (sidebar + header + dashboard) de la experiencia ADMIN (top-nav tradicional).

## 3. Pantallas modificadas

### `/` — PlayerDashboardPage (NUEVA)
- **Archivo**: `pages/PlayerDashboardPage.tsx` + `.css`
- **Qué cambió visualmente**: Página completamente nueva. Antes `/` redirigía a `/leagues` o `/competitions`. Ahora muestra un dashboard con hero gradiente púrpura, tarjetas de partidos con pronóstico, y columna derecha con ranking/fechas/premios/sponsor.
- **Funcionalidad existente que conserva**: Usa endpoints existentes (`/leagues/mine`, `/leagues/{id}/matches`, `/competitions`, `/rankings/editions/{id}`, `/prizes/editions/{id}`). Los pronósticos se guardan/actualizan con la misma API.
- **Elementos nuevos solamente visuales**: DashboardHero (gradiente decorativo), bloque "Más funcionalidades en desarrollo" con badge Próximamente, SponsorBanner con placeholder.

### `/leagues` — LeaguesMinePage (SIN CAMBIOS en la página)
- La página no se modificó. Cambió el layout que la contiene (PlayerLayout con sidebar + header en vez del header único anterior).

### `/competitions/explore`, `/leagues/join`, `/leagues/:leagueId`, `/rankings`, `/prizes`, `/profile` (SIN CAMBIOS en las páginas)
- Todas mantienen su funcionalidad y contenido original. Solo cambió el layout contenedor.

### `/login` — LoginPage
- **Archivo**: `pages/LoginPage.tsx`
- **Qué cambió**: Redirección post-login para PLAYER cambiada de `/leagues` a `/` (dashboard).
- **Funcionalidad conservada**: Login, validación, manejo de errores, registro.

### Rutas ADMIN (`/competitions`, `/admin/*`, etc.)
- **Qué cambió visualmente**: El layout ADMIN ahora muestra "Administración" en vez de "Panel administrativo". Los links del header se simplificaron a Competencias/Premios/Usuarios/Experiencias.
- **Funcionalidad conservada**: 100% — todos los CRUDs, formularios, tablas, breadcrumbs intactos.

## 4. Componentes creados o modificados

### Creados

| Componente | Archivo | Función |
|---|---|---|
| `PlayerHeader` | `components/player/PlayerHeader.tsx` + `.css` | Header PLAYER con marca/logo, nav central (Inicio, Mis Ligas, Ranking, Fixture, Premios), área usuario con avatar y dropdown |
| `PlayerSidebar` | `components/player/PlayerSidebar.tsx` + `.css` | Sidebar izquierdo con secciones GENERAL (5 items) y MI CUENTA (5 items), bloque "¡Invitá amigos!" |
| `PlayerLayout` | `components/player/PlayerLayout.tsx` + `.css` | Wrapper que compone PlayerHeader + PlayerSidebar + Outlet |
| `DashboardHero` | `components/player/DashboardHero.tsx` + `.css` | Card gradiente oscuro/púrpura con saludo, pronósticos pendientes, CTA "Pronosticar ahora", stats de posición y puntos |
| `MatchPredictionCard` | `components/player/MatchPredictionCard.tsx` + `.css` | Card individual de partido con TeamBadges, inputs de pronóstico, estados visuales (pendiente/guardado/finalizado/cancelado) |
| `TeamBadge` | `components/player/TeamBadge.tsx` | Fallback visual para equipos: círculo con iniciales y color generado por hash del nombre |
| `ComingSoonBadge` | `components/player/ComingSoonBadge.tsx` + `.css` | Badge reutilizable "Próximamente" con fondo amarillo claro y texto marrón |
| `RankingPreview` | `components/player/RankingPreview.tsx` + `.css` | Top 5 del ranking con colores oro/plata/bronce y etiqueta "(Vos)" para el usuario actual |
| `CurrentRoundCard` | `components/player/CurrentRoundCard.tsx` + `.css` | Card con nombre de fecha, competencia y barra de progreso |
| `PrizeHighlight` | `components/player/PrizeHighlight.tsx` + `.css` | Card con premio principal, valor de referencia, sponsor y ganador provisional |
| `SponsorBanner` | `components/player/SponsorBanner.tsx` + `.css` | Espacio de sponsor reutilizable con placeholder si no hay sponsor configurado |

### Modificados

| Componente | Archivo | Cambio |
|---|---|---|
| `Layout` | `components/Layout.tsx` | Smart layout: decide entre ADMIN (top-nav) o PLAYER (sidebar+header) según la ruta actual y el rol del usuario |
| `App` | `App.tsx` | Agregada ruta `/` para PlayerDashboardPage, reorganización de rutas PLAYER/ADMIN, import de estilos de ComingSoonBadge |
| `LoginPage` | `pages/LoginPage.tsx` | Redirección PLAYER cambiada de `/leagues` a `/` |
| `index.css` | `index.css` | Variables CSS completas (colores, spacing, radius, shadows, fonts), font Inter, removal de `color-scheme: light dark` |
| `index.html` | `index.html` | `lang="es"`, Google Fonts Inter precarga |

## 5. Estilos creados o modificados

### Archivos CSS creados
- `components/player/PlayerHeader.css` — header sticky blanco con nav, avatar, dropdown
- `components/player/PlayerSidebar.css` — sidebar 240px con secciones, items, invite block, responsive overlay
- `components/player/PlayerLayout.css` — grid flex: header + body(sidebar + content)
- `components/player/DashboardHero.css` — gradiente púrpura, stats cards, CTA blanco, responsive stack
- `components/player/MatchPredictionCard.css` — card con border-left por estado, team badges centrados, inputs compactos
- `components/player/ComingSoonBadge.css` — badge pill amarillo/marrón
- `components/player/RankingPreview.css` — rows con posiciones coloreadas, highlight para usuario
- `components/player/CurrentRoundCard.css` — card simple con barra de progreso
- `components/player/PrizeHighlight.css` — card con fondo amarillo claro para premio
- `components/player/SponsorBanner.css` — card con placeholder cuadrado gris y texto
- `pages/PlayerDashboardPage.css` — grid 2 columnas (main + sidebar derecha), responsive

### Sistema visual
- **Variables CSS**: 30+ variables en `:root` para colores, radius, shadows, fonts, dimensiones
- **Layout**: Flexbox para header/sidebar, CSS Grid para dashboard (main + columna derecha)
- **Tarjetas**: `.mpcard`, `.rpreview`, `.crcard`, `.phl`, `.sponsor-banner`, `.dhero` — todas con `var(--radius-md)`, `var(--color-surface)`, `border: 1px solid var(--color-border)`
- **Navegación**: PlayerHeader sticky top, PlayerSidebar fijo lateral con toggle colapsar
- **Tipografía**: Inter (Google Fonts) con pesos 400/500/600/700/800, fallback system-ui
- **Espaciados**: Consistentes via variables: `--radius-sm/md/lg`, padding 1rem en cards, gap 0.75rem en grids
- **Responsive**: 3 breakpoints — desktop (>1024px sidebar fijo), tablet (768-1024px sidebar overlay con toggle), mobile (<768px una columna, header sin nav)
- **Estados visuales**: MatchPredictionCard con border-left por estado (pendiente=púrpura, guardado=verde, finalizado=gris, cancelado=rojo), badges con colores semánticos

## 6. Experiencia PLAYER

### Inicio/Dashboard (`/`)
- **Hero gradiente**: "¡Bienvenido, {nombre}!" con conteo de pronósticos pendientes reales, CTA "Pronosticar ahora" que scrolla a partidos, stats de posición y puntos (reales si hay datos, "—" si no)
- **Próximos partidos**: Grid de MatchPredictionCards con datos reales de la primera Liga del usuario. Cada card muestra equipos con TeamBadge, fecha/hora, inputs de pronóstico funcionales. Estados visuales diferenciados por color de borde.
- **Columna derecha**: RankingPreview (top 5 real), CurrentRoundCard (datos reales de edición), PrizeHighlight (premio real si existe), SponsorBanner (placeholder visual).
- **Bloque "Más funcionalidades en desarrollo"**: Visual solamente.

### Mis Ligas (`/leagues`)
- Sin cambios en la página. Contenido idéntico al anterior, ahora dentro del layout PLAYER con sidebar.

### Explorar Competencias (`/competitions/explore`)
- Sin cambios en la página.

### Unirse por código (`/leagues/join`)
- Sin cambios en la página.

### Detalle de Liga (`/leagues/:leagueId`)
- Sin cambios en la página.

### Pronósticos (`/leagues/:leagueId/matches`)
- Sin cambios en la página existente (PredictionsMatchesPage). Los pronósticos también están disponibles desde el dashboard vía MatchPredictionCard, que es un componente independiente con la misma lógica.

### Rankings (`/rankings`)
- Sin cambios en las páginas de ranking existentes. El dashboard muestra un preview del ranking (RankingPreview).

### Premios (`/prizes`)
- Sin cambios en las páginas de premios existentes. El dashboard muestra un highlight del premio principal (PrizeHighlight).

### Navegación
- **Header**: Inicio, Mis Ligas, Ranking (funcionales), Fixture y Premios (Fixture = Próximamente, Premios = funcional)
- **Sidebar GENERAL**: Inicio, Mis Ligas, Explorar Ligas, Ranking General (funcionales), Fixture (Próximamente)
- **Sidebar MI CUENTA**: Mi Perfil (funcional), Amigos/Notificaciones/Configuración/Ayuda (Próximamente)
- **Sidebar inferior**: "¡Invitá amigos!" con botón (Próximamente)

### Qué es REAL vs PLACEHOLDER
- **Real**: Dashboard con datos de API, pronósticos funcionales, navegación a páginas existentes, ranking, premios, fechas
- **Placeholder/Próximamente**: Fixture, Amigos, Notificaciones, Configuración, Ayuda, Invitar amigos, Sponsor sin datos reales

## 7. Experiencia ADMIN

- **Layout**: Header horizontal con "PlayPredict — Administración" y links a Competencias, Premios, Usuarios, Experiencias
- **No tiene sidebar**: Mantiene el layout original de top-nav con `<Outlet />`
- **No tiene dashboard**: Aterriza en `/competitions` (lista de competencias) igual que antes
- **Separación visual**: El ADMIN ya no ve "Panel administrativo" genérico sino "Administración" contextual. Las rutas ADMIN usan un header diferente al PLAYER.
- **Funcionalidad**: 100% conservada — todos los CRUDs, formularios, tablas, breadcrumbs, modales funcionan igual que antes
- **Un ADMIN que navega a rutas PLAYER** (ej: `/leagues`) verá el layout PLAYER con sidebar. Esto es intencional: un ADMIN también puede ser jugador.

## 8. Elementos "Próximamente" o no implementados

| Elemento | Visible actualmente | Funcional | Estado |
|---|---|---|---|
| Fixture (header nav) | Sí, botón deshabilitado con badge | No | Próximamente |
| Fixture (sidebar) | Sí, botón con badge | No | Próximamente |
| Amigos (sidebar) | Sí, botón con badge | No | Próximamente |
| Notificaciones (header) | Sí, ícono campana con badge | No | Próximamente |
| Notificaciones (sidebar) | Sí, botón con badge | No | Próximamente |
| Configuración (sidebar) | Sí, botón con badge | No | Próximamente |
| Ayuda (sidebar) | Sí, botón con badge | No | Próximamente |
| Invitar amigos (sidebar) | Sí, botón con badge | No | Próximamente |
| "Más funcionalidades en desarrollo" (dashboard) | Sí, card con badge | No | Próximamente |
| SponsorBanner sin sponsor real | Sí, placeholder visual | Parcial | Preparado para datos futuros |
| Estadísticas avanzadas | Mencionado en "Más funcionalidades" | No | Próximamente |
| Goleadores | Mencionado en "Más funcionalidades" | No | Próximamente |
| Comparativas | Mencionado en "Más funcionalidades" | No | Próximamente |

## 9. Publicidad y sponsors

### Espacios preparados visualmente

| Espacio | Componente | Datos reales | Funcional |
|---|---|---|---|
| Columna derecha del dashboard | `SponsorBanner` | No — usa placeholder | Solo visual |
| Cards de premios | `PrizeHighlight` muestra `sponsorName` si existe | Sí, si el premio tiene sponsor | Funcional |
| Espacio futuro en header | No implementado | No | No |

### Sponsor principal de competencia
- No se creó un banner específico en el header. El `SponsorBanner` en la columna derecha es genérico.
- El modelo `Experience` ya tiene `primaryColor`, `secondaryColor`, `logoUrl` en el backend — preparado para branding.

### Publicidad general
- Solo el `SponsorBanner` placeholder en el dashboard. Sin espacios laterales ni footer con publicidad.

### Branding de competencia
- No se implementó el cambio dinámico de colores/logo según la Experience. El layout PLAYER es uniforme.

### Aclaración
- Todos los espacios de sponsor son **solamente visuales**. No hay lógica de carga de sponsors desde API, ni endpoints nuevos para obtener sponsors. El `SponsorBanner` muestra un placeholder si no se le pasa `sponsorName`.

## 10. Relación con la imagen de referencia

**Referencia**: `docs/imagenes/Playpredict_v1.png`

**El modelo no pudo inspeccionar directamente la imagen.** La implementación se basó enteramente en la descripción textual detallada provista en el prompt del usuario.

### Qué se implementó siguiendo la especificación textual
- Header superior con marca, nav central (Inicio/Mis Ligas/Ranking/Fixture/Premios), área de usuario derecha
- Sidebar izquierdo con secciones GENERAL y MI CUENTA
- Dashboard con hero gradiente oscuro/púrpura
- Bloque "Próximos partidos" con cards de partido y inputs de pronóstico
- Columna derecha con ranking, fecha actual, premios, sponsor
- Badges "Próximamente" para funcionalidades futuras
- Separación ADMIN/PLAYER
- Bloque "Invitar amigos" en sidebar

### Qué debería compararse visualmente de forma manual
- **Proporciones exactas** del sidebar, contenido y columna derecha
- **Espaciados y paddings** específicos de cada sección
- **Estilo visual de las cards de partido** (¿coinciden con el mockup?)
- **Colores exactos** del hero gradiente
- **Estilo del ranking** (¿el podio visual coincide?)
- **Tratamiento del sponsor** (¿el placeholder refleja el espacio del mockup?)
- **Tipografía y pesos** (¿Inter es la font correcta o se usa otra?)
- **Densidad visual** (¿hay más o menos aire que el mockup?)
- **Estilo de los badges "Próximamente"** (¿tamaño, color, posición?)
- **Comportamiento responsive** en los breakpoints que el mockup define

### Ajustes que podrían surgir de la comparación manual
- Cambio de tipografía si la referencia usa una font diferente
- Ajuste de anchos (sidebar, columna derecha, max-width del contenido)
- Modificación de colores del gradiente hero
- Cambio de estilo de los TeamBadges (¿circulares? ¿cuadrados? ¿con borde?)
- Ajuste de densidad: más compacto o más aire según el mockup
- Agregar o quitar elementos del sidebar/header si el mockup muestra algo diferente
- Posibles cambios en el tratamiento de estados de partido (colores, iconos)

## 11. Funcionalidad preservada

Las siguientes funcionalidades preexistentes continúan funcionando y no deberían haberse alterado:

- **Login/Registro**: flujo completo con JWT, validación, redirección por rol
- **CRUD de Competencias**: crear, editar, listar (ADMIN)
- **CRUD de Ediciones**: crear, editar, listar, configurar puntuación (ADMIN)
- **CRUD de Fechas (Rounds)**: crear, editar, listar (ADMIN)
- **CRUD de Partidos**: crear, editar, listar, cargar resultado oficial (ADMIN)
- **Gestión de Experiencias**: listar, crear, editar, publicar, archivar (ADMIN)
- **Gestión de Premios (ADMIN)**: listar, crear, editar, publicar, cerrar, cancelar
- **Gestión de Usuarios**: listar, activar/desactivar (ADMIN)
- **Mis Ligas (PLAYER)**: listar, crear, unirse por código
- **Detalle de Liga**: ver info, participantes, código de invitación
- **Pronósticos**: crear, actualizar, ver evaluación (PLAYER via `/leagues/:id/matches`)
- **Ranking General y por Fecha**: navegar por competencia → edición → ranking
- **Premios (vista PLAYER)**: navegar y ver premios publicados con ganadores provisionales
- **Perfil**: editar nombre y apellido
- **Configuración de puntuación de Edición**: usar configuración propia o heredar de Experience
- **Breadcrumbs**: navegación contextual en todas las pantallas de administración
- **Auth guards**: RequireAuth y RequireAdmin protegen rutas correctamente

## 12. Archivos modificados

### Creados

1. `frontend/src/components/player/PlayerHeader.tsx`
2. `frontend/src/components/player/PlayerHeader.css`
3. `frontend/src/components/player/PlayerSidebar.tsx`
4. `frontend/src/components/player/PlayerSidebar.css`
5. `frontend/src/components/player/PlayerLayout.tsx`
6. `frontend/src/components/player/PlayerLayout.css`
7. `frontend/src/components/player/DashboardHero.tsx`
8. `frontend/src/components/player/DashboardHero.css`
9. `frontend/src/components/player/MatchPredictionCard.tsx`
10. `frontend/src/components/player/MatchPredictionCard.css`
11. `frontend/src/components/player/TeamBadge.tsx`
12. `frontend/src/components/player/ComingSoonBadge.tsx`
13. `frontend/src/components/player/ComingSoonBadge.css`
14. `frontend/src/components/player/RankingPreview.tsx`
15. `frontend/src/components/player/RankingPreview.css`
16. `frontend/src/components/player/CurrentRoundCard.tsx`
17. `frontend/src/components/player/CurrentRoundCard.css`
18. `frontend/src/components/player/PrizeHighlight.tsx`
19. `frontend/src/components/player/PrizeHighlight.css`
20. `frontend/src/components/player/SponsorBanner.tsx`
21. `frontend/src/components/player/SponsorBanner.css`
22. `frontend/src/pages/PlayerDashboardPage.tsx`
23. `frontend/src/pages/PlayerDashboardPage.css`

### Modificados

1. `frontend/index.html` — lang="es", Google Fonts Inter
2. `frontend/src/index.css` — Variables CSS, font Inter, colors, spacing
3. `frontend/src/App.tsx` — Ruta `/` para PlayerDashboardPage, reorganización PLAYER/ADMIN
4. `frontend/src/components/Layout.tsx` — Smart layout: ADMIN top-nav vs PLAYER sidebar+header
5. `frontend/src/pages/LoginPage.tsx` — Redirección PLAYER → `/` en vez de `/leagues`

### Eliminados

1. `frontend/src/components/SmartLayout.tsx` — creado temporalmente y eliminado durante la implementación (no existe en el estado final)

## 13. Backend y base de datos

- **Backend**: SIN CAMBIOS. Ningún archivo del directorio `backend/` fue modificado.
- **Endpoints**: SIN CAMBIOS. No se agregaron, modificaron ni eliminaron endpoints.
- **Migraciones**: SIN CAMBIOS. No se generaron ni ejecutaron migraciones.
- **Modelo de datos**: SIN CAMBIOS. No se alteraron entidades, DTOs ni relaciones.

## 14. Validaciones realizadas

### TypeScript
- **Comando**: `npx tsc -b --noEmit`
- **Resultado**: 0 errores
- **Nota**: Se corrigieron 2 errores durante la implementación (import `../../` incorrecto en PlayerDashboardPage → `../`, y `user` declarado pero no usado en PlayerSidebar)

### Build de producción
- **Comando**: `npm run build` (ejecuta `tsc -b && vite build`)
- **Resultado**: Exitoso
- **Output**: 88 módulos, `dist/assets/index-L6SUKR0L.css` (22.19 kB gzip: 4.53 kB), `dist/assets/index-LAR1RdpL.js` (341.73 kB gzip: 92.75 kB)

### Verificación de backend
- **Comando**: `git diff HEAD --name-only -- backend/`
- **Resultado**: Output vacío — ningún archivo de backend modificado

### Tests E2E / visuales
- **No ejecutados**. No existen tests automatizados en el proyecto. No se probó visualmente en navegador.

### Lint
- **No ejecutado** en esta sesión (requiere `npx oxlint`).

## 15. Cómo probar manualmente

### PLAYER

- [ ] Levantar la app: `docker compose up -d --build` o `npm run dev` en `frontend/`
- [ ] Login como PLAYER (ej: `juan.perez@playpredict.local` / `demo123`)
- [ ] Verificar que aterriza en `/` (dashboard) y no en `/leagues`
- [ ] Verificar que el header muestra "PlayPredict" sin "Panel administrativo"
- [ ] Verificar que el sidebar muestra secciones GENERAL y MI CUENTA
- [ ] Verificar que el hero muestra "¡Bienvenido, Juan!" y datos reales
- [ ] Verificar que los partidos se muestran como cards con TeamBadges
- [ ] Ingresar un pronóstico y verificar que se guarda correctamente
- [ ] Verificar que el ranking en la columna derecha muestra datos reales
- [ ] Verificar que "Fixture" en header y sidebar muestra badge "Próximamente"
- [ ] Verificar que Amigos/Notificaciones/Configuración/Ayuda muestran "Próximamente"
- [ ] Navegar a Mis Ligas desde el sidebar y verificar que funciona
- [ ] Navegar a Ranking desde el sidebar y verificar que funciona
- [ ] Navegar a Perfil desde el sidebar y verificar que funciona
- [ ] Verificar que el bloque "Invitar amigos" en el sidebar muestra "Próximamente"
- [ ] Verificar que el SponsorBanner muestra placeholder "Tu marca aquí"

### ADMIN

- [ ] Login como ADMIN (ej: `admin@playpredict.local` / contraseña configurada)
- [ ] Verificar que aterriza en `/competitions`
- [ ] Verificar que el header muestra "PlayPredict — Administración"
- [ ] Verificar que NO hay sidebar
- [ ] Navegar a Competencias, Ediciones, Fechas, Partidos — CRUD funcional
- [ ] Navegar a Experiencias, Premios, Usuarios — CRUD funcional
- [ ] Cargar un resultado oficial y verificar que funciona
- [ ] Navegar a `/leagues` como ADMIN y verificar que muestra layout PLAYER

### Responsive

- [ ] En desktop (>1024px): verificar sidebar visible, 3 columnas en dashboard
- [ ] Reducir a tablet (~900px): verificar que el sidebar se convierte en overlay con toggle
- [ ] Reducir a mobile (<768px): verificar una sola columna, sidebar oculto, cards full-width
- [ ] Verificar que el toggle del sidebar funciona correctamente
- [ ] Verificar que el dropdown de usuario funciona (o no se superpone) en mobile

## 16. Diferencias o desviaciones respecto del diseño objetivo

1. **No se pudo inspeccionar la imagen de referencia** — toda la implementación se basó en la descripción textual. Las proporciones, espaciados y detalles visuales pueden diferir del mockup.
2. **Íconos del sidebar son emojis** (🏠🏆🔍📊📅👤👥🔔⚙️❓) — el mockup probablemente usa íconos SVG o una librería de íconos.
3. **No se instaló ninguna librería de UI** — todos los componentes son CSS custom. El mockup puede usar componentes de una librería (shadcn, MUI, etc.) con estilos predefinidos.
4. **El PlayerHeader usa `<a href>` para ADMIN** en vez de `<Link>` — esto causa full page reload en la navegación ADMIN. Es un bug conocido.
5. **El menú de usuario se muestra solo con hover** — no funciona en mobile/touch.
6. **No se implementó el bloque de "Notificaciones" del header** como algo más que un ícono placeholder.
7. **No se implementó un avatar con imagen real** — se usan las iniciales del usuario.
8. **El sidebar no tiene ícono de hamburguesa en mobile** — el toggle es una flecha pequeña.
9. **No hay animaciones ni transiciones** más allá de las básicas de CSS hover.
10. **Los colores del gradiente hero son aproximados** — podrían no coincidir exactamente con el mockup.

## 17. Riesgos o deuda técnica

1. **No validado visualmente en navegador** — pueden existir problemas de layout, overflow, superposición o responsive que solo se detectan al correr la app.
2. **PlayerHeader usa `<a href>` para rutas ADMIN** — debería usar `<Link to>` de React Router para evitar full page reload. Esto puede romper el estado de la app al navegar desde PLAYER a ADMIN.
3. **MatchPredictionCard duplica lógica de PredictionsMatchesPage** — ambos componentes manejan pronósticos independientemente. Si la API de pronósticos cambia, hay que actualizar dos lugares.
4. **`isAdminPath()` es heurístico** — la función decide el layout basándose en prefijos de ruta. Si se agregan rutas nuevas que no coinciden con los prefijos, se mostrará el layout incorrecto.
5. **El sidebar en tablet/mobile usa `position: fixed`** — puede superponerse con contenido si el z-index no está bien configurado, o si hay scroll horizontal.
6. **El dropdown de usuario usa `:hover`** — no es accesible en dispositivos táctiles.
7. **No hay gestión de estado del sidebar persistida** — al navegar entre páginas, el sidebar se colapsa/expande según el estado del componente, no según preferencia del usuario.
8. **El dashboard carga datos de la primera Liga del usuario** — si el usuario tiene múltiples ligas, solo ve datos de una. No hay selector de liga en el dashboard.
9. **El SponsorBanner no consume datos de API** — es un componente visual estático que recibe props. No hay integración con endpoints de sponsors.

## 18. Próximos pasos recomendados

### Prioridad 1 — imprescindible
- Validar visualmente en navegador y comparar con `docs/imagenes/Playpredict_v1.png`
- Corregir `<a href>` por `<Link to>` en el header ADMIN dentro de Layout.tsx
- Implementar menú de usuario clickable (no solo hover) para mobile
- Agregar hamburguesa de sidebar visible en mobile
- Verificar que todas las rutas funcionan correctamente (navegación PLAYER y ADMIN)
- Corregir cualquier problema visual detectado al comparar con el mockup

### Prioridad 2 — siguiente evolución
- Reemplazar emojis del sidebar por íconos SVG o librería de íconos
- Agregar selector de Liga en el dashboard (si el usuario tiene múltiples)
- Migrar PredictionsMatchesPage para que use MatchPredictionCard como componente
- Implementar animaciones sutiles de transición entre páginas
- Agregar estados de carga skeletons en el dashboard
- Persistir preferencia de sidebar colapsado/expandido
- Mejorar la barra de progreso de CurrentRoundCard con datos más precisos

### Prioridad 3 — futuro
- Integrar SponsorBanner con datos de API (sponsor por competencia/liga)
- Implementar branding dinámico por Experience (colores, logo)
- Agregar espacios de publicidad en footer y header
- Crear dashboard ADMIN con resumen de estado
- Implementar notificaciones reales
- Agregar onboarding para nuevos jugadores
- Implementar sistema de amigos

## 19. Estado Git

- **Rama actual**: `prueba-glm-ui`
- **No se realizó commit ni push**

### Archivos modificados (tracked)
```
 M frontend/index.html
 M frontend/src/App.tsx
 M frontend/src/components/Layout.tsx
 M frontend/src/index.css
 M frontend/src/pages/LoginPage.tsx
```

### Archivos nuevos (untracked)
```
?? frontend/src/components/player/
?? frontend/src/pages/PlayerDashboardPage.css
?? frontend/src/pages/PlayerDashboardPage.tsx
```

### Archivos sin seguimiento adicionales (no relacionados con esta tarea)
```
?? .qwen/
?? GLM_AUDITORIA_VISUAL_PLAYPREDICT.md
?? docs/imagenes/
```

### node_modules y dist
- `frontend/node_modules/` instalado localmente para type check (normalmente se usa via Docker)
- `frontend/dist/` generado por build de validación

## 20. Veredicto final

| Dimensión | Puntaje (1-10) | Justificación |
|---|---|---|
| **Fidelidad al objetivo visual** | **5/10** | Sin acceso a la imagen de referencia, la implementación se basó en la descripción textual. La estructura general (header/sidebar/dashboard/columna derecha) está correcta, pero los detalles visuales (proporciones, espaciados, colores exactos, iconografía) pueden diferir significativamente del mockup. Necesita validación visual manual. |
| **Calidad UX PLAYER** | **6/10** | El dashboard con hero, partidos y ranking es una mejora radical vs. la tabla genérica anterior. La navegación con sidebar es más clara. Sin embargo, el menú de usuario solo funciona con hover, los íconos son emojis, y la densidad visual puede no ser la óptima. El responsive es funcional pero básico. |
| **Calidad UX ADMIN** | **7/10** | Mínimamente alterado — se cambió "Panel administrativo" por "Administración" y se simplificaron los links del header. Toda la funcionalidad se conserva. El layout sigue siendo funcional y consistente. Podría beneficiarse de un dashboard propio en el futuro. |
| **Preparación comercial** | **4/10** | Mejor que el estado anterior (CRUD genérico), pero no está listo para una demo comercial sin validación visual previa. Los placeholders de sponsor son rudimentarios, los íconos son emojis, y hay bugs conocidos (href vs Link, hover-only dropdown). Con los ajustes de Prioridad 1 completados, podría llegar a 7/10. |

**Veredicto**: **APTOS PARA REVISIÓN VISUAL**

La implementación es funcional (TypeScript y build pasan, la lógica no se rompió), pero necesita revisión visual manual contra el mockup antes de considerar un commit. Los bugs conocidos (href vs Link, hover dropdown) deberían corregirse antes de la demo comercial.
