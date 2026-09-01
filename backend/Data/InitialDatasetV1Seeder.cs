using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Imports;

namespace PlayPredict.Api.Data;

/// <summary>
/// Instalador explícito y destructivo de la base inicial de primera prueba real.
/// Nunca se ejecuta durante un arranque normal: Program.cs exige --seed-initial-v1.
/// </summary>
public static class InitialDatasetV1Seeder
{
    public const string ReferenceName = "Torneo Clausura AFA 2026";
    public const string ClientLeagueName = "COPA EL NENE";

    public static async Task SeedAsync(
        PlayPredictDbContext db,
        string baseWorkbookPath,
        string rosterWorkbookPath,
        CancellationToken cancellationToken = default)
    {
        EnsureFile(baseWorkbookPath);
        EnsureFile(rosterWorkbookPath);

        await ClearReplaceableDataAsync(db, cancellationToken);
        await DataSeeder.SeedCoreDataAsync(db);

        var now = DateTime.UtcNow;
        var company = await db.Companies.SingleAsync(cancellationToken);
        company.Name = "EL NENE";
        company.ShortName = "EL NENE";

        var adminRole = await db.Roles.SingleAsync(role => role.Name == RoleNames.Admin, cancellationToken);
        var playerRole = await db.Roles.SingleAsync(role => role.Name == RoleNames.Player, cancellationToken);
        var hasher = new PasswordHasher<User>();

        var admin = NewUser(company.Id, "ADMIN", "Administrador", "Inicial", adminRole, now);
        admin.PasswordHash = hasher.HashPassword(admin, "admin123");
        var player = NewUser(company.Id, "USUARIO", "Usuario", "Inicial", playerRole, now);
        player.PasswordHash = hasher.HashPassword(player, "usuario");
        db.Users.AddRange(admin, player);

        var experience = new Experience
        {
            Name = ClientLeagueName,
            Description = "Primera prueba real de PlayPredict para EL NENE.",
            Status = ExperienceStatus.Published,
            IsPublic = true,
            DefaultExactScorePoints = 6,
            DefaultCorrectOutcomePoints = 3,
            DefaultIncorrectPoints = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        var competition = new Competition
        {
            Experience = experience,
            Name = ReferenceName,
            Description = "Referencia deportiva canónica; no jugable directamente por PLAYER.",
            Sport = "Fútbol",
            IsActive = true,
            CreatedAtUtc = now
        };
        var edition = new Edition
        {
            Competition = competition,
            Name = "Clausura 2026",
            StartDateUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Status = EditionStatus.Active,
            CreatedAtUtc = now
        };
        db.Editions.Add(edition);
        await db.SaveChangesAsync(cancellationToken);

        db.EditionScoringConfigurations.Add(new EditionScoringConfiguration
        {
            EditionId = edition.Id,
            ExactScorePoints = 6,
            CorrectOutcomePoints = 3,
            IncorrectPoints = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await db.SaveChangesAsync(cancellationToken);

        await ImportRostersAsync(db, rosterWorkbookPath, cancellationToken);
        await ImportMatchesAsync(db, baseWorkbookPath, edition.Id, admin.Id, cancellationToken);

        var sourceLeague = NewLeague(ReferenceName, competition.Id, edition.Id, admin.Id, now);
        sourceLeague.Description = "Fuente deportiva AFA. No visible ni jugable para PLAYER.";
        sourceLeague.IsActive = false;
        db.Leagues.Add(sourceLeague);
        await db.SaveChangesAsync(cancellationToken);

        var clientLeague = NewLeague(ClientLeagueName, competition.Id, edition.Id, admin.Id, now);
        clientLeague.Description = "Competencia oficial y jugable de EL NENE.";
        clientLeague.SourceLeagueId = sourceLeague.Id;
        db.Leagues.Add(clientLeague);
        await db.SaveChangesAsync(cancellationToken);

        db.LeagueParticipants.Add(new LeagueParticipant
        {
            LeagueId = clientLeague.Id,
            UserId = player.Id,
            JoinedAtUtc = now
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static User NewUser(int companyId, string login, string firstName, string lastName, Role role, DateTime now)
    {
        var user = new User
        {
            CompanyId = companyId,
            Email = login.ToLowerInvariant(),
            FirstName = firstName,
            LastName = lastName,
            IsActive = true,
            CreatedAtUtc = now
        };
        user.UserRoles.Add(new UserRole { Role = role });
        return user;
    }

    private static League NewLeague(string name, int competitionId, int editionId, int adminId, DateTime now) => new()
    {
        Name = name,
        CompetitionId = competitionId,
        EditionId = editionId,
        ScopeType = LeagueScopeType.FullCompetition,
        InviteCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(6)),
        LeagueType = LeagueType.Official,
        IsActive = true,
        CreatedByUserId = adminId,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
        UseGeneralScoring = true,
        PreferredPlayerEnabled = true,
        PreferredPlayerPointsPerGoal = 2,
        PreferredPlayerPositions = PlayerPosition.Midfielder | PlayerPosition.Forward
    };

    private static async Task ImportRostersAsync(PlayPredictDbContext db, string path, CancellationToken cancellationToken)
    {
        var service = new TeamRosterImportConfirmationService(db);
        await using var stream = File.OpenRead(path);
        var result = await service.ConfirmAsync(stream, Path.GetFileName(path), "Fútbol", Hash(path), cancellationToken);
        if (result.Status != ImportConfirmationStatus.Success)
            throw new InvalidOperationException($"Falló la importación de planteles: {result.Message} {FormatIssues(result.Issues)}");
    }

    private static async Task ImportMatchesAsync(
        PlayPredictDbContext db, string path, int editionId, int adminId, CancellationToken cancellationToken)
    {
        var service = new MatchImportConfirmationService(
            db,
            NullLogger<MatchImportConfirmationService>.Instance);
        await using var stream = File.OpenRead(path);
        var result = await service.ConfirmAsync(stream, Path.GetFileName(path), editionId, adminId, Hash(path), cancellationToken);
        if (result.Status != ImportConfirmationStatus.Success)
            throw new InvalidOperationException($"Falló la importación de partidos: {result.Message} {FormatIssues(result.Issues)}");
    }

    private static async Task ClearReplaceableDataAsync(PlayPredictDbContext db, CancellationToken cancellationToken)
    {
        await db.PredictionEvaluations.ExecuteDeleteAsync(cancellationToken);
        await db.MatchScorers.ExecuteDeleteAsync(cancellationToken);
        await db.UserTeamPreferredPlayers.ExecuteDeleteAsync(cancellationToken);
        await db.Predictions.ExecuteDeleteAsync(cancellationToken);
        await db.LeagueParticipants.ExecuteDeleteAsync(cancellationToken);
        await db.Prizes.ExecuteDeleteAsync(cancellationToken);
        await db.Leagues.ExecuteDeleteAsync(cancellationToken);
        await db.Matches.ExecuteDeleteAsync(cancellationToken);
        await db.Rounds.ExecuteDeleteAsync(cancellationToken);
        await db.EditionScoringConfigurations.ExecuteDeleteAsync(cancellationToken);
        await db.Editions.ExecuteDeleteAsync(cancellationToken);
        await db.Competitions.ExecuteDeleteAsync(cancellationToken);
        await db.Experiences.ExecuteDeleteAsync(cancellationToken);
        await db.WelcomeCampaignSlides.ExecuteDeleteAsync(cancellationToken);
        await db.WelcomeCampaigns.ExecuteDeleteAsync(cancellationToken);
        await db.CompanyLoginImageSlots.ExecuteDeleteAsync(cancellationToken);
        await db.UserRoles.ExecuteDeleteAsync(cancellationToken);
        await db.Users.ExecuteDeleteAsync(cancellationToken);
        await db.TeamPlayers.ExecuteDeleteAsync(cancellationToken);
        await db.Teams.ExecuteDeleteAsync(cancellationToken);
        await db.Roles.ExecuteDeleteAsync(cancellationToken);
        await db.Companies.ExecuteDeleteAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static string FormatIssues(IReadOnlyList<SpreadsheetValidationIssue> issues) =>
        string.Join(" | ", issues.Take(20).Select(issue => $"{issue.Code}: {issue.Message}"));

    private static void EnsureFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("No se encontró el XLS requerido.", path);
    }
}
