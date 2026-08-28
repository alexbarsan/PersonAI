namespace DreamLens.Api.Infrastructure.Embeddings;

public interface IEmbeddingProvider
{
    Task<EmbeddingResult> CreateAsync(string input, CancellationToken cancellationToken);
}
