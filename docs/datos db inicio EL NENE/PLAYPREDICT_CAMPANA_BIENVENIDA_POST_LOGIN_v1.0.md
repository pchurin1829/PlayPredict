# PlayPredict — Interstitial publicitario post-login
## Especificación funcional y técnica v1.0

## 1. Objetivo

Agregar a PlayPredict una funcionalidad configurable para mostrar **1, 2 o 3 imágenes publicitarias inmediatamente después del login exitoso y antes de que el PLAYER vea cualquier pantalla funcional de PlayPredict**.

La configuración y administración de estas imágenes será exclusiva del rol **ADMIN**.

El PLAYER:
- no verá opciones de configuración;
- sólo verá la secuencia publicitaria cuando corresponda;
- al finalizar la secuencia continuará automáticamente hacia su destino normal dentro de PlayPredict.

La funcionalidad está pensada para que cada empresa pueda promocionar productos, campañas, beneficios, lanzamientos o mensajes institucionales.

---

## 2. Idea optimizada

En lugar de modelarlo simplemente como “3 imágenes después del login”, conviene crear el concepto:

**Campaña de Bienvenida**

Una Campaña de Bienvenida contiene entre **1 y 3 slides publicitarias**.

Cada campaña define:
- nombre interno;
- estado activo/inactivo;
- fecha y hora de inicio opcional;
- fecha y hora de finalización opcional;
- orden de las imágenes;
- duración de cada imagen;
- comportamiento de avance;
- imágenes cargadas por ADMIN.

Esto permite resolver el requerimiento actual sin cerrar la arquitectura a futuras necesidades.

### Ejemplos

Campaña A:
- Imagen 1: 1,5 segundos
- Imagen 2: 1,5 segundos
- Total: 3 segundos

Campaña B:
- Imagen 1: 4 segundos
- Total: 4 segundos

Campaña C:
- Imagen 1: 2 segundos
- Imagen 2: 2 segundos
- Imagen 3: 2 segundos
- Total: 6 segundos

---

## 3. Regla principal de visualización

La campaña se muestra:

**después del login exitoso y antes de ingresar al Dashboard, Liga, Ranking o cualquier otra pantalla PLAYER.**

Debe mostrarse **una sola vez por sesión autenticada**, no en cada navegación interna.

Ejemplo:

Login
→ Campaña de Bienvenida
→ Dashboard

Luego:

Dashboard
→ Ranking
→ Mis Ligas
→ Pronósticos

NO debe volver a aparecer durante esa misma sesión.

Si el usuario hace logout y vuelve a iniciar sesión, podrá volver a mostrarse.

---

## 4. Alcance por rol

### ADMIN

El ADMIN puede:

- acceder al módulo “Campaña de Bienvenida”;
- crear o editar la campaña;
- cargar de 1 a 3 imágenes;
- ordenar las imágenes;
- definir duración individual;
- activar/desactivar la campaña;
- definir vigencia opcional;
- previsualizar la secuencia;
- reemplazar imágenes;
- eliminar imágenes.

### PLAYER

El PLAYER:

- no ve ninguna opción administrativa;
- no puede acceder a endpoints de administración;
- visualiza únicamente la campaña activa que le corresponda;
- al finalizar continúa automáticamente a PlayPredict.

---

## 5. Reglas de negocio v1.0

### Cantidad de imágenes

Mínimo:
- 1 imagen.

Máximo:
- 3 imágenes.

No permitir activar una campaña sin al menos una imagen válida.

### Duración

Cada slide debe tener su propia duración.

Recomendación:

- mínimo: 1 segundo;
- máximo: 10 segundos;
- valor por defecto: 2 segundos.

Ejemplos válidos:

- 2 imágenes × 1,5 s = 3 s total.
- 1 imagen × 4 s = 4 s total.
- 3 imágenes × 2 s = 6 s total.

La duración debe poder guardarse con decimales.

### Orden

Cada imagen tiene:

`displayOrder`

Valores típicos:

1
2
3

### Estado

Una campaña puede estar:

- BORRADOR
- ACTIVA
- INACTIVA

Para MVP puede simplificarse a:

`IsActive`

pero conviene que el diseño deje abierta la evolución futura.

### Vigencia

Campos opcionales:

- StartsAtUtc
- EndsAtUtc

Si ambos son null:
la campaña activa no vence.

Si existe vigencia:
sólo debe mostrarse dentro del período correspondiente.

---

## 6. Una sola campaña activa

Para v1.0:

**sólo puede existir una Campaña de Bienvenida activa al mismo tiempo por empresa/contexto.**

Si el ADMIN activa una nueva campaña, el sistema debe:

- desactivar la anterior;

o

- impedir la activación y pedir confirmación.

Preferencia MVP:

