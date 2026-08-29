namespace DreamLens.Api.Features.Dreams;

public sealed record RequestDreamImageRequest(string? Style);

public sealed record DreamImageResponse(
    Guid Id,
    Guid DreamId,
    string Status,
    string Style,
    Guid? JobId,
    string? DownloadUrl,
    string? ErrorMessage,
    DateTimeOffset CreatedAt);
