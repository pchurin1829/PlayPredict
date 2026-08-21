# PROJECT STATUS

**Cierre PC Trabajo — DEMO 1 y ajustes UX post-demo (2026-08-21):** recorrido PLAYER completo validado en Chrome con Registro → Liga Oficial → Pronósticos → Liga de amigos → Resultados → Ranking → Mis Ligas. Informe: `docs/ai/codex/2026-08-21_DEMO_admin-resultados-ranking.md`. Después de la grabación se aclaró el acceso por invitación como “Unirme a una Liga de amigos”, se retiró del sidebar el bloque engañoso “¡Invitá amigos!” marcado Próximamente y Pronósticos pasó a mostrar Fechas siempre por `Round.order`, con acordeones individuales: cerradas y totalmente pronosticadas colapsadas; Fechas con acción expandidas. Chrome A–E, `tsc`, build, health y `git diff --check` OK. Próxima revisión visual desde Casa: mensaje de alta/participación en Competencia más claro; UX de tarjetas de Mis Ligas; estados Activa/Suspendida; acciones de creador vs participante; abandono/reingreso y conservación de pronósticos/puntos. No se cambió ninguna regla funcional de esos bloques pendientes.

**Checkpoint PLAYER demo + Login definitivo (2026-08-20):** circuito PLAYER demo validado de punta a punta. Pronósticos quedó aprobado manualmente — estados, guardado, eliminación, persistencia y navegación ENTER— y no debe modificarse salvo necesidad estricta de compilación. Login y Registro quedaron visualmente aprobados en 1366×768 y 1920×1080. El Login utiliza exclusivamente `frontend/public/assets/login-football.webp`, convertido desde la imagen aprobada sin recorte, reescala ni cambios de composición (1622×969); textos, formulario y publicidad continúan como HTML/CSS. Este checkpoint es la base estable desde la cual se continuará el trabajo en la PC de Trabajo, rama `prueba-glm-ui`.

**Login/Register visual refresh (2026-08-20, checkpoint listo):** ambas pantallas comparten ahora la paleta violeta/azul oscura del PLAYER. Corregida la causa del contraste roto de Registro: usaba variables `--pp-login-*` fuera del scope `.pp-login`. Integrada la imagen definitiva generada para PlayPredict como `frontend/public/assets/login-football.webp` (1622×969, 78704 bytes), sin superponer la escena SVG anterior; Registro conserva su aspecto aprobado. Autofill oscuro preservado. `tsc`, build, asset/health HTTP, tests API y capturas headless 1366×768/1920×1080 OK. Informe: `docs/ai/codex/2026-08-20_LOGIN_REGISTER_visual-refresh.md`.

**PLAYER — Ajustes UX finales (2026-08-20, sin commit):** sidebar renombrado/reordenado a Inicio → Competencias Oficiales → Mis Ligas → Ranking General → Fixture, conservando ruta y título completo de Explorar. Resultados muestra por Round una fecha o rango calendario calculado desde todos sus partidos, con formato argentino y zona Buenos Aires. Pronósticos queda cerrado y sin cambios de lógica. `tsc` y build OK; prueba visual final pendiente. Informe: `docs/ai/codex/2026-08-20_PLAYER_ajustes-ux-finales.md`.

**PLAYER — Explorar y estados de Pronósticos (2026-08-20, sin commit):** confirmado que `Guardar cambios` inicial provenía de la implementación antigua del tab Pronósticos en `LeagueDetailPage`, que no modelaba saved/current/dirty. Ambas superficies quedan alineadas con `¡Pronosticá!` / `Guardar pronóstico` / `Pronosticado` / `Guardar cambios` / `Eliminar pronóstico`; parciales deshabilitados y ceros válidos. Agregado DELETE individual con ConfirmModal. Explorar diferencia claramente `Participar` de `Ver`. `tsc`, build, health, test DELETE y regresión leave/rejoin OK; prueba visual/ENTER manual pendiente. Informe: `docs/ai/codex/2026-08-20_PLAYER_explorar-y-estados-pronosticos.md`.

**PLAYER — leave/rejoin conservando pronósticos (2026-08-20, sin commit):** deshabilitada temporalmente la restricción que impedía abandonar una Liga con pronósticos. `leave` elimina solamente la participación; al volver a participar se recupera el acceso a los pronósticos persistidos. Se mantiene la prohibición para el creador de una Liga privada. La política definitiva queda pendiente de decisión de producto. Prueba API `join → predict → leave → explore → rejoin` ejecutada OK, recuperando el mismo pronóstico 2-1. Script: `tests/player-official-league-leave-rejoin.ps1`. Informe: `docs/ai/codex/2026-08-20_PLAYER_leave-rejoin-pronosticos.md`.

**PLAYER — Competencias Oficiales (2026-08-20, sin commit):** “Mis Ligas” queda limitado a ligas participadas; “Explorar Competencias Oficiales” concentra Participar/Ir a Liga Oficial y Crear Liga con amigos. Corregidos el ocultamiento de errores de `/leagues/officials`, la reutilización incorrecta de una liga privada como Official demo y el retorno de Crear Liga vía `/competitions/{id}`. TypeScript/build frontend y build backend OK; health/API navegada pendientes por timeout Npgsql del entorno. Informe: `docs/ai/codex/2026-08-20_PLAYER_competencias_oficiales_navegacion.md`.

**Auditoría post-Qwen (2026-08-20, sin commit):** corregidos dos bugs mínimos del circuito PLAYER: doble request al guardar Pronósticos con ENTER (keydown manual + click nativo) y pérdida de descripción al Suspender/Reactivar una Liga. `tsc --noEmit`, build frontend y backend health OK. La prueba visual/teclado completa quedó bloqueada porque Docker Desktop dejó el frontend inaccesible después del reinicio requerido y los comandos de arranque quedaron colgados; DB/backend no se tocaron. Informe: `docs/ai/codex/2026-08-20_1019_auditoria-post-qwen.md`.

Versión: 0.9.0
Estado: Sprint 8 (Gestión de Experiencias — MVP) commiteado y pusheado (`1708be5`). Sprint 8.5 (Ligas y Experiencia de Usuario) — Fase 1, Etapa 1, Etapa 2 y Etapa 2.5 — **commiteado y pusheado** en `21dac5c` (rama `prueba-glm-ui`, sincronizada con `origin`). Sprint 8.6 (Hardening) completado como auditoría de solo lectura, sin cambios de código propios (ver detalle más abajo).

**Corrección de estado (sesión del 2026-08-10)**: `SESSION.md`/`PROJECT_STATUS.md` habían quedado desactualizados — describían el Sprint 8.5 y el trabajo de una sesión posterior como "sin commitear" cuando en realidad ya estaban commiteados y pusheados (`21dac5c` y luego `045703e`). El commit `045703e` ("feat: player UX overhaul and playable league flow") se generó fuera de una sesión de Claude Code (herramienta externa, ver informes `GLM_*.md` en la raíz del repo) y añadió, sin pasar por este protocolo de sesión ni actualizar esta documentación: un rediseño visual completo de la experiencia del Jugador (nuevo `PlayerLayout`/`PlayerHeader`/`PlayerSidebar`, `PlayerDashboardPage` como nueva ruta raíz `/` para `PLAYER`, ~13 pantallas de Jugador reestilizadas a tarjetas), y **Ranking de Liga** (`RankingService.GetLeagueRankingAsync` + `GET /api/rankings/leagues/{leagueId}`) — ver sección "Etapa 2.6" más abajo. Working tree actual: limpio (git status sincronizado con `origin/prueba-glm-ui`).

**Conflicto detectado, informado según protocolo de CLAUDE.md**: el Ranking de Liga fue declarado explícitamente **fuera de alcance** en las Etapas 2 y 2.5 de este mismo documento (ver más abajo, "explícitamente fuera de esta etapa") y sin embargo se implementó igual en `045703e`. No contradice la visión de producto (Rankings es un motor central documentado en `PLAYPREDICT_PRODUCTO_v1.0.md` y reutiliza `RankingService` sin duplicar lógica, consistente con "configuración/reutilización antes que programación"), pero sí se saltó el paso de aprobación explícita de alcance que este proyecto exige. Queda informado; no se revirtió nada.

**Corrección de estado adicional (sesión del 2026-08-12)**: se detectó un commit posterior no documentado, `27895d7` ("WIP: demo visual player y login", 11/08/2026, fuera de una sesión de Claude Code, ya pusheado). Documentado retroactivamente como "Etapa 2.7" más abajo. Working tree sigue limpio.

Próximo paso: la validación visual en navegador de `045703e` y `27895d7` (nunca hecha, ver bloqueo documentado en `SESSION.md` sesión 2026-08-10) sigue pendiente. Antes de seguir con cualquier verificación visual o con decidir el alcance de Premios de Liga / Sponsors en Login / escudos de clubes, resolver ese bloqueo (el usuario debe confirmar desde su propio Chrome si ve el layout nuevo).

**Cierre Test Demo 1 (2026-08-19, sin commit):** corregido exclusivamente el contraste del Ranking de Liga y agregada navegación por `Enter` en los marcadores de Pronósticos (local → visitante → siguiente partido, sin submit automático; `Tab` intacto). Se preservaron los mensajes diferenciados de creación/actualización. Backend healthy, frontend operativo y creación → modificación → persistencia verificadas por API. Pendiente prueba manual visual y de teclado del usuario. Informe: `docs/ai/codex/2026-08-19_1352_test-demo1-cierre.md`.

---

## Sprint 8.5 — Ligas y Experiencia de Usuario

Objetivo: incorporar **Liga** como nuevo concepto principal del producto — creada libremente por cualquier `PLAYER` sobre una Competencia Oficial existente, sin duplicar Fixture ni Resultados — y simplificar el modelo de roles a únicamente `ADMIN`/`PLAYER`. Decisiones de producto ya aprobadas por el usuario (no se discuten): ver `docs/arquitectura/PLAYPREDICT_MODELO_CONCEPTUAL_v2.0.md`, Sección 0.

### Fase 1 — Modelo conceptual (completada y aprobada)

- [x] Nuevo documento `docs/arquitectura/PLAYPREDICT_MODELO_CONCEPTUAL_v2.0.md` — modelo conceptual oficial vigente a partir de este Sprint. No reemplaza ni borra `MODELO_CONCEPTUAL_v1.0.md`, `MODELO_CONCEPTUAL_ADMINISTRADOR_v1.0.md` ni `MODELO_CONCEPTUAL_JUGADOR_v1.0.md` (se conservan como historial de los Sprints 1-8).
- [x] `PROJECT_STATUS.md`, `SESSION.md` y `docs/products/ROADMAP_PRONOSTICOS_v1.0.md` actualizados para reflejar la nueva arquitectura.
- [x] **Corrección aprobada por el usuario, ya reflejada en toda la documentación**: el Pronóstico **no** es global por Usuario+Partido. Cada Pronóstico pertenece a una Liga — identidad lógica `LeagueId + UserId + MatchId` —, de modo que un mismo Jugador puede pronosticar resultados distintos para el mismo Partido en Ligas distintas. Los Partidos y Resultados Oficiales sí se comparten entre Ligas sin duplicarse; los Pronósticos nunca se comparten entre Ligas. Ver `docs/arquitectura/PLAYPREDICT_MODELO_CONCEPTUAL_v2.0.md`, Sección 9.
- [x] Sin cambios de backend, frontend ni migraciones en esta fase.
- [x] Sin commit (fase puramente documental).

