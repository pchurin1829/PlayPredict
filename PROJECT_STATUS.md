# PROJECT STATUS

Versión: 0.3.0
Estado: Sprint 3 (Usuarios/Autenticación), Sprint 3.5 (limpieza funcional pre-Pronósticos) y el fix de edición de Partidos Finalizados completados, commiteados (`df43594`) y pusheados a `origin/main`.
Próximo paso: Sprint 4 (ETAPA 3 del PLAN_IMPLEMENTACION_MVP — Pronósticos). No iniciar sin aprobación explícita.

---

## Nota sobre esta actualización

Esta sesión comenzó tras un corte por error 529 en la sesión anterior. Al verificar el estado real (protocolo "Inicio Sesion"), se encontró que el Sprint 3 completo ya estaba implementado en el árbol de trabajo (compilando y corriendo en los contenedores), pero sin commit y sin reflejarse en `SESSION.md`/`PROJECT_STATUS.md`/`PLAN_DE_TRABAJO.md`. Con aprobación explícita del usuario, se dio el Sprint 3 por finalizado, se documentó, y a continuación se ejecutó el Sprint 3.5 solicitado.

---

## Sprint 3 — Usuarios y Autenticación

### Backend

- Entidades de dominio nuevas (`backend/Domain/Entities/`): `Company`, `Role`, `User`, `UserRole`. Constante `RoleNames` (`backend/Domain/Constants/RoleNames.cs`) con los roles `ADMIN` y `USER`.
- Configuraciones EF Core (`backend/Data/Configurations/`): `CompanyConfiguration`, `RoleConfiguration`, `UserConfiguration`, `UserRoleConfiguration`.
- Migración `AddUsersAndAuthentication` aplicada (tablas `Companies`, `Roles`, `Users`, `UserRoles`).
- Autenticación JWT: `backend/Services/JwtTokenService.cs` genera el token (claims de id, empresa, nombre, email y rol); `Program.cs` registra `AddAuthentication().AddJwtBearer(...)` y `AddAuthorization()`; configuración en `appsettings.json` (sección `Jwt`: Key/Issuer/Audience/ExpiresMinutes — clave de desarrollo, debe reemplazarse antes de producción).
- Endpoints (`backend/Endpoints/`):
  - `AuthEndpoints`: `POST /api/auth/register`, `POST /api/auth/login` (devuelven token + datos de usuario).
  - `UserEndpoints`: `GET /api/users/me`, `PUT /api/users/me` (perfil propio).
  - `AdminUserEndpoints`: `GET /api/admin/users`, `PUT /api/admin/users/{id}` (activar/desactivar), protegidos con rol `ADMIN`.
- Contraseñas hasheadas con `PasswordHasher<User>` (Microsoft.AspNetCore.Identity.Core).
- Seed (`backend/Data/DataSeeder.cs`): `SeedCoreDataAsync` (corre en todos los entornos) crea la empresa "PlayPredict" y los roles ADMIN/USER si no existen — prerrequisito para que Registro/Login funcionen. `SeedAdminUserAsync` (solo Development) crea el usuario `admin@playpredict.local` / `admin123` con rol ADMIN.

### Frontend

- `frontend/src/auth/`: `AuthContext.tsx` (estado de sesión, `login`/`logout`, `RequireAuth`, `RequireAdmin`), `token.ts` (persistencia del JWT en `localStorage`).
- Páginas nuevas (`frontend/src/pages/`): `LoginPage`, `RegisterPage`, `ProfilePage`, `AdminUsersPage`.
- `App.tsx`: rutas `/login` y `/register` públicas; el resto de la aplicación envuelta en `RequireAuth`; `/admin/users` envuelta además en `RequireAdmin`.
- `Layout.tsx`: muestra el nombre del usuario logueado, link a "Usuarios" solo si tiene rol ADMIN, y botón "Salir" (logout).
- `client.ts`: agrega el header `Authorization: Bearer <token>` a las peticiones y maneja 401 (limpia el token).

### Verificado en esta sesión

- `dotnet build`: OK. `npm run build`: OK.
- Login con usuario admin (`POST /api/auth/login`) → 200, token válido.
- `GET /api/users/me` sin token → 401. Con token → 200.
- `GET /api/admin/users` con token ADMIN → 200. Con token de un usuario USER recién registrado → 403.
- Registro de usuario nuevo (`POST /api/auth/register`) → 200, asigna rol USER automáticamente (usuario de prueba eliminado después de la verificación).
- En el navegador: login, sesión persistida entre recargas, ruta protegida (`/competitions`) redirige a `/login` sin sesión, logout funciona, panel "Usuarios" visible solo para ADMIN.

---

