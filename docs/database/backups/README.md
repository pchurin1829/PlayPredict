# DB0 oficial de PlayPredict

## Qué es DB0

DB0 es el snapshot oficial limpio de la Base Inicial PlayPredict v1. Permite recuperar rápidamente una instalación, demo o entorno de pruebas desde un estado conocido.

El archivo oficial es:

```text
DB0_PlayPredict_BaseInicial_v1_2026-09-01.sql
```

Es un dump SQL plano de PostgreSQL que incluye el esquema, el historial de migraciones y los datos. No incluye propietarios ni privilegios específicos de la instalación que lo generó.

## Contenido esperado

```text
Migraciones: 22
Competition: 1
Ligas: 2
Usuarios: 2
Equipos: 30
Jugadores: 1.045
Partidos: 60
Pronósticos: 0
Evaluaciones: 0
Preferencias: 0
```

Ligas:

```text
Torneo Clausura AFA 2026
COPA EL NENE
```

`Torneo Clausura AFA 2026` es la referencia deportiva y no es jugable. `COPA EL NENE` es la liga oficial jugable y referencia a la primera mediante `SourceLeagueId`.

Usuarios DEMO:

```text
ADMIN / admin123
USUARIO / usuario
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
