using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Endpoints;

public static class UserTeamPreferredPlayerEndpoints
{
    public static void MapUserTeamPreferredPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users/me/team-preferred-players")
            .WithTags("User team preferred players")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Player));

        group.MapGet("/", async (ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null) return Results.Unauthorized();

            var preferences = await db.UserTeamPreferredPlayers
                .Where(x => x.UserId == user.Id)
                .Include(x => x.Team)
                .Include(x => x.TeamPlayer)
                .OrderBy(x => x.Team.Name)
                .ToListAsync();

            return Results.Ok(preferences.Select(ToDto));
        });

        group.MapGet("/options", async (ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null) return Results.Unauthorized();

            return Results.Ok(await GetOptionsAsync(db, user.Id));
        });

        group.MapPut("/{teamId:int}", async (int teamId, SaveUserTeamPreferredPlayerDto dto,
            ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null) return Results.Unauthorized();

            var (preference, errors) = await UpsertAsync(db, user.Id, teamId, dto.TeamPlayerId, DateTime.UtcNow);
            if (errors.Count > 0) return Results.ValidationProblem(errors);
            await db.SaveChangesAsync();
            await db.Entry(preference!).Reference(x => x.Team).LoadAsync();
            await db.Entry(preference!).Reference(x => x.TeamPlayer).LoadAsync();
            return Results.Ok(ToDto(preference!));
        });

        group.MapDelete("/{teamId:int}", async (int teamId, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null) return Results.Unauthorized();
            var preference = await db.UserTeamPreferredPlayers
                .FirstOrDefaultAsync(x => x.UserId == user.Id && x.TeamId == teamId);
            if (preference is null) return Results.NoContent();
            db.UserTeamPreferredPlayers.Remove(preference);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    internal static async Task<List<PreferredPlayerProfileTeamDto>> GetOptionsAsync(PlayPredictDbContext db, int userId)
    {
        var teamIds = await GetTeamIdsForUserAsync(db, userId);

        var teams = await db.Teams
            .Where(team => teamIds.Contains(team.Id) && team.Active
                && db.TeamPlayers.Any(player => player.TeamId == team.Id && player.Active))
            .OrderBy(team => team.Name)
            .ToListAsync();
        var resolvedTeamIds = teams.Select(team => team.Id).ToList();
        var players = await db.TeamPlayers
            .Where(player => resolvedTeamIds.Contains(player.TeamId) && player.Active)
            .OrderBy(player => player.DisplayName)
            .ToListAsync();
        var preferences = await db.UserTeamPreferredPlayers
            .Where(x => x.UserId == userId && resolvedTeamIds.Contains(x.TeamId))
            .Include(x => x.Team)
            .Include(x => x.TeamPlayer)
            .ToDictionaryAsync(x => x.TeamId);

        return teams.Select(team => new PreferredPlayerProfileTeamDto(
            team.Id,
            team.Name,
            team.ShortName,
            players.Where(player => player.TeamId == team.Id)
                .Select(player => new PreferredPlayerProfilePlayerDto(player.Id, PlayerLabel(player)))
                .ToList(),
            preferences.TryGetValue(team.Id, out var preference) ? ToDto(preference) : null)).ToList();
    }

    /// <summary>
    /// Un equipo entra en la pantalla si el usuario participa (LeagueParticipants activo) de alguna
    /// Liga -oficial o de amigos derivada- cuyo alcance de Fechas incluya un partido de ese equipo.
    /// La preferencia en sí es global (UserId + TeamId): esto solo determina qué equipos mostrar.
    /// </summary>
    internal static async Task<HashSet<int>> GetTeamIdsForUserAsync(PlayPredictDbContext db, int userId)
    {
        var leagueIds = await db.LeagueParticipants
            .Where(lp => lp.UserId == userId && lp.LeftAtUtc == null)
            .Select(lp => lp.LeagueId)
            .ToListAsync();
        var leagues = await db.Leagues
            .Where(league => leagueIds.Contains(league.Id) && league.IsActive)
            .ToListAsync();

        var teamIds = new HashSet<int>();
        foreach (var league in leagues)
        {
            var matchesQuery = db.Matches.Where(m => m.Round.EditionId == league.EditionId);
            if (league.ScopeType == LeagueScopeType.RoundRange)
            {
                var bounds = await db.Rounds
                    .Where(r => r.Id == league.RoundFromId || r.Id == league.RoundToId)
                    .ToListAsync();
                var from = bounds.FirstOrDefault(r => r.Id == league.RoundFromId);
                var to = bounds.FirstOrDefault(r => r.Id == league.RoundToId);
                if (from is null || to is null) continue;
                matchesQuery = matchesQuery.Where(m => m.Round.Order >= from.Order && m.Round.Order <= to.Order);
            }

            var matchTeams = await matchesQuery
                .Select(m => new { m.HomeTeamId, m.AwayTeamId })
                .ToListAsync();
            foreach (var m in matchTeams)
            {
                teamIds.Add(m.HomeTeamId);
                teamIds.Add(m.AwayTeamId);
            }
        }

        return teamIds;
    }

    internal static async Task<(UserTeamPreferredPlayer? Preference, Dictionary<string, string[]> Errors)> UpsertAsync(
        PlayPredictDbContext db, int userId, int teamId, int teamPlayerId, DateTime now)
    {
        var errors = new Dictionary<string, string[]>();
        if (!await db.Teams.AnyAsync(team => team.Id == teamId))
            errors["teamId"] = ["El equipo indicado no existe."];

        var player = await db.TeamPlayers.FirstOrDefaultAsync(candidate => candidate.Id == teamPlayerId);
        if (player is null)
            errors["teamPlayerId"] = ["El jugador indicado no existe."];
        else if (player.TeamId != teamId)
            errors["teamPlayerId"] = ["El jugador debe pertenecer al equipo configurado."];
        else if (!player.Active)
            errors["teamPlayerId"] = ["El jugador indicado no está activo."];

        if (errors.Count > 0) return (null, errors);

        var preference = await db.UserTeamPreferredPlayers
            .FirstOrDefaultAsync(x => x.UserId == userId && x.TeamId == teamId);
        if (preference is null)
        {
            preference = new UserTeamPreferredPlayer
            {
                UserId = userId,
                TeamId = teamId,
                CreatedAtUtc = now
            };
            db.UserTeamPreferredPlayers.Add(preference);
        }

        preference.TeamPlayerId = teamPlayerId;
        preference.UpdatedAtUtc = now;
        return (preference, errors);
    }

    private static UserTeamPreferredPlayerDto ToDto(UserTeamPreferredPlayer preference) => new(
        preference.Id,
        preference.TeamId,
        preference.Team.Name,
        preference.TeamPlayerId,
        PlayerLabel(preference.TeamPlayer),
        preference.TeamPlayer.Active && preference.TeamPlayer.TeamId == preference.TeamId,
        preference.CreatedAtUtc,
        preference.UpdatedAtUtc);

    private static string PlayerLabel(TeamPlayer player)
    {
        var realName = $"{player.FirstName} {player.LastName}".Trim();
        return string.Equals(player.DisplayName, realName, StringComparison.OrdinalIgnoreCase)
            ? realName
            : $"{realName} · “{player.DisplayName}”";
    }
}
