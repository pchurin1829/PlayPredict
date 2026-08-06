using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
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

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(FrontendCorsPolicy);

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
app.MapPredictionEndpoints();
app.MapEditionScoringConfigurationEndpoints();
app.MapRankingEndpoints();
app.MapAdminPrizeEndpoints();
app.MapPrizeEndpoints();
app.MapAdminExperienceEndpoints();
app.MapLeagueEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
    await db.Database.MigrateAsync();

    await DataSeeder.SeedCoreDataAsync(db);

    if (app.Environment.IsDevelopment())
    {
        await DataSeeder.SeedAsync(db);
        await DataSeeder.SeedAdminUsersAsync(db, app.Configuration, app.Environment);
    }

    // Después de cualquier seed de datos: garantiza que toda Edición (existente o
    // recién sembrada) tenga configuración de puntuación.
    await DataSeeder.SeedEditionScoringConfigurationsAsync(db);

    if (app.Environment.IsDevelopment())
    {
        var evaluationService = scope.ServiceProvider.GetRequiredService<PredictionEvaluationService>();
        await DataSeeder.SeedRankingDemoAsync(db, evaluationService);
        await DataSeeder.SeedPrizesDemoAsync(db);
    }
}

app.Run();
