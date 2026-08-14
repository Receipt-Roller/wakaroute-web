using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Models;
using wakaroute_web.Services.Schools;

namespace wakaroute_web.Controllers;

[ApiController]
[Route("api/schools")]
public sealed class SchoolApiController(ISchoolCatalog schoolCatalog) : ControllerBase
{
    [HttpGet]
    public IActionResult Get(
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
        int page = 1,
        int pageSize = 24)
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
        var result = schoolCatalog.Search(criteria, page, pageSize);
        return Ok(new
        {
            schoolCatalog.Metadata.AsOf,
            Criteria = criteria,
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages,
            result.Items
        });
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var school = schoolCatalog.GetById(id);
        return school is null ? NotFound() : Ok(school);
    }
}
