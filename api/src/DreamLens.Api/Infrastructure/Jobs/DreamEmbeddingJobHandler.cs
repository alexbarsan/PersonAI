using System.Diagnostics;
using System.Text.Json;
using DreamLens.Api.Infrastructure.Embeddings;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class DreamEmbeddingJobHandler(
    DreamLensDbContext dbContext,
    SemanticMemoryService semanticMemory,
    IOptions<EmbeddingOptions> options) : IAsyncJobHandler
{
    public string JobType => AsyncJobTypes.DreamEmbedding;

    public async Task HandleAsync(AsyncJobMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<DreamEmbeddingJobPayload>(message.PayloadJson)
            ?? throw new InvalidOperationException("Embedding job payload is invalid.");

        var dream = await dbContext.Dreams.SingleOrDefaultAsync(
            candidate => candidate.Id == payload.DreamId && candidate.UserSubject == message.UserSubject,
            cancellationToken)
            ?? throw new InvalidOperationException("Dream for embedding job was not found.");

        var profile = await dbContext.UserProfiles.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.UserSubject == message.UserSubject,
            cancellationToken)
            ?? throw new InvalidOperationException("Profile for embedding job was not found.");

        var started = Stopwatch.GetTimestamp();
        try
        {
            var creation = await semanticMemory.CreateForDreamAsync(
                dream,
                profile.ConsentHistoryUse,
                cancellationToken);

            if (creation is not null)
            {
                var inputTokens = creation.ProviderResult.InputTokens;
                dbContext.AiCostLedger.Add(CreateLedger(
                    message,
                    dream.Id,
                    creation.ProviderResult.Provider,
                    creation.ProviderResult.Model,
                    "completed",
                    null,
                    inputTokens,
                    creation.ProviderResult.EstimatedCostUsd,
                    Stopwatch.GetElapsedTime(started)));
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception exception)
        {
            dbContext.AiCostLedger.Add(CreateLedger(
                message,
                dream.Id,
                "Amazon Bedrock",
                options.Value.Model,
                "failed",
                exception.GetType().Name,
                null,
                0,
                Stopwatch.GetElapsedTime(started)));
            await dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private AiCostLedgerRecord CreateLedger(
        AsyncJobMessage message,
        Guid dreamId,
        string provider,
        string model,
        string status,
        string? failureKind,
        int? inputTokens,
        decimal estimatedCostUsd,
        TimeSpan latency) => new()
    {
        UserSubject = message.UserSubject,
        DreamId = dreamId,
        Provider = provider,
        Model = model,
        PersonaId = "dream-embedding",
        OperationType = "dream.embedding",
        Status = status,
        FailureKind = failureKind,
        AttemptCount = 1,
        InputTokens = inputTokens,
        TotalTokens = inputTokens,
        LatencyMilliseconds = Math.Max(0, (long)latency.TotalMilliseconds),
        EstimatedCostUsd = estimatedCostUsd
    };

    public sealed record DreamEmbeddingJobPayload(Guid DreamId);
}
