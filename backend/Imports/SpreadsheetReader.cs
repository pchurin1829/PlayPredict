using System.Globalization;
using System.Text;
using ExcelDataReader;

namespace PlayPredict.Api.Imports;

public sealed class SpreadsheetReader
{
    public const string TeamsSheet = "IMPORTAR_EQUIPOS";
    public const string RostersSheet = "IMPORTAR_PLANTELES";
    public const string MatchesSheet = "IMPORTAR_PARTIDOS";

    private static readonly string[] TeamHeaders = ["NOMBRE DEL EQUIPO", "NOMBRE CORTO"];
    private static readonly string[] RosterHeaders = ["NOMBRE DEL CLUB", "NOMBRE", "APELLIDO", "NOMBRE PARA MOSTRAR", "POSICION"];
    private static readonly string[] MatchHeaders = ["FECHA_NRO", "FECHA", "HORA", "LOCAL", "VISITANTE", "ESTADO"];
    private static readonly HashSet<string> OptionalMatchHeaders =
        new(["TORNEO", "EDICION", "ZONA", "FUENTE"], StringComparer.Ordinal);

    static SpreadsheetReader()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public SpreadsheetReadResult Read(Stream stream, string fileName, SpreadsheetImportKind kind)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var teams = new List<ImportTeamRow>();
        var rosters = new List<ImportRosterRow>();
        var matches = new List<ImportMatchRow>();
        var issues = new List<SpreadsheetValidationIssue>();

