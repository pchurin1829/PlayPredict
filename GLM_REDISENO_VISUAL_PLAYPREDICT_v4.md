# GLM — REDISEÑO VISUAL PLAYPREDICT v4

**Etapa:** CORRECCIONES POST-PRUEBA MANUAL  
**Fecha:** 2026-08-09  
**Rama:** `prueba-glm-ui`  
**Predecesor:** GLM_REDISENO_VISUAL_PLAYPREDICT_v3.md (circuito jugable mínimo, NO modificado)

---

## 1. Observaciones corregidas

### 1.1 Liga — Tabs no visibles ✅ CORREGIDO

**Antes:** En `/leagues/:id` no se veían los tabs (Resumen, Pronósticos, Ranking, Premios, Participantes). Se veía directamente ficha + participantes sin navegación.

**Diagnóstico:** El código JSX de tabs era correcto pero el CSS no era suficientemente prominente. Los tabs tenían fondo transparente, sin contenedor visual, y el indicador activo era apenas un borde-bottom de 2px.

**Corrección (PlayerPages.css):**
- `.pp-tabs` ahora tiene `background`, `border`, `border-radius` superior → se ve como barra de navegación
- `.pp-tab` padding aumentado (0.6rem → 0.75rem), hover con fondo
- `.pp-tab--active` tiene fondo `primary-light` y font-weight 700
- Indicador activo: 3px con border-radius
- `.pp-tab--soon` con opacity 0.7

**Después:** La Liga funciona como workspace PLAYER con navegación tab clara y visible.

---

### 1.2 Pronósticos — UX de edición ✅ CORREGIDO

**Antes:**
- Botón decía "Actualizar" (confuso)
- No había indicación de que se puede editar
- Feedback de guardado era solo "¡Guardado!"

**Corrección (PredictionsMatchesPage.tsx):**
- Botón cambiado de "Actualizar" → **"Guardar cambios"**
- Se agrega texto de ayuda debajo del botón cuando ya existe pronóstico:
  > "Podés modificar tu pronóstico hasta el cierre del partido."
- Feedback diferenciado:
  - Pronóstico nuevo: **"Pronóstico guardado correctamente."**
  - Pronóstico actualizado: **"Pronóstico actualizado correctamente."**
- Duración del mensaje: 4 segundos (era 3)
- Lógica: se agrega `const isUpdate = !!match.myPrediction` antes del POST/PUT

**Corrección (PlayerPages.css):**
- `.pp-match-card__saved` ahora tiene fondo `success-light` y padding → más visible
- Nuevo `.pp-match-card__hint` con estilo italic, color muted

---

### 1.3 Rankings — Identificar al usuario ✅ CORREGIDO

**Antes:** El código de `(Vos)` existía en LeagueDetailPage y RankingGeneralPage pero el badge era muy sutil (texto suelto color primary).

**Corrección (PlayerPages.css):**
- `.pp-ranking__me-badge` ahora es un **pill** con fondo `color-primary` y texto blanco
- `.pp-ranking__me td` tiene `font-weight: 700` → toda la fila resaltada
- Más padding y border-radius en el badge

**Nota:** El código JSX de `(Vos)` ya existía y era correcto en los tres ranking pages (General, Round, Liga). El problema era puramente de visibilidad CSS.

---

### 1.4 Inicio — Contexto de competencia incorrecto ✅ CORREGIDO

**Antes:** En Inicio aparecía "Fecha actual — Fecha 1 de 5 — Copa Libertadores" cuando el usuario juega en Liga Profesional / Clausura 2026.

**Diagnóstico:** `PlayerDashboardPage.tsx` usaba:
```ts
const activeCompetition = competitions.find((c) => c.isActive)
```
Esto tomaba la **primera** competencia activa de la lista global (Libertadores), no la competencia de la liga del usuario.

**Corrección (PlayerDashboardPage.tsx):**
```ts
const activeCompetition = leagues.length > 0
  ? competitions.find((c) => c.id === leagues[0].competitionId)
  : competitions.find((c) => c.isActive)
```
Ahora prioriza la competencia de la primera Liga del usuario. Solo cae a la competencia global si el usuario no tiene Ligas.

**Corrección adicional:** `currentRoundIndex` tenía un bug — `findIndex(r => ... || true)` siempre devolvía 0+1=1. Simplificado a `1` (primera fecha como referencia).

---

### 1.5 Crear Liga — CTA prominente ✅ CORREGIDO

