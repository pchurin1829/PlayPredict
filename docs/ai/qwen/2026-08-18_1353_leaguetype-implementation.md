# LeagueType Implementation - Verified

**Fecha:** 2026-08-18 13:53
**Sesión:** Continuación de trabajo en branch `prueba-glm-ui`

## Objetivo
Implementar la distinción visual entre Ligas Oficiales de PlayPredict y Ligas Privadas de usuarios.

## Cambios Realizados

### Backend

1. **Nuevo Enum** (`backend/Domain/Enums/LeagueType.cs`):
   ```csharp
   public enum LeagueType { Official = 0, Private = 1 }
   ```

2. **Entidad League actualizada** (`backend/Domain/Entities/League.cs`):
   - Propiedad: `public LeagueType LeagueType { get; set; } = LeagueType.Private;`

3. **DTOs actualizados** (`backend/Dtos/LeagueDtos.cs`):
   - `LeagueSummaryDto` y `LeagueDetailDto` incluyen campo `leagueType`

4. **Endpoints actualizados** (`backend/Endpoints/LeagueEndpoints.cs`):
   - `ToSummaryDtoAsync` y `ToDetailDtoAsync` retornan `league.LeagueType.ToString()`

5. **DataSeeder modificado** (`backend/Data/DataSeeder.cs`):
   - Liga demo creada con `LeagueType = LeagueType.Official`
   - Lógica idempotente mejorada para agregar Fechas faltantes

6. **Migración manual** (`backend/Migrations/20260818172000_AddLeagueType.cs`):
   - Creada e aplicada manualmente via SQL directo (hot reload en Docker no detecta cambios de migración)

### Frontend

1. **Types actualizados** (`frontend/src/api/types.ts`):
   ```typescript
   export type LeagueType = 'Official' | 'Private'
   export const LEAGUE_TYPE_LABELS: Record<LeagueType, string> = {
     Official: 'OFICIAL',
     Private: 'PRIVADA',
   }
   ```

2. **LeaguesMinePage.tsx** - Badge condicional:
   - `🏆 OFICIAL` (gradient violet) si `leagueType === 'Official'`
   - `MI LIGA` (gradient cyan) si `isCreator` y no es oficial
   - `PRIVADA` (muted) para el resto

3. **PlayerPages.css** - Estilos de badges:
   - `.pp-league-card__badge` base
   - Variantes `--official`, `--mine`, `--private`

## Problemas Encontrados y Resueltos

1. **Backend unhealthy - DNS error**: `NpgsqlException: Name or service not known`
   - **Solución**: `docker compose restart backend` luego `docker compose up -d --force-recreate backend`

2. **Solo 1 Fecha en BD en lugar de 5**: Seeder idempotente retornaba temprano si la competencia existía
   - **Solución**: Modificar `SeedCompetitionAsync` para agregar Fechas faltantes

3. **Migración no aplicaba**: Hot reload en Docker/Windows no detecta nueva migración
   - **Solución**: Aplicación manual via SQL:
     ```sql
     ALTER TABLE "Leagues" ADD COLUMN IF NOT EXISTS "LeagueType" integer NOT NULL DEFAULT 1;
     UPDATE "Leagues" SET "LeagueType" = 0 WHERE "Name" LIKE '%Liga General%' OR "Name" LIKE '%(demo)%';
     INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion") VALUES ('20260818172000_AddLeagueType', '9.0.0');
     ```

## Verificación

```bash
# Backend saludable
curl http://localhost:8006/api/health
# {"status":"ok"}

# API devuelve leagueType
curl http://localhost:8006/api/leagues/mine -H "Authorization: Bearer <token>"
# [
#   {"id":1,"name":"Liga General - Liga Profesional (demo)","leagueType":"Official",...},
#   {"id":2,"name":"Liga Amigos del Trabajo","leagueType":"Private",...}
# ]
```

## Estado de Prioridades

| # | Prioridad | Estado |
|---|-----------|--------|
| 0 | Backend healthy | ✅ Done |
| 1 | Verificar flujo demo | ⏳ Pendiente |
| 2 | Datos reales Clausura 2026 Fecha 6 | ⏳ Pendiente |
| 3 | LeagueType badges | ✅ **Done** |
| 4 | Fix date range selector | ⏳ Pendiente |
| 5 | Dark theme legibility | ⏳ Pendiente |

## Próximos Pasos

1. **PRIORITY 1**: Verificar flujo demo end-to-end (Ligas → Pronósticos → Ranking)
2. **PRIORITY 2**: Insertar datos reales de Clausura 2026 Fecha 6
3. Continuar con PRIORITY 4 y 5

## Archivos Modificados

- `backend/Domain/Enums/LeagueType.cs` (nuevo)
- `backend/Domain/Entities/League.cs`
- `backend/Dtos/LeagueDtos.cs`
- `backend/Endpoints/LeagueEndpoints.cs`
- `backend/Data/DataSeeder.cs`
- `backend/Migrations/20260818172000_AddLeagueType.cs` (nuevo)
- `frontend/src/api/types.ts`
- `frontend/src/pages/LeaguesMinePage.tsx`
- `frontend/src/pages/PlayerPages.css`
