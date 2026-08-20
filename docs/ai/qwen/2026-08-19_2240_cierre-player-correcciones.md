# Informe: Cierre correcciones PLAYER — Identidad visual / Pronósticos / Suspender

**Fecha:** 2026-08-19  
**Branch:** `prueba-glm-ui`  
**Agente:** Qwen Code  

---

## 1. Archivos modificados

| Archivo | Cambio |
|---|---|
| `frontend/src/pages/LeaguesMinePage.tsx` | Identidad visual: Oficial → `--official`, Privada → `--mine`. Acciones: Dejar (Oficial + Amigos ajena), Suspender/Reactivar (MI LIGA). Modal unificado. |
| `frontend/src/pages/PredictionsMatchesPage.tsx` | Reescrita: RowState con `savedHome/savedAway/hasPrediction`, dirty check explícito, estados A/B/C, validación 0-válido, ENTER → botón → siguiente |
| `frontend/src/pages/PlayerPages.css` | `.pp-league-card--mine` celeste, `.pp-league-card--official` violeta, `.pp-btn--saved` verde, `.pp-league-card__status--suspended` amarillo |
| `frontend/src/components/ConfirmModal.tsx` | Sin cambios (ya existente) |
| `frontend/src/components/ConfirmModal.css` | Sin cambios (ya existente) |

## 2. Causa raíz del problema de estados de Pronósticos

**Problema observado:** Después de guardar un pronóstico, el botón seguía diciendo "Guardar cambios" en vez de "Pronosticado".

**Causa raíz:** La implementación anterior usaba `computeButtonState()` que dependía de `matches?.find(m => m.id === match.id)?.myPrediction` para determinar el estado dirty. Este valor era el del **closure** del render anterior, no el actualizado. Cuando `setMatches` y `updateRow` se ejecutaban en la misma función, React los batcheaba pero el `matches` referenciado por `handleInputChange` seguía siendo el viejo.

Además, `buttonState` era un campo derivado (`'empty' | 'ready' | 'saved' | 'dirty'`) que se recalculaba en cada `handleInputChange` usando datos stale del closure.

**Solución:** Eliminar `buttonState` del estado. En vez de eso, mantener explícitamente los valores guardados:
- `savedHome: string | null` — valor persistido del home score
- `savedAway: string | null` — valor persistido del away score  
- `hasPrediction: boolean` — existe pronóstico en backend

El dirty check es puramente derivado: `homeInput !== savedHome || awayInput !== savedAway`.

Después de guardar exitosamente:
```
savedHome = String(homeScore)
savedAway = String(awayScore)
hasPrediction = true
```
→ El próximo render calcula `isDirty() = false` → muestra "Pronosticado" inmediatamente.

## 3. Cómo se implementaron los estados

### ESTADO A — Sin pronóstico
- `hasPrediction = false`, inputs vacíos
- Botón: `¡Pronosticá!` (disabled)
- Cuando completa ambos → `¡Pronosticá!` (habilitado, ejecuta POST)

### ESTADO B — Pronóstico guardado
- `hasPrediction = true`, `homeInput === savedHome && awayInput === savedAway`
- Botón: `Pronosticado` (disabled, verde `.pp-btn--saved`)
- Label: `✅ PRONOSTICADO`
- Modificación de cualquier input → pasa a ESTADO C inmediatamente

### ESTADO C — Pronóstico modificado
- `hasPrediction = true`, `homeInput !== savedHome || awayInput !== savedAway`
- Botón: `Guardar cambios` (habilitado, ejecuta PUT)
- Después de guardar: `savedHome/savedAway` se actualizan → vuelve a ESTADO B
- Mensaje: "Pronóstico actualizado correctamente."

## 4. Navegación con ENTER

Secuencia exacta:

```
[Resultado Local] → ENTER → [Resultado Visitante] → ENTER → [Botón] → ENTER → [Resultado Local siguiente partido]
```

### Implementación

- `handlePredictionEnter()`: si es el último input (away), mueve foco al botón `[data-prediction-action]`
- `handleActionKeyDown()`: ENTER o Space en el botón → ejecuta `savePrediction()` si no está disabled
- El botón **siempre** recibe foco (incluso si es "Pronosticado" disabled) para que el usuario pueda TAB al siguiente
- `advanceToNextMatch()`: después de guardar exitosamente, foco al primer input del siguiente partido con 300ms delay
- Si el botón está disabled ("Pronosticado"), ENTER no ejecuta acción — el usuario puede TAB o modificar un valor para habilitarlo

### No hay submits accidentales
- Todos los botones son `type="button"`
- No hay form con action
- No se recarga la página

## 5. Resultado de la prueba 1-2 / 0-0 / 2-1

Probado via API:

| Partido | Input | Resultado API | Estado UI esperado |
|---|---|---|---|
| Flamengo vs Palmeiras | 0 - 0 | ✅ POST exitoso, persisted 0-0 | Pronosticado |
| Otros partidos | 1-2, 2-1 | ✅ POST exitoso | Pronosticado |

**0-0 funciona correctamente** — la validación usa `row.homeInput === ''` (string vacío) en vez de `!homeScore` (que trataría 0 como false).

La prueba completa de teclado (3 partidos consecutivos + modificación) requiere interacción manual en el navegador.

## 6. Resultado después de F5

Después de F5, `buildInitialRow()` reconstruye el estado desde `match.myPrediction`:
- `savedHome = String(persisted.predictedHomeScore)`
- `savedAway = String(persisted.predictedAwayScore)`
- `hasPrediction = true`

Si no hay modificaciones → `isDirty() = false` → "Pronosticado" ✅

