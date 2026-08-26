using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class RankingEndpoints
{
    public static void MapRankingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rankings").WithTags("Rankings").RequireAuthorization();

        group.MapGet("/editions/{editionId:int}", async (int editionId, PlayPredictDbContext db, RankingService rankingService) =>
        {
            var editionExists = await db.Editions.AnyAsync(e => e.Id == editionId);
            if (!editionExists)
            {
                return Results.NotFound();
            }

            var ranking = await rankingService.GetEditionRankingAsync(db, editionId);
            return Results.Ok(ranking);
        });

        group.MapGet("/rounds/{roundId:int}", async (int roundId, PlayPredictDbContext db, RankingService rankingService) =>
        {
            var roundExists = await db.Rounds.AnyAsync(r => r.Id == roundId);
            if (!roundExists)
            {
                return Results.NotFound();
            }

            var ranking = await rankingService.GetRoundRankingAsync(db, roundId);
            return Results.Ok(ranking);
        });

        group.MapGet("/leagues/{leagueId:int}", async (int leagueId, ClaimsPrincipal principal, PlayPredictDbContext db, RankingService rankingService) =>
        {
            var league = await db.Leagues.FindAsync(leagueId);
            if (league is null)
            {
                return Results.NotFound();
            }

            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null) return Results.Unauthorized();
            var canView = principal.IsInRole(RoleNames.Admin)
                || (league.LeagueType == LeagueType.Official && league.IsActive)
                || await db.LeagueParticipants.AnyAsync(participant => participant.LeagueId == leagueId && participant.UserId == user.Id);
            if (!canView) return Results.Forbid();

            var ranking = await rankingService.GetLeagueRankingAsync(db, leagueId);
            return Results.Ok(ranking);
        });
    }
}
