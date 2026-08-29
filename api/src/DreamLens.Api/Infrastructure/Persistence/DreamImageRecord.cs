namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamImageRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DreamId { get; set; }

    public required string UserSubject { get; set; }

    public required string Status { get; set; } = DreamImageStatuses.Pending;

    public required string Style { get; set; }

    public string? AssetKey { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class DreamImageStatuses
{
    public const string Pending = "pending";
    public const string Generating = "generating";
    public const string Completed = "completed";
    public const string Failed = "failed";
}
