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
    public string Gender { get; init; } = "unknown";
    public IReadOnlyList<SchoolProgram> Programs { get; init; } = [];
    public bool HasAdmissionResults { get; init; }
    public bool HasExamSchedules { get; init; }
    public bool HasVisitEvents { get; init; }
    public bool HasSchoolLife { get; init; }
    public decimal? LatestApplicationRatio { get; init; }
    public SchoolAccessInfo? AccessInfo { get; init; }
    public SchoolLifeInfo? SchoolLife { get; init; }

    public string OwnershipLabel => Ownership switch
    {
        "national" => "国立",
        "public" => "公立",
        "private" => "私立",
        _ => "その他"
    };

    public string CampusTypeLabel => CampusType == "branch" ? "分校" : "本校";

    public string GenderLabel => Gender switch
    {
        "coeducational" => "共学",
        "boys" => "男子校",
        "girls" => "女子校",
        _ => "共学区分未確認"
    };

    public string ProgramSummary
    {
        get
        {
            var labels = Programs
                .Select(program => $"{program.AttendanceTypeLabel}・{program.Department}")
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (labels.Length == 0) return "課程・学科未確認";
            var visible = string.Join(" ／ ", labels.Take(2));
            return labels.Length > 2 ? $"{visible} ほか{labels.Length - 2}件" : visible;
        }
    }
}

public sealed record SchoolCatalogMetadata(
    int SchemaVersion,
    string AsOf,
    int CurrentSchoolCount,
    int BranchCount,
    int IdentityCount,
    int ClosedIdentityCount,
    IReadOnlyList<string> SourceUrls);

public sealed record SchoolStation(
    string GroupCode,
    string Name,
    decimal Latitude,
    decimal Longitude,
    int DistanceMeters,
    IReadOnlyList<string> Lines,
    IReadOnlyList<string> Operators)
{
    public string DistanceLabel => DistanceMeters < 1000
        ? $"直線約 {DistanceMeters}m"
        : $"直線約 {DistanceMeters / 1000m:0.0}km";

    public string LinesSummary => Lines.Count == 0 ? "路線情報未確認" : string.Join("・", Lines);
}

public sealed record SchoolAccessInfo(
    int CoordinateDataYear,
    int StationDataYear,
    string CoordinateSourceUrl,
    string StationSourceUrl,
    string Method,
    IReadOnlyList<SchoolStation> NearestStations);

public sealed record SchoolFilterOption(string Value, string Label, int Count);

public sealed record SchoolSearchCriteria(
    string Query,
    string Prefecture,
    string Ownership,
    string Gender,
    string AttendanceType,
    string DepartmentCategory,
    string Recruitment,
    bool HasAdmissions,
    bool HasExamSchedule,
    bool HasVisitEvents,
    bool HasSchoolLife,
    string Sort);

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
    SchoolLifeInfo? SchoolLife,
    IReadOnlyList<SchoolExamSchedule> ExamSchedules,
    IReadOnlyList<SchoolAdmissionResult> Admissions,
    IReadOnlyList<SchoolDeviationScore> DeviationScores);

public sealed record SchoolLifeStatus(string Status, string Summary)
{
    public string StatusLabel => Status switch
    {
        "required" => "指定あり",
        "none" => "指定なし",
        "optional" => "標準服あり",
        "available" => "利用可",
        "unavailable" => "なし",
        _ => "未確認"
    };
}

public sealed record SchoolLifeEvent(string Name, string Season);

public sealed record SchoolClub(
    string Name,
    string Category,
    string Gender,
    string ActivityDays,
    string? ActivityPlace)
{
    public string CategoryLabel => Category == "sports" ? "運動系" : "文化系";
    public string GenderLabel => Gender switch
    {
        "boys" => "男子",
        "girls" => "女子",
        "mixed" => "男女",
        _ => "区分未確認"
    };
}

public sealed record SchoolLifeInfo(
    string SchoolId,
    string VerifiedAt,
    SchoolLifeStatus Uniform,
    string RulesSummary,
    SchoolLifeStatus Lunch,
    IReadOnlyList<SchoolLifeEvent> Events,
    IReadOnlyList<SchoolClub> Clubs,
    IReadOnlyList<SchoolDataSource> Sources);

public sealed class SchoolComparisonViewModel
{
    public required IReadOnlyList<SchoolDetailsViewModel> Schools { get; init; }
    public int MaximumSchools { get; init; } = 3;
}

public sealed class SchoolCommuteViewModel
{
    public required IReadOnlyList<SchoolDetailsViewModel> Schools { get; init; }
    public int MaximumSchools { get; init; } = 5;
}

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
    public required IReadOnlyList<SchoolFilterOption> Genders { get; init; }
    public required IReadOnlyList<SchoolFilterOption> AttendanceTypes { get; init; }
    public required IReadOnlyList<SchoolFilterOption> DepartmentCategories { get; init; }
    public required SchoolSearchCriteria Criteria { get; init; }

    public string Query => Criteria.Query;
    public string Prefecture => Criteria.Prefecture;
    public string Ownership => Criteria.Ownership;
    public bool HasAdvancedFilters =>
        !string.IsNullOrEmpty(Criteria.Gender) ||
        !string.IsNullOrEmpty(Criteria.AttendanceType) ||
        !string.IsNullOrEmpty(Criteria.DepartmentCategory) ||
        !string.IsNullOrEmpty(Criteria.Recruitment) ||
        Criteria.HasAdmissions || Criteria.HasExamSchedule || Criteria.HasVisitEvents || Criteria.HasSchoolLife;

    public bool HasAnyFilters =>
        !string.IsNullOrEmpty(Criteria.Query) ||
        !string.IsNullOrEmpty(Criteria.Prefecture) ||
        !string.IsNullOrEmpty(Criteria.Ownership) ||
        HasAdvancedFilters;

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
