namespace PlayPredict.Api.Dtos;

public record PrizeWinnerUserDto(int UserId, string FirstName, string LastName);

// DTO de lectura único, usado tanto por los endpoints administrativos como por los
// públicos/de usuario. El backend arma el texto descriptivo ("forLabel") y el ganador
// actual provisional: el frontend nunca calcula nada, solo muestra lo que llega.
public record PrizeDto(
    int Id,
    int EditionId,
    string EditionName,
    int? RoundId,
    string? RoundName,
    string Name,
    string? Description,
    string PrizeType,
    string PrizeTypeLabel,
    string? ReferenceValue,
    string? SponsorName,
    string? ImageUrl,
    string ScopeType,
    string ScopeLabel,
    string AwardCriteria,
    string CriteriaLabel,
    int? PositionFrom,
    int? PositionTo,
    string Status,
    string StatusLabel,
    string ForLabel,
    List<PrizeWinnerUserDto> CurrentWinners,
    bool HasProvisionalWinner,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public record CreatePrizeDto(
    int EditionId,
    int? RoundId,
    string Name,
    string? Description,
    string PrizeType,
    string? ReferenceValue,
    string? SponsorName,
    string? ImageUrl,
    string ScopeType,
    string AwardCriteria,
    int? PositionFrom,
    int? PositionTo);

public record UpdatePrizeDto(
    int? RoundId,
    string Name,
    string? Description,
    string PrizeType,
    string? ReferenceValue,
    string? SponsorName,
    string? ImageUrl,
    string ScopeType,
    string AwardCriteria,
    int? PositionFrom,
    int? PositionTo);
