# SESSION

##2 Proyecto
PlayPredict

## Rama actual
prueba-glm-ui

## Último commit
21dac5c — feat: add Leagues as the new core concept, simplify roles to ADMIN/PLAYER

## Estado del entorno
- Git: rama `prueba-glm-ui`, basada en `main` (último commit `21dac5c`). **Working tree con cambios NO commiteados**.
- Docker: 3 servicios levantados y healthy.
  - `playpredict_db` (PostgreSQL 18) — Up, healthy
  - `playpredict_backend` — Up, healthy
  - `4predict_frontend` — Up
- URLs:
  - Frontend: http://localhost:5175
  - Backend Swagger: http://localhost:8006/swagger
  - Backend health: http://localhost:8006/api/health
- **IMPORTANTE — Docker/Windows**: Vite y `dotnet watch` NO detectan cambios hechos desde el host Windows a través del bind mount. **Siempre ejecutar `docker compose restart frontend`** (y/o `backend`) después de editar archivos. Ctrl+F5 solo refresca el navegador, no el cache de módulos de Vite.
- Credenciales ADMIN: `admin@playpredict.local` / `admin123`
- Credenciales PLAYER (demo, en Liga): `juan.perez@playpredict.local`, `ana.torres@playpredict.local`, `maria.lopez@playpredict.local`, `pedro.gomez@playpredict.local` — password: `demo123`

---

## Cambios realizados en ESTA sesión

### 1. Correcciones post-prueba manual (v4)

- **Liga tabs visibles**: CSS de `.pp-tabs` mejorado con fondo, borde, indicador activo prominente
- **Pronósticos**: botón "Guardar cambios" (era "Actualizar"), help text "Podés modificar tu pronóstico hasta el cierre del partido", feedback diferenciado "Pronóstico guardado/actualizado correctamente."
- **Rankings**: badge "(Vos)" como pill blanco sobre primary, fila bold para usuario autenticado
- **Inicio**: competencia tomada de la Liga del usuario (no global), fix currentRoundIndex
- **Crear Liga CTA**: botón "+ Crear Liga" prominente en Mis Ligas (header + empty state)
- **Explorar Competencias**: botón "+ Crear Liga" por competencia
- **Invitaciones**: estado documentado (5 — backend COMPLETO, frontend parcial)

### 2. Separar Pronósticos de Resultados + Agrupar por Fecha

- **LeagueDetailPage.tsx** reescrito: tabs ahora son Resumen | Pronósticos | Resultados | Ranking | Premios (PRÓXIMAMENTE) | Participantes
- **Pronósticos**: solo partidos pronosticables, agrupados por Fecha/Jornada de competencia, con filtro (chips ≤3 fechas, dropdown >3)
- **Resultados**: solo partidos finalizados con resultado/pronóstico/puntos/motivo, agrupados por Fecha/Jornada, con filtro
- **Eliminada** la navegación intermedia "Ver partidos y pronosticar" — el tab va directo
- **"Pronost4ar ahora"** del Resumen sigue como CTA hacia el tab Pronósticos
- **Copy-to-clipboard** del código de invitación con feedback "✓ Copiado"

### 3. Datos demo Copa Libertadores

- **DataSeeder.cs**: Copa Libertadores de 1 Fecha genérica → 5 Fechas con equipos sudamericanos reales (River Plate, Flamengo, Palmeiras, Boca Juniors, Atlético Nacional, Peñarol)
- **NO se destruyeron** datos existentes de Liga Profesional

### 4. Crear Liga — label "Torneo / Edición"

- **LeagueCreatePage.tsx**: label "Edición" → "'Torneo / Edición"
- Fecha 1→1 ya funcionaba (hint existente, selectores lo permiten)

---

## Archivos modificados en esta sesión