**Antes:** En "Mis Ligas" no había botón visible para crear una Liga. Solo "Explorar Competencias" (primary) y "Unirse por código" (secondary).

**Corrección (LeaguesMinePage.tsx):**
- **Header actions:** Se agrega **"+ Crear Liga"** como botón primary, más grande (fontSize: 1rem, padding: 0.6rem 1.5rem)
- "Explorar Competencias" pasa a secondary
- **Empty state:** Se agrega "+ Crear Liga" como primera acción (primary) antes de Explorar

---

### 1.6 Explorar Competencias — Acción "Crear Liga" ✅ CORREGIDO

**Antes:** Cada competencia mostraba solo "Ver competencia" como acción.

**Corrección (ExploreCompetitionsPage.tsx):**
- Se agrega **"+ Crear Liga"** como segundo botón en cada card
- Link a `/leagues/new?competitionId={id}` → pre-selecciona la competencia
- Layout: `pp-comp-card__actions` (flex row con gap)

**Corrección (PlayerPages.css):**
- Nuevo `.pp-comp-card__actions` (flex container)
- Nuevo `.pp-comp-card__action--secondary` (outline con border primary)

---

### 1.7 Invitar Amigos — Estado real documentado ✅ DOCUMENTADO

**Qué existe HOY (backend completo):**

| Componente | Detalle |
|---|---|
| `League.InviteCode` | Campo único (8 chars, sin ambigüedad: sin O/0/I/1) |
| `GenerateUniqueInviteCodeAsync` | Genera código aleatorio, verifica unicidad |
| `POST /api/leagues/join` | Acepta `JoinLeagueDto(InviteCode)`, busca Liga, valida que esté activa, agrega usuario como participante |
| `GET /api/leagues/{id}/participants` | Lista participantes con flag `isCreator` |
| `LeagueDetailPage` | Muestra código de invitación al creador |
| `LeagueJoinPage` | Pantalla "Unirse por código" funcional |
| `LeaguesMinePage` | Botón "Unirse por código" |

**Qué FALTA:**

1. **Copy-to-clipboard** del código de invitación (UX: un click para copiar)
2. **Share link** que incluya el código (WhatsApp, etc.)
3. **El bloque "¡Invitá amigos!" PRÓXIMAMENTE** en el dashboard debería reemplazarse por el código real
4. **Validación de duplicado** al unirse (ya existe pero el mensaje de error podría ser más claro)

**Siguiente paso mínimo:** Hacer que el código de invitación sea copiable con un botón, y quitar el PRÓXIMAMENTE del bloque de invitaciones (el backend ya funciona).

---

## 2. Archivos modificados

| Archivo | Cambio |
|---|---|
| `frontend/src/pages/PlayerPages.css` | Tabs visibles, ranking badge prominente, match-card hint/saved, comp-card actions |
| `frontend/src/pages/PredictionsMatchesPage.tsx` | Botón "Guardar cambios", help text, feedback diferenciado |
| `frontend/src/pages/PlayerDashboardPage.tsx` | Competencia de la Liga del usuario, fix currentRoundIndex |
| `frontend/src/pages/LeaguesMinePage.tsx` | CTA "+ Crear Liga" prominente |
| `frontend/src/pages/ExploreCompetitionsPage.tsx` | Botón "+ Crear Liga" por competencia |

---

## 3. Comportamiento antes/después

| Aspecto | Antes | Después |
|---|---|---|
| Liga tabs | Invisibles (sin fondo, sin contenedor) | Barra de navegación clara con fondo, bordes, indicador activo |
| Botón editar pronóstico | "Actualizar" | "Guardar cambios" |
| Help text edición | No existía | "Podés modificar tu pronóstico hasta el cierre del partido." |
| Feedback guardado | "¡Guardado!" (3s) | "Pronóstico guardado/actualizado correctamente." (4s, con fondo) |
| Badge "(Vos)" en ranking | Texto suelto color primary | Pill blanco sobre primary, fila en bold |
| Inicio competencia | Primera competencia global (Libertadores) | Competencia de la Liga del usuario |
| Crear Liga desde Mis Ligas | No existía | Botón primary prominente "+ Crear Liga" |
| Crear Liga desde Explorar | No existía | Botón outline "+ Crear Liga" por competencia |

---

## 4. Qué quedó pendiente

