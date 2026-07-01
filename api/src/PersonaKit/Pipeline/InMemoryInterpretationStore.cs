namespace PersonaKit.Pipeline;

public sealed class InMemoryInterpretationStore : IInterpretationStore, IAiRunStore
{
    private readonly List<InterpretationRecord> _interpretations = [];
    private readonly List<AiRunRecord> _runs = [];

    public IReadOnlyList<InterpretationRecord> Interpretations => _interpretations;

    public IReadOnlyList<AiRunRecord> Runs => _runs;

    public Task SaveAsync(InterpretationRecord record, CancellationToken cancellationToken = default)
    {
        _interpretations.Add(record);
        return Task.CompletedTask;
    }

    public Task RecordAsync(AiRunRecord record, CancellationToken cancellationToken = default)
    {
        _runs.Add(record);
        return Task.CompletedTask;
    }
}
