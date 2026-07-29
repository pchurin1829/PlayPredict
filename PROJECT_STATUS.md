# PROJECT STATUS

Versión: 0.2.0
Estado: Sprint 2 completado — módulo base del fixture administrable (Competencia → Edición → Fecha → Partido) funcionando de punta a punta, con panel administrativo en el frontend.
Próximo paso: Sprint 3 (ETAPA 2 del PLAN_IMPLEMENTACION_MVP — Usuarios: Registro, Login, Perfil).

---

## Sprint 2 — Módulo base del fixture administrable

### Backend

- Entidades de dominio (`backend/Domain/Entities/`): `Competition`, `Edition`, `Round`, `Match`. Enums (`backend/Domain/Enums/`): `EditionStatus` (Draft, Active, Finished, Cancelled), `MatchStatus` (Scheduled, InProgress, Finished, Suspended, Cancelled).
- Configuraciones EF Core separadas por entidad (`backend/Data/Configurations/`, `IEntityTypeConfiguration<T>`): claves primarias, claves foráneas con `DeleteBehavior.Restrict` (sin borrado físico en cascada), longitudes máximas, índices (`Competition.Name`; `Edition(CompetitionId, Name)` único; `Round(EditionId, Order)` único; `Match.RoundId`, `Match.StartsAtUtc`).
- `PlayPredictDbContext` actualizado con los 4 `DbSet` y `ApplyConfigurationsFromAssembly`.
- DTOs de lectura/creación/actualización (`backend/Dtos/`) para las 4 entidades, más `MatchResultDto` para el resultado oficial.
- Validaciones explícitas en los endpoints (nombre/participantes obligatorios, longitudes máximas, fechas coherentes, estado válido, goles no negativos, orden de Fecha único con manejo de conflicto 409).
- Endpoints REST (`backend/Endpoints/`, minimal API) — ver detalle más abajo.
- Migración inicial `InitialFixtureSchema` creada y aplicada a PostgreSQL 18 (tablas `Competitions`, `Editions`, `Rounds`, `Matches`). El backend aplica migraciones pendientes automáticamente al iniciar (`db.Database.MigrateAsync()`).
- Seed de desarrollo idempotente (`backend/Data/DataSeeder.cs`), ejecutado solo en `Development`: Competencia "Liga Profesional" → Edición "Clausura 2026" → Fecha "Fecha 1" → 3 partidos programados. Verifica existencia por nombre antes de insertar; no duplica datos en reinicios sucesivos (verificado).
- Regla `GolesLocal`/`GolesVisitante` solo informables para un Partido Finalizado: el estado `Finished` únicamente puede establecerse vía `PUT /api/matches/{id}/result`; intentarlo desde `PUT /api/matches/{id}` devuelve 400.
- Punto de extensión documentado (comentario) en el endpoint de resultado para el futuro recálculo de Pronósticos/Rankings — sin implementar todavía.
- Nota: se agregó `GET /api/rounds/{id}` (no estaba en la lista original de endpoints) porque la pantalla de edición de Fecha lo necesita para cargar los datos actuales, igual que existe para Competencias, Ediciones y Partidos.

### Endpoints disponibles

```
GET    /api/competitions
GET    /api/competitions/{id}
POST   /api/competitions
PUT    /api/competitions/{id}

GET    /api/competitions/{competitionId}/editions
GET    /api/editions/{id}
POST   /api/competitions/{competitionId}/editions
PUT    /api/editions/{id}

GET    /api/editions/{editionId}/rounds
GET    /api/rounds/{id}
POST   /api/editions/{editionId}/rounds
PUT    /api/rounds/{id}

GET    /api/rounds/{roundId}/matches
GET    /api/matches/{id}
POST   /api/rounds/{roundId}/matches
PUT    /api/matches/{id}
PUT    /api/matches/{id}/result
```

### Frontend

- Panel administrativo con React Router (`frontend/src/pages/`, `frontend/src/components/`): listas y formularios de alta/edición para Competencias, Ediciones, Fechas y Partidos, más modal de carga de Resultado Oficial.
- Cliente API centralizado (`frontend/src/api/client.ts`) con manejo de errores de validación (400) y mensajes claros por campo.
- Estados de carga, mensajes de error y confirmación "guardado correctamente" en cada formulario.
- Navegación jerárquica: Competencias → Ediciones → Fechas → Partidos, con breadcrumbs de vuelta.
- Responsive básico (tablas con scroll horizontal, formularios en columna en pantallas angostas).
- Se reemplazó la pantalla inicial de estado del backend (Sprint 1) por el panel administrativo; `/` redirige a `/competitions`.
- Dependencia agregada: `react-router-dom` (última versión, 7.18.2). `npm audit` reporta una vulnerabilidad alta restante específica de "RSC Mode" (Server Components/Server Actions); no aplica a este proyecto porque el frontend es una SPA 100% cliente sin SSR ni RSC.

### Datos de demostración

Seed automático en `Development` (ya descrito arriba). Verificado tras reinicio del backend: no se duplican registros.

---

## Ajuste posterior al Sprint 1 (cierre de infraestructura)

