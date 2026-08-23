# PlayPredict — Navegación ADMIN y cambio de modo

Fecha: 2026-08-22
Rama: `prueba-glm-ui`
Estado: local, sin commit ni push

## 1. Causa del entorno PLAYER para ADMIN

El rol se resolvía correctamente desde `AuthContext` y `/users/me`, y Login redirigía ADMIN a `/competitions`. El problema estaba en `Layout`: elegía el chrome ADMIN mediante una lista heurística de prefijos de URL. Al visitar `/`, `/rankings`, `/profile` u otra ruta compartida, esa heurística montaba automáticamente Header/Sidebar PLAYER aunque el usuario siguiera teniendo rol ADMIN.

Además, varias rutas del CRUD deportivo estaban dentro de `RequireAuth` pero no de `RequireAdmin`; la UI ADMIN aparecía por la URL, no por un modo explícito.

## 2. Rutas ADMIN encontradas

- `/competitions`
- `/competitions/new`
- `/competitions/:competitionId/edit`
- `/competitions/:competitionId/editions`
- `/competitions/:competitionId/editions/new`
- `/editions/:editionId/edit`
- `/editions/:editionId/scoring-configuration`
- `/editions/:editionId/rounds`
- `/editions/:editionId/rounds/new`
- `/rounds/:roundId/edit`
- `/rounds/:roundId/matches`
- `/rounds/:roundId/matches/new`
- `/matches/:matchId/edit`
- `/admin/official-leagues`
- `/admin/official-leagues/new`
- `/admin/official-leagues/:leagueId/edit`
- `/admin/prizes`
- `/admin/prizes/new`
- `/admin/prizes/:prizeId/edit`
- `/admin/users`
- `/admin/experiences`
- `/admin/experiences/new`
- `/admin/experiences/:experienceId/edit`

Se agregó `/admin` como entrada y Dashboard. Rankings reutiliza las rutas existentes `/rankings` y descendientes; el modo explícito mantiene el chrome ADMIN mientras se consultan.

## 3. Pantallas ADMIN existentes

- CRUD Competition.
- CRUD Edition y configuración de scoring.
- CRUD Round/Fecha.
- CRUD Match y carga/corrección de resultado.
- CRUD de Ligas Oficiales comerciales.
- Premios.
- Usuarios.
- Experiences/configuración general.
- Rankings existentes compartidos para consulta.

No se duplicó ninguna de estas pantallas.

## 4. Menú ADMIN final

1. Dashboard.
2. Organizaciones deportivas — PRÓXIMAMENTE.
3. Competencias.
4. Ediciones — acceso desde Competencias.
5. Equipos — PRÓXIMAMENTE.
6. Fixture / Partidos — acceso desde Competencias.
7. Ligas Oficiales.
8. Resultados — acceso desde Fixture.
9. Rankings.
10. Configuración.

El pie contiene `Vista jugador` y `Cerrar sesión`. El encabezado muestra identidad PlayPredict, badge ADMIN y usuario activo.

## 5. Dashboard ADMIN

Se creó un dashboard mínimo en `/admin` con accesos a Competencias, Ediciones, Fixture/Partidos, Ligas Oficiales, Resultados, Rankings y configuración de scoring. Los accesos contextuales conducen a Competencias porque ése es el inicio real del flujo Competition → Edition → Round → Match.

## 6. Vista jugador

- `Vista jugador` cambia el modo visual y navega a `/` sin cerrar sesión.
- El ADMIN conserva token, usuario y rol.
- En Header PLAYER aparece `Volver a Administración`.
- Esa acción restaura el modo ADMIN y abre `/admin`.
- El modo se persiste en `localStorage` para soportar refresh sin mezclar menús.
- Un login ADMIN nuevo fuerza siempre modo Administración.

## 7. Protección por roles

- Roles encontrados: `ADMIN` y `PLAYER`.
- `RequireAdmin` verifica `user.roles.includes('ADMIN')`.
- Se agregaron guards a todas las rutas frontend de creación/edición de Competition, Edition, Round y Match que antes sólo estaban autenticadas.
- PLAYER que escribe una URL `/admin/*` o una ruta CRUD deportiva es redirigido y no recibe la pantalla ADMIN.
- El cambio a Vista jugador no elimina el rol; sólo cambia el chrome activo.
- No se modificaron permisos backend en esta fase.

## 8. Opciones PRÓXIMAMENTE

- Organizaciones deportivas: no existe Organizer/SportsOrganization.
- Equipos: Match todavía almacena local/visitante como texto y no existe Team.

Importación CSV, Jugador Preferido e IA no fueron agregados.

## 9. Archivos modificados en esta fase

- `frontend/src/App.tsx`
- `frontend/src/auth/AuthContext.tsx`
- `frontend/src/components/Layout.tsx`
- `frontend/src/components/Layout.css`
- `frontend/src/components/admin.css`
- `frontend/src/components/player/PlayerHeader.tsx`
- `frontend/src/components/player/PlayerHeader.css`
- `frontend/src/pages/LoginPage.tsx`
- `frontend/src/pages/RegisterPage.tsx`
- `frontend/src/pages/AdminDashboardPage.tsx`
- `docs/ai/codex/2026-08-22_ADMIN_navigation_and_mode_switch.md`

## 10. Validaciones

- `npx tsc --noEmit`: OK.
- `npm run build`: OK.
- `git diff --check`: OK (sólo avisos informativos LF/CRLF).
- Backend no fue modificado para esta fase.

## 11. Pendientes

- Auditoría responsive completa de ADMIN queda fuera de esta fase; se agregó una adaptación mínima para navegación estrecha.
- Ediciones, Fixture y Resultados no tienen landing global independiente: mantienen el flujo jerárquico existente desde Competencias.
- Rankings reutiliza las pantallas actuales y conserva su semántica.
- La seguridad de mutaciones en endpoints deportivos backend merece una auditoría específica; este P0 corrigió exposición y guards de rutas frontend sin ampliar permisos backend.

## 12. Estado Git

- Rama `prueba-glm-ui`.
- Sin commit ni push.
- El worktree conserva cambios legítimos de las fases Light Theme, Mobile, Home y ADMIN Official Leagues, además de los untracked locales previamente excluidos.
