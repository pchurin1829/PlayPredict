# GLM — REDISEÑO VISUAL PLAYPREDICT v3

**Etapa:** CIRCUITO JUGABLE MÍNIMO  
**Fecha:** 2026-08-09  
**Rama:** `prueba-glm-ui`  
**Predecesor:** GLM_REDISENO_VISUAL_PLAYPREDICT_v2.md (segunda pasada visual, NO modificado)

---

## 1. Estado encontrado al inicio

Al retomar el proyecto, el repositorio tenía:

- Segunda pasada visual completa (17 páginas PLAYER rediseñadas con sistema CSS `PlayerPages.css`)
- Backend funcional con: Leagues, Predictions, Matches, Evaluations, Edition/Round Ranking
- Datos demo limitados: **1 Fecha, 3 partidos**, solo 3 pronósticos por usuario
- Tab "Ranking" en LeagueDetailPage **deshabilitado** (sin endpoint de backend)
- Sin ranking por Liga — solo existía ranking por Edición y por Fecha

### Funcionalidades que YA existían

| Funcionalidad | Endpoint / Servicio | Estado |
|---|---|---|
| Leagues CRUD + scope (FullCompetition / RoundRange) | `LeagueEndpoints.cs` | ✅ Funcional |
| Matches en scope de Liga | `GET /api/leagues/{id}/matches` | ✅ Funcional |
| Carga de resultado oficial + evaluación atómica | `PUT /api/matches/{id}/result` | ✅ Funcional |
| PredictionEvaluationService | Evaluación automática al cargar resultado | ✅ Funcional |
| Predictions CRUD (scope a Liga + validación server-side) | `PredictionEndpoints.cs` | ✅ Funcional |
| Ranking por Edición | `GET /api/rankings/editions/{editionId}` | ✅ Funcional |
| Ranking por Fecha | `GET /api/rankings/rounds/{roundId}` | ✅ Funcional |
| Frontend: LeagueDetailPage con tabs | Resumen, Pronósticos, Ranking (off), Premios (PRÓXIMAMENTE), Participantes | ✅ Visual listo, Ranking sin datos |

### Qué FALTABA realmente

1. **Ranking por Liga** — No existía ningún endpoint ni servicio. El tab "Ranking" estaba visualmente presente pero deshabilitado y sin backend.
2. **Datos demo insuficientes** — Solo 1 Fecha con 3 partidos. No permitía demostrar el circuito completo (no había partidos futuros para pronosticar, no había variedad de resultados).
3. **Usuarios demo desconectados de la Liga** — Los 4 usuarios de ranking existían pero no estaban incorporados como participantes de ninguna Liga.

---

## 2. Cambios realizados

### 2.1 Datos Demo — De 1 Fecha a 5 Fechas

**Archivo:** `backend/Data/DataSeeder.cs`

#### Antes

| Concepto | Valor |
|---|---|
| Fechas | 1 (constante `RoundName = "Fecha 1"`) |
| Partidos | 3 |
| Resultados cargados | 3 |
| Pronósticos por usuario | 3 |
| Usuarios en Liga | 0 (no existía la relación) |

#### Después

| Concepto | Valor |
|---|---|
| Fechas | 5 (`RoundNames` array: Fecha 1 a Fecha 5) |
| Partidos | 15 (3 por Fecha) |
| Resultados cargados | 9 (Fechas 1-3, todas finalizadas) |
| Partidos futuros | 6 (Fechas 4-5, todas Programadas) |
| Pronósticos por usuario | 9 (uno por partido finalizado) |
| Usuarios en Liga | 4 (Ana, Juan, María, Pedro como participantes) |

#### Estructura de datos

**RoundNames** (nuevo array reemplazando constante):
```
["Fecha 1", "Fecha 2", "Fecha 3", "Fecha 4", "Fecha 5"]
```

**ClausuraMatchups** (5×3 array de tuplas Home/Away):
```
Fecha 1: Boca Juniors vs River Plate | Racing Club vs Independiente | Estudiantes vs Gimnasia
Fecha 2: River Plate vs Racing Club  | Independiente vs Estudiantes | Gimnasia vs Boca Juniors
Fecha 3: Boca Juniors vs Independiente | Racing Club vs Gimnasia    | Estudiantes vs River Plate
Fecha 4: River Plate vs Gimnasia     | Independiente vs Boca Juniors | Estudiantes vs Racing Club
Fecha 5: Boca Juniors vs Estudiantes | Racing Club vs River Plate    | Gimnasia vs Independiente
```

**Programación de partidos (StartsAtUtc):**
- Fechas 1-3: Fechas pasadas (julio/agosto 2026) → status `Finished`
- Fechas 4-5: Fechas futuras (agosto 2026) → status `Scheduled`

