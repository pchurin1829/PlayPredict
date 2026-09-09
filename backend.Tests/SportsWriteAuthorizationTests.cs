using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Endpoints;
using PlayPredict.Api.Services;
using Xunit;

namespace PlayPredict.Api.Tests;

public class SportsWriteAuthorizationTests
{
    public static IEnumerable<object?[]> AccessCases =>
        Enumerable.Range(0, 6).SelectMany(operation => new string?[] { null, RoleNames.Player, RoleNames.Admin }
            .Select(role => new object?[] { operation, role }));

    [Theory]
    [MemberData(nameof(AccessCases))]
    public async Task Writes_require_admin_and_rejected_requests_preserve_data(int operation, string? role)
    {
        await using var api = await SportsApi.Create();
        var before = await api.Snapshot();
        var create = operation % 2 == 0;
        var path = operation switch
        {
            0 => "/api/competitions",
            1 => "/api/competitions/1",
            2 => "/api/competitions/1/editions",
            3 => "/api/editions/1",
            4 => "/api/editions/1/rounds",
            _ => "/api/rounds/1"
        };
        object payload = operation switch
        {
            0 or 1 => new { name = "Changed competition", description = "Changed description", sport = "Football", isActive = false, experienceId = 1 },
            2 or 3 => new { name = "Changed edition", startDateUtc = "2026-10-01T00:00:00Z", endDateUtc = "2026-12-01T00:00:00Z", status = "Active" },
            _ => new { name = "Changed round", order = 2, startDateUtc = "2026-10-02T00:00:00Z", endDateUtc = "2026-10-03T00:00:00Z" }
        };
        using var request = new HttpRequestMessage(create ? HttpMethod.Post : HttpMethod.Put, path)
        {
            Content = JsonContent.Create(payload)
        };
        if (role is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", api.Token(role));
        using var response = await api.Client.SendAsync(request);

        if (role != RoleNames.Admin)
        {
            Assert.Equal(role is null ? HttpStatusCode.Unauthorized : HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal(before, await api.Snapshot());
            return;
        }

        Assert.Equal(create ? HttpStatusCode.Created : HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(create ? 2 : 1, body.GetProperty("id").GetInt32());
        Assert.Equal(operation < 2 ? "Changed competition" : operation < 4 ? "Changed edition" : "Changed round", body.GetProperty("name").GetString());
        if (create) Assert.Equal($"{(operation == 0 ? "/api/competitions" : operation == 2 ? "/api/editions" : "/api/rounds")}/2", response.Headers.Location?.OriginalString);
        await api.AssertPersisted(operation);
    }

    private sealed class SportsApi(WebApplication app, SqliteConnection connection) : IAsyncDisposable
    {
        public HttpClient Client { get; } = app.GetTestClient();
        private const string Key = "sports-authorization-tests-only-signing-key-0123456789";

        public static async Task<SportsApi> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = Key, ["Jwt:Issuer"] = "sports-tests", ["Jwt:Audience"] = "sports-tests", ["Jwt:ExpiresMinutes"] = "5"
            });
            builder.Services.AddSingleton<JwtTokenService>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = "sports-tests",
                    ValidateAudience = true, ValidAudience = "sports-tests",
                    ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
                    ValidateLifetime = true
                });
            builder.Services.AddAuthorization();
            builder.Services.AddDbContext<PlayPredictDbContext>(options => options.UseSqlite(connection));
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapCompetitionEndpoints();
            app.MapEditionEndpoints();
            app.MapRoundEndpoints();
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
                await db.Database.EnsureCreatedAsync();
                var experience = new Experience { Id = 1, Name = "Test experience" };
                var competition = new Competition { Id = 1, Experience = experience, Name = "Original competition", Sport = "Football", IsActive = true };
                var edition = new Edition { Id = 1, Competition = competition, Name = "Original edition", Status = EditionStatus.Draft, StartDateUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc) };
                db.Rounds.Add(new Round { Id = 1, Edition = edition, Name = "Original round", Order = 1 });
                await db.SaveChangesAsync();
            }
            await app.StartAsync();
            return new SportsApi(app, connection);
        }

        public string Token(string role) => app.Services.GetRequiredService<JwtTokenService>()
            .GenerateToken(new User { Id = 1, CompanyId = 1, FirstName = "Test", LastName = "User", Email = "test@example.test" }, [role]);

        public async Task<string> Snapshot()
        {
            // Fresh context and scalar projections detect updates as well as inserted rows.
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
            return JsonSerializer.Serialize(new
            {
                Experiences = await db.Experiences.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Name }).ToListAsync(),
                Competitions = await db.Competitions.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.ExperienceId, x.Name, x.Description, x.Sport, x.IsActive, x.CreatedAtUtc }).ToListAsync(),
                Editions = await db.Editions.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.CompetitionId, x.Name, x.StartDateUtc, x.EndDateUtc, x.Status, x.CreatedAtUtc }).ToListAsync(),
                Rounds = await db.Rounds.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.EditionId, x.Name, x.Order, x.StartDateUtc, x.EndDateUtc }).ToListAsync(),
                Scoring = await db.EditionScoringConfigurations.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.EditionId, x.ExactScorePoints, x.CorrectOutcomePoints, x.IncorrectPoints }).ToListAsync()
            });
        }

        public async Task AssertPersisted(int operation)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
            Assert.Equal(operation == 0 ? 2 : 1, await db.Competitions.CountAsync());
            Assert.Equal(operation == 2 ? 2 : 1, await db.Editions.CountAsync());
            Assert.Equal(operation == 4 ? 2 : 1, await db.Rounds.CountAsync());
            Assert.Equal(operation == 2 ? 1 : 0, await db.EditionScoringConfigurations.CountAsync());
            var id = operation % 2 == 0 ? 2 : 1;
            if (operation < 2)
            {
                var entity = await db.Competitions.SingleAsync(x => x.Id == id);
                Assert.Equal("Changed competition", entity.Name);
                Assert.Equal("Changed description", entity.Description);
                Assert.Equal("Football", entity.Sport);
                Assert.False(entity.IsActive);
                Assert.Equal(1, entity.ExperienceId);
            }
            else if (operation < 4)
            {
                var entity = await db.Editions.SingleAsync(x => x.Id == id);
                Assert.Equal("Changed edition", entity.Name);
                Assert.Equal(EditionStatus.Active, entity.Status);
                Assert.Equal(1, entity.CompetitionId);
                Assert.Equal(new DateTime(2026, 10, 1), entity.StartDateUtc);
                Assert.Equal(new DateTime(2026, 12, 1), entity.EndDateUtc);
                if (operation == 2)
                {
                    var scoring = await db.EditionScoringConfigurations.SingleAsync();
                    Assert.Equal(id, scoring.EditionId);
                    Assert.Equal((6, 3, 0), (scoring.ExactScorePoints, scoring.CorrectOutcomePoints, scoring.IncorrectPoints));
                }
            }
            else
            {
                var entity = await db.Rounds.SingleAsync(x => x.Id == id);
                Assert.Equal("Changed round", entity.Name);
                Assert.Equal(2, entity.Order);
                Assert.Equal(1, entity.EditionId);
                Assert.Equal(new DateTime(2026, 10, 2), entity.StartDateUtc);
                Assert.Equal(new DateTime(2026, 10, 3), entity.EndDateUtc);
            }
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
