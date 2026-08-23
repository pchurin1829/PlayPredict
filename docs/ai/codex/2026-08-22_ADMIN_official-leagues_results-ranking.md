# PlayPredict — ADMIN: Ligas Oficiales, resultados y ranking

Fecha: 2026-08-22
Rama: `prueba-glm-ui`
Estado: cambios locales, sin commit ni push

## 1. Modelo actual encontrado

### Implementado antes de esta fase

- **Competition:** fuente deportiva con nombre, deporte, estado y Experience. CRUD ADMIN existente.
- **Edition:** pertenece a Competition; nombre, período y estado. CRUD y configuración de scoring existentes.
- **Round/Fecha:** pertenece a Edition, tiene nombre, orden y período. CRUD existente.
- **Match:** pertenece a Round; local/visitante son textos, fecha/hora, estado y resultado real (`HomeGoals`, `AwayGoals`). CRUD existente.
- **League:** ya tenía `Name` independiente de `Competition.Name`, `LeagueType`, participantes, alcance completo/rango y estado.
- **LeagueType:** `Official` y `Private`.
- **LeagueParticipant:** único por `(LeagueId, UserId)`.
- **Prediction:** única por `(LeagueId, UserId, MatchId)`. Un usuario puede pronosticar distinto el mismo Match en ligas distintas.
- **Resultado:** se almacena una sola vez en Match mediante `PUT /api/matches/{id}/result`.
- **Scoring:** `PredictionEvaluationService` evalúa todos los pronósticos del Match, sin distinguir tipo de liga.
- **Ranking:** lectura agregada de PredictionEvaluation; existe ranking por Edition, Round y League.

### Parcial antes de esta fase

- League sólo referenciaba Competition. Un alcance completo podía incluir varias Editions de la misma Competition.
- Las Ligas Oficiales existían en modelo/API PLAYER, pero no tenían CRUD ADMIN.
- Explorar Competencias asumía de hecho una Liga Oficial principal por Competition y mostraba la Competition como título.
- La edición usada por una Liga de alcance completo no quedaba persistida explícitamente.

### No implementado antes de esta fase

- Creación/edición ADMIN de Liga Oficial comercial.
- Selección obligatoria de Edition para una Liga.
- Presentación PLAYER de múltiples Ligas Oficiales comerciales sobre la misma fuente.

## 2. Gaps detectados

- Ambigüedad Competition vs Edition en el fixture de League.
- Ausencia de pantalla/API ADMIN para Ligas Oficiales.
- Nombre comercial subordinado visualmente al nombre deportivo.
- “Próximos partidos” de Home todavía incluía abiertos pendientes además de ya pronosticados.
- ADMIN no explicaba que corregir un resultado recalcula puntos compartidos.
- No existe `Organizer` en Competition; queda fuera del mínimo solicitado para este circuito.

## 3. Cambios implementados

- `League.EditionId` obligatorio con FK restrictiva e índice.
- Backfill no destructivo de ligas existentes: RoundFrom determina Edition en rangos; alcance completo toma la edición más reciente de su Competition.
- Alcance completo ahora consulta exclusivamente Rounds/Matches de `League.EditionId`.
- Creación privada también exige Edition.
- API ADMIN con listado, detalle, creación y edición de Ligas Oficiales.
- Protección: una Liga Oficial con pronósticos no puede cambiar fuente/edición/alcance; sí nombre, descripción y estado.
- Pantallas ADMIN para listar y crear/editar Ligas Oficiales.
- Partidos ADMIN indican qué Ligas Oficiales reutilizan su Edition.
- Resultado ya cargado se presenta como “Corregir resultado” y explica el recálculo.
- PLAYER muestra el nombre comercial como título y Competition · año/Edition como fuente secundaria.

## 4. Relación final Competition / Edition / League

`Competition 1—N Edition 1—N Round 1—N Match`

`Competition 1—N League` y `Edition 1—N League`.

League conserva Competition para identidad/validación y Edition para fijar el fixture concreto. Match y resultado no pertenecen a League; las ligas los reutilizan por Edition y, opcionalmente, rango de Rounds.

## 5. Múltiples Ligas Oficiales

Sí. No existe restricción única por Competition/Edition. Varias Ligas `Official` pueden apuntar a la misma Edition y compartir Match/resultados, manteniendo participantes, Predictions y ranking por League independientes.

## 6. Creación comercial

Ruta ADMIN: `/admin/official-leagues`.

Campos: nombre público/comercial, descripción, Competition fuente, Edition, alcance completo/rango, Fechas inicial/final y estado. La ayuda distingue explícitamente nombre de Liga Oficial de nombre de Competition.

