using System.Globalization;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using wakaroute_web.Services.Manabu2;
using wakaroute_web.Services.Schools;
using wakaroute_web.Services.UnderstandingMaps;

namespace wakaroute_web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class SeoController(
    ISchoolCatalog schoolCatalog,
    IUnderstandingMapCatalog understandingMapCatalog,
    Manabu2CatalogClient manabu2,
    IMemoryCache cache,
    IConfiguration configuration,
    ILogger<SeoController> logger) : Controller
{
    private const string SitemapCacheKey = "seo-sitemap-v1";
    private static readonly string[] SubjectIds = ["math", "japanese", "english", "science", "social-studies"];
    private static readonly string[] StaticPaths =
    [
        "/",
        "/high-school-exam",
        "/schools",
        "/understanding-map/math",
        "/understanding-map/japanese",
        "/understanding-map/english",
        "/understanding-map/science",
        "/understanding-map/social-studies",
        "/for-parents",
        "/company",
        "/terms",
        "/privacy"
    ];

    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        if (!cache.TryGetValue(SitemapCacheKey, out string? xml) || xml is null)
        {
            xml = await BuildSitemapAsync(cancellationToken);
            cache.Set(SitemapCacheKey, xml, TimeSpan.FromHours(1));
        }

        return Content(xml, "application/xml; charset=utf-8", Encoding.UTF8);
    }

    private async Task<string> BuildSitemapAsync(CancellationToken cancellationToken)
    {
        var baseUrl = (configuration["Site:BaseUrl"] ?? "https://wakaroute.com").TrimEnd('/');
        var urls = new Dictionary<string, DateOnly?>(StringComparer.Ordinal);

        foreach (var path in StaticPaths)
        {
            urls[$"{baseUrl}{path}"] = null;
        }

        foreach (var school in schoolCatalog.Schools)
        {
            urls[$"{baseUrl}/schools/{Uri.EscapeDataString(school.Id)}"] = ParseDate(school.LastVerifiedAt);
        }

        await AddLearningContentAsync(urls, baseUrl, cancellationToken);

        var settings = new XmlWriterSettings
        {
            Async = true,
            Encoding = new UTF8Encoding(false),
            Indent = true,
            OmitXmlDeclaration = false
        };
        await using var output = new MemoryStream();
        await using var writer = XmlWriter.Create(output, settings);
        await writer.WriteStartDocumentAsync();
        await writer.WriteStartElementAsync(null, "urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

        foreach (var entry in urls.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            await writer.WriteStartElementAsync(null, "url", null);
            await writer.WriteElementStringAsync(null, "loc", null, entry.Key);
            if (entry.Value is { } lastModified)
            {
                await writer.WriteElementStringAsync(null, "lastmod", null, lastModified.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            }
            await writer.WriteEndElementAsync();
        }

        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private async Task AddLearningContentAsync(
        IDictionary<string, DateOnly?> urls,
        string baseUrl,
        CancellationToken cancellationToken)
    {
        if (!manabu2.IsConfigured)
        {
            return;
        }

        try
        {
            var courseEntries = new List<(string SubjectId, string CourseId)>();
            foreach (var subjectId in SubjectIds)
            {
                var map = await understandingMapCatalog.GetMapAsync(subjectId, cancellationToken);
                courseEntries.AddRange(map.Areas
                    .SelectMany(area => area.Nodes)
                    .Where(node => !string.IsNullOrWhiteSpace(node.CourseId))
                    .Select(node => (subjectId, node.CourseId!)));
            }

            var distinctCourses = courseEntries.Distinct().ToArray();
            foreach (var entry in distinctCourses)
            {
                var coursePath = $"/understanding-map/{entry.SubjectId}/courses/{Uri.EscapeDataString(entry.CourseId)}";
                urls[$"{baseUrl}{coursePath}"] = null;
            }

            using var concurrency = new SemaphoreSlim(4);
            var courseTasks = distinctCourses.Select(async entry =>
            {
                await concurrency.WaitAsync(cancellationToken);
                try
                {
                    return (Entry: entry, Course: await manabu2.GetCourseAsync(entry.CourseId, cancellationToken));
                }
                finally
                {
                    concurrency.Release();
                }
            });

            foreach (var result in await Task.WhenAll(courseTasks))
            {
                var course = result.Course;
                if (course is null)
                {
                    continue;
                }

                var coursePath = $"/understanding-map/{result.Entry.SubjectId}/courses/{Uri.EscapeDataString(result.Entry.CourseId)}";
                foreach (var lesson in course.Sections.SelectMany(section => section.Lessons))
                {
                    urls[$"{baseUrl}{coursePath}/lessons/{Uri.EscapeDataString(lesson.Id)}"] = null;
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Could not add Manabu2 learning content to the sitemap.");
        }
    }

    private static DateOnly? ParseDate(string value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
}
