namespace DreamLens.Api.Features.Voice;

public sealed record VoiceCaptureResponse(
    Guid Id,
    string Status,
    int DurationSeconds,
    bool RetainRecording,
    string? Transcript,
    string? RecordingUrl,
    Guid? JobId,
    string? ErrorMessage,
    DateTimeOffset CreatedAt);
