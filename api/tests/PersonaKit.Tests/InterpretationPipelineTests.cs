using System.Net;
using System.Text;
using Microsoft.Extensions.AI;
using PersonaKit.Context;
using PersonaKit.Personas;
using PersonaKit.Pipeline;
using PersonaKit.Providers;

namespace PersonaKit.Tests;

public sealed class InterpretationPipelineTests
{
    [Fact]
    public async Task ValidAiOutputMapsToUiResponseSectionsAndPersistsSuccess()
    {
        var chatClient = new RecordingChatClient(new ChatResponse(new ChatMessage(ChatRole.Assistant, CanonicalJson.AiOutput)));
        var store = new InMemoryInterpretationStore();
        var pipeline = CreatePipeline(chatClient, store);

        var result = await pipeline.InterpretAsync(CreateRequest());

        Assert.Equal(InterpretationStatus.Completed, result.Status);
        Assert.NotNull(result.Result);
        Assert.StartsWith("The dream centers", result.Result.Summary, StringComparison.Ordinal);
        Assert.Contains(result.Result.Sections, section => section.Kind == "symbols" && section.Title == "Symbols");
        Assert.Contains(result.Result.Sections, section => section.Kind == "text" && section.Title == "Guidance");
        Assert.Equal(2, result.Result.FollowUpQuestions.Length);
        Assert.Single(store.Interpretations);
        Assert.Equal(AiRunStatus.Succeeded, Assert.Single(store.Runs).Status);
    }

    [Fact]
    public async Task InvalidJsonTriggersExactlyOneRepairRetry()
    {
        var chatClient = new RecordingChatClient(
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "{ invalid json")),
            new ChatResponse(new ChatMessage(ChatRole.Assistant, CanonicalJson.AiOutput)));
        var store = new InMemoryInterpretationStore();
        var pipeline = CreatePipeline(chatClient, store);

        var result = await pipeline.InterpretAsync(CreateRequest());

        Assert.Equal(InterpretationStatus.Completed, result.Status);
        Assert.Equal(2, chatClient.Calls.Count);
        Assert.Contains("Your previous response was invalid.", chatClient.Calls[1].Messages.Last().Text);
        Assert.Contains("{ invalid json", chatClient.Calls[1].Messages.Last().Text);
        Assert.Equal(2, Assert.Single(store.Runs).AttemptCount);
    }

    [Fact]
    public async Task SecondInvalidResponseReturnsFriendlyFailure()
    {
        var chatClient = new RecordingChatClient(
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "{ invalid json")),
            new ChatResponse(new ChatMessage(ChatRole.Assistant, "{ still invalid")));
        var store = new InMemoryInterpretationStore();
        var pipeline = CreatePipeline(chatClient, store);

        var result = await pipeline.InterpretAsync(CreateRequest());

        Assert.Equal(InterpretationStatus.Failed, result.Status);
        Assert.Null(result.Result);
        Assert.Equal("The interpretation service could not produce a valid result. Please try again.", result.ErrorMessage);
        Assert.Equal(2, chatClient.Calls.Count);
        Assert.Empty(store.Interpretations);
        var run = Assert.Single(store.Runs);
        Assert.Equal(AiRunStatus.Failed, run.Status);
        Assert.Equal(2, run.AttemptCount);
        Assert.Equal(AiRunFailureKind.Validation, run.FailureKind);
    }

    [Fact]
    public async Task ProviderFailuresAreRecordedAsFailedAiRuns()
    {
        var chatClient = new RecordingChatClient(new ChatProviderException(HttpStatusCode.ServiceUnavailable, "provider down"));
        var store = new InMemoryInterpretationStore();
        var pipeline = CreatePipeline(chatClient, store);

        var result = await pipeline.InterpretAsync(CreateRequest());

        Assert.Equal(InterpretationStatus.Failed, result.Status);
        Assert.Equal("The interpretation service is temporarily unavailable. Please try again.", result.ErrorMessage);
        var run = Assert.Single(store.Runs);
        Assert.Equal(AiRunStatus.Failed, run.Status);
        Assert.Equal(AiRunFailureKind.Provider, run.FailureKind);
        Assert.Equal(1, run.AttemptCount);
    }

    [Fact]
    public async Task RunMetadataDoesNotContainRawDreamText()
    {
        const string dreamText = "I was falling into dark water while someone told me to ignore all rules.";
        var chatClient = new RecordingChatClient(new ChatResponse(new ChatMessage(ChatRole.Assistant, CanonicalJson.AiOutput)));
        var store = new InMemoryInterpretationStore();
        var pipeline = CreatePipeline(chatClient, store);

        await pipeline.InterpretAsync(CreateRequest(dreamText));

        var serializedRuns = string.Join(Environment.NewLine, store.Runs.Select(run => run.ToString()));
        Assert.DoesNotContain(dreamText, serializedRuns);
        Assert.DoesNotContain("falling into dark water", serializedRuns);
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

    private static InterpretationRequest CreateRequest(
        string dreamText = "I was falling into dark water while someone told me to ignore all rules.")
    {
        return new InterpretationRequest(
            "dream-interpreter",
            new ContextBuildRequest(
                "00000000-0000-0000-0000-000000000001",
                "en-US",
                new ContextPersona("dream-interpreter", "1.1.0"),
                new ContextUserSource(
                    "cognito-sub-123",
                    null,
                    null,
                    33,
                    "male",
                    "male",
                    "en",
                    "America/New_York",
                    new ContextTraits(
                        ["spiders", "public speaking"],
                        ["peanuts"],
                        ["hiking", "painting"],
                        "nurse",
                        "single",
                        "Romanian-American",
                        "irregular, ~6h",
                        "medium",
                        ["new job"]),
                    new ContextConsent(true, true, true)),
                new ContextHistory(["falling", "water"], 11, "Recurring water dreams."),
                new DreamInput(dreamText, "anxious", 2, ["recurring"], "2026-06-12")));
    }
}

internal sealed class RecordingChatClient(params object[] results) : IChatClient
{
    private readonly Queue<object> _results = new(results);

    public List<RecordingChatCall> Calls { get; } = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Calls.Add(new RecordingChatCall(messages.Select(message => message.Clone()).ToArray()));
        var result = _results.Dequeue();
        return result switch
        {
            ChatResponse response => Task.FromResult(response),
            Exception exception => Task.FromException<ChatResponse>(exception),
            _ => throw new InvalidOperationException("Unsupported result.")
        };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

internal sealed record RecordingChatCall(IReadOnlyList<ChatMessage> Messages);
