using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Endpoints;

public static class TeamPlayerEndpoints
{
    private const long MaxPhotoBytes = 1_500_000;
    private static readonly Dictionary<string, string> PhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

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

        group.MapPost("/team-players/{id:int}/photo", async (int id, IFormFile file, PlayPredictDbContext db, IWebHostEnvironment environment) =>
        {
            var player = await db.TeamPlayers.FindAsync(id);
            if (player is null) return Results.NotFound();
            if (file.Length == 0 || file.Length > MaxPhotoBytes)
                return Results.BadRequest(new { message = "La foto optimizada debe pesar menos de 1,5 MB." });
            if (!PhotoExtensions.TryGetValue(file.ContentType, out var extension))
                return Results.BadRequest(new { message = "Usá una imagen JPG, PNG o WEBP." });

            await using var source = file.OpenReadStream();
            await using var input = new MemoryStream();
            await source.CopyToAsync(input);
            input.Position = 0;
            var signature = new byte[12];
            var bytesRead = await input.ReadAsync(signature);
            if (!HasValidImageSignature(signature.AsSpan(0, bytesRead), file.ContentType))
                return Results.BadRequest(new { message = "El archivo no contiene una imagen válida." });
            input.Position = 0;

            var relativeDirectory = Path.Combine("uploads", "team-players");
            var directory = Path.Combine(environment.ContentRootPath, "wwwroot", relativeDirectory);
            Directory.CreateDirectory(directory);
            var fileName = $"player-{id}-{Guid.NewGuid():N}{extension}";
            var destination = Path.Combine(directory, fileName);
            await using (var output = File.Create(destination)) await input.CopyToAsync(output);

            DeleteManagedPhoto(player.PhotoUrl, environment.ContentRootPath);
            player.PhotoUrl = $"/api/uploads/team-players/{fileName}";
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(player));
        }).DisableAntiforgery().RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapDelete("/team-players/{id:int}/photo", async (int id, PlayPredictDbContext db, IWebHostEnvironment environment) =>
        {
            var player = await db.TeamPlayers.FindAsync(id);
            if (player is null) return Results.NotFound();
            DeleteManagedPhoto(player.PhotoUrl, environment.ContentRootPath);
            player.PhotoUrl = null;
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(player));
        }).RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapDelete("/team-players/{id:int}", async (int id, PlayPredictDbContext db, IWebHostEnvironment environment) =>
        {
            var player = await db.TeamPlayers.FindAsync(id);
            if (player is null) return Results.NotFound();
            var usedInPredictions = await db.Predictions.AnyAsync(p => p.PreferredPlayerId == id);
            var usedInResults = await db.MatchScorers.AnyAsync(s => s.TeamPlayerId == id);
            if (usedInPredictions || usedInResults)
                return Results.Conflict(new { message = "No se puede eliminar este jugador porque ya está utilizado en pronósticos o resultados." });
            db.TeamPlayers.Remove(player);
            await db.SaveChangesAsync();
            DeleteManagedPhoto(player.PhotoUrl, environment.ContentRootPath);
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

    private static bool HasValidImageSignature(ReadOnlySpan<byte> bytes, string contentType) => contentType switch
    {
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        "image/png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        "image/webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes[8..12].SequenceEqual("WEBP"u8),
        _ => false
    };

    private static void DeleteManagedPhoto(string? photoUrl, string contentRootPath)
    {
        const string prefix = "/api/uploads/team-players/";
        if (string.IsNullOrWhiteSpace(photoUrl) || !photoUrl.StartsWith(prefix, StringComparison.Ordinal)) return;
        var fileName = Path.GetFileName(photoUrl);
        var path = Path.Combine(contentRootPath, "wwwroot", "uploads", "team-players", fileName);
        if (File.Exists(path)) File.Delete(path);
    }
}