### Resumen del modelo vigente (detalle completo en el documento v2.0)

- Roles simplificados a `ADMIN`/`PLAYER`. Registro público siempre crea `PLAYER`. Instalación inicial con 3 usuarios `ADMIN` de ejemplo (hoy hay 1). Login único, sin selector de rol.
- Competencias Oficiales: sin cambios conceptuales, siguen siendo exclusivamente administrables por `ADMIN`.
- **Liga** (entidad nueva): creada por cualquier `PLAYER`, referencia una Competencia Oficial, alcance "Competencia completa" o "Fecha X → Fecha Y" (sin selección por fases todavía), código de invitación, Participantes, Ranking propio (calculado, reutilizando `RankingService`), Premios propios opcionales (reutilizando `Prize` con un nuevo ámbito `League`).
- **Pronóstico: pertenece a la Liga, no es global.** Identidad `LeagueId + UserId + MatchId`. Un mismo Jugador puede pronosticar distinto el mismo Partido en Ligas distintas. El Ranking de cada Liga se calcula exclusivamente con los Pronósticos cargados dentro de ella.
- `ADMIN` deja de administrar Jugadores (hoy el panel administra todos los usuarios); el `PLAYER` administra únicamente sus propias Ligas.

### Fase 2, Etapa 1 — Roles y base del modelo (implementada y commiteada — `21dac5c`)

Alcance: rol `USER`→`PLAYER` (backend, seeds, autorización), 3 usuarios `ADMIN` de desarrollo (contraseña por configuración, guarda anti-producción), entidades `League`/`LeagueParticipant`, `Prediction.LeagueId` (identidad lógica `LeagueId+UserId+MatchId`), migración `AddLeagues` con backfill dinámico e idempotente, y validaciones completas en `POST/PUT /api/predictions` (Liga existe/activa, pertenencia, Competencia, alcance de Fechas, duplicado, horario) — nunca 500, siempre 400/403/404/409 controlado. `GET /api/predictions/rounds/{roundId}` ahora exige `leagueId` explícito (400 si falta; no se elige ninguna Liga por defecto).

Backfill verificado: 1 Liga técnica `[Migración] Liga general — Liga Profesional`, 4 Participantes (Ana/Juan/María/Pedro), los 12 Pronósticos históricos conservados sin pérdida, Ranking General de Clausura 2026 verificado exacto (15/12/9/6) por API tras la migración. Migración probada también sobre una base vacía nueva (16 tablas, 7 migraciones, sin errores). Seeder verificado idempotente en 2 reinicios sucesivos (sin duplicar roles, admins, Liga demo, participantes ni pronósticos).

Problema real encontrado y corregido durante la implementación: el rol `USER` preexistente en la base no se renombraba solo (el seeder solo agrega roles faltantes); quedaba un rol `PLAYER` nuevo y vacío mientras los usuarios existentes seguían apuntando a `USER`. Se corrigió con un `UPDATE` en el lugar dentro de la propia migración `AddLeagues` (preserva el `Id` del Rol y todas las `UserRoles` existentes).

Sin frontend, sin Premios de Liga, sin endpoints de administración de Ligas (crear/editar/unirse) — explícitamente fuera de esta etapa; las Ligas de prueba se crearon/eliminaron directamente por SQL para validar el motor. Commiteado en `21dac5c`.

**Corrección de seguridad previa a la Etapa 2**: `backend/appsettings.Development.json` (trackeado por Git, a diferencia de `.env`) tenía la contraseña de los 3 ADMIN en texto plano. Se eliminó esa sección por completo; la contraseña ahora se pasa como variable de entorno `DevSeed__AdminPassword` en `docker-compose.yml`, resuelta desde `.env` (gitignored) — documentada como `DEV_ADMIN_PASSWORD` en `.env.example` sin valor real. Sin esa variable, `DataSeeder` cae a un valor por defecto de solo-desarrollo, con guarda que impide su ejecución fuera de `Development`.

### Fase 2, Etapa 2 — Gestión básica de Ligas (implementada y commiteada — `21dac5c`)

Backend: `LeagueEndpoints.cs` (`POST/GET /api/leagues`, `GET /mine`, `GET/PUT /{id}`, `POST /join`, `GET /{id}/participants`, `GET /{id}/matches`) con validación completa (Competencia habilitada, alcance FullCompetition/RoundRange coherente con la Edición, unicidad de `InviteCode`, pertenencia, creador). `Prediction`/`PredictionEndpoints` reutilizados sin duplicar lógica (helpers expuestos como `internal`). Nueva columna `League.Description` (migración `AddLeagueDescription`).

Frontend: pantallas `LeaguesMinePage` (Mis Ligas), `LeagueCreatePage` (con cascada Competencia→Edición→Fecha para el alcance por rango), `LeagueJoinPage`, `LeagueDetailPage`, y `PredictionsMatchesPage.tsx` adaptada para trabajar por `leagueId` (ya no por `roundId` global). Se retiraron `PredictionsCompetitionsPage`/`PredictionsEditionsPage`/`PredictionsRoundsPage` y el link "Pronósticos" del menú por quedar sin ruta válida bajo el nuevo modelo (los Pronósticos se acceden siempre desde una Liga).

Validado de punta a punta en el navegador real (Juan y Ana, dos usuarios distintos): crear Liga, unirse por código (normalizado, idempotente), pronosticar el mismo partido con valores distintos en dos Ligas distintas del mismo usuario, persistencia tras recargar, rechazo 403 al acceder a una Liga ajena, mensaje controlado ante código inválido, participantes sin datos sensibles. Regresión: login ADMIN, panel de Usuarios (roles ya muestran `PLAYER`), Experiencias, Ranking General de Clausura 2026 (15/12/9/6) todo intacto.

Sin Ranking de Liga, sin Premios de Liga, sin edición de participantes, sin eliminación de Ligas — explícitamente fuera de esta etapa (Ranking de Liga se implementó igualmente más tarde en `045703e`, fuera de este flujo de sesión — ver nota de conflicto al inicio del documento y "Etapa 2.6" más abajo). Commiteado en `21dac5c`.

### Fase 2, Etapa 2.5 — Experiencia del Jugador (implementada y commiteada — `21dac5c`)

Reorganización pura de navegación, **sin backend nuevo, sin modelo de datos nuevo, sin migraciones** — construida enteramente reutilizando endpoints ya existentes (`/competitions`, `/competitions/{id}/editions`, `/editions/{id}/rounds`, `/leagues/mine`).

Pantallas nuevas: `ExploreCompetitionsPage` (`/competitions/explore`, solo Competencias activas: nombre/deporte/edición activa/cantidad de Fechas/estado/Ver) y `CompetitionDetailPage` (`/competitions/:competitionId`, vista del Jugador: info general + "Mis Ligas en esta Competencia" + botón "Crear nueva Liga" con la Competencia preseleccionada). `LeagueCreatePage` ahora acepta `?competitionId=` y, si llega, oculta el selector de Competencia (no se vuelve a pedir). `LeaguesMinePage`: se reemplazó el botón "+ Crear Liga" por "Explorar Competencias" (toda creación de Liga arranca desde una Competencia, sin rutas duplicadas). Menú simplificado para `PLAYER`: Mis Ligas / Explorar Competencias / Unirse por código / Rankings / Perfil — "Fixture" y "Premios" quedaron admin-only (siguen existiendo, solo dejaron de mostrarse al Jugador). Login y ruta raíz (`/`) ahora redirigen según rol: `ADMIN` → `/competitions`, `PLAYER` → `/leagues`.

Validado en el navegador real con 3 usuarios: `PLAYER` nuevo sin Ligas (estado vacío correcto), creación de 2 Ligas distintas sobre la misma Competencia desde su detalle, un segundo usuario uniéndose por código, vuelta al detalle de Competencia mostrando la Liga ya creada, y login `ADMIN` con su panel completo intacto (Fixture/Premios/Usuarios/Administrar Premios/Experiencias todos visibles). Sin errores de consola.

Sin Ranking de Liga, sin Premios, sin dashboard definitivo, sin rediseño visual, sin administración avanzada, sin eliminación de Ligas — explícitamente fuera de esta etapa (el dashboard, el rediseño visual y el Ranking de Liga se agregaron después en `045703e` — ver "Etapa 2.6"). Commiteado en `21dac5c`.

### Fase 2, Etapa 2.6 — Rediseño visual del Jugador y Ranking de Liga (implementada y commiteada fuera de una sesión de Claude Code — `045703e`)

**No planificada en este documento antes de implementarse** — generada por una herramienta externa (ver informes `GLM_AUDITORIA_VISUAL_PLAYPREDICT.md`, `GLM_CIRCUITO_JUGABLE_PLAYPREDICT.md`, `GLM_REDISENO_VISUAL_PLAYPREDICT.md` v1-v4 en la raíz del repo) y commiteada directamente sin pasar por el protocolo de aprobación de alcance de `CLAUDE.md`. Documentada acá de forma retroactiva.

- **Backend**: `RankingService.GetLeagueRankingAsync` + `GET /api/rankings/leagues/{leagueId}` (nuevo — cierra el hueco de Ranking de Liga dejado pendiente en la Etapa 2). `DataSeeder.cs`: Copa Libertadores pasó de 1 Fecha/3 partidos a 5 Fechas/15 partidos con equipos sudamericanos reales; los 4 usuarios demo quedaron sembrados como participantes de Liga. Sin migraciones, sin cambios de esquema.
- **Frontend**: nuevo `PlayerLayout`/`PlayerHeader`/`PlayerSidebar` reemplazando el nav compartido con ADMIN en las rutas de `PLAYER`; nueva ruta raíz `/` → `PlayerDashboardPage` (antes `/leagues`) para `PLAYER` tras login; ~13 pantallas de Jugador reestilizadas de tablas/formularios a tarjetas (`PlayerPages.css`, ~1170 líneas nuevas); `LeagueDetailPage` reestructurada en tabs (Resumen/Pronósticos/Resultados/Ranking/Premios "Próximamente"/Participantes) con Pronósticos y Resultados separados y agrupados por Fecha.
- **Validación — autoreportada por la herramienta externa, no verificada en esta sesión de Claude Code**: los informes v1 y v2 declaran explícitamente **"No se validó visualmente en navegador"** (solo `tsc --noEmit`/`npm run build`/`dotnet build` en verde); v3/v4 mencionan ajustes post-"prueba manual" pero sin un log de QA documentado. Sin tests automatizados (el proyecto no los tiene).
- **Pendientes declarados por los propios informes**: "Agregar participante registrado" (búsqueda/alta por ADMIN — diferido por instrucción explícita del usuario en esa sesión externa), datos demo de Copa Libertadores no visibles sin `docker compose down -v` (seeder idempotente por nombre, no hace backfill), rediseño de Login pendiente, tab Premios de Liga sigue "Próximamente" (sin backend de premios por Liga), escudos reales de clubes fuera de alcance, menú de usuario solo con hover (no accesible táctil), iconos de sidebar son emojis placeholder, sin theming dinámico de Experience.
- **Conflicto de alcance con este documento**: Ranking de Liga y el dashboard/rediseño visual estaban explícitamente listados como "fuera de esta etapa" en las Etapas 2 y 2.5 — se implementaron igual. Alineado con la visión de producto (Rankings es un motor central, reutiliza `RankingService` sin duplicar), pero se saltó la aprobación explícita de alcance.
- **Pendiente de esta sesión de Claude Code**: validar visualmente en navegador (nunca hecho), y decidir si el alcance de Ranking de Liga / rediseño se acepta formalmente como cerrado o si necesita revisión.

