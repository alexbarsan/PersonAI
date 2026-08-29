namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class AsyncJobRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string IdempotencyKey { get; set; }

    public required string JobType { get; set; }

    public required string UserSubject { get; set; }

    public Guid? TargetId { get; set; }

    public required string PayloadJson { get; set; }

    public string Status { get; set; } = AsyncJobStatuses.Pending;

    public int AttemptCount { get; set; }

    public DateTimeOffset AvailableAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LockedUntil { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class AsyncJobStatuses
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Completed = "completed";
    public const string Failed = "failed";
}

public static class AsyncJobTypes
{
    public const string DreamEmbedding = "dream.embedding";
    public const string DreamImage = "dream.image";
    public const string VoiceTranscription = "voice.transcription";
    public const string Export = "export";
}
