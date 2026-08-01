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
