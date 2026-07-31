# PROJECT STATUS

Versión: 0.6.0
Estado: Sprint 5 (Motor de Puntuación Configurable Básico) aprobado, verificado visualmente y commiteado, junto con la consolidación de `docs/`.
Próximo paso: Sprint 6 (Ranking General). No iniciar sin aprobación explícita.

---

## Nota: reorganización de `docs/` consolidada

En la sesión del Sprint 5 se encontró la carpeta `docs/` reorganizada manualmente en el árbol de trabajo, sin commit (archivos planos movidos a `docs/arquitectura/`, `docs/products/`, `docs/business/`). Se informó, no se tocó, y se continuó con el Sprint. En la sesión siguiente, ya aprobado el Sprint 5, se pidió consolidar esa reorganización antes de commitear. Se hizo lo siguiente:

- Se verificó contenido idéntico entre cada archivo movido y su versión commiteada anterior: los 4 `.md` (`MODELO_CONCEPTUAL`, `MODELO_DATOS`, `REGLAS_NEGOCIO`, `PANTALLAS_MVP`, `PLAN_IMPLEMENTACION_MVP` → ahora con sufijo `_v1.0`) son byte-idénticos salvo fin de línea (LF→CRLF, ya esperado en este repo); los 5 PDF de `docs/business/` tienen el mismo hash SHA-256 que sus originales. **No se perdió contenido.**
- Se renombró `docs/products/ROADMAP_PRONOSTICOS.md` → `docs/products/ROADMAP_PRONOSTICOS_v1.0.md` (único archivo que faltaba el sufijo de versión pedido).
- No se creó `MOTOR_DE_PRONOSTICOS_v1.0.md` (no existía y no se pidió crearlo); `MODELO_CONCEPTUAL_PRONOSTICOS_v1.0.md` queda como documento central del Motor de Pronósticos.
- `docs/README_DOCS.md` reescrito con las rutas reales de los 14 documentos existentes (antes listaba archivos inexistentes como `MOTOR_DE_PRONOSTICOS_v1.0` y `PRODUCTO_PLAYPREDICT_v1.0`, y omitía varios que sí existen).
- `README.md` (raíz): el enlace a `docs/` en "Documentación funcional" apuntaba a la carpeta genérica; se actualizó para apuntar a `docs/README_DOCS.md`. No se encontraron más enlaces rotos en `CLAUDE.md`, `SESSION.md` ni `PROJECT_STATUS.md` (las menciones a rutas antiguas eran registros históricos de sesiones previas, no enlaces activos; se dejaron como constancia de lo ocurrido en su momento).
- `Audios relevamiento/` se mantiene fuera de `docs/` (es material fuente sin procesar) pero ahora está referenciada explícitamente en `docs/README_DOCS.md`, sección "Research".

Estructura final de `docs/`:
```
docs/
├── README_DOCS.md
├── arquitectura/
│   ├── MODELO_CONCEPTUAL_v1.0.md
│   ├── MODELO_DATOS_v1.0.md
│   ├── REGLAS_NEGOCIO_v1.0.md
│   ├── MODELO_CONCEPTUAL_PRONOSTICOS_v1.0.md
│   ├── TIPOS_PRONOSTICOS_v1.0.md
│   ├── MOTOR_PUNTUACION_v1.0.md
│   └── CONFIGURACION_COMPETENCIAS_v1.0.md
├── products/
│   ├── PANTALLAS_MVP_v1.0.md
│   ├── PLAN_IMPLEMENTACION_MVP_v1.0.md
│   └── ROADMAP_PRONOSTICOS_v1.0.md
└── business/
    ├── Etapa_1_28-07-2026_PlayPredict.pdf
    ├── Propuesta_Plataforma_Engagement_Deportivo_v1.pdf
    ├── Propuesta_Plataforma_de_Pronosticos_Deportiva_v2.pdf
    ├── Propuesta_Plataforma_de_Pronosticos_Deportiva_v3.pdf
    └── Propuesta_Plataforma_de_Pronosticos_Deportiva_v4.pdf
```

---

## Sprint 5 — Motor de Puntuación Configurable Básico

Objetivo: calcular automáticamente los puntos de los pronósticos de marcador ya existentes, con la puntuación configurable por Edición. Sin rankings, posiciones ni premios (quedan para sprints posteriores).

### Modelo

- `EditionScoringConfiguration` (`backend/Domain/Entities/EditionScoringConfiguration.cs`): `Id`, `EditionId` (único, relación 1 a 1 con `Edition`), `ExactScorePoints`, `CorrectOutcomePoints`, `IncorrectPoints`, `CreatedAtUtc`, `UpdatedAtUtc`. FK a `Edition` con `DeleteBehavior.Restrict`.
- `PredictionEvaluation` (`backend/Domain/Entities/PredictionEvaluation.cs`): `Id`, `PredictionId` (único, sin historial — se actualiza en el lugar al recalcular), `Points`, `EvaluationType`, `OfficialHomeScore`, `OfficialAwayScore`, `AppliedRuleValue`, `EvaluatedAtUtc`. FK a `Prediction` con `DeleteBehavior.Restrict`.
- Enum `EvaluationType` (`backend/Domain/Enums/EvaluationType.cs`): `ExactScore`, `CorrectOutcome`, `Incorrect`.
- Migración `AddScoringEngine` creada y aplicada (tablas `EditionScoringConfigurations`, `PredictionEvaluations`).

### Backend

