using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Imports;
using Xunit;

namespace PlayPredict.Api.Tests;

public class TeamRosterImportConfirmationServiceTests
{
    [Fact]
    public async Task Creates_new_team()
    {
        await using var database = await SqliteDatabase.Create();

        var result = await Confirm(database.Db, Teams(["River Plate", "CARP"]), Rosters());

        Assert.Equal(ImportConfirmationStatus.Success, result.Status);
        Assert.Equal(new ImportConfirmationSummary(1, 0, 0), result.Teams);
        var team = await database.Db.Teams.AsNoTracking().SingleAsync();
        Assert.Equal(("River Plate", "CARP", "F\u00fatbol", true), (team.Name, team.ShortName, team.Sport, team.Active));
    }

    [Fact]
    public async Task Creates_team_and_players_with_real_team_id()
    {
        await using var database = await SqliteDatabase.Create();

        var result = await Confirm(database.Db,
            Teams(["River Plate", "CARP"]),
            Rosters(["River Plate", "Juan", "P\u00e9rez", "", "DEFENSOR"]));

        Assert.True(result.IsSuccess);
        Assert.Equal(new ImportConfirmationSummary(1, 0, 0), result.Rosters);
        var team = await database.Db.Teams.AsNoTracking().SingleAsync();
        var player = await database.Db.TeamPlayers.AsNoTracking().SingleAsync();
        Assert.Equal(team.Id, player.TeamId);
        Assert.Equal(("Juan", "P\u00e9rez", "Juan P\u00e9rez", "Defensor", true),
            (player.FirstName, player.LastName, player.DisplayName, player.Position, player.Active));
    }

    [Fact]
    public async Task Updates_only_short_name_and_counts_unchanged_team()
    {
        await using var database = await SqliteDatabase.Create();
        var river = await SeedTeam(database.Db, "River Plate", "RIV");
        await SeedTeam(database.Db, "Boca Juniors", "BOCA");
        database.Db.ChangeTracker.Clear();

        var result = await Confirm(database.Db,
            Teams(["River Plate", "CARP"], ["Boca Juniors", "BOCA"]), Rosters());

        Assert.Equal(new ImportConfirmationSummary(0, 1, 1), result.Teams);
        Assert.Equal("CARP", (await database.Db.Teams.AsNoTracking().SingleAsync(x => x.Id == river.Id)).ShortName);
    }

    [Fact]
    public async Task Creates_player_in_existing_team_and_keeps_unchanged_player()
    {
        await using var database = await SqliteDatabase.Create();
        var team = await SeedTeam(database.Db, "River Plate", "CARP");
        await SeedPlayer(database.Db, team.Id, "Juan", "P\u00e9rez", "Juan P\u00e9rez", "Defensor");
        database.Db.ChangeTracker.Clear();

        var result = await Confirm(database.Db, Teams(), Rosters(
            ["River Plate", "Juan", "P\u00e9rez", "", "DEFENSOR"],
            ["River Plate", "Pedro", "G\u00f3mez", "", "DELANTERO"]));

        Assert.Equal(new ImportConfirmationSummary(1, 0, 1), result.Rosters);
        Assert.Equal(2, await database.Db.TeamPlayers.CountAsync());
    }

    [Fact]
    public async Task Updates_position_and_display_name()
    {
        await using var database = await SqliteDatabase.Create();
        var team = await SeedTeam(database.Db, "River Plate", "CARP");
        var player = await SeedPlayer(database.Db, team.Id, "Juan", "P\u00e9rez", "Juancho", "Delantero");
        database.Db.ChangeTracker.Clear();

        var result = await Confirm(database.Db, Teams(),
            Rosters(["River Plate", "Juan", "P\u00e9rez", "J. P\u00e9rez", "DEFENSOR"]));

        Assert.Equal(new ImportConfirmationSummary(0, 1, 0), result.Rosters);
        var updated = await database.Db.TeamPlayers.AsNoTracking().SingleAsync(x => x.Id == player.Id);
        Assert.Equal(("J. P\u00e9rez", "Defensor"), (updated.DisplayName, updated.Position));
    }

    [Fact]
    public async Task Same_file_is_idempotent_and_preserves_counts()
    {
        await using var database = await SqliteDatabase.Create();
        var teams = Teams(["River Plate", "CARP"]);
        var rosters = Rosters(["River Plate", "Juan", "P\u00e9rez", "", "DEFENSOR"]);

        var first = await Confirm(database.Db, teams, rosters);
        database.Db.ChangeTracker.Clear();
        var second = await Confirm(database.Db, teams, rosters);

        Assert.True(first.IsSuccess);
        Assert.Equal(new ImportConfirmationSummary(0, 0, 1), second.Teams);
        Assert.Equal(new ImportConfirmationSummary(0, 0, 1), second.Rosters);
        Assert.Equal(1, await database.Db.Teams.CountAsync());
        Assert.Equal(1, await database.Db.TeamPlayers.CountAsync());
    }

