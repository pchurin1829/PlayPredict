# PlayPredict — P0.1 Limpieza de navegación ADMIN

Fecha: 2026-08-22
Rama: `prueba-glm-ui`
Estado: cambios locales, sin commit ni push

## 1. Cambios de navegación

- Se eliminó Ediciones como entrada independiente del sidebar.
- Fixture, Resultados y Configuración dejaron de enviar silenciosamente a Competencias.
- Se agregaron entradas contextuales que explican el modelo y solicitan Competition y Edition antes de continuar.
- Competencias ahora muestra acciones explícitas `Editar` y `Ver Ediciones`.
- Ediciones muestra `Editar`, `Configurar puntuación` y `Ver Fixture / Partidos`.
- Rounds y Matches preservan el contexto de Fixture o Resultados mediante query de navegación, sin cambiar endpoints ni datos.

## 2. Menú ADMIN final

### ADMINISTRACIÓN

- Dashboard.

### FUENTES DEPORTIVAS

- Organizaciones deportivas — PRÓXIMAMENTE.
- Competencias.
- Equipos — PRÓXIMAMENTE.

### OPERACIÓN

- Fixture / Partidos.
- Ligas Oficiales.
- Resultados.

### JUEGO

- Rankings.
- Configuración.

## 3. Perfil y cierre de sesión

El perfil ADMIN está en el extremo superior derecho y utiliza avatar con iniciales, nombre y dropdown. Incluye:

- Administración.
- Vista jugador.
- Mi perfil.
- Cerrar sesión.

Se eliminó Cerrar sesión del pie del sidebar.

## 4. Vista jugador / Volver a Administración

- `Vista jugador` cambia el chrome sin cerrar sesión ni eliminar el rol.
- En PLAYER, el dropdown del perfil muestra `Volver a Administración` para usuarios ADMIN.
- La acción restaura el panel y navega al Dashboard ADMIN.
- El modo continúa persistiendo en `localStorage`.
- Los menús ADMIN y PLAYER nunca se muestran simultáneamente.

## 5. Flujo Competition → Edition → Fixture

1. Competencias.
2. `Ver Ediciones` sobre la Competition.
3. `Ver Fixture / Partidos` sobre la Edition.
4. Lista de Fechas de esa Edition.
5. Selección de Fecha.
6. Partidos con alta/edición y resultado existente.

La entrada `/admin/fixture` ofrece también una selección guiada Competition → Edition.

## 6. Flujo Resultados

La ruta `/admin/results` muestra el título Resultados y explica: “Seleccioná la competencia y edición cuyos resultados querés gestionar.”

Luego:

1. Competition.
2. Edition.
3. Fechas de la Edition en contexto Resultados.
4. Partidos, con acciones Cargar/Corregir resultado.

No se creó otra fuente de resultados; se reutiliza Match y su modal existente.

## 7. Configuración de scoring

La ruta `/admin/scoring` explica que las reglas se configuran por Edition. El flujo es:

1. Competition.
2. Edition.
3. Pantalla existente `EditionScoringConfigurationPage`.

No se modificó ninguna regla ni endpoint.

## 8. Breadcrumbs agregados/mejorados

- Competencias → nombre de Competition → Ediciones.
- Competencias → Ediciones → Fixture/Resultados.
- Competencias → Competition → Edition → Fecha/Resultados en Partidos.
- Las pantallas de scoring y formularios existentes conservan sus enlaces de regreso a Ediciones/Fechas.

## 9. Archivos modificados en P0.1

- `frontend/src/App.tsx`
- `frontend/src/components/Layout.tsx`
- `frontend/src/components/Layout.css`
- `frontend/src/components/admin.css`
- `frontend/src/components/player/PlayerHeader.tsx`
- `frontend/src/components/player/PlayerHeader.css`
- `frontend/src/pages/AdminDashboardPage.tsx`
- `frontend/src/pages/AdminOperationEntryPage.tsx`
- `frontend/src/pages/CompetitionsListPage.tsx`
- `frontend/src/pages/EditionsListPage.tsx`
- `frontend/src/pages/RoundsListPage.tsx`
- `frontend/src/pages/MatchesListPage.tsx`
- `docs/ai/codex/2026-08-22_ADMIN_navigation_cleanup_p01.md`

## 10. Validaciones

- `npx tsc --noEmit`: OK.
- `npm run build`: OK.
- `git diff --check`: OK (sólo avisos informativos LF/CRLF).
- Backend no fue modificado.

## 11. Pendientes

- Organizaciones deportivas y Team siguen sin modelo y permanecen PRÓXIMAMENTE.
- No se realizó la auditoría mobile ADMIN completa; la navegación conserva una adaptación estrecha mínima.
- No se crearon landings globales de Edition ni Match: se mantiene la jerarquía de dominio solicitada.

## 12. Estado Git

- Sin commit ni push.
- El worktree conserva todos los cambios legítimos de las fases anteriores y los archivos locales previamente excluidos.