### Fase 2, Etapa 2.7 — Rediseño de Login, escudos de clubes y fix de seed (implementada y commiteada fuera de una sesión de Claude Code — `27895d7`)

**No planificada en este documento antes de implementarse** — commit `27895d7` ("WIP: demo visual player y login", 11/08/2026), fuera de una sesión de Claude Code, ya pusheado. Detectado y documentado retroactivamente al inicio de la sesión de Claude Code del 2026-08-12.

- **Frontend**: rediseño visual completo de `LoginPage.tsx`/`LoginPage.css` (nuevo, 360 líneas) — escena de estadio en SVG, 4 "features" (Competí/Sumá puntos/Ganá premios/Jugá con amigos), formulario con iconos inline y toggle mostrar/ocultar contraseña, y un panel lateral con **3 slots de "PUBLICIDAD" (sponsors) hardcodeados**, sin backend ni configuración. Nuevo sistema de escudos genéricos por club (`data/clubBadges.ts`, `components/player/TeamBadge.tsx`): SVG con patrón de colores (franjas/sash/mitades) para 10 clubes reales cableados, con fallback por hash de color/iniciales para el resto. Nuevo `player/PlayerTheme.css` referenciado desde `Layout.tsx`.
- **Backend**: fix de un bug de índice en `DataSeeder.cs` — el loop de resultados oficiales de la demo de Ranking usaba `RankingDemoMatches.Length` sin acotarse también a `finishedMatches.Count`, lo que podía fallar si una Competencia demo ya existía en la base con menos partidos que el seed nuevo. Sin migraciones, sin cambios de esquema.
- **Validación**: no hay evidencia de validación visual en navegador (mismo patrón que `045703e`). Sin tests automatizados.
- **Conflicto de alcance con este documento**:
  - El rediseño de Login estaba listado como "pendiente" en `SESSION.md` — se implementó sin pasar por aprobación explícita.
  - Los 3 slots de sponsors/publicidad introducen visualmente el concepto de **Sponsors**, declarado explícitamente **fuera de alcance en el Sprint 8** ("Wizard, Sponsors, Branding avanzado... explícitamente fuera de este sprint"). Son estáticos, sin entidad ni persistencia nueva, pero instalan la idea sin aprobación.
  - Los escudos de clubes reintroducen algo que la Etapa 2.6 había declarado **explícitamente fuera de alcance** ("Escudos reales de clubes... tarea posterior separada"). Son colores/patrones genéricos, no escudos oficiales con licencia, pero es el mismo tema diferido.
- **Pendiente**: validar visualmente en navegador (nunca hecho, bloqueo de sesión previa sin resolver), y decidir si los sponsors placeholder y los escudos genéricos se aceptan como alcance o se revisan/revierten.

---

## Sprint 8.6 — Hardening y Preparación para Commit (completado)

Objetivo: dejar el Sprint 8.5 (Fase 1 + Etapas 1, 2 y 2.5) en condiciones de commit con el menor riesgo posible, sin incorporar funcionalidades nuevas, sin cambiar el modelo de datos ni la arquitectura. Ejecutado tras una interrupción de sesión por corte de energía; se hizo primero una auditoría de estado (Fase 0) que confirmó que nada se había perdido.

**Resultado: ningún archivo del repositorio fue modificado.** Las 7 fases fueron de auditoría de solo lectura y pruebas funcionales sobre datos efímeros (revertidos al final, conteos verificados idénticos al baseline).

- **Migraciones**: `AddLeagues` y `AddLeagueDescription` confirmadas necesarias, no redundantes, sin consolidar (no aporta valor, agregaría riesgo). Riesgo verificado bajo; descartado un riesgo teórico sobre el rename de Roles `USER`→`PLAYER` (el orden migración-antes-que-seed en `Program.cs` lo hace imposible en la práctica).
- **Configuración/secretos**: `appsettings.Development.json`, `.env.example`, `docker-compose.yml`, `Program.cs` limpios. Detectado (no corregido, fuera del diff de este Sprint) que `appsettings.json` base tiene un `Jwt:Key` y una contraseña de PostgreSQL en texto plano como *fallback* — pendiente de un hardening de seguridad aparte.
- **Limpieza de código**: sin TODO/FIXME/HACK/DEBUG, sin `console.log`/`Console.WriteLine`, sin código muerto ni imports/DTOs/endpoints huérfanos. `oxlint` en 0 errores.
- **Nomenclatura**: `League`/`LeagueParticipant`/`RoleNames.Player` consistentes con el resto del modelo, sin mezcla `USER`/`Member`/`LeagueUser`. Único hallazgo cosmético sin efecto funcional: estilo de nombre distinto entre la Liga de demostración del seed y la Liga técnica del backfill de la migración.
- **Validaciones técnicas**: `dotnet build` y `npm run build`/`tsc --noEmit` en verde (0 errores, corridos 2 veces cada uno); migraciones sobre base existente confirmadas (8/8 aplicadas, 0 `Predictions.LeagueId` nulos); **seeder confirmado idempotente en 3 ejecuciones reales consecutivas**, conteos idénticos las 3 veces. La prueba de migración sobre base vacía se abortó en esta sesión por contención de recursos (se priorizó proteger el entorno del usuario); queda respaldada por el análisis de código y por la verificación ya documentada de la sesión de la Etapa 1 sobre una base vacía nueva.
- **Recorrido funcional** (sustituto del recorrido visual, extensión de Chrome no disponible en la sesión): login ADMIN/PLAYER, competencias, experiencias, usuarios, premios, mis ligas, explorar competencias, crear Liga, unirse por código, abrir Liga, rechazo controlado de pronóstico sobre partido Finalizado, participantes sin datos sensibles — todo correcto por API. Hallazgo menor no bloqueante: `POST /api/leagues` no tiene protección de reintento (un reintento de cliente tras timeout puede crear una Liga duplicada; comportamiento HTTP esperado, no un bug).
- **Hallazgo de entorno (no de código)**: tras los restarts consecutivos de esta auditoría, el backend real (`dotnet watch`) mostró ciclos de reinicio espurios de su *polling file watcher* (bind mount de Docker Desktop en Windows, ya documentado como "no confiable" en sesiones anteriores). Aislado corriendo el mismo código con `dotnet run` (sin `watch`): arrancó limpio y quedó `healthy` de inmediato. Al cierre de la sesión el contenedor estándar ya se había estabilizado solo.

**Evaluación final**: Arquitectura 9/10, Backend 9/10, Frontend 9/10, Base de Datos 9/10, UX 8/10, Riesgo para commit 9/10. **Recomendación: A) listo para Commit y Push.**

---

## Sprint 8 — Gestión de Experiencias (MVP)

Objetivo: incorporar el concepto de **Experiencia** (docs/arquitectura/MODELO_CONCEPTUAL_EXPERIENCIA_v1.0.md) como entidad principal de PlayPredict, de forma incremental, sin romper ninguna funcionalidad de los Sprints 1 a 7.

### Alcance

Implementado (MVP): entidad `Experience` con datos generales y puntuación por defecto; relación obligatoria Competencia → Experience con migración que preserva los datos existentes; ABM de Experiencias (listar/crear/editar, sin eliminación física, solo estados Borrador/Publicada/Archivada); concepto "Usar configuración de la Experience" vs "Configuración propia" en la Edición, con herencia completa (nunca parcial); pantallas de administración. Explícitamente fuera de alcance: Wizard, Sponsors, Branding avanzado, dominios, idiomas, plantillas, biblioteca de configuraciones/motores, White Label, campañas, dashboard ejecutivo, estadísticas, auditoría.

### Modelo

- `Experience` (`backend/Domain/Entities/Experience.cs`): `Name`, `Description`, `Status` (`ExperienceStatus`: Draft/Published/Archived), `PrimaryColor`, `SecondaryColor`, `LogoUrl` (placeholder, sin uso en el formulario de este Sprint), `IsPublic`, `DefaultExactScorePoints`/`DefaultCorrectOutcomePoints`/`DefaultIncorrectPoints` (pertenecen directamente a la Experience, sin "Motor" ni plantillas), `CreatedAtUtc`, `UpdatedAtUtc`.
- `Competition`: nuevo campo obligatorio `ExperienceId` (FK a `Experience`, `DeleteBehavior.Restrict`).
- `EditionScoringConfiguration`: nuevo campo `UseExperienceDefaults` (bool, default `false` — preserva el comportamiento exacto de los Sprints 1 a 7 para toda Edición existente).
- Migración `AddExperiences`: creada con `dotnet ef migrations add` y **editada a mano** para un backfill seguro: (1) crea la tabla `Experiences`; (2) inserta una Experience "PlayPredict Demo" (Publicada, pública, 6/3/0) directamente en el `Up()`, en todos los entornos, como medida de compatibilidad; (3) agrega `Competitions.ExperienceId` como nullable; (4) `UPDATE` de backfill: toda Competencia existente sin `ExperienceId` queda asociada a "PlayPredict Demo"; (5) recién entonces `ALTER COLUMN` a `NOT NULL` + `FOREIGN KEY`. Ningún dato existente se pierde. `UseExperienceDefaults` se agrega con `HasDefaultValue(false)` a nivel de EF, sin necesidad de backfill manual.

### Backend

- `backend/Endpoints/AdminExperienceEndpoints.cs` (`/api/admin/experiences`, solo ADMIN): `GET`/`GET {id}`/`POST` (crea en Borrador)/`PUT {id}` (bloqueado si Archivada)/`PUT {id}/publish` (solo desde Borrador)/`PUT {id}/archive` (bloqueado si ya Archivada). Validaciones: nombre obligatorio y longitudes máximas, colores y URL de logo con longitud máxima, puntuación por defecto ≥ 0. Sin endpoints públicos: en este MVP la Experience es puramente administrativa (sin pantalla de jugador todavía).
- `backend/Endpoints/CompetitionEndpoints.cs`: `POST` acepta `experienceId` opcional — si se omite, se resuelve automáticamente a "PlayPredict Demo" (compatibilidad total con `CompetitionFormPage.tsx`, que no envía este campo); si se indica, se valida que exista. `PUT` solo cambia la Experience si se envía explícitamente un `experienceId` (si se omite, se conserva la actual — así no se resetea accidentalmente al guardar desde el formulario existente).
- `backend/Endpoints/EditionScoringConfigurationEndpoints.cs`: el DTO ahora incluye `useExperienceDefaults` y los valores "efectivos" (`effectiveExactScorePoints`/`effectiveCorrectOutcomePoints`/`effectiveIncorrectPoints`) — los propios cuando `UseExperienceDefaults` es `false`, o los de la Experience (completos) cuando es `true`. El frontend nunca calcula esto, solo muestra lo que llega.
- `backend/Services/PredictionEvaluationService.cs`: antes de evaluar un partido, resuelve si la Edición usa configuración propia o de la Experience (vía `Round → Edition → Competition → Experience`) y aplica los valores correspondientes de forma completa (sin mezcla parcial). Refactorizado `Evaluate(...)` para recibir los 3 valores de puntuación explícitos en lugar de la entidad de configuración completa.
- `backend/Data/DataSeeder.cs`: nuevo `GetOrCreateDemoExperienceAsync` (idempotente por nombre, defensivo — la Experience ya existe por la migración en todo entorno) usado por `SeedCompetitionAsync` para asociar explícitamente las Competencias demo (solo Development) a "PlayPredict Demo".
- `PlayPredictDbContext`: nuevo `DbSet<Experience> Experiences`.

