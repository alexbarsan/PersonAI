namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DailyDreamContent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateOnly ContentDate { get; set; }

    public required string Quote { get; set; }

    public string? Attribution { get; set; }

    public required string FactsJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
