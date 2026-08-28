using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Endpoints;
using Xunit;

namespace PlayPredict.Api.Tests;

public sealed class UserTeamPreferredPlayerTests
{
    [Fact]
    public void Model_has_unique_preference_per_user_and_team()
    {
        using var db = CreateDb();
        var index = db.Model.FindEntityType(typeof(UserTeamPreferredPlayer))!.GetIndexes()
            .Single(candidate => candidate.Properties.Select(property => property.Name).SequenceEqual(["UserId", "TeamId"]));
        Assert.True(index.IsUnique);
    }

    [Fact]
    public async Task Upsert_creates_and_changes_preference_without_duplicate()
    {
        await using var db = CreateDb();
        var data = await SeedAsync(db);

        var (created, createErrors) = await UserTeamPreferredPlayerEndpoints.UpsertAsync(db, data.User.Id, data.Home.Id, data.HomePlayer.Id, DateTime.UnixEpoch);
        Assert.Empty(createErrors);
        await db.SaveChangesAsync();
        var createdId = created!.Id;

        var (updated, updateErrors) = await UserTeamPreferredPlayerEndpoints.UpsertAsync(db, data.User.Id, data.Home.Id, data.OtherHomePlayer.Id, DateTime.UnixEpoch.AddDays(1));
        Assert.Empty(updateErrors);
        await db.SaveChangesAsync();

        Assert.Equal(createdId, updated!.Id);
        Assert.Equal(data.OtherHomePlayer.Id, updated.TeamPlayerId);
        Assert.Equal(1, await db.UserTeamPreferredPlayers.CountAsync());
    }

    [Fact]
    public async Task Upsert_rejects_player_from_other_team_and_inactive_player()
    {
        await using var db = CreateDb();
        var data = await SeedAsync(db);

        var (_, wrongTeamErrors) = await UserTeamPreferredPlayerEndpoints.UpsertAsync(db, data.User.Id, data.Home.Id, data.AwayPlayer.Id, DateTime.UtcNow);
        Assert.Contains("teamPlayerId", wrongTeamErrors);

        data.HomePlayer.Active = false;
        await db.SaveChangesAsync();
        var (_, inactiveErrors) = await UserTeamPreferredPlayerEndpoints.UpsertAsync(db, data.User.Id, data.Home.Id, data.HomePlayer.Id, DateTime.UtcNow);
        Assert.Contains("teamPlayerId", inactiveErrors);
        Assert.Empty(await db.UserTeamPreferredPlayers.ToListAsync());
    }

    [Fact]
    public async Task Removing_preference_does_not_change_prediction()
    {
        await using var db = CreateDb();
        var data = await SeedAsync(db);
        var prediction = new Prediction
        {
            UserId = data.User.Id, MatchId = data.Match.Id, PreferredPlayerId = data.OtherHomePlayer.Id,
            PredictedHomeScore = 1, PredictedAwayScore = 0, CreatedAtUtc = DateTime.UnixEpoch, UpdatedAtUtc = DateTime.UnixEpoch
        };
        db.Predictions.Add(prediction);
        var (preference, _) = await UserTeamPreferredPlayerEndpoints.UpsertAsync(db, data.User.Id, data.Home.Id, data.HomePlayer.Id, DateTime.UtcNow);
        await db.SaveChangesAsync();

        db.UserTeamPreferredPlayers.Remove(preference!);
        await db.SaveChangesAsync();

        Assert.Equal(data.OtherHomePlayer.Id, (await db.Predictions.SingleAsync()).PreferredPlayerId);
        Assert.Empty(await db.UserTeamPreferredPlayers.ToListAsync());
    }

