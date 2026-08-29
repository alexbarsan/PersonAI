using System.Text.Json;
using DreamLens.Api.Infrastructure.Embeddings;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class DreamEmbeddingJobHandler(
    DreamLensDbContext dbContext,
    SemanticMemoryService semanticMemory) : IAsyncJobHandler
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

        var embedding = await semanticMemory.CreateForDreamAsync(
            dream,
            profile.ConsentHistoryUse,
            cancellationToken);

        if (embedding is not null)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public sealed record DreamEmbeddingJobPayload(Guid DreamId);
}
