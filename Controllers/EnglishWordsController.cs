using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Models;
using wakaroute_web.Services.EnglishVocabulary;

namespace wakaroute_web.Controllers;

[Route("english-words")]
public sealed class EnglishWordsController(IEnglishVocabularyCatalog catalog) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View(new EnglishWordsIndexViewModel { Dataset = catalog.Dataset });

    [HttpGet("junior-high/grade-{grade:int}")]
    public IActionResult Grade(int grade, string? q = null, int page = 1)
    {
        if (grade is < 1 or > 3) return NotFound();
        return View(new EnglishWordsGradeViewModel
        {
            Dataset = catalog.Dataset,
            Grade = grade,
            Query = q?.Trim() ?? string.Empty,
            Result = catalog.Search("junior-high", grade, q, page, 60)
        });
    }
}
