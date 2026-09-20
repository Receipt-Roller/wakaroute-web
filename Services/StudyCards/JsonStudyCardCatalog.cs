using System.Globalization;
using System.Text.Json;
using wakaroute_web.Models;
namespace wakaroute_web.Services.StudyCards;
public sealed class JsonStudyCardCatalog : IStudyCardCatalog
{
    private readonly Dictionary<string,StudyCard> _byId;
    public JsonStudyCardCatalog(IHostEnvironment environment)
    {
        var path=Path.Combine(environment.ContentRootPath,"Data","StudyCards","study-cards.json");
        Dataset=JsonSerializer.Deserialize<StudyCardDataset>(File.ReadAllText(path),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})??throw new InvalidOperationException("Study card dataset is invalid.");
        Validate(Dataset); _byId=Dataset.Items.ToDictionary(x=>x.Id,StringComparer.OrdinalIgnoreCase);
    }
    public StudyCardDataset Dataset { get; }
    public StudyCard? GetById(string id)=>string.IsNullOrWhiteSpace(id)?null:_byId.GetValueOrDefault(id.Trim());
    public StudyCardSearchResult Search(string? subject,string? domain,int? grade,string? cardType,string? query,int page,int pageSize)
    {
        page=Math.Max(1,page); pageSize=Math.Clamp(pageSize,1,200); IEnumerable<StudyCard> items=Dataset.Items;
        if(!string.IsNullOrWhiteSpace(subject)) items=items.Where(x=>x.Subject.Equals(subject,StringComparison.OrdinalIgnoreCase));
        if(!string.IsNullOrWhiteSpace(domain)) items=items.Where(x=>x.Domain.Equals(domain,StringComparison.OrdinalIgnoreCase));
        if(grade is not null) items=items.Where(x=>x.RecommendedGrade==grade);
        if(!string.IsNullOrWhiteSpace(cardType)) items=items.Where(x=>x.CardType.Equals(cardType,StringComparison.OrdinalIgnoreCase));
        if(!string.IsNullOrWhiteSpace(query)){var q=query.Trim();items=items.Where(x=>x.Prompt.Contains(q,StringComparison.OrdinalIgnoreCase)||x.Answer.Contains(q,StringComparison.OrdinalIgnoreCase)||x.Explanation.Contains(q,StringComparison.OrdinalIgnoreCase)||x.Tags.Any(t=>t.Contains(q,StringComparison.OrdinalIgnoreCase)));}
        var matches=items.ToArray(); var pages=Math.Max(1,(int)Math.Ceiling(matches.Length/(double)pageSize)); page=Math.Min(page,pages);
        return new(matches.Skip((page-1)*pageSize).Take(pageSize).ToArray(),matches.Length,page,pageSize,pages);
    }
    private static void Validate(StudyCardDataset d)
    {
        if(d.SchemaVersion!=1||d.OfficialGradeAssignment||!DateOnly.TryParse(d.AsOf,CultureInfo.InvariantCulture,DateTimeStyles.None,out _)) throw new InvalidOperationException("Study card metadata is invalid.");
        if(d.Items.Count<64||d.Items.Select(x=>x.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=d.Items.Count) throw new InvalidOperationException("Study card count or IDs are invalid.");
        foreach(var subject in d.Subjects) foreach(var domain in subject.Domains) if(d.Items.Count(x=>x.Subject==subject.Id&&x.Domain==domain.Id)<8) throw new InvalidOperationException($"Domain {domain.Id} needs at least 8 cards.");
    }
}