## 7. Colores de Mis Ligas

### Oficial → Violeta/lila (SIEMPRE)
- Clase: `pp-league-card--official`
- Fondo: `var(--color-surface)` (#1a1a2e)
- Borde: `var(--color-primary)` violeta + glow
- Badge: gradiente violeta `#8b5cf6 → #6366f1`
- Se aplica tanto en "Disponibles" como en "Mis Ligas"

### Privada/Amigos → Celeste/azulado
- Clase: `pp-league-card--mine`
- Fondo: `#1e2e50` (celeste oscuro, más claro que Oficial)
- Borde: `rgba(6, 182, 212, 0.25)` cyan sutil
- Badge MI LIGA: gradiente cyan `#06b6d4 → #0891b2`
- Badge AMIGOS: fondo neutral

La identidad visual ya NO depende solo del label — el fondo y borde distinguen claramente.

## 8. Acciones implementadas

### Dejar de participar
- **Liga Oficial**: botón "Dejar de participar" con confirmación via ConfirmModal
- **Liga de Amigos ajena** (`!isCreator`): botón "Dejar de participar" con confirmación
- **MI LIGA** (`isCreator`): NO muestra "Dejar de participar"
- Restricciones backend intactas: pronósticos existentes bloquean el leave

### Suspender Liga
- Solo visible para MI LIGA (`Private && isCreator && isActive`)
- Botón "Suspender Liga" → ConfirmModal → PUT `/api/leagues/{id}` con `isActive: false`
- Estado visual: badge "Suspendida" (amarillo)
- Datos preservados: participantes, pronósticos, ranking

### Reactivar Liga
- Solo visible para MI LIGA (`Private && isCreator && !isActive`)
- Botón "Reactivar Liga" (primary) → ConfirmModal → PUT `/api/leagues/{id}` con `isActive: true`
- Estado visual: vuelve a "Activa"

### Backend
No fue necesario modificar el backend. El endpoint `PUT /api/leagues/{id}` ya soporta cambiar `IsActive` y ya restringe al creador. El join endpoint ya bloquea si `!league.IsActive`.

## 9. Tests ejecutados

| Test | Resultado |
|---|---|
| TypeScript `tsc --noEmit` | ✅ Sin errores |
| Backend health | ✅ OK |
| Docker compose ps | ✅ 3/3 healthy |
| Mis Ligas: Oficial usa `--official` | ✅ Card class basada en `leagueType` |
| Mis Ligas: Privada usa `--mine` | ✅ Card class basada en `leagueType` |
| Suspender Liga (PUT isActive:false) | ✅ Funciona |
| Reactivar Liga (PUT isActive:true) | ✅ Funciona |
| Pronóstico 0-0 (cero válido) | ✅ POST exitoso, persisted 0-0 |
| Leave bloqueado con pronósticos | ✅ HTTP 400 |
| Leave Permitido sin pronósticos | ✅ (verificado en sesiones previas) |
| Explorar Competencias | ✅ 2 competencias activas |
| Officials + isParticipant flags | ✅ Correctos |
| ConfirmModal centrado | ✅ Componente propio |

### Tests que requieren navegador (pendientes de verificación manual)
- ENTER: local → visitante → botón → siguiente partido
- 3 partidos consecutivos con teclado
- Modificar pronóstico guardado → "Guardar cambios" → guardar → "Pronosticado"
- F5 → todos "Pronosticado"
- Color visual Oficial violeta vs Privada celeste

## 10. Pendientes reales

- **Prueba visual manual**: El usuario debe verificar en el navegador:
  - Colores de cards (Oficial violeta vs Privada celeste)
  - Flujo ENTER completo
  - Pronosticado → modificar → Guardar cambios → Pronosticado
  - F5 → estado persistido
  - Modal de Suspender/Reactivar

- **Prueba de teclado 3 partidos**: Requiere interacción manual

---

## git diff --stat

```
 backend/Data/DataSeeder.cs                         |  28 ++
 backend/Endpoints/LeagueEndpoints.cs               |  50 ++++
 docs/test/Test Demo 1 - v2 Login y circuito basico.docx | Bin 2278071 -> 2457579 bytes
 frontend/src/api/client.ts                         |   8 +-
 frontend/src/components/player/PlayerSidebar.tsx   |   2 +-
 frontend/src/pages/ExploreCompetitionsPage.tsx     | 167 +++++---
 frontend/src/pages/LeagueCreatePage.tsx            |   1 +
 frontend/src/pages/LeaguesMinePage.tsx             | 321 +++++----
 frontend/src/pages/LoginPage.css                   |  28 +++
 frontend/src/pages/LoginPage.tsx                   |  10 +-
 frontend/src/pages/PlayerDashboardPage.css         |  35 +++
 frontend/src/pages/PlayerDashboardPage.tsx          |  25 +++
 frontend/src/pages/PlayerPages.css                 |  29 +-
 frontend/src/pages/PredictionsMatchesPage.tsx      | 156 +++++--
 frontend/src/pages/RegisterPage.css                |   5 +
 frontend/src/pages/RegisterPage.tsx                |  41 ++-
 16 files changed, 737 insertions(+), 169 deletions(-)
```

## git status final

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
 M frontend/src/pages/PredictionsMatchesPage.tsx
 M frontend/src/pages/RegisterPage.css
 M frontend/src/pages/RegisterPage.tsx
?? frontend/src/components/ConfirmModal.css
?? frontend/src/components/ConfirmModal.tsx
```

**NO COMMIT. NO PUSH.** Esperando prueba visual del usuario.
