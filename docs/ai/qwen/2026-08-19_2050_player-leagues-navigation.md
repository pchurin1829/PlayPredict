# Informe: Simplificación navegación PLAYER — Mis Ligas / Explorar Competencias

**Fecha:** 2026-08-19  
**Branch:** `prueba-glm-ui`  
**Agente:** Qwen Code  
**Sesión:** Continuación (Session 2)  

---

## Objetivo

Simplificar el circuito de navegación PLAYER para las secciones Mis Ligas y Explorar Competencias, eliminando pasos intermedios, reduciendo botones redundantes de Crear Liga, e integrando las acciones "Participar en Liga Oficial" y "Dejar de participar" directamente en las páginas.

---

## Cambios realizados

### 1. Mis Ligas (`LeaguesMinePage.tsx`) — REESCRITA

- **Eliminado** botón global "+ Crear Liga" del header (solo queda "Unirme con código").
- **Eliminado** botón "Crear Liga" del empty state (solo "Explorar Competencias" + "Unirme con código").
- **Agregada** sección "🏆 Ligas Oficiales disponibles" — filtra oficiales donde `!isParticipant`, evitando duplicación con Mis Ligas.
- **Agregado** botón "Dejar de participar" en tarjetas de Ligas Oficiales dentro de "Mis Ligas" (solo si `leagueType === 'Official'`).
- **Agregada** función `handleLeaveLeague()` con `confirm()` y manejo de `fieldErrors.league` del backend.
- **Agregado** estado `leavingId` para feedback visual del botón.
- **Agregada** clase `.pp-league-card__actions-row` para contener múltiples botones en el footer.
- Empty state actualizado: "Explorá competencias para participar en una Liga Oficial de PlayPredict o creá tu propia Liga con amigos."

### 2. Backend: `DELETE /api/leagues/{id}/leave` — NUEVO ENDPOINT

Archivo: `backend/Endpoints/LeagueEndpoints.cs`

Reglas conservadoras implementadas:
- ❌ No participás → error "No participás en esta Liga."
- ❌ Creador de Liga de Amigos → error "El creador de una Liga de Amigos no puede abandonarla."
- ❌ Tiene pronósticos → error "No podés abandonar esta Liga porque ya tenés pronósticos registrados. Los resultados y el ranking quedarían inconsistentes."
- ✅ Participante sin pronósticos en Liga Oficial → elimina `LeagueParticipant`, retorna `{ message: "Dejaste la Liga correctamente." }`

### 3. API Client (`frontend/src/api/client.ts`)

- **Agregado** método `api.del()` para requests DELETE.

### 4. Explorar Competencias (`ExploreCompetitionsPage.tsx`) — REESCRITA

- **Eliminado** botón "Ver competencia" (navegación intermedia a CompetitionDetailPage eliminada del circuito PLAYER).
- **Agregado** fetch de `/leagues/officials` en paralelo con `/competitions` (usando `Promise.allSettled`).
- **Agregado** agrupamiento de Ligas Oficiales por `competitionId` en cada tarjeta de competencia.
- **Agregado** botón contextual por competencia:
  - Si ya participa en oficial → "🏆 Ir a mi Liga" (link a `/leagues/{id}`).
  - Si NO participa y hay oficial disponible → "🏆 Participar en Liga Oficial" (button, llama `handleJoinOfficial`).
  - Si no hay oficiales → no muestra botón de oficial.
- **Renombrado** "+ Crear Liga" → "+ Crear Liga con amigos" (link a `/leagues/new?competitionId=X`).
- **Actualizado** subtítulo: "Elegí una competencia para participar en una Liga Oficial de PlayPredict o crear tu propia Liga con amigos."
- **Agregada** función `handleJoinOfficial()` con refresh de datos tras unirse.

### 5. PlayerSidebar (`PlayerSidebar.tsx`)

- **Cambiado** label "Explorar Ligas" → "Explorar Competencias".

### 6. CSS (`PlayerPages.css`)

- **Agregada** clase `.pp-league-card__actions-row` con `display: flex; gap: 0.5rem; flex-wrap: wrap;`.
- Clase `.pp-btn--sm` ya existía en el archivo.

---

## Verificación funcional (Tests A–F)

### Test A: `GET /api/leagues/mine`
- **Usuario:** ana.torres@playpredict.local (PLAYER, tiene pronósticos)
- **Resultado:** ✅ Devuelve Liga General demo con `isParticipant: true`, `leagueType: "Official"`

### Test B: `GET /api/leagues/officials`
- **Usuario:** ana.torres@playpredict.local
- **Resultado:** ✅ Devuelve Liga Oficial con `isParticipant: true` (correctamente filtrada como "ya participando")

### Test C: `DELETE /api/leagues/1/leave` — Bloqueo por pronósticos
- **Usuario:** ana.torres@playpredict.local (tiene pronósticos en Liga 1)
- **Resultado:** ✅ HTTP 400 — `"No podés abandonar esta Liga porque ya tenés pronósticos registrados."`

### Test D: Join + Leave de usuario sin pronósticos
- **Usuario:** test.leave@playpredict.local (nuevo, sin pronósticos)
- **Resultado:** ✅ Join → `isParticipant: true`; Leave → `"Dejaste la Liga correctamente."`

### Test E: `GET /api/competitions`
- **Resultado:** ✅ 2 competencias activas (Copa Libertadores id=2, Liga Profesional id=1)

### Test F: Oficiales agrupados por competencia + flags `isParticipant`
- **Usuario con participaciones** (ana.torres): ✅ `isParticipant: true`
- **Usuario sin participaciones** (test.leave): ✅ `isParticipant: false`, mine count = 0

### TypeScript check
- `npx tsc --noEmit` → ✅ Sin errores

---

## Rutas preservadas (no eliminadas)

- `/competitions/:competitionId` (CompetitionDetailPage) — ruta y componente intactos, solo se eliminó el link "Ver competencia" desde las tarjetas de Explorar. ADMIN sigue teniendo acceso directo.
- `/leagues/new` (LeagueCreatePage) — intacto, accesible desde "Crear Liga con amigos" en Explorar y desde Mis Ligas vacío.

---

## Archivos modificados

| Archivo | Tipo de cambio |
|---|---|
| `backend/Endpoints/LeagueEndpoints.cs` | Agregado endpoint `DELETE /{id}/leave` |
| `frontend/src/api/client.ts` | Agregado `api.del()` |
| `frontend/src/pages/LeaguesMinePage.tsx` | Reescrita (secciones, leave, sin Crear Liga global) |
| `frontend/src/pages/ExploreCompetitionsPage.tsx` | Reescrita (sin Ver competencia, Participar/Crar inline) |
| `frontend/src/components/player/PlayerSidebar.tsx` | Label "Explorar Ligas" → "Explorar Competencias" |
| `frontend/src/pages/PlayerPages.css` | Agregada `.pp-league-card__actions-row` |

---

## Pendientes / Notas

- El bloqueo de "Dejar de participar" para creadores de Ligas de Amigos se implementó en backend pero el frontend solo muestra el botón para `leagueType === 'Official'` (no aparece en Privadas, así que el caso no se expone en UI).
- `CompetitionDetailPage` se mantiene como ruta accesible pero fuera del circuito PLAYER principal.
- No se realizó commit ni push (instrucción explícita del usuario).
- Cambios de Sesión 1 preservados (Login seguro, Registro con repetir contraseña, PlayerDashboard, etc.).
