using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Endpoints;

public static class LeagueEndpoints
{
    private const string InviteCodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // sin caracteres ambiguos

    public static void MapLeagueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/leagues").WithTags("Leagues").RequireAuthorization();

        group.MapPost("/", async (CreateLeagueDto dto, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var (errors, scopeType, competition, edition) = await ValidateCreateAsync(db, dto);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var inviteCode = await GenerateUniqueInviteCodeAsync(db);
            var now = DateTime.UtcNow;

            var league = new League
            {
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                CompetitionId = competition!.Id,
                EditionId = edition!.Id,
                ScopeType = scopeType,
                LeagueType = LeagueType.Private,
                RoundFromId = scopeType == LeagueScopeType.RoundRange ? dto.RoundFromId : null,
                RoundToId = scopeType == LeagueScopeType.RoundRange ? dto.RoundToId : null,
                InviteCode = inviteCode,
                IsActive = true,
                CreatedByUserId = user.Id,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            db.Leagues.Add(league);
            await db.SaveChangesAsync();

            db.LeagueParticipants.Add(new LeagueParticipant
            {
                LeagueId = league.Id,
                UserId = user.Id,
                JoinedAtUtc = now
            });
            await db.SaveChangesAsync();

            return Results.Created($"/api/leagues/{league.Id}", await ToSummaryDtoAsync(db, league, user.Id));
        });

        group.MapGet("/officials", async (ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var participatingLeagueIds = await db.LeagueParticipants
                .Where(lp => lp.UserId == user.Id)
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
                .Where(lp => lp.UserId == user.Id)
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
                .FirstOrDefaultAsync(lp => lp.LeagueId == id && lp.UserId == user.Id);
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
            db.LeagueParticipants.Remove(participant);
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
            var predictionIds = await db.Predictions
                .Where(p => p.LeagueId == id)
                .Select(p => p.Id)
                .ToListAsync();

            if (predictionIds.Count > 0)
            {
                await db.PredictionEvaluations
                    .Where(e => predictionIds.Contains(e.PredictionId))
                    .ExecuteDeleteAsync();
                await db.Predictions
                    .Where(p => p.LeagueId == id)
                    .ExecuteDeleteAsync();
            }

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
                .Where(lp => lp.LeagueId == id)
                .OrderBy(lp => lp.JoinedAtUtc)
                .Join(db.Users, lp => lp.UserId, u => u.Id,
                    (lp, u) => new { u.Id, u.FirstName, u.LastName, lp.JoinedAtUtc })
                .ToListAsync();

            var dtos = participants.Select(p =>
                new LeagueParticipantDto(p.Id, p.FirstName, p.LastName, p.JoinedAtUtc, p.Id == league.CreatedByUserId));

            return Results.Ok(dtos);
        });

        group.MapGet("/{id:int}/matches", async (int id, ClaimsPrincipal principal, PlayPredictDbContext db) =>
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
                .Where(p => p.LeagueId == id && p.UserId == user.Id && matchIds.Contains(p.MatchId))
                .ToListAsync();

            var evaluations = await PredictionEndpoints.GetEvaluationsForPredictionsAsync(db, predictions);

            var result = matches.Select(m =>
            {
                var prediction = predictions.FirstOrDefault(p => p.MatchId == m.Id);
                var evaluation = prediction is null ? null : evaluations.GetValueOrDefault(prediction.Id);
                return PredictionEndpoints.ToMatchWithPredictionDto(m, prediction, evaluation, league.IsActive);
            });

            return Results.Ok(result);
        });
    }

    private static IResult Forbidden(string message) =>
        Results.Json(new { message }, statusCode: StatusCodes.Status403Forbidden);

    private static Task<bool> IsParticipantAsync(PlayPredictDbContext db, int leagueId, int userId) =>
        db.LeagueParticipants.AnyAsync(lp => lp.LeagueId == leagueId && lp.UserId == userId);

    private static async Task<(Dictionary<string, string[]> Errors, LeagueScopeType ScopeType, Competition? Competition, Edition? Edition)> ValidateCreateAsync(
        PlayPredictDbContext db, CreateLeagueDto dto)
    {
        var errors = ValidateNameDescription(dto.Name, dto.Description);

        if (!Enum.TryParse<LeagueScopeType>(dto.ScopeType, ignoreCase: true, out var scopeType))
        {
            errors["scopeType"] = [$"Alcance inválido. Valores permitidos: {string.Join(", ", Enum.GetNames<LeagueScopeType>())}."];
            return (errors, default, null, null);
        }

        var competition = await db.Competitions.FindAsync(dto.CompetitionId);
        if (competition is null)
        {
            errors["competitionId"] = ["La Competencia indicada no existe."];
        }
        else if (!competition.IsActive)
        {
            errors["competitionId"] = ["La Competencia indicada no está habilitada."];
        }

        var edition = await db.Editions.FindAsync(dto.EditionId);
        if (edition is null)
        {
            errors["editionId"] = ["La Edición indicada no existe."];
        }
        else if (edition.CompetitionId != dto.CompetitionId)
        {
            errors["editionId"] = ["La Edición debe pertenecer a la Competencia elegida."];
        }

        if (errors.Count > 0)
        {
            return (errors, scopeType, competition, edition);
        }

        if (scopeType == LeagueScopeType.FullCompetition)
        {
            if (dto.RoundFromId is not null || dto.RoundToId is not null)
            {
                errors["roundFromId"] = ["No se debe indicar rango de Fechas cuando el alcance es toda la Competencia."];
            }

            return (errors, scopeType, competition, edition);
        }

        // RoundRange
        if (dto.RoundFromId is null || dto.RoundToId is null)
        {
            errors["roundFromId"] = ["Debés indicar la Fecha inicial y la Fecha final para este alcance."];
            return (errors, scopeType, competition, edition);
        }

        var roundFrom = await db.Rounds.FindAsync(dto.RoundFromId.Value);
        var roundTo = await db.Rounds.FindAsync(dto.RoundToId.Value);

        if (roundFrom is null || roundTo is null)
        {
            errors["roundFromId"] = ["Alguna de las Fechas indicadas no existe."];
            return (errors, scopeType, competition, edition);
        }

        var roundFromCompetitionId = await db.Editions
            .Where(e => e.Id == roundFrom.EditionId).Select(e => (int?)e.CompetitionId).FirstOrDefaultAsync();
        var roundToCompetitionId = await db.Editions
            .Where(e => e.Id == roundTo.EditionId).Select(e => (int?)e.CompetitionId).FirstOrDefaultAsync();

        if (roundFromCompetitionId != dto.CompetitionId || roundToCompetitionId != dto.CompetitionId)
        {
            errors["roundFromId"] = ["Ambas Fechas deben pertenecer a la Competencia elegida."];
        }
        else if (roundFrom.EditionId != dto.EditionId || roundTo.EditionId != dto.EditionId)
        {
            errors["roundFromId"] = ["Ambas Fechas deben pertenecer a la Edición elegida."];
        }
        else if (roundFrom.Order > roundTo.Order)
        {
            errors["roundFromId"] = ["La Fecha inicial no puede ser posterior a la Fecha final."];
        }

        return (errors, scopeType, competition, edition);
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
        var participantsCount = await db.LeagueParticipants.CountAsync(lp => lp.LeagueId == league.Id);
        var isCreator = league.CreatedByUserId == currentUserId;
        var (roundFromName, roundToName) = await GetRoundRangeNamesAsync(db, league);

        return new LeagueSummaryDto(
            league.Id, league.Name, league.Description, league.CompetitionId, competitionName, league.EditionId, editionName,
            league.ScopeType.ToString(), league.LeagueType.ToString(),
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
        var participantsCount = await db.LeagueParticipants.CountAsync(lp => lp.LeagueId == league.Id);
        var isCreator = league.CreatedByUserId == currentUserId;
        var (roundFromName, roundToName) = await GetRoundRangeNamesAsync(db, league);
        var rounds = await GetRoundsInLeagueScopeAsync(db, league);

        return new LeagueDetailDto(
            league.Id, league.Name, league.Description, league.CompetitionId, competitionName, league.EditionId, editionName,
            league.ScopeType.ToString(), league.LeagueType.ToString(),
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
