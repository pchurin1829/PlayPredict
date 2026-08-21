# Login y Registro — Visual refresh

**Fecha:** 2026-08-20
**Branch:** `prueba-glm-ui`
**Commit de partida:** `126abb5`
**Commit/push:** no realizados

## Alcance

Refresh exclusivamente visual de `/login` y `/register`. No se modificaron autenticación, endpoints, validaciones funcionales, roles ni el circuito PLAYER.

## Paleta reutilizada

Se reutilizaron los valores del tema oscuro PLAYER (`PlayerTheme.css`):

- fondo `#0e0e1a`;
- superficie `#1a1a2e`;
- superficie secundaria `#23233a`;
- borde `#2c2c46`;
- primario `#8676ff`;
- primario oscuro `#6a5aeb`;
- texto `#f5f5fa`;
- texto secundario `#a3a3c2`.

Login y Registro ahora comparten los tokens `--pp-login-*` declarados para ambos contenedores.

## Causa del bajo contraste en Registro

`RegisterPage.css` utilizaba variables `--pp-login-*`, pero estaban declaradas sólo dentro de `.pp-login`. En `/register` las variables quedaban indefinidas y el navegador descartaba reglas de color, fondo y borde que dependían de ellas. Se corrigió el alcance de esos tokens sin cambiar JSX funcional.

## Imagen deportiva

Se incorporó la imagen definitiva generada específicamente para PlayPredict. El PNG original de 1610×977 (1.98 MB) se convirtió localmente con `ffmpeg` a WebP de alta calidad, conservando las dimensiones y reduciendo el asset a aproximadamente 223 KB:

`frontend/public/assets/login-football.webp`

El PNG fuente con nombre largo se eliminó luego de validar visualmente el WebP para evitar copias redundantes. `Captura_Prueba.png` es una captura antigua del dashboard y se mantuvo expresamente fuera del proyecto/commit.

La fotografía se utiliza sólo como background decorativo del stage de Login, con `cover`, posición centrada y overlay progresivo hacia el formulario. La escena SVG anterior queda oculta como fallback de código y no se superpone visualmente. Registro no usa la foto y conserva su aspecto aprobado.

## Login

- Base violeta/azul oscuro en lugar de negro puro.
- Foto definitiva de futbolista/estadio integrada con overlay y gradiente hacia la card.
- Card con superficie PLAYER, borde sutil, blur y sombra violeta suave.
- Inputs, labels y placeholders con contraste reforzado.
- Focus con borde y halo lila.
- Columna publicitaria integrada con superficies azul/lila.
- Fix de Chrome autofill preservado.

## Registro

- Misma paleta y lenguaje visual del Login.
- Fondo violeta/azul con gradientes y slot opcional de la misma foto.
- Marca, subtítulo, título, labels, inputs, placeholders, botón y link con contraste visible.
- Errores en rosado claro sobre fondo oscuro.
- Focus lila y autofill oscuro.
- Corrección de `min-width` en campos del grid para evitar que Apellido desborde la card.
- Ajustes verticales para pantallas desktop de altura reducida.

## Archivos modificados

- `frontend/src/pages/LoginPage.tsx`
- `frontend/src/pages/LoginPage.css`
- `frontend/src/pages/RegisterPage.tsx`
- `frontend/src/pages/RegisterPage.css`
- `frontend/public/assets/login-football.webp` (nuevo)
- `PROJECT_STATUS.md`
- este informe

## Validaciones

- `npx tsc --noEmit`: OK.
- `npm run build`: OK; 95 módulos transformados.
- `git diff --check`: OK, con advertencias esperadas LF/CRLF.
- Frontend HTTP 200 después de reiniciar sólo el contenedor frontend.
- Asset `/assets/login-football.webp`: HTTP 200, 222508 bytes.
- Capturas headless reales verificadas en 1366×768 y 1920×1080:
  - Login con futbolista y estadio visibles, card legible y publicidad integrada.
  - Registro totalmente legible, sin overflow horizontal ni campos fuera de la card.
  - Cards completas dentro del viewport.
- Links `Registrate` e `Iniciar sesión` conservan sus rutas existentes.
- No se alteró la lógica de submit de Login ni Registro.

Autofill se validó por reglas CSS específicas `:-webkit-autofill`; la comprobación con credenciales guardadas reales queda para uso normal en Chrome.

## Git status final

Sin commit ni push. Se preservan todos los cambios previos sin commit y los untracked preexistentes `.qwen/` y `Captura_Prueba.png`.
