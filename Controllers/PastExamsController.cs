using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Models;
using wakaroute_web.Services.PastExams;

namespace wakaroute_web.Controllers;

public sealed class PastExamsController(IPastExamCatalog pastExamCatalog) : Controller
{
    [HttpGet("past-exams")]
    public IActionResult Index() => View(new PastExamIndexViewModel
    {
        Sources = pastExamCatalog.Sources,
        AsOf = pastExamCatalog.AsOf
    });
}
