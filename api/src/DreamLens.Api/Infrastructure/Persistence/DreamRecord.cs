namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string UserSubject { get; set; }

    public required string Text { get; set; }

    public string? Mood { get; set; }

    public int? SleepQuality { get; set; }

    public string TagsJson { get; set; } = "[]";

    public string? OccurredAt { get; set; }

    public required string Status { get; set; }

    public string? ResultJson { get; set; }

    public string? ErrorMessage { get; set; }

    public string? JournalNote { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
