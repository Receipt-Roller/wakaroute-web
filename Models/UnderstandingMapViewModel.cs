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
}

public sealed record UnderstandingArea(
    string Id,
    string Code,
    string Name,
    string Description,
    IReadOnlyList<UnderstandingNode> Nodes);

public sealed record UnderstandingNode(
    string Id,
    int Grade,
    string Title,
    string Summary,
    string WhyItMatters,
    IReadOnlyList<string> CanDo,
    IReadOnlyList<string> Prerequisites,
    IReadOnlyList<string> LeadsTo);
