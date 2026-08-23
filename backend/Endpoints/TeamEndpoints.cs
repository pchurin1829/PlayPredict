using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;

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
