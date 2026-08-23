# PlayPredict — simplificación y generación de Fechas

Fecha: 2026-08-22
Rama: `prueba-glm-ui`

## 1. Uso real de StartDate/EndDate de Round

No participan en cierre de pronósticos, scoring, alcance de League ni resultados. Sólo aparecen en el seeder de desarrollo para organizar/refrescar datos demo. Se mantienen en DB y contratos por compatibilidad, sin migración ni pérdida de valores, pero se ocultaron del formulario ADMIN. La fecha/hora operativa continúa en cada Match.

## 2. Cambios de UI

- Nueva/Editar Fecha muestra solamente Nombre y Orden.
- Fixture de una Edition muestra su nombre, cantidad actual, bloque destacado “Generar Fechas” y creación manual.
- Cada Fecha expone “Ver Partidos” y “Editar”.
- La cantidad ya no se edita indirectamente desde el formulario general de Edition.

## 3. Generación masiva

Nuevo endpoint ADMIN `POST /api/editions/{editionId}/rounds/generate`, con el total deseado. Genera `Fecha N`, `Order N`, Id propio y EditionId correspondiente.

## 4. Comportamiento incremental

La generación consulta órdenes existentes y crea sólo los faltantes hasta alcanzar el total. Repetir el mismo total devuelve cero creadas y no duplica registros.

## 5. Reducción

Solicitar menos que la cantidad existente no elimina ni modifica Fechas o Partidos. Devuelve: “La edición ya tiene X fechas. Reducir la cantidad no elimina fechas existentes.”

## 6. Creación manual

Se conserva `+ Nueva Fecha` y el formulario Nombre/Orden para jornadas especiales.

## 7. Selector de Teams

Nuevo/Editar Partido conserva selects de Team real, validación local/visitante distintos, fecha/hora requerida y Estado.

## 8. Logos

El select nativo permanece estable y textual. Debajo se muestra el Team seleccionado con `LogoUrl`; cuando falta, aparece un placeholder con iniciales. No se descargaron assets.

## 9. Ligas Oficiales

No se modificó League. Los conteos continúan derivados en tiempo real desde Edition/Rounds/Matches y respetan FullCompetition o RoundRange. Verificación sobre Edition 1: 5 fechas y 15 partidos en sus Ligas Oficiales.

## 10. Archivos modificados

- `backend/Dtos/RoundDtos.cs`
- `backend/Endpoints/RoundEndpoints.cs`
- `backend/Dtos/EditionDtos.cs`
- `backend/Endpoints/EditionEndpoints.cs`
- `frontend/src/pages/RoundFormPage.tsx`
- `frontend/src/pages/RoundsListPage.tsx`
- `frontend/src/pages/EditionFormPage.tsx`
- `frontend/src/pages/MatchFormPage.tsx`
- `frontend/src/components/admin.css`
- este informe.

## 11. Tests

- Generar nuevamente total 5 sobre Edition con 5: 0 creadas, dos ejecuciones consecutivas.
- Solicitar total 3 sobre Edition con 5: 0 eliminadas; total final 5 y mensaje de protección correcto.
- Catálogo Team y Match ya validados en el bloque anterior; no se crearon partidos artificiales.
- Corrección UX posterior: sobre Copa Argentina / Edición 2026, con Fecha 1 existente, el formulario determinó `Fecha 2 / orden 2`; se creó esa jornada lógica y la siguiente sugerencia quedó en `Fecha 3 / orden 3`.
- Repetir manualmente orden 1 devolvió HTTP 400 con la Fecha ocupante y próximo orden 3. No se crearon partidos ni resultados durante esta prueba.

## 12. Estado Git

Sin commit ni push. No hubo migraciones ni cambios manuales de datos. Se preserva el worktree legítimo acumulado y los archivos locales ajenos ya identificados.
