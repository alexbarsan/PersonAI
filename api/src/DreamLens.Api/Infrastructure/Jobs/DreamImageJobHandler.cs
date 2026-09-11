using System.Diagnostics;
using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Infrastructure.Assets;
using DreamLens.Api.Infrastructure.Images;
using DreamLens.Api.Infrastructure.Monetization;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class DreamImageJobHandler(
    DreamLensDbContext dbContext,
    IImageGeneratorRegistry imageGenerators,
    IPrivateAssetStore assetStore,
    IStringEncryptor encryptor) : IAsyncJobHandler
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

        if (string.IsNullOrWhiteSpace(image.EncryptedPrompt) || string.IsNullOrWhiteSpace(image.Provider) || string.IsNullOrWhiteSpace(image.Model))
        {
            throw new InvalidOperationException("Dream image request does not contain a generation snapshot.");
        }
        var started = Stopwatch.GetTimestamp();
        image.Status = DreamImageStatuses.Generating;
        image.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var route = new ImageGenerationRoute(
                Enum.TryParse<EntitlementTier>(image.Tier, true, out var tier) ? tier : EntitlementTier.Premium,
                true,
                image.Provider,
                image.Model,
                image.Width,
                image.Height,
                image.EstimatedCostUsd,
                image.Quality);
            var result = await imageGenerators.GetRequired(image.Provider).GenerateAsync(
                new ImageGenerationRequest(encryptor.Decrypt(image.EncryptedPrompt), image.Style, route),
                cancellationToken);
            var key = $"dream-images/{image.Id:N}.png";
            await using var content = new MemoryStream(result.Content, writable: false);
            await assetStore.PutAsync(key, content, result.ContentType, cancellationToken);
            image.Status = DreamImageStatuses.Completed;
            image.AssetKey = key;
            image.ErrorMessage = null;
            image.UpdatedAt = DateTimeOffset.UtcNow;
            dbContext.AiCostLedger.Add(CreateLedger(message, image, result.Provider, result.Model, "completed", null, Stopwatch.GetElapsedTime(started)));
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            image.Status = DreamImageStatuses.Failed;
            image.ErrorMessage = exception.Message[..Math.Min(exception.Message.Length, 2000)];
            image.UpdatedAt = DateTimeOffset.UtcNow;
            dbContext.AiCostLedger.Add(CreateLedger(message, image, image.Provider, image.Model, "failed", exception.GetType().Name, Stopwatch.GetElapsedTime(started)));
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private AiCostLedgerRecord CreateLedger(
        AsyncJobMessage message,
        DreamImageRecord image,
        string provider,
        string model,
        string status,
        string? failureKind,
        TimeSpan latency)
    {
        return new AiCostLedgerRecord
        {
            UserSubject = message.UserSubject,
            DreamId = image.DreamId,
            Provider = provider,
            Model = model,
            PersonaId = "dream-image",
            OperationType = "dream.image",
            Status = status,
            FailureKind = failureKind,
            AttemptCount = 1,
            LatencyMilliseconds = Math.Max(0, (long)latency.TotalMilliseconds),
            EstimatedCostUsd = image.EstimatedCostUsd
        };
    }

    public sealed record DreamImageJobPayload(Guid ImageId);
}
