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
using PlayPredict.Api.WelcomeCampaigns;
using SkiaSharp;
using Xunit;
using Npgsql;

namespace PlayPredict.Api.Tests;

public class WelcomeCampaignTests
{
    [Fact]
    public async Task Creating_a_campaign_starts_inactive_and_without_slides()
    {
        await using var fixture = await Fixture.Create();
        var campaign = await fixture.Service.CreateAsync(1, 1, "Campana A", null, null);
        Assert.False(campaign.IsActive);
        Assert.Empty(campaign.Slides);
    }

    [Fact]
    public async Task Fourth_slide_is_rejected()
    {
        await using var fixture = await Fixture.Create();
        var campaign = await fixture.Service.CreateAsync(1, 1, "Campana A", null, null);
        for (var i = 0; i < 3; i++)
        {
            var image = await Validated(1200, 900);
            await fixture.Service.AddSlideAsync(1, campaign.Id, image);
        }
        var fourth = await Validated(1200, 900);
        var ex = await Assert.ThrowsAsync<WelcomeCampaignValidationException>(() => fixture.Service.AddSlideAsync(1, campaign.Id, fourth));
        Assert.Equal("MAX_SLIDES", ex.Code);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(10.1)]
    public async Task Duration_outside_one_to_ten_is_rejected(decimal duration)
    {
        await using var fixture = await Fixture.Create();
        var campaign = await fixture.Service.CreateAsync(1, 1, "Campana A", null, null);
        var slide = await fixture.Service.AddSlideAsync(1, campaign.Id, await Validated(1200, 900));
        var ex = await Assert.ThrowsAsync<WelcomeCampaignValidationException>(
            () => fixture.Service.UpdateSlideAsync(1, campaign.Id, slide!.Id, duration, WelcomeCampaignFitMode.Cover));
        Assert.Equal("INVALID_DURATION", ex.Code);
    }

    [Fact]
    public async Task Duration_within_range_and_fit_mode_are_saved()
    {
        await using var fixture = await Fixture.Create();
        var campaign = await fixture.Service.CreateAsync(1, 1, "Campana A", null, null);
        var slide = await fixture.Service.AddSlideAsync(1, campaign.Id, await Validated(1200, 900));
        var updated = await fixture.Service.UpdateSlideAsync(1, campaign.Id, slide!.Id, 1.5m, WelcomeCampaignFitMode.Contain);
        Assert.Equal(1.5m, updated!.DurationSeconds);
        Assert.Equal("Contain", updated.FitMode);
    }

    [Fact]
    public async Task Activation_requires_at_least_one_slide()
    {
        await using var fixture = await Fixture.Create();
        var campaign = await fixture.Service.CreateAsync(1, 1, "Campana A", null, null);
        var ex = await Assert.ThrowsAsync<WelcomeCampaignValidationException>(() => fixture.Service.ActivateAsync(1, 1, campaign.Id));
        Assert.Equal("NO_SLIDES", ex.Code);
    }

    [Fact]
    public async Task Activating_a_campaign_deactivates_the_previously_active_one()
    {
        await using var fixture = await Fixture.Create();
        var a = await fixture.Service.CreateAsync(1, 1, "Campana A", null, null);
        await fixture.Service.AddSlideAsync(1, a.Id, await Validated(1200, 900));
        var b = await fixture.Service.CreateAsync(1, 1, "Campana B", null, null);
        await fixture.Service.AddSlideAsync(1, b.Id, await Validated(1200, 900));

        await fixture.Service.ActivateAsync(1, 1, a.Id);
        Assert.True((await fixture.Service.GetAsync(1, a.Id))!.IsActive);

        await fixture.Service.ActivateAsync(1, 1, b.Id);
        Assert.False((await fixture.Service.GetAsync(1, a.Id))!.IsActive);
        Assert.True((await fixture.Service.GetAsync(1, b.Id))!.IsActive);
    }