**activar una campaña desactiva automáticamente la anterior.**

Esto simplifica muchísimo la lógica PLAYER.

---

## 7. Modelo de datos propuesto

### WelcomeCampaign

Campos sugeridos:

- Id
- CompanyId, si PlayPredict ya maneja separación por empresa y corresponde al modelo actual
- Name
- IsActive
- StartsAtUtc nullable
- EndsAtUtc nullable
- CreatedAtUtc
- UpdatedAtUtc
- CreatedByUserId
- UpdatedByUserId

### WelcomeCampaignSlide

Campos sugeridos:

- Id
- WelcomeCampaignId
- ImageUrl / ImagePath
- DisplayOrder
- DurationSeconds decimal
- IsActive
- CreatedAtUtc
- UpdatedAtUtc

Restricciones:

- máximo 3 slides activas por campaña;
- DisplayOrder único dentro de la campaña;
- DurationSeconds dentro de rango permitido.

---

## 8. Almacenamiento de imágenes

Antes de implementar, auditar cómo PlayPredict almacena actualmente:

- logos;
- imágenes;
- assets subidos por usuarios/admin.

Reutilizar el mecanismo existente si existe.

No guardar binarios grandes directamente en PostgreSQL salvo que la arquitectura actual ya lo haga deliberadamente.

Preferencia:

- archivo físico / storage;
- base de datos guarda ruta o URL.

### Validaciones

Permitir inicialmente:

- JPG
- JPEG
- PNG
- WEBP

Definir tamaño máximo razonable.

Ejemplo:

5 MB por imagen.

Rechazar archivos que no sean imágenes válidas aunque tengan extensión correcta.

---

## 9. Presentación PLAYER

La campaña debe utilizar una pantalla/interstitial de pantalla completa.

Características:

- ocupa el viewport completo;
- sin sidebar;
- sin menú superior;
- sin contenido de PlayPredict visible detrás;
- imagen centrada;
- adaptación responsive;
- transición limpia entre imágenes.

### Ajuste de imagen

Recomendación:

`object-fit: cover`

con una zona segura para evitar deformaciones.

Si el diseño publicitario necesita mostrar la imagen completa puede evaluarse:

`contain`

Por eso conviene que el componente admita una política visual centralizada.

Para v1.0, elegir una sola estrategia consistente y documentarla.

---

## 10. Transiciones

Usar transición discreta:

- fade corto;
- sin animaciones pesadas.

No agregar carrusel manual ni indicadores tipo “1/3” salvo necesidad de UX.

El objetivo no es que parezca una galería, sino una secuencia publicitaria breve.

---

## 11. Saltar publicidad

Para v1.0 recomiendo:

**NO mostrar botón “Saltar” inicialmente.**

Razón:
la empresa está usando este espacio como exposición garantizada.

Pero dejar preparado el componente para una futura propiedad:

`AllowSkip`

Si más adelante se habilita:

- podría aparecer después de X segundos;
- podría configurarse por campaña.

No implementar `AllowSkip` en esta primera versión salvo que resulte trivial y no complique el modelo.

---

## 12. Click publicitario

También conviene dejar previsto, aunque no es obligatorio para v1.0:

- ClickUrl
- CallToAction

Ejemplo futuro:

“Ver producto”

No implementar navegación comercial en esta primera versión salvo que la arquitectura lo haga muy simple.

La prioridad actual es:

**imagen + tiempo + transición + continuación automática.**

---

## 13. Flujo de login

Flujo esperado:

1. Usuario envía credenciales.
2. Backend autentica.
3. Frontend recibe sesión/JWT.
4. Antes de navegar al destino final:
   - consultar si existe campaña activa;
   - determinar si ya fue mostrada en esta sesión.
5. Si NO existe campaña:
   - continuar normalmente.
6. Si existe campaña y todavía no fue mostrada:
   - ir a pantalla interstitial.
7. Reproducir slides.
8. Marcar campaña como vista en esa sesión.
9. Redirigir al destino original.

Importante:

si el usuario estaba intentando entrar a una URL protegida concreta, por ejemplo:

`/leagues/3`

después de la campaña debe regresar a:

`/leagues/3`

y no forzarlo siempre al Dashboard.

---

## 14. Control “una vez por sesión”

No crear inicialmente una tabla histórica de impresiones sólo para resolver esto.

Para v1.0 puede manejarse con estado de sesión del frontend:

por ejemplo:

`sessionStorage`

clave conceptual:

`playpredict_welcome_campaign_seen_{campaignId}`

Ventajas:

- una sola vez por sesión/browser tab;
- no ensucia base de datos;
- no requiere endpoint adicional de tracking.

Importante:

