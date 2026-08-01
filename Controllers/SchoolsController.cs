using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Models;
using wakaroute_web.Services.Schools;

namespace wakaroute_web.Controllers;

public sealed class SchoolsController(ISchoolCatalog schoolCatalog) : Controller
{
    [HttpGet("schools")]
    public IActionResult Index(string? q, string? prefecture, string? ownership, int page = 1)
    {
        var results = schoolCatalog.Search(q, prefecture, ownership, page, 24);
        return View(new SchoolSearchViewModel
        {
            Results = results,
            Metadata = schoolCatalog.Metadata,
            Prefectures = schoolCatalog.Prefectures,
            Query = q?.Trim() ?? string.Empty,
            Prefecture = prefecture?.Trim() ?? string.Empty,
            Ownership = ownership?.Trim() ?? string.Empty
        });
    }
}
