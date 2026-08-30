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
using PlayPredict.Api.Endpoints;
using PlayPredict.Api.Imports;
using Xunit;

namespace PlayPredict.Api.Tests;

public class AdminTeamRosterImportEndpointTests
{
    [Fact]
    public async Task Preview_requires_authentication()
    {
        await using var server = await ImportApi.Create();
        var response = await server.Client.PostAsync("/api/admin/team-roster-import/preview", new MultipartFormDataContent());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_requires_authentication()
    {
        await using var server = await ImportApi.Create();
        var response = await server.Client.PostAsync("/api/admin/team-roster-import/confirm", new MultipartFormDataContent());
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
    public async Task Missing_file_returns_clear_error()
    {
        await using var server = await ImportApi.Create();
        var response = await server.Post("preview", includeFile: false);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("FILE_REQUIRED", await IssueCode(response));
    }

    [Fact]
    public async Task Missing_sport_returns_clear_error()
    {
        await using var server = await ImportApi.Create();
        var response = await server.Post("preview", sport: "");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("SPORT_REQUIRED", await IssueCode(response));
    }

    [Fact]
    public async Task Invalid_extension_is_rejected()
    {
        await using var server = await ImportApi.Create();
        var response = await server.Post("preview", fileName: "equipos.csv");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("INVALID_FILE_EXTENSION", await IssueCode(response));
    }

    [Fact]
    public async Task Oversized_file_is_rejected()
    {
        await using var server = await ImportApi.Create(maxBytes: 100);
        var response = await server.Post("preview", bytes: new byte[101]);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("FILE_TOO_LARGE", await IssueCode(response));
    }

    [Fact]
    public async Task Valid_preview_returns_hash_and_can_confirm()
    {
        await using var server = await ImportApi.Create();
        var response = await server.Post("preview");
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(64, json.RootElement.GetProperty("hash").GetString()!.Length);
        Assert.True(json.RootElement.GetProperty("canConfirm").GetBoolean());
        Assert.Equal(1, json.RootElement.GetProperty("teamsSummary").GetProperty("new").GetInt32());
    }

    [Fact]
    public async Task Conflict_preview_cannot_confirm()
    {
        await using var server = await ImportApi.Create();
        await server.SeedTeam("River Plate", "CARP", "B\u00e1squet");
        var response = await server.Post("preview");
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(json.RootElement.GetProperty("canConfirm").GetBoolean());
        Assert.Equal(1, json.RootElement.GetProperty("teamsSummary").GetProperty("conflicts").GetInt32());
    }

    [Fact]
    public async Task Preview_does_not_modify_database()
    {
        await using var server = await ImportApi.Create();
        var response = await server.Post("preview");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((0, 0), await server.Counts());
    }

    [Fact]
    public async Task Valid_confirmation_persists_team_and_roster()
    {
        await using var server = await ImportApi.Create();
        var bytes = ValidWorkbook();
        var preview = await server.Post("preview", bytes: bytes);
        var hash = (await JsonDocument.ParseAsync(await preview.Content.ReadAsStreamAsync())).RootElement.GetProperty("hash").GetString()!;

        var confirmation = await server.Post("confirm", bytes: bytes, expectedHash: hash);

        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        Assert.Equal((1, 1), await server.Counts());
    }

    [Fact]
    public async Task Wrong_hash_is_unprocessable_and_does_not_write()
    {
        await using var server = await ImportApi.Create();
        var response = await server.Post("confirm", expectedHash: new string('0', 64));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("FILE_HASH_MISMATCH", await IssueCode(response));
        Assert.Equal((0, 0), await server.Counts());
    }

    [Fact]
    public async Task Confirmation_conflict_is_unprocessable_and_atomic()
    {
        await using var server = await ImportApi.Create();
        await server.SeedTeam("River Plate", "CARP", "B\u00e1squet");
        var bytes = ValidWorkbook();
        var response = await server.Post("confirm", bytes: bytes, expectedHash: SpreadsheetFileHash.ComputeSha256(bytes));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal((1, 0), await server.Counts());
    }

    [Fact]
    public async Task Double_confirmation_is_idempotent()
    {
        await using var server = await ImportApi.Create();
        var bytes = ValidWorkbook();
        var hash = SpreadsheetFileHash.ComputeSha256(bytes);

        Assert.Equal(HttpStatusCode.OK, (await server.Post("confirm", bytes: bytes, expectedHash: hash)).StatusCode);
        var second = await server.Post("confirm", bytes: bytes, expectedHash: hash);
        var json = await JsonDocument.ParseAsync(await second.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(0, json.RootElement.GetProperty("teams").GetProperty("created").GetInt32());
        Assert.Equal(0, json.RootElement.GetProperty("rosters").GetProperty("created").GetInt32());
        Assert.Equal((1, 1), await server.Counts());
    }

    private static async Task<string> IssueCode(HttpResponseMessage response)
    {
        var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty("issues")[0].GetProperty("code").GetString()!;
    }

    private static byte[] ValidWorkbook()
    {
        using var workbook = SpreadsheetTestWorkbook.CreateXlsx(
            new SheetData(SpreadsheetReader.TeamsSheet,
                ["NOMBRE DEL EQUIPO", "NOMBRE CORTO"],
                ["River Plate", "CARP"]),
            new SheetData(SpreadsheetReader.RostersSheet,
                ["NOMBRE DEL CLUB", "NOMBRE", "APELLIDO", "NOMBRE PARA MOSTRAR", "POSICION"],
                ["River Plate", "Juan", "P\u00e9rez", "", "DEFENSOR"]));
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

        public static async Task<ImportApi> Create(long maxBytes = TeamRosterImportOptions.DefaultMaxFileSizeBytes)
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
            builder.Services.AddScoped<TeamRosterImportPreviewService>();
            builder.Services.AddScoped<TeamRosterImportConfirmationService>();
            builder.Services.Configure<TeamRosterImportOptions>(options => options.MaxFileSizeBytes = maxBytes);

            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapAdminTeamRosterImportEndpoints();
            await app.StartAsync();
            await using (var scope = app.Services.CreateAsyncScope())
                await scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>().Database.EnsureCreatedAsync();
            return new(app, connection);
        }

        public async Task<HttpResponseMessage> Post(string operation, string role = "ADMIN", bool includeFile = true,
            string sport = "F\u00fatbol", string fileName = "equipos.xlsx", byte[]? bytes = null, string? expectedHash = null)
        {
            using var form = new MultipartFormDataContent();
            if (includeFile)
            {
                var file = new ByteArrayContent(bytes ?? ValidWorkbook());
                file.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                form.Add(file, "file", fileName);
            }
            form.Add(new StringContent(sport), "sport");
            if (expectedHash is not null) form.Add(new StringContent(expectedHash), "expectedHash");
            using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/team-roster-import/{operation}") { Content = form };
            request.Headers.Add("X-Test-Role", role);
            return await Client.SendAsync(request);
        }

        public async Task SeedTeam(string name, string shortName, string sport)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
            db.Teams.Add(new Team { Name = name, ShortName = shortName, Sport = sport, Active = true });
            await db.SaveChangesAsync();
        }

        public async Task<(int Teams, int Players)> Counts()
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
            return (await db.Teams.CountAsync(), await db.TeamPlayers.CountAsync());
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
