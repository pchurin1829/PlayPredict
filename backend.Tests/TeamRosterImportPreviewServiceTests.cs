using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Imports;
using Xunit;

namespace PlayPredict.Api.Tests;

public class TeamRosterImportPreviewServiceTests
{
    [Fact]
    public async Task Classifies_new_team()
    {
        await using var db = CreateDb();
        var result = await Preview(db, teams: [TeamRow("River Plate", "CARP")]);

        Assert.Equal(ImportPreviewClassification.TeamNew, Assert.Single(result.Teams).Classification);
        Assert.Equal(1, result.TeamsSummary.New);
    }

    [Fact]
    public async Task Classifies_existing_team_without_changes()
    {
        await using var db = CreateDb();
        var team = await SeedTeam(db, "River Plate", "CARP", "F\u00fatbol");

        var row = Assert.Single((await Preview(db, teams: [TeamRow("River Plate", "CARP")])).Teams);

        Assert.Equal(ImportPreviewClassification.TeamUnchanged, row.Classification);
        Assert.Equal(team.Id, row.TeamId);
    }

    [Fact]
    public async Task Proposes_short_name_change_without_applying_it()
    {
        await using var db = CreateDb();
        var team = await SeedTeam(db, "River Plate", "RIV", "F\u00fatbol");

        var row = Assert.Single((await Preview(db, teams: [TeamRow("River Plate", "CARP")])).Teams);

        Assert.Equal(ImportPreviewClassification.TeamUpdatable, row.Classification);
        Assert.Equal(new ImportProposedChange("ShortName", "RIV", "CARP"), Assert.Single(row.ProposedChanges));
        Assert.Equal("RIV", (await db.Teams.FindAsync(team.Id))!.ShortName);
    }

    [Fact]
    public async Task Reports_incompatible_team_sport()
    {
        await using var db = CreateDb();
        await SeedTeam(db, "River Plate", "CARP", "B\u00e1squet");

        var row = Assert.Single((await Preview(db, teams: [TeamRow("River Plate", "CARP")])).Teams);

        Assert.Equal(ImportPreviewClassification.TeamSportConflict, row.Classification);
    }

    [Fact]
    public async Task Reports_team_ambiguity_after_normalization()
    {
        await using var db = CreateDb();
        await SeedTeam(db, "River Plate", "CARP", "F\u00fatbol");
        await SeedTeam(db, " river   plate ", "RIV", "F\u00fatbol");

        var row = Assert.Single((await Preview(db, teams: [TeamRow("RIVER PLATE", "CARP")])).Teams);

        Assert.Equal(ImportPreviewClassification.TeamAmbiguousConflict, row.Classification);
    }

    [Fact]
    public async Task Classifies_new_player_in_existing_team()
    {
        await using var db = CreateDb();
        var team = await SeedTeam(db, "River Plate", "CARP", "F\u00fatbol");

        var row = Assert.Single((await Preview(db, rosters: [RosterRow("River Plate", "Juan", "P\u00e9rez")])).Rosters);

        Assert.Equal(ImportPreviewClassification.PlayerNew, row.Classification);
        Assert.Equal(team.Id, row.TeamId);
    }

    [Fact]
    public async Task Classifies_player_as_new_for_new_team_in_same_file()
    {
        await using var db = CreateDb();

        var result = await Preview(db,
            [TeamRow("River Plate", "CARP")],
            [RosterRow(" river   plate ", "Juan", "P\u00e9rez")]);

        Assert.Equal(ImportPreviewClassification.TeamNew, Assert.Single(result.Teams).Classification);
        var player = Assert.Single(result.Rosters);
        Assert.Equal(ImportPreviewClassification.PlayerNew, player.Classification);
        Assert.Null(player.TeamId);
    }

    [Fact]
    public async Task Classifies_existing_player_without_changes()
    {
        await using var db = CreateDb();
        var team = await SeedTeam(db, "River Plate", "CARP", "F\u00fatbol");
        var player = await SeedPlayer(db, team.Id, "Juan", "P\u00e9rez", "Juan P\u00e9rez", "Defensor");

        var row = Assert.Single((await Preview(db, rosters: [RosterRow("River Plate", "Juan", "P\u00e9rez")])).Rosters);

        Assert.Equal(ImportPreviewClassification.PlayerUnchanged, row.Classification);
        Assert.Equal(player.Id, row.TeamPlayerId);
    }

    [Fact]
    public async Task Proposes_position_change()
    {
        await using var db = CreateDb();
        var team = await SeedTeam(db, "River Plate", "CARP", "F\u00fatbol");
        await SeedPlayer(db, team.Id, "Juan", "P\u00e9rez", "Juan P\u00e9rez", "Delantero");

        var row = Assert.Single((await Preview(db, rosters: [RosterRow("River Plate", "Juan", "P\u00e9rez")])).Rosters);

        Assert.Equal(ImportPreviewClassification.PlayerUpdatable, row.Classification);
        Assert.Contains(row.ProposedChanges, change => change.Field == "Position" && change.ProposedValue == "Defensor");
    }

