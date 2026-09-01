using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using PlayPredict.Api.Data;
using PlayPredict.Api.Endpoints;
using PlayPredict.Api.Imports;
using PlayPredict.Api.LoginAppearance;
using PlayPredict.Api.Services;
using PlayPredict.Api.WelcomeCampaigns;

const string AppVersion = "0.1.0";
const string FrontendCorsPolicy = "FrontendCorsPolicy";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:5175", "http://127.0.0.1:5175")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<PlayPredictDbContext>(options =>
{
    options.UseNpgsql(connectionString);
    // Falso positivo verificado con "dotnet ef migrations add": no hay diferencias reales
    // entre el modelo y la última migración; se evita que este chequeo aborte el arranque.
    options.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;
var jwtIssuer = jwtSection["Issuer"]!;
var jwtAudience = jwtSection["Audience"]!;

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddScoped<PredictionEvaluationService>();
builder.Services.AddScoped<RankingService>();
builder.Services.AddScoped<PrizeWinnerService>();
builder.Services.AddScoped<LeagueScoringService>();
builder.Services.AddSingleton<SpreadsheetReader>();
builder.Services.AddScoped<TeamRosterImportPreviewService>();
builder.Services.AddScoped<TeamRosterImportConfirmationService>();
builder.Services.AddScoped<MatchImportPreviewService>();
builder.Services.AddScoped<MatchImportConfirmationService>();
builder.Services.Configure<TeamRosterImportOptions>(
    builder.Configuration.GetSection(TeamRosterImportOptions.SectionName));
builder.Services.Configure<LoginAppearanceOptions>(builder.Configuration.GetSection(LoginAppearanceOptions.SectionName));
builder.Services.AddScoped<ILoginAppearanceCompanyResolver, ConfiguredLoginAppearanceCompanyResolver>();
builder.Services.AddSingleton<ILoginImageStorage, LocalLoginImageStorage>();
builder.Services.AddSingleton<LoginImageValidator>();
builder.Services.AddScoped<LoginAppearanceService>();
builder.Services.AddSingleton<IWelcomeCampaignImageStorage, LocalWelcomeCampaignImageStorage>();
builder.Services.AddSingleton<WelcomeCampaignImageValidator>();
builder.Services.AddScoped<WelcomeCampaignService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var uploadRoot = ManagedImageStorage.GetRoot(builder.Configuration, builder.Environment);
Directory.CreateDirectory(uploadRoot);
ManagedImageStorage.CopyLegacyFiles(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(FrontendCorsPolicy);

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadRoot),
    RequestPath = "/api/uploads",
    OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
    .WithName("GetHealth");

app.MapGet("/api/system/info", () => Results.Ok(new
{
    application = "PlayPredict",
    status = "running",
    version = AppVersion
}))
    .WithName("GetSystemInfo");

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapUserTeamPreferredPlayerEndpoints();
app.MapAdminUserEndpoints();
app.MapCompetitionEndpoints();
app.MapEditionEndpoints();
app.MapRoundEndpoints();
app.MapMatchEndpoints();
app.MapTeamEndpoints();
app.MapTeamPlayerEndpoints();
app.MapPredictionEndpoints();
app.MapEditionScoringConfigurationEndpoints();
app.MapRankingEndpoints();
app.MapAdminPrizeEndpoints();
app.MapPrizeEndpoints();
app.MapAdminExperienceEndpoints();
app.MapLeagueEndpoints();
app.MapAdminOfficialLeagueEndpoints();
app.MapCompanySettingsEndpoints();
app.MapAdminTeamRosterImportEndpoints();
app.MapAdminMatchImportEndpoints();
app.MapLoginAppearanceEndpoints();
app.MapWelcomeCampaignEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (app.Environment.IsEnvironment(LoadTestSeeder.EnvironmentName))
    {
        var databaseName = string.IsNullOrWhiteSpace(connectionString)
            ? "<missing>"
            : new Npgsql.NpgsqlConnectionStringBuilder(connectionString).Database;
        logger.LogWarning("========== LOADTEST ENVIRONMENT ==========");
        logger.LogWarning("LOADTEST backend connected to database: {Database}", databaseName);
        logger.LogWarning("==========================================");
    }

    // --- Migraciones: aplicar antes de cualquier seeder ---
    // Si una migración falla, el proceso aborta aquí. Nunca se ejecuta
    // un seeder contra un esquema incompleto.
    var pendingMigrations = await db.Database.GetPendingMigrationsAsync();

    if (pendingMigrations.Any())
    {
        logger.LogInformation("Pending migrations: {Count}", pendingMigrations.Count());
        foreach (var migration in pendingMigrations)
        {
            logger.LogInformation("Applying migration: {Migration}", migration);
        }

        try
        {
            await db.Database.MigrateAsync();
            logger.LogInformation("All pending migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Migration failed. Aborting startup. Seeders will NOT run.");
            throw;
        }
    }
    else
    {
        logger.LogInformation("Database schema is up to date. No pending migrations.");
    }

    // --- Seeders: solo después de que el schema esté completo ---
    // LoadTest tiene su propio catálogo y no debe recibir equipos/datos de Development.
    if (!app.Environment.IsEnvironment(LoadTestSeeder.EnvironmentName))
    {
        await DataSeeder.SeedCoreDataAsync(db);
    }

    if (app.Environment.IsDevelopment())
    {
        await DataSeeder.SeedAdminUsersAsync(db, app.Configuration, app.Environment);
    }

    if (args.Contains("--reset-demo-game-data", StringComparer.OrdinalIgnoreCase))
    {
        await DemoGameDataResetter.ResetAsync(db);
        logger.LogInformation("Demo game data reset completed.");
        return;
    }

    if (!app.Environment.IsEnvironment(LoadTestSeeder.EnvironmentName))
    {
        await DataSeeder.SeedEditionScoringConfigurationsAsync(db);
    }

    if (args.Contains("--seed-loadtest", StringComparer.OrdinalIgnoreCase))
    {
        var options = builder.Configuration.GetSection(LoadTestSeedOptions.SectionName)
            .Get<LoadTestSeedOptions>() ?? new LoadTestSeedOptions();
        await LoadTestSeeder.SeedAsync(db, options, app.Environment.EnvironmentName, connectionString, logger);
        return;
    }

    if (app.Environment.IsDevelopment())
    {
        var evaluationService = scope.ServiceProvider.GetRequiredService<PredictionEvaluationService>();
        await DemoDatasetV1Seeder.SeedAsync(db, evaluationService);
        if (app.Configuration.GetValue<bool>("DemoSeed:RankingDense"))
        {
            await DemoDatasetV1Seeder.SeedDenseRankingAsync(db, evaluationService);
            logger.LogInformation("Dense ranking demo data is ready in {LeagueName}.", DemoDatasetV1Seeder.DenseRankingLeagueName);
        }
    }
}

app.Run();
