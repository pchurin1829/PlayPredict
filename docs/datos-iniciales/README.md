# Base Inicial PlayPredict v1.0

Este directorio contiene los artefactos versionados necesarios para reconstruir la primera base real de PlayPredict, con corte al 2026-09-01.

## Contenido

- `PlayPredict_Base_Inicial_v1.0_2026-09-01.xlsx`: definición de competencias, usuarios, participación y fixture oficial de las Fechas 8 a 11.
- `PlayPredict_Planteles_Clausura_AFA_2026_v2.xlsx`: 30 equipos y 1.045 jugadores preparados para el importador.
- `playpredict_base_inicial_v1.sql`: dump lógico completo y opcional, generado desde una instalación limpia.

El mecanismo canónico de reconstrucción es el seeder `--seed-initial-v1`, que aplica los importadores XLS existentes. El dump es una alternativa para restaurar exactamente la instantánea materializada sin volver a procesar los XLS.

## Requisitos

- Git.
- Docker Desktop con Docker Compose, o PostgreSQL 18 accesible.
- .NET SDK 10.
- Node.js 22 y npm, si el frontend se ejecutará fuera de Docker.

No se necesita copiar ningún volumen, `pgdata`, log, token ni archivo propio de la PC original.

## Reconstrucción canónica desde los XLS

Desde la raíz del repositorio:

```powershell
docker compose up -d db
dotnet restore backend/PlayPredict.Api.csproj
dotnet run --project backend/PlayPredict.Api.csproj -- --seed-initial-v1
docker compose up -d --build backend frontend
docker compose ps
```

El comando del seeder:

1. comprueba y aplica las migraciones pendientes;
2. elimina datos demo/reemplazables, sin eliminar el esquema;
3. crea empresa y roles base;
4. crea `ADMIN` y `USUARIO` usando `PasswordHasher<User>`;
5. importa equipos y planteles mediante `TeamRosterImportConfirmationService`;
6. importa exclusivamente los 60 partidos programados mediante `MatchImportConfirmationService`;
7. crea la referencia AFA y `COPA EL NENE`, enlazadas con `SourceLeagueId`;
8. incorpora `USUARIO` solamente a `COPA EL NENE`.

El seeder es deliberadamente explícito y destructivo respecto de los datos reemplazables. Nunca se ejecuta durante un arranque normal.

Si los XLS están en otra ubicación:

```powershell
dotnet run --project backend/PlayPredict.Api.csproj -- --seed-initial-v1 `
  --initial-base=C:\ruta\PlayPredict_Base_Inicial_v1.0_2026-09-01.xlsx `
  --initial-rosters=C:\ruta\PlayPredict_Planteles_Clausura_AFA_2026_v2.xlsx
```

## Restauración alternativa desde el dump

El dump contiene esquema, historial de migraciones y datos. Se restaura directamente sobre una base PostgreSQL vacía; no se ejecutan migraciones antes de importarlo.

Con una base nueva y vacía:

```powershell
docker compose up -d db
Get-Content -Raw docs/datos-iniciales/playpredict_base_inicial_v1.sql |
  docker exec -i playpredict_db psql -v ON_ERROR_STOP=1 -U playpredict_user -d playpredict_db
docker compose up -d --build backend frontend
```

La base destino debe estar vacía. No se debe ejecutar previamente ni el seeder XLS ni las migraciones, porque el dump ya contiene ambos resultados. Para instalaciones mantenibles y futuras actualizaciones se recomienda el mecanismo canónico desde migraciones + XLS.

## Validaciones esperadas

```sql
SELECT COUNT(*) FROM "Leagues";      -- 2 competencias funcionales
SELECT COUNT(*) FROM "Users";        -- 2
SELECT COUNT(*) FROM "Teams";        -- 30
SELECT COUNT(*) FROM "TeamPlayers";  -- 1045
SELECT COUNT(*) FROM "Matches";      -- 60
SELECT COUNT(*) FROM "Predictions";  -- 0

SELECT "Position", COUNT(*)
FROM "TeamPlayers"
GROUP BY "Position"
ORDER BY "Position";

SELECT r."Order", COUNT(*)
FROM "Matches" m
JOIN "Rounds" r ON r."Id" = m."RoundId"
GROUP BY r."Order"
ORDER BY r."Order";
```

Resultados por posición:

- Arquero: 94
- Defensor: 346
- Mediocampista: 318
- Delantero: 287

Partidos: Fechas 8, 9, 10 y 11 con 15 partidos cada una. No deben existir Fechas 12 a 16 ni pronósticos iniciales.

## Credenciales de prueba

- `ADMIN` / clave inicial indicada en el XLS de configuración.
- `USUARIO` / clave inicial indicada en el XLS de configuración.

La base almacena solamente hashes generados por el mecanismo normal de PlayPredict. Este documento no contiene hashes, tokens ni claves de conexión adicionales.
