namespace PersonaKit.Personas;

public sealed class PersonaNotFoundException(string personaId)
    : Exception($"Persona '{personaId}' was not found.")
{
    public string PersonaId { get; } = personaId;
}
