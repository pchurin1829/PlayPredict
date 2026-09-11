using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
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
using PlayPredict.Api.Security;
using PlayPredict.Api.Services;
using Xunit;

namespace PlayPredict.Api.Tests;

/// <summary>
/// Cobertura P2.1: policy, login genérico, rate limit, lockout, TokenVersion,
/// MustChangePassword, change password y reset administrativo. Pipeline completo
/// (ForwardedHeaders + autenticación + seguridad central + rate limiter), igual que Program.
/// </summary>
public sealed class AccountSecurityTests
{
    private const string PlayerPassword = "usuario";
    private const string AdminPassword = "admin123";
    private const string GenericMessage = "Email o contraseña incorrectos.";

    [Theory]
    [InlineData("corta123", "password")]
    [InlineData("", "password")]
    public async Task Registration_rejects_short_password(string password, string field)
    {
        await using var api = await TestApi.Create();
        using var response = await api.Client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("Test", "Player", "nuevo@example.com", password, "nuevo@example.com"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact]
    public async Task Registration_accepts_ten_char_password_and_rejects_overlong()
    {
        await using var api = await TestApi.Create();
        using var ok = await api.Client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("Test", "Player", "ok@example.com", "exacta10!!", "ok@example.com"));
        Assert.Equal(HttpStatusCode.Created, ok.StatusCode);
        using var tooLong = await api.Client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto("Test", "Player", "largo@example.com", new string('x', 201), "largo@example.com"));
        Assert.Equal(HttpStatusCode.BadRequest, tooLong.StatusCode);
    }

    [Fact]
    public async Task Login_returns_same_message_for_unknown_email_wrong_password_and_inactive()
    {
        await using var api = await TestApi.Create();
        using var unknown = await api.Login("nadie@example.com", "cualquier1234", "10.9.0.1");
        using var wrong = await api.Login(InitialDatasetV1Seeder.PlayerEmail, "incorrecta12", "10.9.0.2");
        await api.Read(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Email == InitialDatasetV1Seeder.PlayerEmail);
            user.IsActive = false;
            return await db.SaveChangesAsync();
        });
        using var inactive = await api.Login(InitialDatasetV1Seeder.PlayerEmail, PlayerPassword, "10.9.0.3");
        foreach (var response in new[] { unknown, wrong, inactive })
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(GenericMessage, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
        }
    }

    [Fact]
    public async Task Login_rate_limit_returns_429_with_retry_after()
    {
        await using var api = await TestApi.Create();
        HttpStatusCode? sixth = null;
        HttpResponseMessage? last = null;
        for (var i = 0; i < 6; i++)
        {
            last?.Dispose();
            last = await api.Login(InitialDatasetV1Seeder.PlayerEmail, "incorrecta12", "10.9.1.7");
            if (i == 5) sixth = last.StatusCode;
        }
        Assert.Equal(HttpStatusCode.TooManyRequests, sixth);
        Assert.NotNull(last!.Headers.RetryAfter);
        last.Dispose();
    }

    [Fact]
    public async Task Failed_logins_accumulate_and_tenth_locks_account()
    {
        await using var api = await TestApi.Create();
        for (var i = 0; i < 10; i++)
            using (var response = await api.Login(InitialDatasetV1Seeder.PlayerEmail, "incorrecta12", $"10.9.2.{i}"))
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var snapshot = await api.Read(async db =>
            await db.Users.Where(u => u.Email == InitialDatasetV1Seeder.PlayerEmail)
                .Select(u => new { u.FailedLoginAttempts, u.LockoutUntilUtc }).SingleAsync());
        Assert.Equal(10, snapshot.FailedLoginAttempts);
        Assert.NotNull(snapshot.LockoutUntilUtc);
        Assert.True(snapshot.LockoutUntilUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Locked_account_rejects_correct_password_with_generic_message()
    {
        await using var api = await TestApi.Create();
        for (var i = 0; i < 10; i++)
            using (await api.Login(InitialDatasetV1Seeder.PlayerEmail, "incorrecta12", $"10.9.3.{i}")) { }
        using var correct = await api.Login(InitialDatasetV1Seeder.PlayerEmail, PlayerPassword, "10.9.3.99");
        Assert.Equal(HttpStatusCode.Unauthorized, correct.StatusCode);
        Assert.Equal(GenericMessage, (await correct.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("message").GetString());
    }

    [Fact]
    public async Task Expired_lockout_allows_login_and_success_clears_counter()
    {
        await using var api = await TestApi.Create();
        for (var i = 0; i < 3; i++)
            using (await api.Login(InitialDatasetV1Seeder.PlayerEmail, "incorrecta12", $"10.9.4.{i}")) { }
        Assert.Equal(3, await api.Read(async db =>
            await db.Users.Where(u => u.Email == InitialDatasetV1Seeder.PlayerEmail).Select(u => u.FailedLoginAttempts).SingleAsync()));
        using var ok = await api.Login(InitialDatasetV1Seeder.PlayerEmail, PlayerPassword, "10.9.4.50");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var cleared = await api.Read(async db =>
            await db.Users.Where(u => u.Email == InitialDatasetV1Seeder.PlayerEmail)
                .Select(u => new { u.FailedLoginAttempts, u.LockoutUntilUtc }).SingleAsync());
        Assert.Equal(0, cleared.FailedLoginAttempts);
        Assert.Null(cleared.LockoutUntilUtc);

        for (var i = 0; i < 10; i++)
            using (await api.Login(InitialDatasetV1Seeder.PlayerEmail, "incorrecta12", $"10.9.5.{i}")) { }
        await api.Read(async db =>
        {
            var user = await db.Users.SingleAsync(u => u.Email == InitialDatasetV1Seeder.PlayerEmail);
            user.LockoutUntilUtc = DateTime.UtcNow.AddMinutes(-1);
            return await db.SaveChangesAsync();
        });
        using var afterExpiry = await api.Login(InitialDatasetV1Seeder.PlayerEmail, PlayerPassword, "10.9.5.99");
        Assert.Equal(HttpStatusCode.OK, afterExpiry.StatusCode);
    }

    [Fact]
    public async Task Password_change_validates_and_invalidates_previous_token()
    {
        await using var api = await TestApi.Create();
        var auth = await api.Authenticate();
        api.SetBearer(auth.Token);
        using var wrongCurrent = await api.Client.PutAsJsonAsync("/api/users/me/password",
            new ChangePasswordDto("noesesta123", "nueva1234567", "nueva1234567"));
        Assert.Equal(HttpStatusCode.BadRequest, wrongCurrent.StatusCode);
        using var mismatch = await api.Client.PutAsJsonAsync("/api/users/me/password",
            new ChangePasswordDto(PlayerPassword, "nueva1234567", "otra12345678"));
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
        using var shortNew = await api.Client.PutAsJsonAsync("/api/users/me/password",
            new ChangePasswordDto(PlayerPassword, "corta", "corta"));
        Assert.Equal(HttpStatusCode.BadRequest, shortNew.StatusCode);
        using var same = await api.Client.PutAsJsonAsync("/api/users/me/password",
            new ChangePasswordDto(PlayerPassword, PlayerPassword, PlayerPassword));
        Assert.Equal(HttpStatusCode.BadRequest, same.StatusCode);

        using var changed = await api.Client.PutAsJsonAsync("/api/users/me/password",
            new ChangePasswordDto(PlayerPassword, "nueva1234567", "nueva1234567"));
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var fresh = (await changed.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        api.SetBearer(fresh.Token);
        Assert.Equal(HttpStatusCode.OK, (await api.Client.GetAsync("/api/users/me")).StatusCode);
        api.SetBearer(auth.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync("/api/users/me")).StatusCode);
        api.ClearBearer();
        using var loginNew = await api.Login(InitialDatasetV1Seeder.PlayerEmail, "nueva1234567", "10.9.6.1");
        Assert.Equal(HttpStatusCode.OK, loginNew.StatusCode);
    }

    [Fact]
    public async Task Admin_reset_requires_admin_returns_single_use_temporary_and_forces_change()
    {
        await using var api = await TestApi.Create();
        var player = await api.Authenticate();
        api.SetBearer(player.Token);
        using var forbidden = await api.Client.PostAsync("/api/admin/users/1/reset-password", null);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var admin = await api.AuthenticateAsAdmin();
        api.SetBearer(admin.Token);
        using var reset = await api.Client.PostAsync($"/api/admin/users/{player.User.Id}/reset-password", null);
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        var payload = await reset.Content.ReadFromJsonAsync<JsonElement>();
        var temporary = payload.GetProperty("temporaryPassword").GetString()!;
        Assert.Equal(16, temporary.Length);

        var stored = await api.Read(async db =>
            await db.Users.Where(u => u.Id == player.User.Id)
                .Select(u => new { u.PasswordHash, u.MustChangePassword }).SingleAsync());
        Assert.NotEqual(temporary, stored.PasswordHash);
        Assert.True(stored.MustChangePassword);

        api.ClearBearer();
        using var tempLogin = await api.Login(InitialDatasetV1Seeder.PlayerEmail, temporary, "10.9.7.1");
        Assert.Equal(HttpStatusCode.OK, tempLogin.StatusCode);
        var tempAuth = (await tempLogin.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        Assert.True(tempAuth.MustChangePassword);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(tempAuth.Token);
        Assert.Equal("true", jwt.Claims.Single(c => c.Type == AccountSecurityMiddleware.MustChangePasswordClaim).Value);

        api.SetBearer(tempAuth.Token);
        Assert.Equal(HttpStatusCode.OK, (await api.Client.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Client.PutAsJsonAsync("/api/users/me", new { firstName = "X", lastName = "Y" })).StatusCode);

        using var forced = await api.Client.PutAsJsonAsync("/api/users/me/password",
            new ChangePasswordDto(temporary, "definitiva12", "definitiva12"));
        Assert.Equal(HttpStatusCode.OK, forced.StatusCode);
        var normal = (await forced.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        Assert.False(normal.MustChangePassword);
        api.SetBearer(normal.Token);
        Assert.Equal(HttpStatusCode.OK, (await api.Client.PutAsJsonAsync("/api/users/me", new { firstName = "X", lastName = "Y" })).StatusCode);
        api.SetBearer(tempAuth.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task Admin_reset_unlocks_locked_account()
    {
        await using var api = await TestApi.Create();
        for (var i = 0; i < 10; i++)
            using (await api.Login(InitialDatasetV1Seeder.PlayerEmail, "incorrecta12", $"10.9.8.{i}")) { }
        var admin = await api.AuthenticateAsAdmin();
        api.SetBearer(admin.Token);
        var playerId = await api.Read(async db =>
            await db.Users.Where(u => u.Email == InitialDatasetV1Seeder.PlayerEmail).Select(u => u.Id).SingleAsync());
        using var reset = await api.Client.PostAsync($"/api/admin/users/{playerId}/reset-password", null);
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        var temporary = (await reset.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("temporaryPassword").GetString()!;
        api.ClearBearer();
        using var login = await api.Login(InitialDatasetV1Seeder.PlayerEmail, temporary, "10.9.8.99");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Email_change_invalidates_previous_token()
    {
        await using var api = await TestApi.Create();
        var auth = await api.Authenticate();
        api.SetBearer(auth.Token);
        using var response = await api.Client.PutAsJsonAsync("/api/users/me/email",
            new ChangeEmailDto("otro@example.com", "otro@example.com", PlayerPassword));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fresh = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
        api.SetBearer(fresh.Token);
        Assert.Equal(HttpStatusCode.OK, (await api.Client.GetAsync("/api/users/me")).StatusCode);
        api.SetBearer(auth.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync("/api/users/me")).StatusCode);
    }

    [Fact]
    public async Task Deactivation_rejects_token_immediately_and_reactivation_does_not_revive_it()
    {
        await using var api = await TestApi.Create();
        var player = await api.Authenticate();
        var admin = await api.AuthenticateAsAdmin();
        api.SetBearer(admin.Token);
        using var deactivate = await api.Client.PutAsJsonAsync($"/api/admin/users/{player.User.Id}", new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, deactivate.StatusCode);
        api.SetBearer(player.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync("/api/users/me")).StatusCode);
        api.SetBearer(admin.Token);
        using var reactivate = await api.Client.PutAsJsonAsync($"/api/admin/users/{player.User.Id}", new { isActive = true });
        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);
        api.SetBearer(player.Token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync("/api/users/me")).StatusCode);
        api.ClearBearer();
        using var relogin = await api.Login(InitialDatasetV1Seeder.PlayerEmail, PlayerPassword, "10.9.9.1");
        Assert.Equal(HttpStatusCode.OK, relogin.StatusCode);
    }

    private sealed class TestApi(WebApplication app, SqliteConnection connection) : IAsyncDisposable
    {
        public HttpClient Client { get; } = app.GetTestClient();

        public static async Task<TestApi> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseTestServer();
            const string key = "p21-tests-only-signing-key-01234567890123456789";
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Key"] = key, ["Jwt:Issuer"] = "p21-tests", ["Jwt:Audience"] = "p21-tests" });
            builder.Services.AddSingleton<JwtTokenService>();
            builder.Services.AddAuthRateLimiting();
            builder.Services.Configure<ForwardedHeadersOptions>(o =>
                o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = "p21-tests", ValidateAudience = true, ValidAudience = "p21-tests",
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(key)), ValidateLifetime = true,
            });
            builder.Services.AddAuthorization();
            builder.Services.AddDbContext<PlayPredictDbContext>(o => o.UseSqlite(connection));
            var app = builder.Build();
            app.UseForwardedHeaders();
            app.UseAuthentication();
            app.UseAccountSecurity();
            app.UseAuthorization();
            app.UseRateLimiter();
            app.MapAuthEndpoints();
            app.MapUserEndpoints();
            app.MapAdminUserEndpoints();
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
                await db.Database.EnsureCreatedAsync();
                await DataSeeder.SeedCoreDataAsync(db);
                var company = await db.Companies.FirstAsync();
                foreach (var (email, password, role) in new[] { (InitialDatasetV1Seeder.AdminEmail, AdminPassword, RoleNames.Admin), (InitialDatasetV1Seeder.PlayerEmail, PlayerPassword, RoleNames.Player) })
                {
                    var user = new User { CompanyId = company.Id, FirstName = "Test", LastName = "User", Email = email, CreatedAtUtc = DateTime.UtcNow };
                    user.PasswordHash = new PasswordHasher<User>().HashPassword(user, password);
                    user.UserRoles.Add(new UserRole { Role = await db.Roles.SingleAsync(r => r.Name == role) });
                    db.Users.Add(user);
                }
                await db.SaveChangesAsync();
            }
            await app.StartAsync();
            return new TestApi(app, connection);
        }

        public Task<HttpResponseMessage> Login(string email, string password, string? forwardedFor = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login");
            request.Content = JsonContent.Create(new LoginDto(email, password));
            if (forwardedFor is not null) request.Headers.Add("X-Forwarded-For", forwardedFor);
            return Client.SendAsync(request);
        }

        public async Task<AuthResponseDto> Authenticate()
        {
            using var response = await Login(InitialDatasetV1Seeder.PlayerEmail, PlayerPassword, "10.8.0.1");
            response.EnsureSuccessStatusCode();
            var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
            return auth;
        }

        public async Task<AuthResponseDto> AuthenticateAsAdmin()
        {
            using var response = await Login(InitialDatasetV1Seeder.AdminEmail, AdminPassword, "10.8.0.2");
            response.EnsureSuccessStatusCode();
            var auth = (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
            return auth;
        }

        public void SetBearer(string token) =>
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        public void ClearBearer() => Client.DefaultRequestHeaders.Authorization = null;

        public async Task<T> Read<T>(Func<PlayPredictDbContext, Task<T>> action)
        {
            await using var scope = app.Services.CreateAsyncScope();
            return await action(scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>());
        }

        public async ValueTask DisposeAsync() { Client.Dispose(); await app.DisposeAsync(); await connection.DisposeAsync(); }
    }
}
