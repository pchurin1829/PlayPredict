# PlayPredict --- Especificación de importación XLS con UPSERT

**Documento para implementación en Codex**\
**Versión:** 1.0\
**Fecha:** 2026-08-28

## 1. Objetivo

Implementar en PlayPredict un mecanismo simple, seguro y reutilizable
para importar desde un archivo XLS/XLSX:

1.  Planteles de clubes.
2.  Partidos de una competencia.

La importación NO debe ser una carga ciega ni debe borrar previamente la
información existente.

Debe funcionar con lógica **UPSERT**:

-   Si el registro no existe, **CREARLO**.
-   Si el registro ya existe y cambió información importable,
    **ACTUALIZARLO**.
-   Si existe y no cambió, **NO MODIFICARLO**.
-   La ausencia de un registro en el XLS **NO implica eliminarlo** de
    PlayPredict.

El objetivo es que el mismo mecanismo pueda utilizarse para la carga
inicial y posteriormente para actualizaciones periódicas.

------------------------------------------------------------------------

## 2. Principio general

El importador debe ser:

-   Idempotente: importar dos veces el mismo archivo no debe generar
    duplicados.
-   Transaccional: ante un error grave no debe quedar una carga parcial
    inconsistente.
-   Validado antes de confirmar.
-   Tolerante a diferencias irrelevantes de mayúsculas/minúsculas y
    espacios.
-   Auditable: debe informar qué creó, actualizó, ignoró y rechazó.
-   Independiente del orden de las filas del XLS.

No utilizar el nombre visible como identificador permanente cuando ya
exista un ID interno en PlayPredict. El XLS utiliza nombres para
facilitar la operación humana, pero el importador debe resolverlos
contra las entidades internas.

------------------------------------------------------------------------

# 3. Importación de planteles

## 3.1 Hoja

Nombre esperado:

`IMPORTAR_PLANTELES`

## 3.2 Columnas

  Columna                       Obligatoria   Descripción
  ----------------------------- ------------- ----------------------
  NOMBRE DEL CLUB               Sí            Nombre del club
  NOMBRE APELLIDO DEL JUGADOR   Sí            Nombre completo
  POSICION                      Sí            Posición normalizada

Valores permitidos para `POSICION`:

-   `ARQUERO`
-   `DEFENSOR`
-   `MEDIOCAMPISTA`
-   `DELANTERO`

No aceptar silenciosamente otras posiciones.

## 3.3 Normalización

Antes de comparar:

-   Trim de espacios iniciales/finales.
-   Colapsar espacios múltiples.
-   Comparación case-insensitive.
-   Mantener el texto correctamente escrito para visualización.
-   No eliminar acentos del valor almacenado.

Ejemplo:

`River   Plate` debe poder resolver `River Plate`.

## 3.4 Resolución del club

El importador debe buscar primero el club existente.

Si encuentra exactamente un club compatible: - utilizar su ID interno.

Si no encuentra ninguno: - en esta primera versión, informar error y NO
crear automáticamente un club sin confirmación.

Si encuentra más de una coincidencia: - marcar fila como ambigua y no
importarla.

Esto evita crear por error variantes como `Estudiantes`,
`Estudiantes LP`, etc.

## 3.5 UPSERT de jugador

Para la primera versión, identificar al jugador dentro del contexto del
club mediante su nombre completo normalizado.

### Si no existe

Crear jugador y asociarlo al club con la posición indicada.

Resultado: `CREADO`.

### Si existe

Comparar los campos administrados por esta importación.

Si cambió `POSICION`: - actualizarla.

Resultado: `ACTUALIZADO`.

Si no cambió: - no ejecutar una actualización innecesaria.

Resultado: `SIN_CAMBIOS`.

### Si aparece repetido dentro del mismo XLS

No crear duplicados.

Reportar:

`DUPLICADO_EN_ARCHIVO`.

## 3.6 Jugadores que desaparecen del XLS

IMPORTANTE:

La ausencia de un jugador en una nueva planilla **NO debe provocar su
eliminación automática**.

Ejemplo:

Base actual: - Jugador A - Jugador B - Jugador C

Nueva planilla: - Jugador A - Jugador C

El importador NO debe asumir que Jugador B dejó el club.

En esta versión debe mantenerlo y, opcionalmente, informar:

`EXISTE_EN_BASE_NO_INCLUIDO_EN_ARCHIVO`.

La baja, transferencia o cambio de club requiere un mecanismo explícito
posterior.

------------------------------------------------------------------------

# 4. Importación de partidos

## 4.1 Hoja

Nombre esperado:

`IMPORTAR_PARTIDOS`

## 4.2 Columnas

  Columna     Obligatoria   Ejemplo
  ----------- ------------- ------------------------------------
  TORNEO      Sí            TORNEO CLAUSURA MERCADO LIBRE 2026
  EDICION     Sí            CLAUSURA_2026
  FECHA_NRO   Sí            7
  FECHA       Sí            2026-08-28
  HORA        Sí            21:30
  LOCAL       Sí            Boca Juniors
  VISITANTE   Sí            Lanús
  ESTADO      Sí            SCHEDULED

