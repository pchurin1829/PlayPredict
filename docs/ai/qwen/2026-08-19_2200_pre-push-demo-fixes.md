# Informe: Ajustes pre-push — Color cards / Modal / Estados pronóstico / Navegación ENTER

**Fecha:** 2026-08-19  
**Branch:** `prueba-glm-ui`  
**Agente:** Qwen Code  

---

## 1. Color de cards "Mis Ligas" — Fondo celeste/azul claro

### Problema
Las cards `pp-league-card--mine` usaban `#222240` (muy oscuro, apenas distinguible del fondo general `#1a1a2e`).

### Solución
CSS actualizado:
```css
.pp-league-card--mine {
  background: #1e2e50;
  border-color: rgba(6, 182, 212, 0.25);
}
```
- Fondo `#1e2e50`: celeste/azul claro, claramente más luminoso que el dark background y que las cards oficiales.
- Borde con tinte cyan para reforzar la diferenciación.
- Labels OFICIAL / MI LIGA / AMIGOS permanecen perfectamente visibles.
- Sin colores estridentes, dentro del estilo PlayPredict.

### Comparación visual

| Elemento | Fondo | Borde |
|---|---|---|
| Disponibles (`--official`) | `#1a1a2e` (oscuro) | `var(--color-primary)` violeta + glow |
| Mis Ligas (`--mine`) | `#1e2e50` (celeste claro) | `rgba(6, 182, 212, 0.25)` cyan sutil |
| Fondo general | `#0e0e1a` | — |

---

## 2. Modal de confirmación — ConfirmModal

### Problema
`window.confirm()` no permite controlar estilo ni alineación. El diálogo nativo aparece desalineado.

### Solución
Creado componente `ConfirmModal` (archivos nuevos):
- `frontend/src/components/ConfirmModal.tsx`
- `frontend/src/components/ConfirmModal.css`

Características:
- Overlay centrado con `backdrop-filter: blur(4px)`.
- Texto centrado, botones centrados.
- Estilo coherente con el tema oscuro PlayPredict.
- Props: `open`, `title`, `message`, `confirmLabel`, `cancelLabel`, `onConfirm`, `onCancel`.
- Click en overlay cierra el modal (cancelar).

### Uso en LeaguesMinePage
Reemplazado `window.confirm()` por `ConfirmModal`:
- Estado `leaveTarget` (id + name) en vez de confirm nativo.
- `requestLeaveLeague()` abre el modal.
- `cancelLeave()` cierra.
- `confirmLeaveLeague()` ejecuta el leave.

### Regla UX documentada

> **Diálogos/confirmaciones modales de PlayPredict deben presentar texto y acciones centradas y coherentes con el tema visual.** Esta regla NO aplica a mensajes inline de formularios o banners.

---

## 3. Estados del botón de pronóstico

### Estados definidos

| Estado | Condición | Label botón | Acción | Visual |
|---|---|---|---|---|
| **A — Vacío** | Inputs: `- / -` | `Pronosticar` | Disabled (no hace nada) | Primary, disabled |
| **B — Listo** | Inputs completados, sin predicción previa | `Guardar pronóstico` | POST | Primary |
| **C — Guardado** | Inputs coinciden con persistido | `Pronosticado` | Disabled (no guarda de nuevo) | Verde `pp-btn--saved` |
| **D — Modificado** | Inputs difieren del persistido | `Guardar cambios` | PUT | Primary |

### Implementación

Función `computeButtonState(homeInput, awayInput, persistedPrediction)` → devuelve `'empty' | 'ready' | 'saved' | 'dirty'`.

- Se recalcula en cada cambio de input.
- Después de guardar (POST o PUT) → `buttonState = 'saved'`.
- Si el usuario modifica valores → `buttonState = 'dirty'`.
- No se hacen requests si `buttonState === 'empty'` o `'saved'`.

### CSS nuevo
```css
.pp-btn--saved {
  background: rgba(34, 197, 94, 0.15);
  color: #22c55e;
  border: 1px solid rgba(34, 197, 94, 0.3);
  cursor: default;
}
```

### Label de sección
- Cuando `btnState === 'saved'`: label cambia de `"INGRESÁ TU PRONÓSTICO"` a `"✅ PRONOSTICADO"`.
- Hint de modificación solo se muestra cuando hay predicción Y el estado no es `saved`.

---

## 4. Navegación con ENTER

### Flujo anterior
ENTER: local → visitante → siguiente partido (saltaba el botón)

### Flujo nuevo
ENTER: 
1. **local** → ENTER → **visitante** 
2. **visitante** → ENTER → **botón de acción** del mismo partido
3. **botón** → ENTER/Space → ejecuta la acción (si procede) → **primer input del siguiente partido**

### Implementación

