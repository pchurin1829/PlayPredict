using Microsoft.Extensions.Options;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Imports;

namespace PlayPredict.Api.Endpoints;

public static class AdminTeamRosterImportEndpoints
{
    public static void MapAdminTeamRosterImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/team-roster-import")
            .WithTags("Admin Team Roster Import")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapPost("/preview", PreviewAsync).DisableAntiforgery();
        group.MapPost("/confirm", ConfirmAsync).DisableAntiforgery();
    }

    private static async Task<IResult> PreviewAsync(
        HttpRequest request,
        SpreadsheetReader reader,
        TeamRosterImportPreviewService previewService,
        IOptions<TeamRosterImportOptions> options,
        CancellationToken cancellationToken)
    {
        var input = await ReadInputAsync(request, requireHash: false, options.Value, cancellationToken);
        if (input.Error is not null) return input.Error;

        await using var fileStream = input.File!.OpenReadStream();
        var (content, hash) = await SpreadsheetFileHash.ReadAndComputeSha256Async(fileStream, cancellationToken);
        using var spreadsheetStream = new MemoryStream(content, writable: false);
        var spreadsheet = reader.Read(spreadsheetStream, input.File.FileName, SpreadsheetImportKind.TeamsAndRosters);
        var preview = await previewService.PreviewAsync(spreadsheet, input.Sport!, cancellationToken);
        return Results.Ok(new TeamRosterImportPreviewHttpResponse(
            hash, preview.Sport, preview.TeamsSummary, preview.RostersSummary,
            preview.Teams, preview.Rosters, preview.Issues, preview.CanConfirm));
    }

    private static async Task<IResult> ConfirmAsync(
        HttpRequest request,
        TeamRosterImportConfirmationService confirmationService,
        IOptions<TeamRosterImportOptions> options,
        CancellationToken cancellationToken)
    {
        var input = await ReadInputAsync(request, requireHash: true, options.Value, cancellationToken);
        if (input.Error is not null) return input.Error;

        await using var fileStream = input.File!.OpenReadStream();
        var result = await confirmationService.ConfirmAsync(
            fileStream, input.File.FileName, input.Sport!, input.ExpectedHash!, cancellationToken);
        return result.Status switch
        {
            ImportConfirmationStatus.Success => Results.Ok(result),
            ImportConfirmationStatus.Rejected => Results.Json(result, statusCode: StatusCodes.Status422UnprocessableEntity),
            _ => Results.Json(new
            {
                status = ImportConfirmationStatus.Failed,
                processedHash = result.ProcessedHash,
                message = result.Message,
                teams = result.Teams,
                rosters = result.Rosters,
                issues = new[] { new SpreadsheetValidationIssue("IMPORT_FAILED", "No se pudo completar la importaci\u00f3n.") }
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
            return Invalid("FILE_TOO_LARGE", $"El archivo supera el l\u00edmite de {options.MaxFileSizeBytes / 1024 / 1024} MB.");

        var extension = Path.GetExtension(file.FileName);
        if (!extension.Equals(".xls", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return Invalid("INVALID_FILE_EXTENSION", "El archivo debe tener extensi\u00f3n .xls o .xlsx.");

        var sport = SpreadsheetTextNormalizer.Clean(form["sport"].ToString());
        if (sport.Length == 0) return Invalid("SPORT_REQUIRED", "Debe seleccionar un deporte.");

        var expectedHash = SpreadsheetTextNormalizer.Clean(form["expectedHash"].ToString());
        if (requireHash && expectedHash.Length == 0)
            return Invalid("EXPECTED_HASH_REQUIRED", "Debe informar el hash del archivo analizado.");

        return new(file, sport, expectedHash, null);
    }

    private static ImportHttpInput Invalid(string code, string message) =>
        new(null, null, null, Results.BadRequest(new
        {
            message,
            issues = new[] { new SpreadsheetValidationIssue(code, message) }
        }));

    private sealed record ImportHttpInput(IFormFile? File, string? Sport, string? ExpectedHash, IResult? Error);
}

public sealed record TeamRosterImportPreviewHttpResponse(
    string Hash,
    string Sport,
    TeamImportPreviewSummary TeamsSummary,
    RosterImportPreviewSummary RostersSummary,
    IReadOnlyList<TeamImportPreviewRow> Teams,
    IReadOnlyList<RosterImportPreviewRow> Rosters,
    IReadOnlyList<SpreadsheetValidationIssue> Issues,
    bool CanConfirm);
