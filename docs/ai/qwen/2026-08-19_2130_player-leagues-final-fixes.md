# Informe: Correcciones finales — Copa Libertadores + Diferenciación visual + Leave Friends

**Fecha:** 2026-08-19  
**Branch:** `prueba-glm-ui`  
**Agente:** Qwen Code  
**Sesión:** Continuación (Session 2, tanda final)  

---

## 1. Copa Libertadores — Liga Oficial faltante

### Causa raíz

El `DataSeeder` solo creaba una Liga Oficial para **Liga Profesional** (en `GetOrCreateDemoLeagueAsync`, llamada desde `SeedRankingDemoAsync`). Copa Libertadores existía como competencia con ediciones y partidos, pero **no tenía ninguna Liga Oficial asociada**, por lo que `GET /api/leagues/officials` no devolvía nada para `competitionId=2`.

El `ExploreCompetitionsPage` agrupa oficiales por `competitionId`, y sin Liga Oficial para Copa Libertadores, solo mostraba el botón "Crear Liga con amigos".

### Solución

Agregado bloque al final de `SeedRankingDemoAsync` (después de los pronósticos demo) que:
- Busca la competencia "Copa Libertadores" por nombre.
- Verifica idempotencia: si ya existe una Liga Oficial para esa competencia, no crea otra.
- Crea la Liga `Liga General - Copa Libertadores (demo)` con:
  - `LeagueType = Official`
  - `ScopeType = FullCompetition`
  - `InviteCode = "DEMO-COPA-01"`
  - `CreatedByUserId` = primer usuario demo (ana.torres)
  - **Sin participantes automáticos** — el usuario NO queda participante hasta que pulse "Participar en Liga Oficial".

### Resultado

Copa Libertadores ahora muestra:
- Si usuario NO participa → "🏆 Participar en Liga Oficial"
- Si usuario YA participa → "🏆 Ir a mi Liga"
- Siempre → "+ Crear Liga con amigos"

---

## 2. Diferenciación visual — Mis Ligas vs Disponibles

### Problema

Las tarjetas de "Ligas Oficiales disponibles" y "Mis Ligas" usaban el mismo fondo (`var(--color-surface)` = `#1a1a2e`), sin distinción visual.

### Solución

- **Disponibles** (`pp-league-card--official`): borde violeta `var(--color-primary)`, borde 2px, glow sutil. Fondo oscuro normal (`#1a1a2e`).
- **Mis Ligas** (`pp-league-card--mine`): fondo ligeramente más claro (`#222240`), borde `rgba(134, 118, 255, 0.28)`. Sin glow.

Ambas mantienen:
- Dark theme
- Violeta PlayPredict como acento
- Labels OFICIAL / MI LIGA / AMIGOS
- Buen contraste
- Mismo layout y tamaños

### Objetivo logrado

Con una mirada: "arriba disponibles (más oscuras, con glow violeta) / abajo las que ya son mías (más claras, sin glow)".

---

## 3. Dejar de participar — Regla completa

### Backend (`DELETE /api/leagues/{id}/leave`)

La lógica existente ya distingue correctamente los 3 casos:

| Caso | `CreatedByUserId == userId` | `LeagueType` | Tiene pronósticos | Resultado |
|---|---|---|---|---|
| A) Liga Oficial, participante normal | No | Official | No | ✅ Permite leave |
| A) Liga Oficial, participante normal | No | Official | Sí | ❌ Bloquea: "No podés abandonar esta Liga porque ya tenés pronósticos registrados..." |
| B) Liga de Amigos ajena, participante | No | Private | No | ✅ Permite leave |
| B) Liga de Amigos ajena, participante | No | Private | Sí | ❌ Bloquea: mismo mensaje de pronósticos |
| C) Liga de Amigos propia (creador) | Sí | Private | — | ❌ Bloquea: "El creador de una Liga de Amigos no puede abandonarla." |

**No fue necesario modificar el backend.** La condición `CreatedByUserId == user.Id && LeagueType == Private` ya cubre correctamente el caso C sin afectar el caso B.

### Frontend (`LeaguesMinePage.tsx`)

Cambio de condición para mostrar "Dejar de participar":

```diff
- {l.leagueType === 'Official' && (
+ {(l.leagueType === 'Official' || (l.leagueType === 'Private' && !l.isCreator)) && (
```

Esto muestra el botón para:
- ✅ Ligas Oficiales (siempre)
- ✅ Ligas de Amigos donde NO es creador (participante común)
- ❌ Ligas de Amigos donde SÍ es creador (no muestra botón)

---

## 4. Pruebas funcionales

