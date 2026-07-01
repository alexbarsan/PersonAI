namespace PersonaKit.Personas;

public interface IOutputValidator
{
    Task<OutputValidationResult> ValidateAsync(
        PersonaDefinition persona,
        string outputJson,
        CancellationToken cancellationToken = default);
}
