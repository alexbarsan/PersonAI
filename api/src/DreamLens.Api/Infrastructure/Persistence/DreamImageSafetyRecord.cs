namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamImageSafetyRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DreamId { get; set; }

    public required string UserSubject { get; set; }

    public string Status { get; set; } = DreamImageSafetyStatuses.Pending;

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string PromptMode { get; set; } = string.Empty;

    public string CategoriesJson { get; set; } = "[]";

    public string? FailureKind { get; set; }

    public long? LatencyMilliseconds { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class DreamImageSafetyStatuses
{
    public const string Pending = "pending";
    public const string Processing = "processing";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
