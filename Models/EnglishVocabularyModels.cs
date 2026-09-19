namespace wakaroute_web.Models;

public sealed record EnglishVocabularyDataset(
    int SchemaVersion,
    string DatasetVersion,
    string AsOf,
    string Language,
    IReadOnlyList<EnglishVocabularyLicense> Licenses,
    IReadOnlyList<EnglishVocabularySource> Sources,
    EnglishVocabularyAssignment Assignment,
    EnglishVocabularyOfficialGoals OfficialLearningGoals,
    IReadOnlyList<EnglishVocabularySkill> LearningSkills,
    EnglishVocabularyQuality Quality,
    IReadOnlyList<EnglishWord> Items);

public sealed record EnglishVocabularyLicense(string Name, string SpdxId, string Url, string Attribution);
public sealed record EnglishVocabularySource(string Id, string Name, string Url, string Role, string? Version);
public sealed record EnglishVocabularyAssignment(string Classification, bool OfficialGradeAssignment, int ElementaryBaselineCount, int JuniorHighNewWordCount, string Method, IReadOnlyDictionary<string, int> GradeCounts);
public sealed record EnglishVocabularyOfficialGoals(string ElementaryWordRange, string JuniorHighNewWordRange, string CumulativeWordRange);
public sealed record EnglishVocabularySkill(string Id, string Name, string Description);
public sealed record EnglishVocabularyQuality(int MeaningCoverageCount, int MissingMeaningCount, string Note);
public sealed record EnglishWord(string Id, string Lemma, string Stage, int? RecommendedGrade, int Sequence, int FrequencyRank, IReadOnlyList<string> PartsOfSpeech, IReadOnlyList<string> MeaningsJa, IReadOnlyList<string> Pronunciations);
public sealed record EnglishWordSearchResult(IReadOnlyList<EnglishWord> Items, int TotalCount, int Page, int PageSize, int TotalPages);

public sealed class EnglishWordsIndexViewModel
{
    public required EnglishVocabularyDataset Dataset { get; init; }
}

public sealed class EnglishWordsGradeViewModel
{
    public required EnglishVocabularyDataset Dataset { get; init; }
    public required int Grade { get; init; }
    public required string Query { get; init; }
    public required EnglishWordSearchResult Result { get; init; }
}