## Sprint 3.5 — Limpieza funcional pre-Pronósticos

Sin nuevas funcionalidades, sin cambios al modelo de datos, sin nuevas migraciones, sin commit.

### 1. Datos de demostración

- Eliminados de la base de datos: competencia técnica "Competancia Amigos 1" (con su edición "edicion 1", fecha "fecha 1" y partido de prueba CELP–GELP), y la edición/fecha/partido de prueba manual que existía bajo "Copa Libertadores" ("Apertura 2026" / "Fecha 1 - Apertura", 1 partido finalizado con resultado 2-1).
- `backend/Data/DataSeeder.cs` reestructurado: el seed ahora crea **dos** competencias de forma idempotente (antes solo creaba Liga Profesional):
  - Liga Profesional → Clausura 2026 → Fecha 1 → 3 partidos (Equipo A-F), sin cambios respecto al Sprint 2.
  - Copa Libertadores → Fase de Grupos 2026 → Fecha 1 → 3 partidos (Equipo G-L), nuevo.
  - Cada competencia se verifica por nombre antes de insertar (no duplica en reinicios sucesivos).
- Corregido el deporte "Futbol" (sin tilde) → "Fútbol" en Copa Libertadores.
- Usuario de prueba `test.sprint35@playpredict.local` (creado y eliminado en esta misma sesión para verificar el registro) — no queda rastro.
- **Nota para el usuario**: quedan dos usuarios de prueba preexistentes de la sesión anterior (`juan.perez@example.com`, `maria.lopez@example.com`, rol USER) que no se tocaron porque no estaban en el alcance explícito de esta limpieza (la consigna hablaba de datos de fixture, no de usuarios). Si se quiere, se pueden eliminar en una futura sesión con aprobación.

### 2. Textos visibles

- La mayoría de la interfaz ya estaba en castellano correcto (verificado archivo por archivo: títulos, breadcrumbs, botones, mensajes de error/éxito, formularios).
- Único problema encontrado: los estados de Edición y Partido se mostraban en inglés crudo (valores del enum) tanto en las tablas (badges) como en los combos de edición. Corregido agregando `EDITION_STATUS_LABELS` y `MATCH_STATUS_LABELS` en `frontend/src/api/types.ts` (mapeo de visualización; los valores enviados a la API siguen siendo los del enum en inglés, sin tocar el modelo de datos):
  - Edición: Draft → Borrador, Active → Activa, Finished → Finalizada, Cancelled → Cancelada.
  - Partido: Scheduled → Programado, InProgress → En curso, Finished → Finalizado, Suspended → Suspendido, Cancelled → Cancelado.
- Archivos actualizados: `EditionsListPage.tsx`, `EditionFormPage.tsx`, `MatchesListPage.tsx`, `MatchFormPage.tsx`.
- No se encontraron literales "Competancia", "Futbol", "Draft", "Round", "Edition" ni "Competition" como texto visible al usuario (esos términos solo aparecen como nombres de tipos/rutas en el código, no en la UI).

### 3. Navegación

- Revisada de punta a punta: Competencias → Ediciones → Fechas → Partidos, con breadcrumb "← [nivel anterior]" en cada pantalla de lista y de formulario. Sin cambios de código necesarios (ya cumplía).

### 4. Consistencia visual mínima

- Revisados títulos, botones (`btn-primary`/`btn-secondary`), mensajes de éxito ("... guardado/a correctamente") y de error ("No se pudo cargar...", "Ocurrió un error inesperado al..."): ya eran consistentes entre pantallas. Único ajuste fue el de los nombres de estado (punto 2).

### 5. Autenticación (solo verificación, sin tocar JWT ni roles)

- Login con usuario y contraseña correctos → 200 + token.
- Login con contraseña incorrecta → 401.
- Ruta protegida sin sesión → redirige a `/login`.
- Sesión persiste entre recargas (token en `localStorage`, verificado en navegador).
- Logout limpia la sesión y redirige.
- Acceso ADMIN a `/api/admin/users` y a la pantalla "Usuarios" → funciona.
- Restricción USER: token de usuario sin rol ADMIN contra `/api/admin/users` → 403 (verificado por API).

### Validaciones ejecutadas

- `dotnet build` (backend): OK, sin errores.
- `npm run build` (frontend): OK, sin errores.
- `docker compose up -d --build`: los 3 servicios levantan `healthy`/`Up`.
- Datos de demostración verificados directamente en PostgreSQL tras el rebuild (persisten correctamente).
- Logs de `backend` y `frontend` revisados tras el rebuild: sin errores ni excepciones.
- Navegación, login, logout y estados verificados visualmente en el navegador.
- `git status --short`: en el momento de esta validación, cambios pendientes de Sprint 3 + Sprint 3.5 sin commit (se commitearon junto con el fix posterior, ver más abajo).

