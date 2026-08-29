namespace DreamLens.Api.Infrastructure.Images;

public sealed class ImageGenerationOptions
{
    public bool Enabled { get; set; }

    public string Provider { get; set; } = "fake";

    public string Model { get; set; } = "amazon.nova-canvas-v1:0";

    public string DefaultStyle { get; set; } = "SOFT_DIGITAL_PAINTING";

    public int Width { get; set; } = 1024;

    public int Height { get; set; } = 1024;

    public decimal EstimatedCostUsd { get; set; }
}