- `backend/Services/PredictionEvaluationService.cs`: única responsabilidad — dado un `Match` ya con resultado oficial y `Status = Finished`, busca la configuración de puntuación de su Edición (vía `Round.EditionId`), evalúa cada `Prediction` del partido y **prepara** (crea o actualiza) su `PredictionEvaluation` en el `ChangeTracker`, sin llamar a `SaveChangesAsync` — el llamador persiste todo junto en una única transacción implícita. Prioridad: marcador exacto > resultado correcto > incorrecto (una sola categoría aplicada por pronóstico); el signo del resultado (`Math.Sign(local - visitante)`) determina "resultado correcto" (gana local / empate / gana visitante).
- `backend/Endpoints/MatchEndpoints.cs`, `PUT /api/matches/{id}/result`: además de guardar el resultado oficial, invoca `PredictionEvaluationService.PrepareEvaluationsForMatchAsync` antes del único `SaveChangesAsync` — el resultado oficial y todas las evaluaciones afectadas se guardan atómicamente. Si el resultado se vuelve a cargar (corrección), las evaluaciones existentes se actualizan en el lugar (no se duplican, por el índice único en `PredictionId`).
- `backend/Endpoints/EditionScoringConfigurationEndpoints.cs` (nuevo): `GET/PUT /api/editions/{editionId}/scoring-configuration`, ambos `RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin))`. El `PUT` valida enteros ≥ 0 por campo (400 con detalle si no) y Edición existente (404).
- `backend/Dtos/PredictionDtos.cs`: `PredictionDto` ahora incluye `Points`, `EvaluationType`, `EvaluationLabel` (castellano: "Marcador exacto" / "Resultado correcto" / "Incorrecto"), `OfficialHomeScore`, `OfficialAwayScore` — todos `null` si el partido todavía no fue evaluado. `GET /api/predictions/rounds/{roundId}` y `GET /api/predictions/me` arman esta información uniendo `Prediction` con su `PredictionEvaluation` (si existe); `POST`/`PUT` de pronósticos no la incluyen (nunca puede existir evaluación en el momento de crear/editar, porque eso solo es posible mientras el partido no está Finalizado).
- `backend/Dtos/ScoringDtos.cs` (nuevo): `EditionScoringConfigurationDto`, `UpdateEditionScoringConfigurationDto`.
- Configuración inicial automática:
  - `backend/Data/DataSeeder.cs`, `SeedEditionScoringConfigurationsAsync` (corre en todos los entornos, después de cualquier seed de datos): crea 6/3/0 para toda Edición que todavía no tenga configuración — cubre las Ediciones que ya existían antes de este Sprint.
  - `backend/Endpoints/EditionEndpoints.cs`, `POST /api/competitions/{competitionId}/editions`: ahora crea también la `EditionScoringConfiguration` (6/3/0) en el mismo `SaveChangesAsync` al dar de alta una Edición nueva.
- No se calculan puntos para partidos sin resultado oficial ni para partidos Cancelados o Suspendidos: la evaluación solo se dispara desde `/result`, que ya rechaza Cancelado y siempre deja el partido en `Finished`; nunca se ejecuta con el partido en otro estado.
- No hay edición manual de puntos: no existe ningún endpoint que permita escribir `Points` directamente.

### Frontend administrativo

- `frontend/src/pages/EditionScoringConfigurationPage.tsx` (nueva, ruta `/editions/:editionId/scoring-configuration`, envuelta en `RequireAdmin`): formulario con los 3 campos (marcador exacto / resultado correcto / resultado incorrecto), enteros no negativos (clamp en el cliente + validación del backend como autoridad final), mensaje de éxito, botón "Volver a Ediciones". Mismo estilo (`form-card`, `btn`, etc.) que el resto del panel.
- `frontend/src/pages/EditionsListPage.tsx`: agregado el botón "Configurar puntuación" junto a "Editar" en cada fila, visible solo si `user.roles` incluye `ADMIN`.

### Frontend del usuario (Pronósticos)

- `frontend/src/pages/PredictionsMatchesPage.tsx`: sin cambios en partidos no Finalizados (se mantiene el comportamiento del Sprint 4, sin mostrar puntos). Para partidos Finalizados, la columna "Mi pronóstico" ahora muestra, si el usuario pronosticó: "Mi pronóstico: X - Y", "Resultado oficial: X - Y", "Puntos obtenidos: N", "Motivo: <Marcador exacto|Resultado correcto|Incorrecto>" (todo tomado tal cual de la API, nunca calculado en el cliente). Si el usuario no pronosticó ese partido: "Sin pronóstico", sin inventar evaluación ni puntos. Cancelado sigue mostrando "—".
- `frontend/src/api/types.ts`: `Prediction` extendido con `points`, `evaluationType`, `evaluationLabel`, `officialHomeScore`, `officialAwayScore`; nuevo tipo `EditionScoringConfiguration`.
- `admin.css`: una clase nueva (`.prediction-result`) para el bloque de 4 líneas, mismo tamaño de fuente y color que el resto de los textos secundarios del panel. Sin rediseño.

### Pruebas realizadas (Edición "Clausura 2026", vía API con `curl` + verificación visual)

Configuración inicial 6/3/0 (verificada tras el seed).

| Caso | Pronóstico | Resultado oficial | Esperado | Obtenido |
|---|---|---|---|---|
| A | 2-1 | 2-1 | 6, Marcador exacto | ✅ 6, ExactScore |
| B | 1-0 | 3-1 | 3, Resultado correcto | ✅ 3, CorrectOutcome |
| C | 0-0 | 2-2 | 3, Resultado correcto | ✅ 3, CorrectOutcome |
| D | 1-2 | 2-1 | 0, Incorrecto | ✅ 0, Incorrect |

- **Caso E** (recálculo por cambio de configuración): se cambió la configuración a 10/4/1 y se volvió a cargar el mismo resultado oficial en los 3 partidos de los casos A-C → recalcularon a 10, 4 y 4 puntos respectivamente, usando los nuevos valores. El partido del Caso D (de otra Edición, con su propia configuración sin modificar) siguió en 0 — confirma que la configuración es por Edición, no global.
- **Caso F** (corrección de resultado oficial): se corrigió el resultado del Caso B de 3-1 a 1-1 → la evaluación cambió de "Resultado correcto" (4 pts) a "Incorrecto" (1 pt) **en el mismo registro** (`PredictionEvaluations` siguió teniendo exactamente 4 filas en total, una por pronóstico, sin duplicados).
- **Caso G** (usuario sin pronóstico): un usuario registrado sin pronósticos, al consultar `GET /api/predictions/rounds/{roundId}`, recibe `myPrediction: null` en todos los partidos, incluidos los Finalizados — sin evaluación inventada. Confirmado también visualmente ("Sin pronóstico" en las 3 filas).
- **Caso H** (autorización): un usuario sin rol ADMIN recibe 403 tanto en `GET` como en `PUT /api/editions/{id}/scoring-configuration`.
- **Caso I** (validación): `PUT` con valores negativos → 400 con el detalle de los 3 campos; la configuración no se modificó.
- Verificado en el navegador: pantalla "Configurar puntuación" carga y muestra los valores reales de la base; botón visible solo para ADMIN en la lista de Ediciones; pantalla de Pronósticos muestra exactamente "Mi pronóstico / Resultado oficial / Puntos obtenidos / Motivo" para partidos Finalizados, y "Sin pronóstico" cuando corresponde. Consola del navegador sin errores.
- Swagger (`/swagger`) expone correctamente `GET/PUT /api/editions/{editionId}/scoring-configuration` bajo "Edition Scoring Configuration", con su DTO.
- `dotnet build`: OK. `npm run build`: OK. `docker compose up -d --build`: 3 servicios healthy (hubo un timeout transitorio del healthcheck del backend durante el primer arranque post-migración; se resolvió solo, sin intervención de código). Migración `AddScoringEngine` aplicada y verificada directamente en PostgreSQL (2 filas en `EditionScoringConfigurations`, una por Edición existente, ambas en 6/3/0 tras el seed). Logs de `backend`/`frontend`/`db` revisados: sin errores ni excepciones.
- Datos de prueba (pronósticos, evaluaciones, cambios de resultado/configuración, usuario de prueba) revertidos al final: `Predictions` y `PredictionEvaluations` en 0 filas, ambas configuraciones de puntuación de vuelta en 6/3/0, los 6 partidos de demostración de vuelta en estado Programado sin resultado.

