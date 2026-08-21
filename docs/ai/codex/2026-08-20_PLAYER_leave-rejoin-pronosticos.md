# PLAYER — Leave/rejoin conservando pronósticos

**Fecha:** 2026-08-20
**Branch:** `prueba-glm-ui`
**Commit de partida:** `126abb5`
**Commit/push:** no realizados

## Cambio

Se deshabilitó temporalmente la restricción de `DELETE /api/leagues/{id}/leave` que impedía abandonar una Liga cuando el PLAYER ya tenía pronósticos.

El endpoint elimina solamente `LeagueParticipant`. No elimina ni modifica registros de `Prediction`. Al volver a participar en la misma Liga, se crea nuevamente la participación y `GET /api/leagues/{id}/matches` vuelve a exponer los pronósticos persistidos del usuario.

Se mantiene sin cambios la protección que impide al creador de una Liga privada abandonarla. Ese usuario conserva las acciones Suspender/Reactivar.

## Política pendiente

La política definitiva de producto sobre qué debe ocurrir con los pronósticos cuando una persona abandona una Liga queda expresamente pendiente de decisión. Para el circuito demo actual se conservan.

## Prueba agregada

`tests/player-official-league-leave-rejoin.ps1` recorre por API:

1. registrar un PLAYER aislado;
2. participar en una Liga Oficial;
3. guardar un pronóstico 2-1;
4. abandonar la Liga con el pronóstico existente;
5. comprobar que desaparece de `/leagues/mine`;
6. comprobar que `/leagues/officials` vuelve a informarla con `isParticipant=false`;
7. volver a participar;
8. comprobar que reaparece en `/leagues/mine`;
9. comprobar que el mismo pronóstico y sus valores siguen presentes.

## Resultado ejecutado

- Docker: DB y backend healthy; frontend iniciado.
- `GET /api/health`: `{"status":"ok"}`.
- Frontend `http://localhost:5175`: HTTP 200.
- Prueba API leave/rejoin: PASS.
- Evidencia funcional: después del rejoin se recuperó el mismo `prediction.id` con marcador 2-1.

La prueba registra un PLAYER aislado y conserva sus datos como evidencia. Requiere el entorno Docker levantado y backend healthy en `http://localhost:8006`.
