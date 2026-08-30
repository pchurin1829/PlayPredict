using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
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
using PlayPredict.Api.LoginAppearance;
using SkiaSharp;
using Xunit;

namespace PlayPredict.Api.Tests;

public class LoginAppearanceTests
{
    [Fact]
    public async Task Defaults_are_returned_without_database_rows()
    {
        await using var fixture = await Fixture.Create();
        var result = await fixture.Service.GetPublicAsync(1);
        Assert.Equal("default-v1", result.Version);
        Assert.Equal("/assets/el-nene-login/copa-el-nene-panel-principal.png", result.Main.ImageUrl);
        Assert.Equal("Contain", result.Main.FitMode);
        Assert.Equal("Cover", result.AdTop.FitMode);
    }

    [Fact]
    public async Task Custom_configuration_and_companies_are_isolated()
    {
        await using var fixture = await Fixture.Create();
        var image = await Validated(SKEncodedImageFormat.Png, LoginImageSlot.Main, 1200, 900);
        await fixture.Service.ReplaceImageAsync(1, 1, LoginImageSlot.Main, image);
        Assert.StartsWith("/api/uploads/login-appearance/1/", (await fixture.Service.GetPublicAsync(1)).Main.ImageUrl);
        Assert.Equal("/assets/el-nene-login/copa-el-nene-panel-principal.png", (await fixture.Service.GetPublicAsync(2)).Main.ImageUrl);
    }

    [Theory]
    [InlineData(SKEncodedImageFormat.Png, ".png")]
    [InlineData(SKEncodedImageFormat.Jpeg, ".jpg")]
    [InlineData(SKEncodedImageFormat.Webp, ".webp")]
    public async Task Png_jpeg_and_webp_are_detected_from_content(SKEncodedImageFormat format, string extension)
    {
        var result = await Validated(format, LoginImageSlot.AdTop, 960, 720);
        Assert.True(result.IsValid);
        Assert.Equal(extension, result.Extension);
    }

    [Fact]
    public async Task Invalid_and_oversized_files_are_rejected()
    {
        var validator = new LoginImageValidator();
        var invalid = await validator.ValidateAsync(new MemoryStream(Encoding.UTF8.GetBytes("not-image")), 9, LoginImageSlot.Main);
        var oversized = await validator.ValidateAsync(Stream.Null, LoginImageValidator.MaximumBytes + 1, LoginImageSlot.Main);
        Assert.Equal("INVALID_IMAGE", invalid.ErrorCode);
        Assert.Equal("FILE_TOO_LARGE", oversized.ErrorCode);
    }

    [Fact]
    public async Task Extreme_dimensions_are_rejected_before_persistence()
    {
        var bytes = Encode(SKEncodedImageFormat.Png, 8200, 1);
        var result = await new LoginImageValidator().ValidateAsync(new MemoryStream(bytes), bytes.Length, LoginImageSlot.Main);
        Assert.Equal("IMAGE_DIMENSIONS_TOO_LARGE", result.ErrorCode);
    }