    [Fact]
    public async Task Absences_do_not_delete_deactivate_or_modify_existing_records()
    {
        await using var database = await SqliteDatabase.Create();
        var absentTeam = await SeedTeam(database.Db, "Boca Juniors", "BOCA");
        var absentPlayer = await SeedPlayer(database.Db, absentTeam.Id, "Carlos", "L\u00f3pez", "Carlitos", "Arquero");
        database.Db.ChangeTracker.Clear();

        var result = await Confirm(database.Db, Teams(["River Plate", "CARP"]), Rosters());

        Assert.True(result.IsSuccess);
        var team = await database.Db.Teams.AsNoTracking().SingleAsync(x => x.Id == absentTeam.Id);
        var player = await database.Db.TeamPlayers.AsNoTracking().SingleAsync(x => x.Id == absentPlayer.Id);
        Assert.True(team.Active);
        Assert.Equal("BOCA", team.ShortName);
        Assert.True(player.Active);
        Assert.Equal(("Carlitos", "Arquero"), (player.DisplayName, player.Position));
    }

    [Fact]
    public async Task Incompatible_sport_rejects_everything()
    {
        await using var database = await SqliteDatabase.Create();
        await SeedTeam(database.Db, "River Plate", "CARP", "B\u00e1squet");
        database.Db.ChangeTracker.Clear();

        var result = await Confirm(database.Db, Teams(["River Plate", "RIV"], ["Boca Juniors", "BOCA"]), Rosters());

        Assert.Equal(ImportConfirmationStatus.Rejected, result.Status);
        Assert.Equal(1, await database.Db.Teams.CountAsync());
    }

    [Fact]
    public async Task Ambiguous_team_rejects_everything()
    {
        await using var database = await SqliteDatabase.Create();
        await SeedTeam(database.Db, "River Plate", "CARP");
        await SeedTeam(database.Db, " river   plate ", "RIV");
        database.Db.ChangeTracker.Clear();

        var result = await Confirm(database.Db, Teams(["RIVER PLATE", "RP"]), Rosters());

        Assert.Equal(ImportConfirmationStatus.Rejected, result.Status);
        Assert.Equal(2, await database.Db.Teams.CountAsync());
    }

    [Fact]
    public async Task Ambiguous_player_rejects_everything()
    {
        await using var database = await SqliteDatabase.Create();
        var team = await SeedTeam(database.Db, "River Plate", "CARP");
        await SeedPlayer(database.Db, team.Id, "Juan", "P\u00e9rez", "Juan", "Defensor");
        await SeedPlayer(database.Db, team.Id, " juan ", " p\u00c9REZ ", "Juancho", "Defensor");
        database.Db.ChangeTracker.Clear();

        var result = await Confirm(database.Db, Teams(["Boca Juniors", "BOCA"]),
            Rosters(["River Plate", "Juan", "P\u00e9rez", "", "DEFENSOR"]));

        Assert.Equal(ImportConfirmationStatus.Rejected, result.Status);
        Assert.DoesNotContain(await database.Db.Teams.AsNoTracking().ToListAsync(), x => x.Name == "Boca Juniors");
    }

    [Fact]
    public async Task Unresolved_roster_team_rejects_everything()
    {
        await using var database = await SqliteDatabase.Create();

        var result = await Confirm(database.Db, Teams(["Boca Juniors", "BOCA"]),
            Rosters(["Desconocido", "Juan", "P\u00e9rez", "", "DEFENSOR"]));

        Assert.Equal(ImportConfirmationStatus.Rejected, result.Status);
        Assert.Empty(await database.Db.Teams.ToListAsync());
    }

    [Fact]
    public async Task Structural_error_rejects_everything()
    {
        await using var database = await SqliteDatabase.Create();

        var result = await Confirm(database.Db, Teams(["River Plate", "CARP"]),
            Rosters(["River Plate", "Juan", "P\u00e9rez", "", "VOLANTE"]));

        Assert.Equal(ImportConfirmationStatus.Rejected, result.Status);
        Assert.Contains(result.Issues, issue => issue.Code == "INVALID_POSITION");
        Assert.Empty(await database.Db.Teams.ToListAsync());
    }

