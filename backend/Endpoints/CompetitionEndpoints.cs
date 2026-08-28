using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Domain.Constants;

namespace PlayPredict.Api.Endpoints;

public static class CompetitionEndpoints
{
    public static void MapCompetitionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/competitions").WithTags("Competitions");

        group.MapGet("", async (PlayPredictDbContext db) =>
        {
            var competitions = await db.Competitions
                .OrderBy(c => c.Name)
                .Select(c => ToDto(c))
                .ToListAsync();

            return Results.Ok(competitions);
        });

        group.MapGet("/{id:int}", async (int id, PlayPredictDbContext db) =>
        {
            var competition = await db.Competitions.FindAsync(id);
            return competition is null
                ? Results.NotFound()
                : Results.Ok(ToDto(competition));
        });

        group.MapPost("", async (CreateCompetitionDto dto, PlayPredictDbContext db) =>
        {
            var errors = ValidateCompetition(dto.Name, dto.Sport);

            // Si no se indica Experience, se asocia a "PlayPredict Demo" para no romper el
            // alta existente de Competencias (Sprints 1 a 7 no envían este campo).
            var experienceId = dto.ExperienceId;
            if (experienceId is null)
            {
                experienceId = await db.Experiences
                    .Where(e => e.Name == DemoExperienceName)
                    .Select(e => (int?)e.Id)
                    .FirstOrDefaultAsync();

                if (experienceId is null)
                {
                    errors["experienceId"] = ["No se encontró la Experience de demostración; indique una Experience explícita."];
                }
            }
            else if (!await db.Experiences.AnyAsync(e => e.Id == experienceId))
            {
                errors["experienceId"] = ["La Experience indicada no existe."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var competition = new Competition
            {
                ExperienceId = experienceId!.Value,
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                Sport = dto.Sport.Trim(),
                IsActive = dto.IsActive,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Competitions.Add(competition);
            await db.SaveChangesAsync();

            return Results.Created($"/api/competitions/{competition.Id}", ToDto(competition));
        });

        group.MapPut("/{id:int}", async (int id, UpdateCompetitionDto dto, PlayPredictDbContext db) =>
        {
            var errors = ValidateCompetition(dto.Name, dto.Sport);

            var competition = await db.Competitions.FindAsync(id);
            if (competition is null)
            {
                return Results.NotFound();
            }

            // Si no se indica Experience, se mantiene la actual (los formularios existentes
            // de edición no envían este campo y no deben resetearla a la Experience demo).
            if (dto.ExperienceId is not null && !await db.Experiences.AnyAsync(e => e.Id == dto.ExperienceId))
            {
                errors["experienceId"] = ["La Experience indicada no existe."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            competition.Name = dto.Name.Trim();
            competition.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            competition.Sport = dto.Sport.Trim();
            competition.IsActive = dto.IsActive;
            if (dto.ExperienceId is not null)
            {
                competition.ExperienceId = dto.ExperienceId.Value;
            }

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(competition));
        });

        group.MapGet("/{id:int}/dependencies", async (int id, PlayPredictDbContext db) =>
        {
            if (!await db.Competitions.AnyAsync(c => c.Id == id)) return Results.NotFound();
            return Results.Ok(await GetDependenciesAsync(id, db));
        }).RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapDelete("/{id:int}", async (int id, PlayPredictDbContext db) =>
        {
            var competition = await db.Competitions.FindAsync(id);
            if (competition is null) return Results.NotFound();
            var dependencies = await GetDependenciesAsync(id, db);
            if (!dependencies.CanDelete)
                return Results.Conflict(new { message = "No se puede eliminar esta competencia de referencia porque tiene datos relacionados.", dependencies });

            db.Competitions.Remove(competition);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static async Task<CompetitionDependenciesDto> GetDependenciesAsync(int competitionId, PlayPredictDbContext db)
    {
        var editionIds = db.Editions.Where(e => e.CompetitionId == competitionId).Select(e => e.Id);
        var roundIds = db.Rounds.Where(r => editionIds.Contains(r.EditionId)).Select(r => r.Id);
        var matchIds = db.Matches.Where(m => roundIds.Contains(m.RoundId)).Select(m => m.Id);
        var leagueIds = db.Leagues.Where(l => l.CompetitionId == competitionId).Select(l => l.Id);
        var predictionIds = db.Predictions.Where(p => matchIds.Contains(p.MatchId)).Select(p => p.Id);
        return new CompetitionDependenciesDto(
            await editionIds.CountAsync(), await roundIds.CountAsync(), await matchIds.CountAsync(),
            await leagueIds.CountAsync(), await db.LeagueParticipants.CountAsync(p => leagueIds.Contains(p.LeagueId)),
            await predictionIds.CountAsync(), await db.PredictionEvaluations.CountAsync(e => predictionIds.Contains(e.PredictionId)),
            await db.MatchScorers.CountAsync(s => matchIds.Contains(s.MatchId)),
            await db.Prizes.CountAsync(p => editionIds.Contains(p.EditionId)),
            await db.EditionScoringConfigurations.CountAsync(c => editionIds.Contains(c.EditionId)));
    }

    private const string DemoExperienceName = "PlayPredict Demo";

    private static Dictionary<string, string[]> ValidateCompetition(string name, string sport)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["El nombre es obligatorio."];
        }
        else if (name.Trim().Length > 150)
        {
            errors["name"] = ["El nombre no puede superar los 150 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(sport))
        {
            errors["sport"] = ["El deporte/categoría es obligatorio."];
        }
        else if (sport.Trim().Length > 100)
        {
            errors["sport"] = ["El deporte/categoría no puede superar los 100 caracteres."];
        }

        return errors;
    }

    internal static CompetitionDto ToDto(Competition c) =>
        new(c.Id, c.ExperienceId, c.Name, c.Description, c.Sport, c.IsActive, c.CreatedAtUtc);
}

public record CompetitionDependenciesDto(int Editions, int Rounds, int Matches, int Leagues,
    int Participants, int Predictions, int Evaluations, int MatchScorers, int Prizes, int ScoringConfigurations)
{
    public bool CanDelete => Editions == 0 && Rounds == 0 && Matches == 0 && Leagues == 0
        && Participants == 0 && Predictions == 0 && Evaluations == 0 && MatchScorers == 0
        && Prizes == 0 && ScoringConfigurations == 0;
}
