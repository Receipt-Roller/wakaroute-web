using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Services.EnglishVocabulary;

namespace wakaroute_web.Controllers;

[ApiController]
[Route("api/v1/english-words")]
public sealed class EnglishWordsApiController(IEnglishVocabularyCatalog catalog) : ControllerBase
{
    [HttpGet]
    public IActionResult Get(string? stage = null, int? recommendedGrade = null, string? q = null, int page = 1, int pageSize = 100)
    {
        if (stage is not null && stage is not ("elementary-review" or "junior-high")) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(stage)] = ["stage must be elementary-review or junior-high."] }));
        if (recommendedGrade is not null and (< 1 or > 3)) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(recommendedGrade)] = ["recommendedGrade must be 1, 2, or 3."] }));
        var result = catalog.Search(stage, recommendedGrade, q, page, pageSize);
        AddDatasetHeaders();
        return Ok(new { catalog.Dataset.DatasetVersion, catalog.Dataset.AsOf, Stage = stage, RecommendedGrade = recommendedGrade, Query = q?.Trim() ?? string.Empty, result.TotalCount, result.Page, result.PageSize, result.TotalPages, result.Items });
    }

    [HttpGet("dataset")]
    public IActionResult GetDataset()
    {
        if (Request.Headers.IfNoneMatch.Contains(CurrentETag())) return StatusCode(StatusCodes.Status304NotModified);
        AddDatasetHeaders(true);
        return Ok(catalog.Dataset);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var item = catalog.GetById(id);
        if (item is null) return NotFound();
        AddDatasetHeaders();
        return Ok(item);
    }

    private void AddDatasetHeaders(bool includeEntityTag = false)
    {
        Response.Headers.CacheControl = "public,max-age=3600";
        foreach (var license in catalog.Dataset.Licenses) Response.Headers.Append("Link", $"<{license.Url}>; rel=\"license\"");
        Response.Headers.Append("X-Data-License", string.Join(", ", catalog.Dataset.Licenses.Select(license => license.SpdxId)));
        if (includeEntityTag) Response.Headers.ETag = CurrentETag();
    }

    private string CurrentETag() => $"\"english-words-{catalog.Dataset.DatasetVersion}\"";
}
