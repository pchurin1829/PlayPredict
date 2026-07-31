using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Data;

public static class DataSeeder
{
    private const string LigaProfesionalName = "Liga Profesional";
    private const string ClausuraEditionName = "Clausura 2026";
    private const string CopaLibertadoresName = "Copa Libertadores";
    private const string FaseDeGruposEditionName = "Fase de Grupos 2026";
    private const string RoundName = "Fecha 1";
    private const string DefaultCompanyName = "PlayPredict";
    private const string AdminEmail = "admin@playpredict.local";
    private const string AdminPassword = "admin123";

    private const string RankingDemoPassword = "demo123";

    private static readonly (string Email, string FirstName, string LastName)[] RankingDemoUsers =
    {
        ("ana.torres@playpredict.local", "Ana", "Torres"),
        ("juan.perez@playpredict.local", "Juan", "Pérez"),
        ("maria.lopez@playpredict.local", "María", "López"),
        ("pedro.gomez@playpredict.local", "Pedro", "Gómez"),
    };

    // (ParticipantHome, ParticipantAway, HomeGoals, AwayGoals) de la Fecha 1 de Clausura 2026,
    // en el mismo orden que los partidos sembrados por SeedAsync (ordenados por StartsAtUtc).
    private static readonly (string Home, string Away, int HomeGoals, int AwayGoals)[] RankingDemoMatches =
    {
        ("Boca Juniors", "River Plate", 2, 1),
        ("Racing Club", "Independiente", 1, 1),
        ("Estudiantes", "Gimnasia", 0, 2),
    };

    // Pronósticos de demostración por usuario, en el mismo orden que RankingDemoMatches.
    private static readonly Dictionary<string, (int Home, int Away)[]> RankingDemoPredictions = new()
    {
        ["ana.torres@playpredict.local"] = new[] { (2, 1), (0, 0), (1, 2) },
        ["juan.perez@playpredict.local"] = new[] { (1, 0), (1, 1), (0, 2) },
        ["maria.lopez@playpredict.local"] = new[] { (1, 2), (1, 1), (0, 1) },
        ["pedro.gomez@playpredict.local"] = new[] { (2, 1), (2, 0), (1, 0) },
    };

