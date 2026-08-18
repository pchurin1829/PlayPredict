# Migration Hardening — Diagnóstico y solución definitiva

**Fecha:** 2026-08-18  
**Branch:** prueba-glm-ui  
**Agente:** Qwen Code

---

## 1. Causa raíz original

La migración `20260818172000_AddLeagueType` fue creada manualmente — solo existía el archivo `.cs`. Faltaba:

- `20260818172000_AddLeagueType.Designer.cs` — no existía
- `PlayPredictDbContextModelSnapshot.cs` — no incluía `LeagueType` en la entity League

Sin el `.Designer.cs`, EF Core no compilaba la migración en el assembly. `MigrateAsync()` en `Program.cs` no la detectaba y reportaba "database is already up to date". La columna `LeagueType` nunca se creaba. El backend crasheaba en `DataSeeder.SeedRankingDemoAsync` al consultar una columna inexistente (`42703: column l.LeagueType does not exist`).

---

## 2. Reparación de AddLeagueType

### Método utilizado

1. Se creó un Tool Manifest local (`backend/.config/dotnet-tools.json`) con `dotnet-ef` v10.0.10.
2. Se ejecutó `dotnet ef migrations add TempAddLeagueType` dentro del contenedor para generar los artefactos correctos.
3. Se copió el `.Designer.cs` temporal al nombre correcto (`20260818172000_AddLeagueType.Designer.cs`).
4. Se ajustó el `[Migration("...")]` y nombre de clase para preservar el MigrationId original.
5. El `ModelSnapshot` quedó actualizado automáticamente por EF.
6. Se eliminaron los archivos temporales.
7. El `.cs` original se preservó sin cambios (contiene el `defaultValue: 1` y el SQL update para demos).

### Artefactos finales

| Archivo | Estado |
|---------|--------|
| `backend/Migrations/20260818172000_AddLeagueType.cs` | Preservado (sin cambios) |
| `backend/Migrations/20260818172000_AddLeagueType.Designer.cs` | **Creado** |
| `backend/Migrations/PlayPredictDbContextModelSnapshot.cs` | **Actualizado** (incluye `LeagueType`) |

### Verificación de integridad

- El MigrationId `20260818172000_AddLeagueType` se preserva exactamente.
- Una BD que ya tiene ese MigrationId registrado NO intenta reaplicar la columna (verificado en Escenario A).
- Una BD limpia aplica las 9 migraciones en orden incluyendo AddLeagueType (verificado en Escenario B).

---

## 3. Archivos creados/modificados

### Modificados

| Archivo | Cambio |
|---------|--------|
| `backend/Program.cs` | Migraciones con logging, fail-fast, antes de seeders |
| `backend/Migrations/PlayPredictDbContextModelSnapshot.cs` | Actualizado por EF (incluye LeagueType) |
| `INICIO_SESION.bat` | Reescrito: levanta servicios, espera health, muestra logs en fallo |
| `CLAUDE.md` | Agregada sección "Reglas de Migraciones de Base de Datos (OBLIGATORIO)" |

### Creados

| Archivo | Propósito |
|---------|-----------|
| `backend/.config/dotnet-tools.json` | Tool Manifest local para `dotnet-ef` v10.0.10 |
| `backend/Migrations/20260818172000_AddLeagueType.Designer.cs` | Designer.cs de la migración reparada |
| `ACTUALIZAR_BD.bat` | Script para forzar actualización de migraciones |
| `NUEVA_MIGRACION.bat` | Script para crear migraciones EF correctas |
| `QWEN.md` | Reglas para agentes Qwen |
| `docs/ai/qwen/2026-08-18_migracion-addleaguetype.md` | Informe del fix anterior (SQL manual) |

---

## 4. Tool Manifest

**Archivo:** `backend/.config/dotnet-tools.json`

```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "dotnet-ef": {
      "version": "10.0.10",
      "commands": ["dotnet-ef"]
    }
  }
}
```

**Uso:**

```bash
# Dentro del contenedor o desde el host con SDK:
dotnet tool restore

# Crear migración:
dotnet ef migrations add <Nombre> --output-dir Migrations --project PlayPredict.Api.csproj

# Listar migraciones:
dotnet ef migrations list --project PlayPredict.Api.csproj

# Aplicar manualmente (normalmente automático al arrancar):
dotnet ef database update --project PlayPredict.Api.csproj
```

**Versión de EF Core:** 10.0.10 (coincide con `Microsoft.EntityFrameworkCore.Design` en el `.csproj`).

---

## 5. Scripts agregados

### ACTUALIZAR_BD.bat

- Verifica DB corriendo
- Restaura `dotnet-ef` tools
- Lista migraciones pendientes
- Reinicia backend para aplicar migraciones automáticas
- Espera health del backend
- Muestra migraciones aplicadas o logs de error

