using System.Text;
using Microsoft.Extensions.AI;
using PersonaKit.Context;
using PersonaKit.Personas;
using PersonaKit.Pipeline;

namespace PersonaKit.Tests;

public sealed class AstraPersonaReuseTests
{
    [Fact]
    public async Task RegistryLoadsAstrologerPersonaFromConfig()
    {
        var registry = new FilePersonaRegistry(PersonaTestPaths.PersonasRoot);

        var persona = await registry.GetAsync("astrologer");

        Assert.Equal("astrologer", persona.Id);
        Assert.Equal("1.0.0", persona.Version);
        Assert.Equal("Astra", persona.DisplayName);
        Assert.True(File.Exists(persona.PromptTemplatePath));
        Assert.True(File.Exists(persona.OutputSchemaPath));
        Assert.True(File.Exists(persona.SectionMapPath));
    }

    [Fact]
    public async Task AstrologerPromptSnapshotIsStable()
    {
        var registry = new FilePersonaRegistry(PersonaTestPaths.PersonasRoot);
        var persona = await registry.GetAsync("astrologer");
        var renderer = new ScribanPromptRenderer();

        var prompt = await renderer.RenderAsync(persona, new Dictionary<string, object?>
        {
            ["context_json"] = AstraContextJson,
            ["persona_id"] = persona.Id,
            ["persona_version"] = persona.Version,
            ["locale"] = "en-US",
            ["output_schema_json"] = await File.ReadAllTextAsync(persona.OutputSchemaPath)
        });

        await Verifier.Verify(prompt).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task AstrologerSchemaValidatesSampleOutput()
    {
        var registry = new FilePersonaRegistry(PersonaTestPaths.PersonasRoot);
        var persona = await registry.GetAsync("astrologer");
        var validator = new JsonSchemaOutputValidator();

        var result = await validator.ValidateAsync(persona, AstraAiOutput);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task AstrologerOutputMapsThroughGenericSectionRendererShape()
    {
        var registry = new FilePersonaRegistry(PersonaTestPaths.PersonasRoot);
        var persona = await registry.GetAsync("astrologer");
        var mapper = new SectionMapResultMapper();

        var result = await mapper.MapAsync(persona, AstraAiOutput);

        Assert.Equal("A clear, pragmatic day for choosing one priority and following it steadily.", result.Summary);
        Assert.Contains(result.Sections, section => section.Kind == "text" && section.Title == "Cosmic Weather");
        Assert.Contains(result.Sections, section => section.Kind == "list" && section.Title == "Focus Areas");
        Assert.Contains(result.Sections, section => section.Kind == "text" && section.Title == "Guidance");
        Assert.Equal(2, result.FollowUpQuestions.Length);
    }

    [Fact]
    public async Task AstrologerPipelineUsesSamePersonaKitAbstractions()
    {
        var chatClient = new RecordingChatClient(new ChatResponse(new ChatMessage(ChatRole.Assistant, AstraAiOutput)));
        var store = new InMemoryInterpretationStore();
        var pipeline = CreatePipeline(chatClient, store);

        var result = await pipeline.InterpretAsync(CreateRequest());

        Assert.Equal(InterpretationStatus.Completed, result.Status);
        Assert.NotNull(result.Result);
        Assert.Equal("A clear, pragmatic day for choosing one priority and following it steadily.", result.Result.Summary);
        Assert.Contains(result.Result.Sections, section => section.Title == "Focus Areas");
        Assert.Equal("astrologer", Assert.Single(store.Runs).PersonaId);
    }

    private static InterpretationPipeline CreatePipeline(RecordingChatClient chatClient, InMemoryInterpretationStore store)
    {
        var secret = Convert.ToBase64String(Encoding.UTF8.GetBytes("12345678901234567890123456789012"));
        return new InterpretationPipeline(
            new FilePersonaRegistry(PersonaTestPaths.PersonasRoot),
            new ContextBuilder(new HmacPseudonymService(new PseudonymOptions { SecretBase64 = secret })),
            new ScribanPromptRenderer(),
            chatClient,
            new JsonSchemaOutputValidator(),
            new SectionMapResultMapper(),
            store,
            store,
            new NoOpModerationPrecheck());
    }

    private static InterpretationRequest CreateRequest()
    {
        return new InterpretationRequest(
            "astrologer",
            new ContextBuildRequest(
                "00000000-0000-0000-0000-000000000020",
                "en-US",
                new ContextPersona("astrologer", "1.0.0"),
                new ContextUserSource(
                    "cognito-sub-astra",
                    null,
                    null,
                    33,
                    "female",
                    "female",
                    "en",
                    "America/New_York",
                    new ContextTraits(
                        [],
                        [],
                        ["journaling", "career planning"],
                        "designer",
                        "partnered",
                        "Romanian-American",
                        "steady, ~7h",
                        "medium",
                        ["considering a team change"]),
                    new ContextConsent(true, true, true)),
                new ContextHistory(["career", "communication"], 5, "Often asks about timing and focus."),
                new DreamInput("I want a grounded astrology reading for today, focused on work and relationships.", "curious", 4, ["daily"], "2026-07-02")));
    }

    private const string AstraContextJson = """
    {
      "schemaVersion": "1.0",
      "requestId": "00000000-0000-0000-0000-000000000020",
      "locale": "en-US",
      "persona": {
        "id": "astrologer",
        "version": "1.0.0"
      },
      "user": {
        "pseudonymId": "usr_astra123",
        "age": 33,
        "language": "en",
        "timezone": "America/New_York",
        "traits": {
          "interests": ["journaling", "career planning"],
          "occupation": "designer",
          "relationshipStatus": "partnered",
          "stressLevel": "medium"
        },
        "consent": {
          "aiProcessing": true,
          "sensitiveTraits": true,
          "historyUse": true
        }
      },
      "input": {
        "type": "dream",
        "text": "I want a grounded astrology reading for today, focused on work and relationships.",
        "mood": "curious",
        "sleepQuality": 4,
        "tags": ["daily"],
        "occurredAt": "2026-07-02",
        "isUntrusted": true
      }
    }
    """;

    private const string AstraAiOutput = """
    {
      "schemaVersion": "1.0",
      "summary": "A clear, pragmatic day for choosing one priority and following it steadily.",
      "cosmicWeather": "Today favors practical decisions, calm conversations, and finishing one visible commitment.",
      "focusAreas": ["work rhythm", "direct communication", "evening recovery"],
      "guidance": "Pick the task that reduces the most friction and give it an uninterrupted block of attention.",
      "reflectionPrompts": ["What decision becomes easier if you stop waiting for perfect timing?", "Where can a direct conversation reduce ambiguity?"],
      "confidence": 0.72
    }
    """;
}
