# Imágenes de Login — EL NENE / PlayPredict

## A. Campaña principal

| Propiedad | Contrato |
| --- | --- |
| Canvas | 800 × 900 px |
| Relación | 8:9 |
| Formato preferido | PNG o WebP |
| Fondo | Transparencia recomendada |
| Safe area | 50 px desde cada borde; área útil 700 × 800 px |
| Peso máximo recomendado | 500 KB |
| Presentación | Imagen completa con `object-fit: contain`, sin deformación |

Puede incluir logos, títulos, textos, copa, pelota, personajes, productos y premios.
No debe depender de un fondo propio: PlayPredict agrega el estadio por detrás,
independiente de cada campaña. No incluir estadio ni césped en la pieza comercial.
La transparencia permite ver el fondo fijo; no se elimina el fondo
de una imagen opaca automáticamente.

Todos los elementos esenciales deben quedar dentro de la safe area. Esta guía se
aplica al archivo entregado, no es un recorte ni un margen agregado por el navegador.

Plantilla: [plantilla-campana-login.svg](plantilla-campana-login.svg).

## B. Publicidades laterales

El mismo contrato se aplica a Publicidad 1, 2 y 3, en ese orden de arriba abajo:

| Propiedad | Contrato |
| --- | --- |
| Canvas | 600 × 400 px |
| Relación | 3:2 |
| Formatos | JPG, PNG o WebP |
| Safe area | 30 px desde cada borde; área útil 540 × 340 px |
| Peso máximo recomendado | 300 KB por publicidad |
| Presentación | Ancho del slot, relación 3:2 y `object-fit: cover` |

Precios, logos, productos y textos importantes deben quedar dentro de la safe area.
Las piezas de otra proporción pueden recortarse: entregar 3:2 evita ese problema.

Plantilla: [plantilla-publicidad-lateral.svg](plantilla-publicidad-lateral.svg).

## C. Recomendaciones de exportación

- No colocar textos ni precios pegados a los bordes.
- Mantener contraste suficiente, especialmente sobre fondos transparentes.
- Evitar texto demasiado pequeño: revisar la pieza reducida a tamaño laptop/móvil.
- Exportar en sRGB y optimizar el peso sin volver ilegibles letras o precios.
- Eliminar bordes, guías y etiquetas de las plantillas antes de exportar la pieza.
- Las plantillas SVG son material técnico para Diseño, no assets de producción.
- Los máximos de 500 KB / 300 KB son recomendaciones, no nuevos límites de código.

## D. Fondo fijo PlayPredict

Asset fijo incorporado y aprobado:

`frontend/public/assets/login/login-stadium-background.png`

URL pública: `/assets/login/login-stadium-background.png`.

Archivo actual: PNG de 1448 × 1086 px (4:3), 2.212.973 bytes, incorporado sin
conversión. Es independiente de la campaña y no se administra en los cuatro slots.

Entregar un estadio neutro con cielo azul oscuro, iluminación deportiva, tribunas y
césped. Sin textos, logos, promociones, productos, copa ni pelota protagonista.
Debe tolerar recorte proporcional (`background-size: cover`) y no contener contenido
esencial dependiente del encuadre. No es el contrato comercial de campaña 8:9.

### Recomendaciones para futuros archivos de fondo

- Master recomendado: **2400 × 1800 px, relación 4:3**, WebP opaco, color sRGB.
- Alternativa de mayor densidad: 3200 × 2400 px, también 4:3.
- Optimizar orientativamente a 500–800 KB sin introducir bandas en cielo o luces;
  no se agrega un límite técnico de peso en esta etapa.
- No usar 16:9 como proporción objetivo: el fondo cubre el hero, no la ventana completa.

Medición con viewport CSS de 1920 × 1080, zoom 100%:

- Padding exterior: 4 px por lado; separación entre hero y ads: 4 px.
- Ancho disponible para columnas: 1908 px; hero 1431 px y columna ads 477 px.
- Hero exterior: 1431 × 1072 px.
- Fondo dentro del borde de 1 px: **1429 × 1070 px**, relación **1,3355:1**.
- Un archivo de 2400 × 1800 con `cover` se renderiza aproximadamente a
  1429 × 1071,75 px: el recorte vertical total es sólo 1,75 px, centrado.
