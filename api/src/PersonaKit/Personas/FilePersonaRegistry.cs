using System.Text.Json;

namespace PersonaKit.Personas;

public sealed class FilePersonaRegistry(string personasRoot) : IPersonaRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PersonaDefinition> GetAsync(string personaId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(personaId))
        {
            throw new ArgumentException("Persona id is required.", nameof(personaId));
        }

        var personaDirectory = Path.Combine(personasRoot, personaId);
        var configPath = Path.Combine(personaDirectory, "persona.json");
        if (!File.Exists(configPath))
        {
            throw new PersonaNotFoundException(personaId);
        }

        await using var stream = File.OpenRead(configPath);
        var config = await JsonSerializer.DeserializeAsync<PersonaConfig>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Persona config '{configPath}' is empty.");

        if (!string.Equals(config.Id, personaId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Persona config id '{config.Id}' does not match requested id '{personaId}'.");
        }

        return new PersonaDefinition(
            config.Id,
            config.Version,
            config.DisplayName,
            Path.GetFullPath(Path.Combine(personaDirectory, config.PromptTemplate)),
            Path.GetFullPath(Path.Combine(personaDirectory, config.OutputSchema)),
            Path.GetFullPath(Path.Combine(personaDirectory, config.SectionMap)));
    }
}
