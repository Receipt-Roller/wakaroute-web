using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Services.StudyCards;
namespace wakaroute_web.Controllers;
[ApiController]
[Route("api/v1/study-cards")]
public sealed class StudyCardsApiController(IStudyCardCatalog catalog):ControllerBase
{
    [HttpGet]
    public IActionResult Get(string? subject=null,string? domain=null,int? recommendedGrade=null,string? cardType=null,string? q=null,int page=1,int pageSize=100)
    {
        if(subject is not null&&catalog.Dataset.Subjects.All(x=>x.Id!=subject)) return BadRequest(new ValidationProblemDetails(new Dictionary<string,string[]>{{nameof(subject),["Unknown subject."]}}));
        if(recommendedGrade is not null and (<1 or >3)) return BadRequest(new ValidationProblemDetails(new Dictionary<string,string[]>{{nameof(recommendedGrade),["recommendedGrade must be 1, 2, or 3."]}}));
        var result=catalog.Search(subject,domain,recommendedGrade,cardType,q,page,pageSize); AddHeaders();
        return Ok(new{catalog.Dataset.DatasetVersion,catalog.Dataset.AsOf,Subject=subject,Domain=domain,RecommendedGrade=recommendedGrade,CardType=cardType,Query=q?.Trim()??"",result.TotalCount,result.Page,result.PageSize,result.TotalPages,result.Items});
    }
    [HttpGet("dataset")]
    public IActionResult Dataset(){if(Request.Headers.IfNoneMatch.Contains(ETag()))return StatusCode(304);AddHeaders(true);return Ok(catalog.Dataset);}
    [HttpGet("{id}")]
    public IActionResult ById(string id){var item=catalog.GetById(id);if(item is null)return NotFound();AddHeaders();return Ok(item);}
    private void AddHeaders(bool etag=false){Response.Headers.CacheControl="public,max-age=3600";Response.Headers.Append("Link",$"<{catalog.Dataset.License.Url}>; rel=\"license\"");Response.Headers.Append("X-Data-License",catalog.Dataset.License.SpdxId);if(etag)Response.Headers.ETag=ETag();}
    private string ETag()=>$"\"study-cards-{catalog.Dataset.DatasetVersion}\"";
}
