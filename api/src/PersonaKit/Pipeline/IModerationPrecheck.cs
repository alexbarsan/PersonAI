namespace PersonaKit.Pipeline;

public interface IModerationPrecheck
{
    Task<ModerationPrecheckResult> CheckAsync(
        InterpretationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ModerationPrecheckResult(bool IsAllowed, string? FailureMessage)
{
    public static ModerationPrecheckResult Allowed { get; } = new(true, null);
}
