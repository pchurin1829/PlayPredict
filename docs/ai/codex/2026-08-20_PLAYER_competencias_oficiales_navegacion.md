# PLAYER — Competencias Oficiales y navegación

**Fecha:** 2026-08-20  
**Branch:** `prueba-glm-ui`  
**Commit de partida:** `0a59e70`  
**Commit/push:** no realizados

## Objetivo

Simplificar el circuito PLAYER para separar claramente:

- Competencia Oficial: catálogo/fixture deportivo disponible en PlayPredict.
- Liga Oficial PlayPredict: ámbito masivo de participación sobre esa competencia.
- Liga con amigos: ámbito privado que reutiliza los partidos de la competencia; no crea otra competencia.

## Causa de “Participar en Liga Oficial” faltante

Había dos causas complementarias:

1. `ExploreCompetitionsPage` cargaba competencias y `/leagues/officials` con `Promise.allSettled`, pero ignoraba por completo el rechazo del segundo request. Si el endpoint fallaba, `officialLeagues` quedaba vacío y la UI no mostraba ni `Participar en Liga Oficial` ni `Ir a mi Liga`; solo sobrevivía `+ Crear Liga con amigos`. Este comportamiento reproduce exactamente el síntoma observado para todas las competencias.
2. `GetOrCreateDemoLeagueAsync` buscaba cualquier liga de Liga Profesional, sin filtrar `LeagueType`. Una liga privada podía ser reutilizada como “demo” y evitar para siempre la creación de la Liga Oficial correspondiente. Eso confundía dos ámbitos que conceptualmente deben coexistir.

Durante la verificación, backend quedó unhealthy por timeouts de Npgsql contra PostgreSQL aunque DB figuraba healthy. Esto explica por qué `/leagues/officials` podía fallar mientras el frontend ocultaba el problema.

## Correcciones

- El seeder busca exclusivamente una liga `LeagueType.Official`; una privada nunca sustituye a la Oficial.
- La creación normal de una liga declara explícitamente `LeagueType.Private`.
- Explorar muestra un error real si no puede cargar `/leagues/officials`, en vez de presentar falsamente solo el CTA de creación.
- “Mis Ligas” dejó de consultar y mostrar Ligas Oficiales disponibles. Solo consume `/leagues/mine`, que contiene ligas donde el usuario participa.
- Sidebar, título y CTAs secundarios pasan a llamarse `Explorar Competencias Oficiales`.
- Se agregó la explicación visible: “Participá en las Ligas Oficiales de PlayPredict o creá tu propia Liga con amigos usando los partidos de una competencia oficial.”
- `Volver` y `Cancelar` de Crear Liga regresan siempre a `/competitions/explore`.
- Se confirmó por búsqueda que ningún CTA PLAYER activo enlaza a `/competitions/{id}`. La ruta y su pantalla se conservan sin formar parte del flujo actual.
- No se agregaron botones globales de creación; el CTA principal sigue exclusivamente en cada tarjeta de Explorar.

## Navegación

### Anterior

```text
Explorar Competencias
  → Crear Liga con amigos
  → Crear Liga
  → Volver/Cancelar
  → /competitions/{id}
  → Explorar Competencias
```

“Mis Ligas” también mezclaba ligas participadas con Ligas Oficiales todavía disponibles.

### Nueva

```text
Explorar Competencias Oficiales
  ├─ Participar en Liga Oficial / Ir a mi Liga
  └─ + Crear Liga con amigos
       → /leagues/new?competitionId=X
       → Volver/Cancelar
       → /competitions/explore
```

“Mis Ligas” muestra únicamente ligas devueltas por `/leagues/mine`.

## Archivos modificados en esta corrección

- `backend/Data/DataSeeder.cs`
- `backend/Endpoints/LeagueEndpoints.cs`
- `frontend/src/components/player/PlayerSidebar.tsx`
- `frontend/src/pages/ExploreCompetitionsPage.tsx`
- `frontend/src/pages/LeagueCreatePage.tsx`
- `frontend/src/pages/LeaguesMinePage.tsx`
- `frontend/src/pages/PlayerDashboardPage.tsx`
- `docs/ai/codex/2026-08-20_PLAYER_competencias_oficiales_navegacion.md`

Se preservaron los cambios previos de Pronósticos, Suspender/Reactivar y documentación sin commit.

## Pruebas ejecutadas

| Prueba | Resultado |
|---|---|
| `npx tsc --noEmit` | OK, sin errores |
| `npm run build` | OK, exit code 0 |
| Build backend por `dotnet watch` | OK, 0 errores; warning NU1510 preexistente |
| Búsqueda de CTAs PLAYER a `/competitions/{id}` | OK, ninguno activo |
| Búsqueda de CTAs `Crear Liga` | Solo Explorar y la pantalla legacy aislada `/competitions/{id}`; ningún acceso PLAYER conduce a esta última |
| Backend health final | No disponible: contenedor unhealthy por timeout Npgsql contra PostgreSQL |

## Pendientes reales

Cuando backend vuelva a conectar normalmente con PostgreSQL:

- confirmar health OK y que el seeder creó la Official faltante sin modificar las privadas de Rafael;
- ejecutar A–F con Rafael: no participa → participar → Mis Ligas/Ir a mi Liga → dejar → volver a Participar;
- verificar Volver y Cancelar desde Crear Liga en navegador;
- reiniciar/refrescar frontend 5175 para que tome los cambios del host en Docker/Windows.

No se usó `docker compose down -v`, no se hicieron migraciones y no se alteraron volúmenes.
