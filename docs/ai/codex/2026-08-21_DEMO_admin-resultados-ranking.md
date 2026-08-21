# DEMO 1 PLAYER — preparación para grabación

**Fecha:** 2026-08-21
**Rama:** `prueba-glm-ui`
**Checkpoint:** `d699545` (`chore: finalize player demo login visual checkpoint`)
**Commit/push:** no realizados

## Resultado

**DEMO 1 lista para grabar: SÍ.**

Se ejecutó el recorrido completo con Google Chrome real en modo headless, viewport 1366×768 y locale `es-AR`, contra frontend, backend y PostgreSQL locales. La pasada final terminó sin errores de consola ni excepciones de página.

## Flujo probado

1. `/login` y navegación por `Registrate`.
2. Registro de un PLAYER nuevo y entrada automática a Inicio.
3. Apertura de Competencias Oficiales.
4. Participación en `Liga General - Liga Profesional (demo)`.
5. Comprobación de la Liga Oficial en Mis Ligas con badge `OFICIAL`.
6. Carga de tres pronósticos futuros en la Liga Oficial, incluyendo `0-0`.
7. Modificación del primer pronóstico y retorno a `PRONOSTICADO`.
8. Creación normal de una Liga privada por rango de Fechas.
9. Carga de dos pronósticos en la Liga privada.
10. Comprobación simultánea de badges `OFICIAL` y `MI LIGA` en Mis Ligas.
11. Apertura de Resultados históricos de la Liga Oficial: Fecha, equipos, resultado oficial y `Sin pronóstico` para el usuario nuevo.
12. Apertura del Ranking de Liga con cuatro filas históricas reales.
13. Retorno final a Mis Ligas con ambas ligas presentes.

## Caso E2E final

- Usuario: Eugenia Temporal (`UserId 14`).
- Email: `demo1.e2e.20260821.1787319125134@playpredict.test`.
- Contraseña: `Demo1234!`.
- Liga Oficial: `Liga General - Liga Profesional (demo)` (`LeagueId 1`).
- Liga privada temporal: `Los del Trabajo - E2E Temporal` (`LeagueId 7`), Clausura 2026, Fecha 4 → Fecha 5.

Pronósticos Oficiales persistidos:

| Partido | Pronóstico final |
|---|---:|
| Independiente vs Boca Juniors | 2-0 (creado 0-0 y luego modificado) |
| River Plate vs Gimnasia | 1-0 |
| Estudiantes vs Racing Club | 1-2 |

Pronósticos privados persistidos:

| Partido | Pronóstico |
|---|---:|
| Independiente vs Boca Juniors | 0-1 |
| River Plate vs Gimnasia | 1-2 |

## Datos demo disponibles

La Liga Profesional es el mejor recorrido para la filmación:

- Fechas 1, 2 y 3: nueve partidos finalizados con resultados históricos.
- Fechas 4, 5 y 6: trece partidos futuros pronosticables.
- Liga Oficial 1: cuatro participantes históricos, 37 pronósticos y 36 evaluaciones antes de preparar los usuarios temporales.
- Ranking de Liga: cuatro filas históricas con puntos reales.

El PLAYER nuevo no aparece inicialmente en el Ranking porque `RankingService` incluye únicamente usuarios con al menos una evaluación. Esto es coherente con no fabricar puntos. Por ello tampoco aparece `(Vos)` durante esta grabación; el badge sigue implementado y sólo se muestra cuando el usuario autenticado tiene una fila evaluada.

## Bugs encontrados y correcciones

No se reprodujeron bugs funcionales bloqueantes de DEMO 1. No se modificó código de aplicación.

Los primeros intentos del harness temporal requirieron ajustar criterios/selectores de automatización (`networkidle`, texto exacto del dashboard, iconos en nombres accesibles y seguimiento de tarjetas que cambian de estado). Esos ajustes pertenecieron exclusivamente al harness temporal, que fue eliminado al finalizar.

## Pendientes no bloqueantes

- Un PLAYER sin pronósticos evaluados no tiene fila de 0 puntos en el Ranking y, por lo tanto, no muestra `(Vos)`. No se cambió este criterio para evitar alterar reglas ya validadas.
- La Liga privada recomendada abarca sólo Fechas futuras, por lo que Resultados debe mostrarse desde la Liga Oficial.
- Quedaron usuarios y ligas temporales no destructivos creados durante las pasadas E2E. No interfieren con el email ni el nombre final de filmación.

## Datos creados

- Usuarios temporales E2E `UserId 10` a `14`, todos con emails únicos bajo `demo1.e2e.20260821.*@playpredict.test`.
- Participaciones normales de esos usuarios según el avance de cada pasada.
- Ligas privadas temporales `LeagueId 6` y `7`, con sufijo `- E2E Temporal`.
- Cinco pronósticos del caso final `UserId 14`.
- Los tests de regresión existentes crearon sus propios usuarios/datos aislados según su diseño.

No se borraron ni alteraron datos históricos. No se ejecutaron seeders, migraciones ni operaciones masivas.

## Credenciales libres para la filmación

Se comprobó en PostgreSQL que el email y el nombre de Liga siguientes no existen:

