using PersonaKit.Personas;

namespace PersonaKit.Tests;

public sealed class PromptRendererTests
{
    [Fact]
    public async Task StrictModeFailsOnMissingVariables()
    {
        var registry = new FilePersonaRegistry(PersonaTestPaths.PersonasRoot);
        var persona = await registry.GetAsync("dream-interpreter");
        var renderer = new ScribanPromptRenderer();

        await Assert.ThrowsAsync<PromptRenderingException>(() =>
            renderer.RenderAsync(persona, new Dictionary<string, object?>
            {
                ["context_json"] = "{}",
                ["persona_id"] = persona.Id,
                ["persona_version"] = persona.Version,
                ["locale"] = "en-US"
            }));
    }

    [Fact]
    public async Task RenderedPromptSnapshotIsStable()
    {
        var registry = new FilePersonaRegistry(PersonaTestPaths.PersonasRoot);
        var persona = await registry.GetAsync("dream-interpreter");
        var renderer = new ScribanPromptRenderer();

        var prompt = await renderer.RenderAsync(persona, new Dictionary<string, object?>
        {
            ["context_json"] = CanonicalJson.Context,
            ["persona_id"] = persona.Id,
            ["persona_version"] = persona.Version,
            ["locale"] = "en-US",
            ["output_schema_json"] = await File.ReadAllTextAsync(persona.OutputSchemaPath)
        });

        await Verifier.Verify(prompt).UseDirectory("Snapshots");
    }
}