### Frontend

- Nueva entrada "Experiencias" en el menú (solo ADMIN).
- `AdminExperiencesListPage` (`/admin/experiences`): tabla con Nombre, Estado, Pública, Puntuación por defecto y acciones Editar/Publicar/Archivar según el estado.
- `AdminExperienceFormPage` (`/admin/experiences/new` y `/admin/experiences/:id/edit`): dos secciones mediante pestañas simples ("Datos generales": Nombre, Descripción, Color primario, Color secundario, Pública; "Configuración": puntuación por defecto). Estado mostrado como texto de solo lectura. Mismo estilo (`form-card`, `btn`, etc.) que el resto del panel, sin rediseño.
- `EditionScoringConfigurationPage.tsx`: nuevo checkbox "Usar configuración de la Experience" — al activarlo, deshabilita los 3 campos propios y muestra los valores efectivos que se aplicarán; al guardar, el backend confirma los valores efectivos reales.
- `api/types.ts`: nuevo tipo `Experience`, `ExperienceStatus`; `Competition` incluye `experienceId`; `EditionScoringConfiguration` incluye `useExperienceDefaults` y los 3 valores efectivos.

### Datos de demostración

Experience "PlayPredict Demo" (Publicada, pública, 6/3/0) creada por la migración `AddExperiences` en todos los entornos (medida de compatibilidad); Liga Profesional y Copa Libertadores quedaron asociadas a ella automáticamente por el backfill. En Development, el seed reasegura esta asociación de forma idempotente si se sembraran Competencias demo nuevas.

### Pruebas realizadas

- Migración aplicada sin pérdida de datos: verificado en PostgreSQL que las 2 Competencias existentes (`Liga Profesional`, `Copa Libertadores`) quedaron con `ExperienceId = 1` (Experience "PlayPredict Demo"), y que las 2 `EditionScoringConfigurations` existentes quedaron con `UseExperienceDefaults = false` (comportamiento idéntico a antes del Sprint).
- Regresión completa Sprints 1-7 (sin cambios detectados): login, `GET /api/competitions` y `/editions` con el nuevo campo `experienceId` visible pero sin alterar nada más; Ranking General de Clausura 2026 exacto (Juan 15, Ana 12, María 9, Pedro 6); los 5 Premios de Sprint 7 intactos; configuración de puntuación de Edición 7 (configuración propia) sin cambios.
- **Herencia funcionando de punta a punta**: se cambiaron temporalmente los valores por defecto de la Experience "PlayPredict Demo" a 10/5/1, se activó `UseExperienceDefaults = true` en la Edición 8 (Copa Libertadores) → `GET /api/editions/8/scoring-configuration` mostró `effectiveExactScorePoints: 10` (Edición 7, con configuración propia, siguió en 6); se creó un pronóstico temporal y se cargó un resultado oficial exacto sobre un partido de esa Edición → la evaluación real aplicó **10 puntos** (el valor heredado), no 6 (el valor propio guardado en la Edición) — confirmado directamente en `PredictionEvaluations`. Revertido todo (resultado del partido, pronóstico, evaluación, flag de la Edición 8, valores de la Experience) a su estado original.
- **Configuración propia funcionando**: confirmado que la Edición 7, con `UseExperienceDefaults = false`, mantuvo sus valores propios (6/3/0) como valores efectivos durante toda la prueba anterior, sin verse afectada por el cambio de valores de la Experience.
- Alta de Competencia sin `experienceId` (payload idéntico al que envía el formulario actual) → se asoció automáticamente a "PlayPredict Demo". Edición de una Competencia existente sin `experienceId` → conservó su Experience actual (no la reseteó). Ambas pruebas revertidas.
- Verificado visualmente en el navegador: lista de Experiencias, formulario con las dos secciones (pestañas) mostrando los valores correctos, checkbox "Usar configuración de la Experience" deshabilitando los campos propios y mostrando la nota de valores heredados sin necesidad de guardar, pantalla de Competencias sin cambios visibles. Consola del navegador sin errores.
- `dotnet build`: OK. `npm run build`: OK. `docker compose up -d --build`: 3 servicios healthy. Logs de `backend`/`frontend`/`db` revisados: sin errores de la aplicación.
- Estado final verificado: 2 `Competitions`, 12 `Predictions`, 12 `PredictionEvaluations`, 5 `Prizes`, 1 `Experience`, 8 `Users` — exactamente el estado previo al Sprint, sin ningún dato temporal residual.

### Archivos modificados/creados

Backend: `Domain/Entities/Experience.cs`, `Domain/Enums/ExperienceStatus.cs`, `Data/Configurations/ExperienceConfiguration.cs`, `Dtos/ExperienceDtos.cs`, `Endpoints/AdminExperienceEndpoints.cs`, `Migrations/20260801175318_AddExperiences.cs` y `.Designer.cs` (nuevos); `Domain/Entities/Competition.cs`, `Domain/Entities/EditionScoringConfiguration.cs`, `Data/Configurations/CompetitionConfiguration.cs` (sin cambios de código, relación configurada desde `ExperienceConfiguration`), `Data/Configurations/EditionScoringConfigurationConfiguration.cs`, `Data/PlayPredictDbContext.cs`, `Data/DataSeeder.cs`, `Dtos/CompetitionDtos.cs`, `Dtos/ScoringDtos.cs`, `Endpoints/CompetitionEndpoints.cs`, `Endpoints/EditionScoringConfigurationEndpoints.cs`, `Services/PredictionEvaluationService.cs`, `Program.cs`, `Migrations/PlayPredictDbContextModelSnapshot.cs` (modificados).

Frontend: `pages/AdminExperiencesListPage.tsx`, `pages/AdminExperienceFormPage.tsx` (nuevos); `api/types.ts`, `App.tsx`, `components/Layout.tsx`, `components/admin.css`, `pages/EditionScoringConfigurationPage.tsx` (modificados).

---

## Sprint 7 — Módulo de Premios

Objetivo: permitir definir y visualizar Premios asociados a una Edición o a una Fecha. El Premio **no calcula puntos ni posiciones** — el Ranking (Sprint 6) sigue siendo la única fuente para determinarlas.

### Alcance

Implementado: administración de premios (alta, edición, publicar, cerrar, cancelar), premios por Edición, premios por Fecha, premio por posición (rango), premio por mayor cantidad de marcadores exactos, premio "Ganador de Fecha", cálculo del ganador actual provisional, visualización para usuarios. Explícitamente fuera de alcance: `PrizeWinner` persistido, entrega real, pagos, cupones automáticos, reclamos, notificaciones, historial de ganadores, premios mensuales/por empresa/ligas privadas, rediseño visual del panel.

### Modelo

- `Prize` (`backend/Domain/Entities/Prize.cs`): `EditionId` (obligatorio), `RoundId` (opcional, obligatorio solo si `ScopeType == Round`, debe pertenecer a la misma Edición), `Name`, `Description`, `PrizeType`, `ReferenceValue` (texto descriptivo, no un pago procesado), `SponsorName`, `ImageUrl`, `ScopeType`, `AwardCriteria`, `PositionFrom`/`PositionTo` (solo usados con `AwardCriteria == Position`), `Status`, `CreatedAtUtc`, `UpdatedAtUtc`.
- Enums nuevos (`backend/Domain/Enums/`): `PrizeType` (Money, Product, Service, Coupon, Ticket, Recognition, Other), `PrizeScopeType` (Edition, Round, Special), `PrizeAwardCriteria` (Position, RoundWinner, MostExactScores), `PrizeStatus` (Draft, Published, Closed, Cancelled).
- Migración `AddPrizes` creada y aplicada (tabla `Prizes`, FKs a `Editions` y `Rounds` con `DeleteBehavior.Restrict`).
- No se creó `PrizeWinner` ni `PrizeDelivery`: el ganador nunca se persiste, siempre se deriva en el momento de la consulta.

### Backend

- `backend/Services/PrizeWinnerService.cs`: responsabilidad única — dado un `Prize`, consulta `RankingService` (Ranking General o por Fecha, según `ScopeType`) y devuelve la lista de usuarios ganadores actuales *provisionales* según `AwardCriteria`:
  - `Position`: usuarios cuya posición está entre `PositionFrom` y `PositionTo`.
  - `RoundWinner`: posición 1 del Ranking por Fecha (todos los empatados si hay empate).
  - `MostExactScores`: usuarios con el `ExactCount` máximo del Ranking correspondiente (todos los empatados); si el Ranking está vacío, no devuelve ningún ganador (nunca lo inventa).
- `backend/Services/PrizeMapper.cs`: arma el DTO de lectura (`PrizeDto`) con las etiquetas en castellano (`prizeTypeLabel`, `scopeLabel`, `criteriaLabel`, `statusLabel`) y el texto descriptivo `forLabel` (p. ej. "Para: 1.º puesto del Ranking General", "Para: Ganador de la Fecha 1", "Para: Mayor cantidad de marcadores exactos del Ranking General"), además de `currentWinners` y `hasProvisionalWinner`. Compartido entre endpoints administrativos y públicos.
- `backend/Dtos/PrizeDtos.cs`: `PrizeDto`, `CreatePrizeDto`, `UpdatePrizeDto`, `PrizeWinnerUserDto`.
- `backend/Endpoints/AdminPrizeEndpoints.cs` (`/api/admin/prizes`, `RequireAuthorization(RoleNames.Admin)`):
  - `GET`/`GET {id}`: listar todos los Premios (cualquier estado) y obtener uno.
  - `POST`: crea siempre en estado Borrador. Validaciones: Edición existente, Fecha existente y perteneciente a la misma Edición cuando `ScopeType == Round`, nombre obligatorio y longitudes máximas, enums válidos, `RoundWinner` solo compatible con ámbito Fecha, posiciones (`PositionFrom >= 1`, `PositionTo >= PositionFrom`) obligatorias solo con criterio Posición.
  - `PUT {id}`: misma validación; bloqueado si el Premio está Cancelado. La Edición no es editable (fija desde la creación).
  - `PUT {id}/publish`: solo desde Borrador, re-validando coherencia.
  - `PUT {id}/close`: solo desde Publicado.
  - `PUT {id}/cancel`: desde Borrador o Publicado; bloqueado si ya está Cerrado o Cancelado.
- `backend/Endpoints/PrizeEndpoints.cs` (`/api/prizes`, `RequireAuthorization()` sin restricción de rol): `GET /editions/{editionId}`, `GET /rounds/{roundId}`, `GET /{id}` — los tres filtran exclusivamente `Published`/`Closed`; un Premio Borrador o Cancelado devuelve 404 en el `GET /{id}` público y nunca aparece en los listados.
- `PrizeWinnerService` registrado como `Scoped` en `Program.cs`.

### Frontend administrativo