### Archivos modificados/creados

Backend: `Domain/Entities/EditionScoringConfiguration.cs`, `Domain/Entities/PredictionEvaluation.cs`, `Domain/Enums/EvaluationType.cs`, `Data/Configurations/EditionScoringConfigurationConfiguration.cs`, `Data/Configurations/PredictionEvaluationConfiguration.cs`, `Dtos/ScoringDtos.cs`, `Endpoints/EditionScoringConfigurationEndpoints.cs`, `Services/PredictionEvaluationService.cs`, `Migrations/20260731010056_AddScoringEngine.cs` y `.Designer.cs` (nuevos); `Data/PlayPredictDbContext.cs`, `Data/DataSeeder.cs`, `Dtos/PredictionDtos.cs`, `Endpoints/EditionEndpoints.cs`, `Endpoints/MatchEndpoints.cs`, `Endpoints/PredictionEndpoints.cs`, `Migrations/PlayPredictDbContextModelSnapshot.cs`, `Program.cs` (modificados).

Frontend: `pages/EditionScoringConfigurationPage.tsx` (nuevo); `api/types.ts`, `App.tsx`, `pages/EditionsListPage.tsx`, `pages/PredictionsMatchesPage.tsx`, `components/admin.css` (modificados).

### Verificación visual final (sesión posterior, antes de aprobar el commit)

Con el Sprint 5 ya aprobado técnicamente, se pidió una comprobación visual adicional en el navegador (no solo por API) antes de commitear. Se hizo íntegramente en la UI real (pantalla "Configurar puntuación" y modal "Resultado Oficial" del panel de Fixture):

1. Edición "Clausura 2026" configurada en 10 / 4 / 1 desde `/editions/7/scoring-configuration` → mensaje "Configuración guardada correctamente.", valores confirmados al recargar.
2. Pronóstico 2-1 cargado en "Equipo A vs Equipo B" desde la pantalla de Pronósticos.
3. Resultado oficial 2-1 → la pantalla de Pronósticos mostró "Puntos obtenidos: 10 / Motivo: Marcador exacto".
4. Resultado oficial corregido a 3-1 (mismo partido, vía "Cargar resultado" de nuevo) → cambió a "Puntos obtenidos: 4 / Motivo: Resultado correcto".
5. Resultado oficial corregido a 1-2 → cambió a "Puntos obtenidos: 1 / Motivo: Incorrecto".
6. Verificado directamente en PostgreSQL después del paso 5: `PredictionEvaluations` tenía exactamente **1 fila** (mismo `Id`, actualizada tres veces) y `Predictions` exactamente 1 fila — confirma que el recálculo actualiza la evaluación existente y nunca duplica.

Después de la verificación se revirtió todo: `Predictions` y `PredictionEvaluations` en 0 filas, la configuración de la Edición 7 de vuelta en 6/3/0, y el partido usado de vuelta en Programado sin resultado (mismo estado que el resto del fixture de demostración).

---

## Sprint 4 — Sistema de Pronósticos

Objetivo: construir toda la infraestructura para que un usuario pueda pronosticar partidos, sin calcular puntos, sin tablas, sin rankings ni posiciones (eso queda para Sprint 5 en adelante).

### Modelo

- Nueva entidad `Prediction` (`backend/Domain/Entities/Prediction.cs`): `Id`, `MatchId`, `UserId`, `PredictedHomeScore`, `PredictedAwayScore`, `CreatedAtUtc`, `UpdatedAtUtc`.
- Configuración EF (`backend/Data/Configurations/PredictionConfiguration.cs`): índice único `(UserId, MatchId)` — un usuario solo puede tener un pronóstico por partido; FKs a `Matches` y `Users` con `DeleteBehavior.Restrict`.
- Migración `AddPredictions` creada y aplicada (tabla `Predictions`).

### Backend

- DTOs (`backend/Dtos/PredictionDtos.cs`): `PredictionDto`, `CreatePredictionDto`, `UpdatePredictionDto`, `MatchWithPredictionDto` (partido + resultado oficial + pronóstico propio, si existe).
- Endpoints (`backend/Endpoints/PredictionEndpoints.cs`, grupo `/api/predictions`, todos con `RequireAuthorization()`):
  - `GET /api/predictions/rounds/{roundId}` — partidos de la Fecha con el pronóstico propio embebido (o `null`). Es el endpoint que alimenta la pantalla principal del frontend.
  - `GET /api/predictions/me` — todos los pronósticos del usuario autenticado.
  - `POST /api/predictions` — crea un pronóstico (`matchId`, `predictedHomeScore`, `predictedAwayScore`).
  - `PUT /api/predictions/{id}` — modifica un pronóstico propio.
- Reglas implementadas:
  - Un usuario, un pronóstico por partido: índice único en base + verificación explícita antes de insertar (409 si ya existe).
  - Solo se puede crear o modificar un pronóstico si el partido está Programado **y** su horario de inicio todavía no llegó (`CanCreateOrEditPrediction`, corregido — ver "Fix: regla de cierre de Pronósticos" más abajo). Bloqueado (400) en cualquier otro caso, tanto al crear como al editar.
  - Un usuario no puede modificar el pronóstico de otro usuario (403).
  - Goles pronosticados no pueden ser negativos (400 con detalle de campo).
  - Sin autenticación → 401 en todos los endpoints.
  - No se calculan puntos ni se compara contra el resultado oficial en ningún punto del código.

### Frontend

- Nueva entrada "Pronósticos" en el menú (`Layout.tsx`), visible para cualquier usuario autenticado.
- 4 pantallas nuevas (`frontend/src/pages/`), navegación exactamente igual a la del panel administrativo pero de solo lectura hasta llegar a los partidos:
  - `PredictionsCompetitionsPage` (`/predictions`) — lista de Competencias.
  - `PredictionsEditionsPage` (`/predictions/competitions/:competitionId/editions`) — lista de Ediciones.
  - `PredictionsRoundsPage` (`/predictions/editions/:editionId/rounds`) — lista de Fechas.
  - `PredictionsMatchesPage` (`/predictions/rounds/:roundId`) — pantalla única de carga: por cada partido muestra equipos, fecha, hora, resultado oficial (si existe) y un bloque "Mi pronóstico" con inputs Local/Visitante y botón "Guardar pronóstico" (o "Actualizar pronóstico" si ya existe uno), todo guardado partido por partido sin salir de la pantalla.
  - Estados especiales en la columna "Mi pronóstico": partido Finalizado → texto fijo "Pronóstico cerrado" (+ el valor pronosticado entre paréntesis si existía); partido Cancelado → "—" (no se permite pronosticar).
