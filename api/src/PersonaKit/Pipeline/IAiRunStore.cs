namespace PersonaKit.Pipeline;

public interface IAiRunStore
{
    Task RecordAsync(AiRunRecord record, CancellationToken cancellationToken = default);
}