- Si se necesitara coincidir exactamente con esa medición: 1429 × 1070 px a 1×
  o 2858 × 2140 px a 2×. Se recomienda el master 4:3 por su formato estándar.

1920 × 1080 aquí significa **viewport de la página**, no resolución física del
monitor. Barras, pestañas, escala del sistema y zoom cambian el área disponible.
El layout usa `100dvh`; no hay una altura universal que restar por el navegador.
Ejemplo medido con viewport 1920 × 950: fondo de 1429 × 940 px; `cover` recorta
aproximadamente el 12,3% de la altura del master, repartido entre arriba y abajo.

### Safe area y composición del estadio

El fondo es ambiental: no existe una zona que garantice conservar cada detalle en
todas las relaciones de pantalla. Usar esta guía de tolerancia para el master 4:3:

- **Núcleo seguro recomendado:** x=35%–65%, y=10%–90%. En 2400 × 1800:
  x=840–1560 px, y=180–1620 px. Mantener allí suficientes señales de estadio
  para que siga siendo reconocible cuando se recorten los laterales.
- **Cielo oscuro:** parte superior, aproximadamente y=0%–35%.
- **Luces:** distribuir puntos de iluminación entre y=15%–35%; incluir algunas
  en el tercio central, sin depender sólo de las esquinas. Evitar focos blancos
  intensos detrás del formulario.
- **Tribunas:** banda amplia en la zona media; horizonte/transición a cancha
  aproximadamente entre y=60%–65%. Extenderla por todo el ancho.
- **Césped:** desde esa transición hasta el borde inferior; debe existir ya antes
  de y=80%, para sobrevivir a un recorte moderado del borde inferior en desktop.
- Los 10% superior/inferior y los laterales deben admitir pérdida de detalles.
  No colocar sujetos únicos ni puntos focales indispensables en esos bordes.
- En desktop, la zona derecha del hero, aproximadamente x=60%–85%, debe ser
  tranquila y oscura para acompañar al login. Campaña y formulario son capas que
  pueden ocultar partes del fondo; no diseñar elementos esenciales detrás de ellas.

Con el layout actual y `background-position: 50% 50%`, en tablet 768 × 1024 el
hero crece verticalmente y se conserva aproximadamente el 50% central del ancho
del master. En móvil 390 × 844 se conserva aproximadamente el 32% central.
Por eso un margen fijo de 50 px no sirve como safe area para este fondo. Las cifras
son orientativas: cambian con la altura del formulario, fuentes y mensajes de error.
No se promete conservar todas las luces laterales en móvil; sí una lectura coherente
de cielo, tribunas y césped mediante bandas horizontales amplias.

### Cobertura e incorporación sin cambiar geometría

`background-image` se aplica directamente a `.pp-login__stage`, con:

```css
background-size: cover;
background-position: center; /* 50% 50% */
background-repeat: no-repeat;
```

Cubre todo el panel, incluyendo su padding y detrás de ambos slots, hasta los
bordes redondeados. No se necesita una segunda caja con width/height 100%, ni
un pseudo-elemento. El background no participa en el cálculo de tamaño del grid.
La campaña tiene fondo transparente, sin máscara ni fondo añadido, y su imagen
usa `contain`. La campaña inicial actual tiene alfa y deja ver el estadio debajo.
La activación del fondo no cambia escala, posición o dimensiones de campaña,
formulario o publicidades.

La URL está activa en `LOGIN_APPEARANCE.backgroundImageUrl`, dentro de
`frontend/src/login/appearance.ts`. Si se configura `null` no se solicita un fondo.
Si el fondo falla al cargar, permanece el color azul `#031226` del contenedor.
El fondo fijo pertenece al frontend PlayPredict, no al slot comercial `Main`.

## E. Integración y migración

Se reutiliza el sistema existente: `GET /api/public/login-appearance` devuelve
`main`, `adTop`, `adMiddle` y `adBottom`. Los cambios de campaña/publicidades se
reciben por esa API sin cambiar código del login. El fondo fijo y los fallbacks
locales se centralizan en `frontend/src/login/appearance.ts`; no hay otra API ni
otro almacenamiento de campañas.

`main` ahora alimenta un slot independiente 8:9. El formulario HTML ocupa otra
celda del hero, sin superposición. Las publicidades usan tres slots independientes
3:2, incluso si comparten la misma URL.

