# GLM — Segunda Pasada Visual PlayPredict (v2)

## 1. Resumen ejecutivo

Se completó la segunda pasada visual del rediseño PLAYER de PlayPredict. Mientras que la primera pasada estableció el shell visual general (dashboard, sidebar, header), esta segunda pasada extendió el lenguaje visual aprobado al **recorrido PLAYER completo**, eliminando la sensación de panel administrativo en todas las pantallas internas.

Se rediseñaron **13 páginas** y se creó **1 archivo CSS compartido** (`PlayerPages.css`). Se corrigió el bug conocido del Layout admin (usaba `<a href>` en vez de `<Link>`). Se eliminó la terminología "Panel administrativo" del recorrido PLAYER. Se simplificó la navegación de Rankings y Premios de un drill-down de 3 niveles a una smart landing con selector. Los Pronósticos pasaron de tabla administrativa a cards deportivas con TeamBadges.

Todo el trabajo es exclusivamente frontend/presentación: sin cambios de backend, endpoints, migraciones ni modelo de datos. TypeScript y build pasan sin errores.

## 2. Estado inicial encontrado

Todas las pantallas internas del recorrido PLAYER mostraban el MVP anterior dentro del nuevo layout:

- **Mis Ligas**: tabla administrativa con columnas Liga/Competencia/Alcance/Participantes/Estado
- **Detalle de Liga**: ficha con `form-card` + tabla de participantes administrativa, sin navegación interna
- **Pronósticos**: tabla `admin-table` con inputs inline, sin diferenciación visual de estados
- **Ranking**: recorrido Competencia → Edición → Fecha → Ranking (3-4 niveles de tablas admin)
- **Premios**: mismo recorrido drill-down administrativo, con breadcrumb "← Panel administrativo"
- **Explorar Competencias**: tabla `admin-table`
- **Detalle de Competencia**: `form-card` administrativo + tabla de ligas
- **Crear Liga**: formulario `form-card` con selector de alcance confuso
- **Mi Perfil**: mostraba campo "Roles = PLAYER" innecesario
- **Layout admin**: usaba `<a href>` causando full page reload (bug conocido de la primera pasada)

## 3. Archivos modificados

| # | Archivo | Tipo de cambio |
|---|---------|---------------|
| 1 | `frontend/src/pages/LeaguesMinePage.tsx` | Rediseño completo: tabla → cards |
| 2 | `frontend/src/pages/LeagueDetailPage.tsx` | Rediseño completo: ficha → workspace con tabs |
| 3 | `frontend/src/pages/PredictionsMatchesPage.tsx` | Rediseño completo: tabla → cards deportivas |
| 4 | `frontend/src/pages/RankingsCompetitionsPage.tsx` | Rediseño completo: tabla → smart landing con selector + ranking inline |
| 5 | `frontend/src/pages/RankingsEditionsPage.tsx` | Rediseño completo: tabla → cards de ediciones |
| 6 | `frontend/src/pages/RankingGeneralPage.tsx` | Rediseño: tabla admin → tabla player-friendly con highlighting |
| 7 | `frontend/src/pages/RankingsRoundsPage.tsx` | Rediseño: tabla → cards de fechas |
| 8 | `frontend/src/pages/RankingRoundPage.tsx` | Rediseño: tabla admin → tabla player-friendly |
| 9 | `frontend/src/pages/PrizesCompetitionsPage.tsx` | Rediseño completo: tabla → smart landing con selector + prizes inline |
| 10 | `frontend/src/pages/PrizesEditionsPage.tsx` | Rediseño: tabla → cards de ediciones |
| 11 | `frontend/src/pages/PrizesListPage.tsx` | Rediseño: cards admin → cards player-friendly |
| 12 | `frontend/src/pages/ExploreCompetitionsPage.tsx` | Rediseño: tabla → cards de competencia |
| 13 | `frontend/src/pages/CompetitionDetailPage.tsx` | Rediseño: form-card → hero card + league cards |
| 14 | `frontend/src/pages/LeagueCreatePage.tsx` | Rediseño visual: scope selector visual cards |
| 15 | `frontend/src/pages/LeagueJoinPage.tsx` | Rediseño visual: pp-form con código centrado |
| 16 | `frontend/src/pages/ProfilePage.tsx` | Rediseño: avatar section, sin campo "Roles" |
| 17 | `frontend/src/components/Layout.tsx` | Fix: `<a href>` → `<Link to>` para admin nav |

