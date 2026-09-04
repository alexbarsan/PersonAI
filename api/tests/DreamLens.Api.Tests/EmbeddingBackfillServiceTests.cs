using DreamLens.Api.Infrastructure.Embeddings;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;

namespace DreamLens.Api.Tests;

public sealed class EmbeddingBackfillServiceTests
{
    [Fact]
    public async Task EnqueueMissingAsyncRequeuesDreamWithCompletedLegacyEmbeddingJob()
    {
        var options = new DbContextOptionsBuilder<DreamLensDbContext>()
            .UseInMemoryDatabase($"embedding-backfill-{Guid.NewGuid():N}")
            .Options;
        await using var dbContext = new DreamLensDbContext(options);
        var dreamId = Guid.NewGuid();
        dbContext.UserProfiles.Add(new UserProfile
        {
            UserSubject = "subject-a",
            EncryptedTraitsJson = "[]",
            ConsentAiProcessing = true,
            ConsentHistoryUse = true
        });
        dbContext.Dreams.Add(new DreamRecord
        {
            Id = dreamId,
            UserSubject = "subject-a",
            Text = "A river crossed the road.",
            Status = "completed"
        });
        dbContext.DreamEmbeddings.Add(new DreamEmbedding
        {
            DreamId = dreamId,
            UserSubject = "subject-a",
            Embedding = new Vector(new float[1024]),
            Provider = "Amazon Bedrock",
            Model = "amazon.titan-embed-text-v2:0",
            Dimensions = 1024,
            Version = "1"
        });
        dbContext.AsyncJobs.Add(new AsyncJobRecord
        {
            IdempotencyKey = $"{AsyncJobTypes.DreamEmbedding}:{dreamId}:1",
            JobType = AsyncJobTypes.DreamEmbedding,
            UserSubject = "subject-a",
            TargetId = dreamId,
            PayloadJson = "{}",
            Status = AsyncJobStatuses.Completed
        });
        await dbContext.SaveChangesAsync();
        var queue = new RecordingQueue();
        var service = new EmbeddingBackfillService(
            dbContext,
            new AsyncJobService(dbContext, queue),
            Options.Create(new EmbeddingOptions
            {
                Enabled = true,
                Model = "amazon.nova-2-multimodal-embeddings-v1:0",
                Dimensions = 1024,
                Version = "2"
            }),
            Options.Create(new EmbeddingBackfillOptions { BatchSize = 10, MaxJobsPerRun = 10 }));

        var enqueued = await service.EnqueueMissingAsync(CancellationToken.None);

        Assert.Equal(1, enqueued);
        var message = Assert.Single(queue.Messages);
        var job = await dbContext.AsyncJobs.SingleAsync(candidate => candidate.Id == message.JobId);
        Assert.Equal($"{AsyncJobTypes.DreamEmbedding}:{dreamId}:2", job.IdempotencyKey);
        Assert.Equal(AsyncJobStatuses.Pending, job.Status);
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
