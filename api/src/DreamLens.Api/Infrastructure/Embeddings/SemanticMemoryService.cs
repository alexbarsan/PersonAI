using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;

namespace DreamLens.Api.Infrastructure.Embeddings;

public sealed class SemanticMemoryService(
    DreamLensDbContext dbContext,
    IEmbeddingProvider embeddingProvider,
    IOptions<EmbeddingOptions> options)
{
    public async Task<DreamEmbedding?> CreateForDreamAsync(
        DreamRecord dream,
        bool historyConsent,
        CancellationToken cancellationToken)
    {
        if (!historyConsent || !options.Value.Enabled)
        {
            return null;
        }

        var result = await embeddingProvider.CreateAsync(dream.Text, cancellationToken);
        var embedding = new DreamEmbedding
        {
            DreamId = dream.Id,
            UserSubject = dream.UserSubject,
            Embedding = new Vector(result.Values),
            Provider = result.Provider,
            Model = result.Model,
            Dimensions = result.Dimensions,
            Version = result.Version
        };

        dbContext.DreamEmbeddings.Add(embedding);
        return embedding;
    }

    public async Task<IReadOnlyList<DreamEmbedding>> FindSimilarAsync(
        string userSubject,
        float[] query,
        int limit,
        CancellationToken cancellationToken)
    {
        var vector = new Vector(query);
        if (!dbContext.Database.IsNpgsql())
        {
            return await dbContext.DreamEmbeddings
                .AsNoTracking()
                .Where(embedding => embedding.UserSubject == userSubject)
                .ToListAsync(cancellationToken);
        }

        return await dbContext.DreamEmbeddings
            .FromSqlInterpolated($"SELECT * FROM \"DreamEmbeddings\" WHERE \"UserSubject\" = {userSubject} ORDER BY \"Embedding\" <=> {vector} LIMIT {Math.Clamp(limit, 1, 20)}")
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
