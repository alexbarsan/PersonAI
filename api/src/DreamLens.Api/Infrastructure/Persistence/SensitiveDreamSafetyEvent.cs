namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class SensitiveDreamSafetyEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DreamId { get; set; }

    public required string UserSubject { get; set; }

    public required string SubjectPseudonym { get; set; }

    public required string Category { get; set; }

    public decimal Confidence { get; set; }

    public required string Severity { get; set; }

    public bool ReviewRequired { get; set; }

    public bool RestrictsElaboration { get; set; }

    public required string EncryptedDreamText { get; set; }

    public string Status { get; set; } = SensitiveSafetyEventStatuses.Open;

    public DateTimeOffset DetectedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiresAt { get; set; }
}

public static class SensitiveSafetyEventStatuses
{
    public const string Open = "open";
    public const string Acknowledged = "acknowledged";
}
