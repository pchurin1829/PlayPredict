# POLÍTICA DE MEMBRESÍA — Ligas de Amigos (MVP)

Versión: 1.0
Fecha: 2026-09-10
Estado: VIGENTE. Complementa `DECISION_PRONOSTICO_GLOBAL_v1.0.md`.

## Reglas MVP

1. Los pronósticos son globales (`UserId + MatchId`) y NO se eliminan nunca
   por leave ni por expulsión. Pueden estar en uso por otras ligas.
2. Leave (`DELETE /api/leagues/{id}/leave`) y expulsión
   (`DELETE /api/leagues/{id}/participants/{userId}`) terminan el período de
   membresía de ESA liga: `LeagueParticipant.LeftAtUtc = now`.
3. Ranking y evaluaciones respetan los períodos: un pronóstico solo puntúa en
   una liga si existe un período con `JoinedAt < StartsAt` y
   (`LeftAt == null` o `LeftAt > StartsAt`).
4. Reingresar (join directo o por código) crea una NUEVA fila de membresía:
   comienza un período nuevo, sin recuperar puntos del período de ausencia.
5. El creador no puede abandonar su liga (leave) ni autoexpulsarse; solo puede
   suspenderla/reactivarla o eliminarla (elimina evaluaciones y membresías de
   ESA liga, nunca los pronósticos globales).
6. Expulsión: solo el creador, solo ligas `Private`, nunca a sí mismo,
   nunca en ligas oficiales.

## Cumplimiento verificado en código (sin cambios al motor)

- `LeagueEndpoints.cs` (leave): solo fija `LeftAtUtc`; comentario de política.
- `LeagueEndpoints.cs` (`ExpelParticipantAsync`): solo fija `LeftAtUtc`;
  no toca `Predictions` ni `PredictionEvaluations`.
- `PredictionEvaluationService.cs:34-38`: al cargar un resultado, solo evalúa
  si el usuario tiene un período que cubra el inicio del partido.
  Expulsado/ausente a esa fecha → sin evaluación → sin puntos retroactivos.
- `PredictionEvaluationService.cs:75`: ligas creadas después del partido
  tampoco evalúan (sin retroactividad).
- `PredictionEndpoints.cs:453-457` (`IsEligible`): la UI muestra elegibilidad
  con la misma regla de períodos.
- Evaluaciones ya otorgadas mientras era miembro SE CONSERVAN (historial):
  el ranking no se recalcula hacia atrás.

Divergencia con documentación anterior: ninguna en el motor. La mención en
`LeagueEndpoints.cs` (leave) a "política temporal/pendiente de decisión" queda
sustituida por esta política; el código ya la cumple y no se modificó.