- Tipos nuevos en `frontend/src/api/types.ts`: `Prediction`, `MatchWithPrediction`.
- CSS: 3 clases puntuales agregadas a `admin.css` (`.prediction-row`, `.prediction-row__inputs`, `.prediction-row__input`, `.prediction-row__saved`) reutilizando los mismos colores y tipografía ya definidos en el archivo. Sin cambios de layout, colores ni rediseño de pantallas existentes.

### Pruebas realizadas

- **Backend (vía `curl`)**: crear pronóstico (201); duplicado sobre el mismo partido (409); editar pronóstico propio (200, persiste); `GET /me` y `GET /rounds/{roundId}` reflejan el cambio; crear/editar sobre partido Finalizado (400 en ambos casos); crear sobre partido Cancelado (400); goles negativos (400); sin token (401 en todos los endpoints); usuario distinto intentando editar un pronóstico ajeno (403).
- **Frontend (navegador)**: navegación completa Competencias → Ediciones → Fechas → Partidos bajo "Pronósticos"; pantalla de carga renderiza correctamente con el estilo existente; guardar un pronóstico nuevo muestra "Pronóstico guardado correctamente." y cambia el botón a "Actualizar pronóstico"; recargar la página conserva el valor cargado; partido Finalizado muestra "Pronóstico cerrado (X - Y)" sin inputs; partido Cancelado muestra "—" sin inputs ni botón.
  - Nota técnica: la herramienta de automatización de navegador de esta sesión no lograba entregar clics/tecleo reales a la página (se verificó que tampoco funcionaba en un input preexistente de "Mi perfil", ajeno a este Sprint). Se validó el flujo disparando los mismos eventos DOM nativos que React escucha y confirmando las peticiones de red resultantes (201 al guardar, datos persistidos al recargar) — el comportamiento verificado es el mismo que produciría una interacción real de mouse/teclado. Se sugiere una prueba manual rápida antes de aprobar, dado que no se pudo confirmar con clics 100% "físicos" en esta sesión.
- Swagger (`/swagger`) expone correctamente los 4 endpoints nuevos bajo "Predictions", con sus DTOs.
- `dotnet build`: OK. `npm run build`: OK. `docker compose up -d --build`: 3 servicios healthy. Logs de `backend`/`frontend` sin errores ni excepciones.
- Datos de prueba (usuarios, pronósticos, estados de partido) usados durante la validación fueron revertidos al final; el fixture quedó en su estado limpio (6 partidos Programados, sin pronósticos).

### Archivos modificados/creados

Backend: `Domain/Entities/Prediction.cs` (nuevo), `Data/Configurations/PredictionConfiguration.cs` (nuevo), `Dtos/PredictionDtos.cs` (nuevo), `Endpoints/PredictionEndpoints.cs` (nuevo), `Migrations/20260730172829_AddPredictions.cs` y `.Designer.cs` (nuevos), `Data/PlayPredictDbContext.cs`, `Migrations/PlayPredictDbContextModelSnapshot.cs`, `Program.cs` (modificados).

Frontend: `pages/PredictionsCompetitionsPage.tsx`, `pages/PredictionsEditionsPage.tsx`, `pages/PredictionsRoundsPage.tsx`, `pages/PredictionsMatchesPage.tsx` (nuevos), `api/types.ts`, `App.tsx`, `components/Layout.tsx`, `components/admin.css` (modificados).

### Ajuste de UX posterior (antes de aprobar el sprint)

Pedido por el usuario tras probar la pantalla manualmente. Sin tocar lógica de negocio ni backend:

- `PredictionsMatchesPage.tsx`: los inputs de goles pasaron de `type="number"` (con `min={0}`, que forzaba a mostrar/asumir "0") a `type="text"` con `inputMode="numeric"`, `pattern="[0-9]*"` y `placeholder="-"`; el `onChange` sanitiza con `replace(/\D/g, '')`, descartando letras, signos y decimales antes de guardarlos en el estado. Sin pronóstico previo, el campo queda vacío (ya era así en el estado, pero el tipo `number` lo hacía sentir como "0"); con pronóstico existente, se sigue precargando el valor real.
- Se agregó `setTimeout(..., 4000)` para limpiar `savedMessage` tras guardar — antes el mensaje "Pronóstico guardado correctamente." quedaba fijo indefinidamente (no desaparecía).
- El botón Guardar/Actualizar no se tocó.
- `admin.css`: agregadas `.prediction-row__separator` (guion centrado, ancho fijo) y `.prediction-row__message` (contenedor de error/éxito con `min-height` reservado) para que la fila no cambie de alto al aparecer/desaparecer el mensaje; `.prediction-row__input` ahora incluye `box-sizing: border-box` y `text-align: center` para que ambos campos midan exactamente lo mismo.
- Verificado en el navegador: campos vacíos con placeholder "-" cuando no hay pronóstico; solo dígitos aceptados (probado enviando `"2a-3.5b"` → queda `"235"`); valores reales precargados cuando existe pronóstico; alto de fila idéntico (79.2px) antes y después de mostrar el mensaje de éxito; mensaje desaparece solo a los ~4 segundos; persistencia confirmada tras recargar.

Sprint 4 completo (entidad, migración, endpoints, pantallas, reglas originales y este ajuste de UX) commiteado en `1fd819f` — "feat: implement match predictions".

---

## Fix: regla de cierre de Pronósticos (sobre el commit `1fd819f`)

### Diagnóstico

El informe final del Sprint 4 describía la regla como "solo editable/creable mientras el partido esté Programado, En juego o Suspendido; bloqueado para Finalizado y Cancelado". Se verificó que **esa descripción era fiel al código tal como quedó commiteado** — no fue un error de redacción del informe. El código coincidía con la especificación original del Sprint 4, pero esa especificación quedó superada por la regla definitiva acordada después:

> Un pronóstico solamente puede crearse o modificarse cuando el Partido está en estado Programado **y** la fecha/hora actual UTC es anterior a la fecha/hora de inicio del Partido. Cualquier otro caso (Programado con horario ya pasado, En juego, Suspendido, Finalizado, Cancelado) debe quedar bloqueado.

