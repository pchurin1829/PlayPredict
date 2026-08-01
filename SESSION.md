# SESSION

## Proyecto
PlayPredict

## Rama actual
main (sincronizada con origin/main)

## Último commit
c950d19 — feat: add configurable experiences MVP

## Estado del entorno
- Git: rama `main`, sincronizada con `origin/main` (push realizado). Sprint 8 (Gestión de Experiencias — MVP) commiteado y pusheado en `c950d19`, incluyendo los 3 documentos conceptuales preexistentes (`MODELO_CONCEPTUAL_EXPERIENCIA_v1.0.md`, `MODELO_CONCEPTUAL_ADMINISTRADOR_v1.0.md`, `MODELO_CONCEPTUAL_JUGADOR_v1.0.md`) que estaban sin seguimiento de Git.
- Docker (`docker compose ps`): los 3 servicios levantados y healthy.
  - `playpredict_db` (PostgreSQL 18) — Up, healthy
  - `playpredict_backend` — Up, healthy
  - `playpredict_frontend` — Up
- URLs:
  - Frontend: http://localhost:5175
  - Backend Swagger: http://localhost:8006/swagger
  - Backend health: http://localhost:8006/api/health
- Usuario administrador de desarrollo: `admin@playpredict.local` / `admin123` (creado por el seed, solo en Development).
- Usuarios de demostración del Ranking (solo Development, contraseña `demo123`): `ana.torres@playpredict.local`, `juan.perez@playpredict.local`, `maria.lopez@playpredict.local`, `pedro.gomez@playpredict.local`.

## Último trabajo completado
**Sprint 8 — Gestión de Experiencias (MVP)**: se incorporó `Experience` como entidad principal de PlayPredict (docs/arquitectura/MODELO_CONCEPTUAL_EXPERIENCIA_v1.0.md), de forma incremental y sin romper ninguna funcionalidad de los Sprints 1 a 7.

- Backend: entidad `Experience` (datos generales + puntuación por defecto: ExactScorePoints/CorrectOutcomePoints/IncorrectPoints, sin "Motor" ni plantillas) + enum `ExperienceStatus` (Draft/Published/Archived). `Competition` ahora pertenece obligatoriamente a una `Experience` (`ExperienceId`). Migración `AddExperiences`: crea la tabla, agrega `ExperienceId` a `Competitions` (nullable → backfill vía SQL a una Experience "PlayPredict Demo" creada en la propia migración → NOT NULL + FK) y `UseExperienceDefaults` a `EditionScoringConfigurations` (default `false`) — **sin pérdida de datos**, verificado que las 2 Competencias existentes quedaron asociadas automáticamente.
- Endpoints administrativos `/api/admin/experiences` (solo ADMIN): listar, obtener, crear (siempre Borrador), editar (bloqueado si Archivada), publicar (Borrador → Publicada), archivar (no reversible en este MVP). Sin eliminación física.
- `CompetitionEndpoints`: `POST` acepta `experienceId` opcional (si se omite, se asocia a "PlayPredict Demo" — compatibilidad total con el formulario existente, que no envía este campo); `PUT` solo cambia la Experience si se envía explícitamente (nunca la resetea).
- `EditionScoringConfigurationEndpoints` y `PredictionEvaluationService`: nuevo concepto "Usar configuración de la Experience" vs "Configuración propia" (`UseExperienceDefaults`). La herencia es **completa** (no hay mezcla parcial): si está activada, el Motor de Puntuación usa íntegramente los valores por defecto de la Experience de la Competencia; si no, usa los valores propios de la Edición exactamente como en los Sprints 1 a 7. El DTO expone además los valores "efectivos" (los realmente aplicados) para que el frontend nunca tenga que calcularlos.
- Frontend administrativo: nueva entrada "Experiencias" en el menú (solo ADMIN) con pantallas Listado (acciones Publicar/Archivar), Nueva y Editar (dos secciones: "Datos generales" — Nombre, Descripción, Color primario, Color secundario, Pública — y "Configuración" — puntuación por defecto). La pantalla "Configurar puntuación" de Edición ahora tiene un checkbox "Usar configuración de la Experience" que deshabilita los campos propios y muestra los valores heredados que se van a aplicar.
- Datos de demostración (todos los entornos para la Experience "PlayPredict Demo" vía migración, por compatibilidad; asociación de Competencias demo reforzada en Development vía seed idempotente): Experience "PlayPredict Demo" (Publicada, pública, 6/3/0), Liga Profesional y Copa Libertadores asociadas a ella.
- Probado exhaustivamente: migración aplicada sin pérdida de datos (las 2 Competencias existentes quedaron asociadas correctamente a la Experience Demo mediante el backfill); regresión completa de Sprints 1-7 sin cambios (login, Competencias/Ediciones, Ranking 15/12/9/6 sin alteración, 5 Premios intactos, configuración propia de Edición 7 sin cambios); herencia verificada de punta a punta (se cambiaron temporalmente los valores por defecto de la Experience a 10/5/1, se activó "usar configuración de la Experience" en la Edición 8, y una evaluación real de partido aplicó los 10 puntos heredados en vez de los 6 propios — confirmado en la tabla `PredictionEvaluations`); alta de Competencia sin `experienceId` sigue auto-asociándose a la Experience Demo; edición de Competencia sin `experienceId` no resetea la asociación existente. Todo revertido al finalizar. Verificado también visualmente en el navegador (lista y formulario de Experiencias con las dos secciones, checkbox de herencia en Configurar puntuación) sin errores de consola.

No se implementó (fuera de alcance): Wizard, Sponsors, Branding avanzado, dominios, idiomas, plantillas, biblioteca de configuraciones/motores, White Label, campañas, dashboard ejecutivo, estadísticas, auditoría, mezcla parcial de configuración.

**Revisión visual final previa al commit**: en "PlayPredict Demo" se modificaron temporalmente nombre, descripción, color primario y color secundario, se guardó, se recargó la página y se confirmó la persistencia de los 4 campos; luego se restauraron los valores originales (con acentos correctos) y se verificó la restauración tras recargar. En la configuración de puntuación de la Edición "Fase de Grupos 2026" se probaron ambas fuentes con valores distinguibles (propia 8/4/2 vs. Experience 6/3/0): se confirmó visualmente que los valores efectivos mostrados cambian según la fuente elegida, y se restauró el estado original (6/3/0, configuración propia). Sin errores de consola. Verificado en PostgreSQL que no quedó ningún dato temporal residual.

Detalle completo en PROJECT_STATUS.md.

## Pendiente inmediato
Ninguno. Sprint 8 cerrado (commit y push realizados).

## Próximo paso exacto
Sprint 9. No iniciar sin aprobación explícita.

## Comandos para retomar
```bash
git status
git log -5 --oneline
docker compose ps
docker compose up -d        # si los servicios no están corriendo
docker compose logs -f backend
```
