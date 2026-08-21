# PLAYER — Ajustes UX finales

**Fecha:** 2026-08-20
**Branch:** `prueba-glm-ui`
**Commit de partida:** `126abb5`
**Commit/push:** no realizados

## Cambios realizados

### Sidebar PLAYER

- El label visible `Explorar Competencias Oficiales` se redujo a `Competencias Oficiales`.
- La ruta se conserva en `/competitions/explore`.
- El título de la pantalla se mantiene como `Explorar Competencias Oficiales`.
- El orden general quedó: Inicio → Competencias Oficiales → Mis Ligas → Ranking General → Fixture.
- No se amplió el sidebar ni se modificaron padding, alineación o estilos de selección.

### Resultados por Fecha deportiva

El encabezado visible de cada sección de Resultados agrega el día o rango calendario calculado desde `StartsAtUtc` de todos los partidos que pertenecen a esa Round, incluso si no todos están finalizados.

Regla aplicada con locale `es-AR` y zona `America/Argentina/Buenos_Aires`:

- mismo día: `Fecha 2 · 18/08/2026`;
- varios días del mismo mes: `Fecha 2 · 18–20/08/2026`;
- meses distintos del mismo año: `Fecha 2 · 30/08–02/09/2026`;
- años distintos: ambas fechas completas.

No se hardcodearon datos demo. El selector de Fecha no se modificó.

## Bloques preservados

No se modificó la máquina de estados de Pronósticos, ENTER, DELETE, participación, leave/rejoin, suspensión, creación de Liga, ConfirmModal, Ranking ni scoring.

## Archivos modificados en este ajuste

- `frontend/src/components/player/PlayerSidebar.tsx`
- `frontend/src/pages/LeagueDetailPage.tsx`
- `PROJECT_STATUS.md`
- este informe

Los demás cambios sin commit pertenecen a los bloques anteriores aprobados y fueron preservados.

## Validaciones

- `npx tsc --noEmit`: OK.
- `npm run build`: OK; 95 módulos transformados.
- Backend health: OK.
- Frontend HTTP: OK.
- `git diff --check`: OK, con advertencias esperadas de normalización LF/CRLF.
- Smoke estático de Pronósticos: las rutas/componentes siguen compilando sin cambios de lógica.

La comprobación visual exacta del sidebar seleccionado y del encabezado calendario queda para la prueba final del usuario.

## Git status final

Sin commit ni push. Se conservan todos los cambios actuales y los untracked preexistentes `.qwen/` y `Captura_Prueba.png`.