Campaña inicial y fallback:
`frontend/public/assets/login/login-campaign-copa-el-nene.png`.

El archivo entregado mide **1182 × 1330 px**, prácticamente 8:9, y pesa
**1.445.410 bytes**. Tiene canal alfa real; aproximadamente el 50,6% de los píxeles
son totalmente transparentes. Se conserva tal como fue entregado: es una excepción
inicial al canvas recomendado 800 × 900 y al peso recomendado de 500 KB. Se muestra
completo con `contain` en el slot 8:9; no se ha redimensionado ni retocado.

El backend conserva su URL predeterminada histórica
`/assets/el-nene-login/copa-el-nene-panel-principal.png`. El frontend sustituye
exclusivamente esa URL por la campaña inicial transparente, tanto en el login como
en la vista previa administrativa. Las URLs de campañas personalizadas se respetan.
No se elimina la imagen histórica porque todavía puede ser referenciada.
Si la consulta pública falla, siguen disponibles la campaña y las tres ads locales.

Los `fitMode` del DTO se conservan por compatibilidad, pero la nueva composición
fija `contain` para campaña y `cover` para ads. No se modifica autenticación.

### Administrador existente alineado

`/admin/login-appearance` conserva selección, subida y restauración por slot.
Su ayuda, formatos sugeridos y previews representan 8:9/contain para campaña y
3:2/cover para las tres ads. Se retiró el selector de ajuste, porque no afecta a la
composición final. No hay controles de posición del hero en este administrador.

Los contratos y recomendaciones del frontend están en
`frontend/src/login/imageContracts.ts`. La vista previa utiliza dimensiones reales
de la imagen cargada, incluida la sustitución del default histórico. Las advertencias
de proporción y resolución del backend (todavía 4:3) se reemplazan en la presentación
por recomendaciones del nuevo contrato; otras advertencias se conservan. Al seleccionar
un archivo se informa si supera 500 KB / 300 KB, sin bloquear por ese motivo. El DTO
actual no incluye peso de archivos guardados; para éstos se muestra el peso recomendado,
no se inventa una medición ni se descarga el archivo sólo para calcularla.

No se cambian modelo, endpoints, backend, storage ni Base Inicial. Los límites
técnicos existentes siguen vigentes. Los campos `fitMode` permanecen en el DTO por
compatibilidad, aunque el frontend ya no ofrece cambiarlos.

### Evolución futura (sólo documentada)

Reutilizar y adaptar el administrador existente bajo:

Administración → Apariencia → Login

- Campaña principal [Subir imagen]
- Publicidad 1 [Subir imagen]
- Publicidad 2 [Subir imagen]
- Publicidad 3 [Subir imagen]
- [Vista previa] [Publicar]

Pendientes: alinear en otra etapa las recomendaciones del backend; vista previa
conjunta en distintas resoluciones; evaluar
borrador/publicación. Opcionalmente agregar fecha desde, fecha hasta y programación.
La subida actual guarda directamente; no equivale a ese futuro flujo editorial.

## F. Responsive

- Desktop ≥1440 px: campaña izquierda, formulario de hasta 360 px en una celda
  independiente, desplazado 40 px a la derecha. En 1920 × 1080 su centro queda
  aproximadamente al 74,4% del hero. Fondo compartido detrás de ambos.
- Laptop 1366 × 768: se reduce el slot completo de campaña, sin cortar su contenido;
  se reserva un mínimo de 320 px para la celda del login.
- Tablet (hasta 1100 px): formulario primero, campaña completa debajo, ads en fila
  de tres slots 3:2. Se permite scroll vertical, sin carrusel horizontal obligatorio.
- Móvil (hasta 620 px): formulario primero y campaña debajo; ads ocultas como antes.
- Las ads mantienen 3:2; pueden dejar espacio vertical alrededor de la columna,
  en lugar de estirarse para ocupar toda la altura del viewport.

## G. Archivos de referencia

Las capturas de validación son temporales y no forman parte de los assets.
`docs/imagenes/referencia-login-playpredict.png` es una referencia de trabajo y se
excluye del commit. También se excluye la copia fuente duplicada del estadio en
`docs/imagenes/login-stadium-background.png`; el archivo de producción está en
`frontend/public/assets/login/`. Los assets históricos existentes se conservan.
