namespace wakaroute_web.Models;

public sealed record CourseSectionsViewModel(
    string SubjectId,
    string Subject,
    string SubjectMapAction,
    string AreaName,
    string CourseId,
    string Title,
    string? Description,
    IReadOnlyList<CourseSectionViewModel> Sections);

public sealed record CourseSectionViewModel(
    string Id,
    string Title,
    string? Summary,
    int OrderIndex,
    IReadOnlyList<CourseLessonViewModel> Lessons)
{
    public int LessonCount => Lessons.Count;
}

public sealed record CourseLessonViewModel(
    string Id,
    string Title,
    string? Summary,
    int OrderIndex);

public sealed record LessonDetailViewModel(
    string SubjectId,
    string Subject,
    string SubjectMapAction,
    string AreaName,
    string CourseTitle,
    string SectionTitle,
    string Id,
    string CourseId,
    string SectionId,
    string Title,
    string? Summary,
    string BodyHtml,
    IReadOnlyList<LessonMaterialViewModel> Materials,
    LessonQuizViewModel? Quiz);

public sealed record LessonMaterialViewModel(
    string Id,
    string? Summary,
    string? FileName,
    string? FileContentType,
    string? FileUrl,
    string ContentType,
    string? ContentHtml,
    int OrderIndex)
{
    public bool IsDisplayableImage =>
        FileContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) is true &&
        Uri.TryCreate(FileUrl, UriKind.Absolute, out var fileUri) &&
        (fileUri.Scheme == Uri.UriSchemeHttps || fileUri.Scheme == Uri.UriSchemeHttp);

    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Summary)
            ? Summary
            : !string.IsNullOrWhiteSpace(FileName)
                ? FileName
                : "レッスンの補足画像";
}

public sealed record LessonQuizViewModel(
    string Id,
    string Title,
    string? Instructions,
    int PassingScorePercent,
    IReadOnlyList<LessonQuizQuestionViewModel> Questions);

public sealed record LessonQuizQuestionViewModel(
    string Id,
    string QuestionText,
    int OrderIndex,
    IReadOnlyList<LessonQuizOptionViewModel> Options);

public sealed record LessonQuizOptionViewModel(
    string Id,
    string Text,
    int OrderIndex);
