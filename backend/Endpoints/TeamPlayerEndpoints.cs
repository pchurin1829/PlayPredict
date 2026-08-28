using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class TeamPlayerEndpoints
{
    public static void MapTeamPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Team Players").RequireAuthorization();
        group.MapGet("/teams/{teamId:int}/players", async (int teamId, PlayPredictDbContext db) =>
            Results.Ok(await db.TeamPlayers.Where(x => x.TeamId == teamId).OrderByDescending(x => x.Active).ThenBy(x => x.DisplayName).Select(x => ToDto(x)).ToListAsync()));

        group.MapGet("/team-players/{id:int}", async (int id, PlayPredictDbContext db) =>
        {
            var player = await db.TeamPlayers.FindAsync(id);
            return player is null ? Results.NotFound() : Results.Ok(ToDto(player));
        }).RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapPost("/teams/{teamId:int}/players", async (int teamId, SaveTeamPlayerDto dto, PlayPredictDbContext db) =>
        {
            if (!await db.Teams.AnyAsync(x => x.Id == teamId)) return Results.NotFound();
            var errors = Validate(dto);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            var player = new TeamPlayer { TeamId = teamId };
            Apply(player, dto);
            db.TeamPlayers.Add(player);
            await db.SaveChangesAsync();
            return Results.Created($"/api/team-players/{player.Id}", ToDto(player));
        }).RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapPut("/team-players/{id:int}", async (int id, SaveTeamPlayerDto dto, PlayPredictDbContext db) =>
        {
            var player = await db.TeamPlayers.FindAsync(id);
            if (player is null) return Results.NotFound();
            var errors = Validate(dto);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            Apply(player, dto);
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(player));
        }).RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapPost("/team-players/{id:int}/photo", async (int id, IFormFile file, PlayPredictDbContext db, IWebHostEnvironment environment, IConfiguration configuration) =>
        {
            var player = await db.TeamPlayers.FindAsync(id);
            if (player is null) return Results.NotFound();
            var (url, uploadError) = await ManagedImageStorage.SaveAsync(file, "team-players", $"player-{id}", configuration, environment);
            if (uploadError is not null) return Results.BadRequest(new { message = uploadError });
            ManagedImageStorage.Delete(player.PhotoUrl, "team-players", configuration, environment);
            player.PhotoUrl = url;
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(player));
        }).DisableAntiforgery().RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapDelete("/team-players/{id:int}/photo", async (int id, PlayPredictDbContext db, IWebHostEnvironment environment, IConfiguration configuration) =>
        {
            var player = await db.TeamPlayers.FindAsync(id);
            if (player is null) return Results.NotFound();
            ManagedImageStorage.Delete(player.PhotoUrl, "team-players", configuration, environment);
            player.PhotoUrl = null;
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(player));
        }).RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapDelete("/team-players/{id:int}", async (int id, PlayPredictDbContext db, IWebHostEnvironment environment, IConfiguration configuration) =>
        {
            var player = await db.TeamPlayers.FindAsync(id);
            if (player is null) return Results.NotFound();
            var usedInPredictions = await db.Predictions.AnyAsync(p => p.PreferredPlayerId == id);
            var usedInResults = await db.MatchScorers.AnyAsync(s => s.TeamPlayerId == id);
            var usedInPreferences = await db.UserTeamPreferredPlayers.AnyAsync(preference => preference.TeamPlayerId == id);
            if (usedInPredictions || usedInResults || usedInPreferences)
                return Results.Conflict(new { message = "No se puede eliminar este jugador porque ya está utilizado en pronósticos, resultados o preferencias de usuarios." });
            db.TeamPlayers.Remove(player);
            await db.SaveChangesAsync();
            ManagedImageStorage.Delete(player.PhotoUrl, "team-players", configuration, environment);
            return Results.NoContent();
        }).RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));
    }

    private static Dictionary<string, string[]> Validate(SaveTeamPlayerDto dto)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(dto.FirstName)) errors["firstName"] = ["El nombre es obligatorio."];
        if (string.IsNullOrWhiteSpace(dto.LastName)) errors["lastName"] = ["El apellido es obligatorio."];
        if (dto.ShirtNumber is < 0 or > 99) errors["shirtNumber"] = ["El número debe estar entre 0 y 99."];
        return errors;
    }

    private static void Apply(TeamPlayer player, SaveTeamPlayerDto dto)
    {
        player.FirstName = dto.FirstName.Trim();
        player.LastName = dto.LastName.Trim();
        player.DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? $"{player.FirstName} {player.LastName}" : dto.DisplayName.Trim();
        player.ShirtNumber = dto.ShirtNumber;
        player.Position = string.IsNullOrWhiteSpace(dto.Position) ? null : dto.Position.Trim();
        player.Active = dto.Active;
        player.PhotoUrl = string.IsNullOrWhiteSpace(dto.PhotoUrl) ? null : dto.PhotoUrl.Trim();
    }

    private static TeamPlayerDto ToDto(TeamPlayer x) => new(x.Id, x.TeamId, x.FirstName, x.LastName, x.DisplayName, x.ShirtNumber, x.Position, x.Active, x.PhotoUrl);

}
