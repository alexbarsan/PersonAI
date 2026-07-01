namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class AiCostLedgerRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserSubject { get; set; } = string.Empty;

    public Guid? DreamId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public string PersonaId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? FailureKind { get; set; }

    public int AttemptCount { get; set; }

    public int? InputTokens { get; set; }

    public int? OutputTokens { get; set; }

    public int? TotalTokens { get; set; }

    public long LatencyMilliseconds { get; set; }

    public decimal EstimatedCostUsd { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
