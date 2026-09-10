using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Endpoints;
using Xunit;

namespace PlayPredict.Api.Tests;

// P1.1: edición por creador y expulsión con períodos de membresía.
// Sin Docker: InMemory + helpers internos.
public sealed class PrivateLeagueMembershipTests
{
    [Fact]
    public async Task Creator_edits_name_and_description()
    {
        await using var db = CreateDb();
        var (creator, league) = await SeedPrivateLeagueAsync(db);

        var (result, errors, updated) = await LeagueEndpoints.UpdateLeagueAsync(
            db, league.Id, new("Nuevo nombre", "Nueva descripción", true), creator.Id);

        Assert.Equal(LeagueEndpoints.LeagueUpdateResult.Updated, result);
        Assert.Empty(errors);
        Assert.Equal("Nuevo nombre", updated!.Name);
        Assert.Equal("Nueva descripción", updated.Description);
        Assert.Equal("Nuevo nombre", (await db.Leagues.FindAsync(league.Id))!.Name);
    }

    [Fact]
    public async Task Non_creator_cannot_edit()
    {
        await using var db = CreateDb();
        var (_, league) = await SeedPrivateLeagueAsync(db);
        var other = await AddUserAsync(db, "other@test.local");

        var (result, _, updated) = await LeagueEndpoints.UpdateLeagueAsync(
            db, league.Id, new("Hack", null, true), other.Id);

        Assert.Equal(LeagueEndpoints.LeagueUpdateResult.Forbidden, result);
        Assert.Null(updated);
        Assert.Equal("Liga de amigos", (await db.Leagues.FindAsync(league.Id))!.Name);
    }

    [Fact]
    public async Task Creator_expel_closes_membership_and_keeps_prediction()
    {
        await using var db = CreateDb();
        var (creator, league) = await SeedPrivateLeagueAsync(db);
        var member = await AddUserAsync(db, "member@test.local");
        db.LeagueParticipants.Add(new LeagueParticipant
            { LeagueId = league.Id, UserId = member.Id, JoinedAtUtc = DateTime.UtcNow.AddDays(-2) });
        var prediction = new Prediction
        {
            MatchId = 500, UserId = member.Id, PredictedHomeScore = 2, PredictedAwayScore = 1,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1), UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        db.Predictions.Add(prediction);
        await db.SaveChangesAsync();

        var outcome = await LeagueEndpoints.ExpelParticipantAsync(db, league.Id, member.Id, creator.Id);

        Assert.Equal(LeagueEndpoints.LeagueExpelResult.Expelled, outcome);
        var membership = await db.LeagueParticipants
            .SingleAsync(lp => lp.LeagueId == league.Id && lp.UserId == member.Id);
        Assert.NotNull(membership.LeftAtUtc);
        // El pronóstico global sobrevive a la expulsión.
        Assert.NotNull(await db.Predictions.FindAsync(prediction.Id));
    }

