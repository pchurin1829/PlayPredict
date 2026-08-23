# PlayPredict — Auditoría Mobile PLAYER

Fecha: 2026-08-22
Rama: `prueba-glm-ui`
Viewport prioritario: 390 × 844 px
Viewports complementarios: 360 × 800, 430 × 932 y 768 × 1024 px

## 1. Problemas encontrados por severidad

### Bloqueantes

- El sidebar PLAYER se iniciaba desplegado y fijo en anchos pequeños, ocupando gran parte del viewport sin un control mobile claro para abrirlo o cerrarlo.
- El contenido principal no tenía `min-width: 0`, por lo que algunos descendientes podían forzar overflow horizontal.

### Importantes

- Las tabs del detalle podían requerir scroll horizontal o quedar incómodas para tocar.
- Ranking conservaba una tabla de seis columnas, difícil de leer en 360–430 px.
- Cards, formularios y acciones conservaban separaciones y anchos pensados principalmente para desktop.
- Inputs y botones de Pronósticos no garantizaban un target táctil cómodo.
- ConfirmModal podía acercarse demasiado a los bordes y sus acciones podían quedar apretadas.
- El contenido secundario “Próximamente” consumía espacio útil dentro del menú mobile.

### Menores

- Nombres largos de ligas/equipos podían forzar cortes poco naturales.
- Algunos encabezados y metadatos necesitaban compactación moderada.
- La auditoría visual automatizada autenticada no pudo capturar screenshots por una limitación local de conexión WebSocket con Edge DevTools; no se agregaron dependencias para sortearla.

## 2. Correcciones realizadas

- Se implementó un drawer lateral hasta 1024 px, cerrado inicialmente y al navegar.
- Se agregó botón hamburger accesible, backdrop para cierre y foco visible.
- Se eliminó la franja residual del sidebar colapsado.
- Se protegió el layout principal contra overflow por ancho mínimo implícito.
- Se apilaron grids, formularios y acciones cuando el ancho lo requiere.
- Se adaptaron tabs, cards de partidos, inputs, botones y fechas expandibles a targets táctiles.
- Se convirtió el ranking mobile a una lista de bloques sin eliminar posición, nombre, puntos ni estadísticas secundarias.
- Se hizo ConfirmModal seguro para viewport, con scroll interno y botones apilados.

## 3. Estrategia responsive aplicada

- Desktop mantiene el diseño existente.
- Hasta 1024 px, la navegación pasa a drawer y el contenido usa todo el ancho disponible.
- Hasta 768 px, tabs y encabezados se redistribuyen, ranking pasa a cards y los controles principales aseguran altura táctil.
- Hasta 430 px, grids pasan a una columna, formularios y acciones se apilan cuando corresponde, y cards de partidos reducen padding sin reducir legibilidad.

## 4. Menú PLAYER

- Botón hamburger visible en tablet/mobile.
- Drawer lateral superpuesto, con fondo gris del tema claro y backdrop.
- Se cierra al seleccionar una ruta o tocar fuera.
- Conserva Inicio, Competencias Oficiales, Mis Ligas, Ranking General y Mi Perfil.
- Las opciones “Próximamente” se ocultan sólo en mobile estrecho para priorizar navegación funcional.
- No se modificaron rutas ni permisos.

## 5. Mis Ligas

- Una card por fila en mobile estrecho.
- Se conservan identidades Oficial celeste y Amigos verde.
- Badges, estado y nombres largos permanecen visibles.
- Acciones tienen altura táctil y distribución flexible; Entrar conserva jerarquía primaria y Administrar/Dejar de participar la secundaria neutra.

## 6. Pronósticos

- No se modificaron Enter, guardado, edición, eliminación, estados, cierre ni scoring.
- Equipos pueden contraerse sin overflow y sus nombres admiten corte seguro.
- Inputs crecen a tamaño táctil en mobile; botones aseguran altura mínima de 44 px.
- Fechas cerradas conservan colapsado/expandido y el encabezado se redistribuye en dos columnas en pantallas estrechas.
- Cards de partidos permanecen neutras/gris claro.

## 7. Resultados

- Cards y filas flexibles admiten wrap de resultado, pronóstico y puntos.
- En mobile estrecho el contenido se presenta en una columna sin exigir scroll horizontal.
- No se alteró la información ni su cálculo.

## 8. Ranking

- En hasta 768 px deja de presentarse como tabla ancha.
- Cada participante se muestra como bloque con posición, nombre y puntos como información primaria.
- Exactos, correctos y evaluados permanecen debajo como datos secundarios.
- La identificación `(Vos)` se conserva en el nombre y la estructura no depende sólo del color.

## 9. Modales

- ConfirmModal usa ancho con margen lateral, alto máximo dependiente del viewport y scroll interno.
- En hasta 430 px sus botones ocupan el ancho disponible, se apilan y tienen 44 px mínimos.
- No se cambió ninguna confirmación ni comportamiento.

## 10. Crear y administrar Liga

- Selectores, filas de formulario y campos pasan a una columna en mobile estrecho.
- Acciones se apilan y ocupan el ancho disponible.
- Las mismas reglas benefician Unirme con código y el contenido administrativo existente sin tocar su lógica.

## 11. Archivos modificados por la fase mobile

- `frontend/src/components/Layout.tsx`
- `frontend/src/components/ConfirmModal.css`
- `frontend/src/components/player/MatchPredictionCard.css`
- `frontend/src/components/player/PlayerHeader.css`
- `frontend/src/components/player/PlayerHeader.tsx`
- `frontend/src/components/player/PlayerLayout.css`
- `frontend/src/components/player/PlayerSidebar.css`
- `frontend/src/components/player/PlayerSidebar.tsx`
- `frontend/src/pages/PlayerPages.css`
- `docs/ai/codex/2026-08-22_MOBILE_PLAYER_audit.md`

El worktree también conserva cambios legítimos previos del refresh visual claro; no fueron revertidos ni reabiertos.

## 12. Validación técnica

- `npx tsc --noEmit`: OK.
- `npm run build`: OK.
- `git diff --check`: OK (únicamente avisos informativos de normalización LF/CRLF).
- Frontend Docker reiniciado y servido en `http://localhost:5175`.

## 13. Pendientes menores

- Confirmación visual manual final en dispositivo/navegador real por parte del usuario.
- Screenshots autenticados automáticos no generados: Edge headless respondió por HTTP, pero el canal DevTools WebSocket local rechazó la conexión. No afecta la aplicación ni requirió instalar herramientas nuevas.
- Login y Registro conservaron su identidad aprobada; no se detectó una necesidad suficiente para intervenirlos en esta fase.
