namespace DreamLens.Api.Infrastructure.Embeddings;

public interface IEmbeddingProvider
{
    Task<EmbeddingResult> CreateAsync(
        string input,
        EmbeddingPurpose purpose,
        CancellationToken cancellationToken);
}

public enum EmbeddingPurpose
{
    Index,
    TextRetrieval
}
