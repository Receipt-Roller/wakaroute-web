using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Models;
using wakaroute_web.Services.StudyCards;
namespace wakaroute_web.Controllers;
[Route("study-cards")]
public sealed class StudyCardsController(IStudyCardCatalog catalog):Controller
{
    [HttpGet("{subject}")]
    public IActionResult Index(string subject)
    {
        var definition=catalog.Dataset.Subjects.FirstOrDefault(x=>x.Id.Equals(subject,StringComparison.OrdinalIgnoreCase));
        return definition is null?NotFound():View(new StudyCardIndexViewModel{Dataset=catalog.Dataset,Subject=definition});
    }
    [HttpGet("{subject}/cards")]
    public IActionResult Cards(string subject,string? domain=null,int? grade=null,string? cardType=null,string? q=null,int page=1)
    {
        var definition=catalog.Dataset.Subjects.FirstOrDefault(x=>x.Id.Equals(subject,StringComparison.OrdinalIgnoreCase));
        if(definition is null||grade is not null and (<1 or >3)) return NotFound();
        if(!string.IsNullOrWhiteSpace(domain)&&definition.Domains.All(x=>x.Id!=domain)) return NotFound();
        return View(new StudyCardListViewModel{Dataset=catalog.Dataset,Subject=definition,Domain=domain??"",Grade=grade,CardType=cardType??"",Query=q?.Trim()??"",Result=catalog.Search(subject,domain,grade,cardType,q,page,24)});
    }
}
