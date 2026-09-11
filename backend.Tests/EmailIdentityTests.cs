using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NPOI.XSSF.UserModel;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Endpoints;
using PlayPredict.Api.Services;
using Xunit;

namespace PlayPredict.Api.Tests;

public sealed class EmailIdentityTests
{
    [Fact]
    public async Task Registration_normalizes_email_returns_player_and_can_login()
    {
        await using var api = await TestApi.Create();
        using var response = await api.Client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto(" Test ", " Player ", " New@Example.com ", "test123", "new@example.COM"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        Assert.Equal("new@example.com", auth.User.Email);
        Assert.Equal([RoleNames.Player], auth.User.Roles);
        using var logged = await api.Login(" NEW@EXAMPLE.COM ", "test123");
        Assert.Equal(HttpStatusCode.OK, logged.StatusCode);
    }

    [Theory]
    [InlineData("admin", "admin", "email")]
    [InlineData("a@", "a@", "email")]
    [InlineData("a@@example.com", "a@@example.com", "email")]
    [InlineData("a b@example.com", "a b@example.com", "email")]
    [InlineData("Name <a@example.com>", "Name <a@example.com>", "email")]
    [InlineData("a@example.com", "b@example.com", "confirmEmail")]
    [InlineData("a@example.com", null, "confirmEmail")]
    [InlineData(" ADMIN@PLAYPREDICT.LOCAL ", "admin@playpredict.local", "email")]
    public async Task Registration_rejects_invalid_mismatched_missing_or_duplicate_email(string email, string? confirm, string field)
    {
        await using var api = await TestApi.Create();
        using var response = await api.Client.PostAsJsonAsync("/api/auth/register", new RegisterDto("Test", "Player", email, "test123", confirm));
        await AssertFieldError(response, field);
        Assert.Equal(2, await api.Read(db => db.Users.CountAsync()));
    }

    [Fact]
    public async Task Registration_detects_legacy_mixed_case_duplicate()
    {
        await using var api = await TestApi.Create();
        await api.Read(async db => { var u = await db.Users.FirstAsync(); u.Email = "Mixed@Example.com"; return await db.SaveChangesAsync(); });
        using var response = await api.Client.PostAsJsonAsync("/api/auth/register", new RegisterDto("Test", "Player", "mixed@example.com", "test123", "mixed@example.com"));
        await AssertFieldError(response, "email");
    }

    [Theory]
    [InlineData("admin@playpredict.local", "admin123", HttpStatusCode.OK)]
    [InlineData("usuario@playpredict.local", "usuario", HttpStatusCode.OK)]
    [InlineData("admin", "admin123", HttpStatusCode.Unauthorized)]
    [InlineData("usuario", "usuario", HttpStatusCode.Unauthorized)]
    public async Task Login_requires_canonical_email(string email, string password, HttpStatusCode expected)
    {
        await using var api = await TestApi.Create();
        using var response = await api.Login(email, password);
        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Email_change_updates_user_token_and_login_without_invalidating_id_session()
    {
        await using var api = await TestApi.Create();
        var oldAuth = await api.Authenticate();
        using var response = await api.Client.PutAsJsonAsync("/api/users/me/email", new ChangeEmailDto(" NEW@Example.COM ", "new@example.com", "usuario"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        Assert.Equal(oldAuth.User.Id, auth.User.Id);
        Assert.Equal("new@example.com", auth.User.Email);
        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(auth.Token);
        Assert.Equal("new@example.com", jwt.Claims.Single(c => c.Type == "email").Value);
        // The old token still identifies the same user by ID.
        var me = await api.Client.GetFromJsonAsync<UserDto>("/api/users/me");
        Assert.Equal("new@example.com", me!.Email);
        using var oldLogin = await api.Login(InitialDatasetV1Seeder.PlayerEmail, "usuario");
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);
        using var newLogin = await api.Login(" NEW@EXAMPLE.COM ", "usuario");
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
        api.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        Assert.Equal("new@example.com", (await api.Client.GetFromJsonAsync<UserDto>("/api/users/me"))!.Email);
    }

    [Theory]
    [InlineData("new@example.com", "new@example.com", "incorrecta", "currentPassword")]
    [InlineData("admin@playpredict.local", "ADMIN@PLAYPREDICT.LOCAL", "usuario", "newEmail")]
    [InlineData("new@example.com", "other@example.com", "usuario", "confirmEmail")]
    [InlineData("invalid@", "invalid@", "usuario", "newEmail")]
    [InlineData("new@example.com", "new@example.com", "", "currentPassword")]
    public async Task Email_change_rejects_invalid_request_without_writes(string email, string confirm, string password, string field)
    {
        await using var api = await TestApi.Create();
        await api.Authenticate();
        using var response = await api.Client.PutAsJsonAsync("/api/users/me/email", new ChangeEmailDto(email, confirm, password));
        await AssertFieldError(response, field);
        Assert.Equal(InitialDatasetV1Seeder.PlayerEmail, (await api.Client.GetFromJsonAsync<UserDto>("/api/users/me"))!.Email);
    }

    [Fact]
    public async Task Email_change_requires_authentication_and_profile_update_cannot_change_email()
    {
        await using var api = await TestApi.Create();
        using var anonymous = await api.Client.PutAsJsonAsync("/api/users/me/email", new ChangeEmailDto("new@example.com", "new@example.com", "usuario"));
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        await api.Authenticate();
        using var response = await api.Client.PutAsJsonAsync("/api/users/me", new { firstName = "Updated", lastName = "Name", email = "silent@example.com" });
        Assert.Equal(InitialDatasetV1Seeder.PlayerEmail, (await response.Content.ReadFromJsonAsync<UserDto>())!.Email);
    }

    [Fact]
    public async Task Email_unique_index_rejects_duplicate_persistence()
    {
        await using var api = await TestApi.Create();
        await Assert.ThrowsAsync<DbUpdateException>(() => api.Read(async db =>
        {
            var users = await db.Users.OrderBy(u => u.Id).ToListAsync();
            users[1].Email = users[0].Email;
            return await db.SaveChangesAsync();
        }));
    }

    [Fact]
    public async Task Initial_seeder_and_workbook_generate_canonical_users_and_counts()
    {
        await using var api = await TestApi.Create(seedInitial: true);
        using var stream = File.OpenRead(RepoFile("docs/datos-iniciales/PlayPredict_Base_Inicial_v1.0_2026-09-01.xlsx"));
        using var workbook = new XSSFWorkbook(stream);
        var usersSheet = workbook.GetSheet("USUARIOS");
        Assert.Equal("EMAIL", usersSheet.GetRow(3).GetCell(0).StringCellValue);
        Assert.Equal(InitialDatasetV1Seeder.AdminEmail, usersSheet.GetRow(4).GetCell(0).StringCellValue);
        Assert.Equal(InitialDatasetV1Seeder.PlayerEmail, usersSheet.GetRow(5).GetCell(0).StringCellValue);
        Assert.Equal(InitialDatasetV1Seeder.PlayerEmail, workbook.GetSheet("PARTICIPACIONES").GetRow(4).GetCell(0).StringCellValue);
        Assert.Equal(new[] { InitialDatasetV1Seeder.AdminEmail, InitialDatasetV1Seeder.PlayerEmail }, await api.Read(db => db.Users.OrderBy(u => u.Email).Select(u => u.Email).ToArrayAsync()));
        Assert.Equal(30, await api.Read(db => db.Teams.CountAsync()));
        Assert.Equal(1045, await api.Read(db => db.TeamPlayers.CountAsync()));
        Assert.Equal(60, await api.Read(db => db.Matches.CountAsync()));
        Assert.Equal(0, await api.Read(db => db.Predictions.CountAsync()));
        Assert.Equal(2, await api.Read(db => db.Leagues.CountAsync()));
        Assert.Equal(1, await api.Read(db => db.LeagueParticipants.CountAsync()));
        foreach (var (email, password) in new[] { (InitialDatasetV1Seeder.AdminEmail, "admin123"), (InitialDatasetV1Seeder.PlayerEmail, "usuario") })
        {
            using var response = await api.Login(email, password);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        foreach (var email in new[] { "admin", "usuario" })
        {
            using var response = await api.Login(email, email == "admin" ? "admin123" : "usuario");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    private static async Task AssertFieldError(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _));
    }

    private static string RepoFile(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, relative))) directory = directory.Parent;
        return Path.Combine(directory?.FullName ?? throw new FileNotFoundException(relative), relative);
    }

    private sealed class TestApi(WebApplication app, SqliteConnection connection) : IAsyncDisposable
    {
        public HttpClient Client { get; } = app.GetTestClient();
        public static async Task<TestApi> Create(bool seedInitial = false)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseTestServer();
            const string key = "email-tests-only-signing-key-012345678901234567890";
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key, ["Jwt:Issuer"] = "email-tests", ["Jwt:Audience"] = "email-tests" });
            builder.Services.AddSingleton<JwtTokenService>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = "email-tests", ValidateAudience = true, ValidAudience = "email-tests",
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), ValidateLifetime = true,
            });
            builder.Services.AddAuthorization();
            builder.Services.AddDbContext<PlayPredictDbContext>(o => o.UseSqlite(connection));
            var app = builder.Build();
            app.UseAuthentication(); app.UseAuthorization(); app.MapAuthEndpoints(); app.MapUserEndpoints();
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
                await db.Database.EnsureCreatedAsync();
                if (seedInitial)
                    await InitialDatasetV1Seeder.SeedAsync(db, RepoFile("docs/datos-iniciales/PlayPredict_Base_Inicial_v1.0_2026-09-01.xlsx"), RepoFile("docs/datos-iniciales/PlayPredict_Planteles_Clausura_AFA_2026_v2.xlsx"));
                else
                {
                    await DataSeeder.SeedCoreDataAsync(db);
                    var company = await db.Companies.FirstAsync();
                    foreach (var (email, password, role) in new[] { (InitialDatasetV1Seeder.AdminEmail, "admin123", RoleNames.Admin), (InitialDatasetV1Seeder.PlayerEmail, "usuario", RoleNames.Player) })
                    {
                        var user = new User { CompanyId = company.Id, FirstName = "Test", LastName = "User", Email = email, CreatedAtUtc = DateTime.UtcNow };
                        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
                        user.UserRoles.Add(new UserRole { Role = await db.Roles.SingleAsync(r => r.Name == role) });
                        db.Users.Add(user);
                    }
                    await db.SaveChangesAsync();
                }
            }
            await app.StartAsync();
            return new TestApi(app, connection);
        }
        public Task<HttpResponseMessage> Login(string email, string password) => Client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, password));
        public async Task<AuthResponseDto> Authenticate()
        {
            using var response = await Login(InitialDatasetV1Seeder.PlayerEmail, "usuario");
            response.EnsureSuccessStatusCode();
            var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
            return auth;
        }
        public async Task<T> Read<T>(Func<PlayPredictDbContext, Task<T>> action)
        {
            await using var scope = app.Services.CreateAsyncScope();
            return await action(scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>());
        }
        public async ValueTask DisposeAsync() { Client.Dispose(); await app.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
