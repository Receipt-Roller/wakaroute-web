using wakaroute_web.Models;
namespace wakaroute_web.Services.StudyCards;
public interface IStudyCardCatalog { StudyCardDataset Dataset { get; } StudyCard? GetById(string id); StudyCardSearchResult Search(string? subject,string? domain,int? grade,string? cardType,string? query,int page,int pageSize); }
