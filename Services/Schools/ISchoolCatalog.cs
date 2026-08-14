using wakaroute_web.Models;

namespace wakaroute_web.Services.Schools;

public interface ISchoolCatalog
{
    SchoolCatalogMetadata Metadata { get; }
    IReadOnlyList<SchoolFilterOption> Prefectures { get; }
    IReadOnlyList<SchoolFilterOption> Genders { get; }
    IReadOnlyList<SchoolFilterOption> AttendanceTypes { get; }
    IReadOnlyList<SchoolFilterOption> DepartmentCategories { get; }
    SchoolSearchPage Search(SchoolSearchCriteria criteria, int page, int pageSize);
    SchoolDetailsViewModel? GetById(string id);
    IReadOnlyList<SchoolDetailsViewModel> GetByIds(IEnumerable<string> ids, int maximumCount);
}
