using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Endpoints;
using PlayPredict.Api.Services;
using Xunit;

namespace PlayPredict.Api.Tests;

public sealed class PrivateLeagueJoinTests
{
    [Theory]
    [InlineData("none", true)]
    [InlineData("former", true)]
    [InlineData("active", true)]
    [InlineData("former", false)]
    public async Task Private_id_join_is_forbidden_and_preserves_memberships(string membership, bool active)
    {
        await using var api = await JoinApi.Create(membership: membership, active: active);
        var before = await api.Snapshot();

        using var response = await api.Client.PostAsync("/api/leagues/2/join", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("código de invitación", body.GetProperty("message").GetString()!);
        Assert.Equal(before, await api.Snapshot());
    }

    [Theory]
    [InlineData("/api/leagues/2/join")]
    [InlineData("/api/leagues/0002/join/")]
    [InlineData("/API/LEAGUES/2/JOIN")]
    public async Task Private_id_join_cannot_be_bypassed_with_body_or_route_variants(string route)
    {
        await using var api = await JoinApi.Create(membership: "former");
        var before = await api.Snapshot();

        using var response = await api.Client.PostAsJsonAsync(route, new { inviteCode = JoinApi.PrivateCode });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(before, await api.Snapshot());
    }

    [Theory]
    [InlineData("none")]
    [InlineData("former")]
    public async Task Valid_code_allows_join_and_rejoin_without_duplicate_active_membership(string membership)
    {
        await using var api = await JoinApi.Create(membership: membership);
        var before = await api.Memberships();

        using var response = await api.Client.PostAsJsonAsync("/api/leagues/join",
            new { inviteCode = "  " + JoinApi.PrivateCode.ToLowerInvariant() + "  " });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, body.GetProperty("id").GetInt32());
        Assert.True(body.GetProperty("isParticipant").GetBoolean());
        var after = await api.Memberships();
        Assert.Equal(before.Count + 1, after.Count);
        Assert.Equal(before, after.Take(before.Count).ToList());
        Assert.Single(after.Where(p => p.LeagueId == 2 && p.UserId == 10 && p.LeftAtUtc is null));

        var joined = await api.Snapshot();
        using var repeated = await api.Client.PostAsJsonAsync("/api/leagues/join", new { inviteCode = JoinApi.PrivateCode });
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal(joined, await api.Snapshot());
    }

    [Theory]
    [InlineData("INVALID1", HttpStatusCode.NotFound)]
    [InlineData("", HttpStatusCode.BadRequest)]
    [InlineData("   ", HttpStatusCode.BadRequest)]
    public async Task Invalid_code_does_not_create_or_reactivate_membership(string code, HttpStatusCode status)
    {
        await using var api = await JoinApi.Create(membership: "former");
        var before = await api.Snapshot();

        using var response = await api.Client.PostAsJsonAsync("/api/leagues/join", new { inviteCode = code });

        Assert.Equal(status, response.StatusCode);
        Assert.Equal(before, await api.Snapshot());
    }

