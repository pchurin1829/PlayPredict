using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Imports;
using Xunit;

namespace PlayPredict.Api.Tests;

public class MatchImportPreviewServiceTests
{
    private static readonly DateOnly Day = new(2026, 8, 28);
    private static readonly TimeOnly Time = new(21, 30);
    private static readonly DateTime ExpectedUtc = new(2026, 8, 29, 0, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Classifies_new_match_and_flags_the_round_as_new()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        await SeedTeam(db, "Boca Juniors");
        await SeedTeam(db, "River Plate");

        var row = Assert.Single((await Preview(db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"))).Matches);

        Assert.Equal(MatchImportClassification.MatchCreate, row.Classification);
        Assert.True(row.RoundIsNew);
        Assert.Null(row.RoundId);
        Assert.Equal(ExpectedUtc, row.StartsAtUtc);
    }

    [Fact]
    public async Task Classifies_unchanged_existing_match()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        var round = await SeedRound(db, edition.Id, 7);
        var home = await SeedTeam(db, "Boca Juniors");
        var away = await SeedTeam(db, "River Plate");
        await SeedMatch(db, round.Id, home.Id, away.Id, ExpectedUtc);

        var row = Assert.Single((await Preview(db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"))).Matches);

        Assert.Equal(MatchImportClassification.MatchUnchanged, row.Classification);
        Assert.False(row.RoundIsNew);
        Assert.Empty(row.ProposedChanges);
    }

    [Fact]
    public async Task Classifies_update_when_kickoff_time_changes()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        var round = await SeedRound(db, edition.Id, 7);
        var home = await SeedTeam(db, "Boca Juniors");
        var away = await SeedTeam(db, "River Plate");
        await SeedMatch(db, round.Id, home.Id, away.Id, ExpectedUtc.AddHours(1));

        var row = Assert.Single((await Preview(db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"))).Matches);

        Assert.Equal(MatchImportClassification.MatchUpdate, row.Classification);
        var change = Assert.Single(row.ProposedChanges);
        Assert.Equal("StartsAtUtc", change.Field);
    }

    [Fact]
    public async Task Classifies_update_when_status_changes()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        var round = await SeedRound(db, edition.Id, 7);
        var home = await SeedTeam(db, "Boca Juniors");
        var away = await SeedTeam(db, "River Plate");
        await SeedMatch(db, round.Id, home.Id, away.Id, ExpectedUtc, MatchStatus.Suspended);

        var row = Assert.Single((await Preview(db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"))).Matches);

        Assert.Equal(MatchImportClassification.MatchUpdate, row.Classification);
        Assert.Contains(row.ProposedChanges, change => change.Field == "Status");
    }

    [Fact]
    public async Task Reports_unresolved_team_when_home_does_not_exist()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        await SeedTeam(db, "River Plate");

        var row = Assert.Single((await Preview(db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"))).Matches);

        Assert.Equal(MatchImportClassification.UnresolvedTeamError, row.Classification);
    }

    [Fact]
    public async Task Reports_ambiguous_team_after_normalization()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        await SeedTeam(db, "Boca Juniors");
        await SeedTeam(db, " boca   juniors ");
        await SeedTeam(db, "River Plate");

        var row = Assert.Single((await Preview(db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"))).Matches);

        Assert.Equal(MatchImportClassification.UnresolvedTeamError, row.Classification);
    }

    [Fact]
    public async Task Reports_home_equals_away_as_error()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        await SeedTeam(db, "Boca Juniors");

        var row = Assert.Single((await Preview(db, edition.Id, MatchRow(7, "Boca Juniors", "Boca Juniors"))).Matches);

        Assert.Equal(MatchImportClassification.UnresolvedTeamError, row.Classification);
    }

    [Fact]
    public async Task Reports_edition_not_found_without_reading_rows()
    {
        await using var db = CreateDb();

        var result = await Preview(db, editionId: 999, MatchRow(7, "Boca Juniors", "River Plate"));

        Assert.Contains(result.Issues, issue => issue.Code == "EDITION_NOT_FOUND");
        Assert.Empty(result.Matches);
        Assert.False(result.CanConfirm);
    }

    [Fact]
    public async Task Reports_duplicate_match_row_within_the_same_file()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        await SeedTeam(db, "Boca Juniors");
        await SeedTeam(db, "River Plate");

        var result = await Preview(db, edition.Id,
            MatchRow(7, "Boca Juniors", "River Plate", row: 2),
            MatchRow(7, "Boca Juniors", "River Plate", row: 3));

        Assert.Equal(MatchImportClassification.MatchCreate, result.Matches[0].Classification);
        Assert.Equal(MatchImportClassification.DuplicateMatchRowError, result.Matches[1].Classification);
        Assert.False(result.CanConfirm);
    }

    [Fact]
    public async Task Finished_match_is_never_touched_even_if_row_proposes_changes()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        var round = await SeedRound(db, edition.Id, 7);
        var home = await SeedTeam(db, "Boca Juniors");
        var away = await SeedTeam(db, "River Plate");
        await SeedMatch(db, round.Id, home.Id, away.Id, ExpectedUtc.AddHours(2), MatchStatus.Finished);

        var row = Assert.Single((await Preview(db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"))).Matches);

        Assert.Equal(MatchImportClassification.MatchFinishedConflict, row.Classification);
    }

    [Fact]
    public async Task Team_already_playing_in_the_round_blocks_as_conflict_and_mentions_predictions()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        var round = await SeedRound(db, edition.Id, 7);
        var home = await SeedTeam(db, "Boca Juniors");
        var away = await SeedTeam(db, "River Plate");
        var thirdTeam = await SeedTeam(db, "Racing Club");
        var existing = await SeedMatch(db, round.Id, home.Id, thirdTeam.Id, ExpectedUtc);
        db.Predictions.Add(new Prediction { MatchId = existing.Id, UserId = 1, PredictedHomeScore = 1, PredictedAwayScore = 0, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        // El archivo ahora propone "Boca Juniors vs River Plate" en la misma Fecha 7, pero Boca
        // ya está jugando contra Racing en esa Fecha: no puede resolverse silenciosamente.
        var row = Assert.Single((await Preview(db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"))).Matches);

        Assert.Equal(MatchImportClassification.MatchTeamChangeConflict, row.Classification);
        Assert.Contains("pronósticos", row.Message);
    }

    [Fact]
    public async Task Same_pairing_in_a_different_existing_round_is_a_conflict_not_an_automatic_create()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        var roundOne = await SeedRound(db, edition.Id, 1);
        var home = await SeedTeam(db, "Boca Juniors");
        var away = await SeedTeam(db, "River Plate");
        await SeedMatch(db, roundOne.Id, home.Id, away.Id, ExpectedUtc.AddDays(-100));

        // El archivo ahora dice que el mismo enfrentamiento es en la Fecha 7: podría ser una
        // reprogramación o un segundo enfrentamiento legítimo. No se decide automáticamente.
        var row = Assert.Single((await Preview(db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"))).Matches);

        Assert.Equal(MatchImportClassification.MatchRoundChangeConflict, row.Classification);
    }

    [Fact]
    public async Task Same_pairing_across_two_new_rounds_in_the_same_file_is_allowed()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        await SeedTeam(db, "Boca Juniors");
        await SeedTeam(db, "River Plate");

        // Ninguno de los dos partidos existe todavía en DB: un segundo enfrentamiento legítimo
        // introducido en el mismo archivo no debe bloquearse entre sí.
        var result = await Preview(db, edition.Id,
            MatchRow(1, "Boca Juniors", "River Plate", row: 2),
            MatchRow(30, "Boca Juniors", "River Plate", row: 3));

        Assert.All(result.Matches, row => Assert.Equal(MatchImportClassification.MatchCreate, row.Classification));
        Assert.True(result.CanConfirm);
    }

    [Fact]
    public async Task CanConfirm_is_false_when_any_row_is_error_or_conflict()
    {
        await using var db = CreateDb();
        var edition = await SeedEdition(db);
        await SeedTeam(db, "Boca Juniors");

        var result = await Preview(db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"));

        Assert.False(result.CanConfirm);
        Assert.Equal(1, result.Summary.Errors);
    }

    private static PlayPredictDbContext CreateDb() => new(new DbContextOptionsBuilder<PlayPredictDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<Edition> SeedEdition(PlayPredictDbContext db)
    {
        var edition = new Edition { CompetitionId = 1, Name = "Clausura 2026", StartDateUtc = DateTime.UtcNow, Status = EditionStatus.Active, CreatedAtUtc = DateTime.UtcNow };
        db.Editions.Add(edition);
        await db.SaveChangesAsync();
        return edition;
    }

    private static async Task<Round> SeedRound(PlayPredictDbContext db, int editionId, int order)
    {
        var round = new Round { EditionId = editionId, Name = $"Fecha {order}", Order = order };
        db.Rounds.Add(round);
        await db.SaveChangesAsync();
        return round;
    }

    private static async Task<Team> SeedTeam(PlayPredictDbContext db, string name)
    {
        var team = new Team { Name = name, ShortName = name.Length > 4 ? name[..4] : name, Sport = "Fútbol", Active = true };
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return team;
    }

    private static async Task<Match> SeedMatch(PlayPredictDbContext db, int roundId, int homeId, int awayId, DateTime startsAtUtc, MatchStatus status = MatchStatus.Scheduled)
    {
        var match = new Match
        {
            RoundId = roundId, HomeTeamId = homeId, AwayTeamId = awayId,
            ParticipantHome = "Home", ParticipantAway = "Away",
            StartsAtUtc = startsAtUtc, Status = status, CreatedAtUtc = DateTime.UtcNow
        };
        db.Matches.Add(match);
        await db.SaveChangesAsync();
        return match;
    }

    private static Task<MatchImportPreviewResult> Preview(PlayPredictDbContext db, int editionId, params ImportMatchRow[] rows) =>
        new MatchImportPreviewService(db).PreviewAsync(new([], [], rows, []), editionId);

    private static ImportMatchRow MatchRow(int roundNumber, string home, string away,
        ImportMatchStatus status = ImportMatchStatus.Scheduled, int row = 2) => new(
        row, roundNumber.ToString(), Day.ToString("yyyy-MM-dd"), Time.ToString("HH:mm"), home, away, status.ToString(),
        roundNumber, Day, Time, home, away,
        SpreadsheetTextNormalizer.Normalize(home), SpreadsheetTextNormalizer.Normalize(away), status);
}