- Nueva entrada "Administrar Premios" en el menú (solo ADMIN).
- `AdminPrizesListPage` (`/admin/prizes`): tabla con Nombre, Edición (+ Fecha si aplica), Ámbito, Criterio, Estado, Sponsor, Ganador actual; botones Editar y, según el estado, Publicar / Cerrar / Cancelar.
- `AdminPrizeFormPage` (`/admin/prizes/new` y `/admin/prizes/:id/edit`): formulario único de alta/edición con selects en cascada Competencia → Edición (Edición fija y deshabilitada al editar) → Fecha (solo si Ámbito = Fecha), selects de Tipo/Ámbito/Criterio, campos de posición (solo si Criterio = Posición), Estado mostrado como texto de solo lectura. Mismo estilo (`form-card`, `btn`, etc.) que el resto del panel, sin rediseño.

### Frontend de usuario

- Nueva entrada "Premios" en el menú, visible para cualquier usuario autenticado.
- `PrizesCompetitionsPage` (`/prizes`) → `PrizesEditionsPage` (`/prizes/competitions/:id/editions`) → `PrizesListPage` (`/prizes/editions/:id`), misma navegación jerárquica que Rankings/Pronósticos.
- Cada Premio se muestra como tarjeta simple: Nombre, Descripción, Tipo, Valor de referencia (si tiene), Sponsor (si tiene), "Para quién es" (`forLabel`), Estado, y "Ganador actual (provisional): ..." o "Todavía no hay ganador provisional." si el Ranking no tiene datos. Sin botones administrativos, sin Premios Borrador ni Cancelados, sin entrega/pago/reclamo.

### Datos de demostración (solo Development, idempotentes)

`backend/Data/DataSeeder.cs`, `SeedPrizesDemoAsync` (corre después de `SeedRankingDemoAsync`, idempotente por `EditionId`+`Name`), sobre Clausura 2026 / Fecha 1:

| Premio | Tipo | Ámbito | Criterio | Estado | Ganador actual esperado |
|---|---|---|---|---|---|
| Gran Premio Clausura 2026 | Dinero | Edición | Posición 1 | Publicado | Juan Pérez |
| Segundo Premio Clausura 2026 | Producto | Edición | Posición 2 | Publicado | Ana Torres |
| Premio Fecha 1 | Producto | Fecha | Ganador de Fecha | Publicado | Juan Pérez |
| Rey de los Exactos | Reconocimiento | Edición | Mayor cantidad de exactos | Publicado | Juan Pérez |
| Premio Sorpresa | Otro | Edición | Posición 3 | Borrador | (no visible para usuarios) |

### Pruebas realizadas (casos A-L del enunciado, todos por API; datos temporales revertidos)

- **A-D**: ganadores verificados exactos — posición 1 → Juan Pérez; posición 2 → Ana Torres; Ganador de Fecha 1 → Juan Pérez; mayor cantidad de exactos → Juan Pérez.
- **E**: usuario temporal con pronósticos idénticos a Juan Pérez (empate en posición 1) → el Premio de posición 1 devolvió ambos usuarios como ganadores provisionales. Revertido (usuario, pronósticos y evaluaciones eliminados).
- **F**: mismo usuario temporal (mismo `ExactCount` máximo) → el Premio "Rey de los Exactos" devolvió ambos usuarios empatados. Revertido junto con el caso E.
- **G**: `GET /api/admin/prizes` (ADMIN) incluye el Premio Borrador; `GET /api/prizes/editions/7` (USER) lo excluye; `GET /api/prizes/{id}` (USER) devuelve 404 para ese Premio.
- **H**: Premio temporal publicado y cancelado → excluido de `GET /api/prizes/editions/{id}` y 404 en `GET /api/prizes/{id}` para USER. Revertido (eliminado).
- **I**: crear Premio con `EditionId` de una Edición y `RoundId` de una Fecha de otra Edición → 400 ("La Fecha indicada no pertenece a la Edición del Premio.").
- **J**: `PositionFrom = 3`, `PositionTo = 2` → 400 ("La posición hasta debe ser mayor o igual a la posición desde.").
- **K**: usuario USER contra `GET`/`POST /api/admin/prizes` → 403 en ambos.
- **L**: Premio temporal sobre la Edición de Copa Libertadores (Ranking vacío) → `currentWinners: []`, `hasProvisionalWinner: false`; ningún ganador inventado. Revertido (eliminado).
- Validaciones adicionales verificadas: modificar un Premio Cancelado → 400; cerrar un Premio en Borrador → 400; cancelar un Premio ya Cerrado → 400.
- Verificado visualmente en el navegador: lista administrativa (con las 5 filas y las acciones correctas según estado), formulario de alta con selects en cascada Competencia → Edición → Fecha funcionando, flujo completo de alta → edición end-to-end, y las 4 tarjetas de Premios Publicados en la pantalla de usuario (el Premio Borrador no aparece). Consola del navegador sin errores.
- Swagger expone correctamente los 8 endpoints nuevos (`Admin Prizes` y `Prizes`).
- `dotnet build`: OK. `npm run build`: OK. `docker compose up -d --build`: 3 servicios healthy. Migración `AddPrizes` aplicada y verificada en PostgreSQL. Logs de `backend`/`frontend`/`db` revisados: sin errores de la aplicación.
- Estado final verificado: 5 `Prizes`, 12 `Predictions`, 12 `PredictionEvaluations`, sin datos temporales residuales de las pruebas (Premios y usuario temporal de las pruebas E/F/H/L/UI eliminados directamente por no existir endpoint de borrado físico, tal como establece el principio "un Premio publicado no se elimina físicamente" — la eliminación directa se aplicó únicamente a los registros de prueba creados en esta sesión, nunca a los 5 Premios de demostración).

### Archivos modificados/creados

Backend: `Domain/Entities/Prize.cs`, `Domain/Enums/PrizeType.cs`, `Domain/Enums/PrizeScopeType.cs`, `Domain/Enums/PrizeAwardCriteria.cs`, `Domain/Enums/PrizeStatus.cs`, `Data/Configurations/PrizeConfiguration.cs`, `Dtos/PrizeDtos.cs`, `Services/PrizeWinnerService.cs`, `Services/PrizeMapper.cs`, `Endpoints/AdminPrizeEndpoints.cs`, `Endpoints/PrizeEndpoints.cs`, `Migrations/20260731180119_AddPrizes.cs` y `.Designer.cs` (nuevos); `Data/PlayPredictDbContext.cs`, `Data/DataSeeder.cs`, `Program.cs`, `Migrations/PlayPredictDbContextModelSnapshot.cs` (modificados).

Frontend: `pages/AdminPrizesListPage.tsx`, `pages/AdminPrizeFormPage.tsx`, `pages/PrizesCompetitionsPage.tsx`, `pages/PrizesEditionsPage.tsx`, `pages/PrizesListPage.tsx` (nuevos); `api/types.ts`, `App.tsx`, `components/Layout.tsx`, `components/admin.css` (modificados).

---

## Sprint 6 — Motor de Rankings

Objetivo: mostrar posiciones a partir de las evaluaciones ya calculadas por el Motor de Puntuación (Sprint 5). El Ranking **no calcula puntos** — únicamente consulta y ordena.

### Alcance

Implementado: Ranking General de una Edición, Ranking por Fecha. Explícitamente fuera de alcance (no implementado): ranking mensual, histórico, por empresa, por grupo privado, premios, bonificaciones.

### Modelo

Sin tabla nueva, sin migración. Todo se calcula dinámicamente consultando `Predictions`, `PredictionEvaluations`, `Matches`, `Rounds` y `Editions` en el momento de la consulta.

### Backend

- `backend/Services/RankingService.cs`: responsabilidad única, sin persistir nada.
  - `GetEditionRankingAsync(db, editionId)`: filtra `PredictionEvaluations` por `Prediction.Match.Round.EditionId`.
  - `GetRoundRankingAsync(db, roundId)`: filtra por `Prediction.Match.RoundId`.
  - Ambos agrupan por usuario, suman `Points`, cuentan `ExactScore`/`CorrectOutcome`/`Incorrect` y el total evaluado.
  - **Orden**: puntos (desc) → exactos (desc) → correctos (desc) → incorrectos (asc) → apellido → nombre. Los dos últimos criterios (apellido/nombre) solo desempatan visualmente el orden de la lista; nunca alteran el número de posición.
  - **Posición compartida**: se calcula comparando la tupla (puntos, exactos, correctos, incorrectos) fila a fila; si es idéntica a la anterior, se repite el mismo número de posición. Estilo "ranking deportivo" (1, 2, 2, 4, ...) — tras un empate en la posición 2, el siguiente usuario va en la posición 4, no en la 3.
  - Solo participan usuarios con al menos una `PredictionEvaluation` (join implícito: si no hay evaluación, no hay fila).
- `backend/Dtos/RankingDtos.cs`: `RankingEntryDto(Position, UserId, FirstName, LastName, Points, ExactCount, CorrectCount, IncorrectCount, EvaluatedCount)`.
- `backend/Endpoints/RankingEndpoints.cs`: `GET /api/rankings/editions/{editionId}` y `GET /api/rankings/rounds/{roundId}`, ambos `RequireAuthorization()` (sin restricción de rol — cualquier usuario autenticado puede consultar rankings). 404 si la Edición/Fecha no existe.
- `RankingService` registrado como `Scoped` en `Program.cs`.

### Frontend

- Nueva entrada "Rankings" en el menú (`Layout.tsx`), visible para cualquier usuario autenticado.
- 5 pantallas nuevas (mismo estilo que Fixture/Pronósticos, sin rediseño):
  - `RankingsCompetitionsPage` (`/rankings`) — lista de Competencias.
  - `RankingsEditionsPage` (`/rankings/competitions/:competitionId/editions`) — lista de Ediciones; cada fila lleva al Ranking General de esa Edición, con un botón secundario "Ranking por Fecha" hacia la lista de Fechas.
  - `RankingGeneralPage` (`/rankings/editions/:editionId`) — tabla del Ranking General, con acceso directo a "Ranking por Fecha".
  - `RankingsRoundsPage` (`/rankings/editions/:editionId/rounds`) — lista de Fechas de la Edición.
  - `RankingRoundPage` (`/rankings/rounds/:roundId`) — tabla del Ranking de esa Fecha.
- Columnas de la tabla: `#` / Usuario / Puntos / Exactos / Correctos / Incorrectos / Pronósticos — el número de posición (`#`) se muestra tal cual lo entrega el backend, sin recalcularlo ni reordenar en el cliente.
- `frontend/src/api/types.ts`: nuevo tipo `RankingEntry`.

### Datos de demostración (solo Development, idempotentes)

- `backend/Data/DataSeeder.cs`:
  - Los 3 partidos de Fecha 1 / Clausura 2026 pasaron de nombres genéricos ("Equipo A".."F") a nombres reales: Boca Juniors–River Plate, Racing Club–Independiente, Estudiantes–Gimnasia (cambiado en el seed base `SeedAsync`, y con un ajuste de compatibilidad en `SeedRankingDemoAsync` para bases de datos que ya tenían el seed anterior aplicado).
  - `SeedRankingDemoAsync` (nuevo, solo Development, llamado después de `SeedEditionScoringConfigurationsAsync` para que la configuración de puntuación ya exista): crea 4 usuarios (Ana Torres, Juan Pérez, María López, Pedro Gómez — rol USER, contraseña `demo123`, emails `@playpredict.local` para no colisionar con usuarios de prueba preexistentes de sesiones anteriores), sus pronósticos, carga los 3 resultados oficiales (Boca 2-1 River, Racing 1-1 Independiente, Estudiantes 0-2 Gimnasia) y dispara la evaluación automática vía `PredictionEvaluationService` — la misma vía que usaría un Administrador desde el panel.
  - Totales resultantes verificados exactos con la configuración de puntuación por defecto (6/3/0): Ana 12, Juan 15, María 9, Pedro 6 — coinciden con los valores esperados del enunciado del Sprint.
  - Idempotente: cada paso (nombres de partido, alta de usuario, alta de pronóstico, carga de resultado) verifica su propia condición antes de escribir; correr el seed varias veces nunca duplica nada.

