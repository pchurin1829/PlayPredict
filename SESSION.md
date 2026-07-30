# SESSION

## Proyecto
PlayPredict

## Rama actual
main (sincronizada con origin/main)

## Último commit
79c31f7 — docs: sync session documentation after Sprint 3 completion

## Estado del entorno
- Git: rama `main`. Fix de la regla de cierre de Pronósticos (ver abajo) aplicado en esta sesión sobre el commit `1fd819f`. Sin commitear — pendiente de aprobación explícita del usuario.
- Docker (`docker compose ps`): los 3 servicios levantados y healthy tras `docker compose up -d --build`.
  - `playpredict_db` (PostgreSQL 18) — Up, healthy
  - `playpredict_backend` — Up, healthy
  - `playpredict_frontend` — Up
- URLs:
  - Frontend: http://localhost:5175
  - Backend Swagger: http://localhost:8006/swagger
  - Backend health: http://localhost:8006/api/health
- Usuario administrador de desarrollo: `admin@playpredict.local` / `admin123` (creado por el seed, solo en Development).

## Último trabajo completado
**Sprint 4 — Sistema de Pronósticos (infraestructura, sin puntos/rankings/posiciones)**, commiteado en `1fd819f`:
- Backend: entidad `Prediction` (MatchId, UserId, PredictedHomeScore, PredictedAwayScore, CreatedAtUtc, UpdatedAtUtc), único por (UserId, MatchId); migración `AddPredictions`; endpoints `GET /api/predictions/rounds/{roundId}`, `GET /api/predictions/me`, `POST /api/predictions`, `PUT /api/predictions/{id}`, todos autenticados.
- Frontend: nueva sección "Pronósticos" en el menú, con navegación Competencia → Edición → Fecha → Partidos (4 pantallas nuevas), pantalla única de carga por Fecha con inputs Local/Visitante y botón "Guardar pronóstico"/"Actualizar pronóstico" por partido.
- Ajuste de UX (commiteado junto con el sprint): inputs vacíos con placeholder "-", solo dígitos, mensaje de éxito se limpia solo a los 4 segundos.

**Fix post-Sprint 4 — Regla de cierre de Pronósticos corregida** (esta sesión, sin commitear todavía): el informe del Sprint 4 decía "Finalizado/Cancelado bloqueado, resto editable" — esa descripción coincidía con el código tal como quedó commiteado en `1fd819f`, pero **no** con la regla definitiva acordada. El código, no solo el informe, estaba mal. Regla efectiva ahora: un pronóstico solo puede crearse o modificarse si `Match.Status == Scheduled` **y** `DateTime.UtcNow < Match.StartsAtUtc` — ambas condiciones simultáneas. Cualquier otro caso (Programado con horario ya pasado, En juego, Suspendido, Finalizado, Cancelado) queda bloqueado. El backend expone un indicador explícito `canPredict` en `GET /api/predictions/rounds/{roundId}` y es la única fuente de verdad; el frontend fue corregido para usar ese indicador en vez de calcular la regla por su cuenta.

No se implementó (fuera de alcance): cálculo de puntos, comparación con resultados, rankings, posiciones, premios, estadísticas, grupos privados.

Detalle completo en PROJECT_STATUS.md.

## Pendiente inmediato
- Aprobación explícita del usuario para hacer commit del fix de la regla de cierre de Pronósticos.
- Decidir qué hacer con el cambio sin confirmar en `docs/Etapa_1_28-07-2026_PlayPredict.pdf` (no forma parte de este protocolo de sesión; sigue sin resolver de sesiones anteriores).

## Próximo paso exacto
Esperar aprobación del usuario para el commit del fix. Después, Sprint 5 en adelante (puntos, rankings, posiciones, premios) — no iniciar sin aprobación explícita.

## Comandos para retomar
```bash
git status
git log -5 --oneline
docker compose ps
docker compose up -d        # si los servicios no están corriendo
docker compose logs -f backend
```
