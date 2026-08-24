# PlayPredict — Jugador Preferido MVP

Fecha: 2026-08-23
Rama: `prueba-glm-ui`

## 1. Modelo de TeamPlayer

`TeamPlayer` representa al deportista y no se mezcla con `User`. Contiene TeamId, nombre y apellido obligatorios, apodo, dorsal, posición y PhotoUrl opcionales, además de estado. La FK a Team usa `Restrict`. Por compatibilidad, el campo persistido `DisplayName` se reutiliza como apodo: no reemplaza ni elimina `FirstName`/`LastName`.

## 2. Relación con Team y CRUD

Cada Team expone `/api/teams/{teamId}/players`. ADMIN puede listar, crear, editar y eliminar desde Equipos → Plantel. Los formularios conservan Volver, Guardar y Cancelar. La eliminación física sólo se permite sin referencias; pronósticos o goleadores asociados producen HTTP 409 y el estado Activo queda disponible como alternativa conservadora.

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

## 14. Ajuste visual y exposición final

### Causa del selector invisible

La card condicionaba todo el bloque a que ya existiera al menos un jugador activo. Además, el seeder inicial sólo completaba los seis equipos del fixture de Liga Profesional; encuentros como Argentinos Juniors–Belgrano recibían planteles vacíos. Por eso la Edition informaba `PreferredPlayerEnabled=true`, pero la UI ocultaba silenciosamente el control.

Se corrigió en dos niveles: la card muestra siempre el bloque cuando la función está habilitada y el Match está editable, informando “No hay jugadores disponibles para seleccionar” si corresponde; el seeder idempotente completa tres jugadores claramente ficticios para cada Team activo. El select conserva optgroups y sólo recibe planteles del local/visitante.

### Predictions existentes

POST y PUT comparten `PreferredPlayerId` nullable. Un marcador 1-2 existente puede incorporar, cambiar o quitar jugador sin recrearse ni alterar sus goles. La prueba real confirmó crear, refrescar, cambiar y limpiar manteniendo 1-2. Tras el cierre, PUT devuelve 400 y la elección queda sólo lectura.

### TeamPlayer, apodo y foto

Alta/edición usa controles uniformes de 44 px. Posición es un select MVP extensible en frontend (Arquero, Defensor, Mediocampista y Delantero, más vacío). La UI reemplaza “Nombre visible” por “Apodo (opcional)”. Administración siempre muestra Nombre + Apellido y, cuando existe, el apodo como información secundaria. Jugador Preferido usa el mismo formato inequívoco y no modifica scoring.

La foto ya no se ingresa como URL. El formulario admite arrastrar o seleccionar JPG/JPEG, PNG o WEBP, muestra preview, permite cambiar y quitar, y usa iniciales como fallback. El navegador valida hasta 8 MB y recorta/redimensiona a 512×512, convirtiendo a WEBP con calidad 0,82 antes de subir. Backend vuelve a validar tipo, firma y un máximo optimizado de 1,5 MB.

Los archivos se guardan en `backend/wwwroot/uploads/team-players`, excluido de Git, y la base conserva únicamente `PhotoUrl` bajo `/api/uploads/team-players/...`. Los endpoints autenticados de carga y eliminación están separados del CRUD, facilitando sustituir el filesystem por almacenamiento cloud sin cambiar el modelo funcional. Reemplazar o quitar una foto elimina solamente archivos administrados dentro de esa ruta; URLs históricas externas se preservan de forma compatible hasta que el usuario las reemplace o quite.

### Eliminación

El listado incorpora Editar/Eliminar con confirmación. DELETE comprueba `Prediction.PreferredPlayerId` y `MatchScorer.TeamPlayerId`; con referencias devuelve 409 y no borra. Sin dependencias devuelve 204. La desactivación mediante `Active` sigue disponible como alternativa conservadora.

### Configuración general

La UI reemplaza “Experience” por “configuración general de puntuación” y explica la herencia. Actualmente los valores generales provienen de la Experience asociada a la Competition; ese detalle interno queda documentado, no expuesto como jerga al ADMIN. El orden visual termina con un bloque separado de Jugador Preferido.

### Inputs, botones y mobile

Tokens ADMIN centralizados controlan fondo, borde, texto y foco de inputs/selects. Los campos ya no son blancos sobre blanco. Primarios y secundarios ganaron contraste, hover/focus y separación de 1 rem. Formulario de jugador y selector PLAYER se apilan sin overflow a 400–456 px; el select mantiene 44 px táctiles.

### Pruebas del ajuste

- Selección válida: persistió tras refrescar con nombre e ID correctos.
- Cambio y limpieza: persistieron conservando el marcador 1-2.
- Partido cerrado: actualización rechazada con 400.
- Borrado sin dependencias: 204 y limpieza del registro temporal.
- Borrado referenciado: 409, registro preservado.
- Plantel utilizado por card: seis opciones, exclusivamente de ambos Teams.
- Scoring confirmado en código: `PreferredPlayerPoints = Goals × PreferredPlayerPointsPerGoal`; `Points = ResultPoints + PreferredPlayerPoints`, independiente del tipo de acierto.
- Alta temporal sin foto y fallback sin `PhotoUrl`.
- Upload real y acceso de la imagen a través de `localhost:5175`: HTTP 200.
- Reemplazo: nueva foto disponible y archivo administrado anterior eliminado (HTTP 404).
- Quitar foto: `PhotoUrl=null` y archivo eliminado (HTTP 404).
- Tipo no admitido y archivo optimizado demasiado grande: HTTP 400.
- TypeScript, build frontend, build backend y `git diff --check` exitosos.