### Pruebas realizadas

- Ranking General de Clausura 2026 verificado exacto: 1° Juan Pérez (15), 2° Ana Torres (12), 3° María López (9), 4° Pedro Gómez (6) — coincide con el "Ranking esperado" del enunciado. Confirmado por API y visualmente en el navegador.
- Ranking por Fecha (Fecha 1) idéntico al General (es la única Fecha de esa Edición) — confirmado.
- **Empates y posición compartida**: se creó temporalmente un usuario con los mismos pronósticos que Ana Torres (mismo resultado exacto en los 3 partidos) → el ranking mostró posición 2 compartida entre ambos (ordenados alfabéticamente, "Acosta" antes que "Torres"), y el siguiente usuario saltó a la posición 4. Verificado por API y visualmente. Usuario y datos de prueba eliminados al final.
- **Agregar un resultado nuevo**: se creó un pronóstico de Juan sobre un partido de Copa Libertadores (Edición sin ranking previo, `[]` vacío) y se cargó su resultado oficial por primera vez → el ranking de esa Edición pasó de vacío a mostrar a Juan automáticamente. Revertido al final (partido de vuelta a Programado sin resultado, pronóstico y evaluación eliminados).
- **Corregir un resultado existente**: se cambió el resultado de Estudiantes–Gimnasia de 0-2 a 2-2 → el ranking general recalculó automáticamente y generó nuevos empates (Juan/Ana en 9, Pedro/María en 6), confirmando el recálculo. Revertido al resultado original (0-2); el ranking volvió exactamente a 15/12/9/6.
- Verificado en PostgreSQL al final: `Predictions` = 12, `PredictionEvaluations` = 12 (exactamente las 4 usuarias × 3 partidos, sin residuos de las pruebas temporales).
- Swagger (`/swagger`) expone correctamente `GET /api/rankings/editions/{editionId}` y `GET /api/rankings/rounds/{roundId}` bajo "Rankings".
- `dotnet build`: OK. `npm run build`: OK. `docker compose up -d --build`: 3 servicios healthy (hubo que esperar el arranque normal del backend tras aplicar el seed nuevo). Logs de `backend`/`frontend` revisados: sin errores ni excepciones. Consola del navegador sin errores.

### Archivos modificados/creados

Backend: `Dtos/RankingDtos.cs`, `Services/RankingService.cs`, `Endpoints/RankingEndpoints.cs` (nuevos); `Data/DataSeeder.cs`, `Program.cs` (modificados).

Frontend: `pages/RankingsCompetitionsPage.tsx`, `pages/RankingsEditionsPage.tsx`, `pages/RankingsRoundsPage.tsx`, `pages/RankingGeneralPage.tsx`, `pages/RankingRoundPage.tsx` (nuevos); `api/types.ts`, `App.tsx`, `components/Layout.tsx` (modificados).

### Revisión técnica previa al commit (misma sesión, tras la aprobación funcional)

- Documentación: verificado que no quedaran referencias obsoletas ("Sprint 6 pendiente", "próximo paso: Sprint 6", etc.) en `SESSION.md`/`PROJECT_STATUS.md`; corregido el encabezado de estado y el "próximo paso" para reflejar que el Sprint 6 ya está aprobado funcionalmente, pendiente únicamente del commit, y que el próximo paso es el Sprint 7 (Premios).
- Seed: confirmado (no fue necesario corregir nada) que `SeedRankingDemoAsync`, `SeedAsync` y `SeedAdminUserAsync` — los únicos que crean usuarios/pronósticos/evaluaciones/resultados de demostración — solo se invocan dentro de `if (app.Environment.IsDevelopment())` en `Program.cs`; no existe ningún otro punto de llamada. En Producción no se siembra ningún dato de demostración.
- UX de ranking vacío: confirmado (ya estaba implementado, no fue necesario agregar nada) que tanto `RankingGeneralPage.tsx` como `RankingRoundPage.tsx` muestran un mensaje ("Todavía no hay pronósticos evaluados en esta Edición/Fecha.") en vez de una tabla vacía cuando el ranking no tiene filas.
- `dotnet build`: OK. `npm run build`: OK. `docker compose ps`: 3 servicios `Up`/`healthy`.

---

## Nota: reorganización de `docs/` consolidada

En la sesión del Sprint 5 se encontró la carpeta `docs/` reorganizada manualmente en el árbol de trabajo, sin commit (archivos planos movidos a `docs/arquitectura/`, `docs/products/`, `docs/business/`). Se informó, no se tocó, y se continuó con el Sprint. En la sesión siguiente, ya aprobado el Sprint 5, se pidió consolidar esa reorganización antes de commitear. Se hizo lo siguiente:

- Se verificó contenido idéntico entre cada archivo movido y su versión commiteada anterior: los 4 `.md` (`MODELO_CONCEPTUAL`, `MODELO_DATOS`, `REGLAS_NEGOCIO`, `PANTALLAS_MVP`, `PLAN_IMPLEMENTACION_MVP` → ahora con sufijo `_v1.0`) son byte-idénticos salvo fin de línea (LF→CRLF, ya esperado en este repo); los 5 PDF de `docs/business/` tienen el mismo hash SHA-256 que sus originales. **No se perdió contenido.**
- Se renombró `docs/products/ROADMAP_PRONOSTICOS.md` → `docs/products/ROADMAP_PRONOSTICOS_v1.0.md` (único archivo que faltaba el sufijo de versión pedido).
- No se creó `MOTOR_DE_PRONOSTICOS_v1.0.md` (no existía y no se pidió crearlo); `MODELO_CONCEPTUAL_PRONOSTICOS_v1.0.md` queda como documento central del Motor de Pronósticos.
- `docs/README_DOCS.md` reescrito con las rutas reales de los 14 documentos existentes (antes listaba archivos inexistentes como `MOTOR_DE_PRONOSTICOS_v1.0` y `PRODUCTO_PLAYPREDICT_v1.0`, y omitía varios que sí existen).
- `README.md` (raíz): el enlace a `docs/` en "Documentación funcional" apuntaba a la carpeta genérica; se actualizó para apuntar a `docs/README_DOCS.md`. No se encontraron más enlaces rotos en `CLAUDE.md`, `SESSION.md` ni `PROJECT_STATUS.md` (las menciones a rutas antiguas eran registros históricos de sesiones previas, no enlaces activos; se dejaron como constancia de lo ocurrido en su momento).
- `Audios relevamiento/` se mantiene fuera de `docs/` (es material fuente sin procesar) pero ahora está referenciada explícitamente en `docs/README_DOCS.md`, sección "Research".

Estructura final de `docs/`:
```
docs/
├── README_DOCS.md
├── arquitectura/
│   ├── MODELO_CONCEPTUAL_v1.0.md
│   ├── MODELO_DATOS_v1.0.md
│   ├── REGLAS_NEGOCIO_v1.0.md
│   ├── MODELO_CONCEPTUAL_PRONOSTICOS_v1.0.md
│   ├── TIPOS_PRONOSTICOS_v1.0.md
│   ├── MOTOR_PUNTUACION_v1.0.md
│   └── CONFIGURACION_COMPETENCIAS_v1.0.md
├── products/
│   ├── PANTALLAS_MVP_v1.0.md
│   ├── PLAN_IMPLEMENTACION_MVP_v1.0.md
│   └── ROADMAP_PRONOSTICOS_v1.0.md
└── business/
    ├── Etapa_1_28-07-2026_PlayPredict.pdf
    ├── Propuesta_Plataforma_Engagement_Deportivo_v1.pdf
    ├── Propuesta_Plataforma_de_Pronosticos_Deportiva_v2.pdf
    ├── Propuesta_Plataforma_de_Pronosticos_Deportiva_v3.pdf
    └── Propuesta_Plataforma_de_Pronosticos_Deportiva_v4.pdf
```

---

## Sprint 5 — Motor de Puntuación Configurable Básico

Objetivo: calcular automáticamente los puntos de los pronósticos de marcador ya existentes, con la puntuación configurable por Edición. Sin rankings, posiciones ni premios (quedan para sprints posteriores).

### Modelo

- `EditionScoringConfiguration` (`backend/Domain/Entities/EditionScoringConfiguration.cs`): `Id`, `EditionId` (único, relación 1 a 1 con `Edition`), `ExactScorePoints`, `CorrectOutcomePoints`, `IncorrectPoints`, `CreatedAtUtc`, `UpdatedAtUtc`. FK a `Edition` con `DeleteBehavior.Restrict`.
- `PredictionEvaluation` (`backend/Domain/Entities/PredictionEvaluation.cs`): `Id`, `PredictionId` (único, sin historial — se actualiza en el lugar al recalcular), `Points`, `EvaluationType`, `OfficialHomeScore`, `OfficialAwayScore`, `AppliedRuleValue`, `EvaluatedAtUtc`. FK a `Prediction` con `DeleteBehavior.Restrict`.
- Enum `EvaluationType` (`backend/Domain/Enums/EvaluationType.cs`): `ExactScore`, `CorrectOutcome`, `Incorrect`.
- Migración `AddScoringEngine` creada y aplicada (tablas `EditionScoringConfigurations`, `PredictionEvaluations`).

### Backend

- `backend/Services/PredictionEvaluationService.cs`: única responsabilidad — dado un `Match` ya con resultado oficial y `Status = Finished`, busca la configuración de puntuación de su Edición (vía `Round.EditionId`), evalúa cada `Prediction` del partido y **prepara** (crea o actualiza) su `PredictionEvaluation` en el `ChangeTracker`, sin llamar a `SaveChangesAsync` — el llamador persiste todo junto en una única transacción implícita. Prioridad: marcador exacto > resultado correcto > incorrecto (una sola categoría aplicada por pronóstico); el signo del resultado (`Math.Sign(local - visitante)`) determina "resultado correcto" (gana local / empate / gana visitante).
- `backend/Endpoints/MatchEndpoints.cs`, `PUT /api/matches/{id}/result`: además de guardar el resultado oficial, invoca `PredictionEvaluationService.PrepareEvaluationsForMatchAsync` antes del único `SaveChangesAsync` — el resultado oficial y todas las evaluaciones afectadas se guardan atómicamente. Si el resultado se vuelve a cargar (corrección), las evaluaciones existentes se actualizan en el lugar (no se duplican, por el índice único en `PredictionId`).
- `backend/Endpoints/EditionScoringConfigurationEndpoints.cs` (nuevo): `GET/PUT /api/editions/{editionId}/scoring-configuration`, ambos `RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin))`. El `PUT` valida enteros ≥ 0 por campo (400 con detalle si no) y Edición existente (404).
- `backend/Dtos/PredictionDtos.cs`: `PredictionDto` ahora incluye `Points`, `EvaluationType`, `EvaluationLabel` (castellano: "Marcador exacto" / "Resultado correcto" / "Incorrecto"), `OfficialHomeScore`, `OfficialAwayScore` — todos `null` si el partido todavía no fue evaluado. `GET /api/predictions/rounds/{roundId}` y `GET /api/predictions/me` arman esta información uniendo `Prediction` con su `PredictionEvaluation` (si existe); `POST`/`PUT` de pronósticos no la incluyen (nunca puede existir evaluación en el momento de crear/editar, porque eso solo es posible mientras el partido no está Finalizado).
- `backend/Dtos/ScoringDtos.cs` (nuevo): `EditionScoringConfigurationDto`, `UpdateEditionScoringConfigurationDto`.
- Configuración inicial automática:
  - `backend/Data/DataSeeder.cs`, `SeedEditionScoringConfigurationsAsync` (corre en todos los entornos, después de cualquier seed de datos): crea 6/3/0 para toda Edición que todavía no tenga configuración — cubre las Ediciones que ya existían antes de este Sprint.
  - `backend/Endpoints/EditionEndpoints.cs`, `POST /api/competitions/{competitionId}/editions`: ahora crea también la `EditionScoringConfiguration` (6/3/0) en el mismo `SaveChangesAsync` al dar de alta una Edición nueva.