## 4. Archivos nuevos creados

| # | Archivo | Función |
|---|---------|---------|
| 1 | `frontend/src/pages/PlayerPages.css` | CSS compartido para todas las páginas PLAYER: headers, cards, grids, tabs, forms, ranking, prizes, match cards, empty states, scope selector, profile, edition selector |

## 5. Cambios realizados pantalla por pantalla

### 5.1 Inicio (`/`)
Sin cambios. El dashboard actual es la referencia visual principal y se mantiene intacto.

### 5.2 Mis Ligas (`/leagues`)
- **Antes**: tabla `admin-table` con columnas Liga/Competencia/Alcance/Participantes/Estado/Abrir
- **Después**: grid de cards (`pp-league-card`) con nombre, competencia con ícono ⚽, alcance, participantes con ícono 👥, status badge (Activa/Inactiva), botón "Entrar"
- **Empty state**: icono 🏆 + texto + botones de acción (Explorar Competencias / Unirse por código)
- **Acciones**: "Explorar Competencias" (primary) y "Unirse por código" (secondary) en el header

### 5.3 Detalle de Liga (`/leagues/:id`)
- **Antes**: ficha `form-card` con label/value + tabla `admin-table` de participantes
- **Después**: workspace con:
  - **Header de workspace** (`pp-workspace__header`): nombre, descripción, meta (competencia, alcance, participantes, estado), código de invitación si es creador
  - **Tabs** (`pp-tabs`): Resumen / Pronósticos / Ranking (Próximamente) / Premios (Próximamente) / Participantes
  - **Tab Resumen**: info card + preview de participantes como avatares con iniciales
  - **Tab Pronósticos**: CTA "Pronosticar ahora" que navega a `/leagues/:id/matches`
  - **Tab Participantes**: grid de avatares con nombre y badge "Creador"
  - **Tabs Próximamente**: Ranking y Premios visibles con badge ComingSoonBadge

### 5.4 Pronósticos (`/leagues/:id/matches`)
- **Antes**: tabla `admin-table` con inputs inline para pronósticos
- **Después**: tres secciones diferenciadas:
  - **⚽ Pronosticá**: cards de partidos pendientes con TeamBadge, inputs de pronóstico, botón "¡Pronosticá!" o "Actualizar", border-left primario (pendiente) o verde (guardado)
  - **✅ Resultados**: cards de partidos finalizados mostrando claramente: Resultado oficial, Mi pronóstico, Puntos obtenidos (verde si >0), Motivo. Border-left gris.
  - **🔒 Cerrados**: cards compactas para partidos cerrados/cancelados. Border-left gris claro.
- Cada card usa `TeamBadge` con iniciales y color hash-based
- Inputs centrados con estilo consistente (`pp-match-card__input`)

### 5.5 Ranking (`/rankings`)
- **Antes**: recorrido Competencia → Edición → Fecha → Ranking (3-4 niveles de tablas `admin-table`, breadcrumb "← Panel administrativo")
- **Después**:
  - `/rankings`: **smart landing** con selector de competencia que auto-carga ediciones activas y muestra el ranking directamente en la misma página. Tabla player-friendly (`pp-ranking`) con posiciones coloreadas (oro/plata/bronce), highlighting del usuario actual con "(Vos)", fondo `--color-primary-light`. Link a "Ranking por Fecha" disponible si se quiere profundizar.
  - `/rankings/competitions/:id/editions`: cards de ediciones (si se llega por el link)
  - `/rankings/editions/:id`: tabla player-friendly con highlighting
  - `/rankings/editions/:id/rounds`: cards de fechas clickeables
  - `/rankings/rounds/:id`: tabla player-friendly con highlighting
  - **Eliminado**: toda referencia a "Panel administrativo", breadcrumbs con `←` usan `pp-back` con estilo player

### 5.6 Premios (`/prizes`)
- **Antes**: recorrido Competencia → Edición → Premios (3 niveles de tablas, breadcrumb "← Panel administrativo")
- **Después**:
  - `/prizes`: **smart landing** con selector de competencia que auto-carga premios de la edición activa. Muestra solo premios publicados como cards player-friendly (`pp-prize-card`) con ícono 🎁, valor, tipo, sponsor, líder actual con nombre en verde. Link a "Ver todas las ediciones" disponible si se quiere profundizar.
  - `/prizes/competitions/:id/editions`: cards de ediciones
  - `/prizes/editions/:id`: grid de prize cards player-friendly
  - **Eliminado**: toda referencia a "Panel administrativo"