Evidencia de la causa: en `backend/Endpoints/PredictionEndpoints.cs`, la función `IsOpenForPrediction(MatchStatus status)` (usada en `POST /api/predictions` y `PUT /api/predictions/{id}`) devolvía `true` para `Scheduled`, `InProgress` y `Suspended` sin comparar contra `DateTime.UtcNow`/`StartsAtUtc`. Además, `MatchWithPredictionDto` no exponía ningún indicador tipo `canPredict`: el frontend (`PredictionsMatchesPage.tsx`) calculaba la editabilidad por su cuenta con una función local `canPredict(status)` que replicaba la misma regla incompleta, en vez de depender de una decisión del backend.

**Conclusión: el problema estaba en el código, no solo en el informe.**

### Corrección

- `backend/Endpoints/PredictionEndpoints.cs`: `IsOpenForPrediction` reemplazada por `CanCreateOrEditPrediction(Match match) => match.Status == MatchStatus.Scheduled && DateTime.UtcNow < match.StartsAtUtc`, usada en `POST`, `PUT` y para calcular el nuevo campo del DTO.
- `backend/Dtos/PredictionDtos.cs`: `MatchWithPredictionDto` ahora incluye `bool CanPredict`, calculado con la misma función — el backend es la única fuente de verdad.
- `frontend/src/api/types.ts`: `MatchWithPrediction` incluye `canPredict: boolean`.
- `frontend/src/pages/PredictionsMatchesPage.tsx`: eliminada la función local `canPredict(status)`; ahora usa directamente `m.canPredict` recibido del backend. El texto "Pronóstico cerrado (X - Y)" ahora se muestra para **todos** los casos bloqueados salvo Cancelado (que sigue mostrando "—", sin cambios en ese criterio visual).

### Pruebas realizadas (los 6 casos, por API)

Preparados vía SQL directa sobre partidos de prueba (Scheduled+futuro, Scheduled+pasado, InProgress, Suspended, Finished, Cancelled):

| Caso | Estado / horario | Crear (POST) | Editar (PUT) | `canPredict` en GET |
|---|---|---|---|---|
| A | Programado + horario futuro | 201 permitido | 200 permitido | `true` |
| B | Programado + horario pasado | 400 bloqueado | 400 bloqueado | `false` |
| C | En juego | 400 bloqueado | 400 bloqueado | `false` |
| D | Suspendido | 400 bloqueado | 400 bloqueado | `false` |
| E | Finalizado | 400 bloqueado | 400 bloqueado | `false` |
| F | Cancelado | 400 bloqueado | 400 bloqueado | `false` |

Verificado además visualmente en el navegador (Fechas de Liga Profesional y Copa Libertadores): fila editable solo en el caso A; el resto muestra "Pronóstico cerrado (X - Y)" o "—" (Cancelado) según corresponda, con el valor del pronóstico ya cargado visible aunque no editable.

- `dotnet build`: OK. `npm run build`: OK. Logs de `backend`/`frontend` revisados tras el fix: sin errores ni excepciones.
- Datos de prueba (cambios de estado/horario en partidos 15-20, pronósticos de prueba) revertidos al final; el fixture quedó en su estado limpio (6 partidos Programados con horario futuro, sin pronósticos).

---

## Nota sobre esta actualización

Esta sesión comenzó tras un corte por error 529 en la sesión anterior. Al verificar el estado real (protocolo "Inicio Sesion"), se encontró que el Sprint 3 completo ya estaba implementado en el árbol de trabajo (compilando y corriendo en los contenedores), pero sin commit y sin reflejarse en `SESSION.md`/`PROJECT_STATUS.md`/`PLAN_DE_TRABAJO.md`. Con aprobación explícita del usuario, se dio el Sprint 3 por finalizado, se documentó, y a continuación se ejecutó el Sprint 3.5 solicitado.

---

## Sprint 3 — Usuarios y Autenticación

### Backend

- Entidades de dominio nuevas (`backend/Domain/Entities/`): `Company`, `Role`, `User`, `UserRole`. Constante `RoleNames` (`backend/Domain/Constants/RoleNames.cs`) con los roles `ADMIN` y `USER`.
- Configuraciones EF Core (`backend/Data/Configurations/`): `CompanyConfiguration`, `RoleConfiguration`, `UserConfiguration`, `UserRoleConfiguration`.
- Migración `AddUsersAndAuthentication` aplicada (tablas `Companies`, `Roles`, `Users`, `UserRoles`).
- Autenticación JWT: `backend/Services/JwtTokenService.cs` genera el token (claims de id, empresa, nombre, email y rol); `Program.cs` registra `AddAuthentication().AddJwtBearer(...)` y `AddAuthorization()`; configuración en `appsettings.json` (sección `Jwt`: Key/Issuer/Audience/ExpiresMinutes — clave de desarrollo, debe reemplazarse antes de producción).
- Endpoints (`backend/Endpoints/`):
  - `AuthEndpoints`: `POST /api/auth/register`, `POST /api/auth/login` (devuelven token + datos de usuario).
  - `UserEndpoints`: `GET /api/users/me`, `PUT /api/users/me` (perfil propio).
  - `AdminUserEndpoints`: `GET /api/admin/users`, `PUT /api/admin/users/{id}` (activar/desactivar), protegidos con rol `ADMIN`.
- Contraseñas hasheadas con `PasswordHasher<User>` (Microsoft.AspNetCore.Identity.Core).
- Seed (`backend/Data/DataSeeder.cs`): `SeedCoreDataAsync` (corre en todos los entornos) crea la empresa "PlayPredict" y los roles ADMIN/USER si no existen — prerrequisito para que Registro/Login funcionen. `SeedAdminUserAsync` (solo Development) crea el usuario `admin@playpredict.local` / `admin123` con rol ADMIN.

### Frontend

- `frontend/src/auth/`: `AuthContext.tsx` (estado de sesión, `login`/`logout`, `RequireAuth`, `RequireAdmin`), `token.ts` (persistencia del JWT en `localStorage`).
- Páginas nuevas (`frontend/src/pages/`): `LoginPage`, `RegisterPage`, `ProfilePage`, `AdminUsersPage`.
- `App.tsx`: rutas `/login` y `/register` públicas; el resto de la aplicación envuelta en `RequireAuth`; `/admin/users` envuelta además en `RequireAdmin`.
- `Layout.tsx`: muestra el nombre del usuario logueado, link a "Usuarios" solo si tiene rol ADMIN, y botón "Salir" (logout).
- `client.ts`: agrega el header `Authorization: Bearer <token>` a las peticiones y maneja 401 (limpia el token).

### Verificado en esta sesión

