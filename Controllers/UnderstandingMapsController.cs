using Microsoft.AspNetCore.Mvc;
using wakaroute_web.Models;
using wakaroute_web.Services.Manabu2;
using wakaroute_web.Services.UnderstandingMaps;

namespace wakaroute_web.Controllers;

[Route("understanding-map")]
public sealed class UnderstandingMapsController : Controller
{
    private static readonly string[] SubjectIds =
        ["math", "japanese", "english", "science", "social-studies"];

    private readonly IUnderstandingMapCatalog _catalog;
    private readonly Manabu2CatalogClient _manabu2;

    public UnderstandingMapsController(IUnderstandingMapCatalog catalog, Manabu2CatalogClient manabu2)
    {
        _catalog = catalog;
        _manabu2 = manabu2;
    }

    [HttpGet("math")]
    public async Task<IActionResult> Math(CancellationToken cancellationToken) =>
        View(await _catalog.GetMapAsync("math", cancellationToken));

    [HttpGet("japanese")]
    public async Task<IActionResult> Japanese(CancellationToken cancellationToken) =>
        View("Math", await _catalog.GetMapAsync("japanese", cancellationToken));

    [HttpGet("english")]
    public async Task<IActionResult> English(CancellationToken cancellationToken) =>
        View("Math", await _catalog.GetMapAsync("english", cancellationToken));

    [HttpGet("science")]
    public async Task<IActionResult> Science(CancellationToken cancellationToken) =>
        View("Math", await _catalog.GetMapAsync("science", cancellationToken));

    [HttpGet("social-studies")]
    public async Task<IActionResult> SocialStudies(CancellationToken cancellationToken) =>
        View("Math", await _catalog.GetMapAsync("social-studies", cancellationToken));

    [HttpGet("{subjectId}/courses/{courseId}")]
    public async Task<IActionResult> Course(
        string subjectId,
        string courseId,
        CancellationToken cancellationToken)
    {
        var subjectContext = await GetSubjectContextAsync(subjectId, courseId, cancellationToken);
        if (subjectContext is null)
        {
            var canonicalContext = await FindSubjectContextAsync(courseId, cancellationToken);
            return canonicalContext is null
                ? NotFound()
                : RedirectToActionPermanent(nameof(Course), new
                {
                    subjectId = canonicalContext.SubjectId,
                    courseId
                });
        }

        var course = await _manabu2.GetCourseAsync(courseId, cancellationToken);
        if (course is null)
        {
            return NotFound();
        }

        var model = new CourseSectionsViewModel(
            subjectContext.SubjectId,
            subjectContext.Subject,
            subjectContext.SubjectMapAction,
            subjectContext.AreaName,
            course.Id,
            course.Title,
            course.Description,
            course.Sections
                .OrderBy(section => section.OrderIndex)
                .Select(section => new CourseSectionViewModel(
                    section.Id,
                    section.Title,
                    section.Summary,
                    section.OrderIndex,
                    section.Lessons
                        .OrderBy(lesson => lesson.OrderIndex)
                        .Select(lesson => new CourseLessonViewModel(
                            lesson.Id,
                            lesson.Title,
                            lesson.Summary,
                            lesson.OrderIndex))
                        .ToArray()))
                .ToArray());

        return View("Course", model);
    }