        var extension = Path.GetExtension(fileName);
        if (!extension.Equals(".xls", StringComparison.OrdinalIgnoreCase)
            && !extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new("UNSUPPORTED_FILE_TYPE", "El archivo debe tener extensi\u00f3n .xls o .xlsx."));
            return new(teams, rosters, matches, issues);
        }

        var expectedSheets = kind == SpreadsheetImportKind.TeamsAndRosters
            ? new HashSet<string>([TeamsSheet, RostersSheet], StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>([MatchesSheet], StringComparer.OrdinalIgnoreCase);
        var foundSheets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var reader = ExcelReaderFactory.CreateReader(stream, new ExcelReaderConfiguration
            {
                LeaveOpen = true,
                FallbackEncoding = Encoding.GetEncoding(1252)
            });

            do
            {
                if (!expectedSheets.Contains(reader.Name)) continue;
                if (!foundSheets.Add(reader.Name))
                {
                    issues.Add(new("DUPLICATE_SHEET", $"La hoja {reader.Name} aparece m\u00e1s de una vez.", reader.Name));
                    continue;
                }

                if (reader.Name.Equals(TeamsSheet, StringComparison.OrdinalIgnoreCase))
                    ReadTeams(reader, teams, issues);
                else if (reader.Name.Equals(RostersSheet, StringComparison.OrdinalIgnoreCase))
                    ReadRosters(reader, rosters, issues);
                else if (reader.Name.Equals(MatchesSheet, StringComparison.OrdinalIgnoreCase))
                    ReadMatches(reader, matches, issues);
            } while (reader.NextResult());
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            issues.Add(new("INVALID_SPREADSHEET", $"No se pudo leer el archivo: {exception.Message}"));
        }

        foreach (var expectedSheet in expectedSheets.Where(sheet => !foundSheets.Contains(sheet)))
            issues.Add(new("MISSING_SHEET", $"Falta la hoja obligatoria {expectedSheet}.", expectedSheet));

        return new(teams, rosters, matches, issues);
    }

    private static void ReadTeams(IExcelDataReader reader, List<ImportTeamRow> rows, List<SpreadsheetValidationIssue> issues)
    {
        var headers = ReadHeaders(reader, TeamsSheet, TeamHeaders, issues, out var headerRowNumber);
        if (headers is null) return;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rowNumber = headerRowNumber;
        while (reader.Read())
        {
            rowNumber++;
            if (IsEmptyRow(reader))
            {
                issues.Add(new("EMPTY_ROW", "La fila est\u00e1 vac\u00eda.", TeamsSheet, rowNumber));
                continue;
            }

            var originalName = GetOriginal(reader, headers, "NOMBRE DEL EQUIPO");
            var originalShortName = GetOriginal(reader, headers, "NOMBRE CORTO");
            Required(originalName, TeamsSheet, rowNumber, "NOMBRE DEL EQUIPO", issues);
            Required(originalShortName, TeamsSheet, rowNumber, "NOMBRE CORTO", issues);
            var name = SpreadsheetTextNormalizer.Clean(originalName);
            var shortName = SpreadsheetTextNormalizer.Clean(originalShortName);
            var normalizedName = SpreadsheetTextNormalizer.Normalize(name);
            rows.Add(new(rowNumber, originalName, originalShortName, name, shortName, normalizedName));
            if (normalizedName.Length > 0 && !seen.Add(normalizedName))
                issues.Add(new("DUPLICATE_TEAM_ROW", "El equipo aparece m\u00e1s de una vez en el archivo.", TeamsSheet, rowNumber, "NOMBRE DEL EQUIPO"));
        }
    }

    private static void ReadRosters(IExcelDataReader reader, List<ImportRosterRow> rows, List<SpreadsheetValidationIssue> issues)
    {
        var headers = ReadHeaders(reader, RostersSheet, RosterHeaders, issues, out var headerRowNumber);
        if (headers is null) return;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rowNumber = headerRowNumber;
        while (reader.Read())
        {
            rowNumber++;
            if (IsEmptyRow(reader))
            {
                issues.Add(new("EMPTY_ROW", "La fila est\u00e1 vac\u00eda.", RostersSheet, rowNumber));
                continue;
            }

            var originalClub = GetOriginal(reader, headers, "NOMBRE DEL CLUB");
            var originalFirstName = GetOriginal(reader, headers, "NOMBRE");
            var originalLastName = GetOriginal(reader, headers, "APELLIDO");
            var originalDisplayName = GetOriginal(reader, headers, "NOMBRE PARA MOSTRAR");
            var originalPosition = GetOriginal(reader, headers, "POSICION");
            Required(originalClub, RostersSheet, rowNumber, "NOMBRE DEL CLUB", issues);
            Required(originalFirstName, RostersSheet, rowNumber, "NOMBRE", issues);
            Required(originalLastName, RostersSheet, rowNumber, "APELLIDO", issues);
            Required(originalPosition, RostersSheet, rowNumber, "POSICION", issues);

            var club = SpreadsheetTextNormalizer.Clean(originalClub);
            var firstName = SpreadsheetTextNormalizer.Clean(originalFirstName);
            var lastName = SpreadsheetTextNormalizer.Clean(originalLastName);
            var displayName = SpreadsheetTextNormalizer.Clean(originalDisplayName);
            if (displayName.Length == 0) displayName = SpreadsheetTextNormalizer.Clean($"{firstName} {lastName}");
            var normalizedClub = SpreadsheetTextNormalizer.Normalize(club);
            var normalizedFirstName = SpreadsheetTextNormalizer.Normalize(firstName);
            var normalizedLastName = SpreadsheetTextNormalizer.Normalize(lastName);
            var position = ParsePosition(originalPosition);
            if (SpreadsheetTextNormalizer.Clean(originalPosition).Length > 0 && position is null)
                issues.Add(new("INVALID_POSITION", "POSICION debe ser ARQUERO, DEFENSOR, MEDIOCAMPISTA o DELANTERO.", RostersSheet, rowNumber, "POSICION"));

            rows.Add(new(rowNumber, originalClub, originalFirstName, originalLastName, originalDisplayName, originalPosition,
                club, firstName, lastName, displayName, normalizedClub, normalizedFirstName, normalizedLastName, position));
            var key = $"{normalizedClub}|{normalizedFirstName}|{normalizedLastName}";
            if (normalizedClub.Length > 0 && normalizedFirstName.Length > 0 && normalizedLastName.Length > 0 && !seen.Add(key))
                issues.Add(new("DUPLICATE_ROSTER_ROW", "El jugador aparece m\u00e1s de una vez para el mismo equipo.", RostersSheet, rowNumber));
        }
    }

    private static void ReadMatches(IExcelDataReader reader, List<ImportMatchRow> rows, List<SpreadsheetValidationIssue> issues)
    {
        var headers = ReadHeaders(reader, MatchesSheet, MatchHeaders, issues, out var headerRowNumber, OptionalMatchHeaders);
        if (headers is null) return;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rowNumber = headerRowNumber;
        while (reader.Read())
        {
            rowNumber++;
            if (IsEmptyRow(reader))
            {
                issues.Add(new("EMPTY_ROW", "La fila est\u00e1 vac\u00eda.", MatchesSheet, rowNumber));
                continue;
            }

            var roundRaw = GetRaw(reader, headers, "FECHA_NRO");
            var dateRaw = GetRaw(reader, headers, "FECHA");
            var timeRaw = GetRaw(reader, headers, "HORA");
            var originalRound = FormatOriginal(roundRaw);
            var originalDate = FormatOriginal(dateRaw);
            var originalTime = FormatOriginal(timeRaw);
            var originalHome = GetOriginal(reader, headers, "LOCAL");
            var originalAway = GetOriginal(reader, headers, "VISITANTE");
            var originalStatus = GetOriginal(reader, headers, "ESTADO");
            Required(originalRound, MatchesSheet, rowNumber, "FECHA_NRO", issues);
            Required(originalDate, MatchesSheet, rowNumber, "FECHA", issues);
            Required(originalTime, MatchesSheet, rowNumber, "HORA", issues);
            Required(originalHome, MatchesSheet, rowNumber, "LOCAL", issues);
            Required(originalAway, MatchesSheet, rowNumber, "VISITANTE", issues);
            Required(originalStatus, MatchesSheet, rowNumber, "ESTADO", issues);

            var roundNumber = ParseRoundNumber(roundRaw);
            if (SpreadsheetTextNormalizer.Clean(originalRound).Length > 0 && roundNumber is null)
                issues.Add(new("INVALID_ROUND_NUMBER", "FECHA_NRO debe ser un entero mayor o igual a 1.", MatchesSheet, rowNumber, "FECHA_NRO"));
            var date = ParseDate(dateRaw);
            if (SpreadsheetTextNormalizer.Clean(originalDate).Length > 0 && date is null)
                issues.Add(new("INVALID_DATE", "FECHA debe ser una fecha Excel o tener formato YYYY-MM-DD.", MatchesSheet, rowNumber, "FECHA"));
            var time = ParseTime(timeRaw);
            if (SpreadsheetTextNormalizer.Clean(originalTime).Length > 0 && time is null)
                issues.Add(new("INVALID_TIME", "HORA debe ser una hora Excel o tener formato HH:mm o HH:mm:ss.", MatchesSheet, rowNumber, "HORA"));
            var status = ParseStatus(originalStatus);
            if (SpreadsheetTextNormalizer.Clean(originalStatus).Length > 0 && status is null)
                issues.Add(new("INVALID_STATUS", "ESTADO debe ser SCHEDULED, IN_PROGRESS, SUSPENDED o CANCELLED. FINISHED no se admite en esta importaci\u00f3n.", MatchesSheet, rowNumber, "ESTADO"));

            var home = SpreadsheetTextNormalizer.Clean(originalHome);
            var away = SpreadsheetTextNormalizer.Clean(originalAway);
            var normalizedHome = SpreadsheetTextNormalizer.Normalize(home);
            var normalizedAway = SpreadsheetTextNormalizer.Normalize(away);
            rows.Add(new(rowNumber, originalRound, originalDate, originalTime, originalHome, originalAway, originalStatus,
                roundNumber, date, time, home, away, normalizedHome, normalizedAway, status));
            if (roundNumber.HasValue && normalizedHome.Length > 0 && normalizedAway.Length > 0
                && !seen.Add($"{roundNumber}|{normalizedHome}|{normalizedAway}"))
                issues.Add(new("DUPLICATE_MATCH_ROW", "El partido aparece m\u00e1s de una vez en el archivo.", MatchesSheet, rowNumber));
        }
    }

    private static Dictionary<string, int>? ReadHeaders(
        IExcelDataReader reader,
        string sheetName,
        string[] expected,
        List<SpreadsheetValidationIssue> issues,
        out int headerRowNumber,
        IReadOnlySet<string>? optional = null)
    {
        headerRowNumber = 0;
        var expectedSet = expected.ToHashSet(StringComparer.Ordinal);
        while (headerRowNumber < 10 && reader.Read())
        {
            headerRowNumber++;
            var containsContractHeader = Enumerable.Range(0, reader.FieldCount)
                .Select(index => SpreadsheetTextNormalizer.Normalize(FormatOriginal(reader.GetValue(index))))
                .Any(expectedSet.Contains);
            if (containsContractHeader) break;
        }

        if (headerRowNumber == 0 || reader.FieldCount == 0)
        {
            issues.Add(new("EMPTY_SHEET", $"La hoja {sheetName} no contiene encabezados.", sheetName));
            return null;
        }

        var headers = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var index = 0; index < reader.FieldCount; index++)
        {
            var original = FormatOriginal(reader.GetValue(index));
            var normalized = SpreadsheetTextNormalizer.Normalize(original);
            if (normalized.Length == 0)
            {
                issues.Add(new("EMPTY_HEADER", "Hay una columna sin encabezado.", sheetName, headerRowNumber));
                continue;
            }
            if (!expectedSet.Contains(normalized) && !(optional?.Contains(normalized) ?? false))
            {
                issues.Add(new("UNKNOWN_HEADER", $"El encabezado '{original}' no pertenece al contrato de {sheetName}.", sheetName, headerRowNumber, original));
                continue;
            }
            if (!headers.TryAdd(normalized, index))
                issues.Add(new("DUPLICATE_HEADER", $"El encabezado {normalized} aparece m\u00e1s de una vez.", sheetName, headerRowNumber, normalized));
        }

        foreach (var missing in expected.Where(header => !headers.ContainsKey(header)))
            issues.Add(new("MISSING_HEADER", $"Falta el encabezado obligatorio {missing}.", sheetName, headerRowNumber, missing));
        return headers;
    }

    private static bool IsEmptyRow(IExcelDataReader reader)
    {
        for (var index = 0; index < reader.FieldCount; index++)
            if (SpreadsheetTextNormalizer.Clean(FormatOriginal(reader.GetValue(index))).Length > 0) return false;
        return true;
    }

    private static object? GetRaw(IExcelDataReader reader, IReadOnlyDictionary<string, int> headers, string name) =>
        headers.TryGetValue(name, out var index) && index < reader.FieldCount ? reader.GetValue(index) : null;

    private static string GetOriginal(IExcelDataReader reader, IReadOnlyDictionary<string, int> headers, string name) =>
        FormatOriginal(GetRaw(reader, headers, name));

    private static string FormatOriginal(object? value) => value switch
    {
        null => string.Empty,
        DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
        double number => number.ToString("R", CultureInfo.InvariantCulture),
        float number => number.ToString("R", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    private static void Required(string value, string sheet, int row, string column, List<SpreadsheetValidationIssue> issues)
    {
        if (SpreadsheetTextNormalizer.Clean(value).Length == 0)
            issues.Add(new("REQUIRED_VALUE", $"El valor de {column} es obligatorio.", sheet, row, column));
    }

    private static ImportPlayerPosition? ParsePosition(string value) => SpreadsheetTextNormalizer.Normalize(value) switch
    {
        "ARQUERO" => ImportPlayerPosition.Goalkeeper,
        "DEFENSOR" => ImportPlayerPosition.Defender,
        "MEDIOCAMPISTA" => ImportPlayerPosition.Midfielder,
        "DELANTERO" => ImportPlayerPosition.Forward,
        _ => null
    };

    private static ImportMatchStatus? ParseStatus(string value) => SpreadsheetTextNormalizer.Normalize(value) switch
    {
        "SCHEDULED" => ImportMatchStatus.Scheduled,
        "IN_PROGRESS" => ImportMatchStatus.InProgress,
        "SUSPENDED" => ImportMatchStatus.Suspended,
        "CANCELLED" => ImportMatchStatus.Cancelled,
        _ => null
    };

    private static int? ParseRoundNumber(object? value)
    {
        if (value is double number && number >= 1 && number <= int.MaxValue && number == Math.Truncate(number)) return (int)number;
        if (value is int integer && integer >= 1) return integer;
        return int.TryParse(SpreadsheetTextNormalizer.Clean(FormatOriginal(value)), NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed >= 1 ? parsed : null;
    }

    private static DateOnly? ParseDate(object? value)
    {
        if (value is DateTime dateTime) return DateOnly.FromDateTime(dateTime);
        if (value is double serial)
        {
            try { return DateOnly.FromDateTime(DateTime.FromOADate(serial)); }
            catch (ArgumentException) { return null; }
        }
        return DateOnly.TryParseExact(SpreadsheetTextNormalizer.Clean(FormatOriginal(value)), "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsed) ? parsed : null;
    }

    private static TimeOnly? ParseTime(object? value)
    {
        if (value is TimeSpan timeSpan && timeSpan >= TimeSpan.Zero && timeSpan < TimeSpan.FromDays(1)) return TimeOnly.FromTimeSpan(timeSpan);
        if (value is DateTime dateTime) return TimeOnly.FromDateTime(dateTime);
        if (value is double fraction && fraction >= 0 && fraction < 1) return TimeOnly.FromTimeSpan(TimeSpan.FromDays(fraction));
        var text = SpreadsheetTextNormalizer.Clean(FormatOriginal(value));
        foreach (var format in new[] { "HH:mm", "H:mm", "HH:mm:ss", "H:mm:ss" })
            if (TimeOnly.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) return parsed;
        return null;
    }
}
