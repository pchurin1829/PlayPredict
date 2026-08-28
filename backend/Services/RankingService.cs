using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Services;

// Responsabilidad única: consultar evaluaciones ya calculadas (Sprint 5) y producir
// posiciones. No calcula puntos, no persiste nada, no conoce reglas de puntuación.
public class RankingService
{
    public async Task<List<RankingEntryDto>> GetLeagueRankingAsync(PlayPredictDbContext db, int leagueId)
        => await GetLeagueRankingCoreAsync(db, leagueId, null);

    public async Task<List<RankingEntryDto>> GetLeagueRoundRankingAsync(PlayPredictDbContext db, int leagueId, int roundId)
        => await GetLeagueRankingCoreAsync(db, leagueId, roundId);

    private static async Task<List<RankingEntryDto>> GetLeagueRankingCoreAsync(PlayPredictDbContext db, int leagueId, int? roundId)
    {
        var rows = db.PredictionEvaluations
            .Where(e => e.LeagueId == leagueId && (!roundId.HasValue || e.Prediction.Match.RoundId == roundId.Value))
            .Select(e => new EvaluationRow(
                e.Prediction.UserId,
                e.Prediction.User.FirstName,
                e.Prediction.User.LastName,
                e.Points,
                e.EvaluationType));

        var list = await rows.ToListAsync();
        var participants = await db.LeagueParticipants
            .Where(participant => participant.LeagueId == leagueId)
            .Select(participant => new
            {
                participant.UserId,
                participant.User.FirstName,
                participant.User.LastName,
                IsActive = participant.LeftAtUtc == null
            })
            .ToListAsync();
        var participantStates = participants.GroupBy(p => new { p.UserId, p.FirstName, p.LastName })
            .Select(group => new { group.Key.UserId, group.Key.FirstName, group.Key.LastName, IsActive = group.Any(p => p.IsActive) })
            .ToList();
        foreach (var participant in participantStates.Where(participant => list.All(row => row.UserId != participant.UserId)))
        {
            list.Add(new EvaluationRow(participant.UserId, participant.FirstName, participant.LastName, 0, null, false, participant.IsActive));
        }

        list = list.Select(row => row with { IsActiveParticipant = participantStates.FirstOrDefault(p => p.UserId == row.UserId)?.IsActive ?? false }).ToList();

        return BuildRanking(list);
    }

    private static async Task<List<RankingEntryDto>> BuildRankingAsync(IQueryable<EvaluationRow> rows)
    {
        var list = await rows.ToListAsync();
        return BuildRanking(list);
    }

    private static List<RankingEntryDto> BuildRanking(List<EvaluationRow> list)
    {
        // Edition/Fecha reciben sólo evaluaciones. League agrega antes una fila neutra
        // para cada participante sin evaluaciones, de modo que figure con cero puntos.
        var grouped = list
            .GroupBy(r => new { r.UserId, r.FirstName, r.LastName })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.FirstName,
                g.Key.LastName,
                Points = g.Sum(x => x.Points),
                ExactCount = g.Count(x => x.IsEvaluated && x.EvaluationType == EvaluationType.ExactScore),
                CorrectCount = g.Count(x => x.IsEvaluated && x.EvaluationType == EvaluationType.CorrectOutcome),
                IncorrectCount = g.Count(x => x.IsEvaluated && x.EvaluationType == EvaluationType.Incorrect),
                EvaluatedCount = g.Count(x => x.IsEvaluated)
            })
            // Orden deportivo: puntos > exactos > correctos > incorrectos (menor es mejor).
            // El apellido/nombre solo desempata visualmente entre usuarios ya empatados.
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.ExactCount)
            .ThenByDescending(x => x.CorrectCount)
            .ThenBy(x => x.IncorrectCount)
            .ThenBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ToList();

        var result = new List<RankingEntryDto>(grouped.Count);
        var position = 0;
        var sharedPosition = 0;
        (int Points, int Exact, int Correct, int Incorrect)? lastKey = null;

        foreach (var entry in grouped)
        {
            position++;
            var key = (entry.Points, entry.ExactCount, entry.CorrectCount, entry.IncorrectCount);

            // Posición compartida: los criterios deportivos definen el número de posición;
            // si son idénticos a la fila anterior, comparten la misma posición.
            if (lastKey is null || key != lastKey.Value)
            {
                sharedPosition = position;
                lastKey = key;
            }

            result.Add(new RankingEntryDto(
                sharedPosition, entry.UserId, entry.FirstName, entry.LastName,
                entry.Points, entry.ExactCount, entry.CorrectCount, entry.IncorrectCount, entry.EvaluatedCount,
                gActive(list, entry.UserId)));
        }

        return result;
    }

    private record EvaluationRow(
        int UserId, string FirstName, string LastName, int Points,
        EvaluationType? EvaluationType, bool IsEvaluated = true, bool IsActiveParticipant = false);

    private static bool gActive(List<EvaluationRow> rows, int userId) => rows.Any(row => row.UserId == userId && row.IsActiveParticipant);
}
