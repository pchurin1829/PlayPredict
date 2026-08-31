using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Imports;
using Xunit;

namespace PlayPredict.Api.Tests;

public class MatchImportConfirmationServiceTests
{
    private static readonly DateOnly Day = new(2026, 8, 28);
    private static readonly TimeOnly Time = new(21, 30);
    private static readonly DateTime ExpectedUtc = new(2026, 8, 29, 0, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Argentina_local_time_converts_to_the_exact_expected_utc_instant()
    {
        await using var database = await SqliteDatabase.Create();
        var edition = await SeedEdition(database.Db);
        await SeedTeam(database.Db, "Boca Juniors");
        await SeedTeam(database.Db, "River Plate");

        await Confirm(database.Db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"));

        var match = await database.Db.Matches.AsNoTracking().SingleAsync();
        Assert.Equal(new DateTime(2026, 8, 29, 0, 30, 0, DateTimeKind.Utc), match.StartsAtUtc);
    }

    [Fact]
    public async Task Creates_match_and_the_missing_round_with_the_expected_convention()
    {
        await using var database = await SqliteDatabase.Create();
        var edition = await SeedEdition(database.Db);
        var home = await SeedTeam(database.Db, "Boca Juniors");
        var away = await SeedTeam(database.Db, "River Plate");

        var result = await Confirm(database.Db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"));

        Assert.True(result.IsSuccess);
        Assert.Equal(new ImportConfirmationSummary(1, 0, 0), result.Matches);
        var round = await database.Db.Rounds.AsNoTracking().SingleAsync();
        Assert.Equal((7, "Fecha 7"), (round.Order, round.Name));
        var match = await database.Db.Matches.AsNoTracking().SingleAsync();
        Assert.Equal((round.Id, home.Id, away.Id, "Boca Juniors", "River Plate"),
            (match.RoundId, match.HomeTeamId, match.AwayTeamId, match.ParticipantHome, match.ParticipantAway));
    }

    [Fact]
    public async Task Reimporting_the_identical_file_is_idempotent()
    {
        await using var database = await SqliteDatabase.Create();
        var edition = await SeedEdition(database.Db);
        await SeedTeam(database.Db, "Boca Juniors");
        await SeedTeam(database.Db, "River Plate");
        var row = MatchRow(7, "Boca Juniors", "River Plate");

        await Confirm(database.Db, edition.Id, row);
        database.Db.ChangeTracker.Clear();
        var second = await Confirm(database.Db, edition.Id, row);

        Assert.Equal(new ImportConfirmationSummary(0, 0, 1), second.Matches);
        Assert.Equal(1, await database.Db.Matches.CountAsync());
        Assert.Equal(1, await database.Db.Rounds.CountAsync());
    }

    [Fact]
    public async Task Reimporting_with_a_different_kickoff_updates_only_the_time()
    {
        await using var database = await SqliteDatabase.Create();
        var edition = await SeedEdition(database.Db);
        await SeedTeam(database.Db, "Boca Juniors");
        await SeedTeam(database.Db, "River Plate");
        await Confirm(database.Db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"));
        database.Db.ChangeTracker.Clear();

        var result = await Confirm(database.Db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate", time: new TimeOnly(23, 0)));

        Assert.Equal(new ImportConfirmationSummary(0, 1, 0), result.Matches);
        var match = await database.Db.Matches.AsNoTracking().SingleAsync();
        Assert.Equal(new TimeOnly(23, 0), TimeOnly.FromDateTime(match.StartsAtUtc.AddHours(-3)));
    }

    [Fact]
    public async Task Predictions_allow_kickoff_time_updates()
    {
        await using var database = await SqliteDatabase.Create();
        var edition = await SeedEdition(database.Db);
        await SeedTeam(database.Db, "Boca Juniors");
        await SeedTeam(database.Db, "River Plate");
        await Confirm(database.Db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"));
        var match = await database.Db.Matches.AsNoTracking().SingleAsync();
        var user = await SeedUser(database.Db);
        database.Db.Predictions.Add(new Prediction { MatchId = match.Id, UserId = user.Id, PredictedHomeScore = 1, PredictedAwayScore = 1, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();

        var result = await Confirm(database.Db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate", time: new TimeOnly(23, 0)));

        Assert.True(result.IsSuccess);
        Assert.Equal(new ImportConfirmationSummary(0, 1, 0), result.Matches);
    }

    [Fact]
    public async Task Predictions_block_reassigning_a_team_within_the_same_round()
    {
        await using var database = await SqliteDatabase.Create();
        var edition = await SeedEdition(database.Db);
        var round = await SeedRound(database.Db, edition.Id, 7);
        var boca = await SeedTeam(database.Db, "Boca Juniors");
        var racing = await SeedTeam(database.Db, "Racing Club");
        await SeedTeam(database.Db, "River Plate");
        var existing = await SeedMatch(database.Db, round.Id, boca.Id, racing.Id, ExpectedUtc);
        var user = await SeedUser(database.Db);
        database.Db.Predictions.Add(new Prediction { MatchId = existing.Id, UserId = user.Id, PredictedHomeScore = 2, PredictedAwayScore = 0, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();

        var result = await Confirm(database.Db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"));

        Assert.Equal(ImportConfirmationStatus.Rejected, result.Status);
        // Nada se crea ni se toca: el partido existente (Boca vs Racing) sigue exactamente igual,
        // y no aparece un segundo Match para "Boca vs River" en la misma Fecha.
        var untouched = await database.Db.Matches.AsNoTracking().SingleAsync();
        Assert.Equal((existing.Id, boca.Id, racing.Id), (untouched.Id, untouched.HomeTeamId, untouched.AwayTeamId));
    }

    [Fact]
    public async Task Finished_match_is_rejected_and_left_untouched()
    {
        await using var database = await SqliteDatabase.Create();
        var edition = await SeedEdition(database.Db);
        var round = await SeedRound(database.Db, edition.Id, 7);
        var home = await SeedTeam(database.Db, "Boca Juniors");
        var away = await SeedTeam(database.Db, "River Plate");
        await SeedMatch(database.Db, round.Id, home.Id, away.Id, ExpectedUtc.AddHours(5), MatchStatus.Finished);
        database.Db.ChangeTracker.Clear();

        var result = await Confirm(database.Db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"));

        Assert.Equal(ImportConfirmationStatus.Rejected, result.Status);
        var match = await database.Db.Matches.AsNoTracking().SingleAsync();
        Assert.Equal(ExpectedUtc.AddHours(5), match.StartsAtUtc);
    }

    [Fact]
    public async Task Wrong_hash_rejects_without_writing()
    {
        await using var database = await SqliteDatabase.Create();
        var edition = await SeedEdition(database.Db);
        await SeedTeam(database.Db, "Boca Juniors");
        await SeedTeam(database.Db, "River Plate");
        using var file = SpreadsheetTestWorkbook.CreateXlsx(MatchesSheet(MatchRow(7, "Boca Juniors", "River Plate")));

        var result = await new MatchImportConfirmationService(database.Db, NullLogger<MatchImportConfirmationService>.Instance)
            .ConfirmAsync(file, "partidos.xlsx", edition.Id, adminUserId: 1, new string('0', 64));

        Assert.Equal(ImportConfirmationStatus.Rejected, result.Status);
        Assert.Contains(result.Issues, issue => issue.Code == "FILE_HASH_MISMATCH");
        Assert.Empty(await database.Db.Matches.ToListAsync());
    }

    [Fact]
    public async Task Failure_creating_match_rolls_back_the_newly_created_round()
    {
        var interceptor = new ThrowOnSaveInterceptor(2);
        await using var database = await SqliteDatabase.Create(interceptor);
        var edition = await SeedEdition(database.Db);
        await SeedTeam(database.Db, "Boca Juniors");
        await SeedTeam(database.Db, "River Plate");
        interceptor.Enabled = true;

        var result = await Confirm(database.Db, edition.Id, MatchRow(7, "Boca Juniors", "River Plate"));

        Assert.Equal(ImportConfirmationStatus.Failed, result.Status);
        Assert.Empty(await database.Db.Rounds.AsNoTracking().ToListAsync());
        Assert.Empty(await database.Db.Matches.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Database_unique_index_is_the_last_line_of_defense_against_duplicates()
    {
        await using var database = await SqliteDatabase.Create();
        var edition = await SeedEdition(database.Db);
        var round = await SeedRound(database.Db, edition.Id, 7);
        var home = await SeedTeam(database.Db, "Boca Juniors");
        var away = await SeedTeam(database.Db, "River Plate");
        await SeedMatch(database.Db, round.Id, home.Id, away.Id, ExpectedUtc);
        database.Db.ChangeTracker.Clear();

        database.Db.Matches.Add(new Match
        {
            RoundId = round.Id, HomeTeamId = home.Id, AwayTeamId = away.Id,
            ParticipantHome = "Boca Juniors", ParticipantAway = "River Plate",
            StartsAtUtc = ExpectedUtc.AddHours(1), Status = MatchStatus.Scheduled, CreatedAtUtc = DateTime.UtcNow
        });
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => database.Db.SaveChangesAsync());
    }

    private static async Task<MatchImportConfirmationResult> Confirm(PlayPredictDbContext db, int editionId, params ImportMatchRow[] rows)
    {
        using var file = SpreadsheetTestWorkbook.CreateXlsx(MatchesSheet(rows));
        var bytes = file.ToArray();
        var hash = SpreadsheetFileHash.ComputeSha256(bytes);
        using var confirmationFile = new MemoryStream(bytes);
        return await new MatchImportConfirmationService(db, NullLogger<MatchImportConfirmationService>.Instance)
            .ConfirmAsync(confirmationFile, "partidos.xlsx", editionId, adminUserId: 1, hash);
    }

    private static SheetData MatchesSheet(params ImportMatchRow[] rows) => new(SpreadsheetReader.MatchesSheet,
        [["FECHA_NRO", "FECHA", "HORA", "LOCAL", "VISITANTE", "ESTADO"],
            .. rows.Select(r => new object?[] { r.RoundNumber, r.OriginalDate, r.OriginalTime, r.HomeTeam, r.AwayTeam, r.Status.ToString()!.ToUpperInvariant() })]);

    private static ImportMatchRow MatchRow(int roundNumber, string home, string away, TimeOnly? time = null, int row = 2)
    {
        var effectiveTime = time ?? Time;
        return new(row, roundNumber.ToString(), Day.ToString("yyyy-MM-dd"), effectiveTime.ToString("HH:mm"), home, away, "SCHEDULED",
            roundNumber, Day, effectiveTime, home, away,
            SpreadsheetTextNormalizer.Normalize(home), SpreadsheetTextNormalizer.Normalize(away), ImportMatchStatus.Scheduled);
    }

    private static async Task<Edition> SeedEdition(PlayPredictDbContext db)
    {
        var edition = new Edition { CompetitionId = await SeedCompetitionId(db), Name = "Clausura 2026", StartDateUtc = DateTime.UtcNow, Status = EditionStatus.Active, CreatedAtUtc = DateTime.UtcNow };
        db.Editions.Add(edition);
        await db.SaveChangesAsync();
        return edition;
    }

    private static async Task<int> SeedCompetitionId(PlayPredictDbContext db)
    {
        var experience = new Experience { Name = "El Nene", Status = ExperienceStatus.Published, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
        db.Experiences.Add(experience);
        await db.SaveChangesAsync();
        var competition = new Competition { ExperienceId = experience.Id, Name = "Liga Profesional", Sport = "Fútbol", IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Competitions.Add(competition);
        await db.SaveChangesAsync();
        return competition.Id;
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

    private static async Task<User> SeedUser(PlayPredictDbContext db)
    {
        var companyId = await SeedCompanyId(db);
        var user = new User { CompanyId = companyId, FirstName = "Test", LastName = "User", Email = $"user{Guid.NewGuid():N}@test.local", PasswordHash = "x", CreatedAtUtc = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<int> SeedCompanyId(PlayPredictDbContext db)
    {
        var company = new Company { Name = "Test Company", ShortName = "TEST" };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company.Id;
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

    private sealed class SqliteDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public PlayPredictDbContext Db { get; }

        private SqliteDatabase(SqliteConnection connection, PlayPredictDbContext db)
        {
            this.connection = connection;
            Db = db;
        }

        public static async Task<SqliteDatabase> Create(params IInterceptor[] interceptors)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<PlayPredictDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(interceptors)
                .Options;
            var db = new PlayPredictDbContext(options);
            await db.Database.EnsureCreatedAsync();
            return new(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class ThrowOnSaveInterceptor(int throwOnCall) : SaveChangesInterceptor
    {
        private int calls;
        public bool Enabled { get; set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled && ++calls == throwOnCall) throw new InvalidOperationException("Falla de persistencia simulada.");
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }
}
