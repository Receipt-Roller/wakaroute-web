using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Services.Schools;

namespace wakaroute_web.Controllers;

[ApiController]
[Route("api/schools")]
public sealed class SchoolApiController(ISchoolCatalog schoolCatalog) : ControllerBase
{
    [HttpGet]
    public IActionResult Get(string? q, string? prefecture, string? ownership, int page = 1, int pageSize = 24)
    {
        var result = schoolCatalog.Search(q, prefecture, ownership, page, pageSize);
        return Ok(new
        {
            schoolCatalog.Metadata.AsOf,
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages,
            result.Items
        });
    }
}
