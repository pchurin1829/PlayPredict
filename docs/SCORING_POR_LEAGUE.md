# Scoring por Competencia del cliente

La configuración general se persiste en `Companies` (`General*`). Cada `League`
persiste `UseGeneralScoring` y sus valores propios. `LeagueScoringService` es el único
resolvedor de configuración efectiva: si `UseGeneralScoring=true` lee siempre los
valores generales vigentes; de lo contrario usa los valores de esa League.

`PredictionEvaluationService` y la validación de Jugador Preferido resuelven por
`Prediction.LeagueId`. El resultado oficial continúa siendo único en `Matches`.

## Compatibilidad temporal

`EditionScoringConfigurations` se conserva por compatibilidad de esquema y con las
migraciones históricas, pero ya no participa en predicciones, Jugador Preferido ni
evaluación. Su endpoint antiguo permanece oculto de la navegación ADMIN y debe retirarse
en una migración destructiva futura, cuando se decida eliminar definitivamente la deuda.
