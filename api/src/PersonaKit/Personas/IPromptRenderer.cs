namespace PersonaKit.Personas;

public interface IPromptRenderer
{
    Task<string> RenderAsync(
        PersonaDefinition persona,
        IReadOnlyDictionary<string, object?> variables,
        CancellationToken cancellationToken = default);
}
