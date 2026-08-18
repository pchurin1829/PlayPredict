# QWEN

Este proyecto usa CLAUDE.md como documento de contexto principal.
Las reglas completas están en CLAUDE.md — leerlo obligatoriamente al inicio de cada sesión.

## Reglas críticas de migraciones (resumen)

Estas reglas son de cumplimiento obligatorio para todo agente:

1. **Todo cambio de modelo requiere migración EF Core.** No modificar entities sin generar migración.
2. **Las migraciones se generan con EF tooling, nunca manualmente.** Usar `NUEVA_MIGRACION.bat <Nombre>`.
3. **Toda migración tiene 3 archivos** (`.cs`, `.Designer.cs`, `ModelSnapshot.cs`) que deben commitearse juntos. Si falta uno, EF no detecta la migración.
4. **Las migraciones se aplican automáticamente al arrancar** el backend (`MigrateAsync()` en `Program.cs`), antes de los seeders.
5. **NUNCA usar `docker compose down -v`** para resolver desajustes de schema. Eso destruye datos.
6. **Cambiar de PC conserva datos**: `git pull` → `INICIO_SESION.bat` → migraciones automáticas.
7. **`dotnet-ef` se instala vía Tool Manifest** (`backend/.config/dotnet-tools.json`). Usar `dotnet tool restore` en el contenedor.

## Protocolo de sesión

Igual que CLAUDE.md:

- **"Inicio Sesion"**: leer docs obligatorias, verificar estado, informar.
- **"Fin Sesion"**: validar, actualizar docs, proponer commit, no commit sin autorización.
- **No commit ni push sin autorización explícita.**

## Scripts del proyecto

- `INICIO_SESION.bat` — levanta DB + backend (aplica migraciones) + frontend, verifica health.
- `ACTUALIZAR_BD.bat` — fuerza actualización de migraciones.
- `NUEVA_MIGRACION.bat <Nombre>` — crea migración EF correcta con validación de artefactos.
- `FIN_SESION.bat` — cierra sesión de trabajo.
