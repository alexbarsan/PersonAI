using System.Security.Cryptography;
using System.Text.Json;
using DreamLens.Api.Features.Insights;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Tests;

public sealed class GetDreamObservationHandlerTests
{
    [Fact]
    public async Task ReturnsPersistedPatternReflectionAndMonthlyHistory()
    {
        await using var dbContext = new DreamLensDbContext(
            new DbContextOptionsBuilder<DreamLensDbContext>()
                .UseInMemoryDatabase($"dream-observation-{Guid.NewGuid():N}")
                .Options);
        var encryption = new AesGcmStringEncryptor(new EncryptionOptions
        {
            LocalKeyBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        });
        var firstDream = AddDreamWithWater(dbContext, "2026-07-05");
        var secondDream = AddDreamWithWater(dbContext, "2026-09-05");
        var document = new DreamJournalSynthesisDocument(
            "Water recurs.",
            [new DreamJournalSynthesisObservation("Water", "Water recurs.", [firstDream.Id, secondDream.Id])],
            [],
            [new DreamJournalSynthesisPattern(
                "symbol",
                "water",
                "Water appears during two changes in this journal.",
                [firstDream.Id, secondDream.Id])]);
        dbContext.DreamJournalSyntheses.Add(new DreamJournalSynthesisRecord
        {
            UserSubject = "subject-a",
            EncryptedResultJson = encryption.Encrypt(JsonSerializer.Serialize(document, new JsonSerializerOptions(JsonSerializerDefaults.Web))),
            SourceDreamCount = 2,
            SourceLatestDreamAt = DateTimeOffset.UtcNow,
            Provider = "DeepSeek",
            Model = "test-model",
            PromptVersion = "journal-synthesis-v2",
            GeneratedAt = new DateTimeOffset(2026, 9, 6, 1, 0, 0, TimeSpan.Zero)
        });
        await dbContext.SaveChangesAsync();
        var handler = new GetDreamObservationHandler(
            dbContext,
            new StubCurrentUser("subject-a", null, null, "UnitTest"),
            encryption);

        var response = await handler.HandleAsync("symbol", "Water", CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("Water appears during two changes in this journal.", response.PersonalizedInterpretation?.Reflection);
        Assert.Equal("journal-synthesis-v2", response.PersonalizedInterpretation?.PromptVersion);
        Assert.Equal(new DateOnly(2026, 7, 5), response.FirstObservedAt);
        Assert.Equal(new DateOnly(2026, 9, 5), response.LastObservedAt);
        Assert.Equal(2, response.MonthlyOccurrences.Sum(month => month.Count));
        Assert.Contains(response.MonthlyOccurrences, month => month.Month == new DateOnly(2026, 8, 1) && month.Count == 0);
        Assert.All(response.CommonMeanings, meaning => Assert.StartsWith("https://", meaning.Source.Url));
    }

    private static DreamRecord AddDreamWithWater(DreamLensDbContext dbContext, string occurredAt)
    {
        var dream = new DreamRecord
        {
            UserSubject = "subject-a",
            Text = "I crossed water.",
            Status = "completed",
            OccurredAt = occurredAt,
            CreatedAt = DateTimeOffset.Parse($"{occurredAt}T06:00:00Z")
        };
        dbContext.Dreams.Add(dream);
        dbContext.DreamFacts.Add(new DreamFactRecord
        {
            DreamId = dream.Id,
            UserSubject = "subject-a",
            FactType = "symbol",
            NormalizedValue = "water",
            DisplayValue = "water",
            SourceSchemaVersion = "1.1",
            SourceField = "symbols.symbol",
            NormalizationVersion = "v1"
        });
        return dream;
    }

    private sealed record StubCurrentUser(
        string Subject,
        string? Email,
        string? DisplayName,
        string AuthenticationScheme) : ICurrentUser;
}
