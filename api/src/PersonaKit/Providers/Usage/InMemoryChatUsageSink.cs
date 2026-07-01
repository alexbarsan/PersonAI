namespace PersonaKit.Providers.Usage;

public sealed class InMemoryChatUsageSink : IChatUsageSink
{
    private readonly List<ChatUsageRecord> _records = [];

    public IReadOnlyList<ChatUsageRecord> Records => _records;

    public Task RecordAsync(ChatUsageRecord record, CancellationToken cancellationToken)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }
}
