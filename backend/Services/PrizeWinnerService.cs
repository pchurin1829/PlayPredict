using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Services;

// Responsabilidad única: dado un Premio, calcular sus ganadores actuales *provisionales*
// consultando el Ranking ya existente (RankingService). No persiste nada, no calcula
// puntos ni posiciones por su cuenta, y nunca representa una adjudicación definitiva.
public class PrizeWinnerService
{
    public async Task<List<PrizeWinnerUserDto>> GetCurrentWinnersAsync(
        PlayPredictDbContext db, RankingService rankingService, Prize prize)
    {
        switch (prize.AwardCriteria)
        {
            case PrizeAwardCriteria.Position:
            {
                if (prize.PositionFrom is null || prize.PositionTo is null)
                {
                    return new List<PrizeWinnerUserDto>();
                }

                var ranking = await GetRankingByScopeAsync(db, rankingService, prize);
                return ranking
                    .Where(r => r.Position >= prize.PositionFrom.Value && r.Position <= prize.PositionTo.Value)
                    .Select(ToWinner)
                    .ToList();
            }

            case PrizeAwardCriteria.RoundWinner:
            {
                if (prize.RoundId is null)
                {
                    return new List<PrizeWinnerUserDto>();
                }

                var ranking = await rankingService.GetRoundRankingAsync(db, prize.RoundId.Value);
                return ranking.Where(r => r.Position == 1).Select(ToWinner).ToList();
            }

            case PrizeAwardCriteria.MostExactScores:
            {
                var ranking = await GetRankingByScopeAsync(db, rankingService, prize);
                if (ranking.Count == 0)
                {
                    // Sin usuarios evaluados todavía: no se inventa ningún ganador.
                    return new List<PrizeWinnerUserDto>();
                }

                var maxExact = ranking.Max(r => r.ExactCount);
                return ranking.Where(r => r.ExactCount == maxExact).Select(ToWinner).ToList();
            }

            default:
                return new List<PrizeWinnerUserDto>();
        }
    }

    private static async Task<List<RankingEntryDto>> GetRankingByScopeAsync(
        PlayPredictDbContext db, RankingService rankingService, Prize prize)
    {
        return prize.ScopeType switch
        {
            PrizeScopeType.Edition => await rankingService.GetEditionRankingAsync(db, prize.EditionId),
            PrizeScopeType.Round when prize.RoundId is not null =>
                await rankingService.GetRoundRankingAsync(db, prize.RoundId.Value),
            // ScopeType.Special (o Round sin RoundId, que la validación ya impide) no tiene
            // todavía una fuente de Ranking definida: no se inventa ningún ganador.
            _ => new List<RankingEntryDto>()
        };
    }

    private static PrizeWinnerUserDto ToWinner(RankingEntryDto r) => new(r.UserId, r.FirstName, r.LastName);
}
