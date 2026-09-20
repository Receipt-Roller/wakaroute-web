namespace wakaroute_web.Models;

public sealed record StudyCardDataset(int SchemaVersion,string DatasetVersion,string AsOf,string Language,StudyCardLicense License,IReadOnlyList<StudyCardSubject> Subjects,string Classification,bool OfficialGradeAssignment,string EditorialNote,IReadOnlyList<StudyCard> Items);
public sealed record StudyCardLicense(string Name,string SpdxId,string Url,string Attribution);
public sealed record StudyCardSubject(string Id,string Name,IReadOnlyList<StudyCardDomain> Domains,string CurriculumSourceUrl);
public sealed record StudyCardDomain(string Id,string Name);
public sealed record StudyCard(string Id,string Subject,string Domain,int RecommendedGrade,string CardType,string Prompt,string Answer,string Explanation,IReadOnlyList<string> Tags,IReadOnlyList<string> SourceRefs);
public sealed record StudyCardSearchResult(IReadOnlyList<StudyCard> Items,int TotalCount,int Page,int PageSize,int TotalPages);
public sealed class StudyCardIndexViewModel { public required StudyCardDataset Dataset { get; init; } public required StudyCardSubject Subject { get; init; } public int CardCount => Dataset.Items.Count(item => item.Subject == Subject.Id); }
public sealed class StudyCardListViewModel { public required StudyCardDataset Dataset { get; init; } public required StudyCardSubject Subject { get; init; } public required string Domain { get; init; } public required string CardType { get; init; } public required int? Grade { get; init; } public required string Query { get; init; } public required StudyCardSearchResult Result { get; init; } }