### 5.7 Explorar Competencias (`/competitions/explore`)
- **Antes**: tabla `admin-table` con columnas Nombre/Deporte/Edición/Fechas/Estado/Ver
- **Después**: grid de cards (`pp-comp-card`) con ícono 🏆, nombre, edición activa con ícono 📍, deporte 🏅, fechas 📅, botón "Ver competencia"
- Se siente como elegir dónde jugar, no como consultar un ABM

### 5.8 Detalle de Competencia (`/competitions/:id`)
- **Antes**: `form-card` con label/value + tabla de ligas
- **Después**: hero card (`pp-info-card`) con nombre grande, descripción, meta (deporte, edición, fechas), CTA prominente "+ Crear nueva Liga". Mis Ligas como cards reutilizando `pp-league-card`. Empty state con ícono 🏆 y CTA "Crear Liga"

### 5.9 Crear Liga (`/leagues/new`)
- **Antes**: formulario `form-card` con `<select>` para alcance
- **Después**: formulario `pp-form` con:
  - **Scope selector visual** (`pp-scope-selector`): dos cards clickeables — "🏆 Toda la Competencia" vs "📅 Rango de Fechas" con descripción clara de cada uno
  - Hint: "Podés elegir la misma fecha como inicial y final para crear una Liga de una sola fecha"
  - Toda la lógica y validación idéntica al formulario anterior

### 5.10 Mi Perfil (`/profile`)
- **Antes**: `form-card` con campos Nombre/Apellido/Email/Roles
- **Después**: card con avatar section (iniciales con gradiente púrpura + nombre + email), formulario Nombre/Apellido, Email (deshabilitado), sin campo "Roles"
- Mejora visual moderada sin sobreinvertir

### 5.11 Unirse por código (`/leagues/join`)
- **Antes**: formulario `form-card` estándar
- **Después**: formulario `pp-form` con input de código centrado, estilo monospace bold, botón "✋ Unirme" full width

## 6. Componentes reutilizables creados o modificados

### Nuevo archivo CSS compartido
- **`PlayerPages.css`**: Sistema visual compartido para todas las páginas PLAYER con ~1000 líneas de CSS organizado por componente:
  - `pp-header` / `pp-back` — headers y links de navegación
  - `pp-btn` — botones primary/secondary
  - `pp-grid` — grid de cards responsivo
  - `pp-league-card` — cards de liga
  - `pp-comp-card` — cards de competencia
  - `pp-empty` — empty states con ícono y acciones
  - `pp-tabs` / `pp-tab` — tabs del workspace de liga
  - `pp-workspace__*` — header y meta del workspace
  - `pp-participants` / `pp-participant` — grid de participantes con avatares
  - `pp-matches` — grid de match cards
  - `pp-match-card` — cards de partido con estados (pending/saved/finished/closed)
  - `pp-ranking` — tabla de ranking player-friendly con highlighting
  - `pp-prize-card` — cards de premios
  - `pp-form` / `pp-form__*` — formularios player-friendly
  - `pp-scope-selector` / `pp-scope-option` — selector visual de alcance
  - `pp-profile__*` — perfil con avatar
  - `pp-edition-select` — selector de competencia/edición para smart landing
  - `pp-info-card` — card genérica de información

### Componentes existentes reutilizados
- `TeamBadge` — reutilizado en las cards de pronósticos
- `ComingSoonBadge` — reutilizado en tabs Próximamente del workspace de liga
- `StatusMessage` — reutilizado en todas las páginas
- `DashboardHero`, `MatchPredictionCard`, `RankingPreview`, `CurrentRoundCard`, `PrizeHighlight`, `SponsorBanner` — sin cambios (dashboard intacto)

### Componentes modificados
- `Layout.tsx` — fix: `<a href>` → `<Link to>` para navegación admin (elimina full page reload)

## 7. Funcionalidades existentes preservadas

Todas las funcionalidades preexistentes continúan funcionando sin alteraciones:

- **Login/Registro**: flujo completo con JWT, validación, redirección por rol
- **CRUD de Competencias, Ediciones, Fechas, Partidos** (ADMIN): sin cambios
- **Gestión de Experiencias** (ADMIN): sin cambios
- **Gestión de Premios** (ADMIN): sin cambios
- **Gestión de Usuarios** (ADMIN): sin cambios
- **Mis Ligas**: listar, crear, unirse por código — misma API, misma lógica
- **Detalle de Liga**: misma API, misma información
- **Pronósticos**: crear, actualizar, ver evaluación — misma API, misma lógica de validación
- **Ranking General y por Fecha**: misma API, mismos datos
- **Premios**: misma API, mismos datos
- **Explorar Competencias**: misma API, mismos datos
- **Detalle de Competencia**: misma API, misma lógica de ligas asociadas
- **Crear Liga**: misma lógica de validación, cascada Competencia→Edición→Fechas, scope FullCompetition/RoundRange
- **Perfil**: edición de nombre/apellido, misma API
- **Auth guards**: RequireAuth y RequireAdmin sin cambios
- **Dashboard**: sin cambios

## 8. Elementos visuales marcados como PRÓXIMAMENTE / EN DESARROLLO

| Elemento | Ubicación | Estado |
|----------|-----------|--------|
| Ranking (tab en Liga workspace) | `/leagues/:id` tab | Próximamente |
| Premios (tab en Liga workspace) | `/leagues/:id` tab | Próximamente |
| Fixture (header nav) | PlayerHeader | Próximamente |
| Fixture (sidebar) | PlayerSidebar | Próximamente |
| Amigos (sidebar) | PlayerSidebar | Próximamente |
| Notificaciones (header) | PlayerHeader | Próximamente |
| Notificaciones (sidebar) | PlayerSidebar | Próximamente |
| Configuración (sidebar) | PlayerSidebar | Próximamente |
| Ayuda (sidebar) | PlayerSidebar | Próximamente |
| Invitar amigos (sidebar) | PlayerSidebar | Próximamente |
| SponsorBanner sin sponsor real | Dashboard | Placeholder visual |
| Más funcionalidades en desarrollo | Dashboard | Próximamente |

## 9. Cambios funcionales inevitables

1. **Fix del bug `<a href>` → `<Link to>` en Layout admin**: era un bug conocido de la primera pasada que causaba full page reload al navegar entre secciones admin. Corregido reemplazando `<a href>` por `<Link to>` de React Router.

2. **Eliminación del campo "Roles" en Perfil PLAYER**: se ocultó el campo que mostraba "Roles = PLAYER" ya que no aporta valor al usuario PLAYER y refuerza la sensación de panel administrativo. No es un cambio de permisos ni de autenticación — el rol sigue existiendo y funcionando igual.

3. **Smart landing de Ranking**: `/rankings` ahora carga competencias y ediciones activas automáticamente, mostrando el ranking directamente sin requerir drill-down. Las rutas intermedias (`/rankings/competitions/:id/editions`, etc.) siguen existiendo y funcionando para navegación manual.

4. **Smart landing de Premios**: `/prizes` ahora carga competencias y ediciones activas automáticamente, mostrando premios publicados directamente. Las rutas intermedias siguen existiendo.

## 10. Decisiones visuales relevantes

1. **`PlayerPages.css` como CSS compartido en vez de componentes React**: se eligió un archivo CSS único con clases BEM-style para todas las páginas PLAYER, en vez de crear componentes React wrapper adicionales. Esto reduce la complejidad, evita una reestructuración arquitectónica grande, y permite que cada página use las clases que necesite sin overhead.

2. **Ranking como tabla, no como cards**: las tablas de ranking se mantienen como tablas pero con estilo player-friendly (highlighting del usuario, posiciones coloreadas, header uppercase). Convertir rankings a cards sería anti-patrón para datos tabulares largos.

3. **Pronósticos separados en tres secciones**: Pronosticá / Resultados / Cerrados. Esto permite al jugador enfocarse inmediatamente en lo que importa (los partidos pendientes) sin scroll por partidos ya finalizados.

4. **Scope selector visual en Crear Liga**: en vez de un `<select>` simple, se usan dos cards clickeables que comunican conceptualmente "Toda la Competencia" vs "Rango de Fechas" con íconos y descripción. Misma lógica, mejor jerarquía visual.

5. **Back links (`pp-back`) en vez de breadcrumbs admin**: se reemplazaron los breadcrumbs tipo "← Panel administrativo" por links de vuelta limpios ("← Mis Ligas", "← Ranking", etc.) que no exponen terminología administrativa.

