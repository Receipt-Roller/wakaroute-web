namespace wakaroute_web.Models;

public sealed record SchoolCatalogItem(
    string Id,
    string Name,
    string? NameKana,
    string PrefectureCode,
    string Prefecture,
    string Address,
    string PostalCode,
    string Ownership,
    string CampusType,
    string OpenedOn,
    string LastVerifiedAt,
    decimal? Latitude,
    decimal? Longitude,
    string? OfficialUrl,
    IReadOnlyList<string> Tags)
{
    public string OwnershipLabel => Ownership switch
    {
        "national" => "国立",
        "public" => "公立",
        "private" => "私立",
        _ => "その他"
    };

    public string CampusTypeLabel => CampusType == "branch" ? "分校" : "本校";
}

public sealed record SchoolCatalogMetadata(
    int SchemaVersion,
    string AsOf,
    int CurrentSchoolCount,
    int BranchCount,
    int IdentityCount,
    int ClosedIdentityCount,
    IReadOnlyList<string> SourceUrls);

public sealed record SchoolFilterOption(string Value, string Label, int Count);

public sealed record SchoolSearchPage(
    IReadOnlyList<SchoolCatalogItem> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);

public sealed record SchoolProgram(
    string AttendanceType,
    string Department,
    string? Course)
{
    public string AttendanceTypeLabel => AttendanceType switch
    {
        "full-time" => "全日制",
        "part-time" => "定時制",
        "correspondence" => "通信制",
        _ => "課程未確認"
    };
}

public sealed record SchoolDataSource(
    string Title,
    string Url,
    string? PublishedAt,
    string VerifiedAt);

public sealed record SchoolProfile(
    string SchoolId,
    string Gender,
    IReadOnlyList<SchoolProgram> Programs,
    IReadOnlyList<SchoolDataSource> Sources)
{
    public string GenderLabel => Gender switch
    {
        "coeducational" => "共学",
        "boys" => "男子校",
        "girls" => "女子校",
        _ => "未確認"
    };
}

public sealed record SchoolAdmissionResult(
    string SchoolId,
    int AcademicYear,
    string Selection,
    string SelectionLabel,
    string AttendanceType,
    string Department,
    int? Capacity,
    int? Applicants,
    int? Examinees,
    int? Admitted,
    int? Enrolled,
    string? Note,
    IReadOnlyList<SchoolDataSource> Sources)
{
    public string AttendanceTypeLabel => AttendanceType switch
    {
        "full-time" => "全日制",
        "part-time" => "定時制",
        "correspondence" => "通信制",
        _ => "課程未確認"
    };

    public decimal? ApplicationRatio => Ratio(Applicants, Capacity);
    public decimal? ExaminationRatio => Ratio(Examinees, Capacity);
    public decimal? EffectiveRatio => Ratio(Examinees, Admitted);

    private static decimal? Ratio(int? numerator, int? denominator) =>
        numerator.HasValue && denominator > 0
            ? Math.Round(numerator.Value / (decimal)denominator.Value, 2)
            : null;
}

public sealed record SchoolDeviationScore(
    string SchoolId,
    int AcademicYear,
    string Provider,
    decimal? Value,
    decimal? ValueLow,
    decimal? ValueHigh,
    string Population,
    string SourceUrl,
    string License,
    string VerifiedAt);

public sealed record SchoolDetailsViewModel(
    SchoolCatalogItem School,
    SchoolProfile? Profile,
    SchoolDecisionGuide? DecisionGuide,
    IReadOnlyList<SchoolExamSchedule> ExamSchedules,
    IReadOnlyList<SchoolAdmissionResult> Admissions,
    IReadOnlyList<SchoolDeviationScore> DeviationScores);

public sealed record SchoolHighlight(string Title, string Description);

public sealed record SchoolAccessGuide(
    string Summary,
    IReadOnlyList<string> Routes);

public sealed record SchoolVisitEvent(
    string Title,
    int AcademicYear,
    string Status,
    string StatusLabel,
    IReadOnlyList<string> EventDates,
    string Time,
    string ApplicationPeriod,
    string ApplicationMethod,
    string? Note,
    IReadOnlyList<SchoolDataSource> Sources);

public sealed record SchoolDecisionGuide(
    string SchoolId,
    string Summary,
    string VerifiedAt,
    IReadOnlyList<SchoolHighlight> Highlights,
    IReadOnlyList<string> GoodFit,
    IReadOnlyList<string> QuestionsToConsider,
    SchoolAccessGuide Access,
    IReadOnlyList<SchoolVisitEvent> VisitEvents,
    IReadOnlyList<SchoolDataSource> Sources);

public sealed record SchoolExamSchedule(
    string SchoolId,
    int AcademicYear,
    string Selection,
    string SelectionLabel,
    string Status,
    string StatusLabel,
    string ApplicationPeriod,
    IReadOnlyList<string> TestDates,
    string ResultDate,
    string DetailsStatus,
    string DetailsStatusLabel,
    string? OfficialPublishedAt,
    string VerifiedAt,
    IReadOnlyList<string> Notes,
    IReadOnlyList<SchoolDataSource> Sources);

public sealed class SchoolSearchViewModel
{
    public required SchoolSearchPage Results { get; init; }
    public required SchoolCatalogMetadata Metadata { get; init; }
    public required IReadOnlyList<SchoolFilterOption> Prefectures { get; init; }
    public string Query { get; init; } = string.Empty;
    public string Prefecture { get; init; } = string.Empty;
    public string Ownership { get; init; } = string.Empty;

    public IReadOnlyList<int> PageNumbers
    {
        get
        {
            if (Results.TotalPages == 0) return [];
            var start = Math.Max(1, Results.Page - 2);
            var end = Math.Min(Results.TotalPages, start + 4);
            start = Math.Max(1, end - 4);
            return Enumerable.Range(start, end - start + 1).ToArray();
        }
    }
}