    [Fact]
    public async Task Wrong_hash_rejects_without_writing()
    {
        await using var database = await SqliteDatabase.Create();
        using var file = SpreadsheetTestWorkbook.CreateXlsx(Teams(["River Plate", "CARP"]), Rosters());

        var result = await new TeamRosterImportConfirmationService(database.Db)
            .ConfirmAsync(file, "equipos.xlsx", "F\u00fatbol", new string('0', 64));

        Assert.Equal(ImportConfirmationStatus.Rejected, result.Status);
        Assert.Contains(result.Issues, issue => issue.Code == "FILE_HASH_MISMATCH");
        Assert.Empty(await database.Db.Teams.ToListAsync());
    }

    [Fact]
    public async Task Confirmation_revalidates_when_new_team_appears_after_preview()
    {
        await using var database = await SqliteDatabase.Create();
        var teams = Teams(["River Plate", "CARP"]);
        var rosters = Rosters(["River Plate", "Juan", "P\u00e9rez", "", "DEFENSOR"]);
        using (var previewFile = SpreadsheetTestWorkbook.CreateXlsx(teams, rosters))
        {
            var read = new SpreadsheetReader().Read(previewFile, "equipos.xlsx", SpreadsheetImportKind.TeamsAndRosters);
            var preview = await new TeamRosterImportPreviewService(database.Db).PreviewAsync(read, "F\u00fatbol");
            Assert.Equal(ImportPreviewClassification.TeamNew, Assert.Single(preview.Teams).Classification);
        }
        await SeedTeam(database.Db, "River Plate", "CARP");
        database.Db.ChangeTracker.Clear();

        var confirmation = await Confirm(database.Db, teams, rosters);

        Assert.True(confirmation.IsSuccess);
        Assert.Equal(new ImportConfirmationSummary(0, 0, 1), confirmation.Teams);
        Assert.Equal(1, await database.Db.Teams.CountAsync());
        Assert.Equal(1, await database.Db.TeamPlayers.CountAsync());
    }

    [Fact]
    public async Task Confirmation_revalidates_new_sport_conflict_after_preview()
    {
        await using var database = await SqliteDatabase.Create();
        var teams = Teams(["River Plate", "CARP"]);
        using (var previewFile = SpreadsheetTestWorkbook.CreateXlsx(teams, Rosters()))
        {
            var read = new SpreadsheetReader().Read(previewFile, "equipos.xlsx", SpreadsheetImportKind.TeamsAndRosters);
            Assert.Equal(ImportPreviewClassification.TeamNew,
                Assert.Single((await new TeamRosterImportPreviewService(database.Db).PreviewAsync(read, "F\u00fatbol")).Teams).Classification);
        }
        await SeedTeam(database.Db, "River Plate", "CARP", "B\u00e1squet");
        database.Db.ChangeTracker.Clear();

        var confirmation = await Confirm(database.Db, teams, Rosters());

        Assert.Equal(ImportConfirmationStatus.Rejected, confirmation.Status);
    }

