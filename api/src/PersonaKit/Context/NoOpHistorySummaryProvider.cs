namespace PersonaKit.Context;

public sealed class NoOpHistorySummaryProvider : IHistorySummaryProvider
{
    public Task<ContextHistory?> GetAsync(string internalUserId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ContextHistory?>(null);
    }
}
