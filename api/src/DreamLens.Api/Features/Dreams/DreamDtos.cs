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
    string? ErrorMessage,
    string? Mood = null,
    int? SleepQuality = null,
    string[]? Tags = null,
    string? OccurredAt = null,
    string? JournalNote = null);

public sealed record DreamResultResponse(
    string Summary,
    DreamSectionResponse[] Sections,
    string[] FollowUpQuestions,
    DreamSafetyResponse? Safety = null);

public sealed record DreamSafetyResponse(string SelfHarmRisk, string Notes);

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

public sealed record DeepInterpretationResponse(
    Guid Id,
    Guid DreamId,
    DreamResultResponse Result,
    SimilarDreamResponse[] Sources,
    string Model,
    DateTimeOffset CreatedAt);

public sealed record DeepInterpretationResult(
    DeepInterpretationResponse? Interpretation,
    int StatusCode,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public static DeepInterpretationResult Success(DeepInterpretationResponse interpretation) =>
        new(interpretation, StatusCodes.Status200OK, new Dictionary<string, string[]>());

    public static DeepInterpretationResult Failure(int statusCode, string key, string message) =>
        new(null, statusCode, new Dictionary<string, string[]> { [key] = [message] });
}

public sealed record DreamJournalResponse(DreamJournalItemResponse[] Items);

public sealed record DreamJournalItemResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Status,
    string? Summary,
    string? Mood,
    string? OccurredAt);

public sealed record UpdateDreamJournalRequest(
    string? Mood,
    int? SleepQuality,
    string[]? Tags,
    string? OccurredAt,
    string? JournalNote);
