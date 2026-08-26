using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Services;

// Responsabilidad única: consultar evaluaciones ya calculadas (Sprint 5) y producir
// posiciones. No calcula puntos, no persiste nada, no conoce reglas de puntuación.
public class RankingService
{
    public Task<List<RankingEntryDto>> GetEditionRankingAsync(PlayPredictDbContext db, int editionId)
    {
        var rows = db.PredictionEvaluations
            .Where(e => e.Prediction.Match.Round.EditionId == editionId)
            .Select(e => new EvaluationRow(
                e.Prediction.UserId,
                e.Prediction.User.FirstName,
                e.Prediction.User.LastName,
                e.Points,
                e.EvaluationType));

        return BuildRankingAsync(rows);
    }

    public Task<List<RankingEntryDto>> GetRoundRankingAsync(PlayPredictDbContext db, int roundId)
    {
        var rows = db.PredictionEvaluations
            .Where(e => e.Prediction.Match.RoundId == roundId)
            .Select(e => new EvaluationRow(
                e.Prediction.UserId,
                e.Prediction.User.FirstName,
                e.Prediction.User.LastName,
                e.Points,
                e.EvaluationType));

        return BuildRankingAsync(rows);
    }

    public async Task<List<RankingEntryDto>> GetLeagueRankingAsync(PlayPredictDbContext db, int leagueId)
    {
        var rows = db.PredictionEvaluations
            .Where(e => e.Prediction.LeagueId == leagueId)
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
                participant.User.LastName
            })
            .ToListAsync();
        foreach (var participant in participants.Where(participant => list.All(row => row.UserId != participant.UserId)))
        {
            list.Add(new EvaluationRow(participant.UserId, participant.FirstName, participant.LastName, 0, null, false));
        }

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
                entry.Points, entry.ExactCount, entry.CorrectCount, entry.IncorrectCount, entry.EvaluatedCount));
        }

        return result;
    }

    private record EvaluationRow(
        int UserId, string FirstName, string LastName, int Points,
        EvaluationType? EvaluationType, bool IsEvaluated = true);
}
