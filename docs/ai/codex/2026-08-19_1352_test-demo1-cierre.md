# Test Demo 1 — cierre de observaciones pendientes

**Fecha:** 2026-08-19 13:52  
**Rama:** `prueba-glm-ui`  
**Estado:** sin commit ni push; listo para prueba manual

## Diagnóstico

- El Ranking se renderiza sobre el tema oscuro, pero el título, las celdas y la posición común no tenían color explícito. Quedaban expuestos a reglas globales de tabla y varios textos perdían contraste.
- Los inputs de goles no tenían manejo de `Enter`. Los botones ya eran `type="button"`, por lo que no fue necesario cambiar la lógica de guardado para impedir submits.
- La distinción POST/PUT ya estaba implementada correctamente con los mensajes `Pronóstico guardado correctamente.` y `Pronóstico actualizado correctamente.`.

## Cambios realizados

- Se fijó el color de alto contraste del título, las celdas y las posiciones del Ranking dentro de `.pp-ranking`, sin alterar estructura ni diseño general.
- Se reforzó el contraste del badge `(Vos)` usando el tono primario oscuro existente.
- `Enter` recorre los campos de marcador en orden DOM: local → visitante → local del siguiente partido. En el último campo enfoca el botón de guardado del partido.
- `Enter` ejecuta `preventDefault()`, no guarda ni recarga. `Tab` no se intercepta y conserva su comportamiento normal.
- La navegación se agregó tanto al tab de Liga vigente como a la ruta alternativa de partidos.
- No hubo cambios de backend, modelo ni estructura de base de datos; no se creó migración.

## Archivos modificados

- `frontend/src/pages/LeagueDetailPage.tsx`
- `frontend/src/pages/PredictionsMatchesPage.tsx`
- `frontend/src/pages/PlayerPages.css`
- `PROJECT_STATUS.md`
- `docs/ai/codex/2026-08-19_1352_test-demo1-cierre.md`

## Pruebas ejecutadas

- `git diff --check`: sin errores (sólo avisos informativos LF/CRLF de Git para Windows).
- `docker compose ps`: backend y DB healthy; frontend operativo.
- `GET /api/health`: `status: ok`.
- `GET http://localhost:5175`: HTTP 200.
- Verificación del código servido por Vite: presentes `handlePredictionEnter` y las reglas nuevas de `.pp-ranking`.
- Reinicio exclusivo de frontend para invalidar el HMR de Docker/Windows; Vite quedó ready en puerto 5175.
- Prueba API mínima con `juan.perez@playpredict.local`, Liga 1, Partido 32:
  - creación de Pronóstico 37 como `1-0`;
  - actualización a `2-1`;
  - GET posterior confirmó persistencia `2-1`.
- Verificación estática de ambos mensajes diferenciados en las dos pantallas de pronósticos: conservados sin cambios.
- `docker compose exec -T frontend npm run build`: llegó a TypeScript y falló por tres errores preexistentes del WIP actual, fuera de este alcance:
  - `LeagueDetailPage.tsx`: narrowing del fallback de clipboard;
  - `LeagueJoinPage.tsx`: `ApiError.status` inexistente;
  - `LeaguesMinePage.tsx`: import no utilizado.
  El manejador de teclado y el CSS nuevos no agregaron errores de compilación.

## Resultado

- Backend healthy y frontend sirviendo los cambios.
- Ranking con contraste explícito para posición, jugador, puntos, exactos, correctos, evaluados y `(Vos)`.
- Navegación con `Enter` implementada sin submit ni guardado automático; `Tab` permanece intacto.
- Creación, modificación y persistencia de pronóstico verificadas contra el backend real.
- La validación visual y de teclado final queda pendiente de la prueba manual del usuario en su navegador.

## Git status final

Se dejó el árbol sin commit ni push. Cambios propios esperados: los cinco archivos listados arriba. Se preservaron sin tocar los elementos no trackeados ajenos `.qwen/`, `Nuevo Documento de texto.txt` y `docs/test/Test Demo 1 - v2 Login y circuito basico.docx` (este último apareció durante la sesión y no forma parte de los cambios de Codex).
