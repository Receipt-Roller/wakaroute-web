namespace wakaroute_web.Models;

public sealed record OfficialPastExamSource(
    string Id,
    string PrefectureCode,
    string Prefecture,
    string AuthorityName,
    string Title,
    string OfficialPageUrl,
    IReadOnlyList<int> AcademicYears,
    IReadOnlyList<string> ExamScopes,
    IReadOnlyList<string> Materials,
    string VerifiedAt,
    string UsageNote)
{
    private static readonly IReadOnlyDictionary<string, string> MaterialLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["questions"] = "問題",
            ["answers"] = "正答・採点資料",
            ["answer-sheets"] = "解答用紙",
            ["listening-scripts"] = "リスニング台本",
            ["listening-audio"] = "リスニング音源",
            ["scoring-guidance"] = "採点のポイント"
        };

    public int LatestAcademicYear => AcademicYears.Count == 0 ? 0 : AcademicYears.Max();

    public bool HasOnlineQuestions => Materials.Contains("questions", StringComparer.Ordinal);

    public IReadOnlyList<string> MaterialLabelsForDisplay => Materials
        .Select(material => MaterialLabels.GetValueOrDefault(material, material))
        .ToArray();
}

public sealed class PastExamIndexViewModel
{
    public required IReadOnlyList<OfficialPastExamSource> Sources { get; init; }
    public required string AsOf { get; init; }
}
