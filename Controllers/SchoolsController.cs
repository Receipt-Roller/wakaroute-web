using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Models;
using wakaroute_web.Services.Schools;

namespace wakaroute_web.Controllers;

public sealed class SchoolsController(ISchoolCatalog schoolCatalog) : Controller
{
    [HttpGet("schools")]
    public IActionResult Index(
        string? q,
        string? prefecture,
        string? ownership,
        string? gender,
        string? attendanceType,
        string? department,
        string? recruitment,
        bool hasAdmissions = false,
        bool hasExamSchedule = false,
        bool hasVisitEvents = false,
        bool hasSchoolLife = false,
        string? sort = null,
        int page = 1)
    {
        var criteria = new SchoolSearchCriteria(
            q?.Trim() ?? string.Empty,
            prefecture?.Trim() ?? string.Empty,
            ownership?.Trim().ToLowerInvariant() ?? string.Empty,
            gender?.Trim().ToLowerInvariant() ?? string.Empty,
            attendanceType?.Trim().ToLowerInvariant() ?? string.Empty,
            department?.Trim().ToLowerInvariant() ?? string.Empty,
            recruitment?.Trim().ToLowerInvariant() ?? string.Empty,
            hasAdmissions,
            hasExamSchedule,
            hasVisitEvents,
            hasSchoolLife,
            sort?.Trim().ToLowerInvariant() ?? "relevance");
        var results = schoolCatalog.Search(criteria, page, 24);
        return View(new SchoolSearchViewModel
        {
            Results = results,
            Metadata = schoolCatalog.Metadata,
            Prefectures = schoolCatalog.Prefectures,
            Genders = schoolCatalog.Genders,
            AttendanceTypes = schoolCatalog.AttendanceTypes,
            DepartmentCategories = schoolCatalog.DepartmentCategories,
            Criteria = criteria
        });
    }

    [HttpGet("schools/{id}")]
    public IActionResult Details(string id)
    {
        var school = schoolCatalog.GetById(id);
        return school is null ? NotFound() : View(school);
    }

    [HttpGet("schools/compare")]
    public IActionResult Compare(string? ids)
    {
        var schoolIds = (ids ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return View(new SchoolComparisonViewModel
        {
            Schools = schoolCatalog.GetByIds(schoolIds, 3)
        });
    }

    [HttpGet("schools/saved")]
    public IActionResult Saved() => View();

    [HttpGet("schools/commute")]
    public IActionResult Commute(string? ids)
    {
        var schoolIds = (ids ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return View(new SchoolCommuteViewModel
        {
            Schools = schoolCatalog.GetByIds(schoolIds, 5)
        });
    }
}