    [Fact]
    public async Task Proposes_display_name_change()
    {
        await using var db = CreateDb();
        var team = await SeedTeam(db, "River Plate", "CARP", "F\u00fatbol");
        await SeedPlayer(db, team.Id, "Juan", "P\u00e9rez", "Juancho", "Defensor");

        var row = Assert.Single((await Preview(db, rosters: [RosterRow("River Plate", "Juan", "P\u00e9rez", "J. P\u00e9rez")])).Rosters);

        Assert.Equal(ImportPreviewClassification.PlayerUpdatable, row.Classification);
        Assert.Contains(row.ProposedChanges, change => change.Field == "DisplayName" && change.ProposedValue == "J. P\u00e9rez");
    }

    [Fact]
    public async Task Reports_ambiguous_player()
    {
        await using var db = CreateDb();
        var team = await SeedTeam(db, "River Plate", "CARP", "F\u00fatbol");
        await SeedPlayer(db, team.Id, "Juan", "P\u00e9rez", "Juan P\u00e9rez", "Defensor");
        await SeedPlayer(db, team.Id, " juan ", " p\u00c9REZ ", "J. P\u00e9rez", "Defensor");

        var row = Assert.Single((await Preview(db, rosters: [RosterRow("River Plate", "JUAN", "P\u00c9REZ")])).Rosters);

        Assert.Equal(ImportPreviewClassification.PlayerAmbiguousConflict, row.Classification);
    }

    [Fact]
    public async Task Reports_unresolved_roster_team()
    {
        await using var db = CreateDb();

        var row = Assert.Single((await Preview(db, rosters: [RosterRow("Desconocido", "Juan", "P\u00e9rez")])).Rosters);

        Assert.Equal(ImportPreviewClassification.UnresolvedTeamError, row.Classification);
    }

    [Fact]
    public async Task Keeps_structural_duplicate_detection_in_preview()
    {
        await using var db = CreateDb();
        var rows = new[] { TeamRow("River Plate", "CARP", 2), TeamRow(" river plate ", "RIV", 3) };
        var issue = new SpreadsheetValidationIssue("DUPLICATE_TEAM_ROW", "Duplicado", SpreadsheetReader.TeamsSheet, 3);

        var result = await Preview(db, rows, issues: [issue]);

        Assert.Equal(ImportPreviewClassification.TeamNew, result.Teams[0].Classification);
        Assert.Equal(ImportPreviewClassification.StructuralError, result.Teams[1].Classification);
        Assert.Equal(1, result.TeamsSummary.Errors);
    }

    [Fact]
    public async Task Resolves_names_with_spaces_and_case_differences()
    {
        await using var db = CreateDb();
        var team = await SeedTeam(db, "River Plate", "CARP", "F\u00fatbol");
        var player = await SeedPlayer(db, team.Id, "Juan", "P\u00e9rez", "Juan P\u00e9rez", "Defensor");

        var result = await Preview(db,
            [TeamRow("  RIVER   plate ", "CARP")],
            [RosterRow(" river plate ", " JUAN ", " p\u00c9REZ ")],
            sport: " f\u00daTBOL ");

        Assert.Equal(team.Id, Assert.Single(result.Teams).TeamId);
        Assert.Equal(player.Id, Assert.Single(result.Rosters).TeamPlayerId);
    }

    [Fact]
    public async Task Preview_does_not_modify_database_or_change_tracker()
    {
        await using var db = CreateDb();
        var team = await SeedTeam(db, "River Plate", "RIV", "F\u00fatbol");
        var player = await SeedPlayer(db, team.Id, "Juan", "P\u00e9rez", "Juancho", "Delantero");
        db.ChangeTracker.Clear();
        var beforeTeams = await db.Teams.AsNoTracking().Select(x => new { x.Id, x.Name, x.ShortName, x.Sport, x.Active }).ToListAsync();
        var beforePlayers = await db.TeamPlayers.AsNoTracking().Select(x => new { x.Id, x.TeamId, x.FirstName, x.LastName, x.DisplayName, x.Position, x.Active }).ToListAsync();

        await Preview(db, [TeamRow("River Plate", "CARP")], [RosterRow("River Plate", "Juan", "P\u00e9rez", "J. P\u00e9rez")]);

        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Equal(beforeTeams, await db.Teams.AsNoTracking().Select(x => new { x.Id, x.Name, x.ShortName, x.Sport, x.Active }).ToListAsync());
        Assert.Equal(beforePlayers, await db.TeamPlayers.AsNoTracking().Select(x => new { x.Id, x.TeamId, x.FirstName, x.LastName, x.DisplayName, x.Position, x.Active }).ToListAsync());
        Assert.Equal("RIV", (await db.Teams.AsNoTracking().SingleAsync()).ShortName);
        Assert.Equal("Juancho", (await db.TeamPlayers.AsNoTracking().SingleAsync()).DisplayName);
        Assert.Equal("Delantero", (await db.TeamPlayers.AsNoTracking().SingleAsync()).Position);
    }

