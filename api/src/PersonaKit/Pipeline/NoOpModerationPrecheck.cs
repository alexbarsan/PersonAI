namespace PersonaKit.Pipeline;

public sealed class NoOpModerationPrecheck : IModerationPrecheck
{
    public Task<ModerationPrecheckResult> CheckAsync(
        InterpretationRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(ModerationPrecheckResult.Allowed);
    }
}