6. **Tabs de Liga como workspace conceptual**: la pantalla de Detalle de Liga se convirtió en un workspace con tabs (Resumen, Pronósticos, Ranking, Premios, Participantes). Las tabs de Ranking y Premios son "Próximamente" para comunicar la visión completa del producto.

## 11. Diferencias o limitaciones respecto de la referencia visual aprobada

1. **No se pudo inspeccionar directamente la imagen de referencia** (`docs/imagenes/Playpredict_v1.png`) — la implementación sigue basándose en la descripción textual y en el dashboard aprobado como referencia.

2. **Los íconos del sidebar siguen siendo emojis** — no se instaló una librería de íconos SVG. El mockup probablemente usa íconos profesionales.

3. **No se implementó el cambio dinámico de colores según Experience** — el layout PLAYER es uniforme, sin branding dinámico.

4. **El menú de usuario del header sigue dependiendo de `:hover`** — no se implementó un dropdown clickable accesible en mobile/touch (bug conocido de la primera pasada, pendiente).

5. **Ranking de Liga no existe como endpoint** — la tab "Ranking" en el workspace de Liga aparece como Próximamente porque no hay endpoint de ranking por Liga en el backend.

6. **Premios de Liga no existen** — la tab "Premios" en el workspace de Liga aparece como Próximamente por la misma razón.

7. **No se rediseñó Login** — registrado explícitamente como pendiente (ver sección 12).

## 12. Pendientes detectados

1. **Rediseñar Login para alinearlo con la identidad visual PLAYER aprobada** — No se rediseñó en esta tarea por instrucción explícita. La página de login mantiene su estilo actual.

2. **Implementar menú de usuario clickable** — El dropdown del PlayerHeader usa `:hover`, no es accesible en mobile/touch.

3. **Reemplazar emojis por íconos SVG** en sidebar y cards — Los emojis (🏆📋📅⚽ etc.) son placeholders visuales. Una librería de íconos profesionales mejoraría la calidad visual.

4. **Ranking de Liga** — Necesita endpoint de backend para ranking filtrado por Liga.

5. **Premios de Liga** — Necesita modelo de datos y endpoints para premios asociados a Ligas.

6. **Branding dinámico por Experience** — El modelo `Experience` ya tiene `primaryColor`, `secondaryColor`, `logoUrl` en backend, pero el frontend no los usa para cambiar la apariencia dinámicamente.

7. **Validación visual manual en navegador** — Todo el trabajo fue validado con `tsc --noEmit` y `npm run build`, pero no se probó visualmente en navegador.

## 13. Tests ejecutados

No existen tests automatizados en el proyecto frontend. Se ejecutaron las validaciones estáticas:

- `npx tsc --noEmit` — 0 errores
- `npm run build` — exitoso (89 módulos, 37.32 kB CSS + 357.13 kB JS)

## 14. Resultado de validaciones

### `npm run build`
```
✓ 89 modules transformed.
dist/index.html                   0.74 kB │ gzip:  0.40 kB
dist/assets/index-vq-kq9Mt.css   37.32 kB │ gzip:  6.18 kB
dist/assets/index-DP7k1hg7.js   357.13 kB │ gzip: 95.88 kB
✓ built in 451ms
```

### `npx tsc --noEmit`
```
0 errores
```

## 15. Problemas encontrados durante la implementación

1. **`useNavigate` eliminado de RankingsCompetitionsPage y PrizesCompetitionsPage**: las versiones originales usaban `useNavigate` para el patrón de tabla clickable. Al convertir a smart landing con selector, se eliminó la navegación por click de fila, pero las versiones internas (RankingsEditionsPage, RankingsRoundsPage) aún usan `useNavigate` para la navegación por cards clickable. Esto es consistente.

2. **Sin regresión de funcionalidad**: todas las páginas existentes mantienen su API, lógica y datos. Solo cambió la presentación visual.

## 16. Estado final del repositorio

- **Rama**: `prueba-glm-ui`
- **22 archivos modificados** (17 páginas + Layout.tsx + CSS compartido + index.html + App.tsx + index.css)
- **1 archivo nuevo**: `frontend/src/pages/PlayerPages.css`
- **Sin cambios en**: backend, endpoints, migraciones, modelo de datos
- **Sin commit, sin push, sin merge**

## 17. Confirmación expresa

- **NO se realizó commit**
- **NO se realizó push**
- **NO se realizó merge**
