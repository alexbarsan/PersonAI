namespace PersonaKit.Pipeline;

public interface IInterpretationStore
{
    Task SaveAsync(InterpretationRecord record, CancellationToken cancellationToken = default);
}
