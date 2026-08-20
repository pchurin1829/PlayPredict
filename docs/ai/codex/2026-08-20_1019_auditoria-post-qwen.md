# Auditoría post-Qwen — circuito PLAYER

**Fecha:** 2026-08-20 10:19 (America/Buenos_Aires)  
**Branch:** `prueba-glm-ui`  
**Commit de partida:** `0a59e70` — `WIP: player leagues and predictions demo fixes`  
**Commit/push:** no realizados

## Alcance

Auditoría de los cambios dejados por Qwen para Pronósticos, Mis Ligas, Explorar Competencias y regresiones del circuito PLAYER. No se agregaron funcionalidades.

## Estado inicial

```text
## prueba-glm-ui...origin/prueba-glm-ui
?? .claude/
?? .qwen/
?? "Nuevo Documento de texto.txt"
```

Los tres untracked eran preexistentes y se preservaron.

## Bugs confirmados

### 1. Doble guardado al activar el botón con ENTER

En `PredictionsMatchesPage`, el botón ejecutaba `savePrediction()` desde `onKeyDown` para ENTER y también desde `onClick`. Un botón HTML activado con ENTER ya genera su click nativo, por lo que ambos handlers podían ejecutarse antes de que React reflejara `saving`.

Impacto: dos POST/PUT casi simultáneos; en altas podía producir conflicto por duplicado y romper la secuencia teclado → siguiente partido.

### 2. Suspender/Reactivar borraba la descripción

`LeaguesMinePage` enviaba `description: null` al hacer PUT para suspender o reactivar. El endpoint reemplaza todos esos campos, de modo que la acción podía borrar silenciosamente la descripción existente. También se detectó el typo visible `Suspentiendo...`.

## Correcciones

- Se eliminó el handler manual de teclado del botón de Pronósticos. ENTER/Space usan la activación nativa y llegan una sola vez a `onClick`; se conserva la secuencia visitante → botón → guardar → siguiente partido.
- Suspender/Reactivar ahora reenvía `league.description` y preserva el dato.
- Se corrigió `Suspentiendo...` por `Suspendiendo...`.

## Verificaciones de código

- Estados: sin persistencia `¡Pronosticá!`, persistido sin cambios `Pronosticado`, persistido modificado `Guardar cambios`.
- Tras guardar se actualizan `savedHome`, `savedAway` y `hasPrediction`, por lo que vuelve a `Pronosticado` sin F5.
- Los ceros son válidos: se comprueba string vacío y enteros no negativos; 0-0, 0-1 y 1-0 no son descartados por falsy.
- Mis Ligas: oficiales usan `pp-league-card--official`; privadas usan `pp-league-card--mine`; oficiales participadas se excluyen de “disponibles”; propia permite Suspender/Reactivar sin “Dejar”; ajena permite “Dejar”.
- `ConfirmModal` usa overlay fixed con flex centrado y contenido/acciones centrados.
- Explorar: participante → `Ir a mi Liga`; no participante → `Participar en Liga Oficial`; crear enlaza a `/leagues/new?competitionId=X`; no aparece `Ver competencia`.

## Pruebas ejecutadas

| Prueba | Resultado |
|---|---|
| `npx tsc --noEmit` | OK, sin errores |
| `npm run build` | OK (exit code 0) |
| Backend `GET /api/health` | OK — `{"status":"ok"}` |
| `docker compose ps` inicial/final | DB healthy, backend healthy, frontend figura Up |
| Chrome real automatizado | Intentado; el frontend no entregó una respuesta utilizable y los reintentos quedaron colgados |

No se encontraron proyectos de tests automatizados existentes (`*.test.ts`, `*.spec.ts`, `*Tests.csproj`) en el repositorio.

## Bloqueo de entorno y pendientes reales

La documentación indica que Vite en Docker/Windows necesita reinicio para tomar cambios. Después de `docker compose restart frontend`, el puerto 5175 dejó de entregar una respuesta utilizable. Algunas operaciones Docker quedaron colgadas; se cortó únicamente el proceso CLI, sin usar `down`, sin tocar volúmenes y sin reiniciar DB/backend. Al cierre, `docker compose ps` volvió a responder y muestra los tres servicios Up (DB/backend healthy), pero Chrome/HTTP siguieron sin permitir completar la navegación. El backend continuó healthy en 8006.

Por ese bloqueo no se marca como ejecutada la prueba visual/teclado completa. Queda pendiente, cuando Docker Desktop vuelva a responder:

- tres partidos consecutivos por teclado con 0-0, 0-1 y 1-0;
- modificar uno, comprobar `Guardar cambios`, guardar y volver a `Pronosticado`;
- F5 y confirmar persistencia/estado;
- inspección visual final de colores, no duplicación y modal;
- regresión navegada de Registro, Login, Mis Ligas, Unirme, Crear Liga, Pronósticos, Resultados y Ranking;
- confirmar que el frontend responde realmente en 5175, además de figurar Up.

## Archivos modificados

- `frontend/src/pages/PredictionsMatchesPage.tsx`
- `frontend/src/pages/LeaguesMinePage.tsx`
- `docs/ai/codex/2026-08-20_1019_auditoria-post-qwen.md`

## Git status final

Además de los tres untracked preexistentes, quedan los dos cambios mínimos de código y este informe. No se hizo commit ni push.
