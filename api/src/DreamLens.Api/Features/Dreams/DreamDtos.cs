namespace DreamLens.Api.Features.Dreams;

public sealed record SubmitDreamRequest(
    string? Text,
    string? Mood,
    int? SleepQuality,
    string[] Tags,
    string? OccurredAt);

public sealed record DreamResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Status,
    DreamResultResponse? Result,
    string? ErrorMessage);

public sealed record DreamResultResponse(
    string Summary,
    DreamSectionResponse[] Sections,
    string[] FollowUpQuestions);

public sealed record DreamSectionResponse(string Kind, string Title, object? Content);