    [Fact]
    public async Task Failure_creating_players_rolls_back_created_team()
    {
        var interceptor = new ThrowOnSaveInterceptor(2);
        await using var database = await SqliteDatabase.Create(interceptor);
        interceptor.Enabled = true;

        var result = await Confirm(database.Db,
            Teams(["River Plate", "CARP"]),
            Rosters(["River Plate", "Juan", "P\u00e9rez", "", "DEFENSOR"]));

        Assert.Equal(ImportConfirmationStatus.Failed, result.Status);
        Assert.Empty(await database.Db.Teams.AsNoTracking().ToListAsync());
        Assert.Empty(await database.Db.TeamPlayers.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Failure_updating_player_rolls_back_team_update()
    {
        var interceptor = new ThrowOnSaveInterceptor(2);
        await using var database = await SqliteDatabase.Create(interceptor);
        var team = await SeedTeam(database.Db, "River Plate", "RIV");
        var player = await SeedPlayer(database.Db, team.Id, "Juan", "P\u00e9rez", "Juancho", "Delantero");
        database.Db.ChangeTracker.Clear();
        interceptor.ResetAndEnable();

        var result = await Confirm(database.Db,
            Teams(["River Plate", "CARP"]),
            Rosters(["River Plate", "Juan", "P\u00e9rez", "J. P\u00e9rez", "DEFENSOR"]));

        Assert.Equal(ImportConfirmationStatus.Failed, result.Status);
        var unchangedTeam = await database.Db.Teams.AsNoTracking().SingleAsync(x => x.Id == team.Id);
        var unchangedPlayer = await database.Db.TeamPlayers.AsNoTracking().SingleAsync(x => x.Id == player.Id);
        Assert.Equal("RIV", unchangedTeam.ShortName);
        Assert.Equal(("Juancho", "Delantero"), (unchangedPlayer.DisplayName, unchangedPlayer.Position));
    }

    [Fact]
    public async Task Does_not_modify_fields_outside_import_scope()
    {
        await using var database = await SqliteDatabase.Create();
        var team = await SeedTeam(database.Db, "River Plate", "RIV");
        team.LogoUrl = "https://example.test/logo.png";
        team.Active = false;
        var player = await SeedPlayer(database.Db, team.Id, "Juan", "P\u00e9rez", "Juancho", "Delantero");
        player.ShirtNumber = 10;
        player.PhotoUrl = "https://example.test/player.png";
        player.Active = false;
        await database.Db.SaveChangesAsync();
        database.Db.ChangeTracker.Clear();

        var result = await Confirm(database.Db,
            Teams(["River Plate", "CARP"]),
            Rosters(["River Plate", "Juan", "P\u00e9rez", "J. P\u00e9rez", "DEFENSOR"]));

        Assert.True(result.IsSuccess);
        var updatedTeam = await database.Db.Teams.AsNoTracking().SingleAsync();
        var updatedPlayer = await database.Db.TeamPlayers.AsNoTracking().SingleAsync();
        Assert.Equal(("River Plate", "F\u00fatbol", "https://example.test/logo.png", false),
            (updatedTeam.Name, updatedTeam.Sport, updatedTeam.LogoUrl, updatedTeam.Active));
        Assert.Equal((team.Id, "Juan", "P\u00e9rez", 10, "https://example.test/player.png", false),
            (updatedPlayer.TeamId, updatedPlayer.FirstName, updatedPlayer.LastName, updatedPlayer.ShirtNumber, updatedPlayer.PhotoUrl, updatedPlayer.Active));
    }

    [Fact]
    public async Task Hash_is_exposed_and_case_insensitive_on_confirmation()
    {
        await using var database = await SqliteDatabase.Create();
        using var file = SpreadsheetTestWorkbook.CreateXlsx(Teams(["River Plate", "CARP"]), Rosters());
        var bytes = file.ToArray();
        var expected = SpreadsheetFileHash.ComputeSha256(bytes).ToLowerInvariant();
        using var confirmationFile = new MemoryStream(bytes);

        var result = await new TeamRosterImportConfirmationService(database.Db)
            .ConfirmAsync(confirmationFile, "equipos.xlsx", "F\u00fatbol", expected);

        Assert.True(result.IsSuccess);
        Assert.Equal(expected, result.ProcessedHash, ignoreCase: true);
    }

    private static async Task<TeamRosterImportConfirmationResult> Confirm(
        PlayPredictDbContext db, SheetData teams, SheetData rosters, string sport = "F\u00fatbol")
    {
        using var file = SpreadsheetTestWorkbook.CreateXlsx(teams, rosters);
        var bytes = file.ToArray();
        var hash = SpreadsheetFileHash.ComputeSha256(bytes);
        using var confirmationFile = new MemoryStream(bytes);
        return await new TeamRosterImportConfirmationService(db)
            .ConfirmAsync(confirmationFile, "equipos.xlsx", sport, hash);
    }

    private static SheetData Teams(params object?[][] rows) => new(SpreadsheetReader.TeamsSheet,
        [["NOMBRE DEL EQUIPO", "NOMBRE CORTO"], .. rows]);

    private static SheetData Rosters(params object?[][] rows) => new(SpreadsheetReader.RostersSheet,
        [["NOMBRE DEL CLUB", "NOMBRE", "APELLIDO", "NOMBRE PARA MOSTRAR", "POSICION"], .. rows]);

    private static async Task<Team> SeedTeam(PlayPredictDbContext db, string name, string shortName, string sport = "F\u00fatbol")
    {
        var team = new Team { Name = name, ShortName = shortName, Sport = sport, Active = true };
        db.Teams.Add(team);
        await db.SaveChangesAsync();
        return team;
    }

    private static async Task<TeamPlayer> SeedPlayer(PlayPredictDbContext db, int teamId, string firstName,
        string lastName, string displayName, string position)
    {
        var player = new TeamPlayer
        {
            TeamId = teamId, FirstName = firstName, LastName = lastName,
            DisplayName = displayName, Position = position, Active = true
        };
        db.TeamPlayers.Add(player);
        await db.SaveChangesAsync();
        return player;
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

        public void ResetAndEnable()
        {
            calls = 0;
            Enabled = true;
        }

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
