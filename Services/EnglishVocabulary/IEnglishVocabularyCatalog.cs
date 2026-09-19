using wakaroute_web.Models;

namespace wakaroute_web.Services.EnglishVocabulary;

public interface IEnglishVocabularyCatalog
{
    EnglishVocabularyDataset Dataset { get; }
    EnglishWord? GetById(string id);
    EnglishWordSearchResult Search(string? stage, int? recommendedGrade, string? query, int page, int pageSize);
}
