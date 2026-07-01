namespace PersonaKit.Providers.Usage;

public interface IChatUsageSink
{
    Task RecordAsync(ChatUsageRecord record, CancellationToken cancellationToken);
}