| Archivo | Cambio |
|---|---|
| `frontend/src/pages/LeagueDetailPage.tsx` | Tabs Pronósticos/Resultados separados, agrupación por Fecha, filtro, copy código invitación |
| `frontend/src/pages/PredictionsMatchesPage.tsx` | Botón "Guardar cambios", help text, feedback diferenciado |
| `frontend/src/pages/PlayerPages.css` | Tabs visibles, ranking badge, match-card hint/saved, round heading/chips/filter, invite section |
| `frontend/src/pages/LeaguesMinePage.tsx` |E CTA "+ Crear Liga" prominente |
| `frontend/src/pages/ExploreCompetitionsPage.tsx` | Botón "+ Crear Liga" por competencia |
| `frontend/src/pages/PlayerDashboardPage.tsx` | Competencia de la Liga del usuario, fix currentRoundIndex |
| `frontend/src/pages/RankingGeneralPage.tsx` | "(Vos)" ya existía (sin cambios) |
| `frontend/src/pages/LeagueCreatePage.tsx` | Label "Torneo / Edición" |
| `backend/Data/DataSeeder.cs` | Copa Libertadores: 5 Fechas con equipos sudamericanos |

**Informes generados** (untracked):
- `GLM_CIRCUITO_JUGABLE_PLAYPREDICT.md`
- `GLM_REDISENO_VISUAL_PLAYPREDICT_v3.md`
- `GLM_REDISENO_VISUAL_PLAYPREDICT_v4.md`

---

## Validaciones ejecutadas

| Check | Resultado |
|---|---|
| `npx tsc --noEmit` | ✅ 08 errores |
| `npx vite build` | ✅ 89 módulos, ~550ms |
| `dotnet build --no-restore` | ✅ 0 errores, 1 warning (NU1510) |
| League ranking API (`GET /api/rankings/leagues/1`) | ✅ 4 posiciones con scoring correcto |
| League matches API (`GET /api/leagues/1/matches`) | ✅ 15 partidos, 9 finished + 6 canPredict |

---

## Cambios NO commiteados

**Todo el working tree está sin commitear.** Incluye:
- Rediseño visual PLAYER (todas las sesiones previas)
- Circuito jugable mínimo (RankingService, RankingEndpoints, DataSeeder)
- Correcciones post-prueba manual (v4)
- Separación Pronósticos/Resultados + agrupación por Fecha
- Copa Libertadores 5 Fechas
- Informes GLM (untracked)

---

## Pendiente

1. **"Agregar participante registrado"** (búsqueda deD usuarios + agregar por creador): requiere endpoint nuevo de búsqueda de usuarios + endpoint de agregar participante por creador. Backend de invitaciones/unirse ya funciona completo. Dejado pendiente por instrucción explícita del usuario.
2. **Datos demo Copa Libertadores no visibles hasta DB reset**: DataSeeder es idempotente por nombre — si la competencia ya existe con 1 Fecha, no la recrea. Necesita `docker compose down -v && docker compose up -d` para ver las 5 Fechas nuevas.
3. **Login Page**: rediseño visual pendiente (documentado desde v2).
4. **Tab Premios**: sigue PRÓXIMAMENTE (sin backend de premios publicados).
(5. **Escudos reales de clubes**: explícitamente fuera de scope (tarea posterior separada).

---

## Instrucciones para retomar

1. `docker compose ps` — verificar que los 3 servicios están healthy
2. Si se editó código del host: `docker compose restart frontend` (y/o `backend`) **siempre** — Vite no detecta cambios por inotify en Docker/Windows
3. Para ver datos demo de Copa Libertadores actualizados: `docker compose down -v && docker compose up -d` (resetea la DB)
4. Login como PLAYER: `juan.perez@playpredict.local` / `demo123`
5. Verificar: Mis Ligas → Liga → tabs (Resumen/Pronósticos/Resultados/Ranking/Participantes) → Pronósticos agrupados por Fecha → Resultados agrupados por Fecha
6. **NO hacer commit/push/merge** hasta aprobación explícita