    [Fact]
    public async Task Wrong_ratio_and_low_resolution_are_quality_warnings_not_errors()
    {
        var result = await Validated(SKEncodedImageFormat.Png, LoginImageSlot.Main, 400, 800);
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, x => x.Code == "ASPECT_RATIO_MISMATCH");
        Assert.Contains(result.Warnings, x => x.Code == "LOW_RESOLUTION");
    }

    [Fact]
    public async Task Replacement_fit_mode_and_restore_are_safe()
    {
        await using var fixture = await Fixture.Create();
        var first = await Validated(SKEncodedImageFormat.Png, LoginImageSlot.Main, 1200, 900);
        await fixture.Service.ReplaceImageAsync(1, 1, LoginImageSlot.Main, first);
        var firstKey = fixture.Storage.Keys.Single();
        await fixture.Service.ReplaceImageAsync(1, 1, LoginImageSlot.Main, first);
        Assert.Contains(firstKey, fixture.Storage.Deleted);
        var changed = await fixture.Service.UpdateFitModeAsync(1, 1, LoginImageSlot.Main, LoginImageFitMode.Cover);
        Assert.Equal("Cover", changed.FitMode);
        var restored = await fixture.Service.RestoreDefaultAsync(1, LoginImageSlot.Main);
        Assert.True(restored.IsDefault);
        Assert.Empty(await fixture.Db.CompanyLoginImageSlots.Where(x => x.CompanyId == 1).ToListAsync());
    }

    [Fact]
    public async Task Failed_database_write_removes_new_file_and_keeps_old_reference()
    {
        await using var fixture = await Fixture.Create();
        var image = await Validated(SKEncodedImageFormat.Png, LoginImageSlot.Main, 1200, 900);
        await Assert.ThrowsAnyAsync<DbUpdateException>(() => fixture.Service.ReplaceImageAsync(999, 1, LoginImageSlot.Main, image));
        Assert.Single(fixture.Storage.Deleted);
        Assert.Empty(await fixture.Db.CompanyLoginImageSlots.ToListAsync());
    }

    [Fact]
    public async Task Public_endpoint_is_anonymous_and_admin_endpoint_requires_authentication()
    {
        await using var api = await ApiFixture.Create();
        Assert.Equal(HttpStatusCode.OK, (await api.Client.GetAsync("/api/public/login-appearance")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync("/api/admin/login-appearance")).StatusCode);
    }

    [Fact]
    public async Task Admin_endpoint_accepts_admin_and_rejects_player()
    {
        await using var api = await ApiFixture.Create();
        Assert.Equal(HttpStatusCode.OK, (await api.GetAdmin("ADMIN")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.GetAdmin("PLAYER")).StatusCode);
    }

    [Fact]
    public async Task Admin_upload_change_fit_and_restore_work_end_to_end()
    {
        await using var api = await ApiFixture.Create();
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encode(SKEncodedImageFormat.Png, 1200, 900));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", "fake.txt");
        using var upload = new HttpRequestMessage(HttpMethod.Post, "/api/admin/login-appearance/main/image") { Content = form };
        upload.Headers.Add("X-Test-Role", "ADMIN");
        Assert.Equal(HttpStatusCode.OK, (await api.Client.SendAsync(upload)).StatusCode);
        using var fit = new HttpRequestMessage(HttpMethod.Put, "/api/admin/login-appearance/main/fit-mode")
        { Content = new StringContent("{\"fitMode\":\"Cover\"}", Encoding.UTF8, "application/json") };
        fit.Headers.Add("X-Test-Role", "ADMIN");
        Assert.Equal(HttpStatusCode.OK, (await api.Client.SendAsync(fit)).StatusCode);
        using var delete = new HttpRequestMessage(HttpMethod.Delete, "/api/admin/login-appearance/main");
        delete.Headers.Add("X-Test-Role", "ADMIN");
        Assert.Equal(HttpStatusCode.OK, (await api.Client.SendAsync(delete)).StatusCode);
    }

    private static async Task<LoginImageValidationResult> Validated(SKEncodedImageFormat format, LoginImageSlot slot, int width, int height)
    {
        var bytes = Encode(format, width, height);
        return await new LoginImageValidator().ValidateAsync(new MemoryStream(bytes), bytes.Length, slot);
    }

    private static byte[] Encode(SKEncodedImageFormat format, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.Purple);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 85);
        return data.ToArray();
    }

    private sealed class FakeStorage : ILoginImageStorage
    {
        public List<string> Keys { get; } = [];
        public List<string> Deleted { get; } = [];
        public Task<string> SaveAsync(int companyId, LoginImageSlot slot, byte[] content, string extension, CancellationToken cancellationToken = default)
        { var key = $"login-appearance/{companyId}/{slot}-{Guid.NewGuid():N}{extension}"; Keys.Add(key); return Task.FromResult(key); }
        public Task DeleteAsync(string? imageKey, CancellationToken cancellationToken = default)
        { if (imageKey is not null) Deleted.Add(imageKey); return Task.CompletedTask; }
        public string GetPublicUrl(string imageKey) => "/api/uploads/" + imageKey;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public PlayPredictDbContext Db { get; }
        public FakeStorage Storage { get; } = new();
        public LoginAppearanceService Service { get; }
        private Fixture(SqliteConnection connection, PlayPredictDbContext db)
        { this.connection = connection; Db = db; Service = new(db, Storage, TimeProvider.System); }
        public static async Task<Fixture> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var db = new PlayPredictDbContext(new DbContextOptionsBuilder<PlayPredictDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            db.Companies.AddRange(new Company { Id = 1, Name = "One" }, new Company { Id = 2, Name = "Two" });
            db.Users.Add(new User { Id = 1, CompanyId = 1, FirstName = "Admin", LastName = "One", Email = "a@x", PasswordHash = "x", CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
            return new(connection, db);
        }
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }

    private sealed class FixedResolver : ILoginAppearanceCompanyResolver { public Task<int> ResolvePublicCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(1); }
    private sealed class ApiFixture : IAsyncDisposable
    {
        private readonly WebApplication app; private readonly SqliteConnection connection; public HttpClient Client { get; }
        private ApiFixture(WebApplication app, SqliteConnection connection) { this.app = app; this.connection = connection; Client = app.GetTestClient(); }
        public static async Task<ApiFixture> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var builder = WebApplication.CreateBuilder(); builder.WebHost.UseTestServer();
            builder.Services.AddAuthentication(TestAuthHandler.Scheme).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.Scheme, _ => { });
            builder.Services.AddAuthorization(); builder.Services.AddDbContext<PlayPredictDbContext>(x => x.UseSqlite(connection));
            builder.Services.AddSingleton<ILoginImageStorage, FakeStorage>(); builder.Services.AddSingleton<ILoginAppearanceCompanyResolver, FixedResolver>();
            builder.Services.AddSingleton<LoginImageValidator>(); builder.Services.AddScoped<LoginAppearanceService>(); builder.Services.AddSingleton(TimeProvider.System);
            var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapLoginAppearanceEndpoints(); await app.StartAsync();
            await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>(); await db.Database.EnsureCreatedAsync();
            db.Companies.Add(new Company { Id = 1, Name = "One" }); db.Users.Add(new User { Id = 1, CompanyId = 1, FirstName = "A", LastName = "B", Email = "a@x", PasswordHash = "x", CreatedAtUtc = DateTime.UtcNow }); await db.SaveChangesAsync();
            return new(app, connection);
        }
        public Task<HttpResponseMessage> GetAdmin(string role) { var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/login-appearance"); request.Headers.Add("X-Test-Role", role); return Client.SendAsync(request); }
        public async ValueTask DisposeAsync() { Client.Dispose(); await app.DisposeAsync(); await connection.DisposeAsync(); }
    }

    private sealed class TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public new const string Scheme = "Test";
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Role", out var role)) return Task.FromResult(AuthenticateResult.NoResult());
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "1"), new Claim(ClaimTypes.Role, role.ToString()), new Claim("companyId", "1") };
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme)), Scheme)));
        }
    }
}