- `dotnet build`: OK. `npm run build`: OK.
- Login con usuario admin (`POST /api/auth/login`) → 200, token válido.
- `GET /api/users/me` sin token → 401. Con token → 200.
- `GET /api/admin/users` con token ADMIN → 200. Con token de un usuario USER recién registrado → 403.
- Registro de usuario nuevo (`POST /api/auth/register`) → 200, asigna rol USER automáticamente (usuario de prueba eliminado después de la verificación).
- En el navegador: login, sesión persistida entre recargas, ruta protegida (`/competitions`) redirige a `/login` sin sesión, logout funciona, panel "Usuarios" visible solo para ADMIN.

---

## Sprint 3.5 — Limpieza funcional pre-Pronósticos

Sin nuevas funcionalidades, sin cambios al modelo de datos, sin nuevas migraciones, sin commit.

### 1. Datos de demostración

- Eliminados de la base de datos: competencia técnica "Competancia Amigos 1" (con su edición "edicion 1", fecha "fecha 1" y partido de prueba CELP–GELP), y la edición/fecha/partido de prueba manual que existía bajo "Copa Libertadores" ("Apertura 2026" / "Fecha 1 - Apertura", 1 partido finalizado con resultado 2-1).
- `backend/Data/DataSeeder.cs` reestructurado: el seed ahora crea **dos** competencias de forma idempotente (antes solo creaba Liga Profesional):
  - Liga Profesional → Clausura 2026 → Fecha 1 → 3 partidos (Equipo A-F), sin cambios respecto al Sprint 2.
  - Copa Libertadores → Fase de Grupos 2026 → Fecha 1 → 3 partidos (Equipo G-L), nuevo.
  - Cada competencia se verifica por nombre antes de insertar (no duplica en reinicios sucesivos).
- Corregido el deporte "Futbol" (sin tilde) → "Fútbol" en Copa Libertadores.
- Usuario de prueba `test.sprint35@playpredict.local` (creado y eliminado en esta misma sesión para verificar el registro) — no queda rastro.
- **Nota para el usuario**: quedan dos usuarios de prueba preexistentes de la sesión anterior (`juan.perez@example.com`, `maria.lopez@example.com`, rol USER) que no se tocaron porque no estaban en el alcance explícito de esta limpieza (la consigna hablaba de datos de fixture, no de usuarios). Si se quiere, se pueden eliminar en una futura sesión con aprobación.

### 2. Textos visibles

- La mayoría de la interfaz ya estaba en castellano correcto (verificado archivo por archivo: títulos, breadcrumbs, botones, mensajes de error/éxito, formularios).
- Único problema encontrado: los estados de Edición y Partido se mostraban en inglés crudo (valores del enum) tanto en las tablas (badges) como en los combos de edición. Corregido agregando `EDITION_STATUS_LABELS` y `MATCH_STATUS_LABELS` en `frontend/src/api/types.ts` (mapeo de visualización; los valores enviados a la API siguen siendo los del enum en inglés, sin tocar el modelo de datos):
  - Edición: Draft → Borrador, Active → Activa, Finished → Finalizada, Cancelled → Cancelada.
  - Partido: Scheduled → Programado, InProgress → En curso, Finished → Finalizado, Suspended → Suspendido, Cancelled → Cancelado.
- Archivos actualizados: `EditionsListPage.tsx`, `EditionFormPage.tsx`, `MatchesListPage.tsx`, `MatchFormPage.tsx`.
- No se encontraron literales "Competancia", "Futbol", "Draft", "Round", "Edition" ni "Competition" como texto visible al usuario (esos términos solo aparecen como nombres de tipos/rutas en el código, no en la UI).

### 3. Navegación

- Revisada de punta a punta: Competencias → Ediciones → Fechas → Partidos, con breadcrumb "← [nivel anterior]" en cada pantalla de lista y de formulario. Sin cambios de código necesarios (ya cumplía).

### 4. Consistencia visual mínima

- Revisados títulos, botones (`btn-primary`/`btn-secondary`), mensajes de éxito ("... guardado/a correctamente") y de error ("No se pudo cargar...", "Ocurrió un error inesperado al..."): ya eran consistentes entre pantallas. Único ajuste fue el de los nombres de estado (punto 2).

### 5. Autenticación (solo verificación, sin tocar JWT ni roles)

- Login con usuario y contraseña correctos → 200 + token.
- Login con contraseña incorrecta → 401.
- Ruta protegida sin sesión → redirige a `/login`.
- Sesión persiste entre recargas (token en `localStorage`, verificado en navegador).
- Logout limpia la sesión y redirige.
- Acceso ADMIN a `/api/admin/users` y a la pantalla "Usuarios" → funciona.
- Restricción USER: token de usuario sin rol ADMIN contra `/api/admin/users` → 403 (verificado por API).

### Validaciones ejecutadas

- `dotnet build` (backend): OK, sin errores.
- `npm run build` (frontend): OK, sin errores.
- `docker compose up -d --build`: los 3 servicios levantan `healthy`/`Up`.
- Datos de demostración verificados directamente en PostgreSQL tras el rebuild (persisten correctamente).
- Logs de `backend` y `frontend` revisados tras el rebuild: sin errores ni excepciones.
- Navegación, login, logout y estados verificados visualmente en el navegador.
- `git status --short`: en el momento de esta validación, cambios pendientes de Sprint 3 + Sprint 3.5 sin commit (se commitearon junto con el fix posterior, ver más abajo).

---

## Fix post-Sprint 3.5 — Edición de Partido rompía el Resultado Oficial

**Reportado por el usuario** durante la revisión visual del Sprint 3.5: un partido Finalizado con resultado cargado, al editarse (aunque solo se cambiaran participantes/horario) volvía a Programado; los goles seguían en la base de datos pero el listado dejaba de mostrarlos porque dependía del estado.

### Causa exacta

- **Frontend** (`MatchFormPage.tsx`): el estado del formulario `status` se inicializaba en `'Scheduled'` y solo se sincronizaba con el valor real del partido cuando este **no** era `Finished` (a propósito, porque "Finalizado" no es una opción seleccionable). Para un partido Finalizado, `status` nunca se actualizaba y quedaba en `'Scheduled'`. Al guardar, el payload de `PUT /api/matches/{id}` siempre incluía ese `status`, enviando accidentalmente `"Scheduled"`.
- **Backend** (`MatchEndpoints.cs`, función `ValidateMatch`): el endpoint general aceptaba cualquier `status` válido recibido en el DTO y lo aplicaba tal cual (solo rechazaba explícitamente el valor `"Finished"`), sin ninguna protección para un partido que **ya** estuviera Finalizado. Combinado con el bug del frontend, esto sobrescribía el estado a `Scheduled` sin tocar los goles (que ese endpoint nunca gestiona), produciendo exactamente el síntoma reportado.

