using DreamLens.Api.Infrastructure.Embeddings;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Features.Dreams;

public sealed class GetSimilarDreamsHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IOptions<EmbeddingOptions> embeddingOptions,
    SemanticMemoryService? semanticMemory = null)
{
    public async Task<SimilarDreamsResponse?> HandleAsync(Guid dreamId, int limit, CancellationToken cancellationToken)
    {
        var dream = await dbContext.Dreams
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == dreamId && candidate.UserSubject == currentUser.Subject, cancellationToken);
        if (dream is null)
        {
            return null;
        }

        var sourceEmbedding = await dbContext.DreamEmbeddings
            .AsNoTracking()
            .SingleOrDefaultAsync(embedding => embedding.DreamId == dreamId
                && embedding.UserSubject == currentUser.Subject
                && embedding.Model == embeddingOptions.Value.Model
                && embedding.Dimensions == embeddingOptions.Value.Dimensions
                && embedding.Version == embeddingOptions.Value.Version,
                cancellationToken);
        if (sourceEmbedding is null || semanticMemory is null)
        {
            return new SimilarDreamsResponse(dreamId, []);
        }

        var sourceVector = sourceEmbedding.Embedding.ToArray();
        var candidates = await semanticMemory.FindSimilarAsync(
            currentUser.Subject,
            sourceVector,
            Math.Clamp(limit, 1, 10) + 1,
            cancellationToken);
        var matches = candidates
            .Where(candidate => candidate.DreamId != dreamId)
            .Select(candidate => new { candidate.DreamId, Similarity = CosineSimilarity(sourceVector, candidate.Embedding.ToArray()) })
            .OrderByDescending(candidate => candidate.Similarity)
            .Take(Math.Clamp(limit, 1, 10))
            .ToArray();
        var matchIds = matches.Select(match => match.DreamId).ToArray();
        var matchedDreams = await dbContext.Dreams
            .AsNoTracking()
            .Where(candidate => candidate.UserSubject == currentUser.Subject && matchIds.Contains(candidate.Id))
            .ToDictionaryAsync(candidate => candidate.Id, cancellationToken);

        return new SimilarDreamsResponse(
            dreamId,
            matches
                .Where(match => matchedDreams.ContainsKey(match.DreamId))
                .Select(match =>
                {
                    var matchedDream = matchedDreams[match.DreamId];
                    return new SimilarDreamResponse(
                        matchedDream.Id,
                        DreamMapper.ReadSummary(matchedDream),
                        matchedDream.OccurredAt,
                        Math.Round(match.Similarity, 4));
                })
                .ToArray());
    }

    private static decimal CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length != right.Length || left.Length == 0)
        {
            return 0;
        }

        double dotProduct = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;
        for (var index = 0; index < left.Length; index++)
        {
            dotProduct += left[index] * right[index];
            leftMagnitude += left[index] * left[index];
            rightMagnitude += right[index] * right[index];
        }

        return leftMagnitude == 0 || rightMagnitude == 0
            ? 0
            : (decimal)(dotProduct / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude)));
    }
}