### NUEVA_MIGRACION.bat `<Nombre>`

- Valida nombre no vacío
- Restaura tools
- Ejecuta `dotnet ef migrations add`
- **Verifica que los 3 artefactos fueron generados** (.cs, .Designer.cs, ModelSnapshot)
- Falla si falta alguno
- Muestra próximos pasos

---

## 6. Comportamiento de startup (Program.cs)

El bloque de startup ahora sigue este orden estricto:

```
1. Conectar DB
2. Obtener migraciones pendientes (GetPendingMigrationsAsync)
3. Si hay pendientes:
   a. Log: "Pending migrations: N"
   b. Log: "Applying migration: <nombre>" (por cada una)
   c. Ejecutar MigrateAsync()
   d. Log: "All pending migrations applied successfully."
   e. Si falla: LogCritical + throw (fail-fast, no seeder)
4. Si no hay pendientes:
   a. Log: "Database schema is up to date. No pending migrations."
5. Ejecutar seeders (solo si migraciones OK)
6. Arrancar aplicación
```

**Garantías:**
- Los seeders NUNCA se ejecutan contra un esquema incompleto.
- Si una migración falla, el proceso aborta con `LogCritical`.
- Los logs son visibles en `docker compose logs backend`.

---

## 7. Pruebas

### Escenario A — Base existente

**BD:** `playpredict_db` con 9 migraciones registradas (incluida AddLeagueType aplicada manualmente).

| Check | Resultado |
|-------|-----------|
| Backend arranca | ✅ healthy |
| EF detecta AddLeagueType como ya aplicada | ✅ "Database schema is up to date. No pending migrations." |
| No intenta agregar LeagueType nuevamente | ✅ "No migrations were applied." |
| Datos intactos | ✅ LIGA AMIGOS 1 → LeagueType=1, Liga General demo → LeagueType=0 |
| Seed funciona | ✅ Backend healthy |
| `/api/health` | ✅ `{"status":"ok"}` |

### Escenario B — Base nueva/temporal

**BD:** `playpredict_test_clean` creada vacía en el mismo contenedor postgres.

| Check | Resultado |
|-------|-----------|
| Todas las migraciones detectadas | ✅ 9 migraciones |
| Aplicadas en orden cronológico | ✅ InitialFixtureSchema → ... → AddLeagueType |
| AddLeagueType aplicada | ✅ `Applying migration '20260818172000_AddLeagueType'.` |
| Columna LeagueType creada | ✅ `integer` type |
| `__EFMigrationsHistory` registra las 9 | ✅ |
| `dotnet ef database update` exitoso | ✅ `Done.` |
| BD temporal eliminada | ✅ `DROP DATABASE` |

---

## 8. Comandos de uso diario

### Al llegar a otra PC

```bat
git pull --ff-only
INICIO_SESION.bat
```

### Crear una migración

```bat
NUEVA_MIGRACION.bat AddPlayerStats
```

### Forzar actualización de BD

```bat
ACTUALIZAR_BD.bat
```

### Restaurar dotnet-ef manualmente

```bash
docker compose run --rm --no-deps backend bash -c "dotnet tool restore"
```

---

## 9. Reglas para agentes

Agregadas en `CLAUDE.md` (sección "Reglas de Migraciones de Base de Datos") y `QWEN.md` (resumen).

Puntos clave:
1. Todo cambio de modelo requiere migración EF Core versionada.
2. Las migraciones se generan con EF tooling, nunca manualmente.
3. Toda migración tiene 3 archivos que deben commitearse juntos.
4. Migraciones antes de seeder (garantizado por Program.cs).
5. Nunca `docker compose down -v` para resolver desajustes.
6. Cambiar de PC conserva datos: `git pull` → `INICIO_SESION.bat`.

---

## 10. Git status final

```
 M CLAUDE.md
 M INICIO_SESION.bat
 M backend/Migrations/PlayPredictDbContextModelSnapshot.cs
 M backend/Program.cs
?? .qwen/
?? ACTUALIZAR_BD.bat
?? Captura_Prueba.png
?? NUEVA_MIGRACION.bat
?? QWEN.md
?? backend/.config/
?? backend/Migrations/20260818172000_AddLeagueType.Designer.cs
?? docs/ai/qwen/2026-08-18_migracion-addleaguetype.md
```

Sin commit. Sin push. Esperando autorización del usuario.

---

## 11. docker compose ps final

```
playpredict_backend    healthy   0.0.0.0:8006->8080/tcp
playpredict_db         healthy   0.0.0.0:5436->5432/tcp
playpredict_frontend   Up        0.0.0.0:5175->5175/tcp
```