    [HttpGet("{subjectId}/courses/{courseId}/lessons/{lessonId}")]
    public async Task<IActionResult> Lesson(
        string subjectId,
        string courseId,
        string lessonId,
        CancellationToken cancellationToken)
    {
        var subjectContext = await GetSubjectContextAsync(subjectId, courseId, cancellationToken);
        if (subjectContext is null)
        {
            var canonicalContext = await FindSubjectContextAsync(courseId, cancellationToken);
            return canonicalContext is null
                ? NotFound()
                : RedirectToActionPermanent(nameof(Lesson), new
                {
                    subjectId = canonicalContext.SubjectId,
                    courseId,
                    lessonId
                });
        }

        var courseTask = _manabu2.GetCourseAsync(courseId, cancellationToken);
        var lessonTask = _manabu2.GetLessonAsync(lessonId, cancellationToken);
        await Task.WhenAll(courseTask, lessonTask);

        var course = await courseTask;
        var lesson = await lessonTask;
        if (course is null ||
            lesson is null ||
            !string.Equals(lesson.CourseId, courseId, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        var section = course.Sections.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, lesson.SectionId, StringComparison.OrdinalIgnoreCase));
        if (section is null)
        {
            return NotFound();
        }

        return View("Lesson", new LessonDetailViewModel(
            subjectContext.SubjectId,
            subjectContext.Subject,
            subjectContext.SubjectMapAction,
            subjectContext.AreaName,
            course.Title,
            section.Title,
            lesson.Id,
            lesson.CourseId,
            lesson.SectionId,
            lesson.Title,
            lesson.Summary,
            lesson.BodyHtml,
            lesson.MaterialDetails
                .Select(material => new LessonMaterialViewModel(
                    material.Id,
                    material.Summary,
                    material.FileName,
                    material.FileContentType,
                    material.FileUrl,
                    material.ContentType,
                    material.ContentHtml,
                    material.OrderIndex))
                .ToArray(),
            lesson.Quiz is null
                ? CreateQuizPreview(lesson.Id)
                : new LessonQuizViewModel(
                    lesson.Quiz.Id,
                    lesson.Quiz.Title,
                    lesson.Quiz.Instructions,
                    lesson.Quiz.PassingScorePercent,
                    lesson.Quiz.Questions
                        .OrderBy(question => question.OrderIndex)
                        .Select(question => new LessonQuizQuestionViewModel(
                            question.Id,
                            question.QuestionText,
                            question.OrderIndex,
                            question.Options
                                .OrderBy(option => option.OrderIndex)
                                .Select(option => new LessonQuizOptionViewModel(
                                    option.Id,
                                    option.Text,
                                    option.OrderIndex))
                                .ToArray()))
                .ToArray())));
    }

    [HttpGet("{subjectId}/tests/{testId}")]
    public async Task<IActionResult> Test(
        string subjectId,
        string testId,
        CancellationToken cancellationToken)
    {
        var testContext = await GetTestContextAsync(subjectId, testId, cancellationToken);
        if (testContext is null)
        {
            var canonicalContext = await FindTestContextAsync(testId, cancellationToken);
            return canonicalContext is null
                ? NotFound()
                : RedirectToActionPermanent(nameof(Test), new
                {
                    subjectId = canonicalContext.SubjectId,
                    testId
                });
        }

        var test = await _manabu2.GetTestAsync(testId, cancellationToken);
        if (test is null ||
            !string.Equals(test.PathId, testContext.Test.PathId, StringComparison.OrdinalIgnoreCase))
        {
            return NotFound();
        }

        return View("Test", new UnderstandingTestViewModel(
            testContext.SubjectId,
            testContext.Subject,
            testContext.SubjectMapAction,
            testContext.AreaName,
            test.Id,
            test.Title,
            test.Description,
            test.PassingScorePercent,
            test.TimeLimitSeconds,
            test.Questions
                .OrderBy(question => question.OrderIndex)
                .Select(question => new UnderstandingTestQuestionViewModel(
                    question.Id,
                    question.QuestionText,
                    question.OrderIndex,
                    question.Options
                        .OrderBy(option => option.OrderIndex)
                        .Select(option => new UnderstandingTestOptionViewModel(
                            option.Id,
                            option.OptionText,
                            option.ImageUrl,
                            option.OrderIndex))
                        .ToArray()))
                .ToArray()));
    }

