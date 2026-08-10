# GLM — CIRCUITO JUGABLE MÍNIMO · PlayPredict

**Fecha:** 2026-08-09  
**Rama:** `prueba-glm-ui`  
**Tarea:** Cerrar y validar el circuito jugable mínimo de PlayPredict  

---

## 1. Circuito Jugable — Definición

El circuito jugable mínimo es:

> **Crear/usar una Liga → Tener múltiples Fechas → Pronosticar partidos → Cargar resultados oficiales → Calcular puntos → Visualizar ranking de la Liga**

Cada paso fue auditado, implementado donde faltaba, y validado.

---

## 2. Estado del Circuito por Fase

### FASE 1 — Auditoría del Backend ✅

Se auditaron los siguientes componentes existentes:

| Componente | Archivo | Estado |
|---|---|---|
| Leagues CRUD + scope | `LeagueEndpoints.cs` | ✅ Funcional |
| Match result + evaluación atómica | `MatchEndpoints.cs` + `PredictionEvaluationService.cs` | ✅ Funcional |
| Predictions CRUD (scope a Liga) | `PredictionEndpoints.cs` | ✅ Funcional |
| Edition/Round ranking | `RankingService.cs` | ✅ Funcional |
| League ranking | **AUSENTE** | ❌ → Implementado en esta tarea |

**Hallazgo crítico:** No existía endpoint ni servicio para ranking por Liga. El flujo de pronósticos evaluados existía, pero no había forma de ver las posiciones dentro de una Liga.

---

### FASE 2 — Datos Demo Robustos ✅

**Archivo modificado:** `backend/Data/DataSeeder.cs`

Cambios realizados:

| Antes | Después |
|---|---|
| 1 Fecha, 3 partidos | 5 Fechas, 15 partidos |
| 3 resultados cargados | 9 resultados cargados (Fechas 1-3) |
| 3 pronósticos por usuario | 9 pronósticos por usuario |
| 4 usuarios sin Liga | 4 usuarios como participantes de Liga demo |

**Datos semilla resultantes:**

| Entidad | Cantidad |
|---|---|
| Competencias | 1 (Liga Profesional) |
| Ediciones | 1 (Clausura 2026) |
| Fechas | 5 (Fecha 1 a Fecha 5) |
| Partidos | 15 (9 Finalizados + 6 Programados) |
| Liga demo | 1 ("Liga General - Liga Profesional") |
| Participantes | 4 (Ana, Juan, María, Pedro) |
| Pronósticos | 36 (4 usuarios × 9 partidos finalizados) |
| Evaluaciones | 36 (todas las predicciones evaluadas) |

**Partidos por Fecha:**

| Fecha | Partido 1 | Partido 2 | Partido 3 | Estado |
|---|---|---|---|---|
| Fecha 1 | Boca vs River | Racing vs Independiente | Estudiantes vs Gimnasia | ✅ Finalizada |
| Fecha 2 | River vs Racing | Independiente vs Estudiantes | Gimnasia vs Boca | ✅ Finalizada |
| Fecha 3 | Boca vs Independiente | Racing vs Gimnasia | Estudiantes vs River | ✅ Finalizada |
| Fecha 4 | River vs Gimnasia | Independiente vs Boca | Estudiantes vs Racing | 🕐 Programada |
| Fecha 5 | Boca vs Estudiantes | Racing vs River | Gimnasia vs Independiente | 🕐 Programada |

---

### FASE 3 — League Ranking (Backend) ✅

**Archivos modificados:**

#### `backend/Services/RankingService.cs`
Se agregó el método `GetLeagueRankingAsync`:

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

#### `backend/Endpoints/RankingEndpoints.cs`
Se agregó el endpoint:

```
GET /api/rankings/leagues/{leagueId}
```

- Verifica que la Liga exista (404 si no)
- Delega a `RankingService.GetLeagueRankingAsync`
- Retorna `List<RankingEntryDto>` con posiciones

---

### FASE 4 — League Ranking (Frontend) ✅

**Archivo modificado:** `frontend/src/pages/LeagueDetailPage.tsx`

La pestaña "Ranking" de la Liga ahora:
- Se habilitó (antes estaba deshabilitada)
- Consulta `GET /rankings/leagues/{leagueId}` al activarse
- Muestra tabla con posiciones, puntos, exactos, correctos, evaluados
- Resalta al usuario actual con clase `pp-ranking__me` y badge "(Vos)"
- Colores de posición: 🥇🥈🥉 para posiciones 1-3
- Estado vacío con mensaje explicativo si no hay evaluaciones

---

### FASE 5 — Validación End-to-End ✅

#### Verificación por API (curl dentro del contenedor)

**1. Login como jugador:**
```
POST /api/auth/login  →  {"email":"juan.perez@playpredict.local","password":"demo123"}  →  200 OK + JWT
```

**2. Partidos de la Liga (como jugador):**
```
GET /api/leagues/1/matches  →  15 partidos
```
- 9 Finalizados con evaluaciones visibles (puntos, tipo)
- 6 Programados con `canPredict: true` (listos para pronosticar)