Caso creado: **COPA EL NENE**, fuente **Copa Libertadores · 2026** (`EditionId=2`).

## 7. Carga de resultados

Continúa en ADMIN, desde Competition → Edition → Fecha → Partidos. El resultado vive en Match y se guarda una vez. El mismo endpoint evalúa todas las Predictions del Match en Ligas Oficiales y privadas.

## 8. Recálculo

PredictionEvaluation tiene una evaluación vigente única por Prediction. Al corregir un resultado, el servicio actualiza tipo, puntos, marcador oficial y fecha sobre esa fila. No suma una evaluación adicional.

E2E:

- Resultado 2–1: COPA EL NENE = 6 puntos; Liga Amigos E2E = 0.
- Corrección a 1–1: COPA EL NENE = 0; Liga Amigos E2E = 6.
- `EvaluatedCount` final: 1 en cada liga.

## 9. Ranking Oficial

`/api/rankings/leagues/{leagueId}` filtra por `Prediction.LeagueId`. COPA EL NENE quedó validada con ranking propio y recálculo correcto.

## 10. Ranking Liga Amigos

Usa el mismo resultado de Match pero sólo evaluaciones de Predictions de esa League. Se validó con `Liga Amigos E2E Libertadores` y un pronóstico distinto para el mismo Match.

El Ranking General por Edition conserva su comportamiento previo: agrega evaluaciones de la Edition a través de todas las ligas. No se redefinió esa semántica en esta fase.

## 11. Home PLAYER

- “Pronósticos pendientes” sigue reservado a Matches abiertos sin Prediction.
- La segunda sección se renombró **Tus próximos partidos**.
- Sólo incluye próximos ya pronosticados.
- Mantiene Competition, nombre de League y Fecha en cada grupo.

## 12. Migración

- `20260822211335_AddLeagueEdition`.
- Aditiva y aplicada correctamente sobre la DB local.
- No hubo reset, recreación, borrado ni seeder destructivo.

## 13. Archivos modificados en esta fase

### Backend

- `backend/Domain/Entities/League.cs`
- `backend/Data/Configurations/LeagueConfiguration.cs`
- `backend/Data/DataSeeder.cs`
- `backend/Dtos/LeagueDtos.cs`
- `backend/Endpoints/LeagueEndpoints.cs`
- `backend/Endpoints/AdminOfficialLeagueEndpoints.cs`
- `backend/Program.cs`
- `backend/Migrations/20260822211335_AddLeagueEdition.cs`
- `backend/Migrations/20260822211335_AddLeagueEdition.Designer.cs`
- `backend/Migrations/PlayPredictDbContextModelSnapshot.cs`

### Frontend

- `frontend/src/App.tsx`
- `frontend/src/api/types.ts`
- `frontend/src/components/Layout.tsx`
- `frontend/src/components/MatchResultModal.tsx`
- `frontend/src/components/admin.css`
- `frontend/src/pages/AdminOfficialLeaguesListPage.tsx`
- `frontend/src/pages/AdminOfficialLeagueFormPage.tsx`
- `frontend/src/pages/ExploreCompetitionsPage.tsx`
- `frontend/src/pages/LeagueCreatePage.tsx`
- `frontend/src/pages/LeagueDetailPage.tsx`
- `frontend/src/pages/LeaguesMinePage.tsx`
- `frontend/src/pages/MatchesListPage.tsx`
- `frontend/src/pages/PlayerDashboardPage.tsx`
- `frontend/src/pages/PlayerDashboardPage.css`
- `frontend/src/pages/PlayerPages.css`

## 14. Tests y validaciones

- Backend `dotnet build` en contenedor .NET 10: OK (warning NU1510 preexistente).
- E2E API seguro: creación Official/Private, unión, dos Predictions sobre el mismo Match, carga 2–1, ranking, corrección 1–1 y segundo ranking: OK.
- Frontend `npx tsc --noEmit`: OK.
- Frontend `npm run build`: OK.
- `git diff --check`: OK (avisos informativos LF/CRLF).

## 15. Pendientes

- Competition todavía no modela Organizador.
- Equipos siguen siendo textos en Match; no existe catálogo Team, dentro del alcance actual.
- Premios continúan asociados a Edition; premio propio por League queda para la fase posterior indicada por producto.
- No existe proyecto automatizado de tests backend; la lógica se validó mediante build y E2E API puntual.
- Falta aprobación visual manual de las nuevas pantallas ADMIN y de Explorar Competencias PLAYER.

## 16. Estado Git

- Rama `prueba-glm-ui`.
- Sin commit ni push.
- Worktree conserva además los cambios locales legítimos de Light Theme/Mobile/Home de las fases anteriores y los archivos locales deliberadamente excluidos ya identificados.
