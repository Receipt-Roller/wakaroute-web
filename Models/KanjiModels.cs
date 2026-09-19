using System.Text.Json.Serialization;

namespace wakaroute_web.Models;

public sealed record KanjiDataset(
    int SchemaVersion,
    string DatasetVersion,
    string AsOf,
    string Language,
    KanjiDatasetLicense License,
    IReadOnlyList<KanjiDatasetSource> Sources,
    KanjiGradeAssignment Assignment,
    KanjiOfficialLearningGoals OfficialLearningGoals,
    IReadOnlyList<KanjiEntry> Items);

public sealed record KanjiDatasetLicense(
    string Name,
    string SpdxId,
    string Url,
    string Attribution);

public sealed record KanjiDatasetSource(
    string Id,
    string Name,
    string Url,
    string Role,
    string? Version,
    string? CreatedAt);

public sealed record KanjiGradeAssignment(
    string Classification,
    bool OfficialGradeAssignment,
    string Method,
    IReadOnlyDictionary<string, int> GradeCounts);

public sealed record KanjiOfficialLearningGoals(
    IReadOnlyDictionary<string, string> Reading,
    IReadOnlyDictionary<string, string> Writing);

public sealed record KanjiEntry(
    string Id,
    string Character,
    string OfficialStage,
    int RecommendedGrade,
    int Sequence,
    int StrokeCount,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? FrequencyRank,
    IReadOnlyList<string> OnReadings,
    IReadOnlyList<string> KunReadings)
{
    [JsonIgnore]
    public IReadOnlyList<string> OnReadingsForDisplay => OnReadings
        .Select(NormalizeReading)
        .ToArray();

    [JsonIgnore]
    public IReadOnlyList<string> KunReadingsForDisplay => KunReadings
        .Select(NormalizeReading)
        .ToArray();

    private static string NormalizeReading(string reading) => reading.Replace('.', '・');
}

public sealed record KanjiSearchResult(
    IReadOnlyList<KanjiEntry> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed class KanjiIndexViewModel
{
    public required KanjiDataset Dataset { get; init; }
}

public sealed class KanjiGradeViewModel
{
    public required KanjiDataset Dataset { get; init; }
    public required int Grade { get; init; }
    public required string Query { get; init; }
    public required KanjiSearchResult Result { get; init; }

    public string ReadingGoal => Dataset.OfficialLearningGoals.Reading[Grade.ToString()];
    public string WritingGoal => Dataset.OfficialLearningGoals.Writing[Grade.ToString()];
}
