namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class VoiceCaptureRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string UserSubject { get; set; }

    public string Status { get; set; } = VoiceCaptureStatuses.Pending;

    public required string ContentType { get; set; }

    public string? Language { get; set; }

    public int DurationSeconds { get; set; }

    public bool RetainRecording { get; set; }

    public required string SourceAssetKey { get; set; }

    public string? Transcript { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class VoiceCaptureStatuses
{
    public const string Pending = "pending";
    public const string Transcribing = "transcribing";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
