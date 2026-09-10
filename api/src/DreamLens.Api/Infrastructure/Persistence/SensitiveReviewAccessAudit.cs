namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class SensitiveReviewAccessAudit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid SafetyEventId { get; set; }

    public required string ReviewerSubject { get; set; }

    public required string Purpose { get; set; }

    public DateTimeOffset AccessedAt { get; set; } = DateTimeOffset.UtcNow;
}
