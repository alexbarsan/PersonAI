namespace DreamLens.Api.Features.Jobs;

public sealed record JobStatusResponse(
    Guid Id,
    string JobType,
    string Status,
    int AttemptCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? LastError);
