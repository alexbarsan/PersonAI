using System.Diagnostics;
using System.Text.Json;
using DreamLens.Api.Infrastructure.Images;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class DreamImageSafetyJobHandler(
    DreamLensDbContext dbContext,
    IImagePromptSafetyClassifier classifier) : IAsyncJobHandler
{
    public string JobType => AsyncJobTypes.DreamImageSafety;

    public async Task HandleAsync(AsyncJobMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<DreamImageSafetyJobPayload>(message.PayloadJson)
            ?? throw new InvalidOperationException("Dream image safety job payload is invalid.");
        var classification = await dbContext.DreamImageSafety.SingleOrDefaultAsync(
            candidate => candidate.Id == payload.ClassificationId && candidate.UserSubject == message.UserSubject,
            cancellationToken)
            ?? throw new InvalidOperationException("Dream image safety classification was not found.");
        if (classification.Status == DreamImageSafetyStatuses.Completed)
        {
            return;
        }

        var dreamText = await dbContext.Dreams
            .Where(dream => dream.Id == classification.DreamId && dream.UserSubject == message.UserSubject)
            .Select(dream => dream.Text)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Dream was not found for image safety classification.");

        classification.Status = DreamImageSafetyStatuses.Processing;
        classification.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        var started = Stopwatch.GetTimestamp();
        var result = await classifier.ClassifyAsync(dreamText, cancellationToken);
        var latency = Math.Max(0, (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        classification.Provider = result.Provider;
        classification.Model = result.Model;
        classification.PromptMode = result.Mode.ToString().ToLowerInvariant();
        classification.CategoriesJson = JsonSerializer.Serialize(result.Categories);
        classification.Status = result.UsedFallback
            ? DreamImageSafetyStatuses.Failed
            : DreamImageSafetyStatuses.Completed;
        classification.FailureKind = result.UsedFallback ? "SafetyFallback" : null;
        classification.LatencyMilliseconds = latency;
        classification.CompletedAt = DateTimeOffset.UtcNow;
        classification.UpdatedAt = classification.CompletedAt.Value;
        dbContext.AiCostLedger.Add(new AiCostLedgerRecord
        {
            UserSubject = message.UserSubject,
            DreamId = classification.DreamId,
            Provider = result.Provider,
            Model = result.Model,
            PersonaId = "dream-image-safety",
            OperationType = "dream.image.moderation",
            Status = result.UsedFallback ? "failed" : "completed",
            FailureKind = classification.FailureKind,
            AttemptCount = 1,
            LatencyMilliseconds = latency,
            EstimatedCostUsd = 0
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public sealed record DreamImageSafetyJobPayload(Guid ClassificationId);
}