Estados inicialmente permitidos:

-   `SCHEDULED`
-   `IN_PROGRESS`
-   `FINISHED`
-   `CANCELLED`

El importador debe respetar los estados que realmente soporte
actualmente el modelo de PlayPredict. No agregar nuevos estados sólo por
esta especificación si el dominio vigente utiliza otra enumeración.

------------------------------------------------------------------------

# 5. Resolución de competencia y edición

`TORNEO` y `EDICION` deben utilizarse para localizar la
competencia/edición existente.

No crear automáticamente una competencia nueva por un error de
escritura.

Si no puede resolverse de manera inequívoca:

`COMPETENCIA_NO_ENCONTRADA`

o

`EDICION_NO_ENCONTRADA`.

------------------------------------------------------------------------

# 6. Resolución de equipos

`LOCAL` y `VISITANTE` deben resolverse contra clubes/equipos existentes
y transformarse a IDs internos.

Si alguno no existe:

-   no crear el partido;
-   informar claramente qué equipo no pudo resolverse.

Nunca crear automáticamente un segundo club por una diferencia menor de
escritura.

------------------------------------------------------------------------

# 7. UPSERT de partidos

La identidad lógica propuesta para esta primera versión es:

`EDICION + FECHA_NRO + LOCAL_ID + VISITANTE_ID`

### Si no existe

Crear partido con:

-   edición
-   número de fecha
-   fecha
-   hora
-   local
-   visitante
-   estado

Resultado:

`CREADO`.

### Si existe

Actualizar únicamente los campos administrados por la importación que
hayan cambiado, por ejemplo:

-   FECHA
-   HORA
-   ESTADO

Resultado:

`ACTUALIZADO`.

### Si no cambió

Resultado:

`SIN_CAMBIOS`.

Importar dos veces el mismo fixture debe producir cero partidos
duplicados.

------------------------------------------------------------------------

# 8. Atención especial: partidos ya jugados

No sobrescribir información deportiva sensible sin control.

Si un partido está `FINISHED` y ya posee resultado/puntajes, una nueva
importación NO debe poder convertirlo silenciosamente en `SCHEDULED`,
cambiar los equipos ni borrar el resultado.

En esos casos:

-   marcar conflicto;
-   no aplicar automáticamente el cambio;
-   requerir intervención administrativa.

Resultado sugerido:

`CONFLICTO_PARTIDO_FINALIZADO`.

------------------------------------------------------------------------

# 9. Flujo de interfaz recomendado

La operación debería tener dos pasos.

## Paso 1 --- Analizar archivo

Botón:

`IMPORTAR XLS`

El usuario selecciona el archivo.

PlayPredict lo procesa pero todavía NO modifica la base.

Mostrar resumen previo:

### Planteles

-   Filas leídas
-   Jugadores nuevos
-   Jugadores a actualizar
-   Sin cambios
-   Duplicados
-   Errores
-   Clubes no encontrados

### Partidos

-   Filas leídas
-   Partidos nuevos
-   Partidos a actualizar
-   Sin cambios
-   Conflictos
-   Errores

Debe existir una tabla de detalle para revisar problemas.

## Paso 2 --- Confirmar

Sólo si la validación es aceptable:

`CONFIRMAR IMPORTACIÓN`

Entonces ejecutar la transacción.

También:

`CANCELAR`

sin modificar la base.

------------------------------------------------------------------------

# 10. Resultado de la importación

Al finalizar mostrar algo similar a:

## Planteles

-   30 clubes procesados
-   1.045 jugadores analizados
-   37 creados
-   12 actualizados
-   991 sin cambios
-   3 duplicados
-   2 errores

## Partidos

-   15 analizados
-   4 creados
-   2 actualizados
-   9 sin cambios
-   0 errores

Los números anteriores son únicamente ilustrativos.

------------------------------------------------------------------------

# 11. Registro de importaciones

Conviene persistir un historial mínimo:

-   ID de importación
-   fecha/hora
-   usuario administrador
-   nombre del archivo
-   tipo de importación
-   cantidad de filas
-   creados
-   actualizados
-   sin cambios
-   rechazados/errores
-   resultado general

Opcional pero recomendable:

-   hash SHA-256 del archivo.

Esto permitirá saber si exactamente el mismo archivo ya fue procesado.

------------------------------------------------------------------------

# 12. Manejo de errores

Una fila incorrecta no debe producir excepciones genéricas
incomprensibles.

Ejemplos de mensajes:

-   `Fila 37: club "River Platee" no encontrado.`
-   `Fila 81: posición "VOLANTE" inválida. Valores permitidos: ARQUERO, DEFENSOR, MEDIOCAMPISTA, DELANTERO.`
-   `Fila 12 de partidos: visitante "Lanus FC" no pudo resolverse.`
-   `Fila 14: FECHA inválida. Se espera YYYY-MM-DD.`

