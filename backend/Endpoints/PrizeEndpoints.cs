using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class PrizeEndpoints
{
    public static void MapPrizeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/prizes").WithTags("Prizes").RequireAuthorization();

        // Los usuarios solo pueden ver Premios Publicados o Cerrados: nunca Borrador ni Cancelados.
        group.MapGet("/editions/{editionId:int}", async (int editionId, PlayPredictDbContext db, RankingService rankingService, PrizeWinnerService winnerService) =>
        {
            var editionExists = await db.Editions.AnyAsync(e => e.Id == editionId);
            if (!editionExists)
            {
                return Results.NotFound();
            }

            var prizes = await db.Prizes
                .Include(p => p.Edition)
                .Include(p => p.Round)
                .Where(p => p.EditionId == editionId && (p.Status == PrizeStatus.Published || p.Status == PrizeStatus.Closed))
                .OrderBy(p => p.Id)
                .ToListAsync();

            var dtos = new List<Dtos.PrizeDto>(prizes.Count);
            foreach (var prize in prizes)
            {
                dtos.Add(await PrizeMapper.ToDtoAsync(prize, db, rankingService, winnerService));
            }

            return Results.Ok(dtos);
        });

        group.MapGet("/rounds/{roundId:int}", async (int roundId, PlayPredictDbContext db, RankingService rankingService, PrizeWinnerService winnerService) =>
        {
            var roundExists = await db.Rounds.AnyAsync(r => r.Id == roundId);
            if (!roundExists)
            {
                return Results.NotFound();
            }

            var prizes = await db.Prizes
                .Include(p => p.Edition)
                .Include(p => p.Round)
                .Where(p => p.RoundId == roundId && (p.Status == PrizeStatus.Published || p.Status == PrizeStatus.Closed))
                .OrderBy(p => p.Id)
                .ToListAsync();

            var dtos = new List<Dtos.PrizeDto>(prizes.Count);
            foreach (var prize in prizes)
            {
                dtos.Add(await PrizeMapper.ToDtoAsync(prize, db, rankingService, winnerService));
            }

            return Results.Ok(dtos);
        });

        group.MapGet("/{id:int}", async (int id, PlayPredictDbContext db, RankingService rankingService, PrizeWinnerService winnerService) =>
        {
            var prize = await db.Prizes
                .Include(p => p.Edition)
                .Include(p => p.Round)
                .FirstOrDefaultAsync(p => p.Id == id
                    && (p.Status == PrizeStatus.Published || p.Status == PrizeStatus.Closed));

            if (prize is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(await PrizeMapper.ToDtoAsync(prize, db, rankingService, winnerService));
        });
    }
}
