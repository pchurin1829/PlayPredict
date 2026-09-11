# DB0 oficial de PlayPredict

## Qué es DB0

DB0 es el snapshot oficial limpio de la Base Inicial PlayPredict v1. Permite recuperar rápidamente una instalación, demo o entorno de pruebas desde un estado conocido.

El archivo oficial es:

```text
DB0_PlayPredict_BaseInicial_v1_2026-09-01.sql
```

Es un dump SQL plano de PostgreSQL que incluye el esquema, el historial de migraciones y los datos. No incluye propietarios ni privilegios específicos de la instalación que lo generó.

## DB0 vigente — corrección de email del 2026-09-10

Regenerado desde una base PostgreSQL vacía con las migraciones y el procedimiento
oficial `--seed-initial-v1`, usando los XLS versionados. Incorpora las identidades
`admin@playpredict.local` / `usuario@playpredict.local` y las 23 migraciones actuales.
No incorpora datos creados durante la validación manual. Se conserva el nombre
del archivo canónico para no romper las referencias existentes.

La reconstrucción y el restore se verifican en bases aisladas. El restore usa
`psql --single-transaction -v ON_ERROR_STOP=1` sobre una base vacía, sin seeder ni
migraciones previas. Ambos logins canónicos deben funcionar antes y después del
restore; `admin` y `usuario` deben devolver 401. No se imprimen PasswordHash ni tokens.

## Cierre anterior — histórico, sustituido por la corrección de email

Cierre aprobado el 2026-09-08 (opción B). En ese cierre se conservó el siguiente DB0; este hash es histórico y ya no identifica el archivo vigente:

```text
docs/database/backups/DB0_PlayPredict_BaseInicial_v1_2026-09-01.sql
SHA-256: 3FCE46A9AE1FAFA4A2AFE4A087F84E6A079FE2552980749A3D2B9FC7AD4D231E
```

Una reconstrucción independiente generó una base funcionalmente equivalente al DB0. La comparación de esquema, migraciones y datos confirmó que el candidato no incorporaba ninguna corrección material que justificara reemplazarlo. Sus diferencias eran identificadores y secuencias, timestamps, hashes demo, códigos de invitación, orden de filas y versión generadora del dump. Por ello se elimina el archivo `.candidate.sql` y se conserva el canónico.

## Contenido vigente esperado y validado

```text
Migraciones: 23/23
Head: 20260909012331_AddMatchScorerOwnGoals
Competitions: 1
Leagues: 2
Users: 2
Teams: 30
Players (TeamPlayers): 1.045
Matches: 60
Participations (LeagueParticipants): 1
Predictions: 0
PredictionEvaluations: 0
PreferredPlayers (UserTeamPreferredPlayers): 0
Goleadores registrados (MatchScorers): 0
FK validadas: 38
```

Sin FK rotas, datos huérfanos, duplicados relevantes, datos load-test ni residuos de tests. Los 60 partidos están programados (`Scheduled`), sin resultados cargados: Fechas 8, 9, 10 y 11, con 15 partidos cada una. La única participación corresponde a `usuario@playpredict.local` en `COPA EL NENE`.

Historial del cierre anterior (2026-09-08): el restore del DB0 anterior y del candidato se probó en PostgreSQL 18.4 independiente; reproducía 22 migraciones y 37 FK. Esa validación histórica no sustituye la reconstrucción y el restore de la revisión actual.

Ligas:

```text
Torneo Clausura AFA 2026
COPA EL NENE
```

`Torneo Clausura AFA 2026` es la referencia deportiva y no es jugable. `COPA EL NENE` es la liga oficial jugable y referencia a la primera mediante `SourceLeagueId`.

Usuarios DEMO:

```text
ADMIN: admin@playpredict.local / admin123
PLAYER: usuario@playpredict.local / usuario
```

Estas contraseñas son exclusivamente para DEMO. La base almacena sus hashes normales de PlayPredict, no las contraseñas en texto plano.

## Restauración

Los siguientes comandos se ejecutan desde la raíz del repositorio. La restauración reemplaza la base indicada: usar únicamente una PostgreSQL vacía o una base descartable cuyo contenido pueda eliminarse.

1. Levantar PostgreSQL y detener los procesos que podrían conectarse mientras se restaura:

```powershell
docker compose up -d db
docker compose stop backend frontend
```

2. Crear una base vacía. En una instalación nueva, reemplazar `playpredict_db` sólo si se confirmó que no contiene datos que deban conservarse:

```powershell
docker compose exec -T db dropdb --if-exists -U playpredict_user playpredict_db
docker compose exec -T db createdb -U playpredict_user -T template0 playpredict_db
```

3. Restaurar exclusivamente DB0:

```powershell
Get-Content -Raw docs/database/backups/DB0_PlayPredict_BaseInicial_v1_2026-09-01.sql |
  docker compose exec -T db psql -v ON_ERROR_STOP=1 -U playpredict_user -d playpredict_db
```

No ejecutar migraciones ni seeders antes de esta importación: DB0 ya contiene el esquema, `__EFMigrationsHistory` y los datos.

4. Levantar backend y frontend:

```powershell
docker compose up -d --build backend frontend
docker compose ps
```

5. Verificar health y acceso local:

```powershell
Invoke-WebRequest -UseBasicParsing http://localhost:8006/api/health
Invoke-WebRequest -UseBasicParsing http://localhost:5175
```

6. Verificar cantidades:

```powershell
docker compose exec -T db psql -U playpredict_user -d playpredict_db -c '
SELECT
  (SELECT COUNT(*) FROM "__EFMigrationsHistory") AS migrations,
  (SELECT COUNT(*) FROM "Competitions") AS competitions,
  (SELECT COUNT(*) FROM "Leagues") AS leagues,
  (SELECT COUNT(*) FROM "Users") AS users,
  (SELECT COUNT(*) FROM "Teams") AS teams,
  (SELECT COUNT(*) FROM "TeamPlayers") AS players,
  (SELECT COUNT(*) FROM "Matches") AS matches,
  (SELECT COUNT(*) FROM "Predictions") AS predictions,
  (SELECT COUNT(*) FROM "PredictionEvaluations") AS evaluations,
  (SELECT COUNT(*) FROM "UserTeamPreferredPlayers") AS preferences;'
```

Si el proyecto usa nombres de base o usuario distintos, adaptar esos identificadores a la configuración local sin incorporar contraseñas ni otros secretos al repositorio.

## Relación con el seeder

Existen dos mecanismos válidos y complementarios:

### Mecanismo reproducible

```text
--seed-initial-v1
```

Reconstruye el dataset mediante los XLS versionados en `docs/datos-iniciales/`. El seeder y esos XLS continúan siendo la fuente reproducible de la Base Inicial.

### Snapshot rápido

```text
DB0_PlayPredict_BaseInicial_v1_2026-09-01.sql
```

DB0 es el snapshot congelado de una base reconstruida y validada. Se usa cuando interesa recuperar rápidamente el estado completo sin volver a procesar los XLS.

Este archivo DB0 es el único snapshot SQL oficial. No se mantienen dumps equivalentes adicionales en `docs/datos-iniciales/`.