    [Fact]
    public async Task Repeated_preview_is_deterministic()
    {
        await using var db = CreateDb();
        var team = await SeedTeam(db, "River Plate", "RIV", "F\u00fatbol");
        await SeedPlayer(db, team.Id, "Juan", "P\u00e9rez", "Juancho", "Delantero");
        var spreadsheet = Sheet([TeamRow("River Plate", "CARP")], [RosterRow("River Plate", "Juan", "P\u00e9rez")]);
        var service = new TeamRosterImportPreviewService(db);

        var first = await service.PreviewAsync(spreadsheet, "F\u00fatbol");
        var second = await service.PreviewAsync(spreadsheet, "F\u00fatbol");

        Assert.Equal(first.Teams.Select(RowSignature), second.Teams.Select(RowSignature));
        Assert.Equal(first.Rosters.Select(RowSignature), second.Rosters.Select(RowSignature));
        Assert.Equal(first.TeamsSummary, second.TeamsSummary);
        Assert.Equal(first.RostersSummary, second.RostersSummary);
    }

    [Fact]
    public async Task Requires_sport_context()
    {
        await using var db = CreateDb();

        var result = await Preview(db, teams: [TeamRow("River Plate", "CARP")], sport: "   ");

        Assert.Contains(result.Issues, issue => issue.Code == "SPORT_REQUIRED");
        Assert.Equal(ImportPreviewClassification.StructuralError, Assert.Single(result.Teams).Classification);
    }

    private static PlayPredictDbContext CreateDb() => new(new DbContextOptionsBuilder<PlayPredictDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<Team> SeedTeam(PlayPredictDbContext db, string name, string shortName, string sport)
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
            TeamId = teamId,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName,
            Position = position,
            Active = true
        };
        db.TeamPlayers.Add(player);
        await db.SaveChangesAsync();
        return player;
    }

    private static Task<TeamRosterImportPreviewResult> Preview(
        PlayPredictDbContext db,
        IReadOnlyList<ImportTeamRow>? teams = null,
        IReadOnlyList<ImportRosterRow>? rosters = null,
        IReadOnlyList<SpreadsheetValidationIssue>? issues = null,
        string sport = "F\u00fatbol") =>
        new TeamRosterImportPreviewService(db).PreviewAsync(Sheet(teams, rosters, issues), sport);

    private static SpreadsheetReadResult Sheet(
        IReadOnlyList<ImportTeamRow>? teams = null,
        IReadOnlyList<ImportRosterRow>? rosters = null,
        IReadOnlyList<SpreadsheetValidationIssue>? issues = null) =>
        new(teams ?? [], rosters ?? [], [], issues ?? []);

    private static ImportTeamRow TeamRow(string name, string shortName, int row = 2) => new(
        row, name, shortName,
        SpreadsheetTextNormalizer.Clean(name),
        SpreadsheetTextNormalizer.Clean(shortName),
        SpreadsheetTextNormalizer.Normalize(name));

    private static ImportRosterRow RosterRow(string club, string firstName, string lastName,
        string? displayName = null, int row = 2)
    {
        var cleanFirstName = SpreadsheetTextNormalizer.Clean(firstName);
        var cleanLastName = SpreadsheetTextNormalizer.Clean(lastName);
        var cleanDisplayName = SpreadsheetTextNormalizer.Clean(displayName);
        if (cleanDisplayName.Length == 0)
            cleanDisplayName = SpreadsheetTextNormalizer.Clean($"{cleanFirstName} {cleanLastName}");
        return new(row, club, firstName, lastName, displayName ?? string.Empty, "DEFENSOR",
            SpreadsheetTextNormalizer.Clean(club), cleanFirstName, cleanLastName, cleanDisplayName,
            SpreadsheetTextNormalizer.Normalize(club), SpreadsheetTextNormalizer.Normalize(firstName),
            SpreadsheetTextNormalizer.Normalize(lastName), ImportPlayerPosition.Defender);
    }

    private static string RowSignature(TeamImportPreviewRow row) =>
        $"{row.RowNumber}|{row.Classification}|{row.TeamId}|{string.Join(';', row.ProposedChanges)}";

    private static string RowSignature(RosterImportPreviewRow row) =>
        $"{row.RowNumber}|{row.Classification}|{row.TeamId}|{row.TeamPlayerId}|{string.Join(';', row.ProposedChanges)}";
}
