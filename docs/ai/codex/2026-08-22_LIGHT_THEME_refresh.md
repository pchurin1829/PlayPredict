# PlayPredict — Light Theme Refresh

Fecha: 2026-08-22
Rama: `prueba-glm-ui`
Alcance: interfaz autenticada PLAYER, sin cambios funcionales ni de backend/DB.

## 1. Sistema de colores anterior

La aplicación ya contaba con variables compartidas `--color-*` en `index.css`. El entorno ADMIN consumía mayormente su variante clara, mientras `.playout` redefinía esos tokens en `PlayerTheme.css` con fondos azul-negro, superficies violeta oscuro, texto blanco y sombras intensas. La mayoría de componentes PLAYER ya consumía esas variables, pero `PlayerPages.css` conservaba tintes `rgba` y hex específicos del tema oscuro en cards, estados, botones y fechas.

Login y Registro usan deliberadamente un conjunto aislado `--pp-login-*`, fotografía deportiva y fondos nocturnos. Esa identidad aprobada no se alteró.

## 2. Tokens creados y reutilizados

Se creó un contrato semántico único con:

- Fondos: `--pp-bg-page`, `--pp-bg-surface`, `--pp-bg-surface-alt`, `--pp-bg-sidebar`.
- Texto: `--pp-text-primary`, `--pp-text-secondary`, `--pp-text-muted`.
- Bordes: `--pp-border`, `--pp-border-strong`.
- Acción primaria: `--pp-primary`, `--pp-primary-hover`, `--pp-primary-soft`.
- Marca: `--pp-accent`, `--pp-accent-hover`, `--pp-accent-soft`.
- Estados: `--pp-success`, `--pp-success-soft`, `--pp-warning`, `--pp-warning-soft`, `--pp-danger`, `--pp-danger-soft`.
- Contraste sobre color: `--pp-on-color`.

Los tokens históricos `--color-*` quedaron como alias compatibles. Esto permite incorporar futuras paletas Light/Violet/Blue/Graphite sobrescribiendo tokens, sin modificar los componentes ni agregar todavía selector o ThemeContext.

## 3. Paleta final

- Página: `#f5f7fa`.
- Superficies: `#ffffff` y `#f8fafc`.
- Texto principal: `#202936`; secundario `#5e6b7a`; muted `#8793a3`.
- Bordes: `#dfe5ec` / `#c9d3df`.
- Primario azul: `#3182ce`; hover `#2567aa`; soft `#e9f3fb`.
- Acento PlayPredict violeta: `#7667d9`; soft `#f0edff`.
- Liga Oficial: celeste `#3182ce`; Liga de Amigos/Privada: verde `#27855c`.
- Success: `#27855c` / `#e8f7ef`.
- Warning: `#946711` / `#fff5d6`.
- Danger: `#be3f4a` / `#fcebed`.

## 4. Archivos modificados

- `frontend/src/index.css`
- `frontend/src/components/player/PlayerTheme.css`
- `frontend/src/components/player/PlayerHeader.css`
- `frontend/src/components/player/PlayerSidebar.css`
- `frontend/src/components/player/DashboardHero.css`
- `frontend/src/components/player/MatchPredictionCard.css`
- `frontend/src/pages/PlayerPages.css`
- `frontend/src/pages/LeaguesMinePage.tsx`
- `frontend/src/pages/LeagueDetailPage.tsx`
- `frontend/src/components/ConfirmModal.css`
- `docs/ai/codex/2026-08-22_LIGHT_THEME_refresh.md`

## 5. Pantallas adaptadas

El cambio de tokens y los ajustes puntuales cubren Inicio/Dashboard, Competencias Oficiales, Mis Ligas, Crear/Unirse a Liga, Detalle de Liga, Pronósticos, Resultados, Ranking, Premios, Perfil, navegación superior, sidebar y ConfirmModal. No se modificó comportamiento, estados, scoring, teclado ni persistencia de Pronósticos.

Las Ligas Oficiales usan acento celeste y las Ligas de Amigos/Privadas usan verde. La señal acompaña el recorrido desde Mis Ligas hacia Resumen, Pronósticos, Resultados y Ranking mediante encabezado, tab activa, badges e indicadores suaves. Las cards de partidos permanecen neutras. Cards, tablas, inputs y paneles quedan sobre superficies claras con separadores visibles. `(Vos)` conserva texto y badge, por lo que no depende sólo del color.

El ajuste final de contraste usa sidebar gris `#eef1f4`, partidos neutros `#eef1f4`/`#f3f4f6`, cards oficiales `#eaf4ff` y cards privadas `#eaf8ef`. La acción “Dejar de participar” adopta texto/borde danger moderado y fondo casi blanco, sin competir con la acción primaria.

## 6. Hardcodes pendientes deliberados

- Login/Register y su SVG/fotografía: colores nocturnos intencionales, fuera del refresh claro.
- `TeamBadge.tsx`: colores propios de escudos/equipos e indicadores SVG.
- Blancos sobre botones/avatares con fondo sólido: color de contraste intencional.
- `admin.css` y `Layout.css`: mantienen estilos legacy del ADMIN; sólo reciben los tokens globales donde ya los consumen. El rediseño integral ADMIN queda para su fase funcional.
- Colores oro/plata/bronce: semántica propia de posiciones del ranking.

## 7. Validación técnica

- `npx tsc --noEmit`: correcto.
- `npm run build`: correcto (95 módulos transformados).
- `git diff --check`: correcto.
- Revisión headless desktop a 1440 × 1000: correcta.
- Pantallas revisadas: Inicio, Competencias Oficiales, Mis Ligas, Detalle/Resumen, Pronósticos, Resultados, Ranking, Crear Liga, Administrar Liga y ConfirmModal.
- Frontend disponible en `http://localhost:5175`.

## 8. Estado Git final

Cambios locales sin commit ni push, preparados para TEST VISUAL. Los archivos locales/untracked preexistentes ajenos al producto permanecen excluidos e intactos. Backend y DB no fueron modificados.
