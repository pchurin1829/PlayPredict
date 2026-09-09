using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Endpoints;
using PlayPredict.Api.Services;
using Xunit;

namespace PlayPredict.Api.Tests;

public class CompetitionCreationTests
{
    [Fact]
    public async Task Db0_admin_can_create_competition_edition_round_fixture_and_playable_league()
    {
        await using var api = await Db0Api.Create();
        var experiences = await api.Client.GetFromJsonAsync<ExperienceDto[]>("/api/admin/experiences");
        var experience = Assert.Single(experiences!);
        Assert.Equal("COPA EL NENE", experience.Name);
        Assert.DoesNotContain(experiences!, e => e.Name == "PlayPredict Demo");
        var competition = await api.Create<CompetitionDto>("/api/competitions", CompetitionPayload(experience.Id));
        Assert.Equal(experience.Id, competition.ExperienceId);
        var starts = DateTime.UtcNow.AddDays(30);
        var edition = await api.Create<EditionDto>($"/api/competitions/{competition.Id}/editions",
            new CreateEditionDto("Nueva edición", starts, null, "Active"));
        var round = await api.Create<RoundDto>($"/api/editions/{edition.Id}/rounds", new CreateRoundDto("Fecha 1", 1, null, null));
        var teams = await api.Read(db => db.Teams.OrderBy(t => t.Id).Select(t => t.Id).Take(2).ToArrayAsync());
        var match = await api.Create<MatchDto>($"/api/rounds/{round.Id}/matches", new CreateMatchDto(teams[0], teams[1], starts, "Scheduled"));
        var league = await api.Create<AdminOfficialLeagueDto>("/api/admin/official-leagues",
            new CreateOfficialLeagueDto("Nueva liga jugable", null, competition.Id, edition.Id, "FullCompetition",
                null, null, true, true, 6, 3, 0, true, 2, ["Mediocampista", "Delantero"]));
        Assert.True(league.IsActive);
        Assert.Equal(competition.Id, league.CompetitionId);
        Assert.Equal(edition.Id, league.EditionId);
        Assert.Equal(round.Id, match.RoundId);
        Assert.Equal(1, league.MatchesCount);

        await api.Authenticate(RoleNames.Player);
        using var joined = await api.Client.PostAsJsonAsync($"/api/leagues/{league.Id}/join", new { });
        Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
        var matches = await api.Client.GetFromJsonAsync<MatchWithPredictionDto[]>($"/api/leagues/{league.Id}/matches");
        Assert.True(Assert.Single(matches!).CanPredict);
        var prediction = await api.Create<PredictionDto>("/api/predictions",
            new { leagueId = league.Id, matchId = match.Id, predictedHomeScore = 1, predictedAwayScore = 0 });
        Assert.Equal(match.Id, prediction.MatchId);
        Assert.Equal(2, await api.Read(db => db.Competitions.CountAsync()));
        Assert.Equal(61, await api.Read(db => db.Matches.CountAsync()));
        Assert.Equal(3, await api.Read(db => db.Leagues.CountAsync()));
        Assert.Equal(1, await api.Read(db => db.Predictions.CountAsync()));
        Assert.Equal("Torneo Clausura AFA 2026", await api.Read(db => db.Competitions.Where(c => c.Id != competition.Id).Select(c => c.Name).SingleAsync()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(999999)]
    public async Task Missing_or_unknown_experience_returns_field_error_without_creating_data(int? experienceId)
    {
        await using var api = await Db0Api.Create();
        using var response = await api.Client.PostAsJsonAsync("/api/competitions", CompetitionPayload(experienceId));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty("experienceId", out _));
        Assert.Equal(1, await api.Read(db => db.Competitions.CountAsync()));
        Assert.Equal(1, await api.Read(db => db.Experiences.CountAsync()));
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData(RoleNames.Player, HttpStatusCode.Forbidden)]
    public async Task Creation_still_requires_admin(string? role, HttpStatusCode expected)
    {
        await using var api = await Db0Api.Create();
        await api.Authenticate(role);
        var id = await api.Read(db => db.Experiences.Select(e => e.Id).SingleAsync());
        using var response = await api.Client.PostAsJsonAsync("/api/competitions", CompetitionPayload(id));
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(1, await api.Read(db => db.Competitions.CountAsync()));
    }

    [Fact]
    public async Task Explicit_id_selects_the_right_experience_among_multiple_and_ignores_names()
    {
        await using var api = await Db0Api.Create();
        var id = await api.Read(async db =>
        {
            var existing = await db.Experiences.SingleAsync();
            existing.Name = "Nombre cambiado";
            db.Experiences.Add(new Experience { Name = "Otra experiencia" });
            await db.SaveChangesAsync();
            return existing.Id;
        });
        var competition = await api.Create<CompetitionDto>("/api/competitions", CompetitionPayload(id));
        Assert.Equal(id, competition.ExperienceId);
        Assert.Equal(id, await api.Read(db => db.Competitions.Where(c => c.Id == competition.Id).Select(c => c.ExperienceId).SingleAsync()));
    }

    [Fact]
    public async Task Legacy_edit_without_experience_keeps_existing_association_and_invalid_edit_does_not_write()
    {
        await using var api = await Db0Api.Create();
        var original = await api.Read(db => db.Competitions.AsNoTracking().SingleAsync());
        using var invalid = await api.Client.PutAsJsonAsync($"/api/competitions/{original.Id}",
            new UpdateCompetitionDto("Invalid change", null, original.Sport, true, 999999));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(original.Name, await api.Read(db => db.Competitions.Select(c => c.Name).SingleAsync()));
        using var updated = await api.Client.PutAsJsonAsync($"/api/competitions/{original.Id}",
            new { name = "Updated name", description = original.Description, sport = original.Sport, isActive = original.IsActive });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(original.ExperienceId, await api.Read(db => db.Competitions.Select(c => c.ExperienceId).SingleAsync()));
    }

    private static object CompetitionPayload(int? experienceId) =>
        new { name = "Nueva competencia", description = "Creada por ADMIN", sport = "Fútbol", isActive = true, experienceId };

    private sealed class Db0Api(WebApplication app, SqliteConnection connection) : IAsyncDisposable
    {
        public HttpClient Client { get; } = app.GetTestClient();
        private const string Key = "competition-creation-tests-only-signing-key-0123456789";

        public static async Task<Db0Api> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = Key, ["Jwt:Issuer"] = "competition-tests", ["Jwt:Audience"] = "competition-tests"
            });
            builder.Services.AddSingleton<JwtTokenService>();
            builder.Services.AddScoped<LeagueScoringService>();
            builder.Services.AddScoped<PredictionEvaluationService>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = "competition-tests", ValidateAudience = true, ValidAudience = "competition-tests",
                    ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), ValidateLifetime = true
                });
            builder.Services.AddAuthorization();
            builder.Services.AddDbContext<PlayPredictDbContext>(options => options.UseSqlite(connection));
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapAdminExperienceEndpoints();
            app.MapCompetitionEndpoints();
            app.MapEditionEndpoints();
            app.MapRoundEndpoints();
            app.MapMatchEndpoints();
            app.MapAdminOfficialLeagueEndpoints();
            app.MapLeagueEndpoints();
            app.MapPredictionEndpoints();
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
                await db.Database.EnsureCreatedAsync();
                await LoadDb0BusinessData(connection);
                Assert.Equal(30, await db.Teams.CountAsync());
                Assert.Equal(1045, await db.TeamPlayers.CountAsync());
                Assert.Equal(60, await db.Matches.CountAsync());
                Assert.Equal(2, await db.Users.CountAsync());
                Assert.Equal(2, await db.Leagues.CountAsync());
                Assert.Equal(0, await db.Predictions.CountAsync());
                Assert.Equal(await db.MatchScorers.CountAsync(), await db.MatchScorers.CountAsync(s => s.TeamId != 0));
            }
            await app.StartAsync();
            var api = new Db0Api(app, connection);
            await api.Authenticate(RoleNames.Admin);
            return api;
        }

        private static async Task LoadDb0BusinessData(SqliteConnection connection)
        {
            // Read the immutable canonical COPY data into the EF-created SQLite schema.
            // This is an isolated data-equivalence fixture, not a PostgreSQL restore test.
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            const string relative = "docs/database/backups/DB0_PlayPredict_BaseInicial_v1_2026-09-01.sql";
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relative))) directory = directory.Parent;
            Assert.NotNull(directory);
            var sql = await File.ReadAllTextAsync(Path.Combine(directory.FullName, relative));
            using var transaction = connection.BeginTransaction();
            using var defer = connection.CreateCommand();
            defer.Transaction = transaction;
            defer.CommandText = "PRAGMA defer_foreign_keys=ON";
            await defer.ExecuteNonQueryAsync();
            var blocks = Regex.Matches(sql.Replace("\r\n", "\n"), "COPY public\\.\"([^\"]+)\" \\((.*?)\\) FROM stdin;\\n(.*?)\\\\\\.", RegexOptions.Singleline);
            Assert.Equal(23, blocks.Count);
            foreach (System.Text.RegularExpressions.Match block in blocks)
            {
                if (block.Groups[1].Value == "__EFMigrationsHistory") continue;
                var columns = block.Groups[2].Value;
                foreach (var row in block.Groups[3].Value.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var values = row.Split('\t');
                    using var insert = connection.CreateCommand();
                    insert.Transaction = transaction;
                    insert.CommandText = $"INSERT INTO \"{block.Groups[1].Value}\" ({columns}) VALUES ({string.Join(",", values.Select((_, i) => "$p" + i))})";
                    for (var i = 0; i < values.Length; i++)
                    {
                        object value = values[i] switch
                        {
                            "\\N" => DBNull.Value, "t" => 1, "f" => 0,
                            _ => Regex.Replace(values[i], @"\\([\\tnr])", m => m.Groups[1].Value switch { "t" => "\t", "n" => "\n", "r" => "\r", _ => "\\" })
                        };
                        insert.Parameters.AddWithValue("$p" + i, value);
                    }
                    await insert.ExecuteNonQueryAsync();
                }
            }
            await transaction.CommitAsync();
            // Backfill equivalente a la migración AddMatchScorerOwnGoals: el SQL canónico de
            // DB0 precede a la columna TeamId, y en filas históricas el equipo beneficiado es
            // el equipo del goleador.
            using var backfill = connection.CreateCommand();
            backfill.CommandText = @"UPDATE ""MatchScorers"" AS s SET ""TeamId"" = p.""TeamId"" FROM ""TeamPlayers"" AS p WHERE p.""Id"" = s.""TeamPlayerId""";
            await backfill.ExecuteNonQueryAsync();
        }

        public async Task Authenticate(string? role)
        {
            Client.DefaultRequestHeaders.Authorization = null;
            if (role is null) return;
            var user = await Read(db => db.Users.AsNoTracking().SingleAsync(u => u.Email == (role == RoleNames.Admin ? "admin" : "usuario")));
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", app.Services.GetRequiredService<JwtTokenService>().GenerateToken(user, [role]));
        }

        public async Task<T> Create<T>(string path, object body)
        {
            using var response = await Client.PostAsJsonAsync(path, body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<T>())!;
        }

        public async Task<T> Read<T>(Func<PlayPredictDbContext, Task<T>> action)
        {
            await using var scope = app.Services.CreateAsyncScope();
            return await action(scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>());
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