- PostgreSQL actualizado de `postgres:16-alpine` a `postgres:18-alpine` en `docker-compose.yml`.
- Volumen de datos reconfigurado: a partir de PostgreSQL 18 la imagen oficial versiona el PGDATA internamente (`/var/lib/postgresql/<major>/docker`) y declara el `VOLUME` en `/var/lib/postgresql`. Se cambió el mount de `playpredict_db_data` de `/var/lib/postgresql/data` a `/var/lib/postgresql`.
- El volumen anterior (creado con PostgreSQL 16, sin datos de negocio) se eliminó con `docker compose down -v` antes de recrear el entorno.
- Se confirmó que `backend/Program.cs` no contenía código de plantilla (`WeatherForecast`, `summaries`, endpoints/records de ejemplo); ya estaba limpio desde el Sprint 1.
- Se inicializó el repositorio Git en la raíz (`git init`) y se fijó la rama principal como `main` (`git branch -M main`). No se realizó ningún commit ni se conectó un remoto.
- Se revisó `.gitignore`: cubre correctamente `.env`, `bin/`, `obj/`, `node_modules/`, `dist/` y archivos temporales de IDE.

---

## Qué se creó (Sprint 1)

- **Backend** (`backend/`): ASP.NET Core Web API (.NET 10), estilo minimal API.
  - Swagger/OpenAPI (Swashbuckle) habilitado en `/swagger`.
  - CORS habilitado para `http://localhost:5175` y `http://127.0.0.1:5175`.
  - `GET /api/health` → `{ "status": "ok" }`.
  - `GET /api/system/info` → `{ "application": "PlayPredict", "status": "running", "version": "0.1.0" }`.
  - Entity Framework Core + Npgsql configurado con `PlayPredictDbContext`.
  - `Dockerfile.dev` con hot reload (`dotnet watch run`).
- **Frontend** (`frontend/`): React + Vite + TypeScript. `Dockerfile.dev` con `vite --host`.
- **Infraestructura**: `docker-compose.yml` (proyecto `playpredict-dev`) con servicios `db` (PostgreSQL 18), `backend`, `frontend`, healthchecks, volúmenes de desarrollo y dependencias ordenadas.
- **Configuración**: `.env.example`, `.gitignore` (raíz), `README.md` con instrucciones reales de uso.

## Qué funciona (verificado en esta sesión — Sprint 2)

- `dotnet build` del backend: OK, sin errores ni advertencias.
- `npm run build` del frontend (tsc + vite build): OK.
- `docker compose config`: válido.
- `docker compose up -d --build`: los 3 servicios levantan correctamente (se recreó el volumen `node_modules` del frontend, que había quedado desactualizado tras agregar `react-router-dom`).
- `docker compose ps`: los 3 servicios `healthy`/`Up`.
- Migración `InitialFixtureSchema` aplicada; tablas `Competitions`, `Editions`, `Rounds`, `Matches` verificadas en PostgreSQL.
- Los 13 endpoints probados manualmente vía `curl`: altas, lecturas, actualizaciones, validaciones (400), conflicto de orden (409), 404 en recursos padre inexistentes, y la regla de Resultado Oficial (solo vía `/result`, goles no negativos).
- Persistencia verificada directamente en PostgreSQL (`psql`) para Competencias, Ediciones y Partidos, incluyendo el resultado cargado.
- Seed de desarrollo verificado idempotente tras reinicio del backend.
- `http://localhost:8006/swagger` → 200, expone los 13 endpoints nuevos.
- `http://localhost:5175` → 200; proxy `/api/*` funcionando contra el backend real.
- Logs de `backend`, `frontend` y `db` revisados: sin errores no esperados (los dos mensajes de error en el log de `db` corresponden al chequeo inicial estándar de EF Core antes de crear `__EFMigrationsHistory` y a una prueba intencional de conflicto de orden de Fecha).

## Comandos de ejecución

```bash
cp .env.example .env          # primera vez
docker compose up -d --build  # levantar
docker compose ps             # verificar estado
docker compose logs -f backend
docker compose down           # detener
docker compose down -v        # detener y borrar datos de Postgres
```

Migraciones EF Core (desde `backend/`, con `dotnet-ef` instalado — `dotnet tool install --global dotnet-ef`):

```bash
dotnet ef migrations add NombreMigracion --output-dir Migrations
dotnet ef database update
```

## Puertos

| Servicio   | Puerto host | Puerto interno |
|------------|-------------|-----------------|
| Frontend   | 5175        | 5175            |
| Backend    | 8006        | 8080            |
| PostgreSQL | 5436        | 5432            |

## Próximo paso exacto

Iniciar Sprint 3 / ETAPA 2 del `PLAN_IMPLEMENTACION_MVP.md`: Usuarios (Registro, Login, Perfil). No iniciar sin aprobación explícita.

## Notas

- Repositorio Git inicializado en la raíz del proyecto, rama `main`. Aún sin commits y sin remoto conectado (pendiente de autorización).
- La base de datos de desarrollo contiene, además de los datos del seed, algunos registros creados manualmente durante las pruebas de esta sesión (competencia "Copa Libertadores", edición "Apertura 2026", una Fecha y un Partido con resultado cargado). No se eliminaron porque el sprint no incluye endpoints de borrado; no afectan el funcionamiento y sirven como evidencia de que el CRUD completo funciona.
- No se verificó visualmente en navegador (herramientas de navegador no habilitadas en esta sesión); se validó por HTTP/proxy y por inspección directa de PostgreSQL.
