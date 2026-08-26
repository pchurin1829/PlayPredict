using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Services;
using System.Text;
using PlayPredict.Api.Domain.Constants;

namespace PlayPredict.Api.Endpoints;

public static class MatchEndpoints
{
    public static void MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/editions/{editionId:int}/fixture.csv", async (int editionId, PlayPredictDbContext db) =>
        {
            var edition = await db.Editions.Include(e => e.Competition).FirstOrDefaultAsync(e => e.Id == editionId);
            if (edition is null) return Results.NotFound();

            var rows = await db.Matches
                .Where(m => m.Round.EditionId == editionId)
                .OrderBy(m => m.Round.Order).ThenBy(m => m.StartsAtUtc)
                .Select(m => new { Round = m.Round.Name, m.Id, m.ParticipantHome, m.ParticipantAway, m.StartsAtUtc, m.Status })
                .ToListAsync();
            static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
            var csv = new StringBuilder("Competition,Edition,Round,MatchId,HomeTeam,AwayTeam,ScheduledAt,Status\r\n");
            foreach (var row in rows)
            {
                csv.Append(Csv(edition.Competition.Name)).Append(',').Append(Csv(edition.Name)).Append(',')
                    .Append(Csv(row.Round)).Append(',').Append(row.Id).Append(',')
                    .Append(Csv(row.ParticipantHome)).Append(',').Append(Csv(row.ParticipantAway)).Append(',')
                    .Append(row.StartsAtUtc.ToUniversalTime().ToString("O")).Append(',').Append(row.Status).Append("\r\n");
            }
            return Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(),
                "text/csv; charset=utf-8", $"fixture-{editionId}.csv");
        }).RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin)).WithTags("Matches");

        app.MapGet("/api/rounds/{roundId:int}/matches", async (int roundId, PlayPredictDbContext db) =>
        {
            var roundExists = await db.Rounds.AnyAsync(r => r.Id == roundId);
            if (!roundExists)
            {
                return Results.NotFound();
            }

            var matches = await db.Matches
                .Include(m => m.HomeTeam)
                .Include(m => m.AwayTeam)
                .Include(m => m.Scorers).ThenInclude(s => s.TeamPlayer)
                .Where(m => m.RoundId == roundId)
                .OrderBy(m => m.StartsAtUtc)
                .Select(m => ToDto(m))
                .ToListAsync();

            return Results.Ok(matches);
        }).WithTags("Matches");

        app.MapGet("/api/matches/{id:int}", async (int id, PlayPredictDbContext db) =>
        {
            var match = await db.Matches.Include(m => m.HomeTeam).Include(m => m.AwayTeam).Include(m => m.Scorers).ThenInclude(s => s.TeamPlayer).FirstOrDefaultAsync(m => m.Id == id);
            return match is null
                ? Results.NotFound()
                : Results.Ok(ToDto(match));
        }).WithTags("Matches");

        app.MapPost("/api/rounds/{roundId:int}/matches", async (int roundId, CreateMatchDto dto, PlayPredictDbContext db) =>
        {
            var roundExists = await db.Rounds.AnyAsync(r => r.Id == roundId);
            if (!roundExists)
            {
                return Results.NotFound();
            }

            var (errors, status) = ValidateMatch(dto.HomeTeamId, dto.AwayTeamId, dto.StartsAtUtc, dto.Status, MatchStatus.Scheduled);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var teams = await db.Teams.Where(t => t.Id == dto.HomeTeamId || t.Id == dto.AwayTeamId).ToDictionaryAsync(t => t.Id);
            if (!teams.TryGetValue(dto.HomeTeamId, out var homeTeam) || !teams.TryGetValue(dto.AwayTeamId, out var awayTeam))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["teams"] = ["Seleccioná equipos válidos."] });
            var participationErrors = await ValidateRoundTeamAvailabilityAsync(db, roundId, null, homeTeam, awayTeam, []);
            if (participationErrors.Count > 0) return Results.ValidationProblem(participationErrors);

            var match = new Match
            {
                RoundId = roundId,
                HomeTeamId = homeTeam.Id,
                AwayTeamId = awayTeam.Id,
                HomeTeam = homeTeam,
                AwayTeam = awayTeam,
                ParticipantHome = homeTeam.Name,
                ParticipantAway = awayTeam.Name,
                StartsAtUtc = dto.StartsAtUtc,
                Status = status,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Matches.Add(match);
            await db.SaveChangesAsync();

            return Results.Created($"/api/matches/{match.Id}", ToDto(match));
        }).WithTags("Matches");

        app.MapPut("/api/matches/{id:int}", async (int id, UpdateMatchDto dto, PlayPredictDbContext db) =>
        {
            var match = await db.Matches.FindAsync(id);
            if (match is null)
            {
                return Results.NotFound();
            }

            var (errors, status) = ValidateMatch(dto.HomeTeamId, dto.AwayTeamId, dto.StartsAtUtc, dto.Status, match.Status);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var teams = await db.Teams.Where(t => t.Id == dto.HomeTeamId || t.Id == dto.AwayTeamId).ToDictionaryAsync(t => t.Id);
            if (!teams.TryGetValue(dto.HomeTeamId, out var homeTeam) || !teams.TryGetValue(dto.AwayTeamId, out var awayTeam))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["teams"] = ["Seleccioná equipos válidos."] });
            var participationErrors = await ValidateRoundTeamAvailabilityAsync(db, match.RoundId, match.Id, homeTeam, awayTeam,
                [match.HomeTeamId, match.AwayTeamId]);
            if (participationErrors.Count > 0) return Results.ValidationProblem(participationErrors);

            match.HomeTeamId = homeTeam.Id;
            match.AwayTeamId = awayTeam.Id;
            match.HomeTeam = homeTeam;
            match.AwayTeam = awayTeam;
            match.ParticipantHome = homeTeam.Name;
            match.ParticipantAway = awayTeam.Name;
            match.StartsAtUtc = dto.StartsAtUtc;
            match.Status = status;

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(match));
        }).WithTags("Matches");

        app.MapPut("/api/matches/{id:int}/result", async (int id, MatchResultDto dto, PlayPredictDbContext db, PredictionEvaluationService evaluationService) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (dto.HomeGoals < 0)
            {
                errors["homeGoals"] = ["Los goles del local no pueden ser negativos."];
            }

            if (dto.AwayGoals < 0)
            {
                errors["awayGoals"] = ["Los goles del visitante no pueden ser negativos."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var match = await db.Matches.Include(m => m.HomeTeam).Include(m => m.AwayTeam).Include(m => m.Scorers).ThenInclude(s => s.TeamPlayer).FirstOrDefaultAsync(m => m.Id == id);
            if (match is null)
            {
                return Results.NotFound();
            }

            if (match.Status is MatchStatus.Cancelled)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["No se puede cargar un resultado en un Partido Cancelado."]
                });
            }

            var scorerInputs = (dto.Scorers ?? []).Where(s => s.Goals > 0).ToList();
            if (scorerInputs.Select(s => s.TeamPlayerId).Distinct().Count() != scorerInputs.Count)
                errors["scorers"] = ["Cada jugador debe aparecer una sola vez."];
            var playerIds = scorerInputs.Select(s => s.TeamPlayerId).ToList();
            var scorerPlayers = await db.TeamPlayers.Where(p => playerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);
            if (scorerPlayers.Count != playerIds.Distinct().Count() || scorerPlayers.Values.Any(p => p.TeamId != match.HomeTeamId && p.TeamId != match.AwayTeamId))
                errors["scorers"] = ["Cada goleador debe pertenecer a uno de los equipos del partido."];
            var homeScorerGoals = scorerInputs.Where(s => scorerPlayers.TryGetValue(s.TeamPlayerId, out var p) && p.TeamId == match.HomeTeamId).Sum(s => s.Goals);
            var awayScorerGoals = scorerInputs.Where(s => scorerPlayers.TryGetValue(s.TeamPlayerId, out var p) && p.TeamId == match.AwayTeamId).Sum(s => s.Goals);
            if (scorerInputs.Count > 0)
            {
                var scorerErrors = new List<string>();
                if (homeScorerGoals != dto.HomeGoals)
                    scorerErrors.Add(ScorerTotalMessage(match.ParticipantHome, dto.HomeGoals));
                if (awayScorerGoals != dto.AwayGoals)
                    scorerErrors.Add(ScorerTotalMessage(match.ParticipantAway, dto.AwayGoals));
                if (scorerErrors.Count > 0) errors["scorers"] = scorerErrors.ToArray();
            }
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            match.HomeGoals = dto.HomeGoals;
            match.AwayGoals = dto.AwayGoals;
            match.Status = MatchStatus.Finished;

            db.MatchScorers.RemoveRange(match.Scorers);
            match.Scorers = scorerInputs.Select(s => new MatchScorer
            {
                MatchId = match.Id,
                TeamPlayerId = s.TeamPlayerId,
                TeamPlayer = scorerPlayers[s.TeamPlayerId],
                Goals = s.Goals
            }).ToList();

            // Evalúa (crea o recalcula) los Pronósticos de este partido con la configuración
            // de puntuación de su Edición. Todo se persiste en un único SaveChanges, junto con
            // el resultado oficial, para que quede consistente de forma atómica.
            await evaluationService.PrepareEvaluationsForMatchAsync(db, match);

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(match));
        }).WithTags("Matches");

        app.MapDelete("/api/matches/{id:int}", async (int id, PlayPredictDbContext db) =>
        {
            var match = await db.Matches.FindAsync(id);
            if (match is null) return Results.NotFound();

            if (match.Status == MatchStatus.Finished || match.HomeGoals.HasValue || match.AwayGoals.HasValue)
            {
                return Results.Conflict(new
                {
                    message = "No se puede eliminar este partido porque ya tiene un resultado cargado."
                });
            }

            var predictionsCount = await db.Predictions.CountAsync(p => p.MatchId == id);
            if (predictionsCount > 0)
            {
                var evaluationsCount = await db.PredictionEvaluations.CountAsync(e => db.Predictions
                    .Where(p => p.MatchId == id).Select(p => p.Id).Contains(e.PredictionId));
                return Results.Conflict(new
                {
                    message = $"No se puede eliminar el partido porque tiene {predictionsCount} pronóstico(s) y {evaluationsCount} evaluación(es) relacionadas."
                });
            }

            db.Matches.Remove(match);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).WithTags("Matches");
    }

    private static (Dictionary<string, string[]> Errors, MatchStatus Status) ValidateMatch(
        int homeTeamId, int awayTeamId, DateTime startsAtUtc, string? statusInput, MatchStatus fallbackStatus)
    {
        var errors = new Dictionary<string, string[]>();

        if (homeTeamId <= 0) errors["homeTeamId"] = ["El equipo local es obligatorio."];
        if (awayTeamId <= 0) errors["awayTeamId"] = ["El equipo visitante es obligatorio."];
        if (homeTeamId > 0 && homeTeamId == awayTeamId) errors["awayTeamId"] = ["El equipo visitante debe ser distinto del local."];
        if (startsAtUtc == default) errors["startsAtUtc"] = ["La fecha y hora del partido son obligatorias."];

        // Un partido Finalizado solo puede modificarse vía /result: este endpoint nunca
        // le cambia el estado ni el resultado, sin importar qué status llegue en el DTO.
        if (fallbackStatus == MatchStatus.Finished)
        {
            return (errors, MatchStatus.Finished);
        }

        var status = fallbackStatus;
        if (!string.IsNullOrWhiteSpace(statusInput))
        {
            if (!Enum.TryParse<MatchStatus>(statusInput, ignoreCase: true, out status))
            {
                errors["status"] = [$"Estado inválido. Valores permitidos: {string.Join(", ", Enum.GetNames<MatchStatus>())}."];
            }
            else if (status == MatchStatus.Finished)
            {
                errors["status"] = ["El estado Finalizado solo puede establecerse cargando el Resultado Oficial (PUT /api/matches/{id}/result)."];
            }
        }

        return (errors, status);
    }

    private static string ScorerTotalMessage(string teamName, int officialGoals) => officialGoals == 0
        ? $"{teamName} no tiene goles en el resultado. No podés asignarle goleadores."
        : $"{teamName} tiene {officialGoals} {(officialGoals == 1 ? "gol" : "goles")} en el resultado. Debés asignar exactamente {officialGoals} {(officialGoals == 1 ? "gol" : "goles")} entre sus goleadores.";

    private static async Task<Dictionary<string, string[]>> ValidateRoundTeamAvailabilityAsync(
        PlayPredictDbContext db, int roundId, int? currentMatchId, Team homeTeam, Team awayTeam, HashSet<int> grandfatheredTeamIds)
    {
        var roundName = await db.Rounds.Where(r => r.Id == roundId).Select(r => r.Name).FirstAsync();
        var otherMatches = await db.Matches
            .Where(m => m.RoundId == roundId && (!currentMatchId.HasValue || m.Id != currentMatchId.Value))
            .Select(m => new { m.HomeTeamId, m.AwayTeamId })
            .ToListAsync();
        var usedTeamIds = otherMatches.SelectMany(m => new[] { m.HomeTeamId, m.AwayTeamId }).ToHashSet();
        var errors = new Dictionary<string, string[]>();
        if (usedTeamIds.Contains(homeTeam.Id) && !grandfatheredTeamIds.Contains(homeTeam.Id)) errors["homeTeamId"] = [$"{homeTeam.Name} ya participa en otro partido de {roundName}."];
        if (usedTeamIds.Contains(awayTeam.Id) && !grandfatheredTeamIds.Contains(awayTeam.Id)) errors["awayTeamId"] = [$"{awayTeam.Name} ya participa en otro partido de {roundName}."];
        return errors;
    }

    internal static MatchDto ToDto(Match m) =>
        new(m.Id, m.RoundId, m.HomeTeamId, m.AwayTeamId, m.ParticipantHome, m.ParticipantAway,
            m.HomeTeam?.LogoUrl, m.AwayTeam?.LogoUrl, m.StartsAtUtc, m.Status.ToString(), m.HomeGoals, m.AwayGoals,
            m.Scorers.Select(s => new MatchScorerDto(s.TeamPlayerId, s.TeamPlayer.DisplayName, s.TeamPlayer.TeamId, s.Goals)).ToList(), m.CreatedAtUtc);
}
