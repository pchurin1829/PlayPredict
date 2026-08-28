using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class LeagueEndpoints
{
    private const string InviteCodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // sin caracteres ambiguos

    public static void MapLeagueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leagues").WithTags("Leagues").RequireAuthorization();

        group.MapPost("/", async (CreateLeagueDto dto, ClaimsPrincipal principal, PlayPredictDbContext db, LeagueScoringService scoring) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var (league, errors) = await CreatePrivateLeagueAsync(db, scoring, dto, user.Id);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }
            return Results.Created($"/api/leagues/{league!.Id}", await ToSummaryDtoAsync(db, league, user.Id));
        });

        group.MapGet("/officials", async (ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var participatingLeagueIds = await db.LeagueParticipants
                .Where(lp => lp.UserId == user.Id && lp.LeftAtUtc == null)
                .Select(lp => lp.LeagueId)
                .ToListAsync();

            var leagues = await db.Leagues
                .Where(l => l.LeagueType == LeagueType.Official && l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();

            var dtos = new List<LeagueSummaryDto>();
            foreach (var league in leagues)
            {
                var dto = await ToSummaryDtoAsync(db, league, user.Id);
                dtos.Add(dto with { IsParticipant = participatingLeagueIds.Contains(league.Id) });
            }

            return Results.Ok(dtos);
        });

        group.MapGet("/mine", async (ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var leagueIds = await db.LeagueParticipants
                .Where(lp => lp.UserId == user.Id && lp.LeftAtUtc == null)
                .Select(lp => lp.LeagueId)
                .ToListAsync();

            var leagues = await db.Leagues
                .Where(l => leagueIds.Contains(l.Id))
                .OrderByDescending(l => l.CreatedAtUtc)
                .ToListAsync();

            var dtos = new List<LeagueSummaryDto>();
            foreach (var league in leagues)
            {
                dtos.Add(await ToSummaryDtoAsync(db, league, user.Id));
            }

            return Results.Ok(dtos);
        });

        group.MapGet("/{id:int}", async (int id, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var league = await db.Leagues.FindAsync(id);
            if (league is null)
            {
                return Results.NotFound();
            }

            if (!await IsParticipantAsync(db, id, user.Id))
            {
                return Forbidden("No pertenecés a esta Liga.");
            }

            return Results.Ok(await ToDetailDtoAsync(db, league, user.Id));
        });

        group.MapPut("/{id:int}", async (int id, UpdateLeagueDto dto, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var league = await db.Leagues.FindAsync(id);
            if (league is null)
            {
                return Results.NotFound();
            }

            if (league.CreatedByUserId != user.Id)
            {
                return Forbidden("Solo el creador de la Liga puede editarla.");
            }

            var errors = ValidateNameDescription(dto.Name, dto.Description);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            league.Name = dto.Name.Trim();
            league.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            league.IsActive = dto.IsActive;
            league.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(await ToSummaryDtoAsync(db, league, user.Id));
        });

        group.MapPost("/{id:int}/join", async (int id, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var league = await db.Leagues.FindAsync(id);
            if (league is null)
            {
                return Results.NotFound();
            }

            if (!league.IsActive)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["league"] = ["Esta Liga no está activa."]
                });
            }

            var alreadyParticipant = await IsParticipantAsync(db, league.Id, user.Id);
            if (!alreadyParticipant)
            {
                db.LeagueParticipants.Add(new LeagueParticipant
                {
                    LeagueId = league.Id,
                    UserId = user.Id,
                    JoinedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            return Results.Ok(await ToSummaryDtoAsync(db, league, user.Id));
        });

        group.MapDelete("/{id:int}/leave", async (int id, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var league = await db.Leagues.FindAsync(id);
            if (league is null)
            {
                return Results.NotFound();
            }

            var participant = await db.LeagueParticipants
                .FirstOrDefaultAsync(lp => lp.LeagueId == id && lp.UserId == user.Id && lp.LeftAtUtc == null);
            if (participant is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["league"] = ["No participás en esta Liga."]
                });
            }

            // El creador de una Liga privada no puede abandonarla.
            if (league.CreatedByUserId == user.Id && league.LeagueType == LeagueType.Private)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["league"] = ["El creador de una Liga de Amigos no puede abandonarla."]
                });
            }

            // Política temporal para la demo: abandonar una Liga elimina solamente la
            // participación. Los pronósticos se conservan para que, si el usuario vuelve
            // a participar, recupere los valores cargados. La política definitiva sobre
            // pronósticos al abandonar una Liga queda pendiente de decisión de producto.
            participant.LeftAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(new { message = "Dejaste la Liga correctamente." });
        });

        group.MapDelete("/{id:int}", async (int id, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var league = await db.Leagues.FindAsync(id);
            if (league is null)
            {
                return Results.NotFound();
            }

            if (league.LeagueType != LeagueType.Private || league.CreatedByUserId != user.Id)
            {
                return Forbidden("Solo el creador puede eliminar su Liga de Amigos.");
            }

            // Eliminar una Liga es distinto de suspenderla: la confirmación explícita del
            // owner elimina sus datos dependientes en el orden requerido por las FK Restrict.
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.PredictionEvaluations.Where(e => e.LeagueId == id).ExecuteDeleteAsync();

            await db.LeagueParticipants
                .Where(lp => lp.LeagueId == id)
                .ExecuteDeleteAsync();
            db.Leagues.Remove(league);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Results.Ok(new { message = "Liga eliminada correctamente." });
        });

        group.MapPost("/join", async (JoinLeagueDto dto, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(dto.InviteCode))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["inviteCode"] = ["El código de invitación es obligatorio."]
                });
            }

            var normalizedCode = dto.InviteCode.Trim().ToUpperInvariant();
            var league = await db.Leagues.FirstOrDefaultAsync(l => l.InviteCode == normalizedCode);
            if (league is null)
            {
                return Results.NotFound(new { message = "Código de invitación inválido." });
            }

            if (!league.IsActive)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["inviteCode"] = ["Esta Liga no está activa."]
                });
            }

            var alreadyParticipant = await IsParticipantAsync(db, league.Id, user.Id);
            if (!alreadyParticipant)
            {
                db.LeagueParticipants.Add(new LeagueParticipant
                {
                    LeagueId = league.Id,
                    UserId = user.Id,
                    JoinedAtUtc = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }

            // Idempotente: unirse dos veces con el mismo código nunca duplica el Participante
            // ni es un error — devuelve la misma Liga en ambos casos.
            return Results.Ok(await ToSummaryDtoAsync(db, league, user.Id));
        });

        group.MapGet("/{id:int}/participants", async (int id, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var league = await db.Leagues.FindAsync(id);
            if (league is null)
            {
                return Results.NotFound();
            }

            if (!await IsParticipantAsync(db, id, user.Id))
            {
                return Forbidden("No pertenecés a esta Liga.");
            }

            // Datos públicos mínimos: nunca email, hash ni roles internos.
            var participants = await db.LeagueParticipants
                .Where(lp => lp.LeagueId == id && lp.LeftAtUtc == null)
                .OrderBy(lp => lp.JoinedAtUtc)
                .Join(db.Users, lp => lp.UserId, u => u.Id,
                    (lp, u) => new { u.Id, u.FirstName, u.LastName, lp.JoinedAtUtc })
                .ToListAsync();

            var dtos = participants.Select(p =>
                new LeagueParticipantDto(p.Id, p.FirstName, p.LastName, p.JoinedAtUtc, p.Id == league.CreatedByUserId));

            return Results.Ok(dtos);
        });

        group.MapGet("/{id:int}/matches", async (int id, ClaimsPrincipal principal, PlayPredictDbContext db, LeagueScoringService scoring) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var league = await db.Leagues.FindAsync(id);
            if (league is null)
            {
                return Results.NotFound();
            }

            if (!await IsParticipantAsync(db, id, user.Id))
            {
                return Forbidden("No pertenecés a esta Liga.");
            }

            // Ruta dedicada: solo devuelve partidos de la Competencia/alcance de ESTA Liga,
            // nunca un listado global que mezcle partidos de otras Ligas.
            var matches = await GetMatchesInLeagueScopeAsync(db, league);
            var matchIds = matches.Select(m => m.Id).ToList();

            var predictions = await db.Predictions
                .Include(p => p.PreferredPlayer)
                .Where(p => p.UserId == user.Id && matchIds.Contains(p.MatchId))
                .ToListAsync();

            var evaluations = await PredictionEndpoints.GetEvaluationsForPredictionsAsync(db, predictions, id);
            var teamIds = matches.SelectMany(m => new[] { m.HomeTeamId, m.AwayTeamId }).Distinct().ToList();
            var effective = await scoring.GetEffectiveAsync(db, league.Id);
            var allowed = effective?.PreferredPlayerPositions ?? PlayerPosition.None;
            var allActivePlayers = await db.TeamPlayers.Where(p => teamIds.Contains(p.TeamId) && p.Active).OrderBy(p => p.DisplayName).ToListAsync();
            var players = allActivePlayers.Where(p => PlayerPositionCatalog.TryParse(p.Position, out var position) && allowed.HasFlag(position)).ToList();
            var teamPreferences = await db.UserTeamPreferredPlayers
                .Where(preference => preference.UserId == user.Id && teamIds.Contains(preference.TeamId))
                .ToDictionaryAsync(preference => preference.TeamId, preference => preference.TeamPlayerId);
            var preferredEnabled = effective?.PreferredPlayerEnabled ?? false;
            var membershipPeriods = await db.LeagueParticipants
                .Where(p => p.LeagueId == id && p.UserId == user.Id).ToListAsync();

            var result = matches.Select(m =>
            {
                var prediction = predictions.FirstOrDefault(p => p.MatchId == m.Id);
                var evaluation = prediction is null ? null : evaluations.GetValueOrDefault(prediction.Id);
                var eligible = prediction is not null && PredictionEndpoints.IsEligible(prediction, league, m, membershipPeriods);
                return PredictionEndpoints.ToMatchWithPredictionDto(m, prediction, evaluation, league.IsActive, players, preferredEnabled, eligible, teamPreferences, allActivePlayers);
            });

            return Results.Ok(result);
        });
    }

    private static IResult Forbidden(string message) =>
        Results.Json(new { message }, statusCode: StatusCodes.Status403Forbidden);

    private static Task<bool> IsParticipantAsync(PlayPredictDbContext db, int leagueId, int userId) =>
        db.LeagueParticipants.AnyAsync(lp => lp.LeagueId == leagueId && lp.UserId == userId && lp.LeftAtUtc == null);

    internal static async Task<(League? League, Dictionary<string, string[]> Errors)> CreatePrivateLeagueAsync(
        PlayPredictDbContext db, LeagueScoringService scoring, CreateLeagueDto dto, int userId)
    {
        var errors = ValidateNameDescription(dto.Name, dto.Description);
        var sourceLeague = await db.Leagues.FirstOrDefaultAsync(l => l.Id == dto.OfficialLeagueId);
        if (sourceLeague is null) errors["officialLeagueId"] = ["La Competencia Oficial indicada no existe."];
        else if (sourceLeague.LeagueType != LeagueType.Official) errors["officialLeagueId"] = ["La fuente debe ser una Competencia Oficial."];
        else if (!sourceLeague.IsActive) errors["officialLeagueId"] = ["La Competencia Oficial indicada no está activa."];
        if (!Enum.TryParse<LeagueScopeType>(dto.ScopeType, true, out var requestedScope))
            errors["scopeType"] = ["La opción de alcance indicada no es válida."];
        if (errors.Count > 0) return (null, errors);

        int? roundFromId;
        int? roundToId;
        LeagueScopeType privateScope;
        if (requestedScope == LeagueScopeType.FullCompetition)
        {
            privateScope = sourceLeague!.ScopeType;
            roundFromId = sourceLeague.RoundFromId;
            roundToId = sourceLeague.RoundToId;
        }
        else
        {
            privateScope = LeagueScopeType.RoundRange;
            roundFromId = dto.RoundFromId;
            roundToId = dto.RoundToId;
            await ValidateRequestedRangeAsync(db, sourceLeague!, roundFromId, roundToId, errors);
            if (errors.Count > 0) return (null, errors);
        }

        var effective = await scoring.GetEffectiveAsync(db, sourceLeague!.Id)
            ?? throw new InvalidOperationException("No se pudo resolver la configuración de puntuación de la Competencia Oficial.");
        var now = DateTime.UtcNow;
        var league = new League
        {
            Name = dto.Name.Trim(), Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
            CompetitionId = sourceLeague.CompetitionId, EditionId = sourceLeague.EditionId, ScopeType = privateScope,
            RoundFromId = roundFromId, RoundToId = roundToId, LeagueType = LeagueType.Private,
            SourceLeagueId = sourceLeague.Id, InviteCode = await GenerateUniqueInviteCodeAsync(db), IsActive = true,
            CreatedByUserId = userId, CreatedAtUtc = now, UpdatedAtUtc = now, UseGeneralScoring = false,
            ExactScorePoints = effective.ExactScorePoints, CorrectOutcomePoints = effective.CorrectOutcomePoints,
            IncorrectPoints = effective.IncorrectPoints, PreferredPlayerEnabled = effective.PreferredPlayerEnabled,
            PreferredPlayerPointsPerGoal = effective.PreferredPlayerPointsPerGoal, PreferredPlayerPositions = effective.PreferredPlayerPositions
        };
        db.Leagues.Add(league);
        await db.SaveChangesAsync();
        db.LeagueParticipants.Add(new LeagueParticipant { LeagueId = league.Id, UserId = userId, JoinedAtUtc = now });
        await db.SaveChangesAsync();
        return (league, errors);
    }

    private static async Task ValidateRequestedRangeAsync(PlayPredictDbContext db, League sourceLeague,
        int? roundFromId, int? roundToId, Dictionary<string, string[]> errors)
    {
        if (roundFromId is null || roundToId is null)
        {
            errors["roundFromId"] = ["Debés indicar la Fecha inicial y la Fecha final."];
            return;
        }

        var requestedRounds = await db.Rounds
            .Where(r => r.Id == roundFromId || r.Id == roundToId)
            .ToListAsync();
        var from = requestedRounds.FirstOrDefault(r => r.Id == roundFromId);
        var to = requestedRounds.FirstOrDefault(r => r.Id == roundToId);
        if (from is null || to is null || from.EditionId != sourceLeague.EditionId || to.EditionId != sourceLeague.EditionId)
        {
            errors["roundFromId"] = ["Ambas Fechas deben pertenecer a la Edición de la Competencia Oficial."];
            return;
        }
        if (from.Order > to.Order)
        {
            errors["roundFromId"] = ["La Fecha inicial no puede ser posterior a la Fecha final."];
            return;
        }

        if (sourceLeague.ScopeType != LeagueScopeType.RoundRange) return;
        var sourceLimits = await db.Rounds
            .Where(r => r.Id == sourceLeague.RoundFromId || r.Id == sourceLeague.RoundToId)
            .ToListAsync();
        var sourceFrom = sourceLimits.FirstOrDefault(r => r.Id == sourceLeague.RoundFromId);
        var sourceTo = sourceLimits.FirstOrDefault(r => r.Id == sourceLeague.RoundToId);
        if (sourceFrom is null || sourceTo is null || from.Order < sourceFrom.Order || to.Order > sourceTo.Order)
            errors["roundFromId"] = ["El rango debe estar contenido dentro del alcance de la Competencia Oficial."];
    }

    private static Dictionary<string, string[]> ValidateNameDescription(string? name, string? description)
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

        if (description is not null && description.Trim().Length > 1000)
        {
            errors["description"] = ["La descripción no puede superar los 1000 caracteres."];
        }

        return errors;
    }

    private static async Task<string> GenerateUniqueInviteCodeAsync(PlayPredictDbContext db)
    {
        var random = Random.Shared;

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = new string(Enumerable.Range(0, 8).Select(_ => InviteCodeChars[random.Next(InviteCodeChars.Length)]).ToArray());
            if (!await db.Leagues.AnyAsync(l => l.InviteCode == code))
            {
                return code;
            }
        }

        throw new InvalidOperationException("No se pudo generar un código de invitación único.");
    }

    // Fechas (Rounds) comprendidas en el alcance de la Liga.
    private static async Task<List<Round>> GetRoundsInLeagueScopeAsync(PlayPredictDbContext db, League league)
    {
        if (league.ScopeType == LeagueScopeType.FullCompetition)
        {
            return await db.Rounds
                .Where(r => r.EditionId == league.EditionId)
                .OrderBy(r => r.Order)
                .ToListAsync();
        }

        var roundFrom = await db.Rounds.FindAsync(league.RoundFromId!.Value);
        var roundTo = await db.Rounds.FindAsync(league.RoundToId!.Value);

        return await db.Rounds
            .Where(r => r.EditionId == roundFrom!.EditionId && r.Order >= roundFrom.Order && r.Order <= roundTo!.Order)
            .OrderBy(r => r.Order)
            .ToListAsync();
    }

    // Partidos comprendidos en el alcance de la Liga (misma regla que GetRoundsInLeagueScopeAsync,
    // a nivel Partido en vez de Fecha).
    private static async Task<List<Match>> GetMatchesInLeagueScopeAsync(PlayPredictDbContext db, League league)
    {
        if (league.ScopeType == LeagueScopeType.FullCompetition)
        {
            return await db.Matches
                .Where(m => m.Round.EditionId == league.EditionId)
                .OrderBy(m => m.StartsAtUtc)
                .ToListAsync();
        }

        var roundFrom = await db.Rounds.FindAsync(league.RoundFromId!.Value);
        var roundTo = await db.Rounds.FindAsync(league.RoundToId!.Value);

        return await db.Matches
            .Where(m => m.Round.EditionId == roundFrom!.EditionId
                && m.Round.Order >= roundFrom.Order && m.Round.Order <= roundTo!.Order)
            .OrderBy(m => m.StartsAtUtc)
            .ToListAsync();
    }

    private static async Task<LeagueSummaryDto> ToSummaryDtoAsync(PlayPredictDbContext db, League league, int currentUserId)
    {
        var competitionName = await db.Competitions
            .Where(c => c.Id == league.CompetitionId).Select(c => c.Name).FirstAsync();
        var editionName = await db.Editions
            .Where(e => e.Id == league.EditionId).Select(e => e.Name).FirstAsync();
        var participantsCount = await db.LeagueParticipants.CountAsync(lp => lp.LeagueId == league.Id && lp.LeftAtUtc == null);
        var sourceLeague = league.SourceLeagueId is null ? null : await db.Leagues
            .Where(l => l.Id == league.SourceLeagueId)
            .Select(l => new { l.Name, l.ScopeType, l.RoundFromId, l.RoundToId })
            .FirstOrDefaultAsync();
        var usesFullSourceScope = sourceLeague is not null && league.ScopeType == sourceLeague.ScopeType
            && league.RoundFromId == sourceLeague.RoundFromId && league.RoundToId == sourceLeague.RoundToId;
        var isCreator = league.CreatedByUserId == currentUserId;
        var (roundFromName, roundToName) = await GetRoundRangeNamesAsync(db, league);

        return new LeagueSummaryDto(
            league.Id, league.Name, league.Description, league.CompetitionId, competitionName, league.EditionId, editionName,
            league.ScopeType.ToString(), league.LeagueType.ToString(), league.SourceLeagueId, sourceLeague?.Name, usesFullSourceScope,
            league.RoundFromId, league.RoundToId, roundFromName, roundToName,
            league.CreatedByUserId, isCreator, participantsCount, league.IsActive,
            isCreator ? league.InviteCode : null, true);
    }

    private static async Task<LeagueDetailDto> ToDetailDtoAsync(PlayPredictDbContext db, League league, int currentUserId)
    {
        var competitionName = await db.Competitions
            .Where(c => c.Id == league.CompetitionId).Select(c => c.Name).FirstAsync();
        var editionName = await db.Editions
            .Where(e => e.Id == league.EditionId).Select(e => e.Name).FirstAsync();
        var creator = await db.Users.FindAsync(league.CreatedByUserId);
        var participantsCount = await db.LeagueParticipants.CountAsync(lp => lp.LeagueId == league.Id && lp.LeftAtUtc == null);
        var sourceLeague = league.SourceLeagueId is null ? null : await db.Leagues
            .Where(l => l.Id == league.SourceLeagueId)
            .Select(l => new { l.Name, l.ScopeType, l.RoundFromId, l.RoundToId })
            .FirstOrDefaultAsync();
        var usesFullSourceScope = sourceLeague is not null && league.ScopeType == sourceLeague.ScopeType
            && league.RoundFromId == sourceLeague.RoundFromId && league.RoundToId == sourceLeague.RoundToId;
        var isCreator = league.CreatedByUserId == currentUserId;
        var (roundFromName, roundToName) = await GetRoundRangeNamesAsync(db, league);
        var rounds = await GetRoundsInLeagueScopeAsync(db, league);

        return new LeagueDetailDto(
            league.Id, league.Name, league.Description, league.CompetitionId, competitionName, league.EditionId, editionName,
            league.ScopeType.ToString(), league.LeagueType.ToString(), league.SourceLeagueId, sourceLeague?.Name, usesFullSourceScope,
            league.RoundFromId, league.RoundToId, roundFromName, roundToName,
            league.CreatedByUserId, $"{creator!.FirstName} {creator.LastName}", isCreator, participantsCount, league.IsActive,
            isCreator ? league.InviteCode : null,
            rounds.Select(r => new RoundSummaryDto(r.Id, r.Name, r.Order)).ToList());
    }

    private static async Task<(string? RoundFromName, string? RoundToName)> GetRoundRangeNamesAsync(PlayPredictDbContext db, League league)
    {
        if (league.ScopeType != LeagueScopeType.RoundRange)
        {
            return (null, null);
        }

        var roundFromName = await db.Rounds.Where(r => r.Id == league.RoundFromId).Select(r => r.Name).FirstOrDefaultAsync();
        var roundToName = await db.Rounds.Where(r => r.Id == league.RoundToId).Select(r => r.Name).FirstOrDefaultAsync();
        return (roundFromName, roundToName);
    }
}
