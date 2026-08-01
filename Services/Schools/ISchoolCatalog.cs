using wakaroute_web.Models;

namespace wakaroute_web.Services.Schools;

public interface ISchoolCatalog
{
    SchoolCatalogMetadata Metadata { get; }
    IReadOnlyList<SchoolFilterOption> Prefectures { get; }
    SchoolSearchPage Search(string? query, string? prefecture, string? ownership, int page, int pageSize);
}
