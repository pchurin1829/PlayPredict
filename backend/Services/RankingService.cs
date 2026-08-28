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

    public async Task<List<AwardStandingDto>> GetLeagueAwardStandingsAsync(PlayPredictDbContext db, int leagueId)
        => await GetLeagueAwardStandingsCoreAsync(db, leagueId, null);

    public async Task<List<AwardStandingDto>> GetLeagueRoundAwardStandingsAsync(PlayPredictDbContext db, int leagueId, int roundId)
        => await GetLeagueAwardStandingsCoreAsync(db, leagueId, roundId);

    private static async Task<List<RankingEntryDto>> GetLeagueRankingCoreAsync(PlayPredictDbContext db, int leagueId, int? roundId)
    {
        var rows = db.PredictionEvaluations
            .Where(e => e.LeagueId == leagueId && (!roundId.HasValue || e.Prediction.Match.RoundId == roundId.Value))
            .Select(e => new EvaluationRow(
                e.Prediction.UserId,
                e.Prediction.User.FirstName,
                e.Prediction.User.LastName,
                e.Points,
                e.EvaluationType,
                Math.Abs(e.Prediction.PredictedHomeScore - e.OfficialHomeScore)
                    + Math.Abs(e.Prediction.PredictedAwayScore - e.OfficialAwayScore),
                e.PreferredPlayerPoints));

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
            list.Add(new EvaluationRow(participant.UserId, participant.FirstName, participant.LastName, 0, null, 0, 0, false, participant.IsActive));
        }

        list = list.Select(row => row with { IsActiveParticipant = participantStates.FirstOrDefault(p => p.UserId == row.UserId)?.IsActive ?? false }).ToList();

        return BuildRanking(list);
    }

    private static async Task<List<AwardStandingDto>> GetLeagueAwardStandingsCoreAsync(PlayPredictDbContext db, int leagueId, int? roundId)
    {
        var rows = await LoadEvaluationRowsAsync(db, leagueId, roundId);
        return BuildAwardStandings(rows);
    }

    private static async Task<List<EvaluationRow>> LoadEvaluationRowsAsync(PlayPredictDbContext db, int leagueId, int? roundId)
    {
        var rows = await db.PredictionEvaluations
            .Where(e => e.LeagueId == leagueId && (!roundId.HasValue || e.Prediction.Match.RoundId == roundId.Value))
            .Select(e => new EvaluationRow(
                e.Prediction.UserId, e.Prediction.User.FirstName, e.Prediction.User.LastName,
                e.Points, e.EvaluationType,
                Math.Abs(e.Prediction.PredictedHomeScore - e.OfficialHomeScore)
                    + Math.Abs(e.Prediction.PredictedAwayScore - e.OfficialAwayScore),
                e.PreferredPlayerPoints))
            .ToListAsync();
        var participants = await db.LeagueParticipants
            .Where(participant => participant.LeagueId == leagueId)
            .Select(participant => new { participant.UserId, participant.User.FirstName, participant.User.LastName, IsActive = participant.LeftAtUtc == null })
            .ToListAsync();
        var states = participants.GroupBy(item => new { item.UserId, item.FirstName, item.LastName })
            .Select(group => new { group.Key.UserId, group.Key.FirstName, group.Key.LastName, IsActive = group.Any(item => item.IsActive) }).ToList();
        foreach (var participant in states.Where(participant => rows.All(row => row.UserId != participant.UserId)))
            rows.Add(new EvaluationRow(participant.UserId, participant.FirstName, participant.LastName, 0, null, 0, 0, false, participant.IsActive));
        return rows.Select(row => row with { IsActiveParticipant = states.FirstOrDefault(item => item.UserId == row.UserId)?.IsActive ?? false }).ToList();
    }

    public async Task<List<UserLeaguePositionDto>> GetUserLeaguePositionsAsync(PlayPredictDbContext db, int userId)
    {
        var leagues = await db.LeagueParticipants
            .Where(participant => participant.UserId == userId && participant.LeftAtUtc == null && participant.League.IsActive)
            .Select(participant => new { participant.LeagueId, participant.League.Name, participant.League.CreatedAtUtc })
            .Distinct().OrderByDescending(item => item.CreatedAtUtc).ToListAsync();
        var result = new List<UserLeaguePositionDto>(leagues.Count);
        foreach (var league in leagues)
        {
            var entry = (await GetLeagueRankingCoreAsync(db, league.LeagueId, null)).FirstOrDefault(item => item.UserId == userId);
            if (entry is not null)
                result.Add(new UserLeaguePositionDto(league.LeagueId, league.Name, entry.Position, entry.SharedCount, entry.Points));
        }
        return result;
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

        var orderedEntries = grouped.Select(entry => new RankingEntryDto(
            0, entry.UserId, entry.FirstName, entry.LastName,
            entry.Points, entry.ExactCount, entry.CorrectCount, entry.IncorrectCount, entry.EvaluatedCount,
            0, gActive(list, entry.UserId))).ToList();

        return ApplyDensePositions(orderedEntries);
    }

    internal static List<RankingEntryDto> ApplyDensePositions(IReadOnlyList<RankingEntryDto> orderedEntries)
    {
        var usersPerPoints = orderedEntries.GroupBy(entry => entry.Points)
            .ToDictionary(group => group.Key, group => group.Count());
        var result = new List<RankingEntryDto>(orderedEntries.Count);
        var densePosition = 0;
        int? previousPoints = null;

        foreach (var entry in orderedEntries)
        {
            if (previousPoints != entry.Points)
            {
                densePosition++;
                previousPoints = entry.Points;
            }

            result.Add(entry with
            {
                Position = densePosition,
                SharedCount = usersPerPoints[entry.Points] - 1
            });
        }

        return result;
    }

    private static List<AwardStandingDto> BuildAwardStandings(List<EvaluationRow> rows)
    {
        var aggregated = rows.GroupBy(row => new { row.UserId, row.FirstName, row.LastName })
            .Select(group => new AwardStandingDto(
                null, 0, 0, false, group.Key.UserId, group.Key.FirstName, group.Key.LastName,
                group.Sum(item => item.Points),
                group.Count(item => item.IsEvaluated && item.EvaluationType == EvaluationType.ExactScore),
                group.Count(item => item.IsEvaluated && item.EvaluationType == EvaluationType.CorrectOutcome),
                group.Count(item => item.IsEvaluated && item.EvaluationType == EvaluationType.Incorrect),
                group.Count(item => item.IsEvaluated),
                group.Sum(item => item.ScoreError),
                group.Sum(item => item.PreferredPlayerPoints),
                group.Any(item => item.IsActiveParticipant))).ToList();
        return ApplyAwardPolicy(aggregated);
    }

    internal static List<AwardStandingDto> ApplyAwardPolicy(IReadOnlyList<AwardStandingDto> aggregated)
    {
        var ordered = aggregated
            .OrderByDescending(item => item.Points)
            .ThenByDescending(item => item.ExactCount)
            .ThenByDescending(item => item.CorrectCount)
            .ThenBy(item => item.AccumulatedScoreError)
            .ThenByDescending(item => item.PreferredPlayerPoints)
            .ToList();

        var result = new List<AwardStandingDto>(ordered.Count);
        var position = 1;
        foreach (var group in ordered.GroupBy(item => new
        {
            item.Points, item.ExactCount, item.CorrectCount,
            item.AccumulatedScoreError, item.PreferredPlayerPoints
        }))
        {
            var tied = group.Count() > 1;
            var from = position;
            var to = position + group.Count() - 1;
            foreach (var entry in group.OrderBy(item => item.LastName).ThenBy(item => item.FirstName))
                result.Add(entry with { Position = tied ? null : position, PositionFrom = from, PositionTo = to, TieBreakPending = tied });
            position = to + 1;
        }
        return result;
    }

    private record EvaluationRow(
        int UserId, string FirstName, string LastName, int Points,
        EvaluationType? EvaluationType, int ScoreError = 0, int PreferredPlayerPoints = 0,
        bool IsEvaluated = true, bool IsActiveParticipant = false);

    private static bool gActive(List<EvaluationRow> rows, int userId) => rows.Any(row => row.UserId == userId && row.IsActiveParticipant);
}
