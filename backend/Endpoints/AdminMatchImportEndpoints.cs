using System.Security.Claims;
using Microsoft.Extensions.Options;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Imports;

namespace PlayPredict.Api.Endpoints;

public static class AdminMatchImportEndpoints
{
    public static void MapAdminMatchImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/match-import")
            .WithTags("Admin Match Import")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapPost("/preview", PreviewAsync).DisableAntiforgery();
        group.MapPost("/confirm", ConfirmAsync).DisableAntiforgery();
    }

    private static async Task<IResult> PreviewAsync(
        HttpRequest request,
        SpreadsheetReader reader,
        MatchImportPreviewService previewService,
        IOptions<TeamRosterImportOptions> options,
        CancellationToken cancellationToken)
    {
        var input = await ReadInputAsync(request, requireHash: false, options.Value, cancellationToken);
        if (input.Error is not null) return input.Error;

        await using var fileStream = input.File!.OpenReadStream();
        var (content, hash) = await SpreadsheetFileHash.ReadAndComputeSha256Async(fileStream, cancellationToken);
        using var spreadsheetStream = new MemoryStream(content, writable: false);
        var spreadsheet = reader.Read(spreadsheetStream, input.File.FileName, SpreadsheetImportKind.Matches);
        var preview = await previewService.PreviewAsync(spreadsheet, input.EditionId!.Value, cancellationToken);
        return Results.Ok(new MatchImportPreviewHttpResponse(
            hash, preview.EditionId, preview.Summary, preview.Matches, preview.Issues, preview.CanConfirm));
    }

    private static async Task<IResult> ConfirmAsync(
        HttpRequest request,
        ClaimsPrincipal principal,
        MatchImportConfirmationService confirmationService,
        IOptions<TeamRosterImportOptions> options,
        CancellationToken cancellationToken)
    {
        if (!TryAdminUserId(principal, out var adminUserId)) return Results.Unauthorized();

        var input = await ReadInputAsync(request, requireHash: true, options.Value, cancellationToken);
        if (input.Error is not null) return input.Error;

        await using var fileStream = input.File!.OpenReadStream();
        var result = await confirmationService.ConfirmAsync(
            fileStream, input.File.FileName, input.EditionId!.Value, adminUserId, input.ExpectedHash!, cancellationToken);
        return result.Status switch
        {
            ImportConfirmationStatus.Success => Results.Ok(result),
            ImportConfirmationStatus.Rejected => Results.Json(result, statusCode: StatusCodes.Status422UnprocessableEntity),
            _ => Results.Json(new
            {
                status = ImportConfirmationStatus.Failed,
                processedHash = result.ProcessedHash,
                message = result.Message,
                matches = result.Matches,
                issues = new[] { new SpreadsheetValidationIssue("IMPORT_FAILED", "No se pudo completar la importación.") }
            }, statusCode: StatusCodes.Status500InternalServerError)
        };
    }

    private static async Task<ImportHttpInput> ReadInputAsync(
        HttpRequest request,
        bool requireHash,
        TeamRosterImportOptions options,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
            return Invalid("INVALID_MULTIPART", "La solicitud debe usar multipart/form-data.");

        IFormCollection form;
        try { form = await request.ReadFormAsync(cancellationToken); }
        catch (InvalidDataException) { return Invalid("INVALID_MULTIPART", "No se pudo leer el formulario multipart."); }

        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
            return Invalid("FILE_REQUIRED", "Debe seleccionar un archivo XLS o XLSX.");
        if (file.Length > options.MaxFileSizeBytes)
            return Invalid("FILE_TOO_LARGE", $"El archivo supera el límite de {options.MaxFileSizeBytes / 1024 / 1024} MB.");

        var extension = Path.GetExtension(file.FileName);
        if (!extension.Equals(".xls", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return Invalid("INVALID_FILE_EXTENSION", "El archivo debe tener extensión .xls o .xlsx.");

        if (!int.TryParse(form["editionId"].ToString(), out var editionId) || editionId <= 0)
            return Invalid("EDITION_ID_REQUIRED", "Debe indicar una Edición válida.");

        var expectedHash = SpreadsheetTextNormalizer.Clean(form["expectedHash"].ToString());
        if (requireHash && expectedHash.Length == 0)
            return Invalid("EXPECTED_HASH_REQUIRED", "Debe informar el hash del archivo analizado.");

        return new(file, editionId, expectedHash, null);
    }

    private static ImportHttpInput Invalid(string code, string message) =>
        new(null, null, null, Results.BadRequest(new
        {
            message,
            issues = new[] { new SpreadsheetValidationIssue(code, message) }
        }));

    private static bool TryAdminUserId(ClaimsPrincipal principal, out int userId)
    {
        var user = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        return int.TryParse(user, out userId);
    }

    private sealed record ImportHttpInput(IFormFile? File, int? EditionId, string? ExpectedHash, IResult? Error);
}

public sealed record MatchImportPreviewHttpResponse(
    string Hash,
    int EditionId,
    MatchImportPreviewSummary Summary,
    IReadOnlyList<MatchImportPreviewRow> Matches,
    IReadOnlyList<SpreadsheetValidationIssue> Issues,
    bool CanConfirm);
