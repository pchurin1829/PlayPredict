# PLAYER — Explorar y estados de Pronósticos

**Fecha:** 2026-08-20
**Branch:** `prueba-glm-ui`
**Commit de partida:** `126abb5`
**Commit/push:** no realizados

## Causa raíz de `Guardar cambios` al entrar

Había dos implementaciones de la UI de Pronósticos. `PredictionsMatchesPage` ya conservaba valores actuales y guardados, pero el tab Pronósticos realmente usado dentro de `LeagueDetailPage` mantenía la versión anterior.

Ese tab sólo comprobaba `!!match.myPrediction`: si existía, mostraba incondicionalmente `Guardar cambios`. No tenía `savedHome`, `savedAway`, `hasPrediction` ni cálculo de `dirty`, por lo que no podía distinguir hidratación inicial de una edición del usuario.

## Máquina de estados

Ambas superficies quedaron alineadas con estos estados:

| Persistido | Inputs actuales | Acción |
|---|---|---|
| No | ambos vacíos | `¡Pronosticá!`, deshabilitado |
| No | uno vacío | `Completá ambos resultados`, deshabilitado |
| No | ambos completos | `Guardar pronóstico` |
| Sí | iguales a `savedHome/savedAway` | `Pronosticado`, deshabilitado |
| Sí | uno vacío | `Completá ambos resultados`, deshabilitado |
| Sí | ambos completos y distintos | `Guardar cambios` |
| Sí | ambos vacíos | `Eliminar pronóstico` |

Al hidratar desde API, current y saved reciben los mismos strings y `dirty=false`. Tras POST/PUT exitoso, saved se actualiza con current y vuelve inmediatamente a `Pronosticado`. El valor `0` se trata como string válido, no como falsy.

## Campos vacíos y DELETE

Se agregó `DELETE /api/predictions/{id}`. Exige autenticación y propiedad del pronóstico, y reutiliza las validaciones de contexto de modificar: participación vigente, Liga activa y partido todavía abierto.

Cuando un pronóstico persistido queda con ambos inputs vacíos, la UI ofrece `Eliminar pronóstico` y abre `ConfirmModal`. Un DELETE exitoso deja `hasPrediction=false` y current/saved vacíos; la UI pasa a `¡Pronosticá!`. Los parciales nunca hacen POST/PUT.

Esta eliminación individual no modifica la política temporal de leave/rejoin: abandonar una Liga conserva todos los pronósticos.

## Navegación ENTER

El tab de Liga ahora usa la misma secuencia que la página dedicada:

`local → visitante → botón → guardar → local del siguiente partido`.

El botón usa activación nativa; no se agregó un segundo handler de guardado. Después de POST/PUT exitoso, el foco avanza al siguiente partido pronosticable.

## Explorar Competencias Oficiales

Se conservaron título y explicación. Cada competencia ahora comunica explícitamente:

- no participante: `Todavía no participás`, ayuda breve y CTA primario `Participar`;
- participante: `✓ Estás participando`, ayuda breve y acción outline `Ver`;
- `+ Crear Liga con amigos` permanece como acción separada.

No se volvió a mezclar el catálogo de Oficiales con Mis Ligas.

## Pruebas ejecutadas

- `npx tsc --noEmit`: OK.
- `npm run build`: OK, 95 módulos transformados.
- Backend reconstruido con .NET 10: OK.
- `GET /api/health`: OK, `{"status":"ok"}`.
- `tests/player-prediction-delete.ps1`: PASS (`- - → 0-0 → persistencia → DELETE → recarga sin Prediction`).
- `tests/player-official-league-leave-rejoin.ps1`: PASS; leave/rejoin conserva el mismo pronóstico.
- `git diff --check`: OK; sólo advertencias de normalización LF/CRLF.

La verificación visual y de teclado en navegador real de los seis casos solicitados queda pendiente de la prueba manual del usuario.

## Archivos modificados

- `backend/Endpoints/PredictionEndpoints.cs`
- `frontend/src/pages/ExploreCompetitionsPage.tsx`
- `frontend/src/pages/LeagueDetailPage.tsx`
- `frontend/src/pages/PredictionsMatchesPage.tsx`
- `frontend/src/pages/PlayerPages.css`
- `tests/player-prediction-delete.ps1` (nuevo)
- `PROJECT_STATUS.md`
- este informe

Se preservan además los cambios previos sin commit de `LeagueEndpoints`, el test leave/rejoin y su informe.

## Git status final

Sin commit ni push. Archivos versionados modificados y nuevos informes/tests pendientes de seguimiento. Se preservaron `.qwen/` y `Captura_Prueba.png` preexistentes.
