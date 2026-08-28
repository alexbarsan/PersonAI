using DreamLens.Api.Infrastructure.Embeddings;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class EmbeddingBackfillService(
    DreamLensDbContext dbContext,
    AsyncJobService asyncJobService,
    IOptions<EmbeddingOptions> embeddingOptions,
    IOptions<EmbeddingBackfillOptions> backfillOptions)
{
    public async Task<int> EnqueueMissingAsync(CancellationToken cancellationToken)
    {
        if (!embeddingOptions.Value.Enabled)
        {
            return 0;
        }

        var batchSize = Math.Clamp(backfillOptions.Value.BatchSize, 1, 500);
        var maximum = Math.Clamp(backfillOptions.Value.MaxJobsPerRun, batchSize, 5000);
        var enqueued = 0;

        while (enqueued < maximum)
        {
            var remaining = maximum - enqueued;
            var dreams = await dbContext.Dreams
                .AsNoTracking()
                .Where(dream => dream.Status == "completed"
                    && dbContext.UserProfiles.Any(profile => profile.UserSubject == dream.UserSubject
                        && profile.ConsentAiProcessing
                        && profile.ConsentHistoryUse)
                    && !dbContext.DreamEmbeddings.Any(embedding => embedding.DreamId == dream.Id)
                    && !dbContext.AsyncJobs.Any(job => job.JobType == AsyncJobTypes.DreamEmbedding
                        && job.TargetId == dream.Id))
                .OrderBy(dream => dream.CreatedAt)
                .Take(Math.Min(batchSize, remaining))
                .Select(dream => new { dream.Id, dream.UserSubject })
                .ToArrayAsync(cancellationToken);

            if (dreams.Length == 0)
            {
                return enqueued;
            }

            foreach (var dream in dreams)
            {
                await asyncJobService.EnqueueAsync(
                    $"{AsyncJobTypes.DreamEmbedding}:{dream.Id}:{embeddingOptions.Value.Version}",
                    AsyncJobTypes.DreamEmbedding,
                    dream.UserSubject,
                    dream.Id,
                    new DreamEmbeddingJobHandler.DreamEmbeddingJobPayload(dream.Id),
                    cancellationToken);
                enqueued++;
            }
        }

        return enqueued;
    }
}