si el ADMIN cambia y activa una nueva campaña con otro ID durante la misma sesión, la nueva campaña puede mostrarse porque la clave es distinta.

A futuro, si se necesitan métricas reales de impresiones, crear un módulo separado de analytics.

---

## 15. Comportamiento ante errores

La publicidad **nunca debe impedir entrar a PlayPredict**.

Si ocurre:

- error cargando configuración;
- imagen inexistente;
- timeout;
- error de red;

el sistema debe:

- registrar el error;
- saltar la campaña;
- continuar a PlayPredict.

Regla:

**fail open**.

No bloquear al PLAYER por una falla publicitaria.

---

## 16. Precarga

Para evitar pantallas blancas entre imágenes:

- precargar las imágenes de la campaña antes de iniciar la reproducción;
- si una imagen individual falla, omitirla;
- si fallan todas, continuar inmediatamente a PlayPredict.

---

## 17. Módulo ADMIN

Agregar una opción visible únicamente para ADMIN.

Nombre sugerido:

**Campaña de Bienvenida**

Ubicación:
dentro de la zona de configuración/comunicación/branding que mejor coincida con la navegación administrativa actual.

La pantalla debe incluir:

### Datos generales

- Nombre de campaña
- Activa
- Desde
- Hasta

### Slides

Por cada slide:

- previsualización;
- cargar/reemplazar imagen;
- duración;
- orden;
- eliminar.

Máximo visible:
3.

Botón:

**Agregar imagen**

debe deshabilitarse al llegar a 3.

### Acciones

- Guardar
- Activar / Desactivar
- Previsualizar

---

## 18. Previsualización ADMIN

El ADMIN debe poder ejecutar:

**Previsualizar campaña**

Esto debe reproducir exactamente:

- las mismas imágenes;
- los mismos tiempos;
- las mismas transiciones;

pero sin afectar el estado real de sesión del ADMIN ni marcarla como vista para PLAYER.

---

## 19. Seguridad

Todos los endpoints de escritura:

- ADMIN only.

Endpoints administrativos de lectura:
- ADMIN only.

Endpoint PLAYER para campaña activa:
- usuario autenticado.

El PLAYER no debe recibir:

- campañas inactivas;
- borradores;
- campañas futuras;
- campañas vencidas.

No confiar en ocultar botones frontend.

La autorización debe validarse también en backend.

---

## 20. Endpoints sugeridos

Los nombres deben adaptarse a las convenciones actuales del repo.

### ADMIN

`GET /api/admin/welcome-campaigns`

`GET /api/admin/welcome-campaigns/{id}`

`POST /api/admin/welcome-campaigns`

`PUT /api/admin/welcome-campaigns/{id}`

`POST /api/admin/welcome-campaigns/{id}/activate`

`POST /api/admin/welcome-campaigns/{id}/deactivate`

`POST /api/admin/welcome-campaigns/{id}/slides`

`PUT /api/admin/welcome-campaigns/{id}/slides/{slideId}`

`DELETE /api/admin/welcome-campaigns/{id}/slides/{slideId}`

### PLAYER / authenticated

`GET /api/welcome-campaign/active`

Respuesta conceptual:

```json
{
  "campaignId": 12,
  "name": "Productos El Nene Agosto",
  "slides": [
    {
      "id": 31,
      "imageUrl": "...",
      "displayOrder": 1,
      "durationSeconds": 1.5
    },
    {
      "id": 32,
      "imageUrl": "...",
      "displayOrder": 2,
      "durationSeconds": 1.5
    }
  ]
}
```

Si no hay campaña:

- `204 No Content`

o el patrón equivalente ya utilizado por el backend.

---

## 21. Componentes frontend sugeridos

### ADMIN

- WelcomeCampaignAdminPage
- WelcomeCampaignEditor
- WelcomeCampaignSlideEditor
- WelcomeCampaignPreview

### PLAYER

- WelcomeCampaignInterstitial

Evitar duplicar la lógica de reproducción.

Crear un componente reutilizable para que:

- preview ADMIN;
- reproducción PLAYER;

utilicen el mismo motor visual.

---

## 22. Destino post-login

El sistema debe preservar el destino esperado.

Casos:

Login normal:
→ campaña
→ `/`

Ingreso a una ruta protegida:
→ login
→ campaña
→ ruta solicitada originalmente

Esto es obligatorio para no romper navegación.

---

## 23. Diseño responsive

Debe probarse como mínimo en:

- desktop;
- tablet;
- móvil.

Las imágenes no deben deformarse.

El contenido publicitario debe respetar una “safe area” razonable para evitar cortes importantes.

---

## 24. Pruebas mínimas

### Backend

