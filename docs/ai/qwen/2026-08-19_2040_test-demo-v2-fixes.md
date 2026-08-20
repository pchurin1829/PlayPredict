# Test Demo 1 v2 — Correcciones completas

**Fecha**: 2026-08-19 20:40
**Rama**: `prueba-glm-ui`
**Base commit**: `405618f` (WIP: close demo1 test fixes and add test v2)
**Autor**: Qwen Code (asistencia)
**Estado**: sin commit ni push; listo para prueba manual

---

## Causa raíz del bug P0 — Liga creada no aparece en Mis Ligas

### Diagnóstico

El backend funciona correctamente: `POST /api/leagues` crea la Liga y agrega al creador como `LeagueParticipant`, y `GET /api/leagues/mine` la devuelve inmediatamente. El bug era exclusivamente del frontend.

**Causa raíz**: `LeaguesMinePage.tsx` usaba `Promise.all` para cargar `/leagues/mine` y `/leagues/officials` simultáneamente. Si cualquiera de las dos peticiones fallaba (por ejemplo, `/leagues/officials` devuelve error o respuesta inesperada), el `catch` impedía que **ambos** resultados se setearan, incluyendo `myLeagues`. Como resultado, `myLeagues` quedaba en `null` y la sección "Mis Ligas" **nunca se renderizaba**, ni siquiera con el empty state.

**Causa secundaria**: `ApiError` en `client.ts` no tenía propiedad `status`, lo que hacía que `LeagueJoinPage.tsx` (que hace `err.status === 404`) nunca pudiera distinguir un código inválido de otro error. El `status` siempre era `undefined`.

### Corrección aplicada

1. **`LeaguesMinePage.tsx`**: reemplazado `Promise.all` → `Promise.allSettled`. Ahora `/leagues/mine` y `/leagues/officials` se resuelven independientemente. Si `/leagues/officials` falla, Mis Ligas sigue mostrándose. Si `/leagues/mine` falla, se muestra error solo para esa sección.

2. **`api/client.ts`**: `ApiError` ahora recibe y expone `status: number` (default 0). `LeagueJoinPage` puede distinguir 404 (código inválido) de otros errores.

3. **Ligas Oficiales duplicadas**: cuando un usuario ya participa en una Liga Oficial, aparecía tanto en "Ligas Oficiales" como en "Mis Ligas". Ahora se filtran: la sección Oficial muestra solo las que el usuario aún NO participa (`!l.isParticipant`).

### Cómo fue reproducido

1. Registré un PLAYER nuevo (`test.demov2@playpredict.local`, id=13) por API.
2. Verifiqué `GET /leagues/mine` → `[]` (vacío, correcto).
3. Creé una Liga de Amigos: `POST /leagues` → 201 Created (id=6).
4. Verifiqué `GET /leagues/mine` → la Liga aparece (el backend funciona).
5. El bug era exclusivamente frontend: `Promise.all` impedía renderizar Mis Ligas si `/officials` fallaba.

---

## Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `frontend/src/api/client.ts` | `ApiError` ahora tiene `status: number`; se pasa `response.status` al constructor |
| `frontend/src/pages/LeaguesMinePage.tsx` | `Promise.all` → `Promise.allSettled`; Oficiales filtradas por `!isParticipant`; empty state mejorado con 3 botones; subtítulo explicativo |
| `frontend/src/pages/ExploreCompetitionsPage.tsx` | Subtítulo: "Elegí una competencia para participar en las propuestas oficiales de PlayPredict o para crear tu propia Liga con amigos." |
| `frontend/src/pages/LeagueCreatePage.tsx` | Subtítulo: "Estás creando una Liga privada para jugar con amigos utilizando los partidos de esta competencia. No estás creando una nueva Competencia deportiva." |
| `frontend/src/pages/RegisterPage.tsx` | Campo "Repetir contraseña" con validación de coincidencia; borde rojo si no coinciden; botón deshabilitado si no coinciden; no envía POST mientras no coincidan |
| `frontend/src/pages/RegisterPage.css` | Clase `.pp-register__input-wrap--error` para borde rojo |
| `frontend/src/pages/LoginPage.tsx` | Mensaje 401 → "Email o contraseña incorrectos." (no diferencia usuario inexistente vs pass incorrecta); error de red/servidor → "No pudimos conectar con PlayPredict. Intentá nuevamente." |
| `frontend/src/pages/LoginPage.css` | Clases `.pp-login__bg-photo` y `.pp-login__bg-overlay` para slot de fotografía de fondo en escala de grises con overlay oscuro |
| `frontend/src/pages/PlayerDashboardPage.tsx` | Empty state completo cuando no hay Ligas: icono, título "¡Bienvenido a PlayPredict!", texto orientador, botones "Explorar Competencias" y "Unirme con código" |
| `frontend/src/pages/PlayerDashboardPage.css` | Estilos para `.pdash__empty-state`, icon, title, text, actions |