**3. Ranking de la Liga:**
```
GET /api/rankings/leagues/1  →  4 posiciones
```

| Pos | Jugador | Pts | Exactos | Correctos | Incorrectos | Evaluados |
|---|---|---|---|---|---|---|
| 1° | Juan Pérez | 36 | 4 | 4 | 1 | 9 |
| 2° | Ana Torres | 33 | 3 | 5 | 1 | 9 |
| 3° | Pedro Gómez | 15 | 2 | 1 | 6 | 9 |
| 4° | María López | 9 | 1 | 1 | 7 | 9 |

**4. Escoring verificado por partido (Juan Pérez):**
| Partido | Predicción | Resultado | Evaluación | Puntos |
|---|---|---|---|---|
| Boca 2-1 River | 1-0 | 2-1 | CorrectOutcome | 3 |
| Racing 1-1 Ind. | 1-1 | 1-1 | ExactScore | 6 |
| Est. 0-2 Gim. | 0-2 | 0-2 | ExactScore | 6 |
| River 3-0 Racing | 2-0 | 3-0 | CorrectOutcome | 3 |
| Ind. 1-1 Est. | 1-1 | 1-1 | ExactScore | 6 |
| Gim. 0-1 Boca | 1-2 | 0-1 | CorrectOutcome | 3 |
| Boca 2-0 Ind. | 1-0 | 2-0 | CorrectOutcome | 3 |
| Racing 1-2 Gim. | 1-2 | 1-2 | ExactScore | 6 |
| Est. 2-2 River | 2-1 | 2-2 | Incorrect | 0 |

**Total: 36 pts** → coincide con el ranking ✅

---

### FASE 6 — Builds ✅

| Check | Resultado |
|---|---|
| `dotnet build` (backend en Docker) | 0 errores, 1 warning (NU1510, irrelevante) |
| `npx tsc --noEmit` (frontend) | 0 errores |
| `npx vite build` (frontend) | Build exitoso, 89 módulos, 661ms |

---

## 3. Credenciales de Demo

### ADMIN
| Email | Password | Rol |
|---|---|---|
| admin@playpredict.local | admin123 | ADMIN |
| admin2@playpredict.local | admin123 | ADMIN |
| admin3@playpredict.local | admin123 | ADMIN |

### PLAYER (participantes de la Liga demo)
| Email | Password | Rol |
|---|---|---|
| ana.torres@playpredict.local | demo123 | PLAYER |
| juan.perez@playpredict.local | demo123 | PLAYER |
| maria.lopez@playpredict.local | demo123 | PLAYER |
| pedro.gomez@playpredict.local | demo123 | PLAYER |

---

## 4. Flujo Manual a Probar

### Como PLAYER (juan.perez@playpredict.local / demo123):

1. **Login** → Ingresar con credenciales de jugador
2. **Mis Ligas** → Ver "Liga General - Liga Profesional (demo)"
3. **Liga → Resumen** → Ver info de la Liga, participantes
4. **Liga → Pronósticos** → Click "Ver partidos y pronosticar"
5. **Pronosticar** → Ingresar scores para partidos de Fecha 4 y 5 (6 partidos pendientes)
6. **Liga → Ranking** → Ver tabla de posiciones con 4 jugadores, tu fila resaltada
7. **Liga → Participantes** → Ver los 4 participantes con avatar

### Como ADMIN (admin@playpredict.local / admin123):

1. **Login** → Ingresar como admin
2. **Matches** → Cargar resultado a un partido de Fecha 4
3. **Verificar** → Volver como jugador y confirmar que el ranking se actualizó

---

## 5. Archivos Modificados

| Archivo | Cambio |
|---|---|
| `backend/Data/DataSeeder.cs` | 5 Fechas, 15 partidos, 4 usuarios demo en Liga |
| `backend/Services/RankingService.cs` | +`GetLeagueRankingAsync` |
| `backend/Endpoints/RankingEndpoints.cs` | +`GET /api/rankings/leagues/{leagueId}` |
| `frontend/src/pages/LeagueDetailPage.tsx` | Tab Ranking habilitada + conectada a API |

---

## 6. Lo que NO se hizo (por restricción explícita)

- ❌ No se modificó la base de datos (no migraciones, no esquema nuevo)
- ❌ No se agregó funcionalidad nueva más allá del ranking de Liga (mínimo para cerrar el circuito)
- ❌ No se hizo commit/push/merge
- ❌ No se modificó la UI visual más allá de conectar el tab existente al endpoint

---

## 7. Confirmaciones Finales

| Pregunta | Respuesta |
|---|---|
| ¿El circuito jugable quedó completo? | **SÍ** — Liga → Fechas → Pronosticar → Resultados → Puntos → Ranking |
| ¿Qué debo probar manualmente? | Login como jugador, pronosticar Fecha 4-5, ver Ranking, login admin para cargar resultado |
| ¿Hubo migraciones? | **NO** — todo el trabajo fue sobre datos semilla y endpoints, sin cambios de esquema |
| ¿Se hizo commit/push? | **NO** — todo queda sin commitear en rama `prueba-glm-ui` |