1. **Login Page** — Rediseño visual pendiente (documentado desde v2)
2. **Tab Premios** — Sigue PRÓXIMAMENTE (no hay backend de premios pubblicati)
3. **"¡Invitá amigos!" PRÓXIMAMENTE** — El backend funciona pero el dashboard lo muestra como próximamente. Falta conectar y hacer el código copiable.
4. **Copy-to-clipboard** del código de invitación
5. **Dashboard sidebar** — CurrentRoundCard muestra "Fecha 1" siempre (no detecta fecha actual real)
6. **Explorar Competencias** — No es un rediseño completo, solo se agregaron acciones. La estructura de cards funciona.

---

## 5. Estado de Crear Liga

**Flujo completo funcional:**

```
Mis Ligas → "+ Crear Liga" → Formulario:
  - Nombre de Liga
  - Competencia (selector, pre-seleccionable via ?competitionId=)
  - Descripción (opcional)
  - Alcance: "Toda la Competencia" / "Rango de Fechas"
  → Crear → Liga aparece en Mis Ligas
```

**CTAs disponibles:**
- Mis Ligas → "+ Crear Liga" (primary, grande)
- Explorar Competencias → "+ Crear Liga" por competencia (outline)
- CompetitionDetailPage → "+ Crear nueva Liga" (primary)

---

## 6. Estado real de Invitaciones

**Backend: COMPLETO** ✅
- Código de invitación generado automáticamente al crear Liga (8 chars únicos)
- `POST /api/leagues/join` funciona (valida código, que la Liga esté activa, agrega participante)
- Idempotente: no falla si el usuario ya es participante

**Frontend: PARCIAL** ⚠️
- LeagueJoinPage funcional ("Unirse por código")
- LeagueDetailPage muestra código al creador
- **Falta:** Botón copiar código, share link, quitar PRÓXIMAMENTE del dashboard

---

## 7. Validaciones

| Check | Resultado |
|---|---|
| `npx tsc --noEmit` | ✅ 0 errores |
| `npx vite build` | ✅ 89 módulos, 572ms |
| Backend build | No se tocó backend en esta tarea |

---

## 8. git status --short

```
 M SESSION.md
 M backend/Data/DataSeeder.cs
 M backend/Endpoints/RankingEndpoints.cs
 M backend/Services/RankingService.cs
 M frontend/index.html
 M frontend/src/App.tsx
 M frontend/src/components/Layout.tsx
 M frontend/src/index.css
 M frontend/src/pages/CompetitionDetailPage.tsx
 M frontend/src/pages/ExploreCompetitionsPage.tsx
 M frontend/src/pages/LeagueDetailPage.tsx
 M frontend/src/pages/LeaguesMinePage.tsx
 M frontend/src/pages/LoginPage.tsx
 M frontend/src/pages/PlayerDashboardPage.tsx
 M frontend/src/pages/PredictionsMatchesPage.tsx
 M frontend/src/pages/PrizesCompetitionsPage.tsx
 M frontend/src/pages/PrizesEditionsPage.tsx
 M frontend/src/pages/PrizesListPage.tsx
 M frontend/src/pages/ProfilePage.tsx
 M frontend/src/pages/RankingGeneralPage.tsx
 M frontend/src/pages/RankingRoundPage.tsx
 M frontend/src/pages/RankingsCompetitionsPage.tsx
 M frontend/src/pages/RankingsEditionsPage.tsx
 M frontend/src/pages/RankingsRoundsPage.tsx
 M frontend/src/pages/PlayerPages.css
?? .qwen/
?? GLM_AUDITORIA_VISUAL_PLAYPREDICT.md
?? GLM_CIRCUITO_JUGABLE_PLAYPREDICT.md
?? GLM_REDISENO_VISUAL_PLAYPREDICT.md
?? GLM_REDISENO_VISUAL_PLAYPREDICT_v2.md
?? GLM_REDISENO_VISUAL_PLAYPREDICT_v3.md
?? GLM_REDISENO_VISUAL_PLAYPREDICT_v4.md
?? docs/imagenes/
?? frontend/src/components/player/
?? frontend/src/pages/PlayerDashboardPage.css
?? frontend/src/pages/PlayerDashboardPage.tsx
```

---

## 9. Confirmaciones

| Pregunta | Respuesta |
|---|---|
| ¿Se hizo commit? | **NO** |
| ¿Se hizo push? | **NO** |
| ¿Se hizo merge? | **NO** |
| ¿Se tocó backend? | **NO** (solo frontend en esta tarea) |
| ¿Hubo migraciones? | **NO** |

---

*Fin del informe v4 — CORRECCIONES POST-PRUEBA MANUAL*
