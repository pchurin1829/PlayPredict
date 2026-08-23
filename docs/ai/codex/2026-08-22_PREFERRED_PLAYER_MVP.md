# PlayPredict — Jugador Preferido MVP

Fecha: 2026-08-23
Rama: `prueba-glm-ui`

## 1. Modelo de TeamPlayer

`TeamPlayer` representa al deportista y no se mezcla con `User`. Contiene TeamId, nombre, apellido, nombre visible, dorsal y posición opcionales, estado y PhotoUrl opcional. La FK a Team usa `Restrict`.

## 2. Relación con Team y CRUD

Cada Team expone `/api/teams/{teamId}/players`. ADMIN puede listar, crear y editar desde Equipos → Plantel. Los formularios conservan Volver, Guardar y Cancelar. No se agregó eliminación para preservar referencias históricas; se utiliza el estado Activo.

## 3. Datos demo

Seeder idempotente agrega tres jugadores inequívocamente ficticios (`Jugador Demo ...`) por cada Team usado en el fixture demo de Liga Profesional. No se descargaron nombres, fotos ni assets externos.

## 4. Elección

`Prediction.PreferredPlayerId` es nullable. Backend valida que el jugador esté activo, pertenezca al Team local o visitante y que la Edition tenga habilitada la función. La elección se crea, cambia o limpia mediante el mismo POST/PUT del pronóstico y respeta exactamente su ventana de edición.

## 5. Goleadores

`MatchScorer` registra Match, TeamPlayer y cantidad de goles, con unicidad por partido/jugador. El modal permite filas opcionales. Backend exige jugador de uno de los Teams, goles positivos, jugador no repetido y que la suma por Team no supere el resultado. Omitir goleadores no bloquea el resultado.

## 6. Scoring

La Edition define `PreferredPlayerEnabled` y `PreferredPlayerPointsPerGoal` (defaults: true y 2). `PredictionEvaluation` conserva:

- `ResultPoints`: puntuación tradicional.
- `PreferredPlayerPoints`: goles oficiales del elegido × puntos por gol.
- `Points`: suma total consumida por rankings y premios existentes.

El recálculo sigue siendo idempotente: corregir marcador/goleadores reemplaza los valores, no acumula.

## 7. Independencia

Los puntos preferidos se suman aunque el marcador pronosticado sea incorrecto. Si el elegido no convierte, suma 0.

## 8. Sin goleadores

El resultado puede finalizarse sin detalle. En ese caso el componente especial queda en 0 hasta que ADMIN complete o corrija goleadores; la UI lo explica.

## 9. Mobile

El select usa ancho completo y target mínimo de 44 px. El editor de goleadores pasa a filas apiladas bajo 600 px y no altera los inputs del marcador.

## 10. Evolución futura

El MVP evita crear una abstracción prematura. La separación entre elección (`Prediction.PreferredPlayerId`), hecho oficial (`MatchScorer`) y desglose (`PredictionEvaluation`) permite migrar luego a `SpecialPredictionType`, `SpecialPrediction` y `SpecialPredictionResult` para tarjetas, cambios u otros eventos, sin implementarlos ahora.

## 11. Migración

`AddPreferredPlayerMvp` es aditiva: crea TeamPlayers/MatchScorers, agrega FKs/campos y copia `PredictionEvaluations.Points` a `ResultPoints` para preservar el desglose histórico. No elimina ni recrea tablas en `Up`.

## 12. Validaciones

- Builds backend/frontend y TypeScript.
- API de planteles y configuración.
- Restricción de jugador ajeno al Match.
- Restricción de goleadores y suma por Team.
- Exposición de jugadores local/visitante en partidos PLAYER.
- `git diff --check`.

## 13. Pendientes

- Cargar planteles reales verificados cuando exista una fuente autorizada.
- Historial/auditoría de correcciones de goleadores.
- Generalización a otros tipos de pronóstico especial.
