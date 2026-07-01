namespace PersonaKit.Context;

public interface IHistorySummaryProvider
{
    Task<ContextHistory?> GetAsync(string internalUserId, CancellationToken cancellationToken = default);
}
