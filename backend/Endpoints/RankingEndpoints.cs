using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
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
    }
}
