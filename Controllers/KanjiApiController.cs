using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Services.Kanji;

namespace wakaroute_web.Controllers;

[ApiController]
[Route("api/v1/kanji")]
public sealed class KanjiApiController(IKanjiCatalog kanjiCatalog) : ControllerBase
{
    [HttpGet]
    public IActionResult Get(
        int? recommendedGrade = null,
        string? q = null,
        int page = 1,
        int pageSize = 100)
    {
        if (recommendedGrade is not null and (< 1 or > 3))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(recommendedGrade)] = ["recommendedGrade must be 1, 2, or 3."]
            }));

        var result = kanjiCatalog.Search(recommendedGrade, q, page, pageSize);
        AddDatasetHeaders();
        return Ok(new
        {
            kanjiCatalog.Dataset.DatasetVersion,
            kanjiCatalog.Dataset.AsOf,
            RecommendedGrade = recommendedGrade,
            Query = q?.Trim() ?? string.Empty,
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages,
            result.Items
        });
    }

    [HttpGet("dataset")]
    public IActionResult GetDataset()
    {
        if (Request.Headers.IfNoneMatch.Contains(CurrentETag()))
            return StatusCode(StatusCodes.Status304NotModified);

        AddDatasetHeaders(includeEntityTag: true);
        return Ok(kanjiCatalog.Dataset);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var item = kanjiCatalog.GetById(id);
        if (item is null)
            return NotFound();

        AddDatasetHeaders();
        return Ok(item);
    }

    private void AddDatasetHeaders(bool includeEntityTag = false)
    {
        Response.Headers.CacheControl = "public,max-age=3600";
        Response.Headers.Append("Link", $"<{kanjiCatalog.Dataset.License.Url}>; rel=\"license\"");
        Response.Headers.Append("X-Data-License", kanjiCatalog.Dataset.License.SpdxId);
        if (includeEntityTag)
            Response.Headers.ETag = CurrentETag();
    }

    private string CurrentETag() => $"\"kanji-{kanjiCatalog.Dataset.DatasetVersion}\"";
}
