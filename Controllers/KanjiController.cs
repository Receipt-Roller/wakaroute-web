using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Models;
using wakaroute_web.Services.Kanji;

namespace wakaroute_web.Controllers;

[Route("kanji")]
public sealed class KanjiController(IKanjiCatalog kanjiCatalog) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(new KanjiIndexViewModel
    {
        Dataset = kanjiCatalog.Dataset
    });

    [HttpGet("junior-high")]
    public IActionResult JuniorHigh() => RedirectToActionPermanent(nameof(Index));

    [HttpGet("junior-high/grade-{grade:int}")]
    public IActionResult Grade(int grade, string? q = null, int page = 1)
    {
        if (grade is < 1 or > 3)
            return NotFound();

        return View(new KanjiGradeViewModel
        {
            Dataset = kanjiCatalog.Dataset,
            Grade = grade,
            Query = q?.Trim() ?? string.Empty,
            Result = kanjiCatalog.Search(grade, q, page, 60)
        });
    }
}
