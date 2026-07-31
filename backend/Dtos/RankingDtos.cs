namespace PlayPredict.Api.Dtos;

public record RankingEntryDto(
    int Position,
    int UserId,
    string FirstName,
    string LastName,
    int Points,
    int ExactCount,
    int CorrectCount,
    int IncorrectCount,
    int EvaluatedCount);
