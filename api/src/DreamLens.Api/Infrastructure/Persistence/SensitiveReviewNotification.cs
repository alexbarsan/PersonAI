namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class SensitiveReviewNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SafetyEventId { get; set; }

    public Guid DreamId { get; set; }

    public required string SubjectPseudonym { get; set; }

    public required string Category { get; set; }

    public decimal Confidence { get; set; }

    public required string Route { get; set; }

    public string Status { get; set; } = "pending";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
