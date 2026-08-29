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

public sealed record DreamFactsResponse(Guid DreamId, DreamFactResponse[] Facts);

public sealed record DreamFactResponse(
    string Type,
    string Value,
    decimal? Score,
    decimal? ExtractionConfidence,
    string SourceSchemaVersion);

public sealed record SimilarDreamsResponse(Guid DreamId, SimilarDreamResponse[] Matches);

public sealed record SimilarDreamResponse(Guid Id, string? Summary, string? OccurredAt, decimal Similarity);

public sealed record DreamJournalResponse(DreamJournalItemResponse[] Items);

public sealed record DreamJournalItemResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Status,
    string? Summary,
    string? Mood,
    string? OccurredAt);
