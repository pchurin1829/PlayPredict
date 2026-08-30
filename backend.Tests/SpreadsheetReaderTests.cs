using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Imports;
using Xunit;

namespace PlayPredict.Api.Tests;

public class SpreadsheetReaderTests
{
    private readonly SpreadsheetReader reader = new();

    [Fact]
    public void Reads_valid_xlsx_teams_and_rosters_contract()
    {
        using var stream = SpreadsheetTestWorkbook.CreateXlsx(ValidTeams(), ValidRosters());

        var result = reader.Read(stream, "importacion.xlsx", SpreadsheetImportKind.TeamsAndRosters);

        Assert.True(result.IsValid);
        var team = Assert.Single(result.Teams);
        Assert.Equal(("River Plate", "River", "RIVER PLATE"), (team.Name, team.ShortName, team.NormalizedName));
        var player = Assert.Single(result.Rosters);
        Assert.Equal(("Juan", "P\u00e9rez", "Juan P\u00e9rez", ImportPlayerPosition.Midfielder),
            (player.FirstName, player.LastName, player.DisplayName, player.Position));
    }

    [Fact]
    public void Reads_valid_binary_xls_contract()
    {
        using var stream = SpreadsheetTestWorkbook.CreateXls(ValidMatches());

        var result = reader.Read(stream, "fixture.xls", SpreadsheetImportKind.Matches);

        Assert.True(result.IsValid);
        var match = Assert.Single(result.Matches);
        Assert.Equal((7, new DateOnly(2026, 8, 28), new TimeOnly(21, 30), ImportMatchStatus.Scheduled),
            (match.RoundNumber, match.Date, match.Time, match.Status));
    }

    [Fact]
    public void Reports_missing_required_sheet()
    {
        using var stream = SpreadsheetTestWorkbook.CreateXlsx(ValidTeams());

        var result = reader.Read(stream, "equipos.xlsx", SpreadsheetImportKind.TeamsAndRosters);

        Assert.Contains(result.Issues, issue => issue.Code == "MISSING_SHEET" && issue.SheetName == SpreadsheetReader.RostersSheet);
    }

    [Fact]
    public void Reports_missing_and_unknown_headers()
    {
        var teams = new SheetData(SpreadsheetReader.TeamsSheet,
            ["NOMBRE DEL EQUIPO", "COLUMNA EXTRA"],
            ["River Plate", "dato"]);
        using var stream = SpreadsheetTestWorkbook.CreateXlsx(teams, ValidRosters());

        var result = reader.Read(stream, "equipos.xlsx", SpreadsheetImportKind.TeamsAndRosters);

        Assert.Contains(result.Issues, issue => issue.Code == "MISSING_HEADER" && issue.ColumnName == "NOMBRE CORTO");
        Assert.Contains(result.Issues, issue => issue.Code == "UNKNOWN_HEADER" && issue.ColumnName == "COLUMNA EXTRA");
    }

    [Fact]
    public void Reports_empty_and_required_value_rows_with_original_numbers()
    {
        var teams = new SheetData(SpreadsheetReader.TeamsSheet,
            ["NOMBRE DEL EQUIPO", "NOMBRE CORTO"],
            [null, null],
            ["River Plate", null]);
        using var stream = SpreadsheetTestWorkbook.CreateXlsx(teams, ValidRosters());

        var result = reader.Read(stream, "equipos.xlsx", SpreadsheetImportKind.TeamsAndRosters);

        Assert.Contains(result.Issues, issue => issue.Code == "EMPTY_ROW" && issue.RowNumber == 2);
        Assert.Contains(result.Issues, issue => issue.Code == "REQUIRED_VALUE" && issue.RowNumber == 3 && issue.ColumnName == "NOMBRE CORTO");
    }

    [Fact]
    public void Reports_invalid_position()
    {
        var rosters = new SheetData(SpreadsheetReader.RostersSheet,
            RosterHeaders(),
            ["River Plate", "Juan", "P\u00e9rez", "", "VOLANTE"]);
        using var stream = SpreadsheetTestWorkbook.CreateXlsx(ValidTeams(), rosters);

        var result = reader.Read(stream, "equipos.xlsx", SpreadsheetImportKind.TeamsAndRosters);

        Assert.Contains(result.Issues, issue => issue.Code == "INVALID_POSITION" && issue.RowNumber == 2);
    }

    [Theory]
    [InlineData("FINISHED")]
    [InlineData("POSTPONED")]
    public void Reports_invalid_match_status(string status)
    {
        using var stream = SpreadsheetTestWorkbook.CreateXlsx(MatchesWith([7, "2026-08-28", "21:30", "Boca Juniors", "River Plate", status]));

        var result = reader.Read(stream, "fixture.xlsx", SpreadsheetImportKind.Matches);

        Assert.Contains(result.Issues, issue => issue.Code == "INVALID_STATUS" && issue.RowNumber == 2);
    }

