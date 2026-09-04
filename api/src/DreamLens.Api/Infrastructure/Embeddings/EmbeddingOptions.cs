namespace DreamLens.Api.Infrastructure.Embeddings;

public sealed class EmbeddingOptions
{
    public bool Enabled { get; set; }

    public string Provider { get; set; } = "fake";

    public string Model { get; set; } = "amazon.nova-2-multimodal-embeddings-v1:0";

    public int Dimensions { get; set; } = 1024;

    public string Version { get; set; } = "1";

    public decimal InputCostPerMillionTokensUsd { get; set; } = 0.135m;
}
