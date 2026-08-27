using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Endpoints;
using PlayPredict.Api.Services;
using Xunit;

namespace PlayPredict.Api.Tests;

public sealed class PrivateLeagueCreationTests
{
    [Fact]
    public async Task Full_source_scope_is_copied_with_source_and_effective_scoring_snapshot()
    {
        await using var db = CreateDb();
        var (company, user, source) = await SeedSourceAsync(db);
        var result = await CreateAsync(db, user, source, "FullCompetition");

        Assert.Empty(result.Errors);
        var created = Assert.IsType<League>(result.League);
        Assert.Equal(LeagueType.Private, created.LeagueType);
        Assert.Equal(source.Id, created.SourceLeagueId);
        Assert.Equal(source.CompetitionId, created.CompetitionId);
        Assert.Equal(source.EditionId, created.EditionId);
        Assert.Equal(source.ScopeType, created.ScopeType);
        Assert.Equal(source.RoundFromId, created.RoundFromId);
        Assert.Equal(source.RoundToId, created.RoundToId);
        Assert.True(created.IsActive);
        Assert.False(created.UseGeneralScoring);
        Assert.Equal(company.GeneralExactScorePoints, created.ExactScorePoints);
        Assert.Equal(company.GeneralCorrectOutcomePoints, created.CorrectOutcomePoints);
        Assert.Equal(company.GeneralIncorrectPoints, created.IncorrectPoints);
        Assert.Equal(company.GeneralPreferredPlayerEnabled, created.PreferredPlayerEnabled);
        Assert.Equal(company.GeneralPreferredPlayerPointsPerGoal, created.PreferredPlayerPointsPerGoal);
        Assert.Equal(company.GeneralPreferredPlayerPositions, created.PreferredPlayerPositions);
        Assert.Matches("^[A-HJ-NP-Z2-9]{8}$", created.InviteCode);
        Assert.True(await db.LeagueParticipants.AnyAsync(p => p.LeagueId == created.Id && p.UserId == user.Id));

        company.GeneralExactScorePoints = 99;
        source.ExactScorePoints = 88;
        await db.SaveChangesAsync();
        Assert.Equal(11, created.ExactScorePoints);
    }

    [Theory]
    [InlineData(4, 6)]
    [InlineData(7, 7)]
    public async Task Contained_range_including_single_round_is_allowed(int fromOrder, int toOrder)
    {
        await using var db = CreateDb();
        var (_, user, source) = await SeedSourceAsync(db);
        var from = await RoundByOrderAsync(db, source.EditionId, fromOrder);
        var to = await RoundByOrderAsync(db, source.EditionId, toOrder);

        var result = await CreateAsync(db, user, source, "RoundRange", from.Id, to.Id);

        Assert.Empty(result.Errors);
        Assert.Equal(LeagueScopeType.RoundRange, result.League!.ScopeType);
        Assert.Equal(from.Id, result.League.RoundFromId);
        Assert.Equal(to.Id, result.League.RoundToId);
        Assert.Equal(source.Id, result.League.SourceLeagueId);
    }

    [Theory]
    [InlineData(2, 6)]
    [InlineData(4, 9)]
    public async Task Range_outside_official_bounds_is_rejected(int fromOrder, int toOrder)
    {
        await using var db = CreateDb();
        var (_, user, source) = await SeedSourceAsync(db);
        var from = await RoundByOrderAsync(db, source.EditionId, fromOrder);
        var to = await RoundByOrderAsync(db, source.EditionId, toOrder);

        var result = await CreateAsync(db, user, source, "RoundRange", from.Id, to.Id);

        Assert.Null(result.League);
        Assert.Contains("roundFromId", result.Errors);
    }

    [Fact]
    public async Task Rounds_from_another_edition_are_rejected()
    {
        await using var db = CreateDb();
        var (_, user, source) = await SeedSourceAsync(db);
        var foreign = await db.Rounds.SingleAsync(r => r.EditionId == 99);

        var result = await CreateAsync(db, user, source, "RoundRange", foreign.Id, foreign.Id);

        Assert.Null(result.League);
        Assert.Contains("roundFromId", result.Errors);
    }