### Corrección

- **Backend** (`backend/Endpoints/MatchEndpoints.cs`): en `ValidateMatch`, si el partido ya está `Finished`, se ignora cualquier `status` recibido y se devuelve siempre `Finished` — el endpoint general nunca puede sacar a un partido de Finalizado, sin importar qué envíe el cliente. El resultado oficial (`HomeGoals`/`AwayGoals`) solo se modifica en `PUT /api/matches/{id}/result`, que no fue tocado.
- **Frontend** (`frontend/src/pages/MatchFormPage.tsx`):
  - Si el partido es Finalizado, el campo Estado se muestra como texto fijo "Finalizado" (no editable), en vez de un `<select>`.
  - Al guardar un partido Finalizado, el payload **no** incluye `status`.
  - Tras guardar correctamente (alta o edición), se navega automáticamente a la lista de Partidos de la Fecha, mostrando "Partido guardado correctamente." (antes se quedaba en el formulario).
  - Se agregó el botón "Volver a Partidos" junto a "Guardar", que navega sin guardar (además del enlace superior "← Partidos" ya existente).

### Pruebas realizadas

- **Caso A (Programado)**: editar participantes/horario de un partido Programado y guardar → vuelve a la lista, muestra "Partido guardado correctamente.", el estado sigue Programado. Verificado por API y en el navegador.
- **Caso B (Finalizado)** — reproduce exactamente el escenario reportado: cargar resultado 1-3 → Finalizado y "1 - 3" visibles → Editar Partido (estado se ve como "Finalizado", no editable) → cambiar solo horario → Guardar → vuelve automáticamente a la lista → sigue Finalizado → sigue mostrando "1 - 3" → al abrir "Cargar resultado" de nuevo, los goles 1 y 3 siguen cargados. Verificado por API (incluyendo un intento explícito de forzar `status: "Scheduled"` sobre un partido Finalizado, que el backend ahora ignora) y en el navegador.
- **Caso C (Cancelar)**: modificar un campo sin guardar y presionar "Volver a Partidos" → no se persiste ningún cambio. Verificado en el navegador.
- Validaciones no afectadas: intentar poner `status: "Finished"` manualmente vía `PUT /api/matches/{id}` en un partido no Finalizado → sigue rechazado (400); cambiar a Suspendido/otros estados válidos en un partido no Finalizado → sigue funcionando.
- `dotnet build`: OK. `npm run build`: OK. `docker compose ps`: 3 servicios healthy. Consola del navegador sin errores. Logs de `backend`/`frontend` sin errores ni excepciones.
- Datos de demostración usados durante estas pruebas (nombres/fechas/resultados de prueba) se revirtieron al final, dejando el seed de Liga Profesional y Copa Libertadores en su estado limpio original (6 partidos Programados).

### Commit y push

Sprint 3 + Sprint 3.5 + este fix se aprobaron y commitearon juntos en un único commit: `df43594` — "feat: add authentication and complete pre-predictions cleanup" (44 archivos, 2195 inserciones, 84 eliminaciones), pusheado a `origin/main`. Verificado sin `.env`, `bin/`, `obj/`, `node_modules/` ni `dist/` incluidos.

---

## Sprint 2 — Módulo base del fixture administrable

### Backend

- Entidades de dominio (`backend/Domain/Entities/`): `Competition`, `Edition`, `Round`, `Match`. Enums (`backend/Domain/Enums/`): `EditionStatus` (Draft, Active, Finished, Cancelled), `MatchStatus` (Scheduled, InProgress, Finished, Suspended, Cancelled).
- Configuraciones EF Core separadas por entidad (`backend/Data/Configurations/`, `IEntityTypeConfiguration<T>`): claves primarias, claves foráneas con `DeleteBehavior.Restrict` (sin borrado físico en cascada), longitudes máximas, índices (`Competition.Name`; `Edition(CompetitionId, Name)` único; `Round(EditionId, Order)` único; `Match.RoundId`, `Match.StartsAtUtc`).
- `PlayPredictDbContext` actualizado con los 4 `DbSet` y `ApplyConfigurationsFromAssembly`.
- DTOs de lectura/creación/actualización (`backend/Dtos/`) para las 4 entidades, más `MatchResultDto` para el resultado oficial.
- Validaciones explícitas en los endpoints (nombre/participantes obligatorios, longitudes máximas, fechas coherentes, estado válido, goles no negativos, orden de Fecha único con manejo de conflicto 409).
- Endpoints REST (`backend/Endpoints/`, minimal API) — ver detalle más abajo.
- Migración inicial `InitialFixtureSchema` creada y aplicada a PostgreSQL 18 (tablas `Competitions`, `Editions`, `Rounds`, `Matches`). El backend aplica migraciones pendientes automáticamente al iniciar (`db.Database.MigrateAsync()`).
- Seed de desarrollo idempotente (`backend/Data/DataSeeder.cs`), ejecutado solo en `Development`: Competencia "Liga Profesional" → Edición "Clausura 2026" → Fecha "Fecha 1" → 3 partidos programados. Verifica existencia por nombre antes de insertar; no duplica datos en reinicios sucesivos (verificado).
- Regla `GolesLocal`/`GolesVisitante` solo informables para un Partido Finalizado: el estado `Finished` únicamente puede establecerse vía `PUT /api/matches/{id}/result`; intentarlo desde `PUT /api/matches/{id}` devuelve 400.
- Punto de extensión documentado (comentario) en el endpoint de resultado para el futuro recálculo de Pronósticos/Rankings — sin implementar todavía.
- Nota: se agregó `GET /api/rounds/{id}` (no estaba en la lista original de endpoints) porque la pantalla de edición de Fecha lo necesita para cargar los datos actuales, igual que existe para Competencias, Ediciones y Partidos.

### Endpoints disponibles

```
GET    /api/competitions
GET    /api/competitions/{id}
POST   /api/competitions
PUT    /api/competitions/{id}

GET    /api/competitions/{competitionId}/editions
GET    /api/editions/{id}
POST   /api/competitions/{competitionId}/editions
PUT    /api/editions/{id}

GET    /api/editions/{editionId}/rounds
GET    /api/rounds/{id}
POST   /api/editions/{editionId}/rounds
PUT    /api/rounds/{id}

GET    /api/rounds/{roundId}/matches
GET    /api/matches/{id}
POST   /api/rounds/{roundId}/matches
PUT    /api/matches/{id}
PUT    /api/matches/{id}/result
```

### Frontend

