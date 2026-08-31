using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Endpoints;
using PlayPredict.Api.Imports;
using Xunit;

namespace PlayPredict.Api.Tests;

public class AdminMatchImportEndpointTests
{
    private static readonly DateOnly Day = new(2026, 8, 28);
    private static readonly TimeOnly Time = new(21, 30);

    [Fact]
    public async Task Preview_requires_authentication()
    {
        await using var server = await ImportApi.Create();
        var response = await server.Client.PostAsync("/api/admin/match-import/preview", new MultipartFormDataContent());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_requires_authentication()
    {
        await using var server = await ImportApi.Create();
        var response = await server.Client.PostAsync("/api/admin/match-import/confirm", new MultipartFormDataContent());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("preview")]
    [InlineData("confirm")]
    public async Task Player_is_forbidden(string operation)
    {
        await using var server = await ImportApi.Create();
        var response = await server.Post(operation, role: "PLAYER");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Missing_edition_id_returns_clear_error()
    {
        await using var server = await ImportApi.Create();
        var response = await server.Post("preview", editionId: null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("EDITION_ID_REQUIRED", await IssueCode(response));
    }

    [Fact]
    public async Task Valid_preview_returns_hash_and_can_confirm()
    {
        await using var server = await ImportApi.Create();
        var editionId = await server.SeedEditionWithTeams();

        var response = await server.Post("preview", editionId: editionId);
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(64, json.RootElement.GetProperty("hash").GetString()!.Length);
        Assert.True(json.RootElement.GetProperty("canConfirm").GetBoolean());
        Assert.Equal(1, json.RootElement.GetProperty("summary").GetProperty("create").GetInt32());
    }

    [Fact]
    public async Task Preview_does_not_modify_database()
    {
        await using var server = await ImportApi.Create();
        var editionId = await server.SeedEditionWithTeams();

        await server.Post("preview", editionId: editionId);

        Assert.Equal(0, await server.MatchCount());
    }

    [Fact]
    public async Task Valid_confirmation_persists_the_match()
    {
        await using var server = await ImportApi.Create();
        var editionId = await server.SeedEditionWithTeams();
        var bytes = ValidWorkbook();
        var preview = await server.Post("preview", editionId: editionId, bytes: bytes);
        var hash = (await JsonDocument.ParseAsync(await preview.Content.ReadAsStreamAsync())).RootElement.GetProperty("hash").GetString()!;

        var confirmation = await server.Post("confirm", editionId: editionId, bytes: bytes, expectedHash: hash);

        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        Assert.Equal(1, await server.MatchCount());
    }

    [Fact]
    public async Task Wrong_hash_is_unprocessable_and_does_not_write()
    {
        await using var server = await ImportApi.Create();
        var editionId = await server.SeedEditionWithTeams();

        var response = await server.Post("confirm", editionId: editionId, expectedHash: new string('0', 64));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("FILE_HASH_MISMATCH", await IssueCode(response));
        Assert.Equal(0, await server.MatchCount());
    }

    [Fact]
    public async Task Double_confirmation_is_idempotent()
    {
        await using var server = await ImportApi.Create();
        var editionId = await server.SeedEditionWithTeams();
        var bytes = ValidWorkbook();
        var hash = SpreadsheetFileHash.ComputeSha256(bytes);

        Assert.Equal(HttpStatusCode.OK, (await server.Post("confirm", editionId: editionId, bytes: bytes, expectedHash: hash)).StatusCode);
        var second = await server.Post("confirm", editionId: editionId, bytes: bytes, expectedHash: hash);
        var json = await JsonDocument.ParseAsync(await second.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(0, json.RootElement.GetProperty("matches").GetProperty("created").GetInt32());
        Assert.Equal(1, await server.MatchCount());
    }

    private static async Task<string> IssueCode(HttpResponseMessage response)
    {
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("issues")[0].GetProperty("code").GetString()!;
    }

    private static byte[] ValidWorkbook()
    {
        using var workbook = SpreadsheetTestWorkbook.CreateXlsx(
            new SheetData(SpreadsheetReader.MatchesSheet,
                ["FECHA_NRO", "FECHA", "HORA", "LOCAL", "VISITANTE", "ESTADO"],
                [7, Day.ToString("yyyy-MM-dd"), Time.ToString("HH:mm"), "Boca Juniors", "River Plate", "SCHEDULED"]));
        return workbook.ToArray();
    }

    private sealed class ImportApi : IAsyncDisposable
    {
        private readonly WebApplication app;
        private readonly SqliteConnection connection;
        public HttpClient Client { get; }

        private ImportApi(WebApplication app, SqliteConnection connection)
        {
            this.app = app;
            this.connection = connection;
            Client = app.GetTestClient();
        }

        public static async Task<ImportApi> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            builder.Services.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, _ => { });
            builder.Services.AddAuthorization();
            builder.Services.AddDbContext<PlayPredictDbContext>(options => options.UseSqlite(connection));
            builder.Services.AddSingleton<SpreadsheetReader>();
            builder.Services.AddScoped<MatchImportPreviewService>();
            builder.Services.AddScoped<MatchImportConfirmationService>();
            builder.Services.Configure<TeamRosterImportOptions>(_ => { });

            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapAdminMatchImportEndpoints();
            await app.StartAsync();
            await using (var scope = app.Services.CreateAsyncScope())
                await scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>().Database.EnsureCreatedAsync();
            return new(app, connection);
        }

        public async Task<HttpResponseMessage> Post(string operation, string role = "ADMIN", int? editionId = 1,
            byte[]? bytes = null, string? expectedHash = null)
        {
            using var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(bytes ?? ValidWorkbook());
            file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            form.Add(file, "file", "partidos.xlsx");
            if (editionId is not null) form.Add(new StringContent(editionId.Value.ToString()), "editionId");
            if (expectedHash is not null) form.Add(new StringContent(expectedHash), "expectedHash");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/match-import/{operation}") { Content = form };
            request.Headers.Add("X-Test-Role", role);
            return await Client.SendAsync(request);
        }

        public async Task<int> SeedEditionWithTeams()
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
            var experience = new Experience { Name = "El Nene", Status = ExperienceStatus.Published, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow };
            db.Experiences.Add(experience);
            await db.SaveChangesAsync();
            var competition = new Competition { ExperienceId = experience.Id, Name = "Liga Profesional", Sport = "Fútbol", IsActive = true, CreatedAtUtc = DateTime.UtcNow };
            db.Competitions.Add(competition);
            await db.SaveChangesAsync();
            var edition = new Edition { CompetitionId = competition.Id, Name = "Clausura 2026", StartDateUtc = DateTime.UtcNow, Status = EditionStatus.Active, CreatedAtUtc = DateTime.UtcNow };
            db.Editions.Add(edition);
            db.Teams.Add(new Team { Name = "Boca Juniors", ShortName = "Boca", Sport = "Fútbol", Active = true });
            db.Teams.Add(new Team { Name = "River Plate", ShortName = "River", Sport = "Fútbol", Active = true });
            await db.SaveChangesAsync();
            return edition.Id;
        }

        public async Task<int> MatchCount()
        {
            await using var scope = app.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>().Matches.CountAsync();
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string AuthenticationScheme = "Test";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
                return Task.FromResult(AuthenticateResult.NoResult());
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Role, role.ToString())], AuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), AuthenticationScheme)));
        }
    }
}
