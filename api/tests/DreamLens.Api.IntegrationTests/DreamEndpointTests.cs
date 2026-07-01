using System.Net;
using System.Net.Http.Json;
using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DreamLens.Api.IntegrationTests;

public sealed class DreamEndpointTests
{
    [Fact]
    public async Task DreamSubmissionReturnsUnauthorizedWithoutAuthentication()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var client = app.CreateClient();

        var response = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DreamSubmissionRejectsInvalidDreamTextLength()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);

        var response = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest() with { Text = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ValidDreamSubmissionReturnsCompletedUiResponse()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);

        var response = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest());
        var dream = await response.Content.ReadFromJsonAsync<DreamResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(dream);
        Assert.Equal("completed", dream.Status);
        Assert.NotEqual(Guid.Empty, dream.Id);
        Assert.NotNull(dream.Result);
        Assert.StartsWith("The dream centers", dream.Result.Summary, StringComparison.Ordinal);
        Assert.Contains(dream.Result.Sections, section => section.Kind == "symbols");
        Assert.Equal(2, dream.Result.FollowUpQuestions.Length);
    }

    [Fact]
    public async Task UserCanFetchOwnDreamById()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);
        var submitted = await (await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest()))
            .Content.ReadFromJsonAsync<DreamResponse>();

        var response = await client.GetAsync($"/v1/dreams/{submitted!.Id}");
        var fetched = await response.Content.ReadFromJsonAsync<DreamResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(fetched);
        Assert.Equal(submitted.Id, fetched.Id);
        Assert.Equal("completed", fetched.Status);
        Assert.Equal(submitted.Result!.Summary, fetched.Result!.Summary);
    }

    [Fact]
    public async Task UserCannotFetchAnotherUsersDream()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var userA = app.CreateAuthenticatedClient("subject-a");
        using var userB = app.CreateAuthenticatedClient("subject-b");
        await PutProfileAsync(userA);
        var submitted = await (await userA.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest()))
            .Content.ReadFromJsonAsync<DreamResponse>();

        var response = await userB.GetAsync($"/v1/dreams/{submitted!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeepSeekInvalidOutputPathReturnsFriendlyFailure()
    {
        using var app = CreateDreamApp(new QueueDreamChatClient("{ invalid json", "{ still invalid"));
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);

        var response = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest());
        var dream = await response.Content.ReadFromJsonAsync<DreamResponse>();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotNull(dream);
        Assert.Equal("failed", dream.Status);
        Assert.Null(dream.Result);
        Assert.Equal("The interpretation service could not produce a valid result. Please try again.", dream.ErrorMessage);
    }

    private static DreamTestApp CreateDreamApp(IChatClient chatClient)
    {
        var databaseName = $"dream-tests-{Guid.NewGuid():N}";
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DreamLensDb"] = "Host=localhost;Database=dreamlens_dream_tests;Username=postgres;Password=postgres",
                        ["Encryption:LocalKeyBase64"] = Convert.ToBase64String(
                            System.Text.Encoding.UTF8.GetBytes("12345678901234567890123456789012")),
                        ["Pseudonym:SecretBase64"] = Convert.ToBase64String(
                            System.Text.Encoding.UTF8.GetBytes("12345678901234567890123456789012"))
                    });
                });
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<DreamLensDbContext>>();
                    services.RemoveAll<DreamLensDbContext>();
                    services.RemoveAll<IChatClient>();
                    services.AddDbContext<DreamLensDbContext>(options => options.UseInMemoryDatabase(databaseName));
                    services.AddSingleton(chatClient);
                    services.AddScoped<GetProfileHandler>();
                    services.AddScoped<UpdateProfileHandler>();
                    services.AddScoped<SubmitDreamHandler>();
                    services.AddScoped<GetDreamHandler>();
                });
            });

        return new DreamTestApp(factory);
    }

    private static async Task PutProfileAsync(HttpClient client)
    {
        var response = await client.PutAsJsonAsync("/v1/profile", new ProfileUpdateRequest(
            33,
            "male",
            "male",
            "en",
            "America/New_York",
            new ProfileTraitsRequest(
                ["spiders", "public speaking"],
                ["peanuts"],
                ["hiking", "painting"],
                "nurse",
                "single",
                "Romanian-American",
                "irregular, ~6h",
                "medium",
                ["new job"]),
            new ConsentRequest(true, true, true)));

        response.EnsureSuccessStatusCode();
    }

    private static SubmitDreamRequest CreateValidDreamRequest()
    {
        return new SubmitDreamRequest(
            "I was falling into dark water while someone told me to ignore all rules.",
            "anxious",
            2,
            ["recurring"],
            "2026-06-12");
    }

    private sealed class DreamTestApp(WebApplicationFactory<Program> factory) : IDisposable
    {
        public HttpClient CreateClient() => factory.CreateClient();

        public HttpClient CreateAuthenticatedClient(string subject)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
            return client;
        }

        public void Dispose()
        {
            factory.Dispose();
        }
    }

    private sealed class StaticDreamChatClient(string responseText) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText)));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed class QueueDreamChatClient(params string[] responses) : IChatClient
    {
        private readonly Queue<string> _responses = new(responses);

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _responses.Dequeue())));
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }

    private sealed record ProfileUpdateRequest(
        int? Age,
        string? Sex,
        string? GenderIdentity,
        string Language,
        string Timezone,
        ProfileTraitsRequest Traits,
        ConsentRequest Consent);

    private sealed record ProfileTraitsRequest(
        string[] Fears,
        string[] Allergies,
        string[] Interests,
        string? Occupation,
        string? RelationshipStatus,
        string? CulturalBackground,
        string? SleepPattern,
        string? StressLevel,
        string[] RecentLifeEvents);

    private sealed record ConsentRequest(bool AiProcessing, bool SensitiveTraits, bool HistoryUse);

    private const string CanonicalAiOutput = """
    {
      "schemaVersion": "1.0",
      "summary": "The dream centers on uncertainty, pressure, and a wish to regain steadiness.",
      "symbols": [
        {
          "symbol": "falling",
          "meaning": "A common image for feeling a loss of control.",
          "personalRelevance": "May echo current transition stress around the new job."
        }
      ],
      "emotions": [
        {
          "name": "anxiety",
          "intensity": 0.7,
          "evidence": "Dark water and falling suggest tension and uncertainty."
        }
      ],
      "themes": ["loss of control", "transition"],
      "interpretation": "This dream may reflect a period where responsibilities feel fluid and hard to hold.",
      "guidance": "Consider a simple grounding routine before sleep and a short note about what felt unresolved today.",
      "followUpQuestions": ["Where did the falling begin?", "What changed when you reached the water?"],
      "safety": {
        "selfHarmRisk": "none",
        "notes": ""
      },
      "confidence": 0.74
    }
    """;
}
