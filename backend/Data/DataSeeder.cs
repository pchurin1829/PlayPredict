using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Data;

public static class DataSeeder
{
    private const string CompetitionName = "Liga Profesional";
    private const string EditionName = "Clausura 2026";
    private const string RoundName = "Fecha 1";

    public static async Task SeedAsync(PlayPredictDbContext db)
    {
        var competition = await db.Competitions
            .FirstOrDefaultAsync(c => c.Name == CompetitionName);

        if (competition is not null)
        {
            return;
        }

        var now = DateTime.UtcNow;

        competition = new Competition
        {
            Name = CompetitionName,
            Description = "Competencia de demostración generada por el seed de desarrollo.",
            Sport = "Fútbol",
            IsActive = true,
            CreatedAtUtc = now
        };

        var edition = new Edition
        {
            Competition = competition,
            Name = EditionName,
            StartDateUtc = now,
            Status = EditionStatus.Active,
            CreatedAtUtc = now
        };

        var round = new Round
        {
            Edition = edition,
            Name = RoundName,
            Order = 1,
            StartDateUtc = now
        };

        var matches = new[]
        {
            new Match
            {
                Round = round,
                ParticipantHome = "Equipo A",
                ParticipantAway = "Equipo B",
                StartsAtUtc = now.AddDays(1),
                Status = MatchStatus.Scheduled,
                CreatedAtUtc = now
            },
            new Match
            {
                Round = round,
                ParticipantHome = "Equipo C",
                ParticipantAway = "Equipo D",
                StartsAtUtc = now.AddDays(1).AddHours(2),
                Status = MatchStatus.Scheduled,
                CreatedAtUtc = now
            },
            new Match
            {
                Round = round,
                ParticipantHome = "Equipo E",
                ParticipantAway = "Equipo F",
                StartsAtUtc = now.AddDays(2),
                Status = MatchStatus.Scheduled,
                CreatedAtUtc = now
            }
        };

        db.Competitions.Add(competition);
        db.Editions.Add(edition);
        db.Rounds.Add(round);
        db.Matches.AddRange(matches);

        await db.SaveChangesAsync();
    }
}