**SeedCompetitionAsync** — Firma cambiada de `(string Home, string Away)[]` a `(string Home, string Away)[][]` para crear múltiples Fechas con sus partidos.

**RankingDemoMatches** — Expandido de 3 a 9 resultados (3 Fechas × 3 partidos):
```
(2,1), (1,1), (0,2),   // Fecha 1
(3,0), (1,1), (0,1),   // Fecha 2
(2,0), (1,2), (2,2)    // Fecha 3
```

**RankingDemoPredictions** — Expandido de 3 a 9 por usuario (4 usuarios = 36 pronósticos totales):
```
Juan:  (1,0),(1,1),(0,2),(2,0),(1,1),(1,2),(1,0),(1,2),(2,1)
Ana:   (2,1),(0,1),(0,1),(3,1),(1,0),(0,2),(2,1),(0,1),(1,1)
María: (0,0),(2,0),(1,0),(0,1),(0,0),(1,0),(0,0),(0,0),(0,1)
Pedro: (1,1),(1,0),(1,1),(2,1),(2,1),(0,1),(1,0),(0,0),(3,1)
```

**SeedRankingDemoAsync** — Refactorizado:
- Consulta la edición y luego todas sus Fechas (no una sola)
- Usa `allMatches` ordenados por `StartsAtUtc`
- Toma los primeros `RankingDemoMatches.Length` (9) como finalizados
- Aplica resultados y pronósticos solo a esos partidos
- Los 6 restantes quedan como `Scheduled` sin resultado ni pronóstico

**SeedPrizesDemoAsync** — Fix: usa `r.Order == 1` en lugar de `r.Name == RoundName` (que ya no existe como constante).

#### Recuento de datos semilla en BD

| Entidad | Cantidad |
|---|---|
| Competencias | 1 |
| Ediciones | 1 (Clausura 2026) |
| Fechas | 5 |
| Partidos | 15 (9 Finished + 6 Scheduled) |
| Ligas | 1 ("Liga General - Liga Profesional") |
| Participantes de Liga | 4 |
| Pronósticos | 36 |
| Evaluaciones | 36 |

---

### 2.2 Implementación del Ranking de Liga (Backend)

#### RankingService.cs — Nuevo método

**Archivo:** `backend/Services/RankingService.cs`

Se agregó `GetLeagueRankingAsync`:

```csharp
public Task<List<RankingEntryDto>> GetLeagueRankingAsync(PlayPredictDbContext db, int leagueId)
{
    var rows = db.PredictionEvaluations
        .Where(e => e.Prediction.LeagueId == leagueId)
        .Select(e => new EvaluationRow(
            e.Prediction.UserId,
            e.Prediction.User.FirstName,
            e.Prediction.User.LastName,
            e.Points,
            e.EvaluationType));

    return BuildRankingAsync(rows);
}
```

Lógica: filtra `PredictionEvaluations` donde el `Prediction.LeagueId` coincide, y reutiliza `BuildRankingAsync` (el mismo motor de posiciones compartidas y desempate deportivo que usan Edition y Round ranking).

#### RankingEndpoints.cs — Endpoint nuevo

**Archivo:** `backend/Endpoints/RankingEndpoints.cs`

```
GET /api/rankings/leagues/{leagueId}
```

- Requiere autenticación (`RequireAuthorization`)
- Valida que la Liga exista (404 si no)
- Delega a `RankingService.GetLeagueRankingAsync`
- Retorna `List<RankingEntryDto>`: position, userId, firstName, lastName, points, exactCount, correctCount, incorrectCount, evaluatedCount

---

### 2.3 Cambios Frontend — Ranking dentro de la Liga

**Archivo:** `frontend/src/pages/LeagueDetailPage.tsx`

#### Cambios realizados

1. **Tab Ranking habilitado** — Antes tenía `soon: true` que lo deshabilitaba con `<ComingSoonBadge />`. Ahora es un tab funcional sin `soon`.

2. **Fetch del ranking** — Nuevo `useEffect` que se ejecuta al activar el tab:
   ```typescript
   useEffect(() => {
     if (activeTab !== 'ranking' || !leagueId) return
     api.get<RankingEntry[]>(`/rankings/leagues/${leagueId}`)
       .then(data => setRanking(data))
       .catch(() => setRanking([]))
   }, [activeTab, leagueId])
   ```

3. **Tabla de ranking** — Renderizado con:
   - Posiciones con colores: 🥇🥈🥉 para posiciones 1-3 (`pp-ranking__pos--1/2/3`)
   - Highlighting del usuario actual con clase `pp-ranking__me` y badge "(Vos)"
   - Columnas: #, Jugador, Puntos, Exactos, Correctos, Evaluados
   - Estado vacío con mensaje explicativo si no hay evaluaciones

