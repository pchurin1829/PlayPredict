using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class PredictionEndpoints
{
    public static void MapPredictionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/predictions").WithTags("Predictions").RequireAuthorization();

        // A partir del Sprint 8.5 el Pronóstico pertenece a una Liga: por eso este listado
        // exige indicar desde qué Liga se está consultando ("Mi pronóstico" ya no es un
        // concepto único por partido, puede haber uno distinto por Liga). No se elige
        // ninguna Liga por defecto ni en nombre del usuario: sin `leagueId` explícito, 400.
        group.MapGet("/rounds/{roundId:int}", async (int roundId, int? leagueId, ClaimsPrincipal principal, PlayPredictDbContext db, LeagueScoringService scoring) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            if (leagueId is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["leagueId"] = ["Debés indicar desde qué Liga estás consultando los pronósticos."]
                });
            }

            var league = await db.Leagues.FindAsync(leagueId.Value);
            if (league is null)
            {
                return Results.NotFound(new { message = "La Liga indicada no existe." });
            }

            var isParticipant = await db.LeagueParticipants
                .AnyAsync(lp => lp.LeagueId == leagueId.Value && lp.UserId == user.Id && lp.LeftAtUtc == null);
            if (!isParticipant)
            {
                return Results.Json(new { message = "No pertenecés a esta Liga." }, statusCode: StatusCodes.Status403Forbidden);
            }

            var round = await db.Rounds.FindAsync(roundId);
            if (round is null)
            {
                return Results.NotFound();
            }

            if (!await IsRoundWithinLeagueScopeAsync(db, league, round))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["roundId"] = ["La Fecha indicada está fuera del alcance de esta Liga."]
                });
            }

            var matches = await db.Matches
                .Where(m => m.RoundId == roundId)
                .OrderBy(m => m.StartsAtUtc)
                .ToListAsync();

            var matchIds = matches.Select(m => m.Id).ToList();
            var predictions = await db.Predictions
                .Include(p => p.PreferredPlayer)
                .Where(p => p.UserId == user.Id && matchIds.Contains(p.MatchId))
                .ToListAsync();

            var evaluations = await GetEvaluationsForPredictionsAsync(db, predictions, leagueId.Value);
            var teamIds = matches.SelectMany(m => new[] { m.HomeTeamId, m.AwayTeamId }).Distinct().ToList();
            var preferredConfig = await scoring.GetEffectiveAsync(db, leagueId.Value);
            var allowedPositions = preferredConfig?.PreferredPlayerPositions ?? PlayerPosition.None;
            var allActivePlayers = await db.TeamPlayers.Where(p => teamIds.Contains(p.TeamId) && p.Active).OrderBy(p => p.DisplayName).ToListAsync();
            // El selector largo solo debe ofrecer posiciones que puntúan como Jugador Preferido en
            // esta Liga; la sugerencia rápida, en cambio, refleja la preferencia global del usuario
            // (cualquier posición), así que se arma aparte a partir del plantel sin filtrar.
            var players = allActivePlayers.Where(p => PlayerPositionCatalog.TryParse(p.Position, out var position) && allowedPositions.HasFlag(position)).ToList();
            var teamPreferences = await db.UserTeamPreferredPlayers
                .Where(preference => preference.UserId == user.Id && teamIds.Contains(preference.TeamId))
                .ToDictionaryAsync(preference => preference.TeamId, preference => preference.TeamPlayerId);
            var preferredEnabled = preferredConfig?.PreferredPlayerEnabled ?? false;
            var membershipPeriods = await db.LeagueParticipants
                .Where(p => p.LeagueId == league.Id && p.UserId == user.Id).ToListAsync();

            var result = matches.Select(m =>
            {
                var prediction = predictions.FirstOrDefault(p => p.MatchId == m.Id);
                var evaluation = prediction is null ? null : evaluations.GetValueOrDefault(prediction.Id);
                var eligible = prediction is not null && IsEligible(prediction, league, m, membershipPeriods);
                return ToMatchWithPredictionDto(m, prediction, evaluation, true, players, preferredEnabled, eligible, teamPreferences, allActivePlayers);
            });

            return Results.Ok(result);
        });

        // Devuelve todos los Pronósticos del usuario, en todas sus Ligas (sin ambigüedad:
        // es un listado, no "mi pronóstico para este partido").
        group.MapGet("/me", async (ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var predictions = await db.Predictions
                .Include(p => p.PreferredPlayer)
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.UpdatedAtUtc)
                .ToListAsync();

            var dtos = predictions.Select(p => ToDto(p));

            return Results.Ok(dtos);
        });

        group.MapPost("/", async (CreatePredictionDto dto, ClaimsPrincipal principal, PlayPredictDbContext db, LeagueScoringService scoring) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var errors = ValidateScores(dto.PredictedHomeScore, dto.PredictedAwayScore);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var validationError = await ValidatePredictionContextAsync(db, dto.LeagueId, dto.MatchId, user.Id);
            if (validationError is not null)
            {
                return validationError;
            }

            var preferredError = await ValidatePreferredPlayerAsync(db, dto.MatchId, dto.PreferredPlayerId);
            if (preferredError is not null) return preferredError;

            var existing = await db.Predictions
                .Include(p => p.PreferredPlayer)
                .FirstOrDefaultAsync(p => p.UserId == user.Id && p.MatchId == dto.MatchId);
            if (existing is not null)
            {
                ApplyValues(existing, dto.PredictedHomeScore, dto.PredictedAwayScore, dto.PreferredPlayerId, dto.UpdatePreferredPlayer);
                await db.SaveChangesAsync();
                return Results.Ok(ToDto(existing));
            }

            var now = DateTime.UtcNow;
            var prediction = new Prediction
            {
                MatchId = dto.MatchId,
                UserId = user.Id,
                PredictedHomeScore = dto.PredictedHomeScore,
                PredictedAwayScore = dto.PredictedAwayScore,
                PreferredPlayerId = dto.PreferredPlayerId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            db.Predictions.Add(prediction);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { message = "Ya existe un pronóstico para este partido en esta Liga." });
            }

            return Results.Created($"/api/predictions/{prediction.Id}", ToDto(prediction));
        });

        group.MapPut("/{id:int}", async (int id, UpdatePredictionDto dto, ClaimsPrincipal principal, PlayPredictDbContext db, LeagueScoringService scoring) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var errors = ValidateScores(dto.PredictedHomeScore, dto.PredictedAwayScore);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var prediction = await db.Predictions.FindAsync(id);
            if (prediction is null)
            {
                return Results.NotFound();
            }

            if (prediction.UserId != user.Id)
            {
                return Results.Json(new { message = "No podés modificar el pronóstico de otro usuario." }, statusCode: StatusCodes.Status403Forbidden);
            }

            // La Liga y el Partido de un pronóstico ya existente no cambian al editarlo
            // (solo el marcador); se revalida igual porque ambos pueden haber cambiado de
            // estado desde que se creó (Liga desactivada, partido que ya no admite cambios).
            var validationError = await ValidatePredictionContextAsync(db, dto.LeagueId, prediction.MatchId, user.Id);
            if (validationError is not null)
            {
                return validationError;
            }

            ApplyValues(prediction, dto.PredictedHomeScore, dto.PredictedAwayScore, dto.PreferredPlayerId, dto.UpdatePreferredPlayer);
            if (dto.UpdatePreferredPlayer)
            {
                var preferredError = await ValidatePreferredPlayerAsync(db, prediction.MatchId, dto.PreferredPlayerId);
                if (preferredError is not null) return preferredError;
            }

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(prediction));
        });

        group.MapDelete("/{id:int}", async (int id, int? leagueId, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var prediction = await db.Predictions.Include(p => p.PreferredPlayer).FirstOrDefaultAsync(p => p.Id == id);
            if (prediction is null)
            {
                return Results.NotFound();
            }

            if (prediction.UserId != user.Id)
            {
                return Results.Json(new { message = "No podés eliminar el pronóstico de otro usuario." }, statusCode: StatusCodes.Status403Forbidden);
            }

            // Eliminar respeta las mismas reglas temporales que crear/editar: el usuario
            // debe seguir participando, la Liga debe estar activa y el partido abierto.
            if (leagueId is null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["leagueId"] = ["Debés indicar la Liga desde la que eliminás el pronóstico global."] });
            var validationError = await ValidatePredictionContextAsync(db, leagueId.Value, prediction.MatchId, user.Id);
            if (validationError is not null)
            {
                return validationError;
            }

            await db.PredictionEvaluations.Where(e => e.PredictionId == prediction.Id).ExecuteDeleteAsync();
            db.Predictions.Remove(prediction);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }

    // Reglas del Sprint 8.5: valida, en orden, todo lo necesario antes de guardar un
    // Pronóstico. Nunca deja que una clave foránea inválida llegue a SaveChangesAsync
    // (eso produciría un 500): todo caso inválido conocido se traduce acá a 404/403/400/409.
    private static async Task<IResult?> ValidatePredictionContextAsync(
        PlayPredictDbContext db, int leagueId, int matchId, int userId)
    {
        var league = await db.Leagues.FindAsync(leagueId);
        if (league is null)
        {
            return Results.NotFound(new { message = "La Liga indicada no existe." });
        }

        var isParticipant = await db.LeagueParticipants
            .AnyAsync(lp => lp.LeagueId == leagueId && lp.UserId == userId && lp.LeftAtUtc == null);
        if (!isParticipant)
        {
            return Results.Json(new { message = "No pertenecés a esta Liga." }, statusCode: StatusCodes.Status403Forbidden);
        }

        if (!league.IsActive)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["leagueId"] = ["Esta Liga no está activa."]
            });
        }

        var match = await db.Matches.FindAsync(matchId);
        if (match is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["matchId"] = ["El partido indicado no existe."]
            });
        }

        var matchRound = await db.Rounds.FindAsync(match.RoundId);
        var matchCompetitionId = matchRound is null
            ? (int?)null
            : await db.Editions.Where(e => e.Id == matchRound.EditionId).Select(e => (int?)e.CompetitionId).FirstOrDefaultAsync();

        if (matchRound is null || matchCompetitionId is null || matchCompetitionId.Value != league.CompetitionId)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["matchId"] = ["El partido no pertenece a la Competencia de esta Liga."]
            });
        }

        if (!await IsMatchWithinLeagueScopeAsync(db, league, matchRound))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["matchId"] = ["El partido está fuera del alcance de Fechas de esta Liga."]
            });
        }

        if (!CanCreateOrEditPrediction(match))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = ["Este partido no admite pronósticos: ya comenzó, no está Programado, o su horario de inicio ya pasó."]
            });
        }

        return null;
    }

    private static async Task<bool> IsMatchWithinLeagueScopeAsync(PlayPredictDbContext db, League league, Round matchRound)
    {
        return await IsRoundWithinLeagueScopeAsync(db, league, matchRound);
    }

    private static async Task<bool> IsRoundWithinLeagueScopeAsync(PlayPredictDbContext db, League league, Round round)
    {
        if (round.EditionId != league.EditionId) return false;
        if (league.ScopeType == LeagueScopeType.FullCompetition) return true;

        // RoundRange: RoundFromId/RoundToId son obligatorios y ya fueron validados como
        // coherentes (misma Edición, desde <= hasta) al crear la Liga.
        var roundFrom = await db.Rounds.FindAsync(league.RoundFromId!.Value);
        var roundTo = await db.Rounds.FindAsync(league.RoundToId!.Value);

        if (roundFrom is null || roundTo is null || roundFrom.EditionId != league.EditionId || roundTo.EditionId != league.EditionId)
        {
            return false;
        }

        return round.Order >= roundFrom.Order && round.Order <= roundTo.Order;
    }

    // Regla definitiva: un pronóstico solo puede crearse o modificarse si el partido
    // está Programado Y su horario de inicio todavía no llegó. Cualquier otro caso
    // (Programado con horario ya pasado, En juego, Suspendido, Finalizado, Cancelado)
    // queda cerrado. El backend es la autoridad final de esta regla.
    private static bool CanCreateOrEditPrediction(Match match) =>
        match.Status == MatchStatus.Scheduled && DateTime.UtcNow < match.StartsAtUtc;

    private static async Task<IResult?> ValidatePreferredPlayerAsync(PlayPredictDbContext db, int matchId, int? playerId)
    {
        if (playerId is null) return null;
        var match = await db.Matches.FindAsync(matchId);
        var player = await db.TeamPlayers.FindAsync(playerId.Value);
        if (match is null || player is null || !player.Active || (player.TeamId != match.HomeTeamId && player.TeamId != match.AwayTeamId))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["preferredPlayerId"] = ["El Jugador Preferido debe pertenecer a uno de los equipos del partido."] });
        return null;
    }

    private static Dictionary<string, string[]> ValidateScores(int homeScore, int awayScore)
    {
        var errors = new Dictionary<string, string[]>();

        if (homeScore < 0)
        {
            errors["predictedHomeScore"] = ["El resultado del local no puede ser negativo."];
        }

        if (awayScore < 0)
        {
            errors["predictedAwayScore"] = ["El resultado del visitante no puede ser negativo."];
        }

        return errors;
    }

    internal static void ApplyValues(Prediction prediction, int homeScore, int awayScore, int? preferredPlayerId, bool updatePreferredPlayer)
    {
        prediction.PredictedHomeScore = homeScore;
        prediction.PredictedAwayScore = awayScore;
        if (updatePreferredPlayer) prediction.PreferredPlayerId = preferredPlayerId;
        prediction.UpdatedAtUtc = DateTime.UtcNow;
    }

    internal static async Task<Dictionary<int, PredictionEvaluation>> GetEvaluationsForPredictionsAsync(
        PlayPredictDbContext db, List<Prediction> predictions, int leagueId)
    {
        if (predictions.Count == 0)
        {
            return new Dictionary<int, PredictionEvaluation>();
        }

        var predictionIds = predictions.Select(p => p.Id).ToList();
        var evaluations = await db.PredictionEvaluations
            .Where(e => e.LeagueId == leagueId && predictionIds.Contains(e.PredictionId))
            .ToListAsync();

        return evaluations.ToDictionary(e => e.PredictionId);
    }

    private static PredictionDto ToDto(Prediction p, PredictionEvaluation? evaluation = null) =>
        new(p.Id, p.MatchId, p.UserId, p.PredictedHomeScore, p.PredictedAwayScore,
            p.PreferredPlayerId, p.PreferredPlayer is null ? null : PlayerLabel(p.PreferredPlayer), p.CreatedAtUtc, p.UpdatedAtUtc,
            evaluation?.Points,
            evaluation?.ResultPoints,
            evaluation?.PreferredPlayerPoints,
            evaluation?.EvaluationType.ToString(),
            evaluation is null ? null : PredictionEvaluationService.GetLabel(evaluation.EvaluationType),
            evaluation?.OfficialHomeScore,
            evaluation?.OfficialAwayScore);

    internal static MatchWithPredictionDto ToMatchWithPredictionDto(Match m, Prediction? prediction, PredictionEvaluation? evaluation,
        bool leagueIsActive = true, IReadOnlyList<TeamPlayer>? players = null, bool preferredEnabled = false, bool predictionEligible = false,
        IReadOnlyDictionary<int, int>? teamPreferences = null, IReadOnlyList<TeamPlayer>? allActivePlayers = null) =>
        new(m.Id, m.RoundId, m.HomeTeamId, m.AwayTeamId, m.ParticipantHome, m.ParticipantAway, m.StartsAtUtc, m.Status.ToString(),
            m.HomeGoals, m.AwayGoals,
            (players ?? []).Where(p => p.TeamId == m.HomeTeamId).Select(ToAvailablePlayer).ToList(),
            (players ?? []).Where(p => p.TeamId == m.AwayTeamId).Select(ToAvailablePlayer).ToList(),
            // La sugerencia rápida usa la preferencia global (cualquier posición), no el plantel ya
            // filtrado por las posiciones que puntúan en esta Liga.
            BuildQuickPreferredPlayers(m, allActivePlayers ?? players ?? [], teamPreferences),
            preferredEnabled, predictionEligible, prediction is null ? null : ToDto(prediction, evaluation), leagueIsActive && CanCreateOrEditPrediction(m));

    internal static IReadOnlyList<AvailablePlayerDto> BuildQuickPreferredPlayers(
        Match match, IReadOnlyList<TeamPlayer> availablePlayers, IReadOnlyDictionary<int, int>? teamPreferences)
    {
        if (teamPreferences is null || teamPreferences.Count == 0) return [];
        var preferredIds = new[] { match.HomeTeamId, match.AwayTeamId }
            .Where(teamPreferences.ContainsKey)
            .Select(teamId => teamPreferences[teamId])
            .ToHashSet();
        return availablePlayers
            .Where(player => preferredIds.Contains(player.Id)
                && player.Active
                && (player.TeamId == match.HomeTeamId || player.TeamId == match.AwayTeamId))
            .Select(ToAvailablePlayer)
            .ToList();
    }

    internal static bool IsEligible(Prediction prediction, League league, Match match, IReadOnlyList<LeagueParticipant> periods) =>
        prediction.CreatedAtUtc < match.StartsAtUtc
        && league.CreatedAtUtc < match.StartsAtUtc
        && periods.Any(period => period.JoinedAtUtc < match.StartsAtUtc
            && (period.LeftAtUtc == null || period.LeftAtUtc > match.StartsAtUtc));

    private static AvailablePlayerDto ToAvailablePlayer(TeamPlayer player)
    {
        var realName = $"{player.FirstName} {player.LastName}".Trim();
        var nickname = string.Equals(player.DisplayName, realName, StringComparison.OrdinalIgnoreCase) ? null : player.DisplayName;
        return new AvailablePlayerDto(player.Id, player.TeamId, player.FirstName, player.LastName, nickname, player.ShirtNumber, player.Position!);
    }

    private static string PlayerLabel(TeamPlayer player)
    {
        var realName = $"{player.FirstName} {player.LastName}".Trim();
        return string.Equals(player.DisplayName, realName, StringComparison.OrdinalIgnoreCase)
            ? realName
            : $"{realName} · “{player.DisplayName}”";
    }
}
