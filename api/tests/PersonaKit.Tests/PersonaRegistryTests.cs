using PersonaKit.Personas;

namespace PersonaKit.Tests;

public sealed class PersonaRegistryTests
{
    [Fact]
    public async Task LoadsDreamInterpreterPersonaFromPersonasDirectory()
    {
        var registry = new FilePersonaRegistry(PersonaTestPaths.PersonasRoot);

        var persona = await registry.GetAsync("dream-interpreter");

        Assert.Equal("dream-interpreter", persona.Id);
        Assert.Equal("1.0.0", persona.Version);
        Assert.Equal("DreamLens", persona.DisplayName);
        Assert.EndsWith("prompt.scriban", persona.PromptTemplatePath);
        Assert.EndsWith("output.schema.json", persona.OutputSchemaPath);
        Assert.EndsWith("section-map.json", persona.SectionMapPath);
        Assert.True(File.Exists(persona.PromptTemplatePath));
        Assert.True(File.Exists(persona.OutputSchemaPath));
        Assert.True(File.Exists(persona.SectionMapPath));
    }

    [Fact]
    public async Task MissingPersonaReturnsControlledError()
    {
        var registry = new FilePersonaRegistry(PersonaTestPaths.PersonasRoot);

        var exception = await Assert.ThrowsAsync<PersonaNotFoundException>(() => registry.GetAsync("missing"));

        Assert.Equal("missing", exception.PersonaId);
    }
}