    [Theory]
    [InlineData("none")]
    [InlineData("former")]
    public async Task Active_official_league_allows_id_join_and_preserves_closed_periods(string membership)
    {
        await using var api = await JoinApi.Create(leagueId: 1, membership: membership);
        var before = await api.Memberships();

        using var response = await api.Client.PostAsync("/api/leagues/1/join", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var after = await api.Memberships();
        Assert.Equal(before.Count + 1, after.Count);
        Assert.Equal(before, after.Take(before.Count).ToList());
        Assert.Single(after.Where(p => p.LeagueId == 1 && p.UserId == 10 && p.LeftAtUtc is null));
        var joined = await api.Snapshot();
        using var repeated = await api.Client.PostAsync("/api/leagues/1/join", null);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        Assert.Equal(joined, await api.Snapshot());
    }

    [Fact]
    public async Task Inactive_official_league_rejects_id_join_without_changing_memberships()
    {
        await using var api = await JoinApi.Create(leagueId: 1, membership: "former", active: false);
        var before = await api.Snapshot();
        using var response = await api.Client.PostAsync("/api/leagues/1/join", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await api.Snapshot());
    }

    [Theory]
    [InlineData("/api/leagues/1/join")]
    [InlineData("/api/leagues/2/join")]
    [InlineData("/api/leagues/join")]
    public async Task Anonymous_join_is_unauthorized_and_preserves_memberships(string route)
    {
        await using var api = await JoinApi.Create(membership: "former");
        api.Client.DefaultRequestHeaders.Authorization = null;
        var before = await api.Snapshot();
        using var response = await api.Client.PostAsJsonAsync(route, new { inviteCode = JoinApi.PrivateCode });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(before, await api.Snapshot());
    }

    private sealed record Membership(int Id, int LeagueId, int UserId, DateTime JoinedAtUtc, DateTime? LeftAtUtc);

    private sealed class JoinApi(WebApplication app) : IAsyncDisposable
    {
        public const string PrivateCode = "TEST2345";
        private const string Key = "private-league-join-tests-only-signing-key-0123456789";
        public HttpClient Client { get; } = app.GetTestClient();

        public static async Task<JoinApi> Create(int leagueId = 2, string membership = "none", bool active = true)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing", ContentRootPath = AppContext.BaseDirectory
            });
            builder.WebHost.UseTestServer();
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = Key, ["Jwt:Issuer"] = "league-join-tests", ["Jwt:Audience"] = "league-join-tests"
            });
            builder.Logging.ClearProviders();
            builder.Services.AddSingleton<JwtTokenService>();
            builder.Services.AddScoped<LeagueScoringService>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = "league-join-tests",
                    ValidateAudience = true, ValidAudience = "league-join-tests",
                    ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
                    ValidateLifetime = true
                });
            builder.Services.AddAuthorization();
            var databaseName = Guid.NewGuid().ToString();
            builder.Services.AddDbContext<PlayPredictDbContext>(options => options.UseInMemoryDatabase(databaseName));
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapLeagueEndpoints();
            User guest;
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
                var company = new Company { Id = 1, Name = "Test", ShortName = "Test", IsActive = true };
                var creator = new User { Id = 2, CompanyId = 1, Company = company, Email = "creator@example.test", FirstName = "Test", LastName = "Creator", IsActive = true };
                guest = new User { Id = 10, CompanyId = 1, Company = company, Email = "guest@example.test", FirstName = "Test", LastName = "Guest", IsActive = true };
                var competition = new Competition { Id = 1, Name = "Test competition" };
                var edition = new Edition { Id = 1, CompetitionId = 1, Competition = competition, Name = "Test edition" };
                var official = new League { Id = 1, Name = "Official", CompetitionId = 1, Competition = competition, EditionId = 1, Edition = edition,
                    LeagueType = LeagueType.Official, InviteCode = "PUBLIC23", CreatedByUserId = 2, CreatedByUser = creator };
                var friends = new League { Id = 2, Name = "Private", CompetitionId = 1, Competition = competition, EditionId = 1, Edition = edition,
                    LeagueType = LeagueType.Private, InviteCode = PrivateCode, CreatedByUserId = 2, CreatedByUser = creator };
                (leagueId == 1 ? official : friends).IsActive = active;
                db.AddRange(company, creator, guest, competition, edition, official, friends);
                db.LeagueParticipants.Add(new LeagueParticipant { Id = 1, LeagueId = 2, UserId = 2, JoinedAtUtc = DateTime.UnixEpoch });
                if (membership != "none")
                    db.LeagueParticipants.Add(new LeagueParticipant { Id = 2, LeagueId = leagueId, UserId = 10,
                        JoinedAtUtc = DateTime.UnixEpoch.AddDays(1), LeftAtUtc = membership == "former" ? DateTime.UnixEpoch.AddDays(2) : null });
                await db.SaveChangesAsync();
            }
            await app.StartAsync();
            var api = new JoinApi(app);
            api.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                app.Services.GetRequiredService<JwtTokenService>().GenerateToken(guest, [RoleNames.Player]));
            return api;
        }

        public async Task<List<Membership>> Memberships()
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
            return await db.LeagueParticipants.AsNoTracking().OrderBy(p => p.Id)
                .Select(p => new Membership(p.Id, p.LeagueId, p.UserId, p.JoinedAtUtc, p.LeftAtUtc)).ToListAsync();
        }

        public async Task<string> Snapshot() => JsonSerializer.Serialize(await Memberships());

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
        }
    }
}