El administrador debe poder corregir el XLS y volver a importarlo.

------------------------------------------------------------------------

# 13. Transacciones

Separar conceptualmente:

1.  Parseo.
2.  Validación.
3.  Preview.
4.  Confirmación.
5.  Persistencia.

No modificar la base durante el preview.

Al confirmar, utilizar una transacción adecuada para evitar estados
parciales.

------------------------------------------------------------------------

# 14. Seguridad

Sólo usuarios con permisos administrativos adecuados deben poder
ejecutar importaciones.

El backend debe validar nuevamente:

-   formato;
-   permisos;
-   entidades;
-   estados;
-   reglas de dominio.

Nunca confiar exclusivamente en validaciones del frontend.

------------------------------------------------------------------------

# 15. Arquitectura recomendada

Evitar implementar toda la lógica dentro del controller/API endpoint.

Separar responsabilidades, adaptándolas a la arquitectura actual de
PlayPredict:

-   lector/parser XLSX;
-   normalizador;
-   validador;
-   resolver de entidades;
-   servicio de preview;
-   servicio de importación;
-   DTO de resultado;
-   persistencia de auditoría.

La implementación debe respetar la arquitectura, entidades, nomenclatura
y patrones ya existentes en el repositorio. Codex debe auditar el código
actual antes de crear nuevas abstracciones.

------------------------------------------------------------------------

# 16. Pruebas mínimas obligatorias

Implementar tests para, al menos:

1.  Archivo válido de planteles.
2.  Jugador nuevo.
3.  Jugador existente sin cambios.
4.  Cambio de posición.
5.  Posición inválida.
6.  Club inexistente.
7.  Jugador duplicado en XLS.
8.  Importar dos veces el mismo archivo no duplica jugadores.
9.  Partido nuevo.
10. Partido existente con cambio de horario.
11. Partido existente sin cambios.
12. Equipo inexistente.
13. Fecha/hora inválida.
14. Partido duplicado dentro del XLS.
15. Segunda importación no duplica partidos.
16. Partido finalizado no se sobrescribe indebidamente.
17. Preview no modifica la base.
18. Error durante confirmación no deja una importación parcial
    inconsistente.

------------------------------------------------------------------------

# 17. Compatibilidad con evolución futura

Diseñar el mecanismo de manera que posteriormente pueda recibir la misma
información desde:

-   XLS/XLSX;
-   CSV;
-   API externa;
-   proveedor oficial de datos;
-   agente de IA.

La lógica de negocio de UPSERT y validación no debe depender
directamente del lector XLS.

La IA futura debería reemplazar o complementar la obtención/preparación
de datos, no las reglas de integridad del importador.

------------------------------------------------------------------------

# 18. Fuera de alcance de esta primera versión

No implementar todavía, salvo que ya exista infraestructura compatible:

-   scraping automático;
-   IA para buscar planteles;
-   transferencias automáticas entre clubes;
-   eliminación automática de jugadores ausentes;
-   creación automática de clubes desconocidos;
-   conciliación probabilística de jugadores homónimos;
-   actualización automática desde Internet.

------------------------------------------------------------------------

# 19. Criterios de aceptación

La tarea se considera terminada cuando:

1.  PlayPredict acepta el XLSX acordado.
2.  Puede mostrar un preview antes de modificar datos.
3.  Puede importar planteles.
4.  Puede importar partidos.
5.  Reimportar el mismo archivo no genera duplicados.
6.  Los registros existentes pueden actualizarse mediante UPSERT.
7.  Los registros ausentes del XLS no se eliminan.
8.  Los errores se informan por fila.
9.  Los partidos finalizados están protegidos contra sobrescrituras
    peligrosas.
10. Existe cobertura automática de los escenarios críticos.
11. La compilación y tests existentes continúan pasando.
12. No se rompe el flujo actual de carga manual de PlayPredict.

------------------------------------------------------------------------

# 20. Instrucción de trabajo para Codex

Antes de programar:

1.  Auditar las entidades actuales de
    Club/Team/Player/Competition/Edition/Match y sus relaciones.
2.  Auditar los endpoints y servicios existentes.
3.  Verificar exactamente cómo se representan actualmente `MatchStatus`,
    posiciones y participantes.
4.  Comparar esta especificación contra el modelo real.
5.  Informar cualquier incompatibilidad antes de modificar el dominio.
6.  Reutilizar servicios y reglas existentes cuando corresponda.
7.  No crear migraciones ni modificar entidades innecesariamente.
8.  Implementar primero backend + tests.
9.  Luego implementar el flujo de preview/confirmación en frontend.
10. Entregar informe final con archivos modificados, decisiones tomadas,
    tests ejecutados y cualquier limitación pendiente.

**No hacer commit ni push hasta recibir autorización.**
