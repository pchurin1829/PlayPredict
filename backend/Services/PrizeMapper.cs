using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Services;

// Arma el DTO de lectura de un Premio, incluyendo el texto descriptivo ("Para: ...") y el
// ganador actual provisional. Compartido entre los endpoints administrativos y los públicos
// para que ambos muestren exactamente la misma información derivada.
public static class PrizeMapper
{
    public static async Task<PrizeDto> ToDtoAsync(
        Prize prize, PlayPredictDbContext db, RankingService rankingService, PrizeWinnerService winnerService)
    {
        var winners = await winnerService.GetCurrentWinnersAsync(db, rankingService, prize);

        return new PrizeDto(
            prize.Id,
            prize.EditionId,
            prize.Edition.Name,
            prize.RoundId,
            prize.Round?.Name,
            prize.Name,
            prize.Description,
            prize.PrizeType.ToString(),
            PrizeTypeLabel(prize.PrizeType),
            prize.ReferenceValue,
            prize.SponsorName,
            prize.ImageUrl,
            prize.ScopeType.ToString(),
            ScopeLabel(prize.ScopeType),
            prize.AwardCriteria.ToString(),
            CriteriaLabel(prize.AwardCriteria),
            prize.PositionFrom,
            prize.PositionTo,
            prize.Status.ToString(),
            StatusLabel(prize.Status),
            ForLabel(prize),
            winners,
            winners.Count > 0,
            prize.CreatedAtUtc,
            prize.UpdatedAtUtc);
    }

    private static string PrizeTypeLabel(PrizeType type) => type switch
    {
        PrizeType.Money => "Dinero",
        PrizeType.Product => "Producto",
        PrizeType.Service => "Servicio",
        PrizeType.Coupon => "Cupón",
        PrizeType.Ticket => "Entrada",
        PrizeType.Recognition => "Reconocimiento",
        PrizeType.Other => "Otro",
        _ => type.ToString()
    };

    private static string ScopeLabel(PrizeScopeType scope) => scope switch
    {
        PrizeScopeType.Edition => "Edición",
        PrizeScopeType.Round => "Fecha",
        PrizeScopeType.Special => "Especial",
        _ => scope.ToString()
    };

    private static string CriteriaLabel(PrizeAwardCriteria criteria) => criteria switch
    {
        PrizeAwardCriteria.Position => "Posición en el Ranking",
        PrizeAwardCriteria.RoundWinner => "Ganador de la Fecha",
        PrizeAwardCriteria.MostExactScores => "Mayor cantidad de marcadores exactos",
        _ => criteria.ToString()
    };

    private static string StatusLabel(PrizeStatus status) => status switch
    {
        PrizeStatus.Draft => "Borrador",
        PrizeStatus.Published => "Publicado",
        PrizeStatus.Closed => "Cerrado",
        PrizeStatus.Cancelled => "Cancelado",
        _ => status.ToString()
    };

    private static string ForLabel(Prize prize)
    {
        var scopeSuffix = prize.ScopeType == PrizeScopeType.Round && prize.Round is not null
            ? $" de la {prize.Round.Name}"
            : " del Ranking General";

        return prize.AwardCriteria switch
        {
            PrizeAwardCriteria.Position when prize.PositionFrom == prize.PositionTo =>
                $"Para: {prize.PositionFrom}.º puesto{scopeSuffix}",
            PrizeAwardCriteria.Position =>
                $"Para: puestos {prize.PositionFrom}.º a {prize.PositionTo}.º{scopeSuffix}",
            PrizeAwardCriteria.RoundWinner =>
                $"Para: Ganador de la {prize.Round?.Name}",
            PrizeAwardCriteria.MostExactScores =>
                $"Para: Mayor cantidad de marcadores exactos{scopeSuffix}",
            _ => "Para: a definir"
        };
    }
}
