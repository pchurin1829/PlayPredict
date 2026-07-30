using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;

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
            new[] { ("Equipo A", "Equipo B"), ("Equipo C", "Equipo D"), ("Equipo E", "Equipo F") });

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
}
