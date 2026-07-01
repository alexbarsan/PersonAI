using PersonaKit.Personas;

namespace PersonaKit.Tests;

public sealed class OutputValidatorTests
{
    [Fact]
    public async Task DreamInterpreterSchemaAcceptsCanonicalAiOutputJsonV1()
    {
        var registry = new FilePersonaRegistry(PersonaTestPaths.PersonasRoot);
        var persona = await registry.GetAsync("dream-interpreter");
        var validator = new JsonSchemaOutputValidator();

        var result = await validator.ValidateAsync(persona, CanonicalJson.AiOutput);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task DreamInterpreterSchemaRejectsInvalidShapes()
    {
        var registry = new FilePersonaRegistry(PersonaTestPaths.PersonasRoot);
        var persona = await registry.GetAsync("dream-interpreter");
        var validator = new JsonSchemaOutputValidator();
        const string invalidJson = """
        {
          "schemaVersion": "1.0",
          "summary": "Missing required fields.",
          "symbols": [],
          "emotions": [],
          "themes": [],
          "interpretation": "text",
          "guidance": "text",
          "followUpQuestions": [],
          "safety": { "selfHarmRisk": "severe", "notes": "" },
          "confidence": 2.5
        }
        """;

        var result = await validator.ValidateAsync(persona, invalidJson);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("selfHarmRisk", StringComparison.Ordinal));
        Assert.Contains(result.Errors, error => error.Contains("confidence", StringComparison.Ordinal));
    }
}
