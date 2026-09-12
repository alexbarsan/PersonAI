using DreamLens.Api.Features.Dreams;

namespace DreamLens.Api.Features.AdminOperations;

public sealed record AdminDreamSearchResponse(
    int Page,
    int PageSize,
    int Total,
    AdminDreamSearchItemResponse[] Items);

public sealed record AdminDreamSearchItemResponse(
    Guid Id,
    string SubjectPseudonym,
    DateTimeOffset CreatedAt,
    string? OccurredAt,
    string Status,
    string? Mood,
    string[] Tags,
    string? Summary,
    int ImageCount,
    string? LatestImageStatus);

public sealed record AdminDreamAccessRequest(string? Reason);

public sealed record AdminDreamDetailResponse(
    Guid Id,
    string SubjectPseudonym,
    DateTimeOffset CreatedAt,
    string? OccurredAt,
    string Status,
    string Text,
    string? Mood,
    int? SleepQuality,
    string[] Tags,
    string? JournalNote,
    DreamResultResponse? Interpretation,
    DreamResultResponse? DeepInterpretation,
    AdminDreamImageResponse[] Images);

public sealed record AdminDreamImageResponse(
    Guid Id,
    string Status,
    string Style,
    string? DownloadUrl,
    DateTimeOffset CreatedAt);
