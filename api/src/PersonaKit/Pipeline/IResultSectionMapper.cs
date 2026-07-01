using PersonaKit.Personas;

namespace PersonaKit.Pipeline;

public interface IResultSectionMapper
{
    Task<InterpretationResult> MapAsync(
        PersonaDefinition persona,
        string outputJson,
        CancellationToken cancellationToken = default);
}