    [Fact]
    public async Task Active_endpoint_ignores_future_and_expired_campaigns()
    {
        await using var fixture = await Fixture.Create(now: new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));
        var future = await fixture.Service.CreateAsync(1, 1, "Futura", new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc), null);
        await fixture.Service.AddSlideAsync(1, future.Id, await Validated(1200, 900));
        await fixture.Service.ActivateAsync(1, 1, future.Id);
        Assert.Null(await fixture.Service.GetActiveForCompanyAsync(1));

        var expired = await fixture.Service.CreateAsync(1, 1, "Vencida", null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await fixture.Service.AddSlideAsync(1, expired.Id, await Validated(1200, 900));
        await fixture.Service.ActivateAsync(1, 1, expired.Id);
        Assert.Null(await fixture.Service.GetActiveForCompanyAsync(1));
    }

    [Fact]
    public async Task Active_endpoint_returns_the_valid_active_campaign_ordered_by_sort_order()
    {
        await using var fixture = await Fixture.Create();
        var campaign = await fixture.Service.CreateAsync(1, 1, "Vigente", null, null);
        var second = await fixture.Service.AddSlideAsync(1, campaign.Id, await Validated(1200, 900));
        var first = await fixture.Service.AddSlideAsync(1, campaign.Id, await Validated(1200, 900));
        await fixture.Service.ReorderSlideAsync(1, campaign.Id, first!.Id, 1);
        await fixture.Service.ActivateAsync(1, 1, campaign.Id);

        var active = await fixture.Service.GetActiveForCompanyAsync(1);
        Assert.NotNull(active);
        Assert.Equal(2, active!.Slides.Count);
        Assert.Equal(first.Id, active.Slides[0].Id);
        Assert.Equal(second!.Id, active.Slides[1].Id);
    }

    [Fact]
    public async Task Companies_are_isolated()
    {
        await using var fixture = await Fixture.Create();
        var campaign = await fixture.Service.CreateAsync(1, 1, "De la empresa 1", null, null);
        await fixture.Service.AddSlideAsync(1, campaign.Id, await Validated(1200, 900));
        await fixture.Service.ActivateAsync(1, 1, campaign.Id);

        Assert.Null(await fixture.Service.GetAsync(2, campaign.Id));
        Assert.Null(await fixture.Service.GetActiveForCompanyAsync(2));
        Assert.NotNull(await fixture.Service.GetActiveForCompanyAsync(1));
    }

    [Fact]
    public async Task Missing_campaign_returns_null()
    {
        await using var fixture = await Fixture.Create();
        Assert.Null(await fixture.Service.GetAsync(1, 999));
        Assert.Null(await fixture.Service.UpdateAsync(1, 1, 999, "x", null, null));
        Assert.Null(await fixture.Service.ActivateAsync(1, 1, 999));
    }

    [Fact]
    public async Task Invalid_upload_is_rejected_by_the_validator()
    {
        var validator = new WelcomeCampaignImageValidator();
        var invalid = await validator.ValidateAsync(new MemoryStream(Encoding.UTF8.GetBytes("not-image")), 9);
        Assert.Equal("INVALID_IMAGE", invalid.ErrorCode);
        var oversized = await validator.ValidateAsync(Stream.Null, WelcomeCampaignImageValidator.MaximumBytes + 1);
        Assert.Equal("FILE_TOO_LARGE", oversized.ErrorCode);
    }

    [Fact]
    public async Task Replacing_and_deleting_a_slide_cleans_up_storage()
    {
        await using var fixture = await Fixture.Create();
        var campaign = await fixture.Service.CreateAsync(1, 1, "Campana A", null, null);
        var slide = await fixture.Service.AddSlideAsync(1, campaign.Id, await Validated(1200, 900));
        var firstKey = fixture.Storage.Keys.Single();

        await fixture.Service.ReplaceSlideImageAsync(1, campaign.Id, slide!.Id, await Validated(1200, 900));
        Assert.Contains(firstKey, fixture.Storage.Deleted);

        var secondKey = fixture.Storage.Keys.Last();
        var remaining = await fixture.Service.DeleteSlideAsync(1, campaign.Id, slide.Id);
        Assert.Contains(secondKey, fixture.Storage.Deleted);
        Assert.Empty(remaining!);
    }

    [Fact]
    public async Task Deleting_an_active_campaign_is_blocked()
    {
        await using var fixture = await Fixture.Create();
        var campaign = await fixture.Service.CreateAsync(1, 1, "Campana A", null, null);
        await fixture.Service.AddSlideAsync(1, campaign.Id, await Validated(1200, 900));
        await fixture.Service.ActivateAsync(1, 1, campaign.Id);
        var ex = await Assert.ThrowsAsync<WelcomeCampaignValidationException>(() => fixture.Service.DeleteAsync(1, campaign.Id));
        Assert.Equal("CAMPAIGN_ACTIVE", ex.Code);
    }

    [Fact]
    public async Task Public_active_endpoint_requires_authentication_and_admin_endpoint_requires_admin_role()
    {
        await using var api = await ApiFixture.Create();
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync("/api/welcome-campaign/active")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Client.GetAsync("/api/admin/welcome-campaigns")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await api.GetWithRole("/api/welcome-campaign/active", "PLAYER")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await api.GetWithRole("/api/admin/welcome-campaigns", "ADMIN")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.GetWithRole("/api/admin/welcome-campaigns", "PLAYER")).StatusCode);
    }

    [Fact]
    public async Task Admin_create_upload_activate_and_active_endpoint_work_end_to_end()
    {
        await using var api = await ApiFixture.Create();
        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/admin/welcome-campaigns")
        { Content = new StringContent("{\"name\":\"Campana E2E\"}", Encoding.UTF8, "application/json") };
        create.Headers.Add("X-Test-Role", "ADMIN");
        var createResponse = await api.Client.SendAsync(create);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync()).RootElement;
        var campaignId = created.GetProperty("id").GetInt32();

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encode(1200, 900));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(file, "file", "fake.png");
        using var upload = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/welcome-campaigns/{campaignId}/slides") { Content = form };
        upload.Headers.Add("X-Test-Role", "ADMIN");
        Assert.Equal(HttpStatusCode.OK, (await api.Client.SendAsync(upload)).StatusCode);

        using var activate = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/welcome-campaigns/{campaignId}/activate");
        activate.Headers.Add("X-Test-Role", "ADMIN");
        Assert.Equal(HttpStatusCode.OK, (await api.Client.SendAsync(activate)).StatusCode);

        var active = await api.GetWithRole("/api/welcome-campaign/active", "PLAYER");
        Assert.Equal(HttpStatusCode.OK, active.StatusCode);
        var body = await active.Content.ReadAsStringAsync();
        Assert.Contains("Campana E2E", body);
    }

    [Fact]
    public async Task Database_physically_rejects_two_active_campaigns_for_the_same_company()
    {
        await using var fixture = await Fixture.Create();
        var a = await fixture.Service.CreateAsync(1, 1, "Campana A", null, null);
        var b = await fixture.Service.CreateAsync(1, 1, "Campana B", null, null);

        // Activa A por el camino normal (servicio).
        await fixture.Service.AddSlideAsync(1, a.Id, await Validated(1200, 900));
        await fixture.Service.ActivateAsync(1, 1, a.Id);

        // Ahora se intenta, DELIBERADAMENTE bypaseando ActivateAsync, dejar B también activa
        // para la misma Company en un único SaveChanges: esto es lo que el índice físico debe impedir,
        // sea por un bug futuro en la lógica de aplicación o por una carrera real entre dos requests.
        var bEntity = await fixture.Db.WelcomeCampaigns.SingleAsync(c => c.Id == b.Id);
        bEntity.IsActive = true;
        var ex = await Assert.ThrowsAnyAsync<DbUpdateException>(() => fixture.Db.SaveChangesAsync());
        Assert.IsType<DbUpdateException>(ex);

        // Sin corrupción: A sigue siendo la única activa en DB tras el rollback.
        await using var verifyDb = fixture.OpenSeparateContext();
        var active = await verifyDb.WelcomeCampaigns.Where(c => c.CompanyId == 1 && c.IsActive).ToListAsync();
        Assert.Single(active);
        Assert.Equal(a.Id, active[0].Id);
    }

    [Fact]
    public async Task Multiple_inactive_campaigns_for_the_same_company_are_allowed()
    {
        await using var fixture = await Fixture.Create();
        await fixture.Service.CreateAsync(1, 1, "Campana A", null, null);
        await fixture.Service.CreateAsync(1, 1, "Campana B", null, null);
        await fixture.Service.CreateAsync(1, 1, "Campana C", null, null);

        var all = await fixture.Service.GetAllAsync(1);
        Assert.Equal(3, all.Count);
        Assert.All(all, c => Assert.False(c.IsActive));
    }

    [Fact]
    public async Task Activation_does_not_mistranslate_an_unrelated_DbUpdateException_as_a_concurrency_conflict()
    {
        // La regla es: sólo la violación puntual del índice único de "una activa por Company" se
        // traduce a WelcomeCampaignConcurrencyException. Cualquier otro DbUpdateException (por ejemplo,
        // una FK inválida) debe propagarse tal cual, sin ser capturado indiscriminadamente como conflicto.
        await using var fixture = await Fixture.Create();
        var campaign = await fixture.Service.CreateAsync(1, 1, "Campana A", null, null);
        await fixture.Service.AddSlideAsync(1, campaign.Id, await Validated(1200, 900));

        const int nonExistentUserId = 999999;
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Service.ActivateAsync(1, nonExistentUserId, campaign.Id));
        Assert.IsNotType<WelcomeCampaignConcurrencyException>(ex);
    }

    [Fact]
    public async Task Different_companies_can_each_have_their_own_active_campaign()
    {
        await using var fixture = await Fixture.Create();
        var c1 = await fixture.Service.CreateAsync(1, 1, "Campana Company 1", null, null);
        await fixture.Service.AddSlideAsync(1, c1.Id, await Validated(1200, 900));
        var c2 = await fixture.Service.CreateAsync(2, 1, "Campana Company 2", null, null);
        await fixture.Service.AddSlideAsync(2, c2.Id, await Validated(1200, 900));

        await fixture.Service.ActivateAsync(1, 1, c1.Id);
        await fixture.Service.ActivateAsync(2, 1, c2.Id);

        Assert.True((await fixture.Service.GetAsync(1, c1.Id))!.IsActive);
        Assert.True((await fixture.Service.GetAsync(2, c2.Id))!.IsActive);
    }

    private static async Task<WelcomeCampaignImageValidationResult> Validated(int width, int height)
    {
        var bytes = Encode(width, height);
        return await new WelcomeCampaignImageValidator().ValidateAsync(new MemoryStream(bytes), bytes.Length);
    }

    private static byte[] Encode(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.Teal);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 85);
        return data.ToArray();
    }

    private sealed class FakeStorage : IWelcomeCampaignImageStorage
    {
        public List<string> Keys { get; } = [];
        public List<string> Deleted { get; } = [];
        public Task<string> SaveAsync(int companyId, int campaignId, byte[] content, string extension, CancellationToken cancellationToken = default)
        { var key = $"welcome-campaigns/{companyId}/{campaignId}/{Guid.NewGuid():N}{extension}"; Keys.Add(key); return Task.FromResult(key); }
        public Task DeleteAsync(string? imageKey, CancellationToken cancellationToken = default)
        { if (imageKey is not null) Deleted.Add(imageKey); return Task.CompletedTask; }
        public string GetPublicUrl(string imageKey) => "/api/uploads/" + imageKey;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public PlayPredictDbContext Db { get; }
        public FakeStorage Storage { get; } = new();
        public WelcomeCampaignService Service { get; }
        private Fixture(SqliteConnection connection, PlayPredictDbContext db, DateTime? now)
        { this.connection = connection; Db = db; Service = new(db, Storage, now is null ? TimeProvider.System : new FixedTimeProvider(now.Value)); }
        public static async Task<Fixture> Create(DateTime? now = null)
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var db = new PlayPredictDbContext(new DbContextOptionsBuilder<PlayPredictDbContext>().UseSqlite(connection).Options);
            await db.Database.EnsureCreatedAsync();
            db.Companies.AddRange(new Company { Id = 1, Name = "One" }, new Company { Id = 2, Name = "Two" });
            db.Users.Add(new User { Id = 1, CompanyId = 1, FirstName = "Admin", LastName = "One", Email = "a@x", PasswordHash = "x", CreatedAtUtc = DateTime.UtcNow });
            await db.SaveChangesAsync();
            return new(connection, db, now);
        }
        public PlayPredictDbContext OpenSeparateContext() =>
            new(new DbContextOptionsBuilder<PlayPredictDbContext>().UseSqlite(connection).Options);
        public async ValueTask DisposeAsync() { await Db.DisposeAsync(); await connection.DisposeAsync(); }
    }

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
            builder.Services.AddSingleton<IWelcomeCampaignImageStorage, FakeStorage>();
            builder.Services.AddSingleton<WelcomeCampaignImageValidator>(); builder.Services.AddScoped<WelcomeCampaignService>(); builder.Services.AddSingleton(TimeProvider.System);
            var app = builder.Build(); app.UseAuthentication(); app.UseAuthorization(); app.MapWelcomeCampaignEndpoints(); await app.StartAsync();
            await using var scope = app.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>(); await db.Database.EnsureCreatedAsync();
            db.Companies.Add(new Company { Id = 1, Name = "One" }); db.Users.Add(new User { Id = 1, CompanyId = 1, FirstName = "A", LastName = "B", Email = "a@x", PasswordHash = "x", CreatedAtUtc = DateTime.UtcNow }); await db.SaveChangesAsync();
            return new(app, connection);
        }
        public Task<HttpResponseMessage> GetWithRole(string path, string role) { var request = new HttpRequestMessage(HttpMethod.Get, path); request.Headers.Add("X-Test-Role", role); return Client.SendAsync(request); }
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
