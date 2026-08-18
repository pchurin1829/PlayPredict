# CLAUDE

- Priorizar el MVP.
- No agregar funcionalidades sin aprobación.
- Actualizar PROJECT_STATUS.md en cada sesión.

## Protocolo de sesión

Al recibir "Inicio Sesion":

1. Leer la siguiente documentación obligatoria:
   - CLAUDE.md
   - SESSION.md
   - PROJECT_STATUS.md
   - PLAN_DE_TRABAJO.md
   - docs/business/PLAYPREDICT_PRODUCTO_v1.0.md
   - docs/business/MODELO_NEGOCIO_PLAYPREDICT_v1.0.md
   - docs/business/PLAYPREDICT_ESTRATEGIA_v1.0.md
2. Verificar PROJECT_STATUS.md.
3. Revisar Git y Docker.
4. Informar el estado real.
5. Continuar solamente desde el próximo paso documentado.

Al recibir "Fin Sesion":

1. Ejecutar validaciones pertinentes.
2. Actualizar SESSION.md.
3. Actualizar PROJECT_STATUS.md si corresponde.
4. Mostrar git status.
5. Proponer el mensaje de commit.
6. No hacer commit ni push sin autorización explícita.

## Visión Estratégica del Producto

Antes de proponer nuevas funcionalidades, cambios importantes de arquitectura o nuevas entidades del dominio, revisar obligatoriamente:

- docs/business/PLAYPREDICT_PRODUCTO_v1.0.md
- docs/business/MODELO_NEGOCIO_PLAYPREDICT_v1.0.md
- docs/business/PLAYPREDICT_ESTRATEGIA_v1.0.md

Estos tres documentos constituyen la visión oficial del producto.

Toda propuesta futura deberá ser consistente con ellos.

Si una funcionalidad contradice alguno de estos documentos, el conflicto deberá informarse explícitamente antes de comenzar la implementación.

## Principios de Evolución del Producto

Antes de implementar un nuevo módulo deberá evaluarse si la necesidad puede resolverse mediante configuración utilizando componentes existentes.

Evitar crear nuevas entidades, nuevos flujos o nuevas arquitecturas cuando el mismo objetivo pueda alcanzarse reutilizando el modelo actual.

Toda nueva funcionalidad deberá responder afirmativamente a las siguientes preguntas:

- ¿Está alineada con PLAYPREDICT_PRODUCTO_v1.0.md?
- ¿Aporta valor al modelo de negocio?
- ¿Respeta la estrategia de evolución del producto?
- ¿Mantiene la arquitectura modular?
- ¿Puede reutilizarse en múltiples experiencias configurables?
- ¿Favorece la configuración antes que la programación?

Si alguna respuesta es negativa, deberá informarse antes de comenzar el desarrollo.

## Filosofía de Desarrollo

- Configuración antes que programación.
- Reutilización antes que duplicación.
- Arquitectura modular.
- Responsabilidad única por módulo.
- Evolución incremental mediante Sprints.
- White Label como capacidad nativa.
- Escalabilidad desde el diseño.
- Mantener desacoplados los motores de negocio.
- Evitar soluciones específicas cuando puedan resolverse mediante configuración.

## Validación de Nuevos Desarrollos

Antes de comenzar cualquier Sprint importante, verificar:

1. Que el desarrollo esté alineado con la visión del producto.
2. Que no duplique funcionalidades existentes.
3. Que no contradiga la estrategia definida.
4. Que pueda reutilizarse en futuras experiencias configurables.
5. Que mantenga la separación entre los motores del sistema.

Si existe alguna contradicción, informar antes de programar.

## Reglas de Migraciones de Base de Datos (OBLIGATORIO)

Estas reglas aplican a todos los agentes (Claude, Qwen, y cualquier otro). Son de cumplimiento obligatorio.

### Creación de migraciones

1. **Todo cambio persistente del modelo requiere migración EF Core versionada.** Si modificás una entity, agregás una propiedad, o cambiás una relación, generá una migración.

2. **Las migraciones deben generarse mediante EF tooling, nunca manualmente.** Usar:
   ```
   NUEVA_MIGRACION.bat <NombreEnPascalCase>
   ```
   o equivalente:
   ```
   docker compose run --rm --no-deps backend bash -c "dotnet tool restore && dotnet ef migrations add <Nombre> --output-dir Migrations --project PlayPredict.Api.csproj"
   ```

3. **NUNCA crear solamente el `.cs` manualmente.** Toda migración genera TRES artefactos que deben commitearse juntos:
   - `<timestamp>_<Nombre>.cs` — operaciones Up/Down
   - `<timestamp>_<Nombre>.Designer.cs` — modelo objetivo
   - `PlayPredictDbContextModelSnapshot.cs` — snapshot actualizado

   Si falta cualquiera de los tres, EF Core no detecta la migración y la BD queda desactualizada.

4. **Verificar después de generar**: confirmar que los tres archivos existen antes de commitear.

### Aplicación de migraciones

5. **Las migraciones se aplican automáticamente al arrancar el backend.** `Program.cs` ejecuta `MigrateAsync()` antes de los seeders. Si una migración falla, el backend aborta (fail-fast) y no ejecuta seeders.

6. **Migraciones antes de seeder.** El código garantiza este orden. Nunca modificar `Program.cs` para ejecutar seeders antes de migraciones.

7. **NUNCA usar `docker compose down -v` para resolver desajustes de schema.** Eso destruye datos. Si hay un desajuste, investigar la causa y aplicar la migración correcta.

8. **Cambiar de PC debe conservar datos.** El flujo correcto es: `git pull` → `INICIO_SESION.bat` → migraciones se aplican automáticamente. No se pierde data existente.

### Herramientas

9. **`dotnet-ef` se distribuye vía Tool Manifest local** (`backend/.config/dotnet-tools.json`). Ejecutar `dotnet tool restore` dentro del contenedor. No depende de instalación global.

10. **Scripts versionados**:
    - `NUEVA_MIGRACION.bat <Nombre>` — genera migración correcta
    - `ACTUALIZAR_BD.bat` — fuerza actualización de BD
    - `INICIO_SESION.bat` — levanta entorno completo
