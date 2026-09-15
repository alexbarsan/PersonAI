namespace DreamLens.Api.Infrastructure.Persistence;

/// <summary>
/// A durable reservation prevents concurrent Ask requests from exceeding the account-local daily allowance.
/// The question body is deliberately not stored here.
/// </summary>
public sealed class AskQuestionUsageRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string UserSubject { get; set; }

    public required string AccountTimezone { get; set; }

    public DateOnly AccountLocalDate { get; set; }

    public string Status { get; set; } = "reserved";

    public DateTimeOffset ReservedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAt { get; set; }
}