    // Corre en todos los entornos: Registro/Login necesitan la Empresa y los Roles ya existentes.
    public static async Task SeedCoreDataAsync(PlayPredictDbContext db)
    {
        if (!await db.Companies.AnyAsync(c => c.Name == DefaultCompanyName))
        {
            db.Companies.Add(new Company
            {
                Name = DefaultCompanyName,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        foreach (var roleName in new[] { RoleNames.Admin, RoleNames.User })
        {
            if (!await db.Roles.AnyAsync(r => r.Name == roleName))
            {
                db.Roles.Add(new Role { Name = roleName });
            }
        }

        await db.SaveChangesAsync();
    }

    // Corre en todos los entornos: toda Edición debe contar con configuración de puntuación.
    // Crea la configuración inicial (6 / 3 / 0) para cualquier Edición que todavía no la tenga
    // (ediciones creadas antes de este Sprint). Las nuevas Ediciones ya la reciben al crearse
    // desde el endpoint correspondiente.
    public static async Task SeedEditionScoringConfigurationsAsync(PlayPredictDbContext db)
    {
        var editionIdsWithConfig = await db.EditionScoringConfigurations
            .Select(c => c.EditionId)
            .ToListAsync();

        var editionIdsWithoutConfig = await db.Editions
            .Where(e => !editionIdsWithConfig.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        if (editionIdsWithoutConfig.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var editionId in editionIdsWithoutConfig)
        {
            db.EditionScoringConfigurations.Add(new EditionScoringConfiguration
            {
                EditionId = editionId,
                ExactScorePoints = 6,
                CorrectOutcomePoints = 3,
                IncorrectPoints = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        await db.SaveChangesAsync();
    }

    // Sólo en Development: usuario administrador inicial para poder entrar al panel sin registro previo.
    public static async Task SeedAdminUserAsync(PlayPredictDbContext db)
    {
        if (await db.Users.AnyAsync(u => u.Email == AdminEmail))
        {
            return;
        }

        var company = await db.Companies.FirstAsync(c => c.Name == DefaultCompanyName);
        var adminRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.Admin);

        var admin = new User
        {
            CompanyId = company.Id,
            FirstName = "Administrador",
            LastName = "General",
            Email = AdminEmail,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var hasher = new PasswordHasher<User>();
        admin.PasswordHash = hasher.HashPassword(admin, AdminPassword);
        admin.UserRoles.Add(new UserRole { Role = adminRole });

        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }

    public static async Task SeedAsync(PlayPredictDbContext db)
    {
        await SeedCompetitionAsync(
            db,
            LigaProfesionalName,
            "Competencia de demostración generada por el seed de desarrollo.",
            ClausuraEditionName,
            RankingDemoMatches.Select(m => (m.Home, m.Away)).ToArray());

        await SeedCompetitionAsync(
            db,
            CopaLibertadoresName,
            "Competencia de demostración generada por el seed de desarrollo.",
            FaseDeGruposEditionName,
            new[] { ("Equipo G", "Equipo H"), ("Equipo I", "Equipo J"), ("Equipo K", "Equipo L") });
    }

    private static async Task SeedCompetitionAsync(
        PlayPredictDbContext db,
        string competitionName,
        string description,
        string editionName,
        (string Home, string Away)[] matchups)
    {
        if (await db.Competitions.AnyAsync(c => c.Name == competitionName))
        {
            return;
        }

        var now = DateTime.UtcNow;

        var competition = new Competition
        {
            Name = competitionName,
            Description = description,
            Sport = "Fútbol",
            IsActive = true,
            CreatedAtUtc = now
        };

        var edition = new Edition
        {
            Competition = competition,
            Name = editionName,
            StartDateUtc = now,
            Status = EditionStatus.Active,
            CreatedAtUtc = now
        };

        var round = new Round
        {
            Edition = edition,
            Name = RoundName,
            Order = 1,
            StartDateUtc = now
        };

        var matches = matchups.Select((m, i) => new Match
        {
            Round = round,
            ParticipantHome = m.Home,
            ParticipantAway = m.Away,
            StartsAtUtc = now.AddDays(1).AddHours(i * 2),
            Status = MatchStatus.Scheduled,
            CreatedAtUtc = now
        });

        db.Competitions.Add(competition);
        db.Editions.Add(edition);
        db.Rounds.Add(round);
        db.Matches.AddRange(matches);

        await db.SaveChangesAsync();
    }

    // Sólo en Development: usuarios y pronósticos de demostración para poder mostrar el
    // Ranking (Sprint 6) con datos coherentes de punta a punta sobre Fecha 1 de Clausura 2026.
    // Idempotente: cada paso verifica su propia condición antes de escribir, así que correr
    // este método varias veces (reinicios del contenedor) nunca duplica nada.
    public static async Task SeedRankingDemoAsync(PlayPredictDbContext db, PredictionEvaluationService evaluationService)
    {
        var round = await db.Rounds
            .Include(r => r.Matches)
            .FirstOrDefaultAsync(r => r.Name == RoundName && r.Edition.Name == ClausuraEditionName);

        if (round is null || round.Matches.Count < RankingDemoMatches.Length)
        {
            // El seed base (SeedAsync) todavía no corrió; nada que hacer todavía.
            return;
        }

        var matches = round.Matches.OrderBy(m => m.StartsAtUtc).ToList();

        // Nombres reales de equipos, por si la Fecha ya existía con los nombres genéricos
        // de una base de datos sembrada antes de este Sprint.
        for (var i = 0; i < RankingDemoMatches.Length; i++)
        {
            matches[i].ParticipantHome = RankingDemoMatches[i].Home;
            matches[i].ParticipantAway = RankingDemoMatches[i].Away;
        }

        await db.SaveChangesAsync();

        var company = await db.Companies.FirstAsync(c => c.Name == DefaultCompanyName);
        var userRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.User);
        var hasher = new PasswordHasher<User>();

        var users = new Dictionary<string, User>();
        foreach (var (email, firstName, lastName) in RankingDemoUsers)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                user = new User
                {
                    CompanyId = company.Id,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };
                user.PasswordHash = hasher.HashPassword(user, RankingDemoPassword);
                user.UserRoles.Add(new UserRole { Role = userRole });
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            users[email] = user;
        }

        foreach (var (email, scores) in RankingDemoPredictions)
        {
            var user = users[email];
            for (var i = 0; i < matches.Count && i < scores.Length; i++)
            {
                var exists = await db.Predictions.AnyAsync(p => p.UserId == user.Id && p.MatchId == matches[i].Id);
                if (!exists)
                {
                    db.Predictions.Add(new Prediction
                    {
                        MatchId = matches[i].Id,
                        UserId = user.Id,
                        PredictedHomeScore = scores[i].Home,
                        PredictedAwayScore = scores[i].Away,
                        CreatedAtUtc = DateTime.UtcNow,
                        UpdatedAtUtc = DateTime.UtcNow
                    });
                }
            }
        }

        await db.SaveChangesAsync();

        // Resultados oficiales + evaluación automática de los pronósticos (misma vía que
        // usaría un Administrador cargando el Resultado Oficial desde el panel).
        for (var i = 0; i < RankingDemoMatches.Length; i++)
        {
            var match = matches[i];
            if (match.Status != MatchStatus.Finished)
            {
                match.HomeGoals = RankingDemoMatches[i].HomeGoals;
                match.AwayGoals = RankingDemoMatches[i].AwayGoals;
                match.Status = MatchStatus.Finished;
                await evaluationService.PrepareEvaluationsForMatchAsync(db, match);
            }
        }

        await db.SaveChangesAsync();
    }

    // Sólo en Development: Premios de demostración (Sprint 7) sobre Clausura 2026 / Fecha 1.
    // El Premio no calcula nada; solo describe el ámbito y criterio, y el ganador actual se
    // deriva en tiempo real desde el Ranking (Sprint 6). Idempotente por (EditionId, Name).
    private static readonly (
        string Name,
        PrizeType Type,
        string Description,
        string? ReferenceValue,
        string? Sponsor,
        PrizeScopeType Scope,
        PrizeAwardCriteria Criteria,
        int? PositionFrom,
        int? PositionTo,
        PrizeStatus Status)[] PrizeDemoData =
    {
        ("Gran Premio Clausura 2026", PrizeType.Money,
            "Premio para el primer puesto del Ranking General.", "$50.000.000", DefaultCompanyName,
            PrizeScopeType.Edition, PrizeAwardCriteria.Position, 1, 1, PrizeStatus.Published),
        ("Segundo Premio Clausura 2026", PrizeType.Product,
            "Viaje para dos personas.", null, DefaultCompanyName,
            PrizeScopeType.Edition, PrizeAwardCriteria.Position, 2, 2, PrizeStatus.Published),
        ("Premio Fecha 1", PrizeType.Product,
            "Camiseta oficial.", null, DefaultCompanyName,
            PrizeScopeType.Round, PrizeAwardCriteria.RoundWinner, null, null, PrizeStatus.Published),
        ("Rey de los Exactos", PrizeType.Recognition,
            "Reconocimiento para quien consiga más marcadores exactos.", null, null,
            PrizeScopeType.Edition, PrizeAwardCriteria.MostExactScores, null, null, PrizeStatus.Published),
        ("Premio Sorpresa", PrizeType.Other,
            "Premio especial pendiente de publicación.", null, null,
            PrizeScopeType.Edition, PrizeAwardCriteria.Position, 3, 3, PrizeStatus.Draft),
    };

    public static async Task SeedPrizesDemoAsync(PlayPredictDbContext db)
    {
        var edition = await db.Editions.FirstOrDefaultAsync(e => e.Name == ClausuraEditionName);
        if (edition is null)
        {
            // El seed base (SeedAsync) todavía no corrió; nada que hacer todavía.
            return;
        }

        var round = await db.Rounds.FirstOrDefaultAsync(r => r.Name == RoundName && r.EditionId == edition.Id);

        var now = DateTime.UtcNow;
        foreach (var d in PrizeDemoData)
        {
            var exists = await db.Prizes.AnyAsync(p => p.EditionId == edition.Id && p.Name == d.Name);
            if (exists)
            {
                continue;
            }

            db.Prizes.Add(new Prize
            {
                EditionId = edition.Id,
                RoundId = d.Scope == PrizeScopeType.Round ? round?.Id : null,
                Name = d.Name,
                Description = d.Description,
                PrizeType = d.Type,
                ReferenceValue = d.ReferenceValue,
                SponsorName = d.Sponsor,
                ScopeType = d.Scope,
                AwardCriteria = d.Criteria,
                PositionFrom = d.PositionFrom,
                PositionTo = d.PositionTo,
                Status = d.Status,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        await db.SaveChangesAsync();
    }
}