- No se calculan puntos para partidos sin resultado oficial ni para partidos Cancelados o Suspendidos: la evaluación solo se dispara desde `/result`, que ya rechaza Cancelado y siempre deja el partido en `Finished`; nunca se ejecuta con el partido en otro estado.
- No hay edición manual de puntos: no existe ningún endpoint que permita escribir `Points` directamente.

### Frontend administrativo

- `frontend/src/pages/EditionScoringConfigurationPage.tsx` (nueva, ruta `/editions/:editionId/scoring-configuration`, envuelta en `RequireAdmin`): formulario con los 3 campos (marcador exacto / resultado correcto / resultado incorrecto), enteros no negativos (clamp en el cliente + validación del backend como autoridad final), mensaje de éxito, botón "Volver a Ediciones". Mismo estilo (`form-card`, `btn`, etc.) que el resto del panel.
- `frontend/src/pages/EditionsListPage.tsx`: agregado el botón "Configurar puntuación" junto a "Editar" en cada fila, visible solo si `user.roles` incluye `ADMIN`.

### Frontend del usuario (Pronósticos)

- `frontend/src/pages/PredictionsMatchesPage.tsx`: sin cambios en partidos no Finalizados (se mantiene el comportamiento del Sprint 4, sin mostrar puntos). Para partidos Finalizados, la columna "Mi pronóstico" ahora muestra, si el usuario pronosticó: "Mi pronóstico: X - Y", "Resultado oficial: X - Y", "Puntos obtenidos: N", "Motivo: <Marcador exacto|Resultado correcto|Incorrecto>" (todo tomado tal cual de la API, nunca calculado en el cliente). Si el usuario no pronosticó ese partido: "Sin pronóstico", sin inventar evaluación ni puntos. Cancelado sigue mostrando "—".
- `frontend/src/api/types.ts`: `Prediction` extendido con `points`, `evaluationType`, `evaluationLabel`, `officialHomeScore`, `officialAwayScore`; nuevo tipo `EditionScoringConfiguration`.
- `admin.css`: una clase nueva (`.prediction-result`) para el bloque de 4 líneas, mismo tamaño de fuente y color que el resto de los textos secundarios del panel. Sin rediseño.

### Pruebas realizadas (Edición "Clausura 2026", vía API con `curl` + verificación visual)

Configuración inicial 6/3/0 (verificada tras el seed).

| Caso | Pronóstico | Resultado oficial | Esperado | Obtenido |
|---|---|---|---|---|
| A | 2-1 | 2-1 | 6, Marcador exacto | ✅ 6, ExactScore |
| B | 1-0 | 3-1 | 3, Resultado correcto | ✅ 3, CorrectOutcome |
| C | 0-0 | 2-2 | 3, Resultado correcto | ✅ 3, CorrectOutcome |
| D | 1-2 | 2-1 | 0, Incorrecto | ✅ 0, Incorrect |

- **Caso E** (recálculo por cambio de configuración): se cambió la configuración a 10/4/1 y se volvió a cargar el mismo resultado oficial en los 3 partidos de los casos A-C → recalcularon a 10, 4 y 4 puntos respectivamente, usando los nuevos valores. El partido del Caso D (de otra Edición, con su propia configuración sin modificar) siguió en 0 — confirma que la configuración es por Edición, no global.
- **Caso F** (corrección de resultado oficial): se corrigió el resultado del Caso B de 3-1 a 1-1 → la evaluación cambió de "Resultado correcto" (4 pts) a "Incorrecto" (1 pt) **en el mismo registro** (`PredictionEvaluations` siguió teniendo exactamente 4 filas en total, una por pronóstico, sin duplicados).
- **Caso G** (usuario sin pronóstico): un usuario registrado sin pronósticos, al consultar `GET /api/predictions/rounds/{roundId}`, recibe `myPrediction: null` en todos los partidos, incluidos los Finalizados — sin evaluación inventada. Confirmado también visualmente ("Sin pronóstico" en las 3 filas).
- **Caso H** (autorización): un usuario sin rol ADMIN recibe 403 tanto en `GET` como en `PUT /api/editions/{id}/scoring-configuration`.
- **Caso I** (validación): `PUT` con valores negativos → 400 con el detalle de los 3 campos; la configuración no se modificó.
- Verificado en el navegador: pantalla "Configurar puntuación" carga y muestra los valores reales de la base; botón visible solo para ADMIN en la lista de Ediciones; pantalla de Pronósticos muestra exactamente "Mi pronóstico / Resultado oficial / Puntos obtenidos / Motivo" para partidos Finalizados, y "Sin pronóstico" cuando corresponde. Consola del navegador sin errores.
- Swagger (`/swagger`) expone correctamente `GET/PUT /api/editions/{editionId}/scoring-configuration` bajo "Edition Scoring Configuration", con su DTO.
- `dotnet build`: OK. `npm run build`: OK. `docker compose up -d --build`: 3 servicios healthy (hubo un timeout transitorio del healthcheck del backend durante el primer arranque post-migración; se resolvió solo, sin intervención de código). Migración `AddScoringEngine` aplicada y verificada directamente en PostgreSQL (2 filas en `EditionScoringConfigurations`, una por Edición existente, ambas en 6/3/0 tras el seed). Logs de `backend`/`frontend`/`db` revisados: sin errores ni excepciones.
- Datos de prueba (pronósticos, evaluaciones, cambios de resultado/configuración, usuario de prueba) revertidos al final: `Predictions` y `PredictionEvaluations` en 0 filas, ambas configuraciones de puntuación de vuelta en 6/3/0, los 6 partidos de demostración de vuelta en estado Programado sin resultado.

### Archivos modificados/creados

Backend: `Domain/Entities/EditionScoringConfiguration.cs`, `Domain/Entities/PredictionEvaluation.cs`, `Domain/Enums/EvaluationType.cs`, `Data/Configurations/EditionScoringConfigurationConfiguration.cs`, `Data/Configurations/PredictionEvaluationConfiguration.cs`, `Dtos/ScoringDtos.cs`, `Endpoints/EditionScoringConfigurationEndpoints.cs`, `Services/PredictionEvaluationService.cs`, `Migrations/20260731010056_AddScoringEngine.cs` y `.Designer.cs` (nuevos); `Data/PlayPredictDbContext.cs`, `Data/DataSeeder.cs`, `Dtos/PredictionDtos.cs`, `Endpoints/EditionEndpoints.cs`, `Endpoints/MatchEndpoints.cs`, `Endpoints/PredictionEndpoints.cs`, `Migrations/PlayPredictDbContextModelSnapshot.cs`, `Program.cs` (modificados).

Frontend: `pages/EditionScoringConfigurationPage.tsx` (nuevo); `api/types.ts`, `App.tsx`, `pages/EditionsListPage.tsx`, `pages/PredictionsMatchesPage.tsx`, `components/admin.css` (modificados).

### Verificación visual final (sesión posterior, antes de aprobar el commit)

Con el Sprint 5 ya aprobado técnicamente, se pidió una comprobación visual adicional en el navegador (no solo por API) antes de commitear. Se hizo íntegramente en la UI real (pantalla "Configurar puntuación" y modal "Resultado Oficial" del panel de Fixture):

1. Edición "Clausura 2026" configurada en 10 / 4 / 1 desde `/editions/7/scoring-configuration` → mensaje "Configuración guardada correctamente.", valores confirmados al recargar.
2. Pronóstico 2-1 cargado en "Equipo A vs Equipo B" desde la pantalla de Pronósticos.
3. Resultado oficial 2-1 → la pantalla de Pronósticos mostró "Puntos obtenidos: 10 / Motivo: Marcador exacto".
4. Resultado oficial corregido a 3-1 (mismo partido, vía "Cargar resultado" de nuevo) → cambió a "Puntos obtenidos: 4 / Motivo: Resultado correcto".
5. Resultado oficial corregido a 1-2 → cambió a "Puntos obtenidos: 1 / Motivo: Incorrecto".
6. Verificado directamente en PostgreSQL después del paso 5: `PredictionEvaluations` tenía exactamente **1 fila** (mismo `Id`, actualizada tres veces) y `Predictions` exactamente 1 fila — confirma que el recálculo actualiza la evaluación existente y nunca duplica.

Después de la verificación se revirtió todo: `Predictions` y `PredictionEvaluations` en 0 filas, la configuración de la Edición 7 de vuelta en 6/3/0, y el partido usado de vuelta en Programado sin resultado (mismo estado que el resto del fixture de demostración).

---

## Sprint 4 — Sistema de Pronósticos

Objetivo: construir toda la infraestructura para que un usuario pueda pronosticar partidos, sin calcular puntos, sin tablas, sin rankings ni posiciones (eso queda para Sprint 5 en adelante).

### Modelo

- Nueva entidad `Prediction` (`backend/Domain/Entities/Prediction.cs`): `Id`, `MatchId`, `UserId`, `PredictedHomeScore`, `PredictedAwayScore`, `CreatedAtUtc`, `UpdatedAtUtc`.
- Configuración EF (`backend/Data/Configurations/PredictionConfiguration.cs`): índice único `(UserId, MatchId)` — un usuario solo puede tener un pronóstico por partido; FKs a `Matches` y `Users` con `DeleteBehavior.Restrict`.
- Migración `AddPredictions` creada y aplicada (tabla `Predictions`).

### Backend

- DTOs (`backend/Dtos/PredictionDtos.cs`): `PredictionDto`, `CreatePredictionDto`, `UpdatePredictionDto`, `MatchWithPredictionDto` (partido + resultado oficial + pronóstico propio, si existe).
- Endpoints (`backend/Endpoints/PredictionEndpoints.cs`, grupo `/api/predictions`, todos con `RequireAuthorization()`):
  - `GET /api/predictions/rounds/{roundId}` — partidos de la Fecha con el pronóstico propio embebido (o `null`). Es el endpoint que alimenta la pantalla principal del frontend.
  - `GET /api/predictions/me` — todos los pronósticos del usuario autenticado.
  - `POST /api/predictions` — crea un pronóstico (`matchId`, `predictedHomeScore`, `predictedAwayScore`).
  - `PUT /api/predictions/{id}` — modifica un pronóstico propio.
