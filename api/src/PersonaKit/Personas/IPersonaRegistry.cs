namespace PersonaKit.Personas;

public interface IPersonaRegistry
{
    Task<PersonaDefinition> GetAsync(string personaId, CancellationToken cancellationToken = default);
}