---

## Fix post-Sprint 3.5 — Edición de Partido rompía el Resultado Oficial

**Reportado por el usuario** durante la revisión visual del Sprint 3.5: un partido Finalizado con resultado cargado, al editarse (aunque solo se cambiaran participantes/horario) volvía a Programado; los goles seguían en la base de datos pero el listado dejaba de mostrarlos porque dependía del estado.

### Causa exacta

- **Frontend** (`MatchFormPage.tsx`): el estado del formulario `status` se inicializaba en `'Scheduled'` y solo se sincronizaba con el valor real del partido cuando este **no** era `Finished` (a propósito, porque "Finalizado" no es una opción seleccionable). Para un partido Finalizado, `status` nunca se actualizaba y quedaba en `'Scheduled'`. Al guardar, el payload de `PUT /api/matches/{id}` siempre incluía ese `status`, enviando accidentalmente `"Scheduled"`.
- **Backend** (`MatchEndpoints.cs`, función `ValidateMatch`): el endpoint general aceptaba cualquier `status` válido recibido en el DTO y lo aplicaba tal cual (solo rechazaba explícitamente el valor `"Finished"`), sin ninguna protección para un partido que **ya** estuviera Finalizado. Combinado con el bug del frontend, esto sobrescribía el estado a `Scheduled` sin tocar los goles (que ese endpoint nunca gestiona), produciendo exactamente el síntoma reportado.

### Corrección

- **Backend** (`backend/Endpoints/MatchEndpoints.cs`): en `ValidateMatch`, si el partido ya está `Finished`, se ignora cualquier `status` recibido y se devuelve siempre `Finished` — el endpoint general nunca puede sacar a un partido de Finalizado, sin importar qué envíe el cliente. El resultado oficial (`HomeGoals`/`AwayGoals`) solo se modifica en `PUT /api/matches/{id}/result`, que no fue tocado.
- **Frontend** (`frontend/src/pages/MatchFormPage.tsx`):
  - Si el partido es Finalizado, el campo Estado se muestra como texto fijo "Finalizado" (no editable), en vez de un `<select>`.
  - Al guardar un partido Finalizado, el payload **no** incluye `status`.
  - Tras guardar correctamente (alta o edición), se navega automáticamente a la lista de Partidos de la Fecha, mostrando "Partido guardado correctamente." (antes se quedaba en el formulario).
  - Se agregó el botón "Volver a Partidos" junto a "Guardar", que navega sin guardar (además del enlace superior "← Partidos" ya existente).

### Pruebas realizadas

- **Caso A (Programado)**: editar participantes/horario de un partido Programado y guardar → vuelve a la lista, muestra "Partido guardado correctamente.", el estado sigue Programado. Verificado por API y en el navegador.
- **Caso B (Finalizado)** — reproduce exactamente el escenario reportado: cargar resultado 1-3 → Finalizado y "1 - 3" visibles → Editar Partido (estado se ve como "Finalizado", no editable) → cambiar solo horario → Guardar → vuelve automáticamente a la lista → sigue Finalizado → sigue mostrando "1 - 3" → al abrir "Cargar resultado" de nuevo, los goles 1 y 3 siguen cargados. Verificado por API (incluyendo un intento explícito de forzar `status: "Scheduled"` sobre un partido Finalizado, que el backend ahora ignora) y en el navegador.
- **Caso C (Cancelar)**: modificar un campo sin guardar y presionar "Volver a Partidos" → no se persiste ningún cambio. Verificado en el navegador.
- Validaciones no afectadas: intentar poner `status: "Finished"` manualmente vía `PUT /api/matches/{id}` en un partido no Finalizado → sigue rechazado (400); cambiar a Suspendido/otros estados válidos en un partido no Finalizado → sigue funcionando.
- `dotnet build`: OK. `npm run build`: OK. `docker compose ps`: 3 servicios healthy. Consola del navegador sin errores. Logs de `backend`/`frontend` sin errores ni excepciones.
- Datos de demostración usados durante estas pruebas (nombres/fechas/resultados de prueba) se revirtieron al final, dejando el seed de Liga Profesional y Copa Libertadores en su estado limpio original (6 partidos Programados).

### Commit y push

Sprint 3 + Sprint 3.5 + este fix se aprobaron y commitearon juntos en un único commit: `df43594` — "feat: add authentication and complete pre-predictions cleanup" (44 archivos, 2195 inserciones, 84 eliminaciones), pusheado a `origin/main`. Verificado sin `.env`, `bin/`, `obj/`, `node_modules/` ni `dist/` incluidos.

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