    [Fact]
    public async Task Quick_options_support_zero_one_and_two_preferences_and_ignore_invalid_players()
    {
        await using var db = CreateDb();
        var data = await SeedAsync(db);
        var available = new[] { data.HomePlayer, data.OtherHomePlayer, data.AwayPlayer };

        Assert.Empty(PredictionEndpoints.BuildQuickPreferredPlayers(data.Match, available, new Dictionary<int, int>()));

        var one = PredictionEndpoints.BuildQuickPreferredPlayers(data.Match, available,
            new Dictionary<int, int> { [data.Home.Id] = data.HomePlayer.Id });
        Assert.Equal([data.HomePlayer.Id], one.Select(player => player.Id));

        var two = PredictionEndpoints.BuildQuickPreferredPlayers(data.Match, available,
            new Dictionary<int, int> { [data.Home.Id] = data.HomePlayer.Id, [data.Away.Id] = data.AwayPlayer.Id });
        Assert.Equal([data.HomePlayer.Id, data.AwayPlayer.Id], two.Select(player => player.Id));

        data.HomePlayer.Active = false;
        var invalid = PredictionEndpoints.BuildQuickPreferredPlayers(data.Match, available,
            new Dictionary<int, int> { [data.Home.Id] = data.HomePlayer.Id });
        Assert.Empty(invalid);
    }

    [Fact]
    public async Task Preferences_are_scoped_by_user_not_league()
    {
        await using var db = CreateDb();
        var data = await SeedAsync(db);
        var second = new User { Id = 2, CompanyId = data.User.CompanyId, FirstName = "Otra", LastName = "Persona", Email = "other@test" };
        db.Users.Add(second);
        await UserTeamPreferredPlayerEndpoints.UpsertAsync(db, data.User.Id, data.Home.Id, data.HomePlayer.Id, DateTime.UtcNow);
        await UserTeamPreferredPlayerEndpoints.UpsertAsync(db, second.Id, data.Home.Id, data.OtherHomePlayer.Id, DateTime.UtcNow);
        await db.SaveChangesAsync();

        Assert.Equal(data.HomePlayer.Id, (await db.UserTeamPreferredPlayers.SingleAsync(x => x.UserId == data.User.Id)).TeamPlayerId);
        Assert.Equal(data.OtherHomePlayer.Id, (await db.UserTeamPreferredPlayers.SingleAsync(x => x.UserId == second.Id)).TeamPlayerId);
        Assert.Null(typeof(UserTeamPreferredPlayer).GetProperty("LeagueId"));
    }

    private static PlayPredictDbContext CreateDb() => new(new DbContextOptionsBuilder<PlayPredictDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<TestData> SeedAsync(PlayPredictDbContext db)
    {
        var company = new Company { Id = 1, Name = "Test" };
        var user = new User { Id = 1, Company = company, FirstName = "Rafael", LastName = "Demo", Email = "rafael@test" };
        var home = new Team { Id = 1, Name = "Boca", ShortName = "BOC" };
        var away = new Team { Id = 2, Name = "River", ShortName = "RIV" };
        var homePlayer = Player(1, home, "Cavani");
        var otherHomePlayer = Player(2, home, "Merentiel");
        var awayPlayer = Player(3, away, "Colidio");
        var edition = new Edition { Id = 1, CompetitionId = 1, Name = "2026" };
        var round = new Round { Id = 1, Edition = edition, Name = "Fecha 1", Order = 1 };
        var match = new Match
        {
            Id = 1, Round = round, RoundId = round.Id, HomeTeam = home, HomeTeamId = home.Id,
            AwayTeam = away, AwayTeamId = away.Id, ParticipantHome = home.Name, ParticipantAway = away.Name,
            StartsAtUtc = DateTime.UtcNow.AddDays(1)
        };
        db.AddRange(company, user, home, away, homePlayer, otherHomePlayer, awayPlayer, edition, round, match);
        await db.SaveChangesAsync();
        return new(user, home, away, homePlayer, otherHomePlayer, awayPlayer, match);
    }

    private static TeamPlayer Player(int id, Team team, string name) => new()
    {
        Id = id, Team = team, TeamId = team.Id, FirstName = name, LastName = "", DisplayName = name,
        Position = "Delantero", Active = true
    };

    private sealed record TestData(User User, Team Home, Team Away, TeamPlayer HomePlayer,
        TeamPlayer OtherHomePlayer, TeamPlayer AwayPlayer, Match Match);
}
