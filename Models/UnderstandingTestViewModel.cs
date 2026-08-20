namespace wakaroute_web.Models;

public sealed record UnderstandingTestViewModel(
    string SubjectId,
    string Subject,
    string SubjectMapAction,
    string AreaName,
    string Id,
    string Title,
    string? Description,
    int PassingScorePercent,
    int? TimeLimitSeconds,
    IReadOnlyList<UnderstandingTestQuestionViewModel> Questions)
{
    public int? TimeLimitMinutes => TimeLimitSeconds is > 0
        ? (int)Math.Ceiling(TimeLimitSeconds.Value / 60d)
        : null;
}

public sealed record UnderstandingTestQuestionViewModel(
    string Id,
    string QuestionText,
    int OrderIndex,
    IReadOnlyList<UnderstandingTestOptionViewModel> Options);

public sealed record UnderstandingTestOptionViewModel(
    string Id,
    string OptionText,
    string? ImageUrl,
    int OrderIndex);
