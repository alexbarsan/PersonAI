using System.Net;
using System.Net.Http.Json;
using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Features.Insights;
using DreamLens.Api.Features.Jobs;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Quotas;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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
    public async Task UserCanFetchNormalizedFactsForOwnDream()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var userA = app.CreateAuthenticatedClient("subject-a");
        using var userB = app.CreateAuthenticatedClient("subject-b");
        await PutProfileAsync(userA);
        await PutProfileAsync(userB);
        var submitted = await (await userA.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest()))
            .Content.ReadFromJsonAsync<DreamResponse>();

        var response = await userA.GetAsync($"/v1/dreams/{submitted!.Id}/facts");
        var facts = await response.Content.ReadFromJsonAsync<DreamFactsResponse>();
        var otherUserResponse = await userB.GetAsync($"/v1/dreams/{submitted.Id}/facts");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(facts);
        Assert.Equal(submitted.Id, facts.DreamId);
        Assert.Contains(facts.Facts, fact => fact.Type == "symbol" && fact.Value == "falling");
        Assert.Contains(facts.Facts, fact => fact.Type == "emotion" && fact.Value == "anxiety" && fact.Score == 0.7m);
        Assert.Contains(facts.Facts, fact => fact.Type == "person" && fact.Value == "Alex");
        Assert.Contains(facts.Facts, fact => fact.Type == "location" && fact.Value == "dark water");
        Assert.Contains(facts.Facts, fact => fact.Type == "scenario" && fact.Value == "falling");
        Assert.Contains(facts.Facts, fact => fact.Type == "lucidity-score" && fact.Score == 0.1m);
        Assert.Equal(HttpStatusCode.NotFound, otherUserResponse.StatusCode);
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

    [Fact]
    public async Task DreamJournalListsCurrentUsersDreams()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var userA = app.CreateAuthenticatedClient("subject-a");
        using var userB = app.CreateAuthenticatedClient("subject-b");
        await PutProfileAsync(userA);
        await PutProfileAsync(userB);
        var first = await (await userA.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest() with { OccurredAt = "2026-06-12" }))
            .Content.ReadFromJsonAsync<DreamResponse>();
        var second = await (await userA.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest() with { OccurredAt = "2026-06-13" }))
            .Content.ReadFromJsonAsync<DreamResponse>();
        await userB.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest() with { OccurredAt = "2026-06-14" });

        var response = await userA.GetAsync("/v1/dreams");
        var journal = await response.Content.ReadFromJsonAsync<DreamJournalResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(journal);
        Assert.Equal(2, journal.Items.Length);
        Assert.Equal(second!.Id, journal.Items[0].Id);
        Assert.Equal(first!.Id, journal.Items[1].Id);
        Assert.All(journal.Items, item => Assert.Equal("completed", item.Status));
    }

    [Fact]
    public async Task DeleteDreamRemovesOwnDreamAndCannotDeleteAnotherUsersDream()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var userA = app.CreateAuthenticatedClient("subject-a");
        using var userB = app.CreateAuthenticatedClient("subject-b");
        await PutProfileAsync(userA);
        await PutProfileAsync(userB);
        var own = await (await userA.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest()))
            .Content.ReadFromJsonAsync<DreamResponse>();
        var other = await (await userB.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest()))
            .Content.ReadFromJsonAsync<DreamResponse>();

        var otherDelete = await userA.DeleteAsync($"/v1/dreams/{other!.Id}");
        var ownDelete = await userA.DeleteAsync($"/v1/dreams/{own!.Id}");
        var fetchDeleted = await userA.GetAsync($"/v1/dreams/{own.Id}");

        Assert.Equal(HttpStatusCode.NotFound, otherDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, ownDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, fetchDeleted.StatusCode);
    }

    [Fact]
    public async Task InsightsReturnRecurringThemesAndStreaksForCurrentUser()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var userA = app.CreateAuthenticatedClient("subject-a");
        using var userB = app.CreateAuthenticatedClient("subject-b");
        await PutProfileAsync(userA);
        await PutProfileAsync(userB);
        await userA.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest() with { OccurredAt = "2026-06-12" });
        await userA.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest() with { OccurredAt = "2026-06-13" });
        await userB.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest() with { OccurredAt = "2026-06-14" });

        var response = await userA.GetAsync("/v1/insights");
        var insights = await response.Content.ReadFromJsonAsync<InsightsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(insights);
        Assert.Equal(2, insights.TotalDreams);
        Assert.Equal(2, insights.CurrentStreakDays);
        Assert.Contains(insights.RecurringThemes, theme => theme.Name == "loss of control" && theme.Count == 2);
        Assert.Contains(insights.RecurringThemes, theme => theme.Name == "transition" && theme.Count == 2);
    }

    [Fact]
    public async Task DailyQuotaBlocksExcessDreamSubmissions()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput), dailyDreamQuota: 1);
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);

        var first = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest());
        var second = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest());
        var ledgerRows = await app.CountCostLedgerRowsAsync();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal(1, ledgerRows);
        var body = await second.Content.ReadAsStringAsync();
        Assert.Contains("quota_exceeded", body);
        Assert.DoesNotContain(CreateValidDreamRequest().Text!, body);
    }

    [Fact]
    public async Task FailedInterpretationDoesNotConsumeDailyQuota()
    {
        using var app = CreateDreamApp(
            new QueueDreamChatClient("{ invalid json", "{ still invalid", CanonicalAiOutput),
            dailyDreamQuota: 1);
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);

        var failed = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest());
        var retry = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
    }

    [Fact]
    public async Task OwnerCanRequeueFailedAsyncJob()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var client = app.CreateAuthenticatedClient("subject-a");
        var jobId = Guid.NewGuid();
        await app.AddAsyncJobAsync(new AsyncJobRecord
        {
            Id = jobId,
            IdempotencyKey = $"{AsyncJobTypes.DreamEmbedding}:{jobId}:1",
            JobType = AsyncJobTypes.DreamEmbedding,
            UserSubject = "subject-a",
            TargetId = jobId,
            PayloadJson = "{}",
            Status = AsyncJobStatuses.Failed,
            AttemptCount = 5,
            LastError = "Provider temporarily unavailable."
        });

        var response = await client.PostAsync($"/v1/jobs/{jobId}/retry", content: null);
        var job = await response.Content.ReadFromJsonAsync<JobStatusResponse>();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(job);
        Assert.Equal(AsyncJobStatuses.Pending, job.Status);
        Assert.Equal(0, job.AttemptCount);
        Assert.Null(job.LastError);
        Assert.Equal(1, app.PublishedAsyncJobCount);
    }

    [Fact]
    public async Task PremiumEntitlementAllowsHigherDailyQuota()
    {
        using var app = CreateDreamApp(
            new StaticDreamChatClient(CanonicalAiOutput),
            dailyDreamQuota: 1,
            premiumDailyDreamQuota: 2,
            premiumSubjects: ["subject-a"]);
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);

        var first = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest());
        var second = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest());
        var third = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest());

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task EntitlementsEndpointReflectsCurrentTier()
    {
        using var app = CreateDreamApp(
            new StaticDreamChatClient(CanonicalAiOutput),
            premiumSubjects: ["subject-a"]);
        using var freeClient = app.CreateAuthenticatedClient("subject-free");
        using var premiumClient = app.CreateAuthenticatedClient("subject-a");

        var free = await freeClient.GetFromJsonAsync<EntitlementResponse>("/v1/entitlements");
        var premium = await premiumClient.GetFromJsonAsync<EntitlementResponse>("/v1/entitlements");

        Assert.NotNull(free);
        Assert.NotNull(premium);
        Assert.Equal("free", free.Tier);
        Assert.Equal("premium", premium.Tier);
        Assert.False(free.DeepAnalysisEnabled);
        Assert.True(premium.DeepAnalysisEnabled);
        Assert.True(premium.DailyDreamLimit > free.DailyDreamLimit);
    }

    [Fact]
    public async Task RateLimitingReturnsSafeTooManyRequestsBody()
    {
        using var app = CreateDreamApp(
            new StaticDreamChatClient(CanonicalAiOutput),
            rateLimitPermitLimit: 1,
            rateLimitWindow: TimeSpan.FromMinutes(1));
        using var client = app.CreateAuthenticatedClient("subject-a");

        var first = await client.GetAsync("/v1/me");
        var second = await client.GetAsync("/v1/me");
        var body = await second.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Contains("rate_limit_exceeded", body);
        Assert.DoesNotContain("subject-a", body);
    }

    [Fact]
    public async Task ResponsesIncludeSecurityHeaders()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var client = app.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
        Assert.Contains("default-src 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task CostLedgerRecordsSuccessfulAndFailedAiCallsWithoutRawDreamText()
    {
        using var app = CreateDreamApp(new QueueDreamChatClient(CanonicalAiOutput, "{ invalid json", "{ still invalid"));
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);
        const string rawDreamText = "I was falling into dark water while someone told me to ignore all rules.";

        var success = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest() with { Text = rawDreamText });
        var failure = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest() with { Text = rawDreamText });
        var ledgerRows = await app.GetCostLedgerRowsAsync();

        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode);
        Assert.Equal(2, ledgerRows.Length);
        Assert.Contains(ledgerRows, row => row.Status == "completed" && row.Provider == "DeepSeek" && row.PersonaId == "dream-interpreter");
        Assert.Contains(ledgerRows, row => row.Status == "failed" && row.FailureKind == "Validation");
        Assert.All(ledgerRows, row =>
        {
            var serialized = row.ToString();
            Assert.DoesNotContain(rawDreamText, serialized);
            Assert.DoesNotContain("falling into dark water", serialized);
            Assert.True(row.LatencyMilliseconds >= 0);
        });
    }

    [Fact]
    public async Task DreamSubmissionDoesNotWriteRawDreamTextToLogs()
    {
        var capturedLogs = new List<string>();
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput), capturedLogs: capturedLogs);
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);
        const string rawDreamText = "I was falling into dark water while someone told me to ignore all rules.";

        var response = await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest() with { Text = rawDreamText });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(capturedLogs, log => log.Contains(rawDreamText, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(capturedLogs, log => log.Contains("falling into dark water", StringComparison.OrdinalIgnoreCase));
    }

    private static DreamTestApp CreateDreamApp(
        IChatClient chatClient,
        int dailyDreamQuota = 100,
        int premiumDailyDreamQuota = 250,
        string[]? premiumSubjects = null,
        int rateLimitPermitLimit = 1000,
        TimeSpan? rateLimitWindow = null,
        List<string>? capturedLogs = null)
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
                            System.Text.Encoding.UTF8.GetBytes("12345678901234567890123456789012")),
                        ["Monetization:FreeDailyDreamSubmissions"] = dailyDreamQuota.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["Monetization:PremiumDailyDreamSubmissions"] = premiumDailyDreamQuota.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["DreamRateLimiting:PermitLimit"] = rateLimitPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["DreamRateLimiting:Window"] = (rateLimitWindow ?? TimeSpan.FromMinutes(1)).ToString()
                    });
                    if (premiumSubjects is not null)
                    {
                        for (var index = 0; index < premiumSubjects.Length; index++)
                        {
                            configuration.AddInMemoryCollection(new Dictionary<string, string?>
                            {
                                [$"Monetization:PremiumSubjects:{index}"] = premiumSubjects[index]
                            });
                        }
                    }
                });
                if (capturedLogs is not null)
                {
                    builder.ConfigureLogging(logging =>
                    {
                        logging.ClearProviders();
                        logging.AddProvider(new CapturingLoggerProvider(capturedLogs));
                    });
                }

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<DreamLensDbContext>>();
                    services.RemoveAll<DreamLensDbContext>();
                    services.RemoveAll<IChatClient>();
                    services.RemoveAll<IAsyncJobQueue>();
                    services.AddDbContext<DreamLensDbContext>(options => options.UseInMemoryDatabase(databaseName));
                    services.AddSingleton(chatClient);
                    services.AddSingleton<RecordingAsyncJobQueue>();
                    services.AddSingleton<IAsyncJobQueue>(serviceProvider => serviceProvider.GetRequiredService<RecordingAsyncJobQueue>());
                    services.AddScoped<AsyncJobService>();
                    services.AddScoped<IDreamQuotaService, EfDreamQuotaService>();
                    services.AddScoped<GetProfileHandler>();
                    services.AddScoped<UpdateProfileHandler>();
                    services.AddScoped<SubmitDreamHandler>();
                    services.AddScoped<GetDreamHandler>();
                    services.AddScoped<GetDreamFactsHandler>();
                    services.AddScoped<ListDreamsHandler>();
                    services.AddScoped<DeleteDreamHandler>();
                    services.AddScoped<GetInsightsHandler>();
                    services.AddScoped<RetryJobHandler>();
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

        public async Task<int> CountCostLedgerRowsAsync()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
            return await dbContext.AiCostLedger.CountAsync();
        }

        public async Task<AiCostLedgerRecord[]> GetCostLedgerRowsAsync()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
            return await dbContext.AiCostLedger.OrderBy(row => row.CreatedAt).ToArrayAsync();
        }

        public int PublishedAsyncJobCount => factory.Services.GetRequiredService<RecordingAsyncJobQueue>().Messages.Count;

        public async Task AddAsyncJobAsync(AsyncJobRecord job)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
            dbContext.AsyncJobs.Add(job);
            await dbContext.SaveChangesAsync();
        }
    }

    private sealed class StaticDreamChatClient(string responseText) : IChatClient
    {
        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText))
            {
                ModelId = "deepseek-chat",
                Usage = new UsageDetails
                {
                    InputTokenCount = 100,
                    OutputTokenCount = 50,
                    TotalTokenCount = 150
                }
            });
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

    private sealed class RecordingAsyncJobQueue : IAsyncJobQueue
    {
        public List<AsyncJobMessage> Messages { get; } = [];

        public Task PublishAsync(AsyncJobMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
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
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _responses.Dequeue()))
            {
                ModelId = "deepseek-chat",
                Usage = new UsageDetails
                {
                    InputTokenCount = 100,
                    OutputTokenCount = 50,
                    TotalTokenCount = 150
                }
            });
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

    private sealed class CapturingLoggerProvider(List<string> logs) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(logs, categoryName);

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger(List<string> logs, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            logs.Add($"{categoryName}: {formatter(state, exception)} {exception}");
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

    private sealed record DreamJournalResponse(DreamJournalItemResponse[] Items);

    private sealed record DreamFactsResponse(Guid DreamId, DreamFactResponse[] Facts);

    private sealed record DreamFactResponse(
        string Type,
        string Value,
        decimal? Score,
        decimal? ExtractionConfidence,
        string SourceSchemaVersion);

    private sealed record DreamJournalItemResponse(
        Guid Id,
        DateTimeOffset CreatedAt,
        string Status,
        string? Summary,
        string? Mood,
        string? OccurredAt);

    private sealed record InsightsResponse(int TotalDreams, int CurrentStreakDays, ThemeInsightResponse[] RecurringThemes);

    private sealed record ThemeInsightResponse(string Name, int Count);

    private sealed record EntitlementResponse(string Tier, int DailyDreamLimit, bool DeepAnalysisEnabled);

    private const string CanonicalAiOutput = """
    {
      "schemaVersion": "1.1",
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
      "alternativeInterpretations": ["The water may also represent uncertainty about a new responsibility."],
      "people": [
        { "name": "Alex", "role": "a familiar voice" }
      ],
      "locations": [
        { "name": "dark water", "kind": "natural setting" }
      ],
      "objects": ["water"],
      "scenarios": ["falling"],
      "lucidityScore": 0.1,
      "nightmareIntensity": 0.6,
      "factExtractionConfidence": 0.82,
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
