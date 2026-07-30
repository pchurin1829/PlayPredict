# SESSION

## Proyecto
PlayPredict

## Rama actual
main (sincronizada con origin/main)

## Último commit
79c31f7 — docs: sync session documentation after Sprint 3 completion

## Estado del entorno
- Git: rama `main`. Sprint 4 (Sistema de Pronósticos — infraestructura, sin puntos/rankings) implementado en esta sesión. Sin commitear — pendiente de aprobación explícita del usuario.
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
**Sprint 4 — Sistema de Pronósticos (infraestructura, sin puntos/rankings/posiciones)**:
- Backend: entidad `Prediction` (MatchId, UserId, PredictedHomeScore, PredictedAwayScore, CreatedAtUtc, UpdatedAtUtc), único por (UserId, MatchId); migración `AddPredictions`; endpoints `GET /api/predictions/rounds/{roundId}`, `GET /api/predictions/me`, `POST /api/predictions`, `PUT /api/predictions/{id}`, todos autenticados.
- Reglas: un pronóstico por usuario y partido; solo editable/creable mientras el partido esté Programado, En juego o Suspendido; bloqueado para Finalizado y Cancelado; un usuario no puede modificar el pronóstico de otro (403).
- Frontend: nueva sección "Pronósticos" en el menú, con navegación Competencia → Edición → Fecha → Partidos (4 pantallas nuevas), pantalla única de carga por Fecha con inputs Local/Visitante y botón "Guardar pronóstico"/"Actualizar pronóstico" por partido. Sin tocar estilo, colores ni layout existente (solo se agregaron 3 clases CSS puntuales para la fila de carga).
- No se implementó (fuera de alcance, según lo pedido): cálculo de puntos, comparación con resultados, rankings, posiciones, premios, estadísticas, grupos privados.

**Ajuste de UX post-Sprint 4** (pedido por el usuario antes de aprobar, sin tocar lógica de negocio): inputs de goles ahora quedan vacíos con placeholder "-" en vez de "0" cuando no hay pronóstico, solo aceptan dígitos (sin letras/signos/decimales), el mensaje de éxito se limpia solo a los 4 segundos (antes quedaba fijo), y se ajustó CSS para que el alto de la fila no cambie al mostrar/ocultar el mensaje.

Detalle completo en PROJECT_STATUS.md.

## Pendiente inmediato
- Aprobación explícita del usuario para hacer commit del Sprint 4.
- Decidir qué hacer con el cambio sin confirmar en `docs/Etapa_1_28-07-2026_PlayPredict.pdf` (no forma parte de este protocolo de sesión; sigue sin resolver de sesiones anteriores).
- Nota: durante las pruebas manuales en el navegador, el clic/tecleo sintético de la herramienta de automatización no llegó a disparar eventos reales sobre la página (afectaba incluso a un input preexistente de "Mi perfil"), algo ajeno al código de la aplicación. Se verificó el flujo real disparando los mismos eventos DOM que React escucha (equivalentes a una interacción real de usuario) y confirmando las llamadas de red resultantes; igualmente se recomienda una prueba manual rápida del usuario antes de aprobar.

## Próximo paso exacto
Esperar aprobación del usuario para el commit del Sprint 4. Después, Sprint 5 en adelante (puntos, rankings, posiciones, premios) — no iniciar sin aprobación explícita.

## Comandos para retomar
```bash
git status
git log -5 --oneline
docker compose ps
docker compose up -d        # si los servicios no están corriendo
docker compose logs -f backend
```