- Panel administrativo con React Router (`frontend/src/pages/`, `frontend/src/components/`): listas y formularios de alta/edición para Competencias, Ediciones, Fechas y Partidos, más modal de carga de Resultado Oficial.
- Cliente API centralizado (`frontend/src/api/client.ts`) con manejo de errores de validación (400) y mensajes claros por campo.
- Estados de carga, mensajes de error y confirmación "guardado correctamente" en cada formulario.
- Navegación jerárquica: Competencias → Ediciones → Fechas → Partidos, con breadcrumbs de vuelta.
- Responsive básico (tablas con scroll horizontal, formularios en columna en pantallas angostas).
- Se reemplazó la pantalla inicial de estado del backend (Sprint 1) por el panel administrativo; `/` redirige a `/competitions`.
- Dependencia agregada: `react-router-dom` (última versión, 7.18.2). `npm audit` reporta una vulnerabilidad alta restante específica de "RSC Mode" (Server Components/Server Actions); no aplica a este proyecto porque el frontend es una SPA 100% cliente sin SSR ni RSC.

### Datos de demostración

Seed automático en `Development` (ya descrito arriba). Verificado tras reinicio del backend: no se duplican registros.

---

## Ajuste posterior al Sprint 1 (cierre de infraestructura)

- PostgreSQL actualizado de `postgres:16-alpine` a `postgres:18-alpine` en `docker-compose.yml`.
- Volumen de datos reconfigurado: a partir de PostgreSQL 18 la imagen oficial versiona el PGDATA internamente (`/var/lib/postgresql/<major>/docker`) y declara el `VOLUME` en `/var/lib/postgresql`. Se cambió el mount de `playpredict_db_data` de `/var/lib/postgresql/data` a `/var/lib/postgresql`.
- El volumen anterior (creado con PostgreSQL 16, sin datos de negocio) se eliminó con `docker compose down -v` antes de recrear el entorno.
- Se confirmó que `backend/Program.cs` no contenía código de plantilla (`WeatherForecast`, `summaries`, endpoints/records de ejemplo); ya estaba limpio desde el Sprint 1.
- Se inicializó el repositorio Git en la raíz (`git init`) y se fijó la rama principal como `main` (`git branch -M main`). No se realizó ningún commit ni se conectó un remoto.
- Se revisó `.gitignore`: cubre correctamente `.env`, `bin/`, `obj/`, `node_modules/`, `dist/` y archivos temporales de IDE.

---

## Qué se creó (Sprint 1)

- **Backend** (`backend/`): ASP.NET Core Web API (.NET 10), estilo minimal API.
  - Swagger/OpenAPI (Swashbuckle) habilitado en `/swagger`.
  - CORS habilitado para `http://localhost:5175` y `http://127.0.0.1:5175`.
  - `GET /api/health` → `{ "status": "ok" }`.
  - `GET /api/system/info` → `{ "application": "PlayPredict", "status": "running", "version": "0.1.0" }`.
  - Entity Framework Core + Npgsql configurado con `PlayPredictDbContext`.
  - `Dockerfile.dev` con hot reload (`dotnet watch run`).
- **Frontend** (`frontend/`): React + Vite + TypeScript. `Dockerfile.dev` con `vite --host`.
- **Infraestructura**: `docker-compose.yml` (proyecto `playpredict-dev`) con servicios `db` (PostgreSQL 18), `backend`, `frontend`, healthchecks, volúmenes de desarrollo y dependencias ordenadas.
- **Configuración**: `.env.example`, `.gitignore` (raíz), `README.md` con instrucciones reales de uso.

## Qué funciona (verificado en esta sesión — Sprint 2)

- `dotnet build` del backend: OK, sin errores ni advertencias.
- `npm run build` del frontend (tsc + vite build): OK.
- `docker compose config`: válido.
- `docker compose up -d --build`: los 3 servicios levantan correctamente (se recreó el volumen `node_modules` del frontend, que había quedado desactualizado tras agregar `react-router-dom`).
- `docker compose ps`: los 3 servicios `healthy`/`Up`.
- Migración `InitialFixtureSchema` aplicada; tablas `Competitions`, `Editions`, `Rounds`, `Matches` verificadas en PostgreSQL.
- Los 13 endpoints probados manualmente vía `curl`: altas, lecturas, actualizaciones, validaciones (400), conflicto de orden (409), 404 en recursos padre inexistentes, y la regla de Resultado Oficial (solo vía `/result`, goles no negativos).
- Persistencia verificada directamente en PostgreSQL (`psql`) para Competencias, Ediciones y Partidos, incluyendo el resultado cargado.
- Seed de desarrollo verificado idempotente tras reinicio del backend.
- `http://localhost:8006/swagger` → 200, expone los 13 endpoints nuevos.
- `http://localhost:5175` → 200; proxy `/api/*` funcionando contra el backend real.
- Logs de `backend`, `frontend` y `db` revisados: sin errores no esperados (los dos mensajes de error en el log de `db` corresponden al chequeo inicial estándar de EF Core antes de crear `__EFMigrationsHistory` y a una prueba intencional de conflicto de orden de Fecha).

## Comandos de ejecución

```bash
cp .env.example .env          # primera vez
docker compose up -d --build  # levantar
docker compose ps             # verificar estado
docker compose logs -f backend
docker compose down           # detener
docker compose down -v        # detener y borrar datos de Postgres
```

Migraciones EF Core (desde `backend/`, con `dotnet-ef` instalado — `dotnet tool install --global dotnet-ef`):

```bash
dotnet ef migrations add NombreMigracion --output-dir Migrations
dotnet ef database update
```

## Puertos

| Servicio   | Puerto host | Puerto interno |
|------------|-------------|-----------------|
| Frontend   | 5175        | 5175            |
| Backend    | 8006        | 8080            |
| PostgreSQL | 5436        | 5432            |

## Próximo paso exacto

Iniciar Sprint 3 / ETAPA 2 del `PLAN_IMPLEMENTACION_MVP.md`: Usuarios (Registro, Login, Perfil). No iniciar sin aprobación explícita.

## Notas

- Repositorio Git inicializado en la raíz del proyecto, rama `main`. Aún sin commits y sin remoto conectado (pendiente de autorización).
- La base de datos de desarrollo contiene, además de los datos del seed, algunos registros creados manualmente durante las pruebas de esta sesión (competencia "Copa Libertadores", edición "Apertura 2026", una Fecha y un Partido con resultado cargado). No se eliminaron porque el sprint no incluye endpoints de borrado; no afectan el funcionamiento y sirven como evidencia de que el CRUD completo funciona.
- No se verificó visualmente en navegador (herramientas de navegador no habilitadas en esta sesión); se validó por HTTP/proxy y por inspección directa de PostgreSQL.
