namespace DreamLens.Api.Infrastructure.Embeddings;

public sealed class EmbeddingOptions
{
    public bool Enabled { get; set; }

    public string Provider { get; set; } = "fake";

    public string Model { get; set; } = "amazon.titan-embed-text-v2:0";

    public int Dimensions { get; set; } = 1024;

    public string Version { get; set; } = "1";
}
