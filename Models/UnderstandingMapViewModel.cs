namespace wakaroute_web.Models;

public sealed record UnderstandingMapViewModel(
    string SubjectId,
    string Subject,
    string HeroTitleFirst,
    string HeroTitleSecond,
    string Description,
    string MapLead,
    string StructureNote,
    string SourceName,
    string SourceUrl,
    IReadOnlyList<UnderstandingArea> Areas)
{
    public int NodeCount => Areas.Sum(area => area.Nodes.Count);

    public bool IsLiveCatalog { get; init; }

    public DateTimeOffset? CatalogFetchedAt { get; init; }
}

public sealed record UnderstandingArea(
    string Id,
    string Code,
    string Name,
    string Description,
    IReadOnlyList<UnderstandingNode> Nodes)
{
    public IReadOnlyList<UnderstandingTest> Tests { get; init; } = [];
}

public sealed record UnderstandingTest(
    string Id,
    string PathId,
    string Title,
    string? Description,
    int QuestionCount,
    int PassingScorePercent,
    int? TimeLimitSeconds)
{
    public int? TimeLimitMinutes => TimeLimitSeconds is > 0
        ? (int)Math.Ceiling(TimeLimitSeconds.Value / 60d)
        : null;
}

public sealed record UnderstandingNode(
    string Id,
    int Grade,
    string Title,
    string Summary,
    string WhyItMatters,
    IReadOnlyList<string> CanDo,
    IReadOnlyList<string> Prerequisites,
    IReadOnlyList<string> LeadsTo)
{
    public string? CourseId { get; init; }

    public string? PathId { get; init; }
}
