using System.Globalization;
using System.Text.Json;
using wakaroute_web.Models;

namespace wakaroute_web.Services.PastExams;

public sealed class JsonPastExamCatalog : IPastExamCatalog
{
    private static readonly HashSet<string> SupportedMaterials =
    [
        "questions",
        "answers",
        "answer-sheets",
        "listening-scripts",
        "listening-audio",
        "scoring-guidance"
    ];

    private readonly Dictionary<string, OfficialPastExamSource> _sourcesById;

    public JsonPastExamCatalog(IHostEnvironment environment)
    {
        var path = Path.Combine(environment.ContentRootPath, "Data", "PastExams", "official-past-exam-sources.json");
        if (!File.Exists(path))
            throw new InvalidOperationException($"Past exam catalog file was not found: {path}");

        var document = JsonSerializer.Deserialize<PastExamDocument>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException($"Past exam catalog file is empty or invalid: {path}");

        Validate(document);
        AsOf = document.AsOf;
        Sources = document.Sources
            .Select(source => new OfficialPastExamSource(
                source.Id,
                source.PrefectureCode,
                source.Prefecture,
                source.AuthorityName,
                source.Title,
                source.OfficialPageUrl,
                source.AcademicYears.OrderByDescending(year => year).ToArray(),
                source.ExamScopes,
                source.Materials,
                source.VerifiedAt,
                source.UsageNote))
            .OrderBy(source => source.PrefectureCode, StringComparer.Ordinal)
            .ThenByDescending(source => source.LatestAcademicYear)
            .ToArray();
        _sourcesById = Sources.ToDictionary(source => source.Id, StringComparer.Ordinal);
    }

    public string AsOf { get; }
    public IReadOnlyList<OfficialPastExamSource> Sources { get; }

    public IReadOnlyList<OfficialPastExamSource> GetByIds(IEnumerable<string> ids) => ids
        .Where(id => !string.IsNullOrWhiteSpace(id))
        .Select(id => id.Trim())
        .Distinct(StringComparer.Ordinal)
        .Select(id => _sourcesById.GetValueOrDefault(id))
        .Where(source => source is not null)
        .Cast<OfficialPastExamSource>()
        .ToArray();

    private static void Validate(PastExamDocument document)
    {
        if (document.SchemaVersion != 1)
            throw new InvalidOperationException("Unsupported past exam catalog schema version.");
        if (!DateOnly.TryParse(document.AsOf, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            throw new InvalidOperationException("Past exam catalog asOf must be an ISO date.");

        var duplicateId = document.Sources
            .GroupBy(source => source.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateId is not null)
            throw new InvalidOperationException($"Duplicate past exam source ID: {duplicateId.Key}");

        foreach (var source in document.Sources)
        {
            if (string.IsNullOrWhiteSpace(source.Id) ||
                source.PrefectureCode.Length != 2 ||
                !Uri.TryCreate(source.OfficialPageUrl, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                source.AcademicYears.Length == 0 ||
                !DateOnly.TryParse(source.VerifiedAt, CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
                source.Materials.Any(material => !SupportedMaterials.Contains(material)))
                throw new InvalidOperationException($"Past exam source contains invalid data: {source.Id}");
        }
    }

    private sealed class PastExamDocument
    {
        public int SchemaVersion { get; init; }
        public string AsOf { get; init; } = string.Empty;
        public PastExamSourceEntry[] Sources { get; init; } = [];
    }

    private sealed class PastExamSourceEntry
    {
        public string Id { get; init; } = string.Empty;
        public string PrefectureCode { get; init; } = string.Empty;
        public string Prefecture { get; init; } = string.Empty;
        public string AuthorityName { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string OfficialPageUrl { get; init; } = string.Empty;
        public int[] AcademicYears { get; init; } = [];
        public string[] ExamScopes { get; init; } = [];
        public string[] Materials { get; init; } = [];
        public string VerifiedAt { get; init; } = string.Empty;
        public string UsageNote { get; init; } = string.Empty;
    }
}