    [Fact]
    public async Task Non_creator_cannot_expel()
    {
        await using var db = CreateDb();
        var (_, league) = await SeedPrivateLeagueAsync(db);
        var member = await AddUserAsync(db, "member@test.local");
        var other = await AddUserAsync(db, "other@test.local");
        db.LeagueParticipants.Add(new LeagueParticipant
            { LeagueId = league.Id, UserId = member.Id, JoinedAtUtc = DateTime.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync();

        var outcome = await LeagueEndpoints.ExpelParticipantAsync(db, league.Id, member.Id, other.Id);

        Assert.Equal(LeagueEndpoints.LeagueExpelResult.Forbidden, outcome);
        Assert.Null((await db.LeagueParticipants
            .SingleAsync(lp => lp.LeagueId == league.Id && lp.UserId == member.Id)).LeftAtUtc);
    }

    [Fact]
    public async Task Creator_cannot_expel_self()
    {
        await using var db = CreateDb();
        var (creator, league) = await SeedPrivateLeagueAsync(db);

        var outcome = await LeagueEndpoints.ExpelParticipantAsync(db, league.Id, creator.Id, creator.Id);

        Assert.Equal(LeagueEndpoints.LeagueExpelResult.SelfExpel, outcome);
        Assert.Null((await db.LeagueParticipants
            .SingleAsync(lp => lp.LeagueId == league.Id && lp.UserId == creator.Id)).LeftAtUtc);
    }

    [Fact]
    public async Task Official_league_rejects_expel()
    {
        await using var db = CreateDb();
        var (creator, league) = await SeedPrivateLeagueAsync(db);
        league.LeagueType = LeagueType.Official;
        var member = await AddUserAsync(db, "member@test.local");
        db.LeagueParticipants.Add(new LeagueParticipant
            { LeagueId = league.Id, UserId = member.Id, JoinedAtUtc = DateTime.UtcNow.AddDays(-1) });
        await db.SaveChangesAsync();

        var outcome = await LeagueEndpoints.ExpelParticipantAsync(db, league.Id, member.Id, creator.Id);

        Assert.Equal(LeagueEndpoints.LeagueExpelResult.Forbidden, outcome);
    }

    [Fact]
    public async Task Expel_of_non_participant_returns_not_active()
    {
        await using var db = CreateDb();
        var (creator, league) = await SeedPrivateLeagueAsync(db);
        var stranger = await AddUserAsync(db, "stranger@test.local");

        Assert.Equal(LeagueEndpoints.LeagueExpelResult.NotActiveParticipant,
            await LeagueEndpoints.ExpelParticipantAsync(db, league.Id, stranger.Id, creator.Id));
        Assert.Equal(LeagueEndpoints.LeagueExpelResult.LeagueNotFound,
            await LeagueEndpoints.ExpelParticipantAsync(db, 99999, stranger.Id, creator.Id));
    }

    [Fact]
    public async Task Rejoin_after_expel_creates_new_period()
    {
        await using var db = CreateDb();
        var (creator, league) = await SeedPrivateLeagueAsync(db);
        var member = await AddUserAsync(db, "member@test.local");
        db.LeagueParticipants.Add(new LeagueParticipant
            { LeagueId = league.Id, UserId = member.Id, JoinedAtUtc = DateTime.UtcNow.AddDays(-3) });
        await db.SaveChangesAsync();

        Assert.Equal(LeagueEndpoints.LeagueExpelResult.Expelled,
            await LeagueEndpoints.ExpelParticipantAsync(db, league.Id, member.Id, creator.Id));

        // Reingreso: nueva fila, como hacen los endpoints de join.
        db.LeagueParticipants.Add(new LeagueParticipant
            { LeagueId = league.Id, UserId = member.Id, JoinedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var periods = await db.LeagueParticipants
            .Where(lp => lp.LeagueId == league.Id && lp.UserId == member.Id)
            .OrderBy(lp => lp.JoinedAtUtc).ToListAsync();
        Assert.Equal(2, periods.Count);
        Assert.NotNull(periods[0].LeftAtUtc);
        Assert.Null(periods[1].LeftAtUtc);
    }

    [Fact]
    public void Eligibility_respects_absence_period()
    {
        var startsAt = DateTime.UtcNow.AddDays(1);
        var league = new League { CreatedAtUtc = startsAt.AddDays(-10) };
        var match = new Match { StartsAtUtc = startsAt };
        var prediction = new Prediction { CreatedAtUtc = startsAt.AddHours(-2) };

        // Expulsado antes del partido: ningún período lo cubre.
        var absent = new List<LeagueParticipant>
        {
            new() { JoinedAtUtc = startsAt.AddDays(-5), LeftAtUtc = startsAt.AddHours(-1) }
        };
        Assert.False(PredictionEndpoints.IsEligible(prediction, league, match, absent));

        // Miembro activo: sí puntúa.
        var present = new List<LeagueParticipant>
        {
            new() { JoinedAtUtc = startsAt.AddDays(-5), LeftAtUtc = null }
        };
        Assert.True(PredictionEndpoints.IsEligible(prediction, league, match, present));

        // Reingreso posterior al inicio: no recupera esos puntos.
        var rejoinedLate = new List<LeagueParticipant>
        {
            new() { JoinedAtUtc = startsAt.AddDays(-5), LeftAtUtc = startsAt.AddHours(-1) },
            new() { JoinedAtUtc = startsAt.AddHours(1), LeftAtUtc = null }
        };
        Assert.False(PredictionEndpoints.IsEligible(prediction, league, match, rejoinedLate));
    }

    private static PlayPredictDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<PlayPredictDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new PlayPredictDbContext(options);
    }

    private static async Task<User> AddUserAsync(PlayPredictDbContext db, string email)
    {
        var user = new User
        {
            CompanyId = 1, Email = email, FirstName = "Test", LastName = "User", IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<(User Creator, League League)> SeedPrivateLeagueAsync(PlayPredictDbContext db)
    {
        var creator = await AddUserAsync(db, "creator@test.local");
        var league = new League
        {
            Name = "Liga de amigos", Description = "Desc", CompetitionId = 41, EditionId = 42,
            ScopeType = LeagueScopeType.FullCompetition, LeagueType = LeagueType.Private,
            InviteCode = "CODE-" + Guid.NewGuid().ToString("N")[..4].ToUpperInvariant(),
            IsActive = true, CreatedByUserId = creator.Id, CreatedAtUtc = DateTime.UtcNow.AddDays(-7),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-7)
        };
        db.Leagues.Add(league);
        await db.SaveChangesAsync();
        db.LeagueParticipants.Add(new LeagueParticipant
            { LeagueId = league.Id, UserId = creator.Id, JoinedAtUtc = league.CreatedAtUtc });
        await db.SaveChangesAsync();
        return (creator, league);
    }
}
