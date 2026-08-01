using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Models;

namespace wakaroute_web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("high-school-exam")]
    public IActionResult HighSchoolExam()
    {
        return View();
    }

    [HttpGet("company")]
    public IActionResult Company()
    {
        return View();
    }

    [HttpGet("for-parents")]
    public IActionResult ForParents()
    {
        return View();
    }

    [HttpGet("terms")]
    public IActionResult Terms()
    {
        return View();
    }

    [HttpGet("privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
