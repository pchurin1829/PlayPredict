# PLAYPREDICT DEMO_BASE_v1

Fecha: 2026-08-25

Base limpia para comenzar la demo desde `ADMIN → Competencias EL NENE → Nueva Competencia EL NENE`.

## Contenido

- Company: EL NENE SA (`EL NENE`).
- 3 Competencias de referencia: Copa Libertadores, Liga Profesional de Fútbol y Copa Argentina.
- 3 Editions 2026.
- 10 Fechas.
- 30 Partidos, todos sin resultado.
- 25 Equipos reales.
- 181 TeamPlayers.
- 5 planteles completos de prueba: Boca Juniors, River Plate, Flamengo, Palmeiras y Atlético Nacional.
- Leagues: 0.
- LeagueParticipants: 0.
- Predictions: 0.
- PredictionEvaluations: 0.
- Resultados: 0.
- Goleadores: 0.

## Configuración general de juego

- Marcador exacto: 6 puntos.
- Resultado correcto: 3 puntos.
- Incorrecto: 0 puntos.
- Jugador Preferido: habilitado.
- Puntos por gol del Jugador Preferido: 2.
- Posiciones habilitadas: Mediocampista y Delantero.

La configuración general persiste en Company. Las nuevas Leagues usan esta configuración por default y pueden guardar una configuración propia.

## Backup

Archivo: `PlayPredict_DEMO_BASE_v1_2026-08-25.dump`

Es un dump PostgreSQL completo en formato custom, restaurable con `pg_restore`.

## Restore limpio con Docker Compose

Desde la raíz del repositorio, con el dump dentro de `backups`:

```powershell
docker compose stop backend frontend
docker cp .\backups\PlayPredict_DEMO_BASE_v1_2026-08-25.dump playpredict_db:/tmp/PlayPredict_DEMO_BASE_v1_2026-08-25.dump
docker exec playpredict_db dropdb -U playpredict_user --if-exists playpredict_db
docker exec playpredict_db createdb -U playpredict_user playpredict_db
docker exec playpredict_db pg_restore -U playpredict_user -d playpredict_db --exit-on-error /tmp/PlayPredict_DEMO_BASE_v1_2026-08-25.dump
docker compose up -d
```

Estos comandos eliminan y recrean únicamente `playpredict_db` dentro del contenedor PlayPredict. No deben ejecutarse contra otro contenedor o servidor PostgreSQL.
