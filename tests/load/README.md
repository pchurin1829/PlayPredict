# PlayPredict LOADTEST

Este directorio contiene el smoke test k6 de un unico usuario. El entorno asociado es
`docker-compose.loadtest.yml`: usa PostgreSQL, backend, red y volumenes exclusivos y no
comparte `playpredict_db_data` con Development.

## Protecciones

El seeder solo corre cuando se cumplen simultaneamente estas condiciones:

1. se invoca el backend con `--seed-loadtest`;
2. `LoadTest__Enabled=true`;
3. `ASPNETCORE_ENVIRONMENT=LoadTest`;
4. el nombre de la base contiene `loadtest`;
5. cantidad de usuarios entre 1 y 10000;
6. password de usuarios LOADTEST de al menos 8 caracteres.

Copiar `.env.loadtest.example` como `.env.loadtest` y reemplazar todas las credenciales
de ejemplo antes de iniciar. `.env.loadtest` esta ignorado por Git.

## Dataset

- Usuarios configurables: `loadtest00001@playpredict.test`, etc.
- Liga oficial: `PLAYPREDICT LOADTEST OFFICIAL`.
- Cuatro equipos ficticios y cuatro jugadores por equipo.
- Dos partidos pasados con pronosticos y evaluaciones historicas.
- Cuatro partidos futuros para alta y modificacion de pronosticos.
- Cada usuario conserva una participacion historica cerrada para el ranking y comienza
  sin participacion activa, por lo que el primer smoke ejecuta realmente `join`.

El seeder es idempotente para reejecuciones y permite aumentar la poblacion. Para volver
completamente a cero se debe eliminar solo el volumen `playpredict_loadtest_db_data`,
nunca el volumen de Development.

## Smoke test

El script `smoke.js` descubre la Liga Oficial y el partido por API; no usa IDs fijos.
Ejecuta: login, listado y participacion en liga, consulta de partidos, consulta de
pronosticos de la fecha, alta/upsert, modificacion a 2-1 y consulta de ranking.

Metricas etiquetadas: `LOGIN`, `LEAGUES`, `MATCHES`, `PREDICTION_GET`,
`PREDICTION_SAVE` y `RANKING`.

Thresholds iniciales:

- `http_req_failed`: menos de 1 %;
- `http_req_duration`: p95 menor a 500 ms.

El escenario queda deliberadamente fijado en `vus: 1` e `iterations: 1`. No aumentar
concurrencia hasta validar aislamiento, dataset y smoke.

## Arranque controlado

Desde la raiz del repositorio, una vez iniciado Docker Desktop manualmente:

```powershell
Copy-Item .env.loadtest.example .env.loadtest
# Editar .env.loadtest y reemplazar las credenciales de ejemplo.

docker compose --env-file .env.loadtest -f docker-compose.loadtest.yml up -d db-loadtest
docker compose --env-file .env.loadtest -f docker-compose.loadtest.yml ps

docker compose --env-file .env.loadtest -f docker-compose.loadtest.yml run --rm --no-deps backend-loadtest dotnet run --no-launch-profile -- --seed-loadtest
docker compose --env-file .env.loadtest -f docker-compose.loadtest.yml up -d backend-loadtest

Invoke-RestMethod http://localhost:18006/api/health

docker compose --env-file .env.loadtest -f docker-compose.loadtest.yml --profile smoke run --rm k6-smoke run /scripts/smoke.js
```

Para cambiar la poblacion antes de recrear/sembrar el entorno, ajustar
`LOADTEST_USER_COUNT` en `.env.loadtest`. Valores previstos: 100, 500, 1000, 2500,
5000 y 10000.

## Proximos escenarios

Despues de aprobar el smoke de un usuario, preparar escenarios separados y graduales
para 50, 100, 250 y 500 VUs. Mantener por separado la preparacion de usuarios y la
carga del flujo de juego; medir primero login, participacion, lectura de partidos,
alta/modificacion de pronosticos y ranking sin optimizar endpoints anticipadamente.
Antes de cada escala registrar version de k6, cantidad de usuarios sembrados, duracion,
thresholds y estado inicial del volumen LOADTEST.