1. Sin campaña activa → no devuelve campaña.
2. Campaña activa con 1 slide.
3. Campaña activa con 2 slides.
4. Campaña activa con 3 slides.
5. No permitir más de 3.
6. No permitir duración inválida.
7. Campaña futura → no visible.
8. Campaña vencida → no visible.
9. Sólo una activa.
10. PLAYER no puede editar.
11. ADMIN puede editar.
12. Slides respetan DisplayOrder.

### Frontend

1. Login sin campaña → entra directo.
2. Login con 1 imagen × 4 s.
3. Login con 2 imágenes × 1,5 s.
4. Login con 3 imágenes.
5. La campaña aparece una sola vez por sesión.
6. Navegar internamente no vuelve a mostrarla.
7. Logout + login puede mostrarla nuevamente.
8. Error de imagen → no bloquea.
9. Error endpoint → no bloquea.
10. Se conserva returnUrl.
11. Preview ADMIN funciona.
12. PLAYER no ve menú ADMIN.

---

## 25. Escenario demo El Nene

Usar las imágenes ya existentes en:

`docs/imagenes/El nene/`

como referencia visual y, si resulta apropiado para datos demo, poder configurar una Campaña de Bienvenida de prueba.

NO acoplar el producto a El Nene.

El Nene es sólo un escenario/demo.

La funcionalidad debe ser genérica y reutilizable para cualquier empresa.

---

## 26. Decisiones de producto v1.0

Quedan definidas:

- 1 a 3 imágenes.
- Duración individual configurable.
- Se muestra post-login.
- Se muestra antes de cualquier pantalla PLAYER.
- Una vez por sesión.
- Sólo ADMIN configura.
- PLAYER no administra.
- Una sola campaña activa.
- Vigencia opcional.
- Preview ADMIN.
- Fail-open ante errores.
- Sin botón Saltar inicialmente.
- Sin analytics de impresiones inicialmente.
- Sin links comerciales inicialmente.
- Arquitectura preparada para ampliar después.

---

# PROMPT PARA CODEX

Implementar la funcionalidad definida en este documento como **Campaña de Bienvenida post-login**.

## Antes de programar

Auditar el repo actual y determinar:

1. cómo funciona Login y returnUrl;
2. cómo se determina el rol ADMIN/PLAYER;
3. dónde están los menús ADMIN;
4. cómo se almacenan actualmente logos/imágenes/uploads;
5. si existe infraestructura reutilizable para archivos;
6. cómo se organiza EF Core y las migraciones;
7. cómo se implementan endpoints ADMIN;
8. qué componente/ruta PLAYER es el primer destino post-login.

Entregar primero un resumen breve de la arquitectura encontrada y luego implementar.

## Requerimientos obligatorios

Implementar:

- WelcomeCampaign;
- WelcomeCampaignSlide;
- relación 1:N;
- máximo 3 slides;
- duración decimal por slide;
- una sola campaña activa;
- vigencia opcional;
- endpoints ADMIN protegidos;
- endpoint authenticated para campaña activa;
- carga/reemplazo/eliminación de imágenes;
- pantalla ADMIN;
- preview ADMIN;
- interstitial PLAYER;
- reproducción automática;
- preservación de returnUrl;
- una sola visualización por sesión;
- fail-open;
- precarga de imágenes;
- validaciones de formato/tamaño;
- tests backend/frontend razonables.

## Importante

No hardcodear El Nene.

No insertar imágenes del demo dentro del bundle productivo como lógica obligatoria.

No romper el login existente.

No mostrar menú administrativo al PLAYER.

No bloquear acceso a PlayPredict ante fallas de publicidad.

No implementar todavía:

- analytics;
- tracking de impresiones;
- CTA;
- URLs comerciales;
- botón Saltar;
- múltiples campañas simultáneas;
- segmentación por usuario.

## Migración

Si el modelo lo requiere, crear una migración EF Core correctamente integrada.

No aplicar operaciones destructivas.

## UX

Mantener la campaña breve y limpia.

La publicidad debe desaparecer automáticamente al finalizar.

Ejemplo:

2 slides:
- slide 1 = 1,5 s
- slide 2 = 1,5 s

Total = 3 s.

1 slide:
- slide 1 = 4 s

Total = 4 s.

## Validaciones finales

Ejecutar:

`npm run build`

`dotnet test backend.Tests`

`git diff --check`

No hacer commit.

No hacer push.

## Informe final

Informar:

1. arquitectura implementada;
2. migración creada;
3. endpoints;
4. restricciones ADMIN/PLAYER;
5. almacenamiento de imágenes;
6. funcionamiento post-login;
7. cómo se preserva returnUrl;
8. cómo se evita repetir durante la sesión;
9. comportamiento ante errores;
10. archivos modificados;
11. tests y resultado;
12. instrucciones exactas para probarlo manualmente como ADMIN y PLAYER.
