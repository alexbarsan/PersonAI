namespace PersonaKit.Personas;

public sealed record PersonaDefinition(
    string Id,
    string Version,
    string DisplayName,
    string PromptTemplatePath,
    string OutputSchemaPath,
    string SectionMapPath);