- Reglas implementadas:
  - Un usuario, un pronóstico por partido: índice único en base + verificación explícita antes de insertar (409 si ya existe).
  - Solo se puede crear o modificar un pronóstico si el partido está Programado **y** su horario de inicio todavía no llegó (`CanCreateOrEditPrediction`, corregido — ver "Fix: regla de cierre de Pronósticos" más abajo). Bloqueado (400) en cualquier otro caso, tanto al crear como al editar.
  - Un usuario no puede modificar el pronóstico de otro usuario (403).
  - Goles pronosticados no pueden ser negativos (400 con detalle de campo).
  - Sin autenticación → 401 en todos los endpoints.
  - No se calculan puntos ni se compara contra el resultado oficial en ningún punto del código.

### Frontend

- Nueva entrada "Pronósticos" en el menú (`Layout.tsx`), visible para cualquier usuario autenticado.
- 4 pantallas nuevas (`frontend/src/pages/`), navegación exactamente igual a la del panel administrativo pero de solo lectura hasta llegar a los partidos:
  - `PredictionsCompetitionsPage` (`/predictions`) — lista de Competencias.
  - `PredictionsEditionsPage` (`/predictions/competitions/:competitionId/editions`) — lista de Ediciones.
  - `PredictionsRoundsPage` (`/predictions/editions/:editionId/rounds`) — lista de Fechas.
  - `PredictionsMatchesPage` (`/predictions/rounds/:roundId`) — pantalla única de carga: por cada partido muestra equipos, fecha, hora, resultado oficial (si existe) y un bloque "Mi pronóstico" con inputs Local/Visitante y botón "Guardar pronóstico" (o "Actualizar pronóstico" si ya existe uno), todo guardado partido por partido sin salir de la pantalla.
  - Estados especiales en la columna "Mi pronóstico": partido Finalizado → texto fijo "Pronóstico cerrado" (+ el valor pronosticado entre paréntesis si existía); partido Cancelado → "—" (no se permite pronosticar).
- Tipos nuevos en `frontend/src/api/types.ts`: `Prediction`, `MatchWithPrediction`.
- CSS: 3 clases puntuales agregadas a `admin.css` (`.prediction-row`, `.prediction-row__inputs`, `.prediction-row__input`, `.prediction-row__saved`) reutilizando los mismos colores y tipografía ya definidos en el archivo. Sin cambios de layout, colores ni rediseño de pantallas existentes.

### Pruebas realizadas

- **Backend (vía `curl`)**: crear pronóstico (201); duplicado sobre el mismo partido (409); editar pronóstico propio (200, persiste); `GET /me` y `GET /rounds/{roundId}` reflejan el cambio; crear/editar sobre partido Finalizado (400 en ambos casos); crear sobre partido Cancelado (400); goles negativos (400); sin token (401 en todos los endpoints); usuario distinto intentando editar un pronóstico ajeno (403).
- **Frontend (navegador)**: navegación completa Competencias → Ediciones → Fechas → Partidos bajo "Pronósticos"; pantalla de carga renderiza correctamente con el estilo existente; guardar un pronóstico nuevo muestra "Pronóstico guardado correctamente." y cambia el botón a "Actualizar pronóstico"; recargar la página conserva el valor cargado; partido Finalizado muestra "Pronóstico cerrado (X - Y)" sin inputs; partido Cancelado muestra "—" sin inputs ni botón.
  - Nota técnica: la herramienta de automatización de navegador de esta sesión no lograba entregar clics/tecleo reales a la página (se verificó que tampoco funcionaba en un input preexistente de "Mi perfil", ajeno a este Sprint). Se validó el flujo disparando los mismos eventos DOM nativos que React escucha y confirmando las peticiones de red resultantes (201 al guardar, datos persistidos al recargar) — el comportamiento verificado es el mismo que produciría una interacción real de mouse/teclado. Se sugiere una prueba manual rápida antes de aprobar, dado que no se pudo confirmar con clics 100% "físicos" en esta sesión.
- Swagger (`/swagger`) expone correctamente los 4 endpoints nuevos bajo "Predictions", con sus DTOs.
- `dotnet build`: OK. `npm run build`: OK. `docker compose up -d --build`: 3 servicios healthy. Logs de `backend`/`frontend` sin errores ni excepciones.
- Datos de prueba (usuarios, pronósticos, estados de partido) usados durante la validación fueron revertidos al final; el fixture quedó en su estado limpio (6 partidos Programados, sin pronósticos).

### Archivos modificados/creados

Backend: `Domain/Entities/Prediction.cs` (nuevo), `Data/Configurations/PredictionConfiguration.cs` (nuevo), `Dtos/PredictionDtos.cs` (nuevo), `Endpoints/PredictionEndpoints.cs` (nuevo), `Migrations/20260730172829_AddPredictions.cs` y `.Designer.cs` (nuevos), `Data/PlayPredictDbContext.cs`, `Migrations/PlayPredictDbContextModelSnapshot.cs`, `Program.cs` (modificados).

Frontend: `pages/PredictionsCompetitionsPage.tsx`, `pages/PredictionsEditionsPage.tsx`, `pages/PredictionsRoundsPage.tsx`, `pages/PredictionsMatchesPage.tsx` (nuevos), `api/types.ts`, `App.tsx`, `components/Layout.tsx`, `components/admin.css` (modificados).

### Ajuste de UX posterior (antes de aprobar el sprint)

Pedido por el usuario tras probar la pantalla manualmente. Sin tocar lógica de negocio ni backend:

- `PredictionsMatchesPage.tsx`: los inputs de goles pasaron de `type="number"` (con `min={0}`, que forzaba a mostrar/asumir "0") a `type="text"` con `inputMode="numeric"`, `pattern="[0-9]*"` y `placeholder="-"`; el `onChange` sanitiza con `replace(/\D/g, '')`, descartando letras, signos y decimales antes de guardarlos en el estado. Sin pronóstico previo, el campo queda vacío (ya era así en el estado, pero el tipo `number` lo hacía sentir como "0"); con pronóstico existente, se sigue precargando el valor real.
- Se agregó `setTimeout(..., 4000)` para limpiar `savedMessage` tras guardar — antes el mensaje "Pronóstico guardado correctamente." quedaba fijo indefinidamente (no desaparecía).
- El botón Guardar/Actualizar no se tocó.
- `admin.css`: agregadas `.prediction-row__separator` (guion centrado, ancho fijo) y `.prediction-row__message` (contenedor de error/éxito con `min-height` reservado) para que la fila no cambie de alto al aparecer/desaparecer el mensaje; `.prediction-row__input` ahora incluye `box-sizing: border-box` y `text-align: center` para que ambos campos midan exactamente lo mismo.
- Verificado en el navegador: campos vacíos con placeholder "-" cuando no hay pronóstico; solo dígitos aceptados (probado enviando `"2a-3.5b"` → queda `"235"`); valores reales precargados cuando existe pronóstico; alto de fila idéntico (79.2px) antes y después de mostrar el mensaje de éxito; mensaje desaparece solo a los ~4 segundos; persistencia confirmada tras recargar.

Sprint 4 completo (entidad, migración, endpoints, pantallas, reglas originales y este ajuste de UX) commiteado en `1fd819f` — "feat: implement match predictions".

---

## Fix: regla de cierre de Pronósticos (sobre el commit `1fd819f`)

### Diagnóstico

El informe final del Sprint 4 describía la regla como "solo editable/creable mientras el partido esté Programado, En juego o Suspendido; bloqueado para Finalizado y Cancelado". Se verificó que **esa descripción era fiel al código tal como quedó commiteado** — no fue un error de redacción del informe. El código coincidía con la especificación original del Sprint 4, pero esa especificación quedó superada por la regla definitiva acordada después:

> Un pronóstico solamente puede crearse o modificarse cuando el Partido está en estado Programado **y** la fecha/hora actual UTC es anterior a la fecha/hora de inicio del Partido. Cualquier otro caso (Programado con horario ya pasado, En juego, Suspendido, Finalizado, Cancelado) debe quedar bloqueado.

Evidencia de la causa: en `backend/Endpoints/PredictionEndpoints.cs`, la función `IsOpenForPrediction(MatchStatus status)` (usada en `POST /api/predictions` y `PUT /api/predictions/{id}`) devolvía `true` para `Scheduled`, `InProgress` y `Suspended` sin comparar contra `DateTime.UtcNow`/`StartsAtUtc`. Además, `MatchWithPredictionDto` no exponía ningún indicador tipo `canPredict`: el frontend (`PredictionsMatchesPage.tsx`) calculaba la editabilidad por su cuenta con una función local `canPredict(status)` que replicaba la misma regla incompleta, en vez de depender de una decisión del backend.

**Conclusión: el problema estaba en el código, no solo en el informe.**

### Corrección

- `backend/Endpoints/PredictionEndpoints.cs`: `IsOpenForPrediction` reemplazada por `CanCreateOrEditPrediction(Match match) => match.Status == MatchStatus.Scheduled && DateTime.UtcNow < match.StartsAtUtc`, usada en `POST`, `PUT` y para calcular el nuevo campo del DTO.
- `backend/Dtos/PredictionDtos.cs`: `MatchWithPredictionDto` ahora incluye `bool CanPredict`, calculado con la misma función — el backend es la única fuente de verdad.
- `frontend/src/api/types.ts`: `MatchWithPrediction` incluye `canPredict: boolean`.
- `frontend/src/pages/PredictionsMatchesPage.tsx`: eliminada la función local `canPredict(status)`; ahora usa directamente `m.canPredict` recibido del backend. El texto "Pronóstico cerrado (X - Y)" ahora se muestra para **todos** los casos bloqueados salvo Cancelado (que sigue mostrando "—", sin cambios en ese criterio visual).

### Pruebas realizadas (los 6 casos, por API)

Preparados vía SQL directa sobre partidos de prueba (Scheduled+futuro, Scheduled+pasado, InProgress, Suspended, Finished, Cancelled):

| Caso | Estado / horario | Crear (POST) | Editar (PUT) | `canPredict` en GET |
|---|---|---|---|---|
| A | Programado + horario futuro | 201 permitido | 200 permitido | `true` |
| B | Programado + horario pasado | 400 bloqueado | 400 bloqueado | `false` |
| C | En juego | 400 bloqueado | 400 bloqueado | `false` |
| D | Suspendido | 400 bloqueado | 400 bloqueado | `false` |
| E | Finalizado | 400 bloqueado | 400 bloqueado | `false` |
| F | Cancelado | 400 bloqueado | 400 bloqueado | `false` |

Verificado además visualmente en el navegador (Fechas de Liga Profesional y Copa Libertadores): fila editable solo en el caso A; el resto muestra "Pronóstico cerrado (X - Y)" o "—" (Cancelado) según corresponda, con el valor del pronóstico ya cargado visible aunque no editable.

- `dotnet build`: OK. `npm run build`: OK. Logs de `backend`/`frontend` revisados tras el fix: sin errores ni excepciones.
- Datos de prueba (cambios de estado/horario en partidos 15-20, pronósticos de prueba) revertidos al final; el fixture quedó en su estado limpio (6 partidos Programados con horario futuro, sin pronósticos).

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