    private async Task<SubjectContext?> GetSubjectContextAsync(
        string subjectId,
        string courseId,
        CancellationToken cancellationToken)
    {
        UnderstandingMapViewModel map;
        try
        {
            map = await _catalog.GetMapAsync(subjectId, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }

        var area = map.Areas.FirstOrDefault(candidate =>
            candidate.Nodes.Any(node =>
                string.Equals(node.CourseId, courseId, StringComparison.OrdinalIgnoreCase)));

        var subjectMapAction = subjectId.ToLowerInvariant() switch
        {
            "math" => nameof(Math),
            "japanese" => nameof(Japanese),
            "english" => nameof(English),
            "science" => nameof(Science),
            "social-studies" => nameof(SocialStudies),
            _ => null
        };

        return area is null || subjectMapAction is null
            ? null
            : new SubjectContext(map.SubjectId, map.Subject, subjectMapAction, area.Name);
    }

    private async Task<SubjectContext?> FindSubjectContextAsync(
        string courseId,
        CancellationToken cancellationToken)
    {
        foreach (var candidateSubjectId in SubjectIds)
        {
            var context = await GetSubjectContextAsync(
                candidateSubjectId,
                courseId,
                cancellationToken);
            if (context is not null)
            {
                return context;
            }
        }

        return null;
    }

    private async Task<TestContext?> GetTestContextAsync(
        string subjectId,
        string testId,
        CancellationToken cancellationToken)
    {
        UnderstandingMapViewModel map;
        try
        {
            map = await _catalog.GetMapAsync(subjectId, cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return null;
        }

        var area = map.Areas.FirstOrDefault(candidate =>
            candidate.Tests.Any(test =>
                string.Equals(test.Id, testId, StringComparison.OrdinalIgnoreCase)));
        var test = area?.Tests.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, testId, StringComparison.OrdinalIgnoreCase));
        var subjectMapAction = subjectId.ToLowerInvariant() switch
        {
            "math" => nameof(Math),
            "japanese" => nameof(Japanese),
            "english" => nameof(English),
            "science" => nameof(Science),
            "social-studies" => nameof(SocialStudies),
            _ => null
        };

        return area is null || test is null || subjectMapAction is null
            ? null
            : new TestContext(
                map.SubjectId,
                map.Subject,
                subjectMapAction,
                area.Name,
                test);
    }

    private async Task<TestContext?> FindTestContextAsync(
        string testId,
        CancellationToken cancellationToken)
    {
        foreach (var candidateSubjectId in SubjectIds)
        {
            var context = await GetTestContextAsync(
                candidateSubjectId,
                testId,
                cancellationToken);
            if (context is not null)
            {
                return context;
            }
        }

        return null;
    }

    private static LessonQuizViewModel? CreateQuizPreview(string lessonId)
    {
        if (!string.Equals(lessonId, "7725e3a466ec41389f4216a26d2795c7", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new LessonQuizViewModel(
            "negative-numbers-preview",
            "0より小さい数を理解できたか確認しよう",
            "それぞれ、もっとも適切な答えを1つ選びましょう。",
            80,
            [
                Question(1, "負の数とは、どのような数ですか？",
                    "0より小さい数", "0より大きい数", "0と同じ数", "小数だけを集めた数"),
                Question(2, "0℃より5℃低い気温は、どのように表しますか？",
                    "−5℃", "+5℃", "0℃", "5℃より高い"),
                Question(3, "海面を0mとすると、海面より12m低い場所はどのように表しますか？",
                    "−12m", "+12m", "0m", "12m"),
                Question(4, "0について正しい説明はどれですか？",
                    "正の数でも負の数でもない", "負の数である", "正の数である", "数ではない"),
                Question(5, "基準より7点少ないことを表す数はどれですか？",
                    "−7点", "+7点", "0点", "7倍")
            ]);
    }

    private static LessonQuizQuestionViewModel Question(
        int orderIndex,
        string text,
        params string[] options) =>
        new(
            $"preview-question-{orderIndex}",
            text,
            orderIndex,
            options.Select((option, index) => new LessonQuizOptionViewModel(
                    $"preview-option-{orderIndex}-{index + 1}",
                    option,
                    index + 1))
                .ToArray());

    private sealed record SubjectContext(
        string SubjectId,
        string Subject,
        string SubjectMapAction,
        string AreaName);

    private sealed record TestContext(
        string SubjectId,
        string Subject,
        string SubjectMapAction,
        string AreaName,
        UnderstandingTest Test);
}