- Nombre: `Pablo`
- Apellido: `Demo`
- Email: `demo1@playpredict.test`
- Contraseña: `Demo1234!`
- Liga: `Los del Trabajo`

La cuenta final **no fue registrada**, de modo que el video puede comenzar desde Registro.

## Validaciones

- Chrome E2E completo 1366×768: PASS.
- Errores de consola/página durante pasada final: ninguno.
- Persistencia en PostgreSQL de usuario, rol PLAYER, participaciones, Liga y cinco pronósticos: verificada.
- `npx tsc --noEmit`: OK.
- `npm run build`: OK; 95 módulos transformados.
- `tests/player-prediction-delete.ps1`: PASS.
- `tests/player-official-league-leave-rejoin.ps1`: PASS.
- Backend `/api/health`: `{"status":"ok"}`.
- `git diff --check`: OK.

## Archivos modificados

- `docs/ai/codex/2026-08-21_DEMO_admin-resultados-ranking.md`.

El harness E2E y `playwright-core` fueron temporales y se eliminaron. No hubo cambios en frontend, backend, tests permanentes ni modelo de datos.

## Guion de grabación DEMO 1

### Paso 1 — Login y Registro

1. Abrir `http://localhost:5175/login`.
2. Click en `Registrate`.
3. Completar:
   - Nombre: `Pablo`
   - Apellido: `Demo`
   - Email: `demo1@playpredict.test`
   - Contraseña: `Demo1234!`
   - Repetir contraseña: `Demo1234!`
4. Click en `Crear cuenta`.
5. Mostrar brevemente el Inicio PLAYER y el mensaje de bienvenida. No abrir bloques `PRÓXIMAMENTE`.

### Paso 2 — Competencia y Liga Oficial

1. Click en `Competencias Oficiales` en el menú izquierdo.
2. En la tarjeta `Liga Profesional` / `Clausura 2026`, mostrar `Todavía no participás`.
3. Click en `Participar`.
4. Esperar que cambie a `✓ Estás participando` y aparezca `Ver`.
5. Click en `Mis Ligas`.
6. Mostrar `Liga General - Liga Profesional (demo)` con badge `OFICIAL`.
7. Click en `Entrar`.

### Paso 3 — Pronósticos Oficiales

1. Click en la pestaña `Pronósticos`.
2. Si aparece el filtro, elegir `Fecha 4` para concentrar la pantalla.
3. En `Independiente vs Boca Juniors`:
   - Local `0` → Enter.
   - Visitante `0` → Enter.
   - Click/Enter en `Guardar pronóstico`.
   - Mostrar estado `PRONOSTICADO`.
   - Cambiar Local a `2`.
   - Mostrar `Guardar cambios`, guardarlo y volver a `PRONOSTICADO`.
4. En `River Plate vs Gimnasia`: ingresar `1-0` y guardar.
5. En `Estudiantes vs Racing Club`: ingresar `1-2` y guardar.
6. No eliminar estos pronósticos durante la grabación principal.

### Paso 4 — Crear Liga con amigos

1. Click en `Competencias Oficiales`.
2. En la tarjeta `Liga Profesional`, click en `+ Crear Liga con amigos`.
3. Completar:
   - Nombre: `Los del Trabajo`
   - Descripción opcional: `La Liga de la oficina para jugar el Clausura`.
4. Click en `Rango de Fechas`.
5. Torneo/Edición: `Clausura 2026`.
6. Fecha inicial: `Fecha 4`.
7. Fecha final: `Fecha 5`.
8. Click en `Crear Liga`.
9. Mostrar brevemente Resumen y el código de invitación, sin abrir Premios.

### Paso 5 — Pronósticos en Los del Trabajo

1. Click en `Pronósticos`.
2. En `Independiente vs Boca Juniors`: ingresar `0-1` y guardar.
3. En `River Plate vs Gimnasia`: ingresar `1-2` y guardar.
4. Mostrar ambos estados `PRONOSTICADO`.

### Paso 6 — Resultados históricos

1. Click en `Mis Ligas`.
2. En `Liga General - Liga Profesional (demo)`, click en `Entrar`.
3. Click en `Resultados`.
4. Elegir `Fecha 1` en el filtro.
5. Mostrar el encabezado con Fecha/rango calendario y las tarjetas con escudos, equipos y resultado.
6. Señalar `Mi pronóstico: Sin pronóstico`; es el comportamiento honesto esperado para el usuario recién creado.

### Paso 7 — Ranking

1. Sin salir de la Liga Oficial, click en `Ranking`.
2. Mostrar `Ranking de la Liga` y las cuatro filas históricas con puntos, exactos, correctos y evaluados.
3. Explicar verbalmente: “Pablo todavía no suma puntos porque sus pronósticos son para partidos futuros”. No afirmar que aparece en la tabla.

### Paso 8 — Cierre en Mis Ligas

1. Click en `Mis Ligas`.
2. Mostrar juntas:
   - `Liga General - Liga Profesional (demo)` con badge `OFICIAL`.
   - `Los del Trabajo` con badge `MI LIGA`.
3. Finalizar la grabación en esta pantalla.

## Git status esperado

Sin commit ni push. Único archivo versionable nuevo: este informe. Se preservan los untracked locales ajenos `.qwen/` y `Nuevo Documento de texto.txt`.
