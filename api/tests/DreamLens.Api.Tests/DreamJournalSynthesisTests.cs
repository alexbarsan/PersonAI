using System.Security.Cryptography;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PersonaKit.Providers;

namespace DreamLens.Api.Tests;

public sealed class DreamJournalSynthesisTests
{
    [Fact]
    public void ParseAndValidateRemovesUnsupportedEvidence()
    {
        var allowedId = Guid.NewGuid();
        var unsupportedId = Guid.NewGuid();
        var json = $$"""
            {
              "summary": "A grounded summary.",
              "observations": [{
                "title": "Repeated water",
                "reflection": "Water may accompany moments of change.",
                "evidenceDreamIds": ["{{allowedId}}", "{{unsupportedId}}"]
              }],
              "reflectionQuestions": ["What feels uncertain now?"]
            }
            """;

        var result = DreamJournalSynthesisJobHandler.ParseAndValidate(json, [allowedId]);

        var observation = Assert.Single(result.Observations);
        Assert.Equal([allowedId], observation.EvidenceDreamIds);
    }

    [Fact]
    public void ParseAndValidateOnlyKeepsPatternsSupportedByExtractedFacts()
    {
        var allowedId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var json = $$"""
            {
              "summary": "A grounded summary.",
              "observations": [{
                "title": "Repeated water",
                "reflection": "Water may accompany moments of change.",
                "evidenceDreamIds": ["{{allowedId}}"]
              }],
              "reflectionQuestions": [],
              "patterns": [{
                "type": "symbol",
                "value": "Water",
                "reflection": "Water appears near transitions in this journal.",
                "evidenceDreamIds": ["{{allowedId}}", "{{otherId}}"]
              }, {
                "type": "symbol",
                "value": "invented moon",
                "reflection": "This was not supplied.",
                "evidenceDreamIds": ["{{allowedId}}"]
              }]
            }
            """;
        var facts = new[]
        {
            new DreamFactRecord
            {
                DreamId = allowedId,
                UserSubject = "subject-a",
                FactType = "symbol",
                NormalizedValue = "water",
                DisplayValue = "water",
                SourceSchemaVersion = "1.1",
                SourceField = "symbols.symbol",
                NormalizationVersion = "v1"
            },
            new DreamFactRecord
            {
                DreamId = otherId,
                UserSubject = "subject-a",
                FactType = "symbol",
                NormalizedValue = "water",
                DisplayValue = "water",
                SourceSchemaVersion = "1.1",
                SourceField = "symbols.symbol",
                NormalizationVersion = "v1"
            }
        };

        var result = DreamJournalSynthesisJobHandler.ParseAndValidate(json, [allowedId, otherId], facts);

        var pattern = Assert.Single(result.Patterns!);
        Assert.Equal("symbol", pattern.Type);
        Assert.Equal("water", pattern.Value);
        Assert.Equal([allowedId, otherId], pattern.EvidenceDreamIds);
    }

    [Fact]
    public async Task EnqueueStaleAsyncCoalescesOneJobPerUserAndDay()
    {
        await using var dbContext = CreateDbContext();
        var profile = AddEligibleJournal(dbContext, "subject-a", 6);
        await dbContext.SaveChangesAsync();
        var queue = new RecordingQueue();
        var service = new DreamJournalSynthesisService(
            dbContext,
            new AsyncJobService(dbContext, queue),
            Options.Create(new DreamJournalSynthesisOptions()));

        var first = await service.EnqueueStaleAsync(CancellationToken.None);
        var second = await service.EnqueueStaleAsync(CancellationToken.None);

        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Assert.Single(dbContext.AsyncJobs);
        Assert.Single(queue.Messages);
        Assert.Contains(profile.Id.ToString("N"), dbContext.AsyncJobs.Single().IdempotencyKey);
    }

    [Fact]
    public async Task HandlerEncryptsResultAndRecordsAiOperation()
    {
        await using var dbContext = CreateDbContext();
        AddEligibleJournal(dbContext, "subject-a", 6);
        await dbContext.SaveChangesAsync();
        var evidenceId = await dbContext.Dreams
            .Where(dream => dream.UserSubject == "subject-a")
            .OrderByDescending(dream => dream.CreatedAt)
            .Select(dream => dream.Id)
            .FirstAsync();
        var response = $$"""
            {
              "summary": "A repeated journey appears across the journal.",
              "observations": [{
                "title": "Returning paths",
                "reflection": "Repeated paths may reflect attention to ongoing transitions.",
                "evidenceDreamIds": ["{{evidenceId}}"]
              }],
              "reflectionQuestions": ["Which transition feels unfinished?"]
            }
            """;
        var encryption = new AesGcmStringEncryptor(new EncryptionOptions
        {
            LocalKeyBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        });
        var settings = Options.Create(new DreamJournalSynthesisOptions());
        var handler = new DreamJournalSynthesisJobHandler(
            dbContext,
            encryption,
            new FakeChatClient(response),
            settings,
            Options.Create(new DreamLens.Api.Features.Insights.DreamPatternRelationshipOptions()));
        var payload = System.Text.Json.JsonSerializer.Serialize(
            new DreamJournalSynthesisJobHandler.DreamJournalSynthesisJobPayload(settings.Value.PromptVersion));

        await handler.HandleAsync(
            new AsyncJobMessage(Guid.NewGuid(), AsyncJobTypes.DreamJournalSynthesis, "subject-a", payload),
            CancellationToken.None);

        var stored = await dbContext.DreamJournalSyntheses.SingleAsync();
        Assert.StartsWith("v1.", stored.EncryptedResultJson);
        Assert.DoesNotContain("Returning paths", stored.EncryptedResultJson);
        Assert.Contains("Returning paths", encryption.Decrypt(stored.EncryptedResultJson));
        var ledger = await dbContext.AiCostLedger.SingleAsync();
        Assert.Equal("dream.journal-synthesis", ledger.OperationType);
        Assert.Equal("completed", ledger.Status);
    }

    private static DreamLensDbContext CreateDbContext() => new(
        new DbContextOptionsBuilder<DreamLensDbContext>()
            .UseInMemoryDatabase($"journal-synthesis-{Guid.NewGuid():N}")
            .Options);

    private static UserProfile AddEligibleJournal(DreamLensDbContext dbContext, string subject, int dreamCount)
    {
        var profile = new UserProfile
        {
            UserSubject = subject,
            EncryptedTraitsJson = "ignored",
            ConsentAiProcessing = true,
            ConsentHistoryUse = true
        };
        dbContext.UserProfiles.Add(profile);
        for (var index = 0; index < dreamCount; index++)
        {
            dbContext.Dreams.Add(new DreamRecord
            {
                UserSubject = subject,
                Text = $"I followed a familiar path beside water {index}.",
                Title = $"Path {index}",
                Status = "completed",
                ResultJson = "{\"summary\":\"A familiar path beside water.\",\"sections\":[],\"followUpQuestions\":[]}",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-index)
            });
        }

        return profile;
    }

    private sealed class RecordingQueue : IAsyncJobQueue
    {
        public List<AsyncJobMessage> Messages { get; } = [];

        public Task PublishAsync(AsyncJobMessage message, CancellationToken cancellationToken)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