    [Theory]
    [InlineData("28/08/2026", "21:30", "INVALID_DATE")]
    [InlineData("2026-08-28", "25:15", "INVALID_TIME")]
    public void Reports_invalid_date_or_time(string date, string time, string expectedCode)
    {
        using var stream = SpreadsheetTestWorkbook.CreateXlsx(MatchesWith([7, date, time, "Boca Juniors", "River Plate", "SCHEDULED"]));

        var result = reader.Read(stream, "fixture.xlsx", SpreadsheetImportKind.Matches);

        Assert.Contains(result.Issues, issue => issue.Code == expectedCode && issue.RowNumber == 2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1.5)]
    [InlineData("Fecha 7")]
    public void Reports_invalid_round_number(object roundNumber)
    {
        using var stream = SpreadsheetTestWorkbook.CreateXlsx(
            MatchesWith([roundNumber, "2026-08-28", "21:30", "Boca Juniors", "River Plate", "SCHEDULED"]));

        var result = reader.Read(stream, "fixture.xlsx", SpreadsheetImportKind.Matches);

        Assert.Contains(result.Issues, issue => issue.Code == "INVALID_ROUND_NUMBER" && issue.RowNumber == 2);
    }

    [Fact]
    public void Normalizes_spaces_and_case_without_changing_original_values()
    {
        var teams = new SheetData(SpreadsheetReader.TeamsSheet,
            [" nombre   del equipo ", "nombre corto"],
            ["  River   Plate  ", "  RIVER  "]);
        var rosters = new SheetData(SpreadsheetReader.RostersSheet,
            RosterHeaders(),
            [" river   plate ", "  juan ", " p\u00c9REZ  ", "", " mediocampista "]);
        using var stream = SpreadsheetTestWorkbook.CreateXlsx(teams, rosters);

        var result = reader.Read(stream, "equipos.xlsx", SpreadsheetImportKind.TeamsAndRosters);

        Assert.True(result.IsValid);
        Assert.Equal("  River   Plate  ", result.Teams[0].OriginalName);
        Assert.Equal("River Plate", result.Teams[0].Name);
        Assert.Equal("RIVER PLATE", result.Rosters[0].NormalizedClubName);
        Assert.Equal(ImportPlayerPosition.Midfielder, result.Rosters[0].Position);
    }

    [Fact]
    public void Reports_duplicates_in_each_contract_sheet()
    {
        var teams = new SheetData(SpreadsheetReader.TeamsSheet,
            ["NOMBRE DEL EQUIPO", "NOMBRE CORTO"],
            ["River Plate", "River"],
            [" river   plate ", "CARP"]);
        var rosters = new SheetData(SpreadsheetReader.RostersSheet,
            RosterHeaders(),
            ["River Plate", "Juan", "Perez", "", "DEFENSOR"],
            [" river plate ", " juan ", " pEREZ ", "Juan P.", "DEFENSOR"]);
        using var teamsStream = SpreadsheetTestWorkbook.CreateXlsx(teams, rosters);
        var teamsResult = reader.Read(teamsStream, "equipos.xlsx", SpreadsheetImportKind.TeamsAndRosters);

        var matches = MatchesWith(
            [7, "2026-08-28", "21:30", "Boca Juniors", "River Plate", "SCHEDULED"],
            [7, "2026-08-29", "19:00", " boca juniors ", " river plate ", "SUSPENDED"]);
        using var matchesStream = SpreadsheetTestWorkbook.CreateXlsx(matches);
        var matchesResult = reader.Read(matchesStream, "fixture.xlsx", SpreadsheetImportKind.Matches);

        Assert.Contains(teamsResult.Issues, issue => issue.Code == "DUPLICATE_TEAM_ROW");
        Assert.Contains(teamsResult.Issues, issue => issue.Code == "DUPLICATE_ROSTER_ROW");
        Assert.Contains(matchesResult.Issues, issue => issue.Code == "DUPLICATE_MATCH_ROW");
    }

    [Fact]
    public async Task Reading_a_preview_does_not_modify_database()
    {
        await using var db = new PlayPredictDbContext(new DbContextOptionsBuilder<PlayPredictDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Teams.Add(new Team { Name = "Existente", ShortName = "EX", Sport = "F\u00fatbol", Active = true });
        await db.SaveChangesAsync();
        var before = await db.Teams.CountAsync();
        using var stream = SpreadsheetTestWorkbook.CreateXlsx(ValidTeams(), ValidRosters());

        var result = reader.Read(stream, "equipos.xlsx", SpreadsheetImportKind.TeamsAndRosters);

        Assert.True(result.IsValid);
        Assert.Equal(before, await db.Teams.CountAsync());
        Assert.Empty(await db.TeamPlayers.ToListAsync());
    }

    private static SheetData ValidTeams() => new(SpreadsheetReader.TeamsSheet,
        ["NOMBRE DEL EQUIPO", "NOMBRE CORTO"],
        ["River Plate", "River"]);

    private static object?[] RosterHeaders() => ["NOMBRE DEL CLUB", "NOMBRE", "APELLIDO", "NOMBRE PARA MOSTRAR", "POSICION"];

    private static SheetData ValidRosters() => new(SpreadsheetReader.RostersSheet,
        RosterHeaders(),
        ["River Plate", "Juan", "P\u00e9rez", "", "MEDIOCAMPISTA"]);

    private static SheetData ValidMatches() => MatchesWith([7, "2026-08-28", "21:30", "Boca Juniors", "River Plate", "SCHEDULED"]);

    private static SheetData MatchesWith(params object?[][] rows) => new(SpreadsheetReader.MatchesSheet,
        [["FECHA_NRO", "FECHA", "HORA", "LOCAL", "VISITANTE", "ESTADO"], .. rows]);
}
