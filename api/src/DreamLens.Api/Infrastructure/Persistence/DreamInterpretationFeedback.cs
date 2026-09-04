namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamInterpretationFeedback
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DreamId { get; set; }

    public required string UserSubject { get; set; }

    public required string Rating { get; set; }

    public string ReasonsJson { get; set; } = "[]";

    public string? Details { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