### TEST 1: Copa Libertadores — Circuito completo
- ✅ `GET /api/leagues/officials` → Liga Oficial de Copa Libertadores aparece con `isParticipant=False`
- ✅ `POST /api/leagues/8/join` → `isParticipant=True`
- ✅ Officials actualizados: ambas ligas muestran `isParticipant=True`
- ✅ Mine: Copa Libertadores aparece en Mis Ligas
- ✅ No duplicación en disponibles (filtra `!isParticipant`)

### TEST 2: Diferenciación visual
- ✅ Clase `pp-league-card--mine` aplicada a cards de Mis Ligas
- ✅ Clase `pp-league-card--official` mantenida para disponibles
- ✅ CSS: `--mine` usa `background: #222240` (más claro), `--official` usa `var(--color-surface)` + glow

### TEST 3: Liga de Amigos ajena — Leave exitoso
- ✅ Ana crea Liga de Amigos "Liga de Ana" → `isCreator=True`
- ✅ Juan se une con código → `isCreator=False`
- ✅ Juan: `DELETE /api/leagues/9/leave` → "Dejaste la Liga correctamente."
- ✅ Juan: Liga desaparece de sus Mis Ligas

### TEST 4: Creador de Liga de Amigos — Bloqueado
- ✅ Ana intenta `DELETE /api/leagues/9/leave` → HTTP 400, bloqueado
- ✅ Frontend: botón "Dejar de participar" NO se muestra (condición `!l.isCreator`)

### TEST 5: Participante con pronósticos — Bloqueado
- ✅ Ana intenta `DELETE /api/leagues/1/leave` (Liga Profesional con pronósticos) → HTTP 400
- ✅ Mensaje: "No podés abandonar esta Liga porque ya tenés pronósticos registrados. Los resultados y el ranking quedarían inconsistentes."

### TypeScript: `npx tsc --noEmit` → ✅ Sin errores
### Backend: `docker compose ps` → ✅ 3/3 healthy
### Frontend: operativo en puerto 5175

---

## 5. Archivos modificados (esta tanda)

| Archivo | Cambio |
|---|---|
| `backend/Data/DataSeeder.cs` | +const `CopaLibertadoresOfficialLeagueName`, +bloque seeder para Liga Oficial de Copa Libertadores |
| `frontend/src/pages/LeaguesMinePage.tsx` | Clase `pp-league-card--mine`, condición leave extendida a Private no-creador |
| `frontend/src/pages/PlayerPages.css` | +`.pp-league-card--mine` (fondo más claro, borde sutil) |

---

## 6. git diff --stat (completo, incluye sesiones previas)

```
 backend/Data/DataSeeder.cs                         |  28 ++++
 backend/Endpoints/LeagueEndpoints.cs               |  50 ++++++
 docs/test/Test Demo 1 - v2 Login y circuito basico.docx | Bin 2278071 -> 2457579 bytes
 frontend/src/api/client.ts                         |   8 +-
 frontend/src/components/player/PlayerSidebar.tsx   |   2 +-
 frontend/src/pages/ExploreCompetitionsPage.tsx     | 167 ++++++++++---
 frontend/src/pages/LeagueCreatePage.tsx            |   1 +
 frontend/src/pages/LeaguesMinePage.tsx             | 159 +++++++-----
 frontend/src/pages/LoginPage.css                   |  28 ++++
 frontend/src/pages/LoginPage.tsx                   |  10 +-
 frontend/src/pages/PlayerDashboardPage.css         |  35 ++++
 frontend/src/pages/PlayerDashboardPage.tsx         |  25 +++
 frontend/src/pages/PlayerPages.css                 |  16 +-
 frontend/src/pages/RegisterPage.css                |   5 +
 frontend/src/pages/RegisterPage.tsx                |  41 +++-
 15 files changed, 462 insertions(+), 113 deletions(-)
```

## 7. git status final

```
 M backend/Data/DataSeeder.cs
 M backend/Endpoints/LeagueEndpoints.cs
 M frontend/src/api/client.ts
 M frontend/src/components/player/PlayerSidebar.tsx
 M frontend/src/pages/ExploreCompetitionsPage.tsx
 M frontend/src/pages/LeagueCreatePage.tsx
 M frontend/src/pages/LeaguesMinePage.tsx
 M frontend/src/pages/LoginPage.css
 M frontend/src/pages/LoginPage.tsx
 M frontend/src/pages/PlayerDashboardPage.css
 M frontend/src/pages/PlayerDashboardPage.tsx
 M frontend/src/pages/PlayerPages.css
 M frontend/src/pages/RegisterPage.css
 M frontend/src/pages/RegisterPage.tsx
?? docs/ai/qwen/2026-08-19_2040_test-demo-v2-fixes.md
?? docs/ai/qwen/2026-08-19_2050_player-leagues-navigation.md
```

---

**NO COMMIT. NO PUSH.** Esperando aprobación final.
