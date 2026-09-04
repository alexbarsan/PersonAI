using System.Net;
using System.Net.Http.Json;
using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Features.Insights;
using DreamLens.Api.Features.Jobs;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Features.Privacy;
using DreamLens.Api.Features.Voice;
using DreamLens.Api.Infrastructure.Assets;
using DreamLens.Api.Infrastructure.Embeddings;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Quotas;
using DreamLens.Api.Infrastructure.Voice;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Pgvector;

namespace DreamLens.Api.IntegrationTests;

public sealed class DreamEndpointTests
{
    private static readonly Guid AskSourceDreamId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task PremiumUserCanCreateAndReuseDeepInterpretationWithRelatedDreamContext()
    {
        var chatClient = new StaticDreamChatClient(CanonicalAiOutput);
        using var app = CreateDreamApp(
            chatClient,
            premiumSubjects: ["subject-a"],
            embeddingsEnabled: true);
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);
        var sourceId = Guid.NewGuid();
        var relatedId = Guid.NewGuid();
        await app.AddSemanticDreamAsync(sourceId, "subject-a", "I crossed a river while beginning a new job.");
        await app.AddSemanticDreamAsync(relatedId, "subject-a", "A familiar river returned beside an open door.");
        await app.AddSemanticDreamAsync(Guid.NewGuid(), "subject-b", "Another user's private dream.");

        var createdResponse = await client.PostAsync($"/v1/dreams/{sourceId}/deep-interpretation", null);
        var repeatedResponse = await client.PostAsync($"/v1/dreams/{sourceId}/deep-interpretation", null);
        var getResponse = await client.GetAsync($"/v1/dreams/{sourceId}/deep-interpretation");

        Assert.Equal(HttpStatusCode.OK, createdResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, repeatedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var created = await createdResponse.Content.ReadFromJsonAsync<DeepInterpretationResponse>();
        Assert.NotNull(created);
        Assert.Equal("deepseek-v4-pro", created.Model);
        Assert.Equal(relatedId, Assert.Single(created.Sources).Id);
        Assert.Single(chatClient.Calls);
        Assert.Equal("deepseek-v4-pro", chatClient.Calls[0].Options?.ModelId);
        Assert.Contains("A familiar river returned beside an open door.", chatClient.Calls[0].Messages.Single().Text);
        Assert.DoesNotContain("Another user's private dream.", chatClient.Calls[0].Messages.Single().Text);
        var ledger = Assert.Single(await app.GetCostLedgerRowsAsync());
        Assert.Equal("dream.deep-interpretation", ledger.OperationType);
        Assert.Equal("deepseek-v4-pro", ledger.Model);
        Assert.Equal("completed", ledger.Status);
        Assert.Equal(0.00033m, ledger.EstimatedCostUsd);
        Assert.Equal(1, await app.CountDeepInterpretationsAsync());
    }

    [Fact]
    public async Task DeepInterpretationRequiresPremiumAndDoesNotCallProvider()
    {
        var chatClient = new StaticDreamChatClient(CanonicalAiOutput);
        using var app = CreateDreamApp(chatClient, embeddingsEnabled: true);
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);
        var dreamId = Guid.NewGuid();
        await app.AddSemanticDreamAsync(dreamId, "subject-a", "I crossed a river while beginning a new job.");

        var response = await client.PostAsync($"/v1/dreams/{dreamId}/deep-interpretation", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Empty(chatClient.Calls);
        Assert.Equal(0, await app.CountDeepInterpretationsAsync());
    }

    [Fact]
    public async Task DeepInterpretationDoesNotExposeOrProcessAnotherUsersDream()
    {
        var chatClient = new StaticDreamChatClient(CanonicalAiOutput);
        using var app = CreateDreamApp(
            chatClient,
            premiumSubjects: ["subject-a"],
            embeddingsEnabled: true);
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);
        var dreamId = Guid.NewGuid();
        await app.AddSemanticDreamAsync(dreamId, "subject-b", "Another user's private dream.");

