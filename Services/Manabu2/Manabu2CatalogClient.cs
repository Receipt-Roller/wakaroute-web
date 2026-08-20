using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace wakaroute_web.Services.Manabu2;

public sealed class Manabu2CatalogClient
{
    public const string HttpClientName = "Manabu2";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly Manabu2Options _options;

    public Manabu2CatalogClient(IHttpClientFactory httpClientFactory, IOptions<Manabu2Options> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<IReadOnlyList<Manabu2Path>> GetPathsAsync(CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var httpClient = CreateClient();

        var organizationId = Uri.EscapeDataString(_options.OrganizationId);
        var page = await httpClient.GetFromJsonAsync<Manabu2PathPage>(
            $"api/v1/paths?organizationId={organizationId}&culture=ja-JP&pageSize=100",
            cancellationToken) ?? throw new InvalidOperationException("Manabu2 returned an empty path response.");

        var details = await Task.WhenAll(page.Items.Select(path =>
            httpClient.GetFromJsonAsync<Manabu2Path>(
                $"api/v1/paths/{Uri.EscapeDataString(path.Id)}",
                cancellationToken)));

        return details
            .Where(path => path is not null)
            .Cast<Manabu2Path>()
            .ToArray();
    }

    public async Task<IReadOnlyList<Manabu2TestSummary>> GetTestsAsync(
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return [];
        }

        var httpClient = CreateClient();
        var organizationId = Uri.EscapeDataString(_options.OrganizationId);

        return await httpClient.GetFromJsonAsync<IReadOnlyList<Manabu2TestSummary>>(
            $"api/v1/organizations/{organizationId}/tests",
            cancellationToken) ?? [];
    }

    public async Task<Manabu2CourseDetail?> GetCourseAsync(
        string courseId,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return null;
        }

        var httpClient = CreateClient();
        var course = await httpClient.GetFromJsonAsync<Manabu2CourseDetail>(
            $"api/v1/courses/{Uri.EscapeDataString(courseId)}",
            cancellationToken);

        if (course is not null &&
            !string.Equals(course.OrganizationId, _options.OrganizationId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The requested course does not belong to the configured organization.");
        }

        return course;
    }

    public async Task<Manabu2TestDetail?> GetTestAsync(
        string testId,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return null;
        }

        var httpClient = CreateClient();
        var test = await httpClient.GetFromJsonAsync<Manabu2TestDetail>(
            $"api/v1/tests/{Uri.EscapeDataString(testId)}",
            cancellationToken);

        if (test is not null &&
            !string.Equals(test.OrganizationId, _options.OrganizationId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The requested test does not belong to the configured organization.");
        }

        return test;
    }

    public async Task<Manabu2LessonDetail?> GetLessonAsync(
        string lessonId,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            return null;
        }

        var httpClient = CreateClient();
        var lesson = await httpClient.GetFromJsonAsync<Manabu2LessonDetail>(
            $"api/v1/lessons/{Uri.EscapeDataString(lessonId)}",
            cancellationToken);

        if (lesson is null || lesson.Materials is not { Count: > 0 })
        {
            return lesson;
        }

        var materialDetails = await Task.WhenAll(
            lesson.Materials
                .OrderBy(material => material.OrderIndex)
                .Select(material => httpClient.GetFromJsonAsync<Manabu2MaterialDetail>(
                    $"api/v1/materials/{Uri.EscapeDataString(material.Id)}",
                    cancellationToken)));

        return lesson with
        {
            MaterialDetails = materialDetails
                .Where(material => material is not null)
                .Cast<Manabu2MaterialDetail>()
                .OrderBy(material => material.OrderIndex)
                .ToArray()
        };
    }

    private HttpClient CreateClient()
    {
        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.BaseAddress = new Uri(_options.BaseUrl.TrimEnd('/') + "/");
        httpClient.Timeout = TimeSpan.FromSeconds(15);
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", _options.ApiKey);
        return httpClient;
    }
}

public sealed record Manabu2PathPage(IReadOnlyList<Manabu2PathSummary> Items);

public sealed record Manabu2PathSummary(string Id, string Name);

public sealed record Manabu2Path(
    string Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Labels,
    IReadOnlyList<Manabu2Course> Courses);

public sealed record Manabu2Course(
    string Id,
    string Title,
    string? Description,
    int SectionCount,
    int LessonCount);

public sealed record Manabu2TestSummary(
    string Id,
    string OrganizationId,
    string? PathId,
    string Title,
    string? Description,
    int PassingScorePercent,
    bool IsRequired,
    string Culture,
    int QuestionCount,
    int? TimeLimitSeconds);

public sealed record Manabu2TestDetail(
    string Id,
    string OrganizationId,
    string? PathId,
    string Title,
    string? Description,
    int PassingScorePercent,
    bool IsRequired,
    string Culture,
    int? TimeLimitSeconds,
    IReadOnlyList<Manabu2TestQuestion> Questions);

public sealed record Manabu2TestQuestion(
    string Id,
    string QuestionText,
    string QuestionType,
    int OrderIndex,
    IReadOnlyList<Manabu2TestOption> Options);

public sealed record Manabu2TestOption(
    string Id,
    string OptionText,
    string? ImageUrl,
    int OrderIndex);

public sealed record Manabu2CourseDetail(
    string Id,
    string OrganizationId,
    string Title,
    string? Description,
    IReadOnlyList<Manabu2Section> Sections);

public sealed record Manabu2Section(
    string Id,
    string Title,
    string? Summary,
    int OrderIndex,
    IReadOnlyList<Manabu2Lesson> Lessons);

public sealed record Manabu2Lesson(
    string Id,
    string Title,
    string? Summary,
    int OrderIndex);

public sealed record Manabu2LessonDetail(
    string Id,
    string SectionId,
    string CourseId,
    string Title,
    string? Summary,
    string BodyHtml,
    IReadOnlyList<Manabu2MaterialSummary>? Materials,
    Manabu2Quiz? Quiz)
{
    public IReadOnlyList<Manabu2MaterialDetail> MaterialDetails { get; init; } = [];
}

public sealed record Manabu2MaterialSummary(
    string Id,
    string? Summary,
    string? FileName,
    string? FileContentType,
    int OrderIndex);

public sealed record Manabu2MaterialDetail(
    string Id,
    string OrganizationId,
    string? Summary,
    string? FileName,
    string? FileContentType,
    string? FileUrl,
    string ContentType,
    string? ContentHtml,
    int OrderIndex);

public sealed record Manabu2Quiz(
    string Id,
    string Title,
    string? Instructions,
    int PassingScorePercent,
    IReadOnlyList<Manabu2QuizQuestion> Questions);

public sealed record Manabu2QuizQuestion(
    string Id,
    string QuestionText,
    int OrderIndex,
    IReadOnlyList<Manabu2QuizOption> Options);

public sealed record Manabu2QuizOption(
    string Id,
    string Text,
    int OrderIndex);
