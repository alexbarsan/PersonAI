namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class AnonymizationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Cleared after completion. The retained pseudonym cannot be reversed without the HMAC key.
    public string? RequestingUserSubject { get; set; }

    public required string RequesterPseudonym { get; set; }

    public string Status { get; set; } = AnonymizationRequestStatuses.Pending;

    public string? ReviewedBySubject { get; set; }

    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ReviewedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}

public static class AnonymizationRequestStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
}
