using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class AdminOfficialLeagueEndpoints
{
    public static void MapAdminOfficialLeagueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/official-leagues")
            .WithTags("Admin - Official Leagues")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapGet("", async (PlayPredictDbContext db, LeagueScoringService scoring) =>
        {
            var leagues = await db.Leagues
                .Where(league => league.LeagueType == LeagueType.Official)
                .OrderByDescending(league => league.CreatedAtUtc)
                .ToListAsync();

            var result = new List<AdminOfficialLeagueDto>();
            foreach (var league in leagues)
            {
                result.Add(await ToDtoAsync(db, scoring, league));
            }

            return Results.Ok(result);
        });

        group.MapGet("/{id:int}", async (int id, PlayPredictDbContext db, LeagueScoringService scoring) =>
        {
            var league = await db.Leagues.FirstOrDefaultAsync(candidate =>
                candidate.Id == id && candidate.LeagueType == LeagueType.Official);
            return league is null ? Results.NotFound() : Results.Ok(await ToDtoAsync(db, scoring, league));
        });

        group.MapPost("", async (CreateOfficialLeagueDto dto, ClaimsPrincipal principal, PlayPredictDbContext db, LeagueScoringService scoring) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null) return Results.Unauthorized();

            var (errors, scope) = await ValidateAsync(db, dto.Name, dto.Description, dto.CompetitionId,
                dto.EditionId, dto.ScopeType, dto.RoundFromId, dto.RoundToId);
            ValidateScoring(errors, dto.ExactScorePoints, dto.CorrectOutcomePoints, dto.IncorrectPoints, dto.PreferredPlayerPointsPerGoal, dto.PreferredPlayerPositions);
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var now = DateTime.UtcNow;
            var league = new League
            {
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                CompetitionId = dto.CompetitionId,
                EditionId = dto.EditionId,
                ScopeType = scope,
                LeagueType = LeagueType.Official,
                RoundFromId = scope == LeagueScopeType.RoundRange ? dto.RoundFromId : null,
                RoundToId = scope == LeagueScopeType.RoundRange ? dto.RoundToId : null,
                InviteCode = await GenerateCodeAsync(db),
                IsActive = dto.IsActive,
                CreatedByUserId = user.Id,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            ApplyScoring(league, dto.UseGeneralScoring, dto.ExactScorePoints, dto.CorrectOutcomePoints, dto.IncorrectPoints,
                dto.PreferredPlayerEnabled, dto.PreferredPlayerPointsPerGoal, dto.PreferredPlayerPositions);

            db.Leagues.Add(league);
            await db.SaveChangesAsync();
            return Results.Created($"/api/admin/official-leagues/{league.Id}", await ToDtoAsync(db, scoring, league));
        });

        group.MapPut("/{id:int}", async (int id, UpdateOfficialLeagueDto dto, PlayPredictDbContext db, LeagueScoringService scoring) =>
        {
            var league = await db.Leagues.FirstOrDefaultAsync(candidate =>
                candidate.Id == id && candidate.LeagueType == LeagueType.Official);
            if (league is null) return Results.NotFound();

            var (errors, scope) = await ValidateAsync(db, dto.Name, dto.Description, dto.CompetitionId,
                dto.EditionId, dto.ScopeType, dto.RoundFromId, dto.RoundToId);
            ValidateScoring(errors, dto.ExactScorePoints, dto.CorrectOutcomePoints, dto.IncorrectPoints, dto.PreferredPlayerPointsPerGoal, dto.PreferredPlayerPositions);
            var changesFixtureScope = league.CompetitionId != dto.CompetitionId
                || league.EditionId != dto.EditionId
                || league.ScopeType != scope
                || league.RoundFromId != (scope == LeagueScopeType.RoundRange ? dto.RoundFromId : null)
                || league.RoundToId != (scope == LeagueScopeType.RoundRange ? dto.RoundToId : null);
            var currentScopedMatchIds = GetScopedMatchIds(db, league);
            if (changesFixtureScope && (await db.PredictionEvaluations.AnyAsync(evaluation => evaluation.LeagueId == league.Id)
                || await db.Predictions.AnyAsync(prediction => currentScopedMatchIds.Contains(prediction.MatchId))))
            {
                errors["editionId"] = ["No se puede cambiar la fuente deportiva o el alcance porque la Liga ya tiene pronósticos. Podés editar el nombre y el estado."];
            }
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            league.Name = dto.Name.Trim();
            league.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            league.CompetitionId = dto.CompetitionId;
            league.EditionId = dto.EditionId;
            league.ScopeType = scope;
            league.RoundFromId = scope == LeagueScopeType.RoundRange ? dto.RoundFromId : null;
            league.RoundToId = scope == LeagueScopeType.RoundRange ? dto.RoundToId : null;
            league.IsActive = dto.IsActive;
            league.UpdatedAtUtc = DateTime.UtcNow;
            ApplyScoring(league, dto.UseGeneralScoring, dto.ExactScorePoints, dto.CorrectOutcomePoints, dto.IncorrectPoints,
                dto.PreferredPlayerEnabled, dto.PreferredPlayerPointsPerGoal, dto.PreferredPlayerPositions);

            await db.SaveChangesAsync();
            return Results.Ok(await ToDtoAsync(db, scoring, league));
        });

        group.MapGet("/{id:int}/dependencies", async (int id, PlayPredictDbContext db) =>
        {
            var league = await db.Leagues.FirstOrDefaultAsync(l => l.Id == id && l.LeagueType == LeagueType.Official);
            return league is null ? Results.NotFound() : Results.Ok(await GetDependenciesAsync(db, league));
        });

        group.MapDelete("/{id:int}", async (int id, PlayPredictDbContext db) =>
        {
            var league = await db.Leagues.FirstOrDefaultAsync(l => l.Id == id && l.LeagueType == LeagueType.Official);
            if (league is null) return Results.NotFound();
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.PredictionEvaluations.Where(e => e.LeagueId == league.Id).ExecuteDeleteAsync();
            await db.LeagueParticipants.Where(p => p.LeagueId == league.Id).ExecuteDeleteAsync();
            db.Leagues.Remove(league);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return Results.NoContent();
        });
    }

    private static async Task<(Dictionary<string, string[]> Errors, LeagueScopeType Scope)> ValidateAsync(
        PlayPredictDbContext db, string? name, string? description, int competitionId, int editionId,
        string scopeInput, int? roundFromId, int? roundToId)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(name)) errors["name"] = ["El nombre público de la Liga Oficial es obligatorio."];
        else if (name.Trim().Length > 150) errors["name"] = ["El nombre no puede superar los 150 caracteres."];
        if (description?.Trim().Length > 1000) errors["description"] = ["La descripción no puede superar los 1000 caracteres."];

        if (!Enum.TryParse<LeagueScopeType>(scopeInput, true, out var scope))
        {
            errors["scopeType"] = ["El alcance indicado no es válido."];
            return (errors, default);
        }

        var competition = await db.Competitions.FindAsync(competitionId);
        if (competition is null) errors["competitionId"] = ["La Competencia deportiva indicada no existe."];

        var edition = await db.Editions.FindAsync(editionId);
        if (edition is null) errors["editionId"] = ["La Edición indicada no existe."];
        else if (edition.CompetitionId != competitionId) errors["editionId"] = ["La Edición debe pertenecer a la Competencia deportiva elegida."];

        if (scope == LeagueScopeType.FullCompetition)
        {
            if (roundFromId is not null || roundToId is not null)
                errors["roundFromId"] = ["No corresponde indicar fechas para toda la Edición."];
            return (errors, scope);
        }

        if (roundFromId is null || roundToId is null)
        {
            errors["roundFromId"] = ["Debés indicar la Fecha inicial y final."];
            return (errors, scope);
        }

        var rounds = await db.Rounds
            .Where(round => round.Id == roundFromId || round.Id == roundToId)
            .ToListAsync();
        var from = rounds.FirstOrDefault(round => round.Id == roundFromId);
        var to = rounds.FirstOrDefault(round => round.Id == roundToId);
        if (from is null || to is null) errors["roundFromId"] = ["Alguna de las Fechas indicadas no existe."];
        else if (from.EditionId != editionId || to.EditionId != editionId) errors["roundFromId"] = ["Ambas Fechas deben pertenecer a la Edición elegida."];
        else if (from.Order > to.Order) errors["roundFromId"] = ["La Fecha inicial no puede ser posterior a la final."];

        return (errors, scope);
    }

    private static async Task<string> GenerateCodeAsync(PlayPredictDbContext db)
    {
        string code;
        do code = $"OFF-{Guid.NewGuid():N}"[..16].ToUpperInvariant();
        while (await db.Leagues.AnyAsync(league => league.InviteCode == code));
        return code;
    }

    private static async Task<AdminOfficialLeagueDto> ToDtoAsync(PlayPredictDbContext db, LeagueScoringService scoring, League league)
    {
        var competitionName = await db.Competitions.Where(c => c.Id == league.CompetitionId).Select(c => c.Name).FirstOrDefaultAsync()
            ?? $"Competencia no disponible (ID {league.CompetitionId})";
        var editionName = await db.Editions.Where(e => e.Id == league.EditionId).Select(e => e.Name).FirstOrDefaultAsync()
            ?? $"Edición no disponible (ID {league.EditionId})";
        var participants = await db.LeagueParticipants.CountAsync(p => p.LeagueId == league.Id);
        var scopedRounds = db.Rounds.Where(r => r.EditionId == league.EditionId);
        if (league.ScopeType == LeagueScopeType.RoundRange && league.RoundFromId.HasValue && league.RoundToId.HasValue)
        {
            var limits = await db.Rounds.Where(r => r.Id == league.RoundFromId || r.Id == league.RoundToId).Select(r => r.Order).ToListAsync();
            if (limits.Count == 2) scopedRounds = scopedRounds.Where(r => r.Order >= limits.Min() && r.Order <= limits.Max());
        }
        var roundsCount = await scopedRounds.CountAsync();
        var roundIds = scopedRounds.Select(r => r.Id);
        var matchesCount = await db.Matches.CountAsync(m => roundIds.Contains(m.RoundId));
        var fromName = league.RoundFromId is null ? null : await db.Rounds.Where(r => r.Id == league.RoundFromId).Select(r => r.Name).FirstOrDefaultAsync();
        var toName = league.RoundToId is null ? null : await db.Rounds.Where(r => r.Id == league.RoundToId).Select(r => r.Name).FirstOrDefaultAsync();

        var effective = (await scoring.GetEffectiveAsync(db, league.Id))!;
        return new AdminOfficialLeagueDto(league.Id, league.Name, league.Description, league.CompetitionId,
            competitionName, league.EditionId, editionName, league.ScopeType.ToString(), league.RoundFromId,
            league.RoundToId, fromName, toName, league.IsActive, participants, roundsCount, matchesCount,
            league.UseGeneralScoring, league.ExactScorePoints, league.CorrectOutcomePoints, league.IncorrectPoints,
            league.PreferredPlayerEnabled, league.PreferredPlayerPointsPerGoal, PlayerPositionCatalog.ToLabels(league.PreferredPlayerPositions),
            effective.ExactScorePoints, effective.CorrectOutcomePoints, effective.IncorrectPoints, effective.PreferredPlayerEnabled,
            effective.PreferredPlayerPointsPerGoal, PlayerPositionCatalog.ToLabels(effective.PreferredPlayerPositions), league.CreatedAtUtc, league.UpdatedAtUtc);
    }

    private static void ApplyScoring(League league, bool useGeneral, int exact, int correct, int incorrect, bool preferredEnabled, int pointsPerGoal, IEnumerable<string> positions)
    {
        league.UseGeneralScoring = useGeneral; league.ExactScorePoints = exact; league.CorrectOutcomePoints = correct; league.IncorrectPoints = incorrect;
        league.PreferredPlayerEnabled = preferredEnabled; league.PreferredPlayerPointsPerGoal = pointsPerGoal; league.PreferredPlayerPositions = PlayerPosition.None;
        foreach (var label in positions) if (PlayerPositionCatalog.TryParse(label, out var position)) league.PreferredPlayerPositions |= position;
    }

    private static void ValidateScoring(Dictionary<string,string[]> errors, int exact, int correct, int incorrect, int perGoal, IEnumerable<string> positions)
    {
        if (exact < 0 || correct < 0 || incorrect < 0 || perGoal < 0) errors["scoring"] = ["Los puntos deben ser mayores o iguales a cero."];
        if (positions.Any(x => !PlayerPositionCatalog.TryParse(x, out _))) errors["preferredPlayerPositions"] = ["Hay una posición no reconocida."];
    }

    private static async Task<OfficialLeagueDependenciesDto> GetDependenciesAsync(PlayPredictDbContext db, League league)
    {
        var matchIds = GetScopedMatchIds(db, league);
        return new OfficialLeagueDependenciesDto(
            await db.LeagueParticipants.CountAsync(p => p.LeagueId == league.Id),
            await db.Predictions.CountAsync(p => matchIds.Contains(p.MatchId)),
            await db.PredictionEvaluations.CountAsync(e => e.LeagueId == league.Id),
            await db.Matches.CountAsync(m => matchIds.Contains(m.Id) && m.HomeGoals != null && m.AwayGoals != null));
    }

    private static IQueryable<int> GetScopedMatchIds(PlayPredictDbContext db, League league)
    {
        var rounds = db.Rounds.Where(r => r.EditionId == league.EditionId);
        if (league.ScopeType == LeagueScopeType.RoundRange && league.RoundFromId.HasValue && league.RoundToId.HasValue)
        {
            var fromOrder = db.Rounds.Where(r => r.Id == league.RoundFromId.Value).Select(r => (int?)r.Order).FirstOrDefault();
            var toOrder = db.Rounds.Where(r => r.Id == league.RoundToId.Value).Select(r => (int?)r.Order).FirstOrDefault();
            if (fromOrder.HasValue && toOrder.HasValue) rounds = rounds.Where(r => r.Order >= fromOrder && r.Order <= toOrder);
        }
        return db.Matches.Where(m => rounds.Select(r => r.Id).Contains(m.RoundId)).Select(m => m.Id);
    }
}

public record OfficialLeagueDependenciesDto(int Participants, int Predictions, int Evaluations, int OfficialResults)
{
    public bool CanDelete => Participants == 0 && Evaluations == 0;
}
