# Diagnóstico y solución: migración AddLeagueType no aplicada

**Fecha:** 2026-08-18  
**Branch:** prueba-glm-ui  
**Agente:** Qwen Code

---

## Síntoma

El backend crashea al arrancar con:

```
SqlState: 42703
MessageText: column l.LeagueType does not exist
```

El error ocurre en `DataSeeder.GetOrCreateDemoLeagueAsync` (línea 239), llamado desde `SeedRankingDemoAsync` (línea 498).

---

## Causa raíz

La migración `20260818172000_AddLeagueType` **existe como archivo `.cs`** en `backend/Migrations/`, pero **le falta el archivo `.Designer.cs`** companion. EF Core requiere que la migración esté compilada en el assembly para detectarla en tiempo de ejecución.

Sin el `.Designer.cs`, `dotnet build` no incluye la migración en el assembly compilado, y `MigrateAsync()` en `Program.cs` la ignora por completo, reportando:

```
No migrations were applied. The database is already up to date.
```

La tabla `__EFMigrationsHistory` solo tenía 8 migraciones (hasta `AddLeagueDescription`), y la columna `LeagueType` no existía en `Leagues`.

### Por qué falta el `.Designer.cs`

La migración fue probablemente creada manualmente (solo el archivo `.cs`) en lugar de generarse con `dotnet ef migrations add AddLeagueType`. El comando de EF genera automáticamente el `.Designer.cs` y actualiza el `PlayPredictDbContextModelSnapshot.cs`.

---

## Solución aplicada

Migración ejecutada manualmente vía SQL directo contra PostgreSQL, equivalente exacto al `Up()` de la migración:

### Paso 1: Agregar columna

```sql
ALTER TABLE "Leagues" ADD COLUMN "LeagueType" integer NOT NULL DEFAULT 1;
```

### Paso 2: Actualizar ligas demo a Official

```sql
UPDATE "Leagues" SET "LeagueType" = 0
WHERE "Name" LIKE '%Liga General%' OR "Name" LIKE '%(demo)%';
```

### Paso 3: Registrar migración en el historial de EF

```sql
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260818172000_AddLeagueType', '10.0.10');
```

### Paso 4: Reiniciar backend

```bash
docker compose restart backend
docker compose up -d frontend
```

---

## Verificación

| Check | Resultado |
|-------|-----------|
| `__EFMigrationsHistory` tiene 9 migraciones | ✅ Incluye `20260818172000_AddLeagueType` |
| Columna `LeagueType` en `Leagues` | ✅ `integer NOT NULL DEFAULT 1` |
| Liga demo = Official (0) | ✅ `Liga General - Liga Profesional (demo) → LeagueType = 0` |
| LIGA AMIGOS 1 = Private (1) | ✅ `LeagueType = 1` |
| `docker compose ps` backend | ✅ healthy |
| `docker compose ps` db | ✅ healthy |
| `docker compose ps` frontend | ✅ Up, puerto 5175 |
| `curl localhost:8006/api/health` | ✅ `{"status":"ok"}` |

---

## Cambios en archivos

**Ningún archivo modificado.** La corrección fue exclusivamente a nivel de datos en PostgreSQL. No se tocaron archivos del proyecto.

---

## Estado git

```
On branch prueba-glm-ui
Untracked files: .qwen/, Captura_Prueba.png
nothing added to commit
```

Sin cambios en tracked working tree.

---

## Estado docker compose ps final

```
playpredict_backend    healthy   0.0.0.0:8006->8080/tcp
playpredict_db         healthy   0.0.0.0:5436->5432/tcp
playpredict_frontend   Up        0.0.0.0:5175->5175/tcp
```

---

## Recomendación pendiente

La migración debería regenerarse correctamente con `dotnet ef migrations add` para incluir el `.Designer.cs` y actualizar el `ModelSnapshot`. Esto evitará inconsistencias futuras si se agregan más migraciones. Se puede hacer sin impacto en la BD ya existente (EF detectará que ya está aplicada por el registro en `__EFMigrationsHistory`).

Pasos sugeridos (cuando se desee):
1. Eliminar `backend/Migrations/20260818172000_AddLeagueType.cs`
2. Ejecutar `dotnet ef migrations add AddLeagueType` desde el host con acceso a `dotnet-ef`
3. Verificar que el nuevo `.cs` sea equivalente al anterior
4. Commit del `.cs`, `.Designer.cs` y `ModelSnapshot.cs` actualizado
