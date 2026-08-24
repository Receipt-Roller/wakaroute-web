using wakaroute_web.Models;

namespace wakaroute_web.Services.PastExams;

public interface IPastExamCatalog
{
    string AsOf { get; }
    IReadOnlyList<OfficialPastExamSource> Sources { get; }
    IReadOnlyList<OfficialPastExamSource> GetByIds(IEnumerable<string> ids);
}
