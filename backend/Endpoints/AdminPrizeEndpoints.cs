using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class AdminPrizeEndpoints
{
    public static void MapAdminPrizeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/prizes")
            .WithTags("Admin Prizes")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapGet("", async (PlayPredictDbContext db, RankingService rankingService, PrizeWinnerService winnerService) =>
        {
            var prizes = await db.Prizes
                .Include(p => p.Edition)
                .Include(p => p.Round)
                .OrderBy(p => p.EditionId).ThenBy(p => p.Id)
                .ToListAsync();

            var dtos = new List<PrizeDto>(prizes.Count);
            foreach (var prize in prizes)
            {
                dtos.Add(await PrizeMapper.ToDtoAsync(prize, db, rankingService, winnerService));
            }

            return Results.Ok(dtos);
        });

        group.MapGet("/{id:int}", async (int id, PlayPredictDbContext db, RankingService rankingService, PrizeWinnerService winnerService) =>
        {
            var prize = await db.Prizes
                .Include(p => p.Edition)
                .Include(p => p.Round)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prize is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(await PrizeMapper.ToDtoAsync(prize, db, rankingService, winnerService));
        });

        group.MapPost("", async (CreatePrizeDto dto, PlayPredictDbContext db, RankingService rankingService, PrizeWinnerService winnerService) =>
        {
            var (errors, parsed) = await ValidateAsync(db, dto.EditionId, dto.RoundId, dto.Name, dto.Description,
                dto.PrizeType, dto.ReferenceValue, dto.SponsorName, dto.ImageUrl, dto.ScopeType, dto.AwardCriteria,
                dto.PositionFrom, dto.PositionTo);

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var now = DateTime.UtcNow;
            var prize = new Prize
            {
                EditionId = dto.EditionId,
                RoundId = parsed.RoundId,
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                PrizeType = parsed.PrizeType,
                ReferenceValue = string.IsNullOrWhiteSpace(dto.ReferenceValue) ? null : dto.ReferenceValue.Trim(),
                SponsorName = string.IsNullOrWhiteSpace(dto.SponsorName) ? null : dto.SponsorName.Trim(),
                ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim(),
                ScopeType = parsed.ScopeType,
                AwardCriteria = parsed.AwardCriteria,
                PositionFrom = parsed.PositionFrom,
                PositionTo = parsed.PositionTo,
                Status = PrizeStatus.Draft,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            db.Prizes.Add(prize);
            await db.SaveChangesAsync();

            await db.Entry(prize).Reference(p => p.Edition).LoadAsync();
            if (prize.RoundId is not null)
            {
                await db.Entry(prize).Reference(p => p.Round).LoadAsync();
            }

            return Results.Created($"/api/admin/prizes/{prize.Id}", await PrizeMapper.ToDtoAsync(prize, db, rankingService, winnerService));
        });

        group.MapPut("/{id:int}", async (int id, UpdatePrizeDto dto, PlayPredictDbContext db, RankingService rankingService, PrizeWinnerService winnerService) =>
        {
            var prize = await db.Prizes.Include(p => p.Edition).Include(p => p.Round).FirstOrDefaultAsync(p => p.Id == id);
            if (prize is null)
            {
                return Results.NotFound();
            }

            if (prize.Status == PrizeStatus.Cancelled)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["No se puede modificar un Premio Cancelado."]
                });
            }

            var (errors, parsed) = await ValidateAsync(db, prize.EditionId, dto.RoundId, dto.Name, dto.Description,
                dto.PrizeType, dto.ReferenceValue, dto.SponsorName, dto.ImageUrl, dto.ScopeType, dto.AwardCriteria,
                dto.PositionFrom, dto.PositionTo);

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            prize.RoundId = parsed.RoundId;
            prize.Name = dto.Name.Trim();
            prize.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            prize.PrizeType = parsed.PrizeType;
            prize.ReferenceValue = string.IsNullOrWhiteSpace(dto.ReferenceValue) ? null : dto.ReferenceValue.Trim();
            prize.SponsorName = string.IsNullOrWhiteSpace(dto.SponsorName) ? null : dto.SponsorName.Trim();
            prize.ImageUrl = string.IsNullOrWhiteSpace(dto.ImageUrl) ? null : dto.ImageUrl.Trim();
            prize.ScopeType = parsed.ScopeType;
            prize.AwardCriteria = parsed.AwardCriteria;
            prize.PositionFrom = parsed.PositionFrom;
            prize.PositionTo = parsed.PositionTo;
            prize.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();

            await db.Entry(prize).Reference(p => p.Round).LoadAsync();

            return Results.Ok(await PrizeMapper.ToDtoAsync(prize, db, rankingService, winnerService));
        });

        group.MapPut("/{id:int}/publish", async (int id, PlayPredictDbContext db, RankingService rankingService, PrizeWinnerService winnerService) =>
        {
            var prize = await db.Prizes.Include(p => p.Edition).Include(p => p.Round).FirstOrDefaultAsync(p => p.Id == id);
            if (prize is null)
            {
                return Results.NotFound();
            }

            if (prize.Status != PrizeStatus.Draft)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["Solo se puede publicar un Premio en Borrador."]
                });
            }

            // Re-valida coherencia antes de publicar (Edición/Fecha podrían haber cambiado de estado).
            var (errors, _) = await ValidateAsync(db, prize.EditionId, prize.RoundId, prize.Name, prize.Description,
                prize.PrizeType.ToString(), prize.ReferenceValue, prize.SponsorName, prize.ImageUrl,
                prize.ScopeType.ToString(), prize.AwardCriteria.ToString(), prize.PositionFrom, prize.PositionTo);

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            prize.Status = PrizeStatus.Published;
            prize.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(await PrizeMapper.ToDtoAsync(prize, db, rankingService, winnerService));
        });

        group.MapPut("/{id:int}/close", async (int id, PlayPredictDbContext db, RankingService rankingService, PrizeWinnerService winnerService) =>
        {
            var prize = await db.Prizes.Include(p => p.Edition).Include(p => p.Round).FirstOrDefaultAsync(p => p.Id == id);
            if (prize is null)
            {
                return Results.NotFound();
            }

            if (prize.Status != PrizeStatus.Published)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["Solo se puede cerrar un Premio Publicado."]
                });
            }

            prize.Status = PrizeStatus.Closed;
            prize.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(await PrizeMapper.ToDtoAsync(prize, db, rankingService, winnerService));
        });

        group.MapPut("/{id:int}/cancel", async (int id, PlayPredictDbContext db, RankingService rankingService, PrizeWinnerService winnerService) =>
        {
            var prize = await db.Prizes.Include(p => p.Edition).Include(p => p.Round).FirstOrDefaultAsync(p => p.Id == id);
            if (prize is null)
            {
                return Results.NotFound();
            }

            if (prize.Status == PrizeStatus.Closed)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["No se puede cancelar un Premio ya Cerrado."]
                });
            }

            if (prize.Status == PrizeStatus.Cancelled)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["El Premio ya está Cancelado."]
                });
            }

            prize.Status = PrizeStatus.Cancelled;
            prize.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(await PrizeMapper.ToDtoAsync(prize, db, rankingService, winnerService));
        });
    }

    private record ParsedPrize(
        int? RoundId, PrizeType PrizeType, PrizeScopeType ScopeType, PrizeAwardCriteria AwardCriteria,
        int? PositionFrom, int? PositionTo);

    private static async Task<(Dictionary<string, string[]> Errors, ParsedPrize Parsed)> ValidateAsync(
        PlayPredictDbContext db, int editionId, int? roundId, string name, string? description,
        string prizeTypeInput, string? referenceValue, string? sponsorName, string? imageUrl,
        string scopeTypeInput, string awardCriteriaInput, int? positionFrom, int? positionTo)
    {
        var errors = new Dictionary<string, string[]>();

        var editionExists = await db.Editions.AnyAsync(e => e.Id == editionId);
        if (!editionExists)
        {
            errors["editionId"] = ["La Edición indicada no existe."];
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["El nombre es obligatorio."];
        }
        else if (name.Trim().Length > 150)
        {
            errors["name"] = ["El nombre no puede superar los 150 caracteres."];
        }

        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > 1000)
        {
            errors["description"] = ["La descripción no puede superar los 1000 caracteres."];
        }

        if (!string.IsNullOrWhiteSpace(referenceValue) && referenceValue.Trim().Length > 150)
        {
            errors["referenceValue"] = ["El valor de referencia no puede superar los 150 caracteres."];
        }

        if (!string.IsNullOrWhiteSpace(sponsorName) && sponsorName.Trim().Length > 150)
        {
            errors["sponsorName"] = ["El sponsor no puede superar los 150 caracteres."];
        }

        if (!string.IsNullOrWhiteSpace(imageUrl) && imageUrl.Trim().Length > 500)
        {
            errors["imageUrl"] = ["La URL de imagen no puede superar los 500 caracteres."];
        }

        if (!Enum.TryParse<PrizeType>(prizeTypeInput, ignoreCase: true, out var prizeType))
        {
            errors["prizeType"] = [$"Tipo inválido. Valores permitidos: {string.Join(", ", Enum.GetNames<PrizeType>())}."];
        }

        if (!Enum.TryParse<PrizeScopeType>(scopeTypeInput, ignoreCase: true, out var scopeType))
        {
            errors["scopeType"] = [$"Ámbito inválido. Valores permitidos: {string.Join(", ", Enum.GetNames<PrizeScopeType>())}."];
        }

        if (!Enum.TryParse<PrizeAwardCriteria>(awardCriteriaInput, ignoreCase: true, out var awardCriteria))
        {
            errors["awardCriteria"] = [$"Criterio inválido. Valores permitidos: {string.Join(", ", Enum.GetNames<PrizeAwardCriteria>())}."];
        }

        // Coherencia RoundId / ScopeType: obligatorio solo cuando el ámbito es Fecha; debe
        // pertenecer a la misma Edición del Premio. Se omite si el ámbito en sí ya es inválido.
        int? resolvedRoundId = null;
        if (!errors.ContainsKey("scopeType"))
        {
            if (scopeType == PrizeScopeType.Round)
            {
                if (roundId is null)
                {
                    errors["roundId"] = ["La Fecha es obligatoria cuando el ámbito es Fecha."];
                }
                else
                {
                    var round = await db.Rounds.FirstOrDefaultAsync(r => r.Id == roundId.Value);
                    if (round is null)
                    {
                        errors["roundId"] = ["La Fecha indicada no existe."];
                    }
                    else if (round.EditionId != editionId)
                    {
                        errors["roundId"] = ["La Fecha indicada no pertenece a la Edición del Premio."];
                    }
                    else
                    {
                        resolvedRoundId = roundId;
                    }
                }
            }
            else if (roundId is not null)
            {
                errors["roundId"] = ["No debe indicarse una Fecha cuando el ámbito no es Fecha."];
            }

            // Criterio compatible con el ámbito: "Ganador de la Fecha" únicamente tiene sentido
            // dentro del ámbito Fecha.
            if (!errors.ContainsKey("awardCriteria") && awardCriteria == PrizeAwardCriteria.RoundWinner
                && scopeType != PrizeScopeType.Round)
            {
                errors["awardCriteria"] = ["El criterio \"Ganador de la Fecha\" solo es válido con ámbito Fecha."];
            }
        }

        // Posiciones: solo se usan (y son obligatorias) cuando el criterio es Posición.
        // Se omite si el criterio en sí ya es inválido.
        int? resolvedFrom = null;
        int? resolvedTo = null;
        if (!errors.ContainsKey("awardCriteria") && awardCriteria == PrizeAwardCriteria.Position)
        {
            if (positionFrom is null || positionFrom < 1)
            {
                errors["positionFrom"] = ["La posición desde debe ser mayor o igual a 1."];
            }

            if (positionTo is null)
            {
                errors["positionTo"] = ["La posición hasta es obligatoria para el criterio Posición."];
            }
            else if (positionFrom is not null && positionTo < positionFrom)
            {
                errors["positionTo"] = ["La posición hasta debe ser mayor o igual a la posición desde."];
            }

            if (!errors.ContainsKey("positionFrom") && !errors.ContainsKey("positionTo"))
            {
                resolvedFrom = positionFrom;
                resolvedTo = positionTo;
            }
        }
        else if (!errors.ContainsKey("awardCriteria") && (positionFrom is not null || positionTo is not null))
        {
            errors["positionFrom"] = ["Las posiciones solo se usan con el criterio Posición."];
        }

        var parsed = new ParsedPrize(resolvedRoundId, prizeType, scopeType, awardCriteria, resolvedFrom, resolvedTo);
        return (errors, parsed);
    }
}
