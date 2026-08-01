using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Models;
using wakaroute_web.Services.UnderstandingMaps;

namespace wakaroute_web.Controllers;

[Route("understanding-map")]
public sealed class UnderstandingMapsController : Controller
{
    private readonly IReadOnlyDictionary<string, IUnderstandingMapProvider> _providers;

    public UnderstandingMapsController(IEnumerable<IUnderstandingMapProvider> providers)
    {
        _providers = providers.ToDictionary(provider => provider.SubjectId, StringComparer.OrdinalIgnoreCase);
    }

    [HttpGet("math")]
    public IActionResult Math() => View(GetMap("math"));

    [HttpGet("japanese")]
    public IActionResult Japanese() => View("Math", GetMap("japanese"));

    [HttpGet("english")]
    public IActionResult English() => View("Math", GetMap("english"));

    [HttpGet("science")]
    public IActionResult Science() => View("Math", GetMap("science"));

    [HttpGet("social-studies")]
    public IActionResult SocialStudies() => View("Math", GetMap("social-studies"));

    private UnderstandingMapViewModel GetMap(string subjectId) => _providers[subjectId].GetMap();
}
