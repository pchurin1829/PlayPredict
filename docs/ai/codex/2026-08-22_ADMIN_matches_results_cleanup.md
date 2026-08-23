# PlayPredict — Partidos, resultados, eliminación y logos

Fecha: 2026-08-22
Rama: `prueba-glm-ui`

## 1. Causa del cierre del modal

El backdrop cerraba con `onClick`. Una selección iniciada en el input podía terminar con mouseup/click resuelto sobre el overlay. Ahora sólo cierra con un `mousedown` iniciado explícitamente en el propio backdrop (`target === currentTarget`); mouse down/up/click dentro del contenido detienen propagación.

## 2. Inputs

Marcadores compactos, centrados, `min=0`, `max=99`, `inputMode=numeric`. Focus y click ejecutan `select()`, y el estado normaliza a número entre 0 y 99, evitando `02`.

## 3. Eliminación

El listado ofrece Editar, Eliminar y Cargar/Corregir resultado. Eliminar exige confirmación irreversible. La regla final es conservadora: solamente puede eliminarse un partido sin resultado oficial y sin dependencias. Si está Finalizado o posee goles oficiales, el botón permanece visible pero deshabilitado y muestra “Resultado cargado”. `DELETE /api/matches/{id}` aplica la misma regla y responde HTTP 409 con: “No se puede eliminar este partido porque ya tiene un resultado cargado.”

## 4. Dependencias

Si existen Predictions, la eliminación se bloquea con HTTP 409 e informa cantidades de pronósticos/evaluaciones. No se ejecutan cascadas ni se eliminan resultados, pronósticos o evaluaciones. Un Match sólo puede eliminarse cuando no tiene resultado ni dependencias.

## 5. Paleta ADMIN

Se agregó una primera jerarquía visual reutilizable: header y sidebar usan celeste suave, el workspace un celeste casi neutro y las superficies de formularios/cards conservan blanco. Las cards de Ligas Oficiales usan verde muy claro, borde verde suave y estado Activa con mayor presencia.

Tokens definidos dentro de `.layout`, sin afectar PLAYER: `--admin-nav-bg`, `--admin-workspace-bg`, `--surface-card`, `--official-league-bg`, `--official-league-border`, `--official-league-status-bg`, `--official-league-status-text` y `--user-league-bg`.

## 6. Logos

MatchDto expone `HomeTeamLogoUrl` y `AwayTeamLogoUrl`. Listado y modal usan LogoUrl cuando existe; sin logo muestran placeholder compacto con iniciales. No se descargaron imágenes.

## 7. Modal final

Scoreboard horizontal con ambos Teams, logos/placeholders y marcador central. Se quitaron las etiquetas redundantes “Goles local/visitante”. Botones: Confirmar resultado y Cancelar.

## 8. Validaciones

- DOM real: selección/focus/mouseup mantuvo el modal abierto.
- Inputs reales: valor `1`, rango 0..99 e inputMode numeric.
- Resultado 2-1: Match pasó a Finished.
- Corrección 2-1 → 1-1: resultado actualizado e idempotente.
- Eliminación sin dependencias: HTTP 204 (partido de prueba ID 36).
- Eliminación con 2 Predictions y 2 evaluaciones: HTTP 409 (Match ID 31), preservado.
- Eliminación directa de un Match finalizado: HTTP 409 con el mensaje específico, preservado.
- Verificación visual headless: Ligas Oficiales muestra cards verdes y estado Activa legible; Partidos muestra header/sidebar celestes, workspace más claro, tabla blanca y el bloqueo “Resultado cargado”. Competencias, Fechas y formularios comparten esas mismas superficies/tokens globales.
- Capturas: `%TEMP%/playpredict-admin-delete-theme/official-leagues.png` y `%TEMP%/playpredict-admin-delete-theme/matches.png`.
- Modal real mostró dos placeholders; no había LogoUrl configurado para esos Teams.

## 9. Archivos modificados

- `backend/Dtos/MatchDtos.cs`
- `backend/Endpoints/MatchEndpoints.cs`
- `frontend/src/api/types.ts`
- `frontend/src/pages/MatchesListPage.tsx`
- `frontend/src/components/MatchResultModal.tsx`
- `frontend/src/components/ConfirmModal.css`
- `frontend/src/components/admin.css`
- `frontend/src/components/Layout.css`
- este informe.

## 10. Estado Git

Sin commit ni push. Servicios levantados. El Match de prueba Racing Club vs River Plate (ID 35) permanece Finalizado 2-1 y fue preservado por la nueva regla; el Match descartable de Fecha 2 había sido eliminado durante la prueba segura anterior.