    [Fact]
    public async Task From_after_to_is_rejected()
    {
        await using var db = CreateDb();
        var (_, user, source) = await SeedSourceAsync(db);
        var from = await RoundByOrderAsync(db, source.EditionId, 7);
        var to = await RoundByOrderAsync(db, source.EditionId, 4);

        var result = await CreateAsync(db, user, source, "RoundRange", from.Id, to.Id);

        Assert.Null(result.League);
        Assert.Contains("roundFromId", result.Errors);
    }

    [Fact]
    public async Task Missing_private_or_inactive_source_is_rejected()
    {
        await using var db = CreateDb();
        var (_, user, source) = await SeedSourceAsync(db);
        var missing = await LeagueEndpoints.CreatePrivateLeagueAsync(db, new LeagueScoringService(),
            new("Amigos", null, 99999, "FullCompetition", null, null), user.Id);
        Assert.Contains("officialLeagueId", missing.Errors);

        source.LeagueType = LeagueType.Private;
        await db.SaveChangesAsync();
        var privateResult = await CreateAsync(db, user, source, "FullCompetition");
        Assert.Contains("officialLeagueId", privateResult.Errors);

        source.LeagueType = LeagueType.Official;
        source.IsActive = false;
        await db.SaveChangesAsync();
        var inactive = await CreateAsync(db, user, source, "FullCompetition");
        Assert.Contains("officialLeagueId", inactive.Errors);
        Assert.Equal(0, await db.Leagues.CountAsync(l => l.SourceLeagueId != null));
    }

    [Fact]
    public void Player_contract_exposes_only_source_and_scope_but_not_competition_edition_or_scoring()
    {
        var properties = typeof(CreateLeagueDto).GetProperties().Select(property => property.Name).ToArray();
        Assert.Equal(["Name", "Description", "OfficialLeagueId", "ScopeType", "RoundFromId", "RoundToId"], properties);
    }

    private static Task<(League? League, Dictionary<string, string[]> Errors)> CreateAsync(
        PlayPredictDbContext db, User user, League source, string scopeType, int? fromId = null, int? toId = null) =>
        LeagueEndpoints.CreatePrivateLeagueAsync(db, new LeagueScoringService(),
            new("Amigos", "Descripción", source.Id, scopeType, fromId, toId), user.Id);

    private static Task<Round> RoundByOrderAsync(PlayPredictDbContext db, int editionId, int order) =>
        db.Rounds.SingleAsync(round => round.EditionId == editionId && round.Order == order);

    private static PlayPredictDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PlayPredictDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new PlayPredictDbContext(options);
    }

    private static async Task<(Company Company, User User, League Source)> SeedSourceAsync(PlayPredictDbContext db)
    {
        var company = new Company { Name = "Test", ShortName = "Test", IsActive = true, GeneralExactScorePoints = 11,
            GeneralCorrectOutcomePoints = 7, GeneralIncorrectPoints = 1, GeneralPreferredPlayerEnabled = false,
            GeneralPreferredPlayerPointsPerGoal = 5, GeneralPreferredPlayerPositions = PlayerPosition.Defender };
        var user = new User { Company = company, Email = "player@test.local", FirstName = "Test", LastName = "Player", IsActive = true };
        var rounds = Enumerable.Range(2, 8).Select(order => new Round { Id = 100 + order, EditionId = 42, Name = $"Fecha {order}", Order = order }).ToList();
        var foreignRound = new Round { Id = 201, EditionId = 99, Name = "Otra Fecha", Order = 4 };
        var source = new League { Name = "COPA TEST", CompetitionId = 41, EditionId = 42, ScopeType = LeagueScopeType.RoundRange,
            RoundFromId = rounds.Single(r => r.Order == 3).Id, RoundToId = rounds.Single(r => r.Order == 8).Id,
            LeagueType = LeagueType.Official, IsActive = true, InviteCode = "OFF-TEST", CreatedByUser = user,
            UseGeneralScoring = true, ExactScorePoints = 6, CorrectOutcomePoints = 3, IncorrectPoints = 0,
            PreferredPlayerEnabled = true, PreferredPlayerPointsPerGoal = 2, PreferredPlayerPositions = PlayerPosition.Forward };
        db.AddRange(company, user, source);
        db.Rounds.AddRange(rounds.Append(foreignRound));
        await db.SaveChangesAsync();
        return (company, user, source);
    }
}