        var getResponse = await client.GetAsync($"/v1/dreams/{dreamId}/deep-interpretation");
        var createResponse = await client.PostAsync($"/v1/dreams/{dreamId}/deep-interpretation", null);

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, createResponse.StatusCode);
        Assert.Empty(chatClient.Calls);
        Assert.Equal(0, await app.CountDeepInterpretationsAsync());
    }

    [Fact]
    public async Task DeepInterpretationEnforcesCompletedDailyQuota()
    {
        var chatClient = new StaticDreamChatClient(CanonicalAiOutput);
        using var app = CreateDreamApp(
            chatClient,
            premiumSubjects: ["subject-a"],
            embeddingsEnabled: true,
            deepDailyLimit: 1);
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);
        var firstDreamId = Guid.NewGuid();
        var secondDreamId = Guid.NewGuid();
        await app.AddSemanticDreamAsync(firstDreamId, "subject-a", "I crossed a river while beginning a new job.");
        await app.AddSemanticDreamAsync(secondDreamId, "subject-a", "I found an open door beside the same river.");

        var firstResponse = await client.PostAsync($"/v1/dreams/{firstDreamId}/deep-interpretation", null);
        var response = await client.PostAsync($"/v1/dreams/{secondDreamId}/deep-interpretation", null);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Single(chatClient.Calls);
    }

    [Fact]
    public async Task AskDreamHistoryUsesOnlyOwnedSemanticMemoryAndRecordsCosts()
    {
        var answer = $$"""
            {"answer":"Water appears near transitions in the indexed dream.","observations":["The source connects water and uncertainty."],"caveat":"This is a reflective observation, not a diagnosis.","referencedDreamIds":["{{AskSourceDreamId}}"]}
            """;
        using var app = CreateDreamApp(new StaticDreamChatClient(answer), embeddingsEnabled: true);
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);
        await app.AddSemanticDreamAsync(AskSourceDreamId, "subject-a", "A river appeared during a period of change.");
        await app.AddSemanticDreamAsync(Guid.NewGuid(), "subject-b", "This other user's dream must never be retrieved.");

        var response = await client.PostAsJsonAsync("/v1/dreams/ask", new AskDreamsRequest("When does water appear?"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AskDreamsResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result.SampleSize);
        Assert.Equal(AskSourceDreamId, Assert.Single(result.Sources).Id);
        var ledger = await app.GetCostLedgerRowsAsync();
        Assert.Equal(["dream.query-embedding", "dream.ask"], ledger.Select(row => row.OperationType).ToArray());
        Assert.All(ledger, row => Assert.Equal("subject-a", row.UserSubject));
    }

    [Fact]
    public async Task AskDreamHistoryFailsClosedWhenMemoryIsNotReady()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient("should not be used"), embeddingsEnabled: true);
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);

        var response = await client.PostAsJsonAsync("/v1/dreams/ask", new AskDreamsRequest("What pattern repeats?"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var ledger = await app.GetCostLedgerRowsAsync();
        Assert.Single(ledger);
        Assert.Equal("dream.query-embedding", ledger[0].OperationType);
    }

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
    public async Task UserCanStoreAndReplaceOwnedInterpretationFeedback()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var userA = app.CreateAuthenticatedClient("subject-a");
        using var userB = app.CreateAuthenticatedClient("subject-b");
        await PutProfileAsync(userA);
        var dream = await (await userA.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest()))
            .Content.ReadFromJsonAsync<DreamResponse>();

        var empty = await (await userA.GetAsync($"/v1/dreams/{dream!.Id}/feedback"))
            .Content.ReadFromJsonAsync<DreamFeedbackResponse>();
        var invalid = await userA.PutAsJsonAsync(
            $"/v1/dreams/{dream.Id}/feedback",
            new UpdateDreamFeedbackRequest("dislike", [], null));
        var dislikedResponse = await userA.PutAsJsonAsync(
            $"/v1/dreams/{dream.Id}/feedback",
            new UpdateDreamFeedbackRequest("dislike", ["too-generic", "missed-details"], "It overlooked the station."));
        var disliked = await dislikedResponse.Content.ReadFromJsonAsync<DreamFeedbackResponse>();
        var otherUser = await userB.GetAsync($"/v1/dreams/{dream.Id}/feedback");
        var likedResponse = await userA.PutAsJsonAsync(
            $"/v1/dreams/{dream.Id}/feedback",
            new UpdateDreamFeedbackRequest("like", [], null));
        var liked = await likedResponse.Content.ReadFromJsonAsync<DreamFeedbackResponse>();

        Assert.NotNull(empty);
        Assert.Null(empty.Rating);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal(HttpStatusCode.OK, dislikedResponse.StatusCode);
        Assert.Equal("dislike", disliked!.Rating);
        Assert.Equal(["too-generic", "missed-details"], disliked.Reasons);
        Assert.Equal("It overlooked the station.", disliked.Details);
        Assert.Equal(HttpStatusCode.NotFound, otherUser.StatusCode);
        Assert.Equal(HttpStatusCode.OK, likedResponse.StatusCode);
        Assert.Equal("like", liked!.Rating);
        Assert.Empty(liked.Reasons);
        Assert.Null(liked.Details);
        Assert.Equal(1, await app.CountInterpretationFeedbackAsync());
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
    public async Task DreamImageRequestIsUnavailableUntilImageGenerationIsEnabled()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);
        var dream = await (await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest()))
            .Content.ReadFromJsonAsync<DreamResponse>();

        var response = await client.PostAsJsonAsync($"/v1/dreams/{dream!.Id}/image", new { style = "SOFT_DIGITAL_PAINTING" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task VoiceUploadIsUnavailableUntilVoiceTranscriptionIsEnabled()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput), premiumSubjects: ["subject-a"]);
        using var client = app.CreateAuthenticatedClient("subject-a");

        using var response = await client.PostAsync("/v1/voice-captures", CreateVoiceUploadContent());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task PremiumUserCanQueueVoiceTranscriptionAndOnlyOwnerCanReadIt()
    {
        using var app = CreateDreamApp(
            new StaticDreamChatClient(CanonicalAiOutput),
            premiumSubjects: ["subject-a"],
            voiceTranscriptionEnabled: true);
        using var owner = app.CreateAuthenticatedClient("subject-a");
        using var otherUser = app.CreateAuthenticatedClient("subject-b");

        using var upload = await owner.PostAsync("/v1/voice-captures", CreateVoiceUploadContent());
        var capture = await upload.Content.ReadFromJsonAsync<VoiceCaptureResponse>();

        Assert.Equal(HttpStatusCode.Accepted, upload.StatusCode);
        Assert.NotNull(capture);
        Assert.Equal("pending", capture.Status);
        Assert.False(capture.RetainRecording);
        Assert.Equal(1, app.PublishedAsyncJobCount);
        Assert.Equal(1, await app.CountVoiceCapturesAsync());

        using var otherResponse = await otherUser.GetAsync($"/v1/voice-captures/{capture.Id}");
        Assert.Equal(HttpStatusCode.NotFound, otherResponse.StatusCode);
    }

    [Fact]
    public async Task FreeUserCannotQueueVoiceTranscription()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput), voiceTranscriptionEnabled: true);
        using var client = app.CreateAuthenticatedClient("subject-a");

        using var response = await client.PostAsync("/v1/voice-captures", CreateVoiceUploadContent());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task VoiceWorkerDeletesNonRetainedRecordingAndWritesCostLedger()
    {
        using var app = CreateDreamApp(
            new StaticDreamChatClient(CanonicalAiOutput),
            premiumSubjects: ["subject-a"],
            voiceTranscriptionEnabled: true);
        using var client = app.CreateAuthenticatedClient("subject-a");

        using var upload = await client.PostAsync("/v1/voice-captures", CreateVoiceUploadContent());
        var queued = await upload.Content.ReadFromJsonAsync<VoiceCaptureResponse>();
        Assert.NotNull(queued);

        await app.ProcessVoiceCaptureAsync();
        var completed = await client.GetFromJsonAsync<VoiceCaptureResponse>($"/v1/voice-captures/{queued.Id}");
        var ledger = await app.GetCostLedgerRowsAsync();

        Assert.NotNull(completed);
        Assert.Equal("completed", completed.Status);
        Assert.NotNull(completed.Transcript);
        Assert.Null(completed.RecordingUrl);
        var voiceCost = Assert.Single(ledger, row => row.OperationType == "voice.transcription" && row.Status == "completed");
        Assert.Equal(0.0012m, voiceCost.EstimatedCostUsd);
    }

    [Fact]
    public async Task UserCanUpdateJournalMetadataAndFilterTheirJournal()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);
        var dream = await (await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest()))
            .Content.ReadFromJsonAsync<DreamResponse>();

        var update = await client.PutAsJsonAsync($"/v1/dreams/{dream!.Id}/journal", new
        {
            mood = "curious",
            sleepQuality = 4,
            tags = new[] { "water", "recurring" },
            occurredAt = "2026-06-15",
            journalNote = "I remembered the water after breakfast."
        });
        var updated = await update.Content.ReadFromJsonAsync<DreamResponse>();
        var filtered = await (await client.GetAsync("/v1/dreams?query=breakfast&mood=curious&tag=water"))
            .Content.ReadFromJsonAsync<DreamJournalResponse>();

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("curious", updated.Mood);
        Assert.Equal(4, updated.SleepQuality);
        Assert.Contains("water", updated.Tags!);
        Assert.Equal("I remembered the water after breakfast.", updated.JournalNote);
        Assert.NotNull(filtered);
        Assert.Single(filtered.Items);
        Assert.Equal(dream.Id, filtered.Items[0].Id);
    }

    [Fact]
    public async Task UserDataExportContainsProfileDreamsAndAiOperations()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput), premiumSubjects: ["subject-a"]);
        using var client = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(client);
        var dream = await (await client.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest()))
            .Content.ReadFromJsonAsync<DreamResponse>();
        await client.PutAsJsonAsync(
            $"/v1/dreams/{dream!.Id}/feedback",
            new UpdateDreamFeedbackRequest("dislike", ["inaccurate"], "The emotion did not fit."));

        var response = await client.GetAsync("/v1/privacy/export");
        var export = await response.Content.ReadFromJsonAsync<UserDataExportResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(export);
        Assert.Equal(33, export.Profile.Age);
        Assert.Single(export.Dreams);
        Assert.Contains("falling into dark water", export.Dreams[0].Text, StringComparison.Ordinal);
        var exportedFeedback = Assert.IsType<UserDataExportInterpretationFeedback>(export.Dreams[0].Feedback);
        Assert.Equal("dislike", exportedFeedback.Rating);
        Assert.Equal("The emotion did not fit.", exportedFeedback.Details);
        Assert.Single(export.AiOperations);
        Assert.Equal("dream.interpretation", export.AiOperations[0].OperationType);
    }

    [Fact]
    public async Task FreeUserCannotExportData()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var client = app.CreateAuthenticatedClient("subject-a");

        var response = await client.GetAsync("/v1/privacy/export");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminApprovedAnonymizationRemovesUserContentAndBlocksFurtherAccess()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput));
        using var user = app.CreateAuthenticatedClient("subject-a");
        using var nonAdmin = app.CreateAuthenticatedClient("subject-b");
        using var admin = app.CreateAuthenticatedClient("privacy-admin", "dreamlens-admin");
        await PutProfileAsync(user);
        var dream = await (await user.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest()))
            .Content.ReadFromJsonAsync<DreamResponse>();
        await user.PutAsJsonAsync(
            $"/v1/dreams/{dream!.Id}/feedback",
            new UpdateDreamFeedbackRequest("dislike", ["not-useful"], null));

        var requestResponse = await user.PostAsync("/v1/privacy/anonymization-requests", null);
        var request = await requestResponse.Content.ReadFromJsonAsync<AnonymizationRequestResponse>();
        var unauthorizedApproval = await nonAdmin.PostAsync($"/v1/privacy/admin/anonymization-requests/{request!.Id}/approve", null);
        var approval = await admin.PostAsync($"/v1/privacy/admin/anonymization-requests/{request.Id}/approve", null);

        Assert.Equal(HttpStatusCode.Accepted, requestResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedApproval.StatusCode);
        Assert.Equal(HttpStatusCode.OK, approval.StatusCode);
        await app.AssertAnonymizedAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/v1/dreams")).StatusCode);
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
    public async Task DreamMapAggregatesFactsAndOnlyShowsSupportedTimingPatterns()
    {
        using var app = CreateDreamApp(new StaticDreamChatClient(CanonicalAiOutput), dailyDreamQuota: 10);
        using var userA = app.CreateAuthenticatedClient("subject-a");
        await PutProfileAsync(userA);
        var dates = new[] { "2026-06-08", "2026-06-09", "2026-06-10", "2026-06-11", "2026-06-13", "2026-06-14" };
        var dreams = new List<DreamResponse>();
        foreach (var date in dates)
        {
            var dream = await (await userA.PostAsJsonAsync("/v1/dreams", CreateValidDreamRequest() with { OccurredAt = date }))
                .Content.ReadFromJsonAsync<DreamResponse>();
            dreams.Add(Assert.IsType<DreamResponse>(dream));
        }

        await app.AddDreamFactsAsync(
            CreateFact(dreams[0].Id),
            CreateFact(dreams[1].Id),
            CreateFact(dreams[2].Id),
            CreateFact(dreams[4].Id));

        var response = await userA.GetAsync("/v1/insights");
        var insights = await response.Content.ReadFromJsonAsync<InsightsResponse>();
        var noEmbeddingResponse = await userA.GetAsync($"/v1/dreams/{dreams[0].Id}/similar");
        var similarDreams = await noEmbeddingResponse.Content.ReadFromJsonAsync<SimilarDreamsResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(insights);
        Assert.Equal(6, insights.TotalDreams);
        Assert.Equal(new DateOnly(2026, 6, 8), insights.DateRange!.Start);
        var scenarios = Assert.Single(insights.FactGroups, group => group.Type == "scenario");
        Assert.Contains(scenarios.Facts, fact => fact.Value == "being late" && fact.Count == 4 && fact.PercentageOfDreams == 66.7m);
        Assert.Contains(insights.TimingPatterns, pattern => pattern.Value == "being late" && pattern.WeekdayToWeekendRatio == 1.5m);
        Assert.Equal(HttpStatusCode.OK, noEmbeddingResponse.StatusCode);
        Assert.NotNull(similarDreams);
        Assert.Empty(similarDreams.Matches);
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
        List<string>? capturedLogs = null,
        bool voiceTranscriptionEnabled = false,
        bool embeddingsEnabled = false,
        int deepDailyLimit = 3)
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
                        ["DreamRateLimiting:Window"] = (rateLimitWindow ?? TimeSpan.FromMinutes(1)).ToString(),
                        ["VoiceTranscription:Enabled"] = voiceTranscriptionEnabled.ToString(),
                        ["VoiceTranscription:Provider"] = "fake",
                        ["Embedding:Enabled"] = embeddingsEnabled.ToString(),
                        ["Embedding:Provider"] = "fake",
                        ["DeepInterpretation:Enabled"] = "true",
                        ["DeepInterpretation:Model"] = "deepseek-v4-pro",
                        ["DeepInterpretation:DailyLimit"] = deepDailyLimit.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["DeepInterpretation:InputCostPerMillionTokensUsd"] = "1.32",
                        ["DeepInterpretation:OutputCostPerMillionTokensUsd"] = "3.96"
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
                    services.PostConfigure<DeepInterpretationOptions>(configured => configured.DailyLimit = deepDailyLimit);
                    services.RemoveAll<DbContextOptions<DreamLensDbContext>>();
                    services.RemoveAll<DreamLensDbContext>();
                    services.RemoveAll<IChatClient>();
                    services.RemoveAll<IAsyncJobQueue>();
                    services.RemoveAll<IPrivateAssetStore>();
                    services.AddDbContext<DreamLensDbContext>(options => options.UseInMemoryDatabase(databaseName));
                    services.AddSingleton(chatClient);
                    services.AddSingleton<RecordingAsyncJobQueue>();
                    services.AddSingleton<IAsyncJobQueue>(serviceProvider => serviceProvider.GetRequiredService<RecordingAsyncJobQueue>());
                    services.AddSingleton<IPrivateAssetStore, InMemoryPrivateAssetStore>();
                    services.AddScoped<AsyncJobService>();
                    services.AddScoped<IAsyncJobHandler, VoiceTranscriptionJobHandler>();
                    services.AddScoped<IAnonymizedUserAccessService, AnonymizedUserAccessService>();
                    services.AddScoped<IDreamQuotaService, EfDreamQuotaService>();
                    services.AddScoped<GetProfileHandler>();
                    services.AddScoped<UpdateProfileHandler>();
                    services.AddScoped<SubmitDreamHandler>();
                    services.AddScoped<GetDreamHandler>();
                    services.AddScoped<GetDreamFactsHandler>();
                    services.AddScoped<GetSimilarDreamsHandler>();
                    services.AddScoped<GetDreamFeedbackHandler>();
                    services.AddScoped<UpdateDreamFeedbackHandler>();
                    services.AddScoped<SemanticMemoryService>();
                    services.AddScoped<AskDreamsHandler>();
                    services.AddScoped<DeepInterpretationHandler>();
                    services.AddScoped<RequestDreamImageHandler>();
                    services.AddScoped<GetDreamImageHandler>();
                    services.AddScoped<ListDreamsHandler>();
                    services.AddScoped<UpdateDreamJournalHandler>();
                    services.AddScoped<DeleteDreamHandler>();
                    services.AddScoped<GetInsightsHandler>();
                    services.AddScoped<RetryJobHandler>();
                    services.AddScoped<RequestAnonymizationHandler>();
                    services.AddScoped<GetAnonymizationRequestHandler>();
                    services.AddScoped<ListAnonymizationRequestsHandler>();
                    services.AddScoped<ApproveAnonymizationHandler>();
                    services.AddScoped<ExportUserDataHandler>();
                    services.AddScoped<UploadVoiceCaptureHandler>();
                    services.AddScoped<GetVoiceCaptureHandler>();
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

    private static MultipartFormDataContent CreateVoiceUploadContent()
    {
        var form = new MultipartFormDataContent();
        var audio = new ByteArrayContent([1, 2, 3, 4]);
        audio.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/webm");
        form.Add(audio, "audio", "dream.webm");
        form.Add(new StringContent("12"), "durationSeconds");
        form.Add(new StringContent("false"), "retainRecording");
        return form;
    }

    private sealed class DreamTestApp(WebApplicationFactory<Program> factory) : IDisposable
    {
        public HttpClient CreateClient() => factory.CreateClient();

        public HttpClient CreateAuthenticatedClient(string subject, string? groups = null)
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Subject", subject);
            if (!string.IsNullOrWhiteSpace(groups))
            {
                client.DefaultRequestHeaders.Add("X-Test-Groups", groups);
            }

            return client;
        }

        public async Task AssertAnonymizedAsync()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
            Assert.Empty(await dbContext.UserProfiles.ToArrayAsync());
            Assert.Empty(await dbContext.Dreams.ToArrayAsync());
            Assert.Empty(await dbContext.DreamInterpretationFeedback.ToArrayAsync());
            Assert.Empty(await dbContext.DreamFacts.ToArrayAsync());
            Assert.Empty(await dbContext.DreamEmbeddings.ToArrayAsync());
            Assert.Empty(await dbContext.AsyncJobs.ToArrayAsync());
            Assert.Empty(await dbContext.DreamImages.ToArrayAsync());
            Assert.Single(await dbContext.AnonymizedUserTombstones.ToArrayAsync());
            var ledger = Assert.Single(await dbContext.AiCostLedger.ToArrayAsync());
            Assert.StartsWith("anon_", ledger.UserSubject, StringComparison.Ordinal);
            Assert.Null(ledger.DreamId);
            var request = Assert.Single(await dbContext.AnonymizationRequests.ToArrayAsync());
            Assert.Null(request.RequestingUserSubject);
            Assert.Equal(AnonymizationRequestStatuses.Approved, request.Status);
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

        public async Task<int> CountVoiceCapturesAsync()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
            return await dbContext.VoiceCaptures.CountAsync();
        }

        public async Task<int> CountInterpretationFeedbackAsync()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
            return await dbContext.DreamInterpretationFeedback.CountAsync();
        }

        public async Task<int> CountDeepInterpretationsAsync()
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
            return await dbContext.DreamDeepInterpretations.CountAsync();
        }

        public async Task ProcessVoiceCaptureAsync()
        {
            var message = factory.Services.GetRequiredService<RecordingAsyncJobQueue>().Messages
                .Single(job => job.JobType == AsyncJobTypes.VoiceTranscription);
            await using var scope = factory.Services.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetServices<IAsyncJobHandler>()
                .Single(candidate => candidate.JobType == AsyncJobTypes.VoiceTranscription);
            await handler.HandleAsync(message, CancellationToken.None);
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

        public async Task AddDreamFactsAsync(params DreamFactRecord[] facts)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
            dbContext.DreamFacts.AddRange(facts);
            await dbContext.SaveChangesAsync();
        }

        public async Task AddSemanticDreamAsync(Guid id, string userSubject, string summary)
        {
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
            dbContext.Dreams.Add(new DreamRecord
            {
                Id = id,
                UserSubject = userSubject,
                Text = summary,
                TagsJson = "[]",
                Status = "completed",
                ResultJson = System.Text.Json.JsonSerializer.Serialize(new DreamResultResponse(summary, [], []))
            });
            dbContext.DreamEmbeddings.Add(new DreamEmbedding
            {
                DreamId = id,
                UserSubject = userSubject,
                Embedding = new Vector(new float[1024]),
                Provider = "fake",
                Model = "amazon.nova-2-multimodal-embeddings-v1:0",
                Dimensions = 1024,
                Version = "2"
            });
            await dbContext.SaveChangesAsync();
        }
    }

    private sealed class StaticDreamChatClient(string responseText) : IChatClient
    {
        public List<StaticDreamChatCall> Calls { get; } = [];

        public Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Calls.Add(new StaticDreamChatCall(messages.ToArray(), options));
            return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText))
            {
                ModelId = options?.ModelId ?? "deepseek-v4-flash",
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

    private sealed record StaticDreamChatCall(IReadOnlyList<ChatMessage> Messages, ChatOptions? Options);

    private sealed class RecordingAsyncJobQueue : IAsyncJobQueue
    {
        public List<AsyncJobMessage> Messages { get; } = [];

        public Task PublishAsync(AsyncJobMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPrivateAssetStore : IPrivateAssetStore
    {
        private readonly Dictionary<string, byte[]> values = [];

        public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, cancellationToken);
            values[key] = buffer.ToArray();
        }

        public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new MemoryStream(values[key], writable: false));
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            values.Remove(key);
            return Task.CompletedTask;
        }

        public string CreateReadUrl(string key) => $"https://assets.invalid/{key}";
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

    private sealed record InsightsResponse(
        int TotalDreams,
        int CurrentStreakDays,
        ThemeInsightResponse[] RecurringThemes,
        InsightDateRangeResponse? DateRange,
        FactInsightGroupResponse[] FactGroups,
        TimingPatternInsightResponse[] TimingPatterns,
        MonthlyDreamCountResponse[] MonthlyDreamCounts);

    private sealed record SimilarDreamsResponse(Guid DreamId, SimilarDreamResponse[] Matches);

    private sealed record SimilarDreamResponse(Guid Id, string? Summary, string? OccurredAt, decimal Similarity);

    private sealed record InsightDateRangeResponse(DateOnly Start, DateOnly End);

    private sealed record FactInsightGroupResponse(string Type, string Title, FactInsightResponse[] Facts);

    private sealed record FactInsightResponse(string Value, int Count, decimal PercentageOfDreams, decimal? AverageScore);

    private sealed record TimingPatternInsightResponse(
        string Type,
        string Value,
        int Occurrences,
        int WeekdayDreams,
        int WeekendDreams,
        decimal WeekdayRate,
        decimal WeekendRate,
        decimal WeekdayToWeekendRatio);

    private sealed record MonthlyDreamCountResponse(DateOnly Month, int Count);

    private static DreamFactRecord CreateFact(Guid dreamId)
    {
        return new DreamFactRecord
        {
            DreamId = dreamId,
            UserSubject = "subject-a",
            FactType = "scenario",
            NormalizedValue = "being late",
            DisplayValue = "being late",
            SourceSchemaVersion = "1.1"
        };
    }

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