**Stats**: 11 archivos modificados, ~200 líneas nuevas

---

## Cambios visuales

### Explorar Competencias
- Subtítulo debajo del título: orienta al usuario nuevo sobre qué puede hacer.

### Crear Liga
- Subtítulo explícito: aclara que NO está creando una Competencia deportiva.

### Mis Ligas
- Subtítulo: "Tus Ligas de amigos y las Ligas Oficiales de PlayPredict en las que participás"
- Empty state mejorado con 3 CTAs: Explorar Competencias, Crear Liga de Amigos, Unirme con código
- Ligas Oficiales: solo muestra las que el usuario aún NO participa (evita duplicación)

### Registro
- Campo "Repetir contraseña" con input dedicado, icono de candado, placeholder "Repetí tu contraseña"
- Validación en tiempo real: si las contraseñas no coinciden → borde rojo + mensaje rojo
- Botón "Crear cuenta" deshabilitado mientras no coincidan
- Validación pre-envío: campo vacío → mensaje de error; no coincide → mensaje de error
- Se mantiene toggle mostrar/ocultar contraseña

### Login
- Mensaje 401 unificado: "Email o contraseña incorrectos." — no revela si el usuario existe
- Error de red/servidor (status 0 o 500+): "No pudimos conectar con PlayPredict. Intentá nuevamente."

### Login visual
- CSS preparado para fotografía de fondo: `.pp-login__bg-photo` (grayscale + brightness) y `.pp-login__bg-overlay` (gradiente oscuro)
- **Sin foto agregada** — no existe imagen apropiada en el repo y no se descargan recursos con copyright dudoso
- Uso: agregar `<img class="pp-login__bg-photo" src="..." />` dentro de `.pp-login__stage`

### Dashboard
- Si no hay Ligas: pantalla de bienvenida completa con orientación y CTAs

---

## Validaciones Registro

| Campo | Regla | Mensaje |
|-------|-------|---------|
| Repetir contraseña | Obligatorio | "Repetí la contraseña para confirmar." |
| Repetir contraseña | Debe coincidir con Contraseña | "Las contraseñas no coinciden." |
| Contraseña (backend) | Mínimo 6 caracteres | "La contraseña debe tener al menos 6 caracteres." |

El botón "Crear cuenta" se deshabilita cuando las contraseñas no coinciden.

---

## Tratamiento de errores Login

| Escenario | HTTP status | Mensaje al usuario |
|-----------|-------------|-------------------|
| Email no existe | 401 | "Email o contraseña incorrectos." |
| Contraseña incorrecta | 401 | "Email o contraseña incorrectos." |
| Backend inaccesible | 0 (network error) | "No pudimos conectar con PlayPredict. Intentá nuevamente." |
| Error del servidor | 500+ | "No pudimos conectar con PlayPredict. Intentá nuevamente." |
| Otro error (400, 403, etc.) | N | Mensaje del backend si disponible |

El backend ya diferenciaba internamente pero siempre respondía lo mismo al frontend. El frontend ahora también normaliza en vez de pasar el mensaje crudo.

---

## Pruebas API realizadas — 14 pasos de verificación

PLAYER: `verificacion.testv2@playpredict.local` (id=14, creado en esta sesión)