- `handlePredictionEnter()`: cuando el input es el último (away), mueve foco al botón `[data-prediction-action]`.
- `handleActionKeyDown()`: ENTER o Space en el botón → ejecuta `savePrediction()` si el estado es `ready` o `dirty`. Si es `saved` o `empty`, no hace nada (botón disabled).
- `advanceToNextMatch()`: después de guardar exitosamente, busca el siguiente partido pronosticable y mueve foco a su primer input con un `setTimeout(300ms)` para que el save se complete.
- Si el botón está en estado `saved` (deshabilitado), el foco puede avanzar directamente al siguiente partido via TAB; ENTER no activa un botón disabled.

### No hay submits accidentales
- Todos los botones son `type="button"`.
- Los forms no tienen action.
- No se recarga la página.

---

## 5. Pruebas realizadas

### Tests API (predicciones)

| Test | Resultado |
|---|---|
| TEST 1: Partido vacío → sin predicción | ✅ `hasPrediction=False` → estado `empty` |
| TEST 2: Completar 2-1 → POST | ✅ Creada predicción id=45 |
| TEST 3: Verificar estado guardado | ✅ `hasPrediction=True`, values coinciden → estado `saved` |
| TEST 4: Modificar a 3-1 → PUT | ✅ Actualizada correctamente |
| TEST 5: Verificar vuelta a guardado | ✅ Values coinciden → estado `saved` |

### Tests infraestructura

| Test | Resultado |
|---|---|
| `npx tsc --noEmit` | ✅ Sin errores |
| Backend health | ✅ OK |
| `docker compose ps` | ✅ 3/3 healthy |
| Mis Ligas card color | ✅ CSS `.pp-league-card--mine` con `#1e2e50` |
| ConfirmModal | ✅ Componente nuevo, reemplaza `window.confirm` |

### Tests UI (requieren navegador)

| Test | Nota |
|---|---|
| TEST 6: ENTER navigation | Implementado en código: local→visitante→botón→siguiente |
| TEST 7: Card color visual | CSS con celeste/azul claro `#1e2e50` |
| TEST 8: Modal centrado | Componente ConfirmModal con overlay + centrado |

---

## 6. Reglas UX documentadas

### R1: Estados estándar del botón de pronóstico
Los cuatro estados (empty/ready/saved/dirty) con labels, acciones y visuales definidos. Nunca invitar a guardar si no hay cambios.

### R2: Navegación ENTER
local → visitante → botón → siguiente partido. El botón recibe foco; si está deshabilitado (Pronosticado), ENTER no ejecuta acción. TAB funciona normalmente.

### R3: Diálogos modales centrados
Las confirmaciones de PlayPredict usan `ConfirmModal` con texto y acciones centradas, coherentes con el tema visual. No se usa `window.confirm`. No aplica a mensajes inline o banners.

---

## 7. Archivos modificados (esta tanda)

| Archivo | Cambio |
|---|---|
| `frontend/src/components/ConfirmModal.tsx` | **NUEVO** — Componente modal de confirmación |
| `frontend/src/components/ConfirmModal.css` | **NUEVO** — Estilos del modal centrado |
| `frontend/src/pages/LeaguesMinePage.tsx` | Reemplazo `window.confirm` por `ConfirmModal`, estado `leaveTarget` |
| `frontend/src/pages/PredictionsMatchesPage.tsx` | Estados A-D del botón, navegación ENTER, `computeButtonState`, `advanceToNextMatch` |
| `frontend/src/pages/PlayerPages.css` | `.pp-league-card--mine` celeste, `.pp-btn--saved` verde |

---

## 8. git diff --stat (completo, todas las sesiones)

```
 backend/Data/DataSeeder.cs                         |  28 ++++
 backend/Endpoints/LeagueEndpoints.cs               |  50 ++++++
 docs/test/Test Demo 1 - v2 Login y circuito basico.docx | Bin 2278071 -> 2457579 bytes
 frontend/src/api/client.ts                         |   8 +-
 frontend/src/components/player/PlayerSidebar.tsx   |   2 +-
 frontend/src/pages/ExploreCompetitionsPage.tsx     | 167 +++++---
 frontend/src/pages/LeagueCreatePage.tsx            |   1 +
 frontend/src/pages/LeaguesMinePage.tsx             | 186 +++++----
 frontend/src/pages/LoginPage.css                   |  28 +++
 frontend/src/pages/LoginPage.tsx                   |  10 +-
 frontend/src/pages/PlayerDashboardPage.css         |  35 +++
 frontend/src/pages/PlayerDashboardPage.tsx          |  25 +++
 frontend/src/pages/PlayerPages.css                 |  24 +-
 frontend/src/pages/PredictionsMatchesPage.tsx      | 146 +++++---
 frontend/src/pages/RegisterPage.css                |   5 +
 frontend/src/pages/RegisterPage.tsx                |  41 ++-
 16 files changed, 622 insertions(+), 134 deletions(-)
```

## 9. git status final

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

---

**NO COMMIT. NO PUSH.** Esperando aprobación final.
