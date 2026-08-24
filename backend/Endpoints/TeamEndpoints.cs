using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams").RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin)).WithTags("Teams");
        group.MapGet("/", async (PlayPredictDbContext db) => Results.Ok(await db.Teams.OrderBy(t => t.Name).Select(t => ToDto(t)).ToListAsync()));
        group.MapGet("/{id:int}", async (int id, PlayPredictDbContext db) =>
        {
            var team = await db.Teams.FindAsync(id);
            return team is null ? Results.NotFound() : Results.Ok(ToDto(team));
        });
        group.MapPost("/", async (SaveTeamDto dto, PlayPredictDbContext db) => await Save(null, dto, db));
        group.MapPut("/{id:int}", async (int id, SaveTeamDto dto, PlayPredictDbContext db) => await Save(id, dto, db));
        group.MapPost("/{id:int}/logo", async (int id, IFormFile file, PlayPredictDbContext db, IWebHostEnvironment environment, IConfiguration configuration) =>
        {
            var team = await db.Teams.FindAsync(id);
            if (team is null) return Results.NotFound();
            var (url, uploadError) = await ManagedImageStorage.SaveAsync(file, "teams", $"team-{id}", configuration, environment);
            if (uploadError is not null) return Results.BadRequest(new { message = uploadError });
            ManagedImageStorage.Delete(team.LogoUrl, "teams", configuration, environment);
            team.LogoUrl = url;
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(team));
        }).DisableAntiforgery();
        group.MapDelete("/{id:int}/logo", async (int id, PlayPredictDbContext db, IWebHostEnvironment environment, IConfiguration configuration) =>
        {
            var team = await db.Teams.FindAsync(id);
            if (team is null) return Results.NotFound();
            ManagedImageStorage.Delete(team.LogoUrl, "teams", configuration, environment);
            team.LogoUrl = null;
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(team));
        });
        group.MapDelete("/{id:int}", async (int id, PlayPredictDbContext db, IWebHostEnvironment environment, IConfiguration configuration) =>
        {
            var team = await db.Teams.FindAsync(id);
            if (team is null) return Results.NotFound();

            var usedInMatches = await db.Matches.AnyAsync(m => m.HomeTeamId == id || m.AwayTeamId == id);
            var hasPlayers = await db.TeamPlayers.AnyAsync(p => p.TeamId == id);
            if (usedInMatches || hasPlayers)
            {
                return Results.Conflict(new
                {
                    message = "No se puede eliminar este equipo porque está siendo utilizado en partidos, fixture u otros datos de la competencia."
                });
            }

            db.Teams.Remove(team);
            try { await db.SaveChangesAsync(); }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { message = "No se puede eliminar este equipo porque tiene referencias relacionadas que deben revisarse." });
            }
            ManagedImageStorage.Delete(team.LogoUrl, "teams", configuration, environment);
            return Results.NoContent();
        });
    }

    private static async Task<IResult> Save(int? id, SaveTeamDto dto, PlayPredictDbContext db)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.ShortName))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Nombre y nombre corto son obligatorios."] });
        if (await db.Teams.AnyAsync(t => t.Name.ToLower() == dto.Name.Trim().ToLower() && t.Id != id))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["Ya existe un equipo con ese nombre."] });

        Team team;
        if (id.HasValue)
        {
            team = await db.Teams.FindAsync(id.Value) ?? null!;
            if (team is null) return Results.NotFound();
        }
        else
        {
            team = new Team();
            db.Teams.Add(team);
        }
        team.Name = dto.Name.Trim();
        team.ShortName = dto.ShortName.Trim();
        team.LogoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl.Trim();
        team.Sport = string.IsNullOrWhiteSpace(dto.Sport) ? "Fútbol" : dto.Sport.Trim();
        team.Active = dto.Active;
        await db.SaveChangesAsync();
        return id.HasValue ? Results.Ok(ToDto(team)) : Results.Created($"/api/teams/{team.Id}", ToDto(team));
    }

    private static TeamDto ToDto(Team t) => new(t.Id, t.Name, t.ShortName, t.LogoUrl, t.Sport, t.Active);
}
