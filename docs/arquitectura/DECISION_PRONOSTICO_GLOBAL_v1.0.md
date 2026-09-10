# DECISIÓN ARQUITECTÓNICA — Pronóstico global por Usuario+Partido

Versión: 1.0
Fecha: 2026-09-10
Estado: VIGENTE (sustituye el modelo por-liga del Sprint 8.5 en este punto).

## Decisión

El modelo oficial de pronósticos es:

**UN pronóstico por `UserId + MatchId`.**

NO `LeagueId + UserId + MatchId`.

## Reglas

1. El mismo pronóstico deportivo se reutiliza en todas las ligas compatibles
   (oficiales/empresa/amigos) que utilizan ese partido.
2. Las evaluaciones y rankings SÍ pertenecen al contexto de cada liga:
   `PredictionEvaluation` es 1 por (`PredictionId`, `LeagueId`).
3. NO modificar `Prediction` ni su índice único.
4. NO crear pronósticos duplicados por liga.
5. Borrar un pronóstico lo elimina para todas las ligas (es el mismo registro).

## Evidencia en código (fuente de verdad)

- `backend/Data/Configurations/PredictionConfiguration.cs:29-30` —
  índice único (`UserId`, `MatchId`).
- `backend/Data/Configurations/PredictionEvaluationConfiguration.cs:30` —
  índice único (`PredictionId`, `LeagueId`).
- `backend/Endpoints/PredictionEndpoints.cs` — upsert por usuario+partido;
  validación de contexto por liga; mensaje UI "Se borrará en todas las Ligas".

## Documentos corregidos por esta decisión

- `PLAYPREDICT_MODELO_CONCEPTUAL_v2.0.md` — la identidad
  `LeagueId + UserId + MatchId` de la Sección 0 (punto 6), Sección 8 y
  Sección 9 queda SIN EFECTO. Ver nota de vigencia al inicio de ese documento.
- `PROJECT_STATUS.md` — las descripciones del Sprint 8.5 que afirman
  "identidad lógica `LeagueId+UserId+MatchId`" y "pronósticos nunca se
  comparten entre ligas" quedan SIN EFECTO. Ver nota de vigencia.

Esos documentos se conservan como historial y NO se reescriben; las notas de
vigencia indican qué partes fueron sustituidas.