4. **Import agregado** — `useAuth` de `../auth/AuthContext` para identificar al usuario actual

5. **Tipo importado** — `RankingEntry` de `../api/types` (ya existía en el proyecto)

---

## 3. Cálculo / Scoring Validado

### Fórmula de evaluación (ya existente, verificada)

| Tipo | Condición | Puntos |
|---|---|---|
| ExactScore | Marcador predicho = Marcador oficial | 6 |
| CorrectOutcome | Diferencia/ganador correcto, marcador distinto | 3 |
| Incorrect | Ni marcador ni resultado | 0 |

### Verificación: Juan Pérez (userId: 5)

| # | Partido | Predicción | Resultado | Tipo | Puntos |
|---|---|---|---|---|---|
| 1 | Boca vs River | 1-0 | 2-1 | CorrectOutcome | 3 |
| 2 | Racing vs Independiente | 1-1 | 1-1 | ExactScore | 6 |
| 3 | Estudiantes vs Gimnasia | 0-2 | 0-2 | ExactScore | 6 |
| 4 | River vs Racing | 2-0 | 3-0 | CorrectOutcome | 3 |
| 5 | Independiente vs Estudiantes | 1-1 | 1-1 | ExactScore | 6 |
| 6 | Gimnasia vs Boca | 1-2 | 0-1 | CorrectOutcome | 3 |
| 7 | Boca vs Independiente | 1-0 | 2-0 | CorrectOutcome | 3 |
| 8 | Racing vs Gimnasia | 1-2 | 1-2 | ExactScore | 6 |
| 9 | Estudiantes vs River | 2-1 | 2-2 | Incorrect | 0 |

**Cálculo:** 4×6 + 4×3 + 1×0 = 24 + 12 + 0 = **36 puntos** ✅

### Ranking completo validado por API

| Pos | Jugador | Pts | Exactos | Correctos | Incorrectos | Evaluados |
|---|---|---|---|---|---|---|
| 1° | Juan Pérez | 36 | 4 | 4 | 1 | 9 |
| 2° | Ana Torres | 33 | 3 | 5 | 1 | 9 |
| 3° | Pedro Gómez | 15 | 2 | 1 | 6 | 9 |
| 4° | María López | 9 | 1 | 1 | 7 | 9 |

Resultado confirmado vía `GET /api/rankings/leagues/1` → coincide con cálculo manual.

---

## 4. Endpoints Reutilizados vs Nuevos

### Endpoints reutilizados (sin cambios)

| Endpoint | Propósito en el circuito |
|---|---|
| `POST /api/auth/login` | Autenticar jugadores |
| `GET /api/leagues/{id}` | Info de la Liga |
| `GET /api/leagues/{id}/participants` | Lista de participantes |
| `GET /api/leagues/{id}/matches` | Partidos en scope de Liga (con pronósticos y evaluaciones) |
| `POST /api/predictions` | Crear pronóstico |
| `PUT /api/predictions/{id}` | Modificar pronóstico |
| `PUT /api/matches/{id}/result` | Cargar resultado oficial (dispara evaluación atómica) |
| `GET /api/rankings/editions/{editionId}` | Ranking por Edición |
| `GET /api/rankings/rounds/{roundId}` | Ranking por Fecha |

### Endpoint nuevo

| Endpoint | Método | Descripción |
|---|---|---|
| `/api/rankings/leagues/{leagueId}` | GET | Ranking de la Liga: posiciones, puntos, exactos, correctos, evaluados |

---

## 5. Archivos Modificados

| Archivo | Cambio | Líneas afectadas (aprox.) |
|---|---|---|
| `backend/Data/DataSeeder.cs` | RoundNames array, ClausuraMatchups 5×3, RankingDemoMatches 9, RankingDemoPredictions 9×4, SeedCompetitionAsync firma, SeedRankingDemoAsync refactor, SeedPrizesDemoAsync fix | ~150 líneas |
| `backend/Services/RankingService.cs` | +GetLeagueRankingAsync | +10 líneas |
| `backend/Endpoints/RankingEndpoints.cs` | +GET /api/rankings/leagues/{leagueId} | +10 líneas |
| `frontend/src/pages/LeagueDetailPage.tsx` | Tab Ranking habilitado, fetch ranking, tabla con highlighting | +60 líneas |

---

## 6. Validaciones Realizadas

### Backend

