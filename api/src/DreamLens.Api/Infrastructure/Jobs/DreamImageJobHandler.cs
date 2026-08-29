using System.Diagnostics;
using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Infrastructure.Assets;
using DreamLens.Api.Infrastructure.Images;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class DreamImageJobHandler(
    DreamLensDbContext dbContext,
    IImageGenerator imageGenerator,
    IPrivateAssetStore assetStore,
    IOptions<ImageGenerationOptions> options) : IAsyncJobHandler
{
    public string JobType => AsyncJobTypes.DreamImage;

    public async Task HandleAsync(AsyncJobMessage message, CancellationToken cancellationToken)
    {
        var payload = System.Text.Json.JsonSerializer.Deserialize<DreamImageJobPayload>(message.PayloadJson)
            ?? throw new InvalidOperationException("Dream image job payload is invalid.");
        var image = await dbContext.DreamImages.SingleOrDefaultAsync(
            candidate => candidate.Id == payload.ImageId && candidate.UserSubject == message.UserSubject,
            cancellationToken)
            ?? throw new InvalidOperationException("Dream image was not found.");
        if (image.Status == DreamImageStatuses.Completed && !string.IsNullOrWhiteSpace(image.AssetKey))
        {
            return;
        }

        var dream = await dbContext.Dreams.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Id == image.DreamId && candidate.UserSubject == message.UserSubject,
            cancellationToken)
            ?? throw new InvalidOperationException("Dream for image generation was not found.");
        var started = Stopwatch.GetTimestamp();
        image.Status = DreamImageStatuses.Generating;
        image.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await imageGenerator.GenerateAsync(
                new ImageGenerationRequest(BuildPrompt(dream), image.Style),
                cancellationToken);
            var key = $"dream-images/{image.Id:N}.png";
            await using var content = new MemoryStream(result.Content, writable: false);
            await assetStore.PutAsync(key, content, result.ContentType, cancellationToken);
            image.Status = DreamImageStatuses.Completed;
            image.AssetKey = key;
            image.ErrorMessage = null;
            image.UpdatedAt = DateTimeOffset.UtcNow;
            dbContext.AiCostLedger.Add(CreateLedger(message, image.DreamId, result.Provider, result.Model, "completed", null, Stopwatch.GetElapsedTime(started)));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            image.Status = DreamImageStatuses.Failed;
            image.ErrorMessage = exception.Message[..Math.Min(exception.Message.Length, 2000)];
            image.UpdatedAt = DateTimeOffset.UtcNow;
            dbContext.AiCostLedger.Add(CreateLedger(message, image.DreamId, "Amazon Bedrock", options.Value.Model, "failed", exception.GetType().Name, Stopwatch.GetElapsedTime(started)));
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private static string BuildPrompt(DreamRecord dream)
    {
        var summary = DreamMapper.ReadSummary(dream) ?? "A reflective dream scene";
        var clippedSummary = summary.Length <= 700 ? summary : summary[..700];
        return $"A reflective, symbolic dream-inspired scene. {clippedSummary}. Calm composition, no text or letters, no identifiable real people.";
    }

    private AiCostLedgerRecord CreateLedger(
        AsyncJobMessage message,
        Guid dreamId,
        string provider,
        string model,
        string status,
        string? failureKind,
        TimeSpan latency)
    {
        return new AiCostLedgerRecord
        {
            UserSubject = message.UserSubject,
            DreamId = dreamId,
            Provider = provider,
            Model = model,
            PersonaId = "dream-image",
            OperationType = "dream.image",
            Status = status,
            FailureKind = failureKind,
            AttemptCount = 1,
            LatencyMilliseconds = Math.Max(0, (long)latency.TotalMilliseconds),
            EstimatedCostUsd = options.Value.EstimatedCostUsd
        };
    }

    public sealed record DreamImageJobPayload(Guid ImageId);
}
