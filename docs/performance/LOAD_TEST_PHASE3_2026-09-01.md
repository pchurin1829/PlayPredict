# PlayPredict — Load Test Fase 3 (2026-09-01)

Este documento congela el baseline reproducible de la Fase 3. Separa deliberadamente la capacidad de autenticación de la capacidad de uso normal de un PLAYER que inicia sesión una vez y reutiliza su JWT.

## Hardware de referencia

- Intel i5-11400F, 6 núcleos / 12 hilos.
- 15,84 GiB de RAM en el host.
- Docker: aproximadamente 8,24 GB y 12 CPU lógicas disponibles.

## LOGIN aislado

| Login concurrente | Login/s | Error | p95 |
| ---: | ---: | ---: | ---: |
| 25 | 80,58 | 0 % | 407 ms |
| 50 | 84,21 | 0 % | 816 ms |
| 100 | 84,60 | 0 % | 1,78 s |
| 200 | 83,88 | 0 % | 3,26 s |

En este hardware, la capacidad observada es de aproximadamente 81–85 autenticaciones/s, limitada principalmente por PBKDF2 y CPU. Este benchmark mide autenticaciones simultáneas; **no representa la capacidad general de usuarios PLAYER concurrentes**.

Script: `tests/load/login.phase3.js`.

## PLAYER autenticado

Cada VU inicia sesión una sola vez durante el ramp-up y reutiliza el JWT. En steady-state, la mezcla es 30 % partidos, 25 % lectura de pronósticos, 20 % alta/modificación de pronóstico, 15 % ranking y 10 % ligas, con think-time aleatorio de 0,5–1,5 s.

| VUs | RPS steady | Error | p95 | p99 |
| ---: | ---: | ---: | ---: | ---: |
| 250 | 247,93 | 0 % | 11,99 ms | 16,17 ms |
| 500 | 494,78 | 0 % | 16,80 ms | 27,83 ms |

A 500 VUs se observaron aproximadamente 2,48 CPUs en backend, 2,05 CPUs en PostgreSQL, 41 conexiones DB de pico, 1–6 conexiones activas y 0 locks. La clasificación de 500 PLAYER autenticados es **HOLGADO**.

Script: `tests/load/player_session.phase3.js`.

## Ranking A/B

| Versión | Rankings/s | p95 |
| --- | ---: | ---: |
| Original | 171,40 | 328,10 ms |
| Optimizada | 343,09 | 26,50 ms |

La versión optimizada reduce materialización y transferencia de filas y evita saturar el backend. Se decidió conservar la optimización de `RankingService`.

Script: `tests/load/ranking.phase3.js`.

## Integridad

- Predictions: 2.500.
- Duplicados `UserId + MatchId`: 0.
- Evaluaciones: 2.000.
- Duplicados `PredictionId + LeagueId`: 0.
- Deadlocks: 0.
- Locks pendientes: 0.

## Reproducción del entorno aislado

1. Crear `.env.loadtest` desde `.env.loadtest.example`, configurar secretos exclusivos y no productivos, y establecer `LOADTEST_USER_COUNT=1000`. No versionar ese archivo.
2. Levantar PostgreSQL aislado:

   ```powershell
   docker compose --env-file .env.loadtest -f docker-compose.loadtest.yml up -d db-loadtest
   ```

3. Aplicar migraciones y sembrar exclusivamente con el dataset de carga:

   ```powershell
   docker compose --env-file .env.loadtest -f docker-compose.loadtest.yml run --rm --no-deps backend-loadtest dotnet run --no-launch-profile -- --seed-loadtest
   ```

4. Levantar el backend aislado:

   ```powershell
   docker compose --env-file .env.loadtest -f docker-compose.loadtest.yml up -d backend-loadtest
   Invoke-RestMethod http://localhost:18006/api/health
   ```

5. Ejecutar el siguiente control con 750 PLAYER autenticados:

   ```powershell
   docker compose --env-file .env.loadtest -f docker-compose.loadtest.yml --profile smoke run --rm -e TARGET_VUS=750 -e RAMP_SECONDS=90 -e STEADY_SECONDS=180 k6-smoke run /scripts/player_session.phase3.js
   ```

La base debe ser exclusivamente la definida por el entorno load-test (por defecto `playpredict_loadtest`), nunca `playpredict_db` ni DB0.

## Próximo test

```text
750 PLAYER autenticados
↓
si estable:
1.000 PLAYER autenticados
```

No mezclar login repetitivo con el escenario PLAYER.
