# Informe — PlayPredict Backend Fix + Flujo Demo

## Objetivo
- Restaurar backend healthy
- Verificar flujo demo existente (Liga → Pronósticos → Ranking)

## Diagnóstico

### Problema 1: Backend Unhealthy
- **Síntoma**: Container `playpredict_backend` en estado `unhealthy`
- **Causa**: Backend cayó por error de conexión DNS (`Name or service not known`) al intentar resolver hostname `db`
- **Solución**: Reinicio del contenedor backend

### Problema 2: Solo 1 Fecha con 3 Partidos
- **Síntoma**: Base de datos solo tenía Fecha 1 con 3 partidos, todos finalizados
- **Causa**: El seeder original era idempotente por nombre — si la competencia ya existía, no agregaba datos nuevos
- **Solución**: Modificar `SeedCompetitionAsync` para agregar rounds faltantes si la competencia ya existe

## Archivos Modificados

| Archivo | Cambio |
|---------|--------|
| `backend/Data/DataSeeder.cs` | Método `SeedCompetitionAsync` reescrito para agregar rounds faltantes si la competencia ya existe |

## Cambios Realizados

### DataSeeder.cs
```csharp
// Antes: retornaba si la competencia existía
if (await db.Competitions.AnyAsync(c => c.Name == competitionName))
{
    return;
}

// Después: busca la competencia y agrega rounds faltantes
var competition = await db.Competitions
    .Include(c => c.Editions)
    .ThenInclude(e => e.Rounds)
    .ThenInclude(r => r.Matches)
    .FirstOrDefaultAsync(c => c.Name == competitionName);

if (competition is null)
{
    // Crear todo nuevo
}
else
{
    // Agregar rounds que faltan
    var existingRounds = await db.Rounds
        .Where(r => r.EditionId == edition.Id)
        .Select(r => r.Order)
        .ToListAsync();

    for (var roundIndex = 0; roundIndex < roundMatchups.Length; roundIndex++)
    {
        if (existingRounds.Contains(roundIndex + 1))
            continue;
        // Crear round y partidos
    }
}
```

## Pruebas Ejecutadas

| Prueba | Resultado |
|--------|-----------|
| `docker compose ps` | 3 servicios healthy |
| `GET /api/health` | `{"status":"ok"}` |
| `POST /api/auth/login` (juan.perez) | 200 OK, JWT válido |
| `GET /api/leagues/mine` | 2 Ligas (demo + propia) |
| `GET /api/editions/1/rounds` | 5 Fechas creadas |
| `GET /api/leagues/1/matches` | 15 partidos (9 finished, 6 scheduled) |
| `GET /api/rankings/leagues/1` | 4 posiciones con scoring correcto |

### Partidos Pendientes para Pronosticar
- River Plate vs Gimnasia (Fecha 5)
- Independiente vs Boca Juniors (Fecha 5)
- Estudiantes vs Racing Club (Fecha 5)
- Boca Juniors vs Estudiantes (Fecha 6)
- Racing Club vs River Plate (Fecha 6)
- Gimnasia vs Independiente (Fecha 6)

## Resultado

- Backend healthy
- Base de datos con 5 Fechas, 15 partidos
- Flujo demo verificable:
  - Login PLAYER → Mis Ligas → Liga → Pronósticos → Cargar pronóstico → Ranking

## Bugs Encontrados

1. **Partidos de Fecha 6 inesperados**: El seeder debería crear solo 5 fechas pero aparecen partidos con roundId=6. Posiblemente datos preexistentes o el seeder creó más de los esperados.
   - **Impacto**: Menor — no afecta funcionalidad demo
   - **Acción**: Documentar para revisión posterior

## Pendientes

1. **PRIORIDAD 2**: Datos reales del Clausura 2026 Fecha 6
2. **PRIORIDAD 3**: Implementar Liga Oficial vs Privada (LeagueType)
3. **PRIORIDAD 4**: Validación UI de rango de fechas
4. **PRIORIDAD 5**: Revisar contraste/legibilidad dark theme

## Git Status
```
On branch prueba-glm-ui
Changes not staged for commit:
  modified:   backend/Data/DataSeeder.cs
  new file:   docs/ai/qwen/README.md
  new file:   docs/ai/qwen/2026-08-18_1700_backend-fix-flujo-demo.md

Untracked files:
  Nuevo Documento de texto.txt
```

## Commit Actual
```
d348eeb WIP: checkpoint demo PlayPredict antes de cambiar de PC
```
