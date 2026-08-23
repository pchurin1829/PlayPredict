# PlayPredict — ADMIN: fechas, equipos y partidos

Fecha: 2026-08-22
Rama: `prueba-glm-ui`
Checkpoint de inicio del bloque: `80dea34`

## 1. Estado inicial

- `Edition`, `Round` y `Match` ya existían.
- Las Fechas se creaban exclusivamente una por una.
- No existía una entidad `Team`: `Match` persistía local y visitante como texto libre en `ParticipantHome` y `ParticipantAway`.
- La fecha/hora era requerida en backend, pero el formulario podía enviar un valor vacío.
- El listado de Ligas Oficiales no exponía dimensiones del fixture.

## 2. Modelo Team final

Se agregó `Team` con `Id`, `Name`, `ShortName`, `LogoUrl` opcional, `Sport` y `Active`. `Match` ahora tiene `HomeTeamId` y `AwayTeamId`, ambas FK obligatorias y con borrado restringido. Los nombres históricos permanecen en Match como snapshot compatible y se sincronizan con el nombre del Team al crear/editar.

## 3. Migración

Migración aditiva `20260822221310_AddTeamsAndMatchTeamReferences` aplicada correctamente. Crea Teams y agrega las dos FK; no elimina tablas, columnas ni registros.

## 4. Estrategia para Match existentes

La migración crea automáticamente un Team por cada nombre local/visitante distinto ya persistido, vincula todos los Matches y sólo entonces vuelve obligatorias las referencias. Verificación local: 32 Matches revisados, 0 sin Team. Se conservaron `ParticipantHome` y `ParticipantAway` para no romper contratos históricos.

## 5. Fechas automáticas

Crear o editar una Edition permite indicar la cantidad de Fechas/Jornadas. Se generan únicamente las faltantes (`Fecha N`, `Order N`, Id propio). Aumentar es incremental e idempotente; reducir devuelve un mensaje de protección y no elimina ninguna Fecha. La creación manual continúa disponible.

## 6. Validación de fecha/hora

El formulario usa `datetime-local`, lo marca requerido y evita la llamada si está vacío, mostrando “La fecha y hora del partido son obligatorias.”. Backend conserva la validación equivalente.

## 7. Selector de equipos

Nuevo/Editar Partido usa selects de Equipo local y visitante. Ambos son obligatorios y no pueden coincidir. El backend resuelve los Teams y persiste sus IDs; una prueba puntual con el mismo Team devolvió HTTP 400 sin escritura.

## 8. Equipos demo

Seeder idempotente con los 18 clubes solicitados: Boca Juniors, River Plate, Racing Club, Independiente, Estudiantes, Gimnasia, San Lorenzo, Huracán, Vélez, Rosario Central, Newell's, Talleres, Belgrano, Argentinos Juniors, Lanús, Banfield, Defensa y Justicia y Tigre. También preserva/crea equipos sudamericanos ya usados por los fixtures demo. Catálogo local actual: 32 Teams.

## 9. Logos

`LogoUrl` queda disponible y editable. No se descargaron imágenes ni se asociaron assets de licencia incierta. La UI funciona sin logo mediante nombre textual.

## 10. Resumen de fixture

El listado de Ligas Oficiales muestra `N fechas · X partidos`, derivado de Edition/Rounds/Matches. En ligas con rango se cuenta el alcance efectivo; no se duplica información en League.

## 11. Archivos modificados en este bloque

- Backend: entidades/configuraciones de Team y Match, DbContext, DTOs, endpoints de Edition/Match/Team/Liga Oficial, Program, DataSeeder, snapshot y migración.
- Frontend: tipos API, formularios de Edition y Match, catálogo/formulario de Equipos, rutas, sidebar y listado de Ligas Oficiales.
- Este informe.

## 12. Tests y validaciones

- Backend build: OK (0 errores; warning NU1510 preexistente).
- Integridad del backfill: 32 Matches, 0 referencias faltantes.
- Validación local/visitante iguales: HTTP 400.
- Frontend TypeScript/build: OK.
- `git diff --check`: ver cierre de ejecución.

## 13. Pendientes

- Los logos son opcionales y quedan con placeholder textual hasta contar con assets autorizados.
- No se implementaron planteles, jugadores ni importación masiva, fuera de alcance.

## 14. Estado Git

Sin commit ni push. Se preservan todos los cambios legítimos anteriores y los untracked locales deliberadamente excluidos. Servicios frontend/backend quedan levantados para prueba manual.
