namespace DreamLens.Api.Infrastructure.Embeddings;

public sealed record EmbeddingResult(
    float[] Values,
    int? InputTokens,
    string Provider,
    string Model,
    int Dimensions,
    string Version,
    decimal EstimatedCostUsd);
