# Plantilla oficial — Importar Fixture XLSX

Archivo: `PlayPredict_Plantilla_Importar_Fixture.xlsx`
Hoja: `IMPORTAR_PARTIDOS` (única hoja leída para este flujo).

Corresponde exactamente al contrato de `backend/Imports/SpreadsheetReader.cs`
(`MatchHeaders`). No inventa columnas.

## Columnas (encabezados exactos, orden de la plantilla)

| Columna | Obligatoria | Formato aceptado por el reader |
|---|---|---|
| `FECHA_NRO` | Sí | Entero ≥ 1 (número Excel o texto entero). |
| `FECHA` | Sí | Fecha Excel nativa o texto `YYYY-MM-DD`. Otros formatos (p. ej. `DD/MM/YYYY`) se rechazan con `INVALID_DATE`. |
| `HORA` | Sí | Hora Excel nativa o texto `HH:mm` / `HH:mm:ss`. Otros valores se rechazan con `INVALID_TIME`. |
| `LOCAL` | Sí | Nombre del equipo. Debe existir en la base (match por nombre normalizado). |
| `VISITANTE` | Sí | Nombre del equipo. Debe existir en la base. Distinto de `LOCAL`. |
| `ESTADO` | Sí | Uno de `SCHEDULED`, `IN_PROGRESS`, `SUSPENDED`, `CANCELLED`. `FINISHED` se rechaza (`INVALID_STATUS`); los resultados se cargan por el flujo de resultados, no por esta importación. |

Encabezados opcionales tolerados por el reader (no incluidos en la plantilla):
`TORNEO`, `EDICION`, `ZONA`, `FUENTE`. Cualquier otro encabezado genera
`UNKNOWN_HEADER`. Los encabezados se detectan en las primeras 10 filas, por lo
que la hoja admite filas de título previas, pero la plantilla trae el encabezado
en la fila 1 para simplicidad.

## Filas de ejemplo

Las 3 filas son datos ficticios (`(FICTICIO)` en los nombres) y solo sirven para
mostrar el formato. No corresponden a la Base Inicial ni a ningún fixture real.
Los equipos de ejemplo no existen en la base, por lo que el preview los marcará
como `UnresolvedTeamError` hasta reemplazarlos por nombres reales.

## Flujo preview → confirm (implementación actual)

1. ADMIN abre `/admin/fixture/import`, elige la Edición destino y el archivo
   `.xls` / `.xlsx` (máx. 10 MB).
2. **Preview**: el backend lee la hoja, valida estructura y clasifica cada fila
   sin modificar la base (crear / actualizar / sin cambios / error / conflicto).
   Nada se aplica hasta la confirmación explícita.
3. **Confirm**: reenvía el archivo original. El backend verifica que el SHA-256
   coincida con el analizado, revalida todo y aplica en una transacción atómica.
   Si algo falla, no se aplica nada.

## Reglas de UPSERT (según el código existente)

- Clave de coincidencia: Edición + `FECHA_NRO` + `LOCAL` + `VISITANTE`
  (nombres normalizados, insensible a mayúsculas/espacios).
- Fila idéntica a un partido existente → sin cambios.
- Misma clave con distinto horario (`FECHA`+`HORA` en hora Argentina → UTC) o
  distinto `ESTADO` → actualiza solo `StartsAtUtc` y `Status`.
- Clave inexistente → crea el partido. Si la Fecha (`FECHA_NRO`) no existe en la
  Edición, la crea una sola vez como `Fecha {N}`.
- Conflictos que bloquean la confirmación: equipo inexistente o ambiguo, mismo
  equipo de local y visitante, duplicados dentro del archivo, partido ya
  `FINISHED` (se corrige por resultados), errores estructurales.
- Los equipos no se crean desde esta plantilla; deben existir previamente.
