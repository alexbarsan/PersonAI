namespace PersonaKit.Pipeline;

public sealed record AiRunRecord(
    string Id,
    string PersonaId,
    AiRunStatus Status,
    int AttemptCount,
    AiRunFailureKind? FailureKind,
    int? InputTokens,
    int? OutputTokens,
    DateTimeOffset CreatedAt);

public enum AiRunStatus
{
    Succeeded,
    Failed
}

public enum AiRunFailureKind
{
    Provider,
    Validation,
    Moderation
}
