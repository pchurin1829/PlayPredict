# SESSION

## Proyecto
PlayPredict

## Rama actual
main (sincronizada con origin/main)

## Último commit
afe2776 — docs: sync session after Sprint 5

## Estado del entorno
- Git: rama `main`. Sprint 6 (Motor de Rankings) implementado y aprobado funcionalmente sobre el commit `afe2776`. Pendiente únicamente realizar el commit.
- Docker (`docker compose ps`): los 3 servicios levantados y healthy.
  - `playpredict_db` (PostgreSQL 18) — Up, healthy
  - `playpredict_backend` — Up, healthy
  - `playpredict_frontend` — Up
- URLs:
  - Frontend: http://localhost:5175
  - Backend Swagger: http://localhost:8006/swagger
  - Backend health: http://localhost:8006/api/health
- Usuario administrador de desarrollo: `admin@playpredict.local` / `admin123` (creado por el seed, solo en Development).
- Usuarios de demostración del Ranking (solo Development, contraseña `demo123`): `ana.torres@playpredict.local`, `juan.perez@playpredict.local`, `maria.lopez@playpredict.local`, `pedro.gomez@playpredict.local`. No colisionan con los usuarios de prueba preexistentes de sesiones anteriores (`juan.perez@example.com`, `maria.lopez@example.com`, `prueba@example.com`), que se dejaron intactos.

## Último trabajo completado
**Sprint 6 — Motor de Rankings**: el Ranking no calcula puntos (eso ya lo hace el Motor de Puntuación del Sprint 5); solo consulta `PredictionEvaluations` existentes y calcula posiciones dinámicamente, sin persistir nada.

- Backend: `RankingService` (responsabilidad única, sin estado) con `GetEditionRankingAsync`/`GetRoundRankingAsync`; agrupa evaluaciones por usuario, suma puntos y cuenta exactos/correctos/incorrectos; ordena por puntos → exactos → correctos → incorrectos (asc) → apellido/nombre (solo para desempate visual); asigna posición compartida al estilo "1-2-2-4" (ranking deportivo estándar, no "1-2-2-3"). Solo participan usuarios con al menos un pronóstico evaluado.
- Endpoints `GET /api/rankings/editions/{editionId}` y `GET /api/rankings/rounds/{roundId}`, autenticados, sin restricción de rol.
- Sin migración: no se creó ninguna tabla nueva, todo se calcula en memoria a partir de `Predictions`/`PredictionEvaluations`/`Matches`/`Rounds`/`Editions`.
- Frontend: nueva entrada "Rankings" en el menú, con navegación Competencia → Edición → (Ranking General | Fechas → Ranking por Fecha), tabla con columnas # / Usuario / Puntos / Exactos / Correctos / Incorrectos / Pronósticos.
- Datos de demostración (solo Development, idempotentes): Fecha 1 de Clausura 2026 con nombres reales (Boca Juniors–River Plate, Racing Club–Independiente, Estudiantes–Gimnasia) y resultados oficiales (2-1, 1-1, 0-2); 4 usuarios (Ana Torres, Juan Pérez, María López, Pedro Gómez) con sus pronósticos ya evaluados. Ranking resultante verificado exacto: Juan 15, Ana 12, María 9, Pedro 6.
- Probado en el navegador y por API: ranking correcto, empates con posición compartida y desempate alfabético (prueba temporal revertida), recálculo automático al agregar un resultado nuevo y al corregir uno existente (pruebas temporales revertidas). Fixture final limpio: 12 Predictions, 12 PredictionEvaluations, exactamente las esperadas.

No se implementó (fuera de alcance): ranking mensual, histórico, por empresa, por grupo privado, premios, bonificaciones.

Detalle completo en PROJECT_STATUS.md.

## Pendiente inmediato
Únicamente realizar el commit del Sprint 6 (ya aprobado funcionalmente por el usuario).

## Próximo paso exacto
Sprint 7 — Premios. No iniciar sin aprobación explícita.

## Comandos para retomar
```bash
git status
git log -5 --oneline
docker compose ps
docker compose up -d        # si los servicios no están corriendo
docker compose logs -f backend
```
