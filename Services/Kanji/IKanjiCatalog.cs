using wakaroute_web.Models;

namespace wakaroute_web.Services.Kanji;

public interface IKanjiCatalog
{
    KanjiDataset Dataset { get; }
    KanjiEntry? GetById(string id);
    KanjiSearchResult Search(int? recommendedGrade, string? query, int page, int pageSize);
}
