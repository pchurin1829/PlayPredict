# PlayPredict — patrones ADMIN y equipos por Fecha

Fecha: 2026-08-22
Rama: `prueba-glm-ui`

## 1. Formularios ajustados

Competition, Edition, Round, Match, Liga Oficial, configuración de puntuación, Premios, Experiences y Team.

## 2. Navegación superior

Los retornos simples usan `← Volver a X`. Los breadcrumbs jerárquicos existentes se conservaron.

## 3. Guardar / Cancelar

Guardar queda primero y como acción primaria; Cancelar queda inmediatamente después y vuelve al padre sin persistir. Acciones contextuales como Ver Fechas, Ver Ediciones o Ver Partidos se separan hacia la derecha con estilo terciario.

## 4. Validación frontend por Fecha

MatchForm carga Teams y Matches de la Round. Los Teams usados por otros partidos aparecen deshabilitados y rotulados “ya participa en esta Fecha”. En edición se excluye el partido actual y se conservan disponibles sus equipos originales, incluso ante conflictos históricos.

## 5. Validación backend

Crear y editar Match comprueba HomeTeamId/AwayTeamId contra los demás Matches de la Round. Devuelve errores por campo con Team y Fecha, por ejemplo: `Banfield ya participa en otro partido de FECHA 1.`. Home/Away siguen siendo obligatorios y distintos.

## 6. Conflictos históricos

Se auditaron 13 Rounds y 34 Matches antes de las pruebas. Se encontraron dos Teams repetidos en `COPA ARGENTINA / Edicion 2026 / FECHA 1`: Banfield y Argentinos Juniors. No se borraron ni corrigieron. Los equipos originales quedan grandfathered al editar esos partidos.

## 7. Pruebas

- Banfield vs River en FECHA 1: HTTP 400 por Banfield ocupado.
- Racing vs GELP en FECHA 1: HTTP 400 por GELP ocupado.
- Racing vs River en FECHA 1: HTTP 201, ambos libres.
- Editar Banfield vs GELP sin cambiar equipos: HTTP 200.
- Cambiar su visitante a River, ya usado: HTTP 400.
- Banfield vs River en FECHA 2: HTTP 201; la regla es por Round.
- Builds backend/frontend, TypeScript y `git diff --check`: ver cierre de ejecución.

## 8. Archivos modificados

- `backend/Endpoints/MatchEndpoints.cs`
- formularios ADMIN indicados en la sección 1
- `frontend/src/components/admin.css`
- este informe.

## 9. Estado Git

Sin commit ni push. Servicios levantados. Se preservó el worktree acumulado y no se modificaron conflictos históricos.