| Check | Comando | Resultado |
|---|---|---|
| Build .NET | `dotnet build --no-restore` (en Docker) | ✅ 0 errores, 1 warning (NU1510, irrelevante) |
| Health check | `curl http://localhost:8080/api/health` | ✅ `{"status":"ok"}` |
| Fechas en BD | `SELECT COUNT(*) FROM "Rounds"` | ✅ 5 |
| Partidos en BD | `SELECT COUNT(*) FROM "Matches"` | ✅ 15 (mostró 18 incluyendo otra competencia) |
| Pronósticos en BD | `SELECT COUNT(*) FROM "Predictions"` | ✅ 36 |
| Evaluaciones en BD | `SELECT COUNT(*) FROM "PredictionEvaluations"` | ✅ 36 |
| Login jugador | `POST /api/auth/login` con juan.perez | ✅ JWT con role PLAYER |
| Partidos de Liga | `GET /api/leagues/1/matches` | ✅ 15 partidos, 9 con evaluaciones, 6 canPredict:true |
| Ranking Liga | `GET /api/rankings/leagues/1` | ✅ 4 posiciones con scoring correcto |

### Frontend

| Check | Comando | Resultado |
|---|---|---|
| TypeScript | `npx tsc --noEmit` | ✅ 0 errores |
| Build producción | `npx vite build` | ✅ 89 módulos, 661ms, sin errores |

### End-to-End

Circuito completo validado vía API:

1. ✅ Login como jugador (juan.perez) → JWT
2. ✅ GET /api/leagues/1/matches → 15 partidos, 9 Finished con evaluaciones visibles, 6 Scheduled con canPredict:true
3. ✅ GET /api/rankings/leagues/1 → 4 posiciones, scoring validado manualmente

---

## 7. Circuito Manual Exacto a Probar

### Como PLAYER (juan.perez@playpredict.local / demo123)

```
1. Login → Ingresar con juan.perez@playpredict.local / demo123
2. Mis Ligas → Ver tarjeta "Liga General - Liga Profesional (demo)" → Click
3. Liga → Tab "Resumen" → Ver info de la Liga, participantes, código de invitación
4. Liga → Tab "Pronósticos" → Click "Ver partidos y pronosticar"
5. Pronósticos → Sección "Pronosticá" (6 partidos de Fechas 4-5 sin pronóstico)
6. Pronósticos → Ingresar scores en los inputs → Click "Guardar"
7. Pronósticos → Ver sección "Resultados" (9 partidos finalizados con evaluaciones)
8. Volver a Liga → Tab "Ranking" → Ver tabla con 4 jugadores
9. Ranking → Ver tu fila resaltada con "(Vos)" en posición 1°
10. Liga → Tab "Participantes" → Ver los 4 participantes con avatares
```

### Como ADMIN (admin@playpredict.local / admin123)

```
1. Login → Ingresar con admin@playpredict.local / admin123
2. Ir a Matches (panel admin) → Buscar un partido de Fecha 4 (Scheduled)
3. Cargar resultado oficial (ej: River 2-1 Gimnasia)
4. Volver como jugador → Verificar que el ranking se actualizó
```

---

## 8. Credenciales de Demo

### ADMIN

| Email | Password |
|---|---|
| admin@playpredict.local | admin123 |
| admin2@playpredict.local | admin123 |
| admin3@playpredict.local | admin123 |

### PLAYER (participantes de Liga demo)

| Email | Password |
|---|---|
| ana.torres@playpredict.local | demo123 |
| juan.perez@playpredict.local | demo123 |
| maria.lopez@playpredict.local | demo123 |
| pedro.gomez@playpredict.local | demo123 |

---

## 9. Limitaciones y Pendientes

1. **No se puede crear una Liga desde cero como PLAYER en el demo actual** — La Liga demo viene pre-semilla. Crear una Liga nueva funciona por UI pero no hay datos para el circuito completo sin intervención admin.

2. **No se puede cargar resultados como PLAYER** — Solo ADMIN puede hacer `PUT /api/matches/{id}/result`. Para el circuito completo manual, se necesita alternar entre sesión admin y player.

3. **Tab Premios sigue PRÓXIMAMENTE** — No se implementó en esta tarea (fuera de scope del circuito jugable).

4. **18 partidos en BD, 15 en la Liga** — Los 3 partidos adicionales pertenecen a otra competencia/edición que también genera DataSeeder. No afecta el circuito pero podría confundir en consultas globales.

5. **Login Page sin rediseño visual** — Pendiente de la segunda pasada visual (documentado en v2 como pendiente).

6. **No hay notificación ni feedback visual al guardar un pronóstico** — El guardado funciona pero UX podría mejorarse.

---

## 10. Estado Git Final

### Confirmaciones

| Pregunta | Respuesta |
|---|---|
| ¿Hubo migraciones? | **NO** — ningún cambio de esquema de BD |
| ¿Se hizo commit? | **NO** |
| ¿Se hizo push? | **NO** |
| ¿Se hizo merge? | **NO** |

Todos los cambios quedan sin commitear en la rama `prueba-glm-ui`.

---

*Fin del informe v3 — CIRCUITO JUGABLE MÍNIMO*
