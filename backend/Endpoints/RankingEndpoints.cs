using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class RankingEndpoints
{
    public static void MapRankingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rankings").WithTags("Rankings").RequireAuthorization();

        group.MapGet("/leagues/{leagueId:int}", async (int leagueId, ClaimsPrincipal principal,
            PlayPredictDbContext db, RankingService rankingService) =>
        {
            var accessError = await ValidateLeagueAccessAsync(leagueId, principal, db);
            if (accessError is not null) return accessError;
            return Results.Ok(await rankingService.GetLeagueRankingAsync(db, leagueId));
        });

        group.MapGet("/leagues/{leagueId:int}/prize-standings", async (int leagueId, ClaimsPrincipal principal,
            PlayPredictDbContext db, RankingService rankingService) =>
        {
            var accessError = await ValidateLeagueAccessAsync(leagueId, principal, db);
            if (accessError is not null) return accessError;
            return Results.Ok(await rankingService.GetLeagueAwardStandingsAsync(db, leagueId));
        });

        group.MapGet("/leagues/{leagueId:int}/rounds/{roundId:int}", async (int leagueId, int roundId,
            ClaimsPrincipal principal, PlayPredictDbContext db, RankingService rankingService) =>
        {
            var accessError = await ValidateLeagueAccessAsync(leagueId, principal, db);
            if (accessError is not null) return accessError;
            var league = await db.Leagues.FindAsync(leagueId);
            var round = await db.Rounds.FindAsync(roundId);
            if (round is null) return Results.NotFound();
            if (league is null || !await RoundIsInScopeAsync(db, league, round))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["roundId"] = ["La Fecha no pertenece al alcance de esta Liga."] });
            return Results.Ok(await rankingService.GetLeagueRoundRankingAsync(db, leagueId, roundId));
        });

        group.MapGet("/leagues/{leagueId:int}/rounds/{roundId:int}/prize-standings", async (int leagueId, int roundId,
            ClaimsPrincipal principal, PlayPredictDbContext db, RankingService rankingService) =>
        {
            var accessError = await ValidateLeagueAccessAsync(leagueId, principal, db);
            if (accessError is not null) return accessError;
            var league = await db.Leagues.FindAsync(leagueId);
            var round = await db.Rounds.FindAsync(roundId);
            if (round is null) return Results.NotFound();
            if (league is null || !await RoundIsInScopeAsync(db, league, round))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["roundId"] = ["La Fecha no pertenece al alcance de esta Liga."] });
            return Results.Ok(await rankingService.GetLeagueRoundAwardStandingsAsync(db, leagueId, roundId));
        });

        group.MapGet("/me/league-positions", async (ClaimsPrincipal principal, PlayPredictDbContext db, RankingService rankingService) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null) return Results.Unauthorized();
            return Results.Ok(await rankingService.GetUserLeaguePositionsAsync(db, user.Id));
        });
    }

    private static async Task<IResult?> ValidateLeagueAccessAsync(int leagueId, ClaimsPrincipal principal, PlayPredictDbContext db)
    {
        var league = await db.Leagues.FindAsync(leagueId);
        if (league is null) return Results.NotFound();
        var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
        if (user is null) return Results.Unauthorized();
        var canView = principal.IsInRole(RoleNames.Admin)
            || (league.LeagueType == LeagueType.Official && league.IsActive)
            || await db.LeagueParticipants.AnyAsync(p => p.LeagueId == leagueId && p.UserId == user.Id && p.LeftAtUtc == null);
        return canView ? null : Results.Forbid();
    }

    private static async Task<bool> RoundIsInScopeAsync(PlayPredictDbContext db, League league, Round round)
    {
        if (round.EditionId != league.EditionId) return false;
        if (league.ScopeType == LeagueScopeType.FullCompetition) return true;
        var bounds = await db.Rounds.Where(r => r.Id == league.RoundFromId || r.Id == league.RoundToId).ToListAsync();
        var from = bounds.FirstOrDefault(r => r.Id == league.RoundFromId);
        var to = bounds.FirstOrDefault(r => r.Id == league.RoundToId);
        return from is not null && to is not null && round.Order >= from.Order && round.Order <= to.Order;
    }
}
