using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using PlayPredict.Api.Data;
using PlayPredict.Api.Endpoints;
using PlayPredict.Api.Services;

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

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads", "team-players"));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(FrontendCorsPolicy);

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads")),
    RequestPath = "/api/uploads"
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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

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
    await DataSeeder.SeedCoreDataAsync(db);
    await DataSeeder.SeedDemoTeamPlayersAsync(db);

    if (app.Environment.IsDevelopment())
    {
        await DataSeeder.SeedAsync(db);
        await DataSeeder.SeedAdminUsersAsync(db, app.Configuration, app.Environment);
    }

    await DataSeeder.SeedEditionScoringConfigurationsAsync(db);

    if (app.Environment.IsDevelopment())
    {
        var evaluationService = scope.ServiceProvider.GetRequiredService<PredictionEvaluationService>();
        await DataSeeder.SeedRankingDemoAsync(db, evaluationService);
        await DataSeeder.RefreshDemoScheduleAsync(db);
        await DataSeeder.SeedPrizesDemoAsync(db);
    }
}

app.Run();