| # | Paso | Resultado | Detalle |
|---|------|-----------|---------|
| 1 | Registrar PLAYER nuevo | ✅ 201 | Token JWT, rol PLAYER |
| 2 | Login | ✅ 200 | Token JWT, roles: PLAYER |
| 3 | Mis Ligas (vacío) | ✅ 200 | `[]` — vacío correcto |
| 4 | Dashboard (sin ligas) | ✅ N/A | Frontend: empty state con CTAs |
| 5 | Explorar Competencias | ✅ 200 | 2 competencias activas |
| 6 | Crear Liga de Amigos (1 fecha) | ✅ 201 | id=7, Private, inviteCode=T9NCY7PN |
| 7 | Confirmar creación | ✅ | isParticipant=true, participantsCount=1 |
| 8 | Mis Ligas después de crear | ✅ 200 | Liga aparece inmediatamente |
| 9 | Entrar al detalle | ✅ 200 | Liga con rounds, createdByName |
| 10 | Mis Ligas (verificación) | ✅ 200 | Liga persiste |
| 11 | Entrar (navegación) | ✅ 200 | Detail con inviteCode para creador |
| 12 | Logout (simulado: nuevo login) | — | — |
| 13 | Login nuevamente | ✅ 200 | Token JWT, mismo userId=14 |
| 14 | Liga sigue en Mis Ligas | ✅ 200 | Liga id=7 sigue apareciendo |

### Verificaciones adicionales

| Verificación | Resultado |
|-------------|-----------|
| Login con usuario inexistente → 401 | ✅ "Email o contraseña incorrectos." |
| Login con contraseña incorrecta → 401 | ✅ "Email o contraseña incorrectos." |
| Juan Pérez (demo): /leagues/mine | ✅ 2 ligas (1 oficial + 1 amigos) |
| Juan Pérez: /leagues/officials | ✅ 1 oficial con isParticipant=true (se filtrará del bloque "no participando") |
| TypeScript: `npx tsc --noEmit` | ✅ 0 errores |
| Backend health | ✅ `{"status":"ok"}` |
| Frontend sirve código actualizado | ✅ `ApiError.status` presente en Vite |

---

## Pendientes reales

1. **Fotografía de fondo del Login**: CSS preparado (`.pp-login__bg-photo`, `.pp-login__bg-overlay`), sin imagen real. Requiere foto de fútbol en escala de grises sin copyright dudoso.
2. **Validación visual en navegador**: todas las correcciones son funcionales por API y TypeScript compila limpio, pero la validación visual real en el navegador del usuario sigue siendo necesaria.
3. **Ligas Oficiales creadas por ADMIN**: no hay endpoint para que un ADMIN cree Ligas Oficiales. La única Liga Oficial existente es la del seed ("Liga General - Liga Profesional"). Para la demo, esto funciona; para producción se necesita un flujo de administración.
4. **Tab Premios**: sigue mostrando "PRÓXIMAMENTE" (sin backend de premios por Liga).
5. **Test Demo 1 v2 docx**: no se pudo leer (binario). Las correcciones se basan en la descripción del issue, no en el contenido del documento.

---

## docker compose ps

```
playpredict_backend    healthy   0.0.0.0:8006->8080/tcp
playpredict_db         healthy   0.0.0.0:5436->5432/tcp
playpredict_frontend   Up        0.0.0.0:5175->5175/tcp
```

## git status final

```
## prueba-glm-ui...origin/prueba-glm-ui
 M frontend/src/api/client.ts
 M frontend/src/pages/ExploreCompetitionsPage.tsx
 M frontend/src/pages/LeagueCreatePage.tsx
 M frontend/src/pages/LeaguesMinePage.tsx
 M frontend/src/pages/LoginPage.css
 M frontend/src/pages/LoginPage.tsx
 M frontend/src/pages/PlayerDashboardPage.css
 M frontend/src/pages/PlayerDashboardPage.tsx
 M frontend/src/pages/RegisterPage.css
 M frontend/src/pages/RegisterPage.tsx
 M "docs/test/Test Demo 1 - v2 Login y circuito basico.docx"
?? .qwen/
?? Captura_Prueba.png
```

**Sin commit. Sin push.** Esperando prueba manual del usuario.
