using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Services;

// Prize todavía no identifica una Liga. Hasta que el contrato incorpore LeagueId,
// calcular ganadores por Edition/Round mezclaría Ligas y queda deliberadamente desactivado.
public class PrizeWinnerService
{
    public Task<List<PrizeWinnerUserDto>> GetCurrentWinnersAsync(
        PlayPredictDbContext db, RankingService rankingService, Prize prize) =>
        Task.FromResult(new List<PrizeWinnerUserDto>());
}
